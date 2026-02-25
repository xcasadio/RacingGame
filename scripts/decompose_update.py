"""
QUAL-005: Decompose CarPhysics.Update() into focused helper methods.

Extracted methods:
  HandleRotations(float moveFactor)
  HandleViewDistance(float moveFactor)
  HandleSpeed(float moveFactor)
  UpdateTrackAndPhysics()
"""
import pathlib, sys, re, textwrap

ROOT = pathlib.Path(__file__).parent.parent
CAR = ROOT / "RacingGame.Shared/GameLogic/CarPhysics.cs"

# ──────────────────────────────────────────────────────────────────────────────
# 1.  The four inner-region blocks that we want to peel out of Update().
#     We match by unique leading/trailing lines of each block.
# ──────────────────────────────────────────────────────────────────────────────

# Anchor: first line of #region, last line = #endregion just before next #region
ROTATION_OLD = """\
        #region Handle rotations
        float effectiveSensitivity = MinSensitivity +
                                     GameSettings.Default.ControllerSensitivity;

        // First handle rotations (reduce last value)
        rotationChange *= RotationFrictionFactor;

        // Left/right changes rotation
        if (Input.KeyboardLeftPressed ||
            Input.Keyboard.IsKeyDown(Keys.A))
        {
            rotationChange += effectiveSensitivity *
                MaxRotationPerSec * moveFactor / KeyboardRotationDivisor;
        }
        else if (Input.KeyboardRightPressed ||
                 Input.Keyboard.IsKeyDown(Keys.D) ||
                 Input.Keyboard.IsKeyDown(Keys.E))
        {
            rotationChange -= effectiveSensitivity *
                MaxRotationPerSec * moveFactor / KeyboardRotationDivisor;
        }
        else
        {
            rotationChange = 0;
        }

        if (Input.MouseXMovement != 0)
        {
            rotationChange -= effectiveSensitivity *
                              (Input.MouseXMovement / MouseSteeringDivisor) *
                              MaxRotationPerSec * moveFactor;
        }

        if (Input.IsGamePadConnected)
        {
            // More dynamic force changing with gamepad (slow, faster, etc.)
            rotationChange -= effectiveSensitivity *
                Input.GamePad.ThumbSticks.Left.X *
                MaxRotationPerSec * moveFactor / GamePadAnalogStickDivisor;
            // Also allow pad to simulate same behaviour as on keyboard
            if (Input.GamePad.DPad.Left == ButtonState.Pressed)
            {
                rotationChange += effectiveSensitivity *
                    MaxRotationPerSec * moveFactor / GamePadDPadRotationDivisor;
            }
            else if (Input.GamePad.DPad.Right == ButtonState.Pressed)
            {
                rotationChange -= effectiveSensitivity *
                    MaxRotationPerSec * moveFactor / GamePadDPadRotationDivisor;
            }
        }

        float maxRot = MaxRotationPerSec * moveFactor * MaxRotationMultiplier;

        // Handle car rotation after collision
        if (rotateCarAfterCollision != 0)
        {
            if (rotateCarAfterCollision > maxRot)
            {
                rotationChange += maxRot;
                rotateCarAfterCollision -= maxRot;
            }
            else if (rotateCarAfterCollision < -maxRot)
            {
                rotationChange -= maxRot;
                rotateCarAfterCollision += maxRot;
            }
            else
            {
                rotationChange += rotateCarAfterCollision;
                rotateCarAfterCollision = 0;
            }
        }
        else
        {
            // If we are staying or moving very slowly, limit rotation!
            if (speed < LowSpeedThreshold)
            {
                rotationChange *= LowSpeedRotationBase + LowSpeedRotationFactor * speed / LowSpeedThreshold;
            }
            else
            {
                rotationChange *= 1.0f + (speed - LowSpeedThreshold) / HighSpeedRotationDivisor;
            }
        }

        // Limit rotation change to MaxRotationPerSec * 1.5 (usually for mouse)
        if (rotationChange > maxRot)
        {
            rotationChange = maxRot;
        }

        if (rotationChange < -maxRot)
        {
            rotationChange = -maxRot;
        }

        // Rotate dir around up vector
        // Interpolate rotatation amount.
        virtualRotationAmount += rotationChange;
        // Smooth over 200ms
        float interpolatedRotationChange =
            (rotationChange + virtualRotationAmount) *
            moveFactor / RotationInterpolationFactor;
        virtualRotationAmount -= interpolatedRotationChange;
        if (isCarOnGround)
        {
            carDir = Vector3.TransformNormal(carDir,
                Matrix.CreateFromAxisAngle(carUp, interpolatedRotationChange));
        }

        #endregion"""

ROTATION_NEW = "        HandleRotations(moveFactor);"

# ──────────────────────────────────────────────────────────────────────────────

VIEWDIST_OLD = """\
        #region Handle view distance (page up/down and mouse wheel)
        if (Input.Keyboard.IsKeyDown(Keys.PageUp) ||
            Input.GamePadXPressed)
        {
            viewDistance -= moveFactor * ViewDistanceChangeRate;
        }

        if (Input.Keyboard.IsKeyDown(Keys.PageDown) ||
            Input.GamePadYPressed)
        {
            viewDistance += moveFactor * ViewDistanceChangeRate;
        }

        if (Input.MouseWheelDelta != 0)
        {
            viewDistance -= Input.MouseWheelDelta / MouseWheelViewDivisor;
        }

        // Restrict the camera's distance to a range, but allow the camera
        // to be as far as it likes during the start of race zoom in
        if (ZoomInTime <= 0)
        {
            viewDistance =
                MathHelper.Clamp(viewDistance, MinViewDistance, MaxViewDistance);
        }
        else
        {
            viewDistance = Math.Max(viewDistance, MinViewDistance);
        }

        #endregion"""

VIEWDIST_NEW = "        HandleViewDistance(moveFactor);"

# ──────────────────────────────────────────────────────────────────────────────
# Handle speed region – ends with the closing #endregion just before
# "#region Update track position"
SPEED_REGION_START = "        #region Handle speed"
SPEED_REGION_END   = "        carPitchPhysics.Simulate(moveFactor);\n        #endregion"

SPEED_NEW = "        HandleSpeed(moveFactor);"

# ──────────────────────────────────────────────────────────────────────────────

TRACK_OLD = """\
        #region Update track position and handle physics
        int oldTrackSegmentNumber = trackSegmentNumber;
        // Find out where we currently are on the track.
        RacingGameManager.Landscape.UpdateCarTrackPosition(
            carPos, ref trackSegmentNumber, ref trackSegmentPercent);
        // Was the track segment changed?
        if (trackSegmentNumber != oldTrackSegmentNumber &&
            // And we in game?
            RacingGameManager.InGame && !GameOver)
        {
            // Was this the start? Did we finish a lap?
            if (trackSegmentNumber == 0 &&
                // Ignore if we missed one checkpoint.
                RacingGameManager.Landscape.NewReplay.CheckpointTimes.Count >=
                RacingGameManager.Landscape.CheckpointSegmentPositions.Count - 1)
            {
                // Show time we made for this lap
                BaseGame.UI.AddTimeFadeupEffect((int)GameTimeMilliseconds,
                    UIRenderer.TimeFadeupMode.Normal);

                // We finished this lap, start next
                StartNewLap();
            }
            else
            {
                // Always only check for the next checkpoint
                int num =
                    RacingGameManager.Landscape.NewReplay.CheckpointTimes.Count;
                if (ZoomInTime <= 0 && // Do not check before race starts
                    num <
                    RacingGameManager.Landscape.CheckpointSegmentPositions.Count &&
                    RacingGameManager.Landscape.CheckpointSegmentPositions[num] >
                    oldTrackSegmentNumber &&
                    RacingGameManager.Landscape.CheckpointSegmentPositions[num] <=
                    trackSegmentNumber)
                {
                    // We passed that checkpoint, show time
                    // Show improvements of time stored in best replay.
                    int differenceMs =
                        RacingGameManager.Landscape.CompareCheckpointTime(num);

                    if (differenceMs < 0)
                    {
                        Sound.Play(Sound.Sounds.CheckpointBetter);
                    }
                    else
                    {
                        Sound.Play(Sound.Sounds.CheckpointWorse);
                    }

                    BaseGame.UI.AddTimeFadeupEffect(
                        //normal: (int)GameTimeMilliseconds,
                        Math.Abs(differenceMs),
                        differenceMs < 0 ? UIRenderer.TimeFadeupMode.Minus :
                            UIRenderer.TimeFadeupMode.Plus);

                    // Add this checkpoint time to the current replay
                    RacingGameManager.Landscape.NewReplay.CheckpointTimes.Add(
                        RacingGameManager.Player.GameTimeMilliseconds / 1000.0f);
                }
            }
        }

        // And get the TrackMatrix and track values at this location.
        float roadWidth, nextRoadWidth;
        Matrix trackMatrix =
            RacingGameManager.Landscape.GetTrackPositionMatrix(
                trackSegmentNumber, trackSegmentPercent,
                out roadWidth, out nextRoadWidth);

        // Just set car up from trackMatrix, this should be done
        // better with a more accurate gravity model (see gravity calculation!)
        Vector3 remOldRightVec = CarRight;
        carUp = trackMatrix.Up;
        carDir = Vector3.Cross(carUp, remOldRightVec);

        // Set up the ground and guardrail boundings for the physics calculation.
        Vector3 trackPos = trackMatrix.Translation;
        RacingGameManager.Player.SetGroundPlaneAndGuardRails(
            trackPos, trackMatrix.Up,
            // Construct our guardrail positions for the collision testing
            trackPos - trackMatrix.Right *
            (roadWidth / 2 - GuardRail.InsideRoadDistance / 2),
            trackPos - trackMatrix.Right *
            (roadWidth / 2 - GuardRail.InsideRoadDistance / 2) +
            trackMatrix.Forward,
            trackPos + trackMatrix.Right *
            (nextRoadWidth / 2 - GuardRail.InsideRoadDistance / 2),
            trackPos + trackMatrix.Right *
            (nextRoadWidth / 2 - GuardRail.InsideRoadDistance / 2) +
            trackMatrix.Forward);
        carRenderMatrix = RacingGameManager.Player.UpdateCarMatrixAndCamera();

        // Finally check for collisions with the guard rails.
        // Also handle gravity.
        ApplyGravityAndCheckForCollisions();
        #endregion"""

TRACK_NEW = "        UpdateTrackAndPhysics();"

# ──────────────────────────────────────────────────────────────────────────────
# 2.  The new private methods to inject after the closing #endregion of #region Update
# ──────────────────────────────────────────────────────────────────────────────

NEW_METHODS = r"""
    #region Update helper methods
    /// <summary>
    /// Handles steering input and updates <c>rotationChange</c> and <c>carDir</c>.
    /// Called from <see cref="Update"/> each frame.
    /// </summary>
    private void HandleRotations(float moveFactor)
    {
        float effectiveSensitivity = MinSensitivity +
                                     GameSettings.Default.ControllerSensitivity;

        // First handle rotations (reduce last value)
        rotationChange *= RotationFrictionFactor;

        // Left/right changes rotation
        if (Input.KeyboardLeftPressed ||
            Input.Keyboard.IsKeyDown(Keys.A))
        {
            rotationChange += effectiveSensitivity *
                MaxRotationPerSec * moveFactor / KeyboardRotationDivisor;
        }
        else if (Input.KeyboardRightPressed ||
                 Input.Keyboard.IsKeyDown(Keys.D) ||
                 Input.Keyboard.IsKeyDown(Keys.E))
        {
            rotationChange -= effectiveSensitivity *
                MaxRotationPerSec * moveFactor / KeyboardRotationDivisor;
        }
        else
        {
            rotationChange = 0;
        }

        if (Input.MouseXMovement != 0)
        {
            rotationChange -= effectiveSensitivity *
                              (Input.MouseXMovement / MouseSteeringDivisor) *
                              MaxRotationPerSec * moveFactor;
        }

        if (Input.IsGamePadConnected)
        {
            // More dynamic force changing with gamepad (slow, faster, etc.)
            rotationChange -= effectiveSensitivity *
                Input.GamePad.ThumbSticks.Left.X *
                MaxRotationPerSec * moveFactor / GamePadAnalogStickDivisor;
            // Also allow pad to simulate same behaviour as on keyboard
            if (Input.GamePad.DPad.Left == ButtonState.Pressed)
            {
                rotationChange += effectiveSensitivity *
                    MaxRotationPerSec * moveFactor / GamePadDPadRotationDivisor;
            }
            else if (Input.GamePad.DPad.Right == ButtonState.Pressed)
            {
                rotationChange -= effectiveSensitivity *
                    MaxRotationPerSec * moveFactor / GamePadDPadRotationDivisor;
            }
        }

        float maxRot = MaxRotationPerSec * moveFactor * MaxRotationMultiplier;

        // Handle car rotation after collision
        if (rotateCarAfterCollision != 0)
        {
            if (rotateCarAfterCollision > maxRot)
            {
                rotationChange += maxRot;
                rotateCarAfterCollision -= maxRot;
            }
            else if (rotateCarAfterCollision < -maxRot)
            {
                rotationChange -= maxRot;
                rotateCarAfterCollision += maxRot;
            }
            else
            {
                rotationChange += rotateCarAfterCollision;
                rotateCarAfterCollision = 0;
            }
        }
        else
        {
            // If we are staying or moving very slowly, limit rotation!
            if (speed < LowSpeedThreshold)
            {
                rotationChange *= LowSpeedRotationBase + LowSpeedRotationFactor * speed / LowSpeedThreshold;
            }
            else
            {
                rotationChange *= 1.0f + (speed - LowSpeedThreshold) / HighSpeedRotationDivisor;
            }
        }

        // Limit rotation change to MaxRotationPerSec * 1.5 (usually for mouse)
        if (rotationChange > maxRot)
        {
            rotationChange = maxRot;
        }

        if (rotationChange < -maxRot)
        {
            rotationChange = -maxRot;
        }

        // Rotate dir around up vector
        // Interpolate rotatation amount.
        virtualRotationAmount += rotationChange;
        // Smooth over 200ms
        float interpolatedRotationChange =
            (rotationChange + virtualRotationAmount) *
            moveFactor / RotationInterpolationFactor;
        virtualRotationAmount -= interpolatedRotationChange;
        if (isCarOnGround)
        {
            carDir = Vector3.TransformNormal(carDir,
                Matrix.CreateFromAxisAngle(carUp, interpolatedRotationChange));
        }
    }

    /// <summary>
    /// Handles page-up/down, mouse-wheel and gamepad view-distance changes.
    /// Called from <see cref="Update"/> each frame.
    /// </summary>
    private void HandleViewDistance(float moveFactor)
    {
        if (Input.Keyboard.IsKeyDown(Keys.PageUp) ||
            Input.GamePadXPressed)
        {
            viewDistance -= moveFactor * ViewDistanceChangeRate;
        }

        if (Input.Keyboard.IsKeyDown(Keys.PageDown) ||
            Input.GamePadYPressed)
        {
            viewDistance += moveFactor * ViewDistanceChangeRate;
        }

        if (Input.MouseWheelDelta != 0)
        {
            viewDistance -= Input.MouseWheelDelta / MouseWheelViewDivisor;
        }

        // Restrict the camera's distance to a range, but allow the camera
        // to be as far as it likes during the start of race zoom in
        if (ZoomInTime <= 0)
        {
            viewDistance =
                MathHelper.Clamp(viewDistance, MinViewDistance, MaxViewDistance);
        }
        else
        {
            viewDistance = Math.Max(viewDistance, MinViewDistance);
        }
    }

    /// <summary>
    /// Handles acceleration, friction, braking and car-position update.
    /// Called from <see cref="Update"/> each frame.
    /// </summary>
    private void HandleSpeed(float moveFactor)
    {
        // With keyboard, do heavy changes, but still smooth over 200ms
        // Up or left mouse button accelerates
        // Also support ASDW (querty) and AOEW (dvorak) shooter like controlling!
        float newAccelerationForce = 0.0f;
        if (Input.KeyboardUpPressed ||
            Input.Keyboard.IsKeyDown(Keys.W) ||
            Input.MouseLeftButtonPressed ||
            Input.GamePadAPressed)
        {
            newAccelerationForce +=
                maxAccelerationPerSec;// * moveFactor;
        }
        // Down or right mouse button decelerates
        else if (Input.KeyboardDownPressed ||
                 Input.Keyboard.IsKeyDown(Keys.S) ||
                 Input.Keyboard.IsKeyDown(Keys.O) ||
                 Input.MouseRightButtonPressed)
        {
            newAccelerationForce -=
                maxAccelerationPerSec;// * moveFactor;
        }
        else if (Input.IsGamePadConnected)
        {
            // More dynamic force changing with gamepad (slow, faster, etc.)
            newAccelerationForce +=
                (Input.GamePad.Triggers.Right) *
                maxAccelerationPerSec;// *moveFactor;
            // Also allow pad to simulate same behaviour as on keyboard
            if (Input.GamePad.DPad.Up == ButtonState.Pressed)
            {
                newAccelerationForce +=
                    maxAccelerationPerSec;
            }
            else if (Input.GamePad.DPad.Down == ButtonState.Pressed)
            {
                newAccelerationForce -=
                    maxAccelerationPerSec;
            }
        }

        // Limit acceleration (but drive as fast forwards as possible if we
        // are moving backwards)
        if (speed > 0 &&
            newAccelerationForce > MaxAcceleration)
        {
            newAccelerationForce = MaxAcceleration;
        }

        if (newAccelerationForce < MinAcceleration)
        {
            newAccelerationForce = MinAcceleration;
        }

        // Add acceleration force to total car force, but use the current carDir!
        if (isCarOnGround)
        {
            carForce +=
                carDir * newAccelerationForce * (moveFactor * AccelerationForceFactor);
        }

        // Change speed with standard formula, use acceleration as our force
        float oldSpeed = speed;
        Vector3 speedChangeVector = carForce / carMass;
        // Only use the amount important for our current direction (slower rot)
        if (isCarOnGround &&
            speedChangeVector.Length() > 0)
        {
            float speedApplyFactor =
                Vector3.Dot(Vector3.Normalize(speedChangeVector), carDir);
            if (speedApplyFactor > 1)
            {
                speedApplyFactor = 1;
            }

            speed += speedChangeVector.Length() * speedApplyFactor;
        }

        // Apply friction. Basically we have 2 frictions that slow us down:
        // The friction from the contact of the wheels with the road (rolling
        // friction) and the air friction, which becomes bigger as we drive
        // faster. We need more force to overcome the resistances if we drive
        // faster. Our engine is strong enough to overcome the initial
        // car friction and air friction, but we want simulate that we need
        // more force to overcome the resistances at high speeds.
        // Usually this would require a more complex formula and the car
        // should need more fuel and force at high speeds, we just simulate that
        // by reducing the force depending on the frictions to get the same
        // effect while having our constant forces that are calculated above.

        // Max. air friction to MaxAirFiction, else driving very fast becomes
        // too hard.
        float airFriction = AirFrictionPerSpeed * Math.Abs(speed);
        if (airFriction > MaxAirFriction)
        {
            airFriction = MaxAirFriction;
        }

        // Don't use ground friction if we are not on the ground.
        float groundFriction = CarFrictionOnRoad;
        if (isCarOnGround == false)
        {
            groundFriction = 0;
        }

        carForce *= 1.0f - (0.275f * 0.02125f *
                            0.2f * // 20% for force slowdown
                            (groundFriction + airFriction));
        // Reduce the speed, but use very low values to make the game more fun!
        float noFrictionSpeed = speed;
        speed *= 1.0f - (0.01f *
                         0.1f * 0.02125f *
                         (groundFriction + airFriction));
        // Never change more than by 1
        if (speed < noFrictionSpeed - 1)
        {
            speed = noFrictionSpeed - 1;
        }

        if (isCarOnGround)
        {
            bool downPressed =
                Input.MouseRightButtonPressed ||
                Input.KeyboardDownPressed ||
                Input.GamePad.DPad.Down == ButtonState.Pressed;
            bool isBraking = false;

            if (Input.Keyboard.IsKeyDown(Keys.Space) ||
                Input.MouseMiddleButtonPressed ||
                Input.GamePad.Triggers.Left > 0.5f ||
                Input.GamePadBPressed ||
                // Also use back for this
                downPressed)
            {
                float slowdown =
                    1.0f - moveFactor *
                    // Use only half if we just decelerate
                    (downPressed ? BrakeSlowdown / 2 : BrakeSlowdown) *
                    // Don't brake so much if we are already driving backwards
                    (speed < 0 ? 0.33f : 1.0f);
                speed *= Math.Max(0, slowdown);
                // Limit to max. 100 mph slowdown per sec
                if (speed > oldSpeed + MaxSpeedChangePerSec * moveFactor)
                {
                    speed = (oldSpeed + MaxSpeedChangePerSec * moveFactor);
                }

                if (speed < oldSpeed - MaxSpeedChangePerSec * moveFactor)
                {
                    speed = (oldSpeed - MaxSpeedChangePerSec * moveFactor);
                }

                isBraking = true;
            }

            // Calculate pitch depending on the force
            float speedChange = speed - oldSpeed;

            // Add brake tracks.
            if (speed > 0.5f && speed < 7.5f && speedChange > 5.5f * moveFactor ||
                speed > 0.75f && speedChange < 10 * moveFactor && isBraking)
            {
                Sound.Sounds brakeType =
                    Sound.GetBreakSoundType(speed, speedChange, rotationChange);

                // Add brake tracks for major breaks
                if (brakeType == Sound.Sounds.BrakeCurveMajor ||
                    brakeType == Sound.Sounds.BrakeMajor)
                {
                    RacingGameManager.Landscape.AddBrakeTrack(this);
                }

                // And play sound for braking
                Sound.PlayBrakeSound(brakeType);
            }

            // Limit speed change, never apply more than 5 per sec.
            if (speedChange < -8 * moveFactor)
            {
                speedChange = -8 * moveFactor;
            }

            if (speedChange > 8 * moveFactor)
            {
                speedChange = 8 * moveFactor;
            }

            carPitchPhysics.ChangePos(speedChange);
        }

        // Limit speed
        if (speed > maxSpeed)
        {
            speed = maxSpeed;
        }

        if (speed < -maxSpeed)
        {
            speed = -maxSpeed;
        }

        // Apply speed and calculate new car position.
        carPos += speed * carDir * moveFactor * CarSpeedPositionFactor;

        // Handle pitch spring
        carPitchPhysics.Simulate(moveFactor);
    }

    /// <summary>
    /// Updates the car's track-segment position, aligns <c>carUp</c>/<c>carDir</c>
    /// to the road surface, sets guard-rail bounds and checks for collisions.
    /// Called from <see cref="Update"/> each frame.
    /// </summary>
    private void UpdateTrackAndPhysics()
    {
        int oldTrackSegmentNumber = trackSegmentNumber;
        // Find out where we currently are on the track.
        RacingGameManager.Landscape.UpdateCarTrackPosition(
            carPos, ref trackSegmentNumber, ref trackSegmentPercent);
        // Was the track segment changed?
        if (trackSegmentNumber != oldTrackSegmentNumber &&
            // And we in game?
            RacingGameManager.InGame && !GameOver)
        {
            // Was this the start? Did we finish a lap?
            if (trackSegmentNumber == 0 &&
                // Ignore if we missed one checkpoint.
                RacingGameManager.Landscape.NewReplay.CheckpointTimes.Count >=
                RacingGameManager.Landscape.CheckpointSegmentPositions.Count - 1)
            {
                // Show time we made for this lap
                BaseGame.UI.AddTimeFadeupEffect((int)GameTimeMilliseconds,
                    UIRenderer.TimeFadeupMode.Normal);

                // We finished this lap, start next
                StartNewLap();
            }
            else
            {
                // Always only check for the next checkpoint
                int num =
                    RacingGameManager.Landscape.NewReplay.CheckpointTimes.Count;
                if (ZoomInTime <= 0 && // Do not check before race starts
                    num <
                    RacingGameManager.Landscape.CheckpointSegmentPositions.Count &&
                    RacingGameManager.Landscape.CheckpointSegmentPositions[num] >
                    oldTrackSegmentNumber &&
                    RacingGameManager.Landscape.CheckpointSegmentPositions[num] <=
                    trackSegmentNumber)
                {
                    // We passed that checkpoint, show time
                    // Show improvements of time stored in best replay.
                    int differenceMs =
                        RacingGameManager.Landscape.CompareCheckpointTime(num);

                    if (differenceMs < 0)
                    {
                        Sound.Play(Sound.Sounds.CheckpointBetter);
                    }
                    else
                    {
                        Sound.Play(Sound.Sounds.CheckpointWorse);
                    }

                    BaseGame.UI.AddTimeFadeupEffect(
                        //normal: (int)GameTimeMilliseconds,
                        Math.Abs(differenceMs),
                        differenceMs < 0 ? UIRenderer.TimeFadeupMode.Minus :
                            UIRenderer.TimeFadeupMode.Plus);

                    // Add this checkpoint time to the current replay
                    RacingGameManager.Landscape.NewReplay.CheckpointTimes.Add(
                        RacingGameManager.Player.GameTimeMilliseconds / 1000.0f);
                }
            }
        }

        // And get the TrackMatrix and track values at this location.
        float roadWidth, nextRoadWidth;
        Matrix trackMatrix =
            RacingGameManager.Landscape.GetTrackPositionMatrix(
                trackSegmentNumber, trackSegmentPercent,
                out roadWidth, out nextRoadWidth);

        // Just set car up from trackMatrix, this should be done
        // better with a more accurate gravity model (see gravity calculation!)
        Vector3 remOldRightVec = CarRight;
        carUp = trackMatrix.Up;
        carDir = Vector3.Cross(carUp, remOldRightVec);

        // Set up the ground and guardrail boundings for the physics calculation.
        Vector3 trackPos = trackMatrix.Translation;
        RacingGameManager.Player.SetGroundPlaneAndGuardRails(
            trackPos, trackMatrix.Up,
            // Construct our guardrail positions for the collision testing
            trackPos - trackMatrix.Right *
            (roadWidth / 2 - GuardRail.InsideRoadDistance / 2),
            trackPos - trackMatrix.Right *
            (roadWidth / 2 - GuardRail.InsideRoadDistance / 2) +
            trackMatrix.Forward,
            trackPos + trackMatrix.Right *
            (nextRoadWidth / 2 - GuardRail.InsideRoadDistance / 2),
            trackPos + trackMatrix.Right *
            (nextRoadWidth / 2 - GuardRail.InsideRoadDistance / 2) +
            trackMatrix.Forward);
        carRenderMatrix = RacingGameManager.Player.UpdateCarMatrixAndCamera();

        // Finally check for collisions with the guard rails.
        // Also handle gravity.
        ApplyGravityAndCheckForCollisions();
    }
    #endregion
"""

# ──────────────────────────────────────────────────────────────────────────────
# 3.  The anchor for #endregion of #region Update (the Update method's closing
#     #endregion), after which we inject the new #region Update helper methods.
# ──────────────────────────────────────────────────────────────────────────────
UPDATE_CLOSE = "    #endregion\n\n    #region CheckForCollisions"
UPDATE_CLOSE_NEW = "    #endregion\n" + NEW_METHODS + "\n    #region CheckForCollisions"

def apply():
    text = CAR.read_text(encoding="utf-8")
    errors = []

    def replace_once(old, new, label):
        nonlocal text
        c = text.count(old)
        if c == 0:
            errors.append(f"MISSING: {label}")
        elif c > 1:
            errors.append(f"AMBIGUOUS ({c}×): {label}")
        else:
            text = text.replace(old, new)
            print(f"  OK  {label}")

    replace_once(ROTATION_OLD,  ROTATION_NEW,  "#region Handle rotations → HandleRotations()")
    replace_once(VIEWDIST_OLD,  VIEWDIST_NEW,  "#region Handle view distance → HandleViewDistance()")

    # Speed region: use start+end anchors independently
    start_idx = text.find(SPEED_REGION_START)
    end_idx   = text.find(SPEED_REGION_END)
    if start_idx == -1 or end_idx == -1:
        errors.append("MISSING: #region Handle speed boundaries")
    else:
        # full block = from start to end of SPEED_REGION_END
        block_end = end_idx + len(SPEED_REGION_END)
        text = text[:start_idx] + SPEED_NEW + text[block_end:]
        print("  OK  #region Handle speed → HandleSpeed()")

    replace_once(TRACK_OLD,    TRACK_NEW,    "#region Update track position → UpdateTrackAndPhysics()")
    replace_once(UPDATE_CLOSE, UPDATE_CLOSE_NEW, "inject #region Update helper methods")

    if errors:
        for e in errors:
            print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(1)

    CAR.write_text(text, encoding="utf-8")
    print("CarPhysics.cs updated.")

if __name__ == "__main__":
    apply()

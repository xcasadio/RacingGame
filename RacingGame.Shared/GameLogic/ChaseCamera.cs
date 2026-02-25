#region File Description
//-----------------------------------------------------------------------------
// ChaseCamera.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives

using RacingGame.Graphics;
using RacingGame.Helpers;

#endregion

namespace RacingGame.GameLogic;

/// <summary>
/// Chase camera for our car. We are always close behind it.
/// The camera rotation is not the same as the current car rotation,
/// we interpolate the values a bit, allowing the user to do small changes
/// without rotating the camera frantically. Also feels more realistic in
/// curves. References a <see cref="CarPhysics"/> instance to read car state
/// (position, orientation, zoom-in progress). This camera is not controlled
/// by the user — it is fully automatic!
/// </summary>
public class ChaseCamera
{
    #region Variables
    /// <summary>
    /// Reference to the car physics, used to read LookAtPos, CarUpVector and ZoomInTime
    /// without inheriting from the car physics hierarchy.
    /// </summary>
    private readonly CarPhysics _physics;

    /// <summary>Look-at position, delegated from car physics.</summary>
    private Vector3 LookAtPos => _physics.LookAtPos;

    /// <summary>Car up vector, delegated from car physics.</summary>
    private Vector3 CarUpVector => _physics.CarUpVector;

    /// <summary>
    /// Current camera position.
    /// </summary>
    protected Vector3 cameraPos;
    /// <summary>
    /// Distance of the camera to our car.
    /// </summary>
    private float cameraDistance;

    /// <summary>
    /// Look vector to the car. The car is our look at target. The up
    /// vector is the same as the one from the car, but the look vector is
    /// different because we slowly interpolate it.
    /// </summary>
    private Vector3 cameraLookVector;

    /// <summary>
    /// Camera modes
    /// </summary>
    public enum CameraMode
    {
        /// <summary>
        /// Default mode for game and menu, just chasing the car.
        /// </summary>
        Default,
        /// <summary>
        /// Free camera mode, allows to freely rotate and zoom around the car,
        /// much cooler than the Default mode for testing and stuff.
        /// Also used when we lose a game (fallen out of the track).
        /// </summary>
        FreeCamera,
    }

    /// <summary>
    /// Current camera mode.
    /// </summary>
    private CameraMode cameraMode = CameraMode.Default;//FreeCamera;

    /// <summary>
    /// Rotation matrix, used in UpdateViewMatrix.
    /// </summary>
    private Matrix rotMatrix = Matrix.Identity;
    /// <summary>
    /// Rotation matrix
    /// </summary>
    /// <returns>Matrix</returns>
    public Matrix RotationMatrix
    {
        get
        {
            return rotMatrix;
        }
    }
    #endregion

    #region Camera wobble
    /// <summary>
    /// Max. value for camera wobble timeout.
    /// </summary>
    const int MaxCameraWobbleTimeoutMs = 700;

    /// <summary>
    /// Camera wobble timeout.
    /// Used to shake camera after a collision.
    /// </summary>
    static float cameraWobbleTimeoutMs = 0;

    /// <summary>
    /// Camera wobble factor.
    /// </summary>
    static float cameraWobbleFactor = 1.0f;

    /// <summary>
    /// Sets the camera to wobble which fades over time.
    /// </summary>
    /// <param name="factor">Factor</param>
    public static void WobbleCamera(float wobbleFactor)
    {
        cameraWobbleTimeoutMs = (int)
            //((0.75f + 0.5f * wobbleFactor) *
            (MaxCameraWobbleTimeoutMs);
        cameraWobbleFactor = wobbleFactor;
    }
    #endregion

    #region Properties
    /// <summary>
    /// Camera position
    /// </summary>
    /// <returns>Vector 3</returns>
    public Vector3 CameraPosition
    {
        get
        {
            return cameraPos;
        }
    }

    /// <summary>
    /// Get current x axis with help of the current view matrix.
    /// </summary>
    /// <returns>Vector 3</returns>
    static public Vector3 XAxis
    {
        get
        {
            // Get x column
            return new Vector3(
                BaseGame.ViewMatrix.M11,
                BaseGame.ViewMatrix.M21,
                BaseGame.ViewMatrix.M31);
        }
    }

    /// <summary>
    /// Get current y axis with help of the current view matrix.
    /// </summary>
    /// <returns>Vector 3</returns>
    static public Vector3 YAxis
    {
        get
        {
            // Get y column
            return new Vector3(
                BaseGame.ViewMatrix.M12,
                BaseGame.ViewMatrix.M22,
                BaseGame.ViewMatrix.M32);
        }
    }

    /// <summary>
    /// Get current z axis with help of the current view matrix.
    /// </summary>
    /// <returns>Vector 3</returns>
    static public Vector3 ZAxis
    {
        get
        {
            // Get z column
            return new Vector3(
                BaseGame.ViewMatrix.M13,
                BaseGame.ViewMatrix.M23,
                BaseGame.ViewMatrix.M33);
        }
    }

    /// <summary>
    /// Free camera
    /// </summary>
    /// <returns>Bool</returns>
    public bool FreeCamera
    {
        get
        {
            return cameraMode == CameraMode.FreeCamera;
        }
        set
        {
            if (value == true)
            {
                cameraMode = CameraMode.FreeCamera;
            }
            else
            {
                cameraMode = CameraMode.Default;
            }
        }
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Create a chase camera that follows the given car physics object.
    /// The initial position is placed behind and slightly above the car.
    /// </summary>
    /// <param name="physics">Car physics to follow.</param>
    public ChaseCamera(CarPhysics physics)
    {
        _physics = physics;
        SetCameraPosition(physics.CarPosition + new Vector3(0, 10.0f, 25.0f));
    }
    #endregion

    #region Set position
    /// <summary>
    /// Set camera position
    /// </summary>
    /// <param name="setCameraPos">Set camera position</param>
    public void SetCameraPosition(Vector3 setCameraPos)
    {
        cameraPos = setCameraPos;
        cameraDistance = Vector3.Distance(LookAtPos, cameraPos);
        cameraLookVector = LookAtPos - cameraPos;
        wannaCameraDistance = cameraDistance;
        wannaCameraLookVector = cameraLookVector;

        // Build look at matrix
        rotMatrix = Matrix.CreateLookAt(cameraPos, LookAtPos, CarUpVector);
    }

    Vector3 wannaCameraLookVector = Vector3.Zero;
    float wannaCameraDistance = 0.0f;

    /// <summary>
    /// Interpolate camera position
    /// </summary>
    /// <param name="setInterpolatedCameraPos">Set interpolated camera
    /// position</param>
    public void InterpolateCameraPosition(Vector3 setInterpolatedCameraPos)
    {
        // Don't use for free camera
        if (FreeCamera)
        {
            return;
        }

        if (wannaCameraDistance == 0.0f)
        {
            SetCameraPosition(setInterpolatedCameraPos);
        }

        wannaCameraDistance =
            Vector3.Distance(LookAtPos, setInterpolatedCameraPos);
        wannaCameraLookVector = LookAtPos - setInterpolatedCameraPos;
    }
    #endregion

    #region Handle free camera
    /// <summary>
    /// Helper values to keep the free camera steady.
    /// </summary>
    private Vector3 freeCameraRot = new Vector3(
        MathHelper.Pi, 0, -MathHelper.Pi / 2);
    /// <summary>
    /// Wanna have camera rotation
    /// </summary>
    Vector3 wannaHaveCameraRotation = Vector3.Zero;
    /// <summary>
    /// Handle free camera, only used for unit tests.
    /// </summary>
    private void HandleFreeCamera()
    {
        // Don't control the camera in the menu or game, only in unit tests!
        if (cameraMode != CameraMode.FreeCamera)
        {
            return;
        }

        float rotationFactor = 0.0075f;
        float gamePadRotFactor = 5.0f * BaseGame.MoveFactorPerSecond;

        // We don't use lookDistance or cameraRotation here, so we have
        // to calculate this values here.
        cameraDistance = cameraLookVector.Length();

        if (wannaHaveCameraRotation.Equals(Vector3.Zero))
        {
            wannaHaveCameraRotation = freeCameraRot;
        }

        Vector3 rot = wannaHaveCameraRotation;

        float addRotX =
            // Allow mouse input
            -Input.MouseXMovement * rotationFactor +
            // And gamepad input
            Input.GamePad.ThumbSticks.Left.X * gamePadRotFactor;
        // Also allow gamepad and keyboard cursors
        if (addRotX == 0)
        {
            if (Input.GamePadLeftPressed ||
                Input.KeyboardLeftPressed)
            {
                addRotX = -gamePadRotFactor;
            }

            if (Input.GamePadRightPressed ||
                Input.KeyboardRightPressed)
            {
                addRotX = +gamePadRotFactor;
            }
        }
        float addRotY =
            // Allow mouse input
            -Input.MouseYMovement * rotationFactor +
            // And gamepad input
            Input.GamePad.ThumbSticks.Left.Y * gamePadRotFactor;
        // Also allow gamepad and keyboard cursors
        if (addRotY == 0)
        {
            if (Input.GamePadUpPressed ||
                Input.KeyboardUpPressed)
            {
                addRotY = -gamePadRotFactor;
            }

            if (Input.GamePadDownPressed ||
                Input.KeyboardDownPressed)
            {
                addRotY = +gamePadRotFactor;
            }
        }

        wannaHaveCameraRotation = new Vector3(
            rot.X,
            rot.Y + addRotY,
            rot.Z + addRotX);

        // Mix camera rotation slowly to wanna have rotation
        freeCameraRot = Vector3.Lerp(
            freeCameraRot, wannaHaveCameraRotation, 0.5f);

        #region fix the "up-rotaion" to 0-180 degrees //old: 180-360 degrees
        // Substract a very small value to make sure we never reach PI,
        // this causes the z rotation to mess everything else up ...
        float minRotationRange = BaseGame.Epsilon;
        float maxRotationRange = (float)Math.PI - BaseGame.Epsilon;
        if (freeCameraRot.X < minRotationRange)
        {
            freeCameraRot.X = minRotationRange;
        }
        else if (freeCameraRot.X > maxRotationRange)
        {
            freeCameraRot.X = maxRotationRange;
        }
        #endregion

        // Calculate cameraPos like in HandleLookPosCamera()
        cameraLookVector = new Vector3(0, 0, cameraDistance);
        cameraLookVector = Vector3.TransformNormal(cameraLookVector,
            Matrix.CreateRotationX(freeCameraRot.X) *
            Matrix.CreateRotationY(freeCameraRot.Y) *
            Matrix.CreateRotationZ(freeCameraRot.Z));

        float moveFactor =
            (Input.Keyboard.IsKeyDown(Keys.LeftShift) ? 20.0f : 40.0f) *
            BaseGame.MoveFactorPerSecond;
        float smallMoveFactor = moveFactor / 4.0f;

        float lookDistanceChange = 0.0f;
        // Page up/down or Home/End to zoom in and out.
        if (Input.Keyboard.IsKeyDown(Keys.PageUp))
        {
            lookDistanceChange += moveFactor * 0.05f;
        }

        if (Input.Keyboard.IsKeyDown(Keys.PageDown))
        {
            lookDistanceChange -= moveFactor * 0.05f;
        }

        if (Input.Keyboard.IsKeyDown(Keys.Home))
        {
            lookDistanceChange += smallMoveFactor * 0.05f;
        }

        if (Input.Keyboard.IsKeyDown(Keys.End))
        {
            lookDistanceChange -= smallMoveFactor * 0.05f;
        }

        // Also allow mouse wheel to zoom
        if (Input.MouseWheelDelta != 0)
        {
            lookDistanceChange =
                Input.MouseWheelDelta * BaseGame.MoveFactorPerSecond / 16.0f;
        }

        // Also allow gamepad to zoom
        if (Input.GamePad.ThumbSticks.Right.Y != 0)
        {
            lookDistanceChange =
                Input.GamePad.ThumbSticks.Right.Y * BaseGame.MoveFactorPerSecond;
        }

        if (lookDistanceChange != 0)
        {
            // Half zoom effect if shift is pressed
            if (Input.Keyboard.IsKeyDown(Keys.LeftShift))
            {
                lookDistanceChange /= 2.0f;
            }

            cameraDistance *= 1.0f - lookDistanceChange;
            if (cameraDistance < 1.0f)
            {
                cameraDistance = 1.0f;
            }

            // Calculate cameraPos like in HandleLookPosCamera()
            cameraLookVector = Vector3.TransformNormal(
                new Vector3(0, 0, cameraDistance),
                Matrix.CreateRotationX(freeCameraRot.X) *
                Matrix.CreateRotationY(freeCameraRot.Y) *
                Matrix.CreateRotationZ(freeCameraRot.Z));
        }

        // Make sure we use these new values and don't interpolate them back.
        wannaCameraDistance = cameraDistance;
        wannaCameraLookVector = cameraLookVector;
    }
    #endregion

    #region Update view matrix
    Vector3 lastCameraWobble = Vector3.Zero;
    /// <summary>
    /// Update view matrix
    /// </summary>
    private void UpdateViewMatrix()
    {
        cameraDistance = cameraDistance * 0.9f + wannaCameraDistance * 0.1f;

        // Better interpolation formula, not good for slow framerates,
        // but looks much better on high frame rates this way.
        cameraLookVector =
            (cameraLookVector * 0.9f) +
            (wannaCameraLookVector * 0.1f);

        // Update camera pos based on the current lookPos and cameraDistance
        cameraPos = LookAtPos + cameraLookVector;

        // Build look at matrix
        rotMatrix = Matrix.CreateLookAt(cameraPos, LookAtPos, CarUpVector);

        // Is camera wobbling?
        if (cameraWobbleTimeoutMs > 0)
        {
            cameraWobbleTimeoutMs -= BaseGame.ElapsedTimeThisFrameInMilliseconds;
            if (cameraWobbleTimeoutMs < 0)
            {
                cameraWobbleTimeoutMs = 0;
            }
        }

        // Add camera shake if camera wobble effect is on
        if (cameraWobbleTimeoutMs > 0 &&
            // But only if not zooming in and if in game.
            _physics.ZoomInTime <= BasePlayer.StartGameZoomTimeMilliseconds)
        {
            float effectStrength = 1.5f * cameraWobbleFactor *
                                   (cameraWobbleTimeoutMs / (float)MaxCameraWobbleTimeoutMs);
            // Interpolate, make wobbleing more smoooth than in Rocket Commander
            lastCameraWobble =
                lastCameraWobble * 0.9f +
                RandomHelper.GetRandomVector3(
                    -effectStrength, +effectStrength) * 0.1f;
            rotMatrix *= Matrix.CreateTranslation(lastCameraWobble);
        }

        // Just set view matrix
        BaseGame.ViewMatrix = rotMatrix;
    }
    #endregion

    #region Reset
    /// <summary>
    /// Resets camera wobble.
    /// </summary>
    public void Reset()
    {
        cameraWobbleFactor = 0;
    }

    /// <summary>
    /// Clear variables for game over
    /// </summary>
    public void ClearVariablesForGameOver()
    {
        cameraWobbleFactor = 0;
    }
    #endregion

    #region Update
    /// <summary>
    /// Update camera, should be called every frame to handle all the input.
    /// </summary>
    public void Update()
    {
        // Only allow control when zooming is finished.
        HandleFreeCamera();

        UpdateViewMatrix();
    }
    #endregion
}
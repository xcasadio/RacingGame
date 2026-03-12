using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Sounds;
using RacingGame.UI.MGUI;
using RacingGame.UI.MGUI.Views;
using Texture = RacingGame.Graphics.Texture;
using RacingGame.Shaders;

namespace RacingGame.GameScreens;

/// <summary>
/// Car selection
/// </summary>
/// <returns>IGame screen</returns>
class CarSelection : IGameScreen, IMguiScreen
{
    private Point? _mguiViewSize;

    #region Car type variables (max speed, acceleration, etc.)
    /// <summary>
    /// Max speed for each car type
    /// </summary>
    private static float[] CarTypeMaxSpeed = new float[]
    {
        // Car 1 (orange stripes on top)
        CarPhysics.DefaultMaxSpeed * 1.05f, // 288 mph
        // Car 2 (blue stripes on side)
        CarPhysics.DefaultMaxSpeed, // 275 mph
        // Car 3 (Just white)
        CarPhysics.DefaultMaxSpeed * 0.88f, // 240 mph
    };

    /// <summary>
    /// Car mass for each car type
    /// </summary>
    private static float[] CarTypeMass = new float[]
    {
        // Car 1 (orange stripes on top)
        CarPhysics.DefaultCarMass * 1.015f, // 1015 kg
        // Car 2 (blue stripes on side)
        CarPhysics.DefaultCarMass * 1.175f, // 1175 kg
        // Car 3 (Just white)
        CarPhysics.DefaultCarMass * 0.875f, // 875 kg
    };

    /// <summary>
    /// Max acceleration for each car type
    /// </summary>
    private static float[] CarTypeMaxAcceleration = new float[]
    {
        // Car 1 (orange stripes on top)
        CarPhysics.DefaultMaxAccelerationPerSec * 0.85f, // 4 m/s^2
        // Car 2 (blue stripes on side)
        CarPhysics.DefaultMaxAccelerationPerSec * 1.2f, // 6 m/s^2
        // Car 3 (Just white)
        CarPhysics.DefaultMaxAccelerationPerSec, // 5 m/s^2
    };
    // Rest of car variables is automatically calculated below!
    #endregion

    #region Update
    private bool _isFinished = false;
    private IMguiScreenView _mguiView;

    /// <summary>
    /// Process input: car/color navigation, A button, exit.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Advance car rotation animation
        float perCarRot = MathHelper.Pi * 2.0f / 3.0f;
        float newCarSelectionRotationZ =
            RacingGameManager.CurrentCarNumber * perCarRot;
        carSelectionRotationZ = InterpolateRotation(
            carSelectionRotationZ, newCarSelectionRotationZ,
            BaseGame.MoveFactorPerSecond * 5.0f);

        if (Input.KeyboardEscapeJustPressed ||
            Input.GamePadBJustPressed ||
            Input.GamePadBackJustPressed)
        {
            _isFinished = true;
        }
    }
    #endregion

    #region Render
    /// <summary>
    /// Render game screen — drawing only.
    /// </summary>
    /// <returns>Bool</returns>
    public bool Render()
    {
        if (BaseGame.AllowShadowMapping)
        {
            BaseGame.ViewMatrix = Matrix.CreateLookAt(
                new Vector3(0, 10.45f, 2.75f),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, 1));

            Vector3 lightDir = -LensFlare.DefaultLightPos;
            lightDir = new Vector3(lightDir.X, lightDir.Y, -lightDir.Z);
            BaseGame.LightDirection = lightDir;

            float perCarRot = MathHelper.Pi * 2.0f / 3.0f;
            Matrix[] renderMatrices = new Matrix[3];
            for (int carNum = 0; carNum < 3; carNum++)
            {
                renderMatrices[carNum] =
                    Matrix.CreateRotationZ(BaseGame.TotalTime / 3.9f) *
                    Matrix.CreateTranslation(new Vector3(0, 5.0f, 0)) *
                    Matrix.CreateRotationZ(-carSelectionRotationZ + carNum * perCarRot) *
                    Matrix.CreateTranslation(new Vector3(1.5f, 0.0f, 1.0f));
            }

            RacingGameManager.Player.SetCarPosition(Vector3.Zero,
                new Vector3(0, 1, 0), new Vector3(0, 0, 1));

            ShaderEffect.shadowMapping.GenerateShadows(
                delegate
                {
                    for (int carNum = 0; carNum < 3; carNum++)
                    {
                        RacingGameManager.CarModel.GenerateShadow(
                            renderMatrices[carNum]);
                    }
                });

            ShaderEffect.shadowMapping.RenderShadows(
                delegate
                {
                    for (int carNum = 0; carNum < 3; carNum++)
                    {
                        RacingGameManager.CarSelectionPlate.UseShadow(
                            renderMatrices[carNum]);
                        RacingGameManager.CarModel.UseShadow(renderMatrices[carNum]);
                    }
                });
        }

        if (BaseGame.UsePostScreenShaders)
            BaseGame.UI.PostScreenMenuShader.Start();

        BaseGame.UI.RenderMenuBackground();

        Texture.additiveSprite.End();
        Texture.alphaSprite.End();
        Texture.additiveSprite.Begin(SpriteSortMode.Deferred, BlendState.Additive);
        Texture.alphaSprite.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        return _isFinished;
    }

    public IMguiScreenView GetOrCreateMguiView(MguiUiHost host)
    {
        Point viewportSize = new(host.ViewportBounds.Width, host.ViewportBounds.Height);
        if (_mguiView == null || _mguiViewSize != viewportSize)
        {
            _mguiView = new CarSelectionView(this, host);
            _mguiViewSize = viewportSize;
        }

        return _mguiView;
    }

    internal int CurrentCarNumber => RacingGameManager.CurrentCarNumber;

    internal int CurrentCarColor => RacingGameManager.CurrentCarColor;

    internal IReadOnlyList<Color> AvailableColors => RacingGameManager.CarColors;

    internal void MoveToPreviousCar()
    {
        Sound.Play(Sound.Sounds.Highlight);
        RacingGameManager.CurrentCarNumber = (RacingGameManager.CurrentCarNumber + 1) % 3;
    }

    internal void MoveToNextCar()
    {
        Sound.Play(Sound.Sounds.Highlight);
        RacingGameManager.CurrentCarNumber = (RacingGameManager.CurrentCarNumber + 2) % 3;
    }

    internal void SelectCarColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= RacingGameManager.NumberOfCarColors)
            return;

        if (RacingGameManager.CurrentCarColor != colorIndex)
            Sound.Play(Sound.Sounds.Highlight);

        RacingGameManager.CurrentCarColor = colorIndex;
    }

    internal void ConfirmSelection()
    {
        RacingGameManager.AddGameScreen(new TrackSelection());
    }

    internal void RequestBack()
    {
        _isFinished = true;
    }

    internal string GetSelectedCarTitle() => $"Car {RacingGameManager.CurrentCarNumber + 1}";

    internal IReadOnlyList<CarStatEntry> GetCurrentCarStatEntries()
    {
        int car = RacingGameManager.CurrentCarNumber;
        float speedRatio = CarTypeMaxSpeed[car] / CarPhysics.DefaultMaxSpeed;
        float acceleration = CarTypeMaxAcceleration[car] / CarPhysics.DefaultMaxAccelerationPerSec;
        float mass = CarTypeMass[car] / CarPhysics.DefaultCarMass;
        float braking = -0.2f + (-1.25f + 1.85f * acceleration) - (-0.65f + 1.5f * mass) + (-1.5f + 2.45f * speedRatio);
        float friction = -1 + (1 / mass + speedRatio / 5);
        float engine = speedRatio * 0.55f + acceleration * 0.45f;

        static float Normalize(float value, IEnumerable<float> allValues)
        {
            float min = allValues.Min();
            float max = allValues.Max();
            return Math.Clamp(max <= min ? 1f : (value - min) / (max - min), 0f, 1f);
        }

        IEnumerable<float> speedValues = Enumerable.Range(0, 3).Select(index => CarTypeMaxSpeed[index] / CarPhysics.DefaultMaxSpeed);
        IEnumerable<float> accelerationValues = Enumerable.Range(0, 3).Select(index => CarTypeMaxAcceleration[index] / CarPhysics.DefaultMaxAccelerationPerSec);
        IEnumerable<float> massValues = Enumerable.Range(0, 3).Select(index => CarTypeMass[index] / CarPhysics.DefaultCarMass);
        IEnumerable<float> brakingValues = Enumerable.Range(0, 3).Select(index =>
        {
            float currentSpeedRatio = CarTypeMaxSpeed[index] / CarPhysics.DefaultMaxSpeed;
            float currentAcceleration = CarTypeMaxAcceleration[index] / CarPhysics.DefaultMaxAccelerationPerSec;
            float currentMass = CarTypeMass[index] / CarPhysics.DefaultCarMass;
            return -0.2f + (-1.25f + 1.85f * currentAcceleration) - (-0.65f + 1.5f * currentMass) + (-1.5f + 2.45f * currentSpeedRatio);
        });
        IEnumerable<float> frictionValues = Enumerable.Range(0, 3).Select(index =>
        {
            float currentSpeedRatio = CarTypeMaxSpeed[index] / CarPhysics.DefaultMaxSpeed;
            float currentMass = CarTypeMass[index] / CarPhysics.DefaultCarMass;
            return -1 + (1 / currentMass + currentSpeedRatio / 5);
        });
        IEnumerable<float> engineValues = Enumerable.Range(0, 3).Select(index =>
        {
            float currentSpeedRatio = CarTypeMaxSpeed[index] / CarPhysics.DefaultMaxSpeed;
            float currentAcceleration = CarTypeMaxAcceleration[index] / CarPhysics.DefaultMaxAccelerationPerSec;
            return currentSpeedRatio * 0.55f + currentAcceleration * 0.45f;
        });

        return new[]
        {
            new CarStatEntry($"Max Speed: {(int)(CarTypeMaxSpeed[car] / CarPhysics.MphToMeterPerSec)}mph", Normalize(speedRatio, speedValues)),
            new CarStatEntry("Acceleration", Normalize(acceleration, accelerationValues)),
            new CarStatEntry("Car Mass", Normalize(mass, massValues)),
            new CarStatEntry("Braking", Normalize(braking, brakingValues)),
            new CarStatEntry("Friction", Normalize(friction, frictionValues)),
            new CarStatEntry("Engine", Normalize(engine, engineValues)),
        };
    }

    internal IReadOnlyList<string> GetCurrentCarStats()
    {
        int car = RacingGameManager.CurrentCarNumber;
        CarPhysics.SetCarVariablesForCarType(
            CarTypeMaxSpeed[car],
            CarTypeMass[car],
            CarTypeMaxAcceleration[car]);

        float maxSpeed = CarTypeMaxSpeed[car] / CarPhysics.MphToMeterPerSec;
        float acceleration = CarTypeMaxAcceleration[car] / CarPhysics.DefaultMaxAccelerationPerSec;
        float mass = CarTypeMass[car] / CarPhysics.DefaultCarMass;
        float braking = -0.2f + (-1.25f + 1.85f * acceleration) - (-0.65f + 1.5f * mass) + (-1.5f + 2.45f * (CarTypeMaxSpeed[car] / CarPhysics.DefaultMaxSpeed));
        float friction = -1 + (1 / mass + (CarTypeMaxSpeed[car] / CarPhysics.DefaultMaxSpeed) / 5);

        return new[]
        {
            $"Top Speed: {(int)maxSpeed} mph",
            $"Acceleration: {acceleration:0.00}x",
            $"Mass: {mass:0.00}x",
            $"Braking: {braking:0.00}",
            $"Friction: {friction:0.00}",
        };
    }

    internal readonly record struct CarStatEntry(string Label, float FillPercent);
    #endregion

    #region PostUIRender
    #region Helpers
    /// <summary>
    /// Helper for rotating the 3 cars in the car selection screen.
    /// Updated once per frame in Update().
    /// </summary>
    float carSelectionRotationZ = 0.0f;

    /// <summary>
    /// Helper function for RotateSlowly, max. distance between
    /// sourceRot and desiredRot is PI, this allows very easy checks.
    /// </summary>
    public static void AdjustRotRange(ref float desiredRot, float sourceRot)
    {
        if (desiredRot >= sourceRot + (float)Math.PI)
        {
            desiredRot -= (float)Math.PI * 2.0f;
        }

        if (desiredRot < sourceRot - (float)Math.PI)
        {
            desiredRot += (float)Math.PI * 2.0f;
        }
    }

    /// <summary>
    /// Adjust rotation to -PI - PI range
    /// </summary>
    public static void AdjustRotToPIRange(ref float desiredRot)
    {
        if (desiredRot <= -(float)Math.PI)
        {
            desiredRot += (float)Math.PI * 2.0f;
        }

        if (desiredRot > (float)Math.PI)
        {
            desiredRot -= (float)Math.PI * 2.0f;
        }
    }

    /// <summary>
    /// Interpolate rotation
    /// </summary>
    /// <param name="rot">Rot</param>
    /// <param name="targetRot">Target rot</param>
    /// <param name="nearlyEqualRot">Nearly equal rot</param>
    /// <returns>Float</returns>
    public static float InterpolateRotation(
        float rot, float targetRot, float nearlyEqualRot)
    {
        AdjustRotRange(ref targetRot, rot);

        if (rot > targetRot)
        {
            if (Math.Abs(rot - targetRot) < nearlyEqualRot)
            {
                rot = targetRot;
            }
            else
            {
                rot -= nearlyEqualRot;
            }
        }
        else if (rot < targetRot)
        {
            if (Math.Abs(rot - targetRot) < nearlyEqualRot)
            {
                rot = targetRot;
            }
            else
            {
                rot += nearlyEqualRot;
            }
        }

        // Check if rot is in -PI-PI range (for easier calculations!)
        AdjustRotToPIRange(ref rot);

        return rot;
    }
    #endregion

    #region PostUIRender
    /// <summary>
    /// Post user interface render
    /// </summary>
    public void PostUIRender()
    {
        // Let camera point directly at the center, around 10 units away.
        Matrix remViewMatrix = BaseGame.ViewMatrix;
        BaseGame.ViewMatrix = Matrix.CreateLookAt(
            new Vector3(0, 10.45f, 2.75f),
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 1));

        // Let the light come from the front!
        Vector3 lightDir = -LensFlare.DefaultLightPos;
        lightDir = new Vector3(lightDir.X, lightDir.Y, -lightDir.Z);
        // LightDirection will normalize
        BaseGame.LightDirection = lightDir;

        // Show 3d cars — carSelectionRotationZ already interpolated in Update()
        float perCarRot = MathHelper.Pi * 2.0f / 3.0f;
        // Prebuild all render matrices, we will use them for several times
        // here.
        Matrix[] renderMatrices = new Matrix[3];
        for (int carNum = 0; carNum < 3; carNum++)
        {
            renderMatrices[carNum] =
                Matrix.CreateRotationZ(BaseGame.TotalTime / 3.9f) *
                Matrix.CreateTranslation(new Vector3(0, 5.0f, 0)) *
                Matrix.CreateRotationZ(-carSelectionRotationZ + carNum * perCarRot) *
                Matrix.CreateTranslation(new Vector3(1.5f, 0.0f, 1.0f));
        }
        // Last translation translates the position of the cars in the UI

        // For shadows make sure the car position is the origin
        RacingGameManager.Player.SetCarPosition(Vector3.Zero,
            new Vector3(0, 1, 0), new Vector3(0, 0, 1));

        // Now do the real rendering:
        for (int carNum = 0; carNum < 3; carNum++)
        {
            RacingGameManager.CarSelectionPlate.Render(renderMatrices[carNum]);
            RacingGameManager.CarModel.RenderCar(
                carNum,
                RacingGameManager.CarColor,
                false,
                renderMatrices[carNum]);
        }

        // Render all models we remembered this frame (we are in PostUIRender,
        // and we changed our view matrix, render directly here).
        BaseGame.MeshRenderManager.Render();

        // And finally add shadows to the scene
        if (BaseGame.AllowShadowMapping)
        {
            ShaderEffect.shadowMapping.ShowShadows();
        }

        // Reset stuff
        BaseGame.WorldMatrix = Matrix.Identity;
        BaseGame.ViewMatrix = remViewMatrix;
    }
    #endregion
    #endregion
}
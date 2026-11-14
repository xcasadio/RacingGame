using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Shaders;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// --capture-track-audit: first screen under the loading screen. It starts the capture once
    /// the content is loaded and ends the game when the capture screen is done.
    /// </summary>
    class TrackAuditCaptureLauncher : IGameScreen
    {
        private bool launched;

        public void Update(GameTime gameTime)
        {
        }

        public bool Render()
        {
            if (launched)
                // The capture screen is gone: leaving empties the screen stack, which quits the game
                return true;

            launched = true;
            RacingGameManager.AddGameScreen(new TrackAuditCaptureScreen());
            return false;
        }
    }

    /// <summary>
    /// --capture-track-audit: renders fixed views of every built-in track with the race rendering
    /// (sky, landscape, track, objects, car, shadows, glow) but without the HUD, and saves each one
    /// as a PNG file. The cameras are written to views.json, in the game's own coordinates (Z up),
    /// so that the CasaEngine port can replay exactly the same views.
    /// Each view is saved twice: "full" (as the game looks) and "nopost" (post screen shaders off).
    /// </summary>
    class TrackAuditCaptureScreen : GameScreen
    {
        /// <summary>
        /// Frames rendered with a view before it is saved, so that the car matrix, the shadow map
        /// and the glow render targets all belong to that view.
        /// </summary>
        private const int SettleFrames = 8;

        private const int TrackBoundsSamples = 400;

        private static readonly string[] Variants = { "full", "nopost" };

        private sealed class CaptureView
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Target;
            public Vector3 CarPosition;
            public Vector3 CarDirection;
            public Vector3 CarUp;
        }

        private readonly RacingGameManager.Level[] levels =
        {
            RacingGameManager.Level.Beginner,
            RacingGameManager.Level.Advanced,
            RacingGameManager.Level.Expert,
        };

        private readonly List<object> exportedTracks = new List<object>();
        private List<object> exportedViews;
        private List<CaptureView> views;
        private int levelIndex = -1;
        private int viewIndex;
        private int variantIndex;
        private int frame;
        private int savedFiles;
        private bool finished;

        public TrackAuditCaptureScreen()
        {
            // Same car on every run (the menus are skipped, so nothing selected one)
            RacingGameManager.currentCarNumber = 0;
            RacingGameManager.currentCarColor = 0;
            Directory.CreateDirectory(CaptureOptions.OutputDirectory);
            Console.WriteLine("Track audit capture started: " + CaptureOptions.OutputDirectory);
        }

        protected override bool WantsToLeave()
        {
            return finished;
        }

        public override void Update(GameTime gameTime)
        {
            // Player.Update ran just before: pin the car again, the physics must not move it
            if (views != null && viewIndex < views.Count)
                PinCar(views[viewIndex]);
        }

        public override bool Render()
        {
            if (finished)
                return true;

            try
            {
                if (views == null && !BeginNextLevel())
                {
                    WriteViewsFile();
                    Console.WriteLine("Track audit capture completed: " + savedFiles + " file(s)");
                    finished = true;
                    return true;
                }

                CaptureView view = views[viewIndex];
                BaseGame.SetPostScreenShadersForCapture(variantIndex == 0);
                PinCar(view);
                BaseGame.ViewMatrix = Matrix.CreateLookAt(view.Position, view.Target, Vector3.UnitZ);

                RenderScene();

                frame++;
                if (frame == SettleFrames)
                {
                    BaseGame.RequestFrameCapture(Path.Combine(CaptureOptions.OutputDirectory,
                        FileName(levels[levelIndex], view, Variants[variantIndex])));
                    savedFiles++;
                }
                else if (frame > SettleFrames)
                {
                    Advance();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Track audit capture failed: " + ex);
                Environment.ExitCode = 1;
                finished = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// GameScreen.Render without the sounds, the ghost car and the HUD.
        /// </summary>
        private static void RenderScene()
        {
            ShadowMapShader.PrepareGameShadows();

            if (BaseGame.UsePostScreenShaders)
                BaseGame.UI.PostScreenGlowShader.Start();

            BaseGame.UI.RenderGameBackground();
            RacingGameManager.Landscape.Render();
            RacingGameManager.CarModel.RenderCar(
                RacingGameManager.currentCarNumber,
                RacingGameManager.CarColor,
                false,
                RacingGameManager.Player.CarRenderMatrix);
            BaseGame.MeshRenderManager.Render();

            if (BaseGame.AllowShadowMapping)
                ShaderEffect.shadowMapping.ShowShadows();

            if (BaseGame.UsePostScreenShaders)
                BaseGame.UI.PostScreenGlowShader.Show();
        }

        private void Advance()
        {
            frame = 0;
            variantIndex++;
            if (variantIndex < Variants.Length)
                return;

            variantIndex = 0;
            viewIndex++;
            if (viewIndex < views.Count)
                return;

            // Level done, the next Render call loads the following one
            viewIndex = 0;
            views = null;
        }

        private bool BeginNextLevel()
        {
            levelIndex++;
            if (levelIndex >= levels.Length)
                return false;

            RacingGameManager.Level level = levels[levelIndex];

            // Same random scenery on every run, whatever was drawn from the generator before
            RandomHelper.GenerateNewRandomGenerator();
            RacingGameManager.LoadLevel(level);
            RacingGameManager.Player.Reset();
            // Keep the start light red and the car where the capture puts it
            RacingGameManager.Player.HoldCountdown = true;
            BaseGame.LightDirection = LensFlare.DefaultLightPos;

            views = BuildViews();
            exportedViews = new List<object>();
            foreach (CaptureView view in views)
            {
                exportedViews.Add(new
                {
                    name = view.Name,
                    position = ToArray(view.Position),
                    target = ToArray(view.Target),
                    up = ToArray(Vector3.UnitZ),
                    car = new
                    {
                        position = ToArray(view.CarPosition),
                        direction = ToArray(view.CarDirection),
                        up = ToArray(view.CarUp),
                    },
                    files = new
                    {
                        full = FileName(level, view, Variants[0]),
                        nopost = FileName(level, view, Variants[1]),
                    },
                });
            }
            exportedTracks.Add(new
            {
                name = level.ToString(),
                trackLength = RacingGameManager.Landscape.TrackLength,
                views = exportedViews,
            });

            viewIndex = 0;
            variantIndex = 0;
            frame = 0;
            Console.WriteLine("Track audit capture: " + level + ", " + views.Count + " view(s)");
            return true;
        }

        private static List<CaptureView> BuildViews()
        {
            List<CaptureView> result = new List<CaptureView>();
            Landscapes.Landscape landscape = RacingGameManager.Landscape;

            // Start grid: the car is where the game puts it
            Vector3 startPos = RacingGameManager.Player.CarPosition;
            Vector3 startDir = Vector3.Normalize(RacingGameManager.Player.CarDirection);
            Vector3 startUp = Vector3.Normalize(RacingGameManager.Player.CarUpVector);
            result.Add(BehindView("start", startPos, startDir, startUp));

            // Three points along the track, the car is moved there (the shadow map follows it)
            float[] percents = { 0.2f, 0.5f, 0.8f };
            foreach (float percent in percents)
            {
                Matrix m = landscape.GetTrackPositionMatrix(percent, out _, out _);
                result.Add(BehindView("sector-" + (int)Math.Round(percent * 100),
                    m.Translation, Vector3.Normalize(m.Forward), Vector3.Normalize(m.Up)));
            }

            // Whole track from above
            Vector3 min = new Vector3(float.MaxValue), max = new Vector3(float.MinValue);
            for (int i = 0; i < TrackBoundsSamples; i++)
            {
                Vector3 p = landscape.GetTrackPositionMatrix(
                    i / (float)TrackBoundsSamples, out _, out _).Translation;
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            float distance = Math.Max(Math.Max(size.X, size.Y) * 0.55f, 90f);
            result.Add(new CaptureView
            {
                Name = "overview",
                Position = center + new Vector3(-distance * 0.6f, -distance * 0.6f,
                    Math.Max(size.Z, 40f) + 70f),
                Target = center + Vector3.UnitZ * Math.Max(8f, size.Z * 0.1f),
                CarPosition = startPos,
                CarDirection = startDir,
                CarUp = startUp,
            });

            return result;
        }

        /// <summary>
        /// A view from behind, above and slightly to the right of a point of the road.
        /// </summary>
        private static CaptureView BehindView(string name, Vector3 pos, Vector3 dir, Vector3 up)
        {
            Vector3 right = Vector3.Cross(dir, Vector3.UnitZ);
            right = right.LengthSquared() < 0.0001f ? Vector3.UnitX : Vector3.Normalize(right);
            return new CaptureView
            {
                Name = name,
                Position = pos - dir * 18f + Vector3.UnitZ * 10f + right * 5f,
                Target = pos + dir * 6f,
                CarPosition = pos,
                CarDirection = dir,
                CarUp = up,
            };
        }

        private static void PinCar(CaptureView view)
        {
            RacingGameManager.Player.SetCarPosition(
                view.CarPosition, view.CarDirection, view.CarUp);
        }

        private static string FileName(RacingGameManager.Level level, CaptureView view,
            string variant)
        {
            return level + "-" + view.Name + "-" + variant + ".png";
        }

        private static float[] ToArray(Vector3 v)
        {
            return new[] { v.X, v.Y, v.Z };
        }

        private void WriteViewsFile()
        {
            var document = new
            {
                schema = 1,
                source = "RacingGame (MonoGame) --capture-track-audit",
                // X right, Y forward, Z up, 1 unit = 1 metre
                coordinateSystem = "racinggame-z-up",
                seed = CaptureOptions.Seed,
                width = BaseGame.Width,
                height = BaseGame.Height,
                fieldOfViewYDegrees = MathHelper.ToDegrees(BaseGame.FieldOfViewY),
                nearPlane = BaseGame.NearPlaneDistance,
                farPlane = BaseGame.FarPlaneDistance,
                // Direction towards the light
                lightDirection = ToArray(BaseGame.LightDirection),
                carNumber = RacingGameManager.currentCarNumber,
                carColor = new[]
                {
                    (int)RacingGameManager.CarColor.R,
                    (int)RacingGameManager.CarColor.G,
                    (int)RacingGameManager.CarColor.B,
                },
                variants = Variants,
                tracks = exportedTracks,
            };

            File.WriteAllText(Path.Combine(CaptureOptions.OutputDirectory, "views.json"),
                JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}

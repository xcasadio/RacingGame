using System.Text.Json;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Rendering;
using CasaEngine.Framework.Rendering.Environment;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// --capture-reference-views &lt;views.json&gt; [--reference-capture-out &lt;folder&gt;]: replays the views exported by the
/// original game (RacingGame.exe --capture-track-audit, ADR-0015) and saves one &lt;Track&gt;-&lt;view&gt;.png per view, so
/// scripts/TrackCaptureCompare can compare each one with the original's picture.
/// The run uses the back buffer size and the projection (field of view, near and far planes) written in views.json,
/// applied to the capture only: the race camera of the game is not changed. The HUD is removed, the chase and debug
/// cameras are stopped, and the car is hidden in the views where the original does not draw it. Nothing is saved to
/// the settings (ADR-0004).
/// </summary>
internal sealed class ReferenceViewCaptureValidator
{
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinimumStepInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(180);

    private enum CaptureStep
    {
        WaitForFrontEnd,
        StartRace,
        WaitForRace,
        PrepareView,
        CaptureView,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private sealed record ReferenceView(string Name, Vector3 Position, Vector3 Target, Vector3 CarPosition, bool CarVisible);

    private sealed record ReferenceTrack(string Name, IReadOnlyList<ReferenceView> Views);

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly string _outputDirectory;
    private readonly List<ReferenceTrack> _tracks = [];
    private readonly int _width;
    private readonly int _height;
    private readonly float _fieldOfViewY;
    private readonly float _nearPlane;
    private readonly float _farPlane;
    private readonly string? _loadError;
    private readonly string? _sceneFilePath;
    private readonly int? _sceneryRandomSeed;
    private CaptureStep _step = CaptureStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private TimeSpan? _readySince;
    private bool _started;
    private int _trackIndex;
    private int _viewIndex;
    private int _captureCount;

    public ReferenceViewCaptureValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow, string? viewsFilePath, string? outputDirectory)
    {
        _game = game;
        _flow = flow;
        _outputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.Combine(_game.GetUserDataDirectory(), "Screenshots", $"reference-run-{DateTime.Now:yyyyMMdd-HHmmss}")
            : Path.GetFullPath(outputDirectory);

        try
        {
            if (string.IsNullOrWhiteSpace(viewsFilePath) || !File.Exists(viewsFilePath))
            {
                throw new FileNotFoundException($"views.json not found: {viewsFilePath}");
            }

            string sceneFilePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(viewsFilePath))!, "scene.json");
            _sceneFilePath = File.Exists(sceneFilePath) ? sceneFilePath : null;

            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(viewsFilePath));
            JsonElement root = document.RootElement;
            _width = root.GetProperty("width").GetInt32();
            _height = root.GetProperty("height").GetInt32();
            _fieldOfViewY = MathHelper.ToRadians(root.GetProperty("fieldOfViewYDegrees").GetSingle());
            _nearPlane = root.GetProperty("nearPlane").GetSingle();
            _farPlane = root.GetProperty("farPlane").GetSingle();
            // The original seeds its random scenery from this value before each level, so the port does the same.
            _sceneryRandomSeed = root.TryGetProperty("seed", out JsonElement seed) ? seed.GetInt32() : null;
            foreach (JsonElement track in root.GetProperty("tracks").EnumerateArray())
            {
                var views = new List<ReferenceView>();
                foreach (JsonElement view in track.GetProperty("views").EnumerateArray())
                {
                    views.Add(new ReferenceView(
                        view.GetProperty("name").GetString()!,
                        ReadVector(view.GetProperty("position")),
                        ReadVector(view.GetProperty("target")),
                        ReadVector(view.GetProperty("car").GetProperty("position")),
                        view.GetProperty("car").GetProperty("visible").GetBoolean()));
                }

                _tracks.Add(new ReferenceTrack(track.GetProperty("name").GetString()!, views));
            }
        }
        catch (Exception ex)
        {
            _loadError = $"Unable to read the reference views: {ex.Message}";
        }

        _game.PreviewUpdate += OnPreviewUpdate;
    }

    private void OnPreviewUpdate(object? sender, TimeSpan totalTime)
    {
        if (!_started)
        {
            _started = true;
            _startedAt = totalTime;
            _lastTransitionAt = totalTime;
            if (_loadError != null)
            {
                Fail(_loadError);
                return;
            }

            // Same car and colour as the original's capture (first car, first colour), fixed size, nothing saved.
            _flow.State.SelectedCarIndex = 0;
            _flow.State.SelectedCarColorIndex = 0;
            _flow.State.PinScreenAnimations = true;
            DisplaySettings current = _game.GetDisplaySettings();
            _game.ApplyDisplaySettings(new DisplaySettings(_width, _height, false, current.IsVSyncEnabled), persistToProjectSettings: false);
            _flow.State.SelectedResolutionIndex = RacingGameCasaEngineGame.GetResolutionIndex(_width, _height);
            _flow.State.IsFullscreen = false;
            _flow.State.EnableVSync = current.IsVSyncEnabled;
            Logs.WriteInfo($"Reference view capture started, output folder {_outputDirectory}");
        }

        if (_step is CaptureStep.Completed or CaptureStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > Timeout)
        {
            Fail($"Reference view capture timed out at step {_step}.");
            return;
        }

        if (totalTime - _lastTransitionAt < MinimumStepInterval)
        {
            return;
        }

        switch (_step)
        {
            case CaptureStep.WaitForFrontEnd:
                if (_game.GameManager.CurrentWorld?.Name == RaceWorldFactory.FrontEndWorldName)
                {
                    Advance(CaptureStep.StartRace, totalTime, "Front-end ready");
                }
                break;

            case CaptureStep.StartRace:
                if (_trackIndex >= _tracks.Count)
                {
                    Complete();
                    return;
                }

                int catalogIndex = FindCatalogTrackIndex(_tracks[_trackIndex].Name);
                if (catalogIndex < 0)
                {
                    Fail($"Track '{_tracks[_trackIndex].Name}' of the reference views is not in the track catalog.");
                    return;
                }

                _flow.State.SelectedTrackIndex = catalogIndex;
                LegacyTrackSceneFactory.SceneryRandomSeed = _sceneryRandomSeed;
                _flow.StartRaceForAutomation();
                _viewIndex = 0;
                Advance(CaptureStep.WaitForRace, totalTime, $"Loading track {_tracks[_trackIndex].Name}");
                break;

            case CaptureStep.WaitForRace:
                if (IsSettled(IsRaceReady() && HasCaptureSize(), totalTime))
                {
                    // No HUD in the pictures, and no camera of the game moving the capture camera.
                    _game.GameManager.ScreenManager.ClearState();
                    _game.SetDebugCameraEnabled(true);
                    _game.SetCircuitOnlyViewEnabled(false);
                    if (!TryStopCameraControllers())
                    {
                        Fail("Unable to locate the race camera.");
                        return;
                    }

                    LogAlignment(_tracks[_trackIndex]);
                    LogSceneMeasure(_tracks[_trackIndex]);
                    Advance(CaptureStep.PrepareView, totalTime, $"Race world ready for {_tracks[_trackIndex].Name}");
                }
                break;

            case CaptureStep.PrepareView:
                if (!ApplyView(_tracks[_trackIndex], _tracks[_trackIndex].Views[_viewIndex]))
                {
                    Fail("Unable to apply a reference view.");
                    return;
                }

                Advance(CaptureStep.CaptureView, totalTime, $"Prepared view {_tracks[_trackIndex].Views[_viewIndex].Name}");
                break;

            case CaptureStep.CaptureView:
                // The view is re-applied every update until it is captured, so nothing else can move the camera.
                if (!ApplyView(_tracks[_trackIndex], _tracks[_trackIndex].Views[_viewIndex]))
                {
                    Fail("Unable to apply a reference view.");
                    return;
                }

                if (!IsSettled(true, totalTime))
                {
                    return;
                }

                string fileName = $"{_tracks[_trackIndex].Name}-{_tracks[_trackIndex].Views[_viewIndex].Name}.png";
                if (!_game.SaveBackBufferAsPng(Path.Combine(_outputDirectory, fileName)))
                {
                    Fail($"Unable to save {fileName}.");
                    return;
                }

                LogCaptureBench(_tracks[_trackIndex].Name, _tracks[_trackIndex].Views[_viewIndex].Name);
                _captureCount++;
                _viewIndex++;
                Advance(
                    _viewIndex < _tracks[_trackIndex].Views.Count ? CaptureStep.PrepareView : CaptureStep.ReturnToFrontEnd,
                    totalTime,
                    $"Captured {fileName}");
                break;

            case CaptureStep.ReturnToFrontEnd:
                _game.SetDebugCameraEnabled(false);
                _flow.ReturnToFrontEndForAutomation();
                _trackIndex++;
                Advance(CaptureStep.WaitForFrontEnd, totalTime, "Returning to front-end for next track");
                break;
        }
    }

    // Proof that the anti-aliasing ran for the picture just saved: the FXAA pass count of the race view in the frame that was
    // rendered last (RenderStats is reset when the next frame starts rendering the view). 1 in the linear pipeline, 0 in legacy.
    private void LogCaptureBench(string trackName, string viewName)
    {
        World world = _game.GameManager.CurrentWorld!;
        RenderView? raceView = _game.GameManager.ViewManager.Views.FirstOrDefault(view => ReferenceEquals(view.World, world));
        if (raceView == null)
        {
            Logs.WriteWarning($"[CaptureBench] view {trackName}-{viewName}: no render view found for the race world.");
            return;
        }

        string pipeline = world.EnvironmentSettings.ColorPipeline.Mode == ColorPipelineMode.LinearHdr ? "linear" : "legacy";
        Logs.WriteInfo($"[CaptureBench] view {trackName}-{viewName} pipeline {pipeline} fxaaPasses {raceView.RenderStats.FxaaPasses}");
    }

    private bool IsRaceReady()
    {
        return _game.GameManager.CurrentWorld is { } world
            && RaceWorldFactory.IsRaceWorld(world)
            && _game.RaceSession.IsActive;
    }

    private bool HasCaptureSize()
    {
        DisplaySettings settings = _game.GetDisplaySettings();
        return settings.Width == _width && settings.Height == _height;
    }

    // True once the condition has held for SettleDelay, so a view is captured after it has been drawn.
    private bool IsSettled(bool condition, TimeSpan totalTime)
    {
        if (!condition)
        {
            _readySince = null;
            return false;
        }

        _readySince ??= totalTime;
        return totalTime - _readySince.Value >= SettleDelay;
    }

    private static int FindCatalogTrackIndex(string trackName)
    {
        for (int index = 0; index < RaceFrontEndCatalog.Tracks.Count; index++)
        {
            if (string.Equals(RaceFrontEndCatalog.Tracks[index].Name, trackName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    // Logs how far the port's start grid and checkpoints are from the original's car positions once converted: a
    // large distance means the two worlds are not aligned, and no picture of that track can match.
    private void LogAlignment(ReferenceTrack track)
    {
        World world = _game.GameManager.CurrentWorld!;
        string trackName = RaceFrontEndCatalog.Tracks[FindCatalogTrackIndex(track.Name)].Name;
        foreach (ReferenceView view in track.Views)
        {
            Vector3 converted = LegacyTrackSceneFactory.ConvertLegacyPointToRaceWorld(trackName, view.CarPosition);
            Entity? nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Entity entity in world.Entities)
            {
                bool isAnchor = entity.Name == RaceWorldFactory.PlayerStartEntityName
                    || entity.Name?.StartsWith(RaceWorldFactory.CheckpointEntityNamePrefix, StringComparison.Ordinal) == true;
                if (!isAnchor || entity.RootComponent == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(entity.RootComponent.Position, converted);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = entity;
                }
            }

            Logs.WriteInfo(FormattableString.Invariant(
                $"Reference view capture: alignment {track.Name}-{view.Name}: original car at {converted}, nearest anchor {nearest?.Name} at {nearest?.RootComponent?.Position} ({nearestDistance:0.00} m)"));
        }
    }

    // Measures the road points and the placed models of the port against the original's scene.json, when the
    // original wrote one next to views.json.
    private void LogSceneMeasure(ReferenceTrack track)
    {
        if (_sceneFilePath == null || LegacyTrackSceneFactory.LastSceneDiagnostics is not { } diagnostics)
        {
            return;
        }

        try
        {
            string summary = ReferenceSceneMeasure.Measure(_sceneFilePath, track.Name, diagnostics, _outputDirectory);
            Logs.WriteInfo($"Reference view capture: scene measure {track.Name}: {summary}");
        }
        catch (Exception ex)
        {
            Logs.WriteWarning($"Reference view capture: scene measure failed for {track.Name}: {ex.Message}");
        }
    }

    private bool TryStopCameraControllers()
    {
        if (!TryGetRaceCamera(out _, out Entity? cameraEntity))
        {
            return false;
        }

        // The debug camera takes the look target over whenever the window is active: remove both camera drivers for
        // this capture world (the next race world gets new ones).
        if (cameraEntity.GetComponent<DebugFreeCameraComponent>() is { } debugCamera)
        {
            cameraEntity.RemoveComponent(debugCamera);
        }

        if (cameraEntity.GetComponent<ChaseCameraRigComponent>() is { } chaseCamera)
        {
            cameraEntity.RemoveComponent(chaseCamera);
        }

        // The race world draws the checkpoint course as debug lines, which the original does not have.
        foreach (Entity entity in _game.GameManager.CurrentWorld!.Entities.ToList())
        {
            if (entity.GetComponent<RaceCourseDebugComponent>() is { } courseDebug)
            {
                entity.RemoveComponent(courseDebug);
            }
        }

        return true;
    }

    private bool ApplyView(ReferenceTrack track, ReferenceView view)
    {
        if (!TryGetRaceCamera(out CameraLookAtComponent? camera, out _))
        {
            return false;
        }

        string trackName = RaceFrontEndCatalog.Tracks[FindCatalogTrackIndex(track.Name)].Name;
        // The view matrix takes its up vector from the orientation, which the chase camera left tilted: the
        // original's views are all taken with the world up.
        camera.LocalOrientation = Quaternion.Identity;
        camera.FieldOfView = _fieldOfViewY;
        camera.NearPlane = _nearPlane;
        camera.FarPlane = _farPlane;
        camera.SetPositionAndTarget(
            LegacyTrackSceneFactory.ConvertLegacyPointToRaceWorld(trackName, view.Position),
            LegacyTrackSceneFactory.ConvertLegacyPointToRaceWorld(trackName, view.Target));

        World world = _game.GameManager.CurrentWorld!;
        foreach (Entity entity in world.Entities)
        {
            if (entity.Name == RaceWorldFactory.PlayerCarEntityName)
            {
                entity.IsVisible = view.CarVisible;
            }
        }

        return true;
    }

    private bool TryGetRaceCamera(out CameraLookAtComponent? camera, out Entity? cameraEntity)
    {
        cameraEntity = _game.GameManager.CurrentWorld?.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.CameraEntityName);
        camera = cameraEntity?.RootComponent as CameraLookAtComponent;
        return camera != null;
    }

    private static Vector3 ReadVector(JsonElement element)
    {
        return new Vector3(element[0].GetSingle(), element[1].GetSingle(), element[2].GetSingle());
    }

    private void Advance(CaptureStep nextStep, TimeSpan totalTime, string message)
    {
        Logs.WriteInfo($"Reference view capture: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
        _readySince = null;
    }

    private void Complete()
    {
        Logs.WriteInfo($"Reference view capture completed successfully ({_captureCount} file(s)) in {_outputDirectory}");
        Environment.ExitCode = 0;
        _step = CaptureStep.Completed;
        LegacyTrackSceneFactory.SceneryRandomSeed = null;
        _game.Exit();
    }

    private void Fail(string message)
    {
        Logs.WriteError(message);
        Environment.ExitCode = 1;
        _step = CaptureStep.Failed;
        LegacyTrackSceneFactory.SceneryRandomSeed = null;
        _game.Exit();
    }
}

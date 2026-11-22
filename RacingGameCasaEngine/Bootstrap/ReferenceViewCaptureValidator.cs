using System.Text.Json;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Application;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Assets.Loaders;
using CasaEngine.Framework.Assets.Textures;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Rendering;
using CasaEngine.Framework.Rendering.Environment;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Race;
using RacingGame.Gameplay.Track;
using RacingGameCasaEngine.Worlds;

using Color = Microsoft.Xna.Framework.Color;
using Texture = CasaEngine.Framework.Assets.Textures.Texture;

namespace RacingGameCasaEngine.Bootstrap;

/// <summary>
/// --capture-reference-views &lt;views.json&gt; [--reference-capture-out &lt;folder&gt;]: replays the views exported by the
/// original game (RacingGame.exe --capture-track-audit) and saves one &lt;Track&gt;-&lt;view&gt;.png per view, so
/// scripts/TrackCaptureCompare can compare each one with the original's picture.
/// The run uses the back buffer size and the projection (field of view, near and far planes) written in views.json,
/// applied to the capture only: the race camera of the game is not changed beyond the capture world, and nothing is saved
/// to the settings (ADR-0004). The HUD is removed, the chase and debug cameras are stopped, the course debug lines are
/// removed, and the car is hidden in the views where the original does not draw it. A picture is taken at the end of a
/// draw, as the other capture modes do, with one update per draw.
/// When the original wrote a scene.json next to views.json, the road points and the placed models of each race world are also
/// measured against it (<see cref="ReferenceSceneMeasure"/>), into scene-measure-&lt;Track&gt;.txt in the output folder.
/// </summary>
internal sealed class ReferenceViewCaptureValidator
{
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinimumStepInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(240);

    private enum CaptureStep
    {
        WaitForFrontEnd,
        StartRace,
        WaitForRace,
        PrepareView,
        CaptureView,
        ResolveCapture,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private sealed record ReferenceView(string Name, Vector3 Position, Vector3 Target, Vector3 CarPosition, Vector3 CarDirection, bool CarVisible);

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
    private CaptureStep _step = CaptureStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private TimeSpan? _readySince;
    private bool _started;
    private int _trackIndex;
    private int _viewIndex;
    private int _captureCount;
    private ReferenceTrackSpace? _trackSpace;
    private string? _pendingCaptureName;
    private bool _hasLoggedMipmaps;

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

            // The original reseeds its random scenery with this seed for each level. The game draws its scenery with one
            // fixed seed (RaceTrackLayout.ScenerySeed, ADR-0041): views exported with another seed show another scenery.
            if (root.TryGetProperty("seed", out JsonElement seed) && seed.GetInt32() != RaceTrackLayout.ScenerySeed)
            {
                throw new InvalidDataException(
                    $"views.json was exported with the scenery seed {seed.GetInt32()}, the game draws its scenery with the seed {RaceTrackLayout.ScenerySeed}.");
            }

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
                        ReadVector(view.GetProperty("car").GetProperty("direction")),
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

            // One update per draw, no catch-up update, so the draw that follows the capture update shows that update
            // whatever the frame pacing (the game slows down instead of catching up).
            _game.MaxElapsedTime = _game.TargetElapsedTime;
            Logs.WriteInfo($"Reference view capture started, output folder {_outputDirectory}");
            if (_sceneFilePath == null)
            {
                Logs.WriteInfo("Reference view capture: no scene.json next to views.json, the scene measure is skipped.");
            }
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
                _flow.StartRaceForAutomation();
                _viewIndex = 0;
                _trackSpace = null;
                Advance(CaptureStep.WaitForRace, totalTime, $"Loading track {_tracks[_trackIndex].Name}");
                break;

            case CaptureStep.WaitForRace:
                if (IsSettled(IsRaceReady() && HasCaptureSize(), totalTime))
                {
                    // No HUD in the pictures, and no camera of the game moving the capture camera.
                    _game.GameManager.ScreenManager.ClearState();
                    _game.SetDebugCameraEnabled(true);
                    _game.SetCameraPosedByAutomation(true);
                    _game.SetCircuitOnlyViewEnabled(false);
                    if (!TryStopCameraControllers())
                    {
                        Fail("Unable to locate the race camera.");
                        return;
                    }

                    if (!ReferenceTrackSpace.TryCreate(_game.GameManager.CurrentWorld!, out _trackSpace))
                    {
                        Fail($"Unable to resolve the track assets of {_tracks[_trackIndex].Name}.");
                        return;
                    }

                    LogAlignment(_tracks[_trackIndex], _trackSpace!);
                    MeasureScene(_tracks[_trackIndex], _trackSpace!);
                    if (!_hasLoggedMipmaps)
                    {
                        _hasLoggedMipmaps = true;
                        LogMipmaps();
                    }

                    Advance(CaptureStep.PrepareView, totalTime, $"Race world ready for {_tracks[_trackIndex].Name}");
                }
                break;

            case CaptureStep.PrepareView:
                if (!ApplyView(_tracks[_trackIndex].Views[_viewIndex]))
                {
                    Fail("Unable to apply a reference view.");
                    return;
                }

                Advance(CaptureStep.CaptureView, totalTime, $"Prepared view {_tracks[_trackIndex].Views[_viewIndex].Name}");
                break;

            case CaptureStep.CaptureView:
                // The view is re-applied every update until it is captured, so nothing else can move the camera.
                if (!ApplyView(_tracks[_trackIndex].Views[_viewIndex]))
                {
                    Fail("Unable to apply a reference view.");
                    return;
                }

                if (!IsSettled(true, totalTime))
                {
                    return;
                }

                _pendingCaptureName = $"{_tracks[_trackIndex].Name}-{_tracks[_trackIndex].Views[_viewIndex].Name}";
                Directory.CreateDirectory(_outputDirectory);
                _game.RequestScreenshotAtEndOfDraw(Path.Combine(_outputDirectory, $"{_pendingCaptureName}.png"));
                Advance(CaptureStep.ResolveCapture, totalTime, $"Capture requested for {_pendingCaptureName}");
                break;

            case CaptureStep.ResolveCapture:
                // The draw that follows the request saves the picture; it is counted once that draw has run.
                if (_game.IsEndOfDrawScreenshotPending)
                {
                    return;
                }

                if (!_game.IsEndOfDrawScreenshotSaved)
                {
                    Fail($"Unable to save {_pendingCaptureName}.png.");
                    return;
                }

                LogCaptureBench(_pendingCaptureName!);
                _captureCount++;
                _viewIndex++;
                Advance(
                    _viewIndex < _tracks[_trackIndex].Views.Count ? CaptureStep.PrepareView : CaptureStep.ReturnToFrontEnd,
                    totalTime,
                    $"Captured {_pendingCaptureName}.png");
                break;

            case CaptureStep.ReturnToFrontEnd:
                _game.SetCameraPosedByAutomation(false);
                _game.SetDebugCameraEnabled(false);
                _flow.ReturnToFrontEndForAutomation();
                _trackIndex++;
                Advance(CaptureStep.WaitForFrontEnd, totalTime, "Returning to front-end for next track");
                break;
        }
    }

    // Proof of the colour pipeline and of the anti-aliasing for the picture just saved: the FXAA pass count of the race
    // view in the frame rendered last (RenderStats is reset when the next frame starts rendering the view), 1 when the view
    // was anti-aliased and 0 otherwise.
    private void LogCaptureBench(string captureName)
    {
        World world = _game.GameManager.CurrentWorld!;
        RenderView? raceView = _game.GameManager.ViewManager.Views.FirstOrDefault(view => ReferenceEquals(view.World, world));
        if (raceView == null)
        {
            Logs.WriteWarning($"[CaptureBench] view {captureName}: no render view found for the race world.");
            return;
        }

        string pipeline = world.EnvironmentSettings.ColorPipeline.Mode == ColorPipelineMode.LinearHdr ? "linear" : "legacy";
        Logs.WriteInfo($"[CaptureBench] view {captureName} pipeline {pipeline} fxaaPasses {raceView.RenderStats.FxaaPasses}");
        LogShadowFocus(captureName, world);
    }

    // With the shadow cast in perspective: the point the game computes from its own car next to the one of the view (the
    // original's car). On the start views the two cars are on the same grid, so the two points must be the same.
    private static void LogShadowFocus(string captureName, World world)
    {
        RaceShadowFocusComponent? focus = world.Entities
            .Select(static entity => entity.GetComponent<RaceShadowFocusComponent>())
            .FirstOrDefault(static component => component != null);
        if (focus?.ComputedCarFocus is not { } carFocus || focus.FocusOverride is not { } viewFocus)
        {
            return;
        }

        Logs.WriteInfo(FormattableString.Invariant(
            $"[CaptureBench] view {captureName} shadowFocus car {Format(carFocus)} view {Format(viewFocus)} distance {Vector3.Distance(carFocus, viewFocus):F3}"));
    }

    // Proof of the mipmap chains of the race world (S1m), once the first race world is ready: the level count of three cooked
    // textures of the world, acquired by id and loaded as the materials load them, and the level count and the chain of the
    // two paint textures of the first car. "expected" is the full chain of the texture's size.
    private static readonly (string Name, Guid Id)[] MipmapTextures =
    [
        ("Texture.Road", new Guid("438bfe97-634a-50cc-a008-7661c239ab25")),
        ("Texture.Landscape", new Guid("ee748e62-6069-552c-a7cb-2656da413b49")),
        ("Texture.Building", new Guid("5dd4c964-020b-59c5-9d59-b40fc856c0f9")),
    ];

    private void LogMipmaps()
    {
        AssetContentManager assetContentManager = _game.AssetContentManager;
        foreach ((string name, Guid id) in MipmapTextures)
        {
            using AssetHandle<Texture> handle = assetContentManager.Acquire<Texture>(id);
            handle.Asset.Load(assetContentManager);
            Texture2D resource = handle.Asset.Resource;
            Logs.WriteInfo(FormattableString.Invariant(
                $"[CaptureBench] mipmaps {name} levels {resource.LevelCount} expected {MipmapChain.GetLevelCount(resource.Width, resource.Height)}"));
        }

        (Texture2D Painted, Texture2D Unpainted)? paint = LegacyCarVisualFactory.GetCarPaintTextures(0);
        if (paint == null)
        {
            Logs.WriteWarning("[CaptureBench] mipmaps CarPaint: no paint textures for the first car.");
            return;
        }

        LogPaintMipmaps("CarPaint.Painted", paint.Value.Painted);
        LogPaintMipmaps("CarPaint.Unpainted", paint.Value.Unpainted);
        LogCarPaintSlots();
    }

    // The car-texture slots of the first car that sample the painted texture and the unpainted one (S1f): the cooked
    // materials decide which slot is painted, and this count shows that a change of the cooked materials keeps the paint.
    private static void LogCarPaintSlots()
    {
        (int Painted, int Unpainted)? slots = LegacyCarVisualFactory.CountCarTextureSlots(0);
        if (slots == null)
        {
            Logs.WriteWarning("[CaptureBench] car paint slots: no painted model for the first car.");
            return;
        }

        Logs.WriteInfo(FormattableString.Invariant($"[CaptureBench] car paint slots painted {slots.Value.Painted} unpainted {slots.Value.Unpainted}"));
    }

    // chain ok: level 1 read back from the texture is the box average of the level 0 read back from it.
    private static void LogPaintMipmaps(string name, Texture2D texture)
    {
        var level0 = new Color[texture.Width * texture.Height];
        texture.GetData(0, null, level0, 0, level0.Length);
        Color[] expectedLevel1 = MipmapChain.ComputeNextLevel(level0, texture.Width, texture.Height);
        var level1 = new Color[expectedLevel1.Length];
        texture.GetData(1, null, level1, 0, level1.Length);
        string chain = level1.AsSpan().SequenceEqual(expectedLevel1) ? "ok" : "KO";
        Logs.WriteInfo(FormattableString.Invariant(
            $"[CaptureBench] mipmaps {name} levels {texture.LevelCount} expected {MipmapChain.GetLevelCount(texture.Width, texture.Height)} chain {chain}"));
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
    private void LogAlignment(ReferenceTrack track, ReferenceTrackSpace space)
    {
        World world = _game.GameManager.CurrentWorld!;
        Logs.WriteInfo(FormattableString.Invariant(
            $"Reference view capture: track {track.Name} origin {Format(space.Origin)}, road generator world translation {Format(space.RoadWorld.Translation)}, identity {space.RoadWorld == Matrix.Identity}"));
        foreach (ReferenceView view in track.Views)
        {
            Vector3 converted = space.ToRaceWorld(view.CarPosition);
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
                $"Reference view capture: alignment {track.Name}-{view.Name}: original car at {Format(converted)}, nearest anchor {nearest?.Name} at {Format(nearest?.RootComponent?.Position ?? Vector3.Zero)} ({nearestDistance:0.00} m)"));
        }
    }

    private static string Format(Vector3 vector)
    {
        return FormattableString.Invariant($"({vector.X:0.000}, {vector.Y:0.000}, {vector.Z:0.000})");
    }

    // Measures the road points and the placed models of the race world against the original's scene.json, and checks that
    // the conversion to the race world is inverted exactly; a failure is a warning, the pictures are still taken.
    private void MeasureScene(ReferenceTrack track, ReferenceTrackSpace space)
    {
        float pointError = space.MaxPointRoundTripError(track.Views.SelectMany(view => new[] { view.Position, view.Target, view.CarPosition }));
        if (_sceneFilePath == null)
        {
            Logs.WriteInfo(FormattableString.Invariant(
                $"Reference view capture: round trip {track.Name}: point error max {pointError:0.000000} m over {track.Views.Count * 3} view points"));
            return;
        }

        try
        {
            string catalogTrackName = RaceFrontEndCatalog.Tracks[FindCatalogTrackIndex(track.Name)].Name;
            RaceTrackLayout layout = LegacyTrackSceneFactory.ComputeRaceLayout(catalogTrackName, space.Source);
            ReferenceSceneSnapshot snapshot = ReferenceSceneSnapshot.Capture(_game.GameManager.CurrentWorld!, space, layout, out int notBuiltCount);

            IEnumerable<RaceTrackModelPlacement> placements = layout.Scenery.Concat(layout.LeftPosts).Concat(layout.RightPosts).Concat(layout.ColumnSegments);
            float transformError = space.MaxTransformRoundTripError(placements.Select(placement => placement.Transform));
            float roadPointError = space.MaxPointRoundTripError(layout.RoadPoints.Select(point => point.Position));
            Logs.WriteInfo(FormattableString.Invariant(
                $"Reference view capture: round trip {track.Name}: point error max {Math.Max(pointError, roadPointError):0.000000} m over {track.Views.Count * 3} view points and {layout.RoadPoints.Count} road points, transform error max {transformError:0.000000} over {placements.Count()} placed transforms"));

            string childrenSummary = string.Join(", ", snapshot.GeneratorChildren.Select(children =>
                $"{children.Label} {children.ChildCount} children / {children.PlacementCount} placements ({(children.ChildCount == children.PlacementCount && children.ChildrenWithoutPlacement == 0 && children.PlacementsWithoutChild == 0 ? "equal" : $"{children.ChildrenWithoutPlacement} child(ren) without a placement, {children.PlacementsWithoutChild} placement(s) without a child")})"));
            Logs.WriteInfo($"Reference view capture: generated children {track.Name}: {childrenSummary}; the transforms of the children were read from the entities (world matrix of each Post.<n> and Segment.<n> component)");

            string summary = ReferenceSceneMeasure.Measure(_sceneFilePath, track.Name, snapshot, _outputDirectory);
            Logs.WriteInfo($"Reference view capture: scene measure {track.Name}: {summary} ({notBuiltCount} placement(s) of the layout without an entity or component in the race world)");
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
        if (cameraEntity!.GetComponent<DebugFreeCameraComponent>() is { } debugCamera)
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

    private bool ApplyView(ReferenceView view)
    {
        if (_trackSpace == null || !TryGetRaceCamera(out CameraLookAtComponent? camera, out _))
        {
            return false;
        }

        // The view matrix takes its up vector from the orientation, which the chase camera left tilted: the
        // original's views are all taken with the world up.
        camera!.LocalOrientation = Quaternion.Identity;
        camera.FieldOfView = _fieldOfViewY;
        camera.NearPlane = _nearPlane;
        camera.FarPlane = _farPlane;
        camera.SetPositionAndTarget(_trackSpace.ToRaceWorld(view.Position), _trackSpace.ToRaceWorld(view.Target));

        // The original aims its shadow map ahead of the car of its capture (RacingGameOriginal
        // Core/Shaders/ShadowMapShader.cs:208-266): the point is computed in the original's coordinates, then converted.
        Vector3 shadowFocus = _trackSpace.ToRaceWorld(view.CarPosition + view.CarDirection * RaceShadowFocusComponent.FocusLeadDistance);
        foreach (Entity entity in _game.GameManager.CurrentWorld!.Entities)
        {
            if (entity.Name == RaceWorldFactory.PlayerCarEntityName)
            {
                entity.IsVisible = view.CarVisible;
            }

            if (entity.GetComponent<RaceShadowFocusComponent>() is { } focus)
            {
                focus.FocusOverride = shadowFocus;
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
        _game.Exit();
    }

    private void Fail(string message)
    {
        Logs.WriteError(message);
        Environment.ExitCode = 1;
        _step = CaptureStep.Failed;
        _game.Exit();
    }
}

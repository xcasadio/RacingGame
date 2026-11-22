using CasaEngine.Core.Log;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Worlds;
using System.Diagnostics.CodeAnalysis;
using RacingGame.Gameplay.Components;
using RacingGame.Gameplay.Race;
using RacingGame.Gameplay.Track;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class TrackMigrationCaptureValidator
{
    private enum CaptureStep
    {
        WaitForFrontEnd,
        StartRace,
        WaitForRace,
        PrepareChaseRoadOnly,
        CaptureChaseRoadOnly,
        PrepareRoadOnlyStart,
        CaptureRoadOnlyStart,
        PrepareStartFull,
        CaptureStartFull,
        PrepareSector20,
        CaptureSector20,
        PrepareSector50,
        CaptureSector50,
        PrepareSector80,
        CaptureSector80,
        PrepareTerrainOverview,
        CaptureTerrainOverview,
        PrepareHelperPalms,
        CaptureHelperPalms,
        PrepareHelperLaterns,
        CaptureHelperLaterns,
        PrepareHelperTunnel,
        CaptureHelperTunnel,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private const int MinDrawsBetweenSteps = 3;
    private static readonly TimeSpan DefaultStepDelay = TimeSpan.FromMilliseconds(250);

    // The chase rig smooths its pose (6 and 7.5 per second): after this delay it has converged, so the chase view
    // no longer depends on how many updates ran. The start light is hidden in the road-only view.
    private static readonly TimeSpan ChaseSettleDelay = TimeSpan.FromMilliseconds(2000);

    // Road point indices of the views where the road helpers act (plan rgce-track-helpers-tasks.md, P6): the middle of
    // the first palm span, of the first latern span and of the longest tunnel. Reading the helpers does not move the
    // road points, so the same views compare before and after.
    private static readonly Dictionary<string, (int Palms, int Laterns, int Tunnel)> HelperViewRoadPoints = new(StringComparer.Ordinal)
    {
        ["Beginner"] = (215, 428, 817),
        ["Advanced"] = (133, 408, 659),
        ["Expert"] = (398, 177, 3764),
    };

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly List<string> _capturePaths = [];
    private CaptureStep _step = CaptureStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private long _drawCountAtTransition;
    private TimeSpan _stepDelay = DefaultStepDelay;
    private bool _started;
    private int _trackIndex;

    public TrackMigrationCaptureValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow)
    {
        _game = game;
        _flow = flow;
        _game.PreviewUpdate += OnPreviewUpdate;
    }

    private void OnPreviewUpdate(object? sender, TimeSpan totalTime)
    {
        if (!_started)
        {
            _started = true;
            _startedAt = totalTime;
            _lastTransitionAt = totalTime;
            Logs.WriteInfo("Track migration capture audit started");
        }

        if (_step is CaptureStep.Completed or CaptureStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > TimeSpan.FromSeconds(60))
        {
            Fail("Track migration capture audit timed out.");
            return;
        }

        if (totalTime - _lastTransitionAt < _stepDelay)
        {
            return;
        }

        // A capture synchronously encodes a PNG, then the fixed-timestep loop runs catch-up updates without drawing:
        // game time alone can elapse with no frame drawn, and the next capture would read the previous view.
        if (_game.DrawCount - _drawCountAtTransition < MinDrawsBetweenSteps)
        {
            return;
        }

        switch (_step)
        {
            case CaptureStep.WaitForFrontEnd:
                if (_game.GameManager.CurrentWorld?.Name == RaceWorldFactory.FrontEndWorldName)
                {
                    Advance(CaptureStep.StartRace, totalTime, "Front-end ready for automated race capture");
                }
                break;

            case CaptureStep.StartRace:
                if (_trackIndex >= RaceFrontEndCatalog.Tracks.Count)
                {
                    Complete();
                    return;
                }

                _flow.State.SelectedCarIndex = 0;
                _flow.State.SelectedTrackIndex = _trackIndex;
                _flow.StartRaceForAutomation();
                Advance(CaptureStep.WaitForRace, totalTime, $"Loading track {RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
                break;

            case CaptureStep.WaitForRace:
                if (_game.GameManager.CurrentWorld is { } raceWorld
                    && RaceWorldFactory.IsRaceWorld(raceWorld)
                    && _game.RaceSession.IsActive
                    && _game.GameManager.ScreenManager.CurrentState == RaceFrontEndFlow.RaceHudStateName)
                {
                    Advance(CaptureStep.PrepareChaseRoadOnly, totalTime, $"Race world ready for {RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
                }
                break;

            case CaptureStep.PrepareChaseRoadOnly:
                _game.SetDebugCameraEnabled(false);
                _game.SetCircuitOnlyViewEnabled(true);
                Advance(CaptureStep.CaptureChaseRoadOnly, totalTime, "Prepared chase-camera road-only view", ChaseSettleDelay);
                break;

            case CaptureStep.CaptureChaseRoadOnly:
                CaptureCurrentView("chase-road-only");
                _game.SetDebugCameraEnabled(true);
                _game.SetCameraPosedByAutomation(true);
                Advance(CaptureStep.PrepareRoadOnlyStart, totalTime, "Captured chase-camera road-only view");
                break;

            case CaptureStep.PrepareRoadOnlyStart:
                PrepareStartView(roadOnly: true);
                Advance(CaptureStep.CaptureRoadOnlyStart, totalTime, "Prepared road-only start view");
                break;

            case CaptureStep.CaptureRoadOnlyStart:
                CaptureCurrentView("road-only-start");
                Advance(CaptureStep.PrepareStartFull, totalTime, "Captured road-only start view");
                break;

            case CaptureStep.PrepareStartFull:
                PrepareStartView(roadOnly: false);
                Advance(CaptureStep.CaptureStartFull, totalTime, "Prepared full start view");
                break;

            case CaptureStep.CaptureStartFull:
                CaptureCurrentView("start-full");
                Advance(CaptureStep.PrepareSector20, totalTime, "Captured full start view");
                break;

            case CaptureStep.PrepareSector20:
                if (PrepareCheckpointView(checkpointIndex: 0, roadOnly: false))
                {
                    Advance(CaptureStep.CaptureSector20, totalTime, "Prepared first sector view");
                }
                else
                {
                    Fail("Unable to locate checkpoints for automated capture.");
                }
                break;

            case CaptureStep.CaptureSector20:
                CaptureCurrentView("sector-20");
                Advance(CaptureStep.PrepareSector50, totalTime, "Captured first sector view");
                break;

            case CaptureStep.PrepareSector50:
                if (PrepareCheckpointView(checkpointIndex: 1, roadOnly: false))
                {
                    Advance(CaptureStep.CaptureSector50, totalTime, "Prepared second sector view");
                }
                else
                {
                    Advance(CaptureStep.ReturnToFrontEnd, totalTime, "Skipping second sector view because the race world is no longer active");
                }
                break;

            case CaptureStep.CaptureSector50:
                CaptureCurrentView("sector-50");
                Advance(CaptureStep.PrepareSector80, totalTime, "Captured second sector view");
                break;

            case CaptureStep.PrepareSector80:
                if (PrepareCheckpointView(checkpointIndex: 2, roadOnly: false))
                {
                    Advance(CaptureStep.CaptureSector80, totalTime, "Prepared third sector view");
                }
                else
                {
                    Advance(CaptureStep.PrepareTerrainOverview, totalTime, "Skipping third sector view because the race world is no longer active");
                }
                break;

            case CaptureStep.CaptureSector80:
                CaptureCurrentView("sector-80");
                Advance(CaptureStep.PrepareTerrainOverview, totalTime, "Captured third sector view");
                break;

            case CaptureStep.PrepareTerrainOverview:
                if (PrepareTerrainOverview())
                {
                    Advance(CaptureStep.CaptureTerrainOverview, totalTime, "Prepared terrain overview");
                }
                else
                {
                    Advance(CaptureStep.ReturnToFrontEnd, totalTime, "Skipping terrain overview because the race world is no longer active");
                }
                break;

            case CaptureStep.CaptureTerrainOverview:
                CaptureCurrentView("terrain-overview");
                Advance(CaptureStep.PrepareHelperPalms, totalTime, "Captured terrain overview");
                break;

            case CaptureStep.PrepareHelperPalms:
                if (PrepareRoadPointView(HelperViewRoadPoints[RaceFrontEndCatalog.Tracks[_trackIndex].Name].Palms))
                {
                    Advance(CaptureStep.CaptureHelperPalms, totalTime, "Prepared palm helper view");
                }
                else
                {
                    Fail("Unable to locate the road points for the palm helper view.");
                }
                break;

            case CaptureStep.CaptureHelperPalms:
                CaptureCurrentView("helper-palms");
                Advance(CaptureStep.PrepareHelperLaterns, totalTime, "Captured palm helper view");
                break;

            case CaptureStep.PrepareHelperLaterns:
                if (PrepareRoadPointView(HelperViewRoadPoints[RaceFrontEndCatalog.Tracks[_trackIndex].Name].Laterns))
                {
                    Advance(CaptureStep.CaptureHelperLaterns, totalTime, "Prepared latern helper view");
                }
                else
                {
                    Fail("Unable to locate the road points for the latern helper view.");
                }
                break;

            case CaptureStep.CaptureHelperLaterns:
                CaptureCurrentView("helper-laterns");
                Advance(CaptureStep.PrepareHelperTunnel, totalTime, "Captured latern helper view");
                break;

            case CaptureStep.PrepareHelperTunnel:
                if (PrepareRoadPointView(HelperViewRoadPoints[RaceFrontEndCatalog.Tracks[_trackIndex].Name].Tunnel))
                {
                    Advance(CaptureStep.CaptureHelperTunnel, totalTime, "Prepared tunnel helper view");
                }
                else
                {
                    Fail("Unable to locate the road points for the tunnel helper view.");
                }
                break;

            case CaptureStep.CaptureHelperTunnel:
                CaptureCurrentView("helper-tunnel");
                Advance(CaptureStep.ReturnToFrontEnd, totalTime, "Captured tunnel helper view");
                break;

            case CaptureStep.ReturnToFrontEnd:
                _game.SetCircuitOnlyViewEnabled(false);
                _game.SetDebugCameraEnabled(false);
                _game.SetCameraPosedByAutomation(false);
                _flow.ReturnToFrontEndForAutomation();
                _trackIndex++;
                Advance(CaptureStep.WaitForFrontEnd, totalTime, "Returning to front-end for next track");
                break;
        }
    }

    private void PrepareStartView(bool roadOnly)
    {
        if (!TryGetRaceCamera(out PerspectiveCameraComponent? camera, out World? world))
        {
            Fail("Unable to locate race camera for automated capture.");
            return;
        }

        Entity? playerStart = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.PlayerStartEntityName);
        if (playerStart?.RootComponent == null)
        {
            Fail("Unable to locate player start for automated capture.");
            return;
        }

        SceneComponent playerStartRoot = playerStart.RootComponent;

        _game.SetCircuitOnlyViewEnabled(roadOnly);

        Vector3 forward = playerStartRoot.Forward;
        if (forward.LengthSquared() < 0.0001f)
        {
            forward = Vector3.Forward;
        }
        else
        {
            forward.Normalize();
        }

        Vector3 worldUp = Vector3.Up;
        Vector3 right = Vector3.Cross(forward, worldUp);
        if (right.LengthSquared() < 0.0001f)
        {
            right = Vector3.Right;
        }
        else
        {
            right.Normalize();
        }

        Vector3 target = playerStartRoot.Position + forward * 8.0f;
        Vector3 position = playerStartRoot.Position - forward * 20.0f + worldUp * 10.0f + right * 5.0f;
        PoseCamera(camera, position, target);
    }

    private bool PrepareCheckpointView(int checkpointIndex, bool roadOnly)
    {
        if (!TryGetRaceCamera(out PerspectiveCameraComponent? camera, out World? world))
        {
            return false;
        }

        List<Entity> checkpoints = world.Entities
            .Where(entity => entity.Name.StartsWith(RaceWorldFactory.CheckpointEntityNamePrefix, StringComparison.Ordinal))
            .OrderBy(entity => entity.Name, StringComparer.Ordinal)
            .ToList();

        if (checkpoints.Count == 0)
        {
            return false;
        }

        int clampedIndex = Math.Clamp(checkpointIndex, 0, checkpoints.Count - 1);
        Entity currentCheckpoint = checkpoints[clampedIndex];
        Entity nextCheckpoint = checkpoints[(clampedIndex + 1) % checkpoints.Count];

        Vector3 currentPosition = currentCheckpoint.RootComponent?.Position ?? Vector3.Zero;
        Vector3 nextPosition = nextCheckpoint.RootComponent?.Position ?? currentPosition + Vector3.Forward;
        Vector3 direction = nextPosition - currentPosition;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector3.Forward;
        }
        else
        {
            direction.Normalize();
        }

        _game.SetCircuitOnlyViewEnabled(roadOnly);

        Vector3 position = currentPosition - direction * 18.0f + Vector3.Up * 12.0f + Vector3.Right * 4.0f;
        Vector3 target = currentPosition + direction * 6.0f;
        PoseCamera(camera, position, target);
        return true;
    }

    /// <summary>
    /// Poses the camera as <see cref="PrepareCheckpointView"/> does, from the road point <paramref name="roadPointIndex"/>
    /// of the loaded track. The road points come from the track and height map ids of the road generator, through
    /// the same layout it builds the road from.
    /// </summary>
    private bool PrepareRoadPointView(int roadPointIndex)
    {
        if (!TryGetRaceCamera(out PerspectiveCameraComponent? camera, out World? world))
        {
            return false;
        }

        Entity? road = world.Entities.FirstOrDefault(entity => entity.Name?.StartsWith(RaceWorldNames.TrackRoadEntityNamePrefix, StringComparison.Ordinal) == true);
        if (road?.RootComponent is not RaceRoadGeneratorComponent generator
            || !LegacyTrackSceneFactory.TryResolveTrackSource("Track capture audit", generator.TrackAssetId, generator.HeightmapAssetId, out RaceTrackSource? source))
        {
            return false;
        }

        RaceTrackLayout layout = LegacyTrackSceneFactory.ComputeGeneratorLayout(source!, RaceTrackLayoutSettings.Default);
        if (roadPointIndex < 0 || roadPointIndex >= layout.RoadPoints.Count)
        {
            return false;
        }

        RaceTrackRoadPoint roadPoint = layout.RoadPoints[roadPointIndex];
        Matrix roadWorld = generator.WorldMatrixWithScale;
        Vector3 pointPosition = Vector3.Transform(RaceTrackSpace.ConvertLegacyPoint(roadPoint.Position) - layout.Origin, roadWorld);
        Vector3 direction = Vector3.TransformNormal(RaceTrackSpace.ConvertLegacyDirection(roadPoint.Direction), roadWorld);
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector3.Forward;
        }
        else
        {
            direction.Normalize();
        }

        _game.SetCircuitOnlyViewEnabled(false);

        Vector3 position = pointPosition - direction * 18.0f + Vector3.Up * 12.0f + Vector3.Right * 4.0f;
        Vector3 target = pointPosition + direction * 6.0f;
        PoseCamera(camera, position, target);
        return true;
    }

    private bool PrepareTerrainOverview()
    {
        if (!TryGetRaceCamera(out PerspectiveCameraComponent? camera, out World? world))
        {
            return false;
        }

        List<Entity> trackEntities = world.Entities
            .Where(entity => entity.Name?.StartsWith("Track.", StringComparison.Ordinal) == true)
            .ToList();
        if (trackEntities.Count == 0)
        {
            return false;
        }

        BoundingBox bounds = trackEntities[0].GetBoundingBox();
        for (int index = 1; index < trackEntities.Count; index++)
        {
            BoundingBox entityBounds = trackEntities[index].GetBoundingBox();
            bounds = BoundingBox.CreateMerged(bounds, entityBounds);
        }

        _game.SetCircuitOnlyViewEnabled(false);

        Vector3 center = (bounds.Min + bounds.Max) * 0.5f;
        Vector3 size = bounds.Max - bounds.Min;
        float horizontalSpan = Math.Max(size.X, size.Z);
        float distance = Math.Max(horizontalSpan * 0.55f, 90f);
        Vector3 position = center + new Vector3(-distance * 0.6f, Math.Max(size.Y, 40f) + 70f, -distance * 0.6f);
        Vector3 target = center + Vector3.Up * Math.Max(8f, size.Y * 0.1f);
        PoseCamera(camera, position, target);
        return true;
    }

    private void CaptureCurrentView(string suffix)
    {
        string trackName = RaceFrontEndCatalog.Tracks[_trackIndex].Name;
        string fileStem = $"audit-{trackName}-{suffix}";
        string capturePath = _game.CaptureScreenshotWithStem(fileStem);
        if (!string.IsNullOrWhiteSpace(capturePath))
        {
            _capturePaths.Add(capturePath);
        }
    }

    private static void PoseCamera(PerspectiveCameraComponent camera, Vector3 position, Vector3 target)
    {
        // SetPositionAndTarget writes the whole pose, orientation included, with the world up: nothing is left of
        // the orientation of the chase rig, which depends on how many updates it ran.
        camera.SetPositionAndTarget(position, target);
    }

    private bool TryGetRaceCamera([NotNullWhen(true)] out PerspectiveCameraComponent? camera, [NotNullWhen(true)] out World? world)
    {
        world = _game.GameManager.CurrentWorld;
        camera = null;
        if (world == null)
        {
            return false;
        }

        Entity? cameraEntity = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.CameraEntityName);
        camera = cameraEntity?.RootComponent as PerspectiveCameraComponent;
        return camera != null;
    }

    private void Advance(CaptureStep nextStep, TimeSpan totalTime, string message, TimeSpan? stepDelay = null)
    {
        Logs.WriteInfo($"Track capture audit: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
        _drawCountAtTransition = _game.DrawCount;
        _stepDelay = stepDelay ?? DefaultStepDelay;
    }

    private void Complete()
    {
        Logs.WriteInfo($"Track migration capture audit completed successfully ({_capturePaths.Count} screenshot(s))");
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
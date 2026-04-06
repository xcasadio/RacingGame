using CasaEngine.Core.Log;
using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.World;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Worlds;
using System.Diagnostics.CodeAnalysis;

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
        PrepareSector20,
        CaptureSector20,
        PrepareSector50,
        CaptureSector50,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly List<string> _capturePaths = [];
    private CaptureStep _step = CaptureStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
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

        if (totalTime - _startedAt > TimeSpan.FromSeconds(45))
        {
            Fail("Track migration capture audit timed out.");
            return;
        }

        if (totalTime - _lastTransitionAt < TimeSpan.FromMilliseconds(250))
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
                Advance(CaptureStep.CaptureChaseRoadOnly, totalTime, "Prepared chase-camera road-only view");
                break;

            case CaptureStep.CaptureChaseRoadOnly:
                CaptureCurrentView("chase-road-only");
                _game.SetDebugCameraEnabled(true);
                Advance(CaptureStep.PrepareRoadOnlyStart, totalTime, "Captured chase-camera road-only view");
                break;

            case CaptureStep.PrepareRoadOnlyStart:
                PrepareStartView(roadOnly: true);
                Advance(CaptureStep.CaptureRoadOnlyStart, totalTime, "Prepared road-only start view");
                break;

            case CaptureStep.CaptureRoadOnlyStart:
                CaptureCurrentView("road-only-start");
                Advance(CaptureStep.PrepareSector20, totalTime, "Captured road-only start view");
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
                Advance(CaptureStep.ReturnToFrontEnd, totalTime, "Captured second sector view");
                break;

            case CaptureStep.ReturnToFrontEnd:
                _game.SetCircuitOnlyViewEnabled(false);
                _game.SetDebugCameraEnabled(false);
                _flow.ReturnToFrontEndForAutomation();
                _trackIndex++;
                Advance(CaptureStep.WaitForFrontEnd, totalTime, "Returning to front-end for next track");
                break;
        }
    }

    private void PrepareStartView(bool roadOnly)
    {
        if (!TryGetRaceCamera(out CameraLookAtComponent? camera, out World? world))
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
        camera.SetPositionAndTarget(position, target);
    }

    private bool PrepareCheckpointView(int checkpointIndex, bool roadOnly)
    {
        if (!TryGetRaceCamera(out CameraLookAtComponent? camera, out World? world))
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
        camera.SetPositionAndTarget(position, target);
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

    private bool TryGetRaceCamera([NotNullWhen(true)] out CameraLookAtComponent? camera, [NotNullWhen(true)] out World? world)
    {
        world = _game.GameManager.CurrentWorld;
        camera = null;
        if (world == null)
        {
            return false;
        }

        Entity? cameraEntity = world.Entities.FirstOrDefault(entity => entity.Name == RaceWorldFactory.CameraEntityName);
        camera = cameraEntity?.RootComponent as CameraLookAtComponent;
        return camera != null;
    }

    private void Advance(CaptureStep nextStep, TimeSpan totalTime, string message)
    {
        Logs.WriteInfo($"Track capture audit: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
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
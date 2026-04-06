using CasaEngine.Core.Log;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class TrackRuntimeSceneExportValidator
{
    private enum ExportStep
    {
        WaitForFrontEnd,
        StartRace,
        WaitForRace,
        WaitForStableRaceWorld,
        ExportScene,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly string _outputFilePath;
    private readonly List<TrackRuntimeSceneExporter.TrackRuntimeSceneSnapshot> _trackSnapshots = [];
    private ExportStep _step = ExportStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private bool _started;
    private int _trackIndex;
    private int _lastObservedEntityCount = -1;
    private int _stableEntitySamples;

    public TrackRuntimeSceneExportValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow, string? outputFilePath)
    {
        _game = game;
        _flow = flow;
        _outputFilePath = ResolveOutputFilePath(outputFilePath, _game);
        _game.PreviewUpdate += OnPreviewUpdate;
    }

    private void OnPreviewUpdate(object? sender, TimeSpan totalTime)
    {
        if (!_started)
        {
            _started = true;
            _startedAt = totalTime;
            _lastTransitionAt = totalTime;
            Logs.WriteInfo($"Track runtime scene export started: {_outputFilePath}");
        }

        if (_step is ExportStep.Completed or ExportStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > TimeSpan.FromSeconds(45))
        {
            Fail("Track runtime scene export timed out.");
            return;
        }

        if (totalTime - _lastTransitionAt < TimeSpan.FromMilliseconds(250))
        {
            return;
        }

        switch (_step)
        {
            case ExportStep.WaitForFrontEnd:
                if (_game.GameManager.CurrentWorld?.Name == RaceWorldFactory.FrontEndWorldName)
                {
                    Advance(ExportStep.StartRace, totalTime, "Front-end ready for runtime scene export");
                }
                break;

            case ExportStep.StartRace:
                if (_trackIndex >= RaceFrontEndCatalog.Tracks.Count)
                {
                    Complete();
                    return;
                }

                _flow.State.SelectedCarIndex = 0;
                _flow.State.SelectedTrackIndex = _trackIndex;
                _flow.StartRaceForAutomation();
                Advance(ExportStep.WaitForRace, totalTime, $"Loading track {RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
                break;

            case ExportStep.WaitForRace:
                if (_game.GameManager.CurrentWorld is { } raceWorld
                    && RaceWorldFactory.IsRaceWorld(raceWorld)
                    && _game.RaceSession.IsActive
                    && _game.GameManager.ScreenManager.CurrentState == RaceFrontEndFlow.RaceHudStateName)
                {
                    _lastObservedEntityCount = raceWorld.Entities.Count;
                    _stableEntitySamples = 0;
                    Advance(ExportStep.WaitForStableRaceWorld, totalTime, $"Race world ready for {RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
                }
                break;

            case ExportStep.WaitForStableRaceWorld:
                if (_game.GameManager.CurrentWorld is not { } stableRaceWorld || !RaceWorldFactory.IsRaceWorld(stableRaceWorld))
                {
                    Fail("Runtime scene export lost the race world before stabilization.");
                    return;
                }

                int currentEntityCount = stableRaceWorld.Entities.Count;
                if (currentEntityCount == _lastObservedEntityCount)
                {
                    _stableEntitySamples++;
                }
                else
                {
                    _lastObservedEntityCount = currentEntityCount;
                    _stableEntitySamples = 0;
                }

                if (_stableEntitySamples >= 2)
                {
                    Advance(ExportStep.ExportScene, totalTime, $"Race world stabilized for {RaceFrontEndCatalog.Tracks[_trackIndex].Name} with {currentEntityCount} entities");
                }
                break;

            case ExportStep.ExportScene:
                if (_game.GameManager.CurrentWorld is not { } exportWorld || !RaceWorldFactory.IsRaceWorld(exportWorld))
                {
                    Fail("Runtime scene export expected a race world but none was active.");
                    return;
                }

                _trackSnapshots.Add(TrackRuntimeSceneExporter.CaptureTrack(RaceFrontEndCatalog.Tracks[_trackIndex].Name, exportWorld));
                Advance(ExportStep.ReturnToFrontEnd, totalTime, $"Exported runtime scene for {RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
                break;

            case ExportStep.ReturnToFrontEnd:
                _flow.ReturnToFrontEndForAutomation();
                _trackIndex++;
                Advance(ExportStep.WaitForFrontEnd, totalTime, "Returning to front-end for next runtime export");
                break;
        }
    }

    private void Advance(ExportStep nextStep, TimeSpan totalTime, string message)
    {
        Logs.WriteInfo($"Track runtime export: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
    }

    private void Complete()
    {
        TrackRuntimeSceneExporter.WriteFile(_outputFilePath, _trackSnapshots);
        Logs.WriteInfo($"Track runtime scene export completed successfully ({_trackSnapshots.Count} track(s))");
        Environment.ExitCode = 0;
        _step = ExportStep.Completed;
        _game.Exit();
    }

    private void Fail(string message)
    {
        Logs.WriteError(message);
        Environment.ExitCode = 1;
        _step = ExportStep.Failed;
        _game.Exit();
    }

    private static string ResolveOutputFilePath(string? outputFilePath, RacingGameCasaEngineGame game)
    {
        if (!string.IsNullOrWhiteSpace(outputFilePath))
        {
            return Path.GetFullPath(outputFilePath);
        }

        return Path.Combine(
            game.GetUserDataDirectory(),
            "TrackAudit",
            "racinggame-casaengine-live-runtime-scene.json");
    }
}
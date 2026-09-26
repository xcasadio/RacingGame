using System.Globalization;
using System.Text;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Entities;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.Gameplay;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class CarTopSpeedAuditValidator
{
    private enum AuditStep
    {
        WaitForFrontEnd,
        StartRace,
        WaitForRace,
        WaitForCountdown,
        RunTopSpeedSample,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private const float MinimumSampleDurationSeconds = 8.0f;
    private const float MaximumSampleDurationSeconds = 18.0f;
    private const float StabilitySampleIntervalSeconds = 0.5f;
    private const float StabilitySpeedDeltaThresholdMph = 0.35f;
    private const float RequiredStableDurationSeconds = 1.5f;

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly string _outputFilePath;
    private readonly string _detailDirectoryPath;
    private readonly int _trackIndex;
    private readonly List<CarTopSpeedAuditResult> _results = [];
    private AuditStep _step = AuditStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private TimeSpan _lastStabilitySampleAt;
    private bool _started;
    private int _carIndex;
    private float _sampleStartSpeedMph;
    private float _sampleEndSpeedMph;
    private float _peakSpeedMph;
    private int _peakGear;
    private float _stableDurationSeconds;
    private float _lastStabilitySampleSpeedMph;
    private bool _converged;

    public CarTopSpeedAuditValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow, string? outputFilePath, string? trackName)
    {
        _game = game;
        _flow = flow;
        _outputFilePath = ResolveOutputFilePath(outputFilePath);
        _detailDirectoryPath = Path.Combine(Path.GetDirectoryName(_outputFilePath)!, "details");
        _trackIndex = ResolveTrackIndex(trackName);
        _game.PreviewUpdate += OnPreviewUpdate;
    }

    private void OnPreviewUpdate(object? sender, TimeSpan totalTime)
    {
        _ = sender;

        if (!_started)
        {
            _started = true;
            _startedAt = totalTime;
            _lastTransitionAt = totalTime;
            Logs.WriteInfo($"Car top-speed audit started: {_outputFilePath}");
        }

        if (_step is AuditStep.Completed or AuditStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > TimeSpan.FromSeconds(180))
        {
            Fail("Car top-speed audit timed out.");
            return;
        }

        switch (_step)
        {
            case AuditStep.WaitForFrontEnd:
                if (CanAdvance(totalTime)
                    && _game.GameManager.CurrentWorld?.Name == RaceWorldFactory.FrontEndWorldName)
                {
                    Advance(AuditStep.StartRace, totalTime, "Front-end ready for top-speed audit");
                }
                break;

            case AuditStep.StartRace:
                if (CanAdvance(totalTime))
                {
                    if (_carIndex >= RaceFrontEndCatalog.Cars.Count)
                    {
                        Complete();
                        return;
                    }

                    _flow.State.SelectedCarIndex = _carIndex;
                    _flow.State.SelectedTrackIndex = _trackIndex;
                    _flow.State.SelectedDrivingMode = VehicleDrivingMode.Simulation;
                    _flow.StartRaceForAutomation();
                    Advance(
                        AuditStep.WaitForRace,
                        totalTime,
                        $"Loading top-speed scenario car={RaceFrontEndCatalog.Cars[_carIndex].Name} track={RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
                }
                break;

            case AuditStep.WaitForRace:
                if (CanAdvance(totalTime)
                    && _game.GameManager.CurrentWorld is { } raceWorld
                    && RaceWorldFactory.IsRaceWorld(raceWorld)
                    && _game.RaceSession.IsActive
                    && _game.GameManager.ScreenManager.CurrentState == RaceFrontEndFlow.RaceHudStateName)
                {
                    Advance(AuditStep.WaitForCountdown, totalTime, "Race world ready for top-speed audit");
                }
                break;

            case AuditStep.WaitForCountdown:
                if (CanAdvance(totalTime)
                    && TryGetActivePawn(out RacingCarPawn countdownPawn)
                    && _game.RaceSession.GameMode is { CountdownSecondsRemaining: <= 0f, IsPaused: false, IsRaceFinished: false })
                {
                    BeginTopSpeedSample(countdownPawn, totalTime);
                }
                break;

            case AuditStep.RunTopSpeedSample:
                UpdateTopSpeedSample(totalTime);
                break;

            case AuditStep.ReturnToFrontEnd:
                if (CanAdvance(totalTime))
                {
                    ClearForcedInput();
                    _flow.ReturnToFrontEndForAutomation();
                    _carIndex++;
                    Advance(AuditStep.WaitForFrontEnd, totalTime, "Returning to front-end for next top-speed scenario");
                }
                break;
        }
    }

    private void BeginTopSpeedSample(RacingCarPawn pawn, TimeSpan totalTime)
    {
        _sampleStartSpeedMph = pawn.CurrentSpeedMph;
        _sampleEndSpeedMph = pawn.CurrentSpeedMph;
        _peakSpeedMph = pawn.CurrentSpeedMph;
        _peakGear = pawn.CurrentGear;
        _stableDurationSeconds = 0f;
        _lastStabilitySampleSpeedMph = pawn.CurrentSpeedMph;
        _lastStabilitySampleAt = totalTime;
        _converged = false;
        ApplyForcedInput(pawn, 1f, 0f);
        Advance(AuditStep.RunTopSpeedSample, totalTime, "Running long top-speed sample");
    }

    private void UpdateTopSpeedSample(TimeSpan totalTime)
    {
        if (!TryGetActivePawn(out RacingCarPawn pawn))
        {
            Fail("Car top-speed audit lost the player pawn during the sample.");
            return;
        }

        float steering = ComputeTrackFollowingSteering(pawn);
        ApplyForcedInput(pawn, 1f, steering);
        _sampleEndSpeedMph = pawn.CurrentSpeedMph;
        _peakSpeedMph = Math.Max(_peakSpeedMph, pawn.CurrentSpeedMph);
        _peakGear = Math.Max(_peakGear, pawn.CurrentGear);

        float sampleElapsedSeconds = (float)(totalTime - _lastTransitionAt).TotalSeconds;
        if (totalTime - _lastStabilitySampleAt >= TimeSpan.FromSeconds(StabilitySampleIntervalSeconds))
        {
            float speedDelta = Math.Abs(pawn.CurrentSpeedMph - _lastStabilitySampleSpeedMph);
            bool nearPeakSpeed = pawn.CurrentSpeedMph >= _peakSpeedMph - 1.0f;
            if (sampleElapsedSeconds >= MinimumSampleDurationSeconds
                && speedDelta <= StabilitySpeedDeltaThresholdMph
                && nearPeakSpeed)
            {
                _stableDurationSeconds += (float)(totalTime - _lastStabilitySampleAt).TotalSeconds;
            }
            else
            {
                _stableDurationSeconds = 0f;
            }

            _lastStabilitySampleSpeedMph = pawn.CurrentSpeedMph;
            _lastStabilitySampleAt = totalTime;
        }

        if (sampleElapsedSeconds >= MinimumSampleDurationSeconds
            && _stableDurationSeconds >= RequiredStableDurationSeconds)
        {
            _converged = true;
            CaptureCurrentResult(sampleElapsedSeconds);
            Advance(AuditStep.ReturnToFrontEnd, totalTime, "Captured converged top-speed result");
            return;
        }

        if (sampleElapsedSeconds >= MaximumSampleDurationSeconds)
        {
            CaptureCurrentResult(sampleElapsedSeconds);
            Advance(AuditStep.ReturnToFrontEnd, totalTime, "Captured bounded top-speed result at max duration");
        }
    }

    private void CaptureCurrentResult(float sampleDurationSeconds)
    {
        if (!TryGetActivePawn(out RacingCarPawn pawn))
        {
            Fail("Car top-speed audit could not read the final player pawn state.");
            return;
        }

        CarPerformanceProfile profile = pawn.CarProfile ?? RaceFrontEndCatalog.ResolveCarProfile(pawn.SelectedCarIndex);
        string detailFilePath = Path.Combine(_detailDirectoryPath, $"{profile.Id}-simulation-top-speed.txt");
        Directory.CreateDirectory(_detailDirectoryPath);
        File.WriteAllText(detailFilePath, _game.RaceSession.BuildMovementDebugReport());

        _results.Add(new CarTopSpeedAuditResult(
            profile.Name,
            profile.Id,
            profile.Simulation.ChassisMass,
            profile.TargetTopSpeedMph,
            _sampleStartSpeedMph,
            _sampleEndSpeedMph,
            _peakSpeedMph,
            _peakGear,
            sampleDurationSeconds,
            _converged,
            BuildTransmissionSummary(profile),
            Path.GetRelativePath(Path.GetDirectoryName(_outputFilePath)!, detailFilePath)));

        ClearForcedInput();
    }

    private float ComputeTrackFollowingSteering(RacingCarPawn pawn)
    {
        if (!TryGetTrackPhysics(out RaceTrackPhysicsComponent trackPhysics)
            || pawn.RootComponent == null
            || !trackPhysics.TrySampleSurface(pawn.RootComponent.Position, 0, out RaceTrackSurfaceSample surfaceSample))
        {
            return 0f;
        }

        Vector3 currentForward = FlattenAndNormalize(pawn.GetMovementForward(), surfaceSample.Forward);
        Vector3 targetForward = FlattenAndNormalize(surfaceSample.Forward, currentForward);
        float headingErrorDegrees = ComputeSignedHeadingDeltaDegrees(currentForward, targetForward);
        float normalizedLateralOffset = surfaceSample.HalfWidth <= 0.0001f
            ? 0f
            : Math.Clamp(surfaceSample.LateralOffset / surfaceSample.HalfWidth, -1f, 1f);

        float steering = (headingErrorDegrees / 26f) - (normalizedLateralOffset * 0.60f);
        return Math.Clamp(steering, -1f, 1f);
    }

    private bool TryGetTrackPhysics(out RaceTrackPhysicsComponent trackPhysics)
    {
        trackPhysics = null!;

        if (_game.GameManager.CurrentWorld is not { } raceWorld
            || !RaceWorldFactory.IsRaceWorld(raceWorld))
        {
            return false;
        }

        foreach (Entity entity in raceWorld.Entities)
        {
            RaceTrackPhysicsComponent? component = entity.GetComponent<RaceTrackPhysicsComponent>();
            if (component == null)
            {
                continue;
            }

            trackPhysics = component;
            return true;
        }

        return false;
    }

    private bool TryGetActivePawn(out RacingCarPawn pawn)
    {
        pawn = null!;

        if (!_game.RaceSession.IsActive
            || _game.RaceSession.PlayerPawn == null
            || _game.GameManager.CurrentWorld is not { } raceWorld
            || !RaceWorldFactory.IsRaceWorld(raceWorld))
        {
            return false;
        }

        pawn = _game.RaceSession.PlayerPawn;
        return pawn.RootComponent != null;
    }

    private void ApplyForcedInput(RacingCarPawn pawn, float throttle, float steering)
    {
        pawn.ForcedControlInput = new VehicleControlInput(throttle, steering);
    }

    private void ClearForcedInput()
    {
        if (_game.RaceSession.PlayerPawn != null)
        {
            _game.RaceSession.PlayerPawn.ForcedControlInput = null;
        }
    }

    private bool CanAdvance(TimeSpan totalTime)
    {
        return totalTime - _lastTransitionAt >= TimeSpan.FromMilliseconds(250);
    }

    private void Advance(AuditStep nextStep, TimeSpan totalTime, string message)
    {
        Logs.WriteInfo($"Car top-speed audit: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
    }

    private void Complete()
    {
        string outputDirectory = Path.GetDirectoryName(_outputFilePath)!;
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(_outputFilePath, BuildReport());
        Logs.WriteInfo($"Car top-speed audit completed successfully ({_results.Count} scenario(s)): {_outputFilePath}");
        Environment.ExitCode = 0;
        _step = AuditStep.Completed;
        _game.Exit();
    }

    private void Fail(string message)
    {
        ClearForcedInput();
        Logs.WriteError(message);
        Environment.ExitCode = 1;
        _step = AuditStep.Failed;
        _game.Exit();
    }

    private string BuildReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# RacingGameCasaEngine car top-speed audit");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine($"Track: {RaceFrontEndCatalog.Tracks[_trackIndex].Name}");
        builder.AppendLine($"Driving mode: {VehicleDrivingMode.Simulation}");
        builder.AppendLine($"Throttle: 1.00");
        builder.AppendLine($"Track following: enabled");
        builder.AppendLine($"Sample duration: min={MinimumSampleDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s max={MaximumSampleDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s");
        builder.AppendLine($"Convergence: interval={StabilitySampleIntervalSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s delta<={StabilitySpeedDeltaThresholdMph.ToString("0.00", CultureInfo.InvariantCulture)} mph stable={RequiredStableDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s");
        builder.AppendLine();
        builder.AppendLine("| Car | Profile | Mass kg | Target mph | Start mph | End mph | Peak mph | Peak gear | Duration s | Converged | Transmission | Detail report | ");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- | --- |");

        foreach (CarTopSpeedAuditResult result in _results)
        {
            builder.AppendLine(
                $"| {result.CarName} | {result.ProfileId} | {FormatNumber(result.MassKilograms)} | {FormatNumber(result.TargetTopSpeedMph)} | {FormatNumber(result.StartSpeedMph)} | {FormatNumber(result.EndSpeedMph)} | {FormatNumber(result.PeakSpeedMph)} | {result.PeakGear.ToString(CultureInfo.InvariantCulture)} | {FormatNumber(result.SampleDurationSeconds)} | {(result.Converged ? "Yes" : "No")} | {result.TransmissionSummary} | {result.DetailReportPath.Replace('\\', '/')} |");
        }

        return builder.ToString();
    }

    private static string BuildTransmissionSummary(CarPerformanceProfile profile)
    {
        var builder = new StringBuilder();
        for (int index = 0; index < profile.TransmissionDefinition.ForwardGearRatios.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('/');
            }

            builder.Append(profile.TransmissionDefinition.ForwardGearRatios[index].ToString("0.00", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static float FormatSteeringFallback(float value)
    {
        return value;
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static Vector3 FlattenAndNormalize(Vector3 vector, Vector3 fallback)
    {
        Vector3 flattened = new(vector.X, 0f, vector.Z);
        if (flattened.LengthSquared() <= 0.0001f)
        {
            Vector3 flattenedFallback = new(fallback.X, 0f, fallback.Z);
            if (flattenedFallback.LengthSquared() <= 0.0001f)
            {
                return Vector3.Forward;
            }

            flattenedFallback.Normalize();
            return flattenedFallback;
        }

        flattened.Normalize();
        return flattened;
    }

    private static float ComputeSignedHeadingDeltaDegrees(Vector3 referenceForward, Vector3 currentForward)
    {
        float clampedDot = Math.Clamp(Vector3.Dot(referenceForward, currentForward), -1f, 1f);
        float angleRadians = MathF.Acos(clampedDot);
        float crossY = Vector3.Cross(referenceForward, currentForward).Y;
        float signedAngleRadians = crossY >= 0f ? angleRadians : -angleRadians;
        return MathHelper.ToDegrees(signedAngleRadians);
    }

    private static int ResolveTrackIndex(string? trackName)
    {
        if (!string.IsNullOrWhiteSpace(trackName))
        {
            for (int index = 0; index < RaceFrontEndCatalog.Tracks.Count; index++)
            {
                if (string.Equals(RaceFrontEndCatalog.Tracks[index].Name, trackName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }
        }

        return Math.Max(0, RaceFrontEndCatalog.Tracks.Count - 1);
    }

    private static string ResolveOutputFilePath(string? outputFilePath)
    {
        if (!string.IsNullOrWhiteSpace(outputFilePath))
        {
            return Path.GetFullPath(outputFilePath);
        }

        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "artifacts",
            "car-top-speed-audit",
            "racinggame-casaengine-car-top-speed-audit.md"));
    }

    private readonly record struct CarTopSpeedAuditResult(
        string CarName,
        string ProfileId,
        float MassKilograms,
        float TargetTopSpeedMph,
        float StartSpeedMph,
        float EndSpeedMph,
        float PeakSpeedMph,
        int PeakGear,
        float SampleDurationSeconds,
        bool Converged,
        string TransmissionSummary,
        string DetailReportPath);
}
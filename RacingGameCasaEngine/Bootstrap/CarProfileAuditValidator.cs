using System.Globalization;
using System.Text;
using CasaEngine.Core.Log;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;
using RacingGameCasaEngine.Entities;
using RacingGameCasaEngine.Gameplay;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.Bootstrap;

internal sealed class CarProfileAuditValidator
{
    private enum AuditStep
    {
        WaitForFrontEnd,
        StartRace,
        WaitForRace,
        WaitForCountdown,
        RunStraightSample,
        RunTurnSample,
        ReturnToFrontEnd,
        Completed,
        Failed,
    }

    private static readonly VehicleDrivingMode[] Modes =
    [
        VehicleDrivingMode.Arcade,
        VehicleDrivingMode.Simulation,
    ];

    private const float StraightSampleDurationSeconds = 3.0f;
    private const float TurnSampleDurationSeconds = 1.75f;
    private const float TurnSampleThrottle = 0.65f;
    private const float TurnSampleSteering = 0.45f;

    private readonly RacingGameCasaEngineGame _game;
    private readonly RaceFrontEndFlow _flow;
    private readonly string _outputFilePath;
    private readonly List<CarProfileAuditResult> _results = [];
    private AuditStep _step = AuditStep.WaitForFrontEnd;
    private TimeSpan _startedAt;
    private TimeSpan _lastTransitionAt;
    private bool _started;
    private int _modeIndex;
    private int _carIndex;
    private float _straightStartSpeedMph;
    private float _straightEndSpeedMph;
    private float _straightPeakSpeedMph;
    private int _straightPeakGear;
    private float _turnPeakSpeedMph;
    private float _turnHeadingDeltaDegrees;
    private float _turnLateralDisplacementUnits;
    private Vector3 _phaseStartPosition;
    private Vector3 _phaseStartForward = Vector3.Forward;
    private Vector3 _phaseStartRight = Vector3.Right;

    public CarProfileAuditValidator(RacingGameCasaEngineGame game, RaceFrontEndFlow flow, string? outputFilePath)
    {
        _game = game;
        _flow = flow;
        _outputFilePath = ResolveOutputFilePath(outputFilePath);
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
            Logs.WriteInfo($"Car profile audit started: {_outputFilePath}");
        }

        if (_step is AuditStep.Completed or AuditStep.Failed)
        {
            return;
        }

        if (totalTime - _startedAt > TimeSpan.FromSeconds(120))
        {
            Fail("Car profile audit timed out.");
            return;
        }

        switch (_step)
        {
            case AuditStep.WaitForFrontEnd:
                if (CanAdvance(totalTime)
                    && _game.GameManager.CurrentWorld?.Name == RaceWorldFactory.FrontEndWorldName)
                {
                    Advance(AuditStep.StartRace, totalTime, "Front-end ready for car profile audit");
                }
                break;

            case AuditStep.StartRace:
                if (CanAdvance(totalTime))
                {
                    if (_modeIndex >= Modes.Length)
                    {
                        Complete();
                        return;
                    }

                    _flow.State.SelectedCarIndex = _carIndex;
                    _flow.State.SelectedTrackIndex = 0;
                    _flow.State.SelectedDrivingMode = Modes[_modeIndex];
                    _flow.StartRaceForAutomation();
                    Advance(
                        AuditStep.WaitForRace,
                        totalTime,
                        $"Loading car audit scenario mode={Modes[_modeIndex]} car={RaceFrontEndCatalog.Cars[_carIndex].Name}");
                }
                break;

            case AuditStep.WaitForRace:
                if (CanAdvance(totalTime)
                    && _game.GameManager.CurrentWorld is { } raceWorld
                    && RaceWorldFactory.IsRaceWorld(raceWorld)
                    && _game.RaceSession.IsActive
                    && _game.GameManager.ScreenManager.CurrentState == RaceFrontEndFlow.RaceHudStateName)
                {
                    Advance(AuditStep.WaitForCountdown, totalTime, "Race world ready for car profile audit");
                }
                break;

            case AuditStep.WaitForCountdown:
                if (CanAdvance(totalTime)
                    && TryGetActivePawn(out RacingCarPawn countdownPawn)
                    && _game.RaceSession.GameMode is { CountdownSecondsRemaining: <= 0f, IsPaused: false, IsRaceFinished: false })
                {
                    BeginStraightSample(countdownPawn, totalTime);
                }
                break;

            case AuditStep.RunStraightSample:
                UpdateStraightSample(totalTime);
                break;

            case AuditStep.RunTurnSample:
                UpdateTurnSample(totalTime);
                break;

            case AuditStep.ReturnToFrontEnd:
                if (CanAdvance(totalTime))
                {
                    ClearForcedInput();
                    _flow.ReturnToFrontEndForAutomation();
                    Advance(AuditStep.WaitForFrontEnd, totalTime, "Returning to front-end for next car profile audit scenario");
                    MoveToNextScenario();
                }
                break;
        }
    }

    private void BeginStraightSample(RacingCarPawn pawn, TimeSpan totalTime)
    {
        _straightStartSpeedMph = pawn.CurrentSpeedMph;
        _straightEndSpeedMph = pawn.CurrentSpeedMph;
        _straightPeakSpeedMph = pawn.CurrentSpeedMph;
        _straightPeakGear = pawn.CurrentGear;
        ApplyForcedInput(pawn, 1f, 0f);
        Advance(AuditStep.RunStraightSample, totalTime, "Running straight-line acceleration sample");
    }

    private void UpdateStraightSample(TimeSpan totalTime)
    {
        if (!TryGetActivePawn(out RacingCarPawn pawn))
        {
            Fail("Car profile audit lost the player pawn during the straight sample.");
            return;
        }

        ApplyForcedInput(pawn, 1f, 0f);
        _straightEndSpeedMph = pawn.CurrentSpeedMph;
        _straightPeakSpeedMph = Math.Max(_straightPeakSpeedMph, pawn.CurrentSpeedMph);
        _straightPeakGear = Math.Max(_straightPeakGear, pawn.CurrentGear);

        if ((float)(totalTime - _lastTransitionAt).TotalSeconds >= StraightSampleDurationSeconds)
        {
            BeginTurnSample(pawn, totalTime);
        }
    }

    private void BeginTurnSample(RacingCarPawn pawn, TimeSpan totalTime)
    {
        _turnPeakSpeedMph = pawn.CurrentSpeedMph;
        _turnHeadingDeltaDegrees = 0f;
        _turnLateralDisplacementUnits = 0f;
        _phaseStartPosition = pawn.RootComponent?.Position ?? Vector3.Zero;
        _phaseStartForward = FlattenAndNormalize(pawn.GetMovementForward(), Vector3.Forward);
        _phaseStartRight = FlattenAndNormalize(Vector3.Cross(Vector3.Up, _phaseStartForward), Vector3.Right);
        ApplyForcedInput(pawn, TurnSampleThrottle, TurnSampleSteering);
        Advance(AuditStep.RunTurnSample, totalTime, "Running turn-response sample");
    }

    private void UpdateTurnSample(TimeSpan totalTime)
    {
        if (!TryGetActivePawn(out RacingCarPawn pawn))
        {
            Fail("Car profile audit lost the player pawn during the turn sample.");
            return;
        }

        ApplyForcedInput(pawn, TurnSampleThrottle, TurnSampleSteering);
        _turnPeakSpeedMph = Math.Max(_turnPeakSpeedMph, pawn.CurrentSpeedMph);

        Vector3 currentForward = FlattenAndNormalize(pawn.GetMovementForward(), _phaseStartForward);
        float headingDeltaDegrees = ComputeSignedHeadingDeltaDegrees(_phaseStartForward, currentForward);
        if (Math.Abs(headingDeltaDegrees) > Math.Abs(_turnHeadingDeltaDegrees))
        {
            _turnHeadingDeltaDegrees = headingDeltaDegrees;
        }

        Vector3 currentPosition = pawn.RootComponent?.Position ?? _phaseStartPosition;
        float lateralDisplacement = Vector3.Dot(currentPosition - _phaseStartPosition, _phaseStartRight);
        if (Math.Abs(lateralDisplacement) > Math.Abs(_turnLateralDisplacementUnits))
        {
            _turnLateralDisplacementUnits = lateralDisplacement;
        }

        if ((float)(totalTime - _lastTransitionAt).TotalSeconds >= TurnSampleDurationSeconds)
        {
            CaptureCurrentResult();
            Advance(AuditStep.ReturnToFrontEnd, totalTime, "Captured current car profile audit result");
        }
    }

    private void CaptureCurrentResult()
    {
        if (!TryGetActivePawn(out RacingCarPawn pawn))
        {
            Fail("Car profile audit could not read the final player pawn state.");
            return;
        }

        CarPerformanceProfile profile = pawn.CarProfile ?? RaceFrontEndCatalog.ResolveCarProfile(pawn.SelectedCarIndex);
        float averageAccelerationMphPerSecond = (_straightEndSpeedMph - _straightStartSpeedMph) / StraightSampleDurationSeconds;
        _results.Add(new CarProfileAuditResult(
            Modes[_modeIndex],
            profile.Name,
            profile.Id,
            profile.Simulation.ChassisMass,
            profile.TargetTopSpeedMph,
            _straightEndSpeedMph,
            _straightPeakSpeedMph,
            _straightPeakGear,
            averageAccelerationMphPerSecond,
            _turnPeakSpeedMph,
            _turnHeadingDeltaDegrees,
            _turnLateralDisplacementUnits,
            BuildTransmissionSummary(profile)));
        ClearForcedInput();
    }

    private void MoveToNextScenario()
    {
        _carIndex++;
        if (_carIndex < RaceFrontEndCatalog.Cars.Count)
        {
            return;
        }

        _carIndex = 0;
        _modeIndex++;
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
        Logs.WriteInfo($"Car profile audit: {message}");
        _step = nextStep;
        _lastTransitionAt = totalTime;
    }

    private void Complete()
    {
        string outputDirectory = Path.GetDirectoryName(_outputFilePath)!;
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(_outputFilePath, BuildReport());
        Logs.WriteInfo($"Car profile audit completed successfully ({_results.Count} scenario(s)): {_outputFilePath}");
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
        builder.AppendLine("# RacingGameCasaEngine car profile audit");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine($"Track: {RaceFrontEndCatalog.Tracks[0].Name}");
        builder.AppendLine($"Straight sample: throttle=1.00 steering=0.00 duration={StraightSampleDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s");
        builder.AppendLine($"Turn sample: throttle={TurnSampleThrottle.ToString("0.00", CultureInfo.InvariantCulture)} steering={TurnSampleSteering.ToString("0.00", CultureInfo.InvariantCulture)} duration={TurnSampleDurationSeconds.ToString("0.00", CultureInfo.InvariantCulture)}s");
        builder.AppendLine();

        for (int modeIndex = 0; modeIndex < Modes.Length; modeIndex++)
        {
            VehicleDrivingMode mode = Modes[modeIndex];
            builder.AppendLine($"## {mode}");
            builder.AppendLine();
            builder.AppendLine("| Car | Profile | Mass kg | Target mph | Straight end mph | Straight peak mph | Peak gear | Avg accel mph/s | Turn peak mph | Turn heading deg | Turn lateral | Transmission |");
            builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |");

            for (int resultIndex = 0; resultIndex < _results.Count; resultIndex++)
            {
                CarProfileAuditResult result = _results[resultIndex];
                if (result.DrivingMode != mode)
                {
                    continue;
                }

                builder.AppendLine(
                    $"| {result.CarName} | {result.ProfileId} | {FormatNumber(result.MassKilograms)} | {FormatNumber(result.TargetTopSpeedMph)} | {FormatNumber(result.StraightEndSpeedMph)} | {FormatNumber(result.StraightPeakSpeedMph)} | {result.StraightPeakGear.ToString(CultureInfo.InvariantCulture)} | {FormatNumber(result.StraightAverageAccelerationMphPerSecond)} | {FormatNumber(result.TurnPeakSpeedMph)} | {FormatNumber(result.TurnHeadingDeltaDegrees)} | {FormatNumber(result.TurnLateralDisplacementUnits)} | {result.TransmissionSummary} |");
            }

            builder.AppendLine();
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
            "car-profile-audit",
            "racinggame-casaengine-car-profile-audit.md"));
    }

    private readonly record struct CarProfileAuditResult(
        VehicleDrivingMode DrivingMode,
        string CarName,
        string ProfileId,
        float MassKilograms,
        float TargetTopSpeedMph,
        float StraightEndSpeedMph,
        float StraightPeakSpeedMph,
        int StraightPeakGear,
        float StraightAverageAccelerationMphPerSecond,
        float TurnPeakSpeedMph,
        float TurnHeadingDeltaDegrees,
        float TurnLateralDisplacementUnits,
        string TransmissionSummary);
}
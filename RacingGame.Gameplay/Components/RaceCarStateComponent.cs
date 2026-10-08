using RacingGame.Gameplay.Cars;

namespace RacingGame.Gameplay.Components;

/// <summary>Race state of a car entity: labels, selection, driving mode and the telemetry the HUD reads.</summary>
public sealed class RaceCarStateComponent : EntityComponent
{
    public string CarLabel { get; set; } = "Prototype Car";

    public string TrackLabel { get; set; } = "Prototype Track";

    public int SelectedCarIndex { get; set; }

    public int SelectedCarColorIndex { get; set; }

    internal CarPerformanceProfile? CarProfile { get; set; }

    internal VehicleControlInput? ForcedControlInput { get; set; }

    internal VehicleDrivingMode DrivingMode { get; set; } = VehicleDrivingMode.Arcade;

    public float TargetTopSpeedMph { get; set; } = 170.0f;

    public float CurrentSpeedMph { get; set; }

    public float SteeringInput { get; set; }

    public float TachometerAcceleration { get; set; }

    public int CurrentGear { get; set; } = 1;

    public override EntityComponent Clone()
    {
        return new RaceCarStateComponent
        {
            CarLabel = CarLabel,
            TrackLabel = TrackLabel,
            SelectedCarIndex = SelectedCarIndex,
            SelectedCarColorIndex = SelectedCarColorIndex,
            CarProfile = CarProfile,
            ForcedControlInput = ForcedControlInput,
            DrivingMode = DrivingMode,
            TargetTopSpeedMph = TargetTopSpeedMph,
            CurrentSpeedMph = CurrentSpeedMph,
            SteeringInput = SteeringInput,
            TachometerAcceleration = TachometerAcceleration,
            CurrentGear = CurrentGear,
        };
    }
}

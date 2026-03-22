using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using RacingGameCasaEngine.Components;

namespace RacingGameCasaEngine.Entities;

public sealed class RacingCarPawn : Pawn
{
    public string CarLabel { get; set; } = "Prototype Car";

    public string TrackLabel { get; set; } = "Prototype Track";

    public float TargetTopSpeedMph { get; set; } = 170.0f;

    public float CurrentSpeedMph { get; set; }

    public float SteeringInput { get; set; }

    public RacingCarPawn()
    {
        Name = "RacingCarPawn";
        RootComponent = new PlayerStartComponent();
        AddComponent(new ArcadeCarMovementComponent());
        AddComponent(new DebugCarVisualComponent());
    }
}
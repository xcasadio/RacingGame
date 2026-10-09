using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Components;

namespace RacingGame.Gameplay.Entities;

/// <summary>Builds the player car as an ordinary entity: an anchor hierarchy, a body visual and the car components.</summary>
public static class RaceCarEntityFactory
{
    public const string PhysicsRootAnchorId = "PhysicsRoot";

    public const string VisualPivotAnchorId = "VisualPivot";

    public const string BodyAnchorId = "Body";

    public const string ChaseCameraAnchorId = "ChaseCamera";

    public const string CockpitCameraAnchorId = "CockpitCamera";

    public const string AudioEmitterAnchorId = "AudioEmitter";

    public const string CarBodyVisualComponentName = "BodyVisual";

    public const float CollisionLength = 5.6f;

    public const float CollisionWidth = 2.6f;

    public const float CollisionHeight = 1.8f;

    public static Entity Create()
    {
        var car = new Entity
        {
            Name = "RaceCar",
        };
        car.ApplyExplicitPolicies(EntityPolicySet.DynamicDefault);
        car.RootComponent = CreateComponentHierarchy();
        EnsureVisualComponent(car);
        car.AddComponent(new VehicleDynamicsComponent { Name = nameof(VehicleDynamicsComponent) });
        car.AddComponent(new LegacyCarVisualComponent { Name = nameof(LegacyCarVisualComponent) });
        car.AddComponent(new VehicleWheelVisualComponent { Name = nameof(VehicleWheelVisualComponent) });
        car.AddComponent(new DebugCarVisualComponent { Name = nameof(DebugCarVisualComponent) });
        car.AddComponent(new RaceCarStateComponent { Name = nameof(RaceCarStateComponent) });
        return car;
    }

    private static SceneComponent CreateComponentHierarchy()
    {
        var physicsRoot = CreateAnchor(PhysicsRootAnchorId, Vector3.Zero);
        var visualPivot = CreateAnchor(VisualPivotAnchorId, new Vector3(0f, 0.45f, 0f));
        var bodyAnchor = CreateAnchor(BodyAnchorId, Vector3.Zero);
        var chaseCameraAnchor = CreateAnchor(ChaseCameraAnchorId, new Vector3(0f, 0.45f, 0.35f));
        var cockpitCameraAnchor = CreateAnchor(CockpitCameraAnchorId, new Vector3(0f, 0.32f, 0.85f));
        var audioEmitterAnchor = CreateAnchor(AudioEmitterAnchorId, new Vector3(0f, 0.15f, -0.95f));

        visualPivot.AddChildComponent(bodyAnchor);
        visualPivot.AddChildComponent(chaseCameraAnchor);
        visualPivot.AddChildComponent(cockpitCameraAnchor);
        visualPivot.AddChildComponent(audioEmitterAnchor);
        physicsRoot.AddChildComponent(visualPivot);

        return physicsRoot;
    }

    private static RacingCarAnchorComponent CreateAnchor(string anchorId, Vector3 localPosition)
    {
        return new RacingCarAnchorComponent
        {
            Name = anchorId,
            AnchorId = anchorId,
            LocalPosition = localPosition,
        };
    }

    private static void EnsureVisualComponent(Entity car)
    {
        SceneComponent? visualParent = car.BodyAnchorComponent() ?? car.VisualPivotComponent() ?? car.RootComponent;
        if (visualParent == null || RaceCarEntityExtensions.FindSceneComponent<StaticModelComponent>(visualParent) != null)
        {
            return;
        }

        visualParent.AddChildComponent(new StaticModelComponent
        {
            Name = CarBodyVisualComponentName,
        });
    }
}

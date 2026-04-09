using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.GameFramework;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Components;

namespace RacingGameCasaEngine.Entities;

public sealed class RacingCarPawn : Pawn
{
    public const string PhysicsRootAnchorId = "PhysicsRoot";

    public const string VisualPivotAnchorId = "VisualPivot";

    public const string BodyAnchorId = "Body";

    public const string ChaseCameraAnchorId = "ChaseCamera";

    public const string CockpitCameraAnchorId = "CockpitCamera";

    public const string AudioEmitterAnchorId = "AudioEmitter";

    public const string CarBodyVisualComponentName = "BodyVisual";

    public string CarLabel { get; set; } = "Prototype Car";

    public string TrackLabel { get; set; } = "Prototype Track";

    public int SelectedCarIndex { get; set; }

    public int SelectedCarColorIndex { get; set; }

    public float TargetTopSpeedMph { get; set; } = 170.0f;

    public float CurrentSpeedMph { get; set; }

    public float SteeringInput { get; set; }

    public SceneComponent? PhysicalRootComponent => RootComponent;

    public RacingCarAnchorComponent? VisualPivotComponent => FindAnchorComponent(VisualPivotAnchorId);

    public RacingCarAnchorComponent? BodyAnchorComponent => FindAnchorComponent(BodyAnchorId);

    public RacingCarAnchorComponent? ChaseCameraAnchorComponent => FindAnchorComponent(ChaseCameraAnchorId);

    public RacingCarAnchorComponent? CockpitCameraAnchorComponent => FindAnchorComponent(CockpitCameraAnchorId);

    public RacingCarAnchorComponent? AudioEmitterAnchorComponent => FindAnchorComponent(AudioEmitterAnchorId);

    public StaticModelComponent? CarVisualComponent => FindSceneComponent<StaticModelComponent>(BodyAnchorComponent ?? VisualPivotComponent ?? PhysicalRootComponent);

    public RacingCarPawn()
    {
        Name = "RacingCarPawn";
        RootComponent = CreateComponentHierarchy();
        EnsureVisualComponent();
        AddComponent(new ArcadeCarMovementComponent());
        AddComponent(new LegacyCarVisualComponent());
        AddComponent(new DebugCarVisualComponent());
    }

    public Matrix GetBodyWorldMatrixNoScale()
    {
        return GetWorldMatrix(BodyAnchorComponent ?? VisualPivotComponent ?? PhysicalRootComponent);
    }

    public Vector3 GetBodyWorldPosition()
    {
        return GetWorldPosition(BodyAnchorComponent ?? VisualPivotComponent ?? PhysicalRootComponent);
    }

    public Vector3 GetChaseCameraFocusPosition()
    {
        return GetWorldPosition(ChaseCameraAnchorComponent ?? BodyAnchorComponent ?? PhysicalRootComponent);
    }

    public Vector3 GetAudioEmitterWorldPosition()
    {
        return GetWorldPosition(AudioEmitterAnchorComponent ?? BodyAnchorComponent ?? PhysicalRootComponent);
    }

    public Vector3 GetMovementForward()
    {
        return GetWorldForward(PhysicalRootComponent);
    }

    public Vector3 GetVisualForward()
    {
        return GetWorldForward(VisualPivotComponent ?? PhysicalRootComponent);
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

    private void EnsureVisualComponent()
    {
        SceneComponent? visualParent = BodyAnchorComponent ?? VisualPivotComponent ?? PhysicalRootComponent;
        if (visualParent == null || FindSceneComponent<StaticModelComponent>(visualParent) != null)
        {
            return;
        }

        visualParent.AddChildComponent(new StaticModelComponent
        {
            Name = CarBodyVisualComponentName,
        });
    }

    private RacingCarAnchorComponent? FindAnchorComponent(string anchorId)
    {
        return FindAnchorComponent(RootComponent, anchorId);
    }

    private static T? FindSceneComponent<T>(SceneComponent? component) where T : class
    {
        if (component == null)
        {
            return null;
        }

        if (component is T typedComponent)
        {
            return typedComponent;
        }

        for (int index = 0; index < component.Children.Count; index++)
        {
            T? childComponent = FindSceneComponent<T>(component.Children[index]);
            if (childComponent != null)
            {
                return childComponent;
            }
        }

        return null;
    }

    private static RacingCarAnchorComponent? FindAnchorComponent(SceneComponent? component, string anchorId)
    {
        if (component == null)
        {
            return null;
        }

        if (component is RacingCarAnchorComponent anchor
            && string.Equals(anchor.AnchorId, anchorId, StringComparison.Ordinal))
        {
            return anchor;
        }

        for (int index = 0; index < component.Children.Count; index++)
        {
            RacingCarAnchorComponent? childAnchor = FindAnchorComponent(component.Children[index], anchorId);
            if (childAnchor != null)
            {
                return childAnchor;
            }
        }

        return null;
    }

    private static Matrix GetWorldMatrix(SceneComponent? component)
    {
        return component?.WorldMatrixNoScale ?? Matrix.Identity;
    }

    private static Vector3 GetWorldPosition(SceneComponent? component)
    {
        return component == null
            ? Vector3.Zero
            : Vector3.Transform(Vector3.Zero, component.WorldMatrixNoScale);
    }

    private static Vector3 GetWorldForward(SceneComponent? component)
    {
        if (component == null)
        {
            return Vector3.Forward;
        }

        Vector3 forward = Vector3.TransformNormal(Vector3.Forward, component.WorldMatrixNoScale);
        if (forward.LengthSquared() < 0.0001f)
        {
            return Vector3.Forward;
        }

        return Vector3.Normalize(forward);
    }
}
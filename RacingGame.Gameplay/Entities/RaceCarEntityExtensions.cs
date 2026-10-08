using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Components;

namespace RacingGame.Gameplay.Entities;

/// <summary>Anchor lookups and car-space queries of an entity built by <see cref="RaceCarEntityFactory"/>.</summary>
public static class RaceCarEntityExtensions
{
    public static RacingCarAnchorComponent? VisualPivotComponent(this Entity car)
    {
        return FindAnchorComponent(car.RootComponent, RaceCarEntityFactory.VisualPivotAnchorId);
    }

    public static RacingCarAnchorComponent? BodyAnchorComponent(this Entity car)
    {
        return FindAnchorComponent(car.RootComponent, RaceCarEntityFactory.BodyAnchorId);
    }

    public static RacingCarAnchorComponent? ChaseCameraAnchorComponent(this Entity car)
    {
        return FindAnchorComponent(car.RootComponent, RaceCarEntityFactory.ChaseCameraAnchorId);
    }

    public static RacingCarAnchorComponent? CockpitCameraAnchorComponent(this Entity car)
    {
        return FindAnchorComponent(car.RootComponent, RaceCarEntityFactory.CockpitCameraAnchorId);
    }

    public static RacingCarAnchorComponent? AudioEmitterAnchorComponent(this Entity car)
    {
        return FindAnchorComponent(car.RootComponent, RaceCarEntityFactory.AudioEmitterAnchorId);
    }

    public static StaticModelComponent? CarVisualComponent(this Entity car)
    {
        return FindSceneComponent<StaticModelComponent>(car.BodyAnchorComponent() ?? car.VisualPivotComponent() ?? car.RootComponent);
    }

    internal static PlayerController? Controller(this Entity car)
    {
        return car.World?.GetPlayerController(car);
    }

    public static Matrix GetBodyWorldMatrixNoScale(this Entity car)
    {
        return GetWorldMatrix(car.BodyAnchorComponent() ?? car.VisualPivotComponent() ?? car.RootComponent);
    }

    public static Vector3 GetBodyWorldPosition(this Entity car)
    {
        return GetWorldPosition(car.BodyAnchorComponent() ?? car.VisualPivotComponent() ?? car.RootComponent);
    }

    public static Vector3 GetChaseCameraFocusPosition(this Entity car)
    {
        return GetWorldPosition(car.ChaseCameraAnchorComponent() ?? car.BodyAnchorComponent() ?? car.RootComponent);
    }

    public static Vector3 GetAudioEmitterWorldPosition(this Entity car)
    {
        return GetWorldPosition(car.AudioEmitterAnchorComponent() ?? car.BodyAnchorComponent() ?? car.RootComponent);
    }

    public static Vector3 GetMovementForward(this Entity car)
    {
        Vector3 dynamicsForward = car.GetComponent<VehicleDynamicsComponent>()?.Telemetry.MovementForward ?? Vector3.Zero;
        if (dynamicsForward.LengthSquared() > 0.0001f)
        {
            return Vector3.Normalize(dynamicsForward);
        }

        return GetWorldForward(car.RootComponent);
    }

    public static Vector3 GetVisualForward(this Entity car)
    {
        return GetWorldForward(car.VisualPivotComponent() ?? car.RootComponent);
    }

    internal static T? FindSceneComponent<T>(SceneComponent? component) where T : class
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

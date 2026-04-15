using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Entities;

namespace RacingGameCasaEngine.Components;

public sealed class VehicleWheelVisualComponent : EntityComponent
{
    private readonly Dictionary<VehicleWheelSlot, WheelVisualBinding> _bindings = new();
    private bool _hasLoggedMissingBindings;

    public override EntityComponent Clone()
    {
        return new VehicleWheelVisualComponent();
    }

    public override void Update(float elapsedTime)
    {
        _ = elapsedTime;

        if (Owner is not RacingCarPawn pawn || pawn.World == null)
        {
            return;
        }

        VehicleDynamicsComponent? dynamics = pawn.VehicleDynamics;
        StaticModelComponent? visualComponent = pawn.CarVisualComponent;
        if (dynamics == null || visualComponent?.StaticModel == null)
        {
            return;
        }

        if (!TryBindWheelFrames(visualComponent, dynamics.WheelDefinitions))
        {
            return;
        }

        float visualScale = Math.Abs(visualComponent.LocalScale.X) > 0.0001f ? Math.Abs(visualComponent.LocalScale.X) : 1f;

        for (int index = 0; index < dynamics.WheelDefinitions.Count; index++)
        {
            VehicleWheelDefinition definition = dynamics.WheelDefinitions[index];
            VehicleWheelRuntimeState state = dynamics.WheelStates[index];
            if (!_bindings.TryGetValue(definition.Slot, out WheelVisualBinding? binding))
            {
                continue;
            }

            float localSuspensionOffset = (definition.SuspensionRestLength - state.SuspensionLength) / visualScale;
            binding.SteeringComponent.LocalPosition = binding.BaseSteeringPosition - (Vector3.Up * localSuspensionOffset);

            Quaternion steeringRotation = definition.CanSteer
                ? Quaternion.CreateFromAxisAngle(Vector3.Up, state.SteeringAngleRadians)
                : Quaternion.Identity;
            binding.SteeringComponent.LocalOrientation = Quaternion.Normalize(binding.BaseSteeringOrientation * steeringRotation);

            Quaternion spinRotation = Quaternion.CreateFromAxisAngle(Vector3.Right, state.RotationAngleRadians * binding.SpinSign);
            binding.SpinComponent.LocalOrientation = Quaternion.Normalize(binding.BaseSpinOrientation * spinRotation);
        }
    }

    private bool TryBindWheelFrames(StaticModelComponent visualComponent, IReadOnlyList<VehicleWheelDefinition> wheelDefinitions)
    {
        bool hasBoundAll = true;
        for (int index = 0; index < wheelDefinitions.Count; index++)
        {
            VehicleWheelDefinition definition = wheelDefinitions[index];
            if (_bindings.ContainsKey(definition.Slot))
            {
                continue;
            }

            SceneComponent? steeringComponent = FindSceneComponentByName(visualComponent, definition.VisualFrameName);
            if (steeringComponent == null)
            {
                hasBoundAll = false;
                continue;
            }

            SceneComponent spinComponent = steeringComponent.Children.Count > 0
                ? steeringComponent.Children[0]
                : steeringComponent;

            _bindings[definition.Slot] = new WheelVisualBinding(
                steeringComponent,
                steeringComponent.LocalPosition,
                steeringComponent.LocalOrientation,
                spinComponent,
                spinComponent.LocalOrientation,
                definition.Slot is VehicleWheelSlot.FrontLeft or VehicleWheelSlot.RearLeft ? -1f : 1f);
        }

        if (!hasBoundAll && !_hasLoggedMissingBindings)
        {
            Logs.WriteWarning("Unable to bind all legacy wheel frames. Wheel visuals will stay on a stable fallback when a frame is missing.");
            _hasLoggedMissingBindings = true;
        }

        return _bindings.Count == wheelDefinitions.Count;
    }

    private static SceneComponent? FindSceneComponentByName(SceneComponent component, string componentName)
    {
        if (string.Equals(component.Name, componentName, StringComparison.Ordinal))
        {
            return component;
        }

        for (int index = 0; index < component.Children.Count; index++)
        {
            SceneComponent? childMatch = FindSceneComponentByName(component.Children[index], componentName);
            if (childMatch != null)
            {
                return childMatch;
            }
        }

        return null;
    }

    private sealed record WheelVisualBinding(
        SceneComponent SteeringComponent,
        Vector3 BaseSteeringPosition,
        Quaternion BaseSteeringOrientation,
        SceneComponent SpinComponent,
        Quaternion BaseSpinOrientation,
        float SpinSign);
}
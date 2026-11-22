using CasaEngine.Framework.Rendering.Shadows;
using Microsoft.Xna.Framework;
using RacingGame.Gameplay.Entities;
using RacingGame.Gameplay.Race;

namespace RacingGame.Gameplay.Components;

/// <summary>
/// Aims the shadow map of the race world as the original does when the world casts its shadow in perspective
/// (<see cref="ShadowProjection.Perspective"/>): at a point ahead of the player's car (RacingGameOriginal
/// Core/Shaders/ShadowMapShader.cs:208-266, the car plus its direction times a sixth of the distance of the virtual light).
/// With the orthographic projection it leaves the map centred on the camera. Nothing is saved with the world.
/// </summary>
public sealed class RaceShadowFocusComponent : EntityComponent
{
    /// <summary>
    /// How far ahead of the car the map is aimed: a sixth of the distance of the original's virtual light (129.25 / 6).
    /// </summary>
    public const float FocusLeadDistance = 129.25f / 6f;

    private Entity? _car;

    /// <summary>
    /// When set, the point the map is aimed at instead of the one computed from the car, in perspective only. Runtime
    /// value, never saved: the capture bench sets it to the point the original aimed at for the view it replays.
    /// </summary>
    public Vector3? FocusOverride { get; set; }

    /// <summary>
    /// The last point computed from the player's car, whether it was the one used or not; <c>null</c> while the projection
    /// is orthographic or the world has no car. Runtime value, read by the capture bench.
    /// </summary>
    public Vector3? ComputedCarFocus { get; private set; }

    public override EntityComponent Clone()
    {
        return new RaceShadowFocusComponent();
    }

    public override void Update(float elapsedTime)
    {
        World? world = Owner?.World;
        if (world == null)
        {
            return;
        }

        ShadowSettings shadows = world.EnvironmentSettings.Shadows;
        if (shadows.Projection != ShadowProjection.Perspective)
        {
            ComputedCarFocus = null;
            shadows.FocusPosition = null;
            return;
        }

        ComputedCarFocus = ComputeCarFocus(world);
        shadows.FocusPosition = FocusOverride ?? ComputedCarFocus;
    }

    private Vector3? ComputeCarFocus(World world)
    {
        if (_car == null || !ReferenceEquals(_car.World, world))
        {
            _car = world.Entities.FirstOrDefault(static entity => entity.Name == RaceWorldNames.PlayerCarEntityName);
        }

        if (_car?.RootComponent == null)
        {
            return null;
        }

        return _car.RootComponent.Position + _car.GetVisualForward() * FocusLeadDistance;
    }
}

using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace RacingGame.Gameplay.Components;

/// <summary>
/// The camera RacingGame was written against: a perspective camera whose view is
/// <c>Matrix.CreateLookAt(Position, Target, Up)</c>, with <see cref="SceneComponent.Up"/> taken from the orientation of the
/// component. It keeps, on the engine's <see cref="PerspectiveCameraComponent"/>, the view formula of the look-at camera
/// the engine removed (engine ADR-0149), so that the game renders the same pictures.
/// </summary>
[DisplayName("Race Look-At Camera")]
public sealed class RaceLookAtCameraComponent : PerspectiveCameraComponent
{
    private Vector3 _target;

    /// <summary>The point the camera looks at, in world coordinates. Not saved: a loaded camera looks at the origin.</summary>
    public Vector3 Target
    {
        get => _target;
        set
        {
            _needToComputeViewMatrix = true;
            _target = value;
        }
    }

    public RaceLookAtCameraComponent()
    {
    }

    public RaceLookAtCameraComponent(RaceLookAtCameraComponent other) : base(other)
    {
        _target = other._target;
    }

    public override RaceLookAtCameraComponent Clone()
    {
        return new RaceLookAtCameraComponent(this);
    }

    //  The view depends on the position, the target and the orientation: it is computed at every read, from their
    //  values of the moment.
    protected override bool IsViewOutOfDate()
    {
        return true;
    }

    protected override void ComputeViewMatrix()
    {
        _viewMatrix = Matrix.CreateLookAt(Position, Target, Up);
    }

    /// <summary>Writes the position and the target. The orientation, which gives the up vector, is left as it is.</summary>
    public override void SetPositionAndTarget(Vector3 position, Vector3 target)
    {
        Position = position;
        Target = target;
    }
}

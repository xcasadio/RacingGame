using CasaEngine.Framework.Entities.Components;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;

namespace RacingGameCasaEngine.Components;

public sealed class ChaseCameraRigComponent : EntityComponent
{
    private Vector3 _smoothedPosition;
    private bool _hasInitializedPosition;

    public float FollowDistance { get; set; } = 8.5f;

    public float FollowHeight { get; set; } = 3.2f;

    public float LookAheadDistance { get; set; } = 9f;

    public float PositionSmoothing { get; set; } = 6f;

    public override EntityComponent Clone()
    {
        return new ChaseCameraRigComponent
        {
            FollowDistance = FollowDistance,
            FollowHeight = FollowHeight,
            LookAheadDistance = LookAheadDistance,
            PositionSmoothing = PositionSmoothing,
        };
    }

    public override void Update(float elapsedTime)
    {
        if (Owner?.RootComponent is not CameraLookAtComponent camera)
        {
            return;
        }

        if (Owner.World?.Game is not RacingGameCasaEngineGame game)
        {
            return;
        }

        var pawn = game.RaceSession.PlayerPawn;
        if (pawn?.RootComponent == null)
        {
            return;
        }

        Vector3 anchor = pawn.RootComponent.Position;
        Vector3 forward = pawn.RootComponent.Forward;
        if (forward.LengthSquared() < 0.001f)
        {
            forward = Vector3.Forward;
        }

        Vector3 desiredPosition = anchor - Vector3.Normalize(forward) * FollowDistance + Vector3.Up * FollowHeight;
        if (!_hasInitializedPosition)
        {
            _smoothedPosition = desiredPosition;
            _hasInitializedPosition = true;
        }
        else
        {
            float blend = 1f - MathF.Exp(-PositionSmoothing * elapsedTime);
            _smoothedPosition = Vector3.Lerp(_smoothedPosition, desiredPosition, blend);
        }

        Vector3 lookTarget = anchor + Vector3.Normalize(forward) * LookAheadDistance + Vector3.Up * 0.8f;
        camera.SetPositionAndTarget(_smoothedPosition, lookTarget);
    }
}
using CasaEngine.Framework.Entities.Components;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Entities;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Components;

public sealed class DebugCarVisualComponent : EntityComponent
{
    public Color BodyColor { get; set; } = Color.Orange;

    public Vector3 HalfExtents { get; set; } = new(1.3f, 0.9f, 2.8f);

    public override EntityComponent Clone()
    {
        return new DebugCarVisualComponent
        {
            BodyColor = BodyColor,
            HalfExtents = HalfExtents,
        };
    }

    public override void Update(float elapsedTime)
    {
        if (Owner is not RacingCarPawn pawn || Owner.World?.Game == null || !Owner.IsVisible)
        {
            return;
        }

        if (pawn.CarVisualComponent?.StaticModel != null)
        {
            return;
        }

        Matrix transform = pawn.GetBodyWorldMatrixNoScale();
        Span<Vector3> corners = stackalloc Vector3[8]
        {
            new(-HalfExtents.X, -HalfExtents.Y, -HalfExtents.Z),
            new(HalfExtents.X, -HalfExtents.Y, -HalfExtents.Z),
            new(HalfExtents.X, HalfExtents.Y, -HalfExtents.Z),
            new(-HalfExtents.X, HalfExtents.Y, -HalfExtents.Z),
            new(-HalfExtents.X, -HalfExtents.Y, HalfExtents.Z),
            new(HalfExtents.X, -HalfExtents.Y, HalfExtents.Z),
            new(HalfExtents.X, HalfExtents.Y, HalfExtents.Z),
            new(-HalfExtents.X, HalfExtents.Y, HalfExtents.Z),
        };

        for (int index = 0; index < corners.Length; index++)
        {
            corners[index] = Vector3.Transform(corners[index], transform);
        }

        DrawEdge(corners[0], corners[1]);
        DrawEdge(corners[1], corners[2]);
        DrawEdge(corners[2], corners[3]);
        DrawEdge(corners[3], corners[0]);
        DrawEdge(corners[4], corners[5]);
        DrawEdge(corners[5], corners[6]);
        DrawEdge(corners[6], corners[7]);
        DrawEdge(corners[7], corners[4]);
        DrawEdge(corners[0], corners[4]);
        DrawEdge(corners[1], corners[5]);
        DrawEdge(corners[2], corners[6]);
        DrawEdge(corners[3], corners[7]);

        Vector3 noseStart = pawn.GetBodyWorldPosition();
        Vector3 noseEnd = noseStart + pawn.GetVisualForward() * 2.2f;
        Owner.World.Game.Line3dRendererComponent.AddLine(noseStart, noseEnd, Color.Yellow);
    }

    private void DrawEdge(Vector3 start, Vector3 end)
    {
        Owner!.World!.Game.Line3dRendererComponent.AddLine(start, end, BodyColor);
    }
}
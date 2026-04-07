using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using CasaEngine.Framework.Rendering;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Bootstrap;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Components;

public sealed class RaceModelBoundsDebugComponent : EntityComponent
{
    public override EntityComponent Clone()
    {
        return new RaceModelBoundsDebugComponent();
    }

    public override void Update(float elapsedTime)
    {
        if (Owner?.World?.Game is not RacingGameCasaEngineGame game)
        {
            return;
        }

        RuntimeRaceSession session = game.RaceSession;
        if (!session.IsActive || !session.IsDebugCameraEnabled)
        {
            return;
        }

        RenderView? activeView = game.GameManager.ViewManager.ActiveView;
        if (activeView?.World != Owner.World || activeView.Camera == null)
        {
            return;
        }

        var frustum = new BoundingFrustum(activeView.Camera.ViewMatrix * activeView.Camera.ProjectionMatrix);
        foreach (Entity entity in Owner.World.Entities)
        {
            if (!ShouldDrawBounds(entity))
            {
                continue;
            }

            BoundingBox bounds = entity.GetBoundingBox();
            if (bounds.Min == bounds.Max)
            {
                continue;
            }

            ContainmentType containment = frustum.Contains(bounds);
            XnaColor color = containment switch
            {
                ContainmentType.Contains => XnaColor.LimeGreen,
                ContainmentType.Intersects => XnaColor.Gold,
                _ => XnaColor.OrangeRed,
            };

            DrawBoundingBox(game.Line3dRendererComponent, bounds, color);
        }
    }

    private static bool ShouldDrawBounds(Entity entity)
    {
        return entity.IsVisible && entity.GetComponent<StaticModelComponent>() != null;
    }

    private static void DrawBoundingBox(Line3dRendererComponent lineRenderer, BoundingBox boundingBox, XnaColor color)
    {
        Vector3 min = boundingBox.Min;
        Vector3 max = boundingBox.Max;

        Vector3 p000 = new(min.X, min.Y, min.Z);
        Vector3 p100 = new(max.X, min.Y, min.Z);
        Vector3 p010 = new(min.X, max.Y, min.Z);
        Vector3 p110 = new(max.X, max.Y, min.Z);
        Vector3 p001 = new(min.X, min.Y, max.Z);
        Vector3 p101 = new(max.X, min.Y, max.Z);
        Vector3 p011 = new(min.X, max.Y, max.Z);
        Vector3 p111 = new(max.X, max.Y, max.Z);

        lineRenderer.AddLine(p000, p100, color);
        lineRenderer.AddLine(p000, p010, color);
        lineRenderer.AddLine(p100, p110, color);
        lineRenderer.AddLine(p010, p110, color);

        lineRenderer.AddLine(p001, p101, color);
        lineRenderer.AddLine(p001, p011, color);
        lineRenderer.AddLine(p101, p111, color);
        lineRenderer.AddLine(p011, p111, color);

        lineRenderer.AddLine(p000, p001, color);
        lineRenderer.AddLine(p100, p101, color);
        lineRenderer.AddLine(p010, p011, color);
        lineRenderer.AddLine(p110, p111, color);
    }
}
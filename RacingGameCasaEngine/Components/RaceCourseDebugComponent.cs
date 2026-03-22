using CasaEngine.Framework.Entities;
using CasaEngine.Framework.Entities.Components;
using Microsoft.Xna.Framework;
using RacingGameCasaEngine.Worlds;
using Color = Microsoft.Xna.Framework.Color;

namespace RacingGameCasaEngine.Components;

public sealed class RaceCourseDebugComponent : EntityComponent
{
    public Color CourseColor { get; set; } = new(80, 190, 255);

    public override EntityComponent Clone()
    {
        return new RaceCourseDebugComponent
        {
            CourseColor = CourseColor,
        };
    }

    public override void Update(float elapsedTime)
    {
        if (Owner?.World?.Game == null)
        {
            return;
        }

        List<Vector3> coursePoints = Owner.World.Entities
            .Where(static entity => entity.Name == RaceWorldFactory.PlayerStartEntityName || entity.Name.StartsWith("Checkpoint.", StringComparison.Ordinal))
            .OrderBy(static entity => entity.Name, StringComparer.Ordinal)
            .Select(static entity => entity.RootComponent?.Position ?? Vector3.Zero)
            .ToList();

        if (coursePoints.Count < 2)
        {
            return;
        }

        for (int index = 0; index < coursePoints.Count - 1; index++)
        {
            Owner.World.Game.Line3dRendererComponent.AddLine(coursePoints[index], coursePoints[index + 1], CourseColor);
        }

        Owner.World.Game.Line3dRendererComponent.AddLine(coursePoints[^1], coursePoints[0], CourseColor);
    }
}
using System.ComponentModel;

namespace RacingGame.Gameplay.Components;

/// <summary>
/// One model a track generator component built: a procedural mesh, or a shared cached model (a guard rail post, a column
/// segment), with its transform. A generator recreates these children from its settings every time it generates, so the
/// editor never saves them: the type is not browsable.
/// </summary>
[Browsable(false)]
public sealed class RaceGeneratedModelComponent : StaticModelComponent
{
    public RaceGeneratedModelComponent()
    {
    }

    private RaceGeneratedModelComponent(RaceGeneratedModelComponent other)
        : base(other)
    {
        StaticModel = other.StaticModel;
    }

    public override RaceGeneratedModelComponent Clone() => new(this);
}

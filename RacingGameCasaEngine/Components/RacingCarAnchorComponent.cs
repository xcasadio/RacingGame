using System.ComponentModel;

namespace RacingGameCasaEngine.Components;

[DisplayName("Racing Car Anchor")]
public sealed class RacingCarAnchorComponent : SceneComponent
{
    public string AnchorId { get; set; } = string.Empty;

    public RacingCarAnchorComponent()
    {
    }

    private RacingCarAnchorComponent(RacingCarAnchorComponent other) : base(other)
    {
        AnchorId = other.AnchorId;
    }

    public override EntityComponent Clone()
    {
        return new RacingCarAnchorComponent(this);
    }
}
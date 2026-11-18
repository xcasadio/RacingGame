using System.ComponentModel;
using CasaEngine.Core.Serialization;

namespace RacingGame.Gameplay.Components;

[DisplayName("Racing Car Anchor")]
public sealed class RacingCarAnchorComponent : SceneComponent
{
    [SerializedProperty("anchor_id")]
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
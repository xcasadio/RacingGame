using System.ComponentModel;
using CasaEngine.Core.Serialization;
using CasaEngine.Framework.Assets;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>Builds the guard rail of one side of a race track, and the posts that hold it.</summary>
[DisplayName("Race Guard Rail Generator")]
public sealed class RaceGuardRailGeneratorComponent : RaceTrackGeneratorComponent
{
    /// <summary>The side of the road the rail follows.</summary>
    [SerializedProperty("side")]
    public RaceGuardRailSide Side { get; set; } = RaceGuardRailSide.Left;

    /// <summary>The distance between two posts, in legacy units; the rail's texture repeats with it.</summary>
    [SerializedProperty("holder_gap")]
    public float HolderGap { get; set; } = RaceTrackLayout.HolderGap;

    public RaceGuardRailGeneratorComponent()
    {
    }

    private RaceGuardRailGeneratorComponent(RaceGuardRailGeneratorComponent other)
        : base(other)
    {
    }

    public override EntityComponent Clone() => new RaceGuardRailGeneratorComponent(this);

    private protected override RaceGeneratorSignature CreateSignature() => MakeSignature(holderGap: HolderGap, side: Side);

    private protected override void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output)
    {
        var settings = new RaceTrackLayoutSettings(HolderGap, RaceTrackLayout.ColumnsDistance);
        RaceTrackLayout layout = LegacyTrackSceneFactory.ComputeGeneratorLayout(source, settings);
        output.AddRange(LegacyTrackSceneFactory.CreateGuardRailModels(layout, Side == RaceGuardRailSide.Left, assetContentManager));
    }
}

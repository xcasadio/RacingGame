using System.ComponentModel;
using CasaEngine.Core.Serialization;
using CasaEngine.Framework.Assets;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>Builds the road mesh of a race track, and its tunnels, from the spline and the road helpers of its .Track layout.</summary>
[DisplayName("Race Road Generator")]
public sealed class RaceRoadGeneratorComponent : RaceTrackGeneratorComponent
{
    /// <summary>The catalogued material (Material.Track.Road) the road is drawn with.</summary>
    [SerializedProperty("material_asset_id")]
    public Guid MaterialAssetId { get; set; } = RaceTrackMaterialAssets.Road;

    public RaceRoadGeneratorComponent()
    {
    }

    private RaceRoadGeneratorComponent(RaceRoadGeneratorComponent other)
        : base(other)
    {
    }

    public override EntityComponent Clone() => new RaceRoadGeneratorComponent(this);

    private protected override RaceGeneratorSignature CreateSignature() => MakeSignature(materialAssetId: MaterialAssetId);

    private protected override void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output)
    {
        RaceTrackLayout layout = LegacyTrackSceneFactory.ComputeGeneratorLayout(source, RaceTrackLayoutSettings.Default);
        output.Add(LegacyTrackSceneFactory.CreateRoadGeneratorModel(layout, MaterialAssetId));

        // The tunnels take their cooked material (Material.Track.RoadTunnel) as a constant, not as a serialized property:
        // the saved worlds do not change.
        RaceGeneratedModel? tunnels = LegacyTrackSceneFactory.CreateTunnelGeneratorModel(layout, RaceTrackMaterialAssets.RoadTunnel);
        if (tunnels != null)
        {
            output.Add(tunnels);
        }
    }
}

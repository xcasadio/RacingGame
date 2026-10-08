using System.ComponentModel;
using CasaEngine.Core.Serialization;
using CasaEngine.Framework.Assets;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>Builds the columns that hold a race track above the landscape, and their segments.</summary>
[DisplayName("Race Column Generator")]
public sealed class RaceColumnGeneratorComponent : RaceTrackGeneratorComponent
{
    /// <summary>The distance between two columns, in legacy units.</summary>
    [SerializedProperty("columns_distance")]
    public float ColumnsDistance { get; set; } = RaceTrackLayout.ColumnsDistance;

    /// <summary>The catalogued material (Material.Track.RoadColumn) the columns is drawn with.</summary>
    [SerializedProperty("material_asset_id")]
    public Guid MaterialAssetId { get; set; } = RaceTrackMaterialAssets.RoadColumn;

    public RaceColumnGeneratorComponent()
    {
    }

    private RaceColumnGeneratorComponent(RaceColumnGeneratorComponent other)
        : base(other)
    {
    }

    public override EntityComponent Clone() => new RaceColumnGeneratorComponent(this);

    private protected override RaceGeneratorSignature CreateSignature() => MakeSignature(columnsDistance: ColumnsDistance, materialAssetId: MaterialAssetId);

    private protected override void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output)
    {
        var settings = new RaceTrackLayoutSettings(RaceTrackLayout.HolderGap, ColumnsDistance);
        RaceTrackLayout layout = LegacyTrackSceneFactory.ComputeGeneratorLayout(source, settings);
        output.AddRange(LegacyTrackSceneFactory.CreateColumnGeneratorModels(layout, MaterialAssetId));
    }
}

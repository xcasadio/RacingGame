using System.ComponentModel;
using CasaEngine.Framework.Assets;
using RacingGame.Gameplay.Track;

namespace RacingGame.Gameplay.Components;

/// <summary>Builds the road mesh of a race track from the spline of its .Track layout.</summary>
[DisplayName("Race Road Generator")]
public sealed class RaceRoadGeneratorComponent : RaceTrackGeneratorComponent
{
    public RaceRoadGeneratorComponent()
    {
    }

    private RaceRoadGeneratorComponent(RaceRoadGeneratorComponent other)
        : base(other)
    {
    }

    public override EntityComponent Clone() => new RaceRoadGeneratorComponent(this);

    private protected override RaceGeneratorSignature CreateSignature() => MakeSignature();

    private protected override void Generate(RaceTrackSource source, AssetContentManager assetContentManager, List<RaceGeneratedModel> output)
    {
        RaceTrackLayout layout = LegacyTrackSceneFactory.ComputeGeneratorLayout(source, RaceTrackLayoutSettings.Default);
        output.Add(LegacyTrackSceneFactory.CreateRoadGeneratorModel(layout, assetContentManager));
    }
}

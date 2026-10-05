using CasaEngine.Core.Log;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.GameFramework;

internal enum RaceStartLightColor
{
    Red,
    Yellow,
    Green,
}

/// <summary>
/// The race's start light, ported from RacingGame (<c>RacingGame.Shared/GameLogic/BasePlayer.cs</c>, the start zoom, and
/// <c>Landscapes/TrackObjectManager.cs</c>, <c>ReplaceStartLightObject</c> and <c>ResetStartLight</c>): red when the race
/// starts, yellow one second before the start, green at the start. RacingGame swapped the light's model; so does this
/// class, re-initializing the light's <see cref="StaticModelComponent"/>, which rebuilds its meshes from the new model.
/// </summary>
internal sealed class RaceStartLight
{
    private readonly World _world;
    private readonly StaticModelComponent _component;
    private readonly StaticModel[] _models;
    private RaceStartLightColor? _color;

    private RaceStartLight(World world, StaticModelComponent component, StaticModel[] models)
    {
        _world = world;
        _component = component;
        _models = models;
    }

    /// <summary>The colour of the light for the seconds left before the start.</summary>
    public static RaceStartLightColor GetColor(float countdownSecondsRemaining)
    {
        return countdownSecondsRemaining >= 1f
            ? RaceStartLightColor.Red
            : countdownSecondsRemaining > 0f ? RaceStartLightColor.Yellow : RaceStartLightColor.Green;
    }

    /// <summary>The start light of a race world, built red by <see cref="LegacyTrackSceneFactory"/>; null without one.</summary>
    public static RaceStartLight? Find(World world)
    {
        if (world.Entities.FirstOrDefault(static entity => entity.Name.StartsWith(LegacyTrackSceneFactory.StartLightEntityNamePrefix, StringComparison.Ordinal))
                ?.RootComponent is not StaticModelComponent component)
        {
            return null;
        }

        StaticModel?[] models = LegacyTrackSceneFactory.LoadStartLightModels(world.Game.AssetContentManager);
        if (models.Any(static model => model == null))
        {
            Logs.WriteWarning("Start light: a light model is missing, the start light stays as built.");
            return null;
        }

        foreach (StaticModel? model in models)
        {
            model!.Initialize(world.Game.AssetContentManager);
        }

        return new RaceStartLight(world, component, models!);
    }

    /// <summary>Shows <paramref name="color"/>; returns false when the light already showed it.</summary>
    public bool Show(RaceStartLightColor color)
    {
        if (_color == color)
        {
            return false;
        }

        StaticModel model = _models[(int)color];
        if (!ReferenceEquals(_component.StaticModel, model))
        {
            _component.StaticModel = model;
            _component.InitializeWithWorld(_world);
        }

        _color = color;
        Logs.WriteTrace($"Start light: {color.ToString().ToLowerInvariant()}");
        return true;
    }
}

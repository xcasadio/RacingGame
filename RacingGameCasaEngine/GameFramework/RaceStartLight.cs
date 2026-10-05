using CasaEngine.Core.Log;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Audio;
using RacingGameCasaEngine.Worlds;

namespace RacingGameCasaEngine.GameFramework;

/// <summary>
/// The race's start light, ported from RacingGame (<c>RacingGame.Shared/GameLogic/BasePlayer.cs</c>, the start zoom, and
/// <c>Landscapes/TrackObjectManager.cs</c>, <c>ReplaceStartLightObject</c> and <c>ResetStartLight</c>). The light is red
/// when the race starts. Two seconds before the start it beeps, one second before it turns yellow and beeps, and at the
/// start it turns green and bleeps.
/// <para/>
/// RacingGame swapped the light's model; so does this class, re-initializing the light's
/// <see cref="StaticModelComponent"/>, which rebuilds its meshes from the new model. The sounds go through CasaEngine's
/// audio service (ADR-0003), whose voices follow the Sound Volume option (<c>SoundEffect.MasterVolume</c>).
/// </summary>
internal sealed class RaceStartLight : IDisposable
{
    private const string BeepSoundName = "Sound.Beep";
    private const string BleepSoundName = "Sound.Bleep";
    private static readonly string[] SignalColors = ["red", "yellow", "green"];

    private readonly World _world;
    private readonly StaticModelComponent _component;
    private readonly StaticModel[] _models;
    private readonly AssetHandle<SoundAsset>? _beep;
    private readonly AssetHandle<SoundAsset>? _bleep;
    private int _signal = -1;

    private RaceStartLight(World world, StaticModelComponent component, StaticModel[] models, AssetHandle<SoundAsset>? beep, AssetHandle<SoundAsset>? bleep)
    {
        _world = world;
        _component = component;
        _models = models;
        _beep = beep;
        _bleep = bleep;
    }

    /// <summary>
    /// The signal given for the seconds left before the start, numbered as RacingGame's <c>ReplaceStartLightObject</c>
    /// numbered its models: -1 above two seconds, then 0 (red), 1 (yellow) and 2 (green, the start).
    /// </summary>
    public static int GetSignal(float countdownSecondsRemaining)
    {
        return countdownSecondsRemaining >= 2f ? -1
            : countdownSecondsRemaining >= 1f ? 0
            : countdownSecondsRemaining > 0f ? 1
            : 2;
    }

    /// <summary>The start light of a race world, built red by <see cref="LegacyTrackSceneFactory"/>; null without one.</summary>
    public static RaceStartLight? Find(World world)
    {
        if (world.Entities.FirstOrDefault(static entity => entity.Name.StartsWith(LegacyTrackSceneFactory.StartLightEntityNamePrefix, StringComparison.Ordinal))
                ?.RootComponent is not StaticModelComponent component)
        {
            return null;
        }

        AssetContentManager assetContentManager = world.Game.AssetContentManager;
        StaticModel?[] models = LegacyTrackSceneFactory.LoadStartLightModels(assetContentManager);
        if (models.Any(static model => model == null))
        {
            Logs.WriteWarning("Start light: a light model is missing, the start light stays as built.");
            return null;
        }

        foreach (StaticModel? model in models)
        {
            model!.Initialize(assetContentManager);
        }

        return new RaceStartLight(world, component, models!, AcquireSound(assetContentManager, BeepSoundName), AcquireSound(assetContentManager, BleepSoundName));
    }

    /// <summary>Gives the signal due for the seconds left before the start, once: the light's colour and its sound.</summary>
    public void Update(float countdownSecondsRemaining)
    {
        int signal = GetSignal(countdownSecondsRemaining);
        if (signal <= _signal)
        {
            return;
        }

        _signal = signal;
        StaticModel model = _models[signal];
        if (!ReferenceEquals(_component.StaticModel, model))
        {
            _component.StaticModel = model;
            _component.InitializeWithWorld(_world);
        }

        bool isStart = signal == _models.Length - 1;
        Logs.WriteTrace($"Start light: {SignalColors[signal]}, {PlaySound(isStart ? _bleep : _beep, isStart ? "bleep" : "beep")}");
    }

    /// <summary>Gives back the sounds' asset handles.</summary>
    public void Dispose()
    {
        _beep?.Dispose();
        _bleep?.Dispose();
    }

    private string PlaySound(AssetHandle<SoundAsset>? sound, string soundName)
    {
        if (sound?.Asset is not { } soundAsset || _world.Game.AudioSystemComponent is not { } audio)
        {
            return $"{soundName} unavailable";
        }

        AudioVoiceHandle voice = audio.Service.PlaySound(soundAsset, _world);
        return $"{soundName} {(voice.IsValid ? "played" : "not played")} (audio {(audio.IsAudioAvailable ? "available" : "unavailable")})";
    }

    private static AssetHandle<SoundAsset>? AcquireSound(AssetContentManager assetContentManager, string soundName)
    {
        if (AssetCatalog.Get(soundName) is not { } assetInfo)
        {
            Logs.WriteWarning($"Start light: sound '{soundName}' is not in AssetInfos.json.");
            return null;
        }

        try
        {
            return assetContentManager.Acquire<SoundAsset>(assetInfo.Id);
        }
        catch (Exception exception)
        {
            Logs.WriteWarning($"Start light: sound '{soundName}' cannot be loaded. {exception.Message}");
            return null;
        }
    }
}

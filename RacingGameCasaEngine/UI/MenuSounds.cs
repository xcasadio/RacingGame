using System;
using CasaEngine.Core.Log;
using CasaEngine.Framework.Application;
using CasaEngine.Framework.Assets;
using CasaEngine.Framework.Audio;

namespace RacingGameCasaEngine.UI;

/// <summary>The menu sounds of RacingGame (Sound.Sounds of RacingGame.Shared/Sounds/Sound.cs).</summary>
internal enum MenuSound
{
    /// <summary>A selection changes (RacingGame's "Highlight" cue, menu_highlight.wav).</summary>
    Highlight,

    /// <summary>A screen opens over the current one (RacingGame's "ScreenClick" cue, menu_screenclick.wav).</summary>
    ScreenClick,

    /// <summary>A screen closes (RacingGame's "ScreenBack" cue, menu_screenback.wav).</summary>
    ScreenBack,

    /// <summary>A choice moves with the keys or the pad (RacingGame's "ButtonClick" cue, menu_buttonclick.wav).</summary>
    ButtonClick,
}

/// <summary>
/// Holds the menu sound assets for the game's life and plays them through CasaEngine's audio system, at the volume of
/// the options (ADR-0003, ADR-0009).
/// </summary>
internal sealed class MenuSounds : IDisposable
{
    private static readonly string[] SoundNames = ["Sound.MenuHighlight", "Sound.MenuScreenClick", "Sound.MenuScreenBack", "Sound.MenuButtonClick"];

    private readonly CasaEngineGame _game;
    private readonly AssetHandle<SoundAsset>?[] _sounds = new AssetHandle<SoundAsset>?[SoundNames.Length];

    public MenuSounds(CasaEngineGame game)
    {
        _game = game;
        for (int i = 0; i < SoundNames.Length; i++)
        {
            _sounds[i] = Acquire(game.AssetContentManager, SoundNames[i]);
        }
    }

    public void Play(MenuSound sound)
    {
        if (_sounds[(int)sound]?.Asset is not { } soundAsset || _game.AudioSystemComponent is not { } audio)
        {
            Logs.WriteTrace($"Menu sound {sound} unavailable");
            return;
        }

        AudioVoiceHandle voice = audio.Service.PlaySound(soundAsset);
        Logs.WriteTrace($"Menu sound {sound} {(voice.IsValid ? "played" : "not played")} (audio {(audio.IsAudioAvailable ? "available" : "unavailable")})");
    }

    public void Dispose()
    {
        for (int i = 0; i < _sounds.Length; i++)
        {
            _sounds[i]?.Dispose();
            _sounds[i] = null;
        }
    }

    private static AssetHandle<SoundAsset>? Acquire(AssetContentManager assetContentManager, string soundName)
    {
        if (AssetCatalog.Get(soundName) is not { } assetInfo)
        {
            Logs.WriteWarning($"Menu sounds: '{soundName}' is not in AssetInfos.json.");
            return null;
        }

        try
        {
            return assetContentManager.Acquire<SoundAsset>(assetInfo.Id);
        }
        catch (Exception exception)
        {
            Logs.WriteWarning($"Menu sounds: '{soundName}' cannot be loaded. {exception.Message}");
            return null;
        }
    }
}

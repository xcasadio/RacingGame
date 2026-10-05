# ADR-0003: RacingGameCasaEngine plays its sound effects through CasaEngine's audio system

- **Status**: Accepted
- **Date**: 2026-10-05
- **Source**: this chantier: `ai-agent/tasks/rgce-hud-shadows-start-tasks.md`, decision D3 and proposal P4 (author answers and plan approval, 2026-10-05)

## Context

- The author asked for the legacy race start: the 3D start light turns red, yellow then green, with the "Beep" and "Bleep" sounds, at the volume set in the options (D3).
- RacingGame plays them through its own sound façade: "Beep" when the light turns red or yellow, "Bleep" when it turns green (`RacingGame.Shared/Landscapes/TrackObjectManager.cs:161-181`). The sources are `RacingGame/Content/Audio/Waves/Beep.wav` and `Bleep.wav`, 16-bit stereo PCM at 44.1 kHz.
- RacingGameCasaEngine plays no sound. Its Sound Volume and Music Volume options only set MonoGame's `SoundEffect.MasterVolume` and `MediaPlayer.Volume` (`RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:120-121, 143-144`).
- CasaEngine has an audio system:
  - `CasaEngineGame` creates an `AudioSystemComponent` (`CasaEngine/CasaEngine/Framework/Application/CasaEngineGame.cs:63, 366`);
  - its `AudioService.PlaySound(SoundAsset)` plays a sound asset (`CasaEngine/CasaEngine/Framework/Audio/AudioService.cs:104-109`) through a mixer of buses hanging from `Master` (`Mixing/AudioBusNames.cs`);
  - a `.sound` asset references an audio file asset and defaults to the `Sfx` bus (`SoundAsset.cs:20, 28, 77`; CasaEngine `docs/decisions/0002-audio-asset-format-and-editor-scope-v1.md`);
  - without an audio device, playback is silent instead of failing (`Backends/MonoGameAudioBackend.cs:102, 241`).
- ADR-0002 keeps every catalogued file under `RacingGameCasaEngine/Content`.

## Decision

- RacingGameCasaEngine plays its sound effects through CasaEngine's `AudioService`, never through MonoGame's `SoundEffect` directly.
- Each sound is a `.sound` asset on the `Sfx` bus, catalogued in `AssetInfos.json`, next to its audio file under `RacingGameCasaEngine/Content/Audio/`. The audio files are copies of the RacingGame sources, as ADR-0002 requires.
- The Sound Volume option governs these sounds. Whether `SoundEffect.MasterVolume` already reaches CasaEngine's voices is established from the engine code. If it does not, the option also sets the volume of CasaEngine's audio system.
- The first sounds are "Beep" and "Bleep", for the start light.

## Consequences

- The sounds can be previewed and tuned in the CasaEngine editor, like any other asset of the project.
- Each later sound follows the same path: a copied audio file, a `.sound` asset and a catalogue entry.
- The other RacingGame sounds (menu, brakes, crashes, checkpoints, results, music and the engine sound; `RacingGame.Shared/Sounds/Sound.cs:24-48`, `EngineSound.cs`) are not ported by this decision.
- CasaEngine and MGUI are not changed. A missing engine capability is reported, not worked around in the engine.

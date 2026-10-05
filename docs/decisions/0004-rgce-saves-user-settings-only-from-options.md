# ADR-0004: RacingGameCasaEngine saves the user settings only when the player leaves the Options screen

- **Status**: Accepted
- **Date**: 2026-10-05
- **Source**: this chantier: `ai-agent/tasks/rgce-automation-settings-tasks.md`, decision D1 (author answers and plan approval, 2026-10-05)

## Context

- RacingGameCasaEngine keeps two user settings files under `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine`: `display-settings.json` and `front-end-options.json` (`RacingGameCasaEngine/Program.cs:50-52`).
- Both are written by `RacingGameCasaEngineGame.ApplyFrontEndOptions` and by nothing else (`RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:140-141`). That method has three callers:
  - the Back button of the Options screen (`RacingGameCasaEngine/Screens/OptionsScreen.cs:76, 99-103`);
  - `OnWorldLoaded`, on every world load: front-end start, race start and return from a race (`RacingGameCasaEngineGame.cs:169-177`);
  - the `--smoke-frontend` validator, through `RaceFrontEndFlow.ApplyOptionsAndReturnToMainMenuForAutomation` (`RacingGameCasaEngine/Bootstrap/RaceFrontEndFlow.cs:127-132`).
- The automation modes therefore overwrite the author's settings:
  - `--smoke-frontend` switches the saved resolution between 1280x720 and 1920x1080 (`Bootstrap/FrontEndNavigationSmokeValidator.cs:96-103`);
  - the two car audits save the driving mode they force (`Bootstrap/CarProfileAuditValidator.cs:111`, `Bootstrap/CarTopSpeedAuditValidator.cs:107`);
  - every validator that starts a race rewrites both files.
- On 2026-10-05, after a session of validator runs, the resolution in `display-settings.json` had changed several times.

## Decision

- The user settings are saved only when the player leaves the Options screen with its Back button.
- Applying the settings never saves them. World loads and the automation modes still apply the settings, but write neither file, in automation as in normal play.

## Consequences

- The automation modes leave both files unchanged, without a special case for automation in the game.
- The `--smoke-frontend` validator still checks that a resolution chosen in the options is applied. No automation mode exercises the save path any more, so it is checked by hand.
- In normal play, a world load no longer rewrites the files. A window resize, or a state change made outside the Options screen, is no longer saved at the next world load.
- On a first launch, the files are created only when the player first leaves the Options screen. Until then, the loaders fall back to their defaults, as they already do when a file is missing.

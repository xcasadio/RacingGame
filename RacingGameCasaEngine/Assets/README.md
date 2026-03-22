# RacingGameCasaEngine asset conventions

Runtime assets are resolved from `RacingGameCasaEngine/Content`.

## Rules

- `Content/AssetInfos.json` is the runtime index consumed by `AssetCatalog.Load(...)`.
- Asset names must stay stable and unique because gameplay code resolves them by logical name first.
- `file_name` values stay relative to `Content` and always use `/` or simple folder segments.
- Game-specific JSON data lives with the game project, not inside `CasaEngine`.

## Suggested folders

- `Content/Textures/` for UI textures, decals, and simple 2D placeholders.
- `Content/Tracks/` for track data, checkpoints, and race metadata.
- `Content/Cars/` for car definitions, tuning, and visual assets.
- `Content/Worlds/` for serialized CasaEngine worlds when the bootstrap stops building them in code.
- `Content/Audio/` for music and sound effects.
- `Content/Shaders/` for shader assets registered in the catalog.
- `Content/UI/` for MGUI-specific assets or data files.

## Near-term migration policy

- Keep bootstrap assets minimal until the race world composition is stable.
- Prefer adding game-level loaders or data files in `RacingGameCasaEngine` instead of extending `CasaEngine` for racing-specific needs.
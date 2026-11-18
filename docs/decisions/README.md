# Architecture Decision Records

This folder records the architecture decisions of this repository: architecture, data formats, public APIs, backends, and the rules that govern the AI agents working on it.

## Rules

- One file per decision, named `NNNN-short-title.md` with an increasing four-digit number. Decisions taken together on the same theme may share one file.
- Written in English, from the template [template.md](template.md): Status, Date, Source, Context, Decision, Consequences.
- Every decision taken during a plan or a discussion is recorded here, at the time it is taken (skill `adr`).
- Backfilled decisions keep a `Source` pointing at the original document. Existing audits are read-only: the decision is copied here, the audit is not edited.
- A decision is never rewritten: a change is a new record that supersedes the old one, whose status becomes `Superseded by ADR-XXXX`.

## Index

| ADR | Title | Status | Date |
|---|---|---|---|
| ADR-0001 | RacingGameCasaEngine screens are CasaEngine screen assets bound to view models | Accepted | 2026-10-05 |
| ADR-0002 | RacingGameCasaEngine's content folder is a CasaEngine editor project | Accepted; superseded in part by ADR-0023, ADR-0024 | 2026-10-05 |
| ADR-0003 | RacingGameCasaEngine plays its sound effects through CasaEngine's audio system | Accepted | 2026-10-05 |
| ADR-0004 | RacingGameCasaEngine saves the user settings only when the player leaves the Options screen | Superseded by ADR-0013 | 2026-10-05 |
| ADR-0005 | RacingGameCasaEngine's main menu reproduces the original XNA menu with MGUI brushes | Superseded by ADR-0006 | 2026-10-05 |
| ADR-0006 | RacingGameCasaEngine's main menu reproduces the original XNA menu, as delivered | Superseded by ADR-0007 | 2026-10-05 |
| ADR-0007 | RacingGameCasaEngine's main menu reproduces the original XNA menu, with a continuous bevel | Accepted | 2026-10-06 |
| ADR-0008 | RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured | Superseded by ADR-0009 | 2026-10-06 |
| ADR-0009 | RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured, as delivered | Accepted | 2026-10-06 |
| ADR-0010 | RacingGameCasaEngine's track selection reproduces the original XNA screen | Superseded by ADR-0011 | 2026-10-06 |
| ADR-0011 | RacingGameCasaEngine's track selection reproduces the original XNA screen, with cards growing around fixed centres | Accepted | 2026-10-06 |
| ADR-0012 | RacingGameCasaEngine's UI images are premultiplied, except the menu background | Accepted | 2026-10-06 |
| ADR-0013 | RacingGameCasaEngine's Highscores, Options and Help screens reproduce the original XNA screens | Superseded by ADR-0014 | 2026-10-06 |
| ADR-0014 | RacingGameCasaEngine's Help and Options keep their previous layout in the menus' style; Highscores reproduces the original screen | Accepted | 2026-10-06 |
| ADR-0015 | The original game exports the 3D reference views; the port replays them | Accepted | 2026-11-14 |
| ADR-0016 | The legacy normal maps are converted to the standard channel layout | Accepted | 2026-11-14 |
| ADR-0017 | The legacy sky cube is converted to the axes of the converted world | Accepted | 2026-11-14 |
| ADR-0018 | The race world renders in linear HDR with PBR materials | Accepted; amended by ADR-0028, ADR-0031, ADR-0032, ADR-0034 | 2026-11-16 |
| ADR-0019 | RacingGameCasaEngine reads the legacy track helpers and CombiModel items | Accepted; amended by ADR-0027, ADR-0029, ADR-0030 | 2026-10-08 |
| ADR-0020 | RacingGameCasaEngine names every screen element and every component | Accepted | 2026-10-09 |
| ADR-0021 | RacingGameCasaEngine's automation modes ignore the player input | Accepted; completed by ADR-0022 | 2026-10-15 |
| ADR-0022 | RacingGameCasaEngine's automation runs ignore the window state | Accepted | 2026-10-15 |
| ADR-0023 | RacingGame.Gameplay is the CasaEngine editor's gameplay assembly for RacingGameCasaEngine (numbered ADR-0015 until 2026-11-16) | Accepted | 2026-10-08 |
| ADR-0024 | RacingGameCasaEngine's content folder holds every game file (numbered ADR-0016 until 2026-11-16) | Accepted; amended by ADR-0016 and ADR-0017 | 2026-10-08 |
| ADR-0025 | RacingGame.Gameplay holds the game code, and generator components rebuild the track (numbered ADR-0017 until 2026-11-16) | Accepted; superseded in part by ADR-0019 | 2026-10-08 |
| ADR-0026 | RacingGameCasaEngine's models, materials and textures are cooked assets (numbered ADR-0018 until 2026-11-16) | Accepted; amended by ADR-0028 | 2026-10-08 |
| ADR-0027 | RacingGameCasaEngine places the track objects as the original does | Accepted; amended by ADR-0041 | 2026-11-16 |
| ADR-0028 | RacingGameCasaEngine cooks the race world materials as PBR, from the surface families of their recipes | Accepted; amended by ADR-0034, ADR-0037, ADR-0038 | 2026-11-17 |
| ADR-0029 | RacingGameCasaEngine builds the road underside and the city ground plane of the original | Accepted | 2026-11-17 |
| ADR-0030 | RacingGameCasaEngine lights the tunnel with the original's material ambient as an emissive texture | Accepted; amended by ADR-0033 | 2026-11-17 |
| ADR-0031 | RacingGameCasaEngine lights the car selection carousel at the PBR scale and sets the ambient colour of both worlds to 0.05 | Accepted | 2026-11-17 |
| ADR-0032 | RacingGameCasaEngine balances the light of the race world against the original with an exposure of 0.9 | Accepted | 2026-11-17 |
| ADR-0033 | RacingGameCasaEngine lights the signs and the car glass as the original does and shadows the tunnel emissive | Accepted; amended by ADR-0038, ADR-0039 | 2026-11-17 |
| ADR-0034 | RacingGameCasaEngine gives the road the original's sun sheen and takes the environment light off it | Accepted | 2026-11-18 |
| ADR-0035 | RacingGameCasaEngine filters the shadow edges of the race world with 64 taps | Accepted | 2026-11-18 |
| ADR-0036 | RacingGameCasaEngine sets the shadow colour of the race world against the original | Accepted | 2026-11-18 |
| ADR-0037 | RacingGameCasaEngine brings the palm foliage toward the original with a measured base level and the original's alpha test | Accepted; amended by ADR-0040 | 2026-11-18 |
| ADR-0038 | RacingGameCasaEngine lights the window glass of the buildings as the original does | Accepted | 2026-11-18 |
| ADR-0039 | RacingGameCasaEngine neutralises the colour of the car glass | Accepted | 2026-11-18 |
| ADR-0040 | RacingGameCasaEngine gives the leaf of the small palm its own base level | Accepted | 2026-11-18 |
| ADR-0041 | RacingGameCasaEngine draws the scenery of the game with the seed of the reference capture | Proposed | 2026-11-18 |

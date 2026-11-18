# ADR-0041: RacingGameCasaEngine draws the scenery of the game with the seed of the reference capture

- **Status**: Proposed
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-worlds-as-assets-tasks.md`, decision D13 (the author's answer of 2026-11-18 to the question on the scenery a saved world freezes: "Graine 1 partout") and proposition P21, slice M0; the finding: the first review of that plan's envelope; the measurement: `artifacts/worlds-as-assets/m0/` (`ref/`, `m0_check.txt`, `seed2/`) and the script `artifacts/worlds-as-assets/scripts/m0_check.py`; ADR-0027

## Context

Facts verified on 2026-11-18:

- ADR-0027 gave the random scenery (palms, lanterns, start objects, checkpoint banners, signs, random filler) one generator with one seed. The reference view capture took the seed of its `views.json`, which is 1, the value the original's reference capture reseeds its generator with before each level; everywhere else the seed was derived from the track's name.
- The game, the track audit, the interface captures and the scene export therefore showed another scenery than the 15 reference views and the tunnel views, the only ones compared with the original object for object (1147, 1693 and 3404 objects matched, ADR-0027).
- The plan `rgce-worlds-as-assets-tasks.md` saves the race worlds as assets. A saved world freezes one scenery: with two seeds, the worlds the game loads could not be the ones the bench draws.
- The original game seeds its generator once from the clock: no scenery of its own is canonical.

## Decision

- **The scenery of every track is drawn with one constant seed, 1** (`RaceTrackLayout.ScenerySeed`), in the game and in every automation mode. The per-track seed (`RaceTrackLayout.CreateTrackSeed`) and the override the reference capture set (`LegacyTrackSceneFactory.SceneryRandomSeed`) are removed: nothing sets the seed at run time.
- **The reference view capture checks the seed of its `views.json`** against that constant and fails, with a message naming both values, when they differ.

## Consequences

- Measured on the applied state (every run replayed twice, identical byte for byte):
  - the 15 reference views and the 3 tunnel views, in `linear` and in `legacy`, are byte-identical to the previous reference (`artifacts/pbr-handover/s25/g7/ref/`), and so are the scene measures: the bench already drew this scenery;
  - the placements of each track, read from the digest every race world logs when it is built, are the bench's in the track audit, the interface captures and the scene export: 1147, 1693 and 3404 placements (649, 904 and 1745 of scenery) for Beginner, Advanced and Expert, instead of 1274, 1757 and 3432 (776, 968 and 1773);
  - 23 of the 30 track audit captures change; 7 do not (the six taken with the road alone, and the terrain overview of Beginner);
  - the interface screens `ui-race-hud`, `ui-pause` and `ui-race-finished` change; the seven other screens do not;
  - a `views.json` exported with the seed 2 makes the capture exit with the code 1 and write no picture.
- The player sees the palms, lanterns, banners, signs and filler objects at other places than before, once.
- New reference of the track audit and of the interface: `artifacts/worlds-as-assets/m0/ref/`.
- Amends ADR-0027: its sentence "Everywhere else the seed is fixed per track, derived from the track name" no longer stands; its status line points here. ADR-0027 is not rewritten.

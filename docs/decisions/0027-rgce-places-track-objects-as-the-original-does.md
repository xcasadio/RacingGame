# ADR-0027: RacingGameCasaEngine places the track objects as the original does

- **Status**: Proposed
- **Date**: 2026-11-16
- **Source**: this chantier: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, decision D11 (author answer, 2026-11-16: align the topics both machines implemented on the original, now, by correcting the local code), slice S1a, tasks T6.1 and T6.2; measures in `docs/racinggame-3d-capture-bench-local-baseline.md`

## Context

- The capture bench (ADR-0015) measured the local version against the original with the original's seed 1 (`docs/racinggame-3d-capture-bench-local-baseline.md`, first run). The road points were identical. The placed objects were not: 821, 1226 and 2595 objects for the original's 1147, 1693 and 3404, with 712, 1007 and 2140 matched within 1 m. Guard rail posts were scaled by 1.125 where the original's are 1.35, column segments were two to three times too many, and 620, 864 and 1771 of the matched objects cast a shadow the original's do not cast.
- ADR-0019 states that, against the original, nothing is missing. The measure contradicts it: the random filler scenery of the original (`RacingGameOriginal/Core/Tracks/Track.cs:1087-1161`) did not exist in the game.
- The causes are in how objects are added, read in the original's code:
  - every object goes through one add path (`Landscape.cs:392-497`): name fixes, combined models expanded, terrain clamp, a skip rule against every object added before it that uses the model size, and a scale of 1.2. The game sent only the scenery through its own version of it; posts and column segments bypassed it;
  - offsets use the raw right vector, `cross(direction, up)` not normalized (`TrackLine.cs:490`, which replaces the normalized vector of lines 462-464). The game normalized it;
  - the road angle is `acos(dot)` without a clamp (`Vector3Helper.cs:33-40`). The game clamped it, which changes a random draw when the value is not a number;
  - palms, lanterns, start objects, checkpoint banners and signs, and the random filler draw from one generator (`RandomHelper.cs:23-59`, `Track.cs:704-1161`). The game seeds it once, from the clock; the original's reference capture reseeds it before each level (`TrackAuditCaptureScreen.cs:231`). The game drew palms and helpers from two fixed sequences per track (ADR-0019);
  - the skip rule uses the original's model sizes. The sizes read from the cooked glTF models differ;
  - helper ranges (tunnels, palms, lanterns) open on any helper, a reset included (`TrackLine.cs:612-660`). The game did not open one on a reset;
  - an object casts a shadow when it is in the original's near-track list, and receives one when it is near the track, is not a banner or a sign and is not alpha tested (`Landscape.cs:56`, `159-162`, `409-414`, `1314-1321`; `Model.cs:212`, `877`). The game set both flags on almost every object.
- The author decided to align these topics on the original now, by correcting the local code and keeping its architecture (D11).

## Decision

- **One add path.** `RacingGame.Gameplay/Track/LegacyLandscapePlacer.cs` ports the original's add path. Neutral objects of the track file, guard rail posts, column segments and every scenery object go through it, in the original's order (`TrackLine.cs:664-676`, `Track.cs:637-641`, then `Track.cs:704-1161`).
- **The original's side effects are kept**: raw right vector for the offsets, road angle without a clamp, helper ranges that open on a reset.
- **The rail mesh is built from the rail points of its posts**, as the original does (`GuardRail.cs:169-199`): both use the raw right vector. The road mesh keeps the normalized right vector.
- **One random generator, drawn in the original's order.** The layout takes an integer seed (`RaceTrackLayout.Compute`).
  - The reference capture passes the seed of `views.json` (`LegacyTrackSceneFactory.SceneryRandomSeed`, set by `ReferenceViewCaptureValidator`).
  - Everywhere else the seed is fixed per track, derived from the track name (`RaceTrackLayout.CreateTrackSeed`). The game and the other automation modes stay repeatable from launch to launch, as ADR-0019 wanted. The original's time-based seed is not reproduced.
- **Model sizes of the original for the skip rule.** A table in `LegacyLandscapePlacer` holds them, with its source. The cooked models are not changed.
- **Generators follow the same path.** `LegacyTrackSceneFactory.ComputeGeneratorLayout` runs neutral objects, posts and column segments through the add path. It needs no seed: the original adds them before any random draw.
- **Random filler scenery** of the original is placed (`Track.cs:1087-1161`), with the high detail probability the original's export uses.
- **Shadow flags as the original**: cast and receive are set on the scenery entities and on the post and column segment children by the rules above. Generated meshes (road, terrain, rails, columns) are not touched.
- **Sheared placements keep their position and scale.** The original's point space is sheared on a slope, and an entity holds a scale, a rotation and a translation. `RaceTrackSpace.TryDecomposePlacement` gives the exact scale and the closest rotation.

## Consequences

- Amends ADR-0019: its two fixed random sequences per track are replaced by one generator with one seed per track, and its statement that nothing is missing against the original no longer stands as written. Its reading of the track helpers and CombiModel items is unchanged.
- With the bench seed, every object of the original has its object in the game: 1147, 1693 and 3404 matched, none missing, none extra, distance and scale ratio at 0.000, no shadow flag mismatch (`docs/racinggame-3d-capture-bench-local-baseline.md`, second run).
- Known limit: a guard rail post on a slope leans from the original's by up to 1.08, 6.81 and 11.91 degrees on the three tracks, the shear an entity cannot hold. Lanterns, banners, signs and the start light carry the same shear in their entities, by a few degrees (the independent verification of the slice computed up to 4.91 degrees, on a curve sign); the measure reads the scenery from the layout, where they match, so its rotation column does not show it. An exact result needs a pivot hierarchy per object or a matrix transform in the engine. Open point O9 of the plan.
- The scenery of a normal launch changes once, with this decision: other palms, more rocks and ruins, fewer column segments and lanterns. It is then the same on every launch.
- The rail and its posts sit where the original's do, and the road mesh keeps the normalized right vector. Where the raw vector is shorter than 1, the rail stands inside the edge of the road mesh: by 0.008 m at most on Beginner, 0.298 m on Advanced (6 rail points of 1087 above 5 cm) and 0.627 m on Expert (12 of 2234), computed from the right vectors and road widths of the original's `scene.json`. Aligning the road mesh is open point O11 of the plan.
- `scripts/TrackPlacementExporter` reimplements the previous placement and its model sizes. It no longer describes what the game places; it is not updated by this decision.
- The tunnel ranges are 1, 2 and 10 on the three tracks. The original's export has no tunnel data: the counts come from the ported algorithm, not from a run of the original.
- The model size table is transcribed from the other machine's table, which was read from the original's `.xnb` models. It was not read again from those files here; the full match of the 6244 objects is its check.

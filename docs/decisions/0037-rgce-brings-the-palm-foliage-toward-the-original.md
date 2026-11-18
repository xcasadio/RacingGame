# ADR-0037: RacingGameCasaEngine brings the palm foliage toward the original with a measured base level and the original's alpha test

- **Status**: Accepted; amended by ADR-0040 (the leaf of the small palm, `PalmLeave2.tga`, takes the base grey 220)
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G3 (rule of choice written before the measurement in a frozen script, applied mechanically; author's request of 2026-11-18 to continue in autonomous mode, subject "decor materials"); the measurement: `artifacts/pbr-handover/s24/g3/` (`measure_leaves.json`, `measure_leaves.txt`, one folder per candidate) and the frozen script `artifacts/pbr-handover/s23/scripts/measure_leaves.py`; open point O7 of `ai-agent/tasks/rgce-render-gaps-tasks.md`; ADR-0028

## Context

Facts verified on 2026-11-18:

- The palms have four leaf materials, designated by their base texture: `PalmLeave.tga` (`AlphaPalm`, `AlphaPalm2`, `AlphaPalm3`: material `orig 09 - Defaulta`, family `Vegetation`, roughness 0.95) and `PalmLeave2.tga` (`AlphaPalmSmall`: material `orig 09 - Default`, family `Default`, roughness 0.8). All were cooked with a white base colour (the imported diffuse colour) and the alpha cutoff 0.35 of the engine's import presentation. The original draws them with an alpha test at 0.5 (`RacingGameOriginal/Core/Content/Shaders/NormalMapping.fx`, `Core/Graphics/Model.cs`), ambient 0.1 (0.235 for the leaf of `AlphaPalmSmall`) and diffuse 1.
- Measured on four boxes of the reference views, on the green-dominant pixels (G above R + 8 and above B + 8): the leaves of the port were brighter and less saturated than the original's (for example a mean of 96 / 123 / 72 for 57 / 79 / 30 on `Beginner-sector-20`) and covered 1.2 to 3.2 times as many pixels. The mean colour difference D over the four boxes was 26.99.
- **The cause of the difference in brightness is not established.** The inputs of the two formulas are close (white diffuse, the sun, a small ambient), and reading the shaders did not explain a factor of about 1.5 to 2.

## Decision

- **The four leaf materials take a base colour of grey 170 and an alpha cutoff of 0.5** (`RaceMaterialRecipes.ForScenery`, constants `PalmLeafBaseLevel` and `PalmLeafAlphaCutoff`; the materials are designated by the two texture names, not by the family). Their family and roughness do not change; the trunks and the other vegetation (`Hotel01/plants`) are not touched. The base level is a calibration against the original's captures, not a value read in the original.

Candidates: base grey 255, 220, 190, 170, 150, 130 crossed with alpha cutoff 0.35 and 0.5, measured in `linear` on a copy of the build folder. Quantities (script `measure_leaves.py`): d, per box, the mean absolute difference of the mean R, G, B of the leaf pixels of the candidate and of the original; D, the mean of d over the four boxes (`Beginner-sector-20`, `Expert-sector-20`, `Expert-start`, `Beginner-start`); the leaf area per box, candidate over original; E, the mean difference per channel with the original over the 15 views. Rule, written before the measurement: eligible if every box keeps at least 100 leaf pixels, criterion 3 of the probes is at least 27 and E is at most the start state's; the smallest D is retained and, within 1.0 of it, the cutoff 0.5 of the original before 0.35, then the highest grey.

| Base grey | Alpha cutoff | D | d per box | Leaf area per box | E | Eligible |
|---|---|---|---|---|---|---|
| 255 | 0.35 | 26.99 | 41.9 / 27.3 / 18.4 / 20.3 | 3.17 / 1.62 / 1.26 / 1.23 | 15.312 | yes |
| 255 | 0.5 | 28.57 | 41.8 / 28.3 / 20.3 / 23.9 | 3.10 / 1.58 / 1.20 / 1.11 | 15.287 | yes |
| 220 | 0.35 | 19.83 | 35.2 / 20.4 / 9.5 / 14.3 | 3.04 / 1.56 / 1.24 / 1.13 | 15.260 | yes |
| 220 | 0.5 | 21.24 | 34.9 / 21.1 / 11.1 / 17.8 | 3.00 / 1.52 / 1.19 / 1.03 | 15.232 | yes |
| 190 | 0.35 | 14.96 | 29.8 / 14.9 / 5.9 / 9.3 | 2.84 / 1.45 / 1.21 / 0.98 | 15.236 | yes |
| 190 | 0.5 | 15.46 | 29.5 / 15.3 / 4.9 / 12.2 | 2.84 / 1.43 / 1.16 / 0.91 | 15.205 | yes |
| 170 | 0.35 | 14.19 | 26.6 / 11.8 / 9.7 / 8.6 | 2.62 / 1.33 / 1.17 / 0.86 | 15.232 | yes |
| 170 | 0.5 | 13.88 | 26.2 / 11.7 / 8.9 / 8.7 | 2.64 / 1.34 / 1.13 / 0.83 | 15.199 | yes |
| 150 | 0.35 | 14.79 | 23.8 / 12.4 / 13.2 / 9.7 | 2.27 / 1.17 / 1.10 / 0.73 | 15.235 | yes |
| 150 | 0.5 | 14.39 | 23.4 / 12.4 / 12.6 / 9.2 | 2.32 / 1.19 / 1.08 / 0.70 | 15.201 | yes |
| 130 | 0.35 | 15.77 | 21.4 / 13.1 / 16.3 / 12.2 | 1.76 / 0.96 / 0.99 / 0.57 | 15.243 | yes |
| 130 | 0.5 | 15.43 | 21.2 / 13.1 / 15.9 / 11.6 | 1.81 / 0.98 / 0.98 / 0.57 | 15.207 | yes |

The rule retains grey 170 with the cutoff 0.5: the smallest D is 13.88, four states are within 1.0 of it, and among those at the cutoff 0.5 it has the highest grey.

## Consequences

- On the applied state (15 views byte-identical to the captures of the retained candidate): D 26.99 -> 13.88; E 15.312 -> 15.199 in `linear`, 20.009 -> 19.959 in `legacy`; the probes are unchanged (20 of 34, 2 of 4, 27 of 33, 3 of 3 in `linear`).
- The leaves stay bluer than the original's and still cover more pixels on the large palm of `Beginner-sector-20` (area 2.64 times the original's): the alpha cutoff alone does not bring the silhouette back, and the blue is the sky light on a rough surface.
- Editing these materials in the editor changes the game, and running the cooker again reverts it: the values live in `RaceMaterialRecipes`.
- ADR-0028 gives every imported material its imported diffuse colour as base colour: its status line now points here for the four leaf materials. ADR-0028 is not rewritten.

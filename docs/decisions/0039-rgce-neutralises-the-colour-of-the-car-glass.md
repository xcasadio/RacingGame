# ADR-0039: RacingGameCasaEngine neutralises the colour of the car glass

- **Status**: Accepted
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G5 (rule and iteration written before the measurement in a frozen script, applied mechanically; the author chose the subject on 2026-11-18 and accepted that the car glass also changes in the car selection carousel: "Oui, carrousel compris"); the measurement: `artifacts/pbr-handover/s24/g5/` (`measure_glass.json`, `measure_glass.txt`, `g0/` to `g2/`) and the frozen script `artifacts/pbr-handover/s23/scripts/measure_glass.py`; ADR-0033 (decision D1)

## Context

Facts verified on 2026-11-18:

- The glass of the car (`Models/Car/Standard 19.material`, family `CarGlass`: metallic 1, roughness 0.05) has had the base colour grey 224 since ADR-0033. A metal's reflection is tinted by its base colour, and what it reflects is the sky: on the rear window of `Beginner-start` (box x 608-626, y 388-391, glass only on the original and on the port) the port read 114.7 / 175.7 / 196.2 where the original reads 136.9 / 137.6 / 137.9, a neutral grey. ADR-0033 recorded this as an open gap.
- The original draws the glass as the reflection of the same sky cube, with a reflection vector whose Y and Z axes are swapped, a Fresnel term, an ambient and a blend at 0.66 with what is behind (`RacingGameOriginal/Core/Content/Shaders/ReflectionSimpleGlass.fx`, `Core/Graphics/Model.cs:770`): it does not show the blue of the zenith there.

## Decision

- **The base colour of the car glass is (255, 174, 148)** (`RaceMaterialRecipes.LegacyCarGlassBaseColor`). It is not a physical reflectance: it compensates the colour of the sky that the glass reflects, so that the rear window is the original's neutral grey. Metallic 1 and roughness 0.05 are unchanged.

Iteration (script `measure_glass.py`): each step measures the mean R, G, B of the box and proposes the next base colour, per channel 255 x clip(lin(base) x lin(target) / lin(box), 0, 1)^(1/2.2) with lin(x) = (x/255)^2.2, until the largest error of a channel is at most 6, in at most four steps. Rule, written before the measurement: a step is eligible if criterion 3 of the probes is at least 27 and E, the mean difference per channel with the original over the 15 views, is at most the start's plus 0.02; the smallest mean error of the box is retained.

| Step | Base colour R / G / B | Box R / G / B | Mean error | Largest error | E | Criterion 3 |
|---|---|---|---|---|---|---|
| g0 | 224 / 224 / 224 | 114.7 / 175.7 / 196.2 | 39.50 | 58.22 | 15.047 | 27 |
| g1 | 255 / 175 / 158 | 134.4 / 138.7 / 147.4 | 4.33 | 9.41 | 15.046 | 27 |
| g2 | 255 / 174 / 148 | 134.4 / 137.7 / 137.3 | 1.09 | 2.47 | 15.046 | 27 |

The iteration stops at the second step; the rule retains it.

## Consequences

- On the applied state (15 views byte-identical to the captures of the retained step): the box reads 134.4 / 137.7 / 137.3 for 136.9 / 137.6 / 137.9 in the original; its mean error falls from 39.50 to 1.09; E and the probes are unchanged (15.047 -> 15.046; 20 of 34, 2 of 4, 27 of 33, 3 of 3 in `linear`).
- **Side effect, visible.** The base colour is warm, so it tints every reflection that is not the blue sky. In the car selection carousel the windshield turns from sky blue to a neutral grey, and the headlights, which share the material, turn orange (`artifacts/pbr-handover/s24/judge/5-carrousel-vitres-avant-apres.png`); in the race a side window shows an orange patch where it does not reflect the sky. The pixels of the carousel that change go from a mean of 114.5 / 155.2 / 175.0 to 130.0 / 121.3 / 117.0. The author validated the carousel with its blue windshield before this change: this is for the author to judge on the sheet, and the decision is undone by reverting its commit.
- The calibration is tied to the sky of the race world and to one view; another sky calls for a new measurement.
- ADR-0033 fixed the grey 224 (decision D1): its status line points here. It is not rewritten.

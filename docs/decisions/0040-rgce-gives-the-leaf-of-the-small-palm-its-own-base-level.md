# ADR-0040: RacingGameCasaEngine gives the leaf of the small palm its own base level

- **Status**: Accepted
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G7 (rule of choice written before the measurement in a frozen script, applied mechanically; author's request of 2026-11-18 to continue after the verification of slices G1 to G6); the finding: verification of images of that plan (G6), advisory on slice G3; the measurement: `artifacts/pbr-handover/s25/g7/` (`measure_smallpalm.json`, `measure_smallpalm.txt`, `mark/`, one folder per candidate) and the frozen script `artifacts/pbr-handover/s25/scripts/measure_smallpalm.py`; ADR-0037

## Context

Facts verified on 2026-11-18:

- ADR-0037 gave the four leaf materials of the palms a base grey of 170 and the alpha cutoff 0.5, by a rule measured on four boxes of the reference views. None of those boxes contains the small palm (`AlphaPalmSmall`, material `orig 09 - Default`, texture `PalmLeave2.tga`).
- The independent verification of images found that this leaf was close to the original before ADR-0037 and moved away with it. Measured on its own pixels (21601 pixels of the 15 reference views, obtained by drawing the material alone as a marker): the original's mean colour is 83.8 / 98.3 / 57.7; with grey 170 the port's is 62.8 / 76.4 / 55.2.

## Decision

- **The leaf of the small palm takes a base grey of 220** (`RaceMaterialRecipes.SmallPalmLeafBaseLevel`, material designated by its texture `PalmLeave2.tga`); its alpha cutoff stays 0.5. The three leaf materials of the other palms (`PalmLeave.tga`) keep the grey 170 of ADR-0037.

Candidates, for this material alone: base grey 255 or 220 with the cutoff 0.35 or 0.5, and grey 190 with the cutoff 0.5, measured in `linear` on a copy of the build folder. Quantities (script `measure_smallpalm.py`): M, the mean difference per channel with the original over the pixels of the material; E, the mean difference per channel with the original over the 15 views. Rule, written before the measurement: eligible if criterion 3 of the probes is at least 27 and E is at most the start state's plus 0.01; the smallest M is retained and, within 1.0 of it, the highest grey, then the cutoff 0.5. The first row is the start state.

| Base grey | Alpha cutoff | M | Mean R / G / B over the mask | E | Criterion 3 |
|---|---|---|---|---|---|
| 170 | 0.5 | 26.80 | 62.8 / 76.4 / 55.2 | 15.046 | 27 |
| 255 | 0.35 | 25.72 | 85.1 / 106.5 / 64.5 | 15.047 | 27 |
| 255 | 0.5 | 25.94 | 86.2 / 107.5 / 65.2 | 15.044 | 27 |
| 220 | 0.35 | 22.58 | 74.9 / 93.4 / 60.0 | 15.043 | 27 |
| 220 | 0.5 | 22.63 | 76.0 / 94.4 / 60.7 | 15.039 | 27 |
| 190 | 0.5 | 24.38 | 67.8 / 83.4 / 57.2 | 15.042 | 27 |

The rule retains grey 220 with the cutoff 0.5: 220 with either cutoff is within 1.0 of the smallest M, and the cutoff 0.5 is preferred.

## Consequences

- On the applied state (15 views byte-identical to the captures of the retained candidate): M 26.80 -> 22.63; the mean colour of the leaf 62.8 / 76.4 / 55.2 -> 76.0 / 94.4 / 60.7 for 83.8 / 98.3 / 57.7 in the original; E 15.046 -> 15.039; the probes are unchanged.
- The state before ADR-0037 (grey 255, cutoff 0.35) is closer to the original in the hue and the saturation of the mean colour (hue 90.5 against 92.8 degrees for 81.4 in the original; saturation 0.394 against 0.357 for 0.413); grey 220 is closer in mean colour (mean difference per channel 4.92 against 5.42) and has the smaller M.
- Amends ADR-0037 for this one material: its status line points here. ADR-0037 is not rewritten.

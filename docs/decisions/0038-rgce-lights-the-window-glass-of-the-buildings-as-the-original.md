# ADR-0038: RacingGameCasaEngine lights the window glass of the buildings as the original does

- **Status**: Proposed
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G4 (rule of choice written before the measurement in a frozen script, applied mechanically; author's request of 2026-11-18 to continue in autonomous mode, subject "decor materials"); the measurement: `artifacts/pbr-handover/s24/g4/` (`measure_windows.txt`, `gate/measure_windows.txt`, one folder per marker and per candidate, `combined/`) and the frozen script `artifacts/pbr-handover/s23/scripts/measure_windows.py`; open point O7 of `ai-agent/tasks/rgce-render-gaps-tasks.md`; ADR-0028, ADR-0033, ADR-0036

## Context

Facts verified on 2026-11-18:

- The buildings have three window glass materials: `Hotel01` / `39`, `Hotel02` / `fenster`, `Casino01` / `39`, all of the family `BuildingGlass` (metallic 1, roughness 0.05), cooked with the imported diffuse colour as base colour (255 white, 94 / 162 / 231 blue, 64 grey).
- The original draws this glass as the reflection of the sky cube times a Fresnel term and 1.5, plus the material ambient, plus the diffuse and specular terms (`RacingGameOriginal/Core/Content/Shaders/NormalMapping.fx:642-654`). The reflection is not tinted by the diffuse colour, and it does not depend on the sun: the windows are light on faces turned away from the sun (the white stripes of the tower of `Beginner-sector-20`).
- In the port a metal's base colour tints its reflection, and a face turned away from the sun is in its own shadow, so the shadow colour of the world multiplies its reflection too: the window stripes were dark blue or black.
- ADR-0033 had written that the glass of the buildings "must not change" because a wall of the casino is black in the original (probe `building-away-1`, 14 / 13 / 12). Measured over all the pixels of each material in the 15 reference views (masks obtained by drawing each material alone as a marker), the port's casino glass was far from the original on the whole (M below), and that probe does not move with the retained state (26 / 24 / 23 before and after; criterion 1 still passes).

## Decision

Per material, by the rule below:

- **`Hotel01` / `39` no longer receives the shadow**; its base colour stays the imported white.
- **`Hotel02` / `fenster` no longer receives the shadow and takes the base colour grey 224**, the reflectance of the car glass (ADR-0033), instead of the imported blue.
- **`Casino01` / `39` takes the base colour grey 224** instead of the imported grey 64; it still receives the shadow.

`RaceMaterialRecipe` gains `ReceiveShadows` (true by default), which the cooker writes as `receive_shadows` and its verifier checks against the recipe; `RaceMaterialRecipes.ForScenery` designates the three materials by model and material name.

Candidates, for each material alone: the shadow received or not, crossed with the imported base colour or grey 224, measured in `linear` on a copy of the build folder. Quantities (script `measure_windows.py`): M, the mean difference per channel with the original over the pixels of the material in the 15 views; E, the mean difference per channel with the original over the 15 views. Rule, written before the measurement: a material whose mask has fewer than 500 pixels is left alone; a candidate is eligible if E is at most the start state's (15.199) and criteria 1, 3 and 4 of the probes do not fall; the smallest M is retained and, within 1.0 of it, the state closest to the start state.

| Material | Mask pixels | Receives the shadow | Base colour | M | E | Eligible | Retained |
|---|---|---|---|---|---|---|---|
| Hotel01 / 39 | 10848 | yes | 255 / 255 / 255 | 79.97 | 15.199 | yes |  |
| Hotel01 / 39 | 10848 | no | 255 / 255 / 255 | 74.64 | 15.195 | yes | **yes** |
| Hotel01 / 39 | 10848 | yes | 224 grey | 90.33 | 15.209 | no (E) |  |
| Hotel01 / 39 | 10848 | no | 224 grey | 85.93 | 15.206 | no (E) |  |
| Hotel02 / fenster | 11064 | yes | 94 / 162 / 231 | 146.27 | 15.199 | yes |  |
| Hotel02 / fenster | 11064 | no | 94 / 162 / 231 | 87.65 | 15.150 | yes |  |
| Hotel02 / fenster | 11064 | yes | 224 grey | 132.31 | 15.187 | yes |  |
| Hotel02 / fenster | 11064 | no | 224 grey | 47.35 | 15.116 | yes | **yes** |
| Casino01 / 39 | 8187 | yes | 64 / 64 / 64 | 133.17 | 15.199 | yes |  |
| Casino01 / 39 | 8187 | no | 64 / 64 / 64 | 133.09 | 15.199 | yes |  |
| Casino01 / 39 | 8187 | yes | 224 grey | 46.08 | 15.135 | yes | **yes** |
| Casino01 / 39 | 8187 | no | 224 grey | 45.87 | 15.135 | yes |  |

The three retained states applied together give E 15.047, with the probes unchanged, and the script retains the same three states on that combined capture.

## Consequences

- On the applied state (15 views byte-identical to the combined capture): E 15.199 -> 15.047 in `linear`, 19.959 -> 19.806 in `legacy`; the probes are unchanged (20 of 34, 2 of 4, 27 of 33, 3 of 3 in `linear`); M falls from 146.27 to 47.35 for `Hotel02`, from 133.17 to 46.08 for `Casino01`, from 79.97 to 74.64 for `Hotel01`.
- The window stripes are lit on faces turned away from the sun. They show the sky as the port's mirror reflects it, not the part of the sky the original's swapped reflection vector picked: `Hotel01` stays far from the original (M 74.64).
- A window that does not receive the shadow is not darkened by the shadow of another building either.
- The statement of ADR-0033 that the glass of the buildings does not change is amended by this record: its status line points here. ADR-0028 gives every imported material its imported colour: its status line points here for the two materials that take grey 224. Neither is rewritten.
- A cooked material's `receive_shadows` now comes from its recipe; the 96 other cooked materials keep `true`.

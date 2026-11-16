# ADR-0020: RacingGameCasaEngine names every screen element and every component

- **Status**: Accepted
- **Date**: 2026-10-09
- **Source**: this chantier: `ai-agent/tasks/rgce-element-naming-tasks.md`, decisions D1 to D4 (author answers, 2026-10-09), proposals P1 to P7 (validated with the plan), tasks T0.1 to T3.1; CasaEngine ADR-0122 (`CasaEngine/docs/decisions/0122-the-editor-names-the-elements-and-components-it-creates.md`) for the editor side

## Context

- The editor hierarchy of the screen assets showed dozens of unnamed `Image` and `Border` lines. Of the 275 elements of the 9 screens (`RacingGameCasaEngine/Content/UI/Screens/*/*.xaml`), 92 had a name, and 273 can be named (the two `RenderTransform` objects are not MGUI elements).
- MGUI indexes element names per window and throws `MGDuplicateElementNameException` on a duplicate, with an ordinal comparison.
- The names that the code looks up (`btn*`, `txt*`, `face*`) did not follow the naming convention of CasaEngine, which uses `lbl` for a TextBlock and `pnl` for a Border.
- The components of the test world `Worlds/Test/TrackBeginnerGenerators.world` and the components created by the game code (`RaceWorldFactory`, `RaceCarEntityFactory`, `LegacyTrackSceneFactory`, `CarSelectionCarousel`) kept the engine default name `Object <guid>`. The scene export wrote it in 5,118 of the 5,154 `ComponentPath` values.
- A cloned component lost its name (CasaEngine ADR-0122, P8).
- The author asked that every element of the UI, of every asset and of what an asset contains is named properly.

## Decision

- **D1, convention.** Screen elements follow the CasaEngine convention. The 92 existing names are harmonised, and the C# strings that look them up change in the same commit.
- **D2, scope.** The screens, the worlds (entities and components) and the components created by the code. The names of the materials and of the nodes and meshes of the cooked models do not change.
- **D3, ids.** An asset that is renamed gets the uuid5 of its new name as id. No asset is renamed here, so no id changes.
- **D4, editor.** The editor gives a default name to what it creates (CasaEngine ADR-0122). The validator does not warn.
- **P1, prefixes.** `wnd` Window; `pnl` OverlayPanel, StackPanel and Border; `img` Image; `lbl` TextBlock; `btn` Button.
- **P2, shape of a name.** The prefix, then the role in PascalCase, then an index when the role repeats. The role comes from the last segment of the sprite, from the bound path, or from the comment before the element. The elements shared by the menu screens have the same name everywhere (`wnd<Screen>`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `imgBackButton`, `imgSelectButton` and their highlights).
- **P3, existing names.** `btn*` is unchanged. `txt*` becomes `lbl*`, except `txtColorLabel` (`lblCarColor`), `txtLabel0..11` (`lblRow0..11`) and `txtName` (`lblPlayerName`). In MainMenu, `face<X>` becomes `pnl<X>Face` and `lbl<X>` becomes `img<X>Label`. 85 names change and 7 stay.
- **P4, components.** A component created by the code is named after its type (`Name = nameof(Type)`), like the editor default. The components already named by their role (anchors, `BodyVisual`, generated children) keep their name. The six root components of the test world are renamed the same way.
- **P5, clones.** The game components do not change: the central fix of CasaEngine (the three cloning sites copy the name) covers every form of `Clone()`.
- **P6, check.** `scripts/check_ui_element_names.py` checks the 9 screens and exits with code 1 on a deviation: an element without a name, a prefix that does not match its type, or a name used twice in a file. The `<X.Y>` property elements and `RenderTransform` are not counted.
- **P7, references.** The references taken before the naming (`artifacts/element-naming/before/`, ignored by git) are what every later capture is compared with: naming must not change a pixel.

## Consequences

- Every screen element has a unique name that shows in the editor hierarchy. The screens, their `.uiscreen` assets and the design files hold no element name or path, so nothing else changes.
- The code looks up the new names (`lbl*`, `lblCarColor`, `lblRow*`, `lblPlayerName`). A name that is looked up and missing leaves a warning or a load error in `--smoke-frontend`.
- The scene export no longer contains `Object <guid>` in the `ComponentPath` values written by this repository's code; the scene references become readable. A component created by CasaEngine itself would still show it. The comparison script keys neither the entity name nor `ComponentPath`, so the records compare unchanged.
- `scripts/check_ui_element_names.py` keeps the rule: a new screen element has a name, with the prefix of its type, unique in its screen.
- Naming changes no pixel. The `race-finished` state of the capture comparison differs from the references by 13.99 (3D background, not the interface), with or without the names: the references date from the CasaEngine pointer `7a5320b6a`, the submodule is now at `6ed0ef3f7` (open point O2 of the plan).
- The entity names, the sprite, sound and font names of the catalogue, and the names of the materials and of the nodes and meshes of the cooked models are unchanged.
- The default name `Object <guid>` of the engine remains for objects created by other games.
- The author's check of the hierarchy of the screens and components in the editor (O1) and the independent verification (T3.3) are still pending.

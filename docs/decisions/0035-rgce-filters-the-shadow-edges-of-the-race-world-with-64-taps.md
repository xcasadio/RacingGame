# ADR-0035: RacingGameCasaEngine filters the shadow edges of the race world with 64 taps

- **Status**: Proposed
- **Date**: 2026-11-18
- **Source**: this chantier: `ai-agent/tasks/rgce-shadow-colour-decor-tasks.md`, slice G1 (author's request of 2026-11-18 to continue in autonomous mode; the author chose the subject "shadow penumbra", which authorised the engine evolution); the engine setting: ADR-0127 of CasaEngine (`CasaEngine/docs/decisions/0127-the-tap-count-of-the-shadow-filter-is-a-setting.md`); the finding: open point O4 of `ai-agent/tasks/rgce-road-tunnel-shadow-tasks.md`; outputs: `artifacts/pbr-handover/s24/g1/`, trial `artifacts/pbr-handover/s23/taps/`

## Context

Facts verified on 2026-11-18:

- The race world softens its shadow edges with the engine's filter at a radius of 3 texels (`RaceWorldFactory.ShadowFilterRadius`). The filter averaged 12 binary depth comparisons on a fixed disc. Seen at a grazing angle, on the right wall of the Expert tunnel, the edge of the shadow showed as a fan of 3 to 4 translucent bands, visible since the engine fix of the shadow normal offset removed the speckle that hid it (verification of `rgce-road-tunnel-shadow-tasks.md`, S3). The original blurs its shadow in screen space and shows a gradient there.
- The engine now has `ShadowSettings.FilterTaps`: 12 (default, images unchanged), 32 or 64 reads, on a Vogel disc above 12 (CasaEngine ADR-0127, commit `1eacf5764`, verified).
- Trial of 2026-11-18 (engine shader edited for the trial, then restored): 32 reads still show finer bands on that wall; 64 give a smooth gradient (`artifacts/pbr-handover/s23/look/taps-expert-wall.png`).

## Decision

- **The race world sets `Shadows.FilterTaps` to 64** (`RaceWorldFactory.ShadowFilterTaps`). The filter radius stays 3 texels. The car selection carousel keeps the engine default.

## Consequences

- Against the previous reference (`artifacts/pbr-handover/s22/s2/ref`): the three tunnel views are byte-identical to the 64-tap trial; on the Expert tunnel 2.171 % of the pixels change by more than 8 levels; elsewhere only shadow edges change, at most 1.231 % of the pixels of a view (audit capture `Beginner-sector-20`), under the bound of 3 %; the six screens without a 3D world and the car selection are byte-identical.
- E, the mean difference per channel with the original over the 15 views, is unchanged: 15.734 in `linear` (15.732 before), 20.245 in `legacy` (20.249). Probes unchanged: 20 of 34, 0 of 4, 27 of 33, 3 of 3 in `linear`; 4 of 34, 2 of 4, 27 of 33, 3 of 3 in `legacy`. The speckle measure of the Expert wall is 0.304 % (0.328 % before; 0.363 % in the original). The median of the tunnel walls stays in the author's window (30.053, window 28.882 to 31.882).
- **Cost, not measured.** A lit pixel that receives the shadow does 64 shadow-map reads instead of 12. No frame time was measured: the game only runs in its automation modes here.
- Known limits: the engine's shader was built for the DesktopGL target only; the gradient is that of 64 fixed taps, not a blur.

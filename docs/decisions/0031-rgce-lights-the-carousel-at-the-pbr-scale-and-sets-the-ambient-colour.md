# ADR-0031: RacingGameCasaEngine lights the car selection carousel at the PBR scale and sets the ambient colour of both worlds to 0.05

- **Status**: Accepted
- **Date**: 2026-11-17
- **Source**: this chantier: `ai-agent/tasks/rgce-pbr-handover-tasks.md`, phase 13 (author's decision of 2026-11-17 on open point O13: the ambient colour of the race world goes from white to 0.05) and slice S4 (phase 16, decision taken in AUTO mode, to be confirmed by the author with this record), task T16.1, open point O2; `docs/decisions/0018-race-world-renders-in-linear-hdr-with-pbr-materials.md` (Decision, "Light (P4, P8)" and "Carousel (P5)"); the engine: `CasaEngine/docs/engine/pbr-rendering.md` (Units)

## Context

- ADR-0018 says the directional light of the race world and of the car selection carousel has an intensity of pi, and that both worlds take their environment from the sky cube (`AmbientSource.Auto`, ambient and specular intensity 1); the code of the other machine sets the ambient colour to white, which ADR-0018 does not state. Here, the race world got the pi light in slice S1g; the carousel was left at intensity 1 (`RacingGameCasaEngine/UI/CarSelectionCarousel.cs`), with no ambient setting, that is the engine's default colour 0.05.
- The ambient colour of the race world was changed from white to 0.05 in phase 13, on the author's decision after the measurement of open point O13, without an ADR: `lit-pbr` multiplies the irradiance of the sky cube by it, and white took the ambient from 5 % to 100 % of that irradiance.
- A `lit-pbr` surface lit head-on is pi times darker than a `lit-diffuse` surface at the same intensity (`pbr-rendering.md`, Units). The cars and the plates of the carousel are `lit-pbr` materials since slice S1f, so the carousel's central car was about half as bright as before PBR (the overall verification of the port, S2): the luminance of the central box of the car selection screen (x 640 to 1279, y 360 to 719 of the 1920x1080 capture) was 61.7 before PBR and 42.6 on `artifacts/pbr-handover/s3/ref-ui`.
- On the other machine the carousel is lit at pi with a white ambient colour and its handover note calls the result too white; this port had the opposite defect.

## Decision

- **The ambient colour of the race world and of the carousel is 0.05, written out.** `RaceWorldFactory.AmbientColor` (phase 13) and `CarSelectionCarousel.AmbientColor` are named constants equal to the engine's default, so that neither world depends on the default. This records the phase 13 decision for the race world and applies it to the carousel.
- **The light of the carousel is pi**, for the reason the race world's key light is: `CarSelectionCarousel.LightIntensity = MathF.PI`. The colour pipeline of the carousel stays the legacy chain (as ADR-0018 decides: its view needs a cut-out alpha that the tonemapping does not write); the sky cube environment and the other settings are unchanged. One value was tried, the target being the screen of the car selection before PBR; no adjustment by eye.

## Consequences

- Amends ADR-0018: its carousel light of pi, which was not applied here, now is, and the ambient colour of the two worlds, white in the code that ADR-0018 records, is 0.05 (the race world's change was made in phase 13). Its colour pipeline, materials and race world decisions are not changed here.
- The central car of the carousel comes back near its level before PBR; the figures are in `docs/racinggame-3d-capture-bench-local-baseline.md`, section "After the carousel light". Only the car selection screen changes among the interface captures; the race world is not touched.
- The glass and the rear lights of the cars stay black (open point O14), and the plates stay bluish, since the `CarouselPlate` family has metal 1 and reflects the sky.
- The automation mode captures the car selection with one car colour and one car in front; the other colours and each of the three cars in front are not captured, so they were not checked.
- The author's open point O2 (which intensity to aim for) is answered by this record: the level of the screen before PBR. The decision stays `Proposed` until the author confirms it.

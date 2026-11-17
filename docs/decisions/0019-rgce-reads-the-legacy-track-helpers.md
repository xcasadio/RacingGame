# ADR-0019: RacingGameCasaEngine reads the legacy track helpers and CombiModel items

- **Status**: Accepted; amended by ADR-0027, ADR-0029, ADR-0030
- **Date**: 2026-10-08
- **Source**: this chantier: `ai-agent/tasks/rgce-track-helpers-tasks.md`, decisions D1 to D3 (author answers, 2026-10-08), proposals P1 to P8 (validated with the plan), tasks T0.1 to T3.1; `ai-agent/tasks/rgce-editor-project-tasks.md`, decision D9 and open point O7

## Context

- The legacy `.Track` files name their helper items `WidthHelper` and `RoadHelper`, and the `.CombiModel` files name theirs `CombiObject`. The original game reads them with types of those names (`RacingGame.Shared/Tracks/TrackData.cs`, `TrackCombiModels.cs`).
- RacingGameCasaEngine looked these elements up by the names of its own classes (`LegacyWidthHelper`, `LegacyRoadHelper`, `LegacyCombiObject`), as its earlier `XmlSerializer` did. It therefore read none of them. ADR-0025 kept that behaviour for parity, and O7 recorded it.
- The game drew the tracks without:
  - the road widths of the width helpers (1 everywhere), with the rails, posts, checkpoint sizes and track physics that follow them;
  - the palms and laterns of the road helper spans;
  - the tunnels;
  - the 79 objects of the 10 CombiModel groups.
  It also placed 2 to 4 neutral objects per track that the original's duplicate filter removes once the CombiModel objects are placed first.
- The scenery's random sequence served the palms first, then the checkpoint banners and the signs, so reading the palms would have changed the models of the banners and signs.
- RacingGameCasaEngine had no tunnel code. The original builds the tunnels in `Track.cs` with its own material, `roadTunnelMaterial`.
- The author decided:
  - the fix comes after slice S5 and before S6, so S6 freezes the corrected scenery (D1, and D9 of the editor plan);
  - `scripts/TrackPlacementExporter` checks the result automatically against the original, and the author checks it visually (D2);
  - the palms draw from a random sequence of their own (D3).

## Decision

- **Reading.** `LegacyTrackFileReader` reads `WidthHelper`, `RoadHelper` and `CombiObject` under these names, as the original does. The C# class names do not change.
- **What the helpers produce.** The road takes the original's widths, and the CombiModel groups place their objects with the original's duplicate filter and scale. The road helper spans place the palms and laterns.
- **Palms.** The palms draw from a sequence seeded from the track name with the suffix `|LegacyTrackSceneFactory.Palms`. The checkpoint banners and the signs keep the existing `|LegacyTrackSceneFactory.Helpers` sequence, and their models.
- **Tunnels.**
  - `RaceRoadGeneratorComponent` builds a second generated model, `<track>.Tunnel`, with the original's geometry: 4 vertices per road point at height 7.125, normals pointing down, and 6 triangles per segment.
  - `RaceTrackLayout` computes the helper spans with or without the scenery (`TunnelSpans`).
  - The tunnel material is the cooked `Material.Track.RoadTunnel`, with the values of `roadTunnelMaterial`, drawn without culling. The generator holds its id as a constant, not as a serialized property, so saved worlds do not change.
- **Parity tooling.**
  - The track audit gains three views per track, at the palms, the laterns and the longest tunnel.
  - `TrackPlacementExporter` lists every unmatched live entity group, so it checks both directions. It strips the `StaticModel.` prefix that cooked models give to live model names. Its last road widths now blend exactly as the original's `TrackLine.AdjustRoadWidths`, whose third case repeats `Count - 2`.
- **References.** The parity references of the editor plan restart from `artifacts/o7-track-helpers/after/`. S6 compares against them.

## Consequences

- Complements ADR-0025, whose legacy file reading this record replaces: the reader no longer returns what `XmlSerializer` returned for the helper and CombiModel items.
- Against the original, nothing is missing, and the only extra scenery entities are the banners, signs, start light, palms and laterns, which the original places at run time.
- Each road point's width equals the original's to within 2e-6.
- The rendering changes on every track. The road is wider in places, and the scenery entities go from 108, 68 and 202 to 323, 265 and 729 (Advanced, Beginner, Expert). The track physics and checkpoint half widths follow the widths. The car audits did not change.
- The original draws its palms from a clock-seeded generator, so only their presence and side compare with RGCE's fixed palms.
- The tunnel comes out much darker than the original's. `lit-diffuse` multiplies the ambient term by the diffuse colour and the scene's ambient, while the original adds ambient to diffuse. Whether to keep, translate or fix this is open point O3 of the plan, for the author. A change goes through the `ForRoadTunnel` recipe and a new cooking pass; the worlds do not change.
- No performance reference exists. Expert now has 729 scenery entities and 2,829 drawn records.
- The author's visual check in the original game (T3.1) is still pending.

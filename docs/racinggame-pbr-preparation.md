# PBR preparation: what must be decided before the PBR work starts (2026-11-14)

Written while the author was away, after the direction "common base, then PBR" (2026-11-14). Nothing here is executed. It gathers what was verified, so that the PBR work can be planned and approved quickly.

Sources: a read-only check of `CasaEngine/ai-agent/tasks/pbr-rendering-implementation-plan.md` against the engine working tree (branch `chantier/racinggame-lighting`, during the shadow work), the reading of the original's effective material pipeline, and the bench findings (`docs/racinggame-3d-capture-bench-findings.md`).

## 1. The engine already has a PBR plan, and it cannot be executed as written

`CasaEngine/ai-agent/tasks/pbr-rendering-implementation-plan.md` (2026-08-09, decisions in CasaEngine ADR-0013, every task pending) covers: linear + HDR + tonemapping, a GGX `LitPbr.fx`, a `lit-pbr` material definition, image-based lighting, glTF import without degradation, a demo. Its decisions still hold (PBR per material next to `lit-diffuse`, metallic-roughness, forward only, colour pipeline chosen per world with `Legacy` as default, no PBR skinning in V1).

What makes it stale:

| Task | Stale statement | Current fact |
|---|---|---|
| PBR-000, 105, 602 | Reference captures "before any change" | ADR-0089 already changed the ambient of every textured lit material; the baseline must be pinned to a commit. |
| PBR-101 | Add the colour pipeline setting to `WorldEnvironmentSettings` | Five methods to update (reset, load, save, clone, copy), as ADR-0090 did; a test is required; per-view override semantics are not stated. |
| PBR-102 | Always render into the scene target in linear HDR | The scene target only exists when `FrameCapturePlanner` asks for it, its format is `Color`, and the UI is composed inside the view pipeline before the blit: "UI after tonemap" needs a structural change. Buffered virtual resolutions (ADR-0087) and frozen frames (ADR-0086) are not considered. |
| PBR-103 | Tonemap at the presenter | The only blit of the scene target is in `RenderPipeline`; `TexturePresenter` does not draw; the shader descriptor array is private. |
| PBR-104 | Decode sRGB for colour textures | No decode point exists; `SampleBaseTexture` now also multiplies the detail texture (ADR-0091); mipmaps (ADR-0092) are averaged in the stored space. |
| PBR-202 | Ambient = `AmbientColor × AmbientIntensity × occlusion`; sample shadows "exactly as LitForward" | The intensity is already folded into `AmbientColor`; the flat ambient source (ADR-0090) and the compose-once rule (ADR-0089) must be honoured; `HAS_FORWARD_SHADOW_BINDINGS` must be defined or shadows silently become 1; the shadow mode, tint, filter and fade of the shadow slice must be supported. |
| PBR-203 | Technique selection through `ShaderVariantLibrary` | LitForward techniques are chosen by the material; `EffectiveShaderResolver` falls back to LitForward for any unknown lit material, so a `lit-pbr` would silently render as Blinn-Phong without an explicit case; hot reload needs a new case. |
| PBR-301 | "Same pattern as `LitDiffuseMaterial.GetFeatures`" | No such method; features come from `GetShaderCapabilities` and `RenderFeatureResolver`. |
| PBR-302 | Register the definition and compile | Built-in factories receive counted texture handles (ADR-0037), registered ones do not; without a factory the compiler throws; key names must not collide with `lit-diffuse`. |
| PBR-401 to 403 | Keep the current flat ambient as fallback | The flat path now depends on `AmbientSource`; local reflection probes exist and are not addressed. |
| PBR-501 | Read the PBR channels of glTF | Only base colour and normal texture paths and the roughness factor are read; tangents are already read; alpha mode and double-sidedness are not read. |
| PBR-502 | Generate `.material` assets at import | Depends on a pending task of the glTF import migration plan. |
| Whole plan | Commit prefixes `engine(pbr)`, `shaders(pbr)`…; no branch rule, no ADR steps, no test for PBR-101, no open-points section | Conflicts with the engine's `AGENTS.md` (commit types, dedicated branch, ADR per decision, tests on serialization). |

Not covered by that plan, and needed by a game that switches to PBR: transparent PBR materials (glass), local reflection probes, skinned meshes, any post-process pass (bloom / glow), sprites and lens flare in an HDR frame, a performance budget.

## 2. Where glow and lens flare belong

The original's glow (`PostScreenGlow`) and lens flare (bench findings L2, L3) need a post-process stage the engine does not have: there is no post-process pass type, and the 3D pass list (`ForwardRenderPipeline`) has shadow, sky, opaque and transparent only. Phase 1 of the PBR plan (HDR scene target, tonemap) creates exactly the place where a bloom pass would sit. Glow should therefore be planned inside or right after that phase, not as a separate slice before it. One constraint found: today the UI is drawn inside the same target as the 3D scene, so a bloom at the blit would also bloom the HUD until composition is restructured (same change PBR-102 needs).

## 3. Decisions the author must take for RacingGame

1. **Rewrite or amend the engine PBR plan.** Recommended: a new plan from the engine's template that keeps ADR-0013's decisions and integrates the table above, rather than patching the old file.
2. **Colour pipeline of the race world.** Linear HDR with tonemapping changes every colour of the port. The capture bench stays valid for geometry, light direction and shadow placement; colour probes against the original lose their meaning. Decide whether the bench keeps a `Legacy` mode for regression checks.
3. **How the original's materials map to metallic-roughness.** The original gives no roughness and no metalness: the shininess is 24 for every mesh (the model files' value never reaches the GPU), and reflection is chosen by technique. A proposal to arbitrate, per family: terrain, road, masonry, signs and banners (dielectric, rough); guard rails and lamp posts (metal); car paint (clear-coat-like dielectric, or metal); glass (transparent, needs transparent PBR, which the engine plan leaves out); chrome parts of the car (metal, smooth).
4. **The port's specular tuning tables** (`RacingGameLegacyMaterialTuning`) become obsolete; decide whether they are deleted with the switch.

## 4. The original's accidental behaviours, to decide one by one

The author asked to decide these case by case. Each is something the original displays that its authors most likely did not intend. With PBR most of them disappear on their own; the question is whether any should be reproduced on purpose.

| # | Behaviour in the original | Evidence | With PBR |
|---|---|---|---|
| A1 | The shininess is 24 on every mesh; the value of each model file is parsed and never sent to the GPU. | `Core/Shaders/ShaderEffect.cs:825-843`, `Core/Graphics/Material.cs:37` | Replaced by roughness; nothing to reproduce unless a uniform highlight size is wanted. |
| A2 | The car body does not bind its own normal map: it inherits the last one bound, the road cement's. Derived from the draw order, not measured. | `Core/Graphics/Model.cs:645-662`, `Core/Tracks/Track.cs:1246` | The port binds the car's own normal map. Decide: keep the car's own map (intent) or reproduce the inherited one (image). |
| A3 | Signs, lamps and car paint read the sky cube without the axis swap the sky itself uses: their reflections do not match the visible sky. Glass reads it with the swap. | `Core/Content/Shaders/NormalMapping.fx:495-496, 695-696` against `:634` | Engine reflections follow the visible sky. Nothing to reproduce unless the mismatched look is wanted. |
| A4 | Reflection multiplies the surface colour by `0.85 + 0.75 × sky` instead of adding to it. | `NormalMapping.fx:503-531` | Replaced by image-based specular. |
| A5 | Glass buildings: the glass technique runs without blending state changes, so the panels are opaque and dark; a glass material without texture keeps the previously bound texture. Likely cause of the black casino wall of the bench. | `Core/Graphics/MeshRenderManager.cs:227-250`, `ShaderEffect.cs:838-841` | Decide: transparent or mirror glass (intent), or dark opaque panels (image). |
| A6 | Alpha test and its threshold are global state: road, rails, columns, tunnel and city ground use the value left by the previous frame's last model. | `MeshRenderManager.cs:219-222`, `Core/Graphics/Model.cs:733-736` | Per material in the engine. Nothing to reproduce. |
| A7 | The ambient override of signs, banners and windmills (128) is dead code. | `Core/Graphics/Model.cs:261-264` | Already aligned on the image (no override). |
| A8 | `AlphaTrain` is flagged alpha but its technique has no cut-out. | `NormalMapping.fx:503-531` | Decide when the model is used; no bench view shows it. |
| A9 | Models whose name starts with `alpha` (palms) never receive shadows, but a full-screen shadow mask darkens whatever is drawn in front of a shadowed receiver. | `Core/Graphics/Model.cs:862-867`, `Core/Shaders/ShadowMapBlur.cs:119-137, 225` | Independent of PBR; handled as a per-object approximation in the shadow slice. |
| A10 | Shadow casters are drawn two-sided without alpha test: palms cast solid card-shaped shadows. | `Core/Content/Shaders/ShadowMap.fx:122-130` | Independent of PBR. The port follows each material's alpha test. Decide: leaf-shaped shadows (port) or card-shaped (original). |
| A11 | The whole frame is darkened by about 7 %, shadowed or not: the weights of the shadow blur sum to 0.965 and the blurred mask is multiplied over the frame after two passes (0.965 x 0.965 = 0.931). | `Core/Content/Shaders/PostScreenShadowBlur.fx` (`Weights8`), `Core/Shaders/ShadowMapBlur.cs:128, 225` | Independent of PBR. The port does not darken. Decide: leave it (intent) or apply a global 0.931 (image); with a tonemapped PBR frame the question disappears into the exposure setting. |

## 5. Open measurements that PBR will not settle

- Solved since: the port's terrain looked about 7 % brighter than the original's because the original darkens its whole frame (A11).
- Enabling mipmaps slightly changed foliage coverage and road grain against the original, whose mipmaps are DXT-compressed by its content pipeline.
- A missing texture file is swallowed silently by the port's `LoadTexture`.

# MGUI and CasaEngine gaps met by the RacingGameCasaEngine XAML screens

- **Date**: 2026-10-05
- **Source**: plan `ai-agent/tasks/rgce-xaml-screens-tasks.md`, tasks T1.2 to T3.2 (validation notes), decisions D6, D14, D15 and open point O5; [ADR-0001](decisions/0001-rgce-screens-as-casaengine-screen-assets.md) and [ADR-0002](decisions/0002-rgce-content-is-a-casaengine-editor-project.md). M13 to M15: plan `ai-agent/tasks/rgce-main-menu-xna-look-tasks.md` and [ADR-0007](decisions/0007-rgce-main-menu-continuous-bevel.md). M16, M17 and C1 to C4: plan `ai-agent/tasks/rgce-title-car-selection-xna-look-tasks.md` and [ADR-0009](decisions/0009-rgce-title-and-car-selection-as-delivered.md).
- **Scope**: only what the migration of the nine RacingGameCasaEngine (RGCE) screens, and the later rework of the main menu (M13 to M15), actually hit, then the rework of the title screen and the car selection (M16, M17, C1 to C4). Each entry gives the evidence in the CasaEngine submodule (`CasaEngine/`, MGUI in `CasaEngine/MGUI/`), what RGCE does instead, and the related entry of the engine's own audit `CasaEngine/ai-agent/audits/mgui-gaps-from-xaml-screens.md` when there is one. Nothing here was changed in the submodules (D8).

## Summary

| Id | Gap | Met in | RGCE workaround |
|---|---|---|---|
| M1 | Visual states cannot target border thickness, border colour or text colour | Options, Help, Highscores | per-frame restyle in code |
| M2 | Focus does not restyle the focused element's children or siblings | band buttons | per-frame restyle in code |
| M3 | No event attributes in XAML | every button | `AddCommandHandler` in `OnWindowLoaded` |
| M4 | `Image` has no source rectangle | every atlas image | 45 generated `.sprite` assets |
| M5 | No indexer in binding paths, no proven collection binding | Help, Highscores, CarSelection, RaceHud | fixed slots bound to flat or nested properties |
| M6 | `RenderTransform.Rotation` has no bindable attribute | RaceHud needle | `TargetPathOverride=RenderTransform.Rotation` on `RenderTransformTranslation` |
| M7 | XAML `Background` sets only two brush slots; no colour-to-brush conversion | swatches, all `Background` attributes | whole brush bound by `TargetPathOverride=BackgroundBrush` |
| M8 | `TextBlock` draws its text lower than a draw transaction does, and lower than its own line box | RaceHud texts | `ClipToBounds="False"`; texts centred on their cells with a measured correction (D15) |
| M9 | Responsive text scale does not follow the UI scale | RaceHud texts | font sizes bound to values computed in code |
| M10 | An `Image` with an empty `SourceName` asks the catalogue for it | RaceHud digit slots | collapsed slots keep a valid sprite name |
| M11 | `ScrollViewer` has no `AllowClickDragScrolling` attribute | Help, Highscores, Options | none needed (default matches) |
| M12 | The runtime never calls `LoadDefaultResources`; screens are never disposed | every screen | `RaceXamlScreenBase` |
| M13 | No rounded border with a gradient | MainMenu button bevel | one 1-pixel band per pixel, each a solid docked border |
| M14 | Banded border bands are truncated to whole pixels | MainMenu button bevel | one band per pixel |
| M15 | The focused state is shown only in keyboard or pad navigation | MainMenu selection | selection owned by the screen; buttons not focusable |
| M16 | A bitmap font is drawn at its own size, whatever the font size | CarSelection GameFont texts | scaled at draw time by `RenderTransform` |
| M17 | `TextBlock` wraps by default and clips to its layout width | CarSelection GameFont texts | `WrapText="False"`, `ClipToBounds="False"` |
| C1 | Screen changes walk the views without a copy and push each screen into every view's UI | CarSelection carousel view | the game owns the view, adds or enables it after its update, removes its UI |
| C2 | `LitDiffuseMaterial`'s tint is never rendered | car colour, race and carousel | colour painted into the texture on the CPU |
| C3 | Lit shaders write the texture's alpha to the target | carousel render target | a view pipeline sets alpha to 1 over drawn geometry |
| C4 | A bitmap font's page is found by a `\`-separated catalogue file name only | GameFont | page catalogued with `\` |
| E1 | The editor preview is not run-time faithful | every screen | design size on full-screen windows; code-only states verified at run time |
| E2 | Design-time data cannot give an XNA `Color` | CarSelection | colours as XAML colour strings |
| E3 | The editor automation command line needs an entity and can hang | editor verification | anchor entity; bounded wait in the capture script |
| E4 | File > Save rewrites more than the screen | editor round trip | committed files in the editor's own form |

## MGUI

### M1. Visual states cannot target border thickness, border colour or text colour

**Evidence.** A visual-state setter resolves its path through the animation-target registry (`MGUI/MGUI.Core/UI/Animation/States/UIVisualState.cs:148-149, 212`). The registered targets are opacity, render transform, render scale, margin, padding, min height, preferred sizes, background overlay and gradients, border highlight progress, scroll offsets and a few control values (`UIBuiltInAnimationTargets.cs`, `UIExtraAnimationTargets.cs`). `BorderThickness`, the `BorderBrush` colour and a text block's `Foreground` are not among them.

**In RGCE.** The code-built menus changed exactly these on hover, focus and selection. Band buttons change border colour, thickness and label colour. Menu text buttons change border, thickness and opacity. The track selection changes frame border colour, thickness and label colour, and the car selection changes the swatch border. D6 keeps the exact look, so every screen finds these elements by name and restyles them every frame with the functions kept in `RacingGameCasaEngine/UI/LegacyMenuUiTheme.cs`. The main menu, the car selection and the track selection no longer do: their look follows the original XNA screens and is bound to their view models (ADR-0007, ADR-0009, ADR-0010).

**Would remove the workaround.** Registered targets for border thickness, border colour and text foreground, usable by XAML visual states.

### M2. Focus does not restyle the focused element's children or siblings

**Evidence.** An element shows its focused state only when it is itself the keyboard focus handler (`MGUI/MGUI.Core/UI/MGElement.cs:3969-3971`).

**In RGCE.** Band buttons recolour their label child, as part of the per-frame restyle of M1. The main menu had the same need (face, inner borders and a sibling label following the button) and now binds its selected look to its view model instead (ADR-0007).

**Would remove the workaround.** A way for a child or a named sibling to follow another element's visual state.

### M3. No event attributes in XAML

**Evidence.** The XAML has no click or command-handler attribute other than `Button.CommandName`, which registers at desktop level, where duplicate names are refused (plan, "État vérifié du dépôt").

**In RGCE.** Every action is attached in `OnWindowLoaded` with `FindControl<MGButton>(name).AddCommandHandler(...)`, as in CasaEngine's RPGDemo.

### M4. `Image` has no source rectangle

**Evidence.** `Image.SourceName` resolves only `sprite` and `anim2d` assets (`CasaEngine/Framework/UI/Backend/MonoGame/Assets/CasaUIAssetProvider.cs:129-157`). There is no attribute to crop a texture.

**In RGCE.** Every atlas rectangle the screens draw is a `.sprite` asset (45 sprites over 7 `.texture` assets), generated by `scripts/generate_rgce_ui_assets.py`: the rectangles of the old code, the main-menu labels of RacingGame's `UIRenderer`, the main-menu icon glyphs, which `scripts/MenuIconExtractor` extracts from `buttons.png` onto a transparent image, and the title screen, car selection and track selection art of `headers.png`, `ColorSelection.png`, `OptionsScreenWindows.png` and `buttons.png`.

### M5. No indexer in binding paths, no proven collection binding

**Evidence.** A binding path is resolved segment by segment as public property names (`MGUI/MGUI.Core/UI/DataBinding/DataBinding.cs:299-312`), so `Digits[0]` is not a path.

**In RGCE.** Repeated content became fixed XAML slots bound to flat or nested properties:
- Help: 6 sections × (title, 2 lines);
- Highscores: 10 lines;
- CarSelection: 4 stats and 11 swatches;
- TrackSelection: 3 tracks;
- RaceHud: lap, speed and gear digits, 5 best times, 5 race-finished lines.

The screens check that the catalogues still fit the slots. Slot counts are coupled to catalogue sizes (ADR-0001, consequences).

### M6. `RenderTransform.Rotation` has no bindable attribute

**Evidence.** Only `RenderTransformTranslation` and `RenderTransformScale` are mapped to the element's render transform (`MGUI/MGUI.Core/UI/XAML/Element.cs:1055-1072`, MGUI ADR-0020). Rotation and origin are not. This is the remainder of engine audit G9, fixed for translation and scale only.

**In RGCE.** The tachometer needle binds `RenderTransformTranslation="{dataBinding:MGBinding Path=NeedleRotation, TargetPathOverride=RenderTransform.Rotation}"`. The binding sits on an unrelated attribute and is redirected, which the editor cannot show (E1).

**Would remove the workaround.** A `RenderTransformRotation` attribute mapped like translation and scale.

### M7. XAML `Background` sets only two brush slots, and no colour converts to a brush

**Evidence.**
- The `Background` attribute writes the Normal and Focused slots of the element's existing brush (`MGUI/MGUI.Core/UI/XAML/Element.cs:1180-1189`). Code-built screens replaced the whole brush (`new VisualStateFillBrush(brush)`), all slots included.
- The only built-in binding converter turns a string into a `Color` (`DataBinding.cs:813-823`); nothing converts a bound `Color` into a brush.

**In RGCE.**
- RGCE loads no theme, so the default brush has no hover colour and the same 0.06 pressed darkening (`MGUI/MGUI.Core/UI/MGTheme.cs:690-696`). Literal backgrounds therefore look the same in every state these buttons use. Only the Selected and Disabled slots differ, and these buttons use neither.
- The car-colour swatches take their colour from the catalogue. Each binds its whole background brush, `Background="{dataBinding:MGBinding Path=SwatchNBrush, TargetPathOverride=BackgroundBrush}"`, built by `RaceCarSelectionViewModel` as the old code built it.

### M8. `TextBlock` draws its text lower than a draw transaction does, and lower than its own line box

**Evidence.**
- `MGTextBlock` draws at its layout position plus the font's draw origin times the scale, and also passes that draw origin to the text engine (`MGUI/MGUI.Core/UI/MGTextBlock.cs:1605-1640`).
- `CasaDrawTransaction.DrawShadowedText`, which the code-built HUD used, draws at the given position with the same origin and adds nothing (`CasaEngine/Framework/UI/Backend/MonoGame/CasaDrawTransaction.cs:272-287`).
- An element clips its content to its bounds by default (`MGElement.cs:3537`), so the lowered glyphs were cut at the bottom.
- With the FontStashSharp text engine that CasaEngine installs, the glyphs also sit low inside the text block's own line box. The line height is the tight height of the Arial sprite font (`MGUI/MGUI.FontStashSharp/FontStashSharpTextEngine.cs:375-379, 616-621`, `MGUI/MGUI.MonoGame.Integration/Text/FontSet.cs:108-121`), while the glyphs drawn are Tahoma's, with their baseline at the TTF ascent below the box top. Centring a text block, with `TextAlignment` and the default `VerticalContentAlignment="Center"` (`MGUI/MGUI.Core/UI/MGTextBlock.cs:1364, 1237-1248, 1567-1569`), therefore centres the line box, not the ink.
  - Bold digits, UI scale 1, measured: the ink centre is 7.5 px below the line-box centre at 26 pt, 9 px at 30 pt and 10 px at 38 pt.
  - All of it comes from these metrics. `MGTextBlock` adds the draw origin to its position, and the engine passes the same origin to FontStashSharp, which subtracts it again (`MGUI/MGUI.FontStashSharp/FontStashSharpTextEngine.cs:780-784`). The draw origin only explains the difference with `DrawShadowedText`.

**In RGCE.** The HUD text blocks, and the times panel that holds two of them, set `ClipToBounds="False"`. Each text is centred on its sprite cell, and a measured correction lifts the ink to the cell centre (D15).

### M9. Responsive text scale does not follow the UI scale

**Evidence.** The text scale is `clamp(UI scale × text multiplier, 0.85, 3)` (`MGUI/MGUI.Core/UI/Responsive/UIResponsiveResolver.cs:33-34`), a factor of its own.

**In RGCE.** The code-built HUD sized its texts from its panels, so they followed the UI scale. With `UseResponsiveTextScale` the HUD glyphs came out about 3 % narrower at 1920×1080. Kept fixed, they would overflow the panels at smaller UI scales.
- The HUD texts turn the responsive text scale off.
- Their `FontSize` is bound to `RaceHudViewModel.UpdateTextSizes`, which applies the old formula: `max(10, round(design size × scaled panel size / sprite size))`, the panel size scaled like MGUI scales it (`UIResponsiveMath.ScaleInt`).

### M10. An `Image` with an empty `SourceName` asks the catalogue for it

**Evidence.** The run log showed `CasaUIAssetProvider: cannot resolve UI image ''` (once per name) for collapsed digit slots whose bound name was still empty (T3.2, cycle 1).

**In RGCE.** A digit slot starts with the valid name `Ui.Hud.Digit0` while collapsed.

### M11. `ScrollViewer` has no `AllowClickDragScrolling` attribute

**Evidence.** The XAML `ScrollViewer` exposes scroll-bar visibilities and offsets only (`MGUI/MGUI.Core/UI/XAML/Controls.cs:2492-2511`).

**In RGCE.** The code-built screens set it to `false`, which is already the default of `MGScrollViewer`, so nothing is needed today.

### M12. The runtime never calls `LoadDefaultResources`, and screens are never disposed

**Evidence.**
- `UIRoot` never calls `MGDesktop.LoadDefaultResources`, whose check-mark texture check boxes draw (`CasaEngine/Framework/UI/UIRoot.cs:83-91`).
- `ScreenStack` never calls `Dispose` on a screen (`CasaEngine/Framework/UI/ScreenStack.cs:79-107`), and `XamlUIScreenBase.Dispose` is what takes a screen's bindings out of MGUI's static registry.

**In RGCE.** `RacingGameCasaEngine/Screens/RaceXamlScreenBase.cs` loads the default resources once before the first screen, and disposes the screen in `Hide`, which `ScreenStack` calls only on pop or removal.

### M13. No rounded border with a gradient

**Evidence.**
- A border brush whose fill is not solid draws rectangular strips on a rounded shape, so its corners are square and not clipped to the rounded silhouette (`MGUI/MGUI.Core/UI/Brushes/BorderBrushes/MGUniformBorderBrush.cs:103-107`).
- A docked border (one fill per side) keeps rounded corners only when its four sides are solid colours (`MGDockedBorderBrush.cs:204-214`).
- Fills, on the other hand, follow rounded shapes with a four-corner gradient (`MGUI/MGUI.Core/UI/Brushes/FillBrushes/MGGradientFillBrush.cs:63-91`).

**In RGCE.** The original menu buttons have a 10-pixel inner bevel that ramps from dark at the rim to the face colour, darker at the bottom than at the top. The main menu draws it as a `BandedBorderBrush` with one 1-pixel band per pixel of the bevel's thickness, each a docked border with one solid colour per side, interpolated from the art's profile at that depth. `RaceMainMenuButtonViewModel.BevelBrush` builds one per thickness. The ramp is continuous at pixel resolution; only the corners blend the colours of two sides.
- Pitfall met on the way: the bevel is an empty `Border` inside each button. With the button's default content alignment, that border collapses to twice its thickness at the centre of the button and draws as a small disc. The buttons therefore set `HorizontalContentAlignment` and `VerticalContentAlignment` to `Stretch`.

**Would remove the workaround.** A border brush that draws a gradient along a rounded ring, from its outer edge to its inner edge.

### M14. Banded border bands are truncated to whole pixels

**Evidence.** Each band of a `BandedBorderBrush` takes `(int)(total thickness × its weight / total weight)` pixels. The remainder is not drawn (`MGUI/MGUI.Core/UI/Brushes/BorderBrushes/MGBandedBorderBrush.cs:104-128`, the rounded-shape path).

**In RGCE.** The main-menu bevel has one band per pixel of its thickness, so each band's share is exactly one pixel. Where floating-point truncation would give a band 0 pixels (equal weights over 49 pixels), `BevelBrush` uses fewer bands.

**Would remove the workaround.** Distributing the remainder over the bands.

### M15. The focused state is shown only in keyboard or pad navigation

**Evidence.** An element shows its focused state only when it has the keyboard focus and the desktop's active input mode is not the pointer (`MGUI/MGUI.Core/UI/MGElement.cs:3969-3971`, `MGDesktop.cs:457`). The mode starts as the pointer and changes only on input (`MGDesktop.cs:183-193`), so a button focused by code when a screen opens is not drawn as focused, and moving the mouse hides the focused state again.

**In RGCE.** The original main menu always shows its selected button. `RacingGameCasaEngine/Screens/MainMenuScreen.cs` owns the selection instead of MGUI's focus.
- The keyboard, the pad and the mouse hover move the selection, and the screen binds the selected look to its view model.
- The buttons are not focusable, so MGUI's own arrow-key navigation does not move a second selection.

**Would remove the workaround.** An option to show the focused state whatever the input mode, or a selection model that a screen can drive.

### M16. A bitmap font is drawn at its own size, whatever the font size

**Evidence.** A static (bitmap) font registered for a family is resolved with `exactScale: 1` whatever the requested size (`MGUI/MGUI.FontStashSharp/FontStashSharpTextEngine.cs:538-556`).

**In RGCE.** RacingGame drew its GameFont texts stretched to the screen: glyph widths and advances by `XToRes1400`, heights by `YToRes1050` (`git show 4f840a3^:RacingGame.Shared/Graphics/TextureFont.cs:418-434`). `CarSelectionScreen` gives each GameFont text a `RenderTransform` of scale (width / 1400, height / 1050) with its origin at the top left, every frame. At 1920×1080, « Max Speed: 288mph » is then 405 px wide, as RacingGame's sum of advances.

**Would remove the workaround.** A size for bitmap fonts, or a non-uniform text scale.

### M17. `TextBlock` wraps by default and clips to its layout width

**Evidence.** `MGTextBlock.WrapText` takes the theme's `DefaultTextBlockWrapText` (`MGUI/MGUI.Core/UI/MGTextBlock.cs:1346`), and an element clips to its bounds by default (`MGElement.cs:3537`). A text laid out at its native size near the right edge of the window gets the remaining width only.

**In RGCE.** At 1024×768 the car selection's « Max Speed: 288mph », laid out at the native GameFont size from x 766, wrapped onto two lines, then was cut at « 288m » once wrapping was off, although the scaled text fits on the screen. Its GameFont texts set `WrapText="False"` and `ClipToBounds="False"`.

**Would remove the workaround.** Laying a text out at its render-transformed size, or an attribute to take no width constraint.

## CasaEngine runtime

### C1. Screen changes walk the views without a copy and push each screen into every view's UI

**Evidence.**
- `GameScreenManager.TransitionTo` walks `ViewManager.Views` directly, popping the current screen and pushing the new one into each view's UI runtime (`CasaEngine/CasaEngine/Framework/UI/GameScreenManager.cs:99-117`).
- `CasaEngineGame` gives every view added to the manager its own UI runtime (`Framework/Application/CasaEngineGame.cs:654-659`).
- A screen that added a view while it loaded made the transition throw « Collection was modified » (first carousel attempt). A screen that removed one while it was popped would do the same. A second view with a UI runtime would also receive a copy of every later screen.

**In RGCE.** The car selection's carousel view belongs to the game (`RacingGameCasaEngineGame.CarSelectionCarousel`), and the screen asks for it every update. The game adds the view after its own update, enables it while it is asked for and disables it otherwise, and adds it again after a world load, which clears every view. It also removes the view's UI runtime (`CarSelectionCarousel.EnsureView`).

**Would remove the workaround.** A transition that works on a copy of the views and only on views that show screens.

### C2. `LitDiffuseMaterial`'s tint is never rendered

**Evidence.** `LitDiffuseMaterial` declares `TintColor`, `TintStrength` and `TintMaskFromBaseAlpha` (`Framework/Materials/Runtime/LitDiffuseMaterial.cs:29-31`). Neither its `Bind` nor any shader reads them: `LitForward.fx` has no tint.

**In RGCE.** The selected car colour never showed in the race. `LegacyCarVisualFactory` now paints it into the car texture on the CPU, with RacingGame's formula `lerp(rgb, colour, texture alpha)`, in parallel, in about 10 to 16 ms per 2048×2048 texture.

**Would remove the workaround.** A tint in the lit shader, masked by the texture's alpha.

### C3. Lit shaders write the texture's alpha to the target

**Evidence.** The lit pixel shaders return the base texture's alpha times the diffuse alpha, and scale the specular by it (`CasaEngine/Content/Shaders/Lighting.fxh:9-12`, `AddSpecular`). Opaque items are drawn with `BlendState.Opaque`, which writes that alpha.

**In RGCE.** RacingGame's car texture keeps its paint mask in alpha, and RacingGame's shader also scaled the specular by it, so the alpha must stay. In the carousel's render target, cleared to transparent, the car came out see-through over the band. `OpaqueGeometryAlphaViewPipeline` wraps the view's pipeline: after the render, a screen quad just in front of the far plane writes alpha 1 wherever the depth buffer holds something nearer.

**Would remove the workaround.** A material or view option to write an opaque alpha.

### C4. A bitmap font's page is found by a `\`-separated catalogue file name only

**Evidence.** The font loader looks its page up by the path relative to the project root with `\` separators (`Framework/Assets/Fonts/BitmapFontDescriptor.cs`, `CatalogFileNameNextTo`), in a dictionary keyed by the catalogue's `file_name` as written (`Framework/Assets/AssetCatalog.cs:29, 44-48`). The RGCE catalogue writes its other file names with `/`.

**In RGCE.** `scripts/generate_rgce_gamefont.py` catalogues `UI\Fonts\GameFont.png` and `UI\Fonts\GameFont.fnt` with `\`.

**Would remove the workaround.** Normalised separators in catalogue lookups.

## CasaEngine editor

### E1. The editor preview is not run-time faithful (O5)

Verified in the editor discovery and in the per-screen editor captures (`scripts/capture_editor_screen.ps1`):
- **Window size.** A `Window` root is used as is (`CasaEngine.EditorServices/ScreenEditor/Preview/UIScreenPreviewBuilder.cs:117-120`), so a window sized only by its screen placement (`Stretch`) takes its content's size. The Splash panel fell outside the visible area. RGCE full-screen windows therefore declare a design size, `Width="1280" Height="720"`, which the game's `Stretch` placement replaces on every update (`MGUI/MGUI.Core/UI/MGWindow.cs:246-289`). The captures stayed identical.
- **Parsing and names.** The preview parses in Compatibility mode, not Strict, and renames every element `_cse_<id>`.
- **Envelope settings.** `theme_name` and `preview_resolution` are ignored.
- **Font.** The preview uses the editor font (JetBrainsMono), so texts are wider than in the game (Help, Highscores, RaceHud captures).
- **Screen code.** It never runs: the per-frame restyles (M1, M2), the code-applied values and the 3D car carousel are absent.

### E2. Design-time data cannot give an XNA `Color`

**Evidence.** Design-time values are applied with `JsonConvert.PopulateObject`. The first CarSelection preview showed in its status line that a JSON object cannot be deserialized into `Microsoft.Xna.Framework.Color`, which Newtonsoft treats as a string-converted type. Which converter applies then depends on MGUI's global registration (`DataBinding.cs:813-832`) having run.

**In RGCE.** Swatch colours are XAML colour strings (`rgba(r,g,b,a)`), parsed by `XNAColorStringConverter.ParseColor`. The game itself passes the exact catalogue `Color`.

### E3. The editor automation command line needs an entity and can hang

**Evidence.**
- The automation only proceeds once the start world holds an entity (`CasaEngine.Editor/GameEditor.cs:7754-7768`).
- An asset that cannot be opened, followed by `--set-screen-property`, makes it wait forever (`GameEditor.cs:7092-7093, 6464-6469, 7210-7215`).
- Its diagnostics file does not carry `Logs.*` warnings, so a design-time data error shows only in the screenshot's status line.
- Every project open leaves a shadow copy of the gameplay assembly under `%TEMP%\casaeditor-scripts\`, never deleted.

**In RGCE.**
- `Worlds/Editor.world` holds the empty entity `EditorAutomationAnchor`.
- The capture script bounds the wait (`-TimeoutSeconds`) and stops only the process it started.
- Status lines are checked on the captures.

### E4. File > Save rewrites more than the screen

**Evidence.** Save rewrites the current world, the project file and `AssetInfos.json` (`GameEditor.cs:2278-2304`). The world is written with every entity policy, the full default environment, and `"root_component": "null"` as a string (`CasaEngine.EditorServices/EditorEntityJsonSerializer.cs:50-81`, `"null"` at line 67).

**In RGCE.** The committed project file and start world are the editor's own output, so a Save leaves them unchanged (T5.2). The edited XAML element's start tag is rewritten on one line.

## Race HUD text rendering (D15)

> These measurements describe the HUD of the XAML migration. The HUD was doubled afterwards, with its font sizes (plan `ai-agent/tasks/rgce-hud-shadows-start-tasks.md`, T1.1).
>
> Since then, the texts no longer keep the code-built positions: the author asked for them to be centred in their containers. Each text is now centred on the empty area of its sprite cell, measured in `ingame.png`:
> - the time column (x 113 to 341) of each times row;
> - the track-name header body;
> - the rank cell and the time cell of each best-times row.
>
> A bottom padding of about twice the drop measured in M8 lifts the ink to the cell centre. The times are the exception: their line box is too tall for that, so their line is placed from its top instead. Measured on captures at 1920×1080, 1600×900, 1440×810 and 1280×720, every text's ink centre is within 2 px of its cell centre, vertically and horizontally. The ink measured is that of the digits and capital letters; a descender, such as the g of "Beginner", hangs below.

The HUD texts were drawn by `DrawShadowedText`; they are now bold shadowed `TextBlock`s (offset 1,1, colour `rgba(0,0,0,191)`, the old values). The measurements below are text ink boxes compared with `references-579e1d7`, in 1920×1080 at UI scale 1, on the race-finished capture; the race HUD capture gives the same sizes and offsets.

| Text | Size (design → drawn) | Ink width | Ink height | Vertical offset |
|---|---|---|---|---|
| current lap time | 38 → 19 | 89 → 90 px | 20 → 20 px | +3 px |
| best lap time | 38 → 19 | 88 → 88 px | 20 → 20 px | +3 px |
| best five, time (row 1) | 30 → 15 | 71 → 71 px | 16 → 16 px | +3 px |
| best five, rank (row 1) | 30 → 15 | 13 → 13 px | 16 → 16 px | +3 px |
| track name | 26 → 13 | 67 → 67 px | 16 → 16 px | +2 px |

- **Size and clipping.** Same size and glyphs, nothing clipped (M8, M9).
- **Offset.** The only difference is the text being 2 to 3 px lower, the draw-origin offset of M8. In the times panel the best lap time therefore ends 2 px below the panel art.
- **Mean colour difference per panel** (0-255 per channel), race-finished state:
  - times panel 21.8/18.4/13.9 and best-five panel 22.9/22.8/22.8, from the shifted text;
  - laps panel 4.0/3.7/2.8 and tachometer 2.1, from the race scenery seen through the panels (plan O4);
  - race-finished panel 0.00.
- **Whole image.** `race-hud` 0.53/0.51/0.48 and `race-finished` 0.49/0.47/0.44, under the 2.0 threshold.
- **Removing the offset** would take either a text block that draws at its layout position like a draw transaction, or an offset compensation per font size. The HUD measured here accepted the difference; the centred HUD now compensates per font size (note above).

## Post Screen Effects (D14)

The Options screen keeps the "Post Screen Effects" check box, saved as `EnablePostEffects` in the front-end options. Nothing in RGCE reads it apart from the Options screen and its persistence (`RacingGameCasaEngine/Screens/OptionsScreen.cs`, `Persistence/FrontEndOptionsPersistence.cs`).
- **In RacingGame** the option drove the `PostScreenMenu` and `PostScreenGlow` shader effects (`RacingGame.Shared/Shaders/PostScreenMenu.cs:9`, `PostScreenGlow.cs:12`).
- **In CasaEngine** the screen-effect service offers only a colour overlay and fades (`CasaEngine/Framework/Rendering/ScreenEffects/ScreenEffectService.cs:74-164`), so there is no counterpart to drive.
- **Decision.** The option stays, without effect (D14).

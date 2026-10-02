# Tour reconstruction inspection: validation and authoring

Updated 2026-10-02. Implemented in `VR_Tour_Scene` using Unity 6000.3.16f1. Editor checks passed; physical Quest and packaged-build validation are outstanding.

## Delivered behavior

At a configured tour point, press **left Y** to load its restored reconstruction. No inspection globe or placeholder spawns. The model appears in front of the opening view, then the player smoothly approaches it over 0.4 seconds without changing height or facing. The model stays in the world, with the museum's ray rotation interaction and a localized inspection panel to the user's right.

Press Y again, X, or the panel's Close button to end inspection. Y and X also cancel loading or the approach. A/B and Smaller/Larger scale the inspected model. There are no alternate models or model-switching controls. Closing restores the exact pre-inspection rig position/rotation and navigation arrows. Opening the map, leaving a node, returning to the tour menu, disabling the presenter, or unloading the scene cancels inspection and releases its content.

Y is unavailable at the tour menu, over the map, during node transitions, on unconfigured nodes, or while the presenter is disabled. A failed load returns to Ready so Y can retry. Controller hints describe Inspect ruins, Cancel loading, or Exit inspection in English and Thai, and hide when no inspection action is available.

Throughout this scene, the left controller's Y-button surface gently pulses between the canvas's brown (`#533A2D`) and gold (`#BB925C`) over a 1.6-second cycle. The texture, Y lettering, button animation, and other controller surfaces are retained. The effect is configured on both existing left visual rigs for Quest/Rift S, Quest 2, Rift, Touch Pro, and Touch Plus (Quest 3/3S). It follows the SDK's existing controller visibility; hidden controllers are not forced visible. Disabling the highlight restores the original meshes/materials. No controller assets in other scenes were changed.

During inspection, the panorama inside `SphereContainer` smoothly fades over 0.3 seconds toward warm brown (`#533A2D`). At the default 0.85 strength, 15% of the photo remains visible. Models, controllers, and UI are unaffected. Closing restores the original panorama fade and tint.

## Initial content and tuning

| Area / starting point | Restored source | Reconstruction definition |
| --- | --- | --- |
| 2 / `2F1` | `OG_BJ2.prefab` | `OG_BJ2.asset` |
| 3 / `3F1` | `OG_BJ3.prefab` | `OG_BJ3.asset` |
| 8 / `8F1` | `OG_BJ8.prefab` | `OG_BJ8.asset` |
| 9 / `9F1` | `OG_BW9.prefab` | `OG_BW9.asset` |

Definitions are in `Assets/_LangkaLens/Tour/VR Tour Data/Reconstruction/`. Inspection displays and the panel are in `Assets/_LangkaLens/Tour/3D Models/Prefabs/Reconstruction/`. Displays are registered in the local Addressables group `Tour Reconstructions`. The selected `TourArea` supplies the reconstruction for its nodes.

On the `Tour Reconstruction Inspection` scene object:

- Presenter → **Inspection approach → Inspection Viewing Distance**: desired horizontal eye-to-model-centre distance, default 0.8 m. Maximum-scale renderer bounds provide extra clearance. Only the rig moves, with no backward approach.
- Inspection manager → **Movement Settings → Transition Duration**: approach time, default 0.4 s.
- Presenter → **Inspection panel → Inspection Panel Offset**: default `(1.2, -0.2, 1.2)` m relative to the final eye position and opening horizontal view. This places the panel 45 degrees to the right, facing the user. It stays fixed during model rotation.
- Presenter → **Inspection backdrop**: Panorama Fade 0.85, Panorama Fade Duration 0.3 s, and Panorama Fade Color brown `#533A2D`.
- **Tour Inspection Button Highlight**: Brown, Gold, and Blink Period control the Y-button pulse.

Each definition controls model size, initial viewing distance, and vertical offset. Defaults fit the largest dimension to 0.65 m and spawn the model 1.2 m along the opening horizontal view, 0.2 m below it. Model zoom allows 0.5–1.5 times the initial size.

Node icon positions and **Sync from 'IconDummy'** no longer control reconstruction inspection: it opens relative to the user's current view. Legacy node overlays retain their existing authoring path. The old globe wrapper asset is retained for compatibility and is not referenced by the presenter or spawned by this feature.

English and Thai descriptions distinguish archaeological evidence from restored-model interpretation for Ban Jalae 2, 3 and 8, and Ban Wat 9. See [book references and claim mapping](TOUR_RECONSTRUCTION_SOURCES.md).

## Implementation notes

`TourReconstructionPresenter.ToggleInspection()` is the shared entry point for the left Y press and validation. Node entry marks content Ready without spawning anything. A new request captures a generation; close, node change, or disable invalidates it. Late successful Addressables loads release their instances. Failed operations also release their handles and return to a state that permits retry.

`InsideOut360` uses a single texture sample and blends its RGB toward `_InspectionFadeColor` using `_InspectionFade`. The presenter uses a renderer property block, preserving other properties and shared materials. Closing smoothly restores the previous fade strength and color; node cleanup and disable restore immediately. Existing serialized blur strength/duration migrate to fade fields.

The button setup splits only rigid Y-button triangles into an additional submesh on project-owned copies of the five SDK meshes. Vertex positions, UVs, weights, and bind poses are unchanged. Separate emissive materials retain the original base texture. Their emission flags preserve the URP keyword across reloads. The runtime pulse changes only the Y submesh's property block; it creates no material or mesh instances each frame. There is one additional submesh draw per visible highlighted left controller renderer. Device cost has not been measured.

## Editor evidence

| Check | Result | What it establishes |
| --- | --- | --- |
| Unity compilation | Passed | Modified runtime and Editor scripts compiled. |
| Reconstruction assets | 126 checks passed | Four definitions, Addressables entries, model anchors, rotation wiring, scene/panel references, no alternates, translations, fade shader compilation, ten left-controller visual bindings, unchanged controller vertex/triangle counts, and emissive Y materials. |
| Tour Play Mode | 188 checks passed | Y opens/closes and cancels loading; all four area menu paths; no reconstruction placeholders; approach/restore; SDK rotation; right-side panel; scaling/localization/close; panorama fade/tint restore; menu/map/unconfigured/disabled guards; cancellation and rapid reopen; missing content retry; cleanup during a grab; node changes; scene unload; changing button color over time; emission after reload; and original controller mesh/material restoration on highlight disable. |
| Controller hints | Passed across all four scenes | State bindings and English/Thai translation, glyph and layout checks, including Inspect ruins, Cancel loading, and Y Exit inspection. Museum alternate-model hints and MR site-info hints retain their mappings. |
| Museum Play Mode | 7 checks passed | Museum glide, alternate toggle, panel behavior, and rig/model/UI restoration. |
| Camera preview | Visually inspected in Editor | Quest Touch Plus Y surface becomes warm gold while the Y lettering remains visible and the other buttons retain their appearance. Previous inspection previews established the brown panorama fade and sharp model/right-side panel. |

The SDK rotation checks send pointer events to the actual Grabbable/OneGrabFreeRotateTransformer, and panel checks invoke bound callbacks. Y tests call the same ToggleInspection method used by the raw input binding. These checks do not simulate physical controller presses or prove headset visibility and comfort.

The deliberately invalid Addressable key is captured as an expected failure, followed by recovery. Cleanup observes no leftover reconstruction objects; no memory profiling was performed. The Editor also reports existing Meta entitlement/OpenXR/tracking environment warnings, so the console is not globally error-free.

## Repeat the checks and configure content

- Assign a containing TourArea's Reconstruction field to a definition. Enter Play Mode, select the area, and press left Y after arrival.
- **LangkaLens → Validation → Tour Reconstruction Assets** runs outside Play Mode in the saved tour scene.
- **Tour Reconstruction Play Mode** runs in Play Mode and finishes by loading the museum. **Museum Inspection Play Mode** runs in that scene.
- **LangkaLens → Validation → Controller Hints** runs outside Play Mode with saved scenes.
- **LangkaLens → Tour → Configure Y Inspection Button** refreshes only controller highlight meshes/materials and the Y hint translations. Save the scene afterward. Do this when changing the tour's controller visual rigs; update validation's ten-binding expectation if intentionally adding or removing rigs.
- **Set Up Restored Reconstruction Inspection** rebuilds generated displays and the panel, configures area definitions, and includes the Y-button setup. Avoid rerunning it after customizing those assets unless regeneration is intended.
- **Update Reconstruction Descriptions** reapplies curated English/Thai content without rebuilding model or scene assets.

The asset fixture expects exactly four configured areas. Update that expectation when adding areas.

## Remaining device validation

No APK/player build, Addressables player-content build, physical controller/hand test, or CPU/GPU/memory profiling was performed. Validate left Y open/close/cancel, controller visibility and pulse on the target Quest, ray rotation, A/B and X priority, seated/standing approach comfort, panel readability, packaged loading, repeated-open memory behavior, stereo fade appearance, and frame time before shipping.

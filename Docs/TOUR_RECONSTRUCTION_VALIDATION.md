# Tour reconstruction inspection: validation and authoring

Updated 2026-10-02. Implemented in `VR_Tour_Scene` using Unity 6000.3.16f1. Editor checks passed; physical Quest and packaged-build validation are outstanding.

## Delivered behavior

Arriving at a configured tour point spawns a selectable red globe using `Assets/_LangkaLens/Tour/3D Models/Prefabs/globeRED.prefab`. Hover highlights it; selection loads one restored reconstruction in front of the current view, then smoothly moves the player closer over 0.4 seconds without changing height or facing. The model remains in the world, with the museum's ray rotation interaction and a localized inspection panel to the user's right. During inspection, the panorama inside `SphereContainer` smoothly loses focus over 0.3 seconds; its brightness is retained and the model/UI stay sharp.

Select and move the model to rotate it. A/B or the Smaller/Larger panel buttons scale it. X or Close ends inspection; X also cancels a pending load or approach. There are no alternate models, comparison controls, or model-switching hints. Navigation and tour zoom pause while loading or inspecting. Closing cancels any movement and restores the exact pre-inspection rig position/rotation, marker and arrows. Opening the map, leaving a node, returning to the tour menu, disabling the presenter, or unloading the scene clears inspection content.

## Initial content

| Tour point | Restored source | Reconstruction definition |
| --- | --- | --- |
| `2F1` | `OG_BJ2.prefab` | `OG_BJ2.asset` |
| `3F1` | `OG_BJ3.prefab` | `OG_BJ3.asset` |
| `8F1` | `OG_BJ8.prefab` | `OG_BJ8.asset` |
| `9F1` | `OG_BW9.prefab` | `OG_BW9.asset` |

Definitions are in `Assets/_LangkaLens/Tour/VR Tour Data/Reconstruction/`. Inspection displays, the marker, and panel are in `Assets/_LangkaLens/Tour/3D Models/Prefabs/Reconstruction/`. The four displays are registered in the local Addressables group `Tour Reconstructions`. **Panorama Blur** (default 0.85) and **Panorama Blur Duration** (default 0.3 s) are editable on the `Tour Reconstruction Inspection` scene object's presenter under **Inspection backdrop**.

Markers use each node's icon position, rotation and scale. An unset `(0, 0, 0)` position, or one too close to the eye, falls back to 2 m in front of the opening horizontal view and 0.2 m below it. Distant positions retain their bearing but are brought within 3 m of the eye, adjustable using **Placeholder placement → Placeholder Max Distance** on the presenter. Valid nearby authored positions remain unchanged. Each definition displays a model with a largest dimension of 0.65 m before rotation, positioned 1.2 m along the head's horizontal forward direction and 0.2 m below it. Scale controls allow 0.5–1.5 times the initial size. These are starting values for device comfort testing.

The globe is centered and fitted to a 0.45 m diameter, with its original mesh/material retained inside the marker wrapper. The source prefab is unchanged. Its original hover-overlay/drag components are removed from the nested marker instance so the wrapper is the only selection owner.

The presenter targets a horizontal eye-to-model-centre distance of 0.8 m, increased when model bounds at maximum zoom need more clearance. It only moves closer, never backward, and moves the rig rather than the tracked eye transform. Tune **Inspection approach → Inspection Viewing Distance** on the presenter and **Movement Settings → Transition Duration** on its inspection manager. The original stationary-inspection API remains available, and the museum keeps its existing approach/rotation flow.

The information panel appears after the approach at `(1.2, -0.2, 1.2)` metres relative to the final eye position and opening horizontal view: 45 degrees to the right, angled toward the user. It stays in the world and does not rotate with the model. Tune **Inspection panel → Inspection Panel Offset** on `Tour Reconstruction Inspection`. The panel is 0.8 × 0.6 m; the largest model bounding-radius estimate at maximum scale is approximately 0.84 m. At the default panel angle, its left edge is approximately 0.92 m right of the model center, leaving clearance even when the model rotates.

The confirmed source model prefabs and `VR_Museum_Scene` were not modified. Existing museum inspection retains player glide, alternate-model switching, and rig restoration. Nodes without reconstruction definitions retain their existing content path. English and Thai strings in `VTour_UI` now identify Ban Jalae 2, 3 and 8, and Ban Wat 9, with archaeological summaries and a separate restored-model interpretation paragraph. See [book references and claim mapping](TOUR_RECONSTRUCTION_SOURCES.md).

The existing `InsideOut360` shader now supports `_InspectionBlur`, defaulting to zero. It samples the photos' existing prefiltered mip pyramid and explicitly interpolates levels so bilinear texture filtering does not cause focus changes to step. Only the tour's panorama renderer receives a temporary property override; no camera-wide depth-of-field, extra render targets, generated blurred images or material instances are added. The image texture, brightness and warp transforms remain intact. Closing smoothly restores the previous blur value; clearing a node or disabling immediately restores it so the next panorama cannot inherit the blur. All 80 tour photos require mipmaps, checked by asset validation. Legacy serialized fade strength/duration migrate to the new blur fields; the shader retains its old fade property for material compatibility but does not use it to darken the image. GPU cost and stereo appearance still require Quest validation.

## Editor evidence

| Check | Result | What it establishes |
| --- | --- | --- |
| Unity compilation | Passed | New and modified C# compiled in the running Editor. |
| Reconstruction asset validation | 177 checks passed (2026-10-02) | Four definitions, Addressables entries, model anchors and rotation targets, scene and panel references, no alternates, no missing scripts in display prefabs, translations, inspection hint mappings, panorama blur shader support/compilation, mipmaps on all 80 tour photos, globeRED source, and a single marker ray interaction. |
| Tour Play Mode validation | 162 checks passed (2026-10-02) | All four starting points through their actual area-menu buttons; active markers outside the eye and within readable distance; unset, distant and nearby authored placement regressions; preserved authored bearings; hover versus selection; repeated selection; smooth player approach with unchanged height/facing and a fixed model; exact rig restoration on close; cancellation during approach with no later motion; SDK rotation without translation; right-side panel placement/orientation independent of model rotation; panel scaling and close; panorama blur/restoration without changing the shared material; blocked/restored navigation; cancellation and rapid reopen; cleanup during an active grab; map visibility; deliberate invalid-key recovery; leaving during a load; unconfigured node; disabling/re-enabling; menu return; and actual scene unload into the museum. |
| Museum Play Mode regression | 7 checks passed (2026-10-02) | Museum mode, glide, panel and model toggle, exact rig position/rotation restoration, and primary model/UI reset after extending the shared inspection manager. |
| English/Thai panel checks | Included in tour suite | Nonempty localized text, no detected text overflow, and supported visible glyphs after switching locales. |
| Camera preview | Visually inspected in Editor | BJ3's panorama was visibly out of focus during inspection, while model details and panel text stayed sharp and the background retained brightness. Closing restored the detailed panorama with `_InspectionBlur` back to zero. This does not establish comfort or GPU cost on a headset. |
| Globe label layout | 6 localized states checked | English/Thai open, loading, and retry strings fit the 1.2 × 0.2 m label area; measured heights were 0.08 m. The previous oversized font was corrected. |
| Book-based titles and descriptions | All 16 title/body layouts checked | Four sites in both languages fit without missing visible glyphs. The 30-point body font is retained; the description area is 710 × 360, with measured text heights of 273–351 units. |
| Historical-content preview | Visually inspected in Play Mode | The live Thai BJ3 and English BJ8 descriptions, including the bold restored-model labels, fit the right-side panel without touching the title or scale buttons. |
| Closer inspection preview | Visually inspected in Play Mode | BJ3's rig approached about 0.37 m, reducing horizontal model-centre distance from 1.2 m to about 0.83 m. The model stayed fixed, the panel stayed to the final viewpoint's right, and close restored the rig/eye to their exact initial positions. |

The SDK rotation test sends pointer events to the actual `Grabbable`/`OneGrabFreeRotateTransformer`. Panel tests invoke their bound button callbacks. They establish component wiring and behavior, but do not simulate physical controller trigger presses, tracked hands, or all ray hit conditions. The same-frame menu guard is checked directly; a real X press still needs device validation.

The failed-load case deliberately supplies an invalid Addressable key, captures the expected exception, and verifies a selectable retry marker. Successful loads retain their Addressables handles; canceled/stale completions release their instances. Cleanup checks observe no leftover reconstruction objects. Memory profiling was not performed, so these checks are not a measured proof of zero memory leakage.

The Editor console also reported Meta entitlement failure, unavailable OpenXR/tracking-origin setup, and Meta bridge ports already in use. Those platform/environment messages prevent describing the console as globally error-free. The final suite reported no feature assertion failures. The existing tour keyboard fallback was adapted to the project's enabled Input System after Editor testing exposed legacy input exceptions.

The Editor was returned to the saved `VR_Tour_Scene`, outside Play Mode.

After the area/icon refactor, the marker still spawned but the default icon position placed it at the eye for BJ2, BJ8 and BW9. BJ3's authored position `(7.92, 0, 12.47)` was 14.77 m away, giving a tiny marker at the panorama wall. Runtime placement now provides the visible fallback and maximum distance described above without rewriting saved node placement. Before/after camera previews confirmed the visibility regression and recovery; BJ3's corrected position was `(1.61, 0, 2.53)`, with a successful collider ray hit. Renderer bounds are used during placement because a newly moved collider's physics bounds can be stale until synchronization. Tests synchronize transforms before querying hitboxes. Physical Quest and packaged-build checks remain outstanding.

## Place or add a marker

1. Open `VR_Tour_Scene` and select a `TourNode` asset.
2. Assign the containing `TourArea` asset's Reconstruction field to one of the four definitions. The selected area's definition is shared by its tour nodes.
3. Use **Setup Scene for this Node** in the Tour Node inspector. Move, rotate, or scale `IconDummy` against the panorama.
4. Press **Sync from 'IconDummy'**, then **Revert Scene to Default** to remove the preview. Save the node asset. Nearby saved positions are used directly; distant positions retain their bearing and are brought within the presenter's maximum distance.
5. Enter Play Mode, visit that point, and verify the marker is visible and reachable with the ray.

The authoring preview is tagged `EditorOnly` and disables itself during Editor Play Mode. Runtime markers are spawned independently by the presenter. Presentation distance, initial size, and vertical offset can be adjusted on each shared reconstruction definition; changes affect every node referencing it.

The current asset-validation fixture expects exactly four configured areas. If more areas are deliberately added, update that expectation in `TourReconstructionValidation.ValidateAssets`.

## Repeat the checks

- In the saved tour scene, use **LangkaLens → Validation → Tour Reconstruction Assets** outside Play Mode.
- Enter Play Mode in the tour scene and use **LangkaLens → Validation → Tour Reconstruction Play Mode**. The suite changes nodes and finishes by loading the museum scene; stop Play Mode afterward.
- Enter Play Mode in the museum scene and use **LangkaLens → Validation → Museum Inspection Play Mode**. Stop Play Mode after its result.

The setup menu **LangkaLens → Tour → Set Up Restored Reconstruction Inspection** creates and wires the initial content. It rebuilds the feature's generated display/marker/panel assets, so routine marker placement should use the node inspector instead of rerunning setup after manually customizing those assets.

**LangkaLens → Tour → Update Reconstruction Descriptions** reapplies the curated English/Thai content without rebuilding model or scene assets. Its copy and source mapping are maintained in `TourReconstructionDescriptions.cs` and `Docs/TOUR_RECONSTRUCTION_SOURCES.md`.

## Remaining device validation

No APK/player build, Addressables player-content build, physical Quest controller/hand test, or CPU/GPU/memory profiling was performed. Before shipping, validate ray selection and rotation with supported input, A/B and X priority, seated and standing viewing comfort, marker placement in all four panoramas, panel readability in both locales, packaged Addressables loading, repeated-open memory behavior, and frame time on the target Quest device.

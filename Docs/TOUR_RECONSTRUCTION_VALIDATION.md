# Tour reconstruction inspection: validation and authoring

Updated 2026-10-02. Implemented in `VR_Tour_Scene` using Unity 6000.3.16f1. Editor checks passed; physical Quest and packaged-build validation are outstanding.

## Delivered behavior

Arriving at a configured tour point spawns a selectable red globe using `Assets/_LangkaLens/Tour/3D Models/Prefabs/globeRED.prefab`. Hover highlights it; selection loads one restored reconstruction in front of the current view. The player stays in place. The model remains in the world, with the museum's ray rotation interaction and a localized inspection panel to the user's right. During inspection, the panorama inside `SphereContainer` smoothly fades 85% toward black over 0.3 seconds; the model and UI retain their brightness.

Select and move the model to rotate it. A/B or the Smaller/Larger panel buttons scale it. X or Close ends inspection; X also cancels a pending load. There are no alternate models, comparison controls, or model-switching hints. Navigation and tour zoom pause while loading or inspecting. Closing restores the marker and arrows. Opening the map, leaving a node, returning to the tour menu, disabling the presenter, or unloading the scene clears inspection content.

## Initial content

| Tour point | Restored source | Reconstruction definition |
| --- | --- | --- |
| `2F1` | `OG_BJ2.prefab` | `OG_BJ2.asset` |
| `3F1` | `OG_BJ3.prefab` | `OG_BJ3.asset` |
| `8F1` | `OG_BJ8.prefab` | `OG_BJ8.asset` |
| `9F1` | `OG_BW9.prefab` | `OG_BW9.asset` |

Definitions are in `Assets/_LangkaLens/Tour/VR Tour Data/Reconstruction/`. Inspection displays, the marker, and panel are in `Assets/_LangkaLens/Tour/3D Models/Prefabs/Reconstruction/`. The four displays are registered in the local Addressables group `Tour Reconstructions`. Background fade strength and duration are editable on the `Tour Reconstruction Inspection` scene object's presenter under **Inspection backdrop**.

Default markers start at `(0, -0.2, 2)` in tour coordinates. Each definition displays a model with a largest dimension of 0.65 m before rotation, positioned 1.2 m along the head's horizontal forward direction and 0.2 m below it. Scale controls allow 0.5–1.5 times the initial size. These are starting values for device comfort testing.

The globe is centered and fitted to a 0.45 m diameter, with its original mesh/material retained inside the marker wrapper. The source prefab is unchanged. Its original hover-overlay/drag components are removed from the nested marker instance so the wrapper is the only selection owner.

The information panel opens at `(1.2, -0.2, 1.2)` metres relative to the head's horizontal view: 45 degrees to the right, angled toward the user. It stays in the world at the opening position and does not rotate with the model. Tune **Inspection panel → Inspection Panel Offset** on `Tour Reconstruction Inspection`. The panel is 0.8 × 0.6 m; the largest model bounding-radius estimate at maximum scale is approximately 0.84 m. At the default panel angle, its left edge is approximately 0.92 m right of the model center, leaving clearance even when the model rotates.

The confirmed source model prefabs and `VR_Museum_Scene` were not modified. Existing museum inspection retains player glide, alternate-model switching, and rig restoration. Nodes without reconstruction definitions retain their existing content path. English and Thai strings in `VTour_UI` now identify Ban Jalae 2, 3 and 8, and Ban Wat 9, with archaeological summaries and a separate restored-model interpretation paragraph. See [book references and claim mapping](TOUR_RECONSTRUCTION_SOURCES.md).

The existing `InsideOut360` shader now supports `_InspectionFade`, defaulting to zero. Only the tour's panorama renderer receives a temporary property override. The image texture and warp transforms remain intact. Closing smoothly restores the previous fade value; clearing a node or disabling immediately restores it so the next panorama cannot inherit the dimming. No full-screen blur or additional rendered overlay is used.

## Editor evidence

| Check | Result | What it establishes |
| --- | --- | --- |
| Unity compilation | Passed | New and modified C# compiled in the running Editor. |
| Reconstruction asset validation | 97 checks passed (2026-10-02) | Four definitions, Addressables entries, model anchors and rotation targets, scene and panel references, no alternates, no missing scripts in display prefabs, translations, inspection hint mappings, panorama shader support/compilation, globeRED source, and a single marker ray interaction. |
| Tour Play Mode validation | 119 checks passed (2026-10-02) | All four starting points; marker spawning; hover versus selection; repeated selection; stationary rig; SDK rotation without translation; right-side panel placement/orientation independent of model rotation; panel scaling and close; panorama fade/restoration without changing the shared material; blocked/restored navigation; cancellation and rapid reopen; cleanup during an active grab; map visibility; deliberate invalid-key recovery; leaving during a load; unconfigured node; disabling/re-enabling; menu return; and actual scene unload into the museum. |
| Museum Play Mode regression | 7 checks passed (2026-10-01) | Museum mode, glide, panel and model toggle, exact rig position/rotation restoration, and primary model/UI reset. The subsequent right-panel/globe changes are limited to tour code/content. |
| English/Thai panel checks | Included in tour suite | Nonempty localized text, no detected text overflow, and supported visible glyphs after switching locales. |
| Camera preview | Visually inspected in Editor | The globe marker was visible at the first tour point. A preview from the eye position turned 25 degrees right showed the reconstruction and right-side information panel together against the dimmed panorama. This does not establish comfort on a headset. |
| Globe label layout | 6 localized states checked | English/Thai open, loading, and retry strings fit the 1.2 × 0.2 m label area; measured heights were 0.08 m. The previous oversized font was corrected. |
| Book-based titles and descriptions | All 16 title/body layouts checked | Four sites in both languages fit without missing visible glyphs. The 30-point body font is retained; the description area is 710 × 360, with measured text heights of 273–351 units. |
| Historical-content preview | Visually inspected in Play Mode | The live Thai BJ3 and English BJ8 descriptions, including the bold restored-model labels, fit the right-side panel without touching the title or scale buttons. |

The SDK rotation test sends pointer events to the actual `Grabbable`/`OneGrabFreeRotateTransformer`. Panel tests invoke their bound button callbacks. They establish component wiring and behavior, but do not simulate physical controller trigger presses, tracked hands, or all ray hit conditions. The same-frame menu guard is checked directly; a real X press still needs device validation.

The failed-load case deliberately supplies an invalid Addressable key, captures the expected exception, and verifies a selectable retry marker. Successful loads retain their Addressables handles; canceled/stale completions release their instances. Cleanup checks observe no leftover reconstruction objects. Memory profiling was not performed, so these checks are not a measured proof of zero memory leakage.

The Editor console also reported Meta entitlement failure, unavailable OpenXR/tracking-origin setup, and Meta bridge ports already in use. Those platform/environment messages prevent describing the console as globally error-free. The final suite reported no feature assertion failures. The existing tour keyboard fallback was adapted to the project's enabled Input System after Editor testing exposed legacy input exceptions.

The Editor was returned to the saved `VR_Tour_Scene`, outside Play Mode.

## Place or add a marker

1. Open `VR_Tour_Scene` and select a `TourNode` asset.
2. Assign its Reconstruction field to one of the four definitions. Leave it empty on points that should not expose a model.
3. Use **Setup Scene for this Node** in the Tour Node inspector. Move, rotate, or scale `ReconstructionDummy` against the panorama.
4. Press **Sync from 'ReconstructionDummy'**, then **Revert Scene to Default** to remove the preview. Save the node asset.
5. Enter Play Mode, visit that point, and verify the marker is visible and reachable with the ray.

The authoring preview is tagged `EditorOnly` and disables itself during Editor Play Mode. Runtime markers are spawned independently by the presenter. Presentation distance, initial size, and vertical offset can be adjusted on each shared reconstruction definition; changes affect every node referencing it.

The current asset-validation fixture expects exactly the four initial configured nodes. If more nodes are deliberately added, update that expectation in `TourReconstructionValidation.ValidateAssets`.

## Repeat the checks

- In the saved tour scene, use **LangkaLens → Validation → Tour Reconstruction Assets** outside Play Mode.
- Enter Play Mode in the tour scene and use **LangkaLens → Validation → Tour Reconstruction Play Mode**. The suite changes nodes and finishes by loading the museum scene; stop Play Mode afterward.
- Enter Play Mode in the museum scene and use **LangkaLens → Validation → Museum Inspection Play Mode**. Stop Play Mode after its result.

The setup menu **LangkaLens → Tour → Set Up Restored Reconstruction Inspection** creates and wires the initial content. It rebuilds the feature's generated display/marker/panel assets, so routine marker placement should use the node inspector instead of rerunning setup after manually customizing those assets.

**LangkaLens → Tour → Update Reconstruction Descriptions** reapplies the curated English/Thai content without rebuilding model or scene assets. Its copy and source mapping are maintained in `TourReconstructionDescriptions.cs` and `Docs/TOUR_RECONSTRUCTION_SOURCES.md`.

## Remaining device validation

No APK/player build, Addressables player-content build, physical Quest controller/hand test, or CPU/GPU/memory profiling was performed. Before shipping, validate ray selection and rotation with supported input, A/B and X priority, seated and standing viewing comfort, marker placement in all four panoramas, panel readability in both locales, packaged Addressables loading, repeated-open memory behavior, and frame time on the target Quest device.

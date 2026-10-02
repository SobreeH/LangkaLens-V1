# Tour restored-model inspection plan

Status: implemented and validated in Unity; physical Quest validation remains outstanding. Updated 2026-10-02. See [validation and tuning notes](TOUR_RECONSTRUCTION_VALIDATION.md).

Target: `Assets/_LangkaLens/_Scenes/VR_Tour_Scene.unity`.
Reference: `Assets/_LangkaLens/_Scenes/VR_Museum_Scene.unity`.

## Agreed experience

Each display represents the restored ruins of its location, with no alternate model.

1. At a configured tour point, left Y opens inspection. No globe or reconstruction placeholder spawns.
2. While loading, Y or X cancels. Once loaded, the model appears scaled in front of the opening view. The rig smoothly approaches it horizontally, preserving height and facing.
3. The model stays fixed in the world. The user rotates it with the museum's ray interaction and scales it using A/B or Smaller/Larger.
4. A localized information panel opens to the right of the final viewpoint, angled toward the user, and stays independent of model rotation.
5. Y, X, or Close ends inspection, cancels movement, removes the display/panel, restores the exact original rig transform, and restores navigation.
6. Node changes, map opening, menu return, disable, and scene unload cancel pending work and clear inspection. The next node cannot inherit stale content.
7. During inspection, SphereContainer fades toward the canvas's warm brown (`#533A2D`) over 0.3 s at 0.85 strength. The model/UI stay clear, and the previous panorama settings return on exit.
8. In this scene, the left controller's Y-button model gently pulses between canvas brown (`#533A2D`) and gold (`#BB925C`) on a 1.6 s cycle. Other controller surfaces retain their appearance.
9. English/Thai Y hints describe Inspect ruins, Cancel loading, or Exit inspection. Y is unavailable at the tour menu, over the map, during node transitions, on unconfigured nodes, and while the presenter is disabled.

One inspection display may be active at a time. A failed load permits another Y press to retry.

## Content and architecture

The confirmed sources are `OG_BJ2.prefab`, `OG_BJ3.prefab`, `OG_BJ8.prefab`, and `OG_BW9.prefab` under `Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/`. Areas 2, 3, 8, and 9 reference the corresponding reconstruction definitions on TourArea. Their starting points are 2F1, 3F1, 8F1, and 9F1. The selected area's definition supplies content for its nodes.

Dedicated display wrappers under `Assets/_LangkaLens/Tour/3D Models/Prefabs/Reconstruction/` contain ArtifactData, explicit model/UI anchors, and the museum's rotation wiring. Definitions under `Assets/_LangkaLens/Tour/VR Tour Data/Reconstruction/` reference those displays through the local Addressables group Tour Reconstructions. Source models and museum assets retain their existing use.

TourManager owns node entry/exit integration. TourReconstructionPresenter owns the Ready/Loading/Inspecting states, Y toggle, asynchronous display lifetime, placement, and panorama fade. InspectionManager owns scaling, localized panel behavior, approach, and rig restoration. The museum retains its own approach/rotation and alternate-model controls.

Each load captures a request generation. Closing, leaving, or disabling invalidates it; stale successful completions release their instances. Cleanup disables active grabs before returning Addressables ownership and stops rig motion immediately.

The panel remains 45 degrees to the right at offset `(1.2, -0.2, 1.2)` m. Model presentation starts at 1.2 m forward and 0.2 m below the eye, with a 0.65 m largest dimension. The rig approaches a target centre distance of 0.8 m, increased for maximum-zoom clearance. Tune these existing presenter/definition fields for device comfort.

TourInspectionButtonHighlight uses project-owned copies of the SDK meshes with only Y triangles moved into an additional submesh. Original UVs, bones, weights and animation remain intact. The setup binds both existing left controller visual rigs and their five model variants. Property blocks animate brown/gold without creating materials each frame; disabling restores the original meshes/materials. SDK source assets and other scenes are unaffected.

## Controls and authoring

While loading/inspecting, navigation and tour zoom are blocked. A/B scale the model; left Y toggles inspection; X closes/cancels before the tour-menu input can act. Opening the map explicitly closes inspection. No model-switching controls appear.

To configure reconstruction content, assign the TourArea's Reconstruction field to one of the four definitions. Inspection opens relative to the current view, so IconDummy and its Sync button no longer position a reconstruction entry marker. Existing legacy overlay authoring remains available. The old globe wrapper is retained as an unused asset.

**LangkaLens → Tour → Configure Y Inspection Button** regenerates only the scene's Y highlight assets/bindings and localized hints. The broader reconstruction setup also includes it. The pulse colors and period are editable on Tour Inspection Button Highlight, and panorama tint/strength/duration remain under the presenter's Inspection backdrop.

The English/Thai archaeological summaries and restored-model interpretation paragraphs use the two supplied books and the MR BJ3 reference; see [source mapping](TOUR_RECONSTRUCTION_SOURCES.md).

## Acceptance evidence and remaining work

Unity compilation and 126 reconstruction asset checks passed. The tour suite passed 188 Play Mode checks, including Y toggling/cancellation, no placeholders, all four models, rig restoration, SDK rotation, panel actions, fade/tint restoration, lifecycle guards, late-load cleanup, and button pulse/restore. Controller hint translation/layout checks passed across all four scenes. The museum passed 7 regression checks.

These tests invoke the same toggle method used by Y and send events to real SDK rotation components. They do not simulate a physical controller. Quest testing must still verify the Y binding and visible pulse, hand/controller rotation, viewing comfort, stereo appearance, packaged Addressables, memory, and frame time.

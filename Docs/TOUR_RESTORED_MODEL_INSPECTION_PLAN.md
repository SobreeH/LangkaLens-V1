# Tour restored-model inspection plan

Status: implemented and validated in the Unity Editor; Quest device validation remains outstanding. Updated 2026-10-02. See [validation and authoring notes](TOUR_RECONSTRUCTION_VALIDATION.md).

Target: `Assets/_LangkaLens/_Scenes/VR_Tour_Scene.unity` (confirmed active in Unity).
Reference: `Assets/_LangkaLens/_Scenes/VR_Museum_Scene.unity`.

## Agreed experience

The user selected a scaled model appearing in front of them for inspection. Each model represents the restored ruins of its location. There are no alternate models or model-switching controls in this feature.

1. Arriving at a configured tour node spawns a small, selectable placeholder at an authored location, following the navigation arrows' node lifetime.
2. Hover highlights the placeholder. Selecting it with the existing Meta ray interaction starts loading its restored model. Merely hovering does not open inspection.
3. The restored model appears in front of the user's current view, at a comfortable inspection scale. The player rig smoothly approaches it without changing height or facing, targeting about 0.8 m from its centre with model-size clearance. The model remains anchored in the world after placement, so it does not follow every head movement. The panorama in `SphereContainer` loses focus during inspection to draw attention to the reconstruction, preserving brightness and keeping the model/UI sharp, then restores on exit.
4. The user can rotate the restored reconstruction using the museum's interaction behavior, scale it with the museum's zoom controls, and read localized information on a panel to their right, angled toward them.
5. Close or X ends inspection, cancels any approach, restores the original player rig position/rotation, removes the model and panel, and restores the placeholder and navigation arrows.
6. Changing nodes, opening the map, returning to the tour menu, or unloading the scene clears inspection content and pending work. Nothing from the previous node may appear afterward.

Default scope: one placeholder and one active inspection per node. Different nodes can reference different restored models, or share the same model definition. Multiple simultaneous displays are outside this initial feature.

## Confirmed model sources

The user explicitly selected these four source prefabs:

- `Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/OG_BJ8.prefab`
- `Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/OG_BW9.prefab`
- `Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/OG_BJ3.prefab`
- `Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/OG_BJ2.prefab`

Create one reusable inspection display and reconstruction definition for each source prefab. Use the source as a nested visual inside the interaction wrapper, preserving its existing use elsewhere. The proposed area association follows the numeric identifiers: area 2 uses OG_BJ2, area 3 uses OG_BJ3, area 8 uses OG_BJ8, and area 9 uses OG_BW9. Configure which individual nodes expose each display explicitly; do not automatically add placeholders to every node in an area.

## Existing foundations and gaps

- `TourManager` already spawns and clears navigation arrows, hides them during transitions, and stores the current node. It is the correct owner for node entry/exit integration.
- `TourNode` already has optional `overlayModel`, `modelPosition/Rotation/Scale`, and `iconPosition/Rotation/Scale` fields. That existing path loads an overlay on node entry; `HoverIcon` shows and hides it on hover.
- The active scene's `hoverIconPrefab` is unassigned. Of 80 tour nodes inspected, six have overlay references: four reference `globeRED.prefab`, and two reference `sample.glb`. These are not a ready-to-use inspection setup and should not be migrated automatically.
- `Artifact_Display_Station.prefab` contains `ArtifactData`, `Grabbable`, `OneGrabFreeRotateTransformer`, `RayInteractable`, `PointableUnityEventWrapper`, `ColliderSurface`, and related movement wiring. Reuse this working interaction arrangement in a dedicated inspection display prefab.
- `InspectionManager` already handles localized information, model scaling, alternate-model toggling, and exit. Tour inspection reuses its rig ownership and restoration, with a horizontal approach that preserves facing rather than the museum's pedestal-facing rotation.
- Restored building prefabs exist as `OG_BJ2`, `OG_BJ3`, `OG_BJ8`, and `OG_BW9` under `Assets/_LangkaLens/Mixed/3D Models/RestoredModels/Prefabs/`. The inspected prefabs have no MonoBehaviour interaction components. They need an interaction wrapper and scale/pivot setup.
- Tour and museum controls overlap: A/B move the tour rig or scale an inspected model; X returns from the tour or closes inspection. Shared exit confirmation and controller hints also need the inspection state.

## Planned implementation

### 1. Node configuration and reusable content

Add an optional reconstruction reference and placeholder transform to `TourNode`, keeping existing serialized overlay fields intact. An unconfigured node continues to work as before.

Create a small `TourReconstructionDefinition` ScriptableObject shared by nodes that display the same reconstruction. Store an Addressable reference to the complete inspection-display prefab, its localized content through `ArtifactData`, and any content-specific presentation settings. Keep placement on the node and model content in the shared definition.

The display prefab contains one restored reconstruction, explicit model-container and UI-anchor references, and the museum's rotation interaction arrangement. If reusing `ArtifactData`, assign the reconstruction to `primaryModel` and leave `alternateModel` empty. Prefer prefab composition over changing the source building meshes. Configure actual bounds and pivot offsets per display; imported scale values alone do not determine usable inspection size.

A node with this new configuration uses click inspection instead of activating the legacy hover overlay at the same time. Nodes without the new configuration retain their current behavior.

### 2. Selectable placeholder

Create `ReconstructionPlaceholder.prefab` and a small `TourReconstructionPlaceholder` component with an initialization method and selection callback.

- Use the arrow prefab's ColliderSurface, RayInteractable, and InteractableUnityEventWrapper pattern.
- Wire hover/unhover to visual feedback and select to opening inspection.
- Use `Assets/_LangkaLens/Tour/3D Models/Prefabs/globeRED.prefab` as the placeholder visual, fitted to marker size and retaining the wrapper's selection behavior.
- Keep it separate from `NavigationArrow` and the teleport arc's arrow list, so selecting it cannot navigate to another node.
- During loading, show feedback and block repeated selection. Hide the placeholder during inspection and restore it on close or recoverable load failure.

### 3. Node-owned spawning and cleanup

Add a scene component, `TourReconstructionPresenter`, referenced explicitly by `TourManager`. Its responsibility is placeholder/display lifetime and loading. `InspectionManager` retains responsibility for inspection behavior.

`TourManager` calls the presenter when a node transition finishes successfully and clears it at the beginning of a transition or menu return. This follows the arrows' lifetime while preventing interaction during the warp animation.

Use a small state set: Unavailable, Ready, Loading, Inspecting. Only one load/display is active at a time.

For Addressables, capture the requested node and a request generation when loading begins. Closing, changing node, disabling, or unloading invalidates that generation. A late successful completion releases its instance instead of showing stale content. Release failed operations and successful instances exactly once with the matching Addressables ownership path. Clear UI subscriptions, grabs, temporary roots, and runtime references during cleanup.

Load the display on selection, not every node entry. Introduce caching or pooling only if device measurements justify it.

### 4. Museum inspection reuse

Extend `InspectionManager` with an explicit tour presentation mode whose default preserves existing museum behavior. In tour mode:

- Smoothly approach the spawned display horizontally without forced rotation, using its renderer bounds for clearance. Save and restore the rig position/rotation; stop approach immediately on exit, map opening, node change or disable. Show the panel to the right of the final eye position after arrival. Retain the stationary API for existing callers.
- Accept the spawned display and its explicit data/anchors after initialization.
- Reuse model scaling, localized text, and close behavior. Hide the model-toggle button and comparison state label in the tour panel.
- Provide an exit notification so the presenter can release the spawned display and restore tour content, including when X closes inspection internally.
- Restore temporarily disabled components to their previous states rather than enabling locomotion unconditionally.

Place a separate display root using the head's horizontal forward direction at opening time. Begin tuning around 1.2 metres forward, matching the museum scene's configured inspection distance, with a configurable vertical offset. Fit the model to a configurable display envelope using renderer bounds; keep scale uniform and its rotation pivot centered appropriately. Keep the display outside the photosphere's warp hierarchy and outside the camera hierarchy.

Extract a reusable inspection panel from the museum UI, retaining localization and ray interaction. Wire close and accessible scale controls so required actions also have a hand-selectable UI route. Present the content as the location's restored reconstruction, with no current/original comparison UI. Bind all scene-dependent references explicitly.

### 5. Input and tour integration

While loading or inspecting, suppress arrow navigation and teleport aiming. While inspecting, route A/B to model scaling. Y has no model-switching action or model-switching hint. Opening the map through the existing tour UI closes/cancels inspection first. Pause the tour's `VRZoomController` while inspection owns rig movement, then restore the pre-inspection position and preserve its saved zoom offset.

X closes or cancels inspection first. Guard that entire input frame so the same press cannot also return to the tour menu or start exit confirmation, regardless of Update ordering. Reuse the existing inspection exit-frame concept and integrate it with `TourManager` and `MRMainMenuConfirmation`.

Opening the map or requesting a different node explicitly closes/cancels inspection before proceeding. Wire `ControllerCanvasHints` to the scene's inspection manager and update its tour-specific mapping where necessary. Restore prior control/UI visibility states on close. Preserve museum-scene defaults.

### 6. Authoring workflow

Extend `TourNodeEditor` with a `ReconstructionDummy` placement workflow analogous to `PrevDummy` and `NextDummy`: preview, synchronize position/rotation/scale, and clear preview on revert. Use Undo and keep previews out of runtime spawning.

Start with one explicitly configured node and one of the four confirmed restored displays. Verify the proposed building-to-area association against the node's photographed site and author the marker placement before populating more nodes; existing sample overlay assignments are not authoritative content mapping.

## Delivery sequence

1. Add optional node configuration and reusable display/placeholder prefabs.
2. Add presenter loading, node integration, and complete cleanup.
3. Extend shared inspection with tour approach/restore behavior and configure its panel.
4. Integrate input priority, map/exit behavior, and controller hints.
5. Add node placement tooling and configure one demonstration node.
6. Validate the demonstration, regress the museum, then configure the agreed remaining nodes.

## Acceptance checks

- Configured nodes show exactly one placeholder after transition; unconfigured nodes show none.
- Hover only highlights. One selection opens one model, and moving the pointer away does not close it.
- Opening inspection smoothly moves the tour rig closer without changing height or facing. Closing or cancelling restores its original position/rotation. The model stays world-anchored and appropriately sized for seated and standing use.
- Controller and supported hand interactions rotate the model. Scale, information, and close controls work. No model-toggle button, comparison label, or model-switching controller hint appears in tour inspection.
- A/B do not move the tour rig during inspection. X closes inspection without also leaving the node. Teleport aiming cannot remain active behind the model.
- Closing, map opening, node change, menu return, and scene unload remove content and restore the appropriate controls.
- Rapid repeated selection, close during load, late completion, missing content, and load failure do not leave duplicates, stale models, retained grabs, or leaked Addressables handles.
- Existing arrow navigation, unconfigured nodes, legacy overlays, museum player glide, museum model toggling, localization, and exit confirmation remain functional.
- Compile in Unity; run focused lifetime/input regression checks and Play Mode checks. Editor validation passed: 177 asset checks, 162 tour Play Mode checks, and 7 museum regression checks on 2026-10-02. Actual controller/hand selection, approach comfort, blur performance/stereo appearance, memory, and frame time still require target Quest validation; see the validation report for the limits of the Editor checks.

## Remaining content choices

The four restored model sources and the single-reconstruction presentation are confirmed. The initial setup uses each area's starting node and editable default marker positions. Final marker placement against each panorama remains a content refinement. The implementation uses the requested red globe marker and book-based English/Thai descriptions for Ban Jalae 2, 3 and 8, and Ban Wat 9. Archaeological evidence and restored-model interpretation are distinguished; see [source mapping](TOUR_RECONSTRUCTION_SOURCES.md).

## Implemented setup

The four starting nodes expose reconstruction placeholders: `2F1` uses `OG_BJ2`, `3F1` uses `OG_BJ3`, `8F1` uses `OG_BJ8`, and `9F1` uses `OG_BW9`. Marker positions begin at `(0, -0.2, 2)` in tour coordinates and are editable per node. Additional nodes opt in by referencing one of the reconstruction definitions.

Inspection display and panel prefabs are under `Assets/_LangkaLens/Tour/3D Models/Prefabs/Reconstruction/`. Reconstruction definitions are under `Assets/_LangkaLens/Tour/VR Tour Data/Reconstruction/`. The display prefabs are registered in the local Addressables group `Tour Reconstructions`.

Select a marker with a controller ray or hand ray. Select and drag the model to rotate it. A/B or the Smaller/Larger panel buttons change its size. X or Close ends inspection; X also cancels a pending load. No Y model-switching action is added. Opening the map through the tour UI ends inspection.

The presenter also smoothly blurs the panorama over 0.3 seconds during inspection, preserving brightness, sharp model/UI rendering and the existing tour warp. Blur strength defaults to 0.85. These values can be tuned under `Inspection backdrop` on the `Tour Reconstruction Inspection` scene object.

The information panel opens 45 degrees to the user's right, angled toward them, and stays fixed in the world. Its default head-relative offset is `(1.2, -0.2, 1.2)` metres and is editable under `Inspection panel` on the presenter. The marker uses the nested `globeRED` visual normalized to 0.45 m diameter.

Use the Tour Node inspector's `Setup Scene for this Node`, move `ReconstructionDummy`, and press `Sync from 'ReconstructionDummy'` to author marker placement. `Revert Scene to Default` removes the preview. The preview disables itself in Editor Play Mode and is tagged EditorOnly for builds.

Repeatable checks are available under `LangkaLens/Validation/Tour Reconstruction Assets`, `Tour Reconstruction Play Mode`, and `Museum Inspection Play Mode`. Run the Play Mode commands in their respective scenes. The tour smoke test also exercises unloading into the museum scene. Validation details are recorded in `Docs/TOUR_RECONSTRUCTION_VALIDATION.md`.

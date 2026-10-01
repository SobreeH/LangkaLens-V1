# Tour restored-model inspection plan

Status: planned, not implemented. Inspected 2026-10-01.

Target: `Assets/_LangkaLens/_Scenes/VR_Tour_Scene.unity` (confirmed active in Unity).
Reference: `Assets/_LangkaLens/_Scenes/VR_Museum_Scene.unity`.

## Agreed experience

The user selected a scaled model appearing in front of them for inspection. Each model represents the restored ruins of its location. There are no alternate models or model-switching controls in this feature.

1. Arriving at a configured tour node spawns a small, selectable placeholder at an authored location, following the navigation arrows' node lifetime.
2. Hover highlights the placeholder. Selecting it with the existing Meta ray interaction starts loading its restored model. Merely hovering does not open inspection.
3. The restored model appears in front of the user's current view, at a comfortable inspection scale. The player rig stays in place. The model remains anchored in the world after placement, so it does not follow every head movement.
4. The user can rotate the restored reconstruction using the museum's interaction behavior, scale it with the museum's zoom controls, and read localized information.
5. Close or X ends inspection, removes the model and panel, and restores the placeholder and navigation arrows.
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
- `InspectionManager` already handles localized information, model scaling, alternate-model toggling, and exit. Its current entry flow moves the player toward a pedestal and its exit restores the rig; those behaviors need an explicit stationary-player mode for the tour.
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
- Use a lightweight placeholder mesh, with a clear visual indication that it is selectable.
- Keep it separate from `NavigationArrow` and the teleport arc's arrow list, so selecting it cannot navigate to another node.
- During loading, show feedback and block repeated selection. Hide the placeholder during inspection and restore it on close or recoverable load failure.

### 3. Node-owned spawning and cleanup

Add a scene component, `TourReconstructionPresenter`, referenced explicitly by `TourManager`. Its responsibility is placeholder/display lifetime and loading. `InspectionManager` retains responsibility for inspection behavior.

`TourManager` calls the presenter when a node transition finishes successfully and clears it at the beginning of a transition or menu return. This follows the arrows' lifetime while preventing interaction during the warp animation.

Use a small state set: Unavailable, Ready, Loading, Inspecting. Only one load/display is active at a time.

For Addressables, capture the requested node and a request generation when loading begins. Closing, changing node, disabling, or unloading invalidates that generation. A late successful completion releases its instance instead of showing stale content. Release failed operations and successful instances exactly once with the matching Addressables ownership path. Clear UI subscriptions, grabs, temporary roots, and runtime references during cleanup.

Load the display on selection, not every node entry. Introduce caching or pooling only if device measurements justify it.

### 4. Museum inspection reuse

Extend `InspectionManager` with an explicit presentation mode whose default preserves existing museum behavior. In stationary-player mode:

- Skip the player-glide coroutine and skip rig position/rotation restoration on exit.
- Accept the spawned display and its explicit data/anchors after initialization.
- Reuse model scaling, localized text, and close behavior. Hide the model-toggle button and comparison state label in the tour panel.
- Provide an exit notification so the presenter can release the spawned display and restore tour content, including when X closes inspection internally.
- Restore temporarily disabled components to their previous states rather than enabling locomotion unconditionally.

Place a separate display root using the head's horizontal forward direction at opening time. Begin tuning around 1.2 metres forward, matching the museum scene's configured inspection distance, with a configurable vertical offset. Fit the model to a configurable display envelope using renderer bounds; keep scale uniform and its rotation pivot centered appropriately. Keep the display outside the photosphere's warp hierarchy and outside the camera hierarchy.

Extract a reusable inspection panel from the museum UI, retaining localization and ray interaction. Wire close and accessible scale controls so required actions also have a hand-selectable UI route. Present the content as the location's restored reconstruction, with no current/original comparison UI. Bind all scene-dependent references explicitly.

### 5. Input and tour integration

While loading or inspecting, suppress arrow navigation and teleport aiming. While inspecting, route A/B to model scaling. Y has no model-switching action; retain its existing tour menu role, with any map opening closing/cancelling inspection first. Pause the tour's `VRZoomController` without changing the player's current position or its saved zoom offset.

X closes or cancels inspection first. Guard that entire input frame so the same press cannot also return to the tour menu or start exit confirmation, regardless of Update ordering. Reuse the existing inspection exit-frame concept and integrate it with `TourManager` and `MRMainMenuConfirmation`.

Opening the map or requesting a different node explicitly closes/cancels inspection before proceeding. Wire `ControllerCanvasHints` to the scene's inspection manager and update its tour-specific mapping where necessary. Restore prior control/UI visibility states on close. Preserve museum-scene defaults.

### 6. Authoring workflow

Extend `TourNodeEditor` with a `ReconstructionDummy` placement workflow analogous to `PrevDummy` and `NextDummy`: preview, synchronize position/rotation/scale, and clear preview on revert. Use Undo and keep previews out of runtime spawning.

Start with one explicitly configured node and one of the four confirmed restored displays. Verify the proposed building-to-area association against the node's photographed site and author the marker placement before populating more nodes; existing sample overlay assignments are not authoritative content mapping.

## Delivery sequence

1. Add optional node configuration and reusable display/placeholder prefabs.
2. Add presenter loading, node integration, and complete cleanup.
3. Extend shared inspection with stationary-player mode and configure its panel.
4. Integrate input priority, map/exit behavior, and controller hints.
5. Add node placement tooling and configure one demonstration node.
6. Validate the demonstration, regress the museum, then configure the agreed remaining nodes.

## Acceptance checks

- Configured nodes show exactly one placeholder after transition; unconfigured nodes show none.
- Hover only highlights. One selection opens one model, and moving the pointer away does not close it.
- Opening/closing inspection does not move or rotate the tour rig. The model stays world-anchored and appropriately sized for seated and standing use.
- Controller and supported hand interactions rotate the model. Scale, information, and close controls work. No model-toggle button, comparison label, or model-switching controller hint appears in tour inspection.
- A/B do not move the tour rig during inspection. X closes inspection without also leaving the node. Teleport aiming cannot remain active behind the model.
- Closing, map opening, node change, menu return, and scene unload remove content and restore the appropriate controls.
- Rapid repeated selection, close during load, late completion, missing content, and load failure do not leave duplicates, stale models, retained grabs, or leaked Addressables handles.
- Existing arrow navigation, unconfigured nodes, legacy overlays, museum player glide, museum model toggling, localization, and exit confirmation remain functional.
- Compile in Unity; run focused lifetime/input regression checks and Play Mode checks. Validate actual selection, viewing comfort, memory, and frame time on the target Quest device. No runtime validation has been performed for this plan.

## Remaining content choices

The four restored model sources and the single-reconstruction presentation are confirmed. The exact nodes receiving placeholders, their authored positions, the final placeholder artwork, and localized descriptions remain content configuration decisions. They do not block the implementation structure above.

No runtime scripts, scenes, prefabs, or node assets were changed while preparing this plan.

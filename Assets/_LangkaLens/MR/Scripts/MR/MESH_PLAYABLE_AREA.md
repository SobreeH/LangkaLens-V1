# Central model display and mesh walking area

MR_Scene lets visitors select a large reconstruction and walk around it in mixed reality to appreciate its size. The active **Unrestricted** mode allows playback inside and outside the scanned mesh, including the central opening. The mesh supplies the spatial reference for anchoring the model, rather than restricting visitor movement. Alignment, tracking and passthrough still gate playback; Development Mode remains off.

Each selected model is uniformly scaled to fit the 14 x 16.5 m central reference rectangle, centered by its rendered bounds, and grounded at site-local Y = 0. Scaling preserves proportions. The reference rectangle is a user-selected size estimate, **not verified original dimensions**. All four models are independently fitted, so their relative historical sizes are not established by this mode.

## Display reference and legacy rectangular footprint

Coordinates are in PlayableBoundary local X/Z metres.

| Boundary | X minimum / maximum | Z minimum / maximum | Size |
| --- | --- | --- | --- |
| Outer hallway limit | -8.5 / 12.5 | 0.5 / 24.5 | 21 x 24 m |
| Central exclusion | -5 / 9 | 4.5 / 21 | 14 x 16.5 m |

The optional rectangular mode gives 273 m² before stop margins. The optional **Mesh Floors** mode follows the union of eight imported floor planes (approximately 279.2 m² before margins), removing overlap seams and excluding the empty centre. Neither footprint restricts visitors in **Unrestricted** mode. The central rectangle still controls model placement and size. Scan registration is uneven, particularly at the front-left corner.

The [SAC site record](https://archaeology.sac.or.th/archaeology/84) describes a 13.5 m square monument and an 8.25 m central building. A [Fine Arts Department publication](https://www.finearts.go.th/storage/contents/file/iuem8KKNkl2V2LdU9GbtFCsTLiVLDeSIRLKLdgSG.pdf) reports a 13.15 m square plan. Neither establishes surrounding visitor-hallway dimensions. Research date: 2026-09-22. The scene dimensions are an editable first pass from the scans and require measurement on site.

## Editing

Select **PlayableBoundary > MR Mesh Playable Area**. Footprint Mode is **Unrestricted**. No floor references or baked boundary are required for this mode, and no walking boundary or proximity warning is displayed. Orange marks the model's size-reference rectangle in the editor. Unity Rect Y means local Z; Width/Height are X/Z lengths. The eight floor references remain available if Mesh Floors mode is selected later; use **Refresh mesh floor footprint** after moving/scaling those floor sections.

On **MRManager > MR Model Spawner**, **Fit To Central Opening** enables per-model proportional fitting. **Display Base Height** sets the model's base in site-local metres. With fitting disabled, the previous global-scale behaviour remains available. Change **Model Opening Reference (X/Z)** on the area to adjust the display footprint without redefining the walking mesh.

Legacy **Scan Interior** mode retains the original approximately 244 m² bake for comparison. Only that mode requires fresh scan transforms. That central polygon is not the active walking footprint.

Model_Placement is under PlayableBoundary. Fitting places the model's rendered bounds at the opening centre, local X/Z (2, 12.75), with its base at Display Base Height, regardless of the marker's position. Visitors may view the reconstruction from outside the scanned footprint. Scan meshes and Mesh_ref were not repositioned. Model bounds, rather than imported pivots, determine the final placement. Selection order is BJ2, BJ8, BJ3, BW9 to match the button labels.

## Headset operation

1. Hold **right grip** to place the cyan Mesh_ref sign/table outline over the physical feature.
2. Use **right thumbstick horizontally** to rotate and **vertically** to adjust reach. Match position, orientation and height. Check the green arrow points toward the real front hallway.
3. Release grip and press **A**. Playback waits for the session spatial anchor to be tracked.
4. Select a model. It appears at the aligned site position. Walking past the scanned floor edges or into the central opening does not hide it or block selection. Press **B or Y** to remove it and reopen selection.
5. Hold **X for two seconds** to realign. Alignment is required each launch; anchors are not persisted.

Mesh_ref is approximately 1.09 m wide, 0.31 m deep and 0.73 m high, with its pivot about 0.33 m above authored ground. Match its real dimensions, height and surveyed position.

Boundary distances apply only to the optional bounded modes: warning **0.8 m**, stop **0.25 m**, recovery margin **0.1 m**. They have no effect in Unrestricted mode. Headset/anchor tracking and passthrough availability still gate playback. Controller tracking is required for alignment; its loss after confirmation does not itself pause playback. Pausing hides the model and blocks spawning, not the whole application.

**Development Mode** bypasses these gates; it is off in the saved scene.

## Verification

Run **Tools > LangkaLens > MR > Run playable interior checks** and **Run central display checks**. These cover unrestricted positions outside the scan and in the central opening, anchored model visibility, invalid positions, alignment/tracking gates, model hiding/restoration, optional mesh and rectangular footprints, and all four models' fit/centering/grounding/proportions.

Scan renderers/colliders are disabled at runtime. The boundaries test horizontal headset position and are not physical barriers. They do not detect people, floor levels or additional obstacles. Virtual locomotion is disabled; the headset is never clamped or teleported. Quest boundary suppression remains off.

Verify imported floor registration, obstacles and margins on site, and walk all floor-section joins. Test tracking, passthrough, recentering, restart and drift across the full route. One entrance anchor does not guarantee registration accuracy everywhere. Measured model dimensions are needed before describing the fitted reconstructions as historically accurate life size.

## Components

- MRMeshReferenceAlignment: entrance placement and session anchor; moves the complete site.
- MRFloorFootprint: union of imported floor planes and exposed boundary segments.
- MRMeshPlayableArea: mesh floor footprint, legacy modes, clearance, tracking gates and CanPlay.
- MRMeshAreaFeedback: reference outline, heading arrow, walking boundary segments and instructions.
- MRModelSpawner: model fitting, grounding, selection gate and visibility; models follow the aligned site.
- MRMeshAreaTools / MRDisplayChecks: Inspector/Scene tools and automated checks.

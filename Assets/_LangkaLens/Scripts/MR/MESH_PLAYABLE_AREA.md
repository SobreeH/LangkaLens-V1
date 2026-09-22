# Mesh interior and entrance alignment

`MR_Scene` uses the enclosed central space between the eight BJ3 scan meshes as its playable footprint. `Mesh_ref`, the sign/table box in `BJ3F`, is the manual alignment reference. No physical features or scan meshes were moved during setup. `Model_Placement` now moves with `PlayableBoundary` and retains its authored position.

## In the headset

1. Start MR at the entrance with the right controller tracked.
2. Hold **right grip**. A cyan wire outline matching `Mesh_ref` appears in front of the controller. Move your hand to position it over the actual sign/table represented by that box.
3. While holding grip, use the **right thumbstick horizontally** to rotate the site and **vertically** to change placement reach. Match the box's position, orientation and height. The **green arrow must point into the real site's interior**; this prevents a visually similar 180-degree alignment.
4. Release grip and press **A**. The app creates a session spatial anchor at the reference. Content remains paused until this anchor is tracked.
5. Enter the central interior. Approaching its edge shows a warning; leaving it hides the selected model and blocks new model selections. Returning inside restores the selected model. Passthrough/tracking failures also pause content.
6. Hold **X for two seconds** to restart alignment. Confirm the entrance alignment on each app launch; this version does not persist or automatically recognize the sign.

`Mesh_ref` is an imported box approximately 1.09 m wide, 0.31 m deep, and 0.73 m high. Its pivot is at the box centre, approximately 0.33 m above the authored ground. Align the box to the physical feature it represents, including its height—not merely to an arbitrary point on the sign face. If the proxy box does not fit the physical sign/table, correct that mesh or its transform in Unity before relying on the alignment. The table/sign must be in its surveyed location.

## Editing and verification

Select **PlayableBoundary**. `MR Mesh Playable Area` shows the generated outline and its area. Use **Rebuild interior from scan meshes** after changing scan geometry or the relative positions of BJ3 sections. The system detects changed section transforms at startup and refuses stale outlines.

The outline comes from the real triangles in `GLOBAL_MESH` objects intersected at local height **1.2 m**. A visibility polygon from the interior seed follows the inward-facing surfaces. It is not a bounding rectangle or a convex hull. The current seed produces an enclosed central region of about **244 m²**. Helper meshes, furniture boxes, and wireframe duplicates are excluded from the bake. Open cross-sections produce an error instead of an invented enclosing shape.

The method assumes the central region is visible from its seed (star-shaped); it does not expand into hidden side rooms. It tests the horizontal head position, not terrain, feet, or dynamic obstacles. Other surfaces at different heights, ruins inside the space, and people are not automatically treated as exclusions. The warning begins at **0.8 m** from the outline; content pauses within **0.25 m**, with **0.1 m** of recovery hysteresis. These are adjustable application distances, not certified safety margins.

The imported scan renderers and colliders are disabled only at runtime to preserve passthrough. The area test does not require a trigger collider or a Rigidbody on the camera. Virtual controller/hand locomotion is disabled in this scene. The tracked headset is never clamped or teleported by this system.

Run **Tools > LangkaLens > MR > Run playable interior checks** for geometry and scene-wiring checks. On a Quest, verify manual placement, correct heading, entry/exit, edge warnings, model hiding/restoration, loss/recovery of tracking, recentering and restarting. Walk to the far side to measure actual registration drift before using this large area with visitors. One entrance anchor cannot guarantee precise registration throughout the entire site.

The application outline is separate from Quest's system boundary. This setup does not suppress that boundary or enable app-wide boundaryless mode. If the configured system boundary prevents using the full site, contextual boundaryless MR is a separate deployment configuration to evaluate with the actual route and passthrough experience.

## Components

- `MRMeshReferenceAlignment`: manual entrance placement and a session spatial anchor; moves `PlayableBoundary`, not the camera rig.
- `MRMeshPlayableArea`: baked polygon, tracking/passthrough checks, clearance and `CanPlay`.
- `MRMeshAreaFeedback`: sign outline, orientation arrow, setup instructions and edge warnings.
- `MRModelSpawner.playableArea`: blocks spawning and hides/restores the current site model when play is paused. With no area assigned, existing free-placement behaviour remains available.

Spatial anchor lifecycle follows the installed Meta SDK and [Meta's anchor documentation](https://developers.meta.com/horizon/documentation/unity/unity-spatial-anchors-persist-content/).

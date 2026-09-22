# LangkaLens Project Progress

Last updated: 2026-09-22 (Asia/Bangkok)

This file records the main implementation decisions, completed work, verification, and next steps for the LangkaLens mixed-reality experience.

## Current focus

Create an on-site mixed-reality experience for **Ban Jalae 3 at Yarang Ancient City, Pattani, Thailand**, targeting **Meta Quest 3 and Quest 3S**.

The active Unity scene used for this work is:

`Assets/_LangkaLens/Scenes/MR_Scene.unity`

## Implemented: rectangular hallways (dimensions provisional)

- Four connected rectangular hallways surround an excluded central ruin/enclosure.
- Outer rectangle: **21 x 24 m**. Central exclusion: **14 x 16.5 m**. Geometric walking footprint: **273 m²** before stop margins. Dimensions are estimates from the scans and require on-site measurement.
- Quest scans were captured as small rooms; overlapping partitions can represent open space. Scan walls no longer determine the active boundary. The old approximately 244 m² central polygon is retained only in legacy Scan Interior mode.
- Select `PlayableBoundary` to edit both rectangles numerically or with Scene-view edge handles. Both boundaries trigger warnings and playback gating.
- The selected model is hidden when play is paused and restored after the visitor safely re-enters.
- Headset/anchor tracking and passthrough failures pause content. Controller tracking gates alignment input, not playback after confirmation.
- Pausing hides the model and blocks spawning; it is not a global application pause.
- Development Mode bypasses these gates and is **off** in the saved scene.
- Quest boundary suppression is disabled; actual system-boundary behavior requires device testing.

## Completed: entrance alignment

- `Mesh_ref`, representing the real entrance sign on its small table, is the manual site-alignment reference.
- Alignment moves `PlayableBoundary`; it does not move or teleport the camera rig.
- A session spatial anchor is created after alignment is confirmed.
- Alignment currently needs to be performed again on every launch.
- The green orientation arrow must point into the site to prevent a 180-degree alignment error.

### Headset controls

1. Hold **right grip** to display and position the `Mesh_ref` outline over the physical sign/table.
2. Move the **right thumbstick horizontally** to rotate the site.
3. Move the **right thumbstick vertically** to adjust placement reach.
4. Release the grip and press **A** to confirm alignment.
5. Hold **X for two seconds** to restart alignment.

Virtual controller and hand locomotion are disabled in this scene so these controls can be used for site alignment without moving the visitor virtually.

## Main components

- `MRMeshReferenceAlignment`: manual entrance alignment and session spatial-anchor handling.
- `MRMeshPlayableArea`: rectangular ring or legacy baked polygon, clearance, tracking checks, and `CanPlay`.
- `MRMeshAreaFeedback`: reference outline, direction arrow, inner/outer boundaries, instructions, and warnings.
- `MRModelSpawner`: blocks model selection and hides or restores the selected model according to the playable-area state.
- `MRMeshAreaTools`: rectangle editing handles, legacy mesh baking, and validation.

The detailed setup and operation guide is located at:

`Assets/_LangkaLens/Scripts/MR/MESH_PLAYABLE_AREA.md`

## Verification completed

- The scene was saved with the playable-area system wired in.
- The generated outline was inspected in a top-down preview.
- **56 automated geometry and scene-wiring checks passed** for rectangular hallways, including exclusion, hallway/corner coverage, both edge clearances, coordinate transforms and recovery hysteresis. The initial scan-based version had 22 checks.
- Model spawning is blocked before alignment.
- Models are configured to hide outside the area and return after re-entry.
- Scan-transform freshness checks apply only to legacy Scan Interior mode.
- `Model_Placement` remains in the central display region while visitors are restricted to hallways.

Editor checks can be run from:

**Tools > LangkaLens > MR > Run playable interior checks**

## Still to test on a Quest

- Measure the actual hallways and central enclosure; adjust the provisional rectangles and check every corner.
- Match the `Mesh_ref` outline accurately to the physical entrance sign/table.
- Confirm that the green arrow points into the real site.
- Verify edge warnings and exit/re-entry behavior while physically walking.
- Verify model hiding and restoration.
- Test loss and recovery of controller, headset, anchor, and passthrough tracking.
- Test recentering and restarting the application.
- Walk to the far side of the area and measure registration drift.
- Confirm that the physical sign/table dimensions and surveyed position match `Mesh_ref`.
- Evaluate whether Quest's system boundary permits the intended route.
- Establish visitor supervision and site-specific safety margins before public use.

One entrance anchor does not guarantee accurate registration across the entire site. The full route must be validated on location before visitor deployment.

## Related Git history

- `a8109cf` - moved the mesh to reflect real-world space
- `0ab7ee2` - updated `MRModelSpawner` to spawn at `Model_Placement`
- `b9a4f36` - moved and renamed the reference point
- `57c5c50` - added mesh playable-area support, boundary behavior, and site-reference alignment
- `193d754` - corrected `Model_Placement` parenting and location inside the playable area
- `3444db2` - added editor demo spawn/despawn commands and updated `Model_Placement`
- `1d3df67` - added a development-mode toggle to `MRMeshPlayableArea`

## Recommended next milestone

Measure the real hallways and central enclosure, then validate on-site with Quest 3/3S. Record alignment accuracy around the full ring, then adjust rectangles, warning distances and anchor strategy.

## Update convention

When more work is completed, add an entry below with the date, what changed, how it was verified, the relevant commit, and any remaining risks.

### 2026-09-22

- Established the mesh-derived playable interior and `Mesh_ref` entrance-alignment workflow.
- Connected playable-area state to model spawning and visibility.
- Added editor tooling, feedback, documentation, and automated checks.
- Remaining milestone: physical on-site validation with a Quest 3 or Quest 3S.

### 2026-09-22 — rectangular hallway correction

- Replaced the active central footprint with a rectangular hallway ring after confirming that overlapping scan walls may be artificial partitions.
- Added the central exclusion, warnings at both boundaries, rectangle editing handles and hallway tests. Entrance alignment and content gating remain connected.
- Published monument dimensions are approximately 13.15–13.5 m square; modern hallway dimensions were not established: [SAC record](https://archaeology.sac.or.th/archaeology/84), [Fine Arts Department publication](https://www.finearts.go.th/storage/contents/file/iuem8KKNkl2V2LdU9GbtFCsTLiVLDeSIRLKLdgSG.pdf). Scene enclosure dimensions remain estimates from the scans.
- Verification: 56 editor checks passed. No headset/on-site validation performed. Changes are not committed.

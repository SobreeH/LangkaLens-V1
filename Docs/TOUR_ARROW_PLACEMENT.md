# VR tour arrow placement

Updated 2026-10-03 for `VR_Tour_Scene` and all 80 TourNodes in
`Assets/_LangkaLens/Tour/VR Tour Data` (160 previous/next arrows).

## Placement

- Each marker sits 3.2 metres horizontally from the panorama origin. The settled
  tour camera is recentered to that origin by `TourManager.ResetPlayerView`.
- Markers sit 20–30 degrees below the horizon, retaining their photographed
  ground angle within that range. This gives 3.41–3.70 metres of viewing distance
  and heights of approximately -1.16 to -1.85 metres relative to eye origin.
- The arrow prefab is 0.9 metres across and 0.03 metres thick. Its ray interaction
  collider has a 0.45 metre radius, verified after syncing physics transforms.
- Positions remain fixed in the panorama. They do not follow the user's gaze.

All original path bearings remain except these panorama-based corrections:

| Node and connection | Bearing | Reason |
| --- | ---: | --- |
| 3C1 previous → 3L4 | 345° | Follow the aisle; previous placement pointed at a column. |
| 3C1 next → 3F1 | 70° | Follow the perpendicular aisle; previous placement pointed at a column/sign. |
| 2B1 next → 2B2 | 85°, 25° down | Place beyond the foreground bricks on the narrow dirt route. |
| 2R1 next → 2R2 | 58° | Clear the column base and center the dirt route. |

The tilted 9B1 previous marker is now level. All target links, photographs, icon
placements, identifiers, and other serialized node data were preserved.
The arrow shader now includes stereo instancing setup for both eyes.

## Validation

All 160 bearings were reviewed against perspective views derived from the actual
panoramas, using the sphere mesh UVs and the `InsideOut360` horizontal UV flip.
Unity imported all assets and checked the distance/angle limits. The four tour
loops remain reciprocal, and all photograph references resolve to Addressables.
Representative arrows were rendered with their final prefab and panorama
materials. Play Mode exercised next/previous navigation in Area 2, corrected
corner spawning and collider ray hits in Area 3, and spawning in Areas 8 and 9.
The arrow shader imported without shader errors.

Play Mode also reported Meta bridge port conflicts and a Meta entitlement error
from existing integration code; these are separate from arrow placement.
No Quest build or headset comfort test was performed.

For the final headset check, visit each area's start and corners, try both ray
selection and thumbstick navigation, and confirm the markers remain comfortable
and readable in both eyes while seated and standing.

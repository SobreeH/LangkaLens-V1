# Scene loading screen

The existing MR, VR Tour, Museum and return-to-main-menu buttons now call
`SceneLoadingScreen.LoadScene(sceneName)`. No scene or Inspector wiring is needed.
New scene-navigation entry points should call the same method.

The temporary persistent world-space canvas shows Thai or English text using
the selected locale and the existing Sarabun font. It follows the main camera
at 1.5 metres, covers the view, shows scene-loading progress, and fades away
after activation and two rendered camera frames. Duplicate load requests are
ignored. Invalid scene names are rejected before creating the cover.

The tour additionally exposes `IsInitialContentReady`, so the cover waits for
its title panorama to finish loading. Failed panorama requests are logged and
release the cover; a 30-second timeout avoids trapping the user. Other future
asynchronously initialized scene content needs an explicit readiness check.
Progress describes scene loading, not an estimated time remaining.

The render-frame wait has a two-second fallback for hidden/headless views or
missing cameras. All timing uses unscaled time. The overlay shader is in
Resources so it is retained in builds. The canvas and its materials are
destroyed after each transition.

## Place changes inside VR Tour

Area buttons call `TourManager.LoadArea` and show the loading screen through
`BeginContentLoading`. Returning to the tour map also shows the loading screen.
The panel is submitted before requesting the panorama, reports Addressables
progress, and remains visible through the view transition, navigation setup,
two rendered frames and the final fade. Navigation stays locked until the cover
finishes fading.

Navigation arrows and thumbstick teleport call `LoadNode` without creating a
loading screen. The current panorama stays visible while the destination loads,
then the existing smooth warp transition plays. Navigation stays locked until
the transition completes. Both routes share the same request cleanup.

The previous panorama and area remain selected until the new image succeeds.
A failed request or 30-second timeout releases the pending handle and restores
navigation so the user can retry. Repeated area clicks cannot change the pending
destination. Disabling or destroying the tour clears its loading cover and
pending request. Transition animation timing is unscaled so pausing does not
leave the loading screen stuck. No scene or prefab changes are required.

Additional headset checks: select each of the four areas, move with both
navigation arrows, return to the tour map, and rapidly select different areas.
Check a cold area load and confirm the destination is visible only after
the loading panel fades. Move between nodes with arrows and thumbstick teleport
and confirm there is no loading panel. Simulate an unavailable panorama for
both routes to check recovery.

Validation: Unity compilation, shader compilation, enabled scene paths and
Thai font glyph availability were checked. Test all four routes on Quest,
including repeated button presses, a cold tour panorama load, turning the
head during a transition, and returning from each mode. This is Unity-rendered
UI; main-thread activation stalls can also pause the progress display. It is
not an independent Quest compositor loading layer.

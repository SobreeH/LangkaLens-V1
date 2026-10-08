# MR scan tutorial

`MR_Scene` includes **MR Scan Tutorial** under the camera rig's TrackingSpace.
It plays `Assets/_LangkaLens/MR/Video/Scan_Display.mp4` automatically during
entrance alignment. The supplied 18-second clip is copied unchanged into the
project and loops silently.

The world-space panel is 0.72 x 0.405 metres, with its centre 0.9 metres left,
0.18 metres above and 1.6 metres ahead of the viewer's horizontal heading.
`GentleCanvasFollow` leaves small movements alone and eases larger movements.
Its 35-degree turn threshold lets visitors glance at the video without chasing
it. Adjust **Viewer Offset** on that component and the RectTransform dimensions
to tune headset comfort. Keep the 16:9 aspect ratio.

`MRScanTutorial` hides and stops playback during anchor confirmation and after
successful alignment. Restarting alignment shows it again from the beginning.
On-device tracking/focus loss also stops playback; the editor permits a preview
without a headset. Playback errors leave the existing alignment text available.
The panel has no colliders or raycast targets and does not consume input.
Its dedicated UI shader renders over unaligned scene geometry.
The video render target is 1280 x 720 and is released on teardown.

The setup menu **Tools > LangkaLens > MR > Set up scan video tutorial** adds the
configured object to MR_Scene if missing. Running it again restores its video
material and selects the existing object without overwriting placement adjustments.
No manual wiring is needed.

Validated in desktop Unity Play Mode: decoded video frames, visible panel,
separation from existing instructions, stopping after simulated alignment
confirmation, and resuming after BeginAlignment. No script compilation errors.
Actual Quest stereo appearance, tracking/focus handling, comfort and Android
video decoding still require a headset test. The editor session also reported
Meta bridge port conflicts and an app entitlement error unrelated to this feature.

# Four-corner MR placement

`MR_Scene` now uses four approximate corners of the physical base instead of the entrance sign.
The intended result is a stable, roughly centered reconstruction, not a surveyed reconstruction.
The floor area remains Unrestricted. Calibration lasts for this app session only.

## On-site controls

Stand facing the structure from the side you want to treat as its front. Keep that viewpoint's
corner names as you walk around: A near-left, B near-right, C far-right, D far-left.
Pick roughly matching corners at the same base level; you do not need to touch the ruins.

1. Hold the right grip to aim the green cross. Right stick up/down changes reach (up to 15 m).
2. Release grip to freeze the cross. Right stick adjusts it slowly in the horizontal plane.
   Hold the right index trigger while moving the stick vertically to adjust height.
3. Press A to capture the corner and create its session spatial anchor. B undoes the previous corner.
4. Repeat for all four corners. Look around each point while its anchor is created.
5. Review the translucent BJ3 model. Right stick shifts it in the model's horizontal axes;
   trigger + stick adjusts height and heading. Model proportions remain unchanged.
6. Y selects a corner; B replaces that selected corner if needed. A accepts the preview.
7. Select a model from the usual menu. B/Y removes it. Hold X for two seconds to restart setup.

The displayed corner distances describe the unadjusted best fit, before manual preview nudges.
The 75 cm mismatch message is advisory: it does not block confirmation.
Collapsed, reversed, or invalid corner layouts must be recaptured.

## Placement and tracking

- BJ3 (slot 2) inherits the calibrated model root directly. The old 14 x 16.5 m automatic
  rectangle fit is bypassed for it.
- BJ2, BJ8 and BW9 share the calibrated area as demonstrations. `MRManager > Demo Placements`
  provides independent size multipliers, heading, height and horizontal offsets in metres.
- One upright fit combines all four anchor positions. Uniform scale is estimated during setup
  and frozen when accepted. Minor pose changes are smoothed; the headset is never moved.
- All four anchors, headset tracking and passthrough must be available. Content is hidden on
  loss and resumes after 0.5 seconds of valid tracking. A sudden pose change or increased
  anchor disagreement requires A to review the placement, or hold X to start again.
- No cross-session anchor saving is implemented. Spatial tracking still needs testing on Quest
  at the site; this cannot guarantee zero drift.

## Editable model reference

Under `PlayableBoundary`, the inactive `BJ3 Calibration Reference (edit corners here)` contains
four child transforms A-D. They describe an approximate rectangle around the visible lower
platform, excluding the model's extra ground patch. They are not measured site coordinates.
Its initial model-local X coordinates are +/-0.46, Z is -0.44 to 0.36, and Y is 0.005.
Enable this reference only for authoring, then disable it again. Runtime also hides it.
Adjust the child corners if the model changes. Preserve A-B-C-D perimeter order.

`MRMeshReferenceAlignment` retains its entrance implementation for other scenes, but delegates
confirmation/tracking to `MRFourPointCalibration` when the new reference is assigned.
The new preview uses a transparent URP material and the existing Thai-capable Sarabun font.

## Verification

Tools > LangkaLens > MR > Run four-corner calibration checks exercises known transforms,
approximate measurements, fixed-scale tracking, invalid inputs, scene references and demo fitting.
The existing central display checks remain available for the legacy mode.

On a Quest build, verify capture, undo, corner replacement, preview nudges, confirmation,
all four model choices, B/Y removal, X restart, language switching, focus loss and recovery.
Walk around the structure and return to the starting position to assess actual stability.

Implementation validation: Unity compiled successfully; 51 foundation/demo checks and 25 existing
display checks passed. A Play Mode startup check confirmed that capture starts with no points,
the selection menu and ghost remain hidden without tracking, and the tracking message is shown.
The English and Thai capture panels were rendered and checked for overflow and missing glyphs.
The Editor was returned to Edit Mode. Physical capture, anchor recovery, drift and an Android
build have not been verified on the target device.

Unity regenerated Android feature references in `Assets/XR/Settings/OpenXRPackageSettings.asset`
during script reload; those generated changes were preserved. Reload also logged Meta bridge
port conflicts, and exiting Play Mode logged OVR session cleanup warnings. Neither was a C#
compilation error in the new calibration code.

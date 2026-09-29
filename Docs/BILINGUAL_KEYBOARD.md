# Reusable English / Thai VR keyboard

## Already integrated

`Assets/_LangkaLens/Mixed/3D Models/Prefabs/Center/BilingualVirtualKeyboard.prefab` is nested in the shared `Gemini_Canvas.prefab` and connected to its input field. MR, VR Tour, VR Museum, and AI test scenes reference that chatbot prefab and inherit this addition without scene-specific script changes.

The chatbot instance overrides the keyboard's standalone setup: its nested Canvas and GraphicRaycaster are removed, and the keyboard's PointableCanvas targets the chatbot's root Canvas. This is required by the installed Meta SDK's root-canvas hit filter. Keeping a separate nested Canvas as the PointableCanvas target makes the keyboard visible but rejects its key hits. The keyboard keeps its own ray surface and remains inside the reusable chatbot prefab.

Selecting the chat input opens the custom keyboard below the chat panel. Native keyboard opening is suppressed only on fields with a custom keyboard assigned. The existing Quest system-keyboard implementation remains the fallback when that reference is empty.

## Use in another scene or with another input

1. Add an instance of `BilingualVirtualKeyboard.prefab` to the scene. It has its own world-space canvas, GraphicRaycaster, and Meta ray-canvas interaction.
2. Add `QuestKeyboardHandler` to a standard text `TMP_InputField`.
3. Assign the local keyboard instance to the handler's **Custom Keyboard** field.
4. Use a TMP font that contains English and Thai. The supplied key labels use the project's Sarabun font.
5. Position the keyboard where it is comfortable to read and point at. Its standalone size is 2.0 by 0.8 metres at the supplied 0.001 scale; scale the root uniformly to fit the scene. Keep a standalone keyboard outside other Canvases. For the chatbot, reuse the whole `Gemini_Canvas.prefab`; its nested keyboard already has the required Canvas overrides and local scale 1.

The scene needs one EventSystem with Meta `PointableCanvasModule` and an interaction rig with controller/hand ray interactors. The keyboard prefab does not create extra EventSystems or rigs. For additive scenes, provide those shared scene services only once.

Multiple input fields may reference the same scene-local keyboard; the last selected field becomes its target. Separate keyboard instances keep their own language, Shift state, text target, and event subscriptions. There are no global target references, `DontDestroyOnLoad` objects, or cross-scene object references.

## Controls

- **EN / ไทย:** switch between English QWERTY and Thai Kedmanee.
- **Shift:** toggle uppercase/symbols or the shifted Thai layout. Shift stays on until tapped again and resets when switching languages or reopening.
- **Backspace:** delete the selection or the preceding Unicode character. Thai tone/vowel marks are removed individually; UTF-16 surrogate pairs are not split.
- **< / >:** move the insertion cursor or collapse a selection.
- **Space / เว้นวรรค:** insert a space.
- **Enter / ส่ง:** invoke the target input's `onSubmit` once and close the keyboard. In the chatbot this sends the message when **Submit On Done** is enabled.
- **Close / ปิด:** close while preserving the text.

The preview's `|` marks the insertion point. Combining vowel/tone keycaps use `อ` as a visible base; pressing such a key inserts only the vowel/tone mark, not that display base.

This is a free-text keyboard for chat/search fields. It is not an IME with prediction, autocorrection, or custom numeric/email validation. Enter submits rather than inserting a newline. It does not add speech recognition or change the current dictation implementation.

Layout reference: [Unicode CLDR Thai keyboard layouts](https://www.unicode.org/cldr/charts/43/keyboards/layouts/th.html).

## Validation

- September 29 ray-input regression: the original chatbot failed Meta's root-canvas hit filter. After correcting the prefab, all 55 keys passed actual GraphicRaycaster hits and the installed PointableCanvasModule filter. Run **Tools > LangkaLens > Validate Chatbot Keyboard Ray Routing** in Edit Mode to repeat this check. It uses an isolated preview scene and sends no API requests.
- The 14 offline typing checks passed again after the routing correction; the corrected chatbot keyboard also remained open after Play Mode initialization.

- Unity compiled the scripts.
- 14 offline Play Mode checks passed: English and Thai insertion, Shift, tone-mark deletion, selection replacement, caret editing, surrogate handling, character limits, single submission, reopen cleanup, retargeting, disable cleanup, and independent instances.
- The actual chatbot prefab passed pointer-click/focus checks across frames, including preserving the cursor when a keyboard key becomes selected, closing, reopening, and resetting editing feedback.
- English and Thai UI previews were rendered and inspected; no missing Thai key glyphs were found.
- No ISDK assertion errors were found during the checks.

To rerun the offline checks, enter Play Mode in an ISDK scene and choose **Tools > LangkaLens > Validate Bilingual Keyboard (Play Mode)**. The test creates temporary objects and sends no API requests.

On Quest 3, build a fresh APK and test controller rays, hand rays, readability, language switching, `สวัสดี`, Shift, Backspace, Enter, and reopening. Switch between MR, VR Tour, and VR Museum to verify placement and local targeting. Hardware ray interaction and comfort have not yet been verified.

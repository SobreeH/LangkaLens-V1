# Quest chatbot verification

## Current investigation: Groq speech, 2026-09-28

The user has migrated custom speech to Groq Whisper. The native Quest keyboard is confirmed working by the user; preserve its Activity/TMP setup.

### Finding and correction

The Groq prefab contained a Groq credential, but the VR Tour scene retained an `apiKey` override on the speech component. `[FormerlySerializedAs("apiKey")]` migrated that old, non-Groq credential into `groqApiKey`, overriding the correct prefab credential. This is a confirmed configuration defect in the Unity Editor; the original headset HTTP response was not captured, so it is not yet a confirmed explanation of every headset failure.

Removed the credential rename attribute: credentials for different providers must not migrate as if they were the same setting. Freshly loading the saved VR Tour scene now resolves the prefab's Groq credential. The previously loaded scene retained its old in-memory value after recompilation; it was reloaded only after confirming there were no unsaved scene changes. No scene or prefab file was edited by this fix.

Additional changes in `SpeechToTextHandler.cs`:

- Validate the Groq credential format and input reference before recording. Editor PlayerPrefs are not transferred to a Quest build.
- Handle microphone startup failure; use real time for recording/status timing and leave two seconds of buffer headroom. Retain the last observed sample position if the microphone stops before the next frame.
- Check PCM reads, validate sample bounds, release audio clips, and log WAV size/peak/RMS without audio contents. All-zero PCM is reported without uploading it. Nonzero noise still requires a real speech test.
- Keep error messages visible instead of immediately resetting the button. Parse failures and empty transcripts are separate outcomes.
- Abort pending transcription and clear recording state when the chatbot is disabled.
- Log HTTP status/category and input-update character counts. Do not log response bodies, credentials, audio, or transcription text in this component. Existing chat logging is unchanged.

### Evidence by stage

| Stage | Verified | Still unknown |
| --- | --- | --- |
| Permission | Connected Quest 3 has RECORD_AUDIO and INTERNET granted. AppOps recorded a recent allowed microphone session lasting about 7.4 seconds. | Permission after a fresh install, hardware mute and audible samples during the failing attempt. |
| Recording | Microphone access occurred; startup/zero-frame diagnostics added. | Actual Quest sample count and signal level, manual and automatic stop behavior. |
| WAV | Unity synthetic stereo test passed trimming, RIFF/data sizes, rate, channels, sample amplitudes and out-of-range rejection. | Real headset clip contents. |
| API configuration | Wrong scene credential migration reproduced. Fresh saved-scene load now matches the Groq prefab credential. | Fresh APK behavior. |
| Network/authentication | Desktop GET of Groq's `whisper-large-v3` model with the prefab credential returned HTTP 200. No credentials printed by the probe. | Headset connectivity and transcription POST result/quota. A successful model GET does not prove transcription access. |
| Parsing/input | Code distinguishes malformed response, empty text, missing field and successful assignment. Prefab/scene field references are assigned. | Real Groq response parsing and text appearing on Quest. |

Unity 6000.3.16f1, Android target: compilation passed. The working keyboard script, chat script, manifest, Player Settings, Meta setup settings, Gemini prefab and VR Tour scene were checked against pre-edit hashes and remained unchanged. All pre-existing uncommitted changes were retained.

Synthetic live transcription was not completed: Windows speech synthesis could not access a voice; the subsequent upload attempt was not executed because automatic approval review hit its usage limit. No new APK was built or installed in this investigation. Existing headset logs contained no matching speech entries, and the app was not running when checked.

### Recommended service

Keep the existing Groq integration with `whisper-large-v3` while validating the correction. It is a dedicated multilingual transcription service; switching providers again would not fix the scene credential migration.

- Groq lists `whisper-large-v3` at **US$0.111/audio hour** and `whisper-large-v3-turbo` at **US$0.04/audio hour**, with a **10-second minimum billed length per request**. English and Thai are Whisper language codes `en` and `th`. Keep v3 for the initial accuracy test; consider Turbo after comparing real Thai samples. [Groq speech documentation](https://console.groq.com/docs/speech-to-text), [Whisper language definitions](https://github.com/openai/whisper/blob/main/whisper/tokenizer.py).
- Authentication requires a Groq account/API key, sent as `Authorization: Bearer ...`; it does not use a Gemini key. HTTPS multipart WAV upload is platform independent, so this integration does not depend on Android SpeechRecognizer or Google Play Services. This is architectural compatibility, not a completed Quest end-to-end test. [Groq quickstart](https://console.groq.com/docs/quickstart).
- Quest's native keyboard overlay supports voice dictation on Quest 3 and needs no app-managed STT API key. It is the simplest option where the headset exposes it. Meta's documentation does not establish Thai dictation availability for this headset/account/region, so it cannot currently replace the English-and-Thai requirement with confidence. Test its microphone button separately; keyboard language support does not prove dictation language support. [Meta keyboard overlay](https://developers.meta.com/horizon/documentation/unity/unity-keyboard-overlay/).
- For a distributed app, keep the provider credential on an authenticated backend, with the headset uploading WAV to that backend. A serialized key or PlayerPrefs is extractable from a client; the existing development configuration is retained here. No backend or new paid service was provisioned.

### Next headset test and diagnosis

1. Build and install a fresh Android APK with the current Groq changes. Keep Activity, the matching manifest and the ignored GameActivity recommendation intact.
2. Confirm native keyboard typing, Done, Cancel and reopening still work with controller and hand rays.
3. Select English, press Speak, speak for 3–5 seconds and press Speak again. Expected log progression: microphone started → `stage=wav` with nonzero peak/RMS → `stage=http status=200` → `stage=input applied transcript`. Transcript should append once without submitting the chat.
4. Repeat in Thai with the Thai locale selected. Test a historical place name and ordinary speech separately. Then send the text to chat.
5. Let recording reach the 15-second limit; repeat while the menu pauses game time. Close/reopen the chatbot during recording and during transcription; it must recover and allow another attempt.
6. Check muted/silent recording, permission denied, and offline cases. Errors must remain visible, and the next attempt must work after correcting the condition.

Use device Logcat filtered to `[SpeechToTextHandler]` (do not clear the buffer before capturing the failure). Share the stage, numeric HTTP status and category, not keys or request headers. Interpretation:

- `stage=config`: missing/non-Groq credential or stale override; recording has not started.
- `stage=permission`: microphone access not granted; allow it, then press Speak again.
- No microphone devices, startup failure, or zero captured frames: microphone/recording stage.
- `stage=wav` with zero peak: silent PCM; investigate microphone mute/ownership. Nonzero levels alone do not prove intelligible speech.
- HTTP 0: transport/TLS/timeout; 400: request/audio validation; 401: authentication; 403: access; 404: model; 429: rate limit; 5xx: service failure.
- HTTP 200 followed by `stage=parse`: response shape or empty transcript.
- `stage=input applied transcript` but no visible text: investigate the active field instance and subsequent UI/keyboard changes.

## Historical Gemini changes (superseded for custom speech)

- `QuestKeyboardHandler` lets TMP own the system keyboard and synchronize text. It retains editing feedback and the chat submission preference.
- The Gemini canvas prefab allows the soft keyboard.
- Speech requests use Newtonsoft JSON with null omission, so each part contains either audio or text. The existing Thai/English prompts are preserved.
- The prefab uses `gemini-3.6-flash` for transcription and `gemini-3.5-flash-lite` for chat, matching the scripts' defaults instead of the retired preview saved in the prefab.
- Transcription has a 60-second request timeout, reports HTTP status and response errors, and collects non-thought text parts without emitting duplicate input-change events.
- Microphone recording waits for permission; after allowing permission, tap Speak again.

## Activity workaround

Android Player Settings uses **Activity**, and the custom manifest uses `com.unity3d.player.UnityPlayerActivity`. Focus awareness, keyboard overlay support, and microphone permission remain enabled.

Meta's manifest updater was run first, but its current implementation only converts Activity to GameActivity. It does not convert back. The manifest activity and theme were therefore corrected explicitly.

The single Meta setup recommendation `Always specify single "GameActivity" application entry on Unity 2023.2+` is ignored intentionally for this workaround. Other checks remain enabled. Do not re-enable this recommendation until testing confirms that the keyboard works with GameActivity on the project's Unity and headset versions.

Reference: [Meta's GameActivity keyboard investigation](https://developers.meta.com/horizon/feedback/vr/investigations/2279537085906028/). The user subsequently confirmed the native keyboard works on the headset.

## Historical Gemini validation

- Unity compiled the modified scripts and entered/exited Play Mode.
- A serialization probe verified separate audio-only and text-only parts, including Thai text, quotes, and newlines.
- The saved prefab and its instance in VR Tour use the updated models, and the input reference is assigned.
- Activity is saved in Player Settings and matches the custom manifest.
- No APK build, headset keyboard test, or live Gemini transcription request was performed.

The Play Mode check also found unrelated Editor issues: Meta bridge ports already in use, XR capability warnings, and `VRTeleportNavigator.HandleInput` using the legacy Input API while the project selects the new Input System. That keyboard fallback is inside `UNITY_EDITOR`; it is not included in the Quest APK.

## Historical Gemini headset procedure (use Groq procedure above now)

1. Build a fresh Android APK using the existing build scenes and install it on Quest 3.
2. Open the chatbot and select the input field with a controller ray. Confirm that one system keyboard opens.
3. Type text, press Done, and confirm that the chat submits once. Reopen the field and test Cancel and reopening again.
4. Repeat input selection using a hand-tracking ray.
5. Press Speak. Allow microphone permission if requested, then press Speak again.
6. Record a short sentence and press Speak again to stop. Confirm the transcript appears once in the input field. Test both English and Thai.
7. Press Send and confirm that chat responds.

If transcription fails, capture the `[SpeechToTextHandler] Gemini HTTP ...` response from the device log. This distinguishes request-format problems from API-key, model-access, quota, or service errors.

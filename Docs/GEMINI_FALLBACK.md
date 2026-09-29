# Gemini chat fallback

The shared `Gemini_Canvas` prefab enables a single fallback attempt using
`gemini-3.1-flash-lite`. The existing primary model and scene overrides are preserved.
Change **Enable Fallback** and **Fallback Model** in the GeminiChatUI Inspector.

- HTTP 404, 408, 429 and 5xx from the primary trigger the backup once.
- The backup uses the same API key, question, localized instructions and token limit.
- Authentication errors, malformed requests, local connection failures, empty
  responses and safety blocks do not trigger fallback.
- Blank or identical fallback model names are skipped.
- The UI displays a localized backup status, keeps Cancel available, and continues
  blocking additional submissions throughout both attempts.
- Each attempt has a 60-second timeout. A failed backup produces the final error;
  there is no recursive retry. The next question starts with the primary again.
- Both models may be unavailable or share an exhausted quota. Fallback does not
  guarantee service availability, and successful backup requests use normal quota.

## Verification

Run `Tools/Tests/Test-GeminiFallback.ps1` with PowerShell. It extracts the production
request coroutine and runs it against in-memory HTTP doubles without credentials
or network access. Covers primary success, recoverable and non-recoverable errors,
backup success/failure, duplicate/blank configuration, unchanged payload, safety
blocks, disposal and cancellation during either attempt.

Unity compilation and prefab/scene inheritance are checked separately. A live
Gemini request and headset validation have not been performed.

For a live manual check, use a temporary scene-instance override with a nonexistent
primary model and a valid fallback. Sending a question should receive a 404 from
the primary, show the backup status and display one answer. Cancel during a pending
attempt and verify Send is restored. Restore the primary model override afterwards.

# Conventional Commit & Project Hygiene Rules

## 1. Conventional Commits Structure

Every commit message must strictly adhere to the Conventional Commits format:

```text
<type>(<optional scope>): <imperative subject>

[optional body: bullet points detailing key changes and rationale]

[optional footer: e.g., BREAKING CHANGE: ..., Closes #123]
```

### Commit Types

| Type | When to Use | Example |
| :--- | :--- | :--- |
| `feat` | New feature or functional addition | `feat(vr-museum): add model inspection toggle` |
| `fix` | Bug fix or error resolution | `fix(spawner): align spawned models to playable area` |
| `refactor` | Code reorganization or cleanup without behavior changes | `refactor(scenes): adjust MainCanvas position and clean up placement` |
| `chore` | Routine tasks, project settings, build tool configs, or deps | `chore(settings): update project organization ID and OpenXR settings` |
| `perf` | Changes that improve performance or reduce memory footprint | `perf(mesh): reduce polygon count in background models` |
| `docs` | Documentation changes only (README, diagrams, notes) | `docs: update AI prompt flowchart and storyboard` |
| `style` | Formatting, whitespace, or code styling without logic change | `style(scripts): format MRModelSpawner code` |
| `test` | Adding, updating, or fixing unit/integration tests | `test(input): add playmode tests for ray interaction` |
| `ci` | CI/CD configuration files and scripts | `ci: add GitHub Actions workflow for automated builds` |

### Subject Line Rules
1. **Imperative Mood**: Use imperative verbs ("add", "fix", "update", "remove", "refactor") instead of past tense ("added") or progressive ("adding").
2. **Case**: Lowercase immediately following the colon and space (`feat: add...` not `feat: Add...`).
3. **No Trailing Period**: Do not end the subject line with a period (`.`).
4. **Length**: Keep the subject line concise and under 72 characters (aim for <= 50 characters when possible).

### Body Guidelines
- Include a body if the commit involves multiple notable adjustments or requires rationale explaining *why* the change was made.
- Separate the subject from the body with a single blank line.
- Use concise bullet points:
  ```text
  refactor(scenes): adjust MainCanvas position and clean up MR model placement

  - Move MainCanvas Z-position from 5 to 8 for improved VR visibility.
  - Remove redundant root Model_placement GameObject from MR_Scene.
  ```

---

## 2. Unity Project Git Hygiene

### Paired `.meta` Files
- In Unity, every asset or directory in `Assets/` and `Packages/` has a corresponding `.meta` file containing its GUID.
- **Rule**: If an asset (script, scene, texture, prefab, audio, etc.) is added, renamed, moved, or deleted, its corresponding `.meta` file **MUST** be staged and committed together with it.
- Never commit an asset without its `.meta`, and never commit a `.meta` file as an orphan without its asset.

### Ignored Directories
Never commit internal Unity cache and temporary folders:
- `Library/`
- `Temp/`
- `Logs/`
- `Builds/`
- `UserSettings/`
- `.utmp/`
- Visual Studio / IDE caches (`.vs/`, `.idea/`, `*.csproj`, `*.sln`) unless explicitly required by the repository standard.

### Scene (`.unity`) & Prefab (`.prefab`) Diffs
- Unity scene and prefab files are YAML text files.
- Before committing scene changes, inspect `git diff <SceneName>.unity` to identify:
  - Which GameObjects or Components were touched (e.g. `Transform`, `Canvas`, `MonoBehaviour`).
  - Whether changes were deliberate (e.g. repositioning a canvas, reordering elements) or accidental editor touch-ups.
  - Summarize the specific scene components in the commit message scope or body.

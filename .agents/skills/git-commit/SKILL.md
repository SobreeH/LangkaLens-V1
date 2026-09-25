---
name: git-commit
description: >-
  Slash action shortcut (/git-commit) to inspect git repository changes, analyze diffs, automatically split changes into an appropriate number of clean atomic Conventional Commits (never one big commit), and stage and commit changes safely. Use when invoked with /git-commit or whenever the user asks to check git status, create commits, review uncommitted changes, or commit modified files.
---

# /git-commit Workflow

This skill provides a systematic runbook and slash action shortcut (`/git-commit`) for inspecting git working tree changes, **always splitting changes into an appropriate number of clean atomic commits** (never one giant catch-all commit), crafting descriptive Conventional Commit messages, and committing them safely.

Detailed commit conventions, Unity-specific git hygiene rules, and atomic splitting guidelines are documented in [references/commit-rules.md](references/commit-rules.md).

---

## Workflow Steps

### Step 1: Inspect Working Tree & Diffs

1. Check the current repository state:
   ```bash
   git status -s
   ```
2. If there are staged changes:
   ```bash
   git diff --cached
   ```
3. If there are unstaged changes:
   ```bash
   git diff
   ```
4. Check recent commit history to match repository styling and scoping conventions:
   ```bash
   git log -n 5 --oneline
   ```

### Step 2: Analyze & Group Changes into Atomic Units

> [!IMPORTANT]
> **Always Split into Appropriate Atomic Commits by Default**  
> Never bundle changes spanning distinct concerns, multiple features, bug fixes, localization assets, scene layouts, or project settings into a single "catch-all" or "big" commit. Always partition them into an appropriate number of focused, atomic commits.

1. **Classify changes by logical concern**:
   - **Localization & String Tables**: `Assets/.../Localization/Tables/`, Addressable asset group mappings, string table collections, and shared data (`feat(localization)` or `feat(<scope>)`).
   - **Core Feature / Script Logic**: C# scripts (`Assets/.../Scripts/`) introducing or extending feature behavior (`feat(<scope>)`).
   - **Bug Fixes**: Standalone bug fixes, text/markup adjustments, or syntax corrections (`fix(<scope>)`).
   - **Scene & Prefab Layouts**: Scene files (`.unity`) and prefabs (`.prefab`) modifying UI hierarchy, camera setup, positioning, or visual components (`refactor(scenes)` or `feat(scenes)`).
   - **Editor & Tooling**: Editor scripts (`Editor/*.cs`), automated checks, or menu items (`feat(editor)` or `refactor(editor)`).
   - **Project Configuration**: `ProjectSettings/`, packages, or build configs (`chore(settings)` or `chore(...)`).

2. **Handle Multi-Concern Files / Scenes**:
   - If a scene (`.unity`) or file contains changes supporting multiple distinct concerns (e.g. script component wiring for a new feature AND separate UI canvas layout adjustments), split the hunks cleanly:
     - Stage the component reference wiring with the relevant feature commit.
     - Stage the layout/transform overrides with the scene/prefab layout commit.

3. **Unity-specific asset checks**:
   - Ensure every modified/added/deleted asset in `Assets/` or `Packages/` is staged with its matching `.meta` file.
   - Ensure non-project files (e.g., `Library/`, `Temp/`, user settings) are untracked or ignored.

4. **Plan commit sequence**:
   - Order commits logically (e.g. foundation/localization/dependencies -> core feature logic -> bug fixes -> scene/prefab layout).

### Step 3: Formulate Conventional Commit Messages

Construct messages following the Conventional Commits specification:
`<type>(<optional scope>): <imperative summary>`

- **Types**: `feat`, `fix`, `refactor`, `chore`, `perf`, `docs`, `style`, `test`
- **Scope**: Lowercase component or feature (e.g., `mr`, `gemini-ui`, `scenes`, `vr-tour`, `localization`, `settings`)
- **Summary**: Imperative mood, lowercase, no ending period, <= 72 characters (e.g., `add localized strings for site alignment and boundary feedback`)
- **Body**: Bulleted details describing what was changed and why.

Refer to [references/commit-rules.md](references/commit-rules.md) for full reference tables and examples.

### Step 4: Stage & Commit Iteratively

For each logical group in your planned sequence:

1. Stage only the files (and `.meta`s or hunks) intended for this commit:
   ```bash
   git add <file1> <file2> ...
   ```
2. Verify staged diff matches the intended atomic scope:
   ```bash
   git diff --cached
   ```
3. Commit with the formulated message and body details:
   ```bash
   git commit -m "<type>(<scope>): <summary>" -m "- <detail 1>" -m "- <detail 2>"
   ```
4. Repeat until all changes from the working tree are cleanly committed.

### Step 5: Verify Status

1. Confirm working tree status is completely clean:
   ```bash
   git status
   ```
2. Verify all newly created commits in the log:
   ```bash
   git log -n <number_of_commits> --oneline
   ```
3. Report the full breakdown of newly created commits (hashes, messages, and files) clearly to the user.

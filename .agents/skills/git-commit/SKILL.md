---
name: git-commit
description: >-
  Inspect git repository changes, analyze diffs, group changes into logical units, formulate Conventional Commit messages, and stage and commit changes safely. Use whenever the user asks to check git status, create commits, review uncommitted changes, or commit modified files.
---

# Git Commit Workflow

This skill provides a systematic runbook for inspecting git working tree changes, grouping changes into clean atomic commits, crafting descriptive Conventional Commit messages, and committing them safely.

Detailed commit conventions and Unity-specific git hygiene rules are documented in [references/commit-rules.md](references/commit-rules.md).

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

### Step 2: Analyze & Group Changes

1. **Identify logical concerns**:
   - Are changes related to a single feature or fix? -> Single commit.
   - Do changes span multiple independent areas (e.g., project configuration vs. scene edits vs. script logic)? -> Split into separate logical commits.
2. **Unity-specific asset checks**:
   - Ensure every modified/added/deleted asset in `Assets/` or `Packages/` is staged with its matching `.meta` file.
   - Ensure non-project files (e.g., `Library/`, `Temp/`, user settings) are untracked or ignored.
3. **Determine grouping strategy**:
   - If grouping is obvious (e.g. 1-2 tightly coupled files), prepare the commit directly.
   - If there are 3+ files across distinct domains (e.g. `ProjectSettings/` vs `Scenes/`), propose the split or ask the user for their grouping preference before committing.

### Step 3: Formulate Conventional Commit Message

Construct messages following the Conventional Commits specification:
`<type>(<optional scope>): <imperative summary>`

- **Types**: `feat`, `fix`, `refactor`, `chore`, `perf`, `docs`, `style`, `test`
- **Scope**: Lowercase component or feature (e.g., `settings`, `vr-museum`, `scenes`, `spawner`)
- **Summary**: Imperative mood, lowercase, no ending period, <= 72 characters (e.g., `update project organization ID and OpenXR settings`)
- **Body**: (Optional) Bulleted details if the change involves multiple non-obvious tweaks.

Refer to [references/commit-rules.md](references/commit-rules.md) for full reference tables and examples.

### Step 4: Stage & Commit

1. Stage only the files intended for this commit:
   ```bash
   git add <file1> <file2> ...
   ```
2. Commit with the formulated message:
   ```bash
   git commit -m "<message>"
   ```
   *For multi-line messages with a body, format cleanly or pass multiple `-m` flags:*
   ```bash
   git commit -m "<type>(<scope>): <summary>" -m "- <detail 1>" -m "- <detail 2>"
   ```
3. Repeat for remaining groups if multiple commits were planned.

### Step 5: Verify Status

1. Confirm working tree status:
   ```bash
   git status
   ```
2. Verify recent commits in the log:
   ```bash
   git log -n 3 --oneline
   ```
3. Report the newly created commit hashes and messages clearly to the user.

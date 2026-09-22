---
name: commit
description: >-
  Slash action shortcut (/commit) to check git working tree status, analyze diffs, automatically split changes into an appropriate number of clean atomic Conventional Commits (never one big commit), and stage and commit them safely. Use when invoked with /commit or when the user asks to commit changes.
---

# /commit Shortcut

This skill enables the `/commit` slash action shortcut to quickly inspect, **always split into an appropriate number of clean atomic commits** (never one giant commit), and commit git changes safely using Conventional Commits.

Detailed commit conventions, Unity-specific rules, and atomic splitting guidelines are documented in [references/commit-rules.md](references/commit-rules.md).

---

## Workflow Steps

### Step 1: Inspect Working Tree & Diffs
1. Check repository state:
   ```bash
   git status -s
   ```
2. Inspect changes:
   ```bash
   git diff
   git diff --cached
   ```
3. Check recent commits for scoping style:
   ```bash
   git log -n 5 --oneline
   ```

### Step 2: Group Changes into Atomic Units
- **Always split into an appropriate number of atomic commits by default.** Never bundle multiple unrelated features, fixes, localization tables, scene layouts, and settings into one catch-all commit.
- Classify by concern:
  - **Localization & String Tables**: Tables, shared data, Addressable groups (`feat(localization)` or `feat(<scope>)`).
  - **Core Script Logic**: C# scripts implementing features (`feat(<scope>)`).
  - **Bug Fixes**: Isolated syntax, markup, or runtime fixes (`fix(<scope>)`).
  - **Scene & Prefab Layout**: Scene transforms, canvas layouts, UI hierarchy (`refactor(scenes)` or `feat(scenes)`).
  - **Editor & Tooling**: Editor utilities and menus (`feat(editor)` or `refactor(editor)`).
  - **Configuration**: Settings, packages, configs (`chore(settings)` or `chore(...)`).
- For scenes or files with multiple concerns, split and stage hunks selectively to preserve atomic commits.
- Ensure every Unity asset in `Assets/` is staged with its paired `.meta` file.

### Step 3: Formulate Conventional Commit Messages
- Format: `<type>(<optional scope>): <imperative summary>`
- Types: `feat`, `fix`, `refactor`, `chore`, `perf`, `docs`, `style`, `test`
- Imperative verbs, lowercase after colon, no ending period, <= 72 chars.
- Include bulleted body details for non-trivial commits.

### Step 4: Stage, Commit Iteratively & Verify
1. For each logical group, stage target files/hunks:
   ```bash
   git add <files>
   ```
2. Commit with message and body:
   ```bash
   git commit -m "<type>(<scope>): <summary>" -m "- <detail 1>" -m "- <detail 2>"
   ```
3. Repeat until working tree is completely clean.
4. Verify final status and commit log:
   ```bash
   git status
   git log -n <count> --oneline
   ```
5. Report the full breakdown of created commits to the user.

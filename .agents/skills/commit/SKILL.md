---
name: commit
description: >-
  Slash action shortcut (/commit) to check git working tree status, analyze diffs, group changes into clean Conventional Commits, and stage and commit them safely. Use when invoked with /commit or when the user asks to commit changes.
---

# /commit Shortcut

This skill enables the `/commit` slash action shortcut to quickly inspect, group, and commit git changes using Conventional Commits.

Detailed commit conventions and Unity-specific rules are documented in [references/commit-rules.md](references/commit-rules.md).

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
3. Check recent commits:
   ```bash
   git log -n 5 --oneline
   ```

### Step 2: Group Changes & Validate Assets
- Ensure modified Unity assets in `Assets/` are staged with their paired `.meta` files.
- Group independent changes logically (e.g. settings vs scene edits vs scripts).
- If multiple distinct groups exist, confirm preference with the user.

### Step 3: Formulate Conventional Commit Message
- Format: `<type>(<optional scope>): <imperative summary>`
- Types: `feat`, `fix`, `refactor`, `chore`, `perf`, `docs`, `style`, `test`
- Imperative verbs, lowercase, no ending period, <= 72 chars.

### Step 4: Stage, Commit & Verify
1. Stage target files:
   ```bash
   git add <files>
   ```
2. Commit:
   ```bash
   git commit -m "<message>"
   ```
3. Verify clean state:
   ```bash
   git status
   git log -n 1 --oneline
   ```

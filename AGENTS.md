# Project Agent Guidelines: LangkaLens

## Mandatory Instruction: Always Use Unity MCP for Edits & Unity Operations

When working in this project, **always prioritize and use Unity MCP (`unity-mcp`) tools** for easier, safer, and synchronized edits inside the Unity Editor.

### 1. Script Management & Code Edits
- **Creating Scripts**: Use `Unity_CreateScript` to ensure script templates and meta files are cleanly registered in Unity.
- **Editing Scripts**: Use `Unity_ScriptApplyEdits` or `Unity_ApplyTextEdits` for targeted script modifications.
- **Validation & Compilation**: Immediately following any script creation or edit, call `Unity_ValidateScript` and/or `Unity_GetConsoleLogs` / `Unity_ReadConsole` to verify there are no compilation errors or breaking warnings.

### 2. Scene & GameObject Manipulation
- **GameObjects & Components**: Use `Unity_ManageGameObject` to create, modify, inspect components, and adjust transforms directly within the active scene hierarchy.
- **Scenes**: Use `Unity_ManageScene` to open, save, or switch scenes safely.
- **Meta VR & Interaction SDK**: Use the Meta MCP tools provided by unity-mcp (`meta_add_camerarig`, `meta_add_interactionrig`, `meta_add_canvas_interaction_poke`, `meta_add_distance_grabbable`, `meta_add_grabbable`, etc.) for VR/MR setups.

### 3. Asset & Project Inspection
- **Project Exploration**: Use `Unity_GetProjectData` and `Unity_FindProjectAssets` to search and locate assets in the project hierarchy.
- **Asset Management**: Use `Unity_ManageAsset` for importing, modifying, or querying asset properties.

### 4. Console & Diagnostics
- Always consult `Unity_GetConsoleLogs` or `Unity_ReadConsole` to diagnose issues or verify editor runtime health.

### 5. Fallback Policy
- Direct filesystem editing (`edit_file`, `write_file`) is reserved only for non-Unity configuration files (e.g. markdown docs, JSON configs) or as a temporary fallback if the Unity Editor relay is not currently running.


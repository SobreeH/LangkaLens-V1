# LangkaLens Project Rules

## Priority Rule: Always Use Unity MCP for Unity Edits

In this project, always use the **Unity MCP (`unity-mcp`)** tool suite for interacting with and editing the Unity project.

### Core Guidelines:
1. **Script Edits & Creation**:
   - Prefer `Unity_ScriptApplyEdits` and `Unity_ApplyTextEdits` for modifying C# scripts.
   - Use `Unity_CreateScript` when creating new MonoBehaviour or C# script assets.
   - Always run `Unity_ValidateScript` and check `Unity_GetConsoleLogs` / `Unity_ReadConsole` after script edits to confirm compilation and zero console errors.

2. **Scene & Hierarchy Operations**:
   - Use `Unity_ManageGameObject` to inspect, create, remove, or modify GameObjects and components in the active scene.
   - Use `Unity_ManageScene` to inspect, open, and save scenes.

3. **VR / Meta Interactions**:
   - Use the available `meta_*` tools (`meta_add_camerarig`, `meta_add_interactionrig`, `meta_add_canvas_interaction_poke`, etc.) for configuring Quest VR/MR rigs and interactables.

4. **Asset Discovery**:
   - Use `Unity_FindProjectAssets` and `Unity_GetProjectData` to explore assets and paths across the project.


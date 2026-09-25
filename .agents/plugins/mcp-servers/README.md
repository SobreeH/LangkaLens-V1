# Model Context Protocol (MCP) Reference Servers

This plugin configures reference MCP servers from [modelcontextprotocol/servers](https://github.com/modelcontextprotocol/servers).

## Installed Servers

1. **Git (`mcp-server-git`)**
   - **Purpose**: Direct inspection of repository status, unstaged and staged diffs, commit history, and branches.
   - **Environment**: Configured via the project's dedicated Python virtual environment (`.mcp-git/.venv/`).
   - **Tools Exposed**:
     - `git_status`: Inspect changed, untracked, and staged files.
     - `git_diff_unstaged`: Inspect working tree modifications without staging.
     - `git_diff_staged`: Review staged diffs.
     - `git_diff`: Compare branches, commits, or files.
     - `git_log`: View commit history with timestamp/count filtering.
     - `git_show`: View commit details by SHA.
     - `git_branch`: List and inspect local and remote branches.
     - `git_commit`, `git_add`, `git_reset`, `git_create_branch`, `git_checkout`.

2. **Memory (`@modelcontextprotocol/server-memory`)**
   - **Purpose**: Persistent knowledge graph memory across agent turns and sessions.
   - **Storage**: `~/.gemini/memory.jsonl` (configured via `MEMORY_FILE_PATH`).
   - **Tools Exposed**:
     - `create_entities`: Save key concepts, architecture, or entities.
     - `create_relations`: Link entities with relationship types.
     - `add_observations`: Append discoveries and notes to existing entities.
     - `read_graph`: View the entire knowledge graph.
     - `search_nodes`: Search entities and observations by keywords.
     - `open_nodes`: Retrieve specific nodes by name.
     - `delete_entities`, `delete_observations`, `delete_relations`: Manage graph lifecycle.

2. **Sequential Thinking (`@modelcontextprotocol/server-sequential-thinking`)**
   - **Purpose**: Dynamic, reflective step-by-step reasoning for complex problem solving.
   - **Tools Exposed**:
     - `sequentialthinking`: Structured thought sequence supporting branching, revision, and hypothesis verification.

## Configuration Scope

- **Global Config**: `~/.gemini/config/mcp_config.json`
- **Workspace Plugin Config**: `.agents/plugins/mcp-servers/mcp_config.json`
- **Tool Schemas**: Stored in `~/.gemini/antigravity-ide/mcp/`

## Adding Additional Servers

To add more servers from `modelcontextprotocol/servers` (e.g. `filesystem`, `fetch`, `time`, etc.):
1. Install globally or run via `npx`:
   ```bash
   npm install -g @modelcontextprotocol/<package-name>
   ```
2. Add the entry to `mcp_config.json`:
   ```json
   "server-name": {
     "command": "node",
     "args": ["<path-to-dist-index.js>"]
   }
   ```
3. Reload Antigravity IDE (`Ctrl+Shift+P` -> `Developer: Reload Window`).

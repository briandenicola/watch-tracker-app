# MCP Server (connect an AI assistant)

WatchTracker has a built-in [MCP](https://modelcontextprotocol.io) server. It lets an assistant such as Hermes Agent look at your collection and use WatchTracker's AI agents on your behalf. Nothing here can change your watches, wish list or wear log.

## Turn it on

1. **Admin:** Admin → App Settings → **MCP Server** → set `McpServerEnabled` to **Enabled** and save. It is off by default; while off, the endpoint answers `503`.
2. **You:** Settings → **API Keys** → pick an access level and create a key. Copy it right away.

| Access | Can do |
|---|---|
| Read only | Look at watches, wish list, wear log, stored review, style memory |
| Read + AI agents | All of the above, plus run the AI agents |

Keys made before this feature are read only. Browser sign-ins cannot use MCP; only API keys can.

## Connect

Endpoint: `https://<your-watchtracker>/api/mcp` (Streamable HTTP). Send your key in an `X-API-Key` header.

**Hermes Agent** (HTTP transport with a custom header):

```json
{
  "name": "watchtracker",
  "type": "http",
  "url": "https://watches.example.com/api/mcp",
  "headers": { "X-API-Key": "<your key>" }
}
```

The same shape works for Copilot CLI, VS Code and Claude Code. Install the skill at `.github/skills/using-watchtracker-mcp/SKILL.md` so the assistant knows when to use each tool.

## Tools

Read (any key): `list_watches`, `list_wishlist`, `get_watch`, `collection_profile`, `list_wear_log`, `get_collection_review`, `get_style_memory`.

Agents (agents access): `ask_collection_advisor`, `generate_collection_review`, `find_review_candidates`, `ask_style_agent`, `recommend_watch_for_outfit`. See [AI agents](ai-agents.md) for what each does. Agent runs save to the same history you see in the app (a new review replaces the old one) and are limited to 10 a minute per person.

## Safety

- Every tool only sees the key owner's data.
- Read-only keys never see the agent tools.
- Requests are limited to 60 a minute and 128 KiB.
- Revoke a key in Settings and it stops working immediately.

## Troubleshooting

| You see | Meaning |
|---|---|
| `401` | Missing or wrong key |
| `403` | The key lacks the needed access, or you used a browser sign-in |
| `503` | An admin has not enabled the MCP server |
| `429` | Slow down |
| Behind a reverse proxy, agent calls time out | Allow responses of 3+ minutes |

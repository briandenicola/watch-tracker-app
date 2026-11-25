# MCP Server: deploy and use

WatchTracker has a built-in [MCP](https://modelcontextprotocol.io) server. It lets an AI assistant such as Hermes Agent look at your collection and use WatchTracker's AI agents for you. With a key you have given wish list access, it can also add a watch to your wish list. That is the only change it can make: it cannot edit or delete watches, or touch your wear log.

It is part of the normal WatchTracker container: **there is nothing extra to install or run.** You only turn it on and give the assistant a key.

## 1. Deploy

1. Deploy WatchTracker as usual (see [Deployment](deployment.md)). The upgrade adds one database migration (`AddApiKeyScopes`), so back up the data volume first.
2. Confirm `/health/ready` responds, then sign in as an admin.
3. The assistant must be able to reach WatchTracker over HTTPS. If it runs on another machine, publish the app the same way you do for browsers.

### Reverse proxy

- Pass `/api/mcp` through unchanged and forward the `X-API-Key` header (most proxies do by default).
- Allow responses of **3+ minutes**. Agent tools run a local model; the Candidate Finder can take up to three minutes. For nginx, set `proxy_read_timeout 240s;` on that location.
- Do not buffer or cache `/api/mcp`.
- If the proxy is not on the same host, set `TRUSTED_PROXY_NETWORKS` as described in [Deployment](deployment.md).

## 2. Turn it on (admin)

Admin → App Settings → **MCP Server** → set `McpServerEnabled` to **Enabled** → Save.

It is off by default. While off, every MCP request gets a `503`. Setting it back to **Disabled** cuts off all assistants at once, without revoking keys.

## 3. Create a key (each user)

Settings → **API Keys** → name it, pick an access level, **Create Key**. Copy it right away; it is shown once.

| Access | Can do |
|---|---|
| Read only | Look at watches, wish list, wear log, stored review, style memory |
| Read + add to wish list | All of that, plus `add_to_wishlist` over MCP. It can't edit or delete anything, and can't change data over the REST API |
| Read + AI agents | Look at everything, plus run the AI agents over MCP. It can't change data |
| Read + AI agents + add to wish list | Both of the above. This is the one for an assistant that should ask the agents and add what they find to your wish list |
| Read + write | Look at everything, plus add, change and delete over the REST API, for apps that update your collection. It can also use `add_to_wishlist` |

Don't give an assistant Read + write just so it can add to your wish list: that key can edit and delete everything over the REST API. Use an add to wish list key instead. Give each app or assistant the lowest access it needs, and change a key's access in Settings at any time without replacing it: the app keeps working with the same key. Keys made before this feature are read only. Over the REST API, adding, changing or deleting with a key that isn't Read + write gets a 403. No key can create, change or revoke API keys; that is done signed in. A key only ever sees its owner's data. Revoke a key in Settings and it stops working immediately.

## 4. Connect your assistant

Endpoint: `https://<your-watchtracker>/api/mcp` (Streamable HTTP). Send the key in an `X-API-Key` header.

### Hermes Agent

Add WatchTracker as an HTTP MCP server with a custom header:

```json
{
  "name": "watchtracker",
  "type": "http",
  "url": "https://watches.example.com/api/mcp",
  "headers": { "X-API-Key": "<your key>" }
}
```

Keep the key in your agent's secret store or an environment variable rather than in a file you share. Restart or reload the agent, then ask it to list your tools; you should see the WatchTracker ones.

### Other clients

Copilot CLI, VS Code and Claude Code accept the same shape (HTTP transport, URL, custom header). Check your client's MCP settings for where it goes.

### Teach the assistant when to use each tool

Copy `.github/skills/using-watchtracker-mcp/SKILL.md` into your assistant's skills folder. It explains which tool to pick and the order to use them in. It contains no key.

### Quick test with curl

```sh
curl -s https://watches.example.com/api/mcp \
  -H "X-API-Key: $WATCHTRACKER_KEY" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

## 5. What to ask

Read-only (any key), fast:

- "What's in my collection?" → `list_watches`
- "Show my wish list in priority order." → `list_wishlist`
- "Tell me everything about watch 12." → `get_watch`
- "Where are the gaps in my collection?" → `collection_profile`
- "What have I worn lately?" → `list_wear_log`
- "What did my last review say?" → `get_collection_review`
- "What outfits did the style agent suggest for this watch?" → `get_style_memory`

AI agents (agents access), slower and limited to 10 a minute:

- "Ask the advisor whether I need a dive watch under $3,000." → `ask_collection_advisor` (up to 90 s, may search the web)
- "Review my collection." → `generate_collection_review` (up to 2 min, replaces the stored review, needs 2+ watches)
- "Find real listings for those gaps, budget 2,500 USD." → `find_review_candidates` (up to 3 min, run a review first)
- "What should I wear my Speedmaster with to a wedding in the evening?" → `ask_style_agent`
- "Pick a watch for a navy suit at a dinner." → `recommend_watch_for_outfit` (needs 2+ watches)

Add to wish list (wish list access):

- "Add the Tudor Black Bay 58 to my wish list." → `add_to_wishlist` (goes to the end of the priority order; a watch already on the list is reported, not added twice)

Agent results are saved in the same history you see in the app. Treat listings and prices as leads to check, not facts. See [AI agents](ai-agents.md) for what each agent does.

## Limits and safety

- Only API keys work. Browser sign-ins are refused.
- Read-only keys never see the agent tools or `add_to_wishlist`.
- 60 requests a minute per person, 128 KiB per request, 10 agent runs a minute.
- `add_to_wishlist` is the only tool that changes your data, and only for keys with wish list (or write) access. No tool edits or deletes a watch or writes to the wear log.

## Troubleshooting

| You see | Meaning and fix |
|---|---|
| `401` | Missing or wrong key. Check the `X-API-Key` header. |
| `403` | The key is not an API key, or lacks access. Change its access in Settings → API Keys. |
| `503` | MCP is disabled. An admin enables it in App Settings → MCP Server. |
| `429` | Too many requests. Wait a minute. |
| Tool says the key lacks the "agents" scope | Change the key to Read + AI agents in Settings. |
| Agent tools missing from the tool list | The key is read only. |
| `add_to_wishlist` missing from the tool list | The key has no wish list access. Change it in Settings → API Keys. |
| Agent calls time out | Raise the proxy timeout (see above) and check Ollama is reachable. |
| `find_review_candidates` says a review is needed | Run `generate_collection_review` first. |

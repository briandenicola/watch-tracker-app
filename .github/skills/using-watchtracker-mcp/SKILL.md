---
name: using-watchtracker-mcp
description: Use the WatchTracker MCP server to read a collector's watches, wish list, wear log and collection review, and to run its AI agents (Collection Advisor, Collection Review, Candidate Finder, Style Agent, Outfit Recommender). Use when asked about the user's watch collection.
---

# Using the WatchTracker MCP server

WatchTracker exposes one MCP endpoint, `POST /api/mcp` (Streamable HTTP, stateless). Auth is an `X-API-Key` header. Never print, log or store the key.

If calls return 503, the admin has not enabled MCP (Admin > App Settings > MCP Server). If 403, the key is missing the needed scope. Report this to the user; do not retry.

## Read tools (any key)

| Tool | Use it for |
|---|---|
| `list_watches` | Active collection; optional `query`, `limit`, `offset` |
| `list_wishlist` | Wish list in priority order |
| `get_watch` | Every detail of one watch by `id` |
| `collection_profile` | App-calculated coverage, gaps, redundancy, wear |
| `list_wear_log` | Recent wear events |
| `get_collection_review` | Last stored review (no model run) |
| `get_style_memory` | Outfits previously suggested for a watch |

## Agent tools (key needs the `agents` scope)

| Tool | Notes |
|---|---|
| `ask_collection_advisor` | Up to 90 s; may search the web; replies cite sources |
| `generate_collection_review` | Up to 2 min; replaces the stored review; needs 2+ watches |
| `find_review_candidates` | Up to 3 min; needs a stored review first; optional `budget` + `currency` |
| `ask_style_agent` | Needs `watchId`; give `occasion` and `weather` |
| `recommend_watch_for_outfit` | Needs 2+ watches; `occasion` and `outfitDescription` |

## Guidance

- Prefer read tools. They are fast; agent tools run a local model and are limited to 10 per minute.
- Check `get_collection_review` before calling `generate_collection_review`; only regenerate if it is missing or stale.
- Run `generate_collection_review` before `find_review_candidates`.
- Nothing here changes watches, the wish list or the wear log.
- Treat advisor listings and prices as leads to verify, not facts.

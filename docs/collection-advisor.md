# Collection Advisor

The Collection Advisor is a user-scoped, tool-using chat at `/advisor`. It can identify collection gaps and overlap, research brands or models, find current marketplace listings, compare asking prices, and score candidates against recorded collection attributes and a stated budget.

## Configuration

Configure these values under **Admin > Settings**:

| Setting | Required | Purpose |
| --- | --- | --- |
| `OllamaUrl` | Yes | Base URL of the Ollama-compatible model endpoint. |
| `OllamaModel` | Yes | Model used for structured advisor actions. |
| `CollectionAdvisorPrompt` | Yes | Editable advisor persona. Fixed tool and safety rules are appended in code. |
| `WebSearchProvider` | For web research | Selects Brave or SearXNG. |
| `MarketplaceVendor` | For marketplace results | Selects one approved vendor to search; defaults to Chrono24. |
| `BraveSearchApiKey` | For Brave | Credential sent only to Brave Search. It is never included in prompts or diagnostics. |
| `SearXngUrl` | For SearXNG | Base URL of a SearXNG instance. |
| `WebSearchProvider` plus its settings | For vendor marketplace results | Searches the approved vendor catalog through Brave or SearXNG snippets. |
| eBay client settings | Optional | Adds eBay Browse fixed-price listing searches when credentials are available. |

The advisor remains available for collection-only questions when an optional search provider is unavailable. The UI reports provider failures and no-result states instead of presenting them as current evidence.

### WatchPatrol (optional, free interactive MCP)

On the Advisor page, choose **Connect WatchPatrol**, sign in to WatchPatrol and approve access.
Sign-in uses a full-page redirect back to `/advisor`; the existing advisor conversation is saved.
Each WatchTracker user connects their own WatchPatrol account. No API subscription or pasted key is needed.
For an installed PWA whose external sign-in opens a separate browser, connect from the
Advisor page in that browser first. The server-side connection then applies to the same user in the PWA.
Disconnect removes this app's saved authorization and any pending connection; to revoke the
provider-side grant too, use WatchPatrol's connected-app settings.

The supplied MCP address is `https://api.watchpatrol.net/mcp/`. WatchPatrol's OAuth discovery
currently advertises `https://www.watchpatrol.net/mcp/` as its canonical resource, which the
app uses directly to avoid redirecting bearer credentials. The app uses Streamable HTTP and
the already-referenced MCP SDK, OAuth dynamic public-client registration, authorization code
with S256 PKCE, and the `mcp` scope only. Registration, authorization and token exchange are
pinned to WatchPatrol's HTTPS origin. OAuth callbacks are authenticated API POSTs from the
Advisor page, not anonymous account-link endpoints. The callback requires both matching
browser session state and an unexpired, single-use authorization owned by the signed-in user.
Access/refresh tokens and pending PKCE details are encrypted with the app's persisted Data
Protection keys, using a separate protection purpose for each user. Preserve the existing
`DataProtection:KeyPath` volume when replacing the container.

The Advisor first calls `watchpatrol_tools`, then `watchpatrol_call` with a discovered tool name
and its advertised argument schema. It accepts only explicitly read-only, non-destructive
research tools with supported research names; image identification, alerts, starring,
bulk/export and account actions are excluded. Missing read-only annotations fail closed,
with a visible tool-status explanation. Discovery is metadata, never research evidence.

Research results appear as escaped, expandable **Observed WatchPatrol result** text in the
tool activity panel, with the observation time recorded in source citations. The model can
summarize with citations, but cannot invent numeric price prose or turn arbitrary MCP output
into listing cards. WatchPatrol asking prices do not update saved resale values or feed the
scheduled price scanner, candidate finder or resale refresh service.

The free plan currently advertises 60 calls/minute and 250 calls/day; WatchPatrol controls
and may change those limits. The app does not automatically retry failed/throttled MCP
requests. All lookups are initiated by an Advisor chat request, within its existing five-call,
90-second budget. No background synchronization, bulk data collection or paid REST access
is implemented. Query parameters supplied to a research tool are sent to WatchPatrol; the
app does not send OAuth tokens to Ollama or the browser.

The redirect must be `/advisor` on the current API origin or an explicit `AllowedOrigins`
entry. HTTPS is required except on loopback development origins. `AllowedOrigins=*` does
not authorize arbitrary OAuth redirect origins. The status endpoint is not service-worker
cached, so another session or an offline cache cannot supply a stale connection state.

Provider documentation: [integrations](https://www.watchpatrol.net/integrations/),
[MCP/OAuth details](https://www.watchpatrol.net/llms-full.txt),
[terms](https://www.watchpatrol.net/terms/).

## Grounding and safety

- The agent can make at most five tool calls, use at most 48,000 prompt characters, and run for at most 90 seconds.
- Collection, wishlist, session, and feedback queries always include the authenticated user ID.
- All factual output is returned as structured claims. Collection claims name their completed local evidence tools; every external claim cites at least one exact URL observed by an approved tool.
- A failed or no-result external tool cannot authorize model prose. The server replaces it with a controlled provider-failure or no-evidence response.
- Price values are reconstructed by the server from an observed marketplace listing and include a three-letter currency plus the UTC observation time.
- Strict maximum-budget searches exclude listings whose delivered total is unknown because shipping was unavailable.
- Marketplace cards, links, metadata, and wishlist actions are reconstructed from stored server results. Client-supplied listing metadata is not trusted.
- User text, provider titles/snippets, tool output, and feedback notes are labeled as untrusted data. They are never treated as instructions.
- Assistant Markdown is sanitized in the browser. Only an allowlist of structural tags and HTTP/HTTPS links is retained.

## Supported evidence

| Provider/tool | Evidence type | Important limitation |
| --- | --- | --- |
| Collection profile | Recorded collection and wishlist data | Results are only as complete as the user's watch metadata. |
| Selected approved vendor | Current listing snippets from the configured vendor; Chrono24 is the default | One search-engine request is made per marketplace query. Snippets can omit shipping, condition, or availability details. |
| eBay Browse | Active fixed-price listings | Optional; asking prices are not completed sales and listings may change or expire. |
| Brave Search | Current web snippets and source URLs | Snippets can be incomplete and must be verified at the cited source. |
| SearXNG | Current web snippets and source URLs | Coverage and freshness depend on the configured instance. |
| Resale comparables | Min/median/max of matching active asking prices | This is not an appraisal or guaranteed resale value. |
| WatchPatrol MCP | User-requested pricing, listings, sellers and market research | Optional per-user OAuth; asking prices, not completed sales. Observed payloads are displayed separately, not used to create listing cards or saved valuations. |

Prices are observations, not offers or guarantees. Shipping can be unavailable, listings can expire, condition can differ, and currency conversion is not performed. “Value” means current asking-price evidence plus collection fit; investment returns are never guaranteed.

## Recommendation actions and feedback

Adding a recommendation to the wishlist preserves its provider item ID, source URL, known brand/model/reference, observed numeric price, currency, and observation time. Duplicate provider items, source URLs, and matching canonical references return an explicit “already on your wishlist” result.

Feedback can be changed or removed. At most ten recent feedback records are supplied to future conversations as untrusted preference context; full prior conversations are not replayed as memory.

## Diagnostics and privacy

Structured logs record, at `Information` and `Warning`:

- request outcome, elapsed milliseconds, and tool-call count;
- tool name, status, and elapsed milliseconds;
- safe failure category (`grounding_validation`, `tool_execution`, `model_provider`, `safety_limit`, or `invalid_model_output`);
- rate-limit rejection by endpoint definition;
- provider HTTP status codes, provider error type, and reply lengths;
- rejected requests, requests that hit the execution limit, and requests the caller abandoned.

At these levels logs do not contain complete prompts, collection contents, provider response bodies, credentials, configured provider URLs, user IDs, listing/search text, or feedback notes.

Setting **Admin → Settings → Log level** to `Debug` or `Trace` lifts that redaction. It is an explicit operator choice to trade privacy for diagnosis, and at those levels nothing is held back: the prompt (one line per advisor message), the model's reply, the provider's response body, the action type, tool name or clarification constraint the model asked for, the configured provider URL and model name, the user ID a request belongs to, per-round query/listing/selection counts, marketplace query terms, and the underlying exception. Every logged payload is truncated to 4000 characters and flattened to a single line, so untrusted text cannot forge a log entry. Credentials are never logged at any level.

`Debug` and `Trace` also raise the `Microsoft.AspNetCore` category, which is what records a rate-limited or abandoned request; every other level pins it back to `Warning`.

Because those levels put collection contents and model output in the log, treat the log as sensitive while it is on, and return the level to `Information` when you are done.

## Release evaluation thresholds

The repeatable API and frontend suites implement the following release gate:

| Evaluation | Passing threshold | Automated evidence |
| --- | --- | --- |
| Collection gaps and overlap | Deterministic results identify missing coverage and repeated clusters for seeded collections. | `CollectionProfileServiceTests` |
| Budget filtering | 100% of returned strict-budget listings match currency, are fixed price, have a known delivered total, and do not exceed the maximum. | `AdvisorToolServiceTests` |
| Brand/model research | 100% of external claims have one or more exact observed citations. | `AdvisorReplyGeneratorTests` |
| Live listing provenance | 100% of recommendation prices include currency and UTC observation time; cards use server-observed fields. | `AdvisorReplyGeneratorTests`, `EbayBrowseClientTests` |
| Sparse/no-result/stale data | Sparse profiles remain usable with low-confidence reasons; no-result/provider failure is explicit; observation timestamps are retained for freshness warnings. | `CollectionProfileServiceTests`, `AdvisorToolServiceTests`, `AdvisorPage.test.ts` |
| Prompt injection | User direct-answer bypasses, unobserved snippet URLs, tool-result instructions, and model-supplied listing identities are rejected or remain inert data. | `AdvisorReplyGeneratorTests` |
| Tenant isolation | 100% of tested collection, wishlist, session, action, and feedback access is denied across users. | `AdvisorToolServiceTests`, `CollectionAdvisorServiceTests` |
| Operational bounds | Tool-call and execution limits reject overrun paths; model/provider failures produce safe categories with no raw payload logged at `Information` and above, and full prompt/reply/body detail at `Debug`. | `AdvisorReplyGeneratorTests` |
| Recommendation actions | Add, duplicate, feedback update, feedback removal, and ownership paths pass in API and Vue tests. | `CollectionAdvisorServiceTests`, `AdvisorPage.test.ts` |

All listed tests must pass. API build, frontend type-check/build, frontend lint, and both test suites must also succeed.

## Troubleshooting

| Symptom | Action |
| --- | --- |
| Advisor says it is not configured | Set both `OllamaUrl` and `OllamaModel`, then use the Admin connection test. |
| Model request fails | Confirm Ollama is reachable from the API container and the configured model supports structured JSON output. Review `model_provider` diagnostics. |
| No brand research | Configure the selected Brave or SearXNG provider. The tool status identifies not-configured and provider-error states. |
| No marketplace listings | Confirm Brave or SearXNG is configured, then check query specificity and USD listing availability. eBay is an optional additional source. |
| A failure with nothing in the log | Raise **Admin → Settings → Log level** to `Debug`. Rejected requests, execution-limit overruns, abandoned requests, provider connection failures, and per-round counts are all recorded; `Debug` adds the prompt, the model reply, the provider body, the provider URL, model, user ID and exception behind them. |
| Advisor asks a generic clarifying question | The model asked to clarify a constraint outside the allowlist. The reply is server-authored by design; the `Debug` log names the constraint it asked for. |
| A listing is stale or unavailable | Ask the advisor to search again. Saved observations are not refreshed automatically. |
| A response is rejected | Review the safe failure category. `grounding_validation` means the model emitted an unobserved citation, unsupported listing/price, or unstructured external claim. |
| HTTP 429 | Wait for the one-minute advisor rate-limit window before retrying. |
| WatchPatrol authorization fails | Disconnect and connect again. Confirm the browser uses HTTPS (or loopback HTTP), `/advisor` is an allowed redirect, and the app retained its Data Protection keys. |
| WatchPatrol has no supported tools | The provider did not advertise supported research tools with explicit read-only metadata. The app deliberately does not expose unclassified or write tools. |
| WatchPatrol usage limit is reached | Wait for the provider's limit to reset; the free daily allowance is separate from the app's own advisor rate limit. |
| WatchPatrol result is too large | Ask for a narrower search or a smaller result limit. No truncated result is treated as complete evidence. |

Schema migrations run automatically at API startup. Rolling back application code leaves additive advisor tables and nullable marketplace provenance columns in place; older code safely ignores them.

For this integration, run `task test-watchpatrol` for the targeted OAuth, MCP transport/policy
and Advisor regression cases, then `task build` and `task lint-web`. These tests use local fake
provider responses, never live OAuth or paid endpoints. A live acceptance check still requires
signing in through **Connect WatchPatrol** and asking the Advisor for a specific lookup.
The additive `WatchPatrolConnections` migration applies on the next updated API startup;
older application code ignores the table on rollback.

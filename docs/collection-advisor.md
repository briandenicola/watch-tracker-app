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
| `BraveSearchApiKey` | For Brave | Credential sent only to Brave Search. It is never included in prompts or diagnostics. |
| `SearXngUrl` | For SearXNG | Base URL of a SearXNG instance. |
| eBay client settings | For marketplace results | Enables eBay Browse fixed-price listing searches. |

The advisor remains available for collection-only questions when an optional search provider is unavailable. The UI reports provider failures and no-result states instead of presenting them as current evidence.

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
| eBay Browse | Active fixed-price listings | Asking prices are not completed sales; listings may change or expire. |
| Brave Search | Current web snippets and source URLs | Snippets can be incomplete and must be verified at the cited source. |
| SearXNG | Current web snippets and source URLs | Coverage and freshness depend on the configured instance. |
| Resale comparables | Min/median/max of matching active asking prices | This is not an appraisal or guaranteed resale value. |

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
| No marketplace listings | Confirm eBay settings, query specificity, currency, fixed-price availability, and delivered-total availability for strict budgets. |
| A failure with nothing in the log | Raise **Admin → Settings → Log level** to `Debug`. Rejected requests, execution-limit overruns, abandoned requests, provider connection failures, and per-round counts are all recorded; `Debug` adds the prompt, the model reply, the provider body, the provider URL, model, user ID and exception behind them. |
| Advisor asks a generic clarifying question | The model asked to clarify a constraint outside the allowlist. The reply is server-authored by design; the `Debug` log names the constraint it asked for. |
| A listing is stale or unavailable | Ask the advisor to search again. Saved observations are not refreshed automatically. |
| A response is rejected | Review the safe failure category. `grounding_validation` means the model emitted an unobserved citation, unsupported listing/price, or unstructured external claim. |
| HTTP 429 | Wait for the one-minute advisor rate-limit window before retrying. |

Schema migrations run automatically at API startup. Rolling back application code leaves additive advisor tables and nullable marketplace provenance columns in place; older code safely ignores them.

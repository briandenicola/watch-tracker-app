# WatchTracker

> **Note:** This application is 100% vibe coded. It's exclusively for me to learn and experiment with GitHub Copilot CLI.

WatchTracker is a full-stack web application for cataloging and managing your personal watch collection. Track details like brand, model, movement type, band type, purchase info, and images — with optional AI-powered watch analysis via Ollama. Every watch is scoped to your authenticated account using JWT-based authentication.

It also includes a **wish list** for tracking watches you want to buy, a **stats dashboard** with a GitHub-style wear heatmap, **wear logging** to track each time you wear a watch, and a **style agent** that chats through an outfit to wear with any watch in the collection.

On first launch, a setup wizard walks you through creating an admin account and configuring application settings.

Administrators configure one application-wide IANA timezone under **Admin → App
Settings → Regional Settings**. Timestamps are stored as UTC and displayed,
grouped, and edited using that timezone so calendar dates remain consistent
across browsers and devices.

## Architecture

| Layer    | Tech                             | Path       |
| -------- | -------------------------------- | ---------- |
| Backend  | .NET 10 Web API, EF Core, SQLite | `src/api/` |
| Frontend | Vue 3, TypeScript, Vite (PWA) | `src/web/` |

The Vue SPA communicates with the .NET API exclusively via REST (`/api/*`). In production the API serves the SPA as static files from a single container, so no separate web server is needed.

The frontend is a Progressive Web App (PWA) and can be installed on iOS (Safari → Share → Add to Home Screen), Android, and desktop browsers for a native app-like experience with offline caching. When running as a standalone PWA on iOS, the navigation bar collapses into a hamburger menu to avoid the notch and status bar area.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+)
- [Task](https://taskfile.dev/) — optional, for task runner commands
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) — optional, for containerized deployment

## Getting Started

Clone the repository and start the development servers:

```sh
git clone <repo-url> && cd watch-tracker-app
task run        # starts both API and frontend in parallel
```

The API runs on `http://localhost:5062` and the Vite dev server on `http://localhost:5173`. You can also start them individually:

```sh
task run-api    # API only
task run-web    # frontend only
```

When the app launches for the first time, the setup wizard at `/setup` will guide you through creating an admin account and optionally configuring Ollama for AI watch analysis.

## Task Commands

| Command            | Description                              |
| ------------------ | ---------------------------------------- |
| `task init`        | Generate a `.env` file with a random JWT key |
| `task run`         | Run API and frontend in parallel         |
| `task run-api`     | Run the .NET API server                  |
| `task run-web`     | Run the Vite dev server                  |
| `task build`       | Build both API and frontend              |
| `task db-update`   | Apply existing EF Core migrations locally |
| `task db-add-migration NAME=AddFeature` | Create a named EF Core migration |
| `task build-api`   | Build the .NET API project               |
| `task build-web`   | Build the Vue frontend                   |
| `task test-api`    | Run the API test suite                    |
| `task test-web`    | Run the frontend test suite               |
| `task lint-web`    | Run frontend lint                         |
| `task docker-build`| Build the Docker container image         |
| `task docker-run`  | Run the Docker container locally         |
| `task db`          | Create and apply EF Core migrations      |

## CI/CD

A GitHub Actions workflow (`.github/workflows/docker-publish.yml`) builds and pushes the Docker image to Docker Hub:

- **Triggers** — Push to `main` branch and manual dispatch
- **Tags** — Full commit SHA, short commit SHA, and `latest` (on main)
- **Caching** — Uses GitHub Actions cache for Docker layer caching
- **Provenance** — Main-branch images receive a Sigstore-signed SLSA build
  provenance attestation, stored with the Docker Hub image by digest

### Required Secrets

| Secret | Description |
| ------ | ----------- |
| `DOCKERHUB_USERNAME` | Your Docker Hub username |
| `DOCKERHUB_TOKEN` | A Docker Hub access token |

The publishing job requires no GitHub Packages permission. Its OIDC and
attestation permissions are used only to sign and publish provenance after the
image has been pushed. Verify a published digest with GitHub CLI:

```sh
gh attestation verify oci://index.docker.io/<dockerhub-user>/watch-tracker-app@sha256:<digest> \
  --owner briandenicola
```

Dependabot checks GitHub Actions, npm, NuGet, and Docker base-image dependencies
weekly. CI blocks known high- and critical-severity npm and NuGet
vulnerabilities before publishing.

## Deployment

The application ships as a single Docker container that serves both the API and the Vue SPA. The publish workflow pushes the pre-built image to Docker Hub.

### Configuration

Generate a `.env` file with a random JWT signing key:

```sh
task init
```

This creates a `.env` file with a cryptographically random `JWT_KEY`:

```
JWT_KEY=<auto-generated>
ALLOWED_ORIGINS=*           # optional — restrict CORS origins (semicolon-separated, or * for any)
LOG_LEVEL=Information       # optional — initial log level (Trace, Debug, Information, Warning, Error, Critical, None)
```

### Running with Docker Compose

```sh
docker compose up
```

### Running with Task

```sh
task docker-build           # build the image from source
task docker-run             # run the container (reads .env automatically)
```

### Running with Docker directly

```sh
docker run -p 8080:8080 -d \
  -e Jwt__Key=YourSecretKeyHere \
  -e Jwt__Issuer=WatchTracker \
  -e Jwt__Audience=WatchTracker \
  -e AllowedOrigins=* \
  -v watch-tracker-data:/app/data \
  -v watch-tracker-uploads:/app/uploads \
  <dockerhub-user>/watch-tracker-app:latest
```

The application will be available on `http://localhost:8080`. On first launch, the setup wizard will guide you through creating an admin account.

### Deploying Behind a Reverse Proxy

The production image is a single container: it serves the Vue SPA and the API,
so no separate web container is needed. When you deploy behind a reverse proxy,
set `TRUSTED_PROXY_NETWORKS` to the proxy's IP/CIDR range so the API will accept
its `X-Forwarded-For` and `X-Forwarded-Proto` headers. Leave it empty when
clients connect to the app directly.

Set `AllowedOrigins` to your public domain(s) or `*` if the proxy is trusted:

```sh
# Single origin
-e AllowedOrigins=https://watches.example.com

# Multiple origins (semicolon-separated)
-e AllowedOrigins="https://watches.example.com;https://api.example.com"
```

See [`docs/deployment.md`](docs/deployment.md) for deployment, health-check,
backup, and migration recovery steps.

## Features

### Collection Views

The main page holds the **Collection** and **Wish List** behind a tab toggle. Either tab renders in whichever of two
view modes the toolbar has selected:

- **Card view** — Large cards showing the cover image with brand and model. On desktop this is a grid that fills the
  width; on mobile it is a full-width swipe gallery — one watch at a time, with arrows and a position counter.
- **Compact grid** — Square thumbnails, three across on mobile and up to six on desktop, captioned with brand and
  model. It scrolls vertically, so a collection of any size can be scanned at a glance. Tapping a tile opens the watch.

The pair of buttons at the right of the toolbar switches between them. The choice is remembered in the browser and
applies to both tabs.

Beside them, the number is how many watches are currently shown; clicking it opens the filter panel, which filters by
brand and movement type and sorts by date added, brand, last worn, most worn and — on the wish list — priority.
Filters and sorting apply to whichever view is active.

On the wish list sorted by priority, watches can be dragged into order in the desktop card grid, once the brand and
movement filters are cleared. Mobile and the compact grid link to the **Arrange Priority** page instead.

The Brand field on the Add/Edit Watch forms includes an autocomplete dropdown populated from existing brands in your
collection.

### Wish List

Track watches you'd like to buy without cluttering your main collection:

- **Add to Wish List** — Paste a product-page URL to extract the brand, model/reference, USD target price, store link, and main image with the configured Ollama model. The extracted values fill the normal form for review and editing; nothing is saved until you confirm. A × inside the URL field empties it when you want to try a different page. Manual entry remains available when a store blocks extraction or omits details.
- **Wish List Gallery** — Toggle the "Wish List" button in the toolbar to view your wish list. Cards show the watch image (click to edit) and the brand/model as a link to the product page.
- **Edit Wish List Item** — Update details, replace the image, delete the item, or mark it as **Purchased** which redirects to the Add Watch page with the brand and model pre-filled.
- **Price Watch** — Check a wish-list item now, or opt it into scheduled checks with an optional USD target. The app records only attributable USD search-listing or eBay API sightings, labels their condition and match confidence, and raises in-app alerts only for high-confidence matches that beat the target or an earlier price. It is a best-effort signal, not a complete market sweep; unavailable, blocked, and provider-error sources remain visible as such.

Wish list items are stored in the same database table as watches but are hidden from the main collection by default.

### Wear Tracking & Stats

Each time you click **Wore Today** on a watch detail page, an individual wear event is logged (in addition to the aggregate counters). The **Stats** page (accessible from the navigation bar) shows:

- **Summary cards** — Total wears and unique watches worn
- **Wear Heatmap** — A GitHub-style 365-day heatmap showing wear frequency per day, with 4 intensity levels and dark/light theme support
- **Wear Timeline** — Chronological list of the 30 most recent wear events, each linking to the watch detail page

The **Wear Log** page, also in the navigation bar, is where individual wear events are reviewed and corrected:

- **Timeline** — Every wear event, newest first, grouped by day.
- **Calendar** — A month grid with a dot on each day that has a wear. Selecting a day lists what was worn on it.
- **Add watch worn** — From the selected day, pick a watch to log a wear on that date, including past days. Only
  active collection watches are offered — wish list and retired ones are not — and a filter box narrows a larger
  collection. The day's list, its calendar dot and the timeline update immediately.
- **Edit / Remove** — Any entry can be moved to another date, given or cleared of start and end times, or deleted.
  Back-dating an entry never pulls a watch's last-worn date behind a more recent wear.

### Watch Details

Each watch can store:

- **Core fields** — Brand, model, movement type, case size, band type, purchase date/price
- **Extended properties** — Crystal type, case shape, crown type, calendar type, country of origin, water resistance, lug width, lug-to-lug, dial color, bezel type, power reserve, serial/reference number
- **Link** — A URL with customizable display text (defaults to "Product Page"), shown as a chip on the detail page
- **Notes** — Markdown-supported notes field, displayed in a scrollable container within the Additional Details accordion
- **Images** — Multiple image uploads per watch with the ability to choose a cover image for the gallery view

### AI Watch Analysis

Upload photos of a watch and run **AI Analyze** from the overflow menu. Ollama looks at the cover photo and comes back
with two things:

- **A short description** — under 70 words, saved to the watch's AI analysis, replacing the previous one. It is not
  copied into notes: notes are yours to write.
- **Suggested values for fields the record is missing** — dial colour, case shape, bezel, crystal, crown, calendar,
  band type and colour, water resistance, origin, battery type, reference, case size, lug width, lug-to-lug,
  power reserve and production year. Each comes with a confidence and a one-line reason.

It reads the watch's links too. If the watch has a **Product / Reference** link or an **Acquisition Source** link, both
pages are fetched and their text goes to the model alongside the photo, so specs come off a spec sheet instead of a
guess. The model is told to believe a page over its own recollection for anything written down, and to believe the
photo over a page for colour and finish — a listing often covers several variants of one model. The review dialog names
which pages were read. A link that will not load is simply left out.

Fetching a user-supplied URL is fenced in: http(s) only, redirects followed by hand and re-checked at each hop,
responses capped in size, and connections opened only to public IP addresses — checked at connect time, so a hostname
that resolves to an internal address cannot be used to make the server read your private network. Page text is given to
the model as reference material with an explicit instruction to ignore any directions inside it, and it can still only
propose values for allow-listed fields that you approve.

**Nothing is written from a suggestion until you approve it.** The review dialog lists each one with a checkbox and an
editable value — low-confidence guesses start unticked — and only the ones you tick are saved. Fields that already have
a value are never suggested, and the server re-validates every approved value against the same rules as the edit form,
so a bad guess is refused rather than stored. The list of fillable fields is an allow-list in code: serial number,
prices, provenance, storage and notes are not on it.

### Style Agent

Every watch detail page has a **Style Agent** — a chat that recommends an outfit to wear with that watch, backed by the
same Ollama model as the AI analysis.

- **It looks at the watch** — the watch's cover photo is sent with every turn (downscaled to 768px, re-encoded as JPEG),
  and the agent is told to trust what it sees over the recorded fields, which are often blank. An orange dial gets
  styled as an orange dial even when nothing in the database says so. Watches with no photo still work; the agent falls
  back to the fields. If the configured model can't accept images, the request is automatically retried without the
  photo.
- **It asks before it advises** — the agent will not commit to an outfit until it knows the **occasion** and the
  **weather**. Both have a text field and a row of one-tap presets above the transcript, and the agent asks for whatever
  is still missing.
- **It remembers** — every outfit it recommends is stored against the watch. Later conversations are primed with those
  recommendations, so it stops repeating itself and builds on what came before. Rated outfits from the rest of the
  collection are included too, as a read on the owner's taste.
- **It asks how things went** — each recommendation can be marked **Worked** or **Missed**, with an optional note. The
  agent is told which are still unrated and asks about them, and future advice leans towards what worked and away from
  what missed. Individual memories can be forgotten.
- **New chat** clears the transcript without clearing the memory; **Forget** on a remembered outfit drops it for good.

The agent's persona is editable under **Admin → Settings → Style Agent** (`StyleAgentPrompt`). The rules it must follow —
ask first, don't repeat a miss, answer in JSON — are fixed in code, so editing the persona cannot break the chat.

Endpoints live under `/api/watches/{watchId}/style`.

### Collection Advisor

The **Collection Advisor** at `/advisor` answers collection-gap, overlap, brand, budget, and current-listing questions using a bounded tool loop. Collection and wishlist access is always scoped to the signed-in user. External claims use claim-level citations, while listing cards and observed prices are reconstructed from server tool results rather than model-supplied fields.

Recommendations can be added to the wishlist and marked helpful, irrelevant, already owned, or not interested. Only the ten most recent structured feedback records are included in future advisor context.

Configuration, provider behavior, operational limits, privacy-safe diagnostics, evaluation thresholds, and troubleshooting are documented in [`docs/collection-advisor.md`](docs/collection-advisor.md).

### Share a Wish List

**Settings → Sharing → Share Wish List** creates one public link — `/w/<token>` — to your whole list, in your priority
order. It always reflects the list as it stands, so an item added or bought later needs no new link.

- The list is live: watches you add to the wish list appear on the shared page automatically, and ones you buy drop
  off it, with no need to reissue the link.
- Same shape as a watch share: an unguessable token, one link per person, revocable at any time, `noindex`, rate
  limited, and honouring `ShareLinkBaseUrl`.
- Visitors see your display name and, per item, its photos, brand and model, reference, case, dial, strap, movement,
  water resistance and any product link. They never see your collection, what you paid for anything, notes, storage,
  wear history or account details.
- **Target prices are off by default** and can be switched on from the same dialog — handy when you are hinting, but
  nobody should publish their budget by not noticing a checkbox. Changing it does not reissue the link.

`?format=json` works here too: `/w/<token>?format=json` returns the same payload for a script.

### JSON Output

Append `format=json` to a watch detail URL to get the record itself instead of the page.

- `/s/<token>?format=json` is answered by the API, so a share link works from `curl` or a script, not just a browser.
  The payload is the same redacted view the shared page shows.
- `/watches/<id>?format=json` renders your own watch as JSON in the app, for a quick look at the raw record.

### Share a Watch

**Share** in a watch's overflow menu creates a public link — `/s/<token>` — that shows that one watch to anyone,
including people with no account.

- **The token is the credential.** 32 random bytes, unguessable, and the only way in: the public endpoint takes a token
  and nothing else, so there is no watch id to walk. One link per watch, revocable at any time from the same dialog,
  after which the link 404s for everyone holding it.
- **The shared view is an allow-list.** Visitors see photos, brand and model, reference, case, dial, strap, movement,
  water resistance and any product link. They never see what you paid, where you bought it, the serial number, notes,
  AI analysis, resale values, storage location, wear history, disposition, or anything identifying the owner. The
  public payload is mapped field by field in `SharedWatchDto`, so adding a column to `Watch` cannot quietly publish it.
- **The dialog spells out both lists** before you create a link, and afterwards shows the view count and when it was
  last opened.

The public read is rate limited per IP, and is the only unauthenticated endpoint in the app.

**If the app answers on more than one address**, set **Admin → App Settings → Sharing → `ShareLinkBaseUrl`** to the one
your friends can reach, e.g. `https://chronos.example.com`. Share links are otherwise built from whichever address you
happen to be using, which is no use to anyone outside your network when you administer the app on an internal hostname.
Anything that is not an absolute `http(s)` address is ignored, and links fall back to the current origin.

### Cover Image Selection

When editing a watch with multiple images, a **Gallery Image** picker lets you choose which image appears as the cover in the gallery view.

### User Preferences

All authenticated users can open **Settings** from the navigation bar:

- **Profile** — Change your display username and profile photo. Your email address is shown but not editable.
- **Appearance** — Switch between light and dark mode. Defaults to the OS preference and persists in the browser.
- **Collection** — Default sort for the collection and for the wish list, plus the storage locations offered on the
  watch form.
- **Sharing** — Create or revoke the public link to your wish list.
- **Change Password** — Update your account password.
- **Linked Sign-in Accounts** — Connect or disconnect an OIDC provider.
- **API Keys** — Issue and revoke keys for programmatic access. A key is shown once, at creation.
- **Data** — Export your collection, or import one.

Two related settings live elsewhere: the collection view mode is remembered from the toolbar toggle rather than set
here, and the time zone is application-wide and set by an admin.

### Admin Settings

Admins can manage application-wide settings under **Admin → Settings**, organized into grouped sections:

- **Regional Settings** — The application-wide IANA time zone. Timestamps are stored as UTC and displayed, grouped
  and edited against it.
- **Sharing** — The public base address share links are built from.
- **Ollama Configuration** — The Ollama URL and model.
- **Web Search Configuration** — The search provider and its credentials (Brave or SearXNG).
- **eBay Pricing** — eBay client credentials, used for resale lookups.
- **Resale Configuration** — How often resale values are refreshed.
- **Price Monitoring** — The scheduled price-watch interval in hours (1–168). Only opted-in active wish-list items are scanned.
- **Prompts** — The personas and instructions behind AI analysis, resale valuation, the style agent, the watch
  recommendation and the collection advisor. Tool, grounding, privacy and safety rules remain fixed in code.
- **Security** — Max failed login attempts and lockout duration.
- **Logging** — Runtime log level (Trace through None). Changes take effect immediately without a restart.

### Environment Variables

| Variable | Default | Description |
| -------- | ------- | ----------- |
| `JWT_KEY` | *(required)* | JWT signing key (`openssl rand -base64 48`) |
| `ALLOWED_ORIGINS` | `*` | CORS allowed origins (`*` or semicolon-separated URLs) |
| `LOG_LEVEL` | `Information` | Initial log level (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` | Enable forwarded headers for reverse proxy support |
| `TRUSTED_PROXY_NETWORKS` | *(empty)* | Semicolon-separated IP/CIDR ranges permitted to supply `X-Forwarded-For` and `X-Forwarded-Proto`; leave empty when the app is directly reachable |

## Project Structure

```
watch-tracker-app/
├── .github/
│   └── workflows/
│       ├── codeql-analysis.yml   # CodeQL security scanning
│       ├── docker-publish.yml    # Validate, build, and push to Docker Hub
│       └── quality.yml           # API and frontend quality gates
├── src/
│   ├── api/                      # .NET 10 Web API
│   │   ├── Controllers/          # API endpoints
│   │   ├── DTOs/                 # Data transfer objects
│   │   ├── Models/               # EF Core entities
│   │   ├── Services/             # Business logic
│   │   ├── Data/                 # DbContext & configuration
│   │   ├── Migrations/           # EF Core migrations
│   │   └── Program.cs            # App entry point
│   └── web/                      # Vue SPA
│       ├── src/
│       │   ├── components/       # Reusable Vue components
│       │   ├── services/         # API client and resource functions
│       │   ├── stores/           # Pinia application state
│       │   ├── pages/            # Page components
│       │   ├── types/            # TypeScript type definitions
│       │   └── utils/            # Utility functions (gravatar, etc.)
│       ├── public/               # PWA icons & static assets
│       ├── index.html
│       └── vite.config.ts
├── Dockerfile                    # Multi-stage build (web + API)
├── Taskfile.yml                  # Task runner configuration
├── docker-compose.yaml           # Container orchestration
└── README.md
```

## Backlog
Future feature ideas for the app:

- [ ] **Maintenance Tracker** — Log service history, battery replacements, and strap changes. Set reminders for the next scheduled service.
- [ ] **Watch Comparison** — Side-by-side spec comparison of any two watches in your collection.
- [X] **Collection Statistics** — Dashboard with total collection value, most-worn watch, brand breakdown charts, average price, and wearing streaks.
- [X] **Collection Timeline** — Visual timeline of when each watch was acquired, showing how the collection grew over time.
- [X] **Export / Import** — Export your collection as CSV or JSON. Import watches from a spreadsheet.


## License

This project is licensed under the [MIT License](LICENSE).

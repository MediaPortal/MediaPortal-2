# Building a New Media Center — Feasibility & Blueprint

**Question:** can we write a completely new media center that references MediaPortal 2 *functionally*, with a modern, multiplatform-ready frontend and backend?
**Answer:** yes — but the framing "completely new" is where projects like this die, so this document separates what should genuinely be new from what should be carried over, prices both, and gives a phase plan that has a usable product at every step.
**Companions:** [Multiplatform-Assessment-and-Plan.md](./Multiplatform-Assessment-and-Plan.md), [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md)
**Date:** 2026-10-17

---

## 1. Direct answer

**Yes, it's feasible, and for the frontend greenfield is arguably the *right* call** — the existing UI stack is the part with the least reusable substance (24% Direct3D-coupled, and the Windows-independent 76% is still a 2009-vintage retained-mode 2D engine designed for a Win32 message loop).

**For the backend, "completely new" would be a mistake** — and not for the usual hand-wavy "rewrites take longer than you think" reason. The specific reason here: MP2's backend contains *designed* domain assets whose value is invisible until you try to recreate them:

| Asset | Location | Why it matters to a rewrite |
|---|---|---|
| **Media Item Aspect model** — 26 orthogonal metadata facets (Movie, Episode, Series, Season, Video, Audio, AudioAlbum, Person, Character, Company, Genre, Subtitle, VideoStream, VideoAudioStream, ExternalIdentifier, ProviderResource, Relationship, Stub, Image, ThumbnailLarge, Directory, Importer, Reimport, SelectionMarker, MovieCollection, MultipleMediaItem) | `Core\MediaPortal.Common\MediaManagement\` | This is the schema that makes one library hold movies, series, music, photos and online items with cross-links. It is well-designed and would be re-invented worse from scratch |
| **DB-independent query algebra** — ~40 filter/sort types (`BooleanCombinationFilter`, `RelationalFilter`, `RelationshipFilter`, `RelationalUserDataFilter`, `CertificationAgeFilter`, `InFilter`, `LikeFilter`, `BetweenFilter`, `DataSortInformation`, …) | `MediaManagement\MLQueries\` | Media browsing, per-user watch-state, and youth protection all compose through this. Reinventing it means rediscovering its 6 years of SQL-generation edge cases |
| **Declarative extension-point registry** — 110 plugins registering objects at virtual paths (`/Configuration/Settings/…`, `/Media/…`, `/Workflow/…`, `/Players/Builders`) with versioned `CoreDependency`/`PluginReference` contracts and an XSD | `Resources\Schemas\Plugin.xsd` + 110 `plugin.xml` | The entire settings UI (64 sections / 121 settings) is *generated from registrations*, not hardcoded. This is the good idea in MP2. Keep the concept |
| **Resource-access abstraction** — URIs + accessors bridging file/URL/ZIP/ISO/SMB/audio-CD, so extractors and players never touch `System.IO` directly | `Common\Services\ResourceAccess\` | The seam that makes the ingest pipeline testable and platform-neutral |
| **Metadata extractor set** — 29 projects (MediaInfo, TagLib, FFmpeg, Matroska, NFO, BluRay, TVE3, subtitle & stream probing) | `Source\Extensions\MetadataExtractors\` | ~90% portable as-is |
| **Import/recognition pipeline** | `Common\Services\MediaManagement\ImporterWorkerNewGen.cs` | Content-recognition logic, not file-name guessing |

Measured, **~150k of MP2's 613k LOC is portable-or-near-portable domain logic**. A rewrite that starts from a blank file will spend its first year rebuilding those five things — expensively, and without the tacit knowledge that produced them.

> **Recommended framing: new *product*, strangler-fig *core*.**
> Rewrite the edges — frontend, public API, player layer, TV integration, packaging, first-run.
> Port the proven centre — domain model, query algebra, ingest, metadata, resource access, plugin/registry concepts.
> Never let the two depend on each other's internals: the new frontend talks only to the new API; what serves that API is an implementation detail that can change underneath.

---

## 2. What "referencing the current version functionally" actually means

I extracted MP2's functional surface from the code rather than from the feature list on the website. This is the backlog a new product must fill.

| Capability area | Measured surface | Notes |
|---|---|---|
| **Media library** | 26 aspects; 40-type query algebra; multi-share import; per-user userData (watched, resume, likes); relationships (movie↔person↔genre↔collection) | `UiComponents\Media` alone: 385 `.cs`, 111 XAML — the single biggest UI component |
| **Metadata & artwork** | 29 extractor projects; 7 fan-art source providers; genre conversion; TMDb/TVDB/Fanart.tv/imdb id matching (`ImdbIdMatcher`); NFO sidecars | Provider churn is a *permanent* maintenance tax (APIs change monthly) |
| **Playback** | video (DirectShow/EVR), audio (BASS), images/slideshow, DVD, Blu-ray menus, webradio, streaming; subtitles: MPC/vsfilter, DVB, web; teletext; audio-device & passthrough handling; refresh-rate switching; Ken Burns; video effects | The tacit-knowledge graveyard: codec/splitter/renderer quirks |
| **Live TV** | 96 `.cs` + 109 XAML in the TV client alone; EPG (33 files), recordings (76), timeshift (31), radio (42), channel groups (31), timers, conflict resolution, teletext, TV streaming; 3 backend providers (native TVE3, MPExtended, TvMosaic) | MP2's hardest-won domain knowledge, and its most Windows-coupled subsystem |
| **Configuration** | 64 sections / 121 settings, declared in plugin manifests; per-skin settings; home-screen editor | Data-driven — worth copying as a *pattern* |
| **UI framework** | 12 feature components (Media, SkinBase, Configuration, News, Weather, PartyMusicPlayer, SystemStateMenu, BackgroundManager, RemovableMediaManager, Statistics, Utilities, WifiConfiguration); 43 BlueVision screens; workflow/navigation stack | |
| **Users** | user profiles, PIN, certification/age restriction wired into the query layer, per-user data | |
| **Server/network** | UPnP MediaServer + UPnP frontend device, HTTP server, transcoding service, REST (MPExtended), share serving, remote-control API | |
| **Extensibility** | 110 active plugins; 18 extension-point roots; separate `OnlineVideos` repo (hundreds of site providers) | The ecosystem, and the biggest rewrite casualty |
| **Localization** | 44 languages; 1,702 English strings under `Source\` plus skin strings; Transifex pipeline | Translation debt restarts from zero on new strings |

**Scale check:** MP2 is ~613k LOC of C# — of which ~19% is `Incubator`/`Graveyard`/`Tools` (non-product). So the product proper is ~500k LOC, carrying two decades of accumulated edge-case handling from the MP1 lineage. Any plan whose estimate implies you can rebuild that in six months is wrong.

---

## 3. Four paths, priced

| Path | What it is | Effort | First shippable | Product risk | Ecosystem |
|---|---|---|---|---|---|
| **P1. Port in place** | Phases 0–5 of the assessment doc: portable core + Skia renderer, MP2 stays the product | **13.5–25 pm** | Linux server in ~6 mo | Low — never stops working | Fully preserved (plugins, skins, 44 languages) |
| **P2. New frontend, ported backend** ← *recommended default* | P1's Phase 1 (portable core) + **new UI application** against a new API layered on the existing backend services; no SkinEngine work at all | **14–22 pm** | Working new client in ~6 mo, Linux server same time | Medium — API boundary must be drawn honestly | Plugins/settings/domain preserved; **skins dropped**; new UI |
| **P3. New product, strangler core** ← *recommended if the goal is genuinely "a new media center"* | New repo, new API, new UI, new player/TV/packaging layers; MP2's domain assets (aspects, query algebra, extractors, ingest, resource access) **ported as modules** into the new backend; MP2 kept running for coexistence/migration | **33–54 pm** to daily-driver; 50–70+ pm to long-tail parity | Vertical slice in ~4–6 mo | High but *managed* by the strangler seam | Rebuilt deliberately (§7) |
| **P4. Big-bang greenfield** | Blank repo, everything from scratch, MP2 as a specification only | 50–80+ pm | 12–18 mo | **Very high** — nothing usable while you rebuild the domain; classic second-system failure | Rebuilt, but late |

**Cost of the ambition:** P3 costs roughly **2× a full port** and **~50% more than P2**, and buys a cleaner platform, a modern UX, and no inherited architecture debt. That may be exactly what you want. What it does *not* buy is speed: no matter how good the plan, MP2 parity is a multi-year program at any realistic team size.

**My recommendation:**
- If this is 1–2 part-time people (or a single maintainer): **P2**. It delivers a modern multiplatform frontend and Linux/Android reach — which is what most people actually mean by "new media center" — for roughly the same cost as the port, because it *deletes* the SkinEngine work instead of doing it.
- If there is a funded team of 3–5 and a 2–3 year horizon: **P3**, executed as §5–§6 with the vertical slice first.
- **P4 unconditionally no.** Not "risky" — no. §8 R1 explains why specifically.

---

## 4. Target architecture for the new system

### 4.1 Backend — API-first modular monolith

```
                      ┌──────────────────────────────────────────┐
  clients ───────────►│  API layer: REST+JSON (OpenAPI) + gRPC   │
  Win/Linux/Android   │  streaming (HLS/LL-HLS/DASH), WebSocket  │
  web / TV / phone    │  Bearer tokens, device pairing, OIDC opt.│
                      ├──────────────────────────────────────────┤
                      │  Modules (explicit boundaries, no        │
                      │  cross-module internal imports)          │
                      │  Library · Metadata · Artwork · Playback │
                      │  Transcode · TV · Users · Plugins ·      │
                      │  Devices · Notifications · Power         │
                      ├──────────────────────────────────────────┤
                      │  Ported-from-MP2 domain: MIA model,      │
                      │  query algebra, extractors, ingest,      │
                      │  resource-access, registry/settings      │
                      ├──────────────────────────────────────────┤
                      │  Persistence: EF Core + migrations       │
                      │  SQLite (default) / PostgreSQL           │
                      │  FTS5 search · OpenTelemetry             │
                      └──────────────────────────────────────────┘
```

Decisions I would make deliberately:

| Concern | Proposal | Why |
|---|---|---|
| Language/runtime | **.NET 8 LTS, `net8.0` with no `-windows` anywhere in the backend** | Keeps the ported domain assets in-language; PAL pattern from the assessment doc applies unchanged |
| Public contract | **Contract-first** OpenAPI + generated clients; versioned (`/api/v1`); additive-only changes | MP2's client coupling was to internal .NET interfaces, which is *why* the frontend can't be swapped. This single decision is what makes a multiplatform frontend strategy possible |
| Auth | Bearer tokens + device pairing; **no Windows identity, no impersonation** | Removes blocker B8 from the assessment entirely |
| Database | EF Core + migrations; SQLite default, PostgreSQL for large libraries | Fixes MP2's six-provider sprawl (§5 of the assessment) — support exactly two |
| Search | SQLite **FTS5** / `pg_trgm` | MP2 has no real full-text search |
| Ingest | Channel/Dataflow job queue; resumable; progress per share; re-import on change via USN/FSEvents/inotify | MP2's importer is single-threaded-ish and opaque |
| Transcoding | Decision engine (direct-play → direct-stream → transcode), **ffmpeg with hardware accel (VAAPI/QSV/NVDEC)**, HLS/LL-HLS output, adaptive | MP2's transcoding service is Windows-only and marginal |
| Live TV | **Connectors, not a stack**: TvHeadend, TVHeadend-adjacent (TVGateway/NextPVR), plus an IPTV/stream provider. Timeshift/recordings implemented server-side against the provider | Do not rebuild DVB driver handling per-OS; that single decision erases TV blocker B13 |
| DLNA/UPnP | Keep as a **compatibility adapter module**, not the backbone | MP2's core transport being UPnP is a major historical tax |
| Plugins | `AssemblyLoadContext` isolation + **capability-scoped API surface** (a plugin sees only what it registers for); manifest concept inherited from MP2's `plugin.xml` (it is genuinely good), versioned API levels | MP2 plugins get full in-process trust and reach internal APIs |
| Remote access | HTTPS + reverse-proxy support, mDNS discovery, optional relay; no port-forward requirement | MP2 expects LAN/UPnP port mapping |
| Observability | OpenTelemetry traces + structured logs + opt-in crash reporting | MP2 has log4net files |

### 4.2 Frontend — the actual decision

One UI codebase for Windows desktop, Linux desktop, Android phone/TV, plus browser reach. Options:

| Option | Win | Linux | Android/TV | Browser | 10-foot animation perf | Language | Video passthrough | Verdict |
|---|---|---|---|---|---|---|---|---|
| **A. Avalonia (Skia)** | ✅ | ✅ | 🟡 (maturing) | ❌ | good | C# | via native player bridge | **Best fit for a .NET team** — one language end to end, reuses ported domain models directly |
| **B. Flutter (Impeller)** | ✅ | ✅ | ✅ strong | 🟡 (web, heavy) | very good | Dart | via platform channels | **Best device reach & polish**; costs a second language |
| C. .NET MAUI / `net8.0-android` | 🟡 | ❌ | ✅ | ❌ | ok | C# | native | Wrong shape for 10-foot desktop |
| D. Qt/QML | ✅ | ✅ | ✅ | ❌ | very good | C++/QML | excellent | Strong but heaviest for this contributor base |
| E. Web SPA + Tauri/Electron shell | ✅ | ✅ | 🟡 (WebView) | ✅ **all TVs** | ok (CSS/DOM) | TS | weak (no passthrough, no HDR control) | **Unavoidable as a second client**, insufficient as the only one |
| F. Keep MP2 SkinEngine + Skia backend | ✅ | ✅ | ✅ | ❌ | excellent | C# | good | Highest fidelity to existing skins, most owned cost (see SkinEngine doc) |

**Proposal: A (or B) as the primary living-room app + E as a second, thin browser client.**

That pairing is what Jellyfin converged on and for good reason: the native app owns audio passthrough, HDR signalling, subtitle rendering, CEC/remote keys and frame pacing; the web client buys free coverage of webOS, Tizen, Google TV and Fire TV, plus remote browsing.

Choose **A** to stay single-language with the ported C# domain and to let existing contributors participate. Choose **B** if TV/mobile UX polish and Android-TV are the centre of the product and a second language is acceptable. **Do not choose E alone** — browser playback cannot do bitstream passthrough or HDR, which is non-negotiable for an HTPC audience.

Whichever is chosen: **video never renders inside the UI framework.** Native player per platform (libmpv on Win/Linux, Media3 on Android, platform player in the web client) behind one `IPlayerBridge` contract, composited as its own plane — the two-plane model from the SkinEngine proposal.

### 4.3 What must be *different*, not just re-platformed

A rewrite that only changes the platform wastes the rewrite. Fix these too:

1. **First-run works in 5 minutes** — add folders, see art, press play. MP2 needs an installer, a service, a database, metadata config and a codec stack before showing anything.
2. **Recognition by content, not file name.** MP2 leans on filename conventions plus NFO. Probe first (`ffprobe`/MediaInfo: duration, resolution, audio layout, embedded ids), filenames as a hint only.
3. **Config schema with validation and a real migration story** (MP2's registry is DB-backed XML with no versioning discipline).
4. **Audio/video capability policy per device** — query the sink (EDID/`AudioFormat` support) and decide direct-play vs transcode explicitly, with a visible "why am I transcoding?" explanation. This is MP2's most complained-about opacity.
5. **Subtitles owned by the player layer** (libass/Media3), including SSA/ASS styling and forced-subtitle selection — not by the UI renderer.
6. **Theming, not community skins.** A token/theme system plus layout presets and the existing home-screen-editor idea. Full arbitrary skin authoring is a permanent architectural tax (a whole XAML dialect, parser, and renderer feature set) and is the single biggest reason the current engine can't be replaced. Decide explicitly: you are trading skin richness for maintainability.
7. **No 32-bit, no `x86` build**, no registry, no MSI.
8. **Contract tests for the API in CI**, so any client (including a future one you haven't imagined) can be validated independently.

---

## 5. Migration and coexistence (the part greenfield plans omit)

A new media center's real adoption barrier is "I have 40 TB and a working setup."

| Need | Approach |
|---|---|
| Existing library | **Importer that reads MP2's SQLite/MySQL database directly**: shares, media items, aspects, relationships, per-user userData (watched/resume/last-position), playlists, and the settings tree where mappable |
| Metadata sidecars | Keep NFO support; also offer an "export from MP2 to NFO" so the data survives even without DB reading |
| Existing TV setup | TvHeadend config import where the user already runs it; otherwise record/EPG restart is unavoidable — say so publicly |
| During transition | Ship as a **separate product**; never an in-place upgrade. MP2 keeps working beside it, so a rewrite is never a cliff |
| Existing remote apps | Compatibility adapter exposing the UPnP/REST surface MP2 clients expect, for one release line |
| Translations | Machine-assisted pre-fill of new strings from the existing 44-language corpus (`strings_*.xml` is a 1,702-entry translation memory you already own) — cheap, and it makes day-one l10n non-trivial instead of English-only |

That last one is an underrated asset: **you do not have to start localization from zero.**

---

## 6. Phase plan (P3)

| Phase | Deliverable | Exit criterion | pm |
|---|---|---|---|
| **G0 — Spec & spike** | Functional spec from §2 + Appendix A into a public backlog. Spikes: (a) native player + UI compositing with passthrough/HDR on all three targets, (b) remote-input latency on Android TV, (c) MP2-database import PoC, (d) frontend framework bake-off (A vs B) on a real screen | Signed-off framework choice; passthrough proven; import PoC reads a real MP2 DB | 1.5–2 |
| **G1 — Vertical slice** | Server: 1 share, probe → Movie aspect, art fetch, SQLite. Client: browse posters → detail → direct play. Windows, Linux, Android | **Someone's daily driver for movies on all three OSes.** Nothing else works yet — that is the point | 6–9 |
| **G2 — Library breadth** | Series/seasons/episodes, music, photos, users + profiles + kid restrictions, full metadata providers, subtitles, search (FTS), resume, watch-state sync | A household runs its whole movie+TV+music collection on it | 8–12 |
| **G3 — Live TV** | TvHeadend + IPTV connectors, EPG, timers, conflict resolution, server-side timeshift, radio | Daily-driver including live TV | 4–6 |
| **G4 — Platform** | Remote access + TLS + pairing, plugin SDK, top-10 MP2-equivalent plugins, l10n (pre-filled 44 languages), deb/rpm/Flatpak/APK/MSIX, CI matrix, telemetry | Publishable as 1.0 | 6–10 |
| **G5 — Long tail** | DVD/Blu-ray menus, streaming-optimised transcode, OnlineVideos-class provider platform, webradio, CEC/IR/hardware remotes, statistics, weather/news | Diminishing returns forever — define "done" explicitly | 8–15 |

**Daily-driver at ~G2 end: 17–25 pm ⇒ 6–9 months with 3 engineers. Publishable 1.0: 29–47 pm ⇒ ~12–18 months.** MP2 feature *parity* including G5: 2.5–4 years. Set that expectation in writing before starting; the projects that fail are the ones that discover it in month 14.

Team-tier reality check:
- **1–2 developers:** P3 is not completable. Do **P2**, or contribute to Jellyfin/Kodi and build a frontend on their API.
- **3–4 developers:** P3 works if you accept ~18 months to 1.0 and freeze new MP2 features during G1–G2.
- **5+ / funded:** P3 with G5 scoped down aggressively, plus a web client as an extra pair of hands.

---

## 7. What you break, and the honest answer for each

| Broken by a rewrite | Real cost | Mitigation |
|---|---|---|
| **110 in-tree plugins** (source-incompatible APIs) | They don't migrate themselves | Port the top ~15 by hand in G4; publish the SDK in G2 so contributors can port the rest; deliberately retire the tail |
| **`OnlineVideos`** (hundreds of site providers, separate repo, high breakage rate) | Very high ongoing tax — this is a *site-scraping* product line, not a media center feature | Ship a provider SDK + make it a separate project/repo. Do not inherit it into core |
| **Skins** (1,090 XAML, community art, BlueVision) | Emotionally the largest breakage; the community's biggest investment | Theme system + license-permitting a "classic" theme; public, explicit statement that skin compatibility ends. Do this in the open or it becomes a revolt |
| **44-language translations** | Restart | Pre-fill from the existing corpus (§5) |
| **MP2 contributor base** | The people who know the code have the least reason to like a rewrite | Write the spec *with* them (G0), publish the backlog, land G1 fast so the value is visible |
| **Tacit edge-case knowledge** (codecs, splitters, subtitle formats, DVB quirks, Blu-ray menus) | The classic rewrite killer — you only find these in the field | Keep MP2 as a reference oracle, port behaviours with tests capturing them, and be explicit that some features will regress for a while |

---

## 8. Risk register

| # | Risk | Sev | Mitigation |
|---|---|---|---|
| R1 | **Second-system effect / big-bang temptation** — the team designs the ideal plugin system, schema and API for 9 months and ships nothing | 🔴 | G1 is a hard gate. No phase may start without the previous one shipping to real users. Explicit rule: *nothing in G2+ is designed before G1 runs on a couch* |
| R2 | Feature parity treated as the goal, so the project is perpetually "almost ready" | 🔴 | Define a **feature budget** for 1.0 (the 20% of MP2 that is 100% of usage: movies, series, music, photos, live TV, users, subtitles, remote). Publish what is *out of scope* |
| R3 | Domain knowledge lost in a from-scratch backend | 🟠 | Strangler core (§1): port MIA + query algebra + extractors rather than re-deriving them |
| R4 | Frontend framework chosen on enthusiasm, then found weak at 10-foot input latency or HDR | 🟠 | G0 bake-off on real hardware, not a laptop |
| R5 | Passthrough/HDR not achievable with the chosen compositing model | 🟠 | G0 spike (a); it is an architecture-level go/no-go |
| R6 | Provider (TMDb/TVDB/etc.) API changes during an 18-month build | 🟡 | Providers as isolated modules behind a stable interface + contract tests; they will still cost ~20% of maintenance forever |
| R7 | MP2 gets abandoned before the replacement is equal → users stranded, goodwill lost | 🟡 | Never announce a sunset date before G4 |
| R8 | Player-layer duplication (UI framework's player vs native bridge) | 🟡 | `IPlayerBridge` from day one; the UI framework never owns video |

---

## 9. Decisions needed from you

1. **Team reality** — 1–2, 3–5, or funded org? (Decides P2 vs P3; everything else is negotiable, this isn't.)
2. **Skin compatibility** — preserve community skins (forces option F / keeps the SkinEngine), or accept a theme system and a clean break?
3. **Plugin ecosystem** — source-compatible-ish SDK inherited from MP2's model, or fresh minimal SDK + accept the tail dies?
4. **Live TV strategy** — connectors only (recommended; removes the hardest Windows coupling) or own the DVB/record stack yourself?
5. **Frontend** — stay single-language .NET (Avalonia) or best-reach-polish (Flutter) + a web client?
6. **Relationship to MP2** — hard fork under the same GPL-2.0 umbrella sharing code and contributors, or a clean-room new product? (GPL-2.0+ means sharing is legally easy; the choice is social, not legal.)

Answer 1, 2 and 5 and I can turn §6 into a concrete sprint-level plan with named modules and a CI/packaging setup.

---

## Appendix A — Functional spec skeleton (backlog seed)

Derived from the measured code surface; use directly as an epic list.

**Core:** content recognition & probe · library storage (aspects/relationships/userData) · query & browse engine · full-text search · artwork/fan-art pipeline · thumbnail generation · metadata provider framework · subtitle discovery · playlist management · share management & monitoring · per-user profiles + PIN + age certification · settings tree (data-driven) with import/export · plugin host with capability scoping · logging/telemetry · update mechanism · license/credit screens.

**Playback:** direct play · direct stream · transcode decision engine · hardware-accelerated transcode · audio passthrough (AC-3/E-AC-3/DTS/DTS-HD/TrueHD) · stereo downmix · volume normalization · audio delay & lip-sync · subtitle render/position/style/forced · multiple audio/subtitle tracks · resume & watched flags · media stacking & version selection · DVD/ISO · Blu-ray menus (deferred) · gapless audio · visualisations/slide show · sleep timer & idle power.

**Live TV:** provider connectors · channel groups · EPG grab + guide UI · timers + manual scheduling · conflict resolution · server-side timeshift · recordings manager · radio · TV streaming to clients · teletext (deferred) · picture settings per channel.

**Devices & clients:** device pairing · per-device capability profile · remote control / input routing · CEC · hardware remote bridges (MCE/IR) · multi-audio-zone · second-screen remote app · web client · DLNA/UPnP compatibility adapter.

**Platform:** Windows/Linux/Android clients · headless server · deb/rpm/Flatpak/AppImage/MSIX/AAB · HTTPS + reverse proxy · mDNS · backups · migration from MP2 · pre-filled l10n (44 languages) · theme system · accessibility (fonts, contrast, screen-reader basics) · API contract tests in CI.

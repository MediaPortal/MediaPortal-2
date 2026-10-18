# P2 — New Frontend on a Ported Backend: Detailed Plan

**Scope:** replace MediaPortal 2's entire client/UI stack with a new multiplatform frontend, while porting MP2's backend to run on Windows **and** Linux. No SkinEngine work.
**Status:** recommended default path. Chosen over P1 (full port, 13.5–25 pm) because it *deletes* the renderer work instead of doing it; chosen over P3 (greenfield product, 33–54 pm) because it keeps 47 backend plugins and the whole domain model working from week one.
**Companions:** [Multiplatform-Assessment-and-Plan.md](./Multiplatform-Assessment-and-Plan.md) · [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md) · [New-Media-Center-Feasibility-and-Blueprint.md](./New-Media-Center-Feasibility-and-Blueprint.md)
**Date:** 2026-10-17

---

## 1. Why P2 is structurally viable here (this is the evidence)

I checked the project-reference graph to see whether MP2 actually has a client/server seam worth building on. It does, and it is clean:

```
MediaPortal.Common   → MediaPortal.Utilities, UPnP
MediaPortal.Backend  → MediaPortal.Common, MediaPortal.Utilities, UPnP     ← no UI reference
MediaPortal.UI       → MediaPortal.Common, MediaPortal.Utilities, UPnP     ← no Backend reference
SkinEngine           → MediaPortal.Common, MediaPortal.UI, Utilities, Configuration
MediaPortal.Client   → MediaPortal.Common, MediaPortal.UI, Utilities       ← does NOT reference Backend
MediaPortal.Server   → MediaPortal.Backend, MediaPortal.Common, Utilities  ← does NOT reference UI
```

**`MediaPortal.Backend` and `MediaPortal.UI` have zero dependency on each other.** Both depend only on the `MediaPortal.Common` contract layer. They already run in different processes and talk over a wire protocol (UPnP). MP2 is not a monolith with UI code tangled into the domain — it is a strict two-sided architecture whose one flaw is that the *client side of the wire* happens to be a Direct3D 9 engine with a Win32 message loop.

That single fact is what makes P2 work: **the frontend is already a swappable peer, and the seam is a compiled boundary rather than something you have to invent.**

### Plugin census — what survives, measured

Classified by whether each plugin's project references `MediaPortal.UI`/`SkinEngine`:

| Side | Count | Fate under P2 |
|---|---|---|
| **Server-side** | **47** | **Survive, unchanged** |
| **Client-side** | **38** | **Replaced by the new frontend** |
| Skin/data resources | 25 | Replaced by theme system |

**The 47 that survive** — i.e. the entire backend feature set keeps working the day the new frontend first boots:
`ServerBase`, `ServerSettings.Service`, `ServerStateService` · 6 DB providers (`SQLiteDatabase`, `MySQLDatabase`, `MSSQLDatabase`, `FirebirdDatabase`, `ScimoreDb`, `SQLCEDatabase` — trim to SQLite/MySQL/PostgreSQL per §3.4) · **all 29 metadata extractors** (`MediaInfoLib`, `FFMpegLib`, `TagLib`, `MatroskaLib`, `NfoMetadataExtractors`, `BluRayMetadaExtractor`, `SubtitleMetadataExtractor`, `SubtitleDownloaderProvider`, `Video/Audio/Image/Movie/Series/Genre/Stub/Tve3Recording/TvMosaic` extractors, `OnlineLibraries`, `ScriptableMetadataExtractor`, thumbnail providers, `OpenCvLib`, `FreeImageLib`) · 4 resource providers (AudioCD, Iso, NetworkNeighborhood, Zip) · `ConfigurationManager` · TV backend (`SlimTvService3`, `SlimTvService35`, `SlimTvProxy3`, `SlimTvResources`, `TvMosaic.Server`).

**The 38 that get replaced** — and this list *is* the frontend feature backlog:

| Group | Plugins | New-frontend equivalent |
|---|---|---|
| Renderer + skins | `SkinEngine`, `BlueVision`, `Nereus`, `Titanium Extended`, `WMC` | UI framework + theme system (§4.2) |
| Players | `VideoPlayer` (DirectShow/EVR), `BassPlayer`, `ImagePlayer`, `BDHandler`, `RefreshRateChanger`, `VideoEffectChanger` | `IPlayerBridge` on libmpv / Media3 (§4.3) |
| Input | `InputDeviceManager`, `InputDevices`, `HidInput`, `IrInput`, `MceRemoteReceiver` | New input layer (§4.5) |
| TV UI | `SlimTvClient`, `SlimTvNativeProvider`, `SlimTVMPExtendedProvider`, `SlimTv.TvMosaicProvider` | TV screens + provider connectors over the API |
| Feature screens | `Media`, `SkinBase`, `Configuration`, `News`, `Weather`, `SystemStateMenu`, `BackgroundManager`, `PartyMusicPlayer`, `StatisticsRenderer`, `RemovableMediaManager`, `Utilities`, `WifiConfiguration` | Screen set (§4.4) + generic settings UI (§4.6) |
| Fan art | `FanArtService{,.Client,.Local,.Online}` | **Move server-side** — it is HTTP + image work, not UI (§3.5) |

Note the asymmetry that makes P2 cheap: **the 47 survivors contain nearly all of the hard domain logic** (import, metadata, DB, TV scheduling, resource access), while the 38 replaced items are presentation, where a modern framework gives you better output for less code.

**The coexistence payoff:** because `MediaPortal.Client` does not reference `MediaPortal.Backend`, the old SkinEngine client and the new frontend are *peers*. You can ship both against one ported backend — the classic Windows client keeps working while the new frontend matures, so P2 never has a cut-over cliff. That property is unique to this path; P3 does not have it.

---

## 2. The pivotal new component: API facade

P2's one irreversible decision is how the new frontend talks to the backend. MP2's client reaches services through in-process `ServiceRegistration` (standalone mode) or a hand-rolled UPnP device graph (remote mode). **Reusing either for the new frontend would re-create today's coupling** — the reason MP2's UI cannot be swapped *is* that its client is bound to internal .NET interfaces.

### 2.1 Rules

1. New project **`MediaPortal.Server.Api`** — ASP.NET Core / Kestrel, self-hosted inside `MediaPortal.Server`. Not OWIN, not the legacy `HttpServer`, not `http.sys`.
2. It is a **thin facade**: it resolves existing services from `ServiceRegistration` (`IMediaBrowsing`, `IMediaAccess`, `IUserManagement`, `IMediaFanArtHandler`, shares, registry/settings, TV contracts) and maps them to a versioned HTTP contract. **No domain logic lives in the facade.**
3. **The frontend must not reference `MediaPortal.Common`.** Enforce as a CI architecture test from day one. This is the rule that makes "multiplatform frontend" permanent rather than a one-time achievement.
4. Same code path local and remote — the frontend always speaks HTTP to `127.0.0.1` when local. No in-process fast path, no `#if` for local mode. (This costs a little loopback overhead on metadata-heavy screens and buys the ability to run any client on any device. Take it.)
5. Contract-first: OpenAPI spec in the repo, generated clients, additive-only evolution, `/api/v1`, contract tests in CI.

### 2.2 Surface

Only ~18 of the 89 contracts in `MediaPortal.Common` are client-relevant. Suggested endpoints:

```
GET  /api/v1/library/items                    # MediaItemQuery → JSON (query algebra serialised)
GET  /api/v1/library/items/{id}               # item + all aspects + relationships + userData
POST /api/v1/library/query                    # filter tree POST (Boolean/Relational/Relationship/UserData)
GET  /api/v1/library/items/{id}/artwork/{kind}   # fanart/thumbnail, server-side cached
GET  /api/v1/stream/{itemId}                  # direct play
GET  /api/v1/stream/{itemId}/master.m3u8      # HLS / LL-HLS transcode
GET  /api/v1/stream/{itemId}/streams          # audio/subtitle track + capability decision
GET  /api/v1/shares  POST/DELETE …/shares     # share CRUD + rescan + status
GET  /api/v1/settings/tree  GET/PUT …/settings/{path}   # registry-backed settings
GET  /api/v1/users       POST /auth/login     # profiles, PIN, per-user data
GET  /api/v1/tv/guide  /channels  /timers  /recordings  /timeshift
GET  /api/v1/devices  /api/v1/system/state  /api/v1/system/capabilities
GET  /api/v1/events                           # WebSocket: MP2's XMessaging → typed events
```

Two things to get right early, both cheap now and expensive later:

- **Query serialisation, not endpoint proliferation.** Exposing MP2's filter algebra as a POST-able filter tree means the new frontend gets filtering, sorting, per-user state, age restriction and relationship traversal for free and never needs a new endpoint per screen. This is MP2's best asset crossing the boundary intact.
- **Event stream wrapping `MediaPortal.Common\Messaging`.** Import progress, playback state, TV timer state, share errors and server state changes already broadcast through MP2's messaging layer; bridging it to one WebSocket avoids the polling that MP2's remote mode is built on.

Auth: bearer tokens + device pairing (no Windows identity, no impersonation — erases blocker B8 from the assessment).

---

## 3. Backend work items

| # | Item | Files / evidence | Effort |
|---|---|---|---|
| 3.1 | **Break the WinForms spine** — move ~11 files out of `MediaPortal.Common`/`MediaPortal.Utilities` into `*.Windows` satellites; retarget `Common`/`Utilities`/`Backend` to `net8.0` (no `-windows`) | `Screens\SplashScreen.cs`, `Async\ControlAwaiter.cs`, `Services\Logging\{CrashLoggerBase,log4netLogger,UiCrashLogger}.cs`, `UI\FormDpiAwareness.cs`, `Exceptions\LauncherExceptionHandling.cs`, `Configuration\ConfigColoredItemList.cs`, `FanArt\BaseFanArtHandler.cs`, `MediaManagement\Helpers\BaseInfo.cs`, `ImageUtilities.cs` | 1–1.5 pm |
| 3.2 | **PAL** (`IPlatformHost`/`Power`/`Identity`/`Shares`/`Credentials`/`Paths`/`Notification`) bound via existing `ServiceRegistration`; `IHostedService` server with systemd unit + `Microsoft.Extensions.Hosting.WindowsServices` | `Main\MediaPortal.Server\{ApplicationLauncher,WindowsService}.cs`; note `--console` mode already exists and its startup is a clean `StartCoreServices()` → `StartupBackendServices()` sequence | 1.5–2 pm |
| 3.3 | **Linux resource access**: SMBLibrary replacing `WNet*`/`SharesEnumerator`; per-share credentials replacing token impersonation; FUSE or drop Dokan; drop AD/`System.DirectoryServices` | `Utilities\Network\SharesEnumerator.cs`, `Utilities\FileSystem\DriveUtils.cs`, `Services\ResourceAccess\ImpersonationService\`, `Services\Dokan\` | 1–1.5 pm |
| 3.4 | **Persistence**: keep SQLite + MySQL, add PostgreSQL, retire SQL CE / Firebird-embedded / ScimoreDB / MSSQL; add schema migrations (MP2 has none); replace both `BinaryFormatter` uses | `Source\Backend\Database\*`, `…\DiskCachedDictionary.cs`, `…\TvdbLib\Cache\BinaryCacheProvider.cs` | 1–1.5 pm |
| 3.5 | **Move fan-art providers server-side** (they sit under `Source\UI\FanArt` only for historical reasons — they are HTTP + image fetchers) | `UI\FanArt\FanArtService{,.Local,.Online}` | 0.25 pm |
| 3.6 | **`MediaPortal.Server.Api`** (Kestrel + OpenAPI + auth + query serialisation + event bridge) and migrate the OWIN/`MP2Extended` REST surface onto it | new project; `Incubator\MP2Extended` | 2–3 pm |
| 3.7 | **Transcode decision engine** on ffmpeg with VAAPI/QSV/NVDEC + HLS, replacing the Windows transcoding service | `Tests\Test.TranscodingService`, `Incubator\MediaServer` | 1–1.5 pm |
| 3.8 | **TV**: keep `SlimTvService3` Windows-only *behind the API*; add a **TvHeadend connector** so Linux gets TV at all | `UI\TV\SlimTvService3\`, `UI\TV\TvMosaic\` | 1.5–2.5 pm |
| 3.9 | Housekeeping: WMI/EventLog, `#if NET5_0_OR_GREATER` cleanup (104 sites), path/case-sensitivity audit, CI matrix + arch test | — | 0.5–1 pm |

**Backend subtotal: 6–9 pm** (overlaps P1's Phases 1–2; the API facade is the genuinely new line item).

---

## 4. Frontend work items

### 4.1 Framework — settle it in two weeks, on a TV

| Option | Win | Linux | Android | Fit for P2 |
|---|---|---|---|---|
| **Avalonia** | ✅ | ✅ | 🟡 maturing | **Recommended.** C# end to end (reuse the ported models as DTOs), Skia compositor, good 10-foot perf, existing contributors can participate, XAML-ish skills transfer |
| Flutter | ✅ | ✅ | ✅ strong | Choose if Android/Android-TV is the centre of gravity; costs Dart and a second API client |
| MAUI | 🟡 | ❌ | ✅ | Wrong shape — no Linux desktop |
| Web SPA (+ Tauri) | ✅ | ✅ | 🟡 | **Second client**, not primary: no audio passthrough, no HDR signalling, weak for 10-foot frame pacing |
| Keep SkinEngine + Skia | ✅ | ✅ | ✅ | Only if skin compatibility is non-negotiable — then you are doing P1's Phase 3 (+5–7 pm) |

Do a **2-week bake-off** of the poster wall + detail page + playback overlay on a real television (not a laptop) with a remote, measuring input latency and 4K image scroll smoothness. Decide there. This is the single highest-leverage 2 weeks in the plan — a framework swap after G2 costs more than everything in §3 combined.

### 4.2 Frontend architecture

Single-language MVVM (`CommunityToolkit.Mvvm`) + DI; feature modules mirroring the §4.4 screen groups; a navigation stack replacing MP2's client-side workflow manager (`MediaPortal.UI`'s workflow/screens/state stack is client-side and is *not* ported — budget for a clean reimplementation, it is small and well understood); theme resource dictionaries instead of skins; hardware-accelerated image caching (the piece people underestimate for poster walls — budget it explicitly).

### 4.3 Playback — `IPlayerBridge`

**Video never renders inside the UI framework.** Two-plane compositing (UI plane over player plane), as in the SkinEngine proposal:

- **libmpv** on Windows + Linux via the mpv client/render API — one engine, one subtitle renderer (libass → SSA/ASS styling works on all three platforms for the first time), one passthrough path (compressed audio offload via mpv audio device config), one seek/resume model.
- **Media3/ExoPlayer** on Android.
- Bridge contract: load/seek/track selection/speed/aspect/crop/subtitle style/audio-delay/state-events/error. Nothing UI-specific leaks in.

**Honest regression disclosure for Windows users:** P2 retires DirectShow/EVR, `mpcSubs.dll`, `EVRPresenter.dll` and the codec/splitter configuration UI. That is a net win operationally (no more codec-pack roulette, no 32-bit, no registered filters) but it *will* regress some behaviours until mpv is tuned: bit-exact passthrough of some exotic layouts, DVD/Blu-ray **menu** navigation, per-refresh-rate mode switching, and video-effect/Ken-Burns features. Prototype passthrough + a 4K HDR remux clip in G0 before committing — same gate as the SkinEngine proposal's R1.

### 4.4 Screen set

Derived from BlueVision's 43 XAML screens, of which 7 are master templates, so ~36 real screens collapse to **20 target screens**:

| From MP2 screens | New screen |
|---|---|
| `home`, `master_homemenu` | **1. Home / hub** (user-selectable tiles, background manager) |
| `BrowseMediaNavigation`, `master_menu`, `master_bare` | **2. Media navigation shell** (category → node tree) |
| `MoviesShowItems`, `SeriesShowItems`, `VideoShowItems`, `AudioShowItems`, `ImageShowItems`, `SimpleShowItems`, `SimpleFilterItems` | **3. One generic grid/list control** + configured instances — MP2 has seven near-duplicate screens here; this is where the new frontend is structurally cheaper |
| `SimpleSearch`, `SeriesFilterByName`, `SeriesFilterBySeason`, `AudioFilterByAlbum` | **4. Search & filter panel** (drives the query API directly) |
| `FullScreenContentVideo`, `CurrentlyPlayingVideo`, `master_bare_players` | **5. Video player + OSD** |
| `FullscreenContentImage`, `CurrentlyPlayingImage` | **6. Image viewer / slideshow** |
| `FullscreenContentAudio`, `CurrentlyPlayingAudio`, `FullScreenContentDVD` | **7. Audio now-playing** (+ DVD collapses into 5) |
| `dialogBlueVisionConfiguration`, `master_configuration_menu`, `configuration-section`, `master_dialog_*` | **8. Settings shell** (§4.6) |
| `SharesOverview`, `shares_config_*` (8 screens) | **9. Share wizard** — 8 MP2 screens collapse to one wizard with a status/progress view |
| `ShowHomeServer` | **10. Server status / devices** |
| `DialogVirtualKeyboard` | **11. On-screen keyboard** (needed on TV; trivial on touch) |
| `weather` | **12. Weather tile** (low priority) |
| — | **13–20 (new): TV guide, channel list, timer list, recording list, user/profile picker, login/PIN, now-playing-minimal, first-run/onboarding** — MP2 has no first-run experience and creating one is a stated P2 goal |

### 4.5 Input

Keyboard/mouse/touch; TV remotes (CEC on Linux via `libcec`/DRM, HDMI-ARC, Android remote keys, Windows raw input); IR only if a `lirc`/eventdev path proves cheap. MP2's `MceRemoteReceiver` (`setupapi`+`hid`) and `IrInput` (`IrssComms.dll`) do not migrate; treat hardware remotes as G3+ unless the team uses one daily.

### 4.6 Settings — the cost saver hiding in the measurement

I counted the settings surface: **121 `ConfigSetting` declarations, 121 distinct handler classes, but only 8 classes deriving directly from `ConfigSetting`** — the rest are thin subclasses of generic widgets (e.g. `Splitter : GenericCodecSelection`). Combined with the fact that MP2 already declares its whole settings tree (64 sections / 121 settings) **as data in plugin manifests**, the right P2 move is:

> Expose the settings tree + current values over the API (`GET /api/v1/settings/tree`), and have the frontend render **generic widgets** from descriptors (bool, enum, int-range, file, folder, codec, colour, key-capture, per-user override).

That converts ~121 bespoke UI classes into **~19 custom handlers plus one generic settings engine**. It preserves MP2's genuinely good data-driven configuration model and is the largest single saving in §4.

### 4.7 Frontend subtotal

| Work | pm |
|---|---|
| Framework bake-off + app shell, navigation, theming, image cache, input | 1.5–2 |
| Player bridge (mpv + Media3), subtitles, passthrough, A/V sync | 2–3 |
| Screens 1–11 (core library UX + share wizard + settings shell) | 3–4 |
| TV screens over the API | 1–1.5 |
| Users/profiles/kid restrictions, onboarding, first-run | 0.5–1 |
| Screens 12+ (weather, statistics, misc) + polish + Android layout pass | 1–2 |
| **Frontend total** | **9–13.5** |

---

## 5. Phases

| Phase | Deliverable | Exit criterion | pm |
|---|---|---|---|
| **G0** (2–3 wks) | Framework bake-off on a real TV; mpv passthrough + HDR remux prototype on Win/Linux; arch test banning `MediaPortal.Common` from frontend projects; CI matrix | Framework and player chosen with evidence; arch gate live | 0.5–1 |
| **G1** | Backend `net8.0` + PAL + systemd/Win service; **API facade v1** (library query, artwork, stream, users, events); new app doing browse → detail → direct play | **Movies work on Windows and Linux with the new UI.** Old MP2 client still works against the same server | 4–5 |
| **G2** | Share wizard, import progress via events, series + music + photos, users/profiles/kids, settings shell + generic widgets, search/filter, subtitles, resume, watch-state sync | A household runs its whole movie+TV+music collection on the new frontend | 4–6 |
| **G3** | TV: TvHeadend connector (Linux) + SlimTv behind API (Windows), guide/timers/recordings/timeshift screens; transcode decision engine + HLS; Android client | Live TV on all three platforms; Android app in testing | 3–5 |
| **G4** | Remote access + TLS + pairing, plugin SDK surface for client extensions, l10n pre-filled from the existing 44-language corpus, packaging (deb/rpm/Flatpak/MSIX/AAB), decision on the legacy client | Publishable 1.0 | 2–3 |

**Total 13.5–20 pm ⇒ ~6–9 months to a daily-driver with 3 engineers; ~12–15 months to 1.0.**
Sequencing: G1's backend and frontend tracks are independent after G0, so two people run them in parallel from week 3.

### Team tiers

| Team | What to do | Cost |
|---|---|---|
| **1–2 devs** | Drop the native frontend. Do §3.1–3.6 (backend + API) and build the frontend as a **web client** (no passthrough — acceptable if the TV box direct-plays), plus a Media3 Android app later | 8–12 pm, shipping in ~6 months |
| **3–4 devs** | Full P2 as phased above; freeze new MP2 features during G2 | 13.5–20 pm |
| **5+ / funded** | P2, then graduate to the blueprint's plugin SDK and treat P2's backend as the P3 strangler core — P2's work is fully reused, nothing is thrown away | — |

---

## 6. What P2 costs you (no sugar-coating)

| Loss | Sev | Handling |
|---|---|---|
| **Skin compatibility ends** — 4 built-in skins, 1,090 community skin files, the community's biggest investment | 🟠 highest | Theme system + a "classic" homage theme; announce *why* in the open before G1, not after |
| DirectShow/EVR retired **on Windows too** — menus, bit-exact passthrough, refresh-rate, video effects may regress | 🟠 | G0 prototype; keep the legacy MP2 client shipping in parallel (possible because Client ⟂ Backend) as the escape hatch |
| 38 client plugins must be reimplemented or retired (`Media` alone is 385 `.cs` + 111 XAML) | 🟠 | The screen set (§4.4) *is* the reimplementation; the generic grid (screen 3) absorbs most of `Media`; state explicitly which plugins retire |
| Two UI codebases in parallel → contributor split, duplicated bug reports | 🟠 | Publish the boundary: legacy client = fixes only; new frontend = all features; triage tags from day one |
| MCE/IR remote, WiFi configuration UI, DVD/BD menus, statistics, weather | 🟡 | Deferred to G3+/never; say so in the roadmap |
| Linux live TV needs TvHeadend running | 🟡 | Documented dependency, not a regression — MP2 cannot do Linux at all today |
| API facade is permanent maintenance surface | 🟡 | Cheap insurance: it is exactly what keeps the frontend swappable forever, including a later P3 |

What you gain for it: one frontend codebase on Windows/Linux/Android, **working SSA/ASS subtitles on all three** (libass via mpv — impossible today outside Windows), no codec packs or 32-bit, no registry/MSI, a browser-reachable API for third parties, and the option to walk further (P3) with zero sunk cost.

---

## 7. Decisions

1. **Framework** — Avalonia (recommended) vs Flutter. G0 decides it on hardware; nominate the person who owns that call now.
2. **Skin compatibility** — accept the break, or stop reading P2 and do P1's Phase 3 instead. There is no middle path.
3. **Team size / MP2 feature freeze** — decides between the 8–12 pm web variant and the 13.5–20 pm full variant.
4. **Linux TV** — TvHeadend connector (recommended, 1.5–2.5 pm) or "no live TV on Linux at 1.0".
5. **Legacy SkinEngine client** — keep shipping it as a Windows-only maintenance client during P2 (recommended; it is your regression escape hatch), or declare it dead at G1.

Confirm 1, 3 and 5 and I'll turn G0–G1 into a week-by-week plan with named projects, the OpenAPI skeleton, and the CI/packaging setup.

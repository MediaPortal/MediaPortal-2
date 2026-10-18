# MediaPortal 2 — Multiplatform Port Assessment & Plan

**Target platforms:** Windows (current), Linux (server + desktop), Android
**Codebase assessed:** `d:\Mediaportal\MediaPortal-2` (MediaPortal 2, GPL-2.0+, Team MediaPortal)
**Date:** 2026-10-17

---

## 1. Executive summary

MediaPortal 2 is **not** a Windows-application-that-happens-to-use-.NET. It is a **Windows-platform application**: the media center's own rendering engine is built directly on Win32 windows + Direct3D 9 + HLSL, video playback is DirectShow/EVR, and the shared core assemblies are compiled against WinForms. Multi-targeting `net48;net6.0-windows` proves this — every single project that matters is `-windows`.

The good news: the **backend is architecturally separable and is the valuable part**. Media library, metadata import, database layer, plugin/registry/settings model, resource-access abstraction, UPnP server, HTTP server, user management and messaging are all plain .NET and roughly 25% of the code. The **frontend is effectively unportable as written** and should be treated as a Windows-only component with new, thinner clients for Linux desktop and Android.

**Verdict per layer**

| Layer | Share of code | Portability | Recommended treatment |
|---|---|---|---|
| `Source\Core` (Common, Utilities, Backend, UI-abstractions, UPnP, HttpServer, ConfigurationManager) | ~143k LOC | **High** — ~11 files block it | Retarget to `net8.0` (no `-windows`), add a Platform Abstraction Layer |
| `Source\Backend` + DB providers | ~8k LOC | **Medium-high** | Keep SQLite/MySQL/PostgreSQL; drop SQL CE, Firebird-embedded, ScimoreDB |
| `Source\Extensions` (metadata extractors, resource providers) | ~93k LOC | **Medium** | Mostly portable; ~6 extractors need native swaps (GDI+, WIC, Shell, OpenCV-Windows) |
| `Source\UI\SkinEngine` (custom D3D9 renderer + XAML skins) | 82,952 LOC (of ~230k in `Source\UI`) | **Medium** — only 24% is D3D-coupled | Extract a ~20-operation GPU interface; keep the 76% engine-agnostic core; add Skia backends |
| `Source\UI\Players` (DirectShow, EVR, EVRPresenter C++, MPC subtitles) | large | **None** | Windows-only. New player backends: libmpv/GStreamer (Linux), Media3/ExoPlayer (Android) |
| `Source\UI\TV` (SlimTV/TvEngine, DVB, MPExtended, TvMosaic) | large | **Low** | Keep as optional Windows TV provider; use TvHeadend/TVProxy on Linux |
| `Setup` (WiX MSI + WPF bootstrapper) | ~9k LOC | **None** | Replace with per-platform packaging |
| `Tools`, `Incubator`, `Graveyard` | ~105k LOC | n/a | Dev-only / dead code — exclude from port scope |

**Realistic outcome**

* **Linux headless server** (media library, streaming, UPnP/DLNA, web API, remote-control back end): **achievable**, ~3–5 months with 2–3 engineers. Highest value per unit of effort.
* **Linux desktop client with feature parity of the current UI**: **cheaper than it first appears, and not a rewrite.** SkinEngine is 82,952 LOC, of which only 19,877 (77 files) touch Direct3D; the control tree, MPF dependency-property layer, runtime XAML parser, animations, geometry and skin management are all Windows-independent, and the Direct3D 9 surface is ~20 operations. Extract a GPU interface, keep Dx9 as backend #1, add Skia → ~5–7 pm. The remaining cost is Linux windowing and the video plane. Full analysis in [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md).
* **Android**: two options, and the choice depends on Phase 3. Once Skia sits under the existing engine (Phase 3), MP2's skin engine itself runs on Android for only **1–2 pm** more — Skia *is* Android's 2D/text stack. If you skip Phase 3, build a new thin client (`.NET for Android`/MAUI or Kotlin) against the server's UPnP + HTTP surface instead: 4–6 months, better touch ergonomics, no skin compatibility.

**The one recommendation that matters:** stop treating "multiplatform" as a single goal. Split it into **portable core** → **Linux server** → **renderer abstraction (Linux desktop)** → **new Android client**, in that order. Everything else follows from that split.

---

## 2. Method and repo snapshot

Static analysis of the checked-out tree. **No builds were run** (Windows-only toolchain, ~1 h build) and the `OnlineVideos` git submodule is **not checked out** (empty directory), so that plugin set is assessed from its `.gitmodules` reference only.

| Metric | Value |
|---|---|
| Total files / size | 12,043 files / 725 MB |
| C# files / LOC | 4,570 files / ~613k LOC |
| `.csproj` projects | 166 (138 SDK-style, 28 legacy non-SDK) |
| Solutions | 15 (`MP2-Client` 140 project entries, `MP2-Server` 91, `MP2-ServiceMonitor` 6, `MP2-Setup`, 11 tool solutions) |
| Distinct NuGet packages | 96 |
| XAML files | 1,090 (672 under `Source\UI`) |
| HLSL shaders (`.fx`) | 35 |
| P/Invoke `[DllImport]` sites | ~200 across 44 files |
| Framework-conditional `#if` | 104 (`#if NET5_0_OR_GREATER` ×99, `#if NET6_0` ×2, `#if !NET6_0` ×2, `#if NET6_0_OR_GREATER` ×1) — vs. 120 `#if DEBUG` |
| Projects pinning `PlatformTarget` x86/x64 | 56 |
| `plugin.xml` descriptors | 115 |
| Native C++ projects | 4 (`.vcxproj`: EVRPresenter, BaseClasses) |

**LOC by area**

| Area | Files | LOC |
|---|---|---|
| `Source\UI` | 1,595 | 230,506 |
| `Source\Core` | 949 | 143,176 |
| `Incubator` | 992 | 97,462 |
| `Source\Extensions` | 537 | 92,681 |
| `Tests` | 103 | 13,559 |
| `Setup` | 145 | 8,744 |
| `Source\Main` (exes) | 66 | 8,523 |
| `Tools` | 65 | 7,619 |
| `Source\Backend` | 86 | 7,603 |
| `Resources` | 22 | 2,116 |
| `Graveyard` | 10 | 568 |

---

## 3. Current architecture (as-is)

### 3.1 Process model

| Executable | Type | Stack | Role |
|---|---|---|---|
| `MediaPortal.Server` | `WinExe` | WinForms + `ServiceBase` | Backend. Runs as **Windows Service**; `--console` flag runs it interactively (`ApplicationLauncher.Main`) |
| `MediaPortal.Client` | `WinExe` | WinForms host + SkinEngine/D3D9 | The media center UI |
| `MediaPortal.ClientLauncher` | `WinExe` | WPF | Restarts/hardens the client |
| `MediaPortal.ServiceMonitor` | `WinExe` | WPF | Tray monitor for the service; UAC helper |
| `MediaPortal.Manager` | `WinExe` | WinForms, **legacy csproj, net48 only** | Configuration tool |
| `MP2-Setup` / `MP2BootstrapperApp` | MSI + WPF bundle | WiX 4 | Installer |

### 3.2 Coupling model

* **`ServiceRegistration` service-locator + plugin system** is the spine: 115 `plugin.xml` descriptors, ~4,900 `ServiceRegistration` call sites. The client can host the backend **in-process** (standalone mode) or attach to a remote server.
* **Client↔server transport is a custom UPnP implementation** (`Source\Core\UPnP`, `MP2FrontendServerDevice`, `UPnPContentDirectoryServiceProxy`, `ServerConnectionManager`), plus an HTTP server (`Source\Core\HttpServer`, which includes a hand-written raw-socket `HttpListener2` — that is *good* news for portability, since it avoids `http.sys`) and OWIN/WebAPI in `Incubator\MP2Extended`.
* **Settings/"Registry" is MP2's own DB-backed abstraction** (`Services\Registry\Registry.cs`), *not* the Windows registry. That removes a class of blockers people usually expect here.
* **IPC** for launcher↔server uses named pipes (`MediaPortal.Utilities\Process\IpcServer/IpcClient`) — portable.

### 3.3 Rendering and playback (the hard core)

```
WinForms TouchForm (Source\UI\SkinEngine\GUI\MainForm.cs, TouchForm.cs)
  └─ SharpDX Direct3D9 device      DirectX\GraphicsDevice.cs, MPDirect3D.cs, d3dSetup.cs
       ├─ Render pipelines          DirectX\RenderPipelines\ (SBS/TAB 3D, single/multi-pass)
       ├─ 35 HLSL .fx shaders       via MediaPortal.DirectX9 + EffectAssetCore
       ├─ FreeType font rasterizer  Fonts\FreeType.cs (35 DllImports)
       ├─ FreeImage texture decode  ContentManagement\AssetCore\TextureAssetCore.cs
       └─ XAML skin vocabulary      Skin\*, 1090 .xaml — MP2's own XAML dialect, not WPF runtime
Video: DirectShowLib.Standard + EVR + native EVRPresenter.dll (C++, x86+x64) + mpcSubs.dll subtitles
Audio: BASS (BASS.NET / BassCombined)
```
`SkinEngine` references `SharpDX` in 140 of its 491 files — but on measurement **only 77 files / 19,877 of 82,952 LOC (24%) actually touch Direct3D**; the rest use `SharpDX.Mathematics`, which is pure managed code and not a blocker. There is still no renderer interface boundary — D3D9 types leak into the `RenderOverride` of controls, brushes and asset cores, and the draw contract `RenderContext(Matrix, RectangleF, opacity)` → `ImageContext.StartRender(...)` → `PrimitiveBuffer.Render()` is a de-facto sprite-batch API that was never extracted. The distinct Direct3D 9 operation surface is only **~20 calls**. See [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md) for the detailed plan.

### 3.4 The single most important structural fact

The two most-depended-upon assemblies both set **`UseWindowsForms=true`**:

* `Source\Core\MediaPortal.Utilities\MediaPortal.Utilities.csproj` — `UseWindowsForms`, `System.Drawing.Common`, `System.Management`
* `Source\Core\MediaPortal.Common\MediaPortal.Common.csproj` — `UseWindowsForms`

Under .NET 6+, that forces TFM `net6.0-windows` on **everything transitively downstream**, which is exactly why all 133 projects carry `-windows`. The actual WinForms/GDI usage inside those two assemblies is tiny:

| Assembly | Files coupling to WinForms/GDI |
|---|---|
| `MediaPortal.Utilities` | `Screens\FormExtensions.cs`, `Screens\SplashScreen.cs` (+ `System.Drawing` in `ImageUtilities.cs`, `Properties\Resources.Designer.cs`) |
| `MediaPortal.Common` | `Async\ControlAwaiter.cs`, `Configuration\ConfigColoredItemList.cs`, `Exceptions\LauncherExceptionHandling.cs`, `FanArt\BaseFanArtHandler.cs`, `MediaManagement\Helpers\BaseInfo.cs`, `Services\Logging\CrashLoggerBase.cs`, `Services\Logging\log4netLogger.cs`, `Services\Logging\UiCrashLogger.cs`, `UI\FormDpiAwareness.cs` |

**≈11 files stand between the entire backend and `net8.0`.** That is the highest-leverage change in this whole program.

---

## 4. Blocker inventory

Severity: 🔴 blocks running · 🟠 needs replacement · 🟡 needs work · ⚪ cosmetic/dev-only

| # | Sev | Where | Evidence | Issue | Mitigation |
|---|---|---|---|---|---|
| B1 | 🔴 | `MediaPortal.Common`, `MediaPortal.Utilities` | `UseWindowsForms=true`; 11 files (see §3.4) | Core assemblies force Windows TFM | Extract into `*.Windows` satellites; retarget to `net8.0` |
| B2 | 🟠 | `Source\UI\SkinEngine` | `DirectX\GraphicsDevice.cs`, 77 D3D files (19,877 of 82,952 LOC), 35 `.fx` (1,250 LOC, all `vs_2_0`/`ps_2_0`) | Direct3D 9 via unmaintained SharpDX; no renderer abstraction | Smaller than it looks: extract `IGpuDevice`/`IVertexBatch`/`IShaderEffect`, keep Dx9 as backend #1, add Skia. See proposal doc |
| B3 | 🔴 | `Source\UI\Players` | `DirectShowWrapper`, `EVRPresenter\*.vcxproj`, `EVRPresenter.dll` x86/x64, `DirectShowLib.Standard`, `mpcSubs.dll` | DirectShow/EVR exist only on Windows | Player abstraction; libmpv (Win+Linux), Media3 (Android) |
| B4 | 🔴 | `Setup\*` | WiX 4, 25 `.wxs`, WPF bootstrapper, `Dokan.wxs`, `LAVFilters.wxs`, `VCRedists.wxs` | MSI/MSI-bundle install pipeline | deb/rpm + systemd, Flatpak/AppImage, APK/AAB |
| B5 | 🔴 | Android (every UI project) | 49 `UseWindowsForms`, 10 `UseWPF`, 56 x86/x64 pins | No Win32/WinForms/WPF/D3D9/DirectShow on Android | New Android client, not a port |
| B6 | 🟠 | `MediaPortal.Server` | `WindowsService.cs`, `WindowsServiceInstaller.cs`, `ServiceBase.Run` | Windows service host | `Microsoft.Extensions.Hosting` + systemd unit; keep Win service on Windows |
| B7 | 🟠 | P/Invoke surface | ~200 sites: `kernel32` 65, `user32` 51, `wlanapi` 17, `advapi32` 11, `ole32` 10, `Mpr` 6, `setupapi` 6, `iphlpapi` 2, `netapi32` 2, `dwmapi`, `powrprof`, `dwmapi`, `hid`, `quartz`, `urlmon`, `svrapi` | Hard Win32 coupling | HAL interfaces with Win/Linux implementations; delete Android-irrelevant ones |
| B8 | 🟠 | `Services\ResourceAccess\ImpersonationService`, `Utilities\Process\Impersonation*`, `Utilities\Security\Safe*Handle` | `LogonUser`, token/impersonation; already `#if NET5_0_OR_GREATER`-stubbed | Network-share access as another user via Windows tokens | On Linux use credentials-per-share (SMB libs), not OS impersonation |
| B9 | 🟠 | `Utilities\Network\SharesEnumerator`, `FileSystem\DriveUtils` | `PlatformID.Win32NT` guards, `WNet*`, drive letters | NetBIOS/SMB enumeration, drive-letter paths | SMBLibrary/SMB client; URI-based resource IDs (already partly done via `IResourceAccess`) |
| B10 | 🟠 | `Services\Dokan` | `DokanNativeMethods.cs` | Windows-only FUSE-equivalent used for virtual FS | Linux: real FUSE; else drop (virtual FS is optional) |
| B11 | 🟠 | `System.Drawing.Common` + GDI+ | `Utilities\ImageUtilities.cs`, `GDIThumbnailProvider`, `WICThumbnailProvider`, `ShellThumbnailProvider` | `System.Drawing.Common` is Windows-only since 6.0 | SkiaSharp / ImageSharp |
| B12 | 🟠 | DB providers | `Backend\Database\{SQLCEDatabase,FirebirdDatabase,ScimoreDb,MSSQLDatabase}` (legacy csproj, `Microsoft.SqlServerCe`, bundled `msvcr80.dll`) | Windows-only/embedded DBs | Keep SQLite, MySQL; add PostgreSQL (Npgsql); retire the rest |
| B13 | 🟠 | `Source\UI\TV` | `SlimTvService3` (`net48`, `COMRegistration.cs`), `MediaPortal.TvEngine.*` packages, `TvMosaic`, `MPExtended` | TV scanning/DVB is a separate Windows service with COM registration | Keep Windows TV service; add TvHeadend provider for Linux |
| B14 | 🟠 | `Source\UI\Input` | `MceRemoteReceiver\Hardware\Device.cs` (`setupapi`, `hid`), `IrInput\References\IrssComms.dll`, `HidInput`/`SharpLibHid`, `WifiConfiguration` (`wlanapi` ×17) | Win32 device APIs | Linux: lirc/eventdev/HID via `libusb`; drop WiFi config UI on non-Windows |
| B15 | 🟠 | `Microsoft.Win32.Registry` | `CodecHandler.cs` (DirectShow codec registration), `CrashLoggerBase.cs`, `WindowsAPI.cs`, `MceRemoteReceiver\Hardware\Remote.cs`, `Setup\CustomActions` | Real Windows registry use | Confine to Windows-only satellites; settings already DB-backed |
| B16 | 🟡 | `Utilities\SystemAPI\{NativeMethods,WindowsAPI}.cs` (54 DllImports), `TaskScheduler\TaskWaitableTimer.cs`, `Webradio…\Standby.cs` | `SetThreadExecutionState`, waitable timers, `powrprof` | Idle/suspend management | `IPlatformPower` + systemd-inhibit implementation |
| B17 | 🟡 | `System.Management` (WMI), `System.Diagnostics.EventLog`, `System.DirectoryServices` | `MediaPortal.Utilities`, `ServiceMonitor`, `LogCollector` | Windows management APIs | Replace with `Microsoft.Extensions.Diagnostics`/journal; AD via LDAP (Novell.Directory.Ldap) |
| B18 | 🟡 | `Incubator\MediaServer`, `Incubator\MP2Extended` | OWIN + `Microsoft.Owin.Host.HttpListener`, WebAPI, `SamsungProtocolHandler`, `XBoxProtocolHandler` | OWIN self-host is aging; `http.sys` assumptions | Migrate to Kestrel/ASP.NET Core (also fixes DLNA on Linux) |
| B19 | 🟡 | `MediaPortal.Utilities\FileSystem`, path literals | Drive-letter paths, `C:\` occurrences, backslash literals, `<APPLICATION_PATH>` tokens, case-sensitivity assumptions | Linux is case-sensitive, no drive letters | Path/URI audit + tests; centralize in `IPathManager` |
| B20 | 🟡 | 28 legacy-format `.csproj` | skins, DB providers, `HttpServer`, `MediaPortal.Manager`, most `Tools` | Cannot multi-target `net8.0`, no `dotnet build` | Migrate to SDK-style (mechanical but broad) |
| B21 | 🟡 | .NET 6 EOL | `net6.0-windows` everywhere; `NetCore6.0.16_*.wxs` | Unsupported runtime | Move to .NET 8 LTS (or 9 STS) as part of Phase 1 |
| B22 | 🟡 | `BinaryFormatter` | `ScriptableMetadataExtractor\…\DiskCachedDictionary.cs`, `OnlineLibraries\…\BinaryCacheProvider.cs` | Removed/unsafe in modern .NET | Replace with System.Text.Json |
| B23 | ⚪ | `Build\Build.proj` + 16 `.bat`, AppVeyor `Visual Studio 2022` image, Transifex `.transifexrc` seeding, `Tools\AssemblyInfoHelper.exe` run as a build step | Windows CI, batch scripts, MSBuild-on-Windows | Cannot build on Linux agents | `dotnet build` + NUKE/Cake or plain scripts; GitHub Actions matrix |
| B24 | ⚪ | `Tools\*` (SkinDesigner, StringManager, CodeCleanup, TransifexHelper, MergeMSI, UPnPTools, SkinCloneTool), `Resources\Templates\UIPluginTemplate`, `Graveyard\*` | legacy csproj, WinForms/WPF, VS-only | Dev tooling only | Exclude from port scope; keep Windows-only |

---

## 5. Dependency replacement matrix

| Package / native | Projects | Windows-only? | Replacement |
|---|---|---|---|
| `SharpDX`, `SharpDX.Direct3D9/2D1/Mathematics` | 14 | 🔴 yes, project unmaintained | D3D11 (Win) / Vulkan-SkiaSharp-SharpGL; or Silk.NET |
| `MediaPortal.DirectX9` (custom NuGet) | 1 | 🔔 unverified, HLSL effect runtime | Rewrite effect compiler against new renderer |
| `DirectShowLib.Standard` | 1 | 🔴 | libmpv/GStreamer |
| `EVRPresenter.dll` (C++), `BaseClasses.vcxproj` | 4 proj | 🔴 | n/a — remove on Linux/Android |
| `mpcSubs.dll`, `MediaPortal.MpcSubs`, `MediaPortal.DVBSub` | 2 | 🔴 | libass via mpv/GStreamer; Android: Media3 subtitles |
| `System.Drawing.Common` | 1 (+ GDI in core) | 🔴 since 6.0 | SkiaSharp or ImageSharp |
| `FreeImage-dotnet-core`, `MediaPortal.FreeImageNET`, native `FreeImage.dll` | 2 | 🟡 native per-OS | Keep (FreeImage ships Linux/Android) or move to SkiaSharp |
| `MediaPortal.FreeType` + `Fonts\FreeType.cs` | 1 | 🟢 portable | Keep — FreeType is cross-platform; needs build-time interop path per OS |
| `MediaPortal.TagLib`, `TagLib#` | 5 | 🟢 | Keep |
| `MediaInfo.Combined` / `MediaInfoLib` (33 DllImports) | 1 | 🟢 libmediainfo exists for Linux/Android | Keep; per-OS native loading |
| `BASS.NET`, `BassCombined`, `BassRegistration` | 7 | 🟢 BASS ships Linux/Android | Keep; per-OS native load + licensing check |
| `OpenCvSharp4.Windows` | 1 | 🔴 package is Win-specific | `OpenCvSharp4.runtime.linux-x64` / `.android` |
| `System.Data.SQLite.Core`, `.Linq` | 5 | 🟢 | Keep (drop `System.Data.SQLite.Linq` if EF6 goes) |
| `MySQL.Data`, `MySQL.Data.Entity` | 2 | 🟢 | Keep or `MySqlConnector` |
| `Microsoft.SqlServerCe`, `.Entity` | 2 | 🔴 | **Drop** |
| `FirebirdSql.Data.FirebirdClient` + `fbembed.dll`, `msvcr80/80` | 1 | 🔴 embedded | Drop, or client/server only |
| `Scimore.Data.*` | 1 | 🔴 (Windows service DB) | **Drop** |
| `Microsoft.Windows.Compatibility` | 1 | 🟡 | Remove once WMI/EventLog gone (it's a smell anyway) |
| `System.Management`, `System.DirectoryServices`, `System.Diagnostics.EventLog`, `System.ServiceProcess.ServiceController`, `System.Runtime.Caching`, `System.Security.Permissions` | 8 | 🟡 Windows-backed | `Microsoft.Extensions.*` equivalents; LDAP for AD |
| `Microsoft.Owin*` (7 packages), `Microsoft.AspNet.WebApi.Owin` | 11 | 🟡 runs, but `HttpListener`-backed; flaky on Linux | ASP.NET Core (Kestrel) |
| `Hardcodet.NotifyIcon.Wpf.NetCore` | 2 | 🔴 WPF tray | Per-OS tray or drop |
| `Prism.Wpf`, `Microsoft-WindowsAPICodePack-ShellExtensions`, `DiscUtils`, `Coderr.Client.WinForms`, `DotNetZip`, `SharpLibHid` (3) | various | 🔴/🟡 | Replace or scope to Windows projects |
| `WixToolset.*` (4) | 2 | 🔴 | Per-platform packaging |
| `NamedPipeServerStream.NetFrameworkVersion` | 1 | 🟢 (net6 path exists) | Keep, drop the shim for net8 |
| `Newtonsoft.Json`, `HtmlAgilityPack`, `YoutubeExplode`, `SubtitleDownloader`, `Ude.NetStandard`, `SharpZipLib`, `LibGit2Sharp`, `Arkane.Zeroconf`, `DuoVia.FuzzyStrings`, `MimeMapping`, `System.ServiceModel.Syndication`, `log4net`, `NUnit`/`xunit`/`Moq`/`NSubstitute`, `Antlr4.Runtime`, `CommandLineParser`, `System.Threading.Tasks.Dataflow`, `OpenWeatherMap`, `NEbml`, `MediaPortal.TsReader`, `mkvinfo`/`mkvextract`, `ffmpeg`, `MediaPortal.BDReader`, `MPExtended.*` interfaces, `OnlineVideos.Core` | — | 🟢 mostly | Keep; verify native parts (`ffmpeg`, `mkvextract`, `BDReader`) per-OS |

---

## 6. Target architecture (to-be)

```
                          ┌─────────────────────────────────────────┐
                          │        MediaPortal.Core  (net8.0)        │
                          │  Common · Utilities · Backend · UPnP ·   │
                          │  HttpServer · ConfigurationMgr · Plugins │
                          │  + Abstractions (interfaces only)        │
                          └───────────────┬─────────────────────────┘
                                          │ interfaces
             ┌────────────────────────────┼────────────────────────────┐
             ▼                            ▼                            ▼
   Core.Windows (net8.0-windows)  Core.Linux (net8.0-linux)   Core.Android (net8.0-android)
   WinSvc host, registry, WMI,    systemd host, FUSE, SMB,    Android service, SAF, JNI
   Dokan, impersonation, SMB-     posix fs, libsecret         (no fs tricks)
   Win, power, tray
             │                            │                            │
             ▼                            ▼                            ▼
   Windows client                Linux desktop client          Android client
   SkinEngine + D3D11 +          SkinEngine(backend=Vulkan      MAUI / .NET-for-
   DirectShow (as today,         or Skia) OR Avalonia/         Android + Media3,
   cleaned up)                   Skia re-skin                 talks UPnP/HTTP only
```

**Key architectural rules to introduce**

1. **`net8.0` (no `-windows`) for `MediaPortal.Core.*`.** Enforce with a CI check: no project under `Source\Core` or `Source\Backend` may set `UseWindowsForms`/`UseWPF` or reference a `*-windows` TFM.
2. **Platform Abstraction Layer (PAL).** New project `MediaPortal.Core.PlatformAbstractions` with, at minimum: `IPlatformHost` (lifecycle/service), `IPlatformPower`, `IPlatformIdentity`, `IPlatformNetworkShares`, `IPlatformFilesystemExtras`, `IPlatformInputDevices`, `IPlatformNotification`, `IPlatformCredentials`, `IPlatformPaths`. Bind through the existing `ServiceRegistration` — no new DI framework needed.
3. **Renderer abstraction.** `IRenderer` / `IRenderTarget` / `IEffectCompiler` / `IFontRasterizer` / `IImageDecoder` extracted from `SkinEngine\DirectX` and `ContentManagement\AssetCore`. SharpDX implementation becomes `SkinEngine.Dx9` (or upgraded `SkinEngine.Dx11`); add `SkinEngine.Skia` / `SkinEngine.Vulkan`. The control tree, layout, animation and XAML skin parsing must be provably renderer-agnostic — this is the refactor that makes or breaks the Linux client.
4. **Player abstraction.** Promote the existing `MediaPortal.UI.Presentation.Players` interfaces into the real boundary. Implementations: `Player.DirectShow` (Windows), `Player.Mpv` (Win+Linux via libmpv), `Player.Android` (Media3). Subtitles, audio-track selection, video-sync and refresh-rate handling move behind it.
5. **TV abstraction.** `ITVProvider` already exists in spirit (Native / MPExtended / TvMosaic). Add `TvHeadendProvider` and let Linux use it; leave SlimTV/DVB Windows-only.
6. **Retire, don't port.** `Graveyard` (delete), `Incubator` (triage; 97k LOC — port only `MediaServer` after Kestrel migration, drop the rest or move to a separate repo), legacy `Tools`, `MediaPortal.Manager`.

---

## 7. Strategy options

| Option | Description | Cost | Outcome | Recommendation |
|---|---|---|---|---|
| **A. Port everything** | SkinEngine, DirectShow, TV, input, installer all made cross-platform | 40–70 pm | Years of work, permanently Windows-shaped abstractions, dead SharpDX | ❌ reject |
| **B. Portable core + per-platform clients** *(recommended)* | Split core/PAL; ship Linux server + Windows client unchanged + new Linux-desktop renderer + new Android client | 20–32 pm | Real Linux server in ~4 months, incremental | ✅ adopt |
| **C. Backend-only, thin clients everywhere** | Port only the server; Windows keeps current client, Linux & Android get one new cross-platform UI (Avalonia or web) | 14–22 pm | Fastest to Linux+Android, but abandons existing skins | ✅ fallback if resources are tight |

**Recommendation: Option B.** If headcount is ≤2 engineers, take Option C and revisit the Linux desktop renderer later — a web/Avalonia client reuses the same server work, so nothing is wasted.

---

## 8. Roadmap

Effort assumes **3 experienced .NET engineers**, and is stated in person-months (pm). Each phase has explicit exit criteria; do not advance without them.

### Phase 0 — Baseline & guardrails (0.5–1 pm, 2–3 weeks)

* Fix the build on a clean checkout: check out the `OnlineVideos` submodule, document prerequisites, replace `AssemblyInfoHelper.exe` build-step with source generators or an MSBuild task.
* Get a **green, reproducible build** and record it. Currently unverified.
* Introduce CI (GitHub Actions): `windows-latest` + `ubuntu-latest` jobs that run `dotnet restore/build` per solution, so Windows-only breakage becomes visible immediately.
* Add an **architecture test** (NetArchTest / ArchUnitNet or a plain test) asserting "core projects must not reference `System.Windows.Forms`/`System.Drawing`/`Microsoft.Win32`/`SharpDX`/`DirectShowLib`" — it will fail; that failure list *is* the Phase 1 backlog.
* Triage `Incubator` (97k LOC) and `Graveyard`: port / quarantine / delete. Decide the fate of the 28 legacy `.csproj`.
* **Exit criteria:** CI green on both runners; arch-test failure list enumerated; scope frozen and signed off.

### Phase 1 — Portable core, retarget to .NET 8 (4–6 pm, 2.5–4 months)

* Migrate all 28 legacy `.csproj` to SDK-style.
* `net48;net6.0-windows` → **`net8.0`** for everything portable; **`net8.0-windows`** only where WinForms/WPF/SharpDX/DirectShow genuinely remain.
* **Break the WinForms spine (B1):** move the ~11 WinForms/GDI files out of `MediaPortal.Common`/`Utilities` into `MediaPortal.Common.Windows` / `MediaPortal.Utilities.Windows`; replace `System.Drawing` in `ImageUtilities` with SkiaSharp; replace the WinForms `SplashScreen` with an abstraction (`ISplashScreen`, no-op on headless).
* Introduce the PAL (§6.2) and move B6–B11, B16–B18 behind it. Windows implementations keep current behaviour byte-for-byte.
* Windows-service host → `IHostedService`; keep `ServiceBase` on Windows via `Microsoft.Extensions.Hosting.WindowsServices`; systemd on Linux.
* Replace `BinaryFormatter` (B22), `System.Management`/`EventLog` (B17), registry reads (B15).
* Drop SQL CE, Firebird-embedded, ScimoreDB; add **PostgreSQL/Npgsql**; make SQLite the default for Linux.
* Path/URI audit (B19): no drive letters, no `\` literals, case-correct file access; add Linux-path unit tests.
* Extend the existing test projects (NUnit/xUnit, 13.5k LOC) so they run on `ubuntu-latest`; add tests for `IResourceAccess`, `IPathManager`, settings/registry, media import.
* **Exit criteria:** `MediaPortal.Core.*` + `MediaPortal.Backend` compile and unit-test on Linux; arch test passes with the agreed exclusions list; zero `net48`/`net6.0` TFMs remaining; Windows behaviour unchanged (regression-tested).

### Phase 2 — Linux headless server (2–3 pm, 1.5–2.5 months) — **first shippable value**

* `MediaPortal.Server` runs as a **systemd service** (`net8.0-linux-x64`), self-contained single-file publish; optional ARM64 (very relevant for HTPC/NAS/SBC boxes).
* File-access providers: local FS ✅, URL ✅, ZIP/Iso ✅ (verify `DiscUtils`), network shares via **SMBLibrary** replacing `SharesEnumerator`/WNet (B9), **FUSE** replacing Dokan (B10) or omit.
* Metadata extractors: port `MetadataExtractors` (93k LOC) — retire `GDIThumbnailProvider`/`WICThumbnailProvider`/`ShellThumbnailProvider`/`OCVVideoThumbnailer` in favour of one SkiaSharp/OpenCV-cross-platform thumbnailer; confirm `MediaInfo`, `TagLib`, `ffmpeg`, `mkvinfo` load their Linux natives.
* UPnP/DLNA server + `HttpServer` verification on Linux sockets; migrate OWIN/`MP2Extended` to ASP.NET Core (B18) so the JSON/streaming API is portable.
* Package: **deb + rpm** (and ideally AppImage/Flatpak), `mediaportal2-server.service`, `logrotate`, `/etc/mediaportal2` data dir, FHS-compliant paths, upgrade path from Windows config export.
* Optional: TV via **TvHeadend** provider; transcoding service (`Test.TranscodingService` → ffmpeg).
* **Exit criteria:** on Ubuntu + Debian ARM64, a fresh install serves an existing library, imports media, streams over HTTP/DLNA, and is controllable by the existing Windows client and a phone remote. Documented install + upgrade.

### Phase 3 — Renderer abstraction & Linux desktop client (5–7 pm, 6–10 months)

> Superseded in detail by [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md), which quantifies the actual coupling. Effort revised down from 8–14 pm after measurement: SkinEngine is **76% engine-agnostic** and the Direct3D 9 surface is **~20 operations**.

* **3a (0.5 pm):** Replace `SharpDX.Mathematics` with `System.Numerics` in the 64 math-only SkinEngine files; create `MediaPortal.UI.Graphics.Abstractions` (`IGpuDevice`, `IRenderTarget`, `IVertexBatch`, `IShaderEffect`, `IFontEngine`, `IPresentationTarget`, `IVideoProvider`). Gate: Windows pixel-identical, SharpDX confined to 77 files.
* **3b (1.5–2.5 pm):** Move those 77 files into `Graphics.Direct3D9` behind the interfaces. **Do not write a new backend in this step** — using known-good Dx9 code is what proves the interface is complete. Add golden-image regression tests.
* **3c (2–3 pm):** `Graphics.Skia` backend, validated **on Windows first** under `--renderer skia` (GL *and* raster). Shader work is bounded: 35 HLSL files / 1,250 LOC, of which ~6 are free (Skia gradient/tile/blend equivalents), ~9 are trivial, 7 need real SkSL ports, and 6 get deleted. Text moves to Skia's FreeType/HarfBuzz stack, deleting ~2,000–2,500 LOC of hand-rolled glyph code (`FreeType.cs`, `TextBuffer.cs`, `font*.fx`). Windows/Skia parity is the gate that proves Linux readiness without Linux-specific bugs.
* **3d (1–1.5 pm):** Linux `IPresentationTarget` (X11 first, Wayland behind a flag, via SDL3) + input, DPI/multi-monitor, vsync; **two-plane compositing** (player-owned video under a transparent Skia UI) with **libmpv** — this deliberately avoids cross-API texture interop, following Kodi/Android-TV practice.
* Deferred on Linux: DVB/SlimTV, MCE IR remote, WiFi config, refresh-rate switching, tray, DVD/Blu-ray menus, EVR/3D stereo, `glass_tiles`-class effects.
* **Exit criteria:** skins at an agreed parity subset on Linux with video playback; golden images checked in for every backend; Windows client still green on both `d3d9` and `skia`.
* **Risk to prototype in week 1:** compositor alpha/HDR quality when the UI plane overlays video is the one thing that could invalidate the two-plane model (proposal §10 R1).

### Phase 4 — Android client (1–6 pm, 1–5 months) — *two viable strategies*

Because Phase 3 puts Skia underneath the existing engine and Skia is Android's own 2D/text stack anyway, **MP2's skin engine can run on Android**. That opens a second option that the pre-measurement assessment ruled out.

| Strategy | Scope | pm | Trade-off |
|---|---|---|---|
| **A1. SkinEngine on Android** *(now recommended if the 10-foot UI must look like MP2)* | `net8.0-android` `IPresentationTarget` (`SurfaceView`/`TextureView`), D-pad/touch input, `IVideoProvider` on **Media3/ExoPlayer**, lifecycle/surface restore. Reuses the whole engine + skins. | **1–2** (incremental after Phase 3) | Reuses everything, but inherits a desktop-shaped layout model; phone-sized layouts need skin work |
| A2. New thin client | `net8.0-android` + MAUI (or Kotlin) against the server's UPnP + HTTP/JSON API; Media3 playback; PiP, audio focus, leanback | 4–6 | Best touch/Android-TV ergonomics, but a second UI to maintain and no skin compatibility |

Common to both: playback via **AndroidX Media3/ExoPlayer**; server supplies browsing, search, watch-state and user profiles (no client-side library logic); package as **AAB** via `dotnet publish -f net8.0-android` with Play signing and emulator smoke tests; optional Android TV (`leanback`) and DIAL/Chromecast sender.
**Exit criteria:** Play Store-ready AAB — browse library, play from server, control playback, resume, per-user profiles.

### Phase 5 — Windows modernization & convergence (2–4 pm, ongoing)

* Windows client on `net8.0-windows`; SharpDX→D3D11 or Vulkan/ANGLE so one renderer backend serves Win+Linux; DirectShow→mpv on Windows too (retires EVRPresenter C++ and 32-bit constraints).
* Windows packaging: MSI→`winget`/MSIX; retire legacy `Tools`; move `MediaPortal.Manager` to a cross-platform UI or fold into the web UI.
* CI: matrix `win-x64 / linux-x64 / linux-arm64 / android-arm64`; artifact parity; nightly Linux+Windows smoke suite.

### Timeline at a glance

```
Phase 0  ██                                         weeks 1-3
Phase 1  ████████████                                months 1-4     → core on net8, Linux unit tests
Phase 2       ██████                                 months 4-6     → 🎯 Linux server ships
Phase 3            ████████████                      months 6-13    → Linux desktop client
Phase 4                   ██████                     months 9-14    → Android client (A1: 1-2 pm)
Phase 5                              ██████           months 15-20
```
**Cumulative: ~14–24 pm to Linux server + Android client + a credible Linux desktop; ~24–34 pm for broad parity.**

---

## 9. Packaging & CI plan

| Platform | Runtime | Package | Service/UI | CI |
|---|---|---|---|---|
| Windows | `net8.0-windows` | MSI/WiX (today) → MSIX + `winget` | Win service + desktop client | `windows-latest`, VS 2022 + MSBuild (unchanged) |
| Linux (server) | `net8.0-linux-x64`, `linux-arm64`, self-contained | **deb**, **rpm**, AppImage | systemd `mediaportal2-server.service` | `ubuntu-latest`, `dotnet publish` |
| Linux (desktop) | `net8.0-linux-x64` | deb + **Flatpak** (+ AppImage) | user session, X11/Wayland | `ubuntu-latest` + xvfb + Mesa software GL for render tests |
| Android | `net8.0-android` | **AAB** (Play) / APK | Android service + Media3 | `ubuntu-latest` + NDK + emulator |

CI specifics: replace `appveyor.yml` (VS 2022 image, `.bat` chain, `%USERPROFILE%\.transifexrc` seeding, 7z of `MP2-Setup.exe`) with a GitHub Actions matrix; keep Transifex sync as a scheduled job; generate `AssemblyInfo`/`build.targets` copy steps portably (MSBuild targets, not `.bat`); stop shipping `EVRPresenter.dll`/`x86` binaries outside Windows artifacts.

## 10. Testing & QA

* **Portability gate:** the arch test of Phase 0 runs in CI; any new WinForms/GDI/P/Invoke reference in a portable project fails the build.
* **Cross-platform unit tests:** make `Tests.Server`, `Tests.Client`, `MediaPortal.Mock`, `Test.Common` run on Linux; add a Linux file-system suite (case sensitivity, `/` separators, permissions, network-share URIs).
* **Visual regression:** golden-image snapshots of key skin screens, compared on Windows and (Phase 3) on the Skia backend.
* **Playback matrix:** container × codec × subtitle matrix run against mpv/GStreamer on Linux and Media3 on Android.
* **Upgrade test:** Windows-created database + config opens on Linux without loss (this is the most likely field failure).
* **Packaging smoke:** install/upgrade/uninstall in containers per distro; systemd `journalctl` health check.

## 11. Top risks

| Risk | Impact | Mitigation |
|---|---|---|
| ~~SkinEngine cannot be decoupled from D3D9 cheaply~~ | ~~Phase 3 blows up~~ | **Measured and retired:** only 77 files / 19,877 LOC touch Direct3D (24%), over a ~20-operation API. Step 3b is a Windows-only pure refactor with a pixel-identity gate before any second backend exists |
| Compositor alpha/HDR quality in two-plane UI-over-video compositing | Invalidates the video architecture, not the renderer | Prototype on all three targets in week 1 of step 3d; keep same-device `WrapNativeTexture` interop as fallback |
| HLSL `.fx` effects have no Skia equivalent | Visual regressions in skins | Inventory all 35 shaders in Phase 0; agree a reduced skin feature set for Linux |
| SharpDX unmaintained; `MediaPortal.DirectX9` custom NuGet of unknown provenance | Blocks even Windows modernization | Prefer moving to D3D11/Vulkan (SharpGL/Silk.NET) rather than fixing SharpDX |
| Project is GPL-2.0+ | Any derivative must be GPL | Confirmed for internal fork; check before adding proprietary plugins |
| `net48` still shipped to users | Two maintenance tracks | Set an explicit end-of-support date in Phase 1; keep one Windows `net48` release line only if legally required |
| `OnlineVideos` submodule not vendored / not assessed | Unknown work item | Check it out in Phase 0 and re-estimate (it pulls `YoutubeExplode`, `OnlineVideos.Core`) |
| TV/DVB expectations on Linux | User dissatisfaction | Communicate early: Linux = library + streaming; live TV = TvHeadend, not SlimTV |
| Team size / single-maintainer knowledge on SkinEngine | Schedule slip | Written renderer-interface spec + pair rotation before Phase 3 |

## 12. Effort summary

| Phase | Scope | pm (3 engineers) | Calendar |
|---|---|---|---|
| 0 | Baseline, CI, arch tests, triage | 0.5–1 | 2–3 wks |
| 1 | Portable core, .NET 8, PAL | 4–6 | 2.5–4 mo |
| 2 | **Linux headless server** | 2–3 | 1.5–2.5 mo |
| 3 | Renderer abstraction + Linux desktop | 5–7 | 6–10 mo |
| 4 | Android client (A1 reuses Phase 3) | 1–6 | 1–5 mo |
| 5 | Windows modernization/convergence | 2–4 | ongoing |
| | **Total through Phase 4** | **13.5–25 pm** | **~14 mo** |

## 13. Do these five things first

1. **Check out the `OnlineVideos` submodule and get one clean full build**; commit a `BUILDING.md`. Without a known-good baseline nothing else is measurable.
2. **Delete the WinForms/`System.Drawing` dependency from `MediaPortal.Common` and `MediaPortal.Utilities`** (the ~11 files in §3.4) and retarget those two assemblies to `net8.0`. Cheapest, highest-leverage change in the whole program.
3. **Add the architecture test + Linux CI job** so portability regressions are impossible to introduce silently.
4. **Split the SkinEngine renderer behind an interface** per [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md), starting with step 1 (the mechanical `SharpDX.Mathematics` → `System.Numerics` swap across 64 files). The shader inventory that used to be the big unknown is already done: 35 files, 1,250 LOC, and only 7 need genuine SkSL ports.
5. **Publish a scope decision**: Linux server yes / Linux desktop later / Android as new client / TV via TvHeadend / no `net48` beyond date X. Most of the cost in this program comes from ambiguity, not from code.

---

## Appendix A — Evidence index

| Claim | Where |
|---|---|
| TFM `net48;net6.0-windows` in 133 projects | all `.csproj`, e.g. `MediaPortal-2\MediaPortal\Source\Core\MediaPortal.Utilities\MediaPortal.Utilities.csproj` |
| Core WinForms coupling | `Source\Core\MediaPortal.Common\MediaPortal.Common.csproj`, `Source\Core\MediaPortal.Utilities\MediaPortal.Utilities.csproj` |
| D3D9 renderer | `Source\UI\SkinEngine\DirectX\{GraphicsDevice.cs,MPDirect3D.cs,D3DConfiguration.cs,d3dSetup.cs,RenderPipelines,RenderStrategy}` |
| Win32 window host for the renderer | `Source\UI\SkinEngine\GUI\{MainForm.cs,TouchForm.cs}` |
| HLSL shaders | 35 `.fx` files under `Source\UI\Skins\*` and `SkinEngine` |
| DirectShow/EVR | `Source\UI\Players\{DirectShowWrapper,VideoPlayer,EVRPresenter,BDHandler,RefreshRateChanger}` |
| P/Invoke concentration | `Source\Core\MediaPortal.Utilities\SystemAPI\{NativeMethods.cs,WindowsAPI.cs}` (54), `Source\UI\SkinEngine\Fonts\FreeType.cs` (35), `Source\Extensions\MetadataExtractors\MediaInfoLib\MediaInfoDLL.cs` (33), `Source\UI\VideoPlayer\Subtitles\MpcEngine.cs` (21), `Source\UI\UiComponents\WifiConfiguration\Models\NativeWifiWrapper\Interop.cs` (17) |
| Windows service host | `Source\Main\MediaPortal.Server\{ApplicationLauncher.cs,WindowsService.cs,WindowsServiceInstaller.cs}` |
| Console mode (useful for Linux) | `Source\Main\MediaPortal.Server\ApplicationLauncher.cs` (`--console` → `RunAsConsole()`) |
| UPnP client↔server transport | `Source\Core\MediaPortal.UI\Services\ServerCommunication\ServerConnectionManager.cs`, `Source\Core\UPnP\Infrastructure\**`, `Source\Core\MediaPortal.Common\Services\ServerCommunication\*` |
| Custom raw-socket HTTP listener | `Source\Core\HttpServer\{HttpListener2.cs,HttpListenerBase.cs,HttpServer.cs}` |
| DB providers | `Source\Backend\Database\{SQLCEDatabase,FirebirdDatabase,ScimoreDb,MSSQLDatabase,MySQLDatabase,SQLiteDatabase}` |
| Dokan virtual FS | `Source\Core\MediaPortal.Common\Services\Dokan\Native\DokanNativeMethods.cs` |
| Impersonation (already net5-guarded) | `Source\Core\MediaPortal.Common\Services\ResourceAccess\ImpersonationService\AsyncImpersonationProcess.cs`, `Source\Core\MediaPortal.Utilities\Process\Impersonation*.cs` |
| Share/drive enumeration | `Source\Core\MediaPortal.Utilities\Network\SharesEnumerator.cs`, `FileSystem\DriveUtils.cs` |
| Windows registry usage | `Source\UI\Players\VideoPlayer\Tools\CodecHandler.cs`, `Source\UI\Input\MceRemoteReceiver\Hardware\Remote.cs`, `Setup\CustomActions\CustomActions.cs` |
| TV stack | `Source\UI\TV\{SlimTvService3,SlimTvNativeProvider,SlimTVMPExtendedProvider,TvMosaic,SlimTvService,SlimTvService35}` |
| Installer | `Setup\{MP2-Setup,Bootstrapper,MP2BootstrapperApp,CustomActions}`, 25 `.wxs` |
| Build pipeline | `MediaPortal\Build\Build.proj`, `Build.tasks`, 16 `.bat`, `Source\.nuget`, per-project `build.targets` (128), `appveyor.yml` |
| `BinaryFormatter` | `Source\Extensions\MetadataExtractors\ScriptableMetadataExtractor\Data\Collections\DiskCachedDictionary.cs`, `...\OnlineLibraries\Libraries\TvdbLib\Cache\BinaryCacheProvider.cs` |
| Framework `#if` shims (only 104) | e.g. `Source\Core\MediaPortal.Utilities\Process\ImpersonationHelper.cs` |
| Test projects | `MediaPortal\Tests\{MediaPortal.Mock,Test.Common,Tests.Client,Tests.Server,Tests.MediaServer,Test.MediaServer,Tests.TVMosaic,Test.TranscodingService}` |

## Appendix B — Assumptions and open questions

* **Unverified:** no compilation, test run, or runtime execution was performed; all figures are static-analysis counts. `MediaPortal.DirectX9`, `MediaPortal.MpcSubs`, `MediaPortal.BDReader`, `MediaPortal.TvEngine.*`, `BassRegistration`, `svrapi.dll` and `MediaPortal.UrlSourceSplitter` are internal NuGet packages whose source is not in this tree — they may add Windows-only native obligations.
* **Open:** fate of `Incubator` (97k LOC) and `Tools`; whether `net48` users must keep receiving releases; whether existing skins must render identically on Linux (this single requirement drives most of Phase 3's cost); Android form factor (phone/tablet/Android TV); whether an embedded-DSP audio path (BASS ASIO/WASAPI) is a hard requirement on Linux (it is Windows-only — Linux would use ALSA/Passthrough via mpv).
* **Assumption:** Linux desktop, if it happens, keeps the MP2 XAML skin vocabulary rather than adopting a new UI framework. If the team is willing to switch to Avalonia/web UI (Option C), Phase 3 shrinks to roughly 3–5 pm.

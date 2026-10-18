# Multiplatform roadmap

The plan of record for turning MP2 into a Windows/Linux/Android media centre. It is deliberately
short on vision and long on numbers, because every claim below is reproducible with a command in
`scripts/ci/` and every target has a number attached to it. Background documents that explain the
reasoning live next to this file:

- [Multiplatform-Assessment-and-Plan.md](./Multiplatform-Assessment-and-Plan.md) - what the tree looked like before anything was touched.
- [SkinEngine-Multiplatform-Proposal.md](./SkinEngine-Multiplatform-Proposal.md) - why the old skin engine is being replaced rather than ported.
- [New-Media-Center-Feasibility-and-Blueprint.md](./New-Media-Center-Feasibility-and-Blueprint.md) - the "write a new front end against the ported back end" option, and its risks.
- [P2-New-Frontend-on-Ported-Backend-Plan.md](./P2-New-Frontend-on-Ported-Backend-Plan.md) - the frontend plan that Phase 2 below now implements.

Decisions that are settled and should not be reopened:

| Question | Decision |
| --- | --- |
| Target frameworks | .NET 10 only. No .NET Framework, no dual TFM, no `net48` compatibility shim. |
| Architecture | x64 only. No x86 (the remaining 32-bit pins are listed in Phase 1e). |
| Frontend | Avalonia + CommunityToolkit.Mvvm. The MP2 skin engine is replaced, not emulated; skin compatibility is accepted breakage. |
| Player | LibVLC or MPV via .NET bindings, video surface embedded in an Avalonia control. DirectShow is retired. |
| Deleted, not ported | SlimTv service layer, TvMosaic, and (still pending) ScimoreDB and SQL CE. |

## Where the tree stands

Verified 2026-10-18 with `build.ps1 -Solution Server,Client -Ratchet`, `tests.ps1`, `packages.ps1`,
`test-canary-linux.ps1` and `test-server-linux.ps1`:

| Measurement | Value | How to reproduce |
| --- | --- | --- |
| Server solution compiles on Windows | 0 errors, 686 gated findings | `build.ps1 -Solution Server -Ratchet` |
| Client solution compiles on Windows | 0 errors, 2551 findings (not gated) | `build.ps1 -Solution Client` |
| Server solution compiles **on Linux** | 0 errors | `test-server-linux.ps1` |
| Portable-core Windows API references | **0** (83 quarantined in 6 satellite files) | `spine.ps1`, `satellites.ps1` |
| `CA1416` call sites, Windows vs Linux | 196 on both - identical inventories | build log, `warnings.ps1` |
| Unit tests, Windows | 4/4 suites pass | `tests.ps1` |
| Unit tests, Linux (canary) | 20/20 | `test-canary-linux.ps1` |
| Packages with known advisories | **4** (1 moderate, 3 high) | `packages.ps1` |
| Packages via .NET Framework fallback | 7 | `packages.ps1` |
| Clean machine can restore | **no** - 6 package ids exist nowhere reachable | CI README, "The dependency closure is not reproducible" |

The last row gates everything else: nothing builds reproducibly on a clean agent or a container until
those six ids have a home. That is why it is Phase 1a and not a footnote.

## Phase 1 - backend that runs everywhere

**a. Give the six unreachable packages a home.** `BASSCombined`, `BassRegistration`, `BASS.NET`
2.4.14.1, `MediaPortal.TagLib` 2.3.1, `ffmpeg` 3.4.2 and `OnlineVideos.Core` 2.1.0 resolve only from
`https://www.myget.org/F/mediaportal/`, which answers HTTP 500 on package lookup. Verified with an
empty `-p:RestorePackagesPath`: `NU1101` for the ids nuget.org does not carry at all, `NU1102` for the
two it carries only in older versions. Order of attack: rebuild the two small ones that are MP2's own
(`MediaPortal.TagLib`, `OnlineVideos.Core`) and push them to a feed we control or a local folder;
vendor `ffmpeg` and the BASS binaries as a checked-in feed after their licence terms are read. Gate: a
restore with an empty packages folder and `-p:RestoreSources` limited to nuget.org plus that folder.

**b. Kill the advisories.** Four packages, one of which is in almost everything:

| Severity | Package | Version | Projects | Advisory |
| --- | --- | --- | --- | --- |
| Moderate | `log4net` | 2.0.14 | 104 | GHSA-4f7c-pmjv-c25w |
| High | `System.Data.SqlClient` | 4.8.5 | 1 | GHSA-98g6-xh36-x2p7 |
| High | `Microsoft.Windows.Compatibility` | 7.0.1 | 1 | GHSA-555c-2p6r-68mm |
| High | `DotNetZip` | 1.15.0 | 1 | GHSA-xhg6-9j5j-w4vf |

The 104 projects behind `log4net` are one version bump in one place; the per-project log count made a
one-line fix look like 104 problems, which is exactly why `packages.ps1` counts packages instead.
`System.Data.SqlClient` is Windows-flavoured by design, so replacing it with `Microsoft.Data.SqlClient`
or the provider the database actually uses is a porting task wearing a security badge.
`Microsoft.Windows.Compatibility` is itself a compatibility shim - its presence usually means
something else was ported badly, and that something is the real target. `DotNetZip` is unmaintained;
`System.IO.Compression` covers most of what MP2 asks of it. Gate: `packages.ps1 -FailOn High` in the
pipeline, then the same with `-FailOn Moderate`.

Seven more packages still run through the .NET Framework compatibility path (`BASS.NET`,
`BassRegistration`, `MediaPortal.TagLib`, `SharpLibHid`, `DiscUtils`, `OnlineVideos.Core`,
`WorldWeatherOnlineHelper`). `SharpLibHid` is Windows input by nature and belongs in a satellite or in
the bin; `DiscUtils` has .NET-targeted builds.

**c. Replace the remaining Win32, registry and path assumptions.** The inventory is `CA1416`: **196
findings**, and the Windows and Linux inventories are identical for 35 of 38 codes, so this is one
list rather than two. 17 of the 196 are WinForms members in `MP2-Server`'s own `MainForm.cs` and
`ApplicationLauncher.cs` - a Windows tray host that should become a satellite instead of being
"fixed". Registry use, `Environment.SpecialFolder` assumptions and GDI+ are invisible to the analyzer;
`spine.ps1` counts the ones inside the portable core and that count is already 0, so new work happens
at the platform-service seam, not inside the core. Gate: the `CA1416` floor goes down, `spine.ps1`
stays at 0.

**d. Database that is not Windows-shaped.** Decide Firebird's future, verify SQLite as the primary
store on Linux (native binaries per RID, no `System.Data.SQLite` interop surprises), delete ScimoreDB
and SQL CE. Gate: `test-server-linux.ps1` grows from "compiles" to "runs" - the server creates a
database in a container, imports a fixture library and answers a query.

**e. x64 only.** Remove `Prefer32Bit`, the `x86` entries in `Platforms`, `Platform=x86` in the
OnlineLibraries projects, and the bootstrapper's `win-x86` RID. Gate: no `x86` in any project file,
both solutions still build.

**f. The API the new frontend consumes.** The client reaches the backend through in-process service
contracts. Phase 2 needs the same contracts over a real endpoint. Gate: an end-to-end call from a
separate process to a server on Linux returning a real library item.

## Phase 2 - Avalonia front end

Not started. Sequence that keeps risk visible:

1. Headless spike: an Avalonia window implementing the ten-foot navigation model (D-pad focus, no
   mouse) that runs on Windows and in a Linux container on the software renderer. No MP2 code yet -
   this proves the focus model and the CI story first.
2. Bind the existing platform-neutral ViewModels without change. Anything importing
   `MediaPortal.GUI.Library` is a View, not a ViewModel, and gets rewritten.
3. Configure the renderer per OS: DirectX on Windows, Metal on macOS/iOS, Vulkan/Skia on
   Linux/Android - each proven by a probe like `Dockerfile.drawing`, none assumed.
4. Skin engine replacement. The skin XML model gets no Avalonia equivalent; the migration story for
   users is that the new UI ships its own themes.

Gate: the client stops being WinForms, the satellite list in `spine.ps1` shrinks, and a screenshot
from a Linux container is in the commit that closes the phase.

## Phase 3 - player engine

DirectShow and its filters are Windows-only, not buildable here (no C++ toolchain) and unmaintained.
LibVLC or MPV via .NET bindings, video surface embedded in an Avalonia control, hardware decode through
the player (DXVA2 / VAAPI / VideoToolbox) rather than through MP2. `EVRPresenter`, EVR and DirectVobSub
die here. Gate: the same file plays on Windows and on Linux through the same code path, and the deleted
projects show up in `spine.ps1` / `packages.ps1`.

## What cannot be verified without the user

Stated once so it does not have to be repeated per phase: audio passthrough and HDR on real hardware,
DVB/TvHeadend against real multiplexes, the feel of remote navigation, visual taste, and a 40 TB library
import. These need hardware and judgement that are not in this environment, and they are batched into
one hand-off checklist per phase rather than dribbled out.

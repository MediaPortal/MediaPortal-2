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
| Server solution compiles on Windows | 0 errors, 627 gated findings (was 685) | `build.ps1 -Solution Server -Ratchet` |
| Client solution compiles on Windows | 0 errors, ~2430 findings (not gated) | `build.ps1 -Solution Client` |
| Server solution compiles **on Linux** | 0 errors | `test-server-linux.ps1` |
| Core assemblies on a portable target framework | **2 of 90** - `MediaPortal.Utilities` and `UPnP` are `net10.0` with `CA1416` as error | `MediaPortal.Utilities.csproj`, `UPnP.csproj` |
| Portable-core WinForms / GDI+ references | **0** (86 quarantined in 6 satellite files) | `spine.ps1`, `satellites.ps1` |
| Portable-core P/Invoke declarations | **76** in 10 files (was 95; 18 left with Dokan and impersonation), invisible to `CA1416` | `spine.ps1` (P/Invoke column) |
| `CA1416` call sites, Windows vs Linux | 140 on both hosts, identical inventories (was 196) - and the whole surface is 627 findings on both | build log, `warnings.ps1` on both logs |
| Unit tests, Windows | 4/4 suites, 271 executed, 0 failed (3 canary tests skip: Linux-only) | `tests.ps1` |
| Unit tests, Linux (canary) | 31 executed, 0 failed (1 skips: Windows-only), incl. live IPC round trips and XDG autostart | `test-canary-linux.ps1` |
| Built executables are 64-bit | 5/5 apphosts measure `x64`, managed side `MSIL` | `architecture.ps1` |
| 32-bit declarations in source | 0 in SDK-style projects, 42 in old-style ones | `architecture.ps1` |
| Packages with known advisories | **0** (was 5: 1 critical, 3 high, 1 moderate) | `packages.ps1 -FailOn Low` |
| Packages via .NET Framework fallback | 7 | `packages.ps1` |
| Clean machine can restore | **no** - 6 package ids exist nowhere reachable | CI README, "The dependency closure is not reproducible" |

The two unit-test rows say "executed" and name the skips because of a reporting property found while
adding platform-guarded tests: **a test that `Assume`-skips is absent from all four numbers on the
summary line**, not counted as skipped. A green suite whose total quietly shrank by three is therefore
not evidence that the same tests ran, so the counts are written next to the reason they differ.

The client's finding total is written with a tilde on purpose: it includes the restore-time NuGet codes
that are deliberately outside the ratchet, so it moves by a few between runs of identical source. The
gated server number is exact because the codes it counts are functions of the source.

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

**b. Kill the advisories - done 2026-10-18.** What was there, and what closed it:

| Severity | Package | Version | Projects | Advisory | Closed by |
| --- | --- | --- | --- | --- | --- |
| Critical | `System.Drawing.Common` | 5.0.0 | 1 | GHSA-rxg9-xrhp-64gj | transitive, went with `DotNetZip` |
| Moderate | `log4net` | 2.0.14 | 104 | GHSA-4f7c-pmjv-c25w | bumped to 3.5.0 |
| High | `Microsoft.Windows.Compatibility` | 7.0.1 | 1 | GHSA-555c-2p6r-68mm | reference deleted |
| High | `System.Data.SqlClient` | 4.8.5 | 1 | GHSA-98g6-xh36-x2p7 | transitive, went with it |
| High | `DotNetZip` | 1.15.0 | 1 | GHSA-xhg6-9j5j-w4vf | reference deleted |

`packages.ps1 -FailOn Low` now exits 0 across 115 projects, which is the gate for this item. Three
notes that outlive the fix. `log4net`'s advisory is patched only from 3.3.0, so the bump had to be a
major-version jump - possible because the whole of MP2's log4net surface is one file, and now guarded by
tests that configure the shipped server config file, because an appender that stops resolving by name is
dropped silently and the product just stops writing logs. `DotNetZip`'s traversal has no patched release
of that package id at all, so `LogCollector` writes its archive with `System.IO.Compression` (verified
against a real log tree, same entry layout). And `Microsoft.Windows.Compatibility` turned out to be
exactly what it looks like: `Setup\Tests` imported none of the namespaces it supplies, nothing in the
tree needed `System.Data.SqlClient` - MP2's database access is SQLite - so both highs were two deleted
lines rather than a migration.

The `log4net` work is also what surfaced the tree's sharpest test trap, written up in the CI README
under "What decides the log level": the logger constructor applies the level from the settings service
over the configuration file, and `ServiceRegistration` is a process-wide singleton, so a test's result
depends on which fixtures ran before it.

Seven more packages still run through the .NET Framework compatibility path (`BASS.NET`,
`BassRegistration`, `MediaPortal.TagLib`, `SharpLibHid`, `DiscUtils`, `OnlineVideos.Core`,
`WorldWeatherOnlineHelper`). `SharpLibHid` is Windows input by nature and belongs in a satellite or in
the bin; `DiscUtils` has .NET-targeted builds.

**c. Replace the remaining Win32, registry and path assumptions.** The inventory is `CA1416`: **196
findings**, and the Windows and Linux inventories are identical for 35 of 38 codes, so this is one
list rather than two. 17 of the 196 are WinForms members in `MP2-Server`'s own `MainForm.cs` and
`ApplicationLauncher.cs` - a Windows tray host that should become a satellite instead of being
"fixed".

The analyzer is not the whole inventory, and one number proves it: with the portable core reading
**0** spine references, it contains **94 `[DllImport]` declarations in 13 files**. `CA1416` does not
report P/Invoke at all and P/Invoke is neither WinForms nor GDI+, so `spine.ps1` now counts them in a
third column, gated by the same "core may not grow, and a satellite may only grow by what the core
shrinks" rule.

**`MediaPortal.Utilities` lost its `-windows` target - done 2026-10-18.** The work was measured rather
than estimated first: with the TFM and `UseWindowsForms` changed by hand the project compiled with 0
errors and showed 35 `CA1416` call sites in six files (named-pipe ACL 8, mutex ACL 8, autostart registry
9, `HKCR` 3, WMI 3, `ApartmentState` 1). Each was then closed in a way appropriate to what it actually
was, and the project now carries `<TargetFramework>net10.0</TargetFramework>`, `UseWindowsForms=false`
and `<WarningsAsErrors>CA1416</WarningsAsErrors>`, so it cannot regress:

| Site | What it really was | What was done |
| --- | --- | --- |
| `ThreadingUtils.RunSTAThreaded` | `ApartmentState.STA` means nothing off Windows | guarded; the call degrades to a plain thread |
| `SystemAPI\MimeTypeDetector` | `HKCR` MIME lookup | returns `null` off Windows; the 979-entry extension map is the real implementation |
| `Process\SingleInstanceHelper` | a portable named mutex with a Windows ACL and a `HWND_BROADCAST` post | mutex kept, ACL and broadcast guarded; semantics measured in the container |
| `Process\IpcServer.OpenNewPipe` | pipe ACL **and** message transmission mode | Windows branch unchanged; elsewhere byte mode without an ACL, because Unix pipes are byte streams |
| `SystemAPI\WindowsAPI` autostart, 3 methods | not "registry access" but autostart | moved into a new `AutostartRegistry` with a real Linux answer (XDG desktop entry); `IsSupported` reports false where no mechanism exists |
| `FileSystem\DriveUtils.GetDiskSize`, `GetFreeDiskSpace` | dead - no callers anywhere in the tree | deleted, together with the `System.Management` and `System.Drawing.Common` package references they were the only users of |

`CA1416` across the Server solution went **196 → 161**, and `spine.ps1`'s P/Invoke ledger went 95 → 94
(the deleted `GetDiskFreeSpaceEx`). Committing the new floors with `-UpdateBaseline` is part of the
work, not an afterthought: a ratchet that is not re-armed keeps the old ceiling as permission.

Two of those files turned out to hide something the analyzer cannot see in either direction, and both
are now covered by a test that runs on Linux rather than by a comment:

- `PipeTransmissionMode.Message` is flagged as Windows-only, and the honest answer is not "guard it" -
  Unix pipes are byte streams, so message mode does not exist there. The reason dropping it is safe
  was read out of the code, not assumed: the server loop and `IpcClient.SendReceive` both re-read until
  the 6-byte header and the announced payload are complete, so the protocol never depended on message
  boundaries. Four canary tests now run a real `IpcServer` and `IpcClient` against each other over a
  real pipe, which pins that on both platforms instead of trusting the reading.
- `IpcClient.GetRunningApps()` enumerated `Directory.GetFiles(@"\\.\pipe\")`. That is a string literal:
  it is not a P/Invoke, not WinForms, and not a `CA1416` candidate, so it would have crossed the
  portable-core line unnoticed and thrown `DirectoryNotFoundException` on the first call. Off Windows
  .NET materialises a named pipe as a Unix domain socket called `CoreFxPipe_<name>` in the temp
  directory; whether that convention is real was settled by a test that opens a server and asserts the
  listing contains it and, after `Close()`, no longer does - green on Windows and in the container.

This is the third class of Windows dependency that the metrics cannot express, after the P/Invoke
declarations and the stale build output: **a Windows path inside a string**. A guard that is never
executed is not a port, which is why the canary now links the IPC sources and executes both halves of
each branch on the machine it applies to.

The autostart methods were the fourth case of the same shape, and the one with the most useful
finding: three `WindowsAPI` methods looked like "13 registry call sites blocking the port", which is
what an inventory made of `CA1416` lines says. Read as a feature - and with its callers, which turned
out to be three UI/launcher components that all pass `user: true` - it is autostart, which has a
straightforward Linux answer in an XDG autostart desktop entry, plus a per-scope split that maps onto
the `user` parameter already in the signature. `AutostartRegistry` now holds that translation and
`IsSupported` says `false` for platforms where no mechanism exists (Android has none; macOS would need
`~/Library/LaunchAgents`, a format nothing here can verify, so it is reported rather than guessed). The
desktop-entry half is tested through the redirection the specification itself provides
(`XDG_CONFIG_HOME`, `XDG_CONFIG_DIRS`), which means no test-only hook in the production code.

What the retarget deliberately does **not** claim: that every type in `MediaPortal.Utilities` works on
Linux. The assembly still declares 73 kernel32 and advapi32 `[DllImport]`s - `DriveUtils` ejecting an
optical disc, `WindowsAPI`'s window and handle helpers, `NetworkResourcesEnumerator`'s shares APIs - and
those throw `DllNotFoundException` when called off Windows, invisibly to every analyzer. They are
satellite candidates, listed below, and the honest statement about the retarget is the checkable one
that now lives in the project file: this assembly compiles for every platform, and it gains no
`CA1416`-visible Windows-only call site without failing the build.

Next in the same sequence is UPnP, then `MediaPortal.Common`, then `MediaPortal.Backend` - and the order is
set by the reference graph, not by which project looks cheapest. Common references UPnP and Backend
references Common, so a portable project can never sit below a `-windows` one: Backend has no P/Invoke
declaration at all and would be trivially portable, but it cannot move until Common does. An earlier
version of this paragraph ranked the projects by their spine counts and had it backwards.

**UPnP crossed the line without any code changing.** The project is now `net10.0` with
`<WarningsAsErrors>CA1416</WarningsAsErrors>`, and the measurement behind it is that the build produced
zero errors and zero findings on the portable target: no `[DllImport]`, no WinForms, no Windows-only
call site. What the flip did expose was a hole in the ledger rather than in the code. `spine.ps1` counts
a fixed list of "core" directories, and UPnP had never been on it - while `MediaPortal.Common` references
UPnP, so anything Windows-shaped added there would have entered the portable core uncounted. Adding the
directory found nothing (it adds 0 to every column, which is why nobody noticed), and the script now
carries the rule: a project belongs in the ledger the moment a core project references it.

The same probe measured the next project before reverting it, so the size of the following step is known
rather than guessed. `MediaPortal.Common` builds at `net10.0` with **0 errors other than `CA1416`, which
reports 39 distinct lines** (43 if a line referencing two Windows-only members is counted twice): the
Dokan file-system proxy (14, a Windows-only user-mode driver client and a satellite candidate), crash-report
WMI queries (13), Windows identity impersonation (6), power/session broadcasts (5) and one
`Registry.GetValue` in `ApplicationCore`. The two `System.Windows.Forms` strings a plain search finds in
that project are both comments recording earlier removals, which is the difference between grep and a
ledger that strips comments.

**`MediaPortal.Common` is the project now being emptied, one Windows subsystem at a time.** The first
subsystem out was **Dokan** (17 files, the .NET binding of a Windows user-mode file system driver), and it
left without a single line of its code changing: `MediaPortal.Common` keeps the interface
`IResourceMountingService`, registers a portable implementation that answers "this platform cannot mount
resources into the local file system", and the three Windows hosts install the Dokan-backed one during
startup next to `GdiPlusImageTransformer.Register()`. `CA1416` went **161 -> 147** and the core's P/Invoke
column **94 -> 86**, with the satellites gaining exactly the same 14 and 8 - `spine.ps1` prints that
exchange as "relocated, not fixed", and `satellites.ps1` independently reports the
`MediaPortal.Common.Services.Dokan` namespaces as satellite-owned. That the *whole* solution-wide delta
equals the moved files' own count is the evidence that nothing Windows-shaped stayed behind in the core.
**Windows identity impersonation followed it out** - five files (`WindowsImpersonationService`,
`WindowsIdentityWrapper`, `TokenWrapper`, `LogonHelper`, `AsyncImpersonationProcess`) into the satellite, with
`CurrentUserImpersonationService` registered by the core. That one is the cleanest case for the seam pattern:
the Windows class already invokes the delegate directly whenever no credential matches a path, so the portable
class is the existing no-credential path generalised, and every caller keeps its current code path. `TryRegisterCredential`
refuses rather than pretending, because returning `true` would promise access as another user. `CA1416` went
**147 -> 140** and the core P/Invoke column **86 -> 76**, combined total unchanged.

What remains in `MediaPortal.Common` from the measured inventory: crash-report WMI queries (13 findings, a
translation rather than a satellite - a crash report should say what it can on every platform), power/session
broadcasts (5, a platform-event seam) and one `Registry.GetValue` that logs the Windows product name (a guard
with `RuntimeInformation` as the portable answer).

**d. Database that is not Windows-shaped.** Decide Firebird's future, verify SQLite as the primary
store on Linux (native binaries per RID, no `System.Data.SQLite` interop surprises), delete ScimoreDB
and SQL CE. Gate: `test-server-linux.ps1` grows from "compiles" to "runs" - the server creates a
database in a container, imports a fixture library and answers a query.

**e. x64 only - done 2026-10-18, except the installer.** The gate written here ("no `x86` in any
project file") was the wrong test, and finding that out was the useful part: 42 of the 60-odd
declarations are in old-style projects that cannot produce a `net10.0` output at all, while the one
place that really built 32-bit - `MP2-ServiceMonitor.sln`, which maps its `x86` and `Test` platforms to
project configurations with `<PlatformTarget>x86</PlatformTarget>`, and the project's own
`$(Platform)` default of `x86` - was invisible to a search for `Prefer32Bit`.

Removed: the x86 configuration groups in `MediaPortal.Server` and `MediaPortal.ServiceMonitor`, the
`Prefer32Bit=true` on the `AnyCPU` groups of `MediaPortal.Server` and `MediaPortal.Client` (that pair
was what actually made the shipped processes 32-bit on .NET Framework), `<LargeAddressAware>` from both
executables, the ServiceMonitor platform default, and 24 mappings in `MP2-ServiceMonitor.sln`, which now
aliases x86 and Test to `Any CPU` as every other solution in the tree already did.

`Gate:` `architecture.ps1`, which reads the PE `Machine` field of every built apphost rather than the
project files - all five measure `x64` with a managed side of `MSIL`, and it fails if a future SDK-style
project declares 32-bit again. Its first run found a sixth executable that did not exist: the
`bin\Debug\net6.0-windows` output folder the ServiceMonitor stopped using at the retarget is still on
disk, and both rows measured `x64` so the table was right by luck. Rows now carry their write time and
older duplicates are marked `stale`, because 442 pre-retarget output directories are still lying around
and any inspection of build output that does not ask *when* is inspecting the disk, not the source.

Still open, recorded by the same script so it stays visible: the installer chain
(`MP2BootstrapperApp`'s `RuntimeIdentifier win-x86` plus the four WiX projects that really build x86).
A managed bootstrapper application has to match `burn.exe`'s bitness, so the host RID cannot move on its
own; it moves together with the packaging, in a change that has to be verified by running a setup on a
machine with WiX installed.

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

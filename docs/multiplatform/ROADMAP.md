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
`test-canary-linux.ps1`, `test-server-linux.ps1` and `test-server-run-linux.ps1`:

| Measurement | Value | How to reproduce |
| --- | --- | --- |
| Server solution compiles on Windows | 0 errors, 486 gated findings (was 685, then 487) | `build.ps1 -Solution Server -Ratchet` |
| Client solution compiles on Windows | 0 errors, ~2430 findings (not gated) | `build.ps1 -Solution Client` |
| Server solution compiles **on Linux** | 0 errors | `test-server-linux.ps1` |
| Core assemblies on a portable target framework | **4 of 90** - `MediaPortal.Utilities`, `UPnP`, `MediaPortal.Common` and `MediaPortal.Backend` are `net10.0` with `CA1416` as error | `MediaPortal.Common.csproj`, `MediaPortal.Backend.csproj`, `MediaPortal.Utilities.csproj`, `UPnP.csproj` |
| Portable assemblies that declare no platform | 4 - the same four; 77 projects built from `MediaPortal\Source` got `[assembly: SupportedOSPlatform("Windows7.0")]` back from `MP2AddWindowsPlatformAttribute`, and 0 portable ones did | `MediaPortal\Directory.Build.targets`, `$(IntermediateOutputPath)*.PlatformAttributes.cs` |
| Portable-core WinForms / GDI+ references | **0** (83 quarantined in 10 satellite files) | `spine.ps1`, `satellites.ps1` |
| Portable-core P/Invoke declarations | **73** in 9 files (was 95, then 76; the last three were the Kernel32 waitable timer) - invisible to `CA1416` | `spine.ps1` (P/Invoke column) |
| `CA1416` call sites, Windows vs Linux | **0** on both hosts, floor 0 (was 196 at the retarget) - the 107 that remained were all in `-windows` assemblies that had lost their platform declaration; see "the floor went to zero" below | build log, `warnings.ps1` on both logs |
| Unit tests, Windows | **5/5 suites, 562 passed**, 0 failed, 0 skipped (Canary.Portable 29, Tests.Server 220, Tests.Client 90, **Tests.Frontend 155**, Setup\Tests 68); a suite whose assembly predates its own sources reports STALE instead of running the old binary | `tests.ps1` |
| Unit tests, Linux (canary) | 31 executed, 0 failed (1 skips: Windows-only), incl. live IPC round trips and XDG autostart | `test-canary-linux.ps1` |
| Unit tests, Linux (**front end**) | **155 executed, 0 failed** - the real `ConnectionWindow.axaml`/`HomeWindow.axaml`, Fluent theme, bindings, D-pad handler, HTTP client and the SSDP layer, headless with no display in the image; the same image then draws three screens through Skia and writes the PNGs out, the home one populated from the host's server through `host.docker.internal` | `test-frontend-linux.ps1 -ServeFromHost` |
| Server actually **runs** | **yes on Windows and on Linux** as a headless process. Windows: reaches the Running state, creates the SQLite store, answers UPnP discovery over the network, exits 0 with an error-free log. Linux: reaches the same Running state with 0 `FATAL`, 0 `ERROR` and one `WARN` - the documented note that a container has no FUSE driver. Android is still unmeasured | `run-server-check.ps1`, `test-server-upnp.ps1`, `test-server-run-linux.ps1` |
| Server **answers HTTP** | **yes on both hosts**, measured with the same probe: `GET /MediaPortal/Server/health` answers `200` with the server's version and the path it was called with - `pathBase=/MediaPortal/Server`, `path=/health` on Windows *and* in the container, because the shared listener now hands requests over in exactly the shape http.sys uses. The gate checks that text, not just the status code | `run-server-check.ps1 -ProbeUrl`, item **l** below |
| Server **has an HTTP API** | **three endpoints, on both hosts**, in an assembly of its own (`MediaPortal.Server.Api`): `GET /MediaPortal/Server/api/serverinfo`, `GET api/library/aspects` and `GET api/library/count?aspect=…`. They are discovered MVC controllers (the first in this tree) that read MP2's own settings and media-library services on a request thread, and the gates on Windows and in the container require their content, not just a `200`. A library that is absent or still starting is answered with `503` and a reason rather than an empty answer or an internal error. They are unauthenticated because the token endpoint that would authenticate them is still .NET Framework only - which is why they serve schema and no household data | `run-server-check.ps1 -ProbeUrl`, items **m**, **o** and **p** below |
| Front end (Phase 2) | **exists, it finds a server by itself, and it reads it**: an Avalonia 12 app on the bare spine (no `MediaPortal.Common`, no dependency injection, no log4net) with its own JSON API client, its own SSDP/UPnP discovery (a server is listed only after its own HTTP API answered), a screen where the viewer picks the server from what the network reported and typing an address is the fallback, a ViewModel that never touches a UI type, D-pad navigation that moves focus the way a remote does, a headless UI suite (154 tests) and gates that run the built app against a real server and against a real announcement. Writing it found the bugs the design did not anticipate - a process-killing crash from a button press, a fixture that reported 17 failures it had not caused, a screen that overwrote text while it was being typed - and its whole suite also runs headless in a Linux container, with no display and no `EnableWindowsTargeting`. A render probe draws three windows on the **Skia** backend on both hosts and writes the frames out (`docs\multiplatform\screenshots\`) | `build.ps1 -Solution Frontend -Ratchet`, `tests.ps1`, `test-frontend-check.ps1`, `test-frontend-discovery.ps1`, `test-frontend-linux.ps1`, items **q** and **u** below |
| Server **runs on Linux** | **yes** - `HeadlessServerLauncher: Switching to running state`, item **g**'s goal, measured. The layout assembles, `libe_sqlite3.so` (linux-x64) loads, SQLite 3.53.3 opens the store, the subschemas `MediaPortal-Basis`, `MediaLibrary`, `UserProfileDataManagement` and `ClientManager` are created from their SQL scripts, and the resource server plus two control points and one UPnP server share port 55555 through one listener | `test-server-run-linux.ps1`, items **i**, **j** and **k** below |
| Database plugin on a portable driver | `SQLiteDatabase` is `net10.0` with `CA1416` as error, no Win32 calls left, and no longer loads a Windows-only interop library: it talks to SQLite through `Microsoft.Data.Sqlite` (SQLite 3.53.3), whose `e_sqlite3` native is copied into the layout per platform | `SQLiteDatabase.csproj`, items **h** and **j** below |
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

The rest of that inventory went in one step, split by what each piece actually is. **The crash report was
translated rather than relocated** - a crash log should say what it can on every platform, so moving it to a
satellite would have been a way of deleting the information. `DriveInfo()` enumerated the WMI class
`Win32_LogicalDisk`; `System.IO.DriveInfo.GetDrives()` answers the same four questions (name, type, file
system, size and free space) everywhere, keeps the WMI wording ("Local Disk", "CD Drive") so old and new reports
still compare, and reads sizes only when `IsReady` - WMI handed back null properties where this throws, and a
crash report that fails because a card reader was pulled is the worse outcome. `GetCPUInfos()` read
`HKLM\HARDWARE\DESCRIPTION\System\CentralProcessor` once per logical CPU and concatenated the result, so an
eight-core machine got the same sentence eight times with nothing between them; it now reads that key once
behind `OperatingSystem.IsWindows()`, reads `model name`/`cpu MHz` from `/proc/cpuinfo` on Linux and Android,
and says architecture plus core count where neither exists. `GetSizeString(object)` became
`GetSizeString(long)` - its `object` parameter existed only to unbox what WMI returned. `MediaPortal.Common`
no longer references `System.Management`, and nothing in the tree does. `ApplicationCore`'s product-name block
is guarded the same way, with `RuntimeInformation.OSDescription` for platforms without a registry, and its
"Architecture: x64/x86" line now prints `RuntimeInformation.ProcessArchitecture` rather than inferring the
instruction set from pointer size - which on the arm64 devices this port is aiming at would have said "x64"
about an ARM CPU.

**The power handling moved whole**, because there is nothing portable about it: `SystemStateService` calls
`ExitWindowsEx`, `SetSuspendState` and `SetThreadExecutionState`, and `PowerEventHandler` is a hidden window
running a Win32 message loop to catch `WM_POWERBROADCAST`. Both are in the satellite now (the service as
`WindowsSystemStateService`), and the core registers `PortableSystemStateService` - which does the half that
is not Win32 in full (the `SystemState` machine and the `SystemMessaging` notifications every consumer reacts
to), remembers the requested suspend level, and for the machine-level actions logs once that this platform
build cannot change the machine's power state. That is the honest state, not a placeholder: the replacements
are per-platform and not interchangeable (logind over DBus, or `systemctl` with the same polkit approval, on
Linux; nothing at all for an Android app powering the device off), so the follow-up is recorded rather than
implied. Unlike the other three satellites this one has no `Register()` - the hosts hold the instance to drive
state transitions and to dispose it, so they construct it directly. `ApplicationCore`'s registration is
guarded by `Get<ISystemStateService>(false) == null`, the only conditional registration here, because
`MP2-Server` creates its state service *before* calling `RegisterCoreServices()`.

`CA1416` went **140 -> 131**, which is worth spelling out because the arithmetic is not the story. Measured
per file in a build of `HEAD` in a throw-away worktree and in a build of the new tree: `CrashLoggerBase` 13,
`SystemStateService` 4, `PowerEventHandler` 1, `ApplicationCore` 1 went to zero - 19 gone - and 11 appeared in
`MediaPortal.Server\ApplicationLauncher.cs`, which calls `WindowsSystemStateService` exactly the way it already
calls the three satellite `Register()` methods. Those 11 are the seam pattern's price, and chasing their cause
found something bigger: **`MediaPortal.Server` is built with `GenerateAssemblyInfo=false`, which also suppresses
the `[SupportedOSPlatform]` attribute the SDK derives from a `-windows` target framework.** Verified directly -
that project has no generated `AssemblyInfo.cs` at all, while `MediaPortal.Common.Windows`' has two of those
attributes. The sentence that used to be here - "adding the
attribute by hand took `MP2-Server`'s whole `CA1416` count to zero" - was wrong twice over, and correcting
it is worth more than the claim was. An `<AssemblyAttribute>` item is inert while `GenerateAssemblyInfo=false`,
because that item is consumed by the `GenerateAssemblyInfo` target the same property disables; and the
probe's "zero" came from a filter of the form `MediaPortal\.Server\.csproj.*warning CA1416`, which cannot
match, because in an MSBuild log the `[Project.csproj]` tag comes *after* the warning text. The mechanism
itself survived retesting, and it was bigger than the 11 findings it turned up in: **every one of the 107
remaining `CA1416` sites (131 in the ratchet's unit, in both solutions) sat in a `-windows` assembly that had
lost its platform declaration**, `Application.Run(form)` and `MessageBox.Show` in a WinForms application
among them. The fix is `MP2AddWindowsPlatformAttribute` in the two `Directory.Build.targets` files: it writes
the single attribute the SDK would have written into `obj` and hands it to the compiler, for 106 projects,
none of them portable - the condition is `GenerateAssemblyInfo == 'false' AND TargetPlatformIdentifier ==
'Windows'`, so `MediaPortal.Utilities`, `UPnP` and (when it is flipped) `MediaPortal.Common` are untouched
and their `<WarningsAsErrors>CA1416</WarningsAsErrors>` keeps its teeth. `CA1416` went **131 -> 0**, gated
findings **618 -> 487**, and no other warning code moved by a single finding.

So the `CA1416` floor is now **0**, which is stricter than 131 ever was: a `CA1416` finding can now only come
from an assembly that declares no platform, which after this change means a portable one - the portable core
touching a Windows-only API, and the gate stops the build over it. What the port's Windows budget actually is
remains what it was: `spine.ps1`'s combined 180 references and 76 portable `[DllImport]` declarations, both
unchanged by this step. `scripts/ci/README.md` carries both halves of the lesson - how to spot a `-windows`
assembly that lost its declaration, and why a zero produced by a filter that cannot match is the most
convincing wrong number there is. Two smaller things fell out of the same step: `DevTools`
now uses the portable state service (a headless harness should not power off a developer's machine), and
`UiComponents\News\build.targets` stopped copying `System.ServiceProcess.ServiceController.dll` into
`Plugins\News` - a DLL that folder only ever had because `CopyLocalLockFileAssemblies` copies transitive package
assemblies into every consumer's output, and no code in that plugin reads it.

**For the core, (c) is done: `MediaPortal.Common` targets `net10.0` (2026-10-18).** The common assembly is
the one everything else stands on - server, database layer, and in Phase 2 the Avalonia frontend - so while
it carried `-windows` every project above it did too. It now builds as `net10.0` with `UseWindowsForms`
gone and `<WarningsAsErrors>CA1416</WarningsAsErrors>` set: **0 errors and 0 `CA1416` on both hosts**, with
the gated total unmoved at 487 and `MediaPortal.Common -> bin/Debug/net10.0/MediaPortal.Common.dll` in both
build logs. That the analyzer now has teeth here is not an inference - an unguarded
`Microsoft.Win32.Registry.CurrentUser` inserted into the core produced `error CA1416` and exit 1, and was
removed again.

Two things had to happen first, and one of them is the more instructive.

*The last P/Invoke in the core left.* `TaskWaitableTimer` was a `WaitHandle` over `CreateWaitableTimer` /
`SetWaitableTimer`, and it did two jobs at once: fire at a due time (portable, and all the task scheduler
needed) and wake the machine from standby with `bResume: true` (Win32, and the reason a 4 a.m. TV recording
happens). It is now `ITaskWakeUpTimer`, with `PortableTaskWakeUpTimer` in the core, `WindowsTaskWaitableTimer`
plus a factory in `MediaPortal.Common.Windows`, and `WakesSystemFromSuspend` as the flag that says which one
a host got - the portable one says so in the log too, because "the application wakes, the machine does not"
is otherwise invisible until a recording is missing. `TaskScheduler` resolves the factory the first time it
wants to wake anything rather than in its constructor, which is what makes the host's registration order
irrelevant. `spine.ps1`: core P/Invoke **76 → 73** in **9** files, satellites **104 → 107**, combined
**180 unchanged** - moving Windows code into a satellite costs nothing on that ledger, which is the point of
it. Eight new tests in `Tests.Server\Services\TaskScheduler` pin the rules the handshake thread and
cancellation token used to enforce (a replaced, disarmed or closed timer never fires; a throwing handler is
reported instead of taking the process down), and took `tests.ps1` from 271 to **279 executed, 0 failed**.

*A framework reference flows through a project reference.* Flipping `MediaPortal.Common` broke
`MediaPortal.UI` and `MediaPortal.Client.Launcher` with 28 `CS0234`/`CS0246` errors about
`System.Windows.Forms` - not because those projects gained a dependency, but because they lost one they never
declared. Both are full of WinForms (`WindowsMessaging` handles `Message`, the launcher shows a `NotifyIcon`);
they compiled because `MediaPortal.Common` had `UseWindowsForms=true`, the SDK turns that into a
`FrameworkReference` to `Microsoft.WindowsDesktop.App`, and framework references arrive through project
references. Both projects now say `<UseWindowsForms>true</UseWindowsForms>` themselves. The general form is
in `scripts/ci/README.md`: when a project's own `<Reference>`/`<UseXxx>` declarations are thinner than its
`using` statements, a retarget somewhere below it will look like it broke unrelated code.

`System.Runtime.Caching` was the last package the core could not obviously keep, and it measured fine: a
throwaway `net10.0` program ran `MemoryCache` (named instance with `null` and empty `NameValueCollection`
config, `CacheItemPolicy` sliding expiration, `MemoryCache.Default`, and the 200×50 get/set loop
`RelationshipCache` performs) on `linux-x64` in a container - all four checks pass, so it is not a blocker
for the flip. It does drag `System.Configuration.ConfigurationManager`, `System.Security.Permissions` and
`Microsoft.Win32.SystemEvents` into a portable project's output, which is worth remembering when the caching
strategy is revisited.

**The reference graph paid out immediately.** `MediaPortal.Backend` was next by dependency order (Backend →
Common → UPnP/Utilities) and needed no work at all: it references only the three now-portable assemblies, and
`spine.ps1` had been reporting 0 WinForms, 0 GDI+ and 0 `[DllImport]` in it for weeks. Flipping it to
`net10.0` with `CA1416` as error built **0 errors / 0 findings** on the first try, and the gated total stayed
at 487. Four of the ninety SDK-style projects are portable now; the ones above them are the UI, which is what
Phase 2 replaces.

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

**g. The server runs - done on Windows 2026-10-18, on Linux since item k.** Until today every claim about the
server was a claim about the compiler. That changed with `MediaPortal.Server.Headless`, an
`net10.0` console host (`MP2-Server-Headless`) that repeats `MediaPortal.Server`'s startup sequence in
the same order - vital services, core services, backend services, plugin `Initialize`/`Startup`,
`StartCoreServices`, IPC, `StartupBackendServices`, media accessor, `Running`, migration, importer
worker - and drops everything that needs a desktop: no `MainForm`, no `ServiceBase`, no
`Application.Exit`, no `MediaPortal.Common.Windows` registrations. Ctrl+C and SIGTERM both request a
clean shutdown; `--check` brings the server up and immediately down again, which is the shape a
container needs.

Because it does not upgrade the platform seams, it runs the degraded path that no executable used to
run: `PortableSystemStateService`, no resource mounting, current-user impersonation, no image
transformer. That is the point - those fallbacks were written by now, not by a hypothetical future host.

Deployment came with it (`deploy-server-layout.ps1`), and had to: every plugin's `build.targets` copies
into `$(SolutionDir)..\Bin\$(SolutionName)\bin\$(OutDir)\Plugins\`, but only when `$(MP2IsSolutionBuild)`
is set, which `dotnet publish` never sets. So the layout is assembled in the script from the published
host plus one plugin directory per plugin, following the rules
`Source\Backend\Database\SQLiteDatabase\build.targets` encodes - `plugin.xml` comes from the project
directory, `System.Data.SQLite.dll` is a copied local dependency, and its native `SQLite.Interop.dll`
has to sit in an `x64\` subfolder because that is where the wrapper looks for it.

Gate: `run-server-check.ps1` - exit code 0, `Switching to running state` in the log, no `FATAL` line,
and `<DATA>\Database` not empty. Measured on the first run: `Datastore.s3db` (16 MB, WAL alongside),
subschema versions logged, `ImporterWorker: Activated (0 ImportJobs pending)`, zero `ERROR` and zero
`WARN` lines in the whole log.

What the first run actually bought were three bugs that no amount of reading had surfaced:

| Found | Where | Why the compiler could not see it |
| --- | --- | --- |
| `Unkown path label '<LOG>'` | `PathManager.InitializeDefaults` built `DEFAULTS` as `@"<APPLICATION_ROOT>\Defaults"` | A backslash is a filename character off Windows, so `Paths.xml` - which is what defines `<LOG>`, `<DATA>`, `<PLUGINS>`, `<DATABASE>` - was simply never found |
| Path values full of backslashes | `Defaults\Paths.xml` values, e.g. `<DATA>\Log` | Same class of bug one level down: labels are data, so `SetPath` now normalises separators (`NormalizeSeparators`) instead of every platform needing its own `Paths.xml` |
| Server died when `Plugins` did not exist | `PluginManager.LoadPluginsData` | `Directory.GetDirectories` throws on a missing directory but not on an empty one, and startup called it unconditionally |

Still Windows-only on this path, and each one is the next step: the SQLite plugin itself
(`net10.0-windows`, `System.Data.SQLite` natives for win-x64 only, `CompareStringEx` for the `NOCASE`
collation, `GlobalMemoryStatusEx` for cache sizing), and the HTTP stack - the log shows
`HttpSysListener`, and `http.sys` does not exist off Windows, so the resource and UPnP servers need
Kestrel on Linux. The gate for item **d** ("compiles" becomes "runs") is this same script inside the
container.

One side effect worth naming: adding the host to `MP2-Server.sln` made `test-server-linux.ps1` build
`bin/Debug/net10.0/MP2-Server-Headless.dll`, which is the first MP2 *executable* that compiles on
Linux. Compiling is not running - which is precisely the lesson this step paid for.

**h. The database plugin left Windows; its native library did not (2026-10-18).** `SQLiteDatabase` was
the first of the six database providers to flip to `net10.0` with `CA1416` as an error. The others are
still .NET Framework 4.8 projects - `MySQL.Data`, a 2012 `FirebirdSql.Data.FirebirdClient`,
`Microsoft.SqlServerCe`, `Scimore.*` x86 natives - so there was nothing cheap about them, while the
SQLite provider was already SDK-style.

Two Win32 calls lived in it:

| Was | Now |
| --- | --- |
| `GlobalMemoryStatusEx` to size the SQLite cache | `GC.GetGCMemoryInfo().TotalAvailableMemoryBytes`, which is the amount of memory .NET is willing to use - closer to what cache sizing should ask about anyway |
| `CompareStringEx` with `SORT_DIGITS_AS_NUMBERS`, the `NOCASE` collation MP2 databases were written with | kept, and still the Windows implementation, behind `[SupportedOSPlatform("windows")]` and an `OperatingSystem.IsWindows()` branch; everywhere else a new `NaturalStringComparer` does the same job in managed code |

Splitting the comparison out was not decoration. While it lived behind an `IsWindows()` branch no test
on a Windows machine could reach it, so it had never been tested at all. As a class of its own it is
tested on every platform (`TestNaturalStringComparer`, 11 tests, 290 tests in the suite), and one test
compares the two implementations pair by pair, so the managed one cannot drift.

Two things in there were measured rather than assumed, and both were surprises:

- `CompareStringEx` with `SORT_DIGITS_AS_NUMBERS` does **not** call `"x007"` and `"x7"` equal: the
  longer digit run sorts first. A "natural" comparison that treats leading zeros as valueless would
  order them equal, so the portable comparer now breaks the tie on digit-run length, to match the
  ordering existing libraries were filed under.
- Managed culture comparison (`StringComparer.Create(InvariantCulture, false)`, i.e. ICU off Windows)
  puts lowercase *before* uppercase, which is the opposite of what the Windows path does. So the
  case-sensitivity test asserts that the two differ, not which one wins; nobody should depend on a
  direction that changes with the platform.

What was *not* portable was one level down: `System.Data.SQLite.Core` is a .NET Standard assembly that
compiles everywhere, but it loads `SQLite.Interop.dll`, and the package ships win-x64 and win-x86 only.
That is item **j**.

**Running the server found more, after item g had already proved the point.** Three things turned up
once the server was started and stopped repeatedly, and none of them is visible to a compiler:

| Found | Where | What it really was |
| --- | --- | --- |
| Shutdown stalled exactly 30 seconds, then logged `Cannot acquire synchronization lock. Maybe a deadlock happened.` | `SSDPServerController.Close` | `Timer.Dispose(WaitHandle)` and its `WaitOne()` ran *while holding* `SyncObj`, and both timer callbacks take `SyncObj` - so `Close` waited for a callback that was waiting for `Close`, until the callback's `TryEnter` timeout expired. Timers are stopped and awaited outside the lock now, which also keeps "alive" before "byebye" |
| Two to six `Unhandled task exception` ERROR lines per shutdown | SSDP and GENA listeners | `BeginReceiveFrom` is implemented on top of `ReceiveFromAsync`, so closing a socket with a receive pending faults a `Task` nobody observes, which .NET reports from the finalizer thread. The server's listener now uses `SocketAsyncEventArgs`, where the abort is a `SocketError` value. The client-side listeners (`SSDPClientController`, `GENAClientController`) still use the APM pair, so `LauncherExceptionHandling` recognises abort-only unobserved exceptions and logs them as teardown at `Debug` instead of `Error` |
| An M-SEARCH sent to `127.0.0.1:1900` is answered by nobody | Windows UDP socket selection | Several sockets share port 1900 through `SO_REUSEADDR` and Windows delivers a *unicast* datagram to one of them - the Windows SSDP service. Real control points multicast, so the probe multicasts too |

That third line is the reason the probe exists: `run-server-check.ps1` shows the listeners coming up and
going down, which says nothing about whether they answer anything, and the SSDP receive loop had just
been rewritten. So the headless host grew `--duration <seconds>` (stay up, then shut down exactly the way
`--check` does) and `test-server-upnp.ps1` multicasts an M-SEARCH with `MX: 1` and requires a `200 OK`
whose `LOCATION` names MP2's own UPnP listener and whose `SERVER` header says `MediaPortal-2-Server` -
headers no other product on the LAN sends - plus exit code 0, no `ERROR` line and no lock-contention
line. First measured run: seven responses, one for the root device and one for each of its services.

The run also corrected an assumption in item **g**. The HTTP traffic is logged by
`Microsoft.AspNetCore.Server.HttpSys.HttpSysListener`: the resource server, the UPnP servers and the
control point are already ASP.NET Core hosts. `http.sys` is the Windows-only *server* behind that
abstraction, so Linux needs Kestrel as a provider swap, not the replacement of an HTTP stack that item
**g** anticipated.

**i. The server starts on Linux (2026-10-18).** Items **d** through **h** were all argued from
inventories and compile results. This one is a log: `Dockerfile.server` and `test-server-run-linux.ps1`
build the layout and start the host *inside a Linux container*, using the same two scripts the Windows
gate runs (`deploy-server-layout.ps1`, `run-server-check.ps1`) rather than a Linux-specific reimplementation,
so a green result here and a green result there mean the same thing.

It got further than expected. `dotnet publish` assembled the layout, the host started on `.NET 10.0.13`,
`SystemResolver` produced a local system id (so the machine-id path that replaced the registry read
works), plugins were reflected and `SQLiteDatabase` was activated and logged its settings. Then:

| Order | Log line | Root cause | Portable answer |
| --- | --- | --- | --- |
| 1 | `[FATAL] SQLiteDatabase: Error establishing database connection` - `System.DllNotFoundException: Unable to load shared library 'SQLite.Interop.dll'`, tried as `.so` and bare in the plugin folder and the runtime directory | item **h**'s prediction, measured: `System.Data.SQLite` has no Linux native | `Microsoft.Data.Sqlite`, whose `e_sqlite3` natives cover linux-x64 and android; the collation registration and the Guid-as-BLOB storage have to be ported by hand, which is why this is a step and not a package bump |
| 2 | `[FATAL] HeadlessServerLauncher: error while starting the core` - `TypeInitializationException` for `Microsoft.AspNetCore.Server.HttpSys.HttpApi`, `DllNotFoundException: 'HTTPAPI.dll'`, thrown from `ResourceServer.CreateAndStartServer` | `http.sys` does not exist outside Windows; one `UseHttpSys` call in `UPnP.Infrastructure.Http.HttpServer` is the seam, and every HTTP host in the server goes through it | Kestrel. Not a straight swap: MP2 puts several hosts on port 55555 under different base paths, which `http.sys` does natively and Kestrel does not, so off Windows the hosts have to share one listener and dispatch on path prefix |
| 3 | `[FATAL] HeadlessServerLauncher: error while shutting down` - `ServiceNotFoundException: Could not find the IUserProfileDataManagement service` from `BackendExtension.ShutdownBackendServices` | teardown assumes every service it wants to stop was registered. The database failed above, so the service was never built - and the real cause got buried under two extra `FATAL`s | shutdown has to tolerate services that never came up. Not Linux-specific: the same line appeared on Windows when two servers collided on a port, see item **g** |

Writing the probe also exposed a portability bug in the tooling itself: `Join-Path $repoRoot
'MediaPortal\Source'` and `Join-Path $PSScriptRoot '..\..'` are correct on Windows and wrong on Linux,
where a backslash is not a separator, so `'MediaPortal\Source'` is one *file name*. That is the same
class of defect `PathManager` had, sitting in the scripts that are supposed to prove the server runs on
Linux; all of them now build paths from separate components.

**j. The SQLite driver swap: the database opens on Linux (2026-10-19).** Item **i**'s first `FATAL` was
`DllNotFoundException: SQLite.Interop.dll`. `Microsoft.Data.Sqlite` replaces `System.Data.SQLite`: same
SQLite engine through `SQLitePCLRaw`, with `e_sqlite3` natives for linux, android, macOS and Windows
(SDK 10.0.13, SQLite 3.53.3 measured in both layouts). Not a package bump:

| Decision | Why |
| --- | --- |
| `Guid` keys keep binding as 16-byte BLOBs in `Guid.ToByteArray()` order | MP2 databases were written by `System.Data.SQLite` with `BinaryGUID=true`. Measured on a real 16 MB datastore: `MIA_TYPES.MIAM_ID` is declared TEXT but stored as `typeof=blob len=16`. `Microsoft.Data.Sqlite` binds a Guid as TEXT - `WHERE MIAM_ID=@g` returned **0 rows** against such a row, the same query with the bytes returned **1**. Silent empty results are the worst shape a storage-format change can take, so the decision is centralised in `SQLiteValues` and pinned by four tests |
| `Pooling=False`, MP2's own `ConnectionPool<T>` live again | `CreateCollation` registrations are per connection, and the driver's pool *removes* them when a connection is returned: the next user of that connection got `no such collation sequence: NOCASE` - not even SQLite's own built-in. `NO_POOL` had been defined in both configurations, which meant the driver pool was in use and MP2's pool was dead code; it is the only pooling now, so `ConnectionPool` got its first tests (5) and two fixes: `_pool` disposed, and the slot reservation made atomic - "read the count, then increment" let every racing thread past the maximum at once |
| The `NOCASE` override survives as a comparison delegate | the old one was a `[SQLiteFunction]` aggregate, which is a `System.Data.SQLite` extension point. `CreateCollation("NOCASE", ...)` overrides the built-in (measured), and ~18 schema fragments of a real datastore use `COLLATE NOCASE`, so it is load-bearing, not cosmetic. Item **h**'s platform split (Win32 `CompareStringEx` on Windows, `NaturalStringComparer` elsewhere) is exactly what the new registration point needs |
| `PRAGMA page_size` moves out of the connection string | `Page Size`, `journal_mode`, `Cache Size`, `FullUri`, `BinaryGuid` and unknown keywords throw `ArgumentException` (measured) - the driver validates the string. Page size is set with `PRAGMA page_size=N;VACUUM;` at creation and persists across reopen; journal mode and cache size go through the `_connectionPragmas` batch that runs on every connection |
| `ChunkSize`, `SQLiteLog` and `GetMemoryStatistics` are gone | no equivalent in the new driver. The settings entry stays (a config file must still load) and says it is inert; trace logging logs one line naming the loss instead of failing the constructor; `DatabaseVersion` is `ServerVersion` from an open connection |

The native library is a decision, not a lookup, and this step proved that twice. The plugin's build
output has `runtimes\<rid>\native` for *every* RID in the package graph, so picking one is a choice: the
first Linux run of the new layout took `linux-musl-x64`, because that was first in a hand-written list,
inside a glibc Debian image. Both the layout script and `build.targets` now ask
`RuntimeInformation.RuntimeIdentifier` (musl-aware) / `$(NETCoreSdkPortableRuntimeIdentifier)`, and the
targets file warns when it finds no native at all instead of quietly shipping a plugin that throws.
Windows is unaffected: the layout prints `native SQLite library: e_sqlite3.dll (win-x64)` and the server
still logs `SQLiteVersion: 3.53.3`, reaches Running, and stops cleanly.

**The probe found a bug in itself while doing this.** `deploy-server-layout.ps1` assigns its
platform check to `$isWindows`, and pwsh defines `$IsWindows` as a *read-only* automatic variable:
"Cannot overwrite variable IsWindows because it is read-only or constant" stopped the layout mid-way -
inside the container only, because the build host runs Windows PowerShell 5.1, where the name does not
exist. A script whose whole job is to prove the server runs on Linux had a defect that only appears on
Linux; recorded in the CI README with the other traps.

The same class of defect was found in the test gate, by arithmetic rather than by a container.
`tests.ps1` runs `dotnet test --no-build` on purpose, and the suite reported 122 passed for a project
whose assembly held 123 tests: the test added after the last build simply was not in the binary that
ran, and the line was green anyway. Each suite now compares its own sources with its own assembly and
reports `STALE` (a failure, not a warning) when the sources are newer - a floor, not a transitive-closure
proof, and documented as such. The first version of that guard globbed for
`Tests.Server.csproj.dll`, matched nothing, and silently did nothing, so the guard itself was tested by
touching a source file and requiring the red output.

After this step the Linux run had two `FATAL` lines left: `HTTPAPI.dll` (item **i** row 2, Kestrel), and
one from teardown. The teardown turned out to be its own bug, and chasing it through three rebuilds was
worth the time.

`ServiceNotFoundException: Could not find the IUserProfileDataManagement service` reads as "startup did
not register it", so that is what was fixed first - `ShutdownBackendServices` and `StopCoreServices` now
resolve optionally and skip what is absent, with two tests for the case the compiler cannot reach. That
removed the exception and left the `FATAL`: only the *name* of the missing service changed, from
`IUserProfileDataManagement` to `ILogger` to `IRemoteResourceInformationService`, one per rebuild. A bug
whose symptom moves each time it is fixed is a symptom of something else, and here it was: `Stop()` runs
**twice** on the failed-startup path - `Start()` calls it from its catch, `Main()` calls it again in its
`finally`. `DisposeServices()` has carried a comment about exactly that since it was written and guarded
against it; the shutdown sequence above it did not. So the second pass walked a registry the first pass
had already emptied, and reported a shutdown failure directly below the startup failure that mattered -
with a different service named depending on which had been disposed first.

Three changes, then, not one: the teardown is idempotent (`Interlocked.Exchange`, because an IPC shutdown
can race with `Main()`'s), it tolerates services that are not there, and the two together are pinned by
`TestShutdownWithNothingRegisteredDoesNotThrow`, `TestShutdownWithoutALoggerDoesNotThrow` and
`TestStopCoreServicesWithNothingRegisteredDoesNotThrow`. The run ends at one `FATAL` now, which is the
number of real blockers: `HTTPAPI.dll`. The next step is Kestrel behind a shared listener in
`UPnP.Infrastructure.Http.HttpServer`, Windows keeping `http.sys` untouched.

**k. The HTTP layer leaves http.sys: one listener, many base paths (2026-10-19).** Item **i**'s row 2 was the
last blocker: `DllNotFoundException: HTTPAPI.dll`, thrown from `ResourceServer.CreateAndStartServer`, with the
same seam used by the UPnP server and the control point. `http.sys` is not a library that can be missing - it is
a kernel driver, and what MP2 actually depends on is one of its features: several independent web hosts listen
on port 55555, each owning a different URL prefix. Kestrel does not offer that, so the feature had to be
implemented. Four things were measured before writing anything, in a scratch ASP.NET Core project, and two of
them changed the design:

| Question | Measurement |
| --- | --- |
| Can two Kestrel hosts share a port if each is given a base path in its URL, the way `BuildUrls` builds them for http.sys? | No, and not with a conflict message but with `InvalidOperationException: A path base can only be configured using IApplicationBuilder.UsePathBase()` - `UseUrls("http://127.0.0.1:55199/a/")` is not a valid Kestrel address at all. So a prefix cannot travel in the URL; dispatching has to happen inside one listener |
| Does the GENA `XmlSerializer` still get its synchronous request-stream read? | Yes. `KestrelServerOptions.AllowSynchronousIO = true` is the counterpart of the `HttpSysOptions` setting MP2 already uses, and a handler calling `StreamReader.ReadToEnd()` on `Request.Body` succeeded against a Kestrel listener with it set |
| Is prefix matching a `StartsWith`? | No, and the first version got it wrong: `"/a/x".StartsWithSegments("/a/")` is **false** for every request, because the trailing slash makes the prefix two segments, the second one empty. Prefixes are compared after trimming the trailing separator, case-insensitively, and on segment boundaries - `/MediaPortal/Server` does not match `/MediaPortal/ServerRoom` |
| Can one listener bind `0.0.0.0` and `[::]` for a wildcard URL? | On Windows yes; on Linux the second bind fails with `SocketException (98): Address already in use` for `http://[::]:55555`, because a dual stack IPv6 socket already owns the port and covers IPv4. A wildcard is therefore served by one socket - `[::]` where IPv6 is bindable, `0.0.0.0` where it is not |

`SharedHttpPortHost` in `UPnP/Infrastructure/Http` keeps one listener per TCP port, registers endpoints by URL
prefix, and dispatches each request to the endpoint with the longest matching prefix - the rule http.sys
applies, now with fourteen tests of its own. Three properties were chosen to keep the two hosts' behaviour
identical rather than merely working:

- **Requests are not rewritten.** `PathBase` stays empty and `Path` keeps the prefix, because MP2's UPnP
  endpoints work on `HttpRequest.GetUri()` (built from `PathBase` + `Path`) and match against
  `DescriptionPathBase`, which contains the prefix. This is deliberately *not* what http.sys does: measured
  on Windows, a request for `http://127.0.0.1:55555/MediaPortal/Server/health` reaches the endpoint as
  `PathBase=/MediaPortal/Server`, `Path=/health` - the prefix http.sys registered moves into `PathBase`.
  The UPnP endpoints do not care, because `PathBase + Path` is the full path on both hosts, but anything
  matching on `Path` alone has to know which host it runs on. The resource server's conventional route
  `MediaPortal/Server/api/{controller}` is such a case: it can only ever match on the shared listener, so
  the registrations in `ResourceServer` come in a prefixed and a prefix-free shape, and the health endpoint
  reports the two values it was called with so that each platform's gate measures its own host.
- **An endpoint with a pipeline is registered first.** The resource server needs `AddControllers` and one
  `AddApplicationPart` per middleware, and the DI container is closed once the listener is built; plain
  handlers can join and leave while it runs. MP2's own startup order already satisfies this - the resource
  server comes with the core services, the UPnP servers come from plugin startup - and registering the other
  way round says so in an exception instead of failing quietly.
- **Endpoints are reference counted.** The first registration opens the port and the last one closes it, so
  disposing a UPnP server does not take the resource server's listener with it. Restarting the resource server
  inside one process gets the port back, which is asserted with a `TcpListener`.

Windows keeps `http.sys` and its `WebHost.CreateDefaultBuilder` untouched; the portable path uses
`WebApplication`, which is also the modern API - `WebHostBuilder` and `IWebHost` now carry `ASPDEPR004` and
`ASPDEPR008` deprecation warnings, and the container run is the only thing that will ever exercise it off
Windows.

**The step reported two bugs that had nothing to do with HTTP.** The container failed the same way twice before
the second failure was understood, and both times the message was wrong:

| Symptom | What it really was |
| --- | --- |
| The log ends in `Stack overflow.` with 58000 lines of `LauncherExceptionHandling.WriteInnerException` frames, and the process dies with signal 6 (exit 134) | `WriteInnerException` recursed on `exception` instead of `exception.InnerException`, so *any* exception with an inner exception ended the process while its report was being built. It fires on the last-resort path only - no logger registered and no writable log directory - which is exactly the state of a host that dies during core startup. It had been hiding the reason of failures twice. Now iterative, bounded, with five tests |
| `DirectoryNotFoundException: Could not find a part of the path '/src/build/artifacts/server-layout\Scripts\/MediaPortal-Basis-create-1.0.sql'` | Item **g**'s separator class again, one layer further: `PathManager.SetPath` normalized the separators of stored labels, but four subschemas ask for `"<APPLICATION_ROOT>\Scripts\"`, and the part *behind* the label was never normalized. On Linux that is one file name containing backslashes. `GetPath` now normalizes what it returns, with five tests |

The probe reported on itself as well: `-SkipImageBuild` reuses the image, which holds a *snapshot* of the tree,
and the script warns about that in yellow - a warning that was filtered away twice, producing two confident
readings of a binary that no longer existed. Read the warning; rebuild the image when the tree moved.

After this step the Linux log reads: database opens, `libe_sqlite3.so (linux-x64)`, SQLite 3.53.3, all four
subschemas created from their SQL scripts, then four HTTP endpoints on one port -
`/MediaPortal/Server`, `/MediaPortal/UPnPControlPoint_D0278452`, `/MediaPortal/UPnPControlPoint_8C0745F5`,
`/MediaPortal/UPnPServer_49EAC870` - and `Switching to running state`. Zero `FATAL`, zero `ERROR`, four
`WARN`: the resource-mounting note that a container has no FUSE driver for, and three
`Could not set IPv4 options / Cannot assign requested address`. Item **l** below chased those three down: they
were not the multicast setup at all but the socket teardown, and the message was a copy-paste.

### l. The server answers: one HTTP probe, and the teardown that complained on one platform only

Item **k** proved the listeners start. "The server answers" is a different claim, so `run-server-check.ps1`
gained a fifth assertion: while the host runs it requests every URL in `-ProbeUrl` and requires `200`. Probing
needs the server to stay up, so a probing run holds the host with `--duration` instead of `--check`, and both
gates - the Windows one and the container - now ask the same question through the same script.

For there to be an answer, the server needed one endpoint that works without plugins, users, tokens or the
database. `ResourceServer` now serves `GET <base>/health` with its name, version, the current UTC time and the
`PathBase`/`Path` the request arrived with. That last pair is not a diagnostic indulgence: it is what settled a
claim this document had written down wrongly.

| Question | Measurement |
| --- | --- |
| Does http.sys hand a prefixed request over with the prefix still in `Path`, the way the shared listener did? | **No.** `http://127.0.0.1:55555/MediaPortal/Server/health` arrives on Windows as `PathBase=/MediaPortal/Server`, `Path=/health` - http.sys moves the prefix it registered into `PathBase`. Before the change described below, the container saw the same request as `PathBase=""`, `Path=/MediaPortal/Server/health`. The first probe run answered `404` on Windows, and that is how this was found |
| What does that break? | Everything matching on `Path` alone. The UPnP endpoints compare against `PathBase + Path`, so they behave identically on both hosts, but the resource server's conventional route `MediaPortal/Server/api/{controller}` can only ever match on the shared listener - it was dead code on Windows from the start, and there are no controllers to notice |
| How is that squared? | The shared listener was changed to hand requests over the way http.sys does: it matches the longest registered prefix, reports that prefix as `PathBase` and lets `Path` continue behind it (see `SharedHttpPortHost.DispatchAsync`). Every MP2 endpoint was checked before making the change - the UPnP host, GENA and the resource access modules all match on `PathBase + Path`, which is the string they built their keys from all along - so they are unaffected, while anything using routes suddenly works the same on both platforms. The resource server's registrations went back to one shape each: `/health` and `api/{controller}/{id?}` |

**The unification is measurable, not assumed.** `SharedHttpPortHost` restores `PathBase`/`Path` after every
request and hands both to the endpoint it dispatched to; `Tests.Server` pins the shape for one endpoint, for two
endpoints whose prefixes nest inside each other, and for a routed pipeline whose routes are written relative to
the prefix. The health endpoint still reports the pair, so the two gates keep printing what their own platform
really does - which is how the container's answer above became `pathBase=/MediaPortal/Server`, identical to
Windows, in the run that followed the change.

**The teardown complained on Linux only, and the message was a copy-paste.** The container's three
`Could not set IPv4 options / Cannot assign requested address` warnings came from
`NetworkHelper.DisposeSSDPMulticastSocket`, not from the multicast setup the message named - the same sentence
was used by five different `catch` blocks in three different operations, two of which blamed
`GENAServerController` for SSDP sockets. The actual defect: leaving a multicast group has to name the interface
that joined it, and the code passed the group alone, which asks the stack to drop a membership of
`IPAddress.Any` that was never added. Windows answers that without complaint; Linux fails it with
`SocketError.AddressNotAvailable` for every socket the server closes. The interface is now taken from the
socket's bound address - which is the one `BindAndConfigureSSDPMulticastSocket` joined on - the
`exitMulticastGroup` parameter that `SSDPServerController.CloseSSDPEndpoint` documented but ignored is
honoured, and every message says what it did, to which group, on which interface. Five tests, one of which is
the regression itself: join, close, and require that nothing was logged.

| Measurement | Before | After |
| --- | --- | --- |
| Container `WARN` lines | 4 (three teardown, one resource-mounting) | **1** - the documented FUSE note |
| Container `ERROR` / `FATAL` | 0 / 0 | 0 / 0 |
| Health probe, Windows (`http.sys`) | no such assertion | `200`, `pathBase=/MediaPortal/Server`, `path=/health` |
| Health probe, container (shared Kestrel listener) | no such assertion | `200`, `pathBase=/MediaPortal/Server`, `path=/health` - the same answer as on Windows, after the unification |
| Unit tests | 335 | **347** (5 multicast teardown, 5 health endpoint, 2 for the path shape of the shared listener, 1 of them a routed pipeline) |

### m. The first controller: the HTTP API the new front end will talk to

Item **l** ended with a listener that answers, which is not the same claim as an API. The health endpoint is a
delegate mapped onto the pipeline; a front end needs MP2's own services behind URLs. Until this round the tree
had **zero** MVC controllers - the conventional `api/{controller}` route the resource server declares had never
matched anything, on either host.

Two facts decided the shape of the first slice, and both were read out of the code before writing it:

- `ResourceServer.CreateAndStartServer` already adds the assembly of every registered resource access module as
  an MVC application part (`mvc.AddApplicationPart(middleWareType.Assembly)`), with the author's own note that
  this "is currently a hack because plugins that have controllers might not necessarily register a middleware
  ... A separate API host should probably be created with the necessary interfaces". `ResourceAccessModule`
  itself lives in `MediaPortal.Common`, so a controller in that assembly is discovered without any wiring. That
  is where the API starts. It moves to a portable project of its own once it holds several controllers, which is
  a file move plus one `AddApplicationPart` - and the reason it does not move today is that a new project also
  means a solution entry and two more rows in the spine and architecture gates, paid for one endpoint at a time.
- `ServiceRegistration` is a process-wide singleton over a `ConcurrentDictionary`, with no thread affinity and a
  documented "all public methods are multithreading-safe". A controller can therefore resolve MP2 services on
  an ASP.NET Core request thread. The first endpoint does exactly that with `ISettingsManager`, and a test
  registers a stub settings service to prove the answer came from the registration rather than from a default.

`GET api/serverinfo` reports the name, version, the .NET the process runs on, the operating system, both
architectures (the machine's and the process's), the process id, the server's UTC time, the URL prefix the
request arrived on, and the IP endpoints the server was configured to listen on. Writing it surfaced two things
that are worth more than the endpoint itself:

| Finding | Consequence |
| --- | --- |
| The resource server runs inside the **client** too, below `/MediaPortal/Client` - the base path is decided by `ISystemResolver.SystemType`, which is why the constant reads `/Server` or `/Client` | Left alone, a client would have answered questions about a server and served API endpoints it does not use. The endpoint now answers `404` when the resolver reports a process that is not a server |
| `MediaPortalAuthorizeAttribute` demands an identity whose authentication type is `"MediaPortal"`, and the component that issues such identities is the Owin OAuth token endpoint, which only the .NET Framework target of this project still builds | The filter cannot be applied on .NET today - it would answer `403` to every client on every platform. The endpoint is therefore unauthenticated, which is no worse than the resource access module underneath it (that one serves files without any credentials). **Handing user data to the API is blocked on porting the token endpoint**, and that is recorded as a Phase 2 dependency, not as a nice-to-have |

The gate asks a second URL now, with expected content, so both platforms prove a discovered controller and not
merely a mapped delegate: `http://127.0.0.1:55555/MediaPortal/Server/api/serverinfo` has to answer `200` and
contain `"pathBase":"/MediaPortal/Server"`. Six tests cover the endpoint - the answer's content, the answer
without the settings service, the settings read through the registration, the client guard, the server case, and
one that reaches the controller over a real socket through the shared listener with the resource server's own
registration (its assembly as an application part, `MapControllers` plus the conventional route next to the
attribute route, and an unknown controller still answering `404`).

| Measurement | Value |
| --- | --- |
| Controllers in the tree before this round | **0**, on either platform |
| `GET api/serverinfo` on Windows (`http.sys`) | `200`, `version 2.5.2305.14606`, `framework .NET 10.0.13`, `operatingSystem Microsoft Windows 10.0.26200`, served below `PathBase=/MediaPortal/Server` |
| `GET api/serverinfo` in the Linux container (shared listener) | `200`, same version, `operatingSystem Ubuntu 24.04.5 LTS` - the same controller, the same route, discovered the same way, on the platform without http.sys |
| Container log after the change | 0 `FATAL`, 0 `ERROR`, 1 `WARN` (the documented no-FUSE note) |
| Probes in the gate | 1 → **2**, each with expected content |
| Unit tests | 347 → **353** |
| Ratchet / spine | `486` findings = baseline, `Spine OK.` |

### n. Who decides where MVC looks for controllers

`ServerInfoController` was discovered by accident rather than by decision: `configureServices` added the assembly
of every registered *resource access module* as an MVC application part, and the first controller happened to sit
in the assembly that registers `ResourceAccessModule`, which is `MediaPortal.Common`. The author had left a
comment on that line saying it "is currently a hack because plugins that have controllers might not necessarily
register a middleware". One endpoint could live with the accident. The next one cannot:

| Fact | Consequence |
| --- | --- |
| `MediaPortal.Backend` references `MediaPortal.Common`, never the other way round, and the media database API (`IMediaLibrary`) is in Backend | A controller that serves the library cannot live next to `ServerInfoController`. It needs an assembly that the launchers and Backend can both reach - and an assembly like that registers no HTTP middleware at all, so under the old rule it would have been invisible |
| The old rule read "an assembly is searched for controllers if it registered middleware" | That rule says nothing about controllers. An assembly with controllers and no middleware was invisible; an assembly with middleware and no controllers cost an `AddApplicationPart` that did nothing |
| `RestartHttpServers` rebuilds the pipeline, and the shared listener refuses to add an endpoint that a running pipeline already holds | A controller assembly has to be declared *before* `Startup`. The interface says so; a call after startup rebuilds where the platform allows it and says what happened |

So the seam is now explicit: `IResourceServer.AddHttpControllerAssembly(Type)`, and a static
`ResourceServer.AddApplicationParts(mvc, moduleTypes, controllerTypes)` that holds the union rule so it can be
read in one place. `ApplicationCore.RegisterCoreServices()` declares the core's own controller assembly - under
`#if NET5_0_OR_GREATER`, because the `net48` target has neither MVC nor the controller - which means the core API
no longer depends on `ResourceAccessModule` happening to be registered.

Four tests in `Tests.Server\Common\TestResourceServerApiRegistration.cs`, all of them over a real socket through
the shared listener rather than against MVC's internals (the feature-provider types are not reachable from the
test assembly, and asserting on the HTTP answer is the assertion that matters anyway):

- A controller in a declared assembly answers `200`.
- The same controller's route answers `404` when its assembly was **not** declared. This negative control is what
  makes the seam mean something: it proves MVC does not sweep every loaded assembly, so declaring an assembly is
  the thing that moves a controller in and out.
- The old rule still holds: with modules registered and no controller declared explicitly, `/api/serverinfo` still
  answers `200`. Nothing that used to answer stopped answering.
- Argument validation, deduplication, and "declaring a controller while not started does not start the server".

Changing an interface is a tree-wide event: two stub implementations of `IResourceServer` in the test projects had
to grow the member (one records the declared assemblies, the other throws like every other member of that stub).
The build found both, which is why the gate builds the whole solution rather than the projects that were touched.

| Measurement | Value |
| --- | --- |
| `api/probe` with the controller's assembly declared / not declared | `200` / `404` |
| Unit tests | 353 → **357** |
| Windows live server (gate) | both probes with expected content, `Running` state, clean stop |
| Linux container, fresh image | exit `0`, both probes `200` with expected content, same `Running` state, clean stop - the seam is exercised on the shared listener too, not only on http.sys |
| Ratchet / spine / packages | `486` findings = baseline, `Spine OK.`, 0 vulnerable packages at `-FailOn Low` |

### o. The API moves into an assembly of its own, because the media library is on the other side of a reference

The second endpoint could not be written where the first one lived. That is the whole reason for this round, and
it is a fact about the tree rather than a preference:

| Fact | Consequence |
| --- | --- |
| `IMediaLibrary` - the API over the media database - is in `MediaPortal.Backend`, and the reference direction is Backend -> Common | A library controller cannot live in `MediaPortal.Common`, because that would mean the core referencing the database layer |
| `MediaPortal.Common` is also what the client, the plugins and the portable tests reference | The alternative - moving the library API down into the core - would drag the database layer into the client. Not an option either |
| An assembly holding only controllers registers no HTTP middleware, and the resource server used to decide "search this assembly?" from "did it register middleware?" | So the seam from item **n** had to exist first. It is now used by the thing it was built for |

`MediaPortal.Server.Api` is that assembly: `net10.0` with no platform suffix, `CA1416` as an error, references to
`MediaPortal.Common` and `MediaPortal.Backend`, and the shared `VersionInfo.cs` linked in - without it the
endpoint would report version `0.0.0.0` while the health endpoint next door reports the server's version, and the
gates compare those numbers. `ServerInfoController` moved into it, and `MediaPortalServerApi.Register()` is the
door: the headless host and the Windows server each call it while they register their services, which is the only
moment when naming an assembly to MVC is allowed. It cannot be called from `ApplicationCore` - that would be the
core referencing the API, the same forbidden arrow in the other direction - so the composition roots name it, which
is also where it belongs: which controllers a process serves is a property of the process.

| Measurement | Value |
| --- | --- |
| Controllers the launchers reach | still 1, now through `AddHttpControllerAssembly` rather than by accident |
| Unit tests | 357 → **359** (a test that the API names its own assembly to the registered resource server, and one that calling it in the wrong order says so in a sentence) |
| Deployment layout | 17 assemblies, `MediaPortal.Server.Api.dll` among them, without the layout script knowing its name - it publishes the headless host, and the host references the API |
| Spine | new area `MediaPortal.Server.Api` measured at WinForms 0, GDI+ 0, P/Invoke 0; in the core list although no core project references it, because the list is about target frameworks, not about the direction of an arrow |
| Ratchet | `486` findings = baseline |
| Linux container, fresh image | the new project restored, built and published inside the image, and both probes answered `200` with their expected content - the API assembly is portable by measurement, not by the absence of a `-windows` suffix |
| `GET api/serverinfo` on the live Windows server | `200` with the expected content, served from the new assembly - the existing gate probe became the end-to-end proof that the project, its registration and its deployment all work |

Two smaller things the round produced. The rule "the assembly of a registered middleware is searched too" had to
stay, because that is how plugin controllers reach the API today - but with the only controller gone from
`MediaPortal.Common` it was no longer testable against real types, so the fixture now carries a middleware and a
controller in one assembly and asserts both halves. And `build.ps1 -Solution Server` does not build the test
projects, which `dotnet test --no-build` revealed by running 4 tests where 12 existed: the gate is `tests.ps1
-Build`, and `--no-build` is only honest directly after it.

### p. The first endpoint that reads the media database

`GET api/library/aspects` describes what the media database is made of - the media item aspects, their attributes,
their CLR types, and which attributes are indexed or hold collections. `GET api/library/count?aspect=Core` answers
how many items carry a given aspect, taking aspect *names* on the wire and handing the ids it looked up to
`IMediaLibrary.CountMediaItems`, echoing both in the answer so a client can cache the id.

Those two were chosen for one reason: they are the only questions about the media library that are not questions
about the household. Titles, watch state, playlists and users all wait for the OAuth token endpoint (item **m**),
while the schema is the shape of the database and nobody's data. Three smaller decisions came out of writing them:

- **`503` and `404` instead of `0`.** A host without the backend answers `503` rather than "zero aspects", and an
  unknown aspect name answers `404` rather than "zero items". A client cannot tell an empty library from an absent
  one if both are `0`.
- **Ordered by name**, aspects and attributes both. Two answers from two servers should be diffable.
- **The flags are the point.** `Indexed` and `Collection` are in the answer because a front end decides with them
  whether a filter is cheap and whether an attribute is edited with a list control. They are also the two fields
  most likely to be mapped wrongly, so they are asserted by name.

Nine tests covered the endpoint against a mocked `IMediaLibrary` - mocked *strictly*, so an endpoint that decided to
reach for another member of that interface would fail rather than run an unexpected query - with the real
`MediaItemAspectMetadata` types as input, because the mapping is what is under test. All nine passed on the first
run. The live server then failed twice, and both failures were worth more than the tests:

| What was green | What the live server showed |
| --- | --- |
| The nine unit tests | `api/library/aspects` answered **`500`**, a `NullReferenceException` in the mapping. While `MIA_Management.AddMediaItemAspectStorage` creates an aspect's tables it keeps a **null placeholder** under that aspect's id and fills the metadata in afterwards - so a server asked about its schema *while it is starting* is asked about a dictionary with holes in it. The endpoint skips those (an aspect whose storage does not exist yet has no describable shape), and the fixture's dictionary has a hole in it now. A mock cannot contain a bug that only the real implementation has; this is the argument for running the real server in the gate, restated with a number. |
| The gate's content check, working for two probes | It threw `WildcardPatternException: "*""aspects"":[*"`. The expected text was compared with `-like`, and a fragment of JSON is full of characters that `-like` reads as wildcards. It compares literal substrings now, which is also the claim being made. |

| Measurement | Value |
| --- | --- |
| Endpoints the server answers | 2 → **3**, each with expected content in the gate |
| Unit tests | 359 → **371** |
| Fresh install, Linux container | `{"count":18,"aspects":[{"id":"e33242e8-…","name":"AlbumItem","systemAspect":false,…` - the same schema from the same SQL scripts on the platform without http.sys |
| Ratchet / spine / packages | `486` findings = baseline, `Spine OK.`, 0 vulnerable packages |

Those aspect counts are not a difference between the platforms. One Windows probe counted `10` aspects, container
probes counted `22` and `18` on two runs of the same image, and every log shows the same **25** aspect storages
being created - the probe asks while the server is still answering, so the number measures *when it asked*, not
what the database holds. That is why the gate asserts the endpoint by shape (`"aspects":[`) and never by a count.

Then a probe of a server that had been up for half a minute answered `500`, and following that thread turned up the
more important half of the endpoint:

| What was measured | What it turned out to be |
| --- | --- |
| `api/library/aspects` answered `500`, `NullReferenceException` in `MediaLibrary.GetManagedMediaItemAspectMetadata` - although the previous run had answered `200` | The request had arrived while the library was still starting. That method, and `CountMediaItems` with it, reached through `_miaManagement`, which `MediaLibrary.Startup()` only creates later, so a question that early died somewhere inside and the endpoint could report nothing but an internal error. The library now rejects such calls with the `IllegalCallException` it already uses for every other call made in the wrong state (the maintenance-mode checks throw it), and the endpoint answers **`503`** with the reason in the body. "Too early, ask again" stopped being "the server broke". Three tests cover it, one per call the endpoints make. |
| The same run's log: a `65` second gap between the HTTP listener coming up and the first database line | That run's own. Two reproductions of the same launch - with the console redirected and without - go from launch to the `Running` state in `1.4 s` and `1.7 s`, and the gate's runs are the same. The one thing that run had and the others did not is that it followed a server which had been killed rather than stopped, and the database's lock timeout is configured at `30000 ms`. |

A slow or stalled start is therefore no longer a lie from the API, but the gate had to be told about it too: the
probes now retry a `503` like a missing answer and fail on the answer that never changed. Without that rule the
gate would fail or pass depending on which side of a service-startup order the request happened to land, which
teaches nothing; a `500` still fails on the spot, because nothing is ever legitimately "not ready" with a `500`.

### r. The token endpoint: the point at which a request becomes somebody

Item **m** closed with a debt written into the API assembly: no endpoint could carry user data, because the
component that turns a request into a named identity did not exist on this target. `MediaPortalAuthorizeAttribute`
had been ported, but the thing it asks - "is this caller authenticated as *MediaPortal*?" - could only ever be
answered "no": the whole OAuth server sat inside the `#else` of `ResourceServer`, behind
`OAuthAuthorizationServerOptions` and `OAuthAuthorizationServerProvider` from
`Microsoft.Owin.Security.OAuth`, a .NET Framework library with no ASP.NET Core successor. Its access token was
protected by `System.Web.Security.MachineKey`, whose key comes from IIS or from `machineKey` in web.config, so not
even the token format survived the move.

Five pieces and two endpoints exist now, each of them small enough to read:

| Piece | What it decides, and why it is that shape |
| --- | --- |
| `MediaPortalAccessToken` | `base64url(json).base64url(hmac-sha256)` over the three claims MP2 has always put in an identity (name, profile id, role) plus issuer and expiry. The alternative was `DataProtection`, the .NET Core replacement for MachineKey: it works, but it keeps its key ring in a folder whose protection is platform specific, and it produces ciphertext nobody can read in a log or on another machine. A signed, plainly readable token is verifiable on Android with the same thirty lines, and a support case can decode one. Signature checked with `CryptographicOperations.FixedTimeEquals`; expiry takes an injectable clock so that "expired" is a test rather than a sleep. |
| `MediaPortalSigningKey` | 32 random bytes, base64 in `ServerSettings.WebAccessTokenSigningKey`, made on first use. Made rather than required, because a server that will not start until somebody has produced a random string does not start. A stored value that cannot be decoded is *replaced*, never hashed-into-something: hashing a corrupt value would turn a broken setting into a key anybody can compute, at the cost of logging every issued token out - which is the correct failure. |
| `MediaPortalCredentials` | The old `GrantResourceOwnerCredentials` callback with the Owin type taken away from around it, so that the decision "may this person in" is a function rather than a middleware delegate, and a test can call it with a profile store of its own. The wire format is unchanged (the client base64s the password), as are the two legacy rules it enforces: a profile with an empty stored password admits anyone who names it, and the administrator role goes to a profile that restricts nothing. Neither is a new decision; both are now written down where a change would have to argue with a test. |
| `MediaPortalBearerHandler` + `IMediaPortalAccessTokenProtector` | `Authorization: Bearer …` to `HttpContext.User`, with the authentication type the filter compares. The protector is an interface and a registered service so that a test hands the handler a key it knows and can produce an expired token on purpose. A header for another scheme is left alone, and a rejected token is logged without repeating it. |
| `TokenController` (`POST {prefix}/Token`), `SessionController` (`GET {prefix}/api/session`) | The OAuth password grant with the field names and error strings clients of the 2.3 series read (`access_token`, `token_type`, `expires_in`, `invalid_grant`, `unsupported_grant_type`), form-encoded as before and JSON as well, because a client written today sends JSON. The second one is the first endpoint behind the filter: it reports the caller and nobody else, which is the smallest thing a wrong answer about authentication can show up on. |

The pipeline is two lines - `services.AddMediaPortalTokenAuthentication()` and `app.UseAuthentication()` before
`app.UseAuthorization()` - and those two lines produced the one real bug this step found, which is worth
recording because no unit test could have found it:

| What was measured | What it turned out to be |
| --- | --- |
| `GET api/session` with no token answered **`401`**, where MP2's own filter says `403` | While `MediaPortalAuthorizeAttribute` derived from `AuthorizeAttribute` it was *also* an `IAuthorizeData`, so the endpoint carried the framework's own "caller must be authenticated" policy on top of MP2's rule. The framework runs that policy in `UseAuthorization()`, before any MVC filter, so it answered first. The status code is the small part: **`WebAutorizationEnabled=false` silently stopped working**, because the framework has never heard of that setting and refused the anonymous caller anyway. The attribute is an `IAuthorizationFilter` and nothing else now, which is what the .NET Framework branch always was - there, `AuthorizeAttribute.IsAuthorized` *is* the mechanism, so deriving from it was the port keeping a base class it no longer needed. |
| How a test tells those two refusals apart | The answer carries a `WWW-Authenticate` header when the framework challenged, and none when MediaPortal refused. The test asserts on the header rather than on a number that both paths produce. |

Neither of those shows up if the parts are tested separately: the filter is correct about a `ClaimsPrincipal` it
is handed, the handler is correct about a token it is given, and the order they run in is invisible to both. So
the 11 tests in `TestTokenEndpointOverHttp` start the resource server's *own* configuration -
`ResourceServer.ConfigureApiServices` and `ConfigureApiPipeline`, extracted from the listener setup for exactly
this reason, which is also what both HTTP hosts are handed - on `SharedHttpPortHost`, MP2's portable listener, on
a loopback port, and talk to it with `HttpClient`. That is the listener Linux and Android use, so the portable
path is the one being measured.

What is measured where, because it matters for what may be believed:

| Claim | Measured by |
| --- | --- |
| Token round trip, one character changed, another server's key, expiry, garbage that must not throw, errors that must not repeat the token; key generation, reuse, corrupt stored key, too-short stored key; every credential rule including which profile is an administrator; the handler with no header, another scheme, a good token, a broken one | 25 tests, no listener (`Tests.Server\Api\TestAccessToken.cs`) |
| Middleware order, endpoint shapes, form and JSON bodies, the two error answers being indistinguishable so the endpoint cannot enumerate profiles, `WebAutorizationEnabled=false` really opening the endpoint, `/health` staying anonymous beside a protected neighbour, nothing reachable outside the prefix | 11 tests over a socket on the portable host (`TestTokenEndpointOverHttp.cs`) |
| The same endpoints served by a server that booted a database, on http.sys | the new sixth assertion in `run-server-check.ps1`, which asks four questions and wants `403`, `403`, `400 unsupported_grant_type`, `400 invalid_grant` |

What is deliberately **not** measured by the live gate, stated rather than left out quietly: the live gate never
receives a token. A fresh data directory contains no user profiles at all - `USER_PROFILES` has `0` rows after a
first run, measured in the database the gate's own server writes - so on a fresh server there is nobody to log in
as; a real installation gets its first profile from the client's UI, through the UPnP `CreateProfile` action. The
gate takes `-AuthTokenUser` to walk the positive path against a data directory that does have profiles, and says
in its output which of the two it measured. The positive path itself is measured over a socket in the tests, on
the same two configuration methods.

Two things are left as decisions rather than done here. The existing endpoints - `api/serverinfo`,
`api/library/aspects` - stay anonymous, because with `WebAutorizationEnabled` defaulting to true, putting them
behind the token in the same change would break every client that has no token yet, the gates among them: that is
a change of defaults, and it should be argued as one. And nothing in the tree asks for a token yet - the front end
will, at the moment it shows a user their own data rather than the library's schema.

Numbers for this step: `Tests.Server` 184 → **220**, the suite 405 → **441**, the warning count unchanged at
**522** with the ratchet green, and the server gate now prints six assertions where it printed five.

## Phase 2 - Avalonia front end

### q. The front end exists, and it reads a running server

Started, not finished. What exists is deliberately the thinnest thing that can be *proven* rather than the
broadest thing that can be demoed: one screen, one API client, and gates that say whether the front end actually
talks to a real server on a machine with no display.

    MediaPortal\Source\Frontend\MediaPortal.Frontend   the application (net10.0, WinExe)
    MediaPortal\Tests\Tests.Frontend                   34 tests, headless, no display
    MediaPortal\Tests\Frontend.RenderProbe             the same window drawn with Skia, written out as a PNG
    MediaPortal\Source\MP2-Frontend.sln                these three plus what they reference
    scripts\ci\test-frontend-check.ps1                 the front end against a server that is really running

`MediaPortal.Frontend` references **no MP2 project at all**. That is not an oversight and not a style preference:
`MediaPortal.GUI.Library`, `MediaPortal.Common.Utils` and everything behind them drag WinForms and `System.Drawing`
in with them, and the whole point of the phase is a client that builds on Linux and Android. What it references is
Avalonia 12.1.4 (which carries a `net10.0` dependency group - verified against the feed before the project was
created, not assumed) and CommunityToolkit.Mvvm 8.4.2. `CA1416` is an error in this project rather than a ratchet
floor, because a platform warning here is not debt to be paid later - it is the end of the plan.

The application has a mode with no window, `--check`, and that mode is what makes the front end gateable:

```
server:   http://127.0.0.1:55555/MediaPortal/Server
health:   MediaPortal 2 Server 2.5.2305.14606 (pathBase=/MediaPortal/Server)
server:   MediaPortal 2 Server 2.5.2305.14606 - .NET 10.0.13, Microsoft Windows 10.0.26200/X64
library:  11 aspects
first:    AlbumItem with 13 attributes
items:    0 media items
the front end read a running server.
```

Exit codes are a contract, because a gate has to be able to fail: `0` read everything, `1` something refused,
`2` the URL given was not a URL, `3` the server answered `503` - i.e. the item **p** "ask again" answer, which is
the first consumer of that contract anywhere in the tree. `test-frontend-check.ps1` retries on `3` and fails
immediately on `1`, and it also runs the front end against a port nobody listens on and requires exit `1` with a
`FAILED:` line, because a check that cannot fail is not a check.

| Gate | Result |
| --- | --- |
| `build.ps1 -Solution Frontend -Ratchet` | `BUILD OK (Frontend): 0 errors, 238 findings` → scored against a baseline of its own, `Ratchet OK.` |
| `warning-baseline.Frontend.json` | 18 codes / 237 findings, **0 of them from the two new projects** - the inventory belongs to the inherited closure the solution also builds |
| `spine.ps1` | `MediaPortal\Source\Frontend\MediaPortal.Frontend  WinForms 0   GDI+ 0   P/Invoke 0` and it is now a gated core area, so a future `[DllImport]` there is caught |
| `tests.ps1` | `Tests.Frontend` added to the default set (see the scoreboard) |
| `test-frontend-check.ps1` | `FRONTEND CHECK OK - the front end read a running server (attempt 1) and refused a dead one.`, and while that server is still up it renders the populated screen with Skia: `pixels: 921600 (Rgba8888), 2592 distinct colours, 234687 differing from the background, 65736 high-contrast edges, mean luminance 31` |
| `Frontend.RenderProbe` (Windows, populated) | `PROBE OK: the front end drew 921600 pixels of real content on Microsoft Windows 10.0.26200`, `status: 25 aspects on MediaPortal 2 Server, 25 aspect row(s)`, `home-windows.png` 107 757 bytes |
| `Frontend.RenderProbe` (Linux container, `-ServeFromHost`) | `PROBE OK: the front end drew 921600 pixels of real content on Ubuntu 24.04.5 LTS`, `status: 25 aspects on MediaPortal 2 Server, 25 aspect row(s)`, `3872 distinct colours`, `home-linux.png` 119 642 bytes - the container read the Windows host's server through `host.docker.internal`, measured answering `HTTP/1.1 200 OK` / `Server: Microsoft-HTTPAPI/2.0` from inside the image |
| `test-frontend-linux.ps1` | `Passed! - Failed: 0, Passed: 34, Skipped: 0, Total: 34, Duration: 2 s`, then `FRONTEND LINUX OK: 34 tests ran headless on Linux x86_64, populated from http://host.docker.internal:55555/MediaPortal/Server screenshot in /out/home-linux.png` - on `Linux 6.6.87.2-microsoft-standard-WSL2 x86_64, dotnet 10.0.402`, with no display in the image and no `-p:EnableWindowsTargeting` passed |

Two things about that gate are worth stating because they were not obvious when the script was written. It asserts
the *shape* of the answer (`pathBase=/MediaPortal/Server`, `library:`, `aspects`) and never a count: this run
printed `11` aspects, and item **p** has recorded `10`, `18` and `22` from the same tree. And the server's console
is left unredirected while the front end's is captured - the front end prints a dozen short lines that are the
evidence, the server prints thousands that are not.

The same suite then ran on Linux (`Dockerfile.frontend` + `test-frontend-linux.ps1`): the real `HomeWindow.axaml`
built by the XAML compiler, the Fluent theme and Inter font loaded, the D-pad handler driving focus, and the API
client talking to real `MediaPortal.Server.Api` controllers on Kestrel - 32 tests, 0 failed, no display, no X
server installed. Two properties of that image are the gate rather than packaging. It does **not** pass
`-p:EnableWindowsTargeting=true`: `Dockerfile.cross` needs it for the `-windows` projects still in
`MP2-Server.sln`, nothing in the front end's closure may need it, so the first `-windows` reference from this side
answers `NETSDK1100` instead of being quietly absorbed. And its `CMD` fails when the test output does not report at
least one test, because `dotnet test` exits `0` when it discovers nothing - the same false green that "a green test
suite can be a stale one" records for Windows.

### Pixels: the front end draws, and the drawing found a crash

The suite runs on Avalonia's pure-managed headless rasterizer (`UseHeadlessDrawing = true`). That is the right
choice for 34 assertions about layout, bindings and focus - it is fast and needs nothing from the host - and it is
also a blind spot: that path executes no Skia, never shapes a glyph through a font manager, and never encodes an
image. "The tests pass" was therefore not "it draws", and nothing in the tree could say it draws.

`MediaPortal\Tests\Frontend.RenderProbe` is the answer to that: the same `App`, the same `HomeWindow`, the headless
platform with its stub render interface switched *off* and `.UseSkia()` on, then
`CaptureRenderedFrame()`, a pixel census, and a PNG. It prints what a gate can judge, because a gate cannot look
at a picture:

```
host:     .NET 10.0.13, Microsoft Windows 10.0.26200, X64
skia:     Avalonia.Skia 12.1.4.0
content:  populated from http://127.0.0.1:55555/MediaPortal/Server/
status:   25 aspects on MediaPortal 2 Server, 25 aspect row(s)
frame:    1280, 720 at 96, 96, implementation Avalonia.Media.Imaging.WriteableBitmap
pixels:   921600 (Rgba8888), 2592 distinct colours, 234687 differing from the background, 65736 high-contrast edges, mean luminance 31
```

It runs in two gates, and both render the *populated* screen. `test-frontend-check.ps1` runs it **while its own
server is still up**: `25 aspects`, 2592 distinct colours, 65 736 high-contrast edges, i.e. rows of text, not a
gradient. `test-frontend-linux.ps1 -ServeFromHost` starts `MP2-Server-Headless` on the Windows host, waits until it
really serves aspects, and tells the container `MP2_SERVER=http://host.docker.internal:55555/MediaPortal/Server`;
measured from inside the image, `host.docker.internal` resolves and the server answers `HTTP/1.1 200 OK` with
`Server: Microsoft-HTTPAPI/2.0`, while `172.17.0.1` does not connect at all. The frame the container writes is then
the same screen, drawn by Linux: `25 aspects on MediaPortal 2 Server`, **3872 distinct colours**, 119 642 bytes of
PNG. Both frames are committed next to this document - `home-windows-skia.png` (107 757 bytes) and
`home-linux-skia.png` (119 642 bytes). Without `-ServeFromHost` the image still renders, and the OK line then says
plainly that the frame is the screen's own "no server" state.

Whether the screen looks *right* is a different question and stays with the user. The probe's job is only to prove
that real pixels come out, on both platforms, unattended - and that it cannot lie, `--server`/`MP2_SERVER` now
exits **4** when a populated screen was asked for and the server answered no aspects: an error screen drawn against
a URL is worthless as evidence, and exit 0 for it would leave a gate no wiser. The image additionally refuses a run
whose output never contains `aspects on`, so neither the container nor the host script can report the offline or
the error picture as the data picture.

What it found the first time it was run is the reason this section exists. The window's `Opened` handler runs the
same command the refresh button runs, so the probe's render overlapped that first read - and the process died with
`System.ObjectDisposedException: The CancellationTokenSource has been disposed.`, unhandled, on the dispatcher.
`HomeViewModel.RefreshAsync` had been disposing the *previous* run's cancellation source while that run was still
between awaits; the next time that run read `Token` it threw. Nobody noticed for as long as the screen was only
tested by awaiting `RefreshAsync()` from one test at a time. Three changes, each with a test:

- the older source is only ever **cancelled**; every run disposes its own in its own `finally`, and
  `_runningRefresh` is assigned before the cancel so a finishing run cannot clear `IsBusy` on a newer run's behalf;
- one run at a time may touch `Aspects`. Cancelling is not enough for that - a cancelled run carries on until its
  next check, and until then it is clearing and adding to the very collection the newer run is writing. The gate is
  a chain of `Task`s rather than a `SemaphoreSlim`, deliberately: a task needs no disposal, so a view model that is
  thrown away cannot hand `ObjectDisposedException` to a run still waiting on it, which is this class's second
  encounter with the same family of bug;
- the command path is covered too. `CommunityToolkit.Mvvm`'s `Execute()` funnels the task through an `async void`
  continuation that **rethrows whatever escapes** on the thread that pressed the button; `await`ing the method in a
  test never sees it, which is exactly why the crash was invisible. `PressingTheRefreshButtonTwiceDoesNotThrowOnTheDispatcher`
  presses the command twice and awaits `ExecutionTask`, and `RefreshAsync` now ends in a last-resort `catch` that
  reports through the status line - a screen that says "Reading failed: …" is worth more than a front end that dies.

Two more measurements from wiring it up, both of which would have been guesses without the probe:

| What was measured | What it turned out to be |
| --- | --- |
| The Skia render path on Linux | It did not work: `TypeInitializationException` from `SkiaSharp.SKImageInfo`. The native `libSkiaSharp.so` needs `fontconfig` and `freetype`, and the image had neither - so the first container run of the probe failed while the 34 managed tests next to it were green. Both packages are now installed in `Dockerfile.frontend`, with the failure recorded in the comment above them. This is the concrete cost of "the tests use the managed rasterizer": a missing system library on a target platform is invisible to them. |
| A gate running a binary nobody rebuilt | The fix above appeared not to work: the check gate still crashed, because only the test project had been rebuilt and the probe kept executing the copy of `MediaPortal.Frontend.dll` in its own `bin`. `test-frontend-check.ps1` now refuses to run when a `.cs`/`.axaml`/`.csproj` is newer than the DLL it is about to execute (`STALE`-style, same reasoning as `tests.ps1`), because a gate that reports somebody else's binary is worse than no gate. |

`RenderProbe` prints the frame's format (`Rgba8888` under Skia, where the managed path is `Bgra8888`), refuses a
frame that is flat (below 16 distinct colours, or under 1% of pixels differing from the background), and reports
`Avalonia.Media.Imaging.WriteableBitmap` as the implementation type - which is only the public wrapper, so the
probe deliberately does not claim to have proven a native surface from that name alone.

| What was measured | What it turned out to be |
| --- | --- |
| A home screen that compiles, binds and shows an empty list | Nothing asked it to read the server: `RefreshCommand` was wired to the button only. A screen that waits for a press in front of a server that is answering perfectly is worse than a screen that says it cannot reach it, so opening the screen now runs the same command the button runs. Found by the test that shows the real XAML, not by reading the code. |
| `list.Focusable` on that screen | **`False`**. The remote's arrow keys could therefore never enter the list - and worse, the ring had already marked the key handled, so nothing else could answer it either. Two fixes: the list says `Focusable="True"` on the screen, and `DpadNavigation` claims a key only when `Focus()` returned true. The test for the second one builds a control that cannot take focus and requires the key to stay unhandled. |
| The `503` contract, exercised by a client for the first time | It arrived as a `500`. `NotReady` reported itself through `ServiceRegistration.Get<ILogger>()`, which *demands* a logger; a host without one threw from inside the `catch`, and the exception the caller could have acted on became one it could not. It asks for the logger without demanding one (`Get<ILogger>(false)`), which changes nothing on the real server, where one is always registered. A contract that depends on something else working is not a contract. |
| How long a front end waits before it says "no answer" | `HttpClient`'s default: **100 seconds**. On a screen that is not patience but a hang, so the shipped client has a 15 second bound of its own (`ServerApiClient.DefaultTimeout`), readable through `Timeout` so that a test can state it instead of a reader having to trust it. |


### What the sequence looks like now

The headless spike is not a spike any more: the ten-foot navigation model exists (`DpadNavigation`, ten tests),
it runs with no display, the screen draws on the real backend on two platforms, and the CI story is five gates
rather than an intention - the gated build, the suite on Windows, the front end against a live server, that
server's screen rendered through Skia, and the suite plus a render in a Linux container. What is still ahead, in the
order that keeps the risk where it can be seen:

1. Pixels, not only layout - **done, on both platforms, with data on screen**. `Frontend.RenderProbe` renders the
   real window through Skia; the Windows frame comes from the server the check gate starts, and the container's
   frame comes from the host's server through `host.docker.internal` (`-ServeFromHost`), so both committed frames
   in `docs\multiplatform\screenshots\` show 25 aspect rows. The phase gate's named screenshot exists and is
   populated; item **q** records what the render found. What is left of this item is not measurable here: whether
   the screen looks *right* (the taste review reserved for the user), and Android, which still has no measurement
   at all - including no Skia render, because nothing has drawn the front end on that platform yet.
2. The OAuth token endpoint on .NET - **done**, item **r**: token format, signing key, credential check, bearer
   handler, `POST Token` and the first endpoint behind the filter, with the middleware order itself under test over
   a socket rather than assumed. Two things follow from it and are deliberately not decided here: putting the
   existing anonymous endpoints behind a token (a change of defaults, and the gates are clients too), and the front
   end asking for a token at the point where it shows a user their own data.
3. Bind the platform-neutral pieces of the old client's view models. Anything that imported
   `MediaPortal.GUI.Library` was a View, not a ViewModel, and gets rewritten rather than ported.
4. The renderer per OS: DirectX on Windows, Metal on macOS/iOS, Vulkan/Skia on Linux/Android, each proven by a
   probe of its own, none assumed.
5. Skin engine replacement. The skin XML model gets no Avalonia equivalent; the migration story for users is
   that the new UI ships its own themes.

Gate: the client stops being WinForms, the satellite list in `spine.ps1` shrinks, the front end's suite runs in
the Linux container as well as on Windows, and a screenshot from that container is in the commit that closes the
phase.

## Phase 3 - player engine

DirectShow and its filters are Windows-only, not buildable here (no C++ toolchain) and unmaintained.
LibVLC or MPV via .NET bindings, video surface embedded in an Avalonia control, hardware decode through
the player (DXVA2 / VAAPI / VideoToolbox) rather than through MP2. `EVRPresenter`, EVR and DirectVobSub
die here. Gate: the same file plays on Windows and on Linux through the same code path, and the deleted
projects show up in `spine.ps1` / `packages.ps1`.

### s. The player seam: one file, playing, on both platforms

Phase 3 was started before Phase 2's loose ends because of what the machine said, not what the plan said:
`dotnet workload list` reports no Android workload (so the Android items of Phase 1 cannot be measured at
all), there is no mpv anywhere on this host, and there *is* a working VLC - `C:\Program Files\VideoLAN\VLC`
with `libvlc.dll` at FileVersion 3.0.20 and a populated `plugins` tree. Decoding, clocking and track
handling are therefore measurable today, on a real engine, from a unit test. Rendering into a window is not,
and this section says which half is which.

**What exists.** `MediaPortal\Source\Frontend\MediaPortal.Frontend.Player` - seven files, `net10.0`, and
deliberately no reference to anything else in the tree. `IVideoPlayer` is the seam: state (`Idle`, `Opening`,
`Playing`, `Paused`, `Stopped`, `Ended`, `Failed`), `Open`/`Play`/`Pause`/`Stop`, `LengthInMs`/`TimeInMs`,
volume and mute, track lists and track selection, `AttachTo(nint)` for the host that owns a window, and three
events. `LibVlcPlayer` is the only file in the repository that is allowed to know the name `LibVLCSharp`.
`MediaLocation` decides what may be played at all (absolute paths, and URLs in eight schemes, with credentials
stripped by `Describe()` so a log line cannot leak a password). `NativeLibVlc` owns "where is the engine".
The assembly holds no Avalonia reference because the picture goes to whoever hosts it, and a player that
depends on a UI toolkit is a player that cannot be tested headless or used by a second front end.

**Found, never vendored.** libVLC is a GPL core plus a plugin per decoder, demuxer and output - a few hundred
files. It arrives installed, the way VLC always arrives. LibVLCSharp itself (MPL-2.0) is an ordinary
`PackageReference`; `packages.ps1` lists no advisory for it or its closure. Lookup order: an explicit
directory from the caller, then `MP2_LIBVLC`, then the platform's usual folders, then - only where a loader
can search - no directory at all. Every refusal is a sentence about what was looked at, because a TV set with
a `DllNotFoundException` is a crash with no explanation.

**Six things the engine taught by measurement.** Each of these was a test failing first:

- `Length` and `Time` answer **-1** for "unknown", so `LengthInMs`/`TimeInMs` clamp to 0. The interface
  promises 0; a screen that prints `-1` ms renders "-00:01" where there is nothing loaded.
- Volume and mute **cannot be read back**. With `--aout=dummy` the getter answers 0 whatever was set; with a
  real audio output it answers *one command behind* (set 42 while 100 was current, read 100). The player
  therefore stores what the caller asked for and pushes it to the engine - the alternative is a volume slider
  that visibly refuses to move.
- libVLC fires **`Opening` (and `Buffering`) *after* `Playing`**, shortly before the end; the observed sequence
  on a short file was `Opening, Playing, Opening, Ended`. Publishing that second `Opening` would make every
  track change flash a spinner, so `PublishFromOpeningEvent()` drops those two events while playing.
- A headless run is **not paced to wall clock**: a 4000 ms WAV reached `Ended` after 3040 ms, and after
  **1957 ms with `--clock-synchro=1`** - so that flag is not the fix and nothing in the tests compares a
  position with a stopwatch, only with other positions.
- After a second `Open()` on the same player, `Length` is **0 for a few milliseconds** while the new media is
  parsed. `OnePlayerPlaysSeveralItemsInSequence` waits for the length instead of reading it once.
- `Dispose()` raises a native `Stopped`, which would arrive as an event *after* the object is gone; `Publish()`
  returns early once disposed.

**The Linux finding, which is the reason this section is in the roadmap.** The first container run reported
`Failed: 16, Passed: 50`, and every one of the sixteen said the same thing: `Using libvlcDirectoryPath is not
supported on the Linux platform. The recommended way is to have the libvlc librairies in /usr/lib.` Two bugs
were in that message. The first was mine: `TryCreate` catches `VLCException`, `DllNotFoundException` and
`EntryPointNotFoundException`, but not `InvalidOperationException`, so a refusal escaped as an exception
instead of answering with a sentence as the seam promises. The second was the design: telling the binding
where the engine is only works on Windows. On the other platforms the folder is honoured one level lower,
with a `NativeLibrary` resolver registered against LibVLCSharp's own assembly that answers the request for
`libvlc` with the file the locator actually found. That resolver is not a nicety: distribution packages ship
the versioned `libvlc.so.5` and leave the unversioned `libvlc.so` to a `-dev` package, so a plain loader
search finds nothing on a machine that plays every other video file. Without it the player would require
`apt install libvlc-dev` to watch a film.

**The image says what it needs.** Ubuntu noble has no `vlc-nox` (the first draft of the Dockerfile named it
and apt failed the build); the engine is `libvlc5` and the plugins come from `vlc-plugin-base`, and they land
in the multiarch directory `/usr/lib/x86_64-linux-gnu/vlc/plugins`, not in `/usr/lib/vlc/plugins`, which is
empty. The build now asserts both halves - a `libvlc.so*` somewhere and a non-empty `demux` plugin directory -
because an image with the library and no demuxers is worse than no engine: libVLC starts, plays nothing, and
the tests fail as timeouts. The run also refuses while any test is skipped, which is what makes the number
trustworthy: on the engine tests a skip means "not measured", not "fine".

**Measured where.** Windows: `Bestanden! - Fehler: 0, erfolgreich: 66, uebersprungen: 0, gesamt: 66`, of
which 32 are the player's - 16 (`TestPlayerContract`) that need no engine and run everywhere, and 16
(`TestPlayerEngine`) that drive VLC 3.0.20 for real: it names the native library it loaded, a generated WAV
plays and is as long as its header says, the clock runs, seeking to the end brings the end nearer, running out
is announced once, stopping is not the same as running out, pause holds and play resumes, one player plays
several items in sequence, a missing file fails with a sentence, pausing an idle player is refused with a
reason, volume is clamped rather than refused, tracks are listed without the engine's "off" entry, two players
can exist at once, and a disposed player refuses work instead of throwing. Linux: the same 66 in the
container, `Passed! - Failed: 0, Passed: 66, Skipped: 0, Total: 66` on `Linux 6.6.87.2-microsoft-standard-WSL2
x86_64, dotnet 10.0.402`, with the distribution's libVLC loaded through the resolver - the phase gate's
"same file, same code path, both platforms", for everything a headless host can observe. Together with the
other four suites the Windows ratchet is at **473 tests, 0 skipped**.

**What is not here yet.** The picture: `AttachTo` accepts a window only on Windows, because Linux wants an
X11/Wayland window and Android a `Surface`, and this assembly cannot validate either from here - it reports
false rather than accepting a handle it cannot draw into. No front-end screen uses the player yet, which also
means nothing asks the server for a token in order to show its own videos. No mpv back end. Hardware decode
(DXVA2 / VAAPI / VideoToolbox), audio passthrough and HDR are the engine's business and unverifiable in a
container with no GPU - they stay on the hand-off list at the end of this document. And the second half of the
phase gate - the DirectShow projects disappearing from `spine.ps1` and `packages.ps1` - is untouched: the
player exists alongside the old code, it has not replaced it in the client yet.

### t. The player meets a screen: transport, position, and a frame of both on both platforms

The seam of section `s.` could play, but nothing asked it to. This section is the first consumer: a playback
screen a viewer can drive from a remote, and the front end's own startup asking the player whether the machine
can play at all.

**What exists.** `NowPlayingViewModel` (front end, `ViewModels\`) is the whole distance between a screen and
the player: title, state, `PositionText`, `StatusLine`, a `Progress` fraction, volume, mute, and
`Play`/`TogglePause`/`Stop`/`Skip`/`SetVolume`/`ToggleMute`. It is the first class in the front end that owns a
player, and the first thing in the tree that has to obey the seam's threading note: every engine event goes
through a marshaller (`Dispatcher.UIThread.Post` in a window), while everything the screen itself triggered runs
inline. That distinction was not invented - three bugs in the first version came from getting it wrong, each
caught by its own test: a refusal became invisible until the next pump, mute lost the state the player was built
with, and a synchronous first `Playing` event painted itself next to the *previous* media's name. `Skip` clamps
to the media rather than running past it, and a stream with no length is refused with a sentence.

**Bindings are not decoration.** The view model notifies through `INotifyPropertyChanged` with one fan-out per
change (a `null` property name means "read all of them again"). Without it the tests pass - they read properties
directly - while a viewer watches a screen that never updates, which is why
`APropertyThatMovedAnnouncesItselfSoThatAScreenRedrawsIt` asserts the notification itself and
`TheTransportButtonsReachThePlayer` asserts the *button's* text rather than the view model's.

**The screen.** `Views\NowPlayingWindow.axaml` follows the home screen's conventions (1280x720 maximised, the
same background, `Focusable` stated, the ring declared in code): name of the media, state, the picture area,
`PositionText` with a bar, and eight transport buttons. Left and right are taken by the window *before* the ring
sees them - they seek by thirty seconds, because a viewer who presses right mid-film must not watch the
highlight move - while up and down still walk the row, and the ring wraps so that nobody is stuck at its end.
Closing the screen stops nothing: the playback belongs to the application (`App` builds the view model with the
dispatcher as marshaller and releases it at `Exit`), which is what a viewer expects from a media centre when the
music carries on into the library. The home screen offers a `Now playing` button only when the application
managed to build a player; a machine without libVLC is not offered a door to a room with nothing in it.

**The program asks, and prints the answer.** `Main` builds one player for the application and prints the
engine's name or the one-sentence refusal. `--check` gained the same question, headless and before anything
touches the network, so the difference between a Windows box with VLC and a container without it is a line in a
gate log rather than a guess: `player:   libVLC 3.0.20 Vetinari`. A missing player does not change the exit
code - browsing a library without playing a film is still a working front end.

**The picture area is empty, on purpose.** Nothing in this build draws video into Avalonia's own control tree;
the seam's `AttachTo(nint)` exists and is tested, but no screen hands it a handle, because passing the top-level
window would put the player's surface into a layering fight with the surface Avalonia draws into, and a
headless render could not tell success from failure either way. An empty black frame that says what the player
is doing is the truth. Embedding the surface - a child native window on Windows, the platform's equivalent
elsewhere - is the next open item of this phase, and it needs a host with a display to be believed.

**Measured where.** Windows: `Bestanden! - Fehler: 0, erfolgreich: 99, uebersprungen: 0, gesamt: 99` in
`Tests.Frontend` (66 before this section), and **506 tests across all five suites, 0 skipped**, with the
Frontend ratchet still at its floor of **237** findings - none of this code added a warning. Linux: the
container reports `Passed! - Failed: 0, Passed: 99, Skipped: 0, Total: 99` on Ubuntu 24.04 x86_64, which
includes the same position events coming out of the distribution's libVLC.

**The container found a race that Windows wins.** `TheScreenFillsItselfInFromWhatTheServerSent` asserted the
selected aspect's count as `17` straight after the aspects appeared, and on Linux the frame said `?`: the count
is a second request per aspect, and the request had not answered yet. Same code, same in-process server, a
different scheduler. The test now waits for the count, which is the same claim made at a moment when it can be
true - and a gate on one platform alone would have kept saying nothing about it.

**Frames, both platforms, both screens.** `Frontend.RenderProbe` takes `--screen nowplaying` and drives the
playback screen from a scripted player, so that the frame means the same thing everywhere (a real decoder would
have been at a different moment on each host, or at none). The Windows gate and the container each write three
PNGs now - `home-<os>.png`, `nowplaying-<os>.png` and, since section **u**, `connect-<os>.png`, the screen with
nothing found yet. The playback frame measured 1488 distinct colours and 327 691 high-contrast edges on Windows
and 2282 / 327 711 in the container, both with `status: Playing, 0:45 / 1:30` - the clock and the half-filled bar
drawn by Skia, on both platforms, from the same code. Those counts are an order of magnitude above the frame this
screen produced before section **v**, for the reason that the frame now contains a *picture*: the probe pushes
eight colour bars through `VideoFrameSink`, and `CheckBars` finds them in the captured pixels in the order and the
colours they were pushed in, which is what turns "a frame counter moved" into "these pixels were drawn".

**What is not here yet.** Anything that would tell the player *what* to play: the API still has no endpoint that
answers "what can I watch", the sample database holds 0 items, and the front end has not asked `/Token` for
anything - so the playback screen opens showing "Nothing playing" until the server side catches up. No mpv back
end, and the DirectShow projects are still in the tree.

### u. The viewer picks a server from a list; typing an address is the fallback

**The requirement, in the user's words.** "The servers shall be autodetected and listed, so user can pick one.
Manual IP address entry shall be only a backup. Also earlier it was handled like that." That last sentence is
accurate and it was the accusation: the front end up to this point opened a text box and asked for a URL, which
is the way the old MP2 setup wizard only ever worked when discovery had failed.

**The announcement was already in the product.** The server multicasts itself: the log line
`UPnP server: HTTP listener started on addresses http://+:55555/MediaPortal/UPnPServer_A1E50389` is the same
listener that answers SSDP, the device type is
`urn:schemas-team-mediaportal-com:device:MP2-Server:1` (`UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE`, versioned
into the URN by `DvDevice.DeviceTypeVersion_URN`), and `SSDPServerController` replies to a device-type search as
well as to `ssdp:all`. Nothing on the server side had to change. What was missing was somebody on the other
side of the LAN asking.

**Why the front end writes its own SSDP rather than reusing `UPnP.csproj`.** Two independent walls: that project
carries `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`, which does not exist for `net10.0-android`,
and the MP2 glue around it lives in `MediaPortal.Common`, which the front end's boundary rule forbids (the only
project reference allowed is `MediaPortal.Frontend.Player`). So the front end speaks the protocol itself: four
files in `MediaPortal\Source\Frontend\MediaPortal.Frontend\Discovery\`. `SsdpAnnouncement` holds the protocol
strings and a parser that cannot throw; `SsdpSearchClient` owns one real IPv4 UDP socket - multicast with the
ST-tTL, and unicast targets too - and reports `SendFailures` instead of throwing, because "the network is quiet"
and "I could not ask" are different sentences on a screen. `ServerLocator` probes what came back, and
`DetectedServer` is what the list shows. The two duplicated constant strings are pinned by
`TheSearchTargetIsTheDeviceTypeTheServerPublishes`, which compares them against
`MediaPortal.Common.UPnP.UPnPTypesAndIds` reached through the API assembly: a server-side rename breaks a test
instead of quietly ending discovery.

**One rule holds the whole thing together: nothing is listed unless its own HTTP API answered.** An SSDP
announcement is a claim; the locator asks `/health` and `api/serverinfo` of the address behind it and lists the
server only if it replies, so every entry a viewer can select is an entry that will work when the front end
dials it. The answer is also used to fix the guess: the announcement's `LOCATION` is
`http://172.20.224.1:55555/MediaPortal/UPnPServer_A1E50389/upnphost/description/uuid:185c...` - a description
document path, not the API - so the API URL is built from host and port plus `/MediaPortal/Server/`, and if
`/health` reports a different `pathBase` the locator takes the server's word over the front end's guess
(`CorrectedBaseUrl`). A sender's IP is never the address: the announcement that the CI gate receives arrives
from `127.0.0.1` and still points at `172.20.224.1`, which is the only address a client on another machine
could use.

**The screen does what the requirement says.** Detected servers first, `Search again` under them, and the
address field below both, labelled as the fallback rather than hidden. `Left`/`Right` leave the list, `Up`/`Down`
walk it, `Enter` on an entry saves it and connects. When exactly one server is found it is put in the field -
unless the viewer is already typing, which is a fix the first run of the new tests forced: overwriting input as
it is being typed is the kind of cleverness users forgive only once. Nothing is saved from an announcement alone.

**The address survives a restart.** `ServerSettings` writes the last used server where the platform expects a
per-user config file; `--server` overrides it for one run and says so; `MP2_SETTINGS_FILE` points the whole
thing at another file, which is how the tests and the gates stay away from a developer's real configuration and
from each other.

**Measured.** `Tests.Frontend` went from 99 to **155** tests (`Bestanden! - Fehler: 0, erfolgreich: 155,
uebersprungen: 0, gesamt: 155`) - the additions are the settings file, the discovery layer and the new screen,
among them a `FakeUpnpServer` that is a real UDP device on loopback, so the announcement really is sent,
received, parsed, probed and listed. The same suite in the Linux container reports
`Passed! - Failed: 0, Passed: 155, Skipped: 0, Total: 155`, all five Windows suites together report
`All suites passed. 562 tests executed`, and the Frontend warning ratchet is still at its floor of **237**
findings - none of this code added a warning.
The new gate `scripts\ci\test-frontend-discovery.ps1` deploys a server layout, starts it, waits for
`UPnP server online hosting`, and runs the *front end binary* with `--discover`:

    search:   urn:schemas-team-mediaportal-com:device:MP2-Server:1
    listening: 6s on udp/1900
    device:   http://172.20.224.1:55555/MediaPortal/UPnPServer_A1E50389/upnphost/description/uuid:185c1727-...
    server:   MediaPortal 2 Server 2.5.2305.14606 at http://172.20.224.1:55555/MediaPortal/Server/
    answered: 1 message(s), 1 device(s), 1 MediaPortal server(s)

That is the front end finding a server it had never been told about, and then confirming it by dialling it.
`ForNetworkSearch` unicasts the request at this machine in addition to multicasting it, which is what makes the
gate deterministic: containers, VPN adapters and "public network" profiles drop multicast, and a gate that
depended on multicast would fail for reasons that have nothing to do with the code.

**Three things the gates broke first, all worth knowing.** `FakeUpnpServer` was disposed twice (once by a
`using`, once by the fixture's sweep) and the throwing `TearDown` then took the whole fixture down: 17 tests
reported failures that did not exist. `Dispose()` is idempotent now and `TearDown` collects, clears, and fails
at the end. Separately, the new gate script redirected both child streams and read them one after the other,
which deadlocks as soon as the child fills the pipe nobody is reading - the server looked hung and kept holding
port 55555; both streams are drained concurrently now. And `test-frontend-check.ps1` lost a race it had always
been winning: it started the server, checked that the process was alive, and immediately ran the front end -
which correctly refused a port that http.sys had not registered yet, in the few hundred milliseconds between
those two events. Refusing is the behaviour the gate depends on, so the waiting moved into the gate, which
polls `/health` until *any* HTTP answer arrives (a `503` counts: the prefix is live) before letting the front
end ask its serious questions.

**And the container caught a claim that was not true on the platform it ran on.**
`TheTestedAddressIsWhatTheNextStartReads` asks a fair question: after the viewer picks a server and saves it,
where does a start with no argument go? On Windows it went to the address just chosen. In the container it went
to `http://host.docker.internal:55555/MediaPortal/Server` - because the image exports `MP2_SERVER` so that its
own render step draws a populated screen, and the resolution order is argument, environment, saved, default, so
the environment won. It won *correctly*; what was wrong was the test, which had been asserting a three-input
rule with a fourth input silently present in some environments. The fix was not to make the assertion pass but
to put the environment in the signature - `ResolveBaseUrl(argument, environment, saved)` - which then let the
whole precedence table be stated as a pure function, including the rule that an operator's environment outranks
a decision somebody took on the screen months ago. That rule had never been tested, because setting a
process-wide environment variable to find out would have changed the answer for every other test in the run.
`Tests.Frontend` is **155** on both platforms after this.

**The third frame.** The render probe gained `--screen connect`, and both front-end gates draw it, so the screen
that exists to work with *nothing* is also proven to draw on both platforms: `status: Nothing has been saved
yet. Search the network, or type the address of the MediaPortal server., 0 server(s) listed, saving is not
offered yet`, 2128 distinct colours and 28 185 high-contrast edges on Windows, 2919 / 28 429 in the container
(the colour counts differ between platforms because text antialiasing does; the structure of the frame is the
claim, and the search is switched off inside the probe so that nobody's LAN changes the picture). Both frames
are committed as `connect-windows-skia.png` and `connect-linux-skia.png`.

**What is not here yet.** The front end answers searches only when asked; it does not keep a listening socket
open for the server's periodic `NOTIFY`, so a server that starts after the front end appears on the next
`Search again` rather than by itself. Android needs the multicast lock (`WifiManager.MulticastLock`) before a
WiFi search can hear anything - the code path is platform-neutral, the lock is not, and it stays untested until
there is a device. The server picked is trusted: there is no pairing, PIN or certificate involved, which is the
same trust model the LAN API has always had. And the container cannot run this gate - a Docker guest has no
route to a host multicast group - so discovery is gated on Windows while the Linux image keeps the same unit
tests and renders the connect screen with the search switched off (`connect-linux.png`), which is the frame a
viewer sees before there is anything to show.

### v. The film, on the screen, of a film somebody owns

**The claim, and how it is measured.** `MediaPortal.Frontend.exe --play "<file>" [seconds]` opens a real file in
the libVLC player, hands its decoded frames to `VideoFrameSink`, and draws them in the Avalonia window behind the
playback overlay. Measured with the user's own file (`Alien Earth - S01E01 - Nimmerland.mkv`, 665 MB, HEVC in
Matroska): the front end logged `video: 316 frame(s) of 1920x832 drawn, 345 delivered by the player`, and a
screenshot of the desktop taken while it ran shows a ship's corridor where the black rectangle used to be. The
gate that keeps this honest is `scripts\ci\test-player-file.ps1`, which plays a file a person has, reads the
container with ffprobe, and fails if the engine disagrees with the container about length, tracks, or - since
this section - the size of the picture.

**Padding is not a black border.** The container says the film is 1920x**804**; the decoder works in 64-line
blocks and hands over a 1920x**832** frame, and ffprobe's `coded_height` is a third number (808). Drawing the
buffer draws the padding, which stretches the film by 3.5 % - invisible in a log, obvious in a room. libVLC knows
both numbers and is the only party that can say which is the film, so `LibVlcPlayer.FitPictureToSource` asks
`MediaPlayer.Size(0, ref w, ref h)` once per video and tells the sink where the picture stops; the sink keeps
buffer size and picture size apart (`Width`/`Height` versus `PictureWidth`/`PictureHeight`), and
`TryCopyLatest` copies rows from the buffer's pitch up to the picture's width, so a padded row and a padded tail
are both left behind without anyone having to know which one this video had. When the engine declines to answer,
the whole buffer is picture - the worst a wrong answer can then cost is the stretch it would have cost anyway.
Three sources now agree on 804: the container, the engine, and the gate.

**A black rectangle and a dark scene look the same in a frame counter,** so the front end measures the picture it
drew: `picture: mean luminance 7, 729 distinct colour(s) in 48,240 sampled pixels`. A surface that is not being
written to reports one colour and luminance 0; a scene shot at night reports the numbers above, which is the
difference between a broken pipeline and a film that is dark at 0:31. The render probe does the same thing with
real Skia on both platforms and with a *known* picture - eight bars pushed through the sink in a known order -
and fails if the drawn bars are not those bars in those places and colours (`bars: all eight, in the order they
were pushed, in the colours they were pushed in`).

**The byte order was wrong, and only a screenshot could show it.** Avalonia handed the surface a
`WriteableBitmap` whose locked buffer said `Rgba8888` although `Bgra8888` had been asked for. The copy assumed
blue-first bytes, so red and blue reached the screen traded. The surface now asks the framebuffer what it is,
swaps when it says `Rgba8888`, and refuses an unknown format with a sentence rather than drawing a guess
(`VideoSurface.LastFailure`). Measured on Avalonia 12.1.4; the probe's own captured frames came back `Rgba8888`
too, so this is what the compositor wants rather than a quirk of one control.

**Then the picture path killed the process, and it was worth the week.** Disposing the sink of a *playing*
player - which is exactly what happens when a window closes - ended in `0xC0000005` inside libVLC, in the
decoder, with nothing in any log but `avcodec decoder: Using D3D11VA ...`. Two independent faults, found by
bisecting with a throwaway host:

- The sink unpinned its buffers while a decoder thread could still be writing into them. The addresses handed to
  an engine point *inside managed arrays*; the moment the pin is gone the collector may move them, and a decoder
  writing into a moved array is not an exception anybody can catch. The sink now counts the buffers an engine
  holds (`TryAcquireBufferForWriting` … `EndWrite`), asks its engine to stop through
  `EngineWillStopWriting`, and only then frees - and if a writer is still in a buffer after half a second it
  **keeps the pin** and says so (`ReleasedCleanly == false`), because a few megabytes the process will not see
  again is a cheaper failure than its heap. The same wait guards the resolution-change path, where the old
  buffers used to be freed under a frame in flight.
- A memory video output cannot be refused at lock time. With the sink gone, `OnVideoLock` answered the engine
  with the null it had not asked for, and VLC 3.0.20 takes that as an access violation rather than a dropped
  frame. The player now keeps a scratch block the size of one agreed picture and gives that to any picture with
  nowhere to go, tagged so that the unlock and display callbacks know it is not a frame a screen should draw. And
  the engine is stopped *before* the sink is let go, not after: while `Stop` runs libVLC is still locking the
  pictures it has in flight, which are the ones that were being refused.

Five runs of the scenario that used to crash every time now exit 0, `ReleasedCleanly=True`, and the whole
real-file gate is green with the sink disposed mid-play. `Tests.Frontend` is **174**: 14 tests of the sink
(including the two that reach into the assembly to create the held-buffer state, which the public surface cannot
produce), the playback screen's 23, and the real-file fixture's 9 whenever a file is supplied.

**The picture was invisible, and the frame counter did not know it.** A report of "sound but no picture" was
correct, and the counters had been arguing with it: `video: 975 frame(s) drawn` while the screen was black. The
cause was in `VideoSurface`, one line of it: the surface copies decoded bytes into a `WriteableBitmap`, but it
handed that bitmap to the `Image` control *before* the first copy had put anything in it, and never asked the
control to draw again. The compositor kept the empty upload it made first and showed it forever, while
`FramesDrawn` climbed merrily. The fix is to set `Source` only after a copy has succeeded and to call
`InvalidateVisual()` after every fill - the control cannot notice that bytes inside a bitmap it already holds
have changed. This is the clearest case in the whole project of a counter that measured the wrong thing: "drawing
was asked for" is not "a picture is on the screen", and only the pixels can say the second.

**The measurement lied once, and it was a screen capture of a desktop.** The way that was supposed to prove the
fix - photograph the maximised window's rectangle with GDI and count its colours - reported `53 719 distinct
colours` after the change, which looked like a film and was actually the editor showing through beside the
window. A screen capture of where a window *should be* is a capture of whatever is there. So the front end now
photographs *itself*: `--shot <file>` writes the surface's own picture to a PNG, `--start <time>` seeks past the
part of the film that is legitimately dark, and the run prints a `layout:` line (window size, scaling, and the
picture area inside it) so "the pixels landed in a rectangle nobody can see" is ruled out by geometry rather than
by eye. The self-captured frame of the user's film at 4:00 is `picture: mean luminance 42, 1699 distinct
colour(s)` in a 909 KB PNG of the scene - a night exterior - and at 0:11 the *file itself*, decoded by ffmpeg, is
luminance 1 because the episode opens on a title card. Dark is not broken, and now the two can be told apart
without a person at the screen.

**Two things had to be learned about the test platform to get there.** A `DispatcherTimer` never ticks under
`Avalonia.Headless.NUnit` (measured over a second of pumping), so `VideoSurface` exposes `DrawNow()` - the work
the timer does - for tests to drive. And in the headless drawing mode the existing 155 tests insist on, a
`WriteableBitmap`'s locked buffer does not retain what was written into it, so *colour* claims are made in the
render probe, which runs real Skia, while the headless tests claim behaviour. A test that asserted colours in the
headless suite would have been asserting garbage, in the literal sense.

### w. The library on screen: tiles, a list of titles, and the door that was locked

**What was missing was not a skin, it was a road.** The playback screen could show a film somebody owns, but the
home screen offered three buttons that led to developer screens, which is not a media centre. The shortest
honest road from a running server to a film on the sofa is: a tile for a part of the library, a list of titles in
that part, and OK on one of them. That slice is what this section is about - one endpoint, one sign-in, two
screens - and the surprise it turned up was that the door between them had a lock on it all along.

**The endpoint.** `GET api/media/titles?aspect=MovieItem&sort=title&order=desc&limit=60&offset=0` in
`MediaController` answers one page of titles plus what is left behind the page:

```
{"aspect":"MovieItem","total":412,"offset":0,"limit":60,"returned":60,"sort":"title",
 "titles":[{"id":"…","title":"Alien Earth","sortTitle":"Alien Earth","year":2025,
            "runtimeMinutes":43,"path":"{D3525B2F-…}://C:\Video\Alien Earth.mkv","stub":false}]}
```

The names in that answer are the names the server really has: the aspect carrying a title's title is
**`MediaItem`** and the one carrying its year is **`MovieItem`** - not "Media" and "Movie", which is what a reader
of the old client's XML would expect and what this code said for about an hour. `Path` is deliberately still the
server's own serialization (`{provider-id}://path`) rather than a file name, because whether that resource can be
opened depends on which machine is asking; the translation lives on the client
(`MediaLibraryPath.TryAsLocalFile`), and everything it cannot translate - a stacked provider, an archive entry -
comes back as a sentence about why rather than as a path the player would fail on later. One line of the
controller is worth keeping in mind when reading other code here: `MediaItem.GetResourceLocator()` **throws** on
an item whose editions are inconsistent, so it is called per item inside a guard, not once per page. Limits are
`DEFAULT_LIMIT = 500` and `MAX_LIMIT = 2000`, because a title list is a UI's memory, not a database dump.
Eighteen tests in `TestMediaController` cover the sorting, the paging arithmetic, the two limits, the four error
answers, the stub flag and the throwing locator. Two of them exist because `ActionResult<T>` in this ASP.NET
version keeps the framework's result in `.Result` and the payload in `.Value`, and the `ActionResult<T>` itself is
not castable to `IStatusCodeActionResult` - the `.Result` is. That is not obvious, so it is written down in the
test helper that reads it.

**The lock.** `MediaPortalAuthorizeAttribute` guards the endpoint, and
`ServerSettings.WebAutorizationEnabled` is `true` in a field initializer *and* in its `[Setting]` default. So a
stranger at the library endpoints is answered **403**, and that is not a regression to work around - the gate
`run-server-check.ps1` asserts precisely that an endpoint behind that filter says 403 without a token. What the
front end lacked was the ability to stop being a stranger.

`ServerApiClient` now signs in through MP2's own endpoint - `POST Token`, OAuth password grant, form-encoded, the
`{access_token, token_type, expires_in, issued, expires}` answer - and sends the ticket as `Authorization: Bearer`
on every request after that. There is no second way in here, and no invented account system. The home screen grew
a name-and-password row beside the server picker; a successful sign-in repeats the name back and re-reads the
library, and a title list that is refused says *"Films is only for signed-in users on this server. Sign in on the
server screen."* instead of showing an empty list, because an empty list is read as a broken front end. Two
client tests pin the two halves that matter: the ticket from a successful sign-in is really on the next request's
headers, and a refused one (`{"error":"invalid_grant","error_description":"…"}`) leaves the client a stranger with
the server's own sentence as the exception message.

**A test host that cannot answer "who is asking" is not a server.** Adding the filter's check to the tests turned
every 403 into a **500**: the attribute loads `ServerSettings` through `ISettingsManager`, and `InProcessApi` had
never registered that service, so the attribute threw instead of answering. `InProcessApi.RegisterSettings(bool
anonymousAccessAllowed)` now registers a settings manager whose flag the test chooses - off for the tests about
titles and pictures, on for the one test that wants to hear 403. The same run produced a second cross-platform
lesson: `Path.IsPathFullyQualified("/media/Film.mkv")` is **false on Windows**, so a fixture path that "works on
both platforms" has to be built from `Path.GetTempPath()`, not written as a POSIX-looking string.

**The screens.** `HomeSection` (in `MediaCatalog.cs`) is the catalogue of parts of the library a viewer would
want to watch - films, series, episodes, music, videos - filtered against the aspects *this* server actually
has, so a server without the music plugin does not offer a music tile that cannot be filled. Each tile on the
home screen opens a `MediaBrowserWindow` bound to `MediaBrowserViewModel`: 60 titles at a time, sorted by title,
`More` asking for what comes after what is on screen, the first row selected because a list with nothing selected
is a list whose OK key does nothing, and a row that names its title, its year and whether this machine could open
it (`not playable here` before anything is pressed, the same words again if OK is pressed anyway). Enter plays,
Escape closes, and the D-pad handler attached to the other screens is attached here too.

**What it looks like, measured.** The render probe gained a fourth frame, `--screen browser`, which fills the
list through the *wire format* - twelve titles in the endpoint's own JSON, served by a handler rather than a
network - so the frame covers deserialisation and binding and not only drawing, the same reason the playback
frame goes through the frame sink rather than painting a bitmap. Measured on Windows:
`12 title(s) under "Films"`, `2058 distinct colours, 531385 differing from the background, 88529 high-contrast
edges`, and the 1280x720 PNG shows rows of `Title (year)` with a detail line, the first row highlighted, the
status line `12 of 12 films, sorted by title`, and the Esc hint.

**Where this is still short.** A server with no user accounts cannot be signed into, and the front end then
repeats the server's words, which is the honest answer but not a friendly one; the first-start experience on such
a server wants its own sentence. A title whose resource is stacked (an archive entry, a virtual provider) is
listed and marked not playable - the missing piece is a server endpoint that streams any resource whatever it is,
which is the next thing after this. And the tiles count what the server counts; they do not yet remember which
tile a viewer last watched.

**Where the numbers stand.** `Tests.Server` 238, `Tests.Frontend` 187 (the browser screen's 11 and the sign-in
pair among them), the whole suite **612 tests, 0 failures** on Windows.

## What cannot be verified without the user

Stated once so it does not have to be repeated per phase: audio passthrough and HDR on real hardware,
DVB/TvHeadend against real multiplexes, the feel of remote navigation, visual taste, and a 40 TB library
import. These need hardware and judgement that are not in this environment, and they are batched into
one hand-off checklist per phase rather than dribbled out.

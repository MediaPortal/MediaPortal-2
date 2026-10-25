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
on every request after that. There is no second way in here, and no invented account system. One detail is not
negotiable and was only found by reading the server: the `password` field goes out **base64-encoded**, because
`ResourceServer.GrantResourceOwnerCredentials` hands whatever arrives to `Convert.FromBase64String` before
decoding it as UTF-8. A plain-text password is therefore not rejected by MP2 - it is a format error on the server.
Encoding, not protection: over MP2's plain HTTP the password is readable on the network, which is exactly what the
"Web authorization" switch is for.
The home screen grew
a name-and-password row beside the server picker; a successful sign-in repeats the name back and re-reads the
library, and a title list that is refused says *"Films is only for signed-in users on this server. Sign in on the
server screen."* instead of showing an empty list, because an empty list is read as a broken front end. Two
client tests pin the two halves that matter: the ticket from a successful sign-in is really on the next request's
headers (and the password that got it there is the base64 the server expects), and a refused one
(`{"error":"invalid_grant","error_description":"…"}`) leaves the client a stranger with the server's own sentence
as the exception message.

One answer from that endpoint needed translating before the screen could use it. A wrong password gets *"The user
name or password is incorrect."*, but a name that does not exist - and a server where nobody ever set profiles up -
both fall through to **"User management not available."** (`ResourceServer`, after the profile lookup fails). So
the same words cover "typo" and "this server has no accounts", and only one of them is fixed by typing again.
`HomeViewModel.DescribeSignInFailure` turns that particular answer into what can be done - add a profile on the
server, or switch "Web authorization" off there and the library is open to the house - and leaves every other
refusal in the server's own words, where a wrong password needs no interpretation.

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

### x. What happens to the skins, decided rather than drifted into

**The MPF skin engine is not being ported.** It is the part of the old client that a multiplatform front end
does not need twice: MPF (`SkinEngine`) is a WPF-derived immediate/retained hybrid with its own layout engine,
its own focus model and its own XAML loader, and Avalonia already is all of that. So the question was never "how
do we run MPF on Linux" but "what do we keep from what somebody spent years building in it".

**Answer, from the user:** *Nereus* and *WMC* can be rescued by fixing them from MPF to WPF; the other skins are
not needed, and neither is the Graveyard. The deletion happened and is recorded with its measurements in
`scripts\ci\README.md` ("The other skins are gone, and so is the Graveyard"): BlueVision and Titanium Extended
(1068 files between them) and `MediaPortal\Graveyard\` (842 files, seven abandoned skins), the 28 lines they
occupied in `MP2-Client.sln`, and the stale mentions in the two `Directory.Build.props` files,
`Directory.Build.targets`, `architecture.ps1` and the translation tool's project list. `build.ps1 -Solution
Client` still reports 0 errors afterwards, and `Nereus` and `WMC` are what is left under `Source\UI\Skins\`.

**What "rescued" means here, said plainly, because the phrase hides a fork.** Fixing Nereus and WPF skins from
MPF to WPF makes them run in the *old* WPF client on Windows. It does not make them run in Avalonia: MPF screens
are written against MPF's own element types and attached properties, and no amount of WPF-compatible syntax
changes that. So the two skins are kept as two different things, and it matters which one is meant:

- **As a specification for the new screens** - what a film row contains, how a selected item announces itself,
  what a section page puts where, how the TV layout uses a 1920x1080 room. This is worth more than any
  screenshot could be, it costs nothing to use, and it is the reason they stay in the tree.
- **As a runnable skin in the old client** - possible, and the work is real (the MPF types under
  `Source\UI\SkinEngine`), but it is optional and it does not unblock anything in this port.

**Where the new screens stand against that specification.** The four screens that exist now - connect, home,
the title list, playback - were designed from MP2's behaviour and from what a 10-foot interface has to survive
(D-pad only, no hover, no right-click, text readable from a sofa), not copied from either skin. The next pass on
them is a comparison: sit each rendered frame next to the Nereus and WMC screens and take the differences that a
viewer would notice - selection treatment, row composition, what a section header carries - and then decide per
difference, in the Avalonia idiom, rather than translating MPF markup. That is the part of "restore the MP2
functionality with these skins" that can be done honestly; translating the XAML files themselves would produce a
skin that looks like Nereus and works like nothing, because the code behind MPF's attached properties would not
be there.

### y. Sound and subtitles, because a player with one language is a toy

**The feature.** The playback screen picked nothing before this: a film with a German and an English track played
whatever the engine chose, and the only way to change it was to leave the media centre. There is now a row under
the transport keys - `Sound` and `Subtitles`, each with the current choice spelled out beside it - and each button
moves to the next track of its kind, wrapping. Subtitles cycle through "no subtitles" as one of the entries,
because the way back is the thing a viewer looks for and cannot find in a list that only goes one way. The row is
absent for a media that has nothing to choose, which is why the tests, not the screenshot, carry that half.

**One number the seam did not have.** `Tracks(kind)` and `SelectTrack(track)` already existed, so the picking was
there; what was missing was the answer to "which one is on now", so the seam gained
`IVideoPlayer.CurrentTrackId(kind)`. Asked rather than remembered by the caller on purpose: a media chooses its own
default track when it starts, and a screen that marked what the viewer last pressed would mark the wrong row for
every media nobody touched. libVLC answers it with `AudioTrack`/`Spu` and reports the off entry as -1, which is the
same convention the seam already promised, so no second vocabulary came in. The engine's track names are kept as
the engine said them, with the two-letter language in front when the container carries one -
`eng - Language (eng)`, `Director's commentary` when it does not.

**A hidden area is not an area, and the d-pad had to be told.** The row appears and disappears with the media, and
the ring that walks the screen with the arrow keys took its `IsVisible` from each button - which says true for a
button inside a collapsed panel, because it is that control's own answer and not the state of the screen. The
result was a key press that went to something the viewer could not see, and Avalonia refusing the focus, so the
remote's key was swallowed at the end of the transport row. `DpadNavigation` now walks the ancestors
(`EffectivelyVisible`) instead of asking the control alone, and the areas that are not on screen are walked over
rather than walked into. Two tests pin both halves: with a film that has no tracks, the wrap from the last button
still lands on `Close`; with a film that has them, `Close` → down is `Sound`, → down is `Subtitles`, → down is
`Pause`, so the pickers joined the one loop a remote has rather than becoming a second one.

**Where the numbers stand.** `Tests.Frontend` 196 (six of them new: the tracks of a film that started, the off
entry, the wrap-around, a refusal the engine explains, the second film being asked for its own tracks rather than
keeping the first one's, and the keys doing nothing while nothing is loaded). The playback screen's rendered frame
carries the row and is measured with it (`2120 distinct colours, 265 700 high-contrast edges`), and the whole suite
is green on Windows: **621 tests, 0 failures** across `Canary.Portable` (29), `Tests.Server` (238), `Tests.Client`
(90), `Tests.Frontend` (196) and `Setup\Tests` (68).

### z. What one title says about itself, and the key that asks for it

**The feature.** A list of titles can only say a name, a year and a length, so a viewer deciding what to watch had
no way to find out that the server has known the plot of every film since it was imported. The library list now
answers the remote's **info** key (and the letter `I`) with a panel beside the list: the title and year, one line of
what a viewer decides with (`88 min · FSK 12 · rated 8/10 · watched 1×, last 2025-01-12`, or `S01E01 · Nimmerland`
for an episode), the longest prose the title carries, and where the file is. Pressing the key again, or Back, puts
the panel away - Back closes the panel *before* the screen, which is what "back" has always meant on a remote: away
with the newest thing.

**The server had never been asked for one title.** `GET api/media/titles` lists; nothing read a single film. The new
`GET api/media/items/{id}` answers the fields the title carries itself - title, sort title, year, runtime, overview,
story plot, tagline, certification, collection, rating, series/season/episode, play count, last played, path, stub -
`404` with the id named in `error` when the library has no such title, `503` when the library is missing or still
starting, and the same `[MediaPortalAuthorize]` as the rest of the controller. It does *not* use
`IMediaLibrary.LoadItem`, because that wants the id of a **resource system** the front end has never seen; a client
that holds a title from a list holds only its `MediaItemId`. So the endpoint asks the library for the one item whose
id it is given (`MediaItemIdFilter`), with `MediaAspect` necessary and everything else optional - a film imported
without an NFO file still gets an answer instead of a 404 for lacking a plot. Genres and people are deliberately
absent: in MP2 they are their own related items (`GenreAspect`, `PersonAspect`), reachable through a second query,
and an endpoint that quietly ran three queries per title would be a different decision made silently.

**Two things the database does that a first draft got wrong.** `EpisodeAspect.ATTR_EPISODE` is a *collection*
attribute - a double episode stores two episode numbers for one media item - so reading it with the single-value
getter throws, and the endpoint reads it with `GetCollectionAttribute<int>`. And an unset single-string attribute
comes back as `""`, not null, which a details screen would have drawn as a blank line: blank lines read as a broken
screen rather than as a film nobody described, so the controller trims and turns empty into null before answering.

**The panel is an overlay, not a screen.** It sits in the column the Play and More buttons occupy, and nothing in it
takes focus: a details area a viewer has to navigate into is a screen inside a screen, and a remote has one set of
arrows. The list stays where it was behind it, so closing the panel returns to the same row, and the read is
remembered per title - pressing the key to look, and again to think, costs one request. A failure leaves the panel
closed and puts the server's words in the status line, because an empty panel is a second and worse answer to the
question; a `403` says how to sign in rather than reporting a number.

**Where the numbers stand.** Seven tests on the endpoint (unknown id, no library, library still starting, a film
with everything, a film with nothing, an episode including the double-episode case, and an item whose aspects are
partial) take `TestMediaController` to 25; six in the front end (the panel from a real in-process server, the cache,
the 404, the 403, the episode line, and `Key.I`/Escape going through the window's own handler) take
`Tests.Frontend` to 202. The render gate gained a fifth frame: `details-windows.png`, 1863 distinct colours and
17 701 high-contrast edges, written only if the panel is really open - the probe presses `Key.I` on the window
rather than calling the command, and refuses to write the frame otherwise. Whole suite on Windows: **634 tests,
0 failures** - `Canary.Portable` (29), `Tests.Server` (245), `Tests.Client` (90), `Tests.Frontend` (202),
`Setup\Tests` (68).

### aa. The library learns what was watched

**The feature.** The details panel had just started showing `watched 1×, last 2025-01-12`, which is an odd thing for
a media centre to show while every number in it is a fossil: nothing in the new front end ever told the library that
a film had been played. That is now fixed at both ends. Watching something reports it, and the panel that shows the
counters is told to go and read them again, so the sentence under a film describes the viewer's history rather than
the state of the database on the day it was imported.

**One new verb on the server.** `POST api/media/items/{id}/playback` with `{"percentage":50}` in the body - the
number is the content of the request, not a way of choosing which title, and `?percentage=100` on a POST reads like a
GET that was told to lie. It answers with what the library believes *now*: id, title, whether it counts as watched,
the play count and the last-played date. `404` naming the id when the library holds no such title, `503` when the
library is missing or still importing, and the same `[MediaPortalAuthorize]` as the rest of the controller. The
client refuses a number outside 0-100 before it opens a connection, because a client that already knows the answer
should not go and ask a server the same question.

**`NotifyPlayback`, not `NotifyUserPlayback`.** MP2 offers both, and the difference is who the answer belongs to.
`NotifyUserPlayback(userId, …)` stores *per-profile* user data - percentage watched, watched date, parents updated -
and needs a `userId` to store it under. `NotifyPlayback(mediaItemId, watched)` writes to the title itself:
`ATTR_LASTPLAYED` on anything, `ATTR_PLAYCOUNT` when it was finished. The new front end has a bearer token, which is
not a profile, and inventing a profile to hold a percentage would mean choosing one on the viewer's behalf - the
same decision the server already refuses to make when it signs a token in without a profile. So the front end
reports what it actually knows (this title was played; it ended) and the profile-based counters stay untouched until
there is a profile to write them to.

**What counts as watched.** Only the engine saying *the media ended* produces a 100, because "they stopped it at 99%"
and "they saw the last second" are different statements and only the second earns a tick. Anything else - stop, or
switching to another media - reports where the viewer got to, floored at 1 and capped at 99; below 1% nothing is
said, because a title opened and closed in ten seconds was not watched and a viewer who cancels the wrong thing
should not lose their history over it. One report per media: stopping after the credits does not count twice.
And "how far" is worked out *before* the engine is told to stop or to open the next file, because a real engine takes
the media's length away with the media - `FakePlayer.ForgetsLengthOnStop` exists so that a test can hold the view
model to that ordering instead of discovering it on a Sunday evening.

**Said out loud, never waited for.** `PlaybackReporter` subscribes to the playback screen, asks the application
which server to report to *when the report is made* (the front end can be moved to another server while a film
plays), sends it, and writes the outcome on the console: the library's new play count, the server's own refusal
words, "no server configured", or "cancelled". It is started and not waited on - a media centre that holds up the
next screen, or the shutdown, on a network round trip is worse than one that loses a mark now and then - but not
waiting is no reason to be silent, because a missing tick is noticed a week later with no idea when it stopped
working. Only when the server agrees does the browser screen get `ForgetDetails`, which is also the answer to the
panel that reopens with the numbers from before the film.

**A bug the reporter tests were written for.** The first version ended in
`ContinueWith(…, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.OnlyOnRanToCompletion)` - meaning to
say "whatever happens, print something". That asks a finished task for two mutually exclusive endings, and
`ContinueWith` does not pick one: it throws `ArgumentOutOfRangeException` (*"the specified TaskContinuationOptions
exclude all continuation kinds"*), which was verified rather than assumed. The POST had already been sent by then, so
the film *was* marked watched while the exception landed on the thread the engine had raised "stopped" from - the
worst kind of bug, one that works. A continuation with no options and a branch inside it says both outcomes instead
of neither, and the reporter fixture fails the moment anything throws out of a report.

**Where the numbers stand.** Eight endpoint tests (a film watched to the end, one started but not finished, 99%
being explicitly not a finished film, an id the library does not have, a report with no percentage in it, a number
off the scale, a library still starting, and no library at all) take `TestMediaController` from 25 to 33. Ten more on
the playback screen (100 only when the engine says the media ended, 99 rather than 100 for a stop on the last frame,
the furthest point rather than the last one, measuring before the engine forgets the film, nothing at all for a
viewing that never got going or for a file that is not a library title, one report per media, and the previous film
reported when the next one starts) take `TestNowPlayingScreen` to 39; four on the client (the POST really is a POST
with that body, the server's answer arrives as an object, a 404 keeps the server's words, an impossible percentage
never leaves the machine) take `TestServerApiClient` to 15; a new six-test fixture covers the reporter; and one more
browser test shows the panel reading a title again after it was watched. `Tests.Frontend` goes from 202 to 223, and
the whole suite on Windows is **663 tests, 0 failures** - `Canary.Portable` (29), `Tests.Server` (253),
`Tests.Client` (90), `Tests.Frontend` (223), `Setup\Tests` (68). The render gate drew all five frames at that point, the
details frame unchanged at 1863 distinct colours. (Section **ad** carries the numbers as they stand now.)

### ab. Finding a title by name

**Why search, and why now.** The user's standing instruction is to restore MP2 functionality to the new front end, and
of everything the old client could do, the hole that hurts most with a real library was the one that was never there:
a three-thousand-title shelf you can only walk down a page at a time. So the new front end got a search box, and the
rule it follows is the one MP2 already followed.

**The search is the old library's own question, not a filter built here.** MP2's media client does the same thing it
does everywhere else: type a word, press *Find*, and the call goes to
`IMediaLibrary.BuildSimpleTextSearchQuery(text, necessary, optional, filter, includeCLOBs, caseSensitive)`
(`MediaLibrary.cs:934`), which builds a `LIKE` condition over **every short text attribute** of the title - name,
original name, director, artist, genre, tag, and the rest. Two details inside that method are the whole reason the new
endpoint calls it instead of assembling its own `LikeFilter`:

- Every wildcard in the typed text is escaped by `SqlUtils.LikeEscape` (with `ESCAPE_CHAR = '\\'`). A viewer who types
  `%` or `_` is looking for that character, not asking for the entire library.
- Empty text returns `FalseFilter`, never *true* - the library's answer to "nothing typed" is "nothing found", not
  "everything". The endpoint keeps that rule by treating whitespace as "no search at all" *before* asking, so an empty
  box means the plain shelf rather than an empty one.

Two of the six arguments are decisions rather than accidents, and both are asserted in a test so that a future
refactor cannot quietly flip them. `includeCLOBs: false`, because MP2's own screens leave the plot and the review text
out: a word like *earth* sits in a hundred plot summaries and the result list stops being a result list.
`caseSensitive: false`, because a remote control has no shift key and nobody wants to type a film title twice. The
`filter` argument is passed `null` - the search stands alone today, and stays combinable with genre jewels and the
rest, which is what the signature is built for.

**`GET api/media/titles?...&search=word` - a seventh restriction, not a new endpoint.** Same controller, same route,
same sort/limit/offset, so everything the browser already knew about paging still applies to a result set. `Total`
changed meaning along with it: it used to be `CountAllMediaItems()`, and is now counted **through the same filter the
list is read with** (`CountMediaItems(necessaryMIATypeIDs, query.Filter, false, true)`). Without that, a search of one
hit would report "1 of 3443 films" on the first line and "3443 titles" in the status, and the viewer would spend the
next minute wondering whether they had searched at all. That count costs a second query per page while searching; it
was already a second query before, only a cheaper one, and `Total` exists for exactly this sentence.

The answer also echoes the word back (`TitlesResult.Search`). The client knows what it sent, but a page can now be
recognised as an answer to *this* question - which matters the moment a viewer keeps scrolling while typing and a
page from the previous question arrives late.

What the server deliberately does **not** do: split words into stems, forgive spelling mistakes, or search free text
for years and genres. MP2 could not do that either; `BuildComplexSearchQuery` is where somebody would start if they
ever need it.

**The word is remembered twice, and the difference is half the test.** `ServerApiClient.GetTitlesAsync` grew a
`search` parameter and appends `&search=` URL-escaped - a space in the phrase must not cut the query string in half,
and a `%` in a word must not be read as the start of an escape. In the view model, `SearchText` is *what is in the
box* while `ActiveSearch` is *what the list on screen actually answered*, and `ActiveSearch` is assigned only after
the answer is in hand. That is the same reasoning the details panel uses: if the network is down and the list is
still showing last time's titles, the status line has to describe the list, not the wish. A test holds the model to
it - after a failed search the status line still names the old word and no clearing happened.

Which gives the status line three shapes, and the middle one is the one that happens on a real remote:

```
Films                    12 titles in this view              ← no search
Films                    24 titles match 'erd' (page)        ← more still at the server
Films                    1 title in films matches 'alien'    ← everything that matches is on screen
```

The parenthesised "(page)" is what decides whether *More* stays available: shown < total means the server has more
matches, so a single-hit search loses the button and a wide one keeps it.

**Keys, in the order a remote actually presses them.** `F` (and the Windows/Apps key, where the on-screen keyboard
opens) moves focus into the box. Enter *in the box* searches and puts focus back on the list, because the next
keypress should already be selecting the first hit; Enter in an empty box means "take the search away". Escape walks
down exactly one level per press: detail panel first, then the search (list whole again, focus back in the box), then
the window - otherwise a viewer gets stranded in a filtered library they no longer understand. Enter *on the list*
still plays, untouched by any of this. The box sits outside the `DpadNavigation` ring on purpose: the ring installs a
Bubble handler that claims the arrow keys whenever focus is not in the ring, which would make Up/Down move the
selection while somebody is typing; Left/Right still move the caret because the `TextBox` consumes them first.

**Tests.** Six new server tests (`TestMediaController` 33 → 39) do not re-implement a `LIKE` - they ask *which
question the library was given*, through a stub library that overrides `BuildSimpleTextSearchQuery` and answers with
`FalseFilter`. A test that rebuilt the SQL condition would survive the wildcard escaping being deleted; this one
notices if the endpoint stops using the library's method, or flips the CLOB or case flags. The rest cover counting
through the same question, the echo, "no word means the whole shelf", "two words typed together are one phrase, not
two searches", and a 404 for an unknown aspect while searching.

Seven new browser tests (`TestMediaBrowser` 22 → 29, `Tests.Frontend` 223 → 230) run against a stub server that
records the word it was asked and filters its own twelve titles by it, so the view model can be held to "one title"
without anybody reimplementing the filter: the word goes out, the status line names it, the list gets shorter, *More*
survives a partial page and disappears on a complete one, clearing restores the whole shelf, a dead server deletes
nothing, and paging while searching carries the same word forward. The last one is a window test: it focuses the real
`TextBox`, sends `TextInput` and `Key.Enter`, and then presses Escape twice - first the search goes away, then the
window closes.

**The sixth frame.** `search-windows-skia.png` is the same browser window with `alien` typed into the real box, one
row left in the list, and the header reading "1 title in films matches 'alien'". The probe fails if the text never
reached the box, if more than one row survives, or if the number in the header did not fall to one. Independently of
the probe's own counters, sampling the pixels of the row area tells the same story: in the unfiltered frame that band
holds 338 distinct colours (rows of text), in the search frame it holds **one** (empty wall below a single row).

**Where the numbers stand.** `TestMediaController` 39, `TestMediaBrowser` 29, `Tests.Frontend` 230, and the whole
suite on Windows is **676 tests, 0 failures** - `Canary.Portable` (29), `Tests.Server` (259), `Tests.Client` (90),
`Tests.Frontend` (230), `Setup\Tests` (68). The render gate drew six frames at that point and printed
`FRONTEND CHECK OK`.

### ac. What the list knows about what you have seen

**The hole the last two sections dug.** Section **aa** taught the server to remember that a film was finished, and
section **ab** taught the shelf to be searched - but a viewer looking at a row of titles still could not tell a film
they have seen from one they have not, which is the single most useful fact a library has about its own contents.
The data was in the database and nowhere on the screen. This section closes that.

**MP2 already had the two conditions, and they are one column.** Its own list providers do not keep a "watched"
flag; they ask about the play count. `BaseLastWatchedMediaListProvider` filters `PlayCount > 0` and
`BaseUnwatchedMediaListProvider` filters `PlayCount = 0` (`BaseUnwatchedMediaListProvider.cs:45`), falling back to
those two when there is no user profile to read per-profile percentages from - which is exactly the situation this
front end is in, for the reason **aa** gave: a bearer token is not a profile. So the new endpoint uses those two
conditions rather than inventing a third vocabulary. `GET api/media/titles?...&seen=watched` and `&seen=unwatched`,
echoed back in `seen`.

**The unwatched condition needs a second half that MP2's does not, and the reason is NULL.** `PlayCount` is
declared nullable on the Media aspect (`CreateSingleAttributeSpecification("PlayCount", typeof(int), Cardinality.Inline,
true)`), and a film the importer just found has *nothing* written in that column - not zero, nothing. In SQL a
comparison against nothing is not false, it is unknown, and a WHERE clause throws out a row whose condition came
back unknown. So `PlayCount = 0` alone answers "which titles were watched zero times" over the titles the database
happens to have written a zero for, and it says nothing at all about the ones nobody ever opened. Which produces the
most confusing possible answer: a freshly imported library - where nothing has been watched yet - reporting *"no
unwatched titles"*. The endpoint therefore asks for `PlayCount = 0 OR PlayCount IS NULL`, using MP2's own
`EmptyFilter`, which compiles to `IS NULL` for an inline attribute (`CompiledFilter.cs:255`, whose comment records
that those columns come from left outer joins and can be tested for nothing). The `watched` condition needs no such
half: `PlayCount > 0` cannot be true about nothing either, and a title nobody opened is correctly not watched.

The combination is built with `BooleanCombinationFilter.CombineFilters`, which is not decoration: it returns the
single filter when only one is left and `null` when none was asked for, so an unfiltered request reaches the library
without an empty `AND ()` in front of it, and a test asserts exactly that (`AFilterNobodyNamedAsksTheLibraryForNothing`
also covers a misspelling, which is read as "both" and echoed as `null` - the answer never claims a filter it did
not apply).

**Same route, and the count goes through the same condition.** Like the search, the watched filter is a
restriction on `GET api/media/titles`, and `Total` is counted through `query.Filter` - so the seven titles behind a
filter are seven, not seven out of the whole shelf. One test does not check the number but the *identity*: it
asserts that the filter instance the count was asked with is the same object the list was read with, because "7 of
3443 unwatched films" is precisely the sentence this endpoint must not be able to produce. Sorting gained
`sort=lastplayed` (MP2 orders its last-watched list by that column), which is what a "what was I watching" list
asks for, and `search` and `seen` are combined as **one** question - the watched condition is handed to
`BuildSimpleTextSearchQuery` as its own filter argument rather than ANDed together here, which is what the signature
is for.

**What the mark means, and what it refuses to say.** `TitleResult` gained `PlayCount` and `LastPlayed`, read off the
Media aspect the list already asks for, so the row costs no second read. A row carries a tick when
`PlayCount > 0` - nothing more and nothing less. Deliberately *not* marked: a title with a last-played date and no
finished viewing behind it. That is somebody who stopped halfway, and a tick on that row would tell them a film
they still have to watch is one they have seen; `ARowIsMarkedSeenOnlyWhenTheLibrarySaysItWasFinished` holds the
screen to that, with a shelf that contains exactly such a title. Nor does a row get a cross when it is unseen:
ninety unmarked rows out of three thousand are the information, and a mark on every one of them is a wall of ink
saying nothing. The tick lives in a column of its own so that the names of marked and unmarked rows start at the
same x, which is what lets a shelf be scanned by eye.

**Three states, one key, one button.** `W` (and the button above the list, whose label *is* the state) walks
everything → unwatched → watched → everything. Three states on one control rather than two checkboxes, because a
remote reaches its keyboard through one button and nobody should have to remember which of two switches to untick.
The filter goes to the server rather than being applied to the page for the same reason the search does - on a
shelf of three thousand films the twenty that are left are on pages nobody has read - and paging carries it, which
is what `TheWatchedFilterTravelsToTheNextPageToo` is for. Escape now walks four levels, one per press: details
panel, search, watched filter, window. And the status line admits the filter in front of the count -
"2 of 2 unwatched movies, sorted by title" - because a short list that does not say why it is short is a viewer
wondering whether their library shrank.

**Tests and a picture that measures itself.** Seven new server tests (`TestMediaController` 39 → 46) assert the
conditions rather than a re-implementation: that `watched` is MP2's own `PlayCount > 0`, that `unwatched` is the OR
of `= 0` and `IS NULL` - with the failure mode spelled out in the assertion message so nobody "simplifies" the
second half away - that the count went through the same filter object, that the two figures arrive on the row,
that a never-opened title arrives as zero and no date rather than as the year 1, that a word and a filter are one
question, and that `sort=lastplayed` reaches the library as that column. Six new browser tests
(`TestMediaBrowser` 29 → 35, `Tests.Frontend` 230 → 236) run against a stub that filters its own shelf by the word
it received, so the view model is held to "these two titles" without anyone reimplementing the filter: the mark
follows the server's figures, one press filters, the third press is back where it started, the filter survives
paging, and in a real window one `W` filters the list and the first Escape puts the shelf back without leaving the
screen. The whole suite on Windows is **689 tests, 0 failures** - `Canary.Portable` (29), `Tests.Server` (266),
`Tests.Client` (90), `Tests.Frontend` (236), `Setup\Tests` (68).

The seventh frame (`seen-windows-skia.png`) is that screen after two presses of the key: three rows, each with its
tick, the button reading "Watched only", the status line saying "3 of 3 watched films". The probe refuses to write
it unless the filter really reached the third state and every row that survived carries a mark. Independently of the
probe, sampling pixels says the same thing about the *unfiltered* frame: in the mark column, rows one, three and
seven of the shelf carry 46-58 bright pixels each and every other row carries zero - which is exactly the three
titles the script says were finished, and it includes the strongest case in the whole feature, the row of the film
somebody stopped halfway: it has a last-played date on the server and no tick on the screen.

### ad. The home screen remembers what you left half way

**What was added.** A home screen that lists two tiles and can filter a shelf by watched state still does not
answer the one question a home screen is asked: *what was I in the middle of?* The server could say "never opened"
and "finished", but nothing could say "opened, and stopped partway", so the honest answer on screen was an empty
list. The home screen now carries a **"Left half way"** strip: titles whose last-played date is set and which the
library has not counted as finished, newest stop first, each row showing the title, its series or runtime and the
date it was stopped. Choosing a row plays it through the same screen, the same player and the same media id as
choosing a row in the library - which is the whole point, because that path is the one that reports back to the
library and therefore empties the strip from the other end.

**"Started" is MP2's own reading, and it is two conditions.** `BaseContinueWatchMediaListProvider`
(`Source/UI/UiComponents/Media/MediaLists/BaseContinueWatchMediaListProvider.cs:40-59`) asks for a play date and a
percentage between one and ninety-nine when it knows *who* is asking, and falls back to `PlayCount = 0` when it does
not - and when it has no profile it sorts by `LastPlayed` descending, which is the order this strip copies. This
endpoint has no profile, so `seen=started` is the fallback translated into what this library really stores
(`MediaLibrary.NotifyPlayback`, `Source/Core/MediaPortal.Backend/Services/MediaLibrary/MediaLibrary.cs:2768-2783`:
every report writes a date, only a finished one raises the counter): a non-empty `LastPlayed` **and** `PlayCount`
equal to zero **or** empty. Both halves are load-bearing. `PlayCount = 0` alone would also match the handful of
titles that were imported with a date and never watched, and dropping `IS NULL` would silently drop every row that
has no counter at all, since `NULL = 0` is unknown rather than false.
Section **ac** established that a last-played date survives finishing a film, and that is exactly the case this
filter has to exclude - which it does, because finishing raises the counter.

**Not "continue watching", and that is not modesty.** MP2 keeps resume positions per profile
(`MediaLibrary.NotifyUserPlayback`, `Source/Core/MediaPortal.Backend/Services/MediaLibrary/MediaLibrary.cs:2802`
onwards: `KEY_PLAY_DATE`, `KEY_PLAY_PERCENTAGE`, `KEY_PLAY_COUNT` written into the *user* data of the profile that
asked). The front end
does not resolve a profile, so it has no percentage to offer, and a strip labelled "Continue watching" that starts
every film at zero would be a lie with a nicer name. So the strip is named for what is actually known - *left half
way* - and the position follows when the endpoint knows who is asking. That step is section **ae**, taken a slice
later; the name stayed, because the list is still a list of unfinished titles, and a title somebody watched through
does not belong on it whatever figure is attached to it.

**One question per tile, merged by date.** The home screen asks each aspect it offers (`Films`, `Episodes`, …) for
its own eight newest stops and merges the answers by date. Eight *per part* is what makes the merge safe: the
newest stop in the library is necessarily among the newest eight of the part it belongs to, so a shelf of forty
thousand episodes cannot push a film off the strip by being newer in another part. A part that refuses does not
empty the strip - it leaves its reason under the heading ("Could not check what you left half way - 1 part of the
library did not answer"), because "the server wouldn't say" and "there is nothing to watch" look identical in an
empty list and only one of them is a problem. The strip is off entirely when the list is genuinely empty (a home
screen is not the place for a heading about nothing) and off when the machine has no player, since a row that cannot
be played is an invitation to a dead key.

**The fake server had to learn to filter.** `TestMediaBrowser`'s fixture answered `Search` with the whole shelf
whatever it was asked, which was fine while nobody filtered; a fixture like that cannot tell "filtered" from
"forgot to filter" and would have passed this feature by accident. It now evaluates the handful of conditions a
library filter can hold - word, id, watched, unwatched, started, and their AND/OR/NOT combinations - against its
own shelf, mirrors the SQL null rules rather than C# ones (so an implementation that drops the `IS NULL` half fails
a test instead of failing a user), and throws on a condition it does not understand so a new filter cannot be
answered by an implicit "yes". The three possible answers to a titles request are deliberately different sizes (all
four titles / one watched / three unwatched / **two** started), so an unfiltered request cannot satisfy an assertion
written for a filtered one.

**Tests and a picture that measures itself.** Three new server tests (`TestMediaController` 46 → **49**): that a
half-way request is exactly "a date, and no counter - not a finished film", that the same filter object answered
the count, and that a word and a half-way filter go as one question. Five new front-end tests (`TestMediaBrowser`
35 → **40**, `Tests.Frontend` 236 → **241**): that the strip holds only the two half-watched titles and knows they
were not finished, that a machine with nothing to resume shows no strip at all, that one refusing part of the
library leaves its reason on the strip instead of pretending to be empty, that pressing a row raises the same
playback request a library row raises (same title, same media id), and that in a real window the strip sits on the
D-pad ring and `Enter` on it opens the playback screen. The whole suite on Windows is **697 tests, 0 failures** -
`Tests.Server` (269), `Tests.Client` (90), `Tests.Frontend` (241), `Setup\Tests` (68) and the portability canary (29).

The eighth frame (`continue-windows-skia.png`) is that home screen against a scripted server that holds twelve
titles, of which four were left half way - three films and one episode. The probe refuses to write the frame unless
the strip holds exactly those four, in newest-stop-first order, and none of them carries a watched mark, which is
the pair of facts the strip exists to get right. Sampling the pixels agrees without being told the answer, and says
where the columns actually sit: a row band carries three ink blocks - a title at `x 61..261`, a short middle column
at `x 528..595`, and a 172-pixel block at `x 826..997` wide enough for a `yyyy-MM-dd HH:mm` date - and that date
column holds exactly four bands in the lower half of the frame (peaks of 89, 93, 88 and 102 bright pixels) against
zero bands in the same column of the home screen drawn against a real empty server. Four dated rows is also proof of
the merge on its own: no single tile can produce four, because the films answer three and the series one.

### ae. The server knows who is asking, and therefore where you stopped

**What was added.** Section **ad** left a strip that could say *"you stopped this in April"* but not *"eleven minutes
of it are gone"*, because the endpoint did not know who it was talking to. It knew all along. The bearer token the
front end has sent with every request since the sign-in screen was written carries the profile id as a
`ClaimTypes.Sid` claim (`MediaPortalCredentials.CreateIdentity`,
`Source/Core/MediaPortal.Common/Services/ResourceAccess/Auth/MediaPortalCredentials.cs:165`, signed through
`MediaPortalAccessToken`, `Source/Core/MediaPortal.Common/Services/ResourceAccess/Auth/MediaPortalAccessToken.cs:70`),
and only `SessionController` ever looked at it. The media endpoint now reads it, asks the library about *that*
person, and both ends of the resume moved with it:

- **Reading.** `GET api/media/titles`, `GET api/media/titles/{id}` and the playback report answer with a
  `resume` figure - the percentage this caller left, nothing when they left none.
- **Writing.** A report from a known person goes to `NotifyUserPlayback`, which stores the percentage in that
  person's own user data (`KEY_PLAY_PERCENTAGE`) instead of only moving the shared counters.
- **Showing.** The "Left half way" strip grew a fourth column, `42% watched`, and a title with nothing stored shows
  an empty space rather than a nought.
- **Acting.** Choosing such a row starts the film there instead of at the beginning, and the screen says
  *"Resumed at 42% - 00:42:00"*, because a film that opens twelve minutes in looks like a broken player otherwise.

**Authentication was already on; only the filter was optional.** `ResourceServer.ConfigureApiPipeline`
(`Source/Core/MediaPortal.Common/Services/ResourceAccess/ResourceServer.cs:156`) calls `UseAuthentication`
unconditionally, while `ServerSettings.WebAutorizationEnabled` only decides whether the `[MediaPortalAuthorize]`
*filter* rejects anonymous callers. So on a server that lets anonymous requests in, `HttpContext.User` still names
the person behind a token that arrived. That is why the resume works without touching the sign-in screen, and why an
anonymous request still gets the library-wide answer it always got.

**An empty id is not a person.** `CallerProfile` folds three cases into "nobody": no `sid` claim, a claim that is
not a Guid, and `Guid.Empty`. The third one looks harmless and is not - `CompiledMediaItemQuery`
(`Source/Core/MediaPortal.Backend/Services/MediaLibrary/QueryEngine/CompiledMediaItemQuery.cs:422`) tests
`_userProfileId.HasValue`, so an empty Guid asks the user-data join for a profile whose id is all zeros, which
matches no rows and reads as *"this person has never watched anything"* on a profile that merely was not named. A
wrong answer is worse than no answer.

**A position is only sent to the person it belongs to.** `resume` is attached when the *request* was made as
somebody, not merely when the library happened to hand data along. A fake library that returns user data to an
anonymous query therefore cannot leak a position to a caller who is not that person, and that exact case is a test
(`ARequestFromNobodyReportsNoPositionEvenWhenTheLibraryHandsOneOut`). The stored figure is MP2's own fixed-width
text (`UserDataKeysKnown.GetSortablePlayPercentageString`,
`Source/Core/MediaPortal.Common/UserProfileDataManagement/UserDataKeysKnown.cs:62`, writes `"042"` so a text column
sorts numerically); the endpoint parses it and reports nought for anything outside one to ninety-nine.

**One report, not two.** When a profile is known the controller calls *only* `NotifyUserPlayback`. That method
calls `NotifyPlayback` itself (`Source/Core/MediaPortal.Backend/Services/MediaLibrary/MediaLibrary.cs:2802-2812`),
so the shared `ATTR_LASTPLAYED` / `ATTR_PLAYCOUNT` counters that sections **ac** and **ad** filter on keep moving -
and calling both would raise the play count twice for one viewing. A test asserts the shared path is *not*
additionally called for a known person, and that an anonymous report still moves the shared counters on its own.
Finishing behaviour is unchanged: `NotifyPlayback` still only counts a completed viewing at a hundred percent, which
is what keeps a resumed film from being counted as watched when it was stopped again.

**One to ninety-nine, at both ends.** The server reports nothing outside that band and the playback screen refuses
a seek for it, which is MP2's own continue-watch interval
(`Source/UI/UiComponents/Media/MediaLists/BaseContinueWatchMediaListProvider.cs:43-46`). Nought means "no position"
- what a title nobody opened carries, and what an anonymous request is answered with. A hundred is a film that is
over, and seeking to it would open a film one second before the credits. A percentage rather than a time, so that
the figure still means the same thing when the file behind a title is replaced with a longer cut.

**The seek waits for a length.** libVLC reports how long a media is only after it is prepared, and a fraction of
nought is nought: a seek issued before the length is known lands at the beginning with the confidence of a command
that worked, and a resume marked as spent. So the resume is armed when the media is opened, and applied the first
time a position event names a length - and not again after that, so that a viewer who rewinds stays where they
rewound to rather than being dragged back on every event.

**What is deliberately not claimed.** The *positions* are per person now; the *filters* are not. `seen=started`,
the watched counts and the tiles still answer for the library as a whole, so the strip can list a title you
personally never opened if somebody else stopped it, and the two states are then distinguished by whether the row
carries a figure at all. Nothing on screen says whose position a figure is; with one profile per front end that is a
fine simplification, with several people sharing a server it is the next thing to fix. The seek itself has not been
watched happening on a real engine - the container has no video output, so it is proven against a fake engine that
reports lengths the way libVLC does, and the frame shows the figure, not the playing.

**Tests.** Seven new server tests (`TestMediaController` 49 → **56**): that the library is asked about the person
the token names and about nobody when no token named one; that a stored `"042"` arrives as `42`; that a request from
nobody gets no position even when the library hands one out; that `100` and `000` offer nothing to resume; and that
a known person's report is stored under their own name *without* also moving the shared counters, while an anonymous
report still moves them. Two new front-end tests (`TestMediaBrowser` 40 → **42**): that `resume` arrives under the
server's own word for it and that its absence is `null` rather than zero, and that the row says `42% watched` where
there is a figure and nothing where there is none. Four new playback tests (`TestNowPlayingScreen` 39 → **43**):
that a title left at 42% is sought to 42 seconds of a hundred-second film and says so; that the resume is one seek
and not a fight with a viewer who rewinds; that it survives an engine that does not yet know the length and is still
waiting when the length arrives; and that nought and a hundred start the film at the beginning. The whole suite on
Windows is **710 tests, 0 failures** - `Tests.Server` (276), `Tests.Client` (90), `Tests.Frontend` (247),
`Setup\Tests` (68) and the portability canary (29).

**The same frame, with a column measured.** The scripted server behind the `continue` frame now stores a position
for three of its four half-watched titles - 12%, 74% and 61% - and deliberately none for the fourth, and the probe
refuses to write the picture unless the strip carries exactly those three figures and the empty space. Sampling the
pixels agrees on its own: the three rows with a figure each carry a **fourth** ink block (`x 1004..1125`, about 120
pixels wide, three rows agreeing to within ten pixels at their left edge) where the row without one stops at
`x 845`; and every row's date column moved from `x 827..997` to `x 675..845`, which is what adding a fourth
proportional column does to the three that were there. The frame drawn before this change has three blocks in every
row and no ink past `x 997`.

### af. The row you watched from says so when the film ends

**What was added.** A viewing used to change the library and nothing else: go back to the list you picked the film
from and the row still said what it said before you pressed play, until somebody pressed Refresh. The report's own
answer now travels on to the screens that might still be showing that title:

- a finished film in the library list gains its tick, its count and its date, and loses its resume figure - a
  title that is over has nothing left to come back to, and a row carrying a tick next to "42% watched" would be
  describing two viewings at once;
- a strip title that was watched through **leaves** the "Left half way" list, and the line above the list counts
  what is on it now;
- a strip title stopped again keeps its row and gains the newer figure, which is the pair the strip exists for:
  the row is the offer, the figure says how much of it is already gone;
- the details panel cached for that title is dropped, because it says more than those three figures and only a read
  can bring the rest of it up to date.

**From the answer, not from a second question.** `POST api/media/titles/{id}/playback` has always answered with the
library's own numbers after taking the report - the count, the date, whether it counts as watched - and section
**ae** added the position to it. That answer is what the rows are rewritten from. Asking again would be a round trip
whose entire purpose is to learn what has just been said, at the exact moment a film ends; and a refresh of the list
would also throw away the order the viewer was reading and the place their selection had reached, which on a shelf
of three hundred films is not a small thing to spend on a tick. `PlaybackReporter`'s callback therefore grew from
"which title changed" to "which title changed, and what the library now says about it" - and it still fires only
when the server *took* the report, which a test holds it to: a refused report leaves every screen alone.

**What does not move.** Rows stay where they are. A list that re-sorted itself the moment something was watched
would move the row the viewer is looking for out from under them, so the tick appears in the row's old place and
the next question asked of the server comes back in the new order. A filtered list is left alone too: the list is
the answer to the question that was asked of it, and the row that has since changed says so on itself. The tiles and
their counts are untouched, because watching a title does not change how many titles there are.

**The strip is the exception, on purpose.** A finished title is taken out of `Unfinished` rather than left on it,
because that list's whole promise is "press this to go back into it". When the last row goes, the strip takes itself
off the screen with it - the same rule that keeps a home screen from carrying a heading about nothing. The count
line keeps saying how many parts of the library refused to answer, from a figure remembered for the last read
rather than recomputed as nought: a note about a silent part of the library must not vanish because somebody
watched something.

**A row had to learn to speak.** `TitleViewModel` was immutable and said nothing when it changed, because rows used
to be built anew for every read. It now holds its `MediaTitle` and replaces it with the library's newer figures,
announcing the change the way the playback screen does it: one notification naming no property, meaning "read all of
them again" - the tick, the date, the position and the length are all read off the same answer, so announcing them
separately would be three notifications about one event.
**What is honestly not claimed.** This is the front end hearing about *its own* viewings. A second television on the
same server still shows its own stale row until it asks again, because there is no event stream from the server to a
front end yet - that is `p2-4-api-events`, and until it exists a media centre cannot tell two screens about one
change. And no frame shows any of this: a picture holds one moment, and the point of this slice is the difference
between two moments, so it is proven by tests against the same in-process server the other list tests use.

**Tests.** Six new front-end tests (`TestMediaBrowser` 42 → **48**, `Tests.Frontend` 247 → **253**): that the
row a viewer finished gains its tick, gains its date, loses its position, and is announced exactly once, while the
server is asked *no additional question* (the fake handler counts its own requests and the count does not move); that
a stopped-again title gains `61% watched` and stays unmarked; that a report about a title this list is not showing
changes none of its rows and answers `false`; that finishing a strip title leaves the other row and recounts the
heading; and that finishing both takes the strip off the screen. The reporter's own test was rewritten to assert what
it hands over rather than the id alone - the library's count, its "not watched", its position of `50%` and its date -
because screens being handed a guess instead of the answer would still have passed the old version of it. The whole
suite on Windows is **716 tests, 0 failures**.

### ag. An album, a series and a collection open into what is inside them

**What was added.** Pressing an album on the new front end used to say "cannot be watched from here", which is true of the file and wrong about the album: an album is not watched, it is opened. The same dead end stood in front of every series, every season and every film collection, so MP2's music and television browsing - two of the pillars the old skin existed for - were simply missing. Both ends of that are now in place:

- `GET api/media/titles` takes `inside=<id>` and answers the titles filed inside that one title; the answer echoes `inside`, so a screen can tell whether the server actually understood the question;
- the question is asked the way MP2's own code asks it - as a relationship between two aspects, combined into the watched filter that was already there, so "the tracks of this album nobody finished" costs one query;
- a parent that cannot hold the kind of title being asked about is **refused** (404 for an id the server does not have, 400 for a pair like films inside an album) rather than answered with an empty list;
- two new orders: `sort=track` and `sort=episode`, the latter season first and number second;
- rows carry what a viewer reads off them: `season`, `episode`, `track`, `artist`, and `trackCount` on the container itself;
- the front end opens a second list when a row holds titles, headed by the parent, with the position in front of the name and the credited artist under it, and it still plays a leaf.

**The library's own links, not invented ones.** MP2 does not store a parent on a child. An episode carries the *name* of its series, which is a string two series can share and which a re-import rewrites; the real link is a relationship row. The three pairs this endpoint asks with are the pairs MP2's own providers already use - `RelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, albumId)` at `LocalAlbumFanartProvider.cs:79`, the series and season variants at `LocalSeriesFanartProvider.cs:77` and `:81`, and the collection at `LocalMovieCollectionFanartProvider.cs:75`. Nothing here is a guess about the schema, and the pairs are only offered where the parent actually carries the aspect they hang off, which is what makes a refusal possible in the first place.

**A refusal is kinder than an empty list.** An empty page reads as "this album holds no films", which is a fact about the album and a lie about the question; the question was wrong, and a server that answers it encourages the caller to keep making it. So an unknown id is a 404 with a sentence naming the id, and `aspect=MovieItem&inside=<an album>` is a 400. A screen that got the pair right sees the answer it expected, and a screen that got it wrong fails at the moment it was written rather than at the moment somebody scrolls.

**Two orders that are not alphabetical.** A shelf of films is read by name; the titles inside something are read in the order they were made to be experienced. Tracks go by the number the album was pressed with. Episodes go by season, then by number - the pair MP2's own Wi-Fi remote sorts by at `WifiRemote/Helper.cs:315` - because a series whose third season is filed under "S" alphabetically is a series you cannot watch. A series leads to its episodes and not to a season screen: one list the arrow keys walk, rather than a screen to press through twice per series. A season is still accepted as a parent by the server, because which of the two an importer files episodes under depends on where the metadata came from.

**Counting a row instead of timing it.** On the front end a container is measured by what is on it: an album row says `5 tracks` where a film row says `117 min`, and it never says "not playable here", because a row that is opened is not a row that failed to play. A track says `4.` in front of its name and the credited artist under it; an episode says `S01E02`. The artist is a second line rather than a longer name, because the name is what a viewer scans for and the artist is what they check afterwards - and it costs nothing to fetch, since MP2 denormalises the credited names onto both the album and the track.

**Asked before the file.** `ChildrenRequested` is a separate event from `PlayRequested` rather than a flag on it, because the two things a viewer can mean by pressing OK lead to two different windows, and a handler that has to interrogate its argument to know which it was given is a handler that gets it wrong the day a server reports a series with a path on it. The question is asked *before* the row is tested for a playable file, and the application's screen factory is recursive, so a collection opens films and an album opens tracks from the same code. What a list of titles is called in the status line follows the same rule: a shelf is named by the tile the viewer pressed ("1 of 1 movies"), a list read out of one title is named by what it holds ("2 of 2 tracks, sorted by track") - "2 of 2 The Dark Side of the Moon" is not a sentence.

**What is honestly not claimed.** No test drives real SQL through the new relationship filter: `TestMediaController` answers queries from a fake library that records what it was asked, so what is proven here is that the library is handed the right filter, the right sort and the right count. The engine that turns that filter into SQL is MP2's own and is covered by its own tests and by the running server, but this endpoint has not been watched doing it on a real database. No frame shows any of it either, because the drill-down is not on the home screen the render probe draws. There is no season screen, pictures and TV radio are still missing as pillars, and a track's length is only as good as the importer that stored it.

**Tests.** Thirteen new server tests (`TestMediaController` 56 to **69**, `Tests.Server` 276 to **289**): that an album's tracks, a series' and a season's episodes, and a collection's films are each asked for with MP2's own pair of roles; that the watched filter and the parent are combined into one question rather than two; that a whole shelf asks about no parent at all; that a missing id is a 404 and a wrong kind pair a 400 with a sentence; what each kind of row carries, including an album nobody tagged saying nothing rather than nothing-known; and that tracks sort by number, episodes by season then number, and a shelf still by name. Fifteen new front-end tests (`TestMediaBrowser` 48 to **63**, `Tests.Frontend` 253 to **268**): that an album row is counted and not timed; the row shapes for tracks, episodes and films; that only albums, series, seasons and collections open, with every unrecognised aspect played instead; the URLs the child lists ask for (`aspect=AudioItem&inside=<album>&sort=track`, `aspect=EpisodeItem&sort=episode`); that a pressed album raises `ChildrenRequested` and never `PlayRequested`, stays selected, and is not refused; that a track is still played rather than opened; that a film is still played; and that an empty album says "this server has no tracks". The whole suite on Windows is **744 tests, 0 failures**, and the render gate still draws the same eight frames it drew before.

### ah. The home screen has a main menu before a server has answered

**What was added.** The home screen used to show one tile per aspect the server reported, which means it showed *nothing* until a machine on the LAN had answered - and after it answered it showed a list of database tables rather than a menu. MP2's home screen has never worked that way: it is a main menu, contributed by plugins as soon as they load, and it is the same set of words whether the library is empty or full. That menu is now on the new front end: `Movies - TV Series - Music - Videos - Pictures - Latest media - TV - Radio - Settings`, each tile carrying either the number of titles behind it or the reason there is nothing behind it, drawn in the first frame and not after a round trip.

**The names and the order are not invented.** They are the entries MP2's own plugins contribute to the home state: `media-actions.xml:5-56` in `Source/UI/UiComponents/Media/Skin/default/workflow` contributes Browse local media -> Browse media -> Audio, Videos, Series, Movies, Images and Latest Media; `SlimTvClient-actions.xml:5` and `:62` contribute TV and radio; `configuration-actions.xml:6` contributes the configuration screen. The new screen drops the three levels of "browse" nesting into one row, orders the media entries as the old skin's tiles did, and keeps MP2's capitalisation. What it deliberately does *not* list is weather, news, utilities, party music and the home editor - MP2's skin plugins contribute those too, but a new front end that opened with five entries it has no screen for would be a list of other people's missing work rather than a menu.

**The menu exists before the server, and that is the point.** `HomeViewModel` builds it in its constructor with `connected: false`, so a viewer who powers on a television before the server box has finished booting gets a media centre, not a dark rectangle with a clock on it. Nothing can be opened yet, and every tile says so with the same three words: `no server connected`. When the aspects arrive the tiles the server can fill pick up their counts, the ones it cannot fill change to `not on this server`, and when the address is changed with "Try a different server" the menu goes back to `no server connected` rather than continuing to offer the previous server's films. Three states, three sentences, because "there is no server", "this server has no music" and "this server has music and none of it is finished" are three different answers, and a viewer can only act on the right one.

**An entry that leads nowhere stays on the menu and says why.** Four of the nine have nothing behind them in this build: *Latest media*, because the API sorts by title, year, last-played, track and episode and the import date such a list needs is none of those; *TV* and *Radio*, because live television came from the TV service that was removed from this tree in commit `029a262aa4` and nothing serves it since; *Settings*, because MP2 keeps its configuration behind a .NET Remoting interface that the new HTTP API has not grown an equivalent of. Hiding them would make the screen look finished; opening them to an empty list would look like a broken library. Instead the tile carries the reason - `no TV backend`, `no settings API yet` - and pressing the tile is refused, with the same sentence put on the status line so the remote gets an answer. The full explanation of each is written out in `MainMenu`'s remarks, where whoever implements the missing piece can read it.

**One line per reason, because a tile is 237 lines wide.** Two rows of five across the 1184 lines the screen leaves after its margins, a character about seven lines wide at the size a subtitle is drawn in: thirty characters is the most a tile can hold, so the model keeps every reason inside that and a test (`TheReasonOnATileIsShortEnoughToBeReadOffIt`) fails if anybody writes a longer one. No reason is shortened into nonsense - `no import-date sort`, `no TV backend`, `no radio backend`, `no settings API yet` each say which of the four things is missing - and the sentence behind each one moved into the remarks of the table it belongs to, where length costs nothing.

**Two rows, not a scroll.** The menu is laid out in a `UniformGrid` with `Rows` fixed at two, and the two is measured rather than chosen: with the same nine entries on the same scripted server, a wrap panel filled the menu with three rows and 211 lines of ink (`y 190..401`), while two rows of five take 126 (`y 190..316`). Those 85 lines are the difference between the row of buttons sitting at `y 605..649` with the footer below it and sitting at `y 670..699`, in the last twenty lines of a 720-line screen. A home menu whose bottom row is off the television is not a menu. `Episodes` and any other server aspect MP2's menu has no word for are added to the columns rather than to the rows, so nine entries or eleven, the screen always holds them all.

**Music means albums.** A tile can be filled by more than one aspect and the order in the table is the preference: `Music` asks for `AlbumItem` and falls back to `AudioItem`, because the shelf a viewer can go on from is the one with something inside it, while a library imported without album metadata still has its music on screen. `Movies`, `TV Series`, `Videos` and `Pictures` each name one aspect (`MovieItem`, `SeriesItem`, `VideoItem`, `ImageItem`). Everything the server holds that no entry claimed - `EpisodeItem` on a server that reports it, or a custom aspect somebody wrote - is appended after MP2's entries rather than dropped, so no part of a library silently vanishes from the screen; `PersonItem` and the genre aspects stay off the menu because an actor is how a library is organised inside, not a part of it you sit down to watch. On the real server in the render gate - which has all twenty-five aspects and no titles in any of them - the two rows read `Movies 0 | TV Series 0 | Music 0 | Videos 0 | Pictures 0 | Latest media no import-date sort | TV no TV backend | Radio no radio backend | Settings no settings API yet | Episodes 0`, and on the scripted three-aspect server of the `continue` frame they read `Movies 12 | TV Series not on this server | Music not on this server | Videos not on this server | Pictures not on this server | ... | Episodes 3`: the same nine words, filled in differently, because what a media centre is does not depend on what a particular server happens to hold.

**Counts and refusals come from the same reading.** The menu is rebuilt where the aspect list is refilled, so a tile's number and the aspect it counts arrived in one response; it is also rebuilt the moment the aspect list alone is known, because whether a server can fill an entry at all does not depend on the counts that come later. A refresh that fails leaves the last known menu on screen - the same rule the aspect rows follow - while the status line says what failed.

**What is honestly not claimed.** The menu is fixed: MP2's home editor, which let a viewer reorder and re-title entries, is not part of this build, and neither are the skin plugins that contributed weather, news, utilities and party music. Mouse and touch are untested on this list; the tests drive it the way a remote does, by selecting an entry and raising Enter. `Pictures` will read `0` against a real MP2 server until something imports images, and `Latest media`, `TV`, `Radio` and `Settings` say what they say until their backends exist. One thing noticed while re-copying the frames and left where it was found: the bottom status line of the *server-picking* frame differs by a few hundred pixels between two runs of the same probe with the same scripted state, so that one picture is not bit-for-bit repeatable yet; the gate counts pixels rather than comparing them, so nothing hangs on it, but whoever makes that screen's frame deterministic should be able to prove it with two runs and one comparison.

**Tests.** Eleven new front-end tests (`TestMediaBrowser` 63 to **74**, `Tests.Frontend` 268 to **279**): that the menu is complete before any server has answered and none of it opens; that the nine labels are MP2's and in MP2's order, cited to the workflow files; that an entry with nothing behind it is refused with its reason on the tile and in the status line; that no reason is longer than a tile can hold; that Music leads with albums and falls back to tracks; that a server aspect nobody claimed still gets a tile after MP2's own, while `PersonItem` does not; that a tile's count is the server's own number; that a server which answered with only an unwatchable aspect offers nothing to open; that having an address is not having a server; and that pointing at a different server empties the menu back to `no server connected`. One existing UI test was retargeted from the old tile list to the menu, and one renamed so that it says what the screen now does. The whole suite on Windows is **755 tests, 0 failures**.

**Frames.** `home-windows.png` and `continue-windows.png` now show the menu over the "left half way" strip, and the probe still refuses the picture unless the strip holds exactly those four titles in that order with those three positions. Getting there caught a second thing: the probe took the home picture while the screen was still asking, one request per aspect, for how many titles each holds - so the tiles read `Movies ?` of a server that answers `0`. It waits for the screen to stop reading now, prints the menu it drew as a `menu:` line so that the claim about the frame is a sentence the run makes rather than one a reader picks out of a picture, and refuses a `continue` frame whose menu does not offer exactly the two parts the script has. Measuring the refreshed frame says where the screen actually is: menu labels at `y 190..208` and `y 274..293` with their reasons at `y 222..231` and `y 305..316`, the strip's heading at `y 360..376`, its four rows at `y 401..416`, `451..466`, `501..516` and `551..566`, and the buttons and footer at `y 605..649` - all of it inside 648 lines. Four other Windows baselines (`details`, `search`, `nowplaying`, `connect`) had drifted: each was last copied for the slice that changed it, and the slices since then had changed those screens without re-copying them, so the committed pictures no longer matched what the code draws. All six refreshed frames are in `docs/multiplatform/screenshots/`; the playback frame still measures what the probe says it measures (`2120 distinct colours, 265 700 high-contrast edges`, all eight bars read back out of the picture), so its numbers did not move.

### ai. Titles in the shelf now carry the picture the library holds

**What the server gives.** `GET api/media/items/{id}/thumbnail` answers with the bytes MediaPortal 2 already stores for
a title: the `ThumbnailLargeAspect` blob, sniffed for PNG, JPEG, GIF or BMP and sent with that media type and
`Cache-Control: private, max-age=86400`. A title with no stored picture is a `404`, which is an ordinary answer and not a
failure - the client caches it as "this one has none" - while a server without a library answers `503`. An unknown id, an
aspect that exists but holds nothing, and a title whose picture was never imported all answer `404` too, so the front end
never has to tell those apart. The query asks for that one aspect and no other, and a poster is nobody's personal data, so
the read does not go through a user's view of the library the way the title lists do.

**Bytes in the view model, pictures in the view.** The split is deliberate: `MediaBrowserViewModel` asks for `byte[]`,
`TitleViewModel` holds bytes, and only `ArtworkConverter` in `Views/` turns them into an Avalonia `Bitmap`, cached under a
pixel budget with the evicted bitmaps disposed. That is what keeps the view models testable, and the reason it matters
here is measurable: the headless test harness runs with `UseHeadlessDrawing = true`, where `new Bitmap(anything)` succeeds
and returns a 1x1 bitmap for any input at all - a real PNG, a PNG header with nothing behind it, or twenty bytes of ASCII
alike. So no assertion made in the headless suite can prove that a picture decodes; those tests assert only that the
converter caches by identity and hands back what it was given. Whether a poster really becomes pixels is claimed only by
the render gate, which draws with Skia and reads the frame back.

**`Artwork` - one fetch per title, however often it is looked at.** Five rows are on screen at a time, every refresh looks
at them again, several titles are looked at twice as the shelf scrolls, and the details panel asks again for the row it was
opened on. The class in `ViewModels/Artwork.cs` gives each title at most one request in flight (a `TaskCompletionSource`
registered so a second caller joins the first rather than starting a second HTTP call), remembers answers - including
"there is none" - for up to 200 titles least-recently-used, and keeps at most four requests on the wire at a moment. A
title whose bytes arrived corrupted counts as answered, so one broken file in the library does not make the front end ask
again every time that row scrolls past.

**A cache written for one thread, used by several.** Running the whole suite after the artwork tests had been added failed
once, in `TestArtwork.OnlyALittleIsAskedForAtOneMoment`, with "Operations that change non-concurrent collections must have
exclusive access" from `Artwork.Remember`. The class had been written assuming it was called from the UI thread, which held
while callers awaited on a dispatcher - but HTTP answers arrive on thread-pool threads, and so does everything a unit test
uses. Every table mutation now happens under one lock, the awaited fetch is completed outside that lock so no HTTP callback
runs user code inside it, and the counter is `Interlocked`. The fix was checked with a new test
(`PicturesAskedForFromSeveralThreadsAtOnceAreFetchedOnceEach`) that releases 48 concurrent requests for 24 titles from 48
thread-pool threads and asserts each title was fetched exactly once; four repeat runs were green. The test's own fake
source needed the same treatment, which is worth saying out loud: a fake whose counters race can turn a green run into
luck.

**Measurement found the details panel eating the shelf.** Before any of this could be verified the details frame had to be
made honest, and the first measurement showed something wrong in a frame that had been committed for slices: with the panel
open, the shelf behind it drew nothing. The pixel numbers said why - a rectangle in the panel's border colour covering
`x 72..1279, y 182..654`, the whole content area, in the old baseline and the new one alike. The panel sits in a grid column
of `Auto` width and its plot text has `TextWrapping="Wrap"`; an `Auto` column measures wrapped text as if it were unwrapped,
so the panel's `MinWidth="420"` was a floor with no ceiling and the column took the window. It now has a `Width` of its own
(440) and the row's minimum width dropped from 760 to 700 so that rows still fit beside it. After the fix the details frame
shows both: the shelf's posters *and* the panel's own picture, 57 600 pixels of it at `x 892..1131, y 206..445`. The headless
suite never saw this because it asserts on view models, which were correct, and the probe never failed on it because a frame
with a panel and no list is still a colourful frame. That is the gap the new `pictures:` check in the probe closes.

**What the gate now proves.** `CheckPosters` runs on the browser and details frames. It measures the row pitch from the frame
rather than from the XAML (101 lines here), expects film 1's pink poster patch in the picture column of row one and film 3's
teal patch two rows down the same column, expects the placeholder box rather than a poster on the row whose stored bytes are
junk, and on the details frame expects a picture in the panel and *no* teal patch on the right half - a panel showing the
picture of a different title would be caught by exactly that clause. Frame statistics moved with the layout: `browser` 2207
colours / 120 481 edges, `details` 3689 / 167 490 (it draws a shelf again), `search` 1529 / 102 617, `seen` 2191 / 116 226;
the band below the first row now holds 683 distinct colours against two on the filtered frame. Five baselines were re-copied
and `browser-windows-skia.png` is committed for the first time. `scripts/ci/README.md` carries the measurements, and the suite
stands at **784 tests, 0 failures**: 14 new server tests for the endpoint (`Tests.Server` 289 -> **303**) and 15 new
front-end ones in `TestArtwork` (`Tests.Frontend` 279 -> **294**).

**What is honestly not claimed.** A real MP2 library's posters come from importers that this tree does not run, so against a
stock database the shelf will mostly show boxes - the picture path is exercised by the gate's scripted bytes, not by a
populated library. There is no batch picture endpoint: each row asks for its own thumbnail, bounded by the four-at-a-time rule
rather than by one request per screen. Nothing is resized or transcoded server-side, so a 64-pixel box downloads the stored
blob whole, and a title remembered as having no picture is not asked about again until the front end restarts - importing
artwork into a library that a running front end has already looked at will not show up. The still that belongs on the
now-playing screen and on the home strip is still missing, as is the pictures pillar itself; and the thumbnail endpoint is
only the *reading* half of item **p2-3**, whose streaming-with-ranges half is still open.

**A plugin that was one stale reference away from portable.** The library screens can draw a picture, but nothing in this
tree could fill a library: `deploy-server-layout.ps1` deployed exactly one plugin, `SQLiteDatabase`, and the plugin loader
scans `<APPLICATION_ROOT>\Plugins` - which `LoadPluginsData()` treats as optional, so a server with no plugin directory
starts happily and simply has no metadata extractors. Files get walked, nobody answers, the library stays empty, and every
screen looks like a design problem when it is an import problem. The candidate to deploy first was
`NfoMetadataExtractors`: it reads `.nfo` XML and local `folder.jpg` off disk, needs no network for the common case, and it
writes `ThumbnailLargeAspect` - the exact aspect the thumbnail endpoint serves. It was one of 46 projects still on
`net10.0-windows`, and `CA1416` was clean there, which means the target framework was never justified by an actual platform
call. Two things pinned it: `System.Net.Http.WebRequest` (a package that does not exist for plain `net10.0`, and nothing in
the project uses a single type from it) and a `ProjectReference` on `OnlineLibraries`, which is the one extension that
genuinely needs Windows - it resizes TVDb banners with `System.Drawing`, and `System.Drawing` propagates its platform
attribute to everyone who references it.

**A dependency that had quietly gone stale.** Removing the `OnlineLibraries` reference looked like it would cascade and
produced two compile errors, both of them `using` lines. Exactly one call site really used the library: when a `.nfo` file
is not XML at all but contains an IMDb id, `NfoMovieReader` asked `OnlineMatcherService.Instance` to fetch the title over
the network. That path is a bonus, not a requirement, so the plugin now asks for `IOnlineMovieMatcher` - a one-method
interface in `MediaPortal.Common.MediaManagement`, next to the other importer-facing seams - through
`ServiceRegistration.Get<IOnlineMovieMatcher>(false)`, and when no online source is registered it logs, stores nothing, and
leaves the file for a later run instead of failing the import. `OnlineLibraries` registers itself under the new interface
too, so behaviour on a Windows server is unchanged. What was deliberately not done is registering `OnlineMatcherService` a
second time with a second interface: the plugin manager constructs a new object for every `<Service>` entry, so that would
have produced a second instance of a class written as a singleton - the registered class is a three-line forwarder that
calls through to `Instance`. Both branches of the seam are pinned by tests (a registered matcher is asked with the id from
the file and the title is stored; with no matcher nothing throws and nothing is stored).

**A plugin folder has to be self-contained, and that is a build setting.** The plugin's stub parser is written against
`Newtonsoft.Json`, which used to arrive because the `OnlineLibraries` plugin loaded it into the same process. So
`plugin.xml` now lists `Newtonsoft.Json.dll` as an assembly of the NFO plugin itself, and its `build.targets` copies the
file from the project output into the plugin directory - except that the copy was a no-op for a reason worth writing down:
**package assemblies only reach a project's output when the project sets `CopyLocalLockFileAssemblies`**, and this project
did not, so `$(TargetDir)Newtonsoft.Json.dll` did not exist and MSBuild copied nothing without complaining. That is the
mirror image of the `Plugins\News` case in `scripts/ci/README.md`, where a copy list named a DLL the plugin did not depend
on: here it named a file the plugin did depend on and that never appeared. The layout script now refuses to
produce a plugin directory that is missing a file its plugin needs, which is what turned the silent copy into a failure.

**What the layout now assembles is still not proof that the plugin works.** `deploy-server-layout.ps1` reports `17
assemblies, 2 plugin(s): NfoMetadataExtractors, SQLiteDatabase`, and `Plugins\NfoMetadataExtractors` holds the plugin
assembly, its `plugin.xml` and `Newtonsoft.Json.dll`. That is a directory satisfying the loader's contract; that the server
starts with it, that the extractors are registered under `/Media/MetadataExtractors`, and that a folder of video files plus
`.nfo` files actually becomes titles in the database are the next steps and are not claimed here. The suite stands at **786
tests, 0 failures** (`Tests.Server` 303 -> **305** for the two matcher-seam tests).

### aj. A folder of files becomes a title, on any platform, and the server says where the film is

**A gate that asks about content rather than code.** `scripts/ci/test-server-import.ps1` deploys the layout, writes a film
folder the way a scraper leaves one (`.mkv`, `movie.nfo`, a poster the nfo names), starts the headless server against a
scratch data directory, registers the folder through `POST api/media/shares`, and then asks the HTTP API whether the film
arrived: the title and its year, the path a player would open, the plot, tagline, certification and runtime out of the
nfo, an import queue drained to zero, and the title gone again after `DELETE api/media/shares/{id}`. Every gate before it
asked the server about code; this one asks it about a library, which is the question the media centre lives on. It failed
for three separate reasons before it passed, and each of them was a real defect.

**Nothing in the portable layout ever called a file a video.** The extractor chain that fills a library is "something says
this resource is a video, then the nfo readers may read it". The only extractor that says so upstream is
`VideoMetadataExtractor`, which opens containers with MediaInfo, OpenCV and TagLib behind a `net10.0-windows` /
`UseWindowsForms` project - so with the two portable plugins that a layout actually deploys, every nfo extractor bailed at
"this resource is not a video", the importer walked the share from top to bottom, saved nothing, and logged no error at
all. The first run of the new gate proved it: `MetadataExtractorBlock: processed 0`, an empty shelf, no failure line.
`VideoContainerMetadataExtractor` closes that gap inside the nfo plugin: priority `Core` so it runs before the `Extended`
nfo extractors, `processesNonFiles` so a film folder is claimed as the item it is, and the decision made from the
extension alone through `MimeTypeDetector.GetMimeTypeFromExtension` - the content-sniffing overload of the same helper
asks `urlmon.dll` what it makes of a file's first 256 bytes, which is both Windows-only and a read of every file in the
library on every run. For a file it also notes that resource as the item's primary provider resource, which is MP2's
convention - the extractor that recognises a resource is the one that knows what the item *is*, and everything that later
asks the library "where is this film" reads that back. It writes no stream values on purpose: duration, resolution and
codec stay unknown until an analyser that can read a container with managed code exists, and an aspect with no values
contradicts nothing that a real analyser next to it might write. 33 tests, including that it disposes every child
accessor `GetFiles` hands out and that it leaves a directory without a video alone.

**An aspect is not a category.** `GET api/media/titles?aspect=Movie` answers `404 - this server has no aspect called
'Movie'`, because the endpoint is asked for media item *aspects* (`MovieItem`, `EpisodeItem`, `VideoItem`, ...) while the
shares endpoint next door is asked for media *categories* (`Movie`, `Series`, ...). The gate asked for the category and
concluded the import had failed. Both endpoints now say what they accept: the titles endpoint's `404` enumerates every
aspect name the running server manages and points at `GET api/library/aspects`, and its examples and refusals name
`MovieItem`.

**The database had been dropping half of every answer, silently.** With the import working, the title still came back with
`path: null`. It was not the import: the ids of a query's results and the ids of the rows belonging to them were different
bytes. `SQLiteDatabase.SaveTemporaryIds` filled the query engine's temporary id table by reusing one prepared parameter and
assigning a `Guid` to it, which the driver binds as text, while that very provider stores every other id as sixteen bytes
(`SQLiteValues`, so that databases written by the old `System.Data.SQLite` driver stay readable). Everything the engine
cannot fetch in its main select is read through that table - every multiple aspect, so the provider resource a title's path
lives in, the genres of a film, the actors and directors as relationships, editions, and per-user data such as a resume
position, plus the long texts - and all of it came back empty for every paged query, with no exception anywhere. After the
fix the same request answers with the path, and `GET api/media/items/{id}` returns the nfo's plot (MediaPortal 2 keeps it
as `VideoAspect.StoryPlot`, which is why the field is `storyPlot`), tagline and runtime, and its certification as `US_PG`
because that is how MP2 normalises an `<mpaa>` rating. The regression test inserts two rows, fills a temporary id table the
way the provider does, and reads the rows back through it; against the previous binding it finds none of them. That test
fails for the right reason and only for that reason, which was checked by reverting the fix and watching it fail.

**What the gate still does not prove.** Pictures are the honest gap: the nfo readers deliberately keep a `<thumb>` as a
stub for the fanart handler (`MovieFanArtHandler` copies it into the fanart cache as the poster) and leave
`ThumbnailLargeAspect` empty - its writer is commented out upstream - so a title imported from nfo has nothing for
`GET api/media/items/{id}/thumbnail` to serve until the fanart cache is reachable over HTTP, which is the pictures pillar on
the backlog. The imported title also has no duration or resolution, for the reason above. And the gate runs the layout it
deploys, which is win-x64; what travels to Linux and Android today is the extractor's own unit tests, not this gate.

The suite stands at **842 tests, 0 failures**, with `Tests.Server` at **361** where the last commit left it at 305:
33 tests for the new extractor, one for the temporary id table, and the 22 that came with the shares API in the same
push.

### ak. A person can tell the media centre where their media is, from the television

**"Only 3 test buttons in home" was measured before a line was written, and it was not a drawing bug.** The home menu is
built from the aspects a server reports, so a front end with nothing to read shows its three buttons - Refresh, Now
playing, Server - and nothing else. Pointed at a server holding a library a person would recognise (four films described
by `movie.nfo`, two video files with nothing describing them at all, and the real `C:\Users\ge230\Videos\Alien Earth
(2025)`, which is scraped with a `tvshow.nfo`, per-episode nfos, posters and fanart), the same build drew the menu with
the server's own numbers: `Movies 4 | TV Series 1 | Music 0 | Videos 14 | Pictures 0 | Latest media no import-date sort |
TV no TV backend | Radio no radio backend | Settings no settings API yet | Episodes 8`, 27 aspects, PROBE OK. What that
measurement left as the real gap was not the drawing. **Nothing in the new front end could add a folder.** MP2's own
shares editor is a window on a configuration tool that runs on Windows, so the server that the previous section gave an
importer could be fed from the sofa on no platform at all - and on a fresh install on Linux or Android it could serve a
library it could never gain.

**The screen is `Views\FoldersWindow.axaml` with `FoldersViewModel`, on five new client calls** -
`GetSharesAsync`, `AddShareAsync`, `RemoveShareAsync`, `ImportShareAsync`, `GetImportStatusAsync` - which sit on a verb
helper in `ServerApiClient` that the playback reporter was moved onto as well, so there is one place that knows how a
request is written and a refusal becomes an exception. The folders are a list on the left, wide, because the path is the
thing a viewer checks ("is that the drive I meant?"); the things to do with a folder are a column on the right, because
they are used once each. Under the path field is one tick box per media category the *server* offers - the five the demo
server manages are Movie, Series, Video, Music and Picture - and under them one line about the import queue.

**Four decisions on that screen are worth their reasons.** Nothing is ticked when the boxes arrive, and an add with
nothing ticked is refused on the screen rather than sent, because the server would take the folder under every category
it knows and a music folder read by the video extractors arrives as noise in the film list. Registering a folder already
queues its import inside the library, so the screen queues nothing of its own and follows the queue instead: the line
says `importing 1 folder`, then `nothing being imported`, and the "Import finished." underneath is said only when the
screen watched something being done - a queue that was empty the whole time would otherwise erase the `Added "Films" as
Movie.` sentence, which is the sentence the viewer pressed the button for. Forgetting a folder asks twice, hands the
"press remove again" to the row the viewer is already looking at, and takes it back the moment another row is picked,
because a share's titles go with it. A folder registered by another system is listed with that truth rather than with a
button that would only be refused. And the tick boxes do not exist when the screen is built - they arrive with the
server's answer - so the arrow ring is rebuilt when the collection changes; Enter means the field's confirmation in the
field and the journey to the buttons on a row; Up and Down leave the field while Left and Right stay with the caret,
because retyping a UNC path with a remote is not a thing to ask of anybody.

**It is proven three ways.** Sixteen tests in `TestFoldersScreen` run the *real* `SharesController` in-process against
real temporary directories, with only the library, the resolver, the accessor and the importer faked
(`SharesFakes`); they cover what the list says, what a foreign folder says, an empty library, no server, an add with the
categories the boxes were ticked to, an add refused for no category, a refusal for a folder the server cannot see, a
folder that is already shared, the two presses of a delete, the queue followed until it empties, a paused queue, and what
each of the server's refusals becomes on screen. Four more in `TestFoldersWindow` load the XAML on the headless platform
and assert that the rows arrive, that the arrow key leaves the path field and lands on the first tick box (which is the
whole reason the ring is rebuilt), that Enter in the field registers the folder it holds, and that Back closes the
screen and leaves the library exactly as it found it. The visible proof is
`build\artifacts\folders-live.png`, rendered by the probe's new `--screen folders` against the running server: three
folders, five media types, one of them the real `Alien Earth (2025)`, and the queue line below the add button.

**Two defects the screen's own tests caught, both in the view model.** `Dispose()` threw `ObjectDisposedException` the
second time it was called: the screen calls it when it closes, and the source whose run had already finished was still
sitting in the field waiting to be cancelled - so two owners, which is the normal case here, crashed the front end on
its way out. It is idempotent now and cancels quietly. And `DescribeAddFailure` handed a 503 through as `The server
answered 503 (ServiceUnavailable) at http://127.0.0.1:…`, which is a sentence for a log and not for a television; the
number is now replaced with what can be done about it. `Tests.Frontend` is also a friend of the front end assembly now
(`AssemblyInfo.cs`, one attribute, one comment) for two members only: the task that watches the queue - a screen that
has to stop watching when it closes can only be proven to stop by waiting for the watch - and the interval it waits at,
because a suite that slept through the real second and a half would spend minutes to say nothing interesting.

**A hazard that was already written down and was walked into anyway.** Closing the in-process API from the display
thread deadlocks the test host with no exception and no message, because the host's shutdown is a blocking wait for an
awaited stop whose continuation is handed to the dispatcher that the wait is blocking. `TestHomeWindow` disposes its
hosts on a thread-pool thread for exactly this reason and says so in a comment; the folders fixture does the same.
Finding it again cost a dozen runs of a suite that simply never finished, which is the argument for writing hazards down
where the next fixture will read them.

**What this does not do.** The home menu now has one entry that is not MP2's own - `Folders`, last but one, in front of
the `Settings` it cannot open yet - and the two tests that counted MP2's entries were updated to say why rather than to
count a different number. The screen edits shares and nothing else: the rest of the server's configuration is still
behind .NET Remoting, still `p2-5-api-settings` on the backlog. The two video files with nothing describing them still
arrive with an empty title, because the extractor deliberately does not invent one from a filename; the display-side
fallback is on the backlog with the pictures, which still 404 for anything imported from nfo. And the queue line counts
jobs, not files - one job can be a hundred thousand files and the server has no count to give - so it says
`importing 1 folder` where a progress bar would have had to invent a percentage.

The suite stands at **862 tests, 0 failures**, with `Tests.Frontend` at **314** where the last section left it at 294:
the twenty that came with this screen.

### al. Titles get their pictures back

Every thumbnail the front end asked for came back 404, for any title, on any server. That was not one bug but three
underneath each other, all in the nfo readers, and each of them on its own was enough:

1. NfoReaderBase.ParseSimpleImageAsync accepted Uri.UriSchemeHttp and nothing else. Every artwork CDN that has been
   contacted since then answers on https, so every picture named by a modern nfo-file was thrown away with the
   comment "unsupported scheme". It now takes a scheme through IsPictureUrl, which admits http and https and refuses
   ftp:, file:, relative addresses and blanks - the refusal is still a refusal, it just stopped including the one
   scheme the whole internet uses.
2. All five readers had the writer for the aspect thumbnail (TryWriteThumbnailLargeAspectThumbnail) **commented out**,
   with a note that the FanArt collector handles it. That collector lives in the *online* Movie/Series/Audio
   extractors and writes a disk fanart cache; nothing in this port deploys it, and the layout's Plugins directory
   holds NfoMetadataExtractors and SQLiteDatabase only. The writers are registered again, hardened against the
   empty-array and count-0 cases they used to trust.
3. The thumb element itself was registered only when the reader was constructed with includeFanart, and both deployed
   extractors pass false. So on a machine with the fanart collector missing - every machine - pictures were not merely
   unwritten, they were never parsed. The thumb element is now registered unconditionally in the episode, film and
   series readers; fanart, discart, logo, clearart, banner and Landscape stay behind includeFanart, because those
   *are* the collector's business.

On top of the repair there is now one rule MP2 never had, because it is the rule the tools that write nfo-files
actually assume: **a picture lying next to the media is used when the nfo-file names none.** An episode takes
"<nfo basename>-thumb.jpg" (or .png), a film takes poster, cover or folder, a series takes poster, folder or tvshow -
the name-carrying candidates first, .jpg before .png. A picture named by the nfo-file wins over one found by
convention, a file that is empty or unreadable is *not* a picture, quick mode reads none, and the series poster is
stored with Aspect = Poster so the shelf is not handed a season poster or a fanart by accident. An episode never picks
up its series' poster: it looks only for a still named after itself.

Tests.Server\NfoMetadataExtractor\TestNfoPictures.cs pins all of that down offline, thirteen tests against real
temporary folders: named-by-nfo for episode, film and series, poster preferred over the other thumbs a series holds,
an empty file is no picture, the sibling -thumb.jpg used, an episode refusing the series poster, poster.jpg found in a
film's and a series' folder, a named picture winning over a sibling, nothing found meaning no picture, quick mode
reading nothing, and IsPictureUrl on seven addresses.

A live server says the same thing as the tests. build\artifacts\demo-pictures.ps1 deletes its own data directory,
starts the headless server against it and imports three shares from scratch - four demo films whose nfo-files name no
picture, two undescribed video files, and the eight real episodes of Alien Earth (2025):

    served for episode 'Alien: Earth S01E04 - Beobachtung'
      the nfo-file names: https://image.tmdb.org/t/p/w1280/hzE6VJ37veL4p54IicLcI5pDDSJ.jpg
      image/jpeg of 101740 bytes; the picture the nfo-file names: True; the file lying beside the media: True
    served for film 'Alien'
      image/png of 892 bytes; the cover in its folder: True
      MovieItem : 4 titles, a thumbnail was served for 1 of them
      EpisodeItem : 8 titles, a thumbnail was served for 8 of them
      SeriesItem : 1 titles, a thumbnail was served for 1 of them
    titles with a picture served: 10, without: 3

True there is a SHA-256 comparison, not a status code: the bytes GET api/media/items/{id}/thumbnail serves for the
episode are the bytes of the https image its nfo-file names, and the bytes served for the film are the bytes of
poster.png lying in its folder, which no metadata mentions. The three titles left without a picture are the three
demo films whose folders hold no cover at all, and that is the answer this rule promises.

**The frame is the end of the road.** Frontend.RenderProbe --screen browser used to refuse --server and list a
scripted shelf, so the picture a person cares about - the poster to the left of a row - was never once drawn from a
real library. Given --server it now asks that server which aspects it has, takes the one --aspect names (MovieItem by
default) and lists it, then prints three lines and fails rather than rendering an empty or colourless shelf:

    content:  4 title(s) under "Movies", status: 4 of 4 movies, sorted by title
    posters:  1 of 4 title(s) on the frame carry a picture
    heading:  "Movies" on the screen, "Movies" in the view model
    pictures: the bytes the server sent are a picture on the screen (4096 px of rgb(212,64,168) at x 60..123, y 401..464)

The demo cover is a *lossless square PNG of one flat colour* on purpose: a JPEG would be re-coloured by its own
compression, so the frame can be searched for rgb(212,64,168) and find exactly the 64 by 64 picture square of the row
that was given that file. A shelf of episode stills is photographs, so for any aspect but MovieItem the probe says so
and leaves the count as its claim; --aspect EpisodeItem reports "8 of 8 title(s) on the frame carry a picture" and the
frame shows eight different film stills. The heading: line was added because the live shelf had been titled with the
aspect's storage name: a shelf is now titled with the catalogue's own word - Movies, Episodes - which is what its tile
at home says too.

**What this does not do.** The album and artist readers still have their thumb element and their writer switched off;
they are the same repair, and this library has no music in it, so they stay on the backlog rather than be changed
untested. MP2's online metadata extractors and their disk fanart cache are still not deployed - every picture here is
stored in the media database and served from it, which is simple but means a re-import re-downloads. The scratch
drivers run their servers with web authorization off *in the data directory they create*, because a headless database
from scratch holds no user profile to sign in against; what a stranger is answered with when authorization is on is
pinned down by the authorization tests, and the real login road belongs with p2-5-api-settings. Two undescribed video
files still arrive with an empty title, and a title whose server has no picture still gets the placeholder box rather
than a generated one - both unchanged, both still on the backlog.

The suite stands at **875 tests, 0 failures** - Tests.Server at **374** where the last section left it at 361, the
thirteen that came with these readers - and the front-end render gate still draws every scripted frame green,
including the two scripted posters and the row whose stored bytes are not a picture.
### am. The film comes out of the server: a title's file over HTTP

A shelf you can read and a film you can watch are two different achievements, and the front end had only the first
one away from the machine that holds the files. `GET api/media/titles` hands out a title's primary resource as the
server's own serialized path - `{provider-id}://D:/films/Alien Earth - S01E01.mkv` - and `MediaLibraryPath` turns
that into something to open if, and only if, this operating system can name it. A television in the living room
whose files live in the cellar, a Linux client that was never given the Windows share, an Android box that cannot
open a path at all: all three could list the library and nothing else, and every row they showed said "cannot be
watched from here" - truthfully, and uselessly.

**The endpoint is addressed by id, and that is the whole design.** `GET` and `HEAD api/media/items/{id}/stream`
look the title up in the library, open its primary resource through the resource provider, and send the bytes. MP2
already had an HTTP file server - `ResourceAccessModule`, mounted by `ResourceServer`'s constructor - but it is
addressed by a serialized resource path in the URL, and its own source says why that is not enough:
`IsAllowedToAccess` is a `TODO` that returns `true`. A path-addressed file server behind a media centre hands
whatever the server's process may read to anybody who can name a path, including the paths that were never in
anybody's library. An id-addressed one cannot: the largest set of files a caller can ask for is the set the
importer registered, which is exactly what the titles endpoint already lists. The path never leaves the server.

**Ranges are not a nicety here, they are the picture.** `FileStreamResult.EnableRangeProcessing` is on, so ASP.NET
answers a `Range` header with `206` and its own `Content-Range`, refuses an impossible one with `416`, and
advertises `Accept-Ranges: bytes` - and `HEAD` had to be mapped explicitly (`[HttpHead]`), because this pipeline
does not answer `HEAD` from a `GET` route and answered `405` at the first player that asked how long the film is.
A Matroska keeps its cues after the clusters and an MP4 keeps its index at the end, so a player that cannot seek
gets sound and a black screen - which is precisely the failure this tree had already been shown once, from the
other direction. The failures are split the way the sentences are: `404` for an id with no title or no file
recorded, `410` for a file the server names and cannot open ("your library is right and your disk is not
answering"), `503` while the library is still starting, and `403` from the bearer filter like everywhere else in
the API. The media type comes from MP2's own `MimeTypeDetector` table rather than magic bytes, because the sniffing
that table is paired with reads a local path. One lifetime problem came with it: ASP.NET disposes the response
stream and nobody disposes the accessor, so `ResourceReadStream` - a pass-through that keeps seeking intact - owns
the accessor and closes it with the response, and a test polls for exactly that close. Without it every watched
film leaks a provider connection.

**What the client does with the address.** `ServerApiClient.GetStreamUri` returns a `Uri`, not bytes and not a
`Task`: a film is opened by the player, which seeks about in it, and a front end that buffered a film to hand it
over would be a proxy server with a progress bar. `TitleViewModel` now carries both - `LocalPath` and
`StreamAddress` - and `PlaybackLocation` is the one thing the player is handed: the file when this machine has the
file, the address when it does not. The file wins where both exist, and not for speed alone - watching on the
machine that holds the library then depends on no socket for the next two hours, and a server restart mid-film does
not reach the viewer. `MediaLibraryPath.CanBeStreamed` is the new half of the decision, because the endpoint sends
*one file*: a title whose path is a chain of providers (an entry in an archive, a stream a virtual provider
transforms) has no single file behind it, so its row keeps saying it cannot be watched rather than offering an
address the server will refuse and the player will fail on in silence.

**Measured, twice.** Eighteen tests drive the endpoint over a loopback socket against `ResourceServer`'s real
pipeline - `ConfigureApiServices` and `ConfigureApiPipeline`, not a hand-called controller - because the
`206`/`Content-Range`/`416`/`Accept-Ranges` behaviour is the pipeline's doing and invisible any other way. They pin
the whole file byte for byte, an exact ranged slice, an open-ended range, `416`'s `bytes */<size>`, `HEAD`, the
`404` wordings, the three `410` cases, the `503`, and that the accessor is closed once the response is written.
Then the real film, with `scripts\ci\test-player-file.ps1 -Stream`: the same 665.6 MB Matroska a person is
watching is registered as a title reached through MP2's real `LocalFsResourceAccessor`, and libVLC is handed the
address the front end would hand it - not a path.

    stream:   http://127.0.0.1:63623/MediaPortal/Server/api/media/items/82372b08-.../stream
    bytes:    first 4096 identical to the file, type video/x-matroska
    range:    bytes 697945998-697950093/697950094 answered byte for byte
    length:   1:01:07 over HTTP
    seek:     asked 0:20:22, arrived at 0:20:22 over HTTP
    pictures: 100 of 1920x804, decoded 242 audio 501
    hardware  Using D3D11VA (Intel(R) HD Graphics 630) for hardware decoding

The duration line is the range test a decoder performs by itself - a player only learns a streamed film's length
after it has sought to read its index - and the picture line is the one that used to be missing. Fourteen real
playback tests pass, half of them from disk and half over the endpoint, against the same `ffprobe` numbers.

**What this does not do.** The player is pointed at a server in the test process on a loopback port reading a file
on the same disk, so everything past the socket is proved and nothing about a real network is; the honest
claim is about the endpoint, the range handling and libVLC's HTTP access module. A server with web authorization
on still cannot be *streamed from* by a player that has no ticket: `MediaPortalBearerHandler` reads only the
`Authorization` header, and a URL handed to libVLC carries no header - a token in the query string would put the
ticket in every proxy log and browser history between here and there, so it is not done; what such a server
answers is pinned by the `403` tests, and playing through an authorized server needs either the player to set a
header or a short-lived single-purpose URL, which is its own decision. Multi-resource titles (archive entries,
transformed streams) stay unplayable and now say why in the same words as before. The path-based
`ResourceAccessModule` is still mounted and still answers anything, and that is reported to the owner rather than
quietly changed under them. One range per request is all that is supported, which is all any player in this tree
asks for.

The suite stands at **896 tests, 0 failures** - Tests.Server at **392** where the last section left it at 374, the
eighteen that came with the endpoint, and the front end at **317**, where four new tests decide what a row is
allowed to promise and the one that used to promise "cannot be watched from here" for a title the server can
stream is gone. Both gates are green: `test-frontend-check.ps1` reads a running server and refuses a dead one, and the
render probe still draws every scripted frame. One PowerShell trap surfaced on the way and is worth remembering for
every script in this repository: variable names are not case sensitive, so the `foreach ($stream in $probe.streams)`
loop in `test-player-file.ps1` *is* the script's new `-Stream` switch, and assigning a media stream to a `[switch]`
fails the run in a place that has nothing to do with the change.

### an. The file server keeps its hands off everything that is not media

The previous section ended by saying that the path-based `ResourceAccessModule` was still mounted and still
answered anything. That sentence is no longer true, and this is what it was worth.

**What was measured, on the deployed layout, with the settings a fresh install ships with.** The portable host was
started with no configuration file at all, so web authorization was at its default of `true`, and the listener was
bound to `0.0.0.0`:

| asked as a stranger | before | after |
| --- | --- | --- |
| `GET health` | `200` | `200` |
| `GET api/media/titles?aspect=MovieItem` | `403` | `403` |
| `GET api/media/items/{id}/stream` | `403` | `403` |
| `GET GetResource?ResourcePath={local}:///C:/Windows/win.ini` | **`200`, 76 bytes, the file** | `403` |
| `GET GetResource?ResourcePath={local}:///…/database/MediaPortal2.db` | **`200`, the library** | `403` |

So the API asked who was calling and the same socket did not. Anybody who could reach the port - any device on the
LAN, since it listens on every address - read any file the server's process could read, including the server's own
database, its configuration and its logs. Both places that decide this carried the same two-line
`TODO: How to check safety? We don't have access to our shares store here...` above a `return true;`, and they were
right: the file server lives in `MediaPortal.Common` and the shares live behind the media library in the backend,
so the code that asked the question genuinely could not see what it needed.

**The seam.** `IResourceAccessGuard` (`MediaPortal.Common/ResourceAccess`) states the question, and whoever owns
the data registers the answer. `ResourceAccessModule.IsAllowedToAccess` now asks it, and a host that registers no
guard is refused everything, logged once as a warning: an open file server is not the safer default for a missing
piece of plumbing, and a server that came up without its backend has no media to serve anyway. A refusal answers
`403` and says nothing else, because an endpoint that explains itself is an endpoint for asking whether a file
exists.

This is deliberately *not* an authentication seam, and that is not an oversight. `GetResource` URLs are handed to
UPnP renderers for the television to fetch by itself, and a television cannot present a bearer ticket; putting a
token in the URL would put the ticket in every log between here and there. What makes it safe is not who is asking
but what is asked for, so the shipped guard answers with share membership rather than with identity.

**The rule** (`SharesResourceAccessGuard`, `MediaPortal.Backend/Services/MediaLibrary`, registered in
`BackendExtension.RegisterBackendServices()` next to the library itself): a resource may be served if it is
*inside* a folder somebody registered as a share, on the same provider, and not otherwise. The details are the
parts that make it a boundary rather than a string compare:

- The whole file path lives inside one provider segment for the local filesystem provider
  (`{local}:///D:/films/x.mkv` is one segment), so `ResourcePath.IsSameOrParentOf` - which walks segments - says
  nothing about whether a file is under a folder, and it compares with `InvariantCulture`, which would refuse a
  share whose case differs from what the importer wrote down. Case is ignored where Windows ignores it, and
  nowhere else.
- A prefix test is not a boundary: `/films/../windows/win.ini` starts with `/films/`, and the local provider turns
  a provider path into a DOS path by swapping separators and nothing else. A path containing `..` or `.` is
  refused rather than resolved - a resource path has no current directory to resolve against, and an imported
  file's path never contains either.
- `/films-x/film.mkv` starts with `/films`; the character after the share has to be a separator.
- The provider id has to match, not only the text.
- Chained paths - more than one segment, an entry in an archive, a stream a virtual provider transforms - are
  refused, because no registered folder contains them. Those titles are streamed by id, which asks the library
  instead of the filesystem; that is what section `am` built.
- One film is hundreds of range requests and every one arrives here, so the share list is read once and kept for
  thirty seconds. A guard that queried the database per request would behave correctly and cost the viewer their
  film, and a cache is invisible unless somebody counts it, so `SharesReadCount` exists and a test reads the share
  table twice hundred times and asserts it was read once. When the library cannot be reached the last list that
  was read stays in force - dropping it because the database hiccuped would be a self-inflicted outage - and if
  none ever was, nothing is served.

**The same TODO one level up.** `UPnPResourceInformationServiceImpl` had the identical `return true;`, and its
actions are worse: list the directories of, list the files of, name, does-it-exist, for any path a client names.
Being a browse API, it needs one thing the file server does not - browsing *starts* at a share, so the share's own
root has to count - which is why the interface asks two questions: `IsAllowed` for the bytes of a file, and
`IsInsideAShare` for being allowed to talk about a resource at all. They differ by exactly the share root, and the
difference is tested.

**Twelve new tests** (`Tests.Server/Api/TestResourceAccessModuleOverHttp.cs`) drive the real pipeline over a real
socket against real files on disk, with MP2's own `LocalFsResourceProvider` behind them rather than a mock - a mock
accessor would have let the traversal case pass for the wrong reason, since it is the provider's translation of a
provider path into a DOS path that makes `..\` interesting. A file outside every share, the operating system's own
directory, `..\` out of a share, a folder called `films-other` next to a share called `films`, the share root
itself, and a server with no guard registered: all `403` with an empty body. A file inside a share: `200` and the
file's own 4096 bytes. Building them also found out something about the module that no test had ever recorded: its
very first act is to ask `ISystemResolver` what kind of system this is, because a server answers `/MediaPortal/Server`
and a client `/MediaPortal/Client` - and with no resolver registered every request to it died as a `500` before
any log line was written, which is how the first run of this fixture looked.

**What this does not do.** The `200` inside a share is proved over a socket in the test, not on the deployed
layout: the deployed server comes up with an empty library and nothing registered as a share, so every path on a
freshly installed server is refused - which is the correct behaviour for a server with no media, but means the
deployment only demonstrates the refusals. It is not that HTTP cannot add a share - `POST api/media/shares` exists,
and section `ak` put a screen on it - but adding one needs a signed-in caller, and the probe that walks the deployed
layout is deliberately anonymous, so it is turned away at the door along with everything else. The gap is the
deployment's, not the API's: nothing signs the probe in, and nothing hands it a folder to share. The UPnP browse
actions now consult the guard but are not driven over their own socket; their two questions are tested against the
guard directly. Nothing was added to make `GetResource` authenticate, for the
reason above. And a server with authorization on still cannot be *streamed from* by a player that has no ticket,
carried over unchanged from section `am`.

The suite stands at **908 tests, 0 failures** - Tests.Server at **404** where the last section left it at 392, and
the front end unchanged at 317.

### ao. The server tells the screen when something changed, instead of waiting to be asked

Everything the front end knew up to here it knew because somebody pressed something. The shelf was read when a
screen opened, the title was read when a row was selected, and an import that finished while nobody was looking
finished invisibly: the film was in the library and the television did not know it. That is the shape of a polling
client, and MP2 never had one - inside the server there has always been a message broker, and the old Windows
client subscribed to it directly because it ran in the same process. What was missing was the same news crossing
the socket.

**The endpoint is `GET api/media/events`, and it is Server-Sent Events.** Of the three shapes available - long
polling, WebSocket, SSE - SSE was picked for the boring reasons: the server writes and never reads, so the
bidirectional frame of a WebSocket buys nothing while costing an upgrade handshake and a second path through the
authorization filter; long polling means the client re-asks on a timer, which is the thing being removed. SSE is a
response that does not end, so it reuses the pipeline, the guard (`[MediaPortalAuthorize]` like every other media
route), and the logging that already work. It is text over HTTP/1.1, which is right for a dozen frames a minute and
would be wrong for a frame-rate stream. A client that already speaks HTTP speaks this.

**Five frames carry it.** `hello` opens every connection with the protocol version and the server's clock, so a
client can tell "this server is too old for me" from "this server is not answering". `library` says titles changed
or vanished, with the ids when there is room for them (`items`, and `itemsTruncated` when the server had more than
`ITEMS_PER_FRAME` = 50 to say). `import` carries the importer's own states, `import-progress` carries counts while
a job runs, and `gap` says the server ran out of room and dropped something - the alternative being a silence that
looks exactly like a quiet library. Paths travel as `ResourcePath.Serialize()` strings and ids as lowercase `N`
GUID strings, both because that is what the rest of the API already uses.

**Between the broker and the socket sits one listener per connection**, subscribed to the two channels that mean
something to a viewer: `MediaLibraryMessaging.CHANNEL` and `ImporterWorkerMessaging.CHANNEL`. It does not forward
what it hears. Frames are coalesced per key over `FLUSH_TIME` (500 ms), so the hundred messages a share scan
produces become one look at the server; and when 64 different keys are already waiting (`PENDING_LIMIT`) the
listener writes a `gap` rather than growing without bound. A 15-second `:` comment keeps a healthy stream looking
alive to anything in between that gives up on quiet connections.

**The tests found two protocol bugs before anything was wired up**, both worth recording because neither was
visible from the code. The first: the `event:` line was derived by looking for `{"kind":"` at the start of the
serialized body, and `System.Text.Json` does not promise that an inherited property comes first - so every frame
went out as `event: message` and only the body was right. Frames now carry their kind as a value of their own
(`Pending.Kind`) instead of having it guessed back off the wire. The second: coalescing keyed the importer's
`status` messages by directory, so 200 status messages about 200 directories produced 200 frames and the batching
did nothing at all. The rule is now that `status` and `refresh-local-shares` key on state alone - newest wins, and
the frame names the newest path - while `scheduled`, `started`, `completed` and `canceled` keep their path, because
two shares finishing are two facts.

**On the other end is `ServerApiClient.WatchMediaEventsAsync`, and the interesting part is what it refuses to
retry.** A stream that breaks - the server restarting for a plugin update, a laptop whose address changed - is
opened again after one second, then two, four, up to thirty, forever. A server that answers `401` or `403` is not a
broken connection but a fact about this client, and reconnecting behind a locked door turns a message a person
could act on into silence; that comes back as the same `ServerApiException` every other endpoint uses, and the home
screen says the server would not let it listen. So does a frame the client cannot parse, for the same reason. The
stream gets its own `HttpClient` with no timeout: the fifteen seconds that is right for a shelf of titles would
kill a healthy stream the first evening the library had nothing to say.

**The screen now moves by itself.** `HomeViewModel` refreshes only on the frames that mean "come and look", one
pending refresh at a time and debounced, so the burst at the end of a scan is one reload and not two hundred; a
line under the menu says what the server is doing while it does it, and says nothing when it is idle. Switching
servers stops the old watch and starts a new one, signing in starts one that had been refused, and closing the
front end stops it.

**What this does not do.** Only the two channels above cross the socket; `ClientManagerMessaging` and whatever a
plugin puts on its own channel stay inside the server, and there is nothing to bridge from TV because the TV
backend went away with SlimTV. A frame is a nudge and not a diff - the client is told *that* the library changed
and asks again for what it displays, which is deliberate: a diff would mean the server remembering what every
connected screen already has. There is no `Last-Event-ID` resume, so a reconnect re-reads rather than replaying. A
client that vanishes without closing its socket is noticed at the next write, which with a fifteen-second heartbeat
means one request can outlive its client by up to that much, on both sides. And so far exactly one screen listens:
the home screen's status line, with the shelf behind it - other screens still read when they open, which is still
correct, they are simply not subscribed.

**Two things the harness had to learn**, both recorded here because they cost an afternoon each. Cancelling a
`StreamReader.ReadLineAsync` on an `HttpClient` response body does not merely abandon the read - it aborts the
connection and disposes the `NetworkStream`, so a reader that wants "wait up to N ms for the next line" must keep
one read in flight and race it against a timer, not hand the token to the read. And a fixture that shuts an
in-process server down while a stream is open waits for that stream; a server with nothing to say finds out its
client left only when it next writes, which is why `TestMediaEventStream` shortens `HEARTBEAT_TIME` to 200 ms for
its own run rather than inheriting fifteen seconds of patience.

### ap. The pictures pillar: the folders the server was told about, on the television

The home menu has carried a Pictures tile since the first screen of the new front end, and it promised "the
pictures, folder by folder". Nothing behind that promise was ever built. The tile led to the `ImageItem` shelf, and
that shelf is empty on any real installation, because a photograph has nothing for the importer to attach itself
to - no episode, no series, no id to look up. The MP2 client this tree is modelled on solved the same problem by
walking folders, so this does the same thing over HTTP.

**The server** answers `GET api/media/browse?path=<serialized resource path>` with the folders and the picture
files inside one folder: names with the extension taken off, both lists sorted, `otherFileCount` for the files that
are neither, and `foldersTruncated`/`picturesTruncated` when a list stopped at 500. A folder of four thousand
thumbnails is ordinary, and an answer that pretends to have counted them is a lie.

The question it asks the access layer is `IResourceAccessGuard.IsInsideAShare`, the weaker of the two, and
deliberately so. The file server refuses a *folder* because a folder is not a stream; browsing has to start
somewhere, and that somewhere is the share somebody registered. Outside a share the answer is an empty `403`,
identical to the file server's, because an explanation would tell a stranger which folders exist on the machine. A
server with no guard registered answers the same way - the missing piece is a decision, and guessing one is not the
safer answer. A path that does not deserialize is a `400`: `ProviderPathSegment` fails with an `ArgumentException`
and the GUID inside it fails with a `FormatException`, and both are somebody's typo about a string rather than a
reason for a stack trace.

**The client** gains `GetFolderAsync`, `GetResourceUri` and `GetResourceBytesAsync`. The last one carries the
judgement worth writing down: a `403` or a `404` from the file server is *no picture*, not *a broken server*. A
file outside every share and a file deleted since the folder was read look the same from here, and neither is worth
an error banner over a television; anything else keeps its exception, because a screen that turns a broken server
into an empty folder empties the shelf instead of reporting the break.

**The screen** opens on the shares, which is the honest root - it is the boundary the server keeps, and inventing a
root above it would mean a screen that shows folders it is not allowed to show. Tiles are folders first, then
pictures. Enter walks in or raises a picture full-screen, Left and Right move through the folder, Space runs a
slideshow that stops at the last picture and when the viewer leaves, and Back leaves the picture, then the folder,
then the screen. Nothing is claimed to be above the list of shares, so the way up disappears there rather than
pretending.

Two limits stay on the record, and both belong to the server rather than the screen:

- **There is no thumbnail.** Nothing in the API resizes, so a tile's preview is the whole photograph arriving over
  the LAN. The screen fetches pictures for the first `TILES_WITH_A_PICTURE` = 24 tiles, skips files above 8 MB and
  writes the reason on the tile, and says "no preview" on the rest. The next thing this pillar wants is a `?width=`
  on `GetResource` that resizes server-side; the screen is already shaped for it.
- **The screen remembers its own way back.** The endpoint deliberately does not name a folder's parent, because a
  parent is one more thing a stranger could ask about for any path. Walking out is therefore the screen's memory
  rather than the server's answer.

Three findings are worth more than the tests that found them:

- A test that runs on the user interface thread must not `await` the screen. The client hands its answers back
  where they were asked, so a test waiting for an answer is waiting for the thread that is waiting for it; it fires
  the command and pumps instead, with a deadline so a regression fails rather than hangs.
- `SharedHttpPortHost` boots *and shuts down* by waiting synchronously for its own startup and shutdown, and
  ASP.NET Core hands part of both to the context it was asked from. On an ordinary test thread that is invisible;
  on a thread owning a user interface dispatcher it is a deadlock in the test's own `[TearDown]`, which the runner
  reports as a crashed test host eleven tests later. Both directions go to another thread here.
- Resource paths in tests are built by `LocalFsResourceProviderBase.ToResourcePath`, not written by hand. A
  hand-written `{local}://...` looks entirely plausible, is a documentation shorthand rather than the format, and
  failed as a GUID parse inside the controller - which is how the missing `FormatException` in the `400` handling
  turned up. Following that same real path through the client then found a display bug the front end has had for
  every local folder it ever named: the local provider stores `/D:/Movies`, that slash in front of the drive is the
  provider's own prefix, and `MediaLibraryPath.TryAsLocalFile` was therefore answering "not a path this machine can
  open" about an ordinary Windows folder. `LocalFsResourceProviderBase.ToDosPath` has dropped that prefix all along;
  the client now drops it too.

15 tests in `TestPicturesScreen`: the refusals of the controller against a scripted guard, the client over a real
socket against a scripted server, the screen opening, walking in and walking back out, the bounded number of
previews, and the wording of a folder's line. Four older home-menu tests moved with the Pictures tile becoming one
of the entries that open a screen of the front end's own.

Not done here: no EXIF and no order by date (the API has no import-date sort either, which is the same gap
"Latest media" is waiting on), no rotate, no zoom, no interval a viewer can change, no import of photos, no albums
and no faces. The render gate draws this screen too now, from a scripted share of four photographs whose bytes it hands
over itself, two of them in a flat colour: the frame is then asked whether those bytes came back as pixels, and
whether they did so in two different squares, since a tile that painted twice cannot pass for two tiles each
painting the picture it was given.

### aq. The six keys of a remote, and the race a busy machine found

The entry above ends with the pictures screen drawn by the render gate. That proves pixels, and pixels are not the
whole claim: a person on a sofa holds four arrows, Enter and Back, and a ten-foot interface is judged by whether
those six keys do what the screen appears to offer. So the screen now has the key tests the other screens have, and
they earned one change to the screen and one to the view model.

**Enter** walks into the folder under the cursor and raises a photograph to full size, with the same command the
tile itself would run, so the keyboard and a touch cannot disagree about what "open" means. **The left and right
keys while a photograph is raised** belong to the photograph: it moves, the cursor behind it does not, and putting
the photograph down is not the same as leaving the pictures. **Back** was the one that was wrong: the screen closed
from anywhere, which is what nobody means when they press back in the middle of a folder. It steps out of the
folder now, and only the list of shares - the one place with nowhere up to go - lets the key leave the screen.

One fact about Avalonia is worth writing down here because it is not guessable: walking a selection along a grid is
the framework's own code, and that code follows the *container* holding the focus. A key raised on the list itself
selects the first tile and then never moves again, which looks like a broken binding and is not. The test focuses
the tile's container and sends the arrow through the headless platform's input pipeline.

**The race** is the reason this entry exists. The new back-key test passed on its own and failed every time the
whole suite ran. It was waiting for `CanGoUp`, which becomes true the moment the screen commits to walking into a
folder - before the folder has been read - and then pressed back, so on a busy machine the back key won against the
folder. The screen went up to the list of shares, the folder arrived a moment later, and the view model painted it
over the screen the viewer had just walked back to; the read for it had been started with `CancellationToken.None`,
so nothing was waiting to notice that nobody was waiting any more. Walking into a folder is now a read like any
other: it is handed the screen's cancellation, and `ReadFolderAsync` refuses to show an answer nobody is waiting for
any more. The test that pins this scripts a folder that takes a second and a half to arrive, so the timing is a fact
of the test rather than a matter of luck - and it fails when the fix is taken out, which is the only way to know a
test is measuring anything.

The same hunt turned up why this fixture closes its scripted servers and *waits* for them, where it used to close
them and move on: a port goes back in the pool of free ports when the listener closes, which is at the start of a
shutdown rather than the end, so the next test can be handed the same number and join a server that is on its way
out. Its requests are answered until the goodbye finishes and hang after that, which looks exactly like a screen
that stopped listening to its server.

Not done here: no mouse or touch test, no test of the slideshow's key or of its timer, and a read that fails with
something that is neither a cancellation nor a complaint from the server still leaves the screen showing the folder
it was already showing without saying why - the right fallback, but a silent one. Tests.Frontend went from 345 to
351 with these six.

### ar. The threads that used to be strangled

`Thread.Abort` does not exist on .NET 10. This is not a Linux problem or an Android problem: the call answers with
`PlatformNotSupportedException` on Windows too, on every .NET that is not the Windows Framework. Five calls in four
files still reached for it, and all five are on a shutdown or a "the other side stopped answering" path, which is
why years of running this code never showed it - a shutdown that throws after logging has been flushed looks
exactly like a shutdown that simply took a moment longer.

What replaces it is a bargain rather than a fix, and it is the same bargain every time: ask the thread to stop with
a flag, a token, or a handle it waits on; wait as long as the caller can honestly block; then stop waiting and walk
away. That is only survivable if the thread is a background thread, because a thread nobody is waiting for is
otherwise a process that refuses to die. Where that means something new for the caller it is written into the code:

- **The HTTP request queue** aborted its worker when a request had not finished within five seconds - a client that
  stopped reading, a handler stuck inside a plugin. The worker is created `IsBackground = true` now and is left to
  finish the request it is busy with.
- **The skin engine's render loop** was aborted for the same five seconds. The loop already watched a stop flag, so
  it was only ever the stuck driver call that needed the abort; the thread is a background thread and is left to
  return from the call it is stuck in, and the window it drew into is gone either way.
- **The clipboard** is the interesting one, because the thing that hangs is another program. `GetClipboardText` and
  `SetClipboardText` waited 500 ms, then aborted the apartment thread they had started. They still wait 500 ms, and
  then they say *no*. Saying no matters: the caller that cuts only deletes its text when the copy reports success,
  and reporting success for a clipboard that never answered threw the text away without ever putting it anywhere.
- **`ThreadingUtils.CallWithTimeout`** is gone rather than rewritten. It promised to return "whether the thread
  stopped", which was never true - the abort it used was asynchronous, and a thread could refuse it - and it had no
  callers left. What remains of the class is the one helper that starts an STA thread, and that thread is now a
  background thread, because that is what makes walking away from it possible.
- **The dead catches** went with it: `catch (ThreadAbortException)` in the thread pool, in the request queue, and in
  the render loop, and the two `e.GetType() == typeof(ThreadAbortException)` rethrows in the scriptable metadata
  extractor. None of them could ever be taken again, and a catch that cannot be taken teaches the next reader that
  aborting is still on the menu.

The one place that only *documented* an abort - `WindowsTaskWaitableTimer`, whose comment promised
`Thread.Abort(object)` while the code underneath had long since switched to a cancellation token - keeps its code
and loses the lie.

Two tests keep this from rotting back, both in the portability canary. `ThreadingUtils.cs` is now linked into that
project, so it compiles against a portable target framework with `CA1416` turned into an error - which is a
stronger statement than "the Windows build still works", because it means the file has no Windows-only call site
outside the guard it states. Three behaviour tests pin what the helper owes its callers: the action runs on another
thread, and that thread is a background thread both outside and inside the action, and the single-threaded
apartment is set where apartments exist and not claimed where they do not.

`NoThreadAbortTests` then walks every `.cs` file under `Source` - about 3900 files, eight tenths of a second - for
`ThreadAbortException`, `ResetAbort`, `Thread.Suspend` and a bare `.Abort()`, skipping comment lines so that the
prose explaining why a call is gone does not read as a call. One file is on the allow list, with its reason next to
it: `NetworkHelper.cs` calls `HttpWebRequest.Abort()`, which cancels a web request, is not a thread, and works
everywhere. Removing that entry makes the test fail with the file, the line and the guidance printed, which is how
the test was checked to bite at all. The canary went from 29 to 33 tests.

Not done here: `HttpServer` is still a .NET Framework 4.8 project, so the request-queue edit is the one in this set
that no build has compiled - it is small, and the source scan above at least proves no abort survives in it, but it
joins the queue of projects waiting to be retargeted. An abandoned render thread can still be holding a DirectX
device when the process gives up on it, and an abandoned request queue keeps one client connection open until the
request finishes on its own; both are the price of the bargain rather than a new defect, and both are paid only in
the case where the alternative was a crash. The clipboard's abandoned thread may still write into the clipboard
after the caller has given up on it, which is the honest version of a thing that was already true.

### as. A tile asks for a picture the size of the tile

The pictures screen cannot ask the server for a small picture - there is no such request, and `IImageTransformer`,
the seam that would let a server resize one, is filled in by the Windows host and by nobody else yet. So a tile's
preview has always been the whole photograph fetched over the network, and that was written down as the accepted
cost. What was not written down was what happens next, in the converter that turns those bytes into something a
screen can draw.

The converter remembers decoded pictures, oldest thrown away first, with a budget of eight million pixels. A
twelve-megapixel photograph from a camera does not fit in that budget *by itself*, so a folder of twenty-four of
them - one screenful, the number of tiles the screen is willing to fetch pictures for - evicted each other as fast
as they were decoded. Whoever scrolled through a folder of photographs was watching the same photograph be decoded
again and again, and every pass of the scrollbar paid for it. The screen looked right and felt wrong, which is the
kind of defect nobody can point at in a screenshot.

**The fix is a size in the request.** A template that knows its picture is going into a tile now says so -
`ConverterParameter=tile` - and the converter decodes at four hundred and thirty-two device pixels, which is twice
the two hundred and six the tile has for the picture: sharp on a television driven at 200% scaling, and nothing
beyond that. A template whose tiles are another size may pass a number of its own. A template that says nothing -
the poster on a details screen, the shelf's covers - still gets the whole picture, because guessing a size for a
caller that did not ask is how a picture ends up blurred for no reason.

Two consequences had to be handled in the same place:

- **The same bytes at two sizes are two remembered pictures.** The raised photograph and the tile it came from are
  on screen at the same time and are made of the very same array of bytes, which is what the cache keys on. Keying
  on the bytes alone would have handed the raised photograph a 432-pixel picture - or, depending on who asked
  first, a twelve-megapixel picture in a two-hundred-pixel tile.
- **A narrower picture stays as it is.** Scaling here is only ever downwards, so a 256-pixel thumbnail from the
  server's database is decoded exactly as it was before, whether or not the template asked for something wider.

A fact about Avalonia 12 belongs here, because the obvious API is gone: the constructor that used to take a size to
decode to does not exist any more. `Bitmap.DecodeToWidth` and `DecodeToHeight` are what is left, and the pictures
screen's tiles are decoded with the first of those at `MediumQuality`.

**What proves it, and where that proof cannot reach.** The unit-test fixture records, from measurement, that
Avalonia's pure-managed headless rasterizer answers every array of bytes with a one-pixel picture - so in that
fixture a shrink cannot be seen any more than a decode can. The tests therefore prove the decisions: that the word
`tile` means a size, however it is capitalised, that a number is honoured, that anything else means the whole
picture, that the size is at least twice the tile's width and at most four times it (the first is a soft picture on
a 4K panel, the second is the tiles no longer fitting in the budget, which is the two ways this number can be
wrong), and that two sizes of the same bytes are two pictures.

The shrink itself is proved where a real decoder runs: the render gate, which draws with Skia like every platform
does. Its pictures frame now decodes a picture it knows the size of, asks for it twice, and prints what came back -
`decode: a 300x150 picture decodes at 300x150 asked for as a whole and at 100x50 asked for at 100 across` - and
fails with a named complaint when the asked-for picture is not smaller or is out of proportion. That complaint was
heard: with the width removed from the call the frame answers `a picture asked for at 100 across was decoded 300
across`, which is the sentence a future regression needs.

Not done here: the bytes still cross the network whole, which is a server-side resize and therefore a decoder
decision the server host has not made yet; the ceiling that refuses a preview for a file above eight megabytes is
now about bandwidth rather than about memory, and says so; and the raised photograph is decoded in full while its
tile is still on screen behind it, which is correct for the one picture a viewer is looking at. Tests.Frontend went
from 351 to 358 with these seven.

### at. The two rescued skins, as colours rather than as code

"Nereus and WMC can be rescued by fixing them from MPF to WPF" was the instruction, and the first thing worth
reporting is that this was not a translation job waiting to be done. Both skin projects compile as they stand
(`net10.0-windows`, `UseWPF`, zero errors), so nothing in them is MPF any more in the sense of a framework that
needs upgrading. What is untranslated is the *markup*: 785 and 906 files of MP2's own XAML dialect, with
`<Include>`, `<Model>`, `{ThemeResource ...}` and a namespace pointing at the skin engine's DirectX visuals - 927
distinct element names, of which Avalonia understands none. Porting that means rewriting every one of 123 screens
and templates as Avalonia, screen by screen, in a dialect where the layout concepts do not line up. What a viewer
actually missed when the old skins went away, though, was not the bevels. It was the colours.

**So the skins were rescued as palettes.** `Theming/FrontendTheme.cs` holds fourteen keys named after the job on the
screen - `ScreenBackgroundBrush`, `PanelRaisedBrush`, `MutedTextBrush`, `AccentBrush`, `WarningBrush`, `ScrimBrush`,
`VideoBrush` - rather than after the colour, because `DarkBlue1` tells the next reader nothing about why the video
surface is the darkest thing on the television. Every one of the twenty-eight hex values that used to be written into
the seven screens is now a `{DynamicResource}`, and five palettes answer those keys: `default` (what this front end
has always looked like, so nobody's television changes because somebody else wanted Nereus), `nereus`, `wmc`,
`wmc-dark` and `wmc-turquoise`. Each value carries the skin key it was read from next to it
(`// NereusColorDark_Torquoise`, `// MediaItemBackgroundColor1`), which is what made the next step possible.

**"Read from the skin" became a checked claim.** Every non-default palette colour has to appear in that theme's own
`Colors.xaml` - or in the default theme a skin variant inherits from - and one test walks all four skin files and
says which values no skin contains. It found two truths on its own: WMC has no colour for a complaint at all, so the
amber the folders screen shows when a share refuses to be removed is the front end's own in every WMC palette; and
the two WMC variants do not override the dialog border colour, which is why their outline is the default theme's.
The only values exempted are the three warning colours, each with its reason in the test. A palette value that
drifts away from its skin now fails a run instead of depending on somebody remembering where it came from.

**What a theme has to be chosen by** is the same two ways a server address is: `--theme` on the command line, then
`MP2_THEME` for a machine nobody types on. An unknown name is not quietly the default - the front end prints
"'graveyard' is not a theme, so the front end draws with 'default'. Themes: default, nereus, ..." and carries on,
because refusing to start over a typo is worse on a television than being told.

**Ten tests, two of which read the screens.** A screen that asks for `{DynamicResource OutLineBrush}` - one letter
out - compiles, loads, and draws that part of itself with nothing on it; nothing at runtime complains. So one test
scans every `.axaml` in the front end and fails on any resource name that is not a key, another fails on a key no
screen asks for, and a third applies one palette and then another and asserts that *every* key resolves to the second
one, because a palette that was stacked under the first instead of replacing it reads on screen as a theme that only
half applied itself. The scan bites when it is meant to: with a deliberate typo in a screen it answers
`PicturesWindow.axaml asks for OutLineBrush`.

**The render gate now measures colour.** `Frontend.RenderProbe --theme <name>` draws a frame in a palette, and every
frame - themed or not - is now checked in the pixels: the corner, where nothing but the window's own background can
be, has to be the palette's background colour, and that colour has to be at least 2% of the frame rather than a
patch somewhere. Two frames were added, `nowplaying-nereus` and `nowplaying-wmc-dark`, and they are the honest
choice rather than the flattering one: the playback screen carries eleven of the fourteen keys, while the home
screen draws exactly one colour of its own and leaves the rest to the theme, so a pair of themed home frames would
have proved only that a background changed. Measuring the written PNGs says more than the check does - the panel
behind the transport buttons is 4732 sampled pixels of `#1B2A38` in the default palette and the same 4732 pixels of
`#175F5D`, Nereus's medium turquoise, in Nereus's - and asking for a theme that does not exist exits 2 instead of
handing back a default frame a gate could mistake for a pass.

**What this is not.** The Fluent chrome is still Fluent's: buttons, list rows and text boxes that no screen paints
itself keep the theme's colours, so a themed screen is its panels, its text and its accents rather than every pixel
on it - mapping the Fluent resource names onto the palettes is the obvious next step and was deliberately not
smuggled in here. Nereus is fixed to the turquoise accent family (the first its colour file lists) although the skin
offers nine; WMC's gradients, bevels and star ratings were left in the skin because no screen of this front end has
a use for them; nothing is picked from a settings screen yet, because there is no server settings API to store the
choice in; and the default palette holds one colour fewer than the screens it replaced, since the details panel's
`#161F29` and the tiles' `#FF17202A` are two shades nobody could point at. The skin *files* are still unread by the
front end, and `Nereus/screens/CurrentlyPlayingImage.xaml` is still not well-formed - a tail of another screen sits
after its `</Include>` - which matters only as long as somebody wants those screens rather than their colours.
Tests.Frontend went from 358 to 368 with these ten and the render gate from nine frames to eleven; 973 tests, all
green.


### au. The home screen becomes MP2's home, menu contributions and all

The two rescued skins answered "what would it look like"; what was left on the other side of that question was
"what would it *do*", and the honest answer so far had been three prototype buttons on a blank screen. A viewer
sitting down with the Nereus colours said exactly that - "currently I see only 3 test buttons in home" - so the home
screen was rebuilt as MP2's home is built, and it turned out MP2 had written its home down more completely than this
front end had.

**The menu is the plugins' own, not an invention.** MP2 never had a main menu coded anywhere: SkinBase's `MenuModel`
collected every workflow action whose source state was the home state
(`7F702D9C-F2DD-42da-9ED8-0BA92F07787F`), sorted them by `DisplayCategory` and then `SortOrder`, and handed the
result to the skin - and every one of those contributions is still in this tree. The menu is now those fifteen
contributions, in the order their XML sorts into, wearing the contributing plugins' own English words read from each
plugin's `Language/strings_en.xml` by the key the contribution names: *Local media, Browse media, Audio, Videos,
Movies, Series, Images, Latest media, News, TV, Radio, Weather, Party music player, Settings, Utilities*. That is why
the music entry is called "Audio" and the pictures entry "Images" - those are the media plugin's words, and a viewer
who has used MediaPortal before should not have to relearn which tile is which. Six more home contributions live in
`wmc-actions.xml`; they are the television plugin's insides that the WMC skin promoted onto its home screen, and they
need the TV service this tree no longer has, so they stay off the menu - a decision written down rather than drifted
into. An entry with nothing behind it stays visible and says so in its small print ("no TV backend", "no settings
API yet", "its screen is still old skin"), because a media centre that silently offers only what the last read
happened to fill cannot tell "no television here" apart from "the read went wrong".

**A test reads the plugins, so the menu cannot go stale.** `TestHomeMenuContributions.cs` repeats that whole
reading on every test run - all workflow XML outside the skins, all `strings_en.xml` - and fails if the built menu
misses a contribution, gains an invented one, lands in an order the plugins' `DisplayCategory`/`SortOrder` do not
sort into, or wears a label no plugin actually owns. One exception is allowed and proven rather than trusted:
`Home->Movies` and `Home->Series` genuinely tie on sort order `e` in the media plugin's file (MP2's own order between
them was whatever its unstable list sort produced), so the test verifies the tie exists in the XML before permitting
this front end's swap, and films lead because more people press them. Rename a plugin's string or contribute a new
home entry anywhere in the tree, and the menu's drift shows up as a failing test rather than as a screen that quietly
stopped matching the code beside it.

**Beside the menu, the lists MP2 showed under it.** Nereus's `HomeMenuModel` handed the highlighted entry to a
`HomeContent` model offering the same four lists - latest added, continue playing, favourites, unplayed - of which
the skin drew at most six titles each (`AbstractHomeContent.CONTENT_LIST_LIMIT` is six, and that is still the cap).
The content pane draws those lists under Nereus' own headings for whichever entry is highlighted, plus a stand-in
"Newest by year" first, because sorting by year answers the neighbouring question ("what is newest") while saying in
its own heading that it is not the importer-date question. Two of the five cannot be filled over HTTP - the API
sorts by title, year, last-played, track and episode, so the day the importer first saw a file has no sort to ask
for, and nothing on the server keeps favourites - and each stays on the pane as one line saying why, because four
headings of which two quietly vanished would leave the viewer wondering whether the library is empty or the front end
is broken. An entry that opens a screen rather than a shelf ("Local media", "Images") gets a line saying what
pressing it brings up instead, and the half-watched strip that came back in `ad.` still sits under the pane.

**The pane's gold test caught the WIP arranging its own answers wrongly.** The first version wrote all five
headings first and then appended each list's titles as its four HTTP reads landed, so the titles ended up in a heap
at the bottom of the pane, under everyone else's heading - and a refusal note could refuse a list it now sat two
screens below. `ShowContentAsync` now arms the headings up front, remembers where each list's rows belong, and
*inserts* each arriving answer - titles, "nothing here", or the refusal - under its own heading, with a generation
counter so a walk past Movies to Images leaves the pane showing only what Images answered. The insertion order was
pinned by tests that first failed on exactly that heap, which is the whole reason to write gold tests against a mock
server rather than against pixels.

**The d-pad lesson from the connection window, applied again.** A focused Avalonia `ListBox` treats Left and Right
as "previous/next item" and consumes them, so the window-level ring handler never saw the keys and the viewer could
not walk out of a list sideways - the same trap `ConnectionWindow` had already stepped out of with a *tunnel*
handler that claims the keys before the list gets them. The home window grows the same handler over its four lists,
and the ring is menu → content pane → leftovers strip → aspects → buttons, walked in one test.

**What changed for the tests that were already there.** Six of them had pinned the prototype's invented menu -
"Music", "TV Series", "Pictures", "Folders" - and were retargeted at the contributions table; "Local media" said
38 characters on a tile that fits 30, so its second line is now "the server's folders". Tests.Frontend went from 368
to 375 - three contribution-reading tests, three content-pane tests, one ring test - and the render gate's
`home-windows` frame now draws the new home against a live server: menu column, content pane filling itself in, the
two unfillable lists refusing in place. The front-end solution builds clean, the whole gate passes, and every
Windows suite is green: 980 tests in total.

**What this is not.** Nine of the fifteen entries are still a line rather than a screen - the television backend is
gone, the settings API is not built, and News, Weather, Party music and Utilities are still written against the old
skin engine; their places are on the menu so that the menu is MP2's, and their small print is the truth about this
build. The pane is rows of text walked by the remote, not Nereus' poster art - the pictures seam already exists for
when the rows learn to carry them - and highlighting an entry starts nothing; it only shows what would start.


### av. The server learned the date a title entered the library, and one tile stopped apologising

The home menu carried nine small print lines, and the first one to be looked at rather than believed was `no
import-date sort` behind "Latest media". The claim was three layers wrong in the good direction: the media database
does store an import date - `ImporterAspect` carries a `DateAdded` column, and `MediaLibrary.AddOrUpdateMediaItem`
stamps it itself the first time it stores a title and keeps it through every reimport - the query engine already
sorts by any aspect's inline attribute and joins the table for it with a left outer join whether or not the query
asked for the aspect, and the only thing that had never existed was the *word*: `MediaController.ReadSort` knew
title, year, lastplayed, episode, track and none, and everything else fell back to the name order. So `sort=added`
became one case in `ReadSort` and one line in `BuildSorts`, and the endpoint's own docstring, which had been lying
about its sorts for two phases, finally lists them. What the search of the storage layer also settled was the shape
of the wrong answer that was nearly built: `MEDIA_ITEMS` holds only ids, aspect values live in tables generated from
each aspect type's serialized metadata, and `AddMediaItemAspectStorage` *silently ignores* a changed spec for an
aspect the database already knows - so "add a date to the media aspect" would have been a new, empty column nobody
ever wrote. Finding the stamped date first is what kept a schema question out of a one-line feature.

**The pane is Nereus' four headings now.** The content pane had carried five headings because one of the four could
not be filled: "Newest by year" had been invented as the stand-in for "Latest added", and said in its own heading
that it was a stand-in. With the real question answerable, the placeholder retired - the pane draws Nereus' own four
lists in his order, three of them filled from the server and only "Favorites" still refusing, because nothing on
the server keeps favourites and no lie about a date covers that gap. The pane's gold test was rewritten around the
new shape and gained a sharper edge: the shelf mock now records the queries the server passes through, and the test
asserts that *exactly one* of the pane's questions was about `ATTR_DATEADDED` and asked it *descending* - newest
arrivals first, put in that order by the database, because sorting whichever page happened to arrive would sort a
page rather than a library.

**"Latest media" opens its shelf.** The tile no longer apologises: it claims the `VideoItem` shelf - MP2's own
latest-media screen listed everything watchable, films, episodes and the clips nobody filed, which on this server
is exactly that aspect - and carries the order with the claim. The direction travels with the word, from the menu
table through the entry and the section down to the browser's request (`sort=added&order=desc`), because "the
newest titles first" read oldest-first is the opposite list, and a promise about a shelf of three hundred films has
to be kept by the server that holds the shelf, not by whoever displays the first page. A browser test pins the
request; a menu test pins that the tile is openable and why it used not to be; the browser's read call, which had
hardcoded ascending for every shelf since it existed, now asks for the direction its section carries.

**What this is and is not.** This is the first of the nine small-print lines coming off, and the ROADMAP keeps the
earlier sections as they were written - `au.` still describes the five-heading pane, because that is what it
shipped with, and this section is the correction in sequence. The other eight lines were re-examined too and stay:
the television service really is gone from this tree, the configuration really is still behind .NET Remoting, and
the News, Weather, Party music and Utilities plugins are still old-skin screens - though the reconnaissance found
their data collectors (an RSS reader, an OpenWeatherMap grabber, an M3U importer) living outside the skin code,
which is a phase each. Tests.Server went from 414 to 415 and Tests.Frontend from 375 to 377 with these five; 983
tests, all green, and the render gate's eleven frames including the reworked home pane all exit 0.


### aw. The server learned to count its own shelves, so the browse screen will have something to show

The "Browse media" tile apologised with `no grouping over HTTP yet`, and the apology had the usual defect of an
honest-sounding one: the server has been able to count a shelf since before this port existed.
`IMediaLibrary.GetValueGroups` hands a value-to-count map to anyone who asks, and the query engine compiles it into
one `SELECT value, COUNT(*) ... GROUP BY value` inside the database - it is the machinery MP2's own old browse tree
was built from. What was missing was again only the door: the method lived behind .NET Remoting, and the HTTP API
had no word for it. `GET api/media/groups?aspect=MovieItem&by=genre` is that word now, answering the five
categories MP2's browse tree actually had - genre, year, actors, directors, writers - chosen not by taste but by
where the data lives: those are the attributes the importer writes on the title itself (genre rows on the
`GenreAspect`, credit names as string collections on the `VideoAspect`), so counting them costs one grouped query
rather than the relationship walk the separate person and genre *items* would need. That walk stays out of
browsing, and says so in this paragraph rather than in a screenshot.

Two details are the whole content of the endpoint. *A page is not a census*: the counting is done by the database
because it is the only party that has ever seen the whole library, and the answer comes back biggest category first
and then by name, so the genres a library lives in sit where a remote reaches them first. And *"no genre written
here" is not a genre*: the database counts titles that carry nothing under the attribute as a group of their own,
and the endpoint drops that row, because a blank entry among the genres is a category nobody came to the screen to
browse. The year has no column anywhere - it is taken from the recording time by the database itself, through the
`DateToYear` projection the library has always had, which is also why the answer to "which films are from 1998"
could not be an equality: films are recorded at hours, and matching a made-up midnight of the first of January
would lose all of them, so `by=year&value=1998` on the titles endpoint asks instead for the twelve months from
that January up to the next one. An unknown category is refused with the list of the five, on both endpoints -
silently ignoring the filter would hand the viewer the whole shelf under a heading that names one genre, which is
the wrong answer that looks right, and this API's one rule is that a wrong answer says so.

**What this is not yet.** The tile still apologises: the browse screen that will stand behind it is the next
increment, and the gate's live-server step will get to count a real database when the front end asks over a real
socket. Tests.Server went from 414 to 423 across the last two phases with these eight - one test pins that the
count is one question to the database over both the shelf and the aspect holding the values, one that the total of
a filtered list is counted through the very same filter so a screen can say "12 of 12 Action films" rather than
"12 of the whole shelf", and none of them trusts the year to be anything but a span. 991 tests, all green.


### ax. The browse screen: three columns, the server's numbers, and a tile that stopped apologising

The groups endpoint had grown, and the tile behind it still said `no grouping over HTTP yet` - which had become
a lie the honest way, by the endpoint being built and the screen forgetting to follow. It follows now. "Browse
media" opens three columns read left to right: the shelves the home screen has tiles for, the five groupings the
server answers for, and the values that grouping takes in that shelf with the database's own count on each row -
"Drama · 4 titles", biggest first, because the order is part of the answer and a screen that re-sorted it
alphabetically would hide which genre this library actually lives in. Pressing OK on a value opens the ordinary
titles screen wearing that filter: the same list, the same keys, the same play, with the count saying "12 of 12
movies under Drama" so the filtered shelf is never mistaken for the whole one. Browsing is a way *into* the
library, not a second library with its own rules - which is also why the picked pair travels as two fields on the
same `HomeSection` a tile already carried, and why pressing into an album from a browsed shelf does not carry the
genre down with it.

The two halves of a filter travel together or not at all, in the client as in the server: a category whose value
got lost would read the whole shelf under a heading naming one genre, and neither end is allowed to make that
mistake on the other's behalf. The empty group the database answers with for "nobody wrote a genre" is dropped at
the endpoint and filtered again in the DTO - a blank row among the genres is not a category, it is an apology,
and this screen has now seen the last of those. An empty or refused answer is said instead: "None of the Movies
carries a genre the importer wrote" names which of the two nothings the viewer is standing in front of, and the
server's own refusal words reach the status line untouched.

**Proof.** Nine new tests in Tests.Frontend took the suite from 991 to 1000, all green: that both halves travel
or neither does, that the counts survive JSON in the server's order with the hole taken out, that a picked value
hands over the shelf wearing the filter rather than opening a window directly, that the titles query really
carries `by=genre&value=Drama`, and that the tile now names a screen instead of a reason - which moved four
long-standing menu tests, the ones that counted "the two screens the front end owns", to three: folders,
pictures, and a browse screen that reads whatever the server turns out to hold. The render probe gained a
`browse` frame drawn from a scripted count, and its picture shows the third column full, biggest first, the empty
value gone - the screen's two promises about somebody else's numbers, visible in pixels rather than asserted.
The tile's own second line was cut to "grouped by genre, year, actor" to fit the 30-character tile, as the gate's
tile-width test insists.

**What this is not yet.** Person browsing in MP2's old sense - an actor as a node that leads to every film they
appear in *across* shelves - still lives in the relationship tables and stays out, same as the endpoint said;
what works is picking a person under "by actor" and getting that shelf's titles that name them. And the gate's
live-server step counts nothing yet: its database is empty by design, so the real-database GROUP BY is pinned by
the server's own tests over a mocked library and the pixels by the scripted frame - the day the gate gets a
seeded database, the browse screen is the one that will thank it.

### ay. News: the screen that talks to the internet instead of the server

Eleven of the menu's entries ask the MP2 server something. News never did, not even in the old MP2: the news
plugin ran on the television, downloaded MediaPortal's own feed list from `install.team-mediaportal.com`, picked
the region for the interface language, and fetched the RSS itself. The server never had a news endpoint, so this
screen is the one place in the front end that opens a socket to somebody else's machine - and that is on the
record in its header comment rather than hidden, because a rule as clean as "the front end talks to the server
and nothing else" should say out loud where it bends and why.

The screen is the plugin's shape: the feeds down the left, their headlines in the middle, the selected story on
the right. The feed list is fetched in the old plugin's culture ladder - the exact culture (`de-AT`), then the
two-letter language (`de`), then English - and the ladder is a static function so that a test can be somebody
from Vienna without anybody in the build being Austrian. Every feed is read the moment the window opens, because
a screen called "News" that waits for a button is a screen that says the news is over; the old plugin's
fifteen-minute background timer did not come along, since a screen open for ninety seconds of a television's
evening does not need a clock of its own.

**A refused feed is said, not swallowed.** Each feed answers on its own - `NewsFeedAnswer` is headlines *or* the
reason - and the merged list is exactly what answered, newest first, with the undated items last rather than
first: a feed that gives no date is not claiming to be the newest story on the internet. One paper answering
with a 500 costs the viewer nothing of what the other two delivered, and buys itself a row of its own saying
`500 Internal Server Error` and a name in the status line, `3 headlines from 2 of 3 feeds - refused: Dead Paper
(500 Internal Server Error)`. A single try/catch around the whole refresh would have made one dead newspaper a
blank screen, which is the failure mode this screen's whole design exists to avoid. Summaries arrive as HTML -
feeds bury their words in paragraphs and links - so tags are stripped, entities decoded and whitespace collapsed
before anything reaches a row, and when a feed carries both a summary and the full content, the content wins, as
it did in the old reader.

**Proof.** Eight new tests took the suite from 1000 to 1008, all green, and nothing in them touches the
internet: the culture ladder climbs all three rungs of a feed list written inside the test, RSS 2.0 and Atom both
become headlines with the right dates and stripped words, the refusing feed is named while the others still
speak, the merged order is newest-first with the undated item last and "no date" printed rather than a
placeholder, selecting a paper narrows the column without fetching again, and the tile stopped apologising -
`News` now routes to a screen with no reason attached, which is true in a way no other tile is: it opens with a
server, without one, and without any media at all. That made the fourth front-end-owned screen, and the menu
tests that counted three became four; the render probe's continue frame counts six openable entries where it
once counted five. The probe gained a `news` frame drawn from a *scripted internet* - the real XML shape, one RSS
feed, one Atom feed, and one address that answers 500 - so the frame pictures the screen's best day and its
ugliest honest one at once, and its picture shows the feed row carrying `500 Internal Server Error` beside two
rows carrying counts.

**What this is not yet.** Feed editing: the old plugin had a settings dialog for adding feeds by hand, and this
screen reads MediaPortal's list only - an honest v1, since the alternative is a settings screen for a
configuration API the server does not have. No icons, no cache, no background refresh; a story panel that shows
the feed's own words, not the whole article behind the link, because fetching every linked page to read a
screenful is not a thing a television should do.

### az. Weather: the same plugin's rules, the endpoints the plugin no longer has

Weather is the second screen that talks to somebody else's machine, and the second that has no server endpoint to
ask - the old plugin went straight to OpenWeatherMap with three built-in public API keys, chose Celsius or
Fahrenheit by asking `RegionInfo` whether the viewer's country writes metric, asked for descriptions in the
interface language, and spoke wind direction as compass points. All of that survives. What does not survive is
the plugin's address book: its locations were stored as OpenWeatherMap *city ids*, typed in by hand from a
website, and the endpoint that used them (`data/2.5/weather?q=...&mode=json`) no longer answers a city name with
a list - live-probing the API showed one best-guess object where the plugin expected an array. The plugin's world
quietly changed underneath it.

**Places are coordinates now, found by search.** The screen gained what the plugin never had: a search box. The
geocoder (`geo/1.0/direct`) is the endpoint that still answers names, and it answers with latitude, longitude,
state and country - no city id at all. So a watched place is a coordinate pair plus the label the geocoder
printed ("Berlin, Berlin, DE" against "Berlin, New Hampshire, US" is why the second line exists), kept in a
little `weather.json` beside `frontend.json`, together with an optional API key of the viewer's own. The current
read and the forecast take coordinates, which is both what the free endpoints want and a truer record of the
place than a number one vendor invented.

**Days are cut from the three-hourly answer, in the city's own calendar.** The plugin's sixteen-day forecast
endpoint is gone from the free tier; what remains is `data/2.5/forecast`, forty slots of three hours, each with
temperature extremes, a description, a probability of precipitation - and, usefully, the city's UTC offset. The
aggregation the old endpoint did on the server is now done here: a slot belongs to the day it lands on *where
the city is*, so the test fixture is Ürümqi at UTC+6 and its first forecast day starts at 00:30 local while UTC
still calls it the day before. Each day takes its high as the maximum of its slots' maxima and its low as the
minimum of their minima, its words from the slot that hits local noon (the middle of the day is what a daily
forecast used to describe), and its rain chance as the largest probability inside it.

**Refusals name themselves, and the keys step aside.** The three built-in keys are the plugin's own, still
public, and used the plugin's way: a `401` or `429` means *this key* is spent, so the next key is tried before
anybody is told the weather did not arrive - the test server counts the requests and asserts the step happened.
Every other refusal travels up and is written into the status line, `"Berlin did not answer: 503 ... "`, while
the other watched places keep their last weather on screen. A search that finds nothing says `"Nothing on earth
answers to '...'"`; a read in flight makes the next selection cancel it, so a fast walker through the places
column never ends up reading the wrong city. And the culture rules learned one hard lesson on the way:
`RegionInfo` *throws* on a neutral culture like `en` - it is a language, not a region - so the client asks the
culture's own `TextInfo` first and falls back to imperial rather than crashing on the viewer from nowhere.

**What this is not yet.** No weather icons - the plugin drew a picture per condition, and this skin has no
picture set for weather yet, so the forecast is in words. No background refresh, same bargain as News. Maps,
sunrise, and hourly granularity did not come along; neither did the plugin's settings dialog, because the search
box replaced the reason it existed.

**Proof.** Seven new tests took the suite from 1008 to 1015, and none of them reaches the internet. A scripted
OpenWeatherMap answers the geocoder, the current read, and the forecast in the real JSON shapes: a search finds
two Berlins and keeping one writes the settings file, and a *new view model over the same file* - which is what
a restart is - opens on it and reads it without another search; the first of two requests is refused and the
test asserts both the retry *and* that it was with the next key; the Ürümqi fixture proves the local-day cut; a
Vienna culture reads `8°C` and `NE 3,1 m/s` while a US culture reads the identical bytes as `8°F` and
`3.1 mph`; a city that refuses is named while the other place keeps talking; and the compass wraps - the eighth
direction after north is north. `Weather` is the fifth front-end-owned tile. The probe gained a `weather` frame
drawn from a scripted internet - a city at UTC+2 with its slots around local noon - whose content check reads
`8°C, few clouds` and `Saturday 3 January broken clouds 6°C / 1°C rain 20%`, so the picture and the assertions
are the same words.


### ba. Party music player: the screen chooses, the application plays, and both keep score

The old plugin was a whole evening's machinery: it configured a server-side player, shuffled the music
library into it, and let the party run unattended. The multiplatform server has no player to configure and the
front end owns playback now, so the party came back as the one screen that is only *half* in charge:
`PartyViewModel` reads the server's `AudioItem` shelf a page at a time, shuffles everything playable into a
queue, and asks for songs; the application - `App.OnPartySongChosen` - plays them through the same
`NowPlayingViewModel` the library uses, so the song, its position in the library and its resume bookkeeping
are the same machinery as anywhere else. The screen never touches a player.

That split needed an ear, and the ear turned out to be the interesting part. `PlaybackReported` fires when a
song *ends* (100%), when the viewer replaces it (<100%), and when playback stops - only the 100% report means
"this one is done", because the others are statements about what the viewer did, not about what the queue
owed. So the application forwards 100% endings to `TrackFinished` and failures (`StateText == "Failed"`) to
`TrackRefused`, and both ears ignore any report whose id is not the song the queue is waiting on - a second
"finished" about a song the queue already let go would skip one for an ending that already happened, and the
test for exactly that reads like paranoia until you have watched a replaced track report twice.

The rules the plugin actually earned its keep with came over intact. A song that will not open leaves the
queue with the player's own reason in the status line and the party walks on - but three refusals in a row
stop it, because one broken file is an evening saved and three are a fact about the shelf. When the shuffle
runs dry the shelf starts again, reshuffled, with the song that just ended moved to the back so two rounds in
a row never open on the same song. Stop means "no more songs after this one", not "silence now": the queue
stays, the playing song is left alone, and - after a bug the stop test caught on paper before it could ship -
a song ending after a stop leaves the queue but asks for nothing. Pressing a row puts that song on now and
keeps the rest of the order; the one Party view model lives in the application, so closing the window closes
a screen and does not end an evening.

**What this is not yet.** No crossfades, no genre filter, no dance-meter; the plugin's party *settings* were
settings of a server-side player that does not exist. No artwork in the queue. On a machine without libVLC the
screen still queues and counts, it just cannot be heard - the same bargain every screen here makes with an
absent player.

**Proof.** Twelve new tests took the suite from 1015 to 1027 against a scripted music shelf, with the test
standing in for the application on both sides of the split: start queues the shelf and asks for its head; the
same seed twice is the same queue; endings, skips and jumps each ask for exactly one next song and name both
halves of it in the status line; a stale ending asks for nothing; two rounds never open on the same song;
refusals walk on and three in a row stop; the shelf that 404s is answered with its own sentence; unplayable
titles stay out of the queue and are counted out loud; and a stopped party asks for nothing after the song it
was already playing. The menu's openable count moved to six front-end-owned screens, the continue frame to
eight, and the probe gained a `party` frame - a scripted shelf, a fixed shuffle seed, the probe pressing
start - whose content line reads `8 song(s) queued, first Low Tide Again` so the picture and the assertion
are the same words. Fifteen frames now.


### bb. Four tiles that stayed words: what TV, Radio, Settings and Utilities owe before they can open

Four entries of the fifteen remain non-openable, and the honesty rule says a row's reason text is a promise,
so the reasons are recorded here as decisions rather than as gaps. **TV** and **Radio**: the multiplatform
server's TV service layer was removed (`029a262aa4`) along with the backend plugins it talked to - there is no
endpoint to point a TV guide at, and pretending otherwise with an empty screen would be the tile lying.
**Settings**: the old MP2 settings screens were editors over a plugin configuration API the new server does
not have; every setting this front end actually needs (server address, weather places) already has its own
screen. **Utilities**: its plugin.xml turned out to be only a launcher for TV, radio and playlist-import
tools - with the TV backend gone the tile's whole content went with it, which is why its row says *its tools
went with the TV*.

None of this is a verdict against the features; each row's `ScreenKey` slot is in the schema and stays null
only while there is nothing behind it. If a TV service comes back to the server, the tile is one row and one
screen away from opening, and the menu tests will demand the count changes when it does.

**Proof.** `MainMenu`'s docstring names each missing entry's reason, the four menu tests count six openable
rows against the catalogued fifteen, and no test had to be weakened for any of it - the counts moved only for
the party.


### bc. The chrome paints in the skins' colours, Nereus has its families back, and the settings screen picks them

The theme entry's *What this is not* list was written as a to-do, and this entry is it done: the Fluent chrome is
now painted from the palettes, Nereus offers the accent families its colour file lists, and a settings screen
picks the theme from the couch. (It also corrects a miscount made there: the Nereus file lists seven families,
not nine.)

**Fluent's chrome cannot be recoloured from outside, so the mapping is made of styles.** Avalonia 12's Fluent
theme is compiled C#: it names no `PART_` parts and reads no named resource keys - a scratch test measured a
button at `#33ffffff`, a text box at `#66000000` with `#99ffffff` text, a list at `#ff2b2b2b` and a progress bar
at `#ff0078d7` for every palette any application could invent. So `FrontendChrome.Build(palette)` says the
colours itself: author `Style`s for the controls no screen paints - buttons (panel at rest, raised on hover,
pressed, outline, button text), list and rows (accent under the selected row, as the skins put their brightest
colour where the cursor is), text boxes, progress, check boxes and separators - and `FrontendTheme.ApplyChrome`
adds them to the application for a real skin, removes them for the default one, and swaps the set when the skin
changes. Author styles outrank the theme's control themes, which is the only reason this works without touching
Fluent. The default palette never gets chrome: the pixels Fluent draws on its own are exactly the pixels this
front end always shipped, which is a test, not a hope. Scroll bars, check-box boxes, sliders and flyouts were
deliberately left to Fluent - half-mapped chrome reads as a bug in whichever half moved.

**Nereus's seven families are palettes now.** Its `Colors.xaml` lists a Light, a Medium and a Dark shade of each
of seven accent families, and its own settings screen let a viewer pick one; the rescue had pinned the palette
to turquoise, the first family the file lists. The other six are their own palettes - `nereus-blue` and friends,
built by a static constructor from the skin's own three shades - and a family may change only the three keys
Nereus itself tinted (the accent and the two panel shades); the orange of a complaint stays orange. The
provenance test reads their colours from the same skin file, a family test asserts the three-key rule and that
seven families answer with seven accents, and `FrontendTheme.Names` now carries eleven palettes.

**The Settings tile opens the front end's own settings, and the tile says so.** The `bb` entry's promise was
that a `ScreenKey` slot "stays null only while there is nothing behind it" - there is now, and it is this
screen. The old sentence stays true about the server: its configuration is still behind a .NET Remoting
interface the HTTP API has not grown an equivalent of, and no server setting was invented here. What this front
end can change is its own look, so the tile that MP2 called Settings opens `ThemeWindow`: one list, eleven rows,
all fitting one screenful because a choice you cannot see is not a choice, each row naming itself and painting
miniature panel-and-accent swatches. Selecting a row *is* the applying - the screen repaints itself in the
theme it names - and the pick is saved the moment it is made to `theme.json` beside the server settings; a
preview lost by pressing Back would be theatre. The startup chain is `--theme`, then `MP2_THEME`, then the
saved pick, then the factory colours: whoever typed a theme for one run outranks a choice made on another day.

**The pixels were checked, not hoped at.** The render gate draws sixteen frames now, and the new one is
`settings-nereus`: the probe looks for each of the eleven palettes' accent colours by hex and requires every one
of them to have a painted row - the first frame in this tree that proves "every palette reaches the television"
rather than "one theme was applied". The two `nowplaying` frames gained their buttons' panel colours from the
chrome mapping on the way and still pass, which is what swapping a chrome style set without stacking the old
one had to survive. `TestTheme` is eighteen tests: the chrome paints a button, list, selected row and text box
with palette colours, the default leaves Fluent's own pixels untouched, switching skins replaces one style set
instead of adding a second, a pick applies and outlives its screen and its process, the precedence chain holds
in both directions, and a `theme.json` that is not JSON is said out loud and chooses nothing.


### bd. The screen answers the keys and the mouse, because a viewer sat down and found it did not

**The report from the machine was blunt: nothing worked.** No tile answered a click, and Enter did nothing
anywhere. Two causes were in front of that, both invisible to every test in the tree: a screen that never asks
for focus, and an input device no test had ever pretended to be.

**The first cause: no screen had ever taken the focus.** Every action in this front end lives behind an Enter
handler that starts by asking which list holds the focus, and the answer on every screen was "none", because
Avalonia gives the focus wherever somebody asks for it and nobody asked. Arrow keys seemed to work: with
nothing focused the ring answers "go anywhere" by walking to its first area, which is exactly the accident
that makes a screen look alive while its Enter is dead. Every screen now names its starting point the moment
it shows - the menu on home, the feeds on news, the titles on the library, the shelves on browse, the start
button on party - so the first key already lands somewhere.

**The second cause: the focus does not stay where the screens assumed.** Once a list really holds the focus,
Avalonia moves it to the row the highlight stands on, and every `list.IsFocused` test then reads false - the
Enter raised after a walk was dropped on the floor, and the ring could not even be left, because a Left or
Right pressed inside a list answered "nothing has the focus" and teleported to the ring's first area. The
question every one of those branches really asks is "is the viewer standing on this list?", and the property
that answers it is `IsKeyboardFocusWithin`: on it, or on a row inside it. `DpadNavigation` and every
list-facing branch in the windows changed to it; only the checks about where the caret sits stayed as they
were. A test walks to the settings entry with the arrow keys alone and presses Enter after: the picker opens,
where the same walk once answered nothing.

**A double-click is the remote's Enter.** The screens know what Enter means on each list - open a tile, play a
title, choose a group - and they were never told what the mouse means, so a desk machine could select and
nothing else. `MouseRemote` routes a double-click on a list through the same Enter to the same handler on the
same window, so there is one behaviour, not two that can drift. A single click still only selects, because
that is what a click has always meant on a desk, and the key is posted to the dispatcher rather than raised
inside the click: the first click of a double-click is still busy selecting its row, and Enter asked about a
half-selected row opens yesterday's. The folders and themes screens needed neither - buttons and rows act on
a click there already - and the connection screen asks for the key on its list rather than its window, which
is the one place the helper raises it accordingly.

**Measured, not hoped.** Three new headless tests: the screen holds the focus the moment it opens; the real
headless mouse double-clicks the settings entry and the named screen appears; after a walk with the arrow
keys, the key after the walk opens what the walk selected - a viewer who can move the highlight but not open
it is no further than one who can do neither. The front end is at 424 tests, the whole gate is green at 1038,
and the sixteen render frames still draw.

### One window, one workflow: the screens stop opening as windows (phase 2, multiplatform)

The reference client never opened a new window when a tile was pressed. It had one window, a `WorkflowManager`
holding the stops the viewer had walked through, a navigation bar naming them, and a back key that popped one
stop rather than piling up windows. The multiplatform front end had grown eleven `Window` classes that each
opened beside the old ones, so the viewer ended up on a desktop of screens with no way back but closing them -
the opposite of what the reference did, and the reason the complaint about it was the right one.

**The workflow came back first, because everything else hangs on it.** `WorkflowManager` keeps a stack of
`WorkflowFrame`s - a state id, a label, the view model, and (once built) the view - with `Push`, `Pop`,
`PopToState` and `ReplaceCurrent`, and it says which frame it removed and in which order, nearest first, the
way the viewer walked past them backwards. `ShellWindow` is the one window: it hosts the current frame, names
the window after the depth, and shows the trail of stops above the screen - each crumb a button that walks the
workflow back to that stop. It hears the back key only after every screen has declined it, because a view with
a details panel open, a search box typed into, or a photograph raised has to answer the key about its own
business before the workflow moves. A frame's view is cached in the frame: stepping back from a title list
finds the scroll, the search and the selection where the viewer left them, which is what the reference's
screens did, and at the bottom of the stack the back key closes the application - one window, no orphaned
process.

**Every screen became a `UserControl` view.** Home, MediaBrowser, Browse, News, Weather, Pictures, Party,
Folders, Theme, Connection and NowPlaying: eleven windows deleted, eleven views and the shell in their place,
and `App.axaml.cs` now builds frames instead of windows - the view factory dispatches on the frame's state id,
the playback frame is built fresh per showing while its view model keeps the film's position, and a second
press of the same door is refused rather than opening a second copy. The activation moment the old screens
heard as `Opened` is the shell's now: a frame pushed before the window opens is activated when it does, so
the first key after opening lands somewhere again.

**Measured, not hoped.** Fifteen tests for the workflow and the shell alone - the stack, the removal order,
the crumbs, the back key declining, the title following the depth - and every screen test rewritten to host
its view in the real shell rather than in a window of its own, keys raised on the screen itself the way the
platform raises them on the focused control. One shell bug fell out of that rewrite and was fixed: the
single-content constructor pushed its frame before anyone listened, so the screen never reached the window.
The front end is at 439 tests, the whole gate is green at 1053, and the sixteen render frames draw through
the shell.


**The skins moved out of the binary and onto the viewer's disk.** A front end that carries its skins inside
its own assembly cannot be re-skinned by the person sitting on the couch, which is the whole point of a skin.
The engine now looks for a folder - `D:\MediaPortal\TIF\skins` unless `MP2_SKINS_DIR` says otherwise - and
reads every skin that has a `skin.xml` under it, parsing `colors.xaml`, `chrome.xaml` and the rest with
Avalonia's runtime XAML loader instead of the copy compiled into the executable. The embedded copy stays only
as the fallback for a machine that has no folder. Three tests write a skin to a throwaway folder, wear it,
edit the file in place, and watch the colours change on the next apply - which is exactly the workflow a
skin author will live by - and the render gate draws its sixteen frames through the disk path, so "the
reference XAML loads from disk" is a measured claim, not a hoped-for one.

**And one colour the gate caught.** With the skin's header finally sitting over the screens, the settings
list needed its eleven rows to fit beneath it, and the window's own factory colour was showing through the
chrome's margin at the corner of the television - a black theme with a charcoal corner, which is the exact
look that says the theme never arrived. The shell now takes the screen colour out of the worn theme and
paints itself with it, following every switch; the theme list tightened to fit; and the corner of every
frame is the palette's own colour again. The gate stands at 1075.


**The login stopped being a turnstile, and the settings became the place a household configures.** MP2's
server has always carried a switch called "Web authorization"; in the reference it guarded the web pages,
which is why nobody in a living room ever met it - the desktop front end did not speak HTTP at all. This
front end speaks nothing else, so out of the zip it met a login box before it had seen a single film, which
is the opposite of the reference's basic installation, where user management was something a household
*enabled* on its server if it wanted profiles at all. The switch therefore now starts off, it stays the
server's to turn on, and the home screen's sign-in boxes appear only when a server actually refuses an
anonymous caller.

**Settings opened as one question about colours; the reference kept a list of places.** The tile now opens
the hub the reference had: the front end's own machine first - its appearance, its server address, its
playback - and behind them the server with its media sources, where shares are added, removed and imported.
The hub owns no answers: every section opens the screen that already did that work, including the connection
screen, which the hub reaches through the workflow so the trail and the back key keep speaking about the
same stop. Playback offers the volume and the mute of the running player - said honestly, because the
reference's player configuration asked the player itself for renderers and audio devices, and this player
does not answer to those yet; the section names that instead of drawing a form whose buttons would lie.
Four tests cover the hub, and the gate stands at 1079.

### be. The videos have one door again: MP2's second tile for them left the tree

A viewer asked for one door to the videos rather than two, so `Home->Latest Media` is gone from this tree
rather than merely hidden from the menu. Out went the media plugin's workflow action, its contributor
model, its screen model, the three screens it opened - default, Nereus and WMC - the workflow state that
named them, its two language strings, the three style blocks that dressed only that screen, and the eight
tile pictures the shipped and the rescued skins carried for it. Out went the order that had been built for
it alone: `HomeSection.Sort`, `MainMenuEntry.Sort` and the shelf that opened in the order its tile had
promised, so a shelf opens by name now, which is the order it always had.

`TestHomeMenuContributions` stayed green without an exception, which is the point: the contribution is
missing from the plugins' own `media-actions.xml` and from their `strings_en.xml`, not only from the table
that copies them. What stayed is the other kind of "latest" - the media plugin's `Latest*` list providers
feed the home wall's "Latest added" bands, and the server still sorts titles by the date it first saw the
file, because every one of those bands is read newest-first on every home screen.

### bf. Photographs have one word again: the second name this front end invented went away

A viewer counted tiles and found two of them for one kind of media, which was right to look like a bug. MP2 has one
word for photographs - `Media.ImagesMenuItem`, "Images" - and this front end had quietly grown a second one:
`MediaCatalog` labelled the `ImageItem` aspect "Pictures", the pictures screen headed itself "Pictures", and because
the Images tile claims no shelf of its own, the leftover rule handed any server that had filed image titles a *second*
tile reading "Pictures" right next to MP2's "Images". One medium, two words, three places.

Both words are now MP2's. The catalogue, the screen's heading and the workflow frame's title all say "Images", the
`Home->Images` row claims `ImageItem` - which keeps the leftover rule off it without turning the tile into a shelf of
titles, since the tile still opens the folder walk a household's photographs actually need - and the filed titles lose
nothing, because the browse screen lists them under the same word. What stayed is prose: the status line still says
"12 pictures" under a tile called Images, exactly as a tile called Movies says "12 films", and only that.
`ThePicturesHaveOneTileAndItWearsMP2sOwnWord` demands one tile, one word, and a tile that still opens folders rather
than titles, and the four screen tests that walked out of a folder now wait for the heading the tile promises.

### bg. The home head line stands in the reference's three columns, and its clock grew into their height

A viewer asked for `master_home.xaml`'s head line rather than the stack this face had improvised under the clock, and
pointed at the skin settings hanging off it, which is the correct way to ask about a reference skin. The reference's
"Time Label" grid is three Auto columns read from the right inwards: the clock furthest right, the day's weather
*picture* in the middle, and two quiet lines to the left of the picture, right-aligned against it - the day of the week
over the temperature. This front end had four lines under a clock instead, and had put a sentence about the sky where
the reference put a picture of it.

The Nereus header now wears that grid. The picture is the worn skin's own - the file under its `images/Weather/128x128`
folder, named by the same night-aware code that dresses the weather screen - so the head line's sun is the sun the
screen shows, and `ShowWeatherCondition` guards it, which is precisely the element that dial guarded on the reference's
master; no line of words repeats what the picture says. `ChromeInfo` carries the picture's name instead of the
condition's words, and the shell hands it over with the temperature from the same reading. The clock is bigger: the
reference drew its clock at `DateTimeFontSize` (55) over two lines of 21, so the clock came exactly as high as the two
lines stacked, and since this face draws those lines at `SmallerFontSize` (27), the clock wears their proportion - 27
times 55 over 21, which is the new `HeadlineTimeFontSize` of 71. The four dials still answer for their own part
(`ShowTime`, `ShowDate`, `ShowTemperature`, `ShowWeatherCondition`), and the whole slot is still the home screen's
alone, because reading the weather was `master_home`'s business and no other master's.

That head line was grown into its height by the change above, and it was too tall for the room it had: a viewer found
the clock cut off at the top right. The reason is a measurement rather than a drawing. The three columns want 667 of
the 1280 lines - the clock alone asks for 173 of them - and a `Grid` asks an Auto column what it wants and hands it
that whether or not the window has it; what the window does not have it pushes past its right edge, where the header's
clip cuts the last digit off. A wide television never noticed, which is why the gate passed and the couch did not. The
header is a `DockPanel` now, with the head line docked first and the screen's words filling what is left - a docked
child is asked against what remains, which is the question the head line actually needs answered - and the cluster
stands inside a `Viewbox` that only ever scales down (`StretchDirection="DownOnly"`), so a screen with room draws
every part at the size the skin asked for, and a narrow one scales the row as a whole instead of robbing the clock of
its last digit or the title of its words. `TestChromeHeader` draws the head line at both widths and reads where the
clock is *painted* - `TranslatePoint` and not `Bounds`, because inside a scaled row the bounds are the wish and the
transform is the drawing.

### bh. Settings the server keeps can be edited from a television, over the API that already carries the pictures

A household that has no keyboard in the living room still has settings it wants to move: how many fanart
pictures of each kind the library keeps, how much the server writes down. Until now both answers lived on the
server's machine and could only be changed there, because MP2 keeps them through `ISettingsManager` and only
the server ever holds that. The API assembly now publishes a small catalogue of the numbers and switches it is
willing to be told about - `PluginSettingsCatalog.cs` beside the controller that serves them - and answers
three calls over it: the list of sections, one section read, one section written. A section is a title, an
owner, and fields; a field is one of three kinds, `toggle`, `number` or `choice`, and those three words are the
whole contract between the two halves. `GET` and `PUT` answer in the same shape, so a television that has read
a section knows exactly what it sent and what the server kept. The write refuses a whole change over one bad
field - 400, one sentence, nothing stored - which is what a settings file wants anyway, since half a change is
a settings file that no longer adds up.

Two sections were in the catalogue the day this was written. The reason nothing about plugins was in it then was
given wrongly: MP2 does keep options that belong to parts of itself, six of them under the extractors' own
headings, and `bi.` below counts the whole tree, corrects what is said here, and says which of those answers
are asked elsewhere in this front end. `fanart` belongs to the media library and holds the seven counts that say
how many pictures of each kind one title keeps. `logging` belongs to the server and holds one choice,
`log_level`, which is stored at once but only takes effect when the server starts again - and the answer says so
in `restartNeededFor`, so the television can admit it instead of letting a household believe the logging
changed.

The screen half is one hub row, "Plugin settings", owned by "Server", next to the other server-owned rows, and
one view holding both halves of the work: the list of sections the server offered, each row pressing open into
an editor that stands up in the same screen. The client keeps no copy of that table - not the sections, not
their words, not their fields - so a section somebody adds to the catalogue appears on the television by
itself, and the day a third server build offers a kind of field this screen has never been shown, the screen
says that out loud rather than crashing or drawing a box around a value it does not understand. Each kind wears
the control that kind asks for: a switch, a number box with the range it refused outside of written beside it
and the on-screen keyboard a remote needs, and for a choice one press per name in a horizontal palette, which is
what a remote has. Rows are only sent when they differ from what the server answered, so a save cannot overwrite
what another television saved in between, and after a save the screen adopts the values the server returned
rather than the ones that were typed - the server may well have clamped one. A row that cannot say what it holds
(a number that is not whole, or stands outside its range) is refused where it was typed and stops the whole
write before the wire, and a failed call is quoted in the status line with the form left as the viewer left it,
because a settings screen that shows an empty form after a failure is worse than one that says it heard nothing.
`ASectionThatWasLeftAloneSendsNothingAtAll`, `AChangedNumberSendsThatFieldAndOnlyThatField`,
`ANumberOutsideItsRangeIsRefusedByItsRowAndNeverLeavesTheScreen` and `AKindThisScreenCannotDrawIsSaidAndNeverSent`
hold those four promises.

Open: the catalogue is short, and a plugin's own settings only become editable here the day a plugin is given a
settings class and a catalogue entry naming it; `choice` carries bare names, so a field whose options the server
would rather explain per option has nowhere to say it; the screen has been walked with a keyboard, never with a
remote, which stays with everything else in the list below.

### bi. Every settings question the reference asked, and where each of its answers lives now

A screen that looks empty is often a screen whose questions were never counted, so the reference's settings
were counted before anything was declared missing. Its tree is not written down in one place: every plugin says
in its own `plugin.xml` which part of the tree it fills, with `<Register Location="/Configuration/Settings/...">`
around `ConfigSection`, `ConfigGroup`, `ConfigSetting` and `CustomConfigSetting` entries, and the configuration
program assembles those registrations into a tree (`Source/Core/ConfigurationManager/Builders/ConfigBuilder.cs`).
Reading all eighteen of those files that carry settings gives sixty sections under nine root headings, and the
useful fact about them is not their number but their *ownership*: some answers belong to the machine that stores
the library and are therefore the server's, and some belong to the machine wearing the skin, which the reference
kept on the same screen because it was always the same machine asking both.

| Root | Who answered it in the reference | Where the answer is asked now |
| --- | --- | --- |
| General | the client's own machine: autostart, version, connectivity to a server, language, date and time formats | split: what the server binds and how it authenticates is in "Server settings" over the API; what the box looks like and which language it speaks is in "Client settings", kept beside the front end |
| Appearance | the skin and the worn theme, plus background and screensaver | "Skin settings" and "Theme", both beside the front end; the skin dials the reference hung on each skin are the front end's `SkinSettingsBase` |
| Shares | the server, through its importer | "Server settings" and the shares and credentials endpoints, over the API |
| Media | mostly the server's extractors, partly the client's browsing | "Metadata" (extractors) and "Plugin settings" (artwork counts, logging, sidecar readers), both over the API; browsing choices are the screen's own |
| Players | the machine that plays | "Playback settings", beside the front end |
| TV | TV service and client | nowhere yet - the TV service layer is not back on the new server (see the open work below) |
| Plugins | news, weather, TV providers | news feeds and weather places are kept beside the front end and are edited in their files, not on a screen |
| Input devices | the keys, remotes and buttons of the machine in front of the television | the front end's own keyboard files; the reference's key-to-action tables have no equivalent screen here |

The ninth heading, `ServerSettings`, is declared but stands empty: the server's own component registers it and
then hangs every question it owns under `Media` and `Shares` instead, which is why the two tables below speak of
those branches rather than of it.

**The server's own branch** (`Backend/BackendComponents/ServerSettings.Client`) is the part that could move
across as it was asked, because every one of its answers belongs to the machine with the library:

| Section in the reference | Its questions | Where they are asked now |
| --- | --- | --- |
| `AudioMDESettings`, `VideoMDESettings`, `MovieMDESettings`, `SeriesMDESettings`, `ImageMDESettings`, `SubtitleMDESettings` | which source answers for each kind of title, whether it may look online, whether it may walk a share offline, what it filters, which languages it wishes | "Metadata", over `api/media/settings/metadata`; the detail questions (whether the people who acted, played a character, directed, or - for an album - the artists and albums are kept, and how many of each) joined it later |
| `FanArtSettings` | exactly the seven `ServerFanArtMax*` counts | "Plugin settings" > "Artwork", seven numbers with the same 1-20 ground |
| `LanguageSettings` | the server's own language and the languages it wishes for audio, films, series and subtitles | the four wished languages are fields of "Metadata"; the server's own language is not asked, because a headless server reads nobody's screen |
| `ExtendedConfigureShares` and the `Shares` category | the shares, the credentials they need, when the importer runs | the shares, credentials and import-time fields of "Server settings" |
| *(nothing at all)* | the readers of the `.nfo` files a tagging tool leaves beside a title | three sections in "Plugin settings" (`nfo_movie`, `nfo_series`, `nfo_audio`) |

That last row is a correction, including of an earlier entry in this file, which said the reference offered
configuration sections for the extractors themselves. It did not: the six `*MDESettings` sections belong to the
server's own components, and the `.nfo` readers were only ever answerable by editing XML by hand. Asking those
three readers what they keep is therefore going beyond the reference rather than catching up with it, and their
fields were chosen to match what the extractor classes actually do with the answers.

**The client's branch** is where most of what a viewer might call "missing" actually is, and the reason it is
not on the server is not an oversight: the reference asked those questions of the machine wearing the skin, so
bringing them across means answering them again in the front end, not adding another API endpoint.

| Question in the reference | Where it stands |
| --- | --- |
| skin dials (fanart, opacity, watched flags, grid shapes, help texts, touch, dialogs, colours) | asked in "Skin settings", per skin and per viewer, as the front end's `SkinSettingsBase` files |
| which language the box speaks | "Client settings", and it carries the weather's and the news' language with it |
| date and time formats | asked in "Skin settings" as two boxes, per skin and per viewer, as the reference kept them on the worn skin; the header spells itself from them again the moment one is saved (see `### bj.`) |
| which country's age certificate a title shows | asked in the server's "Certificates" section, and the server translates with the reference's table when it hands a title out (`### bj.`) |
| whether virtual folders are walked | asked in the server's "Virtual folders" section, three switches, one per kind of media. The reference had one switch for all of them, and this server already asked every media call whether it should include virtual items - it never asked anybody, and the answer was always no (see `### bj.`) |
| the player's geometry, audio renderer, codecs, skip steps | "Playback settings" holds the ones that mean something without DirectShow; the codec tables are DirectShow's own and stay behind |
| news feeds and their interval, weather places and their API key | kept in `newsfeeds.json` and `weather.json` beside the front end; no screen edits them yet |
| keys, buttons and remote actions | the front end's own keys, in its keyboard files, not a settings tree |
| TV: rows, hours, zap-on-press, auto-start, genre colours, channel list, channel logos, recording notices, the picture address and the timeshift numbers | asked in "TV settings", the client's own page (`### bs.`, plus the two additions in `### bu.` and `### bv.`); only what the reference left unset is written down, and the answers are obeyed by the guide, the live screen and the recording watcher |
| TV: what the reference's television section asked of the *server* | nothing at all. `TvServerSettings` was empty apart from a comment saying the reference had no server-side television settings, and that is still true with the client branch in place |

Open, stated as work rather than as a gap: three questions of the extractors' branch that "Metadata" does not
offer yet - `OnlyLocalMedia` on the film extractor and on the series extractor, which is what the reference's
"download metadata for every film of a collection" and "...every episode of a series" switches wrote, and the
image extractor's geolocation lookup - all three belonging to extractors a headless server may not carry at
all, which is the one thing to check before they are offered; and screens for news feeds and weather places.
The television branch that used to be in that list is in the sections below it, and is asked and obeyed now.
The three questions this section left open - the two formats, the certificate country and the virtual
folders - were closed by the section after it.

### bj. The three open settings closed: the country a mark comes from, the titles there are not, and how the clock spells itself

Three rows of the table above were written as "open", and one of them was written wrong as well, so it is
corrected before it is closed. It claimed that this server's media endpoints never ask for virtual items and
that the reference's switch would therefore be "a handle on a door that is not there". The door has been there
since the library's own interface was ported: `IMediaLibrary` takes an `includeVirtual` argument on the search,
the count, the item fetch and the value-group queries, and a virtual title is one the library learned about from
a sidecar or a collection while no file of it stands on any share - an invented title, which is what the
reference's word "virtual" means here. What was missing was not the door but the question: every one of the
media controller's eight calls answered `false`, on its own behalf, for everybody, forever.

**The country a mark comes from.** The reference kept two strings on the media plugin's settings
(`UiComponents/Media/Settings/MediaCertificationSettings.cs`, per viewer, empty by default) and asked them in a
group called "Certification countries" whose entries are "Movie certification system country" and "Series
certification system country", each opening a list of the countries the table knows whose first entry is
"Original (as imported)". Nothing was ever stored differently: the importer's mark stays in the title's aspect,
and the client rewrote it at display time (`Helpers/CertificationHelper.cs`) with the table in
`MediaPortal.Common.Certifications.CertificationMapper`, blanking the label when the table had nothing to answer
with. Here the translating is the server's, because its screens are other programs that are sent words; the
settings class therefore moved next to the table it uses (`MediaPortal.Common/Certifications/`), and its answer
is asked of `GetItem`, which is where one title is described to one screen. Two choices were made against the
reference and are worth their reason:
- *Global rather than per viewer.* The reference could ask a signed-in person; a headless server answers a
  browse before anybody is known, and a per-viewer certificate country would mean the media API carrying who is
  asking. Until it does, the whole household reads the same country.
- *An untranslatable mark is kept rather than blanked.* The reference dropped the label when its table could not
  answer, so a household that chose "DE" and owned a film rated in a country the table does not know got no
  certificate at all. Here the importer's own words stand: a card showing a foreign mark is a smaller lie than a
  card showing nothing, and the reference's forgiving reading of the stored words ("Rated PG for rude humor" is
  a `PG` to it, because it begins with a mark it holds) is kept exactly as it was.

**The titles there are not.** The reference's three switches live on the media plugin's `ViewSettings` (per
viewer, all off by default: `ShowVirtualSeriesMedia`, `ShowVirtualMovieMedia`, `ShowVirtualAudioMedia`), are
edited as one multiple-choice list ("Show all known media items", with one entry per kind), and are read through
`VirtualMediaHelper.ShowVirtualMedia(necessaryMIATypeIds)` by every browse, every filter criterion and the plain
text search. The helper looks at what a query *asks for* - audio aspects, then series aspects, then films - and
that is the shape this port keeps, because a browse is always about one family at a time.
`MediaPortal.Backend/MediaLibrary/VirtualMediaSettings.cs` holds the same three answers, Global for the same
reason as above, and answers two questions: `ShowsVirtualFor(aspectIds)` for the list, count and value-group
calls, which know what kind they are about, and `LetsVirtualTitlesIn` - the three of them OR-ed - for the five
lookups by id, which are handed an id and no kind. That second question is the one a viewer notices: switching
on invented films should not be the only way to open the page of an invented film one already has a link to.

**How the clock spells itself.** The reference kept `TimeFormat` (default `"t"`) and `DateFormat` (default
`"D"`) on `SkinBaseSettings` - on the worn skin, per viewer - and its configuration entries were plain text
boxes that, after saving, sent the `DateTimeFormatChanged` skin message so the running header rewrote itself.
All three parts of that are here: the two fields are on the front end's `SkinSettingsBase`, so both skins carry
them and either viewer may answer them differently; "Skin settings" shows them as the typed boxes the reference
showed, which is why the row needed a fourth kind in the settings API besides the switch, the number and the
choice; and the shout across the skin is `ChromeInfo.WearFormats`, said when a skin is put on and again when a
format is saved. One visible consequence: the head line now reads `t` rather than the `HH:mm` this front end had
decided for everybody, so an English box says "2:30 PM" and a German one says "14:30" - which is what both
reference skins did, and what the language chosen in "Client settings" has always promised. A format the
calendar cannot read, including a box somebody has only begun to type into, falls back to the shipped answer
instead of blanking the head line.

Tests: seven in the media controller's own fixture (invented films arriving and staying away, the three
switches minding their own kind, a lookup by id following its kind, `R` becoming `FSK 18` and `TV-14` becoming
`FSK 16` for a German household, a country holding no marks leaving the words alone, the same country changing
nothing) and five over HTTP (the two new sections being listed, an unset country answering as empty text, a
stored country landing trimmed, a number offered for a word being refused, the switches going off to on); on the
front end, `TestChromeFormats` for the head line, two rows and a typed box in the skin settings screens, and a
text row in the plugin settings screen. `Tests.Server`: 486 of 486. `Tests.Frontend`: 846 of 848, the two
failures the pre-existing icon ones.

### bk. Where the television comes from: the backend's address, and what is asked of it

The TV layer had to start somewhere, and the first thing to learn about it was that the reference has no TV
either. Under `Source/UI/TV` stand four projects - `SlimTvInterfaces`, `SlimTvClient`, `SlimTvNativeProvider`,
`SlimTVMPExtendedProvider` - and every one of them is a client: `ITvHandler` handing out a
`IChannelAndGroupInfo`, a `IProgramInfo`, a `IScheduleControl` and a `ITimeshiftControl`, the two providers
reaching the actual tuners over UPnP or WCF. The service behind them was deleted deliberately, in `029a262aa4`
("Remove the TV service layer (SlimTvService3/3.5 and friends)", 54 files and 6833 lines), because it was
.NET-Framework-only. So there is no card code, no EPG store, no timer database and no XMLTV importer in this
tree to port: the programmes, the timers and the recordings were always somebody else's job, and this front end
has to name that somebody.

That somebody is **TvHeadend over plain HTTP**, which the viewer chose: one daemon on any machine in the house,
holding the cards, the programme data and the recordings, answering JSON on port 9981 without a client library,
a generated proxy or a platform. What was built here is the asking half only - no screens yet:

- `Settings/TvBackendSettings.cs` keeps the address in `tv.json` beside `frontend.json`, overridable with
  `MP2_TV_FILE` for the gates exactly as the weather file is with `MP2_WEATHER_FILE`. Host, port (9981), the
  name and password it may ask for, and how many days of guide to read ahead (fourteen, which is the reference's
  own `EpgVisibleHours`-sibling `SingleChannelGuideDays`). An empty host is not an error but a box that has not
  been told, `BaseUrl` refuses a port that is not a port, and `Describe()` says host and port and never the
  password.
- `Api/TvModels.cs` holds what a screen is drawn from: a bouquet, a channel (with its `Uri? StreamUrl` already
  resolved), a programme with its instants, an episode, now-and-next, and `TvGenreColors` - the reference's
  eight colours from `SlimTvGenreColorSettings` (`#7532a8` for a film, `#ed7d31` for a series, `#4f7a32`,
  `#c89800`, `#4e93d2`, `#c03636`, `#00817e`), matched by word in either language because the same programme is
  "Film" here and "Movie/Drama" there, with children before drama exactly as the reference painted it.
- `Api/TvHeadendApiClient.cs` asks: `api/channeltag/list` for the bouquets, `api/channel/grid` for the channels
  (with `api/service/grid` behind them, because a channel is only ever called radio through the DVB type bits of
  the services behind it, falling back to the bouquet's and the channel's name when a backend will not say),
  `api/epg/events` for the programmes and `api/epg/content_type/list` for what that backend calls its kinds.
  Three things about that interface shaped the code: times are seconds since 1970 in UTC and become
  `DateTimeOffset` on arrival, because a living room in Lisbon and one in Helsinki have to share one guide
  layout; a table answers at most `limit` rows and says the real number in `total`, so every question pages
  rather than assuming its window is small - a fortnight of a forty-channel bouquet is not; and a window is
  asked for as an overlap (`start < to AND stop > from`, in the backend's `filter` JSON), because a programme
  that began before the guide opens and ends inside it is on the screen the moment it opens.
- `TvBackendException` keeps the three failures three, the way `ServerApiException` does: nobody answered, the
  name was refused (`NeedsLogin`, which a screen turns into "fill the password in"), and something answered that
  was not the answer a TV backend gives - which is what a wrong port landing on a router's web page looks like,
  and is worth saying out loud instead of as a number.
- The "Television" row in the settings hub opens `Views/TvSettingsView.axaml`, whose centre of gravity is the
  press that asks: bouquets, channels, radio, and what the first channel is showing right now, or which of the
  three failures happened. The reference needed no such screen because its TV service was the machine's own;
  here a wrong port would otherwise fail as an empty guide somewhere else rather than as a mistake where it was
  typed.

Not done, and deliberately not attempted here: the guide, live television and radio, and the recording timers.
`MainMenu`'s television and radio entries still carry their "no TV backend" small print, and `BuildNamedFrame`
still does not answer `tv` or `radio` - pressing a Nereus TV shortcut is still the exception it was. Those are
the next parts of the same plan.

Tests: thirty-six in `TestTvBackend`, with the backend scripted inside the file. They pin the bouquets arriving
in the backend's order with their services counted, the channels arriving in the order a viewer would say them,
the two-slash `//stream/channelid/12` becoming a real address and an absent one being built from the number,
radio coming from the service bits and - when the service grid stays silent - from the bouquet, the
`Authorization` header travelling only when a name was written down, the window being asked for as an overlap in
seconds since nineteen seventy, an empty page answering as an empty table, a table with more rows than it sent
being walked to `start=2000`, the news bulletin arriving as the instants it is with its progress, its age rating
and its recording in progress, the film without an end taking one from its length, now-and-next, the eight genre
colours and the DVB genre nibble, and the three failures being three sentences. Nine more over the settings file
(absent, round-tripped, hand-edited with nonsense in it, not JSON at all, `MP2_TV_FILE`, a description that
never says the password) and seven over the screen (what the file holds arriving in the boxes, half-typed
answers staying answers while a month of guide does not, and the press saying how many channels came back,
which backend refused, and which one was never set up). `Tests.Frontend`: 882 of 884, the two failures the
pre-existing icon ones.

### bl. The guide, and what a column is worth

The television and radio tiles open a programme guide now. Before writing any of it the reference was read
properly - `Skin/Nereus/screens/SlimTvClient-fullguide.xaml` for the layout, `Controls/EpgGrid.cs` for the
arithmetic, `SlimTvColors.xaml` and `SlimTvStyles.xaml` for what a block is filled with and how a focused one is
framed - because the guide is the one screen whose wrongness nobody can point at: a bar a column too far left
looks like a guide, it just tells lies about the evening.

What the reference does, and what was kept. Its grid is seventy-five columns wide across the hours the viewer
asked for (one to four, two by default), with a fixed header column of four hundred and fifty pixels holding the
number, the logo and the name, a row height of one ninth of the screen, a block placed at the column its own
beginning falls in and stretched across the columns its own length covers, a page scroll of thirty minutes when
the viewer walks off the edge, a now-indicator recalculated every ten seconds, a two hundred and twenty pixel
band of detail along the bottom over a `#D9000000` scrim, and a focused block wearing three pixels of white with
three of black behind it. All of that is the shape of this screen too. Three things differ, each on purpose. The
window here is ninety columns of two minutes - three hours, which is the reference's two widened to what a flat
screen has room for, and fixed at two minutes a column so that the arithmetic has one unit instead of one that
changes when a setting does. The grid is laid out in code rather than in a template, because "column thirty-seven
across fifteen" is not something a `DataTemplate` can say, and the ruler, every row and the line at now are built
on the same ninety star columns so that they agree at a thousand pixels wide and at three thousand. And the block
chrome is this skin's: two pixels of white for the block being talked about, a dot in the corner for a recording
rather than the reference's `record_single.png`, because this skin has no television pictures to draw yet.

The questions the guide asks are deliberately small. One bouquet at a time rather than every channel at once - a
fortnight of programmes for two hundred channels is not a question to ask a television over the living-room
Wi-Fi. A window that walks by half a screen at a time, which is the reference's thirty-minute page widened to
this window's size. The answer to a superseded question is thrown away, because the arrows can be held down and a
guide that painted answers in the order they happened to arrive would end up showing a window the viewer left
three presses ago. A channel whose programmes did not arrive keeps its row: a guide that quietly removes channels
is a guide a viewer cannot trust. A bouquet that names no channels falls back to every channel of its own half of
the set and says out loud that it had to guess, which is what a backend that never links its channels to its tags
would otherwise look like - an empty evening.

Neither guide offers the other half's bouquets, which is the only place names are read, and the radio guide opens
on the bouquet whose name says radio; a radio guide that opens on the first bouquet in the list opens on a
television bouquet and shows somebody their radio as an empty screen. `MainMenu`'s television and radio tiles
lost their "no TV backend" small print and name screens now, `BuildNamedFrame` answers `tv` and `radio`, and the
four menu tests that counted the screens openable without a library count nine and ten instead of seven and
eight, with the guides' reason written into the sentence: a television does not live in the library.

Not done, and deliberately not attempted here: the arrows walk the buttons and the scroller, not into the grid -
there is no focus ring over the blocks yet, so a keyboard can only reach a programme by mouse. Pressing a
programme selects it and says what it is; watching it and asking the backend to save it belong to the player and
to the timers, which are the next two parts. There are no channel logos, so the header column is a hundred and
ninety pixels where the reference's was four hundred and fifty. And the miniguide over live television, which is
where the reference's `ZapFromGuide` lived, waits for the same reason.

Tests: thirty in `TestTvGuideScreen`, with the backend scripted inside the file and its programmes anchored to
the running clock. They pin the television guide opening on the backend's first bouquet and the radio guide on
the one that says radio, the bouquets that name no kind staying in both lists because guessing about a name takes
channels off somebody; a picked bouquet reading its own channels by their own numbers; the window being asked for
as the window the grid is showing; a ninety-minute film standing in the column its beginning falls in and across
forty-five columns; half an hour across fifteen; a programme already running when the window opens starting at the
left edge and saying its beginning is missing; one that ended before the window not being drawn at all; one
running past the right edge being cut at it; the hours on the ruler being full hours inside the window; the line
at now vanishing over tomorrow; a step moving the window half a screen and asking again, and leaving the
programme being talked about still being talked about; the bouquet buttons wrapping at both ends; the guide
showing the window it last asked about whatever the earlier answers said; the details falling back from the long
description to the summary to the broadcaster having said nothing; the series line arriving only when it was
told; the blocks wearing their genre's colour out of the reference's eight; a refused backend being a sentence
rather than an empty evening; a backend with channels but no programme data still showing its channels and naming
the table that refused; the fallback for a backend that never groups, and the honest empty grid for a bouquet that
really is empty. Four of them look at the drawn screen rather than at the model, because the two agree only
through the attached properties the code-behind sets ninety times a row: one box per programme, each in the
column and across the span the model gave it, exactly one block wearing the frame, and the hours the model named
being written on the ruler. `Tests.Frontend`: 913 of 915, the two failures the pre-existing icon ones.

### bm. The picture, and what a number on a remote means

The watching screen exists now: a channel, its programme data, and the digits somebody types on a remote.
`ViewModels/TvTunerViewModel.cs` is the tuning half, `Views/TvWatchView.axaml` the surface, and the route in is a
press on a programme that is running at the moment - which is the reference's `ZapFromGuide`, on unless somebody
turned it off, and why a guide that knows how to reach the sofa is worth more than a list of what is on.

What the reference does, and what was kept. Its watching surface is
`Skin/Nereus/screens/FullScreenContentTv.xaml`: the picture filling the screen, `VideoBrush Stream="0"`, and a
container two hundred and sixty pixels high along the bottom that slides up over it in three hundred and fifty
milliseconds (`PrimarySlimTvOSDStyle` in `themes/default/styles/SlimTvStyles.xaml`) and holds the channel, the
programme running, the one after it, and a progress bar eighteen pixels high with the time the programme ends at
its right end - the start time is in the markup and commented out, because where a programme began is not a thing
anybody asks for while watching one. The strip here is that shape and that proportion. The next programme is in
`#009999` (`SlimTvColors.xaml:7`), the only accent the watching screens carry, and there is no literal "Now" or
"Next" anywhere because the reference had none either - the two blocks are told apart by where they stand, and here
the one word `next:` does the same work in one line instead of two columns. Radio is the same screen with the
picture absent rather than black, which is what `FullScreenContentRadio.xaml` does when it replaces its video
surface with a photograph and takes the picture's keys off the keyboard.

The digits are the part that cannot be looked at, so they were read carefully. The reference showed the typed
number and nothing else: `superlayers/ChannelZapOSD.xaml` is a box two hundred pixels wide with the digits in it,
self-hiding - never a channel list, because a list answers "what is on?" and the person typing a number has already
answered that. It waited `ZapTimeout`, two seconds by default (`Settings/SlimTvClientSettings.cs:47-49`, described
in its own settings text as "Configures the delay until tuning starts."), and rearmed that wait with every digit
(`Models/ChannelZapModel.cs:148`), because the alternative is a remote whose digits each tune a channel. Then the
number was read as a **place in the bouquet**, one-based, whenever `ZapByChannelIndex` was on - which it is by
default, because that is what the buttons mean to the person pressing them, not to the backend. A lone `0` was zap
back (`ChannelZapModel.cs:167-170`) and a `0` after other digits was a nought, which is how 101 is spelled. All of
that is here, with one exception worth naming: `ChannelZapModel.ZapTimerElapsed` clears `ChannelNumberOrIndex`
*before* parsing it, so in the reference a digit zap never actually fired. That is a bug and it was not copied.

The list over the picture is the reference's miniguide (`Skin/Nereus/screens/SlimTvClient-miniguide.xaml`), kept as
it was drawn rather than as a screen of its own: it is opened as a dialog whose host is transparent, so the channel
keeps playing behind it while its rows are walked; its rows are `EpgSlimRowHeight`, fifty pixels, filled `#2c2c2c`
and `#757575` when they are the one on the screen; its columns are the channel's own number, logo and name, and
what is running on it and after it. And a press on a row **watches** - recording was a different key on a different
menu. Its rows are filled from one question about the whole bouquet rather than one per channel, which is the only
way the list arrives at all: two hundred questions to a machine in another room is not a way to fill a screen.

Four things were left behind on purpose. **Refusals are said out loud.** The reference said nothing in three
different places - a channel with no stream (`SlimTVMPExtendedProvider.cs:246-247`), a tune the backend could not
serve, and a number matching nothing (`ChannelZapModel.cs:141-181`) - and in all three the viewer was left on the
black screen the zap had taken them to, holding a remote that seemed to have stopped working. This says which
channel, which machine and how many channels there were to choose from. **Leaving the screen stops the channel**;
the reference let the sound carry on behind the rest of the media centre, because nothing between its workflow
states ever stopped the player (`SlimTvClientModel.cs:1023-1031` disposes the model and leaves it alone), which is
a habit for a box whose only job is being a television. **The television and the radio are one player here**: the
tuner owns nothing and asks through `PlayRequested` and `StopRequested`, so the reference's swap of two player
objects (`TVHandler/SlimTvHandler.cs:213-221`) became the other tuner being told its channel has gone - and asking
it for a stop would have stopped the picture that had just arrived, which is what a test now prevents. And a live
channel carries **no media id and no report** to the library: the server's "continue watching" shelf is made of
things somebody chose from it, not of whatever the television happened to be showing.

One thing was found on the way that is worth reading twice, because it is invisible from every test that existed.
`TvGuideViewModel` raised a plain `PropertyChanged` event without carrying `INotifyPropertyChanged`. The code
beside the model subscribes to the event directly, so the grid redrew, the focused box was framed, and every model
test and every drawn test passed - while Avalonia's bindings look for the interface, bind nothing without it, and
say nothing at all. The guide's details band along the bottom therefore went on telling the viewer about the
programme under the cursor when the screen opened, forever, and the new tuner would have done exactly the same with
its digits and its programme line. Both now carry the interface, and there is a drawn test that walks the cursor to
another programme and insists the words along the bottom changed with it - the only shape of test that can see this
class of mistake, since the mistake is precisely that the model is right and the screen is not.

Not done, and deliberately not attempted here. There is no timeshift, so live television cannot be paused or
rewound; TVHeadend offers it (`api/multicast/...` and its timeshift endpoints) and the reference's whole watching
experience was built on the timeshift file its provider handed back (`SlimTVMPExtendedProvider.cs:230-260`), so
that is a real gap rather than a missing detail, and it belongs with the recording screens. There are no channel
logos, so the miniguide row is number and name where the reference's reserved a hundred and fifty-five pixels for a
picture. Nothing routes the keyboard's digits to the tuner yet - the model takes them and the screen shows them,
but no key binding hands them over - and the reference's PiP, subtitle, audio-stream, aspect and record keys have
no equivalents at all. Pause, skip, volume and mute are not bound on this screen either, which is the reference's
own omission (`Skin/default/screens/FullscreenTvKeybindings.inc` has no mute and no volume among its watching keys).

Tests: twenty-one in `TestTvPlayback`, with the backend scripted in the file and its programmes anchored to the
running clock snapped to the minute, so the end-of-programme time the strip prints cannot move between the answer
and the assertion. They pin the backend's own streaming address reaching the player untouched; an unwatchable
channel (no stream URL *and* no number, the only combination that really has no address) saying so and asking for
nothing; the same channel twice asking for nothing; zapping wrapping at both ends of the bouquet; going back being
the channel before this one; the digits accumulating into one number and a number nothing matches saying how many
there were; a finished number picking the channel at that place, counted from one; a lone zero going back and a
zero after digits being a nought; Back taking the number off without choosing; the list over the picture arriving
from exactly one question about the bouquet and a channel with no programme data getting the reference's own words;
the strip following the running programme including its end time; a refused now-and-next leaving the picture alone
and saying which of the two it could not do; a radio with no picture opening on the bouquet that says radio and
being played by the same player. Three of them look at the drawn screen: the digits appearing in the middle of the
picture and going away when taken, one row per channel with its two programmes and a press watching it and clearing
the list, and the radio drawing no picture at all. `TestTvGuideScreen` grew a thirty-first for the bindings.

### bn. The timers: what a viewer asks a TVHeadend to keep, in its own table and its own words

The guide already marks a programme that is being recorded, because a TVHeadend says so unasked on the programme
itself. What was missing is the other half: the screens that ask for a recording and the screens that show what has
been asked. This is the layer underneath those - the questions themselves, and the answers in the backend's shape.

The reference reached recordings through two doors and never confused them: `IScheduleControlAsync` for the plans it
could cancel (`CreateScheduleAsync(program, recordingType)`, `RemoveScheduleForProgramAsync`, a
`RecordingStatus` flag per programme) and the media library for the files that were already written, which is why it
had a schedules screen and a recordings screen rather than one list. A TVHeadend keeps both in one table and marks
the difference in the entry's state, so one `TvRecording` covers both screens - with the file's address, the size,
the priority, the retention and the backend's own complaint (`disabledText`) about why an entry is not happening
read loosely out of the row. Three tables answer the three questions a screen asks: `grid_upcoming`,
`grid_finished` and `grid_all`. Four things are asked *about* an entry: `create`, `delete`, `cancel` - which is the
reference's "Stop current recording", because it ends the writing and leaves the entry standing - and `autorenew`,
which is the only series this backend knows.

Two of the reference's answers have no equivalent here and are recorded rather than invented. Its eight recording
types (`Once`, `Daily`, `Weekly`, `EveryTimeOnEveryChannel`, ...) collapse into the backend's one switch
`autorenew=0|1`, so "Record series" means "keep renewing this event whenever the broadcaster lists it again" and
"Delete full series schedule" means the same switch turned off; the reference's own picker "Choose recording
schedule type" therefore has nothing to choose between and is not ported. `CreateScheduleByTimeAsync` - the manual
schedule, with its padding, priority, directory and its own screen of a date and a time - is not ported either: it
is answered here only against a channel's own uuid, which this client never reads (it keys everything off the EPG
channel), and a television has no date editor to ask the question with. A recording is therefore planned from a
programme, which is how the reference's dialog asked for it too. What a refusal looks like is also settled here: a
box that says `success:false` answers an ordinary `TvBackendAnswer` with the backend's own sentences, because
"already being recorded" is an answer and not a fault, while a box that does not answer at all is still the one
kind of exception the screens already catch.

`TestTvRecordings` asks 15 questions of a scripted box: which table each list is asked of and that a screen asks
one table and not both, that a row without a uuid is no recording, that a day-and-hour row is still placed on a
timeline, that `1` and `true` are both yes, which states are waiting, writing, done, failed or called off, that a
plan is asked for by `event_id` rather than by a re-sent time, that a series carries `autorenew=1` with
`dupdetect=0` and that switching a series on says `all=1`, that both spellings of a refusal arrive as sentences,
and that an unreachable box is still exactly one kind of fault.

### bo. Schedules and recordings: the list of what the television was asked to keep, and the presses beside it

The client could ask; now there is somewhere to ask it from. `TvSchedulesViewModel` and `TvSchedulesView` are the
reference's schedules screen (`TV/SlimTvClient/Skin/Nereus/screens/SlimTvClient-schedulelist.xaml`) carried over:
the entries down the left as rows of fifty pixels with no header above them - channel and day, name and what the
backend is doing about it, the two ends of the time, and one word saying whether a single programme or a whole
series stands behind the row - and the words about whichever one is stood on in a dark band at the right, with the
series switch above them. The switch is the reference's `ScheduleSeriesMode` checkbox doing what it did there, where
the same press meant one question with the switch off and another with it on; here the button says which of the two
it is ("Delete" or "Delete series schedule"), because a switch a viewer can see but cannot feel should not be
allowed to change what a press means silently.

Four presses, and each is a question the backend really answers: stop the writing (`cancel`, which is the
reference's "Stop current recording" - the recording ends and the entry stays readable), take an entry away
(`delete`), let go of a series or take it up (`autorenew`), and read the table again. Every one of them ends with the
table being read back, so that the screen never keeps its own idea of what is planned; and with the series switch on
a delete lets go of the series *first*, because a backend still renewing an event answers a delete by putting the
entry back. What the box said is what the line at the bottom says, in the box's own words - "Ein identischer Timer
existiert bereits" and not "that did not work" - and a refusal, unlike a fault, does not stop the read back.

Two things the reference had beside this list are missing on purpose and are named in part bn's entry rather than
here again: its "New schedule" screen, and its way of *playing* a finished recording, which was the media library's
job there and has no equivalent for a television here. The home menu has advertised "Schedules" and "Recordings" for
many parts now and always answered them with "not available in this build"; both tiles lead somewhere now - the same
screen turned to the table each of them means - and so do "Live TV" and "Live radio", which had been pointed at the
guide because there was nothing else to point them at. The tiles whose screens do not exist yet, the programme
search among them, keep the sentence that says so, which is why the screens a shortcut may open are still named in
the home screen rather than asked of the application: the application answers a screen nobody builds with an
exception, and that is right - a tile naming a screen is a bug in the front end, while a viewer asking for a
programme search is an ordinary question.

`TestTvSchedulesScreen` asks 18 questions: that the two tiles open on two different tables and call themselves
different things, that the earliest beginning stands at the top, which states are named and which are passed
through as the backend named them, where the backend's own complaint about a switched-off plan appears, that a
screen with no written-down backend asks nobody nothing and says so out loud, and - the ones this screen exists for
- that each press asks exactly the questions it should, in the right order, with the right switches in the query.
Two of them look at the drawn screen, and one renders it: the plans and the recordings as they come out of a
scripted box, list and band and all.

### bp. "Choose program action...": the question a programme in the guide can be asked

Pressing twice on a programme used to do one of two things depending on whether it happened to be running. The
reference did something else: it asked. `SlimTvGuideModelBase.ShowProgramActions` put up a dialog whose lines were
worked out from the programme, from the clock, and from a question to the backend, and that dialog is here now -
`TvProgramActionsView` over the guide's own model, wearing the skin's `DialogHeadedFrameTemplate` like every other
dialog in this front end, pushed onto the trail the guide itself stands on so that the grid stays lit behind it and
Back leaves the viewer on the block they pressed. The guide is therefore the first screen of the television pair that
needs the workflow handed to it, and `App` builds the dialog for a `programactions` frame out of the same model,
because a second copy of the model could only disagree with the first about which programme was pressed.

What stands on the list is the reference's, in its order and in its words: a finished programme says so and is still
offered its series (its comment read "if program is over already, there is nothing to do", and it went on adding
recording options under that sentence, because a series is about the airing still to come); a running programme
offers its channel first; and then the four-way choice - a writing to stop, a running programme to join, a plan to
take away, a future programme to ask for - of which exactly one line appears, with "Record series" under it. Behind a
renewing entry the choice is different in kind: "Delete single program" and "Delete full series schedule", and never a
plain "Delete schedule", because a backend that is still renewing an event answers a delete by writing the entry
straight back. That difference is why the dialog asks a question at all: an EPG event says a recording is attached to
a programme (`dvrUuid`, `dvrState`) but never whether the plan renews, so `GetRecordingAsync(uuid)` asks about the one
entry - which needed uuid-taking `delete`, `cancel` and `autorenew` on the client beside the row-taking ones, since a
programme in a guide has never seen a table. A programme with no plan attached is the reference's own `None` answer
and is asked about nothing; a plan whose entry cannot be read leaves the list as short as the reference's was when its
status question failed, because offering "Delete schedule" on a guess throws away a household's plan.

Getting to the channel is the answer that is not asked for: `ZapFromGuide`, on unless a viewer turns it off as the
reference had it, still sends a second press on a running programme straight to that channel. With the switch off -
and with any second press on a programme that is not running - the dialog is put up instead, and "Watch program now"
is its first line rather than a shortcut. This part also fixes what the model used to do for a programme nobody had
planned: it stopped after the channel, so the ordinary case of the whole dialog - somebody standing on something they
want recorded - had no "Schedule recording" on it at all.

Three answers are missing and are missing on purpose. "Watch recording from beginning" belongs to a player that can
start a file at the beginning, and this front end reaches a television through a channel, so offering it would be a
press that does nothing. "Choose recording schedule type" has nothing to choose between, since a TVHeadend keeps one
kind of series (part bn). And the list of answers other plugins appended at the end was filled through
`SlimTvExtensionBuilder`, which this front end has no equivalent of - there is nothing that could register an answer
there.

`TestTvProgramActions` asks 25 questions. Nine of them are the list itself, one per situation: over, running and
unplanned, running and being written, ahead and planned once, ahead and unplanned, ahead behind a series, running
behind a series, the entry being read by the uuid the programme carried, and a backend that will not say what the
entry is. Six are what a press sends: `cancel` rather than `delete` for a writing, `autorenew` before `delete` for a
series, `create?event_id=` with `autorenew=1` for a series of something unplanned and with `autorenew=0` for a single
programme, `autorenew?active=1&all=1` for a series of something already planned, the backend's own sentence on a
refusal, and the grid being read back after an answer. Four are how the dialog is reached - one press selects, a
second press zaps while the viewer wants that, asks when they would rather be asked, and asks whatever the programme
is doing when it is not running. The last two look at the drawn dialog: one line per answer with the programme named
on the dialog itself, and the sentence about a finished programme drawn but unable to be stood on. 995 of 997 tests
pass, the two failures being the two the front end already carried in.

### bq. The television settings the reference kept, and the screens that now obey them

`SlimTvClientSettings` held sixteen settings, thirteen of them about the guide, the zapping and what a channel row
says about itself. Part bk ported the address and the days of guide; this part ports the eleven that are about
television as a thing you watch rather than a thing you connect to, and puts each of them where the reference read it.

The file grew `visible_hours`, `visible_rows`, `zap_timeout_seconds`, `show_channel_names`, `show_channel_numbers`,
`show_genre_colors`, `show_series_info`, `hide_all_channels_group`, `zap_from_guide`, `zap_by_channel_index` and
`auto_start_tv`. The settings screen grew three boxes and eight switches beside the address, worded as the reference
worded them - "Zap from the guide", "Zap by channel index", `Hide "All Channels" group`, "Automatically start TV" - in
the order its own `plugin.xml:218-300` asked them, because that order was thought about: the switches first, then the
two numbers, then the zapping.

Where each answer is obeyed is the part that matters, and it is the part a settings file on its own cannot show:

- **The hours** divide the grid's columns, exactly as the reference divided its `EpgVisibleHours * 60` by its own
  seventy-five columns (`Controls/EpgGrid.cs:96-99`). This guide has ninety columns, so three hours is two minutes a
  column and six hours is four, and every programme's column, every hour label and the width of the window come out of
  the one number. Before this part the ninety and the two were both written into the code.
- **The rows** divide the room the grid has been given, which is the reference's own sum: `ActualHeight /
  _numberOfRows` (`EpgGrid.cs:428`). Asking for three rows is asking for three tall rows, not for three rows at the
  top of a screen. The width of a channel row's header is the reference's other sum on the same screen,
  `(numbers ? 90 : 0) + (logos ? 160 : 0) + (names ? 200 : 0)`
  (`Skin/Nereus/screens/SlimTvClient-fullguide.xaml:22-30`), less the hundred and sixty it allowed for a picture this
  client has never learned to fetch.
- **The episode line** goes away when `show_series_info` is off, on the block and in the strip along the bottom, which
  is the reference's `ShowSeriesInfo` at `ProgramListItem.cs:112`.
- **The genre colours** go to one colour when `show_genre_colors` is off. The reference filled everything with its
  skin's plain blue in that case; here the flat fill is a blue of the skin's own (`TvGenreColors.FLAT`) rather than a
  ninth genre.
- **The bouquet** named "All Channels" is dropped by name and nothing else, as `Helpers/ChannelContext.cs:218-231`
  dropped it - from the guide and from the watching screen alike, through one rule on the settings object so the two
  cannot drift apart.
- **The typed number** means a place in the bouquet while `zap_by_channel_index` is on, which the reference implemented
  by renumbering the bouquet one to n before the lookup (`ChannelContext.cs:190-194`), and the number printed on the
  channel when it is off, which is the other branch of `ChannelZapModel.cs:173-181`. The two are now both real:
  before this part only the first existed.
- **The timeout** is read from the file each time somebody types a digit, so a viewer who changes it gets it the next
  time rather than the one that was current when the screen opened. `TvTunerViewModel.ZAP_TIMEOUT_SECONDS` stayed, but
  shrank to what it is: the two seconds the reference defaulted to, which is what answers when the file cannot be read.
- **`zap_from_guide`** is now the settings' answer rather than something a screen could overwrite, so the guide's
  second press asks or zaps according to what was written down.
- **`auto_start_tv`** tunes the channel a watching screen is standing on when it opens with nothing on it, which is
  what `SlimTvClientModel.cs:553-565` did on entering its television home.

Three things the reference asked about are deliberately not asked here. `ChannelLogoStyle` and `ChannelLogoAutoUpdate`
are two of them: the reference kept a picture collection of its own, downloaded a theme of it from a logo site
(`SlimTvLogoSettings.cs`: `RepositoryUrl = http://logomanager.team-mediaportal.com/`, `LogoTheme = "Flat-default"`,
`EnableAutoUpdate`, `UpdateJobId`) and matched a channel to a picture by name. A TVHeadend answers every channel with
the address of the icon it already has, so a second source of pictures would be a second machine to trust, to keep up
to date, and to be connected to while watching television, for an answer the backend already gives. What is left
behind is a theme chooser and an update job; `EpgShowChannelLogos` itself is asked, since part bu. The third is
`EpgVisibleChannelInfo`, which the reference asked as one multiple-choice list of name, number and logo; a remote has
no way of holding a list down, so the three that exist here are three switches. The per-genre colour pickers stay in
the skin, which is where the eight colours already are.

Three answers differ from the reference on purpose, and each is a known difference rather than a drift.
`EpgVisibleHours` defaults to three here and two there - the reference measured seventy-five columns and this guide
measures ninety, and three hours is what this grid drew before the number could be changed; the switch can turn it
back. `ShowEpgGenreColors` defaults on here and off there, because the reference's skin drew a colour strip of its own
beside every block while here the block's fill is the only place a genre is written down. And a number out of range is
brought back inside - half an hour to twelve for the window, one row to forty, half a second to thirty for the timeout
- where the reference clamped nothing at all and a `visible_hours` of 99 would have drawn a guide whose hour labels sat
on top of each other.

One difference is worth naming because it is the largest. The reference built exactly `_numberOfRows` rows and *paged*
the channel list: its grid held nine rows, and PageUp and PageDown moved the offset by nine (`EpgGrid.cs:505`, `905-915`).
Here every channel of the bouquet is in the grid and the grid scrolls, with the rows sized by the same division. The
setting therefore changes how big a channel is here and how many channels fit on one screen there.

19 new tests. Six of them are the file again: every answer surviving a write and a read, the defaults being the
reference's, a hand-written file with 99 hours and nought rows in it being corrected rather than refused, the bouquet
being recognised by its name in either language and in no matter what capitalisation, and the settings screen carrying
the numbers back out of the file and putting the corrected ones back in. Thirteen are the screens obeying them: a guide
six hours wide putting a film in column forty-five and spanning fifteen, the same guide dropping the episode line and
painting everything flat when told to, dropping a bouquet and still opening on one it has, picking up an answer that
changed while it stood on the screen, reading a typed 2 as the second channel or a typed 12 as the printed one, waiting
four and a half seconds because somebody wrote four and a half, starting a picture only when it was told to and never
overruling a channel somebody arrived with, and - drawn on a screen - a header that is only as wide as what it is
allowed to show and rows that are as tall as the room they have divided by the rows that were asked for. 1013 of 1016
tests passed at the end of this part: two are the failures the front end already carried in, and the third is a
music-shuffle test that failed in the middle of a three-minute run and passed when asked again on its own.

### br. A television that is not there: the demo backend under the client

An empty guide is the right answer to "where does the television come from" when nobody wrote an address down, and it
is also the answer that shows a viewer nothing about what the screen would do if there were something to show. So the
front end can now invent a television: one switch on the television settings, `demo_data` in `tv.json`, and every
television screen is filled with channels, programmes and recording timers that exist nowhere.

It sits *under* the API client and not beside it. `DemoTvBackend` is a message handler that answers TVHeadend's own
JSON at TVHeadend's own addresses - `api/channeltag/list`, `api/channel/grid`, `api/service/grid`,
`api/epg/content_type/list`, `api/epg/events` and the `api/dvr/entry/…` tables and their four write endpoints - and
`DemoTvBackend.Open(settings)` is the one place that decides whether the client is pointed at that handler or at a
machine. Which means a demo guide is built by the same query building, the same paging, the same reading of a
hexadecimal content type and the same screens as a real one, and it is also why the demo is worth having as a fixture:
a guide that draws wrongly in demo mode draws wrongly in the client, not in the box. The four screens hand the client
over through their own factories, so all four now default to that one decision.

Sixteen channels, four bouquets, twelve to watch and four to listen to, and no real broadcaster's name anywhere - a
demo carrying somebody's programme titles would be quoting a programme schedule at a screen that is only there to be
looked at. The programmes are generated out of the clock rather than stored: every channel has a day of slots summing
to forty-eight half hours, the day repeats for ever, and a question walks the window it was asked for - starting eight
half hours early, half hour by half hour rather than leaping programme to programme, which is exactly how a programme
that began two hours ago and is still running gets stepped over. So the demo is full at three in the afternoon in
October and at four in the morning in March, the window, bouquet, channel and title questions are all honoured, and a
programme's id is reversible - channel and starting half hour - which is what lets `api/dvr/entry/create` be asked
with nothing but an event id and still make a timer for the right evening. A guide painted from it uses all eight of
the skin's genre colours, and the bouquets include one called "Alle Kanäle", so the setting that hides that bouquet
can be seen doing it.

Recording timers are the one thing the demo remembers, because a viewer who asks for something to be recorded has to
see it on the timers screen afterwards. They live in memory for the length of a run and start seeded with two finished
recordings, one running right now and one still to come, all four built out of the same generator as the programmes so
their titles are the titles the guide shows at those times. `ResetRecordings()` puts them back.

What it deliberately does not do: a **picture**. A demo channel carries a stream address like every other channel, and
pointing a player at it finds nothing, because this front end is not going to ship a video file or point a box at
somebody else's server to make a demo look finished; while demo data is on, that address is
`http://tvbackend.invalid/…`, a name RFC 2606 reserves so it cannot belong to a real machine. Live TV in demo mode
therefore stops with the player's own complaint, and the settings screen says so under the switch. Two more things
follow from the switch and are worth knowing rather than discovering: while it is on, an address that is written down
is *not* asked (the screens cannot talk to two televisions at once), and every status line that counts programmes adds
"demo data, invented by this front end", because made-up channel names pass for real ones at a glance.

The switch has no counterpart in the reference, which could only ever show a television that existed. `IsConfigured`
therefore counts demo data as an answer, so not one screen needed to learn that a demo exists.

22 new tests, and every one of them asks the demo through `TvHeadendApiClient` rather than at its innards, because
that is the claim: bouquets and channels arriving in the backend's own shapes with the radio half recognised through
the service bits, a window being full whichever hour it is asked for and nothing outside it answered, a programme
beginning before the window still drawn at its left edge, a channel question never answering another channel, a title
search answering its title, no programme answered twice in a day (which is what paging would show), all eight genre
colours present, an episode carrying its episode line and a film not carrying one, a recording asked from the guide
appearing on the timers and coming back marked in the guide, the same programme refused when asked for twice, a timer
being renewed with the backend's own `active` spelling and called off afterwards, the guide filling and saying it is
invented, a radio screen offering the four radio channels, the timers screen having something to draw, and the switch
surviving the file and the settings screen's own "ask it" button reporting 12 television and 4 radio channels out of
nobody. 187 television tests pass; the whole front end suite was run again after this part.

### bs. The answer is applied where the screen lives: the television screens stop dying on their own answers

The demo backend did what it was built to do and, in doing it, found a bug that no test could see: the guide drew
its evening as a narrow band pressed against the channel headers, said nothing for the rest of the window, and put
"the calling thread cannot access this object because a different thread owns it" where it normally counts
programmes. Pressing Live TV or Live Radio was worse - it closed the front end.

Both are one bug, and the demo found it by being slow in the way a real box is slow. Every television screen asks a
machine in another room a question. `TvHeadendApiClient` awaits with `ConfigureAwait(false)`, so the code waiting
for the answer wakes up on a thread of the .NET thread pool - and the television models then went on to fill the
collections and say the sentences that a screen is bound to. Avalonia keeps those two threads apart: the rows
belong to the thread that made the grid, and touching them from anywhere else throws. Where a model catches
everything the throw became a status line and the rebuild was left half done, which is the collapsed evening; the
tuner catches only the backend's own complaints, so the throw walked out of a fire-and-forget reload and ended the
process.

This was invisible to the suite because every scripted backend in it answers *while the question is still being
asked*: a task that is already finished runs its waiter on the thread that asked, which in a screen test is the
thread the screen lives on. The whole suite proved the one case that cannot go wrong. `TestTvScreenThreading`
answers 40 ms later, from a timer, which is the case a television in another room always is.

**The fix follows the pattern the front end already had.** `NowPlayingViewModel` has taken an `Action<Action>`
"way onto the screen's thread" since it was written, and the application hands it
`action => Dispatcher.UIThread.Post(action)`. The four television models now take the same parameter, and
`Views/ScreenThread.cs` is the one implementation of it the screens hand over: if this is already the thread the
screen lives on, do the work at once, else post it. A model built without one - which is every test of a model on
its own - applies its answer where it arrives, exactly as before, so not one existing test had to learn about
dispatchers and none of them got slower. `ConfigureAwait(false)` stayed where it is: an answer from the network
should not be held up waiting for a screen.

Two decisions in it are worth keeping:

- **What is deferred and what is not.** The screen's own state goes through the handover - the rows, the hours, the
  bouquets, the entries, the status lines, `IsLoading`, `Checking`, and the events that open and close the programme
  dialog. What the *next question* depends on is decided immediately: which bouquet a guide or a tuner opens on is
  now worked out from what the backend answered rather than from the screen's list of them, and the picked bouquet is
  assigned on the spot, because waiting for the screen to have been told first would have the channels asked for
  while the model still believes none is picked. That was the first test to fail, and it failed in the right direction.
- **Nothing reaches for `Dispatcher.UIThread` inside a model.** The models stay free of Avalonia; only a view or the
  application, which is standing on the screen's thread already, can name it. `ScreenThread.Run` runs work inline
  when it is already there, so a screen is never left showing a question it has answered.

The settings screen went the same way, where the bug was smaller but the same: what its "ask it" press found, and
the button's own state afterwards, were both being said from the network's thread.

6 new tests, and each of them says which of the two cases it is about: the guide, the tuner and the timers screen
are stood on a real window, reloaded against a backend that answers from elsewhere, and then asked what they now
say - the guide's grid full and its programmes standing in more than one column (the collapse is asserted against
by name), the tuner's channels present and its reload throwing nothing at all, the timers counting their rows
instead of reporting itself. The other three pin the arrangement itself: a guide handed a way onto the screen's
thread that has not been walked yet has touched nothing, a model on its own still applies its answer where it
arrives, and the settings sentence about a backend arrives through the handover and not before it. 193 television
tests pass; the whole front end suite was run again after this part and still carries only its two known
baseline failures.

### bt. The grid keeps a page of channels and the remote walks the rest: the guide became usable from a sofa

Two things were wrong with the guide at once, and they hid each other. It drew every channel of a bouquet as a row,
so a household with three hundred channels was carrying about twenty-seven thousand controls on a screen with room
for eight rows, and it answered no key at all - the programme blocks are `Border`s with a `PointerPressed` handler,
and `DpadNavigation` only rings the buttons along the top and the bouquet picker - so on a television it was a
screen that could only be used with a mouse. A guide nobody can reach with the remote is not finished, and a guide
that builds twenty-seven thousand controls is not fast either, so both went the same way they go in the reference:
the grid holds a page, and the remote walks the bouquet.

The reference's `EpgGrid` is the authority for every number here (`Source/UI/TV/SlimTvClient/Controls/EpgGrid.cs`).
It kept `_channelViewOffset` beside `_numberOfRows = settings.EpgNumberOfRows` and drew only the rows between them
(`:494-506`); it opened with the current channel brought into view at the edge it came from (`:461-476`);
`IsViewPortAtTop`/`IsViewPortAtBottom` answered `offset == 0` and `offset >= Count - N`; PageUp and PageDown were
`MoveUp`/`MoveDown(_numberOfRows)` (`:880-915`), Home and End were offset `0` and `max(Count - N, 0)`; a channel
that walked off the page slid it to the edge the channel came in from rather than centring it
(`SkipToChannelIndex`, `:998-1032`); Left and Right walked the programmes of a row and, when a row ran out, moved
the window half an hour (`:965-986`); and the focus was put back by the *time* it had been standing at
(`_focusTime`, `:1216-1240`) rather than at the beginning of whatever row it landed on.

In the model, `_allRows` now stands behind the `Rows` the screen is bound to, and `Rows` is the page: `BuildRows`
builds the whole bouquet, works out which channel the viewer was standing on, opens the page on it and copies only
that page across. Around it is the walking - `RowCount`, `ViewOffset`, `CanScrollRowsUp`/`CanScrollRowsDown`,
`CanMoveRowUp`/`CanMoveRowDown`, `ScrollRowsUp`/`ScrollRowsDown`, `PageRowsUp`/`PageRowsDown`, `FirstRow`,
`LastRow`, `MoveRowUp`/`MoveRowDown` and `MoveProgramForward`/`MoveProgramBackward`. The page height is the
`visible_rows` setting, which was already carried over as the reference's `EpgNumberOfRows` and had nothing to
count it until now. The line along the bottom counts the bouquet and says what the screen shows of it - "16
channels, 214 programmes between 20:00 and 23:00 - 9 of them on the screen, the rest walked with the arrows" -
because a guide that says "9 channels" about a bouquet of sixteen is lying about somebody's television.

The view answers the keys on the grid itself, as a *tunnel* handler registered before `DpadNavigation.Attach`, so
that the grid keeps its arrows while it is stood on and nothing else gets a chance to scroll them away. Up and
Down walk channels, Left and Right walk programmes, PageUp/PageDown go a screenful, Home/End go to the ends of the
bouquet, and Enter is the same press a finger on the box is. Just as important is the case where the handler says
nothing: when the model answers that it cannot move - Up at the first channel, End at the last - the key is left
alone, the ring of buttons above the grid takes it, and that is how a viewer walking up out of a guide gets out of
it instead of bouncing off the top row.

Three decisions are worth keeping:

- **The page slides at the edge instead of centring itself**, because a viewer walking down with the arrow is
  watching the bottom of the screen, and a page that jumped to put the cursor in the middle loses the end of the
  list they were walking towards. The last page is clamped to `RowCount - N`, so the bottom of a bouquet is a full
  screen: the price is that a bouquet which does not divide evenly by the page shows a couple of channels twice,
  which is what the reference paid too.
- **Changing channels keeps the moment**, not the row's first programme: walking down to another channel stands on
  the programme *covering the time the viewer was looking at*, which is what "what is on over there at half past
  eight" means. A row with nothing at that moment takes the next thing it has said.
- **A press with the pointer moves the cursor the remote walks.** Pressing a box used to select the programme
  without moving the row cursor, so the next arrow answered from whatever row the guide had opened on rather than
  from the one that had just been touched. Pointer and remote are one cursor, not two.

7 new tests. Against the scripted backend with a screen one channel tall: a page is a screenful and not a row, the
walk clamps at both ends, Home returns to the top; walking to the channel above lands on `Tatort`, which covers the
time `Kaminkonzert` began at, and walking back lands where the viewer came from; walking past the bottom slides the
page so the cursor is still on the screen, and the first channel says it cannot go further up; walking off the end
of a row moves the window thirty minutes on and asks the backend again, while the left-hand end of a row walks the
window back. Against the demo backend - the only fixture with more channels than fit, sixteen of them on a screen of
three - the whole bouquet is walked page by page and the offsets come out `0, 3, 6, 9, 12, 13`: every channel
appears, none is skipped, the only ones shown twice are the last page sliding back to a full screen, and End and
Home stop at the ends. One headless test sends the keys themselves to the grid control and asserts both halves of
the arrangement: the arrows the guide has use for are taken and reach the model, and a key it has none for is left
on its way. The guide's test helpers can now be asked for a screen height, since what a grid does with a bouquet
taller than the screen is half of what a grid is for.

### bu. The picture a channel is known by: what `icon_public_url` is, and where the header makes room for it

Reference: `Settings/SlimTvClientSettings.cs:56-59` (`EpgShowChannelLogos`, on by default),
`Models/SlimTvSettingsModel.cs:99-108` and `:384` (the same answer handed to the skin as `ShowChannelLogos`),
`Skin/Nereus/screens/SlimTvClient-fullguide.xaml:22-31` (the header's width as a sum of the three switches),
`Skin/Nereus/themes/default/styles/SlimTvStyles.xaml:1432-1478` (the picture, the name beside it and the name that
stands in for a picture that is missing), `Settings/Configuration/EpgVisibleChannelInfoSetting.cs:47,57`,
`Settings/Configuration/ChannelLogoStyle.cs`, `Settings/Configuration/ChannelLogoAutoUpdate.cs` and
`SlimTvInterfaces/Settings/SlimTvLogoSettings.cs`; on TVHeadend's side, `src/api/imagecache.c` and the public-location
building in `src/api/channels.c`.

Asked, in the second pass over the television settings, whether the guide should show the picture a channel is known
by. Four things had to be built before that question could be asked honestly, because until then there was no picture
anywhere in `Api/`:

- **`TvChannel.LogoPath`** is the address of the icon, and it is read only from `icon_public_url`. TVHeadend answers
  channels with two fields: `icon` is the internal id of the icon, which is only useful together with the id of the
  service it belongs to - TVHeadend builds the public location by looking that service up - and `icon_public_url` is
  the finished location. Reading the wrong one fails silently: a request for a picture under an id nobody knows
  answers with nothing.
- **`TvHeadendApiClient.GetChannelLogoAsync`** answers the bytes, or `null` for "this channel has no picture". A
  channel without an address asks nobody; a 404, a 401, a 403, an answer that is not a picture at all and an answer
  bigger than a megabyte are all "no picture" and are remembered as such. A machine that cannot be reached is *not*
  an answer - it throws, so a power cut is never remembered as "this station has no picture".
- **`Caching/ChannelLogos`** is the front end's one façade over the existing `Artwork`/`PictureCache`, under its own
  shelf kind so that a channel icon is never mistaken for a film's poster and never thrown away when a video opens.
  Like `NewsPictures` it asks `TvBackendSettings` for the address afresh every time and rebuilds its client when the
  address changes, so an image cache on another machine is not a reason to restart.
- **The header's column.** The reference's guide column was a sum of the three switches
  (`SlimTvClient-fullguide.xaml:24`: `(ShowChannelNumbers ? 90 : 0) + (ShowChannelLogos ? 160 : 0) +
  (ShowChannelNames ? 200 : 0)`), and `TvGuideView.axaml.cs` kept those three numbers exactly. Where it differs is
  *when* the picture's part of the sum is paid for: the reference asked the setting, this screen asks the page, so a
  guide whose stations name no icon gives the room to programmes instead of drawing a hundred and sixty pixels of
  nothing beside every row.

Where a picture is fetched, what it does, and what it does not do:

- The guide asks for a page's pictures when it fills the page, and only after it has peeked in the shelf - a picture
  already held is placed with the page, not a moment later. `PictureCache` already keeps four loads moving at once
  and remembers the last two hundred pictures, so nothing new was needed for "not one request per row"; a station
  shared by several bouquets names the same address and is asked for once.
- A picture arrives into a row that is already drawn: `LogoReady` names the channel and the refills that row's cell,
  exactly as the view already handles lanes arriving late.
- **A row whose picture is not there says its name where the picture would stand**, and keeps the place beside it
  empty - the reference's arrangement down to the two labels: an image, a `ChannelLogoFallbackLabel` in the same
  column, and a name whose own visibility asks the image whether it has anything to show
  (`SlimTvStyles.xaml:1432-1478`). The reference let the standing-in name have the picture's own width when a name
  was asked for as well, and the whole column when it was not; so does this screen.
- **The picture is drawn at its own size.** The reference gave its image a box of a hundred and forty pixels inside
  the column and `Stretch="Uniform"`, which scales a picture *up* to fill the box; here the picture is decoded at
  twice the column's width and drawn one to one, so a provider's larger icon keeps its crispness while a small icon
  is not blown up into a blur. TVHeadend's own image cache hands out the icons a station has, and those are small.
- **The reference's pictures were not the backend's pictures.** Its image asked the FanArt store for a *banner*
  named after the channel (`SlimTvStyles.xaml:1434`: `FanArtType="Banner"`, `FanArtName="{Binding ChannelLogoPath}"`,
  with the type from `channel.GetFanArtMediaType()` at `Helpers/ProgramProperties.cs:298`), which is why the same
  settings section also carried a logo site, a theme and an update job (`SlimTvLogoSettings.cs`:
  `RepositoryUrl = http://logomanager.team-mediaportal.com/`, `LogoTheme`, `EnableAutoUpdate`). This client asks the
  TV machine for the icon it already keeps instead, which is one machine to trust rather than two and needs no
  matching of names - and it is why `ChannelLogoStyle` and `ChannelLogoAutoUpdate` were left behind, theme chooser
  and update job with them. `EpgShowChannelLogos` is the whole question here.
- The demo backend serves real pictures now - a 64 by 64 bitmap it builds itself per channel index, in eight colours -
  and gives every fourth channel no address at all, so both halves of the setting and the standing-in name can be
  seen without a TVHeadend. `HasLogo` is the guide's question, and its answer differs per station, which is what a
  fixture needs.

Tested by `TestTvChannelLogos`. A machine that is not there is proven to be no answer, three names for the same
address are proven to be one request, an HTML answer is proven to be "no picture" rather than a broken picture, the
header is proven to keep its hundred and sixty pixels only for a page that wants them, and a row whose station has
no picture is proven to say its name once - in the picture's place, and not twice.

### bv. A word when a recording starts or stops: the one watcher that has to run while the viewer is elsewhere

Reference: `Notifications/SlimTvNotificationService.cs:97-102`, `:105-118`, `:120-146` and `:155-166`,
`Models/SlimTvNotificationModel.cs:65-69`, `Notifications/SlimTvRecordingNotification.cs:61-95`,
`Skin/default/superlayers/RecordingStartedNotification.xaml`,
`Skin/default/screens/SlimTvClient-notification_recording.xaml:43-76`, `Language/strings_en.xml:131-137` and
`:172-173`, and `Settings/SlimTvClientSettings.cs:106-121`.

`ShowRecordingStartedNotifications`, `ShowRecordingEndedNotifications` and `RecordingNotificationDuration` were
promised in the settings and deferred in part bo because nothing watched the timers. This part is that watcher - the
only piece of the television layer that has to keep working while the viewer is nowhere near a television screen.

- **`ViewModels/TvRecordingWatcher`** asks the backend for its upcoming timers every thirty seconds and compares the
  ones the backend says are recording now with what it saw the last time; the reference compared the same two lists,
  by the timer's id (`:155-166`: whatever is in the new state and was not in the old one started, whatever is in the
  old one and not in the new one ended). The watcher lives in the front end's composition root, is started when the
  window exists and is disposed when the application exits; no screen owns it and no screen can stop it.
- **It is asked, rather than told - and that is the one structural difference.** The reference was pushed to: MP2's
  television client watched `ServerStateMessaging` for a new `TvServerState` and diffed the old state against the new
  one the moment one arrived (`:74-90`). Nothing behind TVHeadend's HTTP interface pushes to a client, so somebody
  has to ask, and the asking is what a `LOOK_SECONDS` of thirty stands for. Half a minute is a choice rather than a
  fifth question: no viewer notices the difference either way, and a question nobody can answer from a sofa is a bad
  question.
- **The first look never speaks.** It remembers what is already running and reports nothing, which is the reference's
  rule in its own words - its first act was to read the current state so as to *"avoid showing notifications for
  events that happened before the client was started"* (`:105-118`). A box that restarts should not claim to have
  just started six recordings. The marker for "something is being recorded" does light on the first look.
- **One sentence at a time, gone after a while.** `ShowNotification` handed the notification to a single model
  property and opened a super layer with the duration (`SlimTvNotificationModel.cs:65-69`), so a second recording's
  news replaced the first rather than queueing behind it. `RecordingNotificationDuration` is seconds there
  (`Settings/SlimTvClientSettings.cs:118-121`, five of them by default) and seconds here, and the reference's guard -
  nothing at all is shown when the duration is below one or both switches are off (`:126-128`) - is this watcher's
  guard too.
- **What the sentence says.** The reference's was a panel: a header taken from `SlimTvClient.RecordingStartedHeader`
  or `SlimTvClient.RecordingEndedHeader` - "Recording started" and "Recording ended"
  (`Language/strings_en.xml:172-173`) - over the programme name, the channel name and the timer's start and end, with
  the channel's own picture beside it (`SlimTvClient-notification_recording.xaml:43-76`). A bar along the top of every
  screen has room for one line, so this front end keeps the header and the two names that matter, "Recording started:
  {programme} on {channel}", and leaves the times out.
- **A machine that goes silent is not a pile of recordings that ended.** The reference never had to make the
  distinction, because a state that never arrived was never diffed; here a look can fail halfway, and a failed look
  leaves the remembered set alone rather than reporting everything on it as finished. A TVHeadend that is restarting
  has not stopped anybody's recording.
- The watcher re-reads its two switches and its duration *every* look, as the reference re-read them at every change
  (`:123-125`), so the settings page takes effect without anybody being told; and while notifications are switched off
  it keeps comparing, because otherwise switching them back on would report everything that had meanwhile started.
- The bar itself is the skin's: `chrome.xaml` of both Nereus and WMC gained a notice area, and the Nereus header
  comment claiming that nothing of the server's business belonged there was rewritten - the reference did carry
  exactly such a bar, `Nereus/screens/NotificationsBar.inc`, which said what the importer was doing at the library,
  said a sentence the moment a recording started or ended, and stood a mark up the whole time a recording ran
  (`:39`: a button visible while the television state reports `IsRecording`). This front end has one header above
  every screen rather than a bar inside each of them, so all three are said there instead.
- **`ChromeInfo` owns no timers**: the watcher holds the one that withdraws the sentence and merely reports an empty
  one, so a test can be told "a look found a start" and "the sentence was withdrawn" without waiting for anything.

Tested by `TestTvRecordingNotices`, which hands the watcher its own "wake": the look of a test runs when the test says
so, so nothing in the fixture waits thirty seconds for a timer to fire. It pins the silence of the first look, the
silence of both switches and of a duration below one, the marker's independence from all three, the wording, the
withdrawal, and the fact that a backend which merely went silent is not reported as a pile of finished recordings.

### bw. The evening that was drawn a ninetieth of its width

Reported by the user with a picture: the guide's hour ruler was right, the channel names were right, and every
programme of the evening was standing in a narrow band immediately right of the channel's name - which also read as
"there is nothing after the present time", because an evening three hours wide had been squeezed into forty pixels.
Nothing about the model was wrong.

- **The row is one grid**: a fixed header column as wide as the header needs, then `TvGuideViewModel.COLUMNS` (ninety)
  star columns for the evening. The channel header sits in column nought and the programme track in column one, and a
  grid child that is *placed* in a column without being told to *span* occupies that one column. The track therefore
  got a ninetieth of the row, and the ninety columns of its own inner grid - which is where the blocks are placed, by
  the attached properties the code-behind sets - were ninety divisions of that band. The ruler above is a separate
  grid, which is why it stayed honest and why the picture looked like a time bug rather than a layout bug.
- **One line fixes it**: `Grid.SetColumnSpan(track, TvGuideViewModel.COLUMNS)` where the row is built
  (`Views/TvGuideView.axaml.cs`, `BuildRow`).
- **Why eighty-odd guide tests let it through**: they asserted the attached properties on the boxes, and those said
  the truth - column 40 of 90, width 3 columns - inside a container that was itself the wrong width. The new test
  looks at `Grid.GetColumnSpan` of the *track* and at the rendered `Bounds.Width` of the widest block of a page, so a
  grid that is placed correctly and drawn narrow cannot pass again.
- **The reference could not have this bug**, which is worth writing down rather than quietly copying: its guide was
  one control owning one table (`Skin/Nereus/plugins/SlimTvClient/Controls/EpgGrid.cs` - header widths at `:101-102`,
  the column arithmetic at `:429`), so a row's programme area was by construction never narrower than the hours above
  it. Splitting the row into a header and a track is this front end's own doing - it is what lets the header carry a
  number, a picture and a name in one place (see `bu.`) - and the split is exactly what introduced a span to forget.

Tested by `TestTvGuideScreen.TheEveningOfARowIsDrawnAcrossAllNinetyColumnsOfItsTrack`, and confirmed on the user's own
screen: the guide is now full, evening included.

### bx. A settings screen that contradicted the machine

Reported by the user: switching the demo data on worked - the guide, the channel lists and the timers filled with
invented channels - but opening the television settings again showed the switch off. The file on disk was right the
whole time (`demo_data: true` in `%APPDATA%\MediaPortal\tv.json`), so this was never about saving.

- **The screen read its answers after binding its controls.** `Views/TvSettingsView.axaml.cs` set the `DataContext`,
  called `InitializeComponent()` - which is where every `IsChecked` and `Text` binding is made against whatever the
  model held then, which was the defaults - and only afterwards read the file into the model's fields.
- **And the model's "everything changed" notice was a hand-written list.** `TvSettingsViewModel` ended its read with
  a loop over five names: `Host`, `Port`, `Username`, `Password`, `GuideDays`, `Status`, `CanCheck`. Every one of the
  twelve switches and three of the four number boxes was missing, so not one of them was ever told to look again.
  The demo switch is merely the one a viewer cannot miss.
- **Both halves are fixed.** The notice now counts the answers out of the class itself (every public instance
  property), because a list of names can only ever be as complete as the person who last added an answer remembered,
  and this class has gained answers three times in as many parts; and the screen reads the file *before* building its
  controls, so a box is built with the answer it will show instead of opening empty and correcting itself a moment
  later.
- **Why only this screen had it**: the other settings screens are handed their model *after* construction -
  `new MetadataSettingsView { DataContext = metadata }` in `App.axaml.cs:1150-1172` - and a `DataContext` changing
  makes every binding read its source again. Setting the `DataContext` first and filling the model afterwards is what
  loses the answers, and this was the only view doing it.
- **The file itself was tidied by the same look.** `tv.json` had been written with the computed answers in it too -
  `VisibleHoursOr`, `ZapTimeoutSecondsOr`, `IsConfigured`, `BaseUrl` - because `System.Text.Json` writes every
  readable property, and a hand-editable file full of lines that nothing reads is a file somebody edits in the
  belief that they changed something. They are `[JsonIgnore]`d now, as the other settings classes already did; the
  answers the screens actually read (`demo_data` included) had always been written correctly, which is why the file
  was never where this bug lived.
- Nothing here corresponds in the reference: its television settings were edited in the TV service's own configuration
  window on the server's desktop, not in a skin screen, which is why `bv.`'s and `bu.`'s questions had no citation for
  the screens themselves either.

Tested by `TestTvSettingsScreen`, which asks the controls rather than the model - the model had been right all along,
and the old tests asked the model. It checks all twelve switches and seven boxes against what was written down, checks
that reading the file again while the screen stands there moves every one of them, and checks that a notice from the
model does not push an answer back out of a box somebody is typing in. Reverting both halves brings the original
complaint back verbatim out of the first test: *"the switch named demoCheck shows False although True was written
down"*. `TestTvBackend.TheFileHoldsTheAnswersAndNotTheThingsWorkedOutOfThem` pins the file's contents.

### by. The notices in the header's line, and what the server says about itself

Reference: `screens/NotificationsBar.inc:15-16`, `:19-33` and `:35-39`, `screens/master_home.xaml:184` and `:189`,
`themes/default/styles/Buttons.xaml:1717-1739` (the recording glyph, blinking) and `:1740` (the importing glyph),
`themes/default/styles/OtherControlsOverrides.xaml:2089` (the hint), `screens/WatchSystemNotifications.xaml`,
`Models/NotificationsModel.cs:104-135`, `General/Consts.cs:117-120`, and
`Source/Core/MediaPortal.UI/Presentation/UiNotifications/INotificationService.cs`.

The complaint was that a sentence about the server stood to the right of the clock, date and weather. In the reference
no notice ever stood there: `master_home.xaml:189` puts the bar in the top line *before* the clock, and
`NotificationsBar.inc` is a horizontal stack of the hint, the importing glyph with its percentage, and the recording
glyph. Nothing in it is a word of sentence: the hint is an eighty square picture that gains a `(2)` under it as soon as
a second notice joins (`Buttons.xaml:1983-2029` with `NotificationsModel.cs:109-112`), the importer a circle with
`{0}%` under it (`NotificationsBar.inc:29-31`), and the recording a blinking picture with nothing at all beside it
(`Buttons.xaml:1717-1731`). It is ported that way - pictures, `(N)` and `%`, and the long sentences moved onto the
glyphs' tooltips - and extended for the two states the reference never had a glyph for.

- **Where the cluster stands.** Each master of the reference hung the bar into its own top row immediately after that
  master's corner buttons (`master_home.xaml:184`, `master_bare.xaml:109`), which is the end of the row away from the
  clock. Here the header *floats* over the home screen (`ShellWindow.SetChromeHeaderOverlay`), so a cluster docked at
  the left end of that floating line lies across the menu entries it is drawn over - which is exactly what it did
  before. The cluster is now docked right, after the clock's `Viewbox` in markup so that it lands left of the time,
  and its pictures are 46 in a 66 plate, the size the home's own corner buttons use (`styles.xaml:206-230`).
- **The pictures stand on one line.** A `Path` stretched uniformly into a plate of a *told* size keeps to the top of
  that plate, so the connected mark - a wide link, half as high as it is wide - hung at the top edge while the
  recording's full circle filled its plate right through, and the row walked downhill. Given a ceiling
  (`MaxWidth`/`MaxHeight`) instead of a box, a mark measures at its own shape and the plate centres it.
  `TestChromeHeader.ThePicturesOfTheBarStandOnOneLine` checks both halves: the middles agree, and no plate is out of
  proportion with the art inside it, which is the only way a test can tell a centred mark from one stuck at the top.

- **The queue** is `Notifications/NotificationQueue`: the head is the notice to read next, an urgent one is inserted at
  the head rather than queued behind the world's business, offering a notice that is already waiting moves it instead
  of doubling it, and expiry is checked by the header's existing fifteen-second clock rather than by a timer of its
  own. `Changed` and `Arrived` are raised outside the lock, because a listener takes the queue's lock to look at it.
  The shape is the reference's: `NotificationsModel.cs:104-113` read a count, peeked at the next one, and popped it in
  a screen of its own.
- **The hint hides itself while it is being read.** `NotificationsModel.cs:104` shows the hint only while the
  watch-notifications state is *not* already on the navigation stack; `ChromeInfo.HideNotificationHint` does the same,
  driven from the workflow's `Changed` in `App.axaml.cs`.
- **Four types, four glyphs, three pictures.** The reference chose its picture from the notification's type
  (`NotificationsModel.cs:119-134`) out of the four names in `General/Consts.cs:117-120`. Here they are vectors in
  `Skins/Nereus/images/Notification/` and `Skins/WMC/images/Notification/`, and `UiNotification.IconFor` maps error to
  the error glyph, warning to the warning glyph, and everything else - `UserInteractionRequired` included - to the
  info glyph, because the reference's fourth glyph only ever existed as a bitmap and a wrong-but-sharp picture in the
  header line beats a hole in it.
- **Two states the reference could not have**, because its client never spoke about the connection in the header:
  `Theming/ServerActivityState.cs` answers `connected`, `disconnected`, `connecting`, and the importing word with the
  percentage in its place. The percentage standing where a word would is the reference's own idea - the importing
  cluster prints `{0}%` (`NotificationsBar.inc:29-31`) - applied to a job that is filing rather than to a library.
  A dropped connection files a notification too (`HomeViewModel`'s sign-in refusal, at the server's own screen), so
  the word in the line and the thing waiting to be read say the same thing.
- **The blink is on the model's word, not on the picture.** The reference blinked the recording image with a
  storyboard started inside its own button template (`Buttons.xaml:1722-1731`). Avalonia 12.1.4 has no animation
  attached property reachable from loose skin markup - `<Path.Animations>` is AVLN2000 and a `Setter Property="Opacity"`
  inside a loose key frame is AVLN2200 - and because skin `.xaml` is XAML-*compiled* at build, markup that does not
  compile takes the whole skin down with it. `ChromeInfo.RecordingMarkVisible` therefore flips on a seven-hundred
  millisecond timer and the picture only asks whether it may show.
- **Still deferred**: the reference's recording card in the bottom right corner
  (`Skin/default/screens/SlimTvClient-notification_recording.xaml`, promised under `bv.`). `ShellWindow.ShowSuperlayer`
  and `HideSuperlayer` share one layer with the volume and busy displays, so a card that hides after a while would
  switch the volume bar off with it; the recording's sentence stays in the header cluster until that layer is split.

Tested by `TestNotifications` (order, urgency, a notice offered twice, expiry, removal, the loudest type waiting, the
header's state turning into glyph and word with `30%` standing in for a word, the hint counting and hiding itself while
the dialog stands open, a notification's command running, the bar carrying pictures while its sentences stay behind
them on the tooltips, and the dialog reading one notice at a time, handling one, being dismissed, and saying when it has
run out of things to say), the new cases in `TestChromeHeader` - among them that every picture of the bar stands left of
the clock and on one line with the others, that no word of a server sentence is drawn on the line at all, and that the
pictures the line draws come out of the skin the front end ships - and two new cases in `TestMediaEventStream`, which
ask for `Connected` while the stream runs and `NotListening` once it has been stopped. Three tests were red before this
part and are red after it: `AShortcutWithoutItsVectorYetDrawsItsGlassWithoutAGlyph` and
`TheIconsTheSkinCarriesAreShapesAndNotPictures` want loose skin pictures resolvable in a test run, and
`WalkingDownToAChannelBelowTheScreenBringsItInAtTheBottom` fails identically at this commit's parent - it stands the
cursor on a demo programme whose neighbour on the next channel ends exactly at that minute, so whether it passes depends
on the hour it is run at.

### bz. The four ways out of the home, drawn as shapes rather than as pictures

Reference: `themes/default/styles/Buttons.xaml:1788-1819` (the viewer), `:1821-1846` (the settings), `:1892-1917`
(`BrowseMediaButtonStyle`, the search - which is why this port's button is tagged `browse`) and `:1919-1944` (the
power) - four `NotificationMenu\*.png` squares of eighty with ten of margin, half lit until something stood on them,
each with a `*_focus.png` twin for the moment it was stood on.

The port hung the same four doors in `Skins/Nereus/home.xaml`, but out of the picture catalog: an `Image` per button
pointing at `NotificationMenu/power`, `/user`, `/settings` and `/Search_nofocus`, with the `_focus` pair dropped
because this face answers focus with the button's own opacity (`styles.xaml`'s `Button.nereus-corner-button`). Those
pictures were the last bit of the home that was painted rather than drawn, so they are vectors now -
`Skins/Nereus/images/NotificationMenu/{power,user,settings,search}.svg` - and the buttons draw them with
`SkinIconGeometryConverter` like every other shape in the skin.

- **Which shapes, and of which weight.** The four files the user handed over for this row: `power_settings_new`,
  `person`, `settings` and `search`, all four Material's *filled weight 600* cut at fifty points, filed under the
  corner's own names. They are filed a second time rather than shared with the media menu's shelf - the way the
  reference kept `User.png` and `actor.png` apart even though both are a person - precisely because the cut differs:
  `MediaMenu/actor`, `/setup` and `/search` stay the light cut they were, because a tile two hundred points wide can
  afford one. **A heavy cut belongs to a small mark and nowhere else.** That is measurable rather than a matter of
  taste; sampling each shape over its own box says how much of the box the ink covers:

  | mark | light cut | the cut this row wears |
  | --- | --- | --- |
  | power | 48.6 % (weight 200) | the weight 600 filled ring |
  | person | 94.1 % over a 14.0×13.0 box | 94.2 % over a 16.7×17.0 box |
  | gear | 85.3 % over a 17.2×18.0 box | 53.1 % over a 20.4×20.7 box |
  | magnifier | 73.5 % over a 16.2×16.2 box | 56.5 % over an 18.2×18.2 box |

  What the table shows is that the light cuts draw their mark *inside* a smaller square: at the same plate the
  weight 600 files fill more of it, which is what the eye reads as "the row stopped being thin" once the skin is
  scaled onto a television.
- **How big.** The head line's pictures, forty-six inside the sixty-six plate, which is also what the reference's
  eighty square with ten of margin was: three quarters of the plate, centred. A ceiling rather than a box, for the
  reason `by.` learned the hard way - a shape stretched uniformly into a box of a told size keeps to the top of that
  box, and a person, a gear, a magnifier and a ring are cut to different heights.
- **What it lights up as.** The palette paints a stood-on button's light in the button's own corner, and this face
  writes its buttons square (`ButtonControlRadiusX` says 0), so hovering one of the four laid a *square* of light
  behind a round mark. The class now carries half its own width - `CornerRadius 33` - and the light comes up as a
  disc. The reference needed no such rule: it swapped in a `*_focus.png` that was drawn with its own background.
- **What went away.** `power.png`, `power_focus.png`, `user.png`, `user_focus.png`, `settings.png`,
  `settings_focus.png`, `Search_nofocus.png` and `Search_focus.png`. Nothing else in the front end asked for them, and
  a shape cannot use a lit twin anyway: the colour comes from `TextBrush` and the lighting from the button.

Tested by `TestSkinHomeFiles.TheSkinsCornerButtonsDrawShapesAndNotPictures`: every corner button carries no `Image`
and exactly one shape, no shape is bigger than the head line's, each shape's box hugs its own geometry, and all four
come to rest on one line - plus that the worn skin really carries the four names, because a corner whose vector is
missing draws nothing at all. And by `TestSkinHomeFiles.ACornerButtonLightsUpAsADiscAndNotASquare`: the button
carries half its own width as corner, and the element inside its template that paints the light carries the same
corner - a setter nobody reads would keep the square.

### ca. A question raised over a screen has to hold the keyboard

Reported by the viewer: a dialog came up and the arrows did nothing - the keyboard was still in the screen behind.

The reference never had that failure, because its screen took the keyboard as its own business. At the end of
`Screen.Prepare()` - after it had laid the screen out, repeatedly, until measure and arrange stopped disagreeing - it
did two things (`ScreenManagement/Screen.cs:464-495`):

```csharp
HandleScheduledFocus();
if (!_root.IsKeyboardFocusWithin)
  _root.TrySetFocus(true);
```

- `HandleScheduledFocus()` (`:509-528`, fed by `ScheduleSetFocus(element, priority)`, `:990`) is the reference's
  answer to the fact that **a focus asked for before the layout is asked of a control that cannot answer it**. Lists
  hold nothing until they are laid out, a dialog's buttons arrive with the frame's template, and a `Focus()` said too
  early is simply lost - so the reference queued the request with a priority, applied it after the layout, and kept
  asking until it took or the request grew too old (`MAX_SET_FOCUS_AGE`).
- `if (!_root.IsKeyboardFocusWithin) _root.TrySetFocus(true)` is the net under that: a screen that does not hold the
  keyboard within itself takes it with its own root. A screen - dialog or not - never leaves the keyboard outside.

This front end had neither half. `ShellWindow` asked each screen once, at the moment the stop was handed over
(`IWorkflowScreen.OnScreenActivated`), and:

- `NotificationsView` asked in its **constructor**, which is earlier still - the control was not joined to a window
  at all, so the ask could not even be lost, it never happened. The dialog stood over the shelf with the keyboard
  still in the shelf: the arrows walked a screen the viewer could only see blurred.
- `NereusMenuEditView` asked at the right moment and at the wrong one: it asks its list, whose rows are not realized
  before layout - so its own rule "no rows, so stand on OK" answered, and the editor of a menu started with the
  keyboard on its way out.
- `TvProgramActionsView` had already learned this on its own and asked twice; it was the only dialog that worked.

Now the reference's two halves are here too. Every dialog that cares asks twice - `Ask()` now and the same `Ask()`
posted at `DispatcherPriority.Loaded`, which is the same rule as the reference's schedule: re-decide after the layout,
because what the dialog could stand on has changed by then (`NotificationsView.OnScreenActivated`,
`NereusMenuEditView.OnScreenActivated`). And `ShellWindow.HoldTheKeyboardForTheDialog` is the net: once a dialog is
the top of the stack, the window looks at the next layout pass and, if the keyboard is not standing somewhere inside
that dialog, hands it to the first control of the dialog that can take it. A dialog written tomorrow without a word
about its keys is therefore a dialog that can still be answered.

Tested by `TestTheDialogTakesTheKeyboard`: the notification dialog holds the keyboard when it goes up, its arrows walk
its own buttons and never the screen behind, and the keyboard goes back to that screen when the dialog is done; the
menu editor holds it on its list of lines - the control it asked for, not the one the net would have reached; and a
dialog that says nothing about its keys still holds it. Both new tests were seen to fail with the fixes taken out -
the first with the keyboard left in the screen behind, the second with nobody holding it at all.

### cb. One skin, one colour - including the parts the skin does not draw

Reported by the viewer: the colour a dialog's list, the settings rows and the guide paint the row they are standing
on was blue, whatever colour the skin wore.

The skin was not lying about its own colour. `NereusSkinSettings` holds the reference's colour choice as eight
mutually exclusive switches (`useTorquoise` … `useGrey`, turquoise worn), `ChosenColour` reads them back as the
reference's `Colors.xaml` spells a family - a light, a medium and a dark - and `FrontendTheme.TintFromSkinSettings`
has long since been writing those three onto `AccentBrush`, `PanelRaisedBrush` and `PanelBrush`. The skin settings
screen has the row that walks the eight families, and pressing it re-dresses the running front end.

What was wrong is that **a selected row is not drawn by the skin**. A list row, a ticked box and a slider belong to
the theme the controls arrive in, and that theme colours all of them from one question - `SystemAccentColor` and the
six steps of it, `SystemAccentColorLight1..3` and `SystemAccentColorDark1..3` - which it ships as its own blue
`#FF0078D7`. Nothing in this front end ever answered that question, so:

- the skin's own rule `ListBoxItem:selected { Background = AccentBrush }` only moved the *property on the item*; the
  theme paints its selection deeper in, on the item's template presenter, and its rule is aimed at
  `/template/ ContentPresenter#PART_ContentPresenter`, which outranks a setter on the control. Measured with a
  thrown-away probe: the property said `#ff009592`, the screen was painted `#ff0078d7`.
- the guide, a dialog's list and the settings rows are all the same control, so all three kept the stock blue while
  the tiles, the edges and the head line round them wore turquoise. One question, two answers.

The reference could not have this bug: it had no theme under its controls, and its own masters asked its own colour
file for a selected row - the family the eight switches pick is therefore the only accent there is.

`FrontendTheme.AccentFromTheSkinsOwnColour` answers the theme's question from the skin now, after the palette and the
skin's tint have both been written: the seven colours are the accent the face ended up wearing and the theme's own
ramp around it - hue and saturation held, lightness moved a tenth of the scale at a time, thrice up and thrice down.
Only the colours are written; every brush the theme builds from them follows, so nothing here names a brush and a
control from some later version of the theme asks the same seven questions. It is said of the palette rather than of
Nereus, so the front end's own face and WMC wear their own accents too, and because it hangs on `AccentBrush` the
"Skin colour" row on the skin settings screen moves the standing row of every list in the front end as it walks the
eight families. The row is named for what it answers now, "Skin colour" rather than "Colour".

The answer is stamped onto the application itself, not merely into a dictionary merged into it, and it is said again
every time the application changes its light-or-dark answer (`StampTheAccentOnTheApplication` and
`HoldTheAccentThroughAFaceChange`, both at the end of `Apply`). Both follow from the ranking: the theme asks the
operating system for its accent and writes what it gets onto the application directly, so anything written lower down
- in a nested dictionary - loses the question to whatever colour the machine's desktop happens to wear. Writing once
at startup is not enough either, because the theme answers again on every face change. The order inside `Apply` is
therefore: build the palette → the eight switches → the platform's accent → chrome → skin engine → stamp.

Tested by `TestTheRowAListStandsOnWearsTheSkinsColour`, which reads the brush *inside* the item's template rather than
the property a style was given - that is what the eye sees: the standing row is the skin's accent and not the
platform's blue; the seven accent resources are the accent and six honest steps of it (lighter above, darker below,
never a different hue); the application itself answers its accent with the skin's colour and says it again after a
face change, even when the theme wrote its blue in between; all three faces paint their standing row with the accent
each names; and pressing the real "Skin colour" row on the real screen, into a real file of the test's own, repaints
the standing row yellow without a restart. All five were seen to fail with the new call taken out - every one of them
painted `#ff0078d7`.

While measuring this it became visible that `NereusSkinSettings.ChosenColour` was being written into
`skin-nereus.json` as `"ChosenColour": {}` - the tuple is an answer read off the eight switches, and an answer that
can be read back in as an input does not belong in the file. It is `[JsonIgnore]` now.

### cc. The picture inside an mp3 reaches the shelf it is played from

Asked for by the viewer, after the music import had started answering titles: *make sure the album and track cover
are also extracted and stored in the fanart cache.* The titles were there, their art was not - a music wall of
placeholders although almost every file in the library carries its own front picture.

The reference had an answer for this and it could not simply be copied, because **fanart handlers were a client
affair**. Nothing in the whole reference asked an `IMediaFanArtHandler` a question except
`Source/UI/FanArt/FanArtService/FanArtDataflow/FanArtActionBlock.cs` - a block in the running client's picture
pipeline. A headless server has no such pipeline, so on the portable side the handlers were registered, loaded and
never once spoken to. Three pieces were therefore needed, of which only one is about mp3 tags:

The plugin all of this hangs on is new on this side of the tree, and its first job was not pictures at all. Upstream,
the aspect that describes a music file is assigned by the Windows `AudioMetadataExtractor`, which reads tags through
`MediaPortal.TagLib` - published only to MediaPortal's own feed, and only as .NET Framework assemblies - and probes
streams with BASS.NET. Neither reaches Linux or Android, so before this a portable server had no extractor that
claimed an mp3 at all: the importer walked a music share, nobody said "audio", and the shelf stayed empty - and the
NFO audio extractor stayed quiet beside it, because it only reads the `.nfo` of a resource an earlier extractor has
already called audio. `AudioTagMetadataExtractor` is the portable answer: TagLibSharp from nuget.org (its
netstandard2.0 assembly runs wherever the server runs), the `AudioAspect` and `AudioAlbumAspect` built out of the tags
the way the reference builds them, and the relationship extractors that turn a folder of files into an album shelf.
Three things hang off that one plugin:

- **The handler** (`FanArtHandlers/AudioTagFanArtHandler.cs` in the AudioTagMetadataExtractor plugin, registered at
  `/Media/FanArtHandlers`). It says of a title whether it has anything to say - the id must be an album, or a track
  that knows its album - and then looks for the album's front picture in the two places a portable server can look:
  the picture TagLibSharp finds in the file's tag, and failing that the `cover.jpg`/`folder.jpg` the album's folder
  holds, walking one level up when the track lies in a `CD1`-style disc folder. One picture per album, stored under
  the **album's** id, as both `Cover` and `Poster`, because music calls its front picture a cover while every file
  name and every other screen calls the same thing a poster.
- **The asking** (`FanArtWarmingPass.cs`). `POST api/media/fanart/warming` waits for the importer to drain, then
  walks the library page by page - everything optional, a `MediaItemQuery(null, …)` in the client block's own idiom -
  and puts each title in front of the picture handlers. `GET` of the same address reads the pass' state, so a front
  end can press it and then watch. It remembers what it has already looked at, so a second pass over an unchanged
  library costs one database lookup per page and opens no file.
- **The answer** (`MediaController.GetFanArtOfType`). `Cover` joined the kinds an endpoint accepts, and a track asked
  for a front picture is followed along its `Track→Album` relationship, because a thousand tracks share forty-odd
  covers and the import writes each one once. Order of the asking: the cache for the id itself, then the album the
  id belongs to, then - and only then - the folders around the file.

One cover per album rather than one per track is the viewer's choice, and the measurement that made it obvious: in a
sample of the library's files, 400 of 400 carried a picture, 169 KB on average - stored per track that is ~172 MB of
duplicated art for one shelf, stored per album it is a few megabyte.

Two things about the portable platform had to be worked around, and both are worth remembering:

- `BaseFanArtHandler.TrySaveFileImage` goes through `IImageTransformer`, which is GDI+ and therefore Windows-only.
  On `net10.0` it fails quietly - which is exactly how the NFO handler's `<thumb>` shows up in the live log, as a
  WARN and no poster. The handler therefore writes the bytes it read itself, naming the file `File.<title>` with the
  extension sniffed from the magic numbers, which is what `FanArtCache.GetFanArtFiles` lists again.
- Asking for the relationship as an *optional* aspect is not the same as being answered one. The detour guarded on
  `item.Aspects.ContainsKey(AudioAspect.ASPECT_ID)` while its query asked only for `MediaAspect` and
  `RelationshipAspect`, so the answer never carried the aspect and every title looked like one with no album. The
  content gate caught this as a plain 404 for the track; `TestATrackWearsTheCoverItsAlbumHolds` now asserts the
  aspect is asked for, rather than only that the picture comes back (`ATrackWearsTheCoverItsAlbumHolds`).

The ordering of the three questions above was found the same way, on the live library: an album answered its chosen
71703-byte picture while a track of the same album answered 31514 bytes, because the folder walk ran first and found
the `folder.jpg` lying in the album's folder. Two tiles of one album wearing two pictures is the sort of thing no
test notices and everybody sees, so the album is asked before the folders are walked, and
`TestTheAlbumsChosenCoverOutranksThePictureInTheFolder` writes both pictures into one folder to keep it that way.

Proved at every level available. `TestAudioTagFanArtHandler` (14 tests) reads real tag pictures out of real fixtures
through MP2's own local filesystem provider and mirrors the real cache layout on disc, including the disc-folder
walk, the "not a front cover but still a picture" case, the album's folder standing in for a file with no tag, and
the second track of an album not opening the files again. `TestFanArtWarming` (3 tests) covers the pass walking a
library and waiting for the importer. `TestMediaController` (3 tests) covers the endpoint: the detour, the ranking
of the chosen picture over the folder's, and a release of its own still being answered from its own folder.
`Tests.Server` is 527 of 527. `scripts/ci/test-server-import.ps1` gained section 8c, which asks a *running server*
for the album cover, expects the 373-byte JPEG that sits in the fixture's tag, expects the track to be answered with
the same bytes, and then looks in the data folder to see the file is really in the cache rather than read out of the
user's mp3 on every request. And on the viewer's own library: 43 albums, 42 with a cover, 84 files and 14.38 MB
stored, and all 42 albums answering their tracks with byte-identical pictures - zero mismatches.

What is still not portable, and now precisely bounded: a `movie.nfo`'s `<thumb>` needs `IImageTransformer`, so it is
kept as a stub and warned about rather than stored; and fanart from the network (artist pictures, TMDB backdrops) has
no portable source wired up at all. Both are one handler each away, and neither is music.

### cd. The music shelf reads like the reference, opens like it, and stands on the pictures it really has

Four things the viewer found on the live music shelf, all four traceable to a difference between "the port answers"
and "the reference answered":

**The list layout was a strip of covers.** The squares themselves were right - the viewer confirmed them - but the
List view of a music shelf drew the same squares stacked vertically: "the cover as a list". The cause was one line's
hierarchy: `GridTemplateKey` asked the music question before the layout question, so a music shelf got its square in
every layout. The reference separates the two views cleanly - `ListViewAudioItemTemplate` / `ListViewAlbumItemTemplate`
(MediaStyles.xaml 3391/3678) are text rows: title, year, rating, duration or track count, and a thumbnail that is
bound to the database blob the audio importer never fills (zero width at the default size), while the squares belong
to the grid and the cover view - and so the port now answers `TitleRowTemplate` first whenever the layout is List,
music or not, and `ArtworkForLayout` stops fetching covers while the shelf stands in rows.

**A press on an album asked it for a file.** The home content wall builds its rows with `new TitleViewModel(...)`,
which hardcoded `holdsTitles: false` - so `OnNereusTileClick` routed every tile to `ResumeCommand` and an album
answered "the server did not name a file for this title". The reference's wall opened albums (MoveInside), never
played them. The row now carries the truth (`MediaCatalog.HoldsTitles(section.Aspect.Name)` at the one place that
knows the aspect), `TitleViewModel.HoldsTitles` says it out loud, and the application's home handler
(`App.OnHomeTitlePressed`) opens a holding title exactly as the shelf's OK does - pushing the titles-inside frame -
and plays everything else. `HomeView.OnResumeRequested` keeps the other half: a holding title does not get the
player screen pushed beside the album page it just opened, which is what it had been doing.

**The picture cache kept "no picture" forever.** While the viewer's library was being tagged, the wall asked for
posters the server did not yet have, and the front end wrote an absence marker per title - a good idea, until the
tag extractor produced the pictures two hours later and the wall still stood bare, because an absence never expired.
An absence is now an answer for a day (`PictureCache.ABSENT_MAX_AGE`, and the stamp is renewed only by a re-ask that
still answers "none"), which is the same courtesy the folder walk already paid to new pictures;
`AKeptAbsenceAsksAgainOnceItHasAged` keeps it.

**"FanArt" for an album answered 404 while the artist's backdrop lay two folders up.** The reference's
`LocalArtistFanartProvider` answers an album's or a track's FanArt by climbing from the tracks' folders to the
artist's folder and reading `fanart.jpg` (and `ExtraFanArt/`) there. The portable folder walk
(`LocalFanArtSearch.FanArtAnchors`) knew that detour for a series and a season - anchor on their first episode - but
not for music, so albums, which have no file of their own, never started a walk at all. `FirstTrackOf` is the music
shape of the same move: relationship `Track→Album`, first track, three hops up - track folder, album folder, artist
folder - and it is exactly the two hops plus the disc-folder hop the reference counts. Proven byte-identical on the
live library: the two albums that answered 404 now answer with their artists' `fanart.jpg`, 232 238 and 868 044
bytes, the same bytes as the files on disc; `Tests.Server` fanart 53 of 53.

### ce. The background changes like weather now, not like a slideshow

The fanart behind the home menu and the media browser used to be replaced in one sudden frame whenever focus moved
to a title with a different backdrop. The reference never cut: its `ImageSourceWrapper` carried
`Delay="0.5" DelayInOut="true" Transition="transitions\dissolve" TransitionDuration="0.5"` on every fanart picture -
a picture was only adopted once the question had stopped changing for half a second (so walking a rail flickers
nothing), and it then grew in *over* the old one rather than through a dark window between the two.

`Views/DissolveImage.cs` is that control: two stacked images, a one-shot wait that every new source restarts, and a
crossfade that moves both layers in the same breath - the coming picture's opacity up while the standing picture's
goes down - handing over only when the fade has finished, so an interrupted fade can never leave the background
stuck on a half-faded third picture. The three hosts (the skin's `HomeBackgroundTemplate`, the plain home
background, the media browser's background) swapped their `Image` for it with their bindings untouched.

The timings are the reference's delay with a livelier dissolve: 0.5 s of settled-wait, 0.125 s of crossfade, tuned
by feel against the user's own screen ("doppelt so schnell" asked twice). One honest note from building it: the
first draft's per-step branch was written backwards - it faded the old picture out and then handed the new one in
one frame, which is exactly the stutter the user rejected before it was caught.

### cf. The tile's name stands at its foot now, like the shortcut's does

The user pointed at a screenshot: the caption under a home-content cover sat too high on the tile while the
shortcut tiles' captions stood neatly at the bottom. The wall in Nereus mode is the inline `HomeContentTile`
template in `Views/HomeView.axaml`, and its caption band (`Border.home-tile-caption`) let its text slide to the
top of its own height. The text now sits on the band's bottom edge with the same 10 DIP foot the shortcut
captions have (`Padding="8,5,8,10"`, `VerticalAlignment="Bottom"`), so one rule - a name stands 10 DIP above
the tile's foot - holds across the whole wall.

### cg. The server address moved into the settings hub's right pane

The "Server address" row pushed a full-screen page while every other row opens its content in the hub's right
pane. The row now builds the very same `ConnectionView` embedded in that pane - it starts its search and lays
its focus on attach, and saving swaps the client's server and re-reads the library without any navigation,
trail and back key untouched. A view constructed for the pane hides its own "Close" button through an explicit
`offersClose: false` rather than by noticing it has no workflow, because the standalone screen, the tests and
the designer all stand without a workflow and must keep offering the way out.

### ch. The front end wakes up and asks who is watching

The client now opens on the 2026 wallpaper (scaled to fit, `Assets/startup-wallpaper.png`) instead of straight
into the home menu, and the question it asks is the one the reference's login plugin asked - only with a PIN.
`ViewModels/StartupViewModel.cs` is the state machine: read the server's people, ignore its machines, and find
the remembered name among them. Nobody needs numbers - the animation holds its four seconds and the library
opens. The remembered person has four numbers - a round avatar with their initial beside four small rounded
boxes, focus stepping forward with every digit, Del undoing the last one, and the fourth digit alone being the
answer, which is checked against the server's own store (the PIN is the user's password there, hashed by the
server, and `SignInAsync` is the door). A wrong one shakes the card, empties it and asks again; nothing opens
past it, and nobody is ever locked out after N tries.  Pressing the round picture lays the house list over the
wallpaper and the picked person either walks right in or gets asked. `Views/StartupView.axaml` draws all of it -
the wake's animation is a rotating conic-gradient ring with a breathing halo on the same deterministic timer the
dissolve uses.  No on-screen number pad: the keyboard and the remote write the PIN, which is what a television
is driven with; the card sits at the bottom of the screen with a hand's width of margin rather than floating in
the middle, and the greeting line the first draft printed is gone - the picture and the boxes say it already.

Two promises worth their weight: the wake is the workflow's *root*, so the back key and the trail cannot walk
around it - the view eats Back and Escape; and it claims the whole television to itself (`IFullScreenScreen`,
the film's own contract), so no clock, day or weather picture stands over the wallpaper before somebody has
actually arrived. A third: a server that cannot be reached does not weld its door shut -
the remembered person is let through after the wait with the complaint said out loud, because a PIN nobody can
check is not a lock. Settings → Users is the PIN's other half: four digits or nothing typed at all, and the
startup's "asks for a PIN"/"walks right in" wording everywhere. TestStartup keeps ten of these promises; an
honest note: its first run poisoned a later fixture by leaving the answered name as the skin settings' file
owner - the teardown owns that back now.

### ci. The whole household: server-kept house rules, restrictions that bite, and a chip to switch

The wake screen was half the reference's login plugin. This is the rest of it - the parts that live on the
server, the parts that hide things, and the parts that give the television back.

**The server keeps the rules.** `UserLoginSettings.cs` carries the reference's five `UserSettings` fields
(`EnableUserLogin`, `EnableUserLoginScreen`, `AutoLoginUser`, `AutoLogoutEnabled`,
`AutoLogoutIdleTimeoutInMin`) as Global settings, and `api/media/settings` gained them as five nullable fields
that a PUT only touches when present - so the server-settings screen cannot clobber the house rules and vice
versa. `UsersController` grew the whole user picture: the reference's restriction keys (age ceiling with its
parental-guided and unrated exceptions, restriction groups, allowed shares) are written and read with exactly
`UserConfigModel.SaveUser`'s semantics - the inverted "AllowAll" spellings included - through
`WriteRestrictionsAsync`, pictures travel as base64 with the reference's own 128×128 cap
(`Theming/UserPicture.cs`), `LastLogin` rides along read-only (the server stamps it at sign-in, not the
client), and the user list is `[AllowAnonymous]` because the wake has to read names before anybody has a
token. The identity chain was already there: sign-in answers a bearer token carrying the profile id, and
`MediaController.CallerRestrictionFilter` now runs that caller's `GetUserFilter` - the reference's DB-level
`UserProfile.GetUserFilter`, certification range with its two exceptions plus the share filter - into every
title list, count and group the front end reads. A child's shelf is filtered where the titles are *read*, not
where they are painted: a hidden thing that cannot be searched, counted or guessed by id cannot leak.

**The rules are defaults that ask.** The reference shipped `EnableUserLogin` off; this server ships it on,
because a household that has just been given names and PINs should be asked from its first start, not
silently not - the door locks itself when you hang the lock. A house that wants the open door turns it off
once, under Settings → Users, where the `House rules` section reads and writes all five fields as the
server's own answer rather than this machine's opinion.

**The client obeys.** `Theming/ViewerState.cs` is the front end's `IUserManagement`: the current person, the
last-known policy, and the reference's `CheckUserAccess` as `Allows`. The wake reads the policy first: login
off walks the household in unasked; an expected `AutoLoginUser` walks right in - but still signs in with the
empty password, because the sign-in is what tells the server *who is asking*, and a walk-in who stays
anonymous would quietly keep the parent's whole library; a question switched off asks nobody and signs nobody
in; an unreachable server changes nothing at all. The head line carries the watcher as a coin - their picture
or their initial, pressed to open the same wake screen as a *switch* over the library, with "let this
television go" under it; a sign-out calls the workflow's new `Reset`, which closes the whole trail (and stops
the channel that played in it) rather than leaving a back key into a signed-out person's screens. The idle
watch is the reference's auto-logout: fifteen-second looks at the window's own input, the server's minutes,
and never in the middle of something - a paused film keeps its owner in the chair. Restrictions hide rather
than grey: the settings cog lifts off the home screen for a person whose list does not carry
`Settings` (`HomeView.ApplyScreenGates` reads the same gate the press reads, and `BuildNamedFrame` answers a
key nobody may use with nothing), the "Users" section hides behind `Settings.UserProfile`, and the
delete-recording button hides behind `SlimTvClient.DeleteRecording` - with the model refusing again at the
press, because one rule belongs in one place. The catalog offers only groups something here actually obeys;
the reference's `Media.DeleteMediaItem` stayed out until there is a delete press to hide.

`TestUserRules` keeps seventeen of these promises - the `CheckUserAccess` truth table, the four policy shapes
of the wake, the switch and its sign-out, the chip, the refused timeout, and the walk-in who still says their
name at the door.

### cj. Who is the grown-up: the derived administrator, the three kinds, and a door that stays locked

**The role is not stored, it is read off the restrictions.** The reference never ticks "administrator"
anywhere; its token endpoint (`MediaPortalCredentials`) works it out at sign-in - a real person, not a
machine's profile, who restricts neither shares nor ages and does not use restriction groups. That one rule
is now the API's: every `api/users` entry carries `isAdmin`, computed by the same sentence, and
`Tests.Server/Api/TestUsersOverHttp` says it in three directions - the grown-up with a PIN is the
administrator (a PIN is not a restriction), a thirteen-year-old ceiling is not, and a client profile is never
a person at all.

**The three kinds are the reference's, ids included.** `GET api/users/templates` states Admin, Teenager and
Child with the restrictions the Login plugin ships them with and the `TemplateId`s from
`UserTemplateSettings`, so a household that grew from the reference editor finds its kinds already named.
A created or updated user can carry one (`templateId`), stored under the reference's own
`UserDataKeysKnown.KEY_TEMPLATE_ID` - `UserProfile.TemplateId` reads it back - and the users screen has a
`Kind` picker that lays the kind's preset under the age and group boxes and then keeps out of their way:
the kind says what somebody *is*, the boxes say what the household enforces, and naming a grown-up as a
Child does not quietly impose five years on them.

**One household, one administrator at the very least.** The reference guards nothing here: its own editor can
delete every grown-up in the house, and its editor is the only way back in. This server refuses that - the
last unrestricted person cannot be deleted and cannot be restricted into nobody (`400` with the reason, from
`DeleteUser` and from `UpdateUser`, checked against the whole store). The floor holds the way up as well as
the way down: in a store that has already lost its administrator - which this server can meet, because the
reference could produce it - every change stays possible, so a wrong tick can always be undone from the
screen that made it. `TestUsersOverHttp` walks all four cases.

**A silent server locks a numbered door rather than opening it.** Walking the remembered person in when the
server cannot be reached was the fast way in for a PIN-less household, and for a household with a PIN it was
a PIN that simply stopped being asked. The client now remembers the one thing it cannot ask a silent server
- `rememberedViewerHasPin`, learned from the server on every start where the server speaks and written by
every successful sign-in - and with it set the wake keeps its question up: the name, the four boxes, and the
honest sentence that nothing can be checked right now. A PIN typed against a silent server is refused with
that sentence rather than let through.

`TestUsersOverHttp` (eight tests over the real HTTP pipeline) and seven more in `TestUserRules` and
`TestUsersSettings` keep these promises; the live server was proven with a throwaway teenager that came back
thirteen-years-old with its kind attached, a refused deletion of the house's only grown-up, and that
teenager's clean removal afterwards.

### ck. The one user button: the head line's coin is gone, the corner's person is the door

The household got a second door where the reference had one. The head line had grown a round coin wearing
the watcher's picture, while the home screen's corner row - ported from `master_home.xaml`'s
`WindowButtonsPanel` - had carried the reference's own `UserButton` (the `NotificationMenu/user` glyph) all
along. The coin was wrong twice: it was a duplicate, and it stood in the head line even while nobody was in
the chair - an empty circle with no initial and nothing behind it, the artifact the household named.

It is gone from both skins' `chrome.xaml`, and with it the whole chip block of `ChromeInfo` - the picture,
the initial, the command, the viewer-state listener. The corner's person button is now the door the
reference's was: `BuildNamedFrame("viewer")` answers with the very wake the machine starts on, guarded
against stacking on itself. Its visibility is the reference's rule for
that button read through the screen gate - a house that asks nobody (`EnableUserLogin` off) does not hang
the handle.

It is not a page of its own. The reference's `UserButton` navigated to its login screen, and this door
navigates to the very page the machine starts on: the person standing there, their face, their four boxes.
The household is reached the one way it is reached at the start - a press of the round picture - and only a
switch with nobody to ask (a person who wears no numbers) opens on the household directly, because a face
with no question and no way to anybody else is a dead end. The press that lets the television go lives
behind that picture, so the picture opens even in a house of one. `ApplyViewer` and `ViewerFrame` went with
the chip.

The question stands at the head of the wallpaper now rather than at its foot, on the same ninety-six it
carried below, and the wake's ring keeps its centre.

Two things about the question itself were wrong. It printed the numbers - a PIN standing on a television in
a living room is a secret the whole room reads - so each box now holds a plain centred dot and nothing
else, which is all the person typing needs back: the count. And the screen had one focusable thing in it,
the round picture, with no road to it: every arrow was left to the focus manager, which found the boxes
unfocusable and the walk therefore nowhere. The wake now says its own two stops out loud - Left and Up step
from the boxes to the picture, Right and Down back, Tab toggles, Enter opens what the picture opens - and
the picture lights its ring when the keyboard stands on it, the way the reference's `User_focus.png` pair
did. Delete joins Backspace in taking the newest number back, and the household overlay takes the focus to
its list the moment it opens, so the names walk and Enter takes one.

Two more things were missing, and both were about faces. The round container at the question wore the
name's initial even though the server carries a picture for the person - the users screen shows it, so a
household that gave somebody a face could not see it anywhere it mattered. Every road to the question now
runs through one funnel that takes the *person* and not only their name, so the container decodes that
person's picture and wears it, falling back to the initial only when the server carries none; the one road
that cannot know a face is the offline door, where the server is silent and a picture cannot be read from
nothing.

The household's own dialog was built out of a border the view wrote for itself, with names in a list, a
sentence about who asks for numbers, and two buttons under it. It wears the skin's dialog frame now -
`theming:SkinTemplates.OverlayTemplate="DialogFrameTemplate"`, the same frame the close screen and the
notification shelf ask the worn skin for - and it holds nothing but faces: a round picture cut to a circle,
the name under it, and nothing else. It is the *whole* household rather than the others, so a household can
be answered by staying, and pressing one's own face is exactly that answer rather than being asked for
one's own numbers a second time. With the buttons gone, the back key is what closes the wall; it answers
"stay as before", which is also why `StayAsWas` now steps the wall aside on both roads - a question that has
been answered and still lays its faces out would still be asking. Sign-out stays the model's command and lost
its button; if the room wants a press for it, the close screen is where it belongs.

The wake's ring had one more flaw, and it was a timing one: the panel that carries it was bound to the
model's reading phase, so the moment the two server reads answered - a heartbeat on a living-room network -
the animation was swallowed and the question stood alone on a suddenly still wallpaper. Nobody had seen the
wake at all. The binding is gone, and the ring's life is the screen's own clock instead: one turn of 360° at
the wake's 150° a second, two and a half seconds that no server can shorten, and then a third of a second of
fading and the ring is gone for good - the animation is a greeting, not a screensaver, and the four boxes do
not need company for as long as somebody stands reading a name. While the wake still holds the wallpaper to
itself (the no-PIN case, the model's four seconds) the turn simply carries on, because then it is the whole
show. Only the wake's *words* stay bound to the reading phase, because the card under the face says
everything the model has to say once the question opens. The refused PIN says
"Wrong PIN, try again." now rather than addressing the person by name in a sentence, and the line under the
name pointing at the picture ("press the picture - or the left arrow - for somebody else") is gone; the
picture is discoverable on its own, and a hint under every question is noise.

The frontend suite stands at 1163 of 1166 with only the three known baseline failures; the chip test became
a test of the gate's one rule, the head line carries no person again on either skin, and the wall gained a
test each for carrying everybody, for one's own face meaning stay, and for a picked face walking its picture
along to the question.

### cl. The wall walks the way the reference measures: rects, not centres - and the menu is a place to stand

Three complaints about the home screen's keyboard, and the first one was asked as a question: *why* does up from a
tile on the Nereus wall land on a shortcut instead of on the tile above? The answer, measured on a shown screen
rather than guessed at, was that the hand-written walk scored every candidate by the distance between its
**centre** and the current one's, with a fixed one-and-a-half times penalty for standing sideways - and the wall is
built of squares of three different sizes. The shortcut glass is a third of the size of the covers beside it, so a
shortcut's centre sits nearer the middle of the screen than the centre of the cover a row above; from the top row of
covers nothing at all has a centre higher than the shortcut column's, so the first shortcut won every up key, and a
cover twice as deep as the one above it lost its own down key for the same reason. Measuring the same way twice also
explained the second complaint: nothing in the wall ever *was* above a cover, so the walk found nothing, and the one
favour it was written with was a Left key that focused `menuList` - the bare list, which takes the focus, shows no
frame, and answers no arrow. A menu you cannot stand on is a menu you cannot reach.

The reference has both answers already, and both were ported rather than invented:

- `FrameworkElement.FindNextFocusElement` with its four `Located…` predicates (SkinEngine FrameworkElement.cs
  1032-1215) is now `Input/SkinFocusWalk.cs`. A candidate lies in the direction asked for when a line from the
  middle of the edge the viewer moves away from to the middle of the candidate's facing edge points that way within
  eighty-four degrees, or when the two rects merely touch there; the winner is the one whose near edge is best lined
  up along the crossed axis, and between equals the one whose *borders* are nearest (`BorderDistance`, never the
  distance between middles). A square standing beside the one you are on is not above it, however near its centre
  comes, and that is the whole of the fix.
- The walk's candidates are the wall's squares, **the menu rail's entries and the row of buttons the face hangs at
  the head of the screen** - one screen and one walk as MP2 answers its arrows - so left from the wall's left edge
  steps onto the shortcuts and once more onto an entry of the menu, while up from the top row leaves the wall for
  the power, the person, the cog and the search glass, which is the only thing that really does stand above it.
  Where focus lands is a `ListBoxItem`, not the list: the rail steps its viewer onto the entry it is showing (the
  wall's own, or its first), and the menu's Right key hands them back the square they came from, which is what the
  reference's `NereusHomeListView.RestoreFocusItem` was written for - with one exception, the head of the menu,
  where the row of buttons is the thing standing beside the viewer.
- `ItemsControl.RestoreFocus` (SkinEngine ItemsControl.cs 562-620) is the memory: a list a viewer has walked offers
  them the item they last stood on when the focus re-enters it, and only when that item really does lie in the
  direction they asked for - the remembered element is offered to the walk as a candidate, never bolted on after it.
  Ported per *band* (`HomeContentTile.Group`, set by `AddSquareTo`), because this wall holds several bands where
  MP2's home had several lists: genre, then age three squares down, then across to the series and back lands on age.
  The memory lives with the shown wall, so a viewer who changes section and returns starts at the geometry again.

`TestNereusWallNavigation` walks the wall the music entry actually builds - the shortcut column, a `square1` band of
six covers in three sizes, a `square2` band beside it - on a 1920×1080 screen wearing the skin, and it is written as
measurements on that shown screen on purpose: the wall's scale, the rail's virtualized entries and the mixed squares
are exactly the things a unit test of the helper would have missed. It asserts up and down inside a mixed mosaic go
to the squares above and below (including the big square under a small one, whose centre is deliberately further
left), left from the wall's left edge reaches the menu and the menu gives the way back, and a band hands back the
square that was last stood on - with a preceding assertion that the square the geometry would have picked is a
different one, so the test cannot pass by accident.

The row of buttons was the second round of the same complaint, and it was worth the round: the first fix made the
*menu* reachable and the household answered that "the top menu items can still not be reached", meaning the four
buttons of `master_home.xaml`'s `WindowButtonsPanel` - power, person, settings, search - which `home.xaml` hangs at
the head of the wall and which nothing but a mouse had ever touched. They carry no name for a view to hold, only
their `Tag`, so they join the walk as what measurement finds rather than as a special case for one key, and once the
viewer stands on one, the same measurement walks along the row and back down onto the wall. A key with nothing in
its direction is still nobody's: it is left unclaimed, so the ring behind it gets its turn.

The frontend suite stands at 1173 of 1175 with the two known baseline failures (the shortcut glass without its
vector, and the skin's glyphs as shapes); the guide's own walk test passed again this time, which it had been
failing on layout timing alone.

### cm. A dialog is shown with the data of the screen that opened it - the household wall, and what it was hiding

The household wall opened by a press on the wake's round picture came up as a slab: a bordered band near the
middle of the screen, no faces in it, and a glow that looked broken because it was drawn around nothing. The
answer was not in the dialog's markup but in what a skin's dialog frame *is*: `DialogFrameTemplate` is a template
drawn over the dialog's own control, and the `ContentPresenter` inside it takes that control as its content - and a
control a `ContentPresenter` takes as its content is drawn under the *presenter's* data, which is the body itself.
Inside the frame, then, `{Binding}` is the dialog's body and not the screen, so `ItemsSource="{Binding Household}"`
asked the `ListBox` for a household, found none, and the frame measured its own padding and stopped there. A
measurement of the shown wake said the whole story: the frame's `Border` stood 624×52 with its ground `#1b1b1b`,
its outline `#009592`, its corners at 25 and its `BoxShadow` spelled `0 0 120 Transparent, 0 0 26 White` - frame,
outline and glow all intact - around a body of zero height, on a machine that had really read three people.

The rule was already written into the skin and had simply never been carried to the bodies: the heading of
`DialogHeadedFrameTemplate` is read off `$parent[ContentControl].DataContext` rather than from the body for exactly
this reason. Two attempts to fix it in the frame itself were measured and reverted: setting `DataContext` on the
frame's `ContentPresenter` also re-homes its `Content="{Binding}"`, so the body is never hosted at all (the frame
drew the view model instead of the list), and a `DataContext` setter in a style for `ContentPresenter > Control`
never reaches the hosted body. The skin's files are therefore untouched. Each dialog body hands its screen's data
back to itself instead - the wall of faces, the way out, the notification dialog, the television's question about a
programme and the menu editor - which also means the words those last four have always carried are now actually
standing in them: they were empty panels, not merely spare ones.

The wall itself was brought round to what was asked: a face is a round picture and its name, nothing else. So the
furniture a list normally drags in stays out - the accent block a selected row wears and the platform's square focus
frame are both neutralised - and a face the keyboard stands on lights its circle with the very light the wake's own
picture wears one row above, from `#55E2621B` at three to `#FFFFB066` at four. The ring at rest is handed to the
circle by a style rather than by a local value, because a local value outranks every style and the light could never
have won against one. The wall also opens with its first face stood on, since a list that opens with nothing stood on
answers no `Enter`, and a face whose picture the household uploaded now shows that picture where an initial used to
stand.

`TestTheHouseholdDialogShowsItsFaces` measures the wake shown at 1920×1080 in the skin with three people read: every
face stands with its name under it, one wears its picture and one its initial, the frame has grown to the height of
its body and still wears the 120 of glow, and the stood-on face is lit while no orange block or square frame stands
about it. `TestTheDialogFrameKeepsTheScreensData` says the same rule for a dialog written the ordinary way, by
standing the way out's words inside the glowing frame that carries them. The frontend suite stands at 1176 of 1179;
the three failures are the two known baseline ones plus the guide's walk test, which fails on a programme boundary
at this hour of the day and fails identically with these changes stashed away.

### cn. The wall of faces is a dialog the shell raises, and what two skin rules had never reached

Asked again and again over a build the user could see, the wall came up wrong in a way no measurement of the dialog's
own markup could show: its glow was cut off and did not read white, and the home behind it stood on sharp. Both
halves came back to where the dialog stands rather than to what it contains.

It had been laid out *inside* the wake, as a dark panel over the wake's own markup. A panel standing on a screen is
drawn with it, so nothing the shell does to a screen behind a dialog - throwing it out of focus and washing it - can
ever reach it, and the halo of a frame drawn there falls into the ring of light the wake itself throws across the
room: neither white nor whole, exactly as reported. The wall therefore became a dialog of the same making as the
notification shelf and the menu editor: `UserPickerView` is a view of its own, an `IDialogScreen` that wears the
worn skin's `DialogHeadedFrameTemplate` over the `nereus-dialog-screen` ground, and the wake *asks* it - the model
saying "faces" makes `StartupView` push a `household` frame onto the workflow, and the answer pops it again before the
pick is carried back, because picking a person can finish the wake and rebuild the stack the question stood in. The
word the wall is asked under lives on the model as `Heading`, which is where the headed frame reads it from and where
the trail's crumb reads the same string from, so a question is named once. Inside it there is nothing but circles and
names, as asked.

Two rules the skin had carried for a long time had simply never fired, and both hit every dialog rather than this one:

- `UserControl.nereus-dialog-screen` dressed no dialog in anything. A plain type name in a selector means *that type
  exactly*, and a dialog screen is never a `UserControl` off the shelf but a view written below it - so the wall, the
  menu editor, the programme question and the notification shelf had each stood over a home that was washed by
  nothing. Written as `:is(UserControl).nereus-dialog-screen` the rule reaches the views, and the wash measured
  `#a6000000` over a backdrop holding a real photograph of the screen behind. A bare `Window` selector, by contrast,
  was measured to reach `ShellWindow` just fine, so the hover family written that way needed nothing.
- Each dialog view hands its body to the frame template through a `ContentControl` filling the view, and such a
  control is measured to the frame it hosts: the frame's own bounds, not the halo drawn outside them. Clip that and
  the glow is cut to a rectangle at the panel's edge - the frames spell out that nothing in them may clip for
  precisely this reason. The skin now sets `ClipToBounds` false for that control, once, for all five dialogs, and a
  measurement walks every surface between the frame and the window to confirm that each of them that cuts leaves the
  glow its 120.

The blur had a third cause of its own. The shell photographs the ground behind a dialog for every screen except the
ones that are *moving image*, and it had decided "moving image" by asking whether the screen filled the window -
which the wake does, with its wallpaper. So the exemption became what it always meant: a marker of its own,
`IFilmScreen`, worn by the player; a screen that fills the window but stands still is a ground and gets thrown out of
focus like any other, which is the only way the wall of faces can stand over a home it cannot be confused with.

Measured: `TestTheHouseholdDialogShowsItsFaces` now drives a real `WorkflowManager` and a real `ShellWindow` through
the application's own view factory, and besides the faces, names, pictures and initials it asserts that the wake
behind the wall has been thrown out of focus and washed, and that nothing between the frame and the window cuts its
halo. `TestTheDialogBlursTheScreenBehindIt` says which screens are photographed: a film still stands sharp, a
full-bleed screen that stands still does not.

### co. The film plays over the whole screen, and its grey turned out to be a format the engine never agreed to

The playback screen was measured against the reference and found to be wearing a whole window of things the
reference does not have: a navigation bar (home, title, "now playing") above the picture, the title again in a header,
a framed panel saying "Playing" with the poster inside it in the middle of the screen, a second progress bar, and a
line at the bottom still saying "Nothing playing" while a film was running. `NowPlayingView` was rebuilt: the video
area is the screen, `VideoSurface` fills it edge to edge, and everything else - the transport row, the position, the
track switches - is an overlay that stands over the picture and hides itself, which is how the reference behaves. The
shell's own trail is hidden for a screen marked `IFilmScreen`, because a film is not a place in the menu.

The grey was not the source, and it took a measurement rather than an eye to say so. The format callback the engine
calls before the first frame is a *question*, and the frontend had been answering `RV24` while allocating four bytes a
pixel. `libVLC` answered the call with its own chroma - `I420`, planar - and nobody looked again. Reading a
luma-plus-chroma buffer as if it were blue-green-red pixels is exactly the report: no colour (the first third of the
buffer is one grey channel) and vertical stripes (the chroma planes read as pixels). Asking for `RV32`, which is what
the sink's buffers already are, fixed it; the hardware decoder is happy to convert to 32-bit and had simply never been
asked to. With `RV24` the log shows libVLC building a `d3d11_filters` + `Swscale` chain and then removing it, and the
frame counters stay healthy throughout, which is why the logs had looked fine for so long.

Colour was then weighed rather than admired: `MP2_FRAMEPROBE=1` prints the mean blue, green, red and the mean `|R-B|`
of every hundredth frame (grey scores ~0, this film's SMPTE-bars reference scored 85), and the drawn frame was
compared with the same timestamp from `ffmpeg` through `signalstats` - chroma averages within 0.4 of each other, twice,
once against the real film and once against a synthetic bars clip whose bars carry known chroma. `--play` now reports
the negotiation itself in every run (`['I420' offered at 1920x832, taken as 'RV32' at stride 7680]`), so the question
and the answer are in the same line forever.

Two follow-up questions were answered with numbers rather than reassurance. The picture does not lose anything: the
buffer is 1920x832 and the picture 1918x802, which is the container's own display size - codec padding, which by
convention sits at the bottom and right - and `VideoSurface` stretches uniformly, so it cannot crop; a 2.39:1 film on
a 3440x1440 screen (2.392:1) legitimately has no bars. And the track row had vanished on this very episode: it carries
one sound and one subtitle, and the rule "there is a choice" read *more than one of either*, which took the subtitle
switch - including the way to turn subtitles off - away from the most common kind of release. The rule is now split
into `HasAudioChoice` and `HasSubtitleChoice`, each gating its own pair of controls, and an input that announces its
streams late is asked once more when the media first reports a length. Measured live over the server's own stream: the
OSD offers `Subtitles: no subtitles` and no sound button, exactly as the two tracks deserve.

Two tests were found guarding an icon set that a later batch had already finished - the music rail's genre, track and
album vectors exist on disk now, so asserting they do not was only asserting the past. They assert the finished set
and, unchanged, that no shortcut ever borrows the PNG lying beside its vector.

### cp. The foot of a film wears marks, and only the ones that do something

The playback display was a row of words - "Pause", "Audio: 1 of 2", "Subtitles: Off" - which is a form a keyboard
screen wears and a television does not: the words are wider than the remote's buttons, they cover the film they
describe, and half of them describe things the film cannot do. The foot is drawn now, on the model of the players
everybody knows (the web player's row of round marks under the picture, the television player's mark that names
itself when the cursor reaches it), out of the reference's own `images/PlayerControl`. Eighteen vectors came over:
play, pause, the two jumps in both sizes, stop, eject, the four volumes, subtitles and its struck-through twin,
full screen and information. Three were left behind on purpose - `skip_next`, `skip_previous` and `repeat` - because
this front end owns no playlist and no repeat, and an arrow promising the next title is a worse answer than the
space where the arrow would stand.

What a mark may say is the same question as whether it may stand at all. The transport pair flips (it offers to
pause while the film runs and to play while it waits); the speaker says how loud it is rather than what pressing it
does, because that is what a picture of a loudspeaker is for (struck through at mute or silence, a bare cone at a
whisper, one wave at half, three at full); the jumps wear the shape of the viewer's own instant skip - a ten when
the saved answer reaches 20 s or less, a thirty otherwise - and say their real worth in the caption, which matters
because the saved answer is a percentage and twelve seconds of an hour is not twelve seconds of a short film; and
the sound pill wears the *language* rather than an icon, because for a sound the language is the whole answer. At
rest a mark is a glyph on a dark plate; under the cursor the plate turns to light, the glyph turns dark, and the
mark's name appears underneath. Holding a jump or a volume mark repeats it, the way holding a remote does.
Pressing anywhere on the position line moves the film there, which is refused for a stream that never reported a
length, and the full screen mark flips the saved window mode through the shell rather than the window's own.

Nothing unusable is drawn, which is the rule the whole batch hangs on: `CanTransport` for the transport and the
volume, `HasAudioChoice` and `HasSubtitleChoice` each for their own switch, `CanInform` for the information mark -
a file opened by path has no library record to read, so it gets no information mark rather than one that opens an
empty panel - and with nothing loaded the foot keeps only the two marks that always mean something, the way out and
the way to the whole television. A hidden mark is also not a stop in the remote's ring, so a film with one sound
walks from the volume marks to the subtitle switch with nothing in between, tested at both ends.

The shipped skin answers `NowPlayingOverlayTemplate` with a strip of its own - title, position, one bar, the state,
the volume - and the view's reading steps back for it. It took a run at a real film to notice that the view had
been asking the wrong question: it asked the skin's *registry* whether the template exists, which is answered
before the control holding the template has been served, so its own name-and-position-and-bar stayed up next to the
skin's strip. Two progress lines for one film is the exact thing the skin's bar exists to prevent, and neither the
tests (no skin worn) nor the eye on a static picture said so. `SkinTemplates` now answers for the control it serves
(`WearsSkinOverlay`), and the live UI tree of a playing film lists one `ProgressBar`. Measured over the server's own
stream at 02:03: the tree lists `'Skip back'`, `'Pause'`, `'Skip forward'`, `'Volume down'`, `'Mute'`,
`'Volume up'`, `'Subtitles: Forced - [Deutsch]'`, `'Full screen'`, `'Stop'`, `'Close'` - no sound pill, because this
stream announced one sound - and a click on `'Skip forward'` moved `0:22` to `10:19` of `49:41`, which is the saved
fifth of the title and not a fixed half minute.

Two things the suite surfaced on the way, both real. The rebuilt screen had quietly stopped asking for four palette
keys, and the rule "a key every palette carries is asked for by some screen" caught it one key per run: a pressed
mark now wears the palette's `PanelPressedBrush` (which is the colour every other button on every other screen
answers a press with), its glyph the palette's `ButtonTextBrush`, the position line's trough `VideoOutlineBrush` -
which is what the reference's own WMC templates use that key for - and the picture's backdrop `VideoBrush`. And the
guide's cursor drifted: walking between channels re-anchored on the *beginning of whatever programme the cursor was
standing on*, so on a channel whose evening is made of two-hour films three steps down the list landed the viewer an
hour away from the time they had been looking at, on a programme that had never been under the cursor. The grid
keeps the instant the viewer chose - by pressing a programme or by walking along a channel - and vertical movement
moves the channels past it, which is the reference's time column doing what a column does.

### cq. The foot was measured rather than guessed at, and the two jump marks got their faces back

The row built in `cp.` was still the screen's own invention, and it read like one: the marks did not stand on one
line, the lower area kept the strip it had always had, and pause was one grey glyph among eleven. So the row was
measured before it was drawn again. The web player was opened at window size and at full screen and its own boxes
were read off the running page: the foot is 56 px of chrome glued to the bottom edge with 3 px of padding, the
position line is full width and 6 px, sits *above* the buttons (its `y` is 1234 against the buttons' 1248) and
grows to 12 under the pointer, its knob is 12x12, the transport is the first thing on the left at 40x40 followed by
the volume area and then the clock in 14 px type, and none of those numbers grows when the player is put on a
2531x1296 screen. The television player's design guidance says the same two things about a room: focus should
"gently highlight and expand" a thing, and a word must stay legible from across the room. Scaled to this
front end's 3440x1440 target that is a plate of 88 in a column 112 wide, a title of 34, a clock of 26, a caption of
19, and a position line of 16 the moment the pointer arrives.

The band is now: a scrim that is transparent 76 px above the foot and near-black at the screen's edge; the film's
name, a state pill and the clock on one line; one position line stretched the whole width with the playhead as a
knob; and under it a row of mark columns, each carrying its name whether or not anything stands at it, because a
name a viewer has to arrive at with a mouse to see is no answer on a television. A mark at rest is a glyph over
nothing; reached for, its plate turns to light and the whole column grows 7 %, which is the "gently highlight and
expand" the guidance asks for. Transport keeps the same measure as every other plate - a bigger circle would push
its own name out of the line the names stand on - and is the one plate already lit at rest, because it is the mark
everybody looks for first.

That also settled the question `cp.` left open. The shipped skin does answer `NowPlayingOverlayTemplate`, and the
answer is a slab of its own - title, position, bar, volume - which is what the band was already drawing; wearing
the skin's strip is what made the lower area look untouched and the film read twice. So the playback screen stopped
asking the skin for it and `WearsSkinOverlay` went away again; the key stays answered in both skins, because a
skin that offers a template should not be broken by a screen that stopped using it, and one film has one position
line, tested.

Two real bugs came out of measuring rather than looking. The screen still ran the old habit of writing the instant
skip's worth *onto* the two jump buttons (`backButton.Content = "-20 %"`), which replaced each of their plates,
glyphs and names with a bare word - that is the whole of why those two marks looked broken, and no screenshot of
the row as a whole had made it obvious. The reach is said in the caption the view model already writes ("Rewind
20 %"), so the writing stopped. And the test that found it had been asking the *window* for a button named
`backButton`, which is also the television guide's name for its half-screen scroll; a name lookup now starts from
the playback screen, which is the screen under test.

The name of the film was wrong too, and everywhere: the band read whatever `ShortName` made of the location, which
for a streamed library title is the whole address of the server's stream endpoint. `Play` takes the name the viewer
picked the title by, `Title` says that, and `FileName` says which file it is made of - read underneath, and only
when it is a file this machine can actually name and it differs from the title, so a stream invents nothing and a
file opened by hand is not named twice.

Measured live again over the server's own stream: the ten marks the stream deserves are all `112x115` at `y=1299`
- one line, by measurement rather than by eye - their captions at `y=1391`, one `ProgressBar` in the tree, the
clock above the marks, a click on `'Skip forward'` moving `0:26` to `10:23` of `49:41`, and the picture's own
channel spread averaging 6.5 with peaks at 82, which is a film in colour rather than the grey one this batch began
with.

The mockup that arrived with this round asks for four more things - chapter marks, a preview thumbnail for the
chapter a viewer is standing over, the `4K / HDR / Dolby Vision` chips, and picture-in-picture. Three of them were
left undrawn there because nothing honest had ever reached the front end to draw them from, and `cr.` is the batch
that went and asked the engine for the two of those it could get. The third - the HDR chip - still has nothing
behind it, and picture-in-picture is still no feature of this player. The file name line and the circular transport
from the same mockup were real and were drawn.

### cr. The panel that lies over the film stopped being a container and became a band of shade

The playback panel went through three backdrops in this batch and kept the cheapest. The mockup asked for the
controls to sit on frosted glass, and the first attempt painted that frosting by hand: the surface averaged the
picture it was holding down to ninety-odd points, blended them towards the glass's own blue and handed the result
back as a brush. On screen it was right - the light of the film came through the panel, and nothing in it stayed
sharp enough to read - and on the television it cost the film its frames, because sampling a moving picture in
managed code and re-uploading a brush is work a machine that is also decoding the film cannot spare. The
compositor's own acrylic was the second attempt, the same surface the glasses of the start screen stand on, and
it too was taken out again: it cannot be seen through the screenshot path this batch verifies with, and it is a
shader running full-screen behind a panel that only appears for four seconds at a time.

What stands there now is a band of shade (osdArea in "NowPlayingView.xaml"): black at four fifths over the bottom
edge of the screen, thinning to nothing as it climbs, with the buttons standing on the film rather than in a box.
Two colours and a line, and the film keeps its whole picture. Measured live over the server's own stream: the
transport at `y=1186`, the two picker circles at `58x58`, the sound line at `1597,1342,260x40`, and the band doing
exactly what a band of shade should - the film above it reaching `212` in its brightest channel while the same
content under it is held down to `87`, so the words stay readable over a bright scene without the picture
disappearing behind a plate.

The volume line gained its own bug and its own fix. It answers a press anywhere along it, but the control it lives
in is a button, and a button takes the press for its own click and marks it done with: the first live click landed
in the middle of the line and left the sound at `100 %`. The line now asks for the routed press with the handled
flag honoured ("NowPlayingView.xaml.cs", the `AddHandler` beside `volumeRow.SizeChanged`), which is also why the
markup no longer carries `PointerPressed`. Measured live after that: the same click at the middle of the line
leaving `50 %` beside it, and one test ("TheLineOfTheSoundAnswersTheHandWhereverItPoints") raising a press that is
already marked done with, so the difference cannot quietly come back.

What the batch learned about Avalonia 12 on the way, since each of these cost a build:

- `ImageBrush` has no `ImageSource`; its picture is `Source`, of type `IImageBrushSource`.
- `RoutingStrategies` knows `Bubble` and not `Bubbled`.
- `AcrylicSurface` renders nothing through `RenderTargetBitmap.Render`, which is every screenshot this project
  takes - so a control that frosts by shader cannot be verified by the tool that verifies everything else here.
- A `Button` with no content and a transparent background reports an empty rectangle to UI Automation, which left
  the harness no place to aim a click; the painted track, its filled part and the knob are therefore the button's
  own content, and the rectangle came back as `260x40`.
- An `Image` stretched to fill asks for the whole room it is offered, so a frosted picture laid *inside* the
  bottom-anchored panel pushed the panel to the top of the screen; a background brush takes no room and follows
  the corners of the plate it belongs to.

Left undrawn from the mockup, unchanged from the last entry: the chapter thumbnails, the `HDR` chip and
picture-in-picture, none of which has anything honest behind it in this player yet.

### cs. The row over the film was measured in millimetres, and the light it gives was wrong twice over

Nine complaints came back from the panel, all of them about size, shape or a control that was not there: the two
jumps forward and back had gone missing again, every mark was to be thirty per cent larger, the two pickers were
to say *which* subtitle and *which* sound they stand on, the volume was to be moved out of the row (its left and
right keys were the same two keys the row's own navigation uses, so a viewer stepping along the row moved the
loudness instead), the position line was to be thicker, focussable and pressable anywhere along it, the chapter
marks were missing, and one focus effect for mouse and keyboard was wanted rather than two.

The row is now seven marks - back, back-ten, play, forward-ten, forward - standing 70, 70, **99**, 70, 70 px, the
two chapter jumps among them and dimmed when there is nothing to jump to: a row that changes its length between
two chapters is a row nobody can aim at. Under them the position line is one button 40 px high whose painted bar
is 12 px, with the times 104 px wide at either end and the chapter marks 22 px standing on it; along the bottom
the two pickers read "Subtitles: Forced - [Deutsch]" and "Sound: Titel 1 - [Deutsch]" and are 311 and 301 px wide
against the 70 px of an icon, with a chevron 49 px high. Measured live over the server's own stream: a press at
three quarters along the line moved the film from 0:20 to **37:19** of 49:41, and the loudspeaker opens a dialog
of its own - "How loud", quieter, a 358 px line, louder, mute - where a press at two fifths left the mark reading
"Volume 40". The chapter marks are there and dark because the file has none: "vlc: the media carries 0
chapter(s)". A file with chapters lights them, which is what the panel's own tests draw.

Two of the nine were bugs nobody could see from the markup. A `Button` does not stretch into the star-sized room
it is given: the film's position line and the line of the sound both came back `align=Left`, so the line the
viewer is asked to press along measured its own knob - 200 px and 22 px in a 3440 px window. Both styles carry
`HorizontalAlignment=Stretch` now, and the test that failed is kept as `TheLineOfTheFilmIsStoodOnAndMovedByTheHand`
with `bar.Bounds.Width > 600` in it. And the light was wrong, twice over, both times invisibly to every test:

- **The theme dresses the inside of a button, not the button.** Avalonia's Fluent button styles set
  `Background` on `/template/ ContentPresenter#PART_ContentPresenter`, which is drawn *over* the button's own
  background and carries the theme's own small corner. So the glass drawn here was covered: the buttons measured
  70x70 with a radius of 35 and were seen as grey tiles with a corner of 4 - at rest, and again under a pointer,
  and the ring's blue only showed because the theme paints nothing for `:focus`. Every class of button on this
  screen now hands its own dress down to that presenter with `{TemplateBinding}`, which follows the button
  through its states; `TheInsideOfTheMarkWearsWhatTheMarkWears` fails the moment that mirror is taken out.
- **The worn skin outranks a shorter selector.** `Skins/Nereus/themes/default/styles.xaml` dresses *every* button
  of the program under a pointer with `Window:not(.pointer-resting) Button:pointerover`, and that selector counts
  more than `Button.glass:pointerover`, so the raised panel of the skin was painted over the glass. The pointer's
  half of each of this screen's five lit states is written the same way the skin writes it - gated on the window
  wearing `pointer-resting`, and long enough to win - which also means a button the invisible pointer was left on
  stops claiming somebody stands at it, exactly as the skin's own rows do. `Button.glass:focus` stays ungated,
  because a ring has to stay lit when the hand is put away.

Proving the last one took the harness apart: UI Automation reports the panel's names and rectangles while the
panel is hidden (`osdArea` is switched with `IsVisible`, and the peers stay), so four photographs in a row can be
of the film with a tester convinced it is the panel. What settled it was an ASCII map of the mark's own 70x70
rect read off a real desktop screenshot - rest gives a circle and a glyph, the pointer moving over it gives 2 271
blue pixels and 482 pixels of the ring's own `#8EC5FF`, and the ring standing on the same mark gives 3 715 and 680
- the same light, measured. (The tooltip the mouse calls up covers the mark's left half, which is why the
pointer's counts are the smaller pair.)

What this batch learned about Avalonia 12, since each cost a build:

- `StyledElement.PseudoClasses` is protected, so no test can put a control into `:pointerover`; the states can
  only be compared as written (twins) and on the real screen.
- `TemplatedControl.BorderBrush` is an `IBrush`, not a `IPen`.
- Controls inside a container that has never been shown are absent from `GetVisualDescendants()`, so the volume
  dialog has to be opened before its line can be asserted on.
- A headless window's default width leaves a dialog's line a hand's breadth; the two volume tests size their
  window like a television first.
- `Avalonia.Headless` 12.1.4 has no synthetic mouse at all, which is why hover is measured on the desktop and not
  in a test.

Left undrawn, unchanged: the chapter thumbnails, the `HDR` chip and picture-in-picture.

### ct. Only what works is drawn, and the ring went white

Asked after the measurement pass above, with the real stream running: no tooltips, no frame round the position
line, no blue in the frames, no frame round the name of the film, no chapter buttons that do nothing, and the
wrong marks on the two jumps.

- **Tooltips are gone** - all eighteen of them, off the whole playback screen. They were not a nicety: measured on
  the desktop, one rose about a second after the pointer arrived and covered the left half of the mark it
  explained, which is the half a viewer is aiming at. What a mark is called stands in `AutomationProperties.Name`
  now, where a reader and a remote read it without anything being covered, and the two jumps carry their distance
  there ("Rewind 30 s") instead of in a grey box. `NoMarkOfThePanelCarriesAWordOverIt` walks every control of the
  screen and refuses any `ToolTip.Tip`.
- **The frames are white.** Every lit edge on this screen was `#FF8EC5FF`, the platform's focus blue, worn over a
  skin whose accent is teal; it is `#FFFFFFFF`, and the resting hairline `#665E788C` - a blue-grey - became
  `#40FFFFFF`. Measured off the real screen: a focused pill with 3698 neutral pixels against 10 blue-tinted ones,
  a focused circle 945 against 0, mean brightness 251 to 253.
- **The name of the film is a name.** It lit and grew a frame like the marks beside it while being no mark of the
  row; it is `Focusable=False`, and every rule that dressed it is deleted. A press of it still says what the
  library knows, for the one hand that presses names. `TheNameOfTheFilmIsNotStoodOn`.
- **The position line draws no frame.** It is the widest control on the screen, so a box round it is a frame over
  the film rather than a mark on a control; the knob of the playhead grows instead, 22 -> 34 px, the same number
  under a pointer as under the ring. Measured on the desktop: 22x22 px at rest, 34x34 px with the ring standing on
  the line, and 872 white pixels in the whole band of the line - the knob alone, no frame anywhere. The line of
  the sound does the same in its dialog. `TheLineOfTheFilmGrowsItsKnobRatherThanAFrame`.
- **The chapter jumps are drawn when there is somewhere to jump** - `CanChapterBack` and `CanChapterForward`
  answer their visibility instead of leaving them standing dimmed. A control that answers nothing is worse than
  the space it would stand in, which is how the marks on the line and the two pickers of tracks already work; on
  a file with no chapters nothing called "chapter" is left in the tree at all.
- **The two jumps wear the skin's rewind and forward** (`fast_rewind`, `fast_forward`: the two triangles) and not
  `replay_10` / `forward_10`. The numbered round arrows were the wrong picture - the button jumps once by the
  viewer's own distance rather than winding a running tape back - and their figure is a smudge at the size a
  television reads. `FootMarkIcons` went with them, so the vector catalog and the "lands inside the square" test
  vouch for the marks that are drawn; `NearestSkipMark`, which picked between 10 and 30, is gone with them.

22 tests in the playback screen's own file (three of them new) and 1202 in the frontend's suite, all green.

### cu. Marks stand in the middle, lines wear the skin's colour, and chapters are drawn

Asked after the entry above, on the desktop with a file that carries chapters: the two jumps' marks sat off
centre in their plates, the filled part of the position line was still blue, the playhead jumped when the ring
came to it, and four chapters' marks were missing from a line that should have shown them.

- **The toolkit's `Stretch=Uniform` centres nothing.** `Shape.CalculateSizeAndTransform` is
  `translate(-bounds.Position) . scale(min(sx, sy))` - there is no centring term, so a glyph whose box is wider
  than tall piles every spare pixel of its plate *under* the figure. That is what the measurement said:
  `fast_rewind` 7 px too high ((34 - 19.9)/2), `volume_up` 2 px ((34 - 30.9)/2), `pause` 2.5 px left
  ((44 - 40.6)/2), and the four square glyphs exactly 0 px - the signature of a top-left alignment, not of a
  misdrawn path. The reader now squares a glyph's box about the middle of its ink (`SquaredAboutItsMiddle`, two
  inward sub-pixel marks, size untouched), which is right under either convention. Measured off the real screen
  afterwards: rewind 0/0, forward 0/0, pause -0.5/-0.5, volume 0/0, settings 0/0, chapter jumps 0/0.
  `TheShippedGlyphsStandInTheMiddleOfTheBoxTheyAreFittedBy` refuses any shipped glyph whose box's middle is not
  its ink's middle, or whose box is not square, or whose side is not `max(w, h)`.
- **Both lines fill with the skin's accent.** The position line's filled part took the platform's blue through
  the progress template; it is `AccentBrush` now, like the knob and the frames. Measured in the band of the
  line: 1758 pixels of `0,144,144` and no blue-tinted ones left.
- **The playhead grows and stays where it stands.** Resting 908-923 px of the screen, lit 901-930 px, so it
  grows 22 -> 34 px with both centres at 915.5 px: an eased `TransformOperationsTransition` on a knob whose
  growth is symmetric, which is what "no jump" means. `TheLineOfTheFilmGrowsItsKnobRatherThanAFrame` reads the
  scale rather than trusting a single pump.
- **A film without chapters has none, and a film with them had them drawn wrongly.** The file played so far is
  Matroska with no `Chapters` element (0 by `ffprobe`, 0 by the engine's own log), so there is nothing to draw
  and - since the last entry - not even the two chapter buttons. That part was the honest answer. It was not the
  whole answer: on a file with 5 chapters the marks appeared as a stack at the *start* of the line. They were a
  list's containers, and the toolkit takes containers back when the film turns over; the `Canvas.Left` a mark
  had been given went back with them, and the log caught the marks being placed correctly (571, 1191, 1811,
  2479) and found again at nought several passes later. The marks are children of the canvas itself now, which
  nobody hands back. Measured live: 764, 1385, 2005, 2673 px against 763, 1384, 2004, 2673 expected, and the
  same four numbers 18 s and a chapter later.

15 geometry tests, 22 in the playback screen's own file (one new, which closes the panel and calls it back over
the marks) and 1203 in the frontend's suite, all green.

### cv. No knob is cut any more, and the sound's dialog reads as a player

Asked after the entry above, with the sound's dialog open on the desktop: its knob was sliced off at both ends of
its line, and the dialog still carried a heading and a worded Mute button where a player would show a mark.

- **The toolkit cuts a control's picture at the control's own bounds.** Both line controls paint nothing, so
  that never showed - until the knob of a line grew from 22 to 34 px: six pixels at either side of the growth
  fell outside the control whenever the knob stood at the end of its line, and a whole knob was drawn as half a
  knob. Measured off the real screen before: 28 px of a 34 px knob at 100 %. The knob's own placement is clamped
  to the line (`left = at - knob/2`, clamped to `[0, width - knob]`), so the growth had nowhere to go. Each line
  control now holds its line ten pixels in from its bounds (`Padding="10,0"`), and because a press is measured
  against the panel rather than the control, the press, the painted line and the numbers all moved to the same
  shortened line. Measured after: **34 px wide and 34 px high at 100 %, and the same 34x34 at 0 %** - a whole
  circle at either end. The corner radius the two controls carried is deleted with it: rounding on a control
  that draws nothing does one thing only, which is cut.
  `TheGrowthOfAKnobStaysInsideTheLineItStandsOn` stands the knob at both ends of both lines and refuses any
  growth outside the control - it was checked by taking the padding back out, which fails it.
- **The mute is a mark, and it stands before the quieter mark** - `PlayerControl/volume_off`, the struck
  loudspeaker the reference draws for the same button. A word among round marks is the one thing on the row that
  cannot be read from a sofa, and it was the last word left in the dialog. `MuteCaption` still says "Mute"/
  "Unmute" where a reader and a remote read it.
- **The heading is gone and the number stands over the line it counts**, in that line's own column rather than
  in a row of its own - which is the difference between centred over the line and centred over the dialog:
  measured centre of the number 1770 px, centre of the line 1770 px.

23 tests in the playback screen's own file (one new) and 1204 in the frontend's suite, all green.

### cw. The sound's dialog stands upright at the right hand side, above the band

Wanted: the contents upright (the percent on top, then the louder button, then the line turned
by 90 degrees, then the quieter button, then the mute button at the foot), and the dialog at the
right with a margin, held to the bottom but well above the on screen display. - The line is now a
56 x 220 field with `Padding="0,10"`: the run fills from the foot (`Height` instead of `Width`),
the knob hangs from the run by its own lower edge (`Margin.Bottom`), and the percent sits in the
middle of the field above the line. The mute button is a 70 x 70 area that grabs the circle right
at its edge, like the buttons in the foot do. The buttons wear the `upright` style, because a
standing line has no edge one could turn towards "louder". - The line is aimed by its height now
(`1 - Y / height`); the road from the line to the quieter button and the reading "three quarters
down means 25 percent" are both written down. - The dialog sticks to the bottom right
(`HorizontalAlignment="Right"`, `VerticalAlignment="Bottom"`, `Margin="48,0,48,24"`) and
`PlaceVolumeDialog()` lifts it by the kept height of the display plus 24 px; the display reports
that height over `SizeChanged`, because it only learns the button heights from the skin's
measurements later. - Measured live (screen 3440 x 1440): the line is 56 x 220 at x 3302, and
from the top come the percent (y 450-477), the louder button (489-559), the line (571-791), the
quieter button (803-873) and the mute button (885-955); the display's buttons only start at
y 1124 where the dialog ends at 973, so 151 px of air lie between them. The knob measures
34 x 34 px at the foot as well as at the head and therefore stays whole inside the field (field
571-791, knob at 753-786); the filling runs from the foot upwards in the skin's accent colour
(R0 G149 B146). `1205/1205` frontend tests and `24/24` window tests green.

### cx. The animation reads the library, and the first key walks the menu

Two complaints about the way in: the first key press after the machine woke did nothing, and the home
screen stood half-built for a moment after the animation had ended. Both were measured on a shown
screen rather than guessed at, and both came from the same corner.

The hitch was the reading itself. The library was only asked for by `HomeView.OnScreenActivated`, which
is the moment the animation is already over: the read took 941 ms to the menu and the sections and the
wall 273 ms more, all of it under the viewer's eyes. So the wake says the word instead -
`StartupViewModel.WarmLibraryRequested`, raised by `AskForTheLibrary()` from `WaitThenFinishAsync()`
(whose wait is the animation's own seconds) and from `CheckAsync()` once the four numbers were accepted -
and `App.WarmLibraryOnce()` reads the library there. Nothing asks twice: the home screen keeps its
answers when `HomeViewModel.ReadUnderThisViewer` says the shelves were already read under the person
standing here (or are being read under them now), and so does the household's change of eyes, which
until now fired a read of its own behind the wake's - that one measured 1788 ms and cancelled the
reading that had already brought the answers. Measured on the shown screen: the read starts while the
ring turns, the menu and sections stand 297 ms later and its wall 49 ms after that, all of it before
the home is activated; the first heading of the wall appears 5 ms after the wake steps out, where it
had been 1214 ms after it before.

The dead key was the focus. The wake holds the focus, and the read the screen asked for emptied the
menu and filled it again - the row standing under the focus went with the list it stood in, Avalonia
answered by putting the focus on the window, and the next arrow was spent walking back into the window
instead of along the menu. Live, before the fix: focus on `ShellWindow` when the keys were pressed, the
first one landing on the first row and only the second moving anywhere. So the screen takes its menu
back whenever a reading left nothing but the window standing on the screen
(`HomeView.TakeTheMenuBackIfTheRebuildThrewItOut`, asked on the `MenuRebuilt` the read raises), and a
row stays stood on. Live, after: a row holds the focus when the home appears (y 434) and the first
arrow moves it (y 611, then on along the rail as the wall follows each entry).

Four tests keep both: `TestStartup.TheLibraryIsReadUnderTheWakesAnimation` and
`.AWakeGivenItsNumbersReadsTheLibraryOnceBeforeItOpens` on the reading's moment and its being asked
once, `TestHomeWindow.AScreenOpenedUnderAReadingAlreadyMadeForThisViewerKeepsItsAnswers` on the screen
not asking a second time (and asking when the name did change), and
`.TheMenuStandsBackOnItsOwnAfterTheServerHasReadAgain` on the focus - which was checked to fail with
the new call taken back out, the focus then standing on nobody. Frontend suite 1209 tests green.

### cy. The wake keeps the animation's seconds and nobody hangs the skin twice

The complaint after `cx.` was honest: the animation felt longer and the wait after it was exactly as long as it
had been. Measured on the shown screen, both halves were true, and neither was the reading's fault.

The animation felt longer because two readings were being made at a start. The machine reads its library as soon
as it has a server, and the wake - which by then had decided who was watching but had not said it out loud -
asked for another one under that person's name; the second cancelled the first and the ring paid for both, 557 ms
of a beat lost in one run. So the wake's ask is the only ask now: `App` listens for `WarmLibraryRequested` and
reads on the spot, but not before the eyes have been checked, and a reading that stands was made under other eyes
is read again rather than stepped out on, because what a restricted person may be shown is not what the whole
household may be shown. Measured: one reading, 246 ms, its answers standing half a second into the ring's turn.

The wait after the animation was two numbers nobody had looked at. The wake's own seconds were four while the
ring's turn is two and a half, so the wallpaper stood alone for a second and a half for nothing; and the answer
hung the skin anew - `FrontendTheme.Reapply`, 586 ms of it - over a name that was already the name standing
there, because the machine opens on the person it remembers and that is almost every start. So the wake's floor
is the animation's now, `StartupViewModel.WAKE_SECONDS`, the ring's turn and its way out, shared with the screen
that draws the ring rather than counted twice; and a name that has not moved in leaves the skin where it hangs.
What may lengthen the wake past its turn is the library still reading, and only up to `ReadPatience`, after which
a silent server is a complaint on the library screen instead of a television parked on its wake. Measured: the
ring stood 2.68 s and the library took the window 206 ms later with its wall already up, where the answer had
taken 777 to 855 ms to reach the screen and a further read of the shelves was filling itself in after that.

Six tests keep this: `TestStartup.AWakeWhoseShelfFillsLateHoldsItsScreenForIt`,
`.AWakeIsNotWeldedShutByAShelfThatStaysEmpty` and `.TheWakesOwnSecondsAreTheRingsTurn` on the wake's wait,
`TestHomeViewModel.TheWakeIsToldWhenTheWallStands`, `.AReadThatEndedInAComplaintStillSaysSo` and
`.AScreenWithNothingToReadSaysNothingIsComing` on the word the wake listens for - which is said by the wall
going up, not by the read returning, so the home screen is never opened on a wall still on its way in.

### cz. Covers are decoded the width they are drawn at, and a backdrop is asked for once

Walking from Videos to Series and back was measured with a sixteen millisecond beat watcher over the dispatcher,
a wall hand-over stamped with what each of its three parts cost, and every picture decode timed - all of it read
out through the log lines the run wrote, then removed again. The baseline over ten hand-overs on a 3440x1440
window and the real household server: 154 decodes and 3506 ms of them in one two second beat, worst gap between
two beats 210 to 510 ms against an idle mean gap of 24 ms, which is the Windows timer's quantum and not a stall.

Three numbers explained that, in order of what they cost:

- **A cover was decoded at the size the library holds it.** A poster from the fanart cache is a 2000x3000 scan,
  twenty-four megabytes of pixels once decoded, seventy to ninety milliseconds of the thread that draws - and a
  wall realises seventeen to nineteen covers in one beat, so every step along the rail decoded a wall again. The
  decoded pictures could not hide either: one whole scan is larger than the artwork cache's entire budget, so
  each cover pushed the previous wall's covers out. The templates say what they need now - `ConverterParameter=wall`
  on the wall's covers, six hundred and forty pixels because the two by two at a band's head is drawn five hundred
  and seventy-three across and the focus morph steps a tenth nearer, and `ConverterParameter=tile` on the pictures
  screen's rows and tiles - which leaves nineteen decodes and 476 ms for a whole run of ten hand-overs, and none
  at all once both walls have been seen.
- **The backdrop was asked for again on every hand-over, and could not be remembered.** A screen wears one picture
  behind everything, but the binding is re-evaluated every time a wall is handed over, and each of those asked for
  the 3812x2144 scan to be pulled out of the file anew: 76 to 93 ms per hand-over, twice over when a viewer
  alternates. Asking for it at a smaller width was tried and measured *slower* (a JPEG decoder hands out halves
  and quarters for free and resamples anything else by hand - 106 ms at two and a half thousand pixels instead of
  80 at the full scan), so the answer is the picture instead: `SkinFanartConverter` keeps the one backdrop it has
  decoded, and `ArtworkConverter.Hold` no longer remembers a picture too large for the whole budget, because such
  an entry threw every cover of the standing wall out on its way in and was thrown out itself by the next picture
  asked for. That second half was the reason the covers kept needing their seventy milliseconds back.
- **A blurred backdrop was blurred out of a whole scan.** Skia will hand out a quarter of a picture by itself, and
  the sheet a blur works on is a third of the drawn size anyway, so the quarter is the nearest step above it: the
  blur's own pixels are unchanged and a quarter of the scan is read out. This one is off the drawing thread
  already, so it is work saved rather than a hitch, and it belongs to whoever turns the fanart blur dial up.

The budget the cache works off went from eight to sixteen megapixels once the poison was out, sized by what a wall
needs rather than by whole scans: a wall cover is six hundred and fourteen thousand pixels decoded, so twenty-six
of them are held, which is a wall and a bit.

What this leaves, measured and not yet touched: the wall's own fade waits two hundred and twenty milliseconds
before the new wall is put up (`fade 219 to 234 ms` in every hand-over of every run, and nine to fifty when the
wait was switched off by an environment variable for the measurement), the half second of settling before the
reading even starts, and the two hundred and eighty to three hundred ninety-seven millisecond gap that a wall's
realisation still costs while `RaiseWall` fires three or four times per hand-over because the bands are put on one
by one. Once both walls are warm the beats between hand-overs sit at 23 ms mean and 43 to 130 ms worst, which is
the timer again; the hitch that is left is the hand-over itself.

Two tests keep the first of these: `TestCoversDecodeAtTheirTileSize.EveryCoverADrawersLaysOutNamesTheWidthItNeeds`
reads the templates - home, pictures screen, and both skins' - and names any cover asked for without a width,
because nothing at run time would notice, and `.TheWallCoversAskForTheWidthTheWallDrawsThemAt` pins the wall to
its own number.

The clean suite afterwards brought four screens up failing - `TestMediaBrowserTiles.ABannerShelfShowsBannersAndNot
ResizedPosters`, `.ACoverWearsThePosterEvenWhenTheGridWearsBanners`, `.TheWatchedCornersObeyTheGridSetting` and
`TestTheRowAListStandsOnWearsTheSkinsColour.TheColourRowMovesTheRowAListStandsOn` - and they failed alone, in a
group, and with this phase's changes stashed away, which put the blame on `cx.` rather than here. The cause was
the name being read before the palette: on a development machine `client.json` holds whoever the house watched as
last, and a name redirects the skin settings to that person's own file, so a test that had just written
`skin-nereus.json` in a folder of its own was read out of `skin-nereus-gerrit.json` instead - a stranger's
answers, and a screen accused of being wrong. Every fixture already isolates the files it saves; the
application's own start was the one reader outside that rule, and it reads while the application object is made,
before any fixture of a test's own has had its word. So the isolation is said once for a whole run, in
`Tests.Frontend.IsolatedMachineSettings`, which points `MP2_CLIENT_FILE` at an empty file of the run's own unless
a caller named one - and the four screens read what they wrote again. The suite whole: 1217 tests, none failing.

### da. The way down belongs to the step, and a wall is not taken down twice

`cz.` left the hand-over's own two hundred and twenty milliseconds in front of every answer, and left them there
honestly: a wall that goes up in the beat its predecessor came down is a cut, so the view is asked to take the
standing wall down and to answer once that has been on the screen long enough to have been watched. What nobody
had looked at was *when* it is asked. The step is swallowed by half a second of settling first, and only after
that does the reading begin and, at its end, the fade. So through the first five hundred and thirty-five
milliseconds of every switch the screen kept showing a wall that had no business standing there, and its
successor arrived two hundred and twenty milliseconds later still.

The step says so now. `QueueContentForSelection` asks the view for the way down (`HomeViewModel.WallLeaving`)
before it starts the settling, and `HomeView.BeginWallSink` writes the curve the hand-over used to write - once,
because a wall already on its way down is not put down twice, and with a stopwatch, because whoever picks the
curve up afterwards wants to know how much of it the screen has not shown yet. `SinkStandingWallAsync` waits for
that remainder, which - the way down being two hundred and twenty milliseconds and the settling five hundred - is
nothing at all. Two cases still pay the curve in full and on the answer's own clock: a step onto the tile whose
wall is already worn, which is a re-read and not a hand-over, and a wall arriving behind an empty area, where
there is nothing to fade and waiting for a fade would only hold the newcomer back. `RaiseWall` ends the
accounting, so the next step starts a fresh way down.

Measured on the shown screen - 3440x1440, the real server, eight walks along the rail over two of the library's
real shelves, one build with
these two changes stashed away and the next build with them back in, nothing else moved. From the step to the
answer being read: 530 to 570 ms before, 518 to 558 ms after, which is the settling and the reading and is rightly
untouched by any of this. The fade that the answer waits for: 219 to 241 ms before, nothing after - by the time a
successor has been read its predecessor has been going out for five hundred and thirty milliseconds, twice the
curve's length. From the step to its wall standing: **769 to 840 ms before, 540 to 582 ms after**. And `capwin`
catches the difference where no number does: 205 ms after a step the wall is caught mid-fade, its covers sunk and
ghosted, where the same moment before showed it standing untouched and fully lit.

What is left of a switch is the settling itself (`CONTENT_SELECTION_DELAY_MS`, the half second that keeps a held
key from reading five walls on the way past) and the two hundred eighty to three hundred ninety-seven millisecond
gap a wall's realisation costs while `RaiseWall` fires three or four times per hand-over because the bands are put
on one at a time. Both are said here rather than quietly left out.

One screen keeps the arrangement honest: `TestHomeSelectionDial.TheStepOntoANewTileSendsTheStandingWallDown`
counts the asks, and fails as soon as a step waits for its settling again. The suite whole: 1218 tests, none
failing.

### db. A wall is answered once, and a shelf whose number stood is not measured again

A wall goes on band by band and `ContentGroups` is an observable collection, so every band was a notification of its
own, and the view answered each one on the spot. Measured over eight hand-overs: eight folds, twenty-eight raises and
twenty-nine transform writes, where eight walls were being put up. The fold is the `Clear` that opens a swap, which
leaves the count at nothing for one notification; the raises follow on one band, then on two, then on three, and only
then on the finished wall - so the screen was shown a wall being built, which is the one thing `PutUpWall`'s own
comment forbids, and the pane went out and came back four times in the beat that was meant to change it once.

The answer runs at the end of the beat that did the swapping now: `PutFinishedWallInLight`, posted once per arrival
by the one subscription the wall has left, so what it looks at is the wall the viewer is meant to see - one fold or
one raise, and one measurement. `ScaleNereusWall` additionally remembers the number it wrote and leaves the transform
alone when a shelf arrives on the number it already carries, because a fresh `ScaleTransform` is a fresh answer to the
screen and sends the whole shelf through the screen's measurement - and over those eight hand-overs the number never
moved (1,309 the whole time). Same machine, same library, same eight hand-overs: eight raises, one transform write, no
fold at all.

What this did not buy is said here because it was the reason for asking: the hitch did not leave with the churn. The
busy window a wall's arrival costs - the dispatcher's beats of 40 ms and over in the 800 ms after a wall goes up - sat
at 352 ms mean and 446 ms worst before, and sits at 337 ms mean and 398 ms worst after. Three layout passes less are
not three layout passes' worth of that time, so what is left is the realising of a wall at all - four lists of covers
with their bindings, their shadows and their pictures - and it lands in the beat its successor steps into the light.
The next question is therefore not how often a wall is answered but when its pictures are made: making them while the
old wall is still leaving, rather than in the beat the new one arrives, is where the rest of the wait lives.

Live: the home stands finished where the wake steps aside, and steps along the rail alternate two of the library's
real shelves correctly - with every tile looked up again before it is stepped onto, since a UIA element held over from
before a wall change points at whatever tile its recycled container carries now. That is also how two earlier runs
named a tile they were not standing on, and it is worth knowing before reading any measured "Videos to Series" above
this line: what those runs walked was two real shelves, whichever two the recycled containers were holding.

One clean-up came along too: `TestUserRules` deletes the run's own client file, and a save still on its way out of the
application holds it for a moment, which Windows calls "used by another process" - two screens of that fixture fell
over that way in the middle of this round's suite without having anything to do with a wall. The clean-up waits the
handle out. Suite whole: 1218 tests, none failing.

### dc. What a switch is actually waiting for, and the reference's answer that was missing

The guess that a wall's pictures are made too late was tested and is dead, and it died in one run each. Putting the
light a beat behind the content writes - which is what "build the pictures while the old wall is still leaving"
means - was measured twice: the beat posted at `DispatcherPriority.Render` runs in the *same* millisecond as the
content write, because Avalonia's layout pass is not queued between them, and an `UpdateLayout()` written into the
dark beat costs **0 ms, eight times out of eight**. There is nothing to build early. The 100-430 ms beats stay exactly
where they were, on the side of the light. Suspects were next and are no suspects at all: `home-tile-shadow` was believed unstyled because it was looked for in
`*.axaml` files only, while the Nereus face styles it in `Skins/Nereus/themes/default/styles.xaml` - a BoxShadow of
three right, four down and sixteen of blur under every tile's ground, `dd.` measures what it costs.

So the phases were stamped instead of guessed, and that is what showed where a switch's time really goes. Eight
hand-overs, every one of them from the step: **500 ms until the settling has run, 31-47 ms of reading, 0 ms of waiting
for the way down** (which `da.` bought), 31-47 ms for the wall going up. Sum 531-594 ms. Neither the TV server (its
three shelves answer in well under half a tenth of that) nor the picture of a wall is the wait; the wait is the
settling, and the settling is the reference's own number - `HomeMenuModel.UPDATE_DELAY_MS = 500`, which kept its pane
from being refilled for every tile a walker passed over.

Two things came out of that, both the reference's and both missing here. The first is that the reference does not wait
before it *reads*, only before it *answers*: `EnqueueUpdate` was fair for a skin refilling from disk on its UI thread
and would only have been a wasted question here. The reading is now asked for in the beat the tile was stepped onto,
into collections nothing on screen is looking at, and the settling and the reading run side by side; the reveal awaits
whatever the settling did not cover. Measured: 515-594 ms, and the answer is in hand before the settling ends.

The second is the branch that was simply not ported. `HomeMenuModel.OnSelectedItemChanged` answers an *explicitly
chosen* entry at once (`UpdateContent`) and only a tile the highlight had moved onto after the settling
(`EnqueueUpdate`) - and it needed the `EnableTouchDisplay`/`EnableMenuSelection` switch to tell the two apart, because
its menu could not see how it had been moved. This port delayed every selection. A rail that knows its pointer does
not need a setting: `HomeView` now hands a mouse press on a tile to `HomeViewModel.ChooseEntryNow()`, which cancels the
settling the pick interrupted and answers - the standing wall's way down still being waited for by `WallHandover`, so
nothing is cut in half. Registered with `handledEventsToo`, because the tile's own `ListBoxItem` marks the press
handled once it has selected itself. Measured with real injected mouse clicks on the shown screen, wall text read back
over UIA: **a picked tile stands 234-265 ms after the click** against 515-594 ms for a walked one, which is the way
down and the reveal and nothing else. A pick of the tile that is already standing answers nothing.

`_paneEntry` moved with it: it used to be written when the reading started, which with reading ahead of the settling
would have named a wall the screen was not showing yet; it is written when the wall goes up.

Suite whole: 1219 tests, none failing.

### dd. The wall frosted itself, in software, twenty times per frame

`dc.` ended pointing at rendering and stopped there, and the thing it should have named was in the file the user
tagged: `Skins/Nereus/themes/default/styles.xaml`. Searching for the shadow in `*.axaml` only had hidden it - the
Nereus face styles its tiles in `.xaml`, which is why a whole round of guessing went into shadows that were there all
along (measured later: taking every BoxShadow away moves the slow frames from 72 to 51 and their time from 5971 ms to
5140 ms, so they cost, but they are not the story). The story is `Fluid.Avalonia.Acrylic`, and its own README says it
in its first line: "A cross-platform, **software-rendered** acrylic / frosted-glass backdrop ... one SkiaSharp shader",
sampling the app's own rendered content "continuously and by default".

Two of its surfaces sit in the home. One is the shortcut glass - a handful of tiles that do not move. The other is
inside every cover and is shown by `IsVisible="{Binding HasNoPicture}"`, put there so that a cover whose poster has
not arrived would not be a bare black square. A wall arrives with seventeen to twenty-four covers and none of their
posters yet, so for the whole of the way up every one of those covers captured the screen behind it and blurred it on
the CPU, per frame. That is the 40-70 ms frame during a fade, and it is what `dc.` could not account for after the
churn, the decodes and the layout had all been measured innocent - and a layout pass measuring 0 ms was always the
clue that the cost was never in the building of a wall but in the drawing of it.

Eight switches at 3440x1440, frames slower than 40 ms counted with a 16 ms dispatcher beat:

| | frames &ge;40 ms | their summed time | &ge;150 ms |
|---|---|---|---|
| as it stood (shadows on, both frostings on) | 72 | 5971 ms | 4 |
| shadows away, both frostings on | 51 | 5140 ms | 5 |
| shadows away, cover frosting away, glass kept | 32 | 3265 ms | 4 |
| shadows away, both frostings away | 33 | 3096 ms | 2 |
| **as it stands now (shadows on, cover frosting away)** | **25** | **2855 ms** | 3 |

Read across the middle three rows: the cover frosting is the whole cost and the shortcut glass is nothing - 32 frames
with the glass kept against 33 with it off. Which decided it: the glass stays, the cover frosting goes, and a cover
whose poster is on its way stands on the tile's own `#222222` plate until the poster arrives. That plate is also the
more faithful picture, because the frosting was never the reference's - `NereusSkinSettings.FanartBlur` says so in
its own words, "This dial is not the reference's; it shipped no blur at all". Live check with the change in: a clicked
wall stands finished 325 ms after the click with every cover drawn and its shadow under it, and no pale plate anywhere.

`TestCoversDecodeAtTheirTileSize` grew the guard, because a frosting surface that is not there is invisible to every
run-time check - it only costs, exactly like a cover decoded at its scan's full size, which is what that fixture
already exists to catch. Suite whole: 1220 tests, none failing.

### de. The importer lost every picture it was given, twice

The wall came up with holes and the user named the films behind them: *Lilo & Stitch*, *Minions & Monster*, *The End
of Oak Street*. Their folders are on `\\HTPC-SERVER\Video\Movies` and do hold artwork; what they hold is
`Minions & Monster (2026) 1080p AAC-poster.jpg` and `…-fanart.jpg`, which is what TinyMediaManager writes, and no
plain `poster.jpg`. The reference matches a picture by its name **or** by the prefix `<media file>-<name>`, and for a
poster or thumbnail takes a file named exactly like the media item as well (`LocalFanartProvider.cs:105-151`); the
port had carried that rule for thumbnails only, so every other kind was searched for under names that TMM never
writes. `LocalFanArtSearch.MatchFiles` now walks the same three passes the reference walks - the plain names, the
names built from the media file, and for a backdrop whatever merely starts with `fanart`/`backdrop` - and does it
without `LocalFanartHelper.FilterPotentialFanArtFilesByNameOrPrefix`, because that helper feeds priority indices to
`Dictionary.Add` and throws on two pictures answering one prefix, and `fanart.jpg` beside `fanart-2.jpg` is one
film's two backdrops. Four folders of this library are of that shape, which is what the reported holes were.

Underneath that, the server log said the second loss 887 times over 459 distinct titles: `NFO Movie FanArt handler:
Error saving file image … ServiceNotFoundException: Could not find the … IImageTransformer service`. It was worth
measuring before touching, because it looked like a warning and is in fact the whole library: `<data>\FanArt` holds
408 media item folders and **no image in any of them**, only the `.mpcache` count files that make a listing look
populated. `TrySaveFileImage` decoded whatever an NFO referenced and re-encoded it to JPEG through the seam, asked
for the seam with `ServiceRegistration.Get<IImageTransformer>()`, and threw where none was registered - and the
headless host cannot register one, since `MediaPortal.Common.Windows`, which implements it, targets
`net10.0-windows`. Every picture that was found was therefore thrown away at the moment it was found, and every
picture on screen came from the search beside the media file instead.

Now a picture whose bytes already are JPEG or PNG is written as it is, under the extension those bytes are, judged
by the magic that begins them; only a format the fan art cache has no name for still asks the codec, and asks with
`Get<T>(false)`, so a host without one warns once per picture instead of dying 887 times with a stack trace. Naming
a file after its content is safe because nothing downstream trusts the name: `FanArtCache.GetFanArtFiles` lists
`.jpg`, `.png` and `.tbn` alike, and the API answers a picture with a media type read from the bytes - which is
precisely how `AudioTagFanArtHandler` has filed covers out of audio tags all along. It is also the only one of the
four weighed ways that keeps a transparent PNG transparent: a codec would have flattened its alpha onto a background
and re-compressed it, which is what the reference does on Windows today.

Left on the table, and stated where they will be found rather than here: multi-targeting the headless host onto
`net10.0-windows` for the GDI+ codec would be the first multi-target in this repo, would move every launcher path
and CI row, would change nothing on Linux, and would lose the transparency that is the reason those PNGs exist. A
portable codec dependency would additionally buy the shrinking of database thumbnails (`BaseInfo` → `TryResize`),
which stays missing on this host either way - both places say so now, `IImageTransformer` and the headless launcher's
own header. Eleven `.none` markers were taken out of the frontend's picture cache, the ones this had written for
those titles, so the wall asks again.

`TestMediaController` grew three tests for the naming passes and `TestFanArtFileImages` is new with seven for the
storing: a transparent PNG keeps its bytes and is named `.png`, a JPEG is not re-encoded on its way in even where a
codec answers, a GIF is not filed under a name that lies about it but becomes a JPEG where a codec was promised, and
a byte pair too short to carry a signature is not claimed to be a photograph. 177 tests of the fan art and API
fixtures, none failing; headless host and API build clean.

### df. The switches that decide whether a picture beside a file is looked at

The user asked for the settings next, and was right to: the reference asks about pictures in two places, and the
port had carried one of them. `ServerSettings.Client\Settings\Configuration\FanArt\` holds seven pages - "Maximum
number of posters/banners/backdrops/thumbs/clear arts/disc arts/logos" - and those seven have stood in this API as
the `fanart` section since the section table was written; measured on the running server they answer 1/1/3/1/1/1/1,
which is `FanArtSettings` and therefore what `FanArtCache` caps a title at. The second group had never been
carried: every extractor of the reference has its own window with **"Cache FanArt on a local share"** and **"Cache
FanArt on a network share"** (`ServerMovieMDEBrowseOffline.cs`, `ServerAudioMDEBrowseOffline.cs`, and the series and
video ones alike), and `MovieFanArtHandler.cs:122` shows what they are for -

```csharp
shouldCacheLocal = ShouldCacheLocalFanArt(locator.NativeResourcePath,
    MovieMetadataExtractor.CacheLocalFanArt, MovieMetadataExtractor.CacheOfflineFanArt);
...
if (shouldCacheLocal) await ExtractMovieFolderFanArt(...);   // ExtractAllFanArtImages + SaveFolderImagesToCache
```

- they are not a fine-tuning of an import that happens anyway, they are the import. Which is the reason nobody
noticed their absence: **the walk they gate was never ported at all.** All four extractors that ran it - film,
series, video, music - target `net10.0-windows` (TagLib, BassLibraries, OpenCvLib), so the portable server loads
none of them, and its deployed handlers stored only the bytes an NFO file pointed at. A media manager that had
filed `clearlogo.png`, `clearart.png` or `disc.png` beside a film saw nothing of it reach the library. Counted in
this library's folders before the change: 937 clear logos, 904 logos, 777 disc pictures, 708 clear arts among the
films. Counted in `<data>\FanArt` before the change: Logo 0, ClearArt 0, DiscArt 0, Banner 0.

`BaseFanArtHandler.SaveFolderPicturesAsync` is the walk now, in the common core rather than in a plugin because the
portable server needs it from two plugins: the folder of a media file, its pictures sorted by the names the
reference lists per kind, its `ExtraFanArt/` folded into the backdrops, and the file's own name allowed to carry
pictures (`the-matrix-clearart.png`) when the caller names it. It is called from the film reader per film, from the
series reader for the show and the season - with the reference's question to the folders themselves about whether
the folder above an episode is a season's or already the show's, because media managers file both ways - and from
the music reader, which already walked an album's folder but stopped at the cover. It stops there no longer: the
disc, the logo and the banner of a release are stored too, because "what is a picture used for" is a question a
household answers later and throwing pictures away during the import is the one answer that cannot be taken back.
Albums keep the reference's rename of a poster to a cover, and are filed under both names as before.

The switches are per reader, as the reference has them: `CacheLocalFanArt`/`CacheOfflineFanArt` in
`NfoMetadataExtractorSettingsBase` (so the film and series sections each carry a pair) and in a new
`AudioTagMetadataExtractorSettings`, whose section `audio_tags` appears beside the other readers'. Both names and
both defaults come from the reference - including its word "offline" for a network share, kept because that is the
reason a copy exists: the share might not be there later. Local off, network on. `ShouldCacheLocalFanArt`, which
the portable core always had and nothing portable ever called, is what answers them.

Underneath that, a crash inherited from the reference: `FilterPotentialFanArtFilesByNameOrPrefix` ranked every
match by the index of the name or prefix it matched and fed that rank to `Dictionary.Add`, so a folder holding
`clearart.png` *and* `the-physician-clearart.png` threw "An item with the same key has already been added" - and
that shape, a plain name next to a name built from the media file, is what TinyMediaManager leaves behind. Entry
`de.` worked around the helper in `LocalFanArtSearch`; the helper is what was wrong, and it now keeps a list of
rank-and-path with names outranking prefixes, so both pictures are two pictures of one kind and the cache's own cap
is what decides how many of them a title keeps. The test for it says so where it would otherwise be invisible:
before, that folder lost *every* picture of the title, because the throw was caught one level up and the whole walk
given up.

Measured on the running server, which is the only proof that counts here: `GET …/settings/sections` grew
`audio_tags`, and the two switches answered `cache_local_fanart False`, `cache_network_fanart True` - the
attribute defaults, handed out by the settings manager as designed. This library's shares are local, so
`PUT …/sections/{nfo_movie,nfo_series,audio_tags}` with `{"values":{"cache_local_fanart":true}}` answered True
three times, and the warming pass was started. Eighteen minutes into it, with the pass still running:

| kind | before | 18 min in |
|---|---|---|
| Logo | 0 | 618 |
| DiscArt | 0 | 492 |
| ClearArt | 0 | 455 |
| Banner | 0 | 537 |
| Thumbnail | 78 | 581 |
| FanArt | 988 | 1716 |

and the files themselves are honest: `Logo/Folder.logo.png`, `ClearArt/Folder.clearart.png` and
`DiscArt/Folder.disc.png` begin `89 50 4E 47`, which is the PNG signature and the alpha channel still being there -
folder pictures are copied as bytes, which is what entry `de.` established for pictures arriving as bytes and what
the transparent clear arts of this library need.

Left open, where somebody will find them: the reference also reads a **central artist folder** for a musician's own
art (`AudioFanArtHandler` walks `<artist>/<name>/` folders reached through an NFO directory), and this reader does
not; season-prefixed names (`specials-clearart.png`) are not read, only the plain names, said in the method's own
remarks; and the `.actors` folders beside films and episodes are still not walked, although `SavePersonFolderImages`
has stood in the core ready for it. 556 tests, none failing - `TestFolderPictureImport` is new with seven (one
picture per kind by name, pictures named after the file, the extra backdrop folder, an empty folder, an unreadable
folder, and the rank collision above), `TestPluginSettingsOverHttp` grew the two that pin the switches being
offered per reader and their defaults being the reference's.

## What cannot be verified without the user
Stated once so it does not have to be repeated per phase: audio passthrough and HDR on real hardware,DVB/TvHeadend against real multiplexes, the feel of remote navigation, visual taste, and a 40 TB library
import. These need hardware and judgement that are not in this environment, and they are batched into
one hand-off checklist per phase rather than dribbled out.

### dg. Which screen answers a picture question

The switches arrived, and the user looked for them, found them under *Plugin settings* and said the true thing about
that: **"these are not plugin settings, they belong to media settings."** The reference agrees in its own file.
`ServerSettings.Client\plugin.xml` registers `Location="/Configuration/Settings/Media"` (:93) and hangs every
picture answer off that one node - `/Media/FanArtSettings` (:246) for the seven maxima, `/Media/MovieMDESettings`
(:170), `/Media/SeriesMDESettings` (:199), `/Media/AudioMDESettings` (:137) and `/Media/VideoMDESettings` (:161) for
the "Cache FanArt on a local / on a network share" pair (`ServerMovieMDEBrowseOffline.cs` :187) - and nothing about a
picture anywhere under a plugin node. So what the port had been missing was not a setting but the reference's **tree
node**: which of the two screens a question belongs to.

A section of `PluginSettingsCatalog` now carries that as a second fact, `Section.Area`, with `SectionsOf(area)` and
`GET api/media/settings/sections?area=...`; both answers say which area a section is in. Only the grouping moved,
never the storage: the two switches of a reader still live in that reader's own settings class, which is what the
reference does, so two sections may name one class and a `PUT` still writes only the fields it was sent. `fanart` is
a media section, the pair is out of `nfo_movie` (nine fields, now seven) and `nfo_series` (eight, now six), and
`audio_tags` became `pictures_music` beside new `pictures_movie` and `pictures_series` - worded per kind, "Copies of
the pictures lying beside a film / an episode / an album on a disk in this machine", because a household reads a
screen, not a settings class. Renaming a section is not renaming a stored answer: the values are keyed by class name,
and the `cache_local_fanart = true` the film and series readers had been given was still there afterwards.

Both screens ask for their own half - the plugin screen for `plugin`, the media screen for `media` - and the media
screen draws every answer it gets as a row that saves itself when it is answered, below the rows that belong to this
machine. Measured on the running pair: the plugin screen lists six sections (3/2/1/7/6/4 fields) with no picture
question among them, the media screen carries the seven maxima and the six switches, and pressing "Banners kept per
title" on the television moved `max_banners` from 1 to 2 on the server and back. 1227 frontend and 557 server tests
green.


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
| Unit tests, Windows | 4/4 suites, 359 passed, 0 failed, 0 skipped; a suite whose assembly predates its own sources reports STALE instead of running the old binary | `tests.ps1` |
| Unit tests, Linux (canary) | 31 executed, 0 failed (1 skips: Windows-only), incl. live IPC round trips and XDG autostart | `test-canary-linux.ps1` |
| Server actually **runs** | **yes on Windows and on Linux** as a headless process. Windows: reaches the Running state, creates the SQLite store, answers UPnP discovery over the network, exits 0 with an error-free log. Linux: reaches the same Running state with 0 `FATAL`, 0 `ERROR` and one `WARN` - the documented note that a container has no FUSE driver. Android is still unmeasured | `run-server-check.ps1`, `test-server-upnp.ps1`, `test-server-run-linux.ps1` |
| Server **answers HTTP** | **yes on both hosts**, measured with the same probe: `GET /MediaPortal/Server/health` answers `200` with the server's version and the path it was called with - `pathBase=/MediaPortal/Server`, `path=/health` on Windows *and* in the container, because the shared listener now hands requests over in exactly the shape http.sys uses. The gate checks that text, not just the status code | `run-server-check.ps1 -ProbeUrl`, item **l** below |
| Server **has an HTTP API** | **one endpoint, on both hosts**, in an assembly of its own (`MediaPortal.Server.Api`): `GET /MediaPortal/Server/api/serverinfo` is a discovered MVC controller (the first in this tree) that reads MP2's own settings service on a request thread, and the gates on Windows and in the container both require its content. It is unauthenticated because the token endpoint that would authenticate it is still .NET Framework only | `run-server-check.ps1 -ProbeUrl`, items **m** and **o** below |
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

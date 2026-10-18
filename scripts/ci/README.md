# CI scripts

Entry points for building and gating MP2. Everything here is meant to behave identically
on a developer box and on an agent - and the sections below document where it currently
does *not*, because those differences are how CI surprises happen.

| Script | Purpose |
| --- | --- |
| `build.ps1` | Build MP2 solutions with the .NET SDK (`-BuildHost Dotnet`, default) or full Visual Studio MSBuild (`-BuildHost MsBuild`), write logs to `build/artifacts/`. `-Ratchet` also applies the warning gate. |
| `warnings.ps1` | The ratchet: fail if any warning code exceeds its committed count. `-UpdateBaseline` lowers the floor. |
| `spine.ps1` | Second ratchet for the WinForms/GDI+ references that the analyzer cannot see in a `-windows` project. |
| `spine-baseline.json` | Committed ceiling for those references in the portable core. |
| `common.ps1` | Shared helpers (MSBuild discovery, log parsing). Dot-sourced, not run directly. |
| `warning-baseline.json` | Committed maximum allowed count per warning code, for `MP2-Server.sln` (see `"solution"`). |
| `tests.ps1` | Runs the Windows test suites (canary, server, client, installer) and prints one line each. Assumes `build.ps1` already built; `-Build` to rebuild. |
| `packages.ps1` | Package health from `obj\project.assets.json`: which packages carry advisories, which only work through the .NET Framework fallback, which references are now implicit. Deterministic, unlike the same codes in a build log. `-FailOn High` for a pipeline that must not ship one. |
| `satellites.ps1` | Enforces the satellite boundary: a namespace that only exists in a `*.Windows` project may not be imported by a project that does not reference it. |
| `architecture.ps1` | x64-only check, read off the built images: PE `Machine` of every apphost in `MediaPortal\Bin` and `AssemblyName.ProcessorArchitecture` of its managed assembly, plus a list of source declarations that still ask for 32-bit. |
| `Dockerfile.canary` | Linux verification image for the portability canary (`MediaPortal\Tests\Canary.Portable`). |
| `test-canary-linux.ps1` | Builds that image and runs the canary tests inside a Linux container. |
| `Dockerfile.cross` | Linux **cross-compile** image: builds the whole `MP2-Server.sln` under `mcr.microsoft.com/dotnet/sdk:$SDK_TAG` (default `10.0`). |
| `test-server-linux.ps1` | Runs that image (see "Compiling on Linux" below for the package-cache it needs). |
| `Dockerfile.drawing`, `drawing-probe/` | Not a gate: measures which half of `System.Drawing` works on Linux. Read it before changing what `spine.ps1` counts. |

## The gate, in the order that fails fastest

    .\build.ps1 -Solution Server,Client -Ratchet   # builds, then warning + spine + satellite gates
    .\tests.ps1                                    # the four Windows suites, against built output
    .\packages.ps1                                 # advisory and netfx-fallback inventory (a report)

On Linux: `.\test-canary-linux.ps1` runs the canary's tests in a container, and
`.\test-server-linux.ps1` compiles the whole server solution there. Both need a populated package
cache - see "The dependency closure is not reproducible".

## Which build host, and why it matters

`dotnet build` used to be impossible on this tree: any project whose `.resx` holds non-string
resources aborts with `MSB3822`/`MSB3823`, because the .NET CLI host does not load the
design-time assemblies `GenerateResource` needs. On this tree that blocker had exactly one
home - the `MediaPortal.Utilities\Properties\Resources.resx` bitmap/icon set, which turned out
to be dead code (`internal` generated class, no `InternalsVisibleTo`, no consumer anywhere).
With it deleted, the .NET CLI builds the whole server solution:

    dotnet build -t:Rebuild MP2-Server.sln     # 0 errors, 458 warnings, ~65 s (MSBuild: ~4 min, 730 warnings)

`MediaPortal.Client.Launcher.csproj` still carries its own `System.Resources.Extensions`
workaround for the same problem, with `PrivateAssets="All"`, which keeps the assembly out of
the runtime output - that is why the fix was never promoted to `Source\Directory.Build.props`.

The gate now runs on **the .NET SDK host** (`dotnet msbuild`, `-BuildHost Dotnet` is the default), and the
remaining reason to keep Visual Studio MSBuild around disappeared in October 2026:

| host | server solution, 2026-10-18 |
|---|---|
| `MSBuild.exe` 17.14 (Visual Studio 2022) | 0 errors / **1776** warnings — works today, but **cannot build `net10.0-*` at all** |
| `dotnet msbuild` (SDK 10.0.402) | **0 errors / 2330 warnings / 122 assemblies** — the gate |

Why the switch is forced and not a preference: the port retargets to .NET 10, and **Visual Studio 2022 cannot host
.NET 10**. With SDK 10.0.402 installed machine-wide, VS 17.14 still resolves `C:\Program Files\dotnet\sdk\9.0.301`
and stops with `NETSDK1045` ("The SDK 'Microsoft.NET.Sdk' 10.0.0 could not be found"); `MSBuildSdksPath`,
`DOTNET_ROOT` and a `global.json` pin are all ignored, and the reason is measurable — SDK 10 ships MSBuild 18.9.15
while SDK 9 ships 17.14.5, so the resolver skips every SDK newer than the hosting MSBuild. A .NET 10 tree and a
Visual Studio 2022 build host are mutually exclusive. `-BuildHost MsBuild` still works (that is how the table above
was measured) and `Find-MsBuild` stays for it; do not use it for anything that must compile `net10.0-*`.

Driving it with the SDK host has one trap: **`dotnet` needs a verb**. `dotnet <path>.sln /t:Rebuild` fails with "The
application or command could not be found"; `dotnet msbuild <path>.sln /t:Rebuild …` works and forwards the
`/flp:logfile=…;verbosity=…` file logger, so `Get-LogDiagnostics` reads the same format from both hosts.

## The warning surface is toolchain dependent - read this before trusting a count

This used to be the trap in this repository, and it is now closed. Most projects declared
`<CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>` (116 projects) or
`MinimumRecommendedRules.ruleset` (131). Those are *bare* names that MSBuild resolves through Visual Studio's
"Static Analysis Tools\Rule Sets" directory — files that are **not in this repository**. On a full Visual Studio
box every analyzer rule ran as a warning; on a plain SDK host the file could not be found, MSBuild emitted
`MSB3884` (216 times here) and a much smaller default set applied. `CA1063` was 78 on a VS box and ~0 on an SDK
agent, so **a warning count was only comparable to a count taken with the same toolset**.

The fix is what the section title used to warn about, done properly:

- **Severity lives in `MediaPortal\.editorconfig`.** `CA1416` plus eight code-quality rules are pinned to
  `warning` there, for every host, configuration, and target framework. Analyzer severity does not belong in a
  Windows-only, IDE-only rule-set file that silently disappears on a Linux agent — it belongs in the file every
  editor and CI system already reads.
- **`Source\Directory.Build.targets` (+ root twin) clears the VS-only names** and sets `EnableNETAnalyzers=true`
  for old-style `net48` projects (the SDK leaves analyzers off there). With the names cleared, `MSB3884` is gone
  and there is exactly one source of severity.
- **The baseline still records the toolset**, and `warnings.ps1` still refuses to score a log whose `Tool:` line
  does not match — now as a belt-and-braces check rather than a necessity.

Measured on identical source with this baseline flow, 2026-10-18:

| host | total | per-code differences vs the SDK host |
|---|---|---|
| Visual Studio MSBuild 17.14, before the change | 1776 | eight analyzer rules under-reported (CA1063 78 vs 832, …) |
| Visual Studio MSBuild 17.14, after | 2322 | **only CA2024 (0 vs 4) and NETSDK1138 (0 vs 4)** |
| .NET SDK 10.0.402 (`dotnet msbuild`) | **2330** | — |

So the hosts now agree on every code except two that come from the SDK's newer analyzer set and targets, and
**CA1416 was identical on all three measurement environments** — Visual Studio, the SDK host, and the Linux container.
Totals across *platforms* (Windows 2330 vs the Linux probe's 449) still must not be compared: they compile
different project sets. Compare per-code inventories, and treat `CA1416` as the metric.

Those totals are all in the **old counting unit** (raw log lines, on the dual-`net48;net6.0-windows`
tree). The same tree measured with today's de-duplicating parser gives **687 findings**, and the tree
after the retarget gives **777**; see "A finding, not a log line" and "Retargeting the tree to
.NET 10" for the measured bridge between the two units. Do not compare a number from one side of that
line with a number from the other side.

## The ratchet

Making all warnings errors on a codebase this age is not achievable, and an unenforced lint
budget is decoration. Instead each warning code is capped at its last known count in
`warning-baseline.json`. Adding a new instance of any code fails the build; fixing some lets
you lower the floor.

### A finding, not a log line

`Get-LogDiagnostics` (in `common.ps1`) counts **unique findings**, keyed by code + normalised file +
line + column, or by identical line for diagnostics with no location. It has to, because the MSBuild
file log does not contain one line per warning: every diagnostic appears at least twice, once flush
left in the stream and once grouped under its project behind a `     5>` project-context prefix, and
the same file is occasionally spelled two ways (`...\Source\..\Extensions\x.cs` and
`...\Extensions\x.cs`). On `MP2-Server.sln` that is **1600 diagnostic lines for 777 findings**. The
prefix has leading spaces and the indent varies, so both are stripped before the key is built - a
parser that only anchors on `^D:\` silently keeps counting the doubled copy.

Two consequences to remember. First, floors generated before 2026-10-18 are about twice as high as
the same code measured today and cannot be compared with them; `warning-baseline.json` records the
unit and `notes` carry the measured bridge (the pre/post table for CA1416 in particular). Second, a
count that jumps for a whole family of codes is a *measurement* event, not a code event: check
whether a detector turned on before believing the codebase got worse. The retarget did exactly this -
NuGet audit is only enabled for `net8.0`+ targets, so `NU1902` went from 0 to 50 findings without a
single package changing. (Those findings describe ~50 *projects* reachable from a handful of packages;
see "Package health" below for the actual package list.)

The codes that matter for the multiplatform work are:

| Code | Meaning on .NET (non-Framework) |
| --- | --- |
| `CA1416` | Call site uses an API supported only on some platform(s). |
| `SYSLIB0006` | `Thread.Abort()` / `Thread.ResetAbort()` - compiles, then always throws `PlatformNotSupportedException`. |
| `SYSLIB0011` | `BinaryFormatter` - obsolete, and unsupported for untrusted data. |
| `SYSLIB0004` | Constrained Execution Regions - not supported. |

### Restore-time NuGet codes are not ratchet material

Two consecutive runs of the same source, same options, forced restore (`/p:RestoreForce=true` on the
`/t:Restore` pass in `build.ps1`, which is there so assets are never reused silently) produced
**778 findings and then 776**: `NU1510` 21 then 20, `NU1902` 50 then 49. Nothing about the tree
changed. A project only re-emits `NU15xx`/`NU1701`/`NU19xx` into the build log when it re-evaluates
its assets, and which projects do that in a given pass depends on build order and parallelism.

So those codes are listed in `warning-baseline.json` under `untracked`, their current numbers appear
under `informational`, and the gate scores 686 findings instead of 776. They are not silenced - they
are measured where the answer is data rather than log prose, by `packages.ps1`. The lesson generalises:
**a code belongs in the ratchet only if its count is a function of the source.** If it depends on
machine state, ordering, or cache warmth, it belongs in a report that reads that state directly.

`warning-baseline.json` also records `"solution": "Server"`, and `warnings.ps1 -Solution` refuses to
score a log from a different solution. The floors describe one inventory; `MP2-Client.sln` overlaps the
server's and adds the entire UI on top, so scoring it here would report hundreds of "new" codes that
are simply outside the measurement. The client is built by the gate but not scored until it gets its
own baseline file, which it will not need for long if Phase 2 lands.

### Package health: the advisory surface is empty, and how that was measured

`packages.ps1` answers what `NU19xx` only hints at, and does it deterministically. It does not parse a
build log and does not call `dotnet list package` per project (that needs a restore of its own, and a
restore is exactly the thing that fails here). It reads the `logs` array of every
`obj\project.assets.json` - the structured output of the last restore - and ignores any file that does
not describe a `net10.0` target, so a leftover from before the retarget cannot inflate a number.

This is what it found (2026-10-18, 115 current assets files) and what closed each one:

| Severity | Package | Version | Projects | Advisory | Closed by |
| --- | --- | --- | --- | --- | --- |
| Moderate | `log4net` | 2.0.14 | **104** | GHSA-4f7c-pmjv-c25w | bumped to 3.5.0 |
| High | `DotNetZip` | 1.15.0 | 1 | GHSA-xhg6-9j5j-w4vf | dependency deleted |
| High | `Microsoft.Windows.Compatibility` | 7.0.1 | 1 | GHSA-555c-2p6r-68mm | dependency deleted |
| High | `System.Data.SqlClient` | 4.8.5 | 1 | GHSA-98g6-xh36-x2p7 | went with it |
| **Critical** | `System.Drawing.Common` | 5.0.0 | 1 | GHSA-rxg9-xrhp-64gj | went with DotNetZip |

Five entries, four packages, **and `packages.ps1 -FailOn Low` now exits 0** on the same 115 projects.
The counts of projects were the point of reading the assets files: `log4net` looked like 104 problems
in a build log and was one version bump in `MediaPortal.Common.csproj`, the only direct reference in
the tree. Details worth keeping, because each one decided a fix rather than being trivia:

- GHSA-4f7c-pmjv-c25w (silent log event loss in `XmlLayout`) is only patched from log4net **3.3.0**, so
  no 2.x bump could have cleared it. The whole of MP2's log4net surface is one file
  (`log4netLogger.cs`), which is what made a major-version jump safe to attempt; `Log4NetLoggerTests`
  is what makes it safe to *keep*, including a test that configures the shipped `MP2-Server` config
  file and asserts every appender it names by string still resolves. Appenders that stop resolving are
  dropped silently, so the product runs and simply stops writing logs.
- `DotNetZip`'s traversal spans 1.10.1-1.16.0 with **no patched release of that package id** (the
  maintained fix lives in a fork under a different id), so `LogCollector` was moved to
  `System.IO.Compression` - six lines of zip code - instead of being pinned to an advisory. That
  package was also what dragged `System.Drawing.Common` 5.0.0 in, so one deletion closed a Critical.
- `Microsoft.Windows.Compatibility` is a crutch for code ported off .NET Framework; `Setup\Tests`
  imported none of the namespaces it supplies, and `System.Data.SqlClient` reached the project only
  through it. Both highs were one deletion.

Three things that hid part of the answer while the script was being written:

- The first version matched an allow-list of codes, so **`NU1904` - Critical - was silently dropped**.
  There is now a `^NU19\d\d$` fallback that ranks any unknown advisory code and prints the code once.
- Messages are **localized, and the quote characters differ by locale** (`Das Paket „log4net" ...`), so
  ids are extracted with a character class covering ASCII quotes, typographic quotes and guillemets.
  Severity comes from the code, never from prose.
- **`Write-Host` output cannot be redirected.** `-Format json` writes to the success stream; a pipeline
  should use `packages.ps1 -Format json | ConvertFrom-Json`. A redirected run of the earlier version
  reported "no advisories" for this reason, which is the most dangerous kind of wrong output.

Two caveats about the number, both from how `project.assets.json` comes to exist:

- It describes **the last restore each project actually did**. The gate restores only what it builds,
  so `MediaPortal\Setup\*` and `Tools\LogCollector` keep whatever an earlier restore left; build or
  restore them before reading a clean report. The first version of this table missed both projects for
  exactly that reason, which is how the Critical turned up.
- A missing file means *never restored*, not *clean*. The script prints both file counts on every run;
  if the second number does not look like the number of projects you expect, the report is not a
  measurement of the tree.

`NU1701` (restored through the .NET Framework fallback, so it runs on .NET compatibility rather than
support) names seven packages: `BASS.NET` 2.4.14.1 and `BassRegistration` 2.4.14.1 (23 projects each),
`MediaPortal.TagLib` 2.3.1 (11), `SharpLibHid` 1.5.1, `DiscUtils` 0.11.1, `OnlineVideos.Core` 2.1.0,
`WorldWeatherOnlineHelper` 1.0.0.3. `NU1510` names four references the SDK now supplies implicitly
(`Microsoft.CSharp` in 31 projects, plus `System.Threading.Tasks.Dataflow`, `System.Resources.Extensions`
and `System.Security.Permissions`) - each is a one-line deletion. It used to be five: the fifth was
`System.Drawing.Common` in `MediaPortal.Utilities`, and it left with the retarget of that project (see
"The first core assembly off Windows"), which is the only way this line shrinks - the SDK can point at an
unnecessary reference but only a call graph says whether it is also an unused one. Six of the
`NU1701` seven are also the dead-feed packages below, so they cannot be bumped until Phase 1a lands.

### What decides the log level, and why a logging test failed only in a full run

`Log4NetLogger`'s constructor does not stop at the configuration file. After `XmlConfigurator` it asks
the service container for `ISettingsManager` and, **if one happens to be registered**, loads
`LogSettings` and applies `SetLogLevel(settings.LogLevel)` on top of what the file said - which also
saves the level back. So the persisted setting always wins, and the default in `LogSettings` is
`Information`: a server configured for `ERROR` in its config file logs at `INFO` as soon as one
`LogSettings` has ever been saved. That is product behaviour, not a defect, but nothing in the tree
said so until a test found it.

The test found it the way order dependence is usually found - green alone, red in a full run.
`EventsBelowTheConfiguredLevel_AreNotWritten` wrote an `INFO` line it expected to be dropped, because
`ServiceRegistration` is a **process-wide singleton** and an earlier fixture in `Tests.Server` (via
`MockCore`) had left an `ISettingsManager` registered for the rest of the test host's life. Nothing was
parallel; the ambient container was simply different. Any test whose behaviour can be steered by a
registered service has to say which registration it assumes, so that fixture now snapshots the ambient
`ISettingsManager`/`ILogger`, removes them for the duration, registers `NoLogger` (production's own
silent `ILogger`, so no new test double was needed), and restores what it found in teardown. A fourth
test, `PersistedLogLevelOverridesTheConfigurationFile`, then pins the precedence rule itself with an
in-memory settings manager, including that switching level persists exactly once.

The general form is worth keeping: **a global singleton service container turns test order into test
inputs.** In this tree that applies to every test that constructs anything calling
`ServiceRegistration.Get`, which is most of the server.

### What CA1416 cannot see

`CA1416` only fires for Windows-only APIs that arrive from **NuGet packages** -
`System.Drawing.Common`, `System.Management`, `Microsoft.Win32.Registry`. Anything the
`-windows` target framework itself supplies (`System.Windows.Forms`, WPF) is Windows *by
construction*, so calls to it produce no diagnostic at all. This is measurable: deleting
three WinForms call sites from `MediaPortal.Common` moved `CA1416` by exactly zero.

So `CA1416` is a good proxy for the GDI+/WMI/Registry surface (its 468 visible sites land on
`ImageUtilities.cs` 108, `Images.cs` 74, `CrashLoggerBase.cs` 30, `Proxy.cs` 28,
`WindowsAPI.cs` 26, `IpcServer.cs` 20, ...) and a blind one for the WinForms spine.
Two companions close the gap:

* **`spine.ps1`** counts textual `System.Windows.Forms` / `System.Drawing` references and
  ratchets them in two sections: the *portable core* (`MediaPortal.Common`, `Utilities`,
  `Backend`, floor must reach 0) and the `*.Windows` *satellites* (discovered automatically,
  floor must not grow). Crude by design - a grep cannot be silenced by an annotation.
* **The canary** (`MediaPortal\Tests\Canary.Portable`) is the only *authoritative* check:
  compiling a file for a portable TFM makes the analyzer see Windows as genuinely absent,
  which is when the missing diagnostics appear.

There is a third category that neither companion can express, and it turned up in
`IpcClient.GetRunningApps()`: `Directory.GetFiles(@"\\.\pipe\")`. A string literal is not a Windows API,
not a `[DllImport]`, not a `using`, so `CA1416`, `spine.ps1` and a TFM flip are all blind to it. It
walked through the portable core and would have thrown `DirectoryNotFoundException` on the first call on
Linux. Nothing static catches that; the canary test that opens an `IpcServer` and then requires
`GetRunningApps()` to list it does. When a Windows dependency can be written as data, only execution on
the other platform can rule on it.

### A satellite is a firewall outward and a cloak inward

A `*.Windows` satellite declares `[assembly: SupportedOSPlatform("windows")]`. That declaration
cuts both ways, and both halves have been measured here:

* **Outward, desirable.** Before any assembly in a call chain declares a platform, `CA1416`
  cannot judge a cross-assembly call at all, so every caller looks platform-neutral. Declaring
  it is what made `MediaPortal.Server`'s two `FormDpiAwarenessExtension` calls appear as
  warnings - true positives that had been invisible for years.
* **Inward, undesirable.** Inside an assembly that declares `windows`, every Windows call is
  by definition legal. Moving `SplashScreen.cs` into `MediaPortal.Utilities.Windows` moved
  `CA1416` by **-20 while changing no behaviour whatsoever**, and moving `ImageUtilities.cs` in
  the same direction later moved it by a further **-102**. None of those call sites disappeared;
  they simply stopped being reported.

Consequence for the floor: `CA1416` is a *proxy* for Windows coupling and the relocations made it
a weaker one, so the budget no longer lives in `warning-baseline.json`. Visible `CA1416` and the
floor were both re-set to 366 when the cloak could no longer be accounted for by hand, and the
number that now represents the amount of Windows code left to port is `combinedTotalRefs` in
`spine-baseline.json` - `spine.ps1` counts references in the source of both areas, so it cannot be
lowered by moving a file into an assembly that declares `windows`. `notes.CA1416` says this too,
because a reader who only sees 366 will otherwise believe the port is two thirds done.

Typical loop:

    .\build.ps1                       # builds Debug, writes build\artifacts\build-Server-Debug.log
    .\warnings.ps1 -UpdateBaseline    # -LogPath is optional; without it the newest build log is used
    git add scripts\ci\warning-baseline.json

Floors may rise legitimately. When they do, `warnings.ps1` keeps failing until the floor is
re-baselined on purpose, and the reason belongs in `notes.<CODE>` inside
`warning-baseline.json` (preserved across re-generation) as well as in the commit message.

## A satellite may only grow by what the core shrinks

`spine.ps1` gates two numbers, not three:

* the **core** total (Common, Utilities, Backend) may not grow, and its target is 0;
* the **core + satellites** total may not grow either.

Gating each section on its own looked tidy and was wrong: it failed a commit that took two GDI+
references *out* of `MediaPortal.Common` and put them into `MediaPortal.Common.Windows`, which is
the operation this port is made of. With the combined rule, relocating Windows code is allowed and
inventing new Windows coupling is not - a satellite absorbs a dependency, it does not create one.
When a satellite does grow and the core did not, the report says so explicitly instead of letting
the reader work out which case happened.

The count is namespace mentions **plus** GDI+ identifier occurrences, and both were forced by
measurement. Mention counting alone was blind: adding `typeof(Bitmap)` to a file that already
imported `System.Drawing` did not change the number, so the gate could not see new coupling in the
one file that has the most of it (`ImageUtilities.cs`, 7 mentions but 32 references). Identifiers
are only attributed to `System.Drawing` inside files that import or qualify it, which keeps an
unrelated `Image`/`Color`/`Size` member elsewhere in the tree out of the count.

### The core spine is at zero, and what that number turned out not to cover

That is the state the gate holds for the two categories it originally counted, and it is worth
stating precisely because of what it does *not* mean:

    MediaPortal\Source\Core\MediaPortal.Common      WinForms 0  GDI+  0  P/Invoke 21  Drawing primitives 1
    MediaPortal\Source\Core\MediaPortal.Utilities   WinForms 0  GDI+  0  P/Invoke 74  Drawing primitives 0
    MediaPortal\Source\Core\MediaPortal.Backend     WinForms 0  GDI+  0  P/Invoke  0  Drawing primitives 0
                                                    files affected 13  total refs 95
    satellites (MediaPortal.Common.Windows, MediaPortal.Utilities.Windows)  WinForms 19  GDI+ 64  P/Invoke 3  refs 86

That table is the state on the day the P/Invoke column was introduced, and the live figures are in
`spine-baseline.json`; it is kept verbatim because every number in it has since moved and the reasons are
the interesting part.

WinForms and GDI+ are zero in the core: no source file compiled into `MediaPortal.Common`,
`MediaPortal.Utilities` or `MediaPortal.Backend` references them any more. That was done by two moves
(`SplashScreen.cs`, `ImageUtilities.cs` into `MediaPortal.Utilities.Windows`) and one seam
(`IImageTransformer`); `git mv` is enough for the first kind, the second needs a host to install an
implementation.

The P/Invoke column is new, and the reason it is new is a measurement that should have been obvious.
A `[DllImport]` declaration produces no `CA1416` diagnostic - the analyzer reasons over types in
referenced assemblies and a DLL name is a string - and it is neither WinForms nor GDI+, so it was in
none of the columns above. With the core spine reading **0**, the same three assemblies contained
**95 P/Invoke declarations in 13 files** (`SystemAPI\NativeMethods.cs` alone declares 43,
`SystemAPI\WindowsAPI.cs` 13, `MediaPortal.Common\Services\Dokan\Native\DokanNativeMethods.cs` 8).
A gate that reads zero while Kernel32 is being loaded is not a portability measurement. The count is
textual, comments stripped, from files on disk - which means it also counts
`MediaPortal.Utilities\MimeTypeDetector.cs`, a file the csproj excludes with `<Compile Remove>` and
which therefore ships nothing; leaving it counted is deliberate, because a counter that starts
 consulting the build system is a counter whose silence you cannot interpret. It also means the old
line "`MediaPortal.Common` still builds as `net6.0-windows`" was stale prose from before the retarget;
all three are `net10.0-windows` now, for the reasons in the next section.

### How a core assembly loses its `-windows` target

All 90 projects under `MediaPortal\Source` target `net10.0-windows`. Nothing in the tree can run on
Linux until the backend ones stop doing that, and the question "what actually stops
`MediaPortal.Utilities`?" was answered by editing the file and letting the compiler reply, with the
build redirected to a throwaway output path:

* `UseWindowsForms=true` must go, and it can: with the TFM and that property changed by hand, the
  project compiles with **0 errors**. The WinForms code left when the satellites were extracted, and
  only the property stayed behind.
* What is left is **35 distinct `CA1416` call sites in six files** (72 lines in the log, because
  MSBuild lists each diagnostic twice when a build fails): `SystemAPI\WindowsAPI.cs` 13 registry
  reads/writes under `LocalMachine`/`CurrentUser`, `Process\IpcServer.cs` 8 (`PipeSecurity` ACL,
  `WellKnownSidType`, and `PipeTransmissionMode.Message`, which Unix named pipes do not implement at
  all - that one is a behaviour gap, not an annotation problem), `Process\SingleInstanceHelper.cs` 8
  (`MutexSecurity` ACL on a named mutex; the mutex itself is cross-platform),
  `SystemAPI\MimeTypeDetector.cs` 3 (`HKCR` lookup), `FileSystem\DriveUtils.cs` 3 (WMI
  `ManagementObject` for disk size), `ThreadingUtils.cs` 1 (`ApartmentState.STA`).
* The 95 P/Invoke declarations are *additional* to those 35 and are not part of that compile error
  surface at all, which is exactly the trap.

Two decisions came out of that measurement, and both are now conventions:

**A platform branch is written `if (OperatingSystem.IsWindows())`, never
`RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`.** `CA1416` recognises the first as a guard and
stays silent; it does not recognise the second, so with the second you get the same warning as with no
guard at all. That asymmetry is the whole reason for the rule - it is what makes a branch verifiable
by the compiler instead of by a reviewer. Before this was written down the tree contained exactly zero
calls to either.

**Registry and WMI lookups keep their non-Windows answer, they do not gain a stub.**
`MimeTypeDetector.GetMimeTypeFromRegistry` returns `null` off Windows, which is the same answer
Windows gives for an unregistered extension, and both of its callers already fall through to
`GetMimeTypeFromExtension` - a built-in map of 979 extensions that is the actual portable
implementation.
Where the fallback is not already there, the file moves to a satellite instead.

**The worst case of the P/Invoke blind spot is a static initializer.**
`SingleInstanceHelper` declared

    public static readonly uint SHOW_MP2_CLIENT_MESSAGE = WindowsAPI.RegisterWindowMessage("...");

A field initializer runs the first time anything touches the class, so on a machine without
`user32.dll` the first reference to `SingleInstanceHelper` - which is the first thing each launcher
does - would have thrown `DllNotFoundException` out of a type initializer, where no caller can catch
it. `CA1416` said nothing, because a DLL name is a string to the analyzer, and `spine.ps1` said nothing
until the P/Invoke column existed. The registrations are now behind `OperatingSystem.IsWindows()`, as
are the `MutexSecurity` grant (Windows-only type; the named mutex itself is not) and
`SwitchToCurrentInstance`'s `HWND_BROADCAST` post.

The claim that a *named mutex* is usable off Windows was measured rather than assumed, in
`MediaPortal\Tests\Canary.Portable\NamedPrimitiveBehaviourTests.cs`, which runs in the Linux container
under `test-canary-linux.ps1`. Two properties `SingleInstanceHelper` relies on are pinned there:
another thread cannot take a held named mutex, and a second handle created from the same name sees it
as taken and acquires it after the owner releases. Writing the tests produced two corrections that were
wrong about Windows as well as Linux, and both matter whenever launcher code is ported:

* a second `WaitOne()` on a mutex the same thread already holds **succeeds** - a Win32 mutex is
  recursive for its owning thread, so contention only exists across threads.
* `ReleaseMutex()` from a thread other than the owner throws *"Object synchronization method was
  called from an unsynchronized block of code"*.

That second one exposed a trap in the reporting rather than in the code: the run printed
`Passed! - Failed: 0, Passed: 9` and only afterwards `Test Run Aborted`. **A crashed test host still
prints a green summary line**, so a summary line is never evidence on its own. `tests.ps1` reads
`$LASTEXITCODE` first and parses the numbers only for its table, which is why this surfaced as a
failure instead of a pass - verified while writing this, not assumed.

**A platform-skipped test is missing from all four numbers on the summary line.** Not from "skipped" -
from failed, passed, skipped *and* total. `Assume.That(OperatingSystem.IsLinux())` prints its reason in
the detailed console output, but the summary of a suite containing three such tests on Windows reports
`29 passed, 0 skipped, total 29` while the assembly contains 32 tests. So a suite whose reported total
drops can be perfectly green and still not have run what it usually runs, and the summary alone cannot
tell those two situations apart. `tests.ps1` prints the per-suite numbers, and the ROADMAP scoreboard
writes the count as "executed" with the skips named beside it, so that the difference between "fewer
tests passed" and "fewer tests ran" is visible in the number itself.

### Guarding a pipe: where "just add `OperatingSystem.IsWindows()`" is not the answer

`Process\IpcServer.cs` contributed 8 of the 35 sites, and it is the file where the guard recipe stops
being sufficient, because two of the Windows-specific things there are decisions rather than calls.

* **`PipeTransmissionMode.Message`.** Unix named pipes are byte streams; asking for message mode throws.
  So the non-Windows branch uses `PipeTransmissionMode.Byte`, and the question that matters is whether
  the protocol relied on message boundaries. It did not, and that was read out of the code before being
  trusted: the server's read loop and `IpcClient.SendReceive` both keep reading until the 6-byte header
  and the announced payload have arrived, and every response carries its length in its own header. Byte
  mode is the same protocol over a stream, not a degraded one.
* **`PipeSecurity`.** Off Windows there is no ACL API to call, so there is nothing to grant and the
  socket file's own permissions are the answer. Dropping it is a real (small) behaviour difference, not
  a silent one: these pipes carry a local launcher's "shut down" to a local process.

Neither half is verifiable by reading, so `Canary.Portable` links `Ipc.cs`, `IpcClient.cs` and
`IpcServer.cs` and `IpcRoundTripTests.cs` runs a server and client against each other over a real pipe -
a round trip, a second concurrent connection (which exercises the `OpenNewPipe()` call inside the
connection callback), a shutdown delivered to the custom callback, and a `GetRunningApps()` listing that
must contain an open server and lose it after `Close()`. Green on Windows (the `IsWindows()` branch) and
green in the container (the else-branch), 27 tests.

The canary gained a `<PackageReference>` to do this - `NamedPipeServerStream.NetFrameworkVersion`, the
package that supplies the `NamedPipeServerStream` factory overload taking a `PipeSecurity`. That is worth
distinguishing from a Windows *target framework*: the package carries `net5.0`/`net6.0`/`netstandard2.0`
assets, restores and compiles everywhere, and its types throw off Windows, which is exactly why they sit
inside the guard. A linked file needing a package is not a failed port; a linked file needing
`net10.0-windows` is.

### The first core assembly off Windows

`MediaPortal.Utilities` is now `<TargetFramework>net10.0</TargetFramework>` with `UseWindowsForms=false`
and `<WarningsAsErrors>CA1416</WarningsAsErrors>` in its own project file, which is what keeps the state
from rotting back. How the last groups of call sites went:

* **ApartmentState, `HKCR`, pipe ACL, mutex ACL** - guards, as above.
* **Autostart (9 sites in three `WindowsAPI` methods)** - the inventory called these "registry", but read
  together with their callers they are a feature, and a feature gets translated:
  `SystemAPI\AutostartRegistry.cs` does the `CurrentVersion\Run` value on Windows and an XDG autostart
  desktop entry on Linux, and reports `IsSupported == false` where no mechanism exists. `WindowsAPI`'s
  three methods delegate, so no call site changed. On Linux the path lives in the entry's `Exec=` key and
  is quoted per the key-file rules, which is what lets a path containing spaces survive a round trip -
  worth a test rather than a paragraph, so it has one
  (`DesktopEntryQuotingSurvivesCharactersThatWouldBreakALine`).
* **WMI disk size, `GetDiskFreeSpaceEx` (4 sites)** - dead. `GetDiskSize` and `GetFreeDiskSpace` had no
  callers anywhere in the tree, so they were deleted rather than ported, which in turn revealed that
  `System.Management` and `System.Drawing.Common` were referenced by `MediaPortal.Utilities` only for
  them. Both package references are gone: two Windows-backed dependencies left the portable core because
  a call graph was checked instead of a warning list.

Measured effect: `CA1416` in the Server solution **196 → 161**, spine P/Invoke ledger **95 → 94**, and
`spine.ps1` now reports the core's remaining declarations as 73 in `Utilities` plus 21 in `Common`. Both
baselines were re-armed with `-UpdateBaseline` in the same change.

The order that fell out of doing this: delete dead code first (free, and it can remove dependencies
outright), then ask what feature a call site is implementing before choosing between a guard, a
translation, or a satellite. The inventory says *where*; only the callers say *which*.

Two verification details, because the claim "the assembly is portable now" is the kind that is easy to
make and hard to mean. First, what reaches the *shipped* output was checked, not assumed: the copy of
`MediaPortal.Utilities.dll` inside `MediaPortal\Bin\MP2-Server\bin\Debug\net10.0-windows\` contains no
reference to `System.Windows.Forms` (it did before the flip) even though it now lives next to
Windows-targeted assemblies - the folder name describes the consumer, not the dependency. Second, the
project's own stale `bin\Debug\{net48,net6.0-windows,net10.0-windows}` and matching `obj` directories
were deleted: a `net10.0-windows` folder with an 08:23 timestamp sitting beside a `net10.0` folder with
08:31 is exactly the false witness that already produced one wrong number in this project's history.

### The second assembly off Windows found a hole in the ledger

`UPnP` is now `net10.0` with `CA1416` as an error, and its migration was a single property: the project
declares no `[DllImport]`, no WinForms and no Windows-only call site, so the portable build produced 0
errors and 0 findings on the first try. Two things worth recording came out of it anyway.

The first is which project is *eligible* next. A portable project cannot reference a `-windows` one, so
the retarget order is fixed by the reference graph: `UPnP` -> `MediaPortal.Common` -> `MediaPortal.Backend`.
Ranked by Windows-dependency counts the order would have been Backend, UPnP, Common - Backend has zero P/Invoke
declarations and no WinForms - and it is unbuildable, because Backend references Common. Eligibility is
measured from the graph; cost is measured from the code.

The second is that `spine.ps1` counts a hard-coded list of core directories, and `UPnP` was never on it.
Since `MediaPortal.Common` references `UPnP`, any Windows dependency added there would have entered the
portable core without moving a single number in the gate - the same class of blind spot as `CA1416` never
seeing a `[DllImport]`. Adding the directory changed every column by 0, which is precisely why the omission
had survived: a gate that reports nothing looks identical to a gate that checked and found nothing. The
script now states the rule (a project joins the ledger when a core project references it). Two counting
details from the same probe, both of which can produce a confidently wrong number:

* **Decide what a "site" is before quoting a count.** Flipped to `net10.0`, `MediaPortal.Common` reports
  `CA1416` on 39 distinct source lines, or 43 if a line that touches two Windows-only members counts once
  per member - and the probe log contains 88 message occurrences, because each site is reported two or four
  times over (several compiles of the same project, and lines with more than one Windows-only member). Only
  one of those three numbers is comparable across runs; the baseline stores findings, not log lines.
* **`grep` is not a ledger.** A plain search for `System.Windows.Forms` in `MediaPortal.Common` returns two
  hits; `spine.ps1` reports 0. Both hits are comments describing earlier removals of `SystemInformation` and
  `Application.ExecutablePath`. The ledger strips comments before matching, which is the only reason its
  number is a measurement of code rather than of text.

### What a whole Windows subsystem leaving the core looks like

`Services\Dokan` (17 files, the .NET binding for the Dokan user-mode file system driver) was the
largest single block of Windows coupling in `MediaPortal.Common`. Moving it is the first case of a
*subsystem* rather than a helper crossing into a satellite, and three things made it mechanical:

* **Find the one consumer first.** A repo-wide search for `Dokan` outside its own folder returned
  exactly one file, `Services\ResourceAccess\ResourceMountingService.cs`. That file is the seam, so it
  moved with the binding and was renamed `DokanResourceMountingService` with a static `Register()`, the
  way `GdiPlusImageTransformer` does. `MediaPortal.Common` keeps the interface
  (`IResourceMountingService`, which always lived outside the `Dokan` folder) and now registers
  `UnsupportedResourceMountingService`, which answers "this platform cannot mount".
* **Check whether the degraded state already exists before inventing one.** The Dokan implementation
  already ran in a "no mount" mode on every Windows machine without the driver: `Dokan.Install()`
  returns `null`, it logged a warning, `RootPath` was null, `IsVirtualResource()` was false and the
  mutators returned null - which is also what the interface documents. `MountingDataProxy.MountResource()`
  branches on `CreateRootDirectory(...) == null` today. So the portable class is not new behaviour to
  test; it is an existing runtime state promoted to an implementation, and reading that one caller is
  what proves it. (It is also one bug better: `RootDirectories` used to dereference the null executor.)
* **Let two independent checks agree.** The spine ledger reported the move as an exchange - core P/Invoke
  **94 -> 86**, satellites **86 -> 94**, combined unchanged at 180 - and printed "satellites grew by 8
  while the core shrank by 8 - relocated, not fixed". `satellites.ps1` separately reports
  `MediaPortal.Common.Services.Dokan` and `.Native` as namespaces now owned exclusively by
  `MediaPortal.Common.Windows.csproj`. A count and a name-based structure check agreeing is worth more
  than either alone.

`CA1416` fell by exactly 14 (161 -> 147): 13 findings in `Dokan\Proxy.cs`, 1 in `DokanFileInfo.cs`. That
the solution-wide delta equals the moved files' own count is the check that nothing Windows-shaped from
Dokan stayed behind in the core - had one call site remained, the number would have dropped by less.

## Platform services: the core declares the seam, the host fills it

`MediaPortal.Common` cannot reference a platform assembly, so where it needs a Windows technology it
declares an interface and each host registers an implementation during startup, next to the other
platform services it installs:

| Seam | Declared in | Windows implementation | Installed by |
| --- | --- | --- | --- |
| `IImageTransformer` (decode/resize/encode) | `MediaPortal.Common\Services\Imaging` | `GdiPlusImageTransformer` in `MediaPortal.Common.Windows` | `ApplicationLauncher` of Server, Client, ServiceMonitor; `Client.cs` in DevTools |
| `LauncherExceptionHandling.LastResortReporter` | `MediaPortal.Common\Exceptions` | a `MessageBox` lambda in the launcher | the same launchers, each with its own dialog kind |
| `UiCrashLogger.ScreenInfoProvider` | `MediaPortal.Common\Services\Logging` | a `SystemInformation` lambda | the Client launcher |

Two rules follow from that shape, and both are easy to break by accident:

* **Every host must fill every seam it can reach.** The defaults are chosen so a headless host still
  works; an unregistered `IImageTransformer` is not fatal but costs thumbnails and only shows up as a
  warning. A new host must register one, and the Linux host must register a portable one - that is
  the point of the seam.
* **Do not "simplify" a seam by giving the core a default that names a Windows type.** The whole
  value of `IImageTransformer` is that `MediaPortal.Common` no longer mentions `System.Drawing`;
  `MediaPortal.Common` now measures 0 WinForms *and* 0 GDI+ references.


`satellites.ps1` is the third gate, and it exists because a satellite move can break a project
that never calls the moved code. Moving a type keeps its namespace, and a `using` of that
namespace is a compile dependency even when the only line that used the type is commented out.
That happened here: `MediaPortal.ServiceMonitor` still had `using MediaPortal.Common.UI;`, so
moving `FormDpiAwareness.cs` broke it - and ServiceMonitor lives in `MP2-ServiceMonitor.sln`,
which no build here performs.

The rule it enforces: if a namespace is declared **only** inside a `*.Windows` satellite, any
file importing it must belong to a project that references that satellite. Namespaces declared in
two places (`MediaPortal.Common.Async` is in both `MediaPortal.Common` and
`MediaPortal.Common.Windows`) prove nothing and are ignored. It reads source rather than build
output, which is what gives it coverage of projects whose solutions currently do not restore.

It is slow (tens of seconds - it walks every `.cs` file under `Source`, `Extensions`, `Tests` and
`Incubator`) but runs once per `-Ratchet`.

## A `using System.Drawing` is not necessarily a Windows dependency

`System.Drawing` is two assemblies with opposite portability, and a metric that treats the
namespace as one thing lies about both:

| Assembly | Types | Off Windows |
| --- | --- | --- |
| `System.Drawing.Primitives` | `Color`, `KnownColor`, `Point`/`PointF`, `Size`/`SizeF`, `Rectangle`/`RectangleF`, `ContentAlignment` | ships with the shared framework; **works** |
| `System.Drawing.Common` | `Image`, `Bitmap`, `Graphics`, `ImageFormat`, `ImageCodecInfo`, `Icon`, `PixelFormat`, ... | Windows-only; **not even compilable** without a package reference |

Both halves were measured in `mcr.microsoft.com/dotnet/sdk:8.0` on `net8.0`, not looked up. The
probe is committed at `scripts/ci/drawing-probe` (build it with `scripts/ci/Dockerfile.drawing`)
and its output on Linux is:

    PRIMITIVES WORK: argb=ff0c2238 size=4x3 rect=3 known=Red
    IMAGING REFUSES: System.TypeInitializationException: The type initializer for 'Gdip' threw an exception.
      innermost: System.PlatformNotSupportedException: System.Drawing.Common is not supported on
                 non-Windows platforms. See https://aka.ms/systemdrawingnonwindows for more information.

And that is with the `System.Drawing.Common` package *referenced*. Without it the imaging half
does not even reach the runtime: `CS1069: The type name 'Bitmap' could not be found in the
namespace 'System.Drawing'. This type has been forwarded to assembly 'System.Drawing.Common'`.

Consequence for `spine.ps1`: `System.Drawing` mentions are classified per file. A file that only
touches value types (`MediaPortal.Common\Configuration\ConfigColoredItemList.cs`, which exposes
`System.Drawing.Color` in its config API) is counted in a separate **Drawing primitives** column
that is reported but *not* gated. Without that split the must-reach-zero list contains a type
that is already portable, and the next person to hit it "fixes" a public configuration API for
zero portability gain. A `System.Drawing.X` type that is in neither list is counted as Windows -
the pessimistic reading is the only safe default for a gate whose whole job is not being
silenced.

## The TV service layer is gone

Removed by decision, not by decay: `Source\UI\TV\SlimTvService3`, `SlimTvService35`, `SlimTvProxy3`,
`SlimTvIntegration`, `SlimTvResources` and the orphan `SlimTvService` folder - **54 files, 6 833
lines** - together with their installer features, bootstrapper metadata and one test. That means
live TV, EPG, timeshift and timers served by the MP2 TV Server are no longer part of what this tree
builds or installs. `SlimTv.Interfaces` (the shared TV contract, referenced by **18** projects
including MediaServer, MPExtendedProvider, NativeProvider, TvMosaicProvider, TranscodingService and
two metadata extractors) and `SlimTv.Client` stay, so the client-side and TVMosaic paths still
compile.

Two findings worth keeping:

* `SlimTv.Service3` was the **only** consumer of the Gentle Framework assemblies
  (`Gentle.Common`, `Gentle.FrameWork`, `Gentle.Provider.MySQL/SQLite/SQLServer`, referenced as raw
  `<Reference>`s from a `lib` folder). Gentle cannot run on .NET Core, measured rather than asserted:
  `Gentle.Common` is a `v4.0.30319` image that references **`System.Web`** and
  `System.Configuration`, and `Gentle.Provider.SQLite` is still a `v2.0.50727` image. *That* is why
  Service3 was the single project in the tree targeting plain `net48` while Service35 was the
  CoreWCF-based successor. It is also what the (commented-out) `Gentle.*` binding redirects in
  `MediaPortal.Server\app.config` were about - they pinned `1.5.0.29053-1.5.0.29112` to
  `1.5.0.29113`, the exact version of the DLLs on disk. With Service3 gone, **Gentle has zero
  references left in the tree**.
* The same is true of the `MediaPortal.TvEngine.*` NuGet packages - the last consumers were in the
  deleted set (measured: no `.csproj` in the repo mentions `Gentle` or `TvEngine` any more). Both
  dependency groups can now be dropped from the package story entirely.

`MediaPortal.TvEngine.*` also carried the tree's only **x86-compiled managed assemblies**. Measured
with `AssemblyName.ProcessorArchitecture` on the packages in the NuGet cache: `TvBusinessLayer`,
`TvControl`, `TVDatabase`, `TvLibrary.Interfaces` and `TvThumbnails` are `X86` (not AnyCPU), so any
assembly referencing them inherits a 32-bit process requirement. That is what the two `MSB3270`
"processor architecture mismatch" warnings in the ratchet baseline were reporting - Gentle itself is
`MSIL` and innocent. Deleting the TV service layer took the warning count from 2 to 0 and, more
importantly, removed the x86 pin from the server graph; see "What actually pins MP2 to 32-bit".

The removal touched seven surfaces, which is the checklist for removing *any* plugin in this tree:
project + `.sln` block and its `ProjectConfigurationPlatforms` lines, the feature `.wxs`, the
`FeatureRef` in `Server.wxs`/`Client.wxs`, the `DefineConstants` + `HarvestDirectory` pair in
`MP2-Setup\build.targets`, the `F_*` strings in `en-us.wxl`, the bootstrapper (`FeatureId` const,
`Features\Metadata\*.cs`, `strings_*.xml`, `Resources\Features\*.png`, the `.csproj` resource
entries) and `Setup\Tests` (the mock `BootstrapperApplicationData.xml`, `MockFeatureMetadataProvider`
and `PluginInstallationTests`).

`PluginInstallationTests` is worth a note: the deleted services were the only features in the
product that declared *related* features (`Service3` pulled `ServiceClient` in automatically), so
that branch of `FeatureUtils.GetSelectableChildFeatures` no longer has a real instance. The theory
was rewritten against the surviving features and now covers the parent check and the
"no relations" path.

### TvMosaic followed (2026-10-18)

`TvMosaic` was the last piece of the TV stack left standing: the provider, the API, the shared
contracts, the server component, the metadata extractor, its test project, its installer feature,
bootstrapper metadata, feature icon and the four Transifex translation strings - deleted on request,
**after** checking that nothing outside `TvMosaic` referenced it (the only inbound reference was the
`SlimTv.TvMosaicClient` feature id in `TvServiceClient.cs`, which declared a *relation*, not a code
dependency). `SlimTv.Interfaces` and `SlimTv.Client` remain, so the TV client contract is intact.

Deleting a feature turned out to need an invariant of its own: `PluginInstallationTests` covered the
relation and conflict rules *by example*, using exactly the two features that were deleted, so the
coverage silently vanished instead of failing. `Should_NotDeclareRelationsOrConflictsWithoutATest`
now enumerates every feature's `Relations`/`Conflicts` through the real
`AssemblyFeatureMetadataProvider` and fails if any pair exercises those code paths without a
`PluginInstallationTests` case. A feature that gains a relation must come with a test.

## Retargeting the tree to .NET 10 (done 2026-10-18)

`net48` is gone from everything the CI gate builds. **137 SDK-style projects** were rewritten from a
multi-target `<TargetFrameworks>` to a single `<TargetFramework>` - the census before the change was
128 × `net48;net6.0-windows`, 1 × `net48;net6.0`, 1 × `net6.0`, 1 × `net47;net6.0` - and the tree is
now **125 × `net10.0-windows` + 6 × `net10.0`** (the six plain ones are the two Incubator
`*OnlineLibraries`, `Webradio.Stations.Scrapper`, `Setup\CustomActions`, `Tests\Canary.Portable` and
`scripts\ci\drawing-probe`; plus six TvMosaic projects that were deleted the same day, which is why
the rewrite count is 137 and the census 131). The single-TFM decision is the user's: *"we do not need
a dual platform solution"*.

The **23 old-style (non-SDK) projects were deliberately left alone**. They are `Graveyard\`,
`Incubator` tools, `Resources\Templates`, the five `Backend\Database\*` backends, `HttpServer`,
`TvdbLib`, `ShellThumbnailProvider`, `MediaPortal.Manager` and `Test.MediaServer`, and **no gated
project references any of them** - which is what made the retarget self-contained. To keep the two
halves from contaminating each other, both `Directory.Build.props` files now apply the .NET Framework
reference-assembly rule only when `'$(TargetFramework)' == '' AND '$(TargetFrameworkVersion)' == 'v4.8'`,
i.e. to old-style projects, instead of keying off a `net48` TFM that no longer exists.

### What broke, in the order it surfaced

MSBuild **skips a project whose dependency failed**, so the first build reports the errors of the first
dependency layer only and the next build reveals the next layer. An error count going down after a fix
means nothing until a build reports zero. The list, with what each one actually was:

* **`WFO1000`, 17 sites, error-level on `net10.0-windows`.** Not a BinaryFormatter problem: the WinForms
  analyzer complains that a property of a `Component`-derived type has no code-serialization metadata. It
  fires on *any* `Component` subclass when `UseWindowsForms=true` - including
  `internal class AsyncImpersonationProcess : Process`. Fix is per-property
  `[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` (plus `using System.ComponentModel;`):
  9 properties in `SplashScreen.cs`, then `MainForm`/`CompressionWebClient`/`BaseClasses`/`SkinEngine\GUI\MainForm`/
  `UserControlLogMonitor` and the client's six designer forms.
* **`SYSLIB0011` is an error since .NET 8**, so `BinaryFormatter` could not be grandfathered.
  `TvdbLib\Cache\BinaryCacheProvider.cs` was deleted (its own comment said "broken at the moment -> use
  CacheProvider") and `DiskCachedDictionary` writes JSON via `JsonConvert` **without** `TypeNameHandling` -
  that option is how you reintroduce the exact deserialization gadget class BinaryFormatter is notorious for.
* **`NU1107` version conflict in `MP2-Client.sln`**, which had never restored in this project's history.
  Root cause: `BassRegistration 2.4.14.1`'s nuspec declares `<dependency id="BASS.NET" />` with **no version**,
  so NuGet resolves the feed's stale `1.0.0` for the transitive path, which ties with the direct `2.4.14.1`
  at equal distance for *indirect* consumers only. Fix is what NuGet itself advises: a direct
  `PackageReference` pin in the two failing projects (`Utilities`, `PartyMusicPlayer`). NuGet ids are
  case-insensitive, which is why this hides as `Bass.Net` vs `BASS.NET`.
* **`win7-x86` / `win7-x64` RIDs no longer exist** (`NETSDK1083`) → `win-x64`. `FreeImageLib\build.targets`
  references literal `runtimes\win7-x86\native` paths *inside* a NuGet package and must **not** be changed.
* **`CopyToOutput` (deploy to `MediaPortal\Bin`) started running in standalone builds.** With two TFMs the
  copy target had been conditioned away; the guard had to be re-derived. `Exists('$(SolutionDir)')` is not a
  solution build *is not* "SolutionDir is empty" and `$(SolutionName) != ''` is not it either: the SDK
  auto-detects a sibling `.sln`, and `Microsoft.Common.CurrentVersion.targets` (SDK 10.0.402, line 349)
  fills an empty `$(SolutionName)` with the literal `*Undefined*`. The working flag is `MP2IsSolutionBuild`
  in both `Directory.Build.targets` twins, consumed by 122 `build.targets` files - defined there because a
  target's *condition* is evaluated at execution time, after the project's own `build.targets` import.
* Stray XML from a bad `-->` (`MSB4067`), and four `#if NET6_0` / `#if !NET6_0` sites that had to become
  `#if !NETFRAMEWORK` / `#if NETFRAMEWORK` - a TFM-specific symbol is now always or never defined, which is
  the trap in collapsing a multi-target build.

### What it bought, measured on identical tooling

Both numbers below come from **the same parser on the same day on the same SDK**, the second one taken in a
`git worktree` checked out at the commit before the retarget, so the comparison is real:

| code | before (`net48;net6.0-windows`) | after (`net10.0-windows`) | reading |
|---|---|---|---|
| total findings | 687 | 777 | +90, and the arithmetic closes exactly (see below) |
| `SYSLIB0011` | 12 | **0** | BinaryFormatter gone from the tree, not suppressed |
| `NETSDK1138` | 1 | **0** | "net6.0 is out of support" - net10.0 is supported to 2028-11-14 |
| `CA1063` / `CA1001` | 201 / 44 | 190 / 42 | deleted code |
| `NU1902` | 0 | **50** | NuGet audit only runs for `net8.0`+ targets: 50 packages with known advisories |
| `NU1701` / `NU1510` | 0 | 20 / 20 | .NET-Framework-only packages; packages the SDK now supplies implicitly |
| `ASPDEPR008`, `SYSLIB0060`, `SYSLIB0045`, `NETSDK1189`, `NETSDK1206` | 0 | 4 / 2 / 1 / 1 / 1 | detectors that only the newer target switches on |
| `CA1416` | 179 | 196 | +17, all named, none of them new code |
| 18 other codes | — | — | **byte-identical** between the two measurements |

`+99` from detectors that the old TFMs kept switched off, `-27` from real removals, `+17` and `+1` from
detections the newer analyzer makes: that is the whole +90. The `CA1416` rise was checked call site by call
site: 12 of the 17 are in `MediaPortal.Server\MainForm.cs` and 5 in `ApplicationLauncher.cs`, all WinForms
members (`Application.Run`, `MessageBox.Show`, `ListView.*`, `NotifyIcon.Icon`), in files that contain no new
source (`MainForm.cs` gained a `using` and one designer attribute). Four `CA1416` findings disappeared, both
explained. See `notes.CA1416` in `warning-baseline.json`.

### How the decision was tested first

Asked "can we drop `net48` and move to the newest .NET?", the answer was measured, not argued.
`test-server-linux.ps1` takes `-SdkTag` and `-BuildArg`, and `Dockerfile.cross` forwards both into
the container. Three experiments on `MP2-Server.sln`, all re-run after the TV service layer was
removed (the numbers before that removal were 467 warnings / 3:01 on the unchanged tree and
**11** errors on the forced-`net10.0` probe - the extra error is explained below and is now gone):

| Experiment | Result |
| --- | --- |
| SDK **10.0.402**, tree unchanged (`net48;net6.0-windows`) | **0 errors**, 449 warnings, 2:26 |
| SDK **8.0.426**, tree unchanged (the committed gate) | 0 errors, 449 warnings, 2:26 |
| SDK 10, `TargetFrameworks` globally set to `net10.0-windows` | **10 errors**, 306 warnings, 0:40 |

The first two rows being identical is the point: the two SDKs disagree about nothing in this tree.
Both report the same **366 CA1416** sites, the same 216 `MSB3884` and the same 122 assemblies, so
choosing a newer target framework is not a toolchain risk at all - see "The warning surface is
toolchain dependent" for why *absolute* totals still must not be compared between hosts and
configurations. (CA1416 did not move when the TV layer was deleted: those projects had no visible
platform call sites, which is exactly what a `[SupportedOSPlatform]`-free, Windows-only plugin looks
like in this metric - another reminder that the ledger in `spine-baseline.json` is the real budget.)

Read the third row carefully, because it is **not** what it looks like. A global
`-p:TargetFrameworks=` does not reach every project: the 23 old-style projects use
`TargetFrameworkVersion` and ignore the property entirely. Before the TV service layer was deleted it
also did not reach the one SDK-style project that declared the *singular*
`<TargetFramework>net48</TargetFramework>` (`SlimTv.Service3.csproj`) - that leg failed with
`MSB3644`, an artefact of the probe rather than a porting problem, and it disappeared with the
project, which is why this row now reads 10 and not 11.

What was actually measured is therefore: **ten errors in two files, across every project that did
accept a `net10.0-windows` leg.** Both are `WFO1000` ("property does not configure the code
serialization for its property content"):

* `Core/MediaPortal.Utilities.Windows/SplashScreen.cs` - 9 properties of a WinForms splash form.
* `Core/MediaPortal.Common/Services/ResourceAccess/ImpersonationProcess/AsyncImpersonationProcess.cs:336`
  - `PriorityClass`, one property of a `Process` subclass.

They appear only on a `net10.0-windows` leg: the same SDK 10 building the then-committed
`net48;net6.0-windows` legs reports **zero** `WFO1000`. The WinForms source generator that
introduced the rule is enabled by the newer target, not by the newer SDK. The fix is per-property
(`[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` on runtime-only
properties), and the second one disappears on its own once `MediaPortal.Common` stops setting
`UseWindowsForms` - which is exactly the retargeting task the now-empty spine unlocked.

Nothing else. No missing API, no removed type, no package that refuses `net10.0` - because the
dual TFM was already compiling `net6.0-windows` everywhere, and .NET's compatibility surface
between 6 and 10 carries the rest.

Two things this measurement could not see, both now handled or reclassified:

* The **23 old-style (`non-SDK`) projects** - 12 of them are tools and templates, the rest are the
  five database backends, `HttpServer`, `TvdbLib`, `Test.MediaServer` and the dead
  `MediaPortal.Manager`. Overriding `TargetFrameworks` did nothing to them; they still need their
  project files rewritten, and they still cannot produce a `net10.0` output at all. What changed is
  that they no longer block anything: none of them is referenced by a gated project, so `net48`
  stopped being load-bearing the moment the SDK-style half moved.
* **`net6.0` was out of support since 2024-11-12** and `net8.0`/`net9.0` both end 2026-11-10, while
  `net10.0` runs to 2028-11-14 (from the dotnet/core releases index, not from memory). A port that
  landed on `net6.0` or `net8.0` would have landed on an unsupported runtime; the retarget removed
  that risk, and `NETSDK1138` leaving the baseline is the machine-checked proof.

Gotcha found while building this: Docker substitutes build `ARG`s in `ADD`/`COPY`/`ENTRYPOINT`/
`FROM`/`LABEL` only. A shell-form `CMD` that references `${BUILD_ARGS}` sees an empty string at
runtime, which silently dropped `-p:EnableWindowsTargeting=true` and produced 16 identical
`NETSDK1100` errors that looked like a toolchain verdict. The Dockerfile re-exports the `ARG` as
`ENV`; a probe that reports zero errors of a single code is the symptom to look for.

## What actually pins MP2 to 32-bit

`<PlatformTarget>x86</PlatformTarget>` appears in 22 project files and `x86` appears in the
solution configuration matrix of all three solutions (444 `x86` config entries in `MP2-Client.sln`
alone), which makes it look like a deep constraint. It is not. Measured, in order of how much they
really constrain a 64-bit build:

* **The observable switch is `Prefer32Bit`, not `PlatformTarget`.** Of the 18 executables in the
  tree, exactly two carry `Prefer32Bit=true`: `MediaPortal.Client` and `MediaPortal.Server`, both
  `AnyCPU`. On .NET Framework, `AnyCPU` + `Prefer32Bit=true` *is* "run 32-bit on a 64-bit OS", so
  today's shipped server is a 32-bit process even though nothing calls itself x86. On .NET Core and
  later the flag is ignored and bitness comes from the apphost RID, so retargeting to `net10.0`
  with no other change already produces an x64 process on an x64 SDK.
* **The last x86-compiled *managed* dependency is gone.** `MediaPortal.TvEngine.*` shipped
  `TvBusinessLayer`, `TvControl`, `TVDatabase`, `TvLibrary.Interfaces` and `TvThumbnails` compiled
  with `/platform:x86` (measured with `AssemblyName.ProcessorArchitecture`, not inferred from a
  folder name), which forced any referencing assembly into a 32-bit process. Only the TV service
  layer referenced them, and it was deleted; the two `MSB3270` architecture-mismatch warnings those
  references produced are now at zero. `Gentle` - the other foreign blob in that layer - is `MSIL`
  and was a *framework* blocker, not an architecture one.
* **Two checked-in dependencies are genuinely 32-bit-only**, and both are dead products: the
  Firebird set under `Backend\Database\FirebirdDatabase\References` (`fbembed.dll`, ICU 3.0,
  `msvcr80`, and `FirebirdSql.Data.FirebirdClient, Version=2.5.0.0 ... processorArchitecture=x86`)
  and `ScimoreDb` (client marked `X86`, native embedded server `X86`). Modern Firebird ships x64 and
  its current client is `AnyCPU`, so Firebird survives a move to x64; ScimoreDB has no vendor.
  `SQLCEDatabase` pulls `Microsoft.SqlServerCe`, a product Microsoft discontinued.
* **`EVRPresenter` is not a blocker**: the tree ships both `Release\Win32` and `Release\x64`
  native builds of the DirectShow EVR presenter, which is the usual reason media players stayed
  32-bit.
* **BASS and ffmpeg ship both bitnesses**, with three exceptions in the pinned package:
  `bass_adx`, `bass_aix` and `bass_wadsp` are 32-bit only. All three are emulator/game audio
  add-ons, not media-centre codecs.
* **DirectShow is the real reason to be careful, and it is a client problem.** The heaviest users
  are `UI\Players\VideoPlayer\Tools\FilterGraphTools.cs` (54 hits), `TsVideoPlayer.cs` (34) and
  `DirectShowWrapper\DirectShow.cs` (31). DirectShow filters are in-proc COM: a 64-bit process
  can only load 64-bit filters, so a 64-bit *client* changes what the user's installed codec pack
  has to be. That is a Windows-only concern (DirectShow does not exist on Linux or Android) and it
  belongs to the player the new frontend replaces, not to the backend.

Conclusion recorded for the plan: the **server and the portable core can go x64-only at once**;
x64-only for the Windows *player* is a decision about third-party filters, and Linux needs its own
answer per distribution architecture (x64 and ARM64), which 32-bit Windows support never provided.

### What was changed for x64-only, and one correction to the above

The claim that the two executables carrying `Prefer32Bit` were both plain `AnyCPU` was incomplete, and
the case the analysis missed was the only place in the tree where a solution genuinely built 32-bit:

* `MediaPortal.ServiceMonitor` defaulted `$(Platform)` to `x86` when built on its own, and
  `MP2-ServiceMonitor.sln` mapped `Debug|x86`, `Release|x86` and `Test|x86` straight to project
  configurations that set `<PlatformTarget>x86</PlatformTarget>`. It is the only one of the six
  solutions that does not alias its x86 platform to `Any CPU`, and it mapped all four of its projects
  that way - so `MediaPortal.Common`, `MediaPortal.Utilities` and `UPnP` were being built under a
  platform they never even declared.
* `MediaPortal.Server` carried both switches: `Prefer32Bit=true` on its `AnyCPU` groups *and* two
  `|x86` groups with `PlatformTarget=x86`, unreachable only because every solution containing it
  aliases x86 to `Any CPU`.

Removed: the x86 configuration groups in `MediaPortal.Server` and `MediaPortal.ServiceMonitor`, the
`Prefer32Bit=true` from `MediaPortal.Server` and `MediaPortal.Client`, `<LargeAddressAware>` from both
executables (a 4 GB address-space workaround that only means anything for a 32-bit image), the
`Platform == ''` -> `x86` default, and 24 mappings in `MP2-ServiceMonitor.sln`, which now aliases its
x86 and Test platforms to `Any CPU` like every other solution here. The monitor's output moves from
`Bin\MP2-ServiceMonitor\bin\x86\<config>` to `Bin\MP2-ServiceMonitor\bin\<config>`, which is where
`MP2-Setup.sln` has always put it. `MP2-ServiceMonitor.sln` builds clean after the change.

`architecture.ps1` is the guard, and it deliberately reads images rather than project files: the PE
`Machine` field of every apphost in `MediaPortal\Bin`, plus `AssemblyName.ProcessorArchitecture` of the
managed assembly next to it. All five built executables measure `x64` / `MSIL`. It also lists source
declarations but only fails on ones in SDK-style projects - the 42 remaining
`<PlatformTarget>x86</PlatformTarget>` lines are in the old-style tools and database backends, which
cannot produce a `net10.0` output at all, so they are reported as a purge list rather than as
violations. Grepping for `x86` and being satisfied with the count would have told you none of this.

The first run reported six executables, and one of them was a lie. `MP2-ServiceMonitor` appeared twice:
once in `bin\Debug\net10.0-windows`, written by the build that had just happened, and once in
`bin\Debug\net6.0-windows`, the output directory of the target framework the project had before the
retarget, which no build writes to any more but which nothing deletes either. Both measured `x64`, so
the verdict was accidentally right, but the table was answering a question about the disk. Every row now
carries the file's write time, and when one name appears more often than once the rows older than the
newest write are marked `stale`. That is the general lesson about reading build output as evidence:
442 directories named `net48`, `net6.0-windows`, `netstandard2.0` and similar still sit under `bin\Debug`
and `bin\Release` across the tree, all of them pre-retarget, none of them tracked. They are harmless to
a build and fatal to an inspection that does not ask when a file was written.

Left open deliberately, and named by the script so it cannot be forgotten: `MP2BootstrapperApp` still
declares `<RuntimeIdentifier>win-x86</RuntimeIdentifier>`, and the four WiX projects in `MP2-Setup.sln`
map their x86 platform to a real x86 build. A managed bootstrapper application is loaded by `burn.exe`
and has to match its bitness, so flipping the host RID alone would produce a setup that dies before its
first dialog - on a machine that can build installers, which this one cannot. It moves with the
packaging, as one change, verified by running the result.

## Known pre-existing breakage

`MP2-Client.sln` **does not restore** on the current upstream tree, independently of any work
here:

    UiComponents\Utilities\Utilities.csproj      : error NU1107: Version conflict for "BASS.NET"
    UiComponents\PartyMusicPlayer\PartyMusicPlayer.csproj : error NU1107: same

`BassLibraries` pulls `BassRegistration 2.4.14.1`, which depends on the package id `Bass.Net`,
while those projects reference the id `BASS.NET`. It is a package-graph conflict decided purely
by `PackageReference` entries, so no source change outside those two projects can cause or fix
it. Consequence for the gates: `build.ps1 -Solution Server -Ratchet` is the Windows regression
gate that actually runs; the Client solution cannot serve as one until this is resolved
(fix = settle on one BASS package id, or drop `BassRegistration`).

Workaround, and it is a good one: the two conflicting projects are UI *components*, and neither
`MediaPortal.Client` nor `SkinEngine` depends on them, so **individual Client-side projects still
restore and build**. `build.ps1 -Project` builds one project and injects the two properties MP2
expects (`$(SolutionDir)`, which `MediaPortal.Common\build.targets` needs to copy dependencies,
and `$(SolutionName)`, which selects the `Bin` subfolder); without them a bare project build dies
with `MSB3021: ... ..\..\..\Bin\*Undefined*\... Illegales Zeichen im Pfad`.

    .\build.ps1 -Project UI\SkinEngine\SkinEngine.csproj
    .\build.ps1 -Project Main\MediaPortal.Client\MediaPortal.Client.csproj

The warning floors are measured on a solution, so they are not applied to `-Project` builds;
`spine.ps1` still runs if you pass `-Ratchet`.

Two more things `-Project` builds reveal, both pre-existing:

* `Main\MediaPortal.Manager` is in **no solution** and three of its files still `using`
  `MediaPortal.Core.*` / `MediaPortal.UI.Presentation.*`, namespaces that do not exist in MP2 (they
  are MP1-era). It cannot compile and is not expected to. It also only defines `Debug|x86`, so a
  bare build needs `-MsBuildProperty Platform=x86` before it will even reach the compiler.
* `Extensions\MetadataExtractors\ShellThumbnailProvider` is in no solution either, and was
  unbuildable for two reasons that have nothing to do with each other. Its TFM was `v4.7` while
  every core project multi-targets `net48`, and MSBuild refuses a project reference from a lower
  .NET Framework version - that one is fixed here (raised to `v4.8`). Its `packages.config`
  dependencies (`Windows7APICodePack-Core`/`-Shell` 1.1.0.0) were never restored because no
  solution ever pulls the project; the `HintPath`s point into `MediaPortal\Packages`, which did not
  exist. That is a per-machine problem, not a source problem: `Packages` is gitignored, and
  `dotnet restore` does not understand `packages.config`, so the two `.nupkg`s (both on nuget.org as
  `windows7apicodepack-core`/`-shell` 1.1.0) have to be unpacked into
  `MediaPortal\Packages\Windows7APICodePack-{Core,Shell}.1.1.0.0\lib\` by hand. With both done the
  project builds clean (0 errors, 2 warnings) at `Platform=x86`.
* Some folders under `Source` contain source files that belong to no project (the orphan
  `UI\TV\SlimTvService` was one; the whole TV service layer was removed by decision, see
  "The TV service layer is gone"). They are not compiled by anything.

## Scripting against the Windows PowerShell host

Every script in this directory runs on Windows PowerShell 5.1 as well as 7, because that is what a
contributor's `powershell.exe` and a plain Windows agent have. Three things about it produced a *wrong
answer* rather than an error, so they are worth writing down:

* **`Write-Host` output cannot be redirected.** It goes to the information stream, and `>`,
  `Out-File`, `$(…)` and a `ConvertFrom-Json` pipe all miss it. `packages.ps1 -Format json` therefore
  writes to the success stream. A redirected run of the earlier version printed nothing and looked like
  "no advisories", which is the most dangerous kind of clean.
* **A script block passed where a .NET delegate is expected is not portable.**
  `[regex]::Replace($text, $pattern, { param($m) ... }, 'Singleline')` worked when the script was run
  directly and threw (`the property 'Value' was not found on this object`) the moment `build.ps1` ran
  the same script in the gate - so a check looked healthy in development and died silently in the
  pipeline. `architecture.ps1` now walks its matches with an explicit `foreach`. Related: PS 5.1 has no
  bare `Directory.Exists`/`File.Exists`, only `[IO.Directory]::Exists`/`[IO.File]::Exists`.
* **`Get-Content -Raw` decodes BOM-less UTF-8 as the ANSI codepage**, which mangles the typographic
  quotes that localized NuGet messages use (`„log4net"`), and a package-id regex then matches nothing.
  Anything that parses diagnostic text is read with `[IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)`.
* **The write direction adds a BOM that no source file has.** `Set-Content -Encoding UTF8` on PS 5.1
  emits UTF-8 *with* BOM; every `.cs` file in the tree is BOM-less (checked byte by byte across
  `MediaPortal.Common`, `MediaPortal.Common.Windows` and the Dokan move). A two-line log-prefix rename
  done with `Get-Content -Raw` + `Set-Content` therefore showed up in `git diff` as an encoding change on
  top of the edit, and the BOM only becomes visible when the next tool complains. Text edits to sources
  go through `[IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))`, which
  round-trips without a BOM on both hosts.

If a script here is ever ported to PS 7 only, delete this section; until then, test the interesting path
by invoking the script the way the gate does (`powershell -NoProfile -File wrapper.ps1` reproduces a
class of these that a direct run will not).

## Locale

Build output on many contributor machines is localized - the summary reads `0 Fehler` and
`Der Buildvorgang wurde erfolgreich ausgeführt`. Diagnostic *lines* keep the invariant
English shape `file(line,col): warning CODE: message`, which is what the parsers match.
Never script against the summary text.

What runs on Linux today is the **portability canary** only:

    .\test-canary-linux.ps1

That compiles a whitelist of `MediaPortal.Utilities` sources for `net10.0` and runs their tests
inside `mcr.microsoft.com/dotnet/sdk:10.0`. It is a real signal about a real slice of MP2, and it is
deliberately narrow. Everything else in the tree still targets a `-windows` TFM, which cannot execute
on Linux. So "the canary is green" must never be reported as "MP2 runs on Linux" - the retarget to
net10 changed the runtime of the build, not the platform support of the assemblies.

The canary exists so that Linux coverage grows file by file while the rest of the tree is
still Windows-only, and shrinks back to nothing once `MediaPortal.Common`/`Utilities`
themselves target a portable TFM. See the header comment in
`MediaPortal\Tests\Canary.Portable\Canary.Portable.csproj` for the per-file procedure.

## Compiling on Linux (the cross-compile probe)

    .\test-server-linux.ps1

This builds **all 84 projects of `MP2-Server.sln`** inside `mcr.microsoft.com/dotnet/sdk:10.0`
and is green: **0 errors**, ~2 min. Since the retarget this is a *pure* `net10` result — before it,
the same probe produced 129 assemblies (64 × `net6.0-windows`, 63 × `net48`) and needed the
`Microsoft.NETFramework.ReferenceAssemblies` shim to do that. There is no `net48` leg in the solution
anymore, so the shim only serves the old-style projects outside it now; keep the rule, those projects
still need it off-Windows. It is still a build-system result, not a runtime one: a `-windows`
assembly cannot execute on Linux. What it settles is that the remaining work is code that calls
Windows APIs, not build topology. Three things were needed:

* `-p:EnableWindowsTargeting=true`, so a non-Windows SDK will restore and compile `-windows`
  targets at all.
* `Directory.Build.props` (one in `MediaPortal\`, one in `MediaPortal\Source\`; MSBuild stops
  at the first one it finds, so both are needed) adding
  `Microsoft.NETFramework.ReferenceAssemblies` **only** when `'$(OS)' != 'Windows_NT'`, which
  makes the old-style `v4.8` projects compile off-Windows without touching Windows builds.
* `Tools\DevTools` had a `mkdir`/`xcopy` post-build event; on Linux MSBuild runs the event
  through `/bin/sh` and `xcopy` exits 127. It is now guarded by `Condition="'$(OS)' ==
  'Windows_NT'"`. Expect more of these - `xcopy` in post-build steps is an MP2 idiom.

**The inventory is the same on both hosts.** Parsing the container log and the Windows file log with
the same `Get-LogDiagnostics` gives **35 of 38 codes identical, `CA1416` = 196 on both**. The three
that differ (`NU1902`, `NU1701`, `NU1510`) are NuGet *restore* diagnostics which `dotnet build` prints
twice on the console - once under "Determining projects to restore…", once in the build phase - while
the MSBuild file log does not; keyed by (project, package, version, advisory) instead of by line, the
two hosts agree exactly (20 `NU1701` each, symmetric difference 0).

Which is also the second locale lesson: the container answers in English and the host in German, so
**message text is never a join key** - the host writes "Das Paket „log4net" … weist eine bekannte
Moderat Schweregrad-Sicherheitsanfälligkeit auf" for the finding the container calls "Package
'log4net' 2.0.14 has a known moderate severity vulnerability". Only `file(line,col): warning CODE` is
invariant; see "Locale".

## The dependency closure is not reproducible

**A clean checkout cannot restore, on any operating system.** `MediaPortal\NuGet.Config` adds
one feed:

    <add key="TeamMediaPortal source @ MyGet" value="https://www.myget.org/F/mediaportal/" />

Its v3 index answers `200`, but package lookup (`FindPackagesById`) answers **HTTP 500**, and
NuGet turns a failing source into a failed restore (`NU1301`, 292 of them on the first Linux
attempt). Removing that line leaves six package ids that exist on no other feed:

| Package | Needed by |
| --- | --- |
| `BASSCombined`, `BassRegistration`, `BASS.NET` 2.4.14.1 | `Extensions\BassLibraries`, `MetadataExtractors\AudioMetadataExtractor`, and transitively the Client NU1107 conflict |
| `MediaPortal.TagLib` | `AudioMetadataExtractor` |
| `ffmpeg` 3.4.2 | `Incubator\TranscodingService` |
| `OnlineVideos.Core` | `Incubator\MP2Extended` |

Windows builds pass only because these packages are already in the *developer machine's* NuGet
cache; there is no `Packages\` folder in the repository. Until that changes, a fresh clone, a
clean agent, or any container will fail at restore, and the "restore is green" premise behind
every later phase is not safe.

Two probes on 2026-10-18, one of them wrong, and the one that settles it:

- **Wrong.** `dotnet restore <sln> -p:RestoreSources=https://api.nuget.org/v3/index.json --force`
  exits **0** for both solutions, which looks like "nothing needs MyGet". It proves nothing: NuGet
  satisfies an exact pinned version from `%USERPROFILE%\.nuget\packages` before it asks any source,
  and all six ids are sitting in that cache on this machine. **A forced restore is not evidence of
  feed independence while the global package cache is warm.**
- **Right.** The same command with `-p:RestorePackagesPath=<empty directory>` fails as expected:
  `error NU1101: Das Paket "BASSCombined" wurde nicht gefunden` (also `BassRegistration`), and
  `error NU1102: Das Paket "BASS.NET" der Version (>= 2.4.14.1) wurde nicht gefunden - 1 Version(en)
  gefunden in "nuget.org" [Nächste Version: 1.0.0]`.
- **Corroborated without building anything:** `https://api.nuget.org/v3-flatcontainer/<id>/index.json`
  answers **404** for `basscombined`, `bassregistration`, `mediaportal.taglib`, `onlinevideos.core`;
  `bass.net` and `ffmpeg` exist but only as 1.0.0 and 1.1.3 against the 2.4.14.1 and 3.4.2 this tree
  asks for.

The probe works around it with a **read-only** fallback folder pointing at a populated cache
(`-e NUGET_FALLBACK_PACKAGES=...`, which never writes to the host cache) and deletes the dead
feed line inside the image layer only. The repository's own `NuGet.Config` is untouched,
because the real fix is a decision, not a patch:

1. Vendor the six `.nupkg` files into the repository and add a local source (self-contained,
   but redistributes third-party binaries - BASS and ffmpeg have licence terms to check first).
2. Re-point the references at packages that do exist (`ffmpeg` on nuget.org is a different,
   much older package; `BASS.NET` on nuget.org is 1.0.0, not 2.4.14.1 - so this is not a one-line
   change).
3. Keep a mirror of the old feed somewhere we control.

Whichever is chosen, the NU1107 Client conflict above should be fixed in the same step, since
it is the same package family.

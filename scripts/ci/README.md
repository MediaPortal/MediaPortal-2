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
| `warning-baseline.json` | Committed maximum allowed count per warning code, for `MP2-Server.sln` (see `"solution"`). `build.ps1 -Ratchet` uses `warning-baseline.<Solution>.json` when that file exists, which is how `warning-baseline.Frontend.json` is the floor of `MP2-Frontend.sln` and nothing else. |
| `tests.ps1` | Runs the Windows test suites (canary, server, client, installer) and prints one line each. Assumes `build.ps1` already built; `-Build` to rebuild. |
| `packages.ps1` | Package health from `obj\project.assets.json`: which packages carry advisories, which only work through the .NET Framework fallback, which references are now implicit. Deterministic, unlike the same codes in a build log. `-FailOn High` for a pipeline that must not ship one. |
| `satellites.ps1` | Enforces the satellite boundary: a namespace that only exists in a `*.Windows` project may not be imported by a project that does not reference it. |
| `architecture.ps1` | x64-only check, read off the built images: PE `Machine` of every apphost in `MediaPortal\Bin` and `AssemblyName.ProcessorArchitecture` of its managed assembly, plus a list of source declarations that still ask for 32-bit. |
| `Dockerfile.canary` | Linux verification image for the portability canary (`MediaPortal\Tests\Canary.Portable`). |
| `test-canary-linux.ps1` | Builds that image and runs the canary tests inside a Linux container. |
| `Dockerfile.cross` | Linux **cross-compile** image: builds the whole `MP2-Server.sln` under `mcr.microsoft.com/dotnet/sdk:$SDK_TAG` (default `10.0`). |
| `test-server-linux.ps1` | Runs that image (see "Compiling on Linux" below for the package-cache it needs). |
| `test-frontend-check.ps1` | Starts the real server, runs the built front end with `--check` against it, then against a port nobody listens on: exit 0 plus the server's own answers (the player's answer about whether this machine can play, and where the address it used came from), then exit 1 plus a readable message. `MP2_SETTINGS_FILE` is pointed at a temp file so the run can neither read nor overwrite the developer's saved server. While that server is up it renders four screens with `Frontend.RenderProbe` - home, the list of titles in one part of the library (twelve films served in the endpoint's own JSON, so the frame covers the parsing too), playback, and the pick-a-server screen with nothing found yet (`-SkipRender`, `-RenderDir`) - and it refuses to execute a DLL older than its own sources. |
| `test-frontend-discovery.ps1` | The autodetection gate. Deploys a server layout, starts the server, waits for its `UPnP server online hosting` line, and runs `MediaPortal.Frontend --discover` as its own process: the front end must find the server from its SSDP announcement alone (nothing is passed to it, and `MP2_SETTINGS_FILE` points at a file that does not exist), list it, and exit 0. Windows only: a container guest has no route to a host multicast group. |
| `Dockerfile.frontend` | Linux image that builds `MP2-Frontend.sln`, runs `Tests.Frontend` headless and renders four screens with `Frontend.RenderProbe` on Skia - no display, no `-windows` target, and deliberately no `EnableWindowsTargeting`. |
| `test-frontend-linux.ps1` | Runs that image (same package-cache requirement). `-ServeFromHost` starts the server on this machine and points the container at it through `host.docker.internal:55555`, so the home-screen frame the run produces is the populated screen. |
| `Dockerfile.drawing`, `drawing-probe/` | Not a gate: measures which half of `System.Drawing` works on Linux. Read it before changing what `spine.ps1` counts. |

## The gate, in the order that fails fastest

    .\build.ps1 -Solution Server,Frontend,Client -Ratchet   # builds, then warning + spine + satellite gates
    .\tests.ps1                                    # five Windows suites, against built output; refuses to run a stale one
    .\packages.ps1                                 # advisory and netfx-fallback inventory (a report)
    .\run-server-check.ps1                         # assembles the layout, starts the headless server, probes its HTTP
    .\test-server-upnp.ps1                         # multicasts an M-SEARCH at that server and wants a 200 OK
    .\test-frontend-check.ps1                      # runs the front end against that server, and against a dead port; renders the screens on Skia while the server is up
    .\test-frontend-discovery.ps1                  # starts a server and makes the front end find it over SSDP, with nothing told to it

On Linux: `.\test-canary-linux.ps1` runs the canary's tests in a container,
`.\test-server-linux.ps1` compiles the whole server solution there, `.\test-server-run-linux.ps1`
assembles the layout and *starts* the server there, and `.\test-frontend-linux.ps1` builds the front
end, runs its headless UI tests there and draws the screen with Skia. All four need a populated package
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

### Two builds on one working tree is one broken measurement

Recorded because it produced a *better-looking* number, which is the dangerous direction. Two
`build.ps1 -Ratchet` runs were allowed to overlap on this working tree (one Debug, one Release). The
`MP2-Server.sln` count then read **384** findings in Debug and **470** in Release against a floor of
**486** - "Improved, commit the new floor" - while simultaneously introducing codes the baseline had
never listed, two `CS0105`s from `Incubator\TranscodingService`. A drop spread over whole code families
(`CA1063` -6, `CA1065` -6, `CA1001` -4) is the signature the paragraph above warns about, only here
caused by the measurement rather than by the tree: a project whose compile was disturbed contributes
nothing, and the run that follows inherits its half-written state.

That is what happened next: the following run failed with **58 x `CS0006`** - "metadata file
`...\obj\Debug\net10.0\ref\MediaPortal.Common.dll` not found" - because one run's `Clean` deleted the
reference assemblies while the other was consuming them, leaving the up-to-date check pointing at
outputs that no longer exist. Recovery is mechanical: delete `obj\Debug` and `bin\Debug` of the projects
named in the `CS0006` lines (`MediaPortal.Common` and `MediaPortal.Utilities` here), then build one
solution at a time. Doing that reproduced **exactly the recorded floor, 486, green** - which is the
evidence that nothing in the tree had changed and that the "improvement" was never real.

Two rules follow. **Never let two `build.ps1` runs share a working tree** - a container build and a host
build are fine (separate trees), an overlapping host build is not. And when a count improves across
several unrelated code families at once, treat it as a measurement event until a clean sequential run
reproduces it; only then is it a floor worth committing.

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
script now states the rule (a project joins the ledger when a core project references it), plus one
deliberate exception: `MediaPortal.Server.Api` is listed although no core project references it - nothing in
the spine may know that assembly exists - because the ledger is a statement about target frameworks, and an
assembly served over the portable HTTP listener on Linux has no business hiding a `[DllImport]` from the only
gate that looks for them. Two counting
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

### The second subsystem out: Windows impersonation

`IImpersonationService` had one implementation, and it is a Windows logon session - `LogonUser`, a token
duplicated onto the calling thread, `CreateProcessWithToken` for external programs. Five files moved to
`MediaPortal.Common.Windows` (`WindowsImpersonationService`, `WindowsIdentityWrapper`, `TokenWrapper`,
`LogonHelper`, `AsyncImpersonationProcess`), the interface stayed in the core, and the core now registers
`CurrentUserImpersonationService`. `CA1416` fell **147 -> 140** and the core's P/Invoke column **86 -> 76**;
combined total unchanged at 180.

Three things are worth keeping from this one, because they were not obvious from the Dokan case:

* **The portable implementation is the existing no-credential path, member by member.** When no identity
  matches a path, the Windows class invokes the delegate directly (`action.Invoke()`), creates the process
  with `ProcessUtils.Create`, and `TryUnregisterCredential` returns `false`. So `CurrentUserImpersonationService`
  does exactly that for every path, and no caller gained a code path. The one member deliberately *not*
  faked is `TryRegisterCredential`: storing the credential and returning `true` would tell the caller it may
  reach a protected SMB share as another user, which is a promise this class cannot keep. It returns `false`,
  which `NetworkNeighborhoodResourceProvider` already handles.
* **`sealed` is a correctness detail for the ratchet.** The new class started as `public class` and the gate
  failed with `CA1063 +2` - "implement IDisposable correctly" wants the `Dispose(bool)` pattern, and it does
  not apply to sealed types. The moved Windows class is sealed, so it never had those findings. Two findings
  in a brand-new file are new debt, so the class was sealed rather than the floor raised. Note that the gate
  prints "Ratchet failed" *before* the spine and architecture sections, so a green-looking tail is not a green
  run - read the `findings:` line and the delta table, not the last screen.
* **A finding count and a source-line count differ when a line hosts two Windows-only members.** The
  impersonation move removed 6 source lines carrying 7 findings (`WindowsIdentityWrapper.cs` has 5 lines with
  7 column-distinct sites, `LogonHelper.cs` 1). Quoting either number without saying which unit it is makes two
  honest reports look like a contradiction.

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

## The other skins are gone, and so is the Graveyard (2026-11-05)

Decision, not decay: the front end is being rebuilt in Avalonia (see `docs\multiplatform\ROADMAP.md`), and the
only two MPF skins worth reading afterwards are **Nereus** and **WMC** - the user's call, and the reason they are
the two that remain under `Source\UI\Skins\`. Deleted:

- `Source\UI\Skins\BlueVision` and `Source\UI\Skins\Titanium Extended` - **1068 files** between them, including
  the `.sln` BlueVision carried inside its own folder.
- `MediaPortal\Graveyard\` - **842 files**, seven abandoned skins and an old `SlimTvClient`: ApolloOne,
  Black Backdrop, News, Reflexion, RisingSkin, Titanium, SlimTvClient. No `.sln` in the tree referenced anything
  in it (measured before deleting: zero matches for `Graveyard` across all `*.sln`), which is what made this a
  folder removal rather than a porting task.

`MP2-Client.sln` lost **28 lines** for the two projects: the two `Project(...)` headers, their two `EndProject`
lines, 22 build-configuration entries and 2 `NestedProjects` entries. Proved rather than assumed:

    .\build.ps1 -Solution Client     # BUILD OK (Client): 0 errors, 1423 findings
    .\architecture.ps1               # exit 0 - 277 project files scanned, 0 SDK-style x86 declarations,
                                     # 5 built executables all x64

`Client` still has no `warning-baseline.Client.json`, so the ratchet reports "no baseline" and passes for that
solution; the 1423 above is the number to calibrate from if that ever changes. One caveat so that nobody re-derives
a wrong figure from this file: the count of old-style (non-SDK) projects earlier in this document says 23, which
was a count of named folders at the time of the .NET 10 retarget. Measured now by scanning every `.csproj` for an
`Sdk=` attribute on its `<Project>` element: **37 of 157** project files are old-style, 42 of them carry a
`<PlatformTarget>x86</PlatformTarget>` declaration, and none is referenced by a gated project.

What was deliberately **not** touched: the `Skin\BlueVision\…` fragment folders that live *inside* live plugin
projects (`UiComponents\News`, `TV\SlimTvClient`, `Incubator\AppLauncher`, `HomeEditor`, `Trakt`,
`InputDeviceManager`). Those are content items of plugins that remain, they only ever render if the BlueVision
skin is installed, and editing six project files to remove inert content is churn with no behaviour behind it. If
BlueVision fragments are wanted gone for tidiness, that is a pass of its own with the client build as its proof.

## Retargeting the tree to .NET 10 (done 2026-10-18)



`net48` is gone from everything the CI gate builds. **137 SDK-style projects** were rewritten from a
multi-target `<TargetFrameworks>` to a single `<TargetFramework>` - the census before the change was
128 × `net48;net6.0-windows`, 1 × `net48;net6.0`, 1 × `net6.0`, 1 × `net47;net6.0` - and the tree is
now **125 × `net10.0-windows` + 6 × `net10.0`** (the six plain ones are the two Incubator
`*OnlineLibraries`, `Webradio.Stations.Scrapper`, `Setup\CustomActions`, `Tests\Canary.Portable` and
`scripts\ci\drawing-probe`; plus six TvMosaic projects that were deleted the same day, which is why
the rewrite count is 137 and the census 131). The single-TFM decision is the user's: *"we do not need
a dual platform solution"*.

The **23 old-style (non-SDK) projects were deliberately left alone**. They were the `Graveyard\` projects (since
deleted - see "The other skins are gone, and so is the Graveyard"), `Incubator` tools, `Resources\Templates`,
the five `Backend\Database\*` backends, `HttpServer`,
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
contributor's `powershell.exe` and a plain Windows agent have. These are the things that produced a *wrong
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
* **The format operator cannot be appended to a `Write-Host` call.** `Write-Host ("a" + "b {0}") -f $x
  -ForegroundColor Yellow` binds `-f` as a *parameter of Write-Host*, which has none: the statement throws a
  "parameter cannot be found" error, the message never prints, and with `$ErrorActionPreference = 'Continue'`
  the script carries on as if nothing happened - so the guardrail that was supposed to warn existed and
  printed nothing. Build the string first (`$text = "..." -f $x; Write-Host $text -ForegroundColor Yellow`).
* **A .NET method exception inside an `if` condition kills only that statement.**
  `[datetime]::TryParse($s, $ci, [DateTimeStyles]::AdjustToUniversal -bor [DateTimeStyles]::RoundtripKind, [ref]$out)`
  throws (`RoundtripKind` cannot be combined with `AdjustToUniversal`) instead of returning `$false`, so the
  `[ref]` target stayed at `[datetime]::MinValue` and the guard reported "an image from 0001-01-01". The plain
  two-argument `TryParse` handles an ISO-8601 `Z` value and converts it to local time. Rule: when a
  *guardrail* prints a suspicious value, check the guardrail before believing the thing it guards.
* **`.NET` path APIs ignore PowerShell's location.** `[IO.File]::ReadAllText('scripts\ci\build.ps1')` resolves
  against the *process* working directory, not the `Set-Location` directory, and failed with "part of the path
  could not be found" while the same relative path worked for `Get-Content`. Use absolute paths or
  `(Resolve-Path ...)` with the .NET APIs.
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

**`-SkipImageBuild` does not skip a cache, it skips the sources.** `Dockerfile.cross` does `COPY . /src`, so
the image carries a snapshot of the tree taken when the image was built. Running with `-SkipImageBuild`
therefore compiles *that snapshot*: it reported 634 findings / `CA1416` 147 while the same tree on Windows had
already moved to 627 / 140, i.e. a green probe that had nothing to do with the working tree. The script now
compares the image creation time with the newest `.cs`/`.csproj`/`.props`/`.sln` and prints a `WARNING:` line
naming both timestamps (verified with a deliberately newer file). Drop the flag for a real answer.

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


## `GenerateAssemblyInfo=false` deletes the platform declaration, and `CA1416` notices

`MP2-Server` reports `CA1416` on `Application.Run(form)`, `MessageBox.Show(...)`, `ServiceBase.Run(...)`
and on every call into `MediaPortal.Common.Windows` - in a project whose target framework is
`net10.0-windows` and which sets `UseWindowsForms`. The message is always *"this call site is reachable
on all platforms"*, which is the message the analyzer uses when the **caller** declares no platform.

It is not a puzzle and it is not debt. The SDK derives `[assembly: SupportedOSPlatform("Windows7.0")]`
from a `-windows` target framework and emits it **as part of the generated assembly info**; these
executables set `GenerateAssemblyInfo=false` because they hand-write `VersionInfo.cs`, so the platform
attribute is never generated. Verified three ways:

- `MediaPortal\Source\Main\MediaPortal.Server\obj\Debug\net10.0-windows\` contains no
  `*.AssemblyInfo.cs` at all, while `MediaPortal.Common.Windows`'s generated file contains
  `SupportedOSPlatformAttribute("windows")` (from the explicit `AssemblyAttribute` item) *and*
  `SupportedOSPlatformAttribute("Windows7.0")` (from the target framework).
- Adding an `<AssemblyAttribute>` item does **nothing**, because that item is consumed by the
  `GenerateAssemblyInfo` target - the very target `GenerateAssemblyInfo=false` switches off. That was the
  fix attempted here first. It changed nothing, and the "proof" that it had worked was worse than the fix:
  the filter `MediaPortal\.Server\.csproj.*warning CA1416` cannot match, because in an MSBuild log the
  `[Project.csproj]` tag comes *after* the warning text. A zero from a pattern that cannot match is the
  most convincing wrong number there is.

- What works is the `MP2AddWindowsPlatformAttribute` target in both `Directory.Build.targets` twins. It
  writes `[assembly: SupportedOSPlatform("Windows7.0")]` into `$(IntermediateOutputPath)`, adds the file
  to `@(Compile)` and stops for portable projects at the condition - which is exactly what the SDK's own
  `GetAssemblyAttributes` does, minus the version attributes this tree writes by hand. It applies to
  **106 projects**, none of them a portable one, and took `CA1416` from 131 findings to **0** in both
  solutions while every other warning code stayed identical to the finding (618 -> 487, `-131`, no other
  code moved).

`Application.Run(form)` in a WinForms application, `MessageBox.Show`, `ServiceBase.Run` and the three
satellite `Register()` calls in `ApplicationLauncher.cs` were never port debt. They were the sound of an
analyzer being told nothing about the caller. The `CA1416` floor is now **0**, stricter than any number
this tree has carried: a `CA1416` finding from now on means a *portable* assembly touched a Windows-only
API, and that stops the build.

Two rules follow. When a `-windows` assembly reports `CA1416` on everything it touches, look for the
platform declaration before believing the count. And when a satellite member called from a Windows-only host
produces findings, say which of the two cases you are looking at - a ratchet floor that moves for an
unexplained reason stops being evidence.

## Attribute a delta by measuring both sides, not by arithmetic

The step that translated the crash logger and moved the power handling measured **-9** `CA1416`
(140 -> 131). The inventory of the files it touched said -20. Both numbers are right and neither
explains the other: 11 findings appeared in `MediaPortal.Server` (the paragraph above). The only way to
get an attribution that survives review is to build the *old* commit too:

```powershell
git worktree add ..\mp2-baseline-check HEAD     # a second checkout of the same repo, no new clone
Set-Location ..\mp2-baseline-check
New-Item -ItemType Directory -Force build\artifacts | Out-Null   # the gate does not create it
& scripts\ci\build.ps1 -Solution Server
# then group 'warning CA1416' lines by file:line and Compare-Object against the new tree's log
```

`git worktree` shares the object database, so this costs a restore and a build, not a clone. Remove it
with `git worktree remove ..\mp2-baseline-check --force` (build output makes it dirty on purpose).

Use the same "count on both hosts" trick for `CA1416` parity: the Windows build log is German and the
container's is English, so comparing message text gives 107 vs 108 and looks like a difference. Compare
`file:line` sets instead - those came out identical at 107 sites, with 618 gated findings on both hosts.

## Transitive package assemblies land in plugin folders

`UiComponents\News\build.targets` copied `System.ServiceProcess.ServiceController.dll` into
`Plugins\News` although no code in that plugin uses it, simply because `CopyLocalLockFileAssemblies`
puts every transitive package assembly into a project's `bin` - and the plugin got it as long as
`MediaPortal.Common` happened to reference the package. Removing the package from `Common` turned that
into `MSB3030: could not copy ... file not found`, i.e. a build break in an unrelated plugin. When a
`build.targets` copy list names a third-party DLL, check which project actually depends on it before
assuming the plugin does.

## Known flakes: the client suite is timing-sensitive

`tests.ps1` once reported `Tests.Client` 89/90 with one failure while a Docker Desktop Linux
build was saturating the same cores; run alone on an idle machine the suite is 90/90, and so is
`tests.ps1` (verified again after the change). The candidates are visible in the source:
`TestWifiRemote.cs` blocks on `WaitOne(5000)` (and one `WaitOne(10)`) waiting for network
handshakes, and `TestAsync.TestTimeOut` asserts a timeout threshold directly.

So: **do not read a single client failure as a regression while a container build is running.**
Re-run the suite alone before believing it, and never "fix" it by raising a floor. A named client
test that fails repeatedly on an idle machine is real.

## A framework reference flows through a project reference

`MediaPortal.Common` targeting `net10.0-windows` with `UseWindowsForms=true` did not just make
*itself* a Windows assembly. `UseWindowsForms` becomes a `FrameworkReference` to
`Microsoft.WindowsDesktop.App`, and framework references flow through `ProjectReference`, so every
project that referenced `Common` could compile `System.Windows.Forms.*` while declaring nothing of the
sort. Flipping `Common` to `net10.0` therefore failed 28 errors in two projects that had never
mentioned WinForms:

```
MediaPortal.UI\General\WindowsMessaging.cs(25,22): error CS0234: ... "Forms" ist im Namespace "System.Windows" nicht vorhanden
MediaPortal.ClientLauncher\ApplicationLauncher.cs(55,20): error CS0246: ... "NotifyIcon" wurde nicht gefunden
```

Both were honest WinForms users (`WindowsMessaging` handles `System.Windows.Forms.Message`, the
launcher shows a tray icon) and now set `<UseWindowsForms>true</UseWindowsForms>` themselves. The
check before retargeting a widely-referenced project: grep its consumers for `System.Windows.Forms`,
`System.IO.Ports`, `WPF`-only and `WindowsDesktop` types, and compare against what those projects
declare. Expect the fix to be "declare what you use", not "put the framework reference back".

## Prove a portable project's `CA1416` gate actually bites

`<WarningsAsErrors>CA1416</WarningsAsErrors>` in a portable project is worth nothing until it has
failed once. It takes ten seconds to find out, and this is how `MediaPortal.Common` was checked after
the flip:

```powershell
# one unguarded Windows-only call into the portable core
#   var probe = Microsoft.Win32.Registry.CurrentUser;
dotnet build MediaPortal\Source\Core\MediaPortal.Common\MediaPortal.Common.csproj -c Debug
# expect: error CA1416 ... "Registry.CurrentUser" nur unterstützt für: 'windows'   and exit 1
```

Remove the line afterwards, of course. If the build stays green, the gate is decorative - usually
because the assembly declares a platform it should not (see the `GenerateAssemblyInfo` section above)
or because `AllRules.ruleset` downgraded the diagnostic.

## A green test suite can be a stale one

`tests.ps1` runs `dotnet test --no-build` by default, on purpose: the reference closure of these suites
is most of the tree, and rebuilding it before every suite costs more than running them. The contract is
"build first" - which turned out not to be a contract anyone keeps, because the failure is silent. Added
a test after the last build, ran the gate, and it reported 122 passed while the assembly on disk held 123
tests: the newest one simply was not in the binary that ran, and the line was green either way.

The gate now compares the suite's own sources with its own assembly before running it, and says no:

```
=== Tests.Server
  STALE: Tests.Server.dll was built at 2026-10-18T12:04:39, but TestSQLiteValues.cs is newer (2026-10-18T12:06:46).
  The suite would run a binary that does not contain the current sources. Run tests.ps1 -Build, or build the solution first.
```

The comparison is deliberately narrow - the suite's folder against its own output - because a correct
transitive closure check would need MSBuild's project graph for every suite. It is a floor, not a
guarantee: an edit to `MediaPortal.Backend` after the build still needs `-Build`. Writing it also
produced a lesson worth repeating: the first version globbed for `"$projectLeaf.dll"` where
`$projectLeaf` was the *project file* name, so it looked for `Tests.Server.csproj.dll`, found nothing,
and the check did nothing at all. A guard that silently opts out is the same class of defect as the
green-but-stale suite it was meant to catch, so it was tested by touching a source file and requiring the
red output.

## The server runs: `deploy-server-layout.ps1` and `run-server-check.ps1`

Every gate in this folder until now answers a question about the compiler. These two answer the other
question, and the difference turned out to be three bugs.

`deploy-server-layout.ps1` assembles a directory that can actually start: `dotnet publish` of
`MediaPortal.Server.Headless`, then one directory per plugin under `Plugins\`. The plugin half is a
script and not MSBuild because MP2's own plugin deployment (`build.targets` in ~100 projects) copies
into `$(SolutionDir)..\Bin\$(SolutionName)\bin\$(OutDir)\Plugins\` under
`Condition="'$(MP2IsSolutionBuild)' == 'true'"` - a property `dotnet publish` never sets, writing a
Windows installer staging tree. What a plugin directory must contain is not "the build output":
`plugin.xml` is source, and for the SQLite plugin the four driver assemblies (`Microsoft.Data.Sqlite.dll`
plus `SQLitePCLRaw.batteries_v2.dll`, `.core.dll` and `.provider.e_sqlite3.dll`) are copied next to it
together with the *platform's* `e_sqlite3` native. Flat, in the plugin folder, not in an `x64\`
subfolder: SQLitePCLRaw probes the directory of `SQLitePCLRaw.provider.e_sqlite3.dll`, while the older
`System.Data.SQLite` wanted `x64\SQLite.Interop.dll`. A missing driver assembly fails the layout build
instead of producing a plugin that throws later.
`Source\Backend\Database\SQLiteDatabase\build.targets` is the reference for those rules.
`Source\Backend\Database\SQLiteDatabase\build.targets` is the reference for those rules.

Since the metadata-import slice the script deploys a second plugin, `NfoMetadataExtractors`, and what makes that one
different is instructive: it is the first plugin in the tree whose `-windows` target framework was *removed* (it was
Windows-only only because of a `ProjectReference` on the online-metadata library that two files named without using a type
from it - see section **aj** of `docs/multiplatform/ROADMAP.md`), and it is the first plugin deployed whose directory must contain a third-party assembly the
plugin itself depends on, `Newtonsoft.Json.dll`, because `plugin.xml` now asks the loader to load it by name from the
plugin's own folder. That copy needs `CopyLocalLockFileAssemblies=true` in the project - without it a package assembly
never appears in `bin` and MSBuild copies nothing at all silently - so the script checks the file exists after building and
throws if it does not, rather than shipping a plugin whose assembly load fails when the server starts.

`run-server-check.ps1` starts `MP2-Server-Headless --data <scratch>` and asserts six things,
because "the process exited 0" alone is not one of them:

1. it exits by itself within the timeout,
2. with exit code 0,
3. `<DATA>\Log\Server.log` contains `Switching to running state`,
4. `<DATA>\Database` is not empty,
5. every URL in `-ProbeUrl` answers `200`, and an entry written as `url|text` also answers with a body
   containing that text,
6. the auth checks answer as specified: `GET api/session` is `403` without a token and `403` with a token this
   server never signed, and `POST Token` answers `400` with `unsupported_grant_type` for a grant it does not
   implement and with `invalid_grant` for a profile that does not exist.

Assertion 4 is what proves the database layer rather than the service registration: a fresh run leaves
`Datastore.s3db` plus its `-wal`/`-shm` partners there. The data directory is kept in both outcomes -
the log inside it is the only diagnostic there is - and its path is printed. Assertion 5 is what proves
the HTTP layer, because "the listener started" and "the server answers" are different statements. The two
default probes are the resource server's health endpoint (a delegate mapped onto the pipeline, answering
without plugins, users, tokens or a database) and `api/serverinfo`, an MVC controller discovered as an
application part of the resource server's assembly - the two ways an endpoint exists here, each proven over a
socket. Probing needs the server to stay up, so a probing run passes `--duration` rather than `--check`;
`-ProbeUrl @()` restores the old, non-probing behaviour.

Assertion 6 is the same trick one layer up, for the part of the HTTP layer that decides *who* is asking. It asks
four questions and expects refusals, which is what a fresh server can answer: the positive path - a real profile
getting a real token and opening `api/session` - needs a profile to exist, and a fresh data directory has none.
That was measured, not assumed: after a first run the `USER_PROFILES` table in `Datastore.s3db` holds `0` rows,
because the first profile is created by a client through the UPnP `CreateProfile` action. So the gate takes
`-AuthTokenUser <name>` to walk the positive path against a `-DataDir` that has profiles, and prints which of the
two it measured rather than quietly passing a shorter list. The positive path itself is covered by
`Tests.Server\Api\TestTokenEndpointOverHttp.cs`, which starts `ResourceServer.ConfigureApiServices` and
`ConfigureApiPipeline` - the same two methods both HTTP hosts are handed - on the portable listener over a
loopback socket. That split is deliberate: middleware order is the thing no unit test of the parts can see, and a
test that builds the server's real pipeline sees it while a container image does not have to be rebuilt to check it.
`-AuthProbeUrl ''` drops assertion 6, for running the script against a server that is not MP2's.

The body is printed on purpose, and it is checked: the default entry asks for `MediaPortal 2 Server` in the
answer, because a status code on its own is also produced by whatever proxy sits in front of the server.
Among the reported fields, `PathBase`/`Path` are the ones that settled a question this repository had
answered wrongly once: http.sys moves the prefix it registers into `PathBase` and lets `Path` continue behind
it, and the shared Kestrel listener used to leave the whole path in `Path`. That disagreement meant a route
written with the prefix in it could only ever match on one of the two platforms. The shared listener now
reports the http.sys shape, so both gates print `pathBase=/MediaPortal/Server` and `path=/health` for the same
request, and routes are written relative to the prefix everywhere. The SQLite half is no longer a caveat
either: the layout copies the platform's own `e_sqlite3` native and the server logs `SQLiteVersion` from an
open connection, on both platforms.

### MSBuild's `-getProperty` needs `-noconsolelogger`

Asking MSBuild where a project builds to beats globbing `bin\<Configuration>\*\` - stale
pre-retarget `net48` and `net6.0-windows` output folders are still on disk here, and a glob finds all
three. But the plain invocation puts the build's own warnings in front of the value:

```powershell
dotnet msbuild some.csproj -getProperty:TargetDir -p:Configuration=Debug -v:q
# ...NU1510 warning lines..., then the JSON. ConvertFrom-Json then fails with
# "Invalid JSON primitive: D." - the D of a drive letter.
```

`-noconsolelogger` silences the build logging and leaves only what the CLI prints. One property prints
the bare value; two or more print a JSON object - `deploy-server-layout.ps1` handles both spellings.

### A PowerShell function returns your build log

`function F { dotnet build ...; return $dir }` returns an *array*: the command's stdout is part of the
function output. The symptom was a "deploying plugin from <several warning lines and a path>" message
followed by `Join-Path : Cannot bind argument to parameter 'Path' because it is an empty string`. Pipe
chatty commands to `Out-Host` (writes to the host, contributes nothing to the pipeline) or to
`Out-Null`. Two related traps hit in the same file:

- `"output for plugin $Name:`n..."` is a parse error, not a string: after `$Name` a colon starts a
  drive-qualified variable reference. Write `$($Name):` or rephrase.
- `$LASTEXITCODE` after `& $tool @args | Tee-Object ...` reflects the *native command*, which is what
  you want, but after a pipeline that ends in a cmdlet which can fail (like `Select-Object -First N`,
  which can stop the pipeline), it can be `-1` for a build that succeeded. Judge builds by
  `$LASTEXITCODE` captured immediately after the tool call.
## The server answers: `test-server-upnp.ps1`

`run-server-check.ps1` proves the listeners come up and go down. It says nothing about whether they
answer anything, which stopped being theoretical the day the SSDP receive loop was rewritten. So the
headless host grew `--duration <seconds>` (stay up that long, then shut down exactly the way `--check`
does - `WaitForExitRequest(TimeSpan)`, so Ctrl+C still cuts it short) and this script talks to the
running server:

1. start with `--duration`, wait for `UPnP server online hosting` in the log,
2. multicast an M-SEARCH (`ST: ssdp:all`, `MX: 1`) to `239.255.255.250:1900`,
3. require a `200 OK` whose `LOCATION` names MP2's own UPnP listener **and** whose `SERVER` header says
   `MediaPortal-2-Server` - other devices on the LAN will answer the multicast too, and "somebody
   answered" is not the assertion,
4. require exit code 0, no `[ERROR]` line and no `Cannot acquire synchronization lock` line, because
   the shutdown is the part that had the bug.

Measured on the first green run: seven responses, the root device plus one per service.

Multicast, not unicast, and that was measured too: an M-SEARCH sent to `127.0.0.1:1900` gets no answer
at all on Windows. Several sockets share UDP port 1900 through `SO_REUSEADDR` - here MP2's three
endpoints plus the Windows SSDP service - and Windows gives a *unicast* datagram to one of them, which
is not the one MP2 is listening with. Multicast is what every real control point sends anyway.

Two shutdown defects surfaced only because this script exists:

- `SSDPServerController.Close()` called `Timer.Dispose(WaitHandle)` and waited on that handle **while
  holding** `SyncObj`, while both timer callbacks take `SyncObj` (the advertisement one with
  `Monitor.TryEnter(SyncObj, TIMEOUT_TIMER_LOCK_ACCESS)`). Every shutdown that raced a timer tick
  therefore waited out the full lock timeout - measured as a 30 second stall plus
  `Maybe a deadlock happened.` in the log - and only ended because the timeout exists. Stopping and
  awaiting the timers happens outside the lock now.
- Closing a UDP socket with a receive pending faults the `Task` behind `BeginReceiveFrom` (which is
  built on `ReceiveFromAsync`), nobody observes it, and the finalizer hands it to
  `TaskScheduler_UnobservedTaskException` as an application error - two to six `[ERROR]` lines per
  shutdown. The server's listener uses `SocketAsyncEventArgs` now, where the abort is a `SocketError`
  value. The client-side listeners still use the APM pair, so the launcher classifies abort-only
  unobserved exceptions as teardown (`Debug`) rather than failure (`Error`); see Phase 1h in the
  roadmap.

### Comment-based help and `-File`

Both bit while writing the script above:

- A file whose first line is `#<#` is **not** a comment block: `#` starts a line comment, so the rest of
  the help text is parsed as code. Windows PowerShell accepts `<#` as the first token of comment-based
  help - that is what every other script in this folder uses. The symptom is unrelated parse errors on
  the help text's own lines ("Unexpected token 'the'").
- `powershell -File script.ps1 -Solution Server,Client` passes the single string `"Server,Client"` to a
  `[string[]]` parameter, which fails its `ValidateSet`. Array arguments need `-Command`, or repeated
  `-Solution Server -Solution Client`.
- Both scripts in that pair, and `deploy-server-layout.ps1`/`run-server-check.ps1`, also had to lose
  every hardcoded backslash before they would run in a Linux container: `Join-Path $repoRoot
  'MediaPortal\Source'` yields a path containing a literal backslash there, i.e. one *file name*.
  Assemble paths from components (`Join-Path (Join-Path $root 'MediaPortal') 'Source'`) in anything a
  container is expected to execute.
- Related, and nastier because it is silent on the build host: do not assign to `$IsWindows`,
  `$IsLinux` or `$IsMacOS` in these scripts. pwsh defines them as **read-only** automatic variables, so
  `$isWindows = ...` fails with "Cannot overwrite variable IsWindows because it is read-only or
  constant" - under Windows PowerShell 5.1, which the build host runs, those names do not exist and the
  same line works. It stopped `deploy-server-layout.ps1` in the middle only inside the container. Ask
  .NET instead (`[System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(...)`) and store the
  answer in a name that is not reserved (`$onWindows`).

## The server runs on Linux: `test-server-run-linux.ps1`

`test-server-linux.ps1` answers "does it compile there?", which is a question about the toolchain.
This one starts the server: `Dockerfile.server` copies the tree into a .NET 10 SDK image, installs
PowerShell with `dotnet tool install --global PowerShell` (the SDK image already has `dotnet`; this
avoids a Microsoft apt feed, GPG keys and `curl` in the image), and its `CMD` runs the *same* two
scripts the Windows gate runs:

    docker build -f scripts/ci/Dockerfile.server -t mp2-server .
    docker run --rm -v <nuget-cache>:/nuget-cache:ro -e NUGET_FALLBACK_PACKAGES=/nuget-cache mp2-server

or `.\test-server-run-linux.ps1`, which does both and maps the exit code. Keeping the logic in the
scripts rather than in a `RUN` line is deliberate: a fix then applies to both platforms at once, and a
green result here means the same thing as a green result there.

`build/` is in `.dockerignore`, so the container never sees the Windows layout; it builds its own from
the copied source. `-SkipImageBuild` therefore means "run the snapshot currently in Docker" - the same
caveat as the cross-compile probe, which prints a warning comparing the image timestamp with the newest
source file.

What it currently measures: the layout assembles, the host executes on .NET 10.0.13, `SystemResolver`
reports a local system id, the SQLite plugin loads the platform's `libe_sqlite3.so` and logs
`SQLiteVersion: 3.53.3`, the subschemas `MediaPortal-Basis`, `MediaLibrary`,
`UserProfileDataManagement` and `ClientManager` are created from their SQL scripts, four HTTP endpoints
(`/MediaPortal/Server`, two control points, one UPnP server) take port 55555 together through
`SharedHttpPortHost`, and the log reaches `HeadlessServerLauncher: Switching to running state`. Zero
`FATAL`, zero `ERROR`; the seven `WARN` lines are the documented "no file system driver to mount
resources" note plus `Could not set IPv4 options / Cannot assign requested address` from the SSDP and
GENA multicast setup, which needs a NIC with a multicast route - see the last paragraph. ROADMAP items
**d** and **g** are measured on Linux now, not argued.

The loop this probe is built for - fix the first failure, rebuild, read the next one - ran three times to
get here: the SQLite interop library, then `HTTPAPI.dll`, then a `\Scripts\` path. Two of those three
took an extra round because of a trap in the loop itself: `-SkipImageBuild` runs the image that is
currently in Docker, and the image holds a *snapshot* of the tree. The script compares the image
timestamp with the newest source file and prints a yellow warning, and twice that warning was scrolled
past while reading the filtered output, which produced two confident readings of a binary that no longer
existed. Nothing in the gate can tell a stale reading from a real one if its output is filtered down to
the interesting lines: rebuild the image whenever the tree moved.

Reading that list in order is the point of the probe, and it has been misleading in both directions. When
three `FATAL`s were listed, the third (`ServiceNotFoundException` during teardown) looked like a
consequence of the first; it was not, it was `Stop()` running twice, and it stayed after the database
started working - with a different service named each time a caller was made tolerant. Conversely the
count is now smaller than the number of fixes: teardown tolerance alone removed two of the three.

Two things it does not cover, both on purpose. The UPnP gate stays on Windows because Docker Desktop's
NAT does not give a container the LAN's multicast group, so `test-server-upnp.ps1` would be testing the
VM's networking rather than MP2. And the check stops at process teardown: it does not assert that the
server served anything, which is what the Windows UPnP probe is for.


## The front end: `MP2-Frontend.sln` and `test-frontend-check.ps1`

`MediaPortal\Source\Frontend\MediaPortal.Frontend` is the Phase 2 UI, and it is the first thing in this
tree that is neither a server assembly nor a `-windows` one. It has its own solution
(`MP2-Frontend.sln`) and its own floor (`warning-baseline.Frontend.json`), because `build.ps1 -Ratchet`
now looks for `warning-baseline.<Solution>.json` and uses it when present - otherwise a second solution
would either be scored against the server's numbers or not scored at all (`warnings.ps1` prints "Not
scored: this baseline describes 'Server'" and exits 0 for an unknown solution, which is exactly the kind
of green that means nothing). The Frontend floor is 237 findings and every one of them is a NuGet code
inherited from the `Server.Api` -> `Backend` -> `Common` -> `Utilities` closure its tests reference; **the
front end's own code contributes 0**, and that is what the `"untracked"` list in the file is for.

Two details of the wiring are decisions, not plumbing:
- **`Tests.Frontend` is a separate project from `Tests.Server` because of NUnit.** Avalonia's headless
  runner (`Avalonia.Headless.NUnit`) is built against NUnit 4.5.1; the inherited suites are on NUnit 3
  and `Assert.That(cond, "text {0}", arg)` still *compiles* there while substituting nothing. One
  assembly cannot host both runners, and matching the old count by downgrading the new code would be the
  wrong way round. `tests.ps1` therefore reports five suites, not four.
- **The front end has no MP2 project reference except the API it consumes**, and `spine.ps1` counts it
  as a core area with 0 WinForms, 0 GDI+ and 0 P/Invoke. It reaches the server through HTTP only, so it
  carries no Windows dependency and no dependency on the server's startup order.
- **The player's suite is split by what it can measure without an engine.** `TestPlayerContract` (16 tests)
  needs nothing but the assembly; `TestPlayerEngine` (16) drives a real libVLC and calls `Assert.Ignore` with
  the engine's own refusal sentence when there is none - the only skips anywhere in the tree, on purpose, so
  that a machine without VLC reports "not measured" instead of a failure. That is exactly why the Linux image
  installs an engine and then refuses a run with any skips: on these sixteen a skip is a shrunken measurement
  wearing green.

`test-frontend-check.ps1` is the part that makes it a claim rather than a folder of XAML: it assembles
the server layout, starts the headless host, and runs the *built front end binary* with `--check`. It
retries the `503` case (a server whose library is still starting is not a failure), then asserts the
answer's shape - `pathBase=/MediaPortal/Server`, `library:`, `aspects`, the closing line - and never a
number, because aspect counts move between runs. Then it runs the same binary against a port nobody
listens on and requires exit 1 with a readable message. Exit codes are part of the contract: 0 ok, 1
refused, 2 bad URL, 3 server not ready.

### Headless Avalonia is not what a normal test suite expects

Writing the UI suite produced a class of failure no compiler or linter sees, and the measurements are
worth recording because they read like bugs in the test code:

- **A test that `await`s deadlocks.** `[AvaloniaTest]` runs its body on the dispatcher thread with
  `AvaloniaSynchronizationContext` installed. NUnit waits for the returned `Task` *with that thread
  blocked*, so the continuation - queued for that same dispatcher - never runs. It looks like a hanging
  suite (it sat for eight minutes before anyone instrumented it). The rule in `TestHomeWindow` is now:
  start the work, pump (`Dispatcher.UIThread.RunJobs()` plus a 5 ms sleep) until the condition holds or a
  deadline expires, then assert. The deadline is what turns a regression into a failure instead of a
  hang.
- **Teardown must clean up before it complains.** The fixture hosts the API on one shared HTTP port; if
  a test throws while its request is in flight, `Dispose` of that host on the UI thread wedges, and every
  later test times out on top of it. So teardown closes the host from a pool thread with a bounded wait,
  and only *then* rethrows the first failure.
- **Closing that host on the Avalonia UI thread while a request may be in flight hangs**; from a pool
  thread with `Task.Run(...).Wait(...)` it completes (measured).
- **`ListBox.Focusable` was `False` on the real screen.** The D-pad walked into the list area, `Focus()`
  returned false, and the handler had already marked the key handled - so the remote's down button did
  nothing and nothing reported it. Fixed in the XAML (`Focusable="True"`) and in the handler (mark
  handled only when focus actually moved), each with a test.
- **`Button` handles `Key.Enter` itself**, so a "non-directional keys are not mine" test cannot use
  Enter; it uses `F9`.
- **NUnit 4 removed the `params object[]` message overloads** (`Assert.Fail(string, args)` is CS1501), so
  messages are built with `string.Format` or concatenation.

One correction, recorded because it was written down wrong first: the four bugs above were found by
`Tests.Frontend`/`--check`, and the suite totals in the commit that introduced them attributed the 403
tests to the wrong suite names. `tests.ps1` prints them in default order - Canary.Portable 29,
Tests.Server 184, Tests.Client 90, Tests.Frontend 32, Setup\Tests 68 - which is what ROADMAP now says.
(Both numbers have grown since: 34 and 405, the second one including the crash the render probe found; 66 and
473 since the player's 32 tests joined `Tests.Frontend`; 99 and 506 since the playback screen and its view model
joined it.)

### The front end draws: `Frontend.RenderProbe`

The suites above run on Avalonia's pure-managed headless rasterizer (`UseHeadlessDrawing = true`): fast, no
display needed, and completely blind to Skia, to font shaping, and to image encoding. "The tests pass" is not "it
draws". `MediaPortal\Tests\Frontend.RenderProbe` is a console project in `MP2-Frontend.sln` that configures the same
`App` with the headless platform's stub rendering switched off plus `.UseSkia()`, shows the same `HomeWindow`,
captures the frame, counts its pixels and writes a PNG:

    dotnet MediaPortal\Tests\Frontend.RenderProbe\bin\Debug\net10.0\Frontend.RenderProbe.dll `
        --out build\artifacts\frontend-render\home.png --server http://127.0.0.1:55555/MediaPortal/Server

- `--out` is required, its directory is created, and the file must come back with a PNG signature.
- `--screen home|browser|nowplaying|connect` picks the screen (default `home`). The playback screen is driven by a
  *scripted* player rather than libVLC, and the title list by a *scripted* page of twelve films served in the exact
  JSON `GET api/media/titles` answers with: a frame that is supposed to mean the same thing on Windows and in a
  container cannot depend on which moment a decoder happened to reach, nor on whether whoever runs the gate has
  imported anything. Both still go through the real parsing and the real bindings - the probe's job is the
  drawing, not the decoding and not a shortcut into the view model.
- `--server` renders the populated screen; without it (and without `MP2_SERVER`) the screen's own "no server"
  state is rendered, which is a real state and gets measured too.
- `--width`/`--height` default to 1280x720, and anything below 100x100 is refused as an argument error (exit 2).
- Exit 0 drew something, 1 the frame is flat or the app would not set up, 2 usage, 3 the write failed, 4 a
  populated screen was asked for and the server answered no aspects (an error screen against a URL proves nothing).

What it prints is what a gate judges, because a gate cannot look at a picture:

    pixels:   921600 (Rgba8888), 2592 distinct colours, 234687 differing from the background,
              65736 high-contrast edges, mean luminance 31

Flat frames fail: under 16 distinct colours, or under 1% of pixels differing from the dominant background colour.

**A frame counter is not a picture, and one black screen proved it.** The playback surface copied decoded bytes
into its `WriteableBitmap` but handed that bitmap to the `Image` control before the first copy had filled it, and
never asked the control to draw again - so the compositor kept showing the empty first upload, a black rectangle
the size of the film, while the app's own `video: N frame(s) drawn` counter climbed into the hundreds. The fix
(`Views\VideoSurface.cs`) sets `Source` only after a copy succeeds and calls `InvalidateVisual()` after every
fill. No headless test caught it, and neither did a desktop screenshot: a GDI capture of the maximised window's
rectangle once reported `53 719 distinct colours` after the change, which was the editor showing beside the
window, not a film. So the front end now photographs *itself* rather than the monitor being photographed from
outside - see `--shot`, `--start` and the `layout:` line under the playback entry below - because a capture of
where a window *should be* is a capture of whatever happens to be there.

It runs from two gates, twice each, and all four runs show real data. `test-frontend-check.ps1` runs it **while
its server is still up** (item 6 of that script, `-SkipRender` to leave it out, `-RenderDir` to move the output),
which writes `build\artifacts\frontend-render\home-windows.png` from the populated screen
(`status: 25 aspects on MediaPortal 2 Server, 25 aspect row(s)`) - MP2's main menu in two rows of five, each tile
carrying either the server's own count or the reason it has nothing behind it (`Movies 0`,
`Videos 0`, `TV no TV backend`, `Settings no settings API yet`), with an `Episodes` tile after MP2's own because
that server reports an aspect MP2's menu has no word for - and `nowplaying-windows.png` from the playback
screen (`status: Playing, 0:45 / 1:30`, 2120 distinct colours, 265 700 edges). The home frame is taken only once the
screen has stopped reading, which the probe now waits for: it used to capture while the count requests were still
arriving, and printed `Movies ?` for a server that answers `0` - a picture of a screen mid-thought rather than of a
screen. What the menu settled to is printed as a `menu:` line, so the claim about that frame is a sentence the run
asserts rather than one a reader has to pick out of a picture. That frame also carries the sound and
subtitle row: the scripted player names three sound tracks and two subtitles, so the frame shows what the screen
does with an engine's own track names - `Sound: eng - Language (eng)` and `Subtitles: no subtitles`, the second
being the "off" entry the picker starts on because the media had no subtitles selected. A media whose player names
one track gets no row at all, which is the other half of the behaviour and is what the tests cover. The playback screen's counts went up
by a factor of twenty-odd when it started drawing a *picture* rather than only text and bars: the probe drives the
screen through `VideoFrameSink` with eight colour bars pushed in a known order, and `CheckBars` then reads them
back out of the captured frame - all eight, in that order, in those colours - so a front end that draws a black
rectangle where the film should be fails this gate instead of passing it with a healthy-looking frame counter.
The library list is drawn too, from a scripted page of twelve films (`browser-windows.png`, 2207 distinct colours,
120 481 edges), and the frame after it is that same list with the info key pressed (`details-windows.png`, 3689
colours, 167 490 edges, `details: panel open on "Nimmerland (2011)" - "88 min · FSK 12 · rated 8/10 · watched 1×,
last 2025-01-12"`). That fifth frame presses `Key.I` on the window rather than calling the view model's command, so
it only goes green if the key, the handler on the window and the panel hold together; the probe refuses to write a
frame at all when the panel stayed closed, because a "details" picture of the plain list would be a picture of
nothing. Both are committed, as `docs\multiplatform\screenshots\browser-windows-skia.png` and
`docs\multiplatform\screenshots\details-windows-skia.png`.

Those two frames carry the titles' *pictures* too, and a colour count cannot tell a poster from the box that is drawn
where there is none, so both frames are asked about by colour instead. The scripted library stores a flat poster in
one colour for the first film (rgb 212,64,168), one in a second colour for the third (rgb 32,176,152), nothing for
nine of the twelve, and bytes that are not a picture at all for the fifth. `CheckPosters` then wants to see a 64 by 64
patch of the first colour in the picture column of row one (measured: `x 60..123, y 199..262`), the second colour two
rows further down that same column (`y 401..464`; rows are 101 lines apart in this frame), and the plain box - not a
poster - on the row whose stored bytes are junk, because a shelf that drew *a* poster there drew some other title's.
The details frame is asked for all of that *and* for a 240 by 240 patch of the first colour in the panel on the right
(`x 892..1131, y 206..445`), plus one more clause: nothing of the second colour anywhere on the right half, which is
what a panel that had read the picture of the wrong title leaves behind. The probe prints what it saw as a `pictures:`
line, and - unlike the colour and edge counts, which are only compared with a baseline - this check *refuses* a frame
it does not like, so a shelf that loses its pictures stops the build instead of quietly becoming a different number.
What measuring these two frames turned up - that with the details panel open the list behind it had been vanishing off
the window entirely - is in ROADMAP item **ai**.
The sixth frame is that list again with a word in the search box (`search-windows.png`): the probe focuses the real
`TextBox`, types `alien` through `TextInput`, presses `Key.Enter`, and refuses to write the picture unless the list
came back with a single row *and* the header fell to "1 title in films matches 'alien'" - so the frame is the search
box, the filtered list and the counted total drawn together, and it fails if the keystrokes never reached the field
rather than passing with a picture of the unfiltered shelf. It is committed as
`docs\multiplatform\screenshots\search-windows-skia.png`. Sampling the pixel band where list rows three and deeper
would be says the same thing without the probe's help: 683 distinct colours on the unfiltered frame, two on the
search frame.
The seventh frame is the same list after the watched key was pressed twice (`seen-windows.png`): three rows, each
carrying the tick in front of its name, the button reading "Watched only" and the status line saying
"3 of 3 watched films". The probe refuses the picture unless the filter really reached the third state and every
row that survived carries a mark. The tick can also be measured rather than trusted: sampling the mark column
(`x 138..178`, pushed right by the new picture column) row by row gives 45, 37 and 37 bright pixels on the three rows
of the watched-only frame and zero on the rows below them, and marks the first and the third row of the *unfiltered*
shelf frame (45 and 37) while leaving the rows directly under it empty. The third watched title is off the bottom of
that shelf frame now: a row with a picture in it measures 101 lines where the same row in the previously committed
frame measured some 67 lines, so the seven rows that used to be on screen became five. Notably still unmarked is the title whose
last-played date came from stopping it halfway, which is the distinction the whole feature turns on. It is committed as
`docs\multiplatform\screenshots\seen-windows-skia.png`.

The eighth frame is the home screen of a scripted server that holds twelve titles and says four of them were left
half way (`continue-windows.png`). Above the fold is MP2's main menu - two rows of five, `Movies 12` and
`Episodes 3` being the two parts of the library the script offers, and the seven entries it cannot fill each
carrying its own reason (`TV Series not on this server`, `TV no TV backend`, `Settings no settings API yet`), which
is what the probe's `menu:` line prints. Under the menu the strip lists the film whose name alone is known, the
episode, and two more films, newest stop first, each with its runtime and the date it was stopped, none of them
carrying the tick that section **ac** introduced, and three of the four also carrying the position the viewer left -
`12% watched`, `74% watched`, `61% watched` - while the oldest of them carries none. The probe refuses the picture
unless those four - and only those four - are on the strip in that order, with exactly those three figures and the
one empty space, no title is marked watched, the two parts the script offers are also the two openable entries of
the menu, and the server named at the top is the scripted one; a wrong endpoint path in the probe once rendered a
frame with no server name at all, so that condition is now asserted too. Measuring the pixels independently of the
probe agrees, and says where the screen actually is: the menu's labels at `y 190..208` and `y 274..293` with their
reasons under them at `y 222..231` and `y 305..316`, the strip's heading at `y 360..376`, and its four row bands at
`y 401..416`, `451..466`, `501..516` and `551..566` - each band holding a title (`x 60..302` at its widest), a
short middle column somewhere in `x 440..511` depending on how long the runtime is, and a date at `x 674..846`; and
the three rows that carry a position hold a fourth block at `x 1004..1127` - some 120 pixels of it, beginning
within ten pixels of one another across the three rows - where the row with nothing stored ends at `x 846`. The same
measurement on the frame drawn before the positions existed, which is still in git, finds three blocks in every row
and no ink past `x 997` anywhere. Three figures is also proof that the
positions came attached to the titles rather than being invented per row, since they differ from row to row in the
order the script stored them. It is committed as
`docs\multiplatform\screenshots\continue-windows-skia.png`.

Five more were re-copied when the shelf grew a picture column (`browser`, `details`, `search`, `seen`, `connect`):
the first four because the rows they show are taller and carry pictures now, `connect` because of its known
nondeterministic status line. `browser` had never been committed at all before that, so the frame the picture work
made meaningful - posters and empty boxes side by side in one list - is now on disk next to the others.

Six Windows baselines were re-copied when the main menu arrived, four of them (`details`, `search`, `nowplaying`,
`connect`) for a reason that has nothing to do with it: a picture is copied by the slice that changes it, and the
slices since had changed those screens - a second line under a row of titles, the playback screen's engine line,
the address line on the server screen - without anybody re-copying the frame. A committed screenshot the current
code cannot draw is worse than no screenshot, so every frame that differs gets re-copied, not only the one a slice
meant to touch.

`test-frontend-linux.ps1 -ServeFromHost` starts
`MP2-Server-Headless` on this machine, waits until it really serves aspects (measured: a just-started server answers
`200` with an *empty* list, and 25 aspects about a second later), and passes `MP2_SERVER` to the container as
`http://host.docker.internal:55555/MediaPortal/Server` - measured from inside the image to resolve and answer
`HTTP/1.1 200 OK` / `Server: Microsoft-HTTPAPI/2.0`, while `172.17.0.1` does not connect at all. The frames the
container writes are then the populated ones too (`3872 distinct colours`, 119 642 bytes for the home screen;
2282 colours, 327 711 edges and 25 193 bytes for the playback screen, with `bars: all eight` - the colour bars
pushed through `VideoFrameSink` are read back out of the frame the Linux Skia drew, so the picture path is proven
on that platform and not only on Windows), and the image refuses to go green unless its output says
`populated from`, so the screenshots cannot silently fall back to the offline or the error picture (asking for a
server and getting no aspects is the probe's own exit 4, so it is refused there rather than re-derived here).
Without `-ServeFromHost` the run still renders, and the OK line says the frame is the screen's own "no server"
state. Those four frames are committed under `docs\multiplatform\screenshots\` as
`home-windows-skia.png`, `home-linux-skia.png`, `nowplaying-windows-skia.png` and `nowplaying-linux-skia.png`,
and the host script refuses a green run whose mount delivered only one of the two Linux frames.

Three things came out of adding it, all of them measurements rather than opinions:

- **It found a crash.** The window refreshes itself when it opens; that overlapped the probe's own read, and the
  process died on the dispatcher with `ObjectDisposedException: The CancellationTokenSource has been disposed.`
  `HomeViewModel` was disposing the previous run's cancellation source mid-flight. ROADMAP item **q** has the three
  fixes and the two tests (`TwoRefreshesStartedTogetherLeaveOneCleanScreen`,
  `PressingTheRefreshButtonTwiceDoesNotThrowOnTheDispatcher`); the second one goes through `RefreshCommand.Execute`
  on purpose, because the toolkit rethrows whatever a command lets escape as `async void` - an awaited test never
  sees it.
- **The Skia path did not work on Linux.** `TypeInitializationException` from `SkiaSharp.SKImageInfo`: the native
  `libSkiaSharp.so` needs `fontconfig` and `freetype`, and the image had neither. Both are installed in
  `Dockerfile.frontend` now. The managed suite was green the whole time, which is precisely the point of the probe.
- **This gate could run a binary nobody rebuilt.** After the fix the gate still crashed, because the probe kept
  executing the copy of `MediaPortal.Frontend.dll` in its own `bin`. It now refuses to start when a `.cs`, `.axaml`
  or `.csproj` under the front end or the probe is newer than the DLL it is about to run - same reasoning as
  "A green test suite can be a stale one" above.

The API facts it needed are recorded in the probe's comments, because they are not obvious from the docs:
`CaptureRenderedFrame` is `Avalonia.Headless.HeadlessWindowExtensions.CaptureRenderedFrame(TopLevel)` and returns
the public `WriteableBitmap` wrapper (so its type name proves nothing about the backend); `RenderToFile` is not
public API at all; `Bitmap.Save(string)` is obsolete in Avalonia 12, pass `PngBitmapEncoderOptions.Default`;
`AvaloniaLocator.Current` is gone in v12; `UseHeadlessDrawing = false` without `.UseSkia()` throws "No rendering
system configured"; and the locked framebuffer is `Rgba8888` under Skia where the managed path is `Bgra8888`.

### The front end runs on Linux: `test-frontend-linux.ps1`

`Dockerfile.cross` compiles; this one *executes*. `Dockerfile.frontend` builds `MP2-Frontend.sln` and
then runs `Tests.Frontend`, i.e. it builds the real `HomeWindow.axaml`, loads the real Fluent theme and
the Inter font, drives the D-pad handler, and calls the real `MediaPortal.Server.Api` controllers over a
Kestrel host - inside a container, under a user with no display, on a machine that has never seen
Windows. Measured: `Passed! - Failed: 0, Passed: 99, Skipped: 0, Total: 99, Duration: 8 s`, then
`FRONTEND LINUX OK: 99 tests ran headless on Linux x86_64, populated from
http://host.docker.internal:55555/MediaPortal/Server screenshots in /out/home-linux.png and
/out/nowplaying-linux.png` on
`Linux 6.6.87.2-microsoft-standard-WSL2 x86_64, dotnet 10.0.402`.

Thirty-two of those 99 belong to `MediaPortal.Frontend.Player`, and the image carries a real engine so that
sixteen of them are measurements rather than skips. Ubuntu noble has no `vlc-nox` - naming it failed the apt
step - so the engine comes from `libvlc5` and the plugins from `vlc-plugin-base`, and the plugins live in the
multiarch directory `/usr/lib/x86_64-linux-gnu/vlc/plugins`; `/usr/lib/vlc/plugins` exists and is empty, which
sent one debugging run in circles. The `RUN` that installs them then refuses an image that has a `libvlc.so*`
but no `demux` plugins, because that combination is worse than having no engine at all: libVLC initialises,
plays nothing, and the tests fail as timeouts instead of as a missing engine. The `CMD` refuses the run while
anything is skipped, which is what makes the 66 mean 66: on the engine tests a skip would read as green while
measuring nothing. That rule is what made the first player run say `Failed: 16, Passed: 50` - libVLC was found,
`Core.Initialize(directory)` is refused on Linux, and the fix (a loader resolver that answers `libvlc` with the
versioned file the distribution actually ships) is ROADMAP item **s**. Two consequences worth stating plainly:
the same 99 tests pass on Windows against VLC 3.0.20 and on Linux against the distribution's libVLC, and an
image whose VLC packages rot will fail this gate rather than quietly shrink the measurement.

**The second thing the container caught was a race, not a platform hole.** `TheScreenFillsItselfInFromWhatTheServerSent`
had been green on Windows for many runs and failed here on `Does.Contain("17")` with the aspect row showing `?`:
the aspects arrive in one request and each aspect's count in another, and the test asserted the count the moment
the aspects were on screen. Windows' scheduler won that race, Linux' did not, and the code under test was
identical and correct - the screen really does fill the number in a moment later. The test waits for the count
now. The reason this belongs in this README rather than only in ROADMAP is the gate: a suite run on one platform
alone would have kept reporting this as a passing test forever, and the claim it makes ("the screen shows what it
read") was never actually being checked on the slower host.

Two properties of that image are the gate, not the packaging:

- **No `-p:EnableWindowsTargeting=true`.** `Dockerfile.cross` needs it because the server solution still
  contains `-windows` projects. Nothing in the front end's closure may need it, so leaving it out means
  the day a `-windows` project is referenced from this side the build answers `NETSDK1100` on Linux -
  which is the regression worth failing on, rather than a flag that quietly absorbs it.
- **A run that discovers no tests is a failure.** The `CMD` parses `Total:` out of the test output and
  exits 1 if it is not at least 1, because `dotnet test` reports "no tests were discovered" and exits 0.
  Without that line this image would go green the moment someone broke the test project - the same class
  of false green as a stale test assembly, see "A green test suite can be a stale one".
- **`-ServeFromHost` waits for aspects, not for a port.** `/health` answers as soon as the listener is up, and a
  server that has just started answers `api/library/aspects` with `200` and an empty list before it answers with 25
  aspects a second later; waiting for the port or for a `200` would let the probe render an empty screen and make
  the verdict a race. The URL is built by concatenation, not `Join-Path`, which inserts a backslash on Windows -
  measured, that produced 90 seconds of "the server never served aspects" for a server that was serving them the
  whole time.

What green here does *not* mean: there is still no X server in the image and none is installed, so nothing has
appeared on a Linux desktop. Since the render probe runs in this image too, the Skia backend, the native
`libSkiaSharp.so`, font shaping through fontconfig/freetype and PNG encoding are all measured on Linux - but
software-rendered into a bitmap, in a container, without a window manager, a compositor or a GPU. Whether the
screen looks *right*, and Android, remain separate measurements that have not been made.

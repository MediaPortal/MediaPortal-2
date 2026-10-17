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
| `warning-baseline.json` | Committed maximum allowed count per warning code. |
| `satellites.ps1` | Enforces the satellite boundary: a namespace that only exists in a `*.Windows` project may not be imported by a project that does not reference it. |
| `Dockerfile.canary` | Linux verification image for the portability canary (`MediaPortal\Tests\Canary.Portable`). |
| `test-canary-linux.ps1` | Builds that image and runs the canary tests inside a Linux container. |
| `Dockerfile.cross` | Linux **cross-compile** image: builds the whole `MP2-Server.sln` under `mcr.microsoft.com/dotnet/sdk:8.0`. |
| `test-server-linux.ps1` | Runs that image (see "Compiling on Linux" below for the package-cache it needs). |
| `Dockerfile.drawing`, `drawing-probe/` | Not a gate: measures which half of `System.Drawing` works on Linux. Read it before changing what `spine.ps1` counts. |

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
**CA1416 is 366 on all three measurement environments** — Visual Studio, the SDK host, and the Linux container.
Totals across *platforms* (Windows 2330 vs the Linux probe's 449) still must not be compared: they compile
different project sets. Compare per-code inventories, and treat `CA1416` as the metric.

## The ratchet

Making all warnings errors on a codebase this age is not achievable, and an unenforced lint
budget is decoration. Instead each warning code is capped at its last known count in
`warning-baseline.json`. Adding a new instance of any code fails the build; fixing some lets
you lower the floor. The codes that matter for the multiplatform work are:

| Code | Meaning on .NET (non-Framework) |
| --- | --- |
| `CA1416` | Call site uses an API supported only on some platform(s). |
| `SYSLIB0006` | `Thread.Abort()` / `Thread.ResetAbort()` - compiles, then always throws `PlatformNotSupportedException`. |
| `SYSLIB0011` | `BinaryFormatter` - obsolete, and unsupported for untrusted data. |
| `SYSLIB0004` | Constrained Execution Regions - not supported. |

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

### The core spine is at zero

That is the state the gate now holds, and it is worth stating precisely because of what it does
*not* mean:

    MediaPortal\Source\Core\MediaPortal.Common      WinForms 0  GDI+  0  Drawing primitives 1
    MediaPortal\Source\Core\MediaPortal.Utilities   WinForms 0  GDI+  0  Drawing primitives 0
    MediaPortal\Source\Core\MediaPortal.Backend     WinForms 0  GDI+  0  Drawing primitives 0
                                                    files affected 0  total refs 0
    satellites (MediaPortal.Common.Windows, MediaPortal.Utilities.Windows)  WinForms 19  GDI+ 64  refs 83

It means: no source file compiled into `MediaPortal.Common`, `MediaPortal.Utilities` or
`MediaPortal.Backend` references WinForms or `System.Drawing.Common` any more, so those three
assemblies can be re-targeted to a portable TFM without first deleting code. It does **not** mean
the code runs off Windows - 83 references still exist one project over, `MediaPortal.Common` still
*builds* as `net6.0-windows` because of `UseWindowsForms`, and the Windows API Code Pack, BASS and
the shell providers are all still Windows. Reaching zero was done by two moves (`SplashScreen.cs`,
`ImageUtilities.cs` into `MediaPortal.Utilities.Windows`) and one seam (`IImageTransformer`);
`git mv` is enough for the first kind, the second kind needs a host to install an implementation.

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
"no relations" path; the conflict rule is still covered by `ServiceClient` ↔ `TvMosaicClient`.

## Target framework: what the newest SDK actually says about this tree

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

They appear only on a `net10.0-windows` leg: the same SDK 10 building the committed
`net48;net6.0-windows` legs reports **zero** `WFO1000`. The WinForms source generator that
introduced the rule is enabled by the newer target, not by the newer SDK. The fix is per-property
(`[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]` on runtime-only
properties), and the second one disappears on its own once `MediaPortal.Common` stops setting
`UseWindowsForms` - which is exactly the retargeting task the now-empty spine unlocked.

Nothing else. No missing API, no removed type, no package that refuses `net10.0` - because the
dual TFM was already compiling `net6.0-windows` everywhere, and .NET's compatibility surface
between 6 and 10 carries the rest.

Two things this measurement cannot see, both of which are real work:

* The **23 old-style (`non-SDK`) projects** - 12 of them are tools and templates, the rest are the
  five database backends, `HttpServer`, `TvdbLib`, `Test.MediaServer` and the dead
  `MediaPortal.Manager`. Overriding `TargetFrameworks` does nothing to them; they need their
  project files rewritten. They are also the reason `net48` is load-bearing: an old-style project
  cannot produce a `net10.0` output at all.
* **`net6.0` is out of support since 2024-11-12** and `net8.0`/`net9.0` both end 2026-11-10, while
  `net10.0` runs to 2028-11-14 (from the dotnet/core releases index, not from memory). A port that
  lands on `net6.0` or `net8.0` lands on an unsupported runtime.

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

## Locale

Build output on many contributor machines is localized - the summary reads `0 Fehler` and
`Der Buildvorgang wurde erfolgreich ausgeführt`. Diagnostic *lines* keep the invariant
English shape `file(line,col): warning CODE: message`, which is what the parsers match.
Never script against the summary text.

## Linux

What runs on Linux today is the **portability canary** only:

    .\test-canary-linux.ps1

That compiles a whitelist of `MediaPortal.Utilities` sources for `net8.0` and runs their
tests inside `mcr.microsoft.com/dotnet/sdk:8.0`. It is a real signal about a real slice of
MP2, and it is deliberately narrow. Nothing else in the tree runs on Linux: every other
project targets `net48;net6.0-windows`, and a `-windows` TFM cannot execute there. So "the
canary is green" must never be reported as "MP2 runs on Linux".

The canary exists so that Linux coverage grows file by file while the rest of the tree is
still Windows-only, and shrinks back to nothing once `MediaPortal.Common`/`Utilities`
themselves target a portable TFM. See the header comment in
`MediaPortal\Tests\Canary.Portable\Canary.Portable.csproj` for the per-file procedure.

## Compiling on Linux (the cross-compile probe)

    .\test-server-linux.ps1

This builds **all 93 projects of `MP2-Server.sln`** inside `mcr.microsoft.com/dotnet/sdk:8.0`
and is green: **0 errors, 129 assemblies produced (64 `net6.0-windows`, 63 `net48`), 2 min
48 s**. It is a build-system result, not a runtime one - a `-windows` assembly still cannot
execute on Linux, and nothing here means MP2 *runs* there. What it does settle is that the
remaining work is code that calls Windows APIs, not build topology. Three things were needed:

* `-p:EnableWindowsTargeting=true`, so a non-Windows SDK will restore and compile `-windows`
  targets at all.
* `Directory.Build.props` (one in `MediaPortal\`, one in `MediaPortal\Source\`; MSBuild stops
  at the first one it finds, so both are needed) adding
  `Microsoft.NETFramework.ReferenceAssemblies` **only** when `'$(OS)' != 'Windows_NT'`, which
  makes the `net48` legs compile off-Windows without touching Windows builds.
* `Tools\DevTools` had a `mkdir`/`xcopy` post-build event; on Linux MSBuild runs the event
  through `/bin/sh` and `xcopy` exits 127. It is now guarded by `Condition="'$(OS)' ==
  'Windows_NT'"`. Expect more of these - `xcopy` in post-build steps is an MP2 idiom.

The analyzer surface is the same on both hosts where it should be: the Linux log reports
**468 CA1416 sites**, the same number the Windows gate sees. `MSB3884` appears 230 times on
Linux, which is the "AllRules.ruleset not found" consequence described above - so total
warning counts still must never be compared across hosts.

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

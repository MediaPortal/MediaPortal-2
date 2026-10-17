# CI scripts

Entry points for building and gating MP2. Everything here is meant to behave identically
on a developer box and on an agent - and the sections below document where it currently
does *not*, because those differences are how CI surprises happen.

| Script | Purpose |
| --- | --- |
| `build.ps1` | Build MP2 solutions on Windows with full MSBuild, write logs to `build/artifacts/`. `-Ratchet` also applies the warning gate. |
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

Windows gates keep using **full Visual Studio MSBuild**, not because `dotnet` cannot build the
tree, but because the committed warning floors were measured with it and are only comparable
to itself (next section). Use `dotnet` for single portable projects and for the Linux probe.

## The warning surface is toolchain dependent - read this before trusting a count

Most projects declare `<CodeAnalysisRuleSet>AllRules.ruleset</CodeAnalysisRuleSet>`. That is
a *bare* name, and MSBuild resolves bare rule set names through the Visual Studio
"Static Analysis Tools\Rule Sets" directory. On a machine with that component installed it
resolves to Microsoft's "All Rules" set, which is:

    <RuleSet Name="Microsoft All Rules" ...>
      <IncludeAll Action="Warning" />
    </RuleSet>

So on a full Visual Studio box every analyzer rule runs as a warning, while on a plain SDK
host the file cannot be found, MSBuild emits `warning MSB3884`, and the default (much
smaller) analysis set applies. Concretely: `CA1063` appears ~78 times on a VS box and would
be ~0 on a bare SDK agent. **A warning count is therefore only comparable to another count
taken with the same toolset, configuration and verbosity.** `warning-baseline.json` records
those; do not compare it against a `dotnet build` log or a Linux log.

The correct fix is to commit an explicit ruleset into the repository and reference it by
path, so every host analyzes with the same rules. That is deliberately *not* done here yet:
it changes the diagnostic surface of ~133 projects at once and needs a decision about which
rule families we actually intend to enforce.

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
  `CA1416` by **-20 while changing no behaviour whatsoever**: the 20 GDI+ call sites still
  exist, they simply stopped being reported.

Consequence for the floor: `CA1416` is treated as a *budget of Windows coupling*, not a count of
diagnostics, so it stayed at 488 while only 468 are visible. The 20-unit slack is stated in
`notes.CA1416` and is exactly what `spine.ps1`'s satellite section prevents from growing.

Typical loop:

    .\build.ps1                       # builds Debug, writes build\artifacts\build-Server-Debug.log
    .\warnings.ps1 -UpdateBaseline    # -LogPath is optional; without it the newest build log is used
    git add scripts\ci\warning-baseline.json

Floors may rise legitimately. When they do, `warnings.ps1` keeps failing until the floor is
re-baselined on purpose, and the reason belongs in `notes.<CODE>` inside
`warning-baseline.json` (preserved across re-generation) as well as in the commit message.

## Satellite boundaries

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
* Some folders under `Source` contain source files that belong to no project (for example
  `UI\TV\SlimTvService`, superseded by `SlimTvService3`). They are not compiled by anything.

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

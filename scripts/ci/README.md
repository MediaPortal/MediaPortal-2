# CI scripts

Entry points for building and gating MP2. Everything here is meant to behave identically
on a developer box and on an agent - and the sections below document where it currently
does *not*, because those differences are how CI surprises happen.

| Script | Purpose |
| --- | --- |
| `build.ps1` | Build MP2 solutions on Windows with full MSBuild, write logs to `build/artifacts/`. `-Ratchet` also applies the warning gate. |
| `warnings.ps1` | The ratchet: fail if any warning code exceeds its committed count. `-UpdateBaseline` lowers the floor. |
| `common.ps1` | Shared helpers (MSBuild discovery, log parsing). Dot-sourced, not run directly. |
| `warning-baseline.json` | Committed maximum allowed count per warning code. |
| `Dockerfile.canary` | Linux verification image for the portability canary (`MediaPortal\Tests\Canary.Portable`). |
| `test-canary-linux.ps1` | Builds that image and runs the canary tests inside a Linux container. |

## Why full MSBuild and not `dotnet build`

`dotnet build` cannot build the `net48` target of this tree. Projects whose `.resx`
contains non-string resources fail with `MSB3822`/`MSB3823`: the `GenerateResource` task
needs design-time assemblies the .NET CLI host does not load. Full Visual Studio MSBuild
handles them. `MediaPortal.Client.Launcher.csproj` already carries a
`System.Resources.Extensions` workaround for this, but with `PrivateAssets="All"`, which
keeps the assembly out of the runtime output - applying that globally would risk breaking
resource loading on .NET Framework, so it stays out of `Source\Directory.Build.props`.

Use `dotnet` for the portable TFMs and for Linux; use full MSBuild for anything that still
ships `net48`.

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

Typical loop:

    .\build.ps1                       # builds Debug, writes build\artifacts\build-Server-Debug.log
    .\warnings.ps1 -LogPath ..\..\build\artifacts\build-Server-Debug.log -UpdateBaseline
    git add scripts\ci\warning-baseline.json

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

<#
.SYNOPSIS
  Shared helpers for the CI scripts. Dot-source this from the other scripts.

.NOTES
  Locale note: MSBuild's console summary is localized ("0 Fehler", "Buildvorgang
  erfolgreich"), but the per-diagnostic line format is invariant English:
      file(line,col): warning CA1416: <localized message>
  Every parser here matches only the invariant part, so counting works under any
  developer locale. Never grep these logs for "Build succeeded" or German prose.
#>

Set-StrictMode -Version Latest

function Find-MsBuild {
<#
    Returns the path to a full Visual Studio MSBuild.exe.

    This is now the *alternative* host (build.ps1 -BuildHost MsBuild); the default is the .NET SDK.
    Two historical reasons for insisting on Visual Studio MSBuild are gone: the net48 legs used to
    abort under the CLI with MSB3822/MSB3823 because of non-string .resx entries, and those
    resources turned out to be dead and were deleted; and the rule sets named in the project files
    only resolved here, which meant every other host silently analysed less. Severity now comes
    from MediaPortal\.editorconfig, so both hosts run the same rules - but not the same totals,
    because a Visual Studio that is not installed cannot build net10 either (17.14 rejects it with
    NETSDK1045). Use this host only to compare against an old measurement.
#>
  $candidates = @()
  $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
  if (Test-Path $vswhere) {
    $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
                        -find 'MSBuild\**\Bin\MSBuild.exe' 2>$null
    if ($found) { $candidates += $found }
  }
  $candidates += Get-ChildItem 'C:\Program Files*\Microsoft Visual Studio\*\*\MSBuild\Current\Bin\MSBuild.exe' `
                              -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
  foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }
  throw 'Full Visual Studio MSBuild.exe not found. Install VS2022 with the .NET desktop workload, or pass -MsBuildPath.'
}

function Get-LogDiagnostics {
<#
    Parses an MSBuild file log into a diagnostic inventory.
    Returns a hashtable: @{ errors = <int>; warnings = @{ code = count }; duplicates = <int> }

    Findings are de-duplicated, see the comment below. This changes the numbers compared with the
    first version of this script, which counted raw log lines - see README.md, "The warning ratchet".
#>
  param([Parameter(Mandatory)][string]$LogPath)

  if (-not (Test-Path $LogPath)) { throw "Log file not found: $LogPath" }

  # MSBuild writes one line per *emitted* diagnostic, and this log format contains each one twice:
  # once in the stream as it happened and once grouped under the project it came from, prefixed with
  # the parallel-build project context ("     5>..."). Measured on MP2-Server.sln: 1600 diagnostic
  # lines, 800 of them prefixed, and the two sets are the same findings. On top of that a single
  # finding in the source can legitimately be emitted more than once - a project that several others
  # reference is built once per distinct set of global properties, and MSBuild does not always spell
  # the same path twice the same way ("...\Source\..\Extensions\x.cs" and "...\Extensions\x.cs").
  # Counting lines therefore measured the build graph and the logger, not the code. The ratchet is
  # about the code, so a finding is counted once per (code, file, line, column); diagnostics without
  # a location are counted once per identical line.
  $errors = 0
  $duplicates = 0
  $warnings = @{}
  $seen = [System.Collections.Generic.HashSet[string]]::new()
  $located = [regex]::new('^(?:\s*\d+>)?(?<f>.+?)\((?<l>\d+),(?<c>\d+)\): (?<kind>error|warning) (?<code>[A-Za-z]+\d+)[:\s]')
  foreach ($line in [System.IO.File]::ReadLines((Resolve-Path $LogPath).ProviderPath)) {
    if ($line -notmatch ':\s(error|warning)\s') { continue }
    # Same rule for the unlocated branch: without stripping, "  5>Foo.csproj : warning NU1510" and
    # its unprefixed twin would be counted as two findings.
    $bare = $line -replace '^\s*\d+>', ''
    $m = $located.Match($bare.TrimStart())
    if ($m.Success) {
      # The logger writes the same diagnostic flush left in one place and indented under its project
      # in another, so the path is trimmed before it becomes part of the identity.
      $file = $m.Groups['f'].Value.Trim()
      try { $file = [System.IO.Path]::GetFullPath($file) } catch { }
      $key = $m.Groups['code'].Value + '|' + $file.ToLowerInvariant() + '|' +
             $m.Groups['l'].Value + '|' + $m.Groups['c'].Value
      $kind = $m.Groups['kind'].Value
      $code = $m.Groups['code'].Value
    } else {
      # Diagnostics with no file location (NuGet restore, MSBuild task warnings). The line itself
      # carries the project it belongs to, so the whole trimmed line is the identity.
      $key = 'line|' + $bare.Trim().ToLowerInvariant()
      if ($bare -match '(?<kind>error|warning)\s+(?<code>[A-Za-z]+\d+)') {
        $kind = $Matches['kind']; $code = $Matches['code']
      } else { $kind = ''; $code = '' }
    }
    if (-not $seen.Add($key)) { $duplicates++; continue }
    if ($kind -eq 'error') { $errors++; continue }
    if ($code -notmatch '^[A-Za-z]+\d+$') {
      # A diagnostic whose code could not be read is counted under its own name rather than dropped,
      # otherwise an unknown code would silently vanish from the ratchet.
      $code = 'UNKNOWN'
    }
    if ($warnings.ContainsKey($code)) { $warnings[$code]++ } else { $warnings[$code] = 1 }
  }
  return @{ errors = $errors; warnings = $warnings; duplicates = $duplicates }
}

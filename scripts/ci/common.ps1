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
    Returns a hashtable: @{ errors = <int>; warnings = @{ code = count } }
#>
  param([Parameter(Mandatory)][string]$LogPath)

  if (-not (Test-Path $LogPath)) { throw "Log file not found: $LogPath" }

  $errors = 0
  $warnings = @{}
  foreach ($line in [System.IO.File]::ReadLines((Resolve-Path $LogPath).ProviderPath)) {
    if ($line -notmatch ':\s(error|warning)\s') { continue }
    if ($line -match ':\serror\s') { $errors++; continue }
    if ($line -match ':\swarning\s+([A-Za-z]+\d+)') {
      $code = $Matches[1]
      if ($warnings.ContainsKey($code)) { $warnings[$code]++ } else { $warnings[$code] = 1 }
    }
  }
  return @{ errors = $errors; warnings = $warnings }
}

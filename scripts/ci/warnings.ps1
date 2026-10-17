<#
.SYNOPSIS
  Warning ratchet: fails when the warning inventory of a build log grows.

.DESCRIPTION
  The portable-core work is measured by warning counts that can only go down
  (CA1416 = Windows-only API call sites, SYSLIB00xx = APIs that are obsolete or
  that throw on .NET). Blanket "warnings as errors" is not viable on a codebase of
  this age, so instead every warning code is capped at its last known count.
  New instances of any code fail the build; decreases are reported and can be
  committed as the new floor with -UpdateBaseline.

  This turns "we should fix platform stuff someday" into a number that cannot move
  the wrong way.

.EXAMPLE
  .\warnings.ps1 -LogPath ..\..\build\artifacts\build-Server-Debug.log
  .\warnings.ps1 -LogPath ...\build-Server-Debug.log -UpdateBaseline
#>
[CmdletBinding()]
param(
  # Optional: without it the most recent build\artifacts\build-*.log is analysed (see below).
  [string]$LogPath,

  # Resolved in the body: $PSScriptRoot is still empty while the param block is evaluated.
  [string]$BaselinePath,

  [switch]$UpdateBaseline
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'warning-baseline.json' }

# Get-LogDiagnostics binds -LogPath mandatorily, so a missing path must be resolved before the call.
if (-not $LogPath) {
  $artifacts = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'build\artifacts'
  $latest = Get-ChildItem $artifacts -Filter 'build-*.log' -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
  if (-not $latest) {
    Write-Host "No -LogPath given and no build log found under $artifacts - run build.ps1 first." -ForegroundColor Red
    exit 2
  }
  $LogPath = $latest.FullName
  Write-Host "no -LogPath given, analysing most recent build log: $($latest.Name)" -ForegroundColor DarkGray
}

$current = Get-LogDiagnostics -LogPath $LogPath

if ($current.errors -gt 0) {
  Write-Host "FAIL: $($current.errors) compile errors in $LogPath" -ForegroundColor Red
  exit 1
}

$baseline = $null
if (Test-Path $BaselinePath) {
  $doc = Get-Content $BaselinePath -Raw | ConvertFrom-Json
  $baseline = @{}
  $doc.warnings.PSObject.Properties | ForEach-Object { $baseline[$_.Name] = [int]$_.Value }
}

function Save-Baseline {
  # Per-code documentation (why a floor has the value it has) must survive a re-generation,
  # otherwise a future reader cannot tell an intentional floor from an accident.
  $notes = [ordered]@{}
  if (Test-Path $BaselinePath) {
    $existing = Get-Content $BaselinePath -Raw | ConvertFrom-Json
    if ($existing.notes) {
      $existing.notes.PSObject.Properties | ForEach-Object { $notes[$_.Name] = [string]$_.Value }
    }
  }

  $payload = [ordered]@{
    generated = (Get-Date).ToString('yyyy-MM-dd')
    log       = Split-Path $LogPath -Leaf
    toolset   = 'The .NET SDK host (dotnet msbuild). Counts depend on this: severity comes from MediaPortal\.editorconfig, but the inventory also depends on which target frameworks the host builds and analyses, so it is NOT comparable with a full Visual Studio MSBuild log (that host sees fewer target frameworks and applies its own rule sets). See README.md, "Which build host, and why it matters".'
    note      = 'Maximum allowed occurrences per warning code. Regenerate with warnings.ps1 -UpdateBaseline after an intentional change and record why in notes.<CODE> and the commit message.'
    warnings  = [ordered]@{}
    notes     = $notes
  }
  foreach ($key in ($current.warnings.Keys | Sort-Object)) {
    $payload.warnings[$key] = $current.warnings[$key]
  }
  # No BOM: PowerShell 5.1 -Encoding utf8 would prepend one.
  [System.IO.File]::WriteAllText(
    [System.IO.Path]::GetFullPath($BaselinePath),
    ($payload | ConvertTo-Json -Depth 4),
    (New-Object System.Text.UTF8Encoding($false)))
  Write-Host "Baseline written: $BaselinePath" -ForegroundColor Green
}

if ($UpdateBaseline) { Save-Baseline; exit 0 }

if (-not $baseline) {
  Write-Host "No baseline at $BaselinePath - run with -UpdateBaseline to create one." -ForegroundColor Yellow
  exit 0
}

$regressions = @()
$improvements = @()
$newCodes = @()

foreach ($code in ($current.warnings.Keys | Sort-Object)) {
  $now = $current.warnings[$code]
  if (-not $baseline.ContainsKey($code)) {
    if ($now -gt 0) { $newCodes += [pscustomobject]@{ Code = $code; Now = $now } }
    continue
  }
  $was = $baseline[$code]
  if ($now -gt $was) {
    $regressions += [pscustomobject]@{ Code = $code; Baseline = $was; Now = $now; Delta = "+$($now - $was)" }
  } elseif ($now -lt $was) {
    $improvements += [pscustomobject]@{ Code = $code; Baseline = $was; Now = $now; Delta = "-$($was - $now)" }
  }
}

$curTotal = ($current.warnings.Values | Measure-Object -Sum).Sum
$baseTotal = ($baseline.Values | Measure-Object -Sum).Sum
Write-Host "warnings: $curTotal (baseline $baseTotal)"

if ($improvements.Count) {
  Write-Host ""
  Write-Host "Improved - commit these as the new floor with -UpdateBaseline:" -ForegroundColor Green
  $improvements | Format-Table -AutoSize | Out-String | Write-Host
}

if ($newCodes.Count -or $regressions.Count) {
  if ($newCodes.Count) {
    Write-Host ""
    Write-Host "New warning codes introduced:" -ForegroundColor Red
    $newCodes | Format-Table -AutoSize | Out-String | Write-Host
  }
  if ($regressions.Count) {
    Write-Host ""
    Write-Host "REGRESSION - counts above baseline:" -ForegroundColor Red
    $regressions | Format-Table -AutoSize | Out-String | Write-Host
  }
  Write-Host "Ratchet failed. Remove the new warnings, or re-baseline deliberately and say why in the commit message." -ForegroundColor Red
  exit 1
}

Write-Host "Ratchet OK." -ForegroundColor Green
exit 0

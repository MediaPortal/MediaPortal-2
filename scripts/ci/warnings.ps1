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

  # Which solution the log belongs to ('Server', 'Client'). The floors describe one solution's
  # inventory, so scoring another solution's log against them would be meaningless - see $doc.solution.
  [string]$Solution,

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
$untracked = @()
$baselineSolution = ''
if (Test-Path $BaselinePath) {
  # UTF-8 explicitly: the notes contain non-ASCII characters and these files carry no BOM.
  $doc = [System.IO.File]::ReadAllText((Resolve-Path $BaselinePath).ProviderPath,
                                       [System.Text.Encoding]::UTF8) | ConvertFrom-Json
  $baseline = @{}
  $doc.warnings.PSObject.Properties | ForEach-Object { $baseline[$_.Name] = [int]$_.Value }
  # Codes NuGet emits during the build but which are not a property of the source alone. See the
  # comment at the split below.
  if ($doc.untracked) { $untracked = @($doc.untracked) }
  if ($doc.solution) { $baselineSolution = [string]$doc.solution }
}

function Save-Baseline {
  # Per-code documentation (why a floor has the value it has) must survive a re-generation,
  # otherwise a future reader cannot tell an intentional floor from an accident.
  $notes = [ordered]@{}
  $carryUntracked = @()
  $carrySolution = ''
  if (Test-Path $BaselinePath) {
    $existing = [System.IO.File]::ReadAllText((Resolve-Path $BaselinePath).ProviderPath,
                                              [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    if ($existing.notes) {
      $existing.notes.PSObject.Properties | ForEach-Object { $notes[$_.Name] = [string]$_.Value }
    }
    if ($existing.untracked) { $carryUntracked = @($existing.untracked) }
    if ($existing.solution) { $carrySolution = [string]$existing.solution }
  }
  $writtenFor = if ($Solution) { $Solution } else { $carrySolution }

  # Untracked codes are still written down, under informational, so the number is visible in the
  # repo without being a floor somebody can fail on.
  $info = [ordered]@{}
  foreach ($key in ($current.warnings.Keys | Sort-Object)) {
    if ($carryUntracked -contains $key) { $info[$key] = $current.warnings[$key] }
  }

  $payload = [ordered]@{
    generated = (Get-Date).ToString('yyyy-MM-dd')
    log       = Split-Path $LogPath -Leaf
    solution  = $writtenFor
    toolset   = 'The .NET SDK host (dotnet msbuild). Counts depend on this: severity comes from MediaPortal\.editorconfig, but the inventory also depends on which target frameworks the host builds and analyses, so it is NOT comparable with a full Visual Studio MSBuild log (that host sees fewer target frameworks and applies its own rule sets). See README.md, "Which build host, and why it matters".'
    note      = 'Maximum allowed occurrences per warning code. Regenerate with warnings.ps1 -UpdateBaseline after an intentional change and record why in notes.<CODE> and the commit message. Codes listed in "untracked" are reported under "informational" instead and never fail the gate.'
    untracked = $carryUntracked
    warnings  = [ordered]@{}
    informational = $info
    notes     = $notes
  }
  foreach ($key in ($current.warnings.Keys | Sort-Object)) {
    if ($carryUntracked -contains $key) { continue }
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

# The floors describe the inventory of one solution. MP2-Client.sln overlaps MP2-Server.sln but adds
# the whole UI on top, so scoring its log against these floors would report hundreds of "new" codes
# that are simply not part of this measurement. The client gets its own baseline file when Phase 2
# starts replacing it; until then it is built but not scored.
if ($Solution -and $baselineSolution -and ($Solution -ne $baselineSolution)) {
  Write-Host "Not scored: this baseline describes '$baselineSolution', the log is '$Solution'. Create a separate baseline with -BaselinePath warning-baseline.$Solution.json -UpdateBaseline -Solution $Solution to gate it." -ForegroundColor Yellow
  exit 0
}

$regressions = @()
$improvements = @()
$newCodes = @()

# These codes come from NuGet's restore/audit targets, which run *inside* the build and only speak
# up when a project re-evaluates its assets. Two consecutive runs of the same source with the same
# options and a forced restore (build.ps1 -Solution Server, 2026-10-18) produced 778 findings and
# then 776: NU1510 21 then 20, NU1902 50 then 49. A floor that jitters by one makes the gate red at
# random, and a gate that is red at random gets ignored, so they are reported but not enforced. What
# they actually measure - which packages carry known advisories - is measured properly, and
# deterministically, by packages.ps1.
$tracked = @{}
$informational = [ordered]@{}
foreach ($code in ($current.warnings.Keys | Sort-Object)) {
  if ($untracked -contains $code) { $informational[$code] = $current.warnings[$code]; continue }
  $tracked[$code] = $current.warnings[$code]
}

foreach ($code in ($tracked.Keys | Sort-Object)) {
  $now = $tracked[$code]
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

$curTotal = ($tracked.Values | Measure-Object -Sum).Sum
$baseTotal = ($baseline.Values | Measure-Object -Sum).Sum
Write-Host "findings: $curTotal (baseline $baseTotal, $($current.duplicates) duplicate log lines ignored)"
if ($informational.Count) {
  $parts = ($informational.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Value)" }) -join '  '
  Write-Host "package state (not gated here, see packages.ps1): $($parts -replace 'NU', '')" -ForegroundColor DarkGray
}

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

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
  [Parameter(Mandatory)][string]$LogPath,

  # Resolved in the body: $PSScriptRoot is still empty while the param block is evaluated.
  [string]$BaselinePath,

  [switch]$UpdateBaseline
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'warning-baseline.json' }

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
  $payload = [ordered]@{
    generated = (Get-Date).ToString('yyyy-MM-dd')
    log       = Split-Path $LogPath -Leaf
    toolset   = 'Full Visual Studio MSBuild. Counts depend on this: the bare AllRules.ruleset only resolves on a Visual Studio host, so these numbers are NOT comparable with dotnet build or Linux logs. See README.md.'
    note      = 'Maximum allowed occurrences per warning code. Regenerate with warnings.ps1 -UpdateBaseline after an intentional decrease.'
    warnings  = [ordered]@{}
  }
  foreach ($key in ($current.warnings.Keys | Sort-Object)) {
    $payload.warnings[$key] = $current.warnings[$key]
  }
  $payload | ConvertTo-Json -Depth 4 | Set-Content -Path $BaselinePath -Encoding utf8
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

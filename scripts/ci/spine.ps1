<#
.SYNOPSIS
  Counts the Windows-only "spine" in the portable core, and fails when it grows.

.DESCRIPTION
  Companion to warnings.ps1, because that one cannot see the whole problem.

  CA1416 only fires for Windows-only APIs that arrive from NuGet packages
  (System.Drawing.Common, System.Management, Microsoft.Win32.Registry). Types that the
  -windows target framework itself supplies - System.Windows.Forms, WPF - are considered
  Windows by construction, so calls to them produce no diagnostic at all. Removing three
  WinForms call sites from MediaPortal.Common changed CA1416 by exactly zero.

  So the portable core needs a second, deliberately crude metric: how many references to
  System.Windows.Forms and System.Drawing are still compiled into the assemblies that must
  run on Linux. It cannot tell whether a specific call works, but it can only go down, and
  it is the number that has to reach zero before those assemblies can lose their -windows
  target.

  Note this counts textual references, including using-directives and doc comments. Crude
  on purpose: a grep cannot miss a namespace, whereas an analyzer can be silenced.

.EXAMPLE
  .\spine.ps1
  .\spine.ps1 -UpdateBaseline
#>
[CmdletBinding()]
param(
  [string]$BaselinePath,
  [switch]$UpdateBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'spine-baseline.json' }

# Assemblies that must eventually build without a -windows target.
$coreAreas = @(
  'MediaPortal\Source\Core\MediaPortal.Common',
  'MediaPortal\Source\Core\MediaPortal.Utilities',
  'MediaPortal\Source\Core\MediaPortal.Backend'
)

$patterns = [ordered]@{
  WinForms = 'System\.Windows\.Forms'
  Drawing  = 'System\.Drawing'
}

$result = @{}
$affected = @()
$totalRefs = 0

foreach ($area in $coreAreas) {
  $areaRoot = Join-Path $repoRoot $area
  if (-not (Test-Path $areaRoot)) { throw "Portable core area missing: $areaRoot" }
  $areaCounts = @{}
  foreach ($k in $patterns.Keys) { $areaCounts[$k] = 0 }

  $files = Get-ChildItem $areaRoot -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

  foreach ($file in $files) {
    $text = [System.IO.File]::ReadAllText($file.FullName)
    $fileHits = 0
    foreach ($k in $patterns.Keys) {
      $n = ([regex]::Matches($text, $patterns[$k])).Count
      $areaCounts[$k] += $n
      $fileHits += $n
    }
    if ($fileHits -gt 0) {
      $affected += [pscustomobject]@{ File = $file.FullName.Substring($repoRoot.Length + 1); Refs = $fileHits }
      $totalRefs += $fileHits
    }
  }
  $result[$area] = $areaCounts
}

function Write-Report {
  Write-Host "Windows spine still compiled into the portable core:" -ForegroundColor Cyan
  foreach ($area in $result.Keys) {
    "  {0,-42} WinForms {1,4}   System.Drawing {2,4}" -f $area, $result[$area]['WinForms'], $result[$area]['Drawing']
  }
  "  {0,-42} files affected {1,4}   total refs {2,4}" -f '', $affected.Count, $totalRefs
}

Write-Report

if ($UpdateBaseline) {
  $payload = [ordered]@{
    generated = (Get-Date).ToString('yyyy-MM-dd')
    note      = 'Textual references to System.Windows.Forms / System.Drawing in the portable core. Must only decrease. Reaches zero when Common/Utilities/Backend can drop the -windows target.'
    areas     = $result
    filesAffected = $affected.Count
    totalRefs = $totalRefs
  }
  $payload | ConvertTo-Json -Depth 5 | Set-Content -Path $BaselinePath -Encoding utf8
  Write-Host "Spine baseline written: $BaselinePath" -ForegroundColor Green
  exit 0
}

if (-not (Test-Path $BaselinePath)) {
  Write-Host "No baseline at $BaselinePath - run with -UpdateBaseline to create one." -ForegroundColor Yellow
  exit 0
}

$doc = Get-Content $BaselinePath -Raw | ConvertFrom-Json
$fail = $false

if ($totalRefs -gt [int]$doc.totalRefs) {
  Write-Host "REGRESSION: $totalRefs Windows references, baseline was $($doc.totalRefs)." -ForegroundColor Red
  $fail = $true
}
if ($affected.Count -gt [int]$doc.filesAffected) {
  Write-Host "REGRESSION: Windows references spread to $($affected.Count) files, baseline was $($doc.filesAffected)." -ForegroundColor Red
  $fail = $true
}

if ($fail) {
  Write-Host ""
  Write-Host "Newest spine files (worst first):" -ForegroundColor Red
  $affected | Sort-Object Refs -Descending | Select-Object -First 10 | Format-Table -AutoSize | Out-String | Write-Host
  Write-Host "Move the Windows code to a Windows-only satellite, or replace it with a portable equivalent." -ForegroundColor Red
  exit 1
}

if ($totalRefs -lt [int]$doc.totalRefs) {
  Write-Host "Improved from $($doc.totalRefs) to $totalRefs - commit the new floor with -UpdateBaseline." -ForegroundColor Green
}
Write-Host "Spine OK." -ForegroundColor Green
exit 0
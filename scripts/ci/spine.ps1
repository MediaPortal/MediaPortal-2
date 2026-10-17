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

  Two ledger sections are counted, and both may only shrink:

    core       Common, Utilities, Backend - code that has to become portable.
    satellites *.Windows companion assemblies. Code moved here is quarantined, NOT fixed.
               They need their own floor because a satellite declares
               [SupportedOSPlatform("windows")], which makes CA1416 treat its own call sites
               as platform-neutral: moving code there removes it from the CA1416 count without
               removing a single Windows dependency. Measured, not theorised - extracting
               SplashScreen.cs moved CA1416 by -20 while changing no behaviour at all.

  Counts are textual references in code (comments stripped). Crude on purpose: a grep cannot
  miss a namespace, whereas an analyzer can be silenced or satisfied by an annotation.

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

function Get-CodeOnlyText {
<#
    Strips comments before counting. A comment that merely mentions a namespace is not a compile
    dependency, and counting one would put the floor out of sync with reality and train everyone to
    ignore this report. Heuristic: drop /* ... */ blocks, then drop // ... to end of line, but not
    when the slashes follow a colon so that URIs inside string literals survive.
#>
  [CmdletBinding()]
  param([Parameter(Mandatory)][string] $Text)

  $noBlocks = [regex]::Replace($Text, '/\*[\s\S]*?\*/', ' ')
  return [regex]::Replace($noBlocks, '(?<!:)//[^\r\n]*', ' ')
}

function Measure-SpineArea {
<#
    Counts WinForms/GDI+ references over a set of source directories.
    Returns Areas (per directory code counts), Files (per file totals) and TotalRefs.
#>
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][string[]] $Dirs,
    [Parameter(Mandatory)][System.Collections.Specialized.OrderedDictionary] $Patterns
  )

  $areas = [ordered]@{}
  $files = @()
  $total = 0

  foreach ($dir in $Dirs) {
    $areaRoot = Join-Path $repoRoot $dir
    if (-not (Test-Path $areaRoot)) { throw "source area missing: $areaRoot" }
    $areaCounts = @{}
    foreach ($k in $Patterns.Keys) { $areaCounts[$k] = 0 }

    foreach ($file in (Get-ChildItem $areaRoot -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })) {
      $text = Get-CodeOnlyText -Text ([System.IO.File]::ReadAllText($file.FullName))
      $fileHits = 0
      foreach ($k in $Patterns.Keys) {
        $n = ([regex]::Matches($text, $Patterns[$k])).Count
        $areaCounts[$k] += $n
        $fileHits += $n
      }
      if ($fileHits -gt 0) {
        $files += [pscustomobject]@{ File = $file.FullName.Substring($repoRoot.Length + 1); Refs = $fileHits }
        $total += $fileHits
      }
    }
    $areas[$dir] = $areaCounts
  }

  return [pscustomobject]@{ Areas = $areas; Files = $files; TotalRefs = $total }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'spine-baseline.json' }

# Assemblies that must eventually build without a -windows target.
$coreAreas = @(
  'MediaPortal\Source\Core\MediaPortal.Common',
  'MediaPortal\Source\Core\MediaPortal.Utilities',
  'MediaPortal\Source\Core\MediaPortal.Backend'
)

# Windows-only satellites are discovered, not listed, so a new one cannot escape the ledger.
$satelliteAreas = @(
  Get-ChildItem (Join-Path $repoRoot 'MediaPortal\Source\Core') -Directory -Filter '*.Windows' |
    ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1) } |
    Sort-Object
)

$patterns = [ordered]@{
  WinForms = 'System\.Windows\.Forms'
  Drawing  = 'System\.Drawing'
}

$core = Measure-SpineArea -Dirs $coreAreas -Patterns $patterns
$sat = Measure-SpineArea -Dirs $satelliteAreas -Patterns $patterns

function Write-AreaReport {
  [CmdletBinding()]
  param([string] $Title, [string] $Colour, [pscustomobject] $Measure)

  Write-Host $Title -ForegroundColor $Colour
  foreach ($area in $Measure.Areas.Keys) {
    "  {0,-52} WinForms {1,4}   System.Drawing {2,4}" -f $area, $Measure.Areas[$area]['WinForms'], $Measure.Areas[$area]['Drawing']
  }
  "  {0,-52} files affected {1,4}   total refs {2,4}" -f '', $Measure.Files.Count, $Measure.TotalRefs
  $Measure.Files | Sort-Object Refs -Descending | ForEach-Object { "    {0,-80} {1,3}" -f $_.File, $_.Refs } | ForEach-Object { Write-Host $_ }
}

Write-AreaReport -Title 'Windows spine still compiled into the portable core (must become 0):' -Colour Cyan -Measure $core
Write-AreaReport -Title 'Windows-only satellites (quarantined, not fixed - must not grow):' -Colour DarkGray -Measure $sat

if ($UpdateBaseline) {
  $payload = [ordered]@{
    generated = (Get-Date).ToString('yyyy-MM-dd')
    note      = 'Textual references to System.Windows.Forms / System.Drawing, comments stripped. core = code that must become portable, floor must reach 0. satellites = *.Windows companions holding quarantined code; they must not grow either, because moving code there changes no behaviour and buys no portability (their SupportedOSPlatform declaration even hides those call sites from CA1416).'
    areas     = $core.Areas
    filesAffected = $core.Files.Count
    totalRefs = $core.TotalRefs
    satelliteAreas = $sat.Areas
    satelliteFilesAffected = $sat.Files.Count
    satelliteTotalRefs = $sat.TotalRefs
  }
  # No BOM: PowerShell 5.1 -Encoding utf8 would prepend one.
  [System.IO.File]::WriteAllText(
    [System.IO.Path]::GetFullPath($BaselinePath),
    ($payload | ConvertTo-Json -Depth 5),
    (New-Object System.Text.UTF8Encoding($false)))
  Write-Host "Spine baseline written: $BaselinePath" -ForegroundColor Green
  exit 0
}

if (-not (Test-Path $BaselinePath)) {
  Write-Host "No baseline at $BaselinePath - run with -UpdateBaseline to create one." -ForegroundColor Yellow
  exit 0
}

$doc = Get-Content $BaselinePath -Raw | ConvertFrom-Json
$fail = $false

function Test-Growth {
  [CmdletBinding()]
  param([string] $Label, [int] $Now, [int] $Floor, [int] $FilesNow, [int] $FilesFloor)

  if ($Now -gt $Floor) {
    Write-Host "REGRESSION: $Label has $Now Windows references, floor was $Floor." -ForegroundColor Red
    $script:fail = $true
  }
  if ($FilesNow -gt $FilesFloor) {
    Write-Host "REGRESSION: $Label spread to $FilesNow files, floor was $FilesFloor." -ForegroundColor Red
    $script:fail = $true
  }
  if ($Now -lt $Floor) {
    Write-Host "improved: $Label $Floor -> $Now (commit the new floor with -UpdateBaseline)" -ForegroundColor Green
  }
}

Test-Growth -Label 'portable core' -Now $core.TotalRefs -Floor ([int]$doc.totalRefs) -FilesNow $core.Files.Count -FilesFloor ([int]$doc.filesAffected)

if ($doc.PSObject.Properties.Name -contains 'satelliteTotalRefs') {
  Test-Growth -Label 'Windows satellites' -Now $sat.TotalRefs -Floor ([int]$doc.satelliteTotalRefs) -FilesNow $sat.Files.Count -FilesFloor ([int]$doc.satelliteFilesAffected)
} else {
  Write-Host "baseline has no satellite section yet - run -UpdateBaseline to start gating it" -ForegroundColor Yellow
}

if ($fail) {
  Write-Host ""
  Write-Host "Files carrying the spine (worst first):" -ForegroundColor Red
  @($core.Files) + @($sat.Files) | Sort-Object Refs -Descending | Select-Object -First 12 | Format-Table -AutoSize | Out-String | Write-Host
  Write-Host "In the portable core: replace with a portable equivalent, or move genuinely Win32 code to a *.Windows satellite." -ForegroundColor Red
  Write-Host "In a satellite: it must not grow - a satellite is a quarantine, not a solution." -ForegroundColor Red
  exit 1
}

Write-Host "Spine OK." -ForegroundColor Green
exit 0

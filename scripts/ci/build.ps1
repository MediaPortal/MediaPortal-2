<#
.SYNOPSIS
  Builds MP2 solutions - or single projects - and feeds the warning ratchet.

.DESCRIPTION
  The default host is `dotnet` (the .NET SDK), because that is what every contributor and every
  container actually has: it needs no Visual Studio, it is the only host that can target .NET 10
  (Visual Studio 2022 17.14 refuses with NETSDK1045 - its MSBuild is 17.14, the .NET 10 SDK ships
  18.9), and since MediaPortal\.editorconfig carries the analyzer severity it measures the same
  rules as any other host. `-BuildHost MsBuild` still works and is kept for comparison; the two
  hosts are NOT comparable in absolute totals - see "The warning surface is toolchain dependent"
  in scripts/ci/README.md.

.EXAMPLE
  .\build.ps1                       # server solution, dotnet host, with the ratchet
  .\build.ps1 -Solution Server,Client -Ratchet
  .\build.ps1 -BuildHost MsBuild    # full Visual Studio MSBuild, for comparison

.EXAMPLE
  # One project, e.g. when its solution does not restore as a whole (see README).
  # The warning ratchet is calibrated per solution, so it is not applied to single projects.
  .\build.ps1 -Project UI\SkinEngine\SkinEngine.csproj
#>
[CmdletBinding()]
param(
  [ValidateSet('Server', 'Client', 'ServiceMonitor')]
  [string[]]$Solution = @('Server'),

  # Build these projects instead of a solution. Path is absolute or relative to MediaPortal\Source.
  [string[]]$Project,

  # Only used by -Project: MP2's build.targets copies dependencies to $(SolutionDir)..\Bin\$(SolutionName),
  # and neither variable exists when MSBuild is handed a bare project. This picks the folder name.
  [string]$SolutionName = 'MP2-Standalone',

  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  # 'Dotnet' (default) uses the .NET SDK on PATH. 'MsBuild' uses full Visual Studio MSBuild.
  [ValidateSet('Dotnet', 'MsBuild')]
  [string]$BuildHost = 'Dotnet',

  [string]$MsBuildPath,
  [string]$DotnetPath,
  [string]$ArtifactsDir,
  [switch]$SkipRestore,

  # Extra MSBuild properties, e.g. 'Platform=x86'. A bare project build has no solution to take the
  # platform from and defaults to AnyCPU, which projects such as MediaPortal.Manager (Debug|x86
  # only) do not define.
  [string[]]$MsBuildProperty,

  # After building, fail if the warning inventory regressed against the committed baseline.
  [switch]$Ratchet
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sourceDir = Join-Path $repoRoot 'MediaPortal\Source'
if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $repoRoot 'build\artifacts' }
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

if ($BuildHost -eq 'MsBuild') {
  $tool = if ($MsBuildPath) { $MsBuildPath } else { Find-MsBuild }
  # MSBuild.exe takes the project or solution as its first argument.
  $toolArgs = @()
} else {
  $tool = if ($DotnetPath) { $DotnetPath } else { (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
  if (-not $tool) { throw 'dotnet not found on PATH. Install the .NET SDK, or pass -DotnetPath.' }
  # `dotnet` needs a verb before anything else, and `dotnet msbuild` is a thin wrapper that accepts
  # the same /t:, /v:, /m, /p: and /flp: switches as MSBuild.exe. That keeps one argument list for
  # both hosts instead of translating between the CLI and MSBuild spellings.
  $toolArgs = @('msbuild')
}

Write-Host "Host    : $BuildHost"
Write-Host "Tool    : $tool"
Write-Host "Config  : $Configuration"

$solutions = @{
  Server         = Join-Path $sourceDir 'MP2-Server.sln'
  Client         = Join-Path $sourceDir 'MP2-Client.sln'
  ServiceMonitor = Join-Path $sourceDir 'MP2-ServiceMonitor.sln'
}

# A bare project needs what a solution would have defined.
$solutionLevelArgs = @()

# Both hosts take MSBuild syntax for properties and loggers, so one argument list serves both.
# EnableWindowsTargeting lets the net48 legs resolve reference assemblies without Visual Studio,
# which is what makes the Windows numbers and the Linux container numbers describe the same build.
$hostArgs = @("/p:Configuration=$Configuration", '/p:EnableWindowsTargeting=true')

function Invoke-BuildTarget {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][string] $Name,
    [Parameter(Mandatory)][string] $Path,
    # The floors in warning-baseline.json were measured on a whole solution. A project that the
    # solution does not contain can legitimately exceed one, so the ratchet is only sound for the
    # solutions it was calibrated on.
    [switch]$ApplyRatchet
  )

  $log = Join-Path $ArtifactsDir "build-$Name-$Configuration.log"

  Write-Host ""
  Write-Host "=== $Name ===" -ForegroundColor Cyan

  if (-not $SkipRestore) {
    # Out-Host, not the pipeline: whatever a native command prints would otherwise become this
    # function's return value and mask the numeric code the caller tests.
    #
    # RestoreForce: NuGet only re-audits packages when it actually re-resolves them, and the NU19xx /
    # NU1701 / NU1510 findings are replayed into every later build from the assets file. Without a
    # forced restore a project whose assets were still considered current stayed silent, so the same
    # tree scored differently from one gated run to the next (NU1902 50 then 51 on MP2-Server.sln).
    # The ratchet needs the run to be a function of the source, not of what happened last time.
    & $tool @toolArgs $Path /t:Restore /v:q /nologo /m /p:RestoreForce=true @hostArgs @solutionLevelArgs | Out-Host
    if ($LASTEXITCODE -ne 0) { Write-Host "RESTORE FAILED ($Name)" -ForegroundColor Red; return 1 }
  }

  & $tool @toolArgs $Path /t:Rebuild /v:m /nologo /m @hostArgs "/flp:logfile=$log;verbosity=normal" @solutionLevelArgs | Out-Host
  if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED ($Name), exit $LASTEXITCODE  ->  $log" -ForegroundColor Red
    return 1
  }

  $diag = Get-LogDiagnostics -LogPath $log
  $warnTotal = ($diag.warnings.Values | Measure-Object -Sum).Sum
  # $warnTotal counts findings, not log lines; MSBuild writes each diagnostic at least twice into
  # the file log (see Get-LogDiagnostics), so the suppressed count is printed to keep the number
  # explainable to someone reading the log by hand.
  $dupNote = if ($diag.duplicates) { ", $($diag.duplicates) duplicate log lines ignored" } else { '' }
  Write-Host "BUILD OK ($Name): $($diag.errors) errors, $warnTotal findings$dupNote  ->  $log"
  if ($diag.errors -gt 0) { return 1 }

  if ($Ratchet -and $ApplyRatchet) {
    & (Join-Path $PSScriptRoot 'warnings.ps1') -LogPath $log -Solution $Name
    if ($LASTEXITCODE -ne 0) { return 1 }
  }
  return 0
}

$exitCode = 0

if ($Project) {
  $solutionLevelArgs = @("/p:SolutionDir=$sourceDir\", "/p:SolutionName=$SolutionName")
  foreach ($extra in @($MsBuildProperty)) { if ($extra) { $solutionLevelArgs += "/p:$extra" } }
  foreach ($p in $Project) {
    $full = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { Join-Path $sourceDir $p }
    if (-not (Test-Path $full)) { Write-Host "project not found: $full" -ForegroundColor Red; $exitCode = 1; continue }
    $name = [System.IO.Path]::GetFileNameWithoutExtension($full)
    if (Invoke-BuildTarget -Name $name -Path $full) { $exitCode = 1 }
  }
} else {
  foreach ($name in $Solution) {
    $sln = $solutions[$name]
    if (-not (Test-Path $sln)) { throw "Solution not found: $sln" }
    if (Invoke-BuildTarget -Name $name -Path $sln -ApplyRatchet) { $exitCode = 1 }
  }
}

if ($Ratchet) {
  # Independent of what was built: these scan source, not a build log.
  & (Join-Path $PSScriptRoot 'spine.ps1')
  if ($LASTEXITCODE -ne 0) { $exitCode = 1 }

  & (Join-Path $PSScriptRoot 'satellites.ps1')
  if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}

exit $exitCode

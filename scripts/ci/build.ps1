<#
.SYNOPSIS
  Builds MP2 solutions - or single projects - on Windows with full Visual Studio MSBuild.

.EXAMPLE
  .\build.ps1                       # server solution
  .\build.ps1 -Solution Server,Client
  .\build.ps1 -Configuration Release -Ratchet

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

  [string]$MsBuildPath,
  [string]$ArtifactsDir,
  [switch]$SkipRestore,

  # After building, fail if the warning inventory regressed against the committed baseline.
  [switch]$Ratchet
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sourceDir = Join-Path $repoRoot 'MediaPortal\Source'
if (-not $ArtifactsDir) { $ArtifactsDir = Join-Path $repoRoot 'build\artifacts' }
New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null

$msbuild = if ($MsBuildPath) { $MsBuildPath } else { Find-MsBuild }
Write-Host "MSBuild : $msbuild"
Write-Host "Config  : $Configuration"

$solutions = @{
  Server         = Join-Path $sourceDir 'MP2-Server.sln'
  Client         = Join-Path $sourceDir 'MP2-Client.sln'
  ServiceMonitor = Join-Path $sourceDir 'MP2-ServiceMonitor.sln'
}

# A bare project needs what a solution would have defined.
$solutionLevelArgs = @()

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
    & $msbuild $Path /t:Restore /v:q /nologo /m /p:Configuration=$Configuration @solutionLevelArgs | Out-Host
    if ($LASTEXITCODE -ne 0) { Write-Host "RESTORE FAILED ($Name)" -ForegroundColor Red; return 1 }
  }

  & $msbuild $Path /t:Rebuild /v:m /nologo /m /p:Configuration=$Configuration @solutionLevelArgs "/flp:logfile=$log;verbosity=normal" | Out-Host
  if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED ($Name), exit $LASTEXITCODE  ->  $log" -ForegroundColor Red
    return 1
  }

  $diag = Get-LogDiagnostics -LogPath $log
  $warnTotal = ($diag.warnings.Values | Measure-Object -Sum).Sum
  Write-Host "BUILD OK ($Name): $($diag.errors) errors, $warnTotal warnings  ->  $log"
  if ($diag.errors -gt 0) { return 1 }

  if ($Ratchet -and $ApplyRatchet) {
    & (Join-Path $PSScriptRoot 'warnings.ps1') -LogPath $log
    if ($LASTEXITCODE -ne 0) { return 1 }
  }
  return 0
}

$exitCode = 0

if ($Project) {
  $solutionLevelArgs = @("/p:SolutionDir=$sourceDir\", "/p:SolutionName=$SolutionName")
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
  # Independent of what was built: it scans source, not a build log.
  & (Join-Path $PSScriptRoot 'spine.ps1')
  if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}

exit $exitCode

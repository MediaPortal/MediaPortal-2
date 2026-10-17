<#
.SYNOPSIS
  Builds the MP2 solutions on Windows with full Visual Studio MSBuild.

.EXAMPLE
  .\build.ps1                       # server solution
  .\build.ps1 -Solution Server,Client
  .\build.ps1 -Configuration Release -Ratchet
#>
[CmdletBinding()]
param(
  [ValidateSet('Server', 'Client', 'ServiceMonitor')]
  [string[]]$Solution = @('Server'),

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

$exitCode = 0
foreach ($name in $Solution) {
  $sln = $solutions[$name]
  if (-not (Test-Path $sln)) { throw "Solution not found: $sln" }
  $log = Join-Path $ArtifactsDir "build-$name-$Configuration.log"

  Write-Host ""
  Write-Host "=== $name ===" -ForegroundColor Cyan

  if (-not $SkipRestore) {
    & $msbuild $sln /t:Restore /v:q /nologo /m /p:Configuration=$Configuration
    if ($LASTEXITCODE -ne 0) { Write-Host "RESTORE FAILED ($name)" -ForegroundColor Red; $exitCode = 1; continue }
  }

  & $msbuild $sln /t:Rebuild /v:m /nologo /m /p:Configuration=$Configuration "/flp:logfile=$log;verbosity=normal"
  if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED ($name), exit $LASTEXITCODE" -ForegroundColor Red; $exitCode = 1; continue }

  $diag = Get-LogDiagnostics -LogPath $log
  $warnTotal = ($diag.warnings.Values | Measure-Object -Sum).Sum
  Write-Host "BUILD OK ($name): $($diag.errors) errors, $warnTotal warnings  ->  $log"

  if ($Ratchet) {
    & (Join-Path $PSScriptRoot 'warnings.ps1') -LogPath $log
    if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
  }
}

if ($Ratchet) {
  # Independent of which solutions were built: it scans source, not a build log.
  & (Join-Path $PSScriptRoot 'spine.ps1')
  if ($LASTEXITCODE -ne 0) { $exitCode = 1 }
}

exit $exitCode

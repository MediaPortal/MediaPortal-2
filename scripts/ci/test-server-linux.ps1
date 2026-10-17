<#
.SYNOPSIS
  Compiles the MP2 server solution inside a Linux container (cross-compile probe).
.DESCRIPTION
  Answers one question: can the solution be *built* on Linux? It does not run anything, and a
  green run does not mean the code works there - net6.0-windows assemblies still cannot execute
  on Linux. See scripts/ci/Dockerfile.cross for what this proves and what it needs.
.EXAMPLE
  .\test-server-linux.ps1
.EXAMPLE
  .\test-server-linux.ps1 -FallbackPackageFolder D:\shared\nuget-cache
#>
[CmdletBinding()]
param(
  [string]$ImageName = 'mp2-cross',
  # Packages that only the dead MyGet feed ever served (BASSCombined, BassRegistration,
  # BASS.NET 2.4.14.1, MediaPortal.TagLib, OnlineVideos.Core, ffmpeg 3.4.2). The container reads
  # them from here and downloads everything else from nuget.org into its own layer.
  [string]$FallbackPackageFolder = (Join-Path $env:USERPROFILE '.nuget\packages'),
  [switch]$SkipImageBuild
)

# Docker writes its BuildKit progress to stderr, which PowerShell turns into a terminating
# error record under 'Stop'. Failures are detected through $LASTEXITCODE instead.
$ErrorActionPreference = 'Continue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if (-not (Test-Path $FallbackPackageFolder)) {
  throw "Fallback package folder '$FallbackPackageFolder' does not exist. Pass -FallbackPackageFolder with a NuGet cache that holds the MyGet-only packages."
}

if (-not $SkipImageBuild) {
  & docker build -f (Join-Path $PSScriptRoot 'Dockerfile.cross') -t $ImageName $repoRoot
  if ($LASTEXITCODE -ne 0) { throw "Container image build failed ($LASTEXITCODE)." }
}

& docker run --rm `
  -v "${FallbackPackageFolder}:/nuget-cache:ro" `
  -e 'NUGET_FALLBACK_PACKAGES=/nuget-cache' `
  $ImageName
$exit = $LASTEXITCODE

if ($exit -ne 0) { throw "Linux cross-compile failed ($exit)." }
Write-Host 'MP2 server solution compiles on Linux.' -ForegroundColor Green

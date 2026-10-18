<#
.SYNOPSIS
  Builds the front end solution and runs its headless UI tests inside a Linux container.
.DESCRIPTION
  Answers the Phase 2 question that Windows cannot answer: does the new Avalonia front end *run* on a
  non-Windows host, with no display? Dockerfile.cross compiles the server solution on Linux; this gate
  compiles MP2-Frontend.sln and then executes Tests.Frontend, which builds the real `HomeWindow.axaml`
  under Avalonia's headless platform and drives it - so it covers the XAML compiler, the theme, the
  bindings, the D-pad handler and the HTTP client against real `MediaPortal.Server.Api` controllers.

  What green means, and what it does not: it means the front end's own code runs on Linux. It does not
  mean anything is visible - there is no X server here and none is installed, which is exactly the
  configuration a headless set-top box shares. Rendering on a real Linux display, and Android, are still
  separate measurements (see docs/multiplatform/ROADMAP.md, Phase 2).

  The container is built from the whole tree on purpose (see Dockerfile.cross' note on -SkipImageBuild):
  an image reused from an earlier commit would report the front end of that commit.
.EXAMPLE
  .\test-frontend-linux.ps1
.EXAMPLE
  .\test-frontend-linux.ps1 -FallbackPackageFolder D:\shared\nuget-cache
#>
[CmdletBinding()]
param(
  [string]$ImageName = 'mp2-frontend',
  # The NuGet cache of a machine that has built the old tree; it holds the packages the dead MyGet feed
  # used to serve. Everything else is downloaded into the image layer.
  [string]$FallbackPackageFolder = (Join-Path $env:USERPROFILE '.nuget\packages'),
  [string]$SdkTag = '10.0',
  [switch]$SkipImageBuild
)

# Docker writes BuildKit progress to stderr, which PowerShell turns into a terminating error record
# under 'Stop'; results are taken from $LASTEXITCODE instead.
$ErrorActionPreference = 'Continue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).path

if (-not (Test-Path $FallbackPackageFolder)) {
  throw "Fallback package folder '$FallbackPackageFolder' does not exist. Pass -FallbackPackageFolder with a NuGet cache that holds the MyGet-only packages."
}

if (-not $SkipImageBuild) {
  & docker build -f (Join-Path $PSScriptRoot 'Dockerfile.frontend') -t $ImageName `
      --build-arg "SDK_TAG=$SdkTag" $repoRoot
  if ($LASTEXITCODE -ne 0) { throw "Container image build failed ($LASTEXITCODE)." }
} else {
  # Same caveat as test-server-linux.ps1: the image carries a snapshot of the tree taken when it was
  # built, so a reused image reports the front end of that snapshot, not of today's sources.
  $imageCreatedText = (& docker image inspect $ImageName --format '{{.Created}}' 2>$null)
  $imageCreated = [datetime]::MinValue
  if ($imageCreatedText) {
    $parsed = [datetime]::MinValue
    if ([datetime]::TryParse("$imageCreatedText", [ref]$parsed)) { $imageCreated = $parsed.ToUniversalTime() }
  }
  $newest = (Get-ChildItem $repoRoot -Recurse -Include '*.cs', '*.axaml', '*.csproj', '*.props', '*.sln' -File -ErrorAction SilentlyContinue |
      Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|packages)\\' } |
      Measure-Object -Property LastWriteTime -Maximum).Maximum
  if ($newest -and $newest.ToUniversalTime() -gt $imageCreated) {
    $text = ("WARNING: -SkipImageBuild with an image from {0} UTC, newest source from {1} UTC - the " +
        "container runs the SNAPSHOT, not the current tree. Drop -SkipImageBuild for a real answer.") -f `
        $imageCreated.ToString('s'), $newest.ToUniversalTime().ToString('s')
    Write-Host $text -ForegroundColor Yellow
  }
}

& docker run --rm `
  -v "${FallbackPackageFolder}:/nuget-cache:ro" `
  -e 'NUGET_FALLBACK_PACKAGES=/nuget-cache' `
  $ImageName
$exit = $LASTEXITCODE

if ($exit -ne 0) { throw "Front end build/test on Linux failed ($exit)." }
Write-Host 'Front end compiles on Linux and its headless UI tests pass there.' -ForegroundColor Green

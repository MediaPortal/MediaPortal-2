<#
.SYNOPSIS
  Starts the MP2 server inside a Linux container (run probe).
.DESCRIPTION
  test-server-linux.ps1 answers "does the server solution COMPILE on Linux?". This one answers the
  next question, "does the server RUN there?", by building the layout and starting the host inside a
  Linux container with the same two scripts the Windows gate uses (deploy-server-layout.ps1 and
  run-server-check.ps1).

  The value is in the failure list. Compiling proves nothing about code that calls Windows APIs at
  runtime, and this tree has already shown twice that a startup/shutdown defect can sit behind a
  green build. Expected blockers, in the order the server reaches them:

    1. System.Data.SQLite - SQLite.Interop.dll is Windows-only, so the database cannot open.
       Fix: Microsoft.Data.Sqlite (e_sqlite3 natives exist for linux-x64 and android).
    2. ASP.NET Core's HttpSys server (UPnP + resource servers) - http.sys does not exist outside
       Windows. Fix: Kestrel, which needs work because MP2 puts several hosts on one port under
       different base paths, something http.sys does natively and Kestrel does not.

  A red result here is useful; a green one means the headless host reaches "Switching to running
  state" on Linux, which is ROADMAP item d completed.
.EXAMPLE
  .\test-server-run-linux.ps1
.EXAMPLE
  .\test-server-run-linux.ps1 -SkipImageBuild   # reuse the image snapshot currently in Docker
#>
[CmdletBinding()]
param(
  [string]$ImageName = 'mp2-server',

  # Same trick as the cross-compile probe: packages that only the dead MyGet feed ever served are
  # read from a host folder, everything else comes from nuget.org.
  [string]$FallbackPackageFolder,

  [string]$SdkTag = '10.0',

  [switch]$SkipImageBuild
)

# Docker writes BuildKit progress to stderr; under 'Stop' PowerShell turns that into an error record.
# $LASTEXITCODE decides instead.
$ErrorActionPreference = 'Continue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot (Join-Path '..' '..'))).Path

if (-not $FallbackPackageFolder) {
  # $env:USERPROFILE only exists on Windows; a container-side path would be wrong here because the
  # folder is mounted from the host.
  $profile = if ($env:USERPROFILE) { $env:USERPROFILE } elseif ($env:HOME) { $env:HOME } else { $null }
  if (-not $profile) { throw 'Cannot find a user profile folder. Pass -FallbackPackageFolder.' }
  $FallbackPackageFolder = Join-Path (Join-Path $profile '.nuget') 'packages'
}
if (-not (Test-Path $FallbackPackageFolder)) {
  throw "Fallback package folder '$FallbackPackageFolder' does not exist. Pass -FallbackPackageFolder."
}

if (-not $SkipImageBuild) {
  & docker build -f (Join-Path $PSScriptRoot 'Dockerfile.server') -t $ImageName `
      --build-arg "SDK_TAG=$SdkTag" $repoRoot
  if ($LASTEXITCODE -ne 0) { throw "Container image build failed ($LASTEXITCODE)." }
} else {
  $imageCreatedText = (& docker image inspect $ImageName --format '{{.Created}}' 2>$null)
  $imageCreated = [datetime]::MinValue
  if ($imageCreatedText) {
    $parsed = [datetime]::MinValue
    if ([datetime]::TryParse("$imageCreatedText", [ref]$parsed)) { $imageCreated = $parsed.ToUniversalTime() }
  }
  $newest = (Get-ChildItem $repoRoot -Recurse -Include '*.cs', '*.csproj', '*.props', '*.sln' -File -ErrorAction SilentlyContinue |
      Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|packages)\\' } |
      Measure-Object -Property LastWriteTime -Maximum).Maximum
  if ($newest -and $newest.ToUniversalTime() -gt $imageCreated) {
    $text = ("WARNING: -SkipImageBuild with an image from {0} UTC, newest source from {1} UTC - the " +
        "container runs a SNAPSHOT of the tree, not the current one.") -f $imageCreated.ToString('s'), $newest.ToUniversalTime().ToString('s')
    Write-Host $text -ForegroundColor Yellow
  }
}

& docker run --rm `
  -v "${FallbackPackageFolder}:/nuget-cache:ro" `
  -e 'NUGET_FALLBACK_PACKAGES=/nuget-cache' `
  $ImageName
$exit = $LASTEXITCODE

if ($exit -ne 0) {
  Write-Host ''
  Write-Host "The server did not pass the check on Linux (exit $exit). Read the log lines above in order -" -ForegroundColor Yellow
  Write-Host 'the first FATAL or exception is the next thing to port.'
  exit $exit
}

Write-Host 'MP2 server runs on Linux.' -ForegroundColor Green

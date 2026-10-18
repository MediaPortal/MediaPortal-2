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
  green build. The list it produced, in the order the server reaches things, is closed:

    1. Closed (ROADMAP item j): System.Data.SQLite's SQLite.Interop.dll, Windows-only. The plugin uses
       Microsoft.Data.Sqlite now, and the layout copies the platform's own e_sqlite3 native, so the
       database opens here - the log shows SQLiteVersion: 3.53.3 from a connection on Linux.
    2. Closed (ROADMAP item k): ASP.NET Core's HttpSys server. http.sys is a kernel driver, so the one
       feature MP2 needed from it - several hosts sharing port 55555, each owning a URL prefix - is
       provided off Windows by SharedHttpPortHost, one Kestrel listener per port dispatching on the
       longest prefix. The log shows the resource server, two control points and one UPnP server on
       55555 together.
    3. Closed (ROADMAP item k): PathManager normalized stored labels but not the sub-path callers write
       behind them, so the subschemas' "<APPLICATION_ROOT>\Scripts\" was one file name on Linux and the
       database subschemas could not be created.

  What is left in the log is one WARN, and it is the documented one: the resource-mounting note that a
  container has no FUSE driver for. The three "Could not set IPv4 options / Cannot assign requested address"
  lines this probe used to end with were not the multicast setup - they were sockets being closed, where
  leaving a multicast group has to name the interface that joined it (Windows tolerated the missing
  interface, Linux did not). See ROADMAP item l.

  The probe also asks a second question now, through run-server-check.ps1's -ProbeUrl: it requests
  http://127.0.0.1:55555/MediaPortal/Server/health while the server runs and requires 200. "The listener
  started" and "the server answers" are different claims; here the endpoint answers with the PathBase and
  Path it was called with, which is how this container measured that the shared listener leaves the prefix in
  Path while http.sys moves it into PathBase.

  Also measured here, because it only shows up in a log: Stop() runs twice on the failed-startup path,
  and the second pass reported its own FATAL below the real one. Teardown is idempotent now, so the
  number of FATAL lines in this output is the number of real blockers.

  A green result means the headless host reaches "Switching to running state" on Linux, which is ROADMAP
  item d completed - measured green since item k.
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

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

  -ServeFromHost is the stronger run. The image has no server in it, so by default the probe draws the screen's
  own "no server" state - a real state, labelled as one in the OK line, but not the screen anybody will watch on a
  TV. With the switch, this script starts MP2-Server-Headless on the *host* (same layout the server gates use) and
  points the container at it through `host.docker.internal:55555`, which was measured from inside this image: it
  resolves and answers `HTTP/1.1 200 OK` with `Server: Microsoft-HTTPAPI/2.0`, while `172.17.0.1` does not connect
  at all. The image then refuses to go green unless the probe reports `content:  populated`, so the screenshot
  cannot quietly fall back to the offline picture.
.EXAMPLE
  .\test-frontend-linux.ps1
.EXAMPLE
  .\test-frontend-linux.ps1 -ServeFromHost -SkipImageBuild
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

  # Where the screenshot the container renders is written. Mounted into the image at /out.
  [string]$RenderDir,

  # Render the populated screen: start the server on this machine and let the container reach it.
  [switch]$ServeFromHost,

  # What the container is told to ask. `host.docker.internal` is Docker Desktop's name for the host.
  [string]$HostServerUrl = 'http://host.docker.internal:55555/MediaPortal/Server',

  # The runnable server layout, same default as run-server-check.ps1 and test-frontend-check.ps1.
  [string]$LayoutDir,

  [switch]$SkipDeploy,

  # How long the host server is held open for the container.
  [int]$HoldSeconds = 420,

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

# The screenshot the container renders is written here, so that the artifact leaves the image with the run.
if (-not $RenderDir) { $RenderDir = Join-Path (Join-Path $repoRoot 'build\artifacts') 'frontend-render' }
New-Item -ItemType Directory -Path $RenderDir -Force | Out-Null

$runArgs = @('--rm',
             '-v', "${FallbackPackageFolder}:/nuget-cache:ro",
             '-e', 'NUGET_FALLBACK_PACKAGES=/nuget-cache',
             '-v', "${RenderDir}:/out")

$server = $null
$dataDir = $null
try {
  if ($ServeFromHost) {
    if (-not $LayoutDir) { $LayoutDir = Join-Path (Join-Path $repoRoot 'build\artifacts') 'server-layout' }
    if (-not $SkipDeploy) {
      & (Join-Path $PSScriptRoot 'deploy-server-layout.ps1') -OutputDir $LayoutDir
      if (-not $?) { throw 'layout deployment failed' }
    }
    $hostDll = Join-Path $LayoutDir 'MP2-Server-Headless.dll'
    if (-not (Test-Path $hostDll)) { throw "no $hostDll - run deploy-server-layout.ps1 first" }

    $dataDir = Join-Path ([IO.Path]::GetTempPath()) ('mp2-frontend-linux-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

    Write-Host "==> starting the server on this host, so that the container has something to read ($HostServerUrl)" -ForegroundColor Cyan
    $server = Start-Process dotnet `
        -ArgumentList ('"' + $hostDll + '"', '--duration', "$HoldSeconds", '--data', '"' + $dataDir + '"') `
        -WorkingDirectory $LayoutDir -PassThru -WindowStyle Hidden

    # /health answers as soon as the listener is up, which is earlier than the point where the media library can
    # serve anything: measured, a server that has just started answers `200` with an *empty* aspect list, and 25
    # aspects about a second later. The screen is only populated once aspects exist, so that is what is waited
    # for - otherwise the probe would render the empty screen and the gate would depend on a race.
    #
    # Concatenated with '/', deliberately not Join-Path: Join-Path inserts a backslash on Windows, which turns
    # into a 404 here and looked for 90 seconds like a server that never got ready. The last answer is kept so a
    # failure says what the server actually said.
    $localUrl = ($HostServerUrl -replace 'host\.docker\.internal', '127.0.0.1').TrimEnd('/')
    $aspectsUrl = $localUrl + '/api/library/aspects'
    $ready = $false
    $lastAnswer = 'no attempt made'
    for ($try = 0; $try -lt 45; $try++) {
      if ($server.HasExited) { throw "the server exited (exit $($server.ExitCode)) before the container could use it" }
      try {
        $answer = Invoke-WebRequest -Uri $aspectsUrl -UseBasicParsing -TimeoutSec 5
        $aspects = $answer.Content | ConvertFrom-Json
        $count = 0
        if ($aspects.count) { $count = [int]$aspects.count } elseif ($aspects.aspects) { $count = @($aspects.aspects).Count }
        if ($count -gt 0) {
          Write-Host "==> the host server serves $count aspect(s) on $($try * 2)s of waiting" -ForegroundColor Cyan
          $ready = $true
          break
        }
        $lastAnswer = "HTTP $($answer.StatusCode) with $count aspects (the library is still starting)"
      } catch {
        $lastAnswer = $_.Exception.Message
        if ($_.Exception.Response) { $lastAnswer = "HTTP $([int]$_.Exception.Response.StatusCode)" }
      }
      Start-Sleep -Seconds 2
    }
    if (-not $ready) { throw "the host server never served aspects at $aspectsUrl; last answer: $lastAnswer" }
    Write-Host "==> server pid $($server.Id) is serving aspects; the container will read it as $HostServerUrl" -ForegroundColor Cyan

    $runArgs += @('-e', "MP2_SERVER=$HostServerUrl")
  }

  Write-Host '==> docker run' -ForegroundColor Cyan
  $run = & docker run @runArgs $ImageName 2>&1
  $exit = $LASTEXITCODE
  $run | ForEach-Object { "$_" }
} finally {
  if ($server -and -not $server.HasExited) {
    Write-Host '==> stopping the host server' -ForegroundColor Cyan
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
    $server.WaitForExit(15000) | Out-Null
  }
  if ($server) { $server.Dispose() }
  if ($dataDir) { Remove-Item $dataDir -Recurse -Force -ErrorAction SilentlyContinue }
}

if ($exit -ne 0) { throw "Front end build/test on Linux failed ($exit)." }
$screenshot = Join-Path $RenderDir 'home-linux.png'
if (-not (Test-Path $screenshot)) {
  throw "the run was green but no screenshot arrived at '$screenshot' - the mount is not working."
}

# The playback screen is the second frame and is checked the same way, because a screen that only ever ran on
# Avalonia's managed rasterizer has never been drawn by the code a Linux TV draws with.
$playbackShot = Join-Path $RenderDir 'nowplaying-linux.png'
if (-not (Test-Path $playbackShot)) {
  throw "the run was green but the playback screen left no frame at '$playbackShot'."
}

# The third frame is the screen a viewer sees before there is anything to show: no server picked, nothing
# saved. It is in the list because it is the one screen that has to work with no server at all - if the front
# end needs a connection to build a window, this frame is where that shows up.
$connectShot = Join-Path $RenderDir 'connect-linux.png'
if (-not (Test-Path $connectShot)) {
  throw "the run was green but the server-picking screen left no frame at '$connectShot'."
}

# The image checks this too, and both sides are worth checking: the image refuses a frame that did not reach the
# server, and this refuses a run whose output does not say it was populated, so neither can report the offline
# picture as the data picture.
if ($ServeFromHost) {
  $text = ($run | Out-String)
  if ($text -notmatch 'content:\s+populated from') {
    throw "asked for a populated screen and the run never said it drew one - see the output above."
  }
}

Write-Host ("Front end compiles on Linux, its headless UI tests pass there, and it rendered {0} ({1:N0} bytes), {2} ({3:N0} bytes) and {4} ({5:N0} bytes){6}." -f `
    $screenshot, (Get-Item $screenshot).Length, $playbackShot, (Get-Item $playbackShot).Length,
    $connectShot, (Get-Item $connectShot).Length,
    $(if ($ServeFromHost) { ', the home screen populated from the host server' } else { '' })) -ForegroundColor Green

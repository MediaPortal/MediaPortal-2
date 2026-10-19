<#
.SYNOPSIS
  Runs the new front end against a server that is really running, and requires it to report what it read.

.DESCRIPTION
  This is the gate that answers "does the front end actually work end to end?" as distinct from "does it
  compile" (build.ps1) and "do its parts behave" (Tests.Frontend, which hosts the server's real controllers
  inside the test process). Here nothing is mocked:

    1. a runnable server layout is deployed (deploy-server-layout.ps1);
    2. MP2-Server-Headless is started with --duration so that it holds its port open;
    3. MediaPortal.Frontend --check is run against it as a separate process, over real HTTP on a real socket;
    4. the run must exit 0 and print the answers it got;
    5. the same front end aimed at a port nobody listens on must exit 1 and say so, because a check that
       cannot fail is not a check;
    6. while the server is still up, Frontend.RenderProbe draws the real screens through Skia and writes
       sixteen PNGs (default build\artifacts\frontend-render\: home, continue, browser, details, search,
       seen, nowplaying, connect, pictures, browse, news, weather, party, settings, and the playback
       screen twice more drawn with the colours of the two rescued skins).
       The probe fails this gate if a frame comes back flat, and if the pixels it was painted with are not
       the palette the frame asked for; the pictures themselves are for a person, and -SkipRender leaves
       them out.

  What is asserted about the printed answers is their shape, never their counts: how many aspects a freshly
  created database exposes is a measurement of when the request arrived relative to the media library's
  startup, and it has been seen to be 10, 18 and 22 in three runs of the same tree. "aspects" in the output
  is the claim; "25 aspects" would be a test that fails on a Tuesday for no reason.

  Retrying: the front end exits 3 when the server answers 503, which is what the API says while the media
  library is still starting. That is a "ask again", so this script keeps asking until the deadline instead of
  calling it a failure. A 500 still fails immediately - the 503 contract exists precisely so that the two can
  be told apart.

.EXAMPLE
  .\test-frontend-check.ps1
  .\test-frontend-check.ps1 -SkipDeploy
  .\test-frontend-check.ps1 -Configuration Release
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  # Where the runnable server layout lives. The same default as run-server-check.ps1, so -SkipDeploy reuses
  # whatever that gate deployed last.
  [string]$LayoutDir,

  [switch]$SkipDeploy,

  [string]$ServerUrl = 'http://127.0.0.1:55555/MediaPortal/Server',

  # How long the server is held open. The front end's own per-request patience is 15 seconds, so this has to
  # cover the server's startup plus the whole conversation several times over.
  [int]$HoldSeconds = 150,

  # How long the front end may keep trying before the run counts as failed.
  [int]$ReadyTimeoutSeconds = 75,

  # Where the rendered screenshot goes. The render probe runs while the server is still up, so the picture is
  # the populated screen and not the offline one. Pass -SkipRender to leave the screen alone.
  [string]$RenderDir,

  [switch]$SkipRender,

  [string]$DotnetPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $LayoutDir) { $LayoutDir = Join-Path (Join-Path $repoRoot 'build\artifacts') 'server-layout' }

$dotnet = if ($DotnetPath) { $DotnetPath } else { (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet) { throw 'dotnet not found on PATH. Install the .NET SDK, or pass -DotnetPath.' }

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Fail([string]$message) { Write-Host "FAIL: $message" -ForegroundColor Red }

<#
.SYNOPSIS
  Fails the gate if a source file is newer than the binary that should have been built from it.

.DESCRIPTION
  This gate executes whatever is on disk, which makes a stale build the one failure mode it cannot otherwise
  see. It happened: after the HomeViewModel cancellation fix the first run still crashed with the old
  ObjectDisposedException, because only the test project had been rebuilt - the probe kept running against the
  copy of MediaPortal.Frontend.dll in its own bin folder. A gate that reports somebody else's binary is worse
  than no gate, because its verdict is trusted.
#>
function Assert-FrontendBuildIsCurrent {
  [CmdletBinding()]
  param([string[]] $Dlls)

  $sources = Get-ChildItem -Path (Join-Path $repoRoot 'MediaPortal\Source\Frontend'),
                                 (Join-Path $repoRoot 'MediaPortal\Tests\Frontend.RenderProbe') `
      -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -match '\.(cs|axaml|csproj)$' }

  foreach ($dll in $Dlls) {
    if (-not (Test-Path $dll)) { continue }   # the caller reports a missing binary in its own words
    $built = (Get-Item $dll).LastWriteTime
    $newer = @($sources | Where-Object { $_.LastWriteTime -gt $built } | Sort-Object LastWriteTime -Descending)
    if ($newer.Count) {
      Write-Fail ("$([IO.Path]::GetFileName($dll)) was built at $($built.ToString('HH:mm:ss')), but " +
                  "$($newer[0].FullName) is newer ($($newer[0].LastWriteTime.ToString('HH:mm:ss')))")
      Write-Host '   rebuild first:  .\build.ps1 -Solution Frontend' -ForegroundColor Yellow
      exit 2
    }
  }
}

$frontendDll = Join-Path $repoRoot `
    "MediaPortal\Source\Frontend\MediaPortal.Frontend\bin\$Configuration\net10.0\MediaPortal.Frontend.dll"
if (-not (Test-Path $frontendDll)) {
  Write-Fail "no front end at $frontendDll"
  Write-Host "   build it first:  .\build.ps1 -Solution Frontend -Configuration $Configuration" -ForegroundColor Yellow
  exit 2
}

# Defined up here as well so that the freshness check below can look at it; the render step uses the same path.
$probeDll = Join-Path $repoRoot `
    "MediaPortal\Tests\Frontend.RenderProbe\bin\$Configuration\net10.0\Frontend.RenderProbe.dll"

$freshDlls = @($frontendDll)
if (-not $SkipRender) { $freshDlls += $probeDll }
Assert-FrontendBuildIsCurrent -Dlls $freshDlls

if (-not $SkipDeploy) {
  & (Join-Path $PSScriptRoot 'deploy-server-layout.ps1') -Configuration $Configuration -OutputDir $LayoutDir
  if (-not $?) { throw 'layout deployment failed' }
}

$hostDll = Join-Path $LayoutDir 'MP2-Server-Headless.dll'
if (-not (Test-Path $hostDll)) { throw "no $hostDll - run deploy-server-layout.ps1 first" }

$dataDir = Join-Path ([IO.Path]::GetTempPath()) ('mp2-frontend-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

# The front end loads the last server it used from its settings file before it looks at --server, and it
# saves that file on exit. Pointed at the real location this gate would read whatever a developer last
# connected to, and would overwrite it with the throwaway address it tests; it also makes the "settings:"
# line in the output a fact about this run rather than a fact about whoever runs the gate.
$env:MP2_SETTINGS_FILE = Join-Path ([IO.Path]::GetTempPath()) ('mp2-frontend-check-settings-' + [Guid]::NewGuid().ToString('N').Substring(0, 8) + '.xml')

<#
.SYNOPSIS
  Runs "MediaPortal.Frontend --check" as its own process and returns its exit code and its whole output.

.DESCRIPTION
  RedirectStandardOutput is deliberate here (and deliberately NOT done for the server, whose log4net chatter is
  large): the front end prints a dozen short lines, and what it printed is the evidence this gate reads, so it
  has to be captured rather than scrolled past. StandardError is merged because a .NET unhandled exception
  report would otherwise vanish from the failure message.
#>
function Invoke-FrontendProcess {
  [CmdletBinding()]
  param([string] $Arguments, [string] $WorkingDirectory)

  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $dotnet
  $psi.Arguments = $Arguments
  $psi.WorkingDirectory = $WorkingDirectory
  $psi.UseShellExecute = $false
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError = $true
  $psi.CreateNoWindow = $true

  $process = [System.Diagnostics.Process]::Start($psi)
  # ReadToEnd before WaitForExit: a child that fills the 4 KB pipe while the parent waits for it to exit is
  # waiting for a parent that is waiting for it.
  $output = $process.StandardOutput.ReadToEnd() + $process.StandardError.ReadToEnd()
  $process.WaitForExit()

  try { return [pscustomobject]@{ Code = $process.ExitCode; Text = $output } }
  finally { $process.Dispose() }
}

function Invoke-FrontendCheck {
  [CmdletBinding()]
  param([string] $Url)

  return Invoke-FrontendProcess -Arguments ('"' + $frontendDll + '" --check --server "' + $Url + '"') `
      -WorkingDirectory (Split-Path $frontendDll -Parent)
}

Write-Step "starting the server (layout $LayoutDir, data $dataDir, held for $HoldSeconds seconds)"

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $dotnet
$psi.Arguments = '"' + $hostDll + '" --duration ' + $HoldSeconds + ' --data "' + $dataDir + '"'
$psi.WorkingDirectory = $LayoutDir
$psi.UseShellExecute = $false
$server = [System.Diagnostics.Process]::Start($psi)

$failures = @()
$attempt = 0
$check = $null
try {
  if (-not $server.WaitForExit(500)) {
    # 500 ms is not a wait for startup, it is a check that the process died immediately (a missing plugin,
    # a port already taken). Everything else is asked about by the front end itself.
    Write-Step "server pid $($server.Id) is up, waiting for its HTTP prefix"

    # Between "the process exists" and "http.sys hands out http://+:55555/MediaPortal/Server" there is a window
    # of a few hundred milliseconds in which a request is refused outright. The front end is right to call that
    # a failure with exit 1 - it is the behaviour that makes the gate meaningful - and this loop only retries
    # the 503 the server sends when it is up but still starting. So the waiting happens here, where "not yet"
    # is the expected answer, and the front end's first attempt is a test rather than a race.
    $healthUrl = $ServerUrl.TrimEnd('/') + '/health'
    $readyDeadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
    $listening = $false
    while ((Get-Date) -lt $readyDeadline) {
      try {
        Invoke-WebRequest -Uri $healthUrl -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop | Out-Null
        $listening = $true
        break
      } catch {
        if ($_.Exception.Response) {   # any HTTP answer counts: a 503 here means the prefix is live
          $listening = $true
          break
        }
        Start-Sleep -Milliseconds 250
      }
    }
    if (-not $listening) {
      Write-Host "  $healthUrl never answered within $ReadyTimeoutSeconds seconds - asking anyway" -ForegroundColor Yellow
    }

    $deadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
      $attempt++
      $check = Invoke-FrontendCheck -Url $ServerUrl
      if ($check.Code -eq 0) { break }
      if ($check.Code -eq 3) {
        Write-Host ("  attempt {0}: the server said it is not ready yet, asking again" -f $attempt) -ForegroundColor DarkGray
        Start-Sleep -Seconds 2
        continue
      }
      break   # 1 or 2: a real failure, or a URL this script got wrong. Repeating it changes nothing.
    }
  } else {
    $failures += "the server exited before the front end could talk to it (exit $($server.ExitCode))"
  }

  if ($check) {
    Write-Host "--- MediaPortal.Frontend --check, attempt $attempt, exit $($check.Code)"
    ($check.Text -split "`r?`n" | Where-Object { $_.Trim() }) | ForEach-Object { "    $_" } | Write-Host

    if ($check.Code -ne 0) {
      $failures += "the front end exited $($check.Code) instead of 0"
    }

    # Shape, not numbers: see the .DESCRIPTION.
    $expected = @(
      'pathBase=/MediaPortal/Server',   # the prefix arrived intact through the client and the listener
      'library:',                       # the media-library endpoint was reached and parsed
      'aspects',
      'health:',                        # the endpoint autodetection uses to confirm an announcement
      'player:',                        # the player was asked whether this machine can play, and answered -
                                        # either an engine name or one sentence about why there is none
      'settings:',                      # where the address it used came from, so this run is traceable
      'the front end read a running server.'
    )
    foreach ($needle in $expected) {
      if (([string] $check.Text).IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        $failures += "the front end did not print '$needle'"
      }
    }
  } else {
    $failures += 'the front end was never run'
  }

  # The negative half. A check that always prints something and always exits 0 would pass on a machine with no
  # server at all, which is the exact situation the gate is supposed to catch. Port 1 is not bound by anything.
  Write-Step 'and now against a port nobody is listening on, which has to be reported rather than hidden'
  $dead = Invoke-FrontendCheck -Url 'http://127.0.0.1:1/MediaPortal/Server'
  Write-Host "--- MediaPortal.Frontend --check (dead port), exit $($dead.Code)"
  ($dead.Text -split "`r?`n" | Where-Object { $_.Trim() }) | ForEach-Object { "    $_" } | Write-Host
  if ($dead.Code -eq 0) {
    $failures += 'the front end reported success against a port that cannot answer'
  }
  if (([string] $dead.Text).IndexOf('FAILED', [StringComparison]::Ordinal) -lt 0) {
    $failures += 'the front end failed but did not say so on its output'
  }

  # The screen itself, rendered while the server is still answering. The probe draws the real HomeWindow through
  # Skia and fails if the frame is flat; the PNG it leaves behind is for a person, the exit code is for here.
  if (-not $SkipRender) {
    if (-not (Test-Path $probeDll)) {
      $failures += "no render probe at $probeDll - build it with .\build.ps1 -Solution Frontend"
    } else {
      if (-not $RenderDir) { $RenderDir = Join-Path (Join-Path $repoRoot 'build\artifacts') 'frontend-render' }
      New-Item -ItemType Directory -Path $RenderDir -Force | Out-Null

      # Fourteen screens, sixteen frames. The home screen is drawn from a server that is really answering; the
      # continue frame is the same screen of a household that has been watching things, drawn from a scripted
      # server, because the strip cannot be drawn from an empty database; the playback screen is drawn from a
      # scripted player, because a frame that is supposed to mean the same
      # thing on Windows and in a container cannot depend on what a decoder happened to decode; the title
      # list is drawn from a scripted page of films, for the same reason - the gate's database is empty; the
      # details frame is that same list with the info key pressed, because a panel that only works when
      # nothing else on the screen is asking for room is not a panel; the search frame is that same list with
      # words typed into its box and Enter pressed, which is the only way to catch a search that works in a
      # test and not on the screen; the seen frame is that list after the watched key was pressed twice, so
      # the filter and the mark in front of a watched row are both in the picture; and the connect screen is
      # drawn with nothing found yet, which is the first
      # thing a viewer who has never pointed the front end at a server sees.
      $frames = @(
        @{ Name = 'home-windows'; Arguments = '--server "' + $ServerUrl + '"' },
        @{ Name = 'continue-windows'; Arguments = '--screen continue' },
        @{ Name = 'browser-windows'; Arguments = '--screen browser' },
        @{ Name = 'details-windows'; Arguments = '--screen details' },
        @{ Name = 'search-windows'; Arguments = '--screen search' },
        @{ Name = 'seen-windows'; Arguments = '--screen seen' },
        @{ Name = 'nowplaying-windows'; Arguments = '--screen nowplaying' },
        @{ Name = 'connect-windows'; Arguments = '--screen connect' },
        # The pictures screen is drawn from a scripted server too, because whose photographs the gate's machine
        # happens to hold is not something a frame may depend on - and because the question this screen has is
        # whether a preview's bytes became pixels inside the right tile, which only a known picture can answer.
        @{ Name = 'pictures-windows'; Arguments = '--screen pictures' },
        # The browse screen is drawn from a scripted count too, for the same emptiness-of-database reason: the
        # frame that matters is the third column full of groups, biggest first, with the empty value the database
        # answers with taken out - the order and the hole are the screen's two promises about somebody else's
        # numbers, and only a known answer shows whether it kept them.
        @{ Name = 'browse-windows'; Arguments = '--screen browse' },
        # The news screen is drawn from a scripted internet - feed list, RSS, Atom and one refusal - because a
        # frame must mean the same thing on every machine, and the state worth picturing is the honest ugly one:
        # three feeds asked, two answered, one came back with a 500 and says so in its own row.
        @{ Name = 'news-windows'; Arguments = '--screen news' },
        # The weather screen is drawn from a scripted OpenWeatherMap - one fixed sky and four three-hourly
        # entries spanning two local days - because a live forecast would mean a different picture every
        # afternoon, and the claim in this frame is the aggregation: a day takes its midday's words and its own
        # extremes, and the city's midnight divides its days, not the television's.
        @{ Name = 'weather-windows'; Arguments = '--screen weather' },
        # The party screen is drawn from the scripted music shelf - eight songs with artists and track numbers -
        # and from a fixed shuffle seed, because a frame of a shuffle whose order changes every afternoon means
        # nothing; the claim in the frame is the queue and the song it says is playing, and both have to be the
        # same claims on every machine that draws them.
        @{ Name = 'party-windows'; Arguments = '--screen party' },
        # The playback screen in the colours of the two skins MP2 shipped, because "the skins were rescued" is a
        # claim about pixels and because that screen carries most of what a palette says: the background, the panel
        # behind every transport button, the text on it, the outlines and the focus ring. The probe checks the frame
        # against the palette itself - the corner has to be the palette's background colour and that colour has to be
        # what the frame stands on - so a palette that was applied to the application but never asked for by a screen
        # fails here rather than looking fine. The home screen is deliberately not one of these frames: it draws one
        # colour of its own and leaves the rest to the theme, so its two frames would say nothing the first one
        # did not already say.
        @{ Name = 'nowplaying-nereus'; Arguments = '--screen nowplaying --theme nereus' },
        @{ Name = 'nowplaying-wmc-dark'; Arguments = '--screen nowplaying --theme wmc-dark' },
        # The settings screen, wearing Nereus, is the frame that says the palettes are not a table in a binary
        # but pixels on a television: every row paints its own accent as a swatch, and the probe counts the
        # accents by name - eleven palettes, eleven accent colours looked for by hex, each needing a painted
        # row. It is drawn with a theme other than its rows' colours on purpose: what a viewer picks on this
        # screen must survive being looked at through somebody else's colours.
        @{ Name = 'settings-nereus'; Arguments = '--screen settings --theme nereus' }
      )
      foreach ($frame in $frames) {
        $renderFile = Join-Path $RenderDir ($frame.Name + '.png')
        Write-Step "rendering $($frame.Name) with Skia to $renderFile"
        $render = Invoke-FrontendProcess `
            -Arguments ('"' + $probeDll + '" --out "' + $renderFile + '" ' + $frame.Arguments) `
            -WorkingDirectory (Split-Path $probeDll -Parent)
        Write-Host "--- Frontend.RenderProbe $($frame.Name), exit $($render.Code)"
        ($render.Text -split "`r?`n" | Where-Object { $_.Trim() }) | ForEach-Object { "    $_" } | Write-Host
        if ($render.Code -ne 0) {
          $failures += "the render probe for $($frame.Name) exited $($render.Code) - see its output above"
        }
      }
    }
  }
} finally {
  if (-not $server.HasExited) {
    Write-Step 'stopping the server'
    $server.Kill()
    $server.WaitForExit(15000) | Out-Null
  }
  $server.Dispose()
  Remove-Item $dataDir -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count) {
  foreach ($failure in $failures) { Write-Fail $failure }
  Write-Host 'FRONTEND CHECK FAILED' -ForegroundColor Red
  exit 1
}

Write-Host "FRONTEND CHECK OK - the front end read a running server (attempt $attempt) and refused a dead one." -ForegroundColor Green
exit 0

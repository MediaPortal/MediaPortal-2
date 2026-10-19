<#
.SYNOPSIS
  Plays a real media file - one that a person actually watches - through the player seam and prints what the
  engine did with it.
.DESCRIPTION
  Everything else in the front-end test suite plays files the tests generate themselves, because a repository
  cannot hold a feature-length film. That covers the state machine and the clock and cannot cover the half that
  matters most to a viewer: a real codec, a container whose length is in hours, several audio and subtitle
  tracks, and seeking across keyframes.

  This script closes that gap with somebody's own file:

    1. the file's own facts are read with ffprobe when ffprobe is installed - duration, and how many audio,
       subtitle and video streams the container holds. Those become the expectations, so the player is checked
       against an instrument that is not the player;
    2. Tests.Frontend is built with -p:RealMediaTests=true, which is the only way the RealMedia fixture is
      compiled at all (a test that skips when its input is missing would keep the suite green while never
      decoding a frame, which is why the fixture is excluded by default rather than skipped);
    3. the fixture is run, and the script insists that tests actually ran - "0 tests, exit 0" is a failure here,
       because a filter that matches nothing looks exactly like a pass;
    4. what the engine counted is printed: the length it reports next to the container's, the frames it decoded,
       the tracks it listed, and where a seek to a third of the way in actually landed.

  Nothing is written outside the build output and nothing is configured. The file path is only ever passed to
  the test process as MP2_TEST_MEDIA; it is not stored anywhere by this script.

  Pass -Show to play with a real video output instead of libVLC's dummy one - the picture appears in a window,
  which is the only way to answer "does it look right", a question no assertion can settle.

  Pass -Stream and the same file is played a second way: the server's own API is started in the test process with
  the file registered as a title reached through MP2's local filesystem resource provider, and the player is
  handed the address the front end would hand it - api/media/items/{id}/stream. That is the only configuration in
  which the streaming endpoint is measured by something that has to seek: a Matroska keeps its index after the
  clusters, so a server whose range answers are wrong plays sound with no picture. The file fixture and the
  stream fixture are then run against one set of numbers from ffprobe, so the two are comparable line for line.
.PARAMETER File
  The media file to play. Defaults to MP2_TEST_MEDIA. Required either way; a missing file is an error, not a skip.
.PARAMETER Seconds
  How long to watch while counting decoded frames. Default 6.
.PARAMETER Show
  Play with a real video output rather than the dummy one. Needs a display.
.PARAMETER Stream
  Also play the file over the server's HTTP stream endpoint, in the same run.
.PARAMETER DurationMs
  The container's duration in ms, for hosts without ffprobe. Without this and without ffprobe the length is
  printed but not compared.
.PARAMETER Configuration
  Debug or Release.
.EXAMPLE
  .\test-player-file.ps1 -File 'C:\Users\you\Videos\Alien Earth (2025)\Season 1\Alien Earth - S01E01 - Nimmerland.mkv'
.EXAMPLE
  .\test-player-file.ps1 -File D:\media\tear_of_the_gods.mkv -Show -Seconds 20
.EXAMPLE
  .\test-player-file.ps1 -File D:\media\tear_of_the_gods.mkv -Stream
  Plays it from disk and again from api/media/items/{id}/stream, and prints both sets of figures.
#>
[CmdletBinding()]
param(
  [string]$File,

  [int]$Seconds = 6,

  [switch]$Show,

  [switch]$Stream,

  [long]$DurationMs = 0,

  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  [switch]$SkipBuild,

  [string]$DotnetPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot (Join-Path '..' '..'))).Path
$testProject = Join-Path (Join-Path (Join-Path $repoRoot 'MediaPortal') 'Tests') 'Tests.Frontend'
$testProjectFile = Join-Path $testProject 'Tests.Frontend.csproj'

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Fail([string]$message) { Write-Host "FAIL: $message" -ForegroundColor Red }
function Write-Fact([string]$message) { Write-Host "    $message" }

if (-not $File) { $File = $env:MP2_TEST_MEDIA }
if (-not $File) {
  Write-Fail 'no media file was given'
  Write-Host '    pass -File <path to a video file>, or set MP2_TEST_MEDIA'
  Write-Host '    this gate exists to play a file that a person has; without one there is nothing to measure'
  exit 2
}

$resolved = $null
try { $resolved = (Resolve-Path -LiteralPath $File).Path } catch { }
if (-not $resolved -or -not (Test-Path -LiteralPath $resolved)) {
  Write-Fail "there is no file at $File"
  exit 2
}

if (-not (Test-Path $testProjectFile)) { throw "no test project at $testProjectFile" }

$dotnet = $DotnetPath
if (-not $dotnet) {
  $command = Get-Command dotnet -ErrorAction SilentlyContinue
  if ($command) { $dotnet = $command.Source } else { throw 'no dotnet on PATH; pass -DotnetPath' }
}

Write-Host ''
Write-Host '======================================================================' -ForegroundColor Cyan
Write-Host ' THE PLAYER IN FRONT OF A REAL FILE' -ForegroundColor Cyan
Write-Host '======================================================================' -ForegroundColor Cyan
Write-Fact "file:  $resolved"
Write-Fact ("size:  {0:N1} MB" -f ((Get-Item -LiteralPath $resolved).Length / 1MB))
if ($Stream) {
  Write-Fact 'stream:  the same file is also played through the server''s HTTP stream endpoint'
}

# ---- what the container itself says -------------------------------------------------------------------

$ffprobe = Get-Command ffprobe -ErrorAction SilentlyContinue
$expectLengthMs = $DurationMs
$audioTracks = ''
$subtitleTracks = ''
$hasVideo = ''
$displaySize = ''

if ($ffprobe) {
  Write-Step 'reading the container with ffprobe'
  $probe = $null
  try {
    $json = & $ffprobe.Source -v error -print_format json -show_format -show_streams -- $resolved 2>$null
    $probe = ($json -join "`n") | ConvertFrom-Json
  } catch {
    Write-Host '    ffprobe failed, so the player is measured against nothing but its own output'
  }

  if ($probe) {
    # $mediaStream rather than $stream: PowerShell variable names are not case sensitive, so a loop variable
    # called $stream is this script's -Stream switch, and assigning a stream to a [switch] fails.
    foreach ($mediaStream in $probe.streams) {
      $extra = ''
      if ($mediaStream.codec_type -eq 'video') {
        $extra = " $($mediaStream.width)x$($mediaStream.height) $($mediaStream.r_frame_rate) fps"
        # ffprobe's width/height are the size the film is *shown* at; a decoder works in blocks and hands over
        # something taller. Only the container's number can tell a stretched picture from a correct one.
        $displaySize = "$($mediaStream.width)x$($mediaStream.height)"
      }
      Write-Fact ("{0,-9} {1,-12} {2}{3}" -f $mediaStream.codec_type, $mediaStream.codec_name,
                  ($mediaStream.tags.language), $extra)
    }

    if ($probe.format.duration) {
      $containerSeconds = [double]$probe.format.duration
      if ($expectLengthMs -le 0) {
        $expectLengthMs = [long][Math]::Round($containerSeconds * 1000)
      }
      Write-Fact ("duration  {0} ({1:N3} s)" -f ([TimeSpan]::FromSeconds($containerSeconds)).ToString('h\:mm\:ss'),
                  $containerSeconds)
    }
    $audioTracks = @(($probe.streams | Where-Object { $_.codec_type -eq 'audio' })).Count
    $subtitleTracks = @(($probe.streams | Where-Object { $_.codec_type -eq 'subtitle' })).Count
    $hasVideo = @(($probe.streams | Where-Object { $_.codec_type -eq 'video' })).Count
    Write-Fact "streams   $hasVideo video, $audioTracks audio, $subtitleTracks subtitle"
    if ($displaySize -ne '') {
      Write-Fact "picture   $displaySize is what the film is shown as; a decoder hands over a block-rounded buffer " +
                 'taller than this, and the player has to cut it back'
    }
    Write-Host '    the player has to agree with these numbers, and it does not produce them' -ForegroundColor DarkGray
  }
} else {
  Write-Host '==> ffprobe is not installed, so the container is not read independently' -ForegroundColor Yellow
  if ($expectLengthMs -gt 0) {
    Write-Fact "duration given on the command line: $expectLengthMs ms"
  }
}

# ---- run the tests -----------------------------------------------------------------------------------

if (-not $SkipBuild) {
  Write-Step "building the front-end tests with the real-file fixture compiled in ($Configuration)"
  & $dotnet build $testProjectFile --nologo -v q -c $Configuration -p:RealMediaTests=true
  if ($LASTEXITCODE -ne 0) { throw 'the test build failed' }
}

Write-Step "playing it ($([int]$Seconds)s of watching, $(if ($Show) { 'real video output' } else { 'headless' }))"

$env:MP2_TEST_MEDIA = $resolved
$env:MP2_TEST_MEDIA_SECONDS = "$Seconds"
$env:MP2_TEST_MEDIA_SHOW = $(if ($Show) { '1' } else { '0' })
if ($expectLengthMs -gt 0) { $env:MP2_TEST_MEDIA_LENGTH_MS = "$expectLengthMs" } else {
  Remove-Item Env:\MP2_TEST_MEDIA_LENGTH_MS -ErrorAction SilentlyContinue
}
if ($audioTracks -ne '') { $env:MP2_TEST_MEDIA_AUDIO_TRACKS = "$audioTracks" }
if ($subtitleTracks -ne '') { $env:MP2_TEST_MEDIA_SUBTITLE_TRACKS = "$subtitleTracks" }
if ($hasVideo -ne '') { $env:MP2_TEST_MEDIA_HAS_VIDEO = $(if ([int]$hasVideo -gt 0) { '1' } else { '0' }) }
if ($displaySize -ne '') { $env:MP2_TEST_MEDIA_VIDEO = $displaySize } else {
  Remove-Item Env:\MP2_TEST_MEDIA_VIDEO -ErrorAction SilentlyContinue
}

# The summary line this script reads is translated ("Bestanden: 8" on a German dotnet), and a script that cannot
# count its own tests is worse than no script - it fails a passing run, and would pass a failing one just as
# happily. The CLI is asked for English, and the counts are then read from a line whose wording is known.
$previousLanguage = $env:DOTNET_CLI_UI_LANGUAGE
$env:DOTNET_CLI_UI_LANGUAGE = 'en'

# With -Stream both fixtures run: the file one is the baseline that says the film is playable at all, and the
# stream one is the thing being proved. Reading them in one run means one set of ffprobe numbers for both.
$filter = 'FullyQualifiedName~TestRealMediaFile'
if ($Stream) { $filter = 'FullyQualifiedName~TestRealMedia' }

$log = Join-Path ([IO.Path]::GetTempPath()) ("mp2-player-file-" + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$output = & $dotnet test $testProjectFile --nologo -c $Configuration -p:RealMediaTests=true `
    --filter $filter --logger "console;verbosity=normal" 2>&1
$testExit = $LASTEXITCODE
$output | ForEach-Object { "$_" } | Out-File -FilePath $log -Encoding utf8

if ($previousLanguage) { $env:DOTNET_CLI_UI_LANGUAGE = $previousLanguage } else {
  Remove-Item Env:\DOTNET_CLI_UI_LANGUAGE -ErrorAction SilentlyContinue
}

# VSTest's summary block, in the English the run was asked for above:
#   Test Run Successful.
#   Total tests: 8
#        Passed: 8
# Any of those numbers being unreadable has to be noticed here rather than assumed, because the shape of a run
# that played nothing is identical to the shape of a run that played everything.
$summary = @($output | Where-Object {
      "$_" -match 'Total tests:|Passed:|Failed:|Skipped:|No test matches|test\(s\) passed|Test Run' })
$total = 0
$passed = 0
$failedCount = 0
$skipped = 0
foreach ($line in $summary) {
  if ("$line" -match 'Total tests:\s*(\d+)') { $total = [int]$Matches[1] }
  if ("$line" -match 'Passed:\s*(\d+)') { $passed = [int]$Matches[1] }
  if ("$line" -match 'Failed:\s*(\d+)') { $failedCount = [int]$Matches[1] }
  if ("$line" -match 'Skipped:\s*(\d+)') { $skipped = [int]$Matches[1] }
}
$hardware = @($output | ForEach-Object { if ("$_" -match 'decoder: (Using .*)$') { $Matches[1] } } |
    Select-Object -Unique)

Write-Host ''
Write-Host '---- what the engine reported ------------------------------------------------------------------' -ForegroundColor Cyan
# Each of these is printed by whichever test happens to print it, and one line per test - the engine names
# itself eight times, which is not information.
$seenLabels = @{}
$evidence = @($output | Where-Object {
      "$_" -match '  media:|  engine:|  length:|  decoded:|  counters:|  audio:|  subtitle:|  seek:|  expected:|  pictures:|  stream:|  bytes:|  range:' } |
    ForEach-Object {
      $text = "$_".Trim()
      if ($text -match '^(engine|media|expected):') {
        $label = $text.Split(':')[0]
        if ($seenLabels.ContainsKey($label)) { return }
        $seenLabels[$label] = $true
      }
      $text
    })
if ($evidence.Count -gt 0) { $evidence | ForEach-Object { Write-Host "    $_" } } else {
  Write-Host '    (the run printed none of the lines the fixture writes - see the log)' -ForegroundColor Yellow
}
foreach ($line in $hardware) {
  Write-Host ("    hardware  {0}" -f ("$line".Trim())) -ForegroundColor DarkGray
}
Write-Host ("    tests     {0} passed, {1} failed, {2} skipped" -f $passed, $failedCount, $skipped)

$failed = @($output | Where-Object { "$_" -match '^\s+(Failed|Error)\s+\w' } | Select-Object -First 12)
if ($failed.Count -gt 0) {
  Write-Host ''
  Write-Host '---- what it could not do ----------------------------------------------------------------------' -ForegroundColor Red
  $failed | ForEach-Object { Write-Host "    $($_.Trim())" }
}

Write-Host ''
Write-Host "log: $log"

if ($output -match 'No test matches') {
  Write-Fail 'no test matched the filter, so nothing was played'
  Write-Host "    the real-file fixture is compiled with -p:RealMediaTests=true; if that flag is missing the"
  Write-Host "    suite runs and passes without ever opening a file, which is the outcome to be most afraid of"
  exit 1
}
if ($total -eq 0 -or $passed -eq 0) {
  Write-Fail "no test ran (total: $total, passed: $passed)"
  Write-Host "    either the fixture was not compiled in or the filter does not name it; either way nothing"
  Write-Host '    was played, which is the one outcome this script must never report as success'
  exit 1
}
if ($skipped -gt 0) {
  Write-Fail "$skipped test(s) were skipped - a skipped real-file test has not looked at a real file"
  exit 1
}
if ($failedCount -gt 0 -or $testExit -ne 0) {
  Write-Fail "$failedCount test(s) failed, exit code $testExit"
  exit 1
}

if ($Stream) {
  Write-Host "==> $passed real playback test(s) passed, from disk and over the server's stream endpoint" -ForegroundColor Green
} else {
  Write-Host "==> $passed real-file test(s) passed against $([IO.Path]::GetFileName($resolved))" -ForegroundColor Green
}
if (-not $Show) {
  Write-Host '    run again with -Show to watch the same file: whether the picture looks right is a question' -ForegroundColor DarkGray
  Write-Host '    for a person, not for an assertion' -ForegroundColor DarkGray
}
exit 0

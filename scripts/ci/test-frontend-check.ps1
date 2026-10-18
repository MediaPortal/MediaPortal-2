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
       cannot fail is not a check.

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

  [string]$DotnetPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $LayoutDir) { $LayoutDir = Join-Path (Join-Path $repoRoot 'build\artifacts') 'server-layout' }

$dotnet = if ($DotnetPath) { $DotnetPath } else { (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet) { throw 'dotnet not found on PATH. Install the .NET SDK, or pass -DotnetPath.' }

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Fail([string]$message) { Write-Host "FAIL: $message" -ForegroundColor Red }

$frontendDll = Join-Path $repoRoot `
    "MediaPortal\Source\Frontend\MediaPortal.Frontend\bin\$Configuration\net10.0\MediaPortal.Frontend.dll"
if (-not (Test-Path $frontendDll)) {
  Write-Fail "no front end at $frontendDll"
  Write-Host "   build it first:  .\build.ps1 -Solution Frontend -Configuration $Configuration" -ForegroundColor Yellow
  exit 2
}

if (-not $SkipDeploy) {
  & (Join-Path $PSScriptRoot 'deploy-server-layout.ps1') -Configuration $Configuration -OutputDir $LayoutDir
  if (-not $?) { throw 'layout deployment failed' }
}

$hostDll = Join-Path $LayoutDir 'MP2-Server-Headless.dll'
if (-not (Test-Path $hostDll)) { throw "no $hostDll - run deploy-server-layout.ps1 first" }

$dataDir = Join-Path ([IO.Path]::GetTempPath()) ('mp2-frontend-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

<#
.SYNOPSIS
  Runs "MediaPortal.Frontend --check" as its own process and returns its exit code and its whole output.

.DESCRIPTION
  RedirectStandardOutput is deliberate here (and deliberately NOT done for the server, whose log4net chatter is
  large): the front end prints a dozen short lines, and what it printed is the evidence this gate reads, so it
  has to be captured rather than scrolled past. StandardError is merged because a .NET unhandled exception
  report would otherwise vanish from the failure message.
#>
function Invoke-FrontendCheck {
  [CmdletBinding()]
  param([string] $Url)

  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $dotnet
  $psi.Arguments = '"' + $frontendDll + '" --check --server "' + $Url + '"'
  $psi.WorkingDirectory = Split-Path $frontendDll -Parent
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
    Write-Step "server pid $($server.Id) is up, asking it through the front end"

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

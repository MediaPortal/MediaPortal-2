<#
.SYNOPSIS
  Starts the MP2 server and proves that the front end finds it on its own, without being told an address.
.DESCRIPTION
  The "which server?" screen lists the servers it finds, so the front end has to be able to answer two questions
  about a machine it has never heard of: is something there, and is it a MediaPortal server. This script answers
  both against a real server:

    1. the server is deployed and started with --duration, and the script waits for the log line that says its
       UPnP listeners are up (a search before that lands on a half-built device tree);
    2. the front end is run with --discover, which is the same search the screen performs when it opens;
    3. the run has to exit 0 and print a "server:" line naming this machine's server - the front end found the
       announcement, built the API address from it, and confirmed it by asking, so the address in that line is one
       that has just answered;
    4. the server has to shut down by itself with exit code 0 and without an [ERROR] log line.

  Nothing is saved by this script and nothing is configured: --discover neither reads nor writes the settings file,
  so what it prints is what a front end on a machine that has never been set up would show.

  A machine where multicast does not work still passes, because the front end also asks 127.0.0.1:1900 directly -
  which is also why this runs on a build agent. That the search reaches a server on another box is the same code
  path with one more hop, and is covered by test-server-upnp.ps1 on the announcement side.
.PARAMETER LayoutDir
  Deployment layout from deploy-server-layout.ps1. Defaults to build\artifacts\server-layout.
.PARAMETER DataDir
  Scratch directory for the database, configuration and log. Defaults to a new directory in TEMP.
.PARAMETER DurationSeconds
  How long the server stays up. Has to outlast the discovery window plus the time the front end needs to ask the
  servers it found.
.PARAMETER DiscoverSeconds
  How long the front end listens for announcements.
.EXAMPLE
  .\test-frontend-discovery.ps1
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  [string]$LayoutDir,

  [string]$DataDir,

  [int]$DurationSeconds = 60,

  [int]$DiscoverSeconds = 6,

  [switch]$SkipDeploy,

  [switch]$SkipFrontendBuild,

  [string]$DotnetPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot (Join-Path '..' '..'))).Path
if (-not $LayoutDir) {
  $LayoutDir = Join-Path (Join-Path (Join-Path $repoRoot 'build') 'artifacts') 'server-layout'
}

$dotnet = if ($DotnetPath) { $DotnetPath } else { (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet) { throw 'dotnet not found on PATH. Install the .NET SDK, or pass -DotnetPath.' }

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }
function Write-Fail([string]$message) { Write-Host "FAIL: $message" -ForegroundColor Red }

if (-not $SkipDeploy) {
  & (Join-Path $PSScriptRoot 'deploy-server-layout.ps1') -Configuration $Configuration -OutputDir $LayoutDir
  if (-not $?) { throw 'layout deployment failed' }
}

$hostDll = [IO.Path]::Combine($LayoutDir, 'MP2-Server-Headless.dll')
if (-not (Test-Path $hostDll)) { throw "no $hostDll - run deploy-server-layout.ps1 first" }

# The front end is not part of the server layout - it is a separate program that talks to the server over the
# network - so it is built where it lives and run from there.
$frontendProject = Join-Path (Join-Path $repoRoot 'MediaPortal\Source\Frontend\MediaPortal.Frontend') 'MediaPortal.Frontend.csproj'
$frontendDll = Join-Path (Join-Path (Join-Path (Join-Path $repoRoot 'MediaPortal\Source\Frontend\MediaPortal.Frontend') 'bin') $Configuration) 'net10.0\MediaPortal.Frontend.dll'

if (-not $SkipFrontendBuild) {
  Write-Step "building the front end ($Configuration)"
  & $dotnet build $frontendProject -c $Configuration -v q --nologo
  if (-not $?) { throw 'the front end did not build' }
}
if (-not (Test-Path $frontendDll)) { throw "no $frontendDll - build the front end first" }

if (-not $DataDir) {
  $DataDir = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'mp2-frontend-discovery-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
if (-not (Test-Path $DataDir)) {
  New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
}

$logFile = [IO.Path]::Combine($DataDir, 'Log', 'Server.log')
if (Test-Path $logFile) { Remove-Item $logFile -Force }

Write-Step "starting the server for $DurationSeconds seconds (layout $LayoutDir, data $DataDir)"

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $dotnet
$psi.Arguments = '"' + $hostDll + '" --duration ' + $DurationSeconds + ' --data "' + $DataDir + '"'
$psi.WorkingDirectory = $LayoutDir
$psi.UseShellExecute = $false
$server = [System.Diagnostics.Process]::Start($psi)

function Stop-Server {
  if (-not $server.HasExited) {
    $server.Kill()
    $server.WaitForExit()
  }
}

$failures = @()

# The front end reads its address from a file, and the discovery run must not be able to succeed because of what a
# previous run left there. MP2_SETTINGS_FILE points it at a file in this run's own data directory, which does not
# exist - so everything the run prints came out of the network.
$env:MP2_SETTINGS_FILE = [IO.Path]::Combine($DataDir, 'no-such-settings-file.json')

Write-Step 'waiting for the server to bring its UPnP listeners up'
$ready = $false
$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
  if ((Test-Path $logFile) -and ((Get-Content $logFile -Raw -ErrorAction SilentlyContinue) -match 'UPnP server online hosting')) {
    $ready = $true
    break
  }
  if ($server.HasExited) { break }
  Start-Sleep -Milliseconds 200
}
if (-not $ready) {
  Write-Fail "the server never brought its UPnP server up (see $logFile)"
  $failures += 'the UPnP server did not start'
  Stop-Server
}

if ($failures.Count -eq 0) {
  Write-Step "asking the network as the front end does (--discover $DiscoverSeconds)"
  $probePsi = New-Object System.Diagnostics.ProcessStartInfo
  $probePsi.FileName = $dotnet
  $probePsi.Arguments = '"' + $frontendDll + '" --discover ' + $DiscoverSeconds
  $probePsi.WorkingDirectory = $repoRoot
  $probePsi.UseShellExecute = $false
  $probePsi.RedirectStandardOutput = $true
  $probePsi.RedirectStandardError = $true
  $probe = [System.Diagnostics.Process]::Start($probePsi)
  # Both streams are drained at the same time. Reading them one after the other deadlocks as soon as the child
  # writes more than a pipe buffer to the one nobody is reading - and a run that hangs is worse than a run that
  # fails, because it looks like a slow network.
  $errorTask = $probe.StandardError.ReadToEndAsync()
  $stdout = $probe.StandardOutput.ReadToEnd()
  $stderr = $errorTask.Result
  if (-not $probe.WaitForExit(120000)) {
    $probe.Kill()
    $failures += 'the discovery run did not finish within two minutes'
  }
  $discoveryCode = if ($probe.HasExited) { $probe.ExitCode } else { -1 }

  ($stdout -split "`r?`n" | Where-Object { $_.Trim() }) | ForEach-Object { "    $_" } | Write-Host
  if ($stderr.Trim().Length -gt 0) {
    Write-Host '  stderr:' -ForegroundColor Yellow
    ($stderr -split "`r?`n" | Where-Object { $_.Trim() }) | ForEach-Object { "    $_" } | Write-Host
  }

  # "exit 0" alone would not be enough: the line has to name the server that is standing in the other process. An
  # announcement from some other UPnP device on the network would be counted by the exit code and is filtered here.
  $serverLines = @($stdout -split "`r?`n" | Where-Object { $_ -match '^server:\s+.*http://[^/]+:\d+/MediaPortal/Server/?' })
  if ($discoveryCode -ne 0) {
    Write-Fail "the discovery run exited with $discoveryCode"
    $failures += "the discovery run exited with $discoveryCode"
  }
  if ($serverLines.Count -eq 0) {
    Write-Fail 'the front end did not report a MediaPortal server it had found'
    $failures += 'no server found by the front end'
  } else {
    Write-Host "==> the front end found $($serverLines.Count) server(s) it had never been told about"
    if ($stdout -notmatch 'MediaPortal 2 Server') {
      Write-Fail 'the found server did not say who it was, so the address was not confirmed by the server itself'
      $failures += 'the found server never identified itself'
    }
  }

  Write-Step "waiting for the server to stop (it stops after $DurationSeconds seconds)"
  $exitDeadline = [Math]::Max(10000, ($DurationSeconds * 1000) + 20000)
  if (-not $server.WaitForExit([int] $exitDeadline)) {
    Write-Fail 'the server was still running 20 seconds after its duration elapsed'
    $failures += 'the server did not shut down'
    Stop-Server
  }
  if ($server.HasExited -and $server.ExitCode -ne 0) {
    Write-Fail "the server exited with $($server.ExitCode)"
    $failures += "the server exited with $($server.ExitCode)"
  }
}

if (Test-Path $logFile) {
  $log = Get-Content $logFile -Raw
  $errorLines = @($log -split "`n" | Where-Object { $_ -match '\[ERROR\]' })
  if ($errorLines.Count -gt 0) {
    Write-Fail "$($errorLines.Count) error log line(s)"
    $errorLines | Select-Object -First 5 | ForEach-Object { Write-Host "    $($_.Trim())" }
    $failures += "$($errorLines.Count) error log line(s)"
  }
} else {
  $failures += "no log file at $logFile"
}

Remove-Item Env:\MP2_SETTINGS_FILE -ErrorAction SilentlyContinue

Write-Host ''
Write-Host "log:    $logFile"
Write-Host "output: $frontendDll --discover $DiscoverSeconds"
if ($failures.Count -gt 0) {
  Write-Fail "$($failures.Count) check(s) failed"
  $failures | ForEach-Object { Write-Host "  - $_" }
  Write-Host "data directory kept for inspection: $DataDir"
  exit 1
}

Write-Host '==> the front end found the server by itself and confirmed it by asking' -ForegroundColor Green

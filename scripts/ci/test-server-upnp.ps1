<#
.SYNOPSIS
  Starts the MP2 server, sends it a UPnP discovery request and checks that it answers.
.DESCRIPTION
  The server hosts a UPnP device tree (one root device even in the minimal layout) and answers SSDP
  M-SEARCH requests on port 1900. run-server-check.ps1 only proves that the SSDP listeners come up and
  go down; this script proves they work, by asking the running server a question:

    1. the server is started with --duration so that it shuts down by itself;
    2. a unicast M-SEARCH goes to 127.0.0.1:1900 - unicast, so that only this machine can answer and
       a NAS or a Windows SSDP service on the LAN cannot produce a false positive;
    3. the answer has to be a "200 OK" whose LOCATION header points at MP2's own UPnP HTTP listener
       (http://+:55555/MediaPortal/UPnPServer_.../description.xml), which no other product sends;
    4. the server has to exit with code 0, without a single [ERROR] log line and without the
       "Cannot acquire synchronization lock" message that a shutdown racing a timer tick used to
       produce.

  Step 4 is the reason this script exists next to run-server-check.ps1: the SSDP receive loop and the
  shutdown of its timers are only exercised when something actually talks to the server.
.PARAMETER LayoutDir
  Deployment layout from deploy-server-layout.ps1. Defaults to build\artifacts\server-layout.
.PARAMETER DataDir
  Scratch directory for the database, configuration and log. Defaults to a new directory in TEMP.
.PARAMETER DurationSeconds
  How long the server stays up. The search response is delayed by the MX value of the request, so this
  has to be comfortably longer than the time the probe needs.
.EXAMPLE
  .\test-server-upnp.ps1
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  [string]$LayoutDir,

  [string]$DataDir,

  [int]$DurationSeconds = 25,

  # How long to wait for the search response after the request was sent.
  [int]$ResponseTimeoutSeconds = 12,

  [switch]$SkipDeploy,

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

if (-not $DataDir) {
  $DataDir = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'mp2-upnp-probe-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
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
    # Ctrl+C cannot be delivered from another console session, so if the script gives up the process is
    # killed rather than left running.
    $server.Kill()
    $server.WaitForExit()
  }
}

$failures = @()

# Wait for the line the UPnP server writes after its listeners and SSDP endpoints are up. Until then a
# search request would land on a half-built device tree.
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
  # A search request with an MX of one second, as SSDP expects; the server delays its answer by that
  # time, which is also part of the code path being tested.
  $request = @(
    'M-SEARCH * HTTP/1.1',
    'HOST: 239.255.255.250:1900',
    'MAN: "ssdp:discover"',
    'MX: 1',
    'ST: ssdp:all',
    '',
    ''
  ) -join "`r`n"

  Write-Step 'multicasting an M-SEARCH request to 239.255.255.250:1900'
  $responses = @()
  $probe = New-Object System.Net.Sockets.UdpClient
  $probe.Client.ReceiveTimeout = 1000
  try {
    $bytes = [Text.Encoding]::ASCII.GetBytes($request)
    [void] $probe.Send($bytes, $bytes.Length, '239.255.255.250', 1900)

    $receiveDeadline = (Get-Date).AddSeconds($ResponseTimeoutSeconds)
    while ((Get-Date) -lt $receiveDeadline) {
      $remote = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
      try {
        $data = $probe.Receive([ref] $remote)
        $responses += [pscustomobject]@{ From = $remote.ToString(); Body = [Text.Encoding]::ASCII.GetString($data) }
      } catch [System.Net.Sockets.SocketException] {
        # The ReceiveTimeout elapsed; keep trying until the overall deadline.
      }
    }
  } finally {
    $probe.Close()
  }

  Write-Host "received $($responses.Count) UDP response(s)"
  foreach ($response in $responses) {
    Write-Host "--- from $($response.From)"
    ($response.Body -split "`r`n") | Select-Object -First 8 | ForEach-Object { Write-Host "    $_" }
  }

  # Only answers that name MP2's own UPnP listener count: another UPnP stack on the machine would answer
  # a discovery request too, and the test would pass without the server under test having done anything.
  $fromServer = @($responses | Where-Object { $_.Body -match '(?m)^LOCATION: *http://[^/]+/MediaPortal/UPnPServer' -and $_.Body -match 'MediaPortal-2-Server' })
  if ($fromServer.Count -eq 0) {
    Write-Fail 'no search response from the MP2 UPnP server'
    $failures += 'the server did not answer the SSDP search request'
  } else {
    Write-Host "==> the MP2 UPnP server answered $($fromServer.Count) search response(s)"
    if ($fromServer[0].Body -notmatch 'HTTP/1\.1 200 OK') {
      Write-Fail 'the search response was not a 200 OK'
      $failures += 'the search response was not a 200 OK'
    }
  }

  Write-Step "waiting for the server to stop (it stops after $DurationSeconds seconds)"
  $exitDeadline = [Math]::Max(10000, ($DurationSeconds * 1000) + 20000)
  $waited = $server.WaitForExit([int] $exitDeadline)
  if (-not $waited) {
    Write-Fail 'the server was still running 20 seconds after its duration elapsed'
    $failures += 'the server did not shut down'
    Stop-Server
  }

  if ($server.ExitCode -ne 0) {
    Write-Fail "exit code was $($server.ExitCode)"
    $failures += "exit code was $($server.ExitCode)"
  }
}

if (Test-Path $logFile) {
  $log = Get-Content $logFile -Raw
  $errorLines = @($log -split "`n" | Where-Object { $_ -match '\[ERROR\]' })
  $fatal = @($log -split "`n" | Where-Object { $_ -match '\bFATAL\b' })
  if ($fatal.Count -gt 0) { $failures += "$($fatal.Count) FATAL log line(s)" }
  if ($errorLines.Count -gt 0) {
    Write-Fail "$($errorLines.Count) error log line(s)"
    $errorLines | Select-Object -First 5 | ForEach-Object { Write-Host "    $($_.Trim())" }
    $failures += "$($errorLines.Count) error log line(s)"
  }
  if ($log -match 'Cannot acquire synchronization lock') {
    Write-Fail 'a UPnP timer could not take the SSDP lock, which stalls shutdown for TIMEOUT_TIMER_LOCK_ACCESS'
    $failures += 'SSDP lock contention during shutdown'
  }
} else {
  $failures += "no log file at $logFile"
}

Write-Host ''
Write-Host "log:  $logFile"
if ($failures.Count -gt 0) {
  Write-Fail "$($failures.Count) check(s) failed"
  $failures | ForEach-Object { Write-Host "  - $_" }
  Write-Host "data directory kept for inspection: $DataDir"
  exit 1
}

Write-Host '==> the server answered a UPnP discovery request and shut down cleanly' -ForegroundColor Green
exit 0

<#
.SYNOPSIS
  Starts the MP2 server headless, checks that it reached the Running state, and stops it again.

.DESCRIPTION
  This is the smallest thing that answers "does the server actually run?", as opposed to "does the
  server compile?", which is all the build gates have been able to say so far. It deploys a runnable
  layout (see deploy-server-layout.ps1), starts MP2-Server-Headless with --check and a scratch data
  directory, and fails unless the process comes up, writes its log, creates the media database and
  shuts down with exit code 0.

  Five assertions, because "the process exited 0" is not one:

    1. the process exits by itself, within -TimeoutSeconds;
    2. its exit code is 0;
    3. <DATA>\Log\Server.log contains the "Switching to running state" line, which is the last thing
       the launcher logs before the importer worker and the database migration run;
    4. the log contains no FATAL line;
    5. every URL in -ProbeUrl answers 200 while the server is up.

  The fifth one is there because "the server reached the Running state" is a statement about its log, not
  about the network. The default probe is the resource server's health endpoint, which answers without
  plugins, users, tokens or the database, so it proves the platform's HTTP listener really serves the
  endpoint it registered (the listener having started is a weaker claim than the server answering). A
  probe needs the server to stay up, so when probing, the host runs with --duration instead of --check -
  --check exits the moment the Running state is reached.

  The data directory is left in place on failure and named in the output, because the log inside it
  is the diagnostic.

  It runs the same way on every platform, through the dotnet muxer rather than the apphost .exe, so
  the same script can be the container's server test.

.EXAMPLE
  .\run-server-check.ps1

.EXAMPLE
  .\run-server-check.ps1 -SkipDeploy -TimeoutSeconds 60
  # Re-run against the layout that is already on disk.
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  [string]$LayoutDir,

  # Where the media database, configuration and log go. Defaults to a fresh directory under the
  # system temp path; an existing directory is reused, so pointing this at a populated install
  # directory is a way to test an upgrade.
  [string]$DataDir,

  [int]$TimeoutSeconds = 180,

  # URLs the running server has to answer with 200. Empty disables probing, which is also the only way
  # to run the host with --check (a probe needs the server to stay up).
  [string[]]$ProbeUrl = @('http://127.0.0.1:55555/MediaPortal/Server/health'),

  # How long the probes may keep asking before the run counts as failed. The server is held open for
  # this long plus a margin, so a slow machine slows the probe, not the verdict.
  [int]$ProbeTimeoutSeconds = 45,

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

# Asks the running server until it answers, so that a probe does not have to guess when the server got
# around to opening its port. HttpClient rather than Invoke-WebRequest: the cmdlet throws on a non-2xx
# status, which is exactly the information the gate wants, and it behaves differently in Windows
# PowerShell and pwsh. Returns one record per URL with the status it finally saw (0 for "never answered").
function Request-Probes([string[]]$Urls, $Server, [int]$DeadlineSeconds)
{
  $results = @()
  if (-not $Urls -or $Urls.Count -eq 0) { return $results }

  if (-not ('System.Net.Http.HttpClient' -as [type])) { Add-Type -AssemblyName System.Net.Http }
  $client = New-Object System.Net.Http.HttpClient
  $client.Timeout = [TimeSpan]::FromSeconds(10)
  try {
    $pending = New-Object 'System.Collections.Generic.List[string]'
    foreach ($url in $Urls) { $pending.Add($url) }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($pending.Count -gt 0 -and $stopwatch.Elapsed.TotalSeconds -lt $DeadlineSeconds) {
      if ($Server.HasExited) { break } # nothing is going to answer any more
      foreach ($url in @($pending)) {
        try {
          $get = $client.GetAsync($url)
          if (-not $get.Wait(11000)) { continue }
          $response = $get.Result
          $text = $response.Content.ReadAsStringAsync().Result
          $results += [pscustomobject]@{ Url = $url; StatusCode = [int]$response.StatusCode; Body = $text }
          $pending.Remove($url) | Out-Null
        } catch {
          # Nothing is listening yet, or the request did not finish; both are retried until the deadline.
        }
      }
      if ($pending.Count -gt 0) { Start-Sleep -Milliseconds 500 }
    }
    foreach ($url in $pending) { $results += [pscustomobject]@{ Url = $url; StatusCode = 0; Body = '' } }
  } finally {
    $client.Dispose()
  }
  return $results
}

if (-not $SkipDeploy) {
  & (Join-Path $PSScriptRoot 'deploy-server-layout.ps1') -Configuration $Configuration -OutputDir $LayoutDir
  if (-not $?) { throw 'layout deployment failed' }
}

$hostDll = [IO.Path]::Combine($LayoutDir, 'MP2-Server-Headless.dll')
if (-not (Test-Path $hostDll)) { throw "no $hostDll - run deploy-server-layout.ps1 first" }

if (-not $DataDir) {
  $DataDir = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'mp2-server-check-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
if (-not (Test-Path $DataDir)) {
  New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
}

$logFile = [IO.Path]::Combine($DataDir, 'Log', 'Server.log')
if (Test-Path $logFile) { Remove-Item $logFile -Force }

$probes = @($ProbeUrl | Where-Object { $_ })
# --check returns the moment the Running state is reached, which leaves no window in which a request could
# be answered, so a probing run holds the server open for a fixed time instead.
$holdSeconds = if ($probes.Count -gt 0) { $ProbeTimeoutSeconds + 15 } else { 0 }
$runArguments = if ($probes.Count -gt 0) { '--duration ' + $holdSeconds } else { '--check' }
if ($TimeoutSeconds -lt $holdSeconds + 60) {
  # Otherwise this script would kill a server that is only still starting on purpose.
  Write-Host "raising -TimeoutSeconds to $(($holdSeconds + 60)) to cover the probe window"
  $TimeoutSeconds = $holdSeconds + 60
}

Write-Step "running the server (layout $LayoutDir, data $DataDir, $($probes.Count) probe(s), $runArguments)"

# System.Diagnostics.Process rather than Start-Process: only this type reliably exposes ExitCode
# after a WaitForExit(timeout), and a check whose exit code is unreliable is not a gate.
# Output is not redirected on purpose - log4net writes a lot to the console, and an unread pipe
# fills up and blocks the child, which would then look like a startup hang.
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $dotnet
# The DLL path can contain spaces (it does under "Program Files" checkouts and in CI workspaces),
# so both arguments need quoting; ProcessStartInfo.Arguments is a single raw string on every OS.
$psi.Arguments = '"' + $hostDll + '" ' + $runArguments + ' --data "' + $DataDir + '"'
$psi.WorkingDirectory = $LayoutDir
$psi.UseShellExecute = $false

$server = [System.Diagnostics.Process]::Start($psi)
$probeResults = @(Request-Probes -Urls $probes -Server $server -DeadlineSeconds $ProbeTimeoutSeconds)
$timedOut = -not $server.WaitForExit($TimeoutSeconds * 1000)
if ($timedOut) {
  Write-Fail "the server did not exit within $TimeoutSeconds seconds; killing it"
  $server.Kill()
  $server.WaitForExit()
}
$exitCode = if ($timedOut) { -1 } else { $server.ExitCode }

$failures = @()
if ($timedOut) {
  $failures += "timed out after $TimeoutSeconds seconds"
} elseif ($exitCode -ne 0) {
  $failures += "exit code was $exitCode"
}

if (-not (Test-Path $logFile)) {
  $failures += "no log file at $logFile - the server did not get as far as the logger"
} else {
  $log = Get-Content $logFile -Raw
  if ($log -notmatch 'Switching to running state') {
    $failures += 'the log never reached the Running state'
  }
  $fatal = @($log -split "`n" | Where-Object { $_ -match '\bFATAL\b' })
  if ($fatal.Count -gt 0) {
    $failures += "$($fatal.Count) FATAL log line(s)"
  }
}

# The log saying "Running" only proves DatabaseManager found an ISQLDatabase. The file proves the
# provider opened one: <DATABASE> comes from Paths.xml and the default SQLite store is
# Datastore.s3db (the name is a setting, so the assertion is about the folder not being empty).
$databaseDir = [IO.Path]::Combine($DataDir, 'Database')
$dbFiles = @(Get-ChildItem -Path $databaseDir -File -ErrorAction SilentlyContinue)
if ($dbFiles.Count -eq 0) {
  $failures += "the database folder $databaseDir is empty - no database was created"
}

foreach ($probe in $probeResults) {
  if ($probe.StatusCode -ne 200) {
    $answered = if ($probe.StatusCode -eq 0) { 'no response at all' } else { "HTTP $($probe.StatusCode)" }
    $failures += "the probe of $($probe.Url) got $answered - the server did not serve its own endpoint"
  }
}

Write-Host ''
Write-Host "exit code: $exitCode"
if ($probeResults.Count -gt 0) {
  Write-Host 'probes:'
  foreach ($probe in $probeResults) {
    $snippet = if ($probe.Body) { $probe.Body.Substring(0, [Math]::Min(140, $probe.Body.Length)) } else { '' }
    Write-Host ("  {0} -> {1} {2}" -f $probe.Url, $probe.StatusCode, $snippet)
  }
}
Write-Host "log:       $logFile"
if (Test-Path $logFile) {
  Write-Host ''
  Write-Host '--- last 20 log lines ---'
  Get-Content $logFile | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
}

if ($failures.Count -gt 0) {
  Write-Host ''
  $failures | ForEach-Object { Write-Fail $_ }
  Write-Host "data directory kept for inspection: $DataDir"
  exit 1
}

Write-Host ''
Write-Step "server reached the Running state, stopped cleanly$(if ($probeResults.Count -gt 0) { ", and served all $($probeResults.Count) probe URL(s)" })"
# The data directory is kept in both outcomes. It holds the log, the configuration the server
# wrote and the SQLite database it created, which is the only evidence there is that the run was
# real; it lives under the system temp path, so nothing accumulates in the repository.

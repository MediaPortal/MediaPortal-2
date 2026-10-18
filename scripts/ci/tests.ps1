<#
.SYNOPSIS
  Runs the unit test suites on Windows and reports one line per suite.

.DESCRIPTION
  This is the Windows half of the gate; the Linux half is test-server-linux.ps1 and
  test-canary-linux.ps1. It exists so that "run the tests" is a command in the repository rather
  than something done with dotnet test by hand and remembered incorrectly afterwards.

  Default assumption is that build.ps1 has already built the tree, so tests run with --no-build:
  rebuilding the reference closure before every suite costs more than the tests themselves. Pass
  -Build to let dotnet test build first.

  Because --no-build runs the assembly on disk, each suite is checked for staleness first: if a source
  file in the suite's own project is newer than its test assembly, the suite reports STALE and fails
  instead of reporting a green result for a binary that does not contain the current sources. The check
  covers the suite's own folder only; edits to a referenced project still need -Build.

  Which suites run by default is a decision, not an accident:
    Canary.Portable  - the portability canary, the only suite that also runs on Linux.
    Tests.Server     - the server/backend suite, part of MP2-Server.sln.
    Tests.Client     - the client suite; it passes, but exercises WinForms code that Phase 2 removes.
    Tests.Frontend   - the new Avalonia front end, run headless: its own client against the server's real
                       controllers, and the real screens on a platform with no display. It is a separate
                       suite (and a separate NUnit version) because Avalonia.Headless needs NUnit 4 while the
                       inherited suites are pinned to NUnit 3; see Tests.Frontend.csproj.
    Setup\Tests      - the bootstrapper installer tests (pure C#, no WiX needed).
  Tests.MediaServer is NOT in the default set: it belongs to a solution that is not in the gate and
  has not been verified since the retarget, so including it would report an unknown as a failure.
  Ask for it explicitly with -Include.

.EXAMPLE
  .\tests.ps1
  .\tests.ps1 -Build
  .\tests.ps1 -Include Canary.Portable,Tests.MediaServer -Build
#>
[CmdletBinding()]
param(
  [string]$Configuration = 'Debug',

  [switch]$Build,

  # Suites to run instead of the default set (names as they appear in $suites below).
  [string[]]$Include
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$suites = [ordered]@{
  'Canary.Portable'   = 'MediaPortal\Tests\Canary.Portable\Canary.Portable.csproj'
  'Tests.Server'      = 'MediaPortal\Tests\Tests.Server\Tests.Server.csproj'
  'Tests.Client'      = 'MediaPortal\Tests\Tests.Client\Tests.Client.csproj'
  'Tests.Frontend'    = 'MediaPortal\Tests\Tests.Frontend\Tests.Frontend.csproj'
  'Tests.MediaServer' = 'MediaPortal\Tests\Tests.MediaServer\Tests.MediaServer\Tests.MediaServer.csproj'
  'Setup\Tests'       = 'MediaPortal\Setup\Tests\Tests.csproj'
}
$default = @('Canary.Portable', 'Tests.Server', 'Tests.Client', 'Tests.Frontend', 'Setup\Tests')
$chosen = if ($Include) { $Include } else { $default }

foreach ($name in $chosen) {
  if (-not $suites.Contains($name)) {
    Write-Host "unknown suite '$name'; known: $($suites.Keys -join ', ')" -ForegroundColor Red
    exit 2
  }
}

$dotnet = (Get-Command dotnet | Select-Object -ExpandProperty Source)
$failures = @()
$results = @()
$ran = 0

foreach ($name in $chosen) {
  $proj = Join-Path $repoRoot $suites[$name]
  if (-not (Test-Path $proj)) {
    Write-Host "MISSING  $name  ($proj)" -ForegroundColor Red
    $failures += $name
    continue
  }

  Write-Host ""
  Write-Host "=== $name" -ForegroundColor Cyan
  $cliArgs = @('test', $proj, '-c', $Configuration, '--nologo', '-v', 'q')
  if (-not $Build) { $cliArgs += '--no-build' }

  # '--no-build' runs the assembly that happens to be on disk. That is the fast and documented path -
  # but it means a green line can be evidence about a binary that predates the sources, which is how a
  # test added after the last build silently does not run. Compare the newest source file in the suite's
  # own project with the test assembly and refuse to report a result in that case.
  # Only the suite's own folder is watched (not its referenced projects), so this is a floor: an edit to
  # MediaPortal.Backend after the build still needs -Build.
  if (-not $Build) {
    $projectDir = Split-Path $proj -Parent
    $testAssemblyName = [IO.Path]::GetFileNameWithoutExtension($proj)
    $testDll = Get-ChildItem -Path (Join-Path $projectDir 'bin') -Recurse -Filter "$testAssemblyName.dll" -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '\\obj\\' } |
        Sort-Object -Property LastWriteTime -Descending | Select-Object -First 1
    if ($testDll) {
      $newestSource = Get-ChildItem -Path $projectDir -Recurse -Include '*.cs', '*.csproj', '*.props', '*.targets' -File -ErrorAction SilentlyContinue |
          Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
          Sort-Object -Property LastWriteTime -Descending | Select-Object -First 1
      if ($newestSource -and $newestSource.LastWriteTime -gt $testDll.LastWriteTime) {
        Write-Host ("  STALE: {0} was built at {1}, but {2} is newer ({3})." -f `
            $testDll.Name, $testDll.LastWriteTime.ToString('s'), $newestSource.Name, $newestSource.LastWriteTime.ToString('s')) -ForegroundColor Red
        Write-Host "  The suite would run a binary that does not contain the current sources. Run tests.ps1 -Build, or build the solution first." -ForegroundColor Red
        $failures += "$name (stale build)"
        $results += [pscustomobject]@{ Suite = $name; Tests = 0; Passed = 0; Failed = 0; Result = 'STALE (not built after the last source change)' }
        continue
      }
    }
  }

  # Capture the console output as data, then read the exit code. A suite that dies without printing
  # a summary line (non-zero exit, no "Passed!" text) must still count as failed, so the exit code -
  # not the text - is the authority.
  $out = & $dotnet @cliArgs 2>&1 | Out-String
  $code = $LASTEXITCODE
  $text = $out -split "`r?`n"

  # The summary line is localized ("Bestanden! : Fehler: 0, erfolgreich: 68, übersprungen: 0,
  # gesamt: 68, Dauer: 164 ms - Tests.dll (net10.0)"), so it is never parsed by label. The labels are
  # translated but their order is not, so the first four integers on the line are, in order:
  # failed, passed, skipped, total. ("Dauer/Duration" and the "(net10.0)" suffix come later.)
  $line = $text | Where-Object { $_ -match '\.dll\s*\(' -and $_ -match '\d+.*\d+.*\d+.*\d+' } | Select-Object -Last 1
  $failed = 0; $passed = 0; $skipped = 0; $total = 0
  if ($line) {
    $nums = [regex]::Matches($line, '\d+') | ForEach-Object { [int]$_.Value }
    # The four counts are the first four integers; "Dauer: 164 ms" comes after them.
    if ($nums.Count -ge 4) { $failed = $nums[0]; $passed = $nums[1]; $skipped = $nums[2]; $total = $nums[3] }
    Write-Host ("  total={0} passed={1} failed={2} skipped={3}" -f $total, $passed, $failed, $skipped)
  } else {
    Write-Host "  no summary line found in the output" -ForegroundColor Yellow
  }
  $ran += $total

  if ($code -ne 0) {
    $failures += $name
    Write-Host "  FAILED (exit $code) - relevant lines:" -ForegroundColor Red
    $text | Where-Object { $_ -match '^\s*(Error Message|Assert|Expected|Stack Trace|error )' } |
      Select-Object -First 12 | ForEach-Object { "    $($_.Trim())" } | Write-Host
    $results += [pscustomobject]@{ Suite = $name; Tests = $total; Passed = $passed; Failed = $failed; Result = "FAILED (exit $code)" }
  } else {
    $results += [pscustomobject]@{ Suite = $name; Tests = $total; Passed = $passed; Failed = $failed; Result = 'OK' }
  }
}

Write-Host ""
$results | Format-Table -AutoSize | Out-String -Width 160 | Write-Host
if ($failures.Count) {
  Write-Host "TESTS FAILED: $($failures -join ', ')  ($ran tests executed)" -ForegroundColor Red
  exit 1
}
Write-Host "All suites passed. $ran tests executed." -ForegroundColor Green
exit 0

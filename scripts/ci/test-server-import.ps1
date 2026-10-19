<#
.SYNOPSIS
  Points a running MP2 server at a folder of files and checks that the folder becomes a title.
.DESCRIPTION
  Every other gate in this folder asks the server about code. This one asks it about content, and it is the
  gate that answers the question the media centre actually lives on: a person points the server at their film
  folder, and does the shelf fill up?

  Nothing before it could answer that. A freshly deployed server had no metadata extractor plugin in its
  Plugins directory, so the importer walked every file, asked nobody, and left the library empty - which reads
  on screen like a broken front end and is a broken installation.

  What it does, in order:

    1. deploys the layout (headless host + SQLiteDatabase + NfoMetadataExtractors);
    2. writes a media folder the way a tool like tinyMediaManager leaves one: a video file, a movie.nfo with a
       title, a year and a <thumb> that names a picture in the same folder, and the picture itself;
    3. starts the server against a scratch data directory and waits for its HTTP listener;
    4. asks GET  api/media/shares     - 200, no shares yet, and "Movie" among the media categories. That last
       claim is the first proof that the NFO plugin was loaded: the "Movie" category does not exist until the
       plugin's movie extractor registers it;
    5. asks POST api/media/shares     - 201, which also queues the import (registering a local share schedules
       it; the endpoint deliberately does not queue it a second time);
    6. polls GET  api/media/titles?aspect=MovieItem until the title is there, and checks its year and its path.
       Note the name: the endpoint is asked for a media item *aspect*, and the aspect is called "MovieItem"
       while the share category next door is called "Movie". Asking for "Movie" is answered with a 404 that
       lists the names this server knows;
    7. asks GET  api/media/items/{id} - the plot, tagline and certification have to be the ones from movie.nfo,
       which is the reader, the database and HTTP in one answer. Pictures are deliberately not asserted here:
       an NFO's <thumb> is kept as a stub for the fanart handler (MovieFanArtHandler copies it into the fanart
       cache as the poster), it does not end up in ThumbnailLargeAspect, and the fanart cache has no HTTP
       endpoint yet - serving pictures is the "pictures pillar" on the backlog;
    8. asks GET  api/media/import     - the queue has to drain to zero, otherwise "the title turned up" would
       still leave open whether the import is finished or merely halfway;
    9. asks DELETE api/media/shares/{id} - 204, and the title is gone from the shelf afterwards, which is what
       removing the folder from a library has to mean.

  The scratch configuration turns WebAutorizationEnabled off. That is not the gate dodging the token: a fresh
  database holds no user profile, profiles are created through the local UI or UPnP, so there is nobody a token
  could be signed for. Authorization itself is covered by run-server-check.ps1, which asks the same server for
  the same endpoints with and without tokens.

  The server has to exit with code 0 and the log must not contain a single [ERROR] line. An import that
  "worked" while logging three exceptions worked by accident.
.PARAMETER LayoutDir
  Deployment layout from deploy-server-layout.ps1. Defaults to build\artifacts\server-layout.
.PARAMETER DataDir
  Scratch directory for database, configuration and log. Defaults to a new directory in TEMP.
.PARAMETER MediaDir
  Where the film folder is written. Defaults to a new directory in TEMP. It has to be readable by the server
  process, which for this gate is the same user.
.PARAMETER DurationSeconds
  How long the server stays up.
.PARAMETER ImportTimeoutSeconds
  How long to wait for the title to appear.
.EXAMPLE
  .\test-server-import.ps1
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  [string]$LayoutDir,

  [string]$DataDir,

  [string]$MediaDir,

  [int]$DurationSeconds = 120,

  [int]$ImportTimeoutSeconds = 60,

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

$BaseUrl = 'http://127.0.0.1:55555/MediaPortal/Server'
$MovieTitle = 'The Example Movie'
$MovieYear = 2026

# The poster the movie.nfo names. It is written because a scraper leaves one and the <thumb> element points at
# it, so the folder the gate imports looks like a real one; the gate does not ask for the picture back, which
# step 7 of the description explains.
$PosterBase64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg=='
$PosterBytes = [Convert]::FromBase64String($PosterBase64)

if (-not $SkipDeploy) {
  & (Join-Path $PSScriptRoot 'deploy-server-layout.ps1') -Configuration $Configuration -OutputDir $LayoutDir
  if (-not $?) { throw 'layout deployment failed' }
}

$hostDll = [IO.Path]::Combine($LayoutDir, 'MP2-Server-Headless.dll')
if (-not (Test-Path $hostDll)) { throw "no $hostDll - run deploy-server-layout.ps1 first" }

$temp = [IO.Path]::GetTempPath()
if (-not $DataDir) {
  $DataDir = [IO.Path]::Combine($temp, 'mp2-import-data-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
if (-not $MediaDir) {
  $MediaDir = [IO.Path]::Combine($temp, 'mp2-import-media-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
New-Item -ItemType Directory -Path $DataDir -Force | Out-Null

# ---------------------------------------------------------------------------
# 1. A media folder the way a scraper leaves it
# ---------------------------------------------------------------------------

$movieDir = [IO.Path]::Combine($MediaDir, "$MovieTitle ($MovieYear)")
New-Item -ItemType Directory -Path $movieDir -Force | Out-Null
Write-Step "writing a film folder to $movieDir"

# A movie.nfo modelled on what tinyMediaManager writes (the plugin's own test data is one), kept to the elements
# that a shelf needs: a title, a year, a plot, and a <thumb> naming a picture in this folder rather than a URL,
# so the folder is built without touching the internet.
$nfo = @(
  '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>',
  '<!-- written by scripts/ci/test-server-import.ps1 -->',
  '<movie>',
  "    <title>$MovieTitle</title>",
  "    <originaltitle>$MovieTitle</originaltitle>",
  "    <year>$MovieYear</year>",
  '    <runtime>104</runtime>',
  '    <plot>A film that exists only while this gate runs.</plot>',
  '    <tagline>Nothing else.</tagline>',
  '    <mpaa>PG</mpaa>',
  '    <premiered>2026-04-01</premiered>',
  '    <genre>Drama</genre>',
  '    <thumb>poster.png</thumb>',
  '</movie>'
)
[IO.File]::WriteAllLines([IO.Path]::Combine($movieDir, 'movie.nfo'), $nfo, (New-Object System.Text.UTF8Encoding($false)))
[IO.File]::WriteAllBytes([IO.Path]::Combine($movieDir, 'poster.png'), $PosterBytes)
# The extractor does not open the video - the only extractor in this layout reads the .nfo - but the importer
# only offers a directory as a film when a file in it is a video file by extension.
[IO.File]::WriteAllBytes([IO.Path]::Combine($movieDir, "$MovieTitle ($MovieYear).mkv"), [byte[]](0..255))

# ---------------------------------------------------------------------------
# 2. The scratch configuration
# ---------------------------------------------------------------------------

$configDir = [IO.Path]::Combine($DataDir, 'Config')
New-Item -ItemType Directory -Path $configDir -Force | Out-Null
$serverSettingsFile = [IO.Path]::Combine($configDir,
  'MediaPortal.Common.Services.ResourceAccess.Settings.ServerSettings.xml')
@(
  '<?xml version="1.0" encoding="utf-8"?>',
  '<Configuration>',
  '  <Property Name="WebAutorizationEnabled">',
  '    <boolean>false</boolean>',
  '  </Property>',
  '</Configuration>'
) | Set-Content -Path $serverSettingsFile -Encoding UTF8
Write-Step "scratch config at $serverSettingsFile (web authorization off: a fresh database has no profile)"

$logFile = [IO.Path]::Combine($DataDir, 'Log', 'Server.log')
if (Test-Path $logFile) { Remove-Item $logFile -Force }

# ---------------------------------------------------------------------------
# 3. Start the server
# ---------------------------------------------------------------------------

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

function Wait-LogLine([string]$pattern, [int]$timeoutSeconds) {
  $deadline = (Get-Date).AddSeconds($timeoutSeconds)
  while ((Get-Date) -lt $deadline) {
    if ((Test-Path $logFile) -and
        ((Get-Content $logFile -Raw -ErrorAction SilentlyContinue) -match $pattern)) { return $true }
    if ($server.HasExited) { return $false }
    Start-Sleep -Milliseconds 200
  }
  return $false
}

# The resource server opens its port before the media library has finished starting, so this waits for the
# importer to say it is activated as well - the point from which a scheduled share is really taken.
if (-not (Wait-LogLine 'ImporterWorker: Activated' 90)) {
  Write-Fail "the importer never activated (see $logFile)"
  $failures += 'the importer did not start'
  Stop-Server
}

$client = $null

# HttpClient rather than Invoke-WebRequest: the cmdlet throws on a non-2xx answer, which is exactly the thing
# this gate has to look at. The type only needs adding on Windows PowerShell, where it is not referenced by
# default; on pwsh the check says it is already there.
if (-not ('System.Net.Http.HttpClient' -as [type])) { Add-Type -AssemblyName System.Net.Http }

function Get-Json([string]$url) {
  $answer = $client.GetAsync($url).GetAwaiter().GetResult()
  $body = $answer.Content.ReadAsStringAsync().GetAwaiter().GetResult()
  return @{ StatusCode = [int] $answer.StatusCode; Body = $body }
}

function Send-Json([string]$method, [string]$url, [string]$json) {
  # HttpMethod is a class, not an enum, so the verb has to be spelled through the type - PowerShell will not
  # turn a string into it on its own.
  $httpMethod = New-Object System.Net.Http.HttpMethod($method)
  $request = New-Object System.Net.Http.HttpRequestMessage($httpMethod, $url)
  if ($null -ne $json) {
    $request.Content = New-Object System.Net.Http.StringContent($json, [System.Text.Encoding]::UTF8, 'application/json')
  }
  $answer = $client.SendAsync($request).GetAwaiter().GetResult()
  $body = $answer.Content.ReadAsStringAsync().GetAwaiter().GetResult()
  return @{ StatusCode = [int] $answer.StatusCode; Body = $body }
}

$shareId = $null
$itemId = $null

try {
  $client = New-Object System.Net.Http.HttpClient
  $client.Timeout = [TimeSpan]::FromSeconds(30)

  # -------------------------------------------------------------------------
  # 4. What is registered, and what may be asked for
  # -------------------------------------------------------------------------

  Write-Step 'asking the server which shares it has'
  $answer = Get-Json ($BaseUrl + '/api/media/shares')
  if ($answer.StatusCode -ne 200) {
    Write-Fail "GET api/media/shares answered $($answer.StatusCode): $($answer.Body)"
    $failures += 'the share list was not answerable'
  } else {
    $shares = $answer.Body | ConvertFrom-Json
    if ($shares.count -ne 0) {
      Write-Fail "a fresh server reported $($shares.count) share(s); the database is not fresh"
      $failures += 'the scratch database was not empty'
    }
    # The category is registered by the plugin's movie extractor in its static constructor, so seeing it here
    # means the plugin was found, loaded and asked for its extractors.
    if ($shares.mediaCategories -notcontains 'Movie') {
      Write-Fail "the server knows no 'Movie' media category (it knows: $($shares.mediaCategories -join ', '))"
      $failures += 'the NFO plugin did not register its media category'
    }
  }

  # -------------------------------------------------------------------------
  # 5. Point it at the folder
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0) {
    Write-Step "registering $movieDir as a share"
    $body = ConvertTo-Json @{ path = $movieDir; mediaCategories = @('Movie') }
    $answer = Send-Json 'POST' ($BaseUrl + '/api/media/shares') $body
    if ($answer.StatusCode -ne 201) {
      Write-Fail "POST api/media/shares answered $($answer.StatusCode): $($answer.Body)"
      $failures += 'the share could not be registered'
    } else {
      $shareId = ($answer.Body | ConvertFrom-Json).id
      Write-Step "share $shareId registered, the import is queued"
    }
  }

  # -------------------------------------------------------------------------
  # 6. The title has to turn up
  # -------------------------------------------------------------------------

  $title = $null
  if ($failures.Count -eq 0) {
    Write-Step "waiting up to $ImportTimeoutSeconds seconds for '$MovieTitle' to reach the shelf"
    $deadline = (Get-Date).AddSeconds($ImportTimeoutSeconds)
    while ((Get-Date) -lt $deadline -and $null -eq $title) {
      $answer = Get-Json ($BaseUrl + '/api/media/titles?aspect=MovieItem')
      if ($answer.StatusCode -eq 200) {
        $titles = $answer.Body | ConvertFrom-Json
        $title = @($titles.titles) | Where-Object { $_.title -eq $MovieTitle } | Select-Object -First 1
      }
      if ($null -eq $title) { Start-Sleep -Milliseconds 500 }
    }
    if ($null -eq $title) {
      Write-Fail "'$MovieTitle' never appeared in api/media/titles?aspect=MovieItem"
      $failures += 'the folder did not become a title'
    } else {
      Write-Step "the shelf holds '$($title.title)' ($($title.year))"
      if ($title.year -ne $MovieYear) {
        Write-Fail "the title's year is '$($title.year)', expected $MovieYear"
        $failures += 'the imported year is wrong'
      }
      if (-not $title.path) {
        Write-Fail 'the title carries no path, so no front end could reach the file'
        $failures += 'the imported title has no resource path'
      }
      $itemId = $title.id
    }
  }

  # -------------------------------------------------------------------------
  # 7. The text of the nfo has to come back out of the database
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0 -and $itemId) {
    Write-Step 'asking for the title in detail'
    $answer = Get-Json ($BaseUrl + "/api/media/items/$itemId")
    if ($answer.StatusCode -ne 200) {
      Write-Fail "GET api/media/items/$itemId answered $($answer.StatusCode)"
      $failures += 'the imported title was not served in detail'
    } else {
      $detail = $answer.Body | ConvertFrom-Json
      foreach ($check in @(
          @{ Name = 'plot'; Got = $detail.storyPlot; Want = 'A film that exists only while this gate runs.' },
          @{ Name = 'tagline'; Got = $detail.tagline; Want = 'Nothing else.' },
          # MP2 keeps a certification as a country-prefixed code, so the "PG" the nfo names comes back as
          # "US_PG" - the reader normalized it, which is what a front end shows next to the title.
          @{ Name = 'certification'; Got = $detail.certification; Want = 'US_PG' },
          @{ Name = 'runtime'; Got = $detail.runtimeMinutes; Want = 104 })) {
        if ($check.Got -ne $check.Want) {
          Write-Fail "the imported $($check.Name) is '$($check.Got)', expected '$($check.Want)'"
          $failures += "the imported $($check.Name) is wrong"
        }
      }
      # The picture on purpose is not checked here. An nfo's <thumb> is handed to the fanart handler as a
      # stub - MovieFanArtHandler copies it into the fanart cache as the poster - and that cache is not
      # reachable over HTTP yet, so there is nothing this gate could ask for the picture with.
      Write-Step "the nfo's text came back through the database (runtime $($detail.runtimeMinutes) minutes)"
    }
  }

  # -------------------------------------------------------------------------
  # 8. The queue has to drain
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0) {
    Write-Step 'waiting for the import queue to drain'
    $deadline = (Get-Date).AddSeconds($ImportTimeoutSeconds)
    $pending = -1
    while ((Get-Date) -lt $deadline) {
      $answer = Get-Json ($BaseUrl + '/api/media/import')
      if ($answer.StatusCode -eq 200) {
        $pending = ($answer.Body | ConvertFrom-Json).pending
        if ($pending -eq 0) { break }
      }
      Start-Sleep -Milliseconds 500
    }
    if ($pending -ne 0) {
      Write-Fail "the importer still holds $pending job(s) after $ImportTimeoutSeconds seconds"
      $failures += 'the import did not finish'
    }
  }

  # -------------------------------------------------------------------------
  # 9. Taking the folder out again
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0 -and $shareId) {
    Write-Step "removing share $shareId again"
    $answer = Send-Json 'DELETE' ($BaseUrl + "/api/media/shares/$shareId") $null
    if ($answer.StatusCode -ne 204) {
      Write-Fail "DELETE api/media/shares/$shareId answered $($answer.StatusCode): $($answer.Body)"
      $failures += 'the share could not be removed'
    } else {
      $answer = Get-Json ($BaseUrl + '/api/media/titles?aspect=MovieItem')
      $titles = @()
      if ($answer.StatusCode -eq 200) { $titles = @(($answer.Body | ConvertFrom-Json).titles) }
      if (@($titles | Where-Object { $_.title -eq $MovieTitle }).Count -ne 0) {
        Write-Fail 'the title is still on the shelf after its share was removed'
        $failures += 'removing the share left the title behind'
      }
    }
  }
} finally {
  if ($client) { $client.Dispose() }
}

# ---------------------------------------------------------------------------
# 10. The server itself has to be healthy
# ---------------------------------------------------------------------------

if (-not $server.WaitForExit(($DurationSeconds + 60) * 1000)) {
  Write-Fail 'the server did not shut down by itself'
  $failures += 'the server did not exit'
  Stop-Server
}
$exitCode = $server.ExitCode
if ($exitCode -ne 0) {
  Write-Fail "the server exited with code $exitCode"
  $failures += "the server exited with $exitCode"
}

$errors = @()
if (Test-Path $logFile) {
  $errors = @(Get-Content $logFile | Where-Object { $_ -match '\[ERROR\]' })
}
if ($errors.Count -gt 0) {
  Write-Fail "$($errors.Count) error line(s) in $logFile"
  $errors | Select-Object -First 10 | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }
  $failures += 'the server logged errors'
}

Write-Host ''
if ($failures.Count -eq 0) {
  Write-Host "PASS: a folder with an .nfo became a title with the year it says, a path a player could open," -ForegroundColor Green
  Write-Host "      and the text of its .nfo." -ForegroundColor Green
  Write-Host "      data: $DataDir"
  Write-Host "      media: $MediaDir"
  exit 0
}

Write-Host "FAIL ($($failures.Count)):" -ForegroundColor Red
$failures | ForEach-Object { Write-Host "   - $_" -ForegroundColor Red }
Write-Host "      log: $logFile"
exit 1

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

    1. deploys the layout (headless host + SQLiteDatabase + NfoMetadataExtractors + AudioTagMetadataExtractor);
    2. writes a media folder the way a tool like tinyMediaManager leaves one: a video file, a movie.nfo with a
       title, a year and a <thumb> that names a picture in the same folder, and the picture itself. Next to it
       it writes a music folder with two mp3 files - one with a full ID3v2.3 tag, one with nothing written in
       it - because a media centre whose films import while its music does not is half a media centre. The tagged
       file also carries a JPEG in its front-cover frame, which step 8c asks for back;
    3. starts the server against a scratch data directory and waits for its HTTP listener;
    4. asks GET  api/media/shares     - 200, no shares yet, and "Movie" among the media categories. That last
       claim is the first proof that the NFO plugin was loaded: the "Movie" category does not exist until the
       plugin's movie extractor registers it;
    5. asks POST api/media/shares         - 201 twice, once for the film folder under the Movie category and once for the music folder under Audio,
          which also queues the import (registering a local share schedules it; the endpoint deliberately does not
          queue it a second time);
       6. polls GET  api/media/titles?aspect=MovieItem until the title is there, and checks its year and its path.
       Note the name: the endpoint is asked for a media item *aspect*, and the aspect is called "MovieItem"
       while the share category next door is called "Movie". Asking for "Movie" is answered with a 404 that
       lists the names this server knows;
    7. asks GET  api/media/items/{id} - the plot, tagline and certification have to be the ones from movie.nfo,
       which is the reader, the database and HTTP in one answer. Pictures are not asserted here: an NFO's <thumb>
       is kept as a stub for the fanart handler, which stores it under the poster once a fanart pass has run -
       step 8c is where pictures are asked about, and it asks about the music covers;
    8. asks the same shelf for aspect=AudioItem and looks for both mp3 files: the tagged one as
       "Fixture Album: 3 - Test Track" - the title MP2 builds out of album, track number and title - with the
       artist, the year of its TYER frame, a track number and a path, and the untagged one as
       "9 - Anonymous Track", which is what the file name alone has to be enough for. It then asks for
       aspect=AlbumItem - the audio album aspect is simply named "AlbumItem" by this server - expects the album
       the tags describe, and asks which tracks are inside that album:
       a two-track answer is the Track->Album relationship rows being in the database, which is the half of the
       import a track count alone cannot show;
    8c. asks POST api/media/fanart/warming, then GET api/media/items/{albumId}/fanart/Cover, and expects the JPEG
        that sits inside the tag to come back byte for byte - and the track behind it to be answered with the same
        bytes, because the import writes one cover per album and a track wears its album's through the relationship
        rather than carrying a copy. It finally looks at the data folder to see that the answer really came out of
        the fanart cache rather than out of the user's file on every request. This is the picture story a portable
        server can tell without a network: covers are read out of the files the user already has;
    9. asks GET  api/media/import     - the queue has to drain to zero, otherwise "the title turned up" would
       still leave open whether the import is finished or merely halfway;
    10. asks DELETE api/media/shares/{id} for both shares - 204, and the film and the two tracks are gone from
       their shelves afterwards, which is what removing a folder from a library has to mean.

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
  Where the film folder and the music folder are written. Defaults to a new directory in TEMP. It has to be
  readable by the server process, which for this gate is the same user.
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

# What the two mp3 files have to turn into. The track title is not the title in the tag: MP2 names a track after
# its album and its number on it (TrackInfo.TRACK_FORMAT_STR), which is why "Test Track" tagged as track 3 of
# "Fixture Album" is listed as "Fixture Album: 3 - Test Track". The album is named after itself plus its year
# (AlbumInfo.ALBUM_FORMAT_STR), and the second file has no tag at all, so its name is all the library has.
$ArtistName = 'Fixture Artist'
$AlbumName = 'Fixture Album'
$AlbumTitle = "$AlbumName (2008)"
$TaggedTrackTitle = "$AlbumName`: 3 - Test Track"
$UntaggedTrackTitle = '9 - Anonymous Track'

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
# 1b. A music folder, because a shelf of nothing but films proves nothing about the audio side
# ---------------------------------------------------------------------------

$albumDir = [IO.Path]::Combine($MediaDir, 'Music', $AlbumName)
New-Item -ItemType Directory -Path $albumDir -Force | Out-Null
Write-Step "writing a music folder to $albumDir"

# The two mp3 files are the audio extractor's own fixtures, read out of the test project instead of being written
# out again here: one second of silence written by ffmpeg, once with a full ID3v2.3 tag and once with nothing at
# all. The gate and the unit tests then ask about the same bytes, which is the point - the unit tests prove the
# reader, this gate proves the reader is installed, loaded and given the file.
#
# The tagged one is the variant that also carries a JPEG in its tag's front-cover frame, because that frame is
# half of what the audio side of a media centre is for: a shelf of albums without covers is a wall of
# placeholders however well the names were read. The same file therefore proves the tag was read for its names
# and for its picture.
$fixtureDir = [IO.Path]::Combine($repoRoot, 'MediaPortal', 'Tests', 'Tests.Server', 'AudioTagMetadataExtractor', 'TestData')
$taggedFixture = [IO.Path]::Combine($fixtureDir, 'cover.mp3')
if (-not (Test-Path $taggedFixture)) { throw "no $taggedFixture - the audio half of this gate has no file to import" }
$coverFixture = [IO.Path]::Combine($fixtureDir, 'cover.jpg')
if (-not (Test-Path $coverFixture)) { throw "no $coverFixture - this gate cannot check what came back out of the tag" }
Copy-Item $taggedFixture ([IO.Path]::Combine($albumDir, '03 - Test Track.mp3')) -Force
Copy-Item ([IO.Path]::Combine($fixtureDir, 'untagged.mp3')) ([IO.Path]::Combine($albumDir, '09 - Anonymous Track.mp3')) -Force

# The bytes the handler is supposed to hand back. cover.jpg is the very JPEG that was pasted into cover.mp3's
# front-cover frame when the fixtures were made, so comparing the stored cover against it says the picture the
# user pasted in is the picture the shelf wears - not a re-encoded one, not another file's.
$CoverBytes = [IO.File]::ReadAllBytes($coverFixture)
Write-Step "the tagged fixture carries a front-cover picture of $($CoverBytes.Length) byte(s), which is what the cache has to hold"

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

function Get-Picture([string]$url) {
  # The picture endpoints answer with bytes, and reading them as a string would turn every byte above 0x7F into
  # a question mark - which is exactly the difference between "the user's cover" and "some bytes that came back".
  $answer = $client.GetAsync($url).GetAwaiter().GetResult()
  $bytes = $answer.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
  $text = $null
  if ([int] $answer.StatusCode -ne 200) {
    # A 404 says why in words, and that is the only answer worth quoting back.
    $text = $answer.Content.ReadAsStringAsync().GetAwaiter().GetResult()
  }
  return @{
    StatusCode = [int] $answer.StatusCode
    Bytes = $bytes
    ContentType = $answer.Content.Headers.ContentType.MediaType
    Text = $text
  }
}

$shareIds = @()
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
    # Said apart from the check above because the audio half of the gate is a different plugin with a different
    # reader. Without a category by this name the music folder cannot be registered at all, so it is worth
    # hearing here rather than from a 400 on the POST below.
    if ($shares.mediaCategories -notcontains 'Audio') {
      Write-Fail "the server knows no 'Audio' media category (it knows: $($shares.mediaCategories -join ', '))"
      $failures += 'the server would not accept a music folder'
    }
  }

  # -------------------------------------------------------------------------
  # 5. Point it at the folders
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0) {
    # Two shares on purpose: the film folder is read by the nfo plugin and the music folder by the audio tag
    # plugin, so a layout that ships one of the two still imports half a media library and passes a one-share
    # gate.
    foreach ($wish in @(
        @{ Dir = $movieDir; Category = 'Movie' },
        @{ Dir = $albumDir; Category = 'Audio' })) {
      Write-Step "registering $($wish.Dir) as a $($wish.Category) share"
      $body = ConvertTo-Json @{ path = $wish.Dir; mediaCategories = @($wish.Category) }
      $answer = Send-Json 'POST' ($BaseUrl + '/api/media/shares') $body
      if ($answer.StatusCode -ne 201) {
        Write-Fail "POST api/media/shares for $($wish.Dir) answered $($answer.StatusCode): $($answer.Body)"
        $failures += 'the share could not be registered'
      } else {
        $id = ($answer.Body | ConvertFrom-Json).id
        $shareIds += $id
        Write-Step "share $id registered, the import is queued"
      }
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
  # 8. The music shelf has to hold both mp3 files, and the album they describe
  # -------------------------------------------------------------------------

  $albumId = $null
  if ($failures.Count -eq 0) {
    Write-Step "waiting up to $ImportTimeoutSeconds seconds for the two mp3 files to reach the music shelf"
    $tracks = @()
    $deadline = (Get-Date).AddSeconds($ImportTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
      $answer = Get-Json ($BaseUrl + '/api/media/titles?aspect=AudioItem')
      if ($answer.StatusCode -eq 200) { $tracks = @(($answer.Body | ConvertFrom-Json).titles) }
      if (@($tracks | Where-Object { $_.title -eq $TaggedTrackTitle }).Count -ne 0 -and
          @($tracks | Where-Object { $_.title -eq $UntaggedTrackTitle }).Count -ne 0) { break }
      Start-Sleep -Milliseconds 500
    }

    $tagged = @($tracks | Where-Object { $_.title -eq $TaggedTrackTitle }) | Select-Object -First 1
    if ($null -eq $tagged) {
      # This is the answer the bug report was about: an mp3 nobody claims is dropped by the importer without a
      # word, so an empty music shelf is what a missing audio extractor looks like from the outside.
      Write-Fail "'$TaggedTrackTitle' never appeared in api/media/titles?aspect=AudioItem (the shelf holds: $(@($tracks.title) -join ', '))"
      $failures += 'the tagged mp3 did not become a track'
    } else {
      Write-Step "the music shelf holds '$($tagged.title)' by '$($tagged.artist)' ($($tagged.year))"
      foreach ($check in @(
          @{ Name = 'artist'; Got = $tagged.artist; Want = $ArtistName },
          # The year is the tag's TYER frame arriving as the recording time; the track number is its TRCK frame.
          @{ Name = 'year'; Got = $tagged.year; Want = 2008 },
          @{ Name = 'track number'; Got = $tagged.track; Want = 3 })) {
        if ($check.Got -ne $check.Want) {
          Write-Fail "the imported track's $($check.Name) is '$($check.Got)', expected '$($check.Want)'"
          $failures += "the imported track's $($check.Name) is wrong"
        }
      }
      if (-not $tagged.path) {
        Write-Fail 'the track carries no path, so nothing could play it'
        $failures += 'the imported track has no resource path'
      }
    }

    # The second file is what a ripped CD without tags looks like, and guessing from the name is the other half
    # of the reader's job: "09 - Anonymous Track.mp3" has to be enough for track nine of that name.
    if (@($tracks | Where-Object { $_.title -eq $UntaggedTrackTitle }).Count -eq 0) {
      Write-Fail "'$UntaggedTrackTitle' is not on the music shelf; the file named '09 - Anonymous Track.mp3' has nothing but its name to go by"
      $failures += 'the untagged mp3 did not become a track'
    }
  }

  # -------------------------------------------------------------------------
  # 8b. The tracks have to know the album they came from, and the album the artist
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0) {
    Write-Step 'asking the shelf for the album the tags describe'
    $albums = @()
    $album = $null
    # The album is not written by the reader but by the relationship extractor one block later, so it turns up a
    # moment after the two tracks did. Asking once would be asking the importer whether it happened to be quick
    # on this run, which is how a gate starts failing on a Tuesday for no reason anybody can see.
    $deadline = (Get-Date).AddSeconds($ImportTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
      # The aspect's name is "AlbumItem" - AudioAlbumAspect.Metadata.Name - so the audio album shelf is asked for by
      # that, not by anything saying "Audio". A 404 here lists the names this server knows.
      $answer = Get-Json ($BaseUrl + '/api/media/titles?aspect=AlbumItem')
      if ($answer.StatusCode -ne 200) { break }
      $albums = @(($answer.Body | ConvertFrom-Json).titles)
      $album = @($albums | Where-Object { $_.title -eq $AlbumTitle }) | Select-Object -First 1
      if ($null -ne $album) { break }
      Start-Sleep -Milliseconds 500
    }

    if ($answer.StatusCode -ne 200) {
      Write-Fail "GET api/media/titles?aspect=AlbumItem answered $($answer.StatusCode): $($answer.Body)"
      $failures += 'the album shelf was not answerable'
    } else {
      if ($null -eq $album) {
        # An album is not something the reader writes - it is what the Track->Album relationship extractor makes
        # out of the track it claimed, so a missing album means that half of the import never ran.
        Write-Fail "no album '$AlbumTitle' on the shelf (it holds: $(@($albums.title) -join ', '))"
        $failures += 'the tracks never got the album their tags describe'
      } else {
        $albumId = $album.id
        Write-Step "the album shelf holds '$($album.title)' by '$($album.artist)'"
        if ($album.artist -ne $ArtistName) {
          Write-Fail "the album's artist is '$($album.artist)', expected '$($ArtistName)'"
          $failures += "the imported album's artist is wrong"
        }

        # Asking what is inside the album walks the relationship rows rather than a text match, which is the
        # only way this gate can see that the link between the track and its album was actually written.
        $answer = Get-Json ($BaseUrl + "/api/media/titles?aspect=AudioItem&inside=$albumId")
        if ($answer.StatusCode -ne 200) {
          Write-Fail "GET api/media/titles?aspect=AudioItem&inside=$albumId answered $($answer.StatusCode): $($answer.Body)"
          $failures += 'the album could not be opened'
        } else {
          $inside = @(($answer.Body | ConvertFrom-Json).titles)
          if (@($inside | Where-Object { $_.title -eq $TaggedTrackTitle }).Count -eq 0) {
            Write-Fail "the album does not know '$TaggedTrackTitle' (it holds: $(@($inside.title) -join ', '))"
            $failures += 'the track is not linked to its album'
          }
        }
      }
    }
  }

  # -------------------------------------------------------------------------
  # 8c. The cover in the tag has to reach the fanart cache and answer over HTTP
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0 -and $albumId) {
    # Nothing here has to be pressed for the server to fill its cache - a pass also starts by itself when an import
    # ends. Asking is what keeps this gate from depending on that timing, and polling is because a pass waits for
    # the import queue to drain before it walks the library.
    Write-Step 'warming the fanart cache and asking for the album cover'
    $answer = Send-Json 'POST' ($BaseUrl + '/api/media/fanart/warming') $null
    if ($answer.StatusCode -ne 200 -and $answer.StatusCode -ne 202) {
      Write-Fail "POST api/media/fanart/warming answered $($answer.StatusCode): $($answer.Body)"
      $failures += 'the server would not run a fanart pass'
    }

    $cover = $null
    $deadline = (Get-Date).AddSeconds($ImportTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
      $answer = Get-Picture ($BaseUrl + "/api/media/items/$albumId/fanart/Cover")
      if ($answer.StatusCode -eq 200) { $cover = $answer; break }
      if ($answer.StatusCode -ne 404) { break }
      Start-Sleep -Milliseconds 500
    }

    if ($null -eq $cover) {
      Write-Fail "GET api/media/items/$albumId/fanart/Cover never answered with a picture ($($answer.StatusCode) $($answer.Text))"
      $failures += "the album's cover never reached the fanart cache"
    } else {
      $stored = @($cover.Bytes)
      $wanted = @($CoverBytes)
      $different = $stored.Count - $wanted.Count
      if ($different -lt 0) { $different = -$different }
      for ($i = 0; $i -lt [Math]::Min($stored.Count, $wanted.Count); $i++) {
        if ($stored[$i] -ne $wanted[$i]) { $different++ }
      }
      if ($different -ne 0) {
        Write-Fail "the cover the server handed back is not the picture the tag holds ($($stored.Count) vs $($wanted.Count) byte(s), $different different)"
        $failures += "the album's cover is not the picture its tag carries"
      } elseif ($cover.ContentType -notlike 'image/*') {
        Write-Fail "the cover was answered as '$($cover.ContentType)', which no image tile will draw"
        $failures += 'the cover was answered with the wrong content type'
      } else {
        Write-Step "the album wears the front-cover picture from its tag: $($stored.Count) bytes of $($cover.ContentType)"

        # The track is asked next, because a music shelf shows tracks while the import stored exactly one picture,
        # under the album. A 200 here is the Track->Album relationship being walked by the endpoint.
        $answer = Get-Picture ($BaseUrl + "/api/media/items/$($tagged.id)/fanart/Cover")
        if ($answer.StatusCode -ne 200) {
          Write-Fail "GET api/media/items/$($tagged.id)/fanart/Cover answered $($answer.StatusCode): $($answer.Text)"
          $failures += 'a track was not given its album cover'
        } elseif (@($answer.Bytes).Count -ne $stored.Count) {
          Write-Fail "the track's cover is $(@($answer.Bytes).Count) byte(s) while the album's is $($stored.Count)"
          $failures += 'the track was answered with something other than its album cover'
        } else {
          Write-Step 'and the track wears its album cover, through the relationship rather than a second copy'

          # Where those bytes actually live, because "the endpoint answered" alone would still leave open whether
          # the server has a cache at all or is reading the user's file on every request.
          $coverDir = [IO.Path]::Combine($DataDir, 'FanArt', $albumId.ToString().ToUpperInvariant(), 'Cover')
          $pictures = @(Get-ChildItem -Path $coverDir -File -ErrorAction SilentlyContinue |
              Where-Object { '.jpg', '.png', '.tbn' -contains $_.Extension })
          if ($pictures.Count -eq 0) {
            Write-Fail "no picture file under $coverDir, though the endpoint answered with one"
            $failures += 'the cover is not in the fanart cache on disc'
          } else {
            Write-Step "it is stored once for the album: $($pictures[0].Name) in $(Split-Path $coverDir -Parent)"
          }
        }
      }
    }
  }

  # -------------------------------------------------------------------------
  # 9. The queue has to drain
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
  # 10. Taking the folders out again
  # -------------------------------------------------------------------------

  if ($failures.Count -eq 0 -and @($shareIds).Count -gt 0) {
    foreach ($id in $shareIds) {
      Write-Step "removing share $id again"
      $answer = Send-Json 'DELETE' ($BaseUrl + "/api/media/shares/$id") $null
      if ($answer.StatusCode -ne 204) {
        Write-Fail "DELETE api/media/shares/$id answered $($answer.StatusCode): $($answer.Body)"
        $failures += 'a share could not be removed'
      }
    }

    # One list for both shelves rather than two copies of the same check: taking a folder out of a library has
    # to take the titles that came out of it along, whether they came from an .nfo or from a tag. The album is
    # deliberately not in the list - it is a title the importer invented from a track, it has no resource of its
    # own inside the folder, and whether it goes when the folder goes is not settled anywhere yet.
    foreach ($shelf in @(
        @{ Aspect = 'MovieItem'; Title = $MovieTitle },
        @{ Aspect = 'AudioItem'; Title = $TaggedTrackTitle },
        @{ Aspect = 'AudioItem'; Title = $UntaggedTrackTitle })) {
      $answer = Get-Json ($BaseUrl + '/api/media/titles?aspect=' + $shelf.Aspect)
      $titles = @()
      if ($answer.StatusCode -eq 200) { $titles = @(($answer.Body | ConvertFrom-Json).titles) }
      if (@($titles | Where-Object { $_.title -eq $shelf.Title }).Count -ne 0) {
        Write-Fail "'$($shelf.Title)' is still on the $($shelf.Aspect) shelf after its share was removed"
        $failures += 'removing the shares left titles behind'
      }
    }
  }
} finally {
  if ($client) { $client.Dispose() }
}

# ---------------------------------------------------------------------------
# 11. The server itself has to be healthy
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
  Write-Host "PASS: a folder with an .nfo became a title with the year it says and a path a player could open," -ForegroundColor Green
  Write-Host "      a folder with two mp3 files became two tracks, an album and a credited artist," -ForegroundColor Green
  Write-Host "      and the cover inside the tag came back out of the fanart cache over HTTP." -ForegroundColor Green
  Write-Host "      data: $DataDir"
  Write-Host "      media: $MediaDir"
  exit 0
}

Write-Host "FAIL ($($failures.Count)):" -ForegroundColor Red
$failures | ForEach-Object { Write-Host "   - $_" -ForegroundColor Red }
Write-Host "      log: $logFile"
exit 1

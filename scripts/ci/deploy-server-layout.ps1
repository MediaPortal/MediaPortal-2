<#
.SYNOPSIS
  Assembles a directory that can actually run the MP2 server: the headless host plus its plugins.

.DESCRIPTION
  MP2 has no deployment story that survives leaving Windows. Each plugin project carries a
  build.targets that copies its output into $(SolutionDir)..\Bin\$(SolutionName)\$(OutDir)\Plugins\<Name>\,
  but that target is conditional on $(MP2IsSolutionBuild) - it only fires when the whole .sln is
  built with the legacy layout - and the layout it writes is a Windows installer staging folder.
  A portable host needs the same result from plain `dotnet publish`, on any OS, without a solution.

  So the layout is assembled here, from the two things a runnable server needs:

    <OutputDir>\                      the headless host: MP2-Server-Headless.dll, its .config
                                      (which carries the log4net configuration), MediaPortal.Common.dll
                                      and the Defaults\ folder PathManager reads Paths.xml from.
    <OutputDir>\Plugins\<Name>\       one directory per plugin, containing plugin.xml, the plugin
                                      assembly and any native or third-party library it loads.

  The plugin directory contents are copied by explicit rules rather than by "copy everything in the
  build output", because the plugin loader treats anything in the plugin folder as part of the
  plugin, and because the interesting parts (the Microsoft.Data.Sqlite driver assemblies and the
  e_sqlite3 native library, which SQLitePCLRaw looks for next to its own provider assembly) are not
  where a plain publish puts them. The rules mirror Source\Backend\Database\SQLiteDatabase\build.targets.

  What is deployed is a decision: without a plugin registering ISQLDatabase the server comes up, logs
  that it has no plugins, and then dies in DatabaseManager.Startup with "There is no database present
  in the system" - so the database is part of "the server runs", not an optional extra. The second
  plugin, NfoMetadataExtractors, is the smallest thing that makes the server useful rather than merely
  alive: it is a metadata extractor, and without one the server can look at a folder of films and put
  nothing in the library. TV backends, fanart handlers that talk to the internet and the online
  metadata libraries are still absent.

.EXAMPLE
  .\deploy-server-layout.ps1
  # -> build\artifacts\server-layout

.EXAMPLE
  .\deploy-server-layout.ps1 -Configuration Release -RuntimeIdentifier linux-x64
  # Cross-publishes the managed host for Linux. The native SQLite library is taken for the platform
  # the script *runs* on, so a cross-published layout carries the host's native - build the Linux
  # layout on Linux (scripts/ci/test-server-run-linux.ps1 does exactly that inside a container).
#>
[CmdletBinding()]
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',

  [string]$OutputDir,

  # Empty means framework-dependent, portable output - which is what you want on a machine that has
  # the .NET runtime. A RID is only needed for a self-contained deployment.
  [ValidatePattern('^$|^(win-x64|linux-x64)$')]
  [string]$RuntimeIdentifier = '',

  [string]$DotnetPath
)

$ErrorActionPreference = 'Stop'

# Path separators: every path here is assembled with Join-Path from separate components. A literal
# 'MediaPortal\Source' works on Windows and silently becomes a single file *name* containing a
# backslash on Linux, where backslash is not a separator - the same bug PathManager had, in the
# tooling that is supposed to prove the server runs on Linux.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot (Join-Path '..' '..'))).Path
$sourceRoot = Join-Path (Join-Path $repoRoot 'MediaPortal') 'Source'
if (-not $OutputDir) {
  $OutputDir = Join-Path (Join-Path (Join-Path $repoRoot 'build') 'artifacts') 'server-layout'
}

$dotnet = if ($DotnetPath) { $DotnetPath } else { (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet) { throw 'dotnet not found on PATH. Install the .NET SDK, or pass -DotnetPath.' }

function Write-Step([string]$message) { Write-Host "==> $message" -ForegroundColor Cyan }

# Builds one project and returns the directory its assembly landed in.
#
# The directory is asked of MSBuild rather than guessed from bin\<Configuration>\*\: the plugins in
# this tree still have stale net48 and net6.0-windows outputs on disk from before the .NET 10
# retarget, and a glob finds three of them. It is also the only way to learn the target framework
# without hardcoding it, which matters because the SQLite plugin still builds as net10.0-windows
# while the host does not - and the moment that changes, a hardcoded guess here would silently pick
# the wrong folder.
function Publish-PluginProject {
  param(
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][string]$Project
  )

  $projectPath = Join-Path $sourceRoot $Project
  Write-Step "building plugin $Name"
  # Out-Host, not the pipeline: a function returns everything it does not capture, and `dotnet
  # build` writes warnings even at -v quiet. Left in the stream they would be prepended to the
  # directory this function is supposed to return.
  & $dotnet build $projectPath -c $Configuration --nologo -v quiet | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "plugin $Name failed to build (exit $LASTEXITCODE)" }

  # -noconsolelogger keeps the output to the value the CLI prints; without it, MSBuild writes the
  # build's warnings into the same stream (this tree has NU1510 warnings even at -v:q) and the
  # result is a path mixed into log text. One property prints the bare value, several print JSON, so
  # both spellings are handled.
  $propLines = @(& $dotnet msbuild $projectPath -getProperty:TargetDir -p:Configuration=$Configuration -nologo -v:q -noconsolelogger |
    ForEach-Object { "$_".Trim() } | Where-Object { $_ })
  if ($LASTEXITCODE -ne 0) { throw "could not evaluate TargetDir for plugin $Name (exit $LASTEXITCODE)" }
  if ($propLines.Count -eq 0) { throw "MSBuild returned no TargetDir for plugin $Name" }

  $last = $propLines[-1]
  $targetDir = if ($last.StartsWith('{')) { ($last | ConvertFrom-Json).Properties.TargetDir } else { $last }
  if (-not $targetDir) { throw "MSBuild returned no TargetDir for plugin $Name" }

  $assembly = [IO.Path]::Combine($targetDir, "$Name.dll")
  if (-not (Test-Path $assembly)) { throw "built plugin $Name but $assembly does not exist" }

  # Both directories matter: the build output holds the assemblies, but plugin.xml is source, not
  # build output - MP2's own build.targets takes it from $(ProjectDir) too.
  return [pscustomobject]@{
    TargetDir  = $targetDir
    ProjectDir = Split-Path $projectPath -Parent
  }
}

# ---------------------------------------------------------------------------
# 1. The host
# ---------------------------------------------------------------------------

$hostProject = [IO.Path]::Combine($sourceRoot, 'Main', 'MediaPortal.Server.Headless', 'MediaPortal.Server.Headless.csproj')

if (Test-Path $OutputDir) {
  Write-Step "clearing $OutputDir"
  Remove-Item -Path $OutputDir -Recurse -Force
}

Write-Step "publishing the headless server host ($Configuration)"
$publishArgs = @('publish', $hostProject, '-c', $Configuration, '--nologo', '-v', 'quiet', '-o', $OutputDir)
if ($RuntimeIdentifier) { $publishArgs += @('-r', $RuntimeIdentifier, '--self-contained', 'false') }
& $dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "the headless host failed to publish (exit $LASTEXITCODE)" }

# Paths.xml is not optional: without it PathManager knows only the labels it seeds itself, and the
# first GetPath("<LOG>") throws "Unkown path label". The host project copies it to its output, and
# publish carries output content along, so it should be there; asserting it here turns a confusing
# startup failure into a deployment failure that names the missing file.
$pathsXml = [IO.Path]::Combine($OutputDir, 'Defaults', 'Paths.xml')
if (-not (Test-Path $pathsXml)) { throw "Defaults\Paths.xml is missing from $OutputDir" }

# ---------------------------------------------------------------------------
# 2. The plugins a minimal server needs
# ---------------------------------------------------------------------------

# Everything below is the plugin set that lets the server reach the Running state. Media providers,
# metadata extractors and the TV backend are deliberately absent: the server starts without them and
# logs which ones it found.
$pluginName = 'SQLiteDatabase'
$plugin = Publish-PluginProject -Name $pluginName -Project ([IO.Path]::Combine('Backend', 'Database', 'SQLiteDatabase', 'SQLiteDatabase.csproj'))
$pluginTargetDir = $plugin.TargetDir
$pluginDest = [IO.Path]::Combine($OutputDir, 'Plugins', $pluginName)
New-Item -ItemType Directory -Path $pluginDest -Force | Out-Null

Write-Step "deploying $pluginName from $pluginTargetDir"

Copy-Item ([IO.Path]::Combine($pluginTargetDir, "$pluginName.dll")) $pluginDest -Force
Copy-Item ([IO.Path]::Combine($plugin.ProjectDir, 'plugin.xml')) $pluginDest -Force
if ($Configuration -eq 'Debug') {
  $pdb = [IO.Path]::Combine($pluginTargetDir, "$pluginName.pdb")
  if (Test-Path $pdb) { Copy-Item $pdb $pluginDest -Force }
}

# The driver's managed assemblies and its native library. None of these land in a plugin folder by
# itself: they are package dependencies, and the plugin folder is what the plugin loader searches.
foreach ($assembly in 'Microsoft.Data.Sqlite.dll', 'SQLitePCLRaw.batteries_v2.dll', 'SQLitePCLRaw.core.dll', 'SQLitePCLRaw.provider.e_sqlite3.dll') {
  $source = [IO.Path]::Combine($pluginTargetDir, $assembly)
  if (-not (Test-Path $source)) { throw "$assembly was not copied to $pluginTargetDir - the plugin cannot work without it" }
  Copy-Item $source $pluginDest -Force
}

# The native library goes in *flat* next to the plugin, because that is where SQLitePCLRaw looks: it
# probes the directory of SQLitePCLRaw.provider.e_sqlite3.dll. (System.Data.SQLite wanted its
# SQLite.Interop.dll in an x64\ subfolder; that convention is gone.)
#
# Which RID's build to take is asked of .NET, and the answer is kept in variables that are not called
# $IsWindows or $IsMacOS: pwsh defines those names as *read-only* automatic variables, so assigning to
# them fails - and fails only under pwsh, i.e. only inside the Linux container, never on the Windows
# build host with Windows PowerShell 5.1, where the names do not exist.
$osPlatform = [System.Runtime.InteropServices.OSPlatform]
$onWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform($osPlatform::Windows)
$onMac = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform($osPlatform::OSX)

if ($onWindows) {
  $nativeName = 'e_sqlite3.dll'
  $fallbackRids = @('win-x64')
} elseif ($onMac) {
  $nativeName = 'libe_sqlite3.dylib'
  $fallbackRids = @('osx-x64', 'osx-arm64')
} else {
  $nativeName = 'libe_sqlite3.so'
  $fallbackRids = @('linux-x64', 'linux-arm64')
}

# The host's own RID first. `RuntimeInformation.RuntimeIdentifier` knows the C library, which matters
# more than it sounds: the plugin's build output carries a runtimes\ folder for every RID the package
# graph mentions, so guessing costs nothing to write and lands the wrong binary anyway. The first
# measured run of this script in the (glibc, Debian) SDK container took 'linux-musl-x64' because that
# was first in a hand-written list, and the server then died on the native library it could not load.
# The property is .NET Core only, hence the fallback list - Windows PowerShell 5.1 runs on
# .NET Framework, where it does not exist.
$rids = @()
$ridProperty = [System.Runtime.InteropServices.RuntimeInformation].GetProperty('RuntimeIdentifier')
if ($ridProperty) {
  $hostRid = [string]$ridProperty.GetValue($null)
  if ($hostRid) { $rids += $hostRid }
}
$rids += $fallbackRids | Where-Object { $rids -notcontains $_ }

$native = $null
foreach ($rid in $rids) {
  $candidate = [IO.Path]::Combine((Join-Path (Join-Path $pluginTargetDir 'runtimes') $rid), 'native', $nativeName)
  if (Test-Path $candidate) { $native = $candidate; break }
}

if ($native) {
  Copy-Item $native $pluginDest -Force
  Write-Step ("  native SQLite library: {0} ({1})" -f $nativeName, (Split-Path (Split-Path (Split-Path $native -Parent) -Parent) -Leaf))
} else {
  # A missing native is the DllNotFoundException the server dies with at startup, and it is much
  # easier to read here than to find in a container log.
  Write-Warning ("{0} not found under {1}\runtimes\{{{2}}}/native - the plugin will not be able to open a database on this platform." -f `
      $nativeName, $pluginTargetDir, ($rids -join ', '))
}

# ---------------------------------------------------------------------------
# 2b. The metadata extractor that reads what a media folder already holds
# ---------------------------------------------------------------------------

# A server with no metadata extractor can walk a share all day and learn nothing: every file is
# offered to the extractors, none of them answers, and the library stays empty - which is exactly the
# state a freshly deployed server was in, and the reason a shelf full of nothing looked like a
# front-end problem. NfoMetadataExtractors is the extractor that needs nothing installed: it reads the
# .nfo sidecar Kodi and Tiny Media Manager write next to the video, and the poster they leave beside it.
#
# It is also the first plugin in this tree whose Windows target framework was removed rather than
# skipped. It had been -windows only because it referenced the online-metadata library (TMDB, TVDb),
# which uses System.Drawing; two files named that library and used no type from it.
$nfoName = 'NfoMetadataExtractors'
$nfo = Publish-PluginProject -Name $nfoName -Project ([IO.Path]::Combine('Extensions', 'MetadataExtractors', 'NfoMetadataExtractors', 'NfoMetadataExtractors.csproj'))
$nfoTargetDir = $nfo.TargetDir
$nfoDest = [IO.Path]::Combine($OutputDir, 'Plugins', $nfoName)
New-Item -ItemType Directory -Path $nfoDest -Force | Out-Null

Write-Step "deploying $nfoName from $nfoTargetDir"

Copy-Item ([IO.Path]::Combine($nfoTargetDir, "$nfoName.dll")) $nfoDest -Force
Copy-Item ([IO.Path]::Combine($nfo.ProjectDir, 'plugin.xml')) $nfoDest -Force
if ($Configuration -eq 'Debug') {
  $nfoPdb = [IO.Path]::Combine($nfoTargetDir, "$nfoName.pdb")
  if (Test-Path $nfoPdb) { Copy-Item $nfoPdb $nfoDest -Force }
}

# The NFO stub parser is written against Newtonsoft.Json, and plugin.xml asks the loader to load it by
# name from this folder - it used to be reached through the online library's folder above, and that
# dependency is gone. A plugin's assemblies are searched for in the plugin's own folder, so the copy
# belongs here even though the host directory has a Newtonsoft of its own.
$nfoJson = [IO.Path]::Combine($nfoTargetDir, 'Newtonsoft.Json.dll')
if (-not (Test-Path $nfoJson)) {
  throw "Newtonsoft.Json.dll was not copied to $nfoTargetDir - the NFO readers cannot parse a stub without it"
}
Copy-Item $nfoJson $nfoDest -Force

# ---------------------------------------------------------------------------
# 2c. The metadata extractor that reads the tags inside audio files
# ---------------------------------------------------------------------------

# A share of music needs the same kind of help a share of films needed the NFO extractor for, and it
# was missing for the same reason: the extractor that assigns the AudioAspect upstream is the Windows
# AudioMetadataExtractor, which is written against MediaPortal.TagLib (MediaPortal's own feed, .NET
# Framework assemblies only) and probes streams with BASS.NET. Without an extractor that assigns that
# aspect, the importer walks a music share, no extractor claims a single file, and the library ends up
# with zero tracks - and the NFO audio extractor stays quiet too, because it only reads the .nfo of a
# resource an earlier extractor has already called audio.
$audioName = 'AudioTagMetadataExtractor'
$audio = Publish-PluginProject -Name $audioName -Project ([IO.Path]::Combine('Extensions', 'MetadataExtractors', 'AudioTagMetadataExtractor', 'AudioTagMetadataExtractor.csproj'))
$audioTargetDir = $audio.TargetDir
$audioDest = [IO.Path]::Combine($OutputDir, 'Plugins', $audioName)
New-Item -ItemType Directory -Path $audioDest -Force | Out-Null

Write-Step "deploying $audioName from $audioTargetDir"

Copy-Item ([IO.Path]::Combine($audioTargetDir, "$audioName.dll")) $audioDest -Force
Copy-Item ([IO.Path]::Combine($audio.ProjectDir, 'plugin.xml')) $audioDest -Force
if ($Configuration -eq 'Debug') {
  $audioPdb = [IO.Path]::Combine($audioTargetDir, "$audioName.pdb")
  if (Test-Path $audioPdb) { Copy-Item $audioPdb $audioDest -Force }
}

# TagLibSharp is the tag reader, and plugin.xml asks the loader to load it from this folder. A plugin's
# assemblies are searched for in the plugin's own folder, so it has to be copied here even though the
# tag library has nothing to do with the server itself.
$audioTagLib = [IO.Path]::Combine($audioTargetDir, 'TagLibSharp.dll')
if (-not (Test-Path $audioTagLib)) {
  throw "TagLibSharp.dll was not copied to $audioTargetDir - the audio tag extractor cannot read a single tag without it"
}
Copy-Item $audioTagLib $audioDest -Force

# ---------------------------------------------------------------------------
# 3. Report
# ---------------------------------------------------------------------------

Write-Step "layout at $OutputDir"
$dlls = @(Get-ChildItem -Path $OutputDir -Filter '*.dll' -File)
$plugins = @(Get-ChildItem -Path (Join-Path $OutputDir 'Plugins') -Directory -ErrorAction SilentlyContinue)
Write-Host ("  {0} assemblies, {1} plugin(s): {2}" -f $dlls.Count, $plugins.Count, (($plugins | ForEach-Object { $_.Name }) -join ', '))

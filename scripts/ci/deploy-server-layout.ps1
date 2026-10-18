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
  plugin, and because the interesting parts (System.Data.SQLite's native SQLite.Interop.dll, which
  it looks for in an x64\ or x86\ subfolder next to its managed assembly) are not where a plain
  publish puts them. The rules mirror Source\Backend\Database\SQLiteDatabase\build.targets.

  Only what a minimal server needs is deployed: without a plugin registering ISQLDatabase the server
  comes up, logs that it has no plugins, and then dies in DatabaseManager.Startup with "There is no
  database present in the system". That failure is correct behaviour, but it means the plugins are
  part of "the server runs", not an optional extra.

.EXAMPLE
  .\deploy-server-layout.ps1
  # -> build\artifacts\server-layout

.EXAMPLE
  .\deploy-server-layout.ps1 -Configuration Release -RuntimeIdentifier linux-x64
  # Cross-publishes the managed host for Linux. See the warning the script prints for the SQLite
  # native library: the managed System.Data.SQLite package this tree uses ships no Linux native.
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

# System.Data.SQLite: managed wrapper plus a native interop DLL that the wrapper looks for in an
# x64\ or x86\ subdirectory of its own directory. Neither lands in a plugin folder by itself - the
# first is a copied local dependency, the second comes from the NuGet package's runtimes\ folder.
$managed = [IO.Path]::Combine($pluginTargetDir, 'System.Data.SQLite.dll')
if (-not (Test-Path $managed)) { throw "System.Data.SQLite.dll was not copied to $pluginTargetDir" }
Copy-Item $managed $pluginDest -Force

$interop = [IO.Path]::Combine($pluginTargetDir, 'runtimes', 'win-x64', 'native', 'SQLite.Interop.dll')
if (Test-Path $interop) {
  $interopDest = [IO.Path]::Combine($pluginDest, 'x64')
  New-Item -ItemType Directory -Path $interopDest -Force | Out-Null
  Copy-Item $interop $interopDest -Force
} else {
  # True for a Linux layout, and the reason the SQLite provider is still a Windows-only component:
  # the System.Data.SQLite.Core package carries Windows natives only. Saying it here, at the moment
  # somebody builds a Linux layout, beats a DllNotFoundException at three in the morning.
  Write-Warning "SQLite.Interop.dll not found under the plugin output's runtimes folder - the plugin will not be able to open a database on this platform."
}

# ---------------------------------------------------------------------------
# 3. Report
# ---------------------------------------------------------------------------

Write-Step "layout at $OutputDir"
$dlls = @(Get-ChildItem -Path $OutputDir -Filter '*.dll' -File)
$plugins = @(Get-ChildItem -Path (Join-Path $OutputDir 'Plugins') -Directory -ErrorAction SilentlyContinue)
Write-Host ("  {0} assemblies, {1} plugin(s): {2}" -f $dlls.Count, $plugins.Count, (($plugins | ForEach-Object { $_.Name }) -join ', '))

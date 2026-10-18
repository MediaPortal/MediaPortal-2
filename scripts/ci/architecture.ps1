<#
.SYNOPSIS
  Checks that everything MP2 actually builds is 64-bit, from the binaries rather than from the
  project files.

.DESCRIPTION
  The port is x64-only (docs/multiplatform/ROADMAP.md). That decision is easy to state and easy to
  lose, because in this tree it was never held by the obvious-looking declaration:

  * <PlatformTarget>x86</PlatformTarget> appeared in 22 project files but no solution built 32-bit
    except MP2-ServiceMonitor.sln, which mapped its x86 and Test platforms straight to project
    configurations that set it.
  * <Prefer32Bit>true</Prefer32Bit> on an AnyCPU executable is what really produced a 32-bit process
    on .NET Framework, and MediaPortal.Client and MediaPortal.Server both carried it while never
    mentioning x86. On .NET Core and later the flag is ignored and bitness comes from the apphost RID,
    so a retarget silently changed the answer - in both directions.

  So this script does not ask what the project files claim. It reads the produced images:

  * apphost executables: the PE Machine field (0x8664 = amd64, 0x14c = i386, 0xaa64 = arm64).
  * managed assemblies: AssemblyName.ProcessorArchitecture, which is what <PlatformTarget> writes
    into the assembly, so a project that quietly reintroduces x86 shows up here even if no apphost is
    rebuilt from it.

  What it cannot see, and does not claim to: a 32-bit *native* dependency loaded at runtime, and the
  bitness of the WiX installer chain, which is a property of burn.exe and the bundle rather than of any
  managed image (see the installer section below).

  A source declaration only fails the check if it is in an SDK-style project, i.e. one that really
  builds on net10.0. 42 declarations sit in the old-style (pre-SDK) tools and database backends, which
  cannot produce a net10.0 output at all; they are listed as the purge list instead of as violations,
  because 42 unavoidable lines would bury the one line that would matter.

  Runs from build.ps1 -Ratchet, after the builds, because it reads their output.

.PARAMETER Configuration
  Which build output to inspect. Default 'Debug', matching build.ps1.

.PARAMETER BinRoot
  Root of the build output. Default 'MediaPortal\Bin', which is where every project in the tree writes
  because they set OutputPath themselves.

.EXAMPLE
  .\scripts\ci\architecture.ps1

.EXAMPLE
  .\scripts\ci\architecture.ps1 -Configuration Release
#>

[CmdletBinding()]
param(
  [string]$Configuration = 'Debug',

  [string]$BinRoot,

  # Exit 0 even when something is wrong, for use while the number is being argued about.
  [switch]$ReportOnly
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $BinRoot) { $BinRoot = Join-Path $root 'MediaPortal\Bin' }

function Get-PeArchitecture {
  <#
  .SYNOPSIS
    Reads the Machine field from the COFF file header of a PE image.
  .DESCRIPTION
    e_lfanew at 0x3C points at the "PE\0\0" signature; the two bytes straight after it are the Machine.
    Done by hand because PowerShell 5.1 cannot load a .NET 10 assembly and System.Reflection does not
    expose Machine anyway.
  #>
  param([string]$Path)

  $stream = [IO.File]::OpenRead($Path)
  try {
    $reader = New-Object IO.BinaryReader($stream)
    if ($reader.ReadUInt16() -ne 0x5A4D) { return 'not a PE image' }   # MZ
    $stream.Position = 0x3C
    $peOffset = $reader.ReadInt32()
    $stream.Position = $peOffset
    if ($reader.ReadUInt32() -ne 0x00004550) { return 'not a PE image' } # "PE\0\0"
    switch ($reader.ReadUInt16()) {
      0x014c { 'x86' }
      0x8664 { 'x64' }
      0xaa64 { 'arm64' }
      default { 'unknown' }
    }
  } finally { $stream.Dispose() }
}

function Get-ManagedArchitecture {
  <#
  .SYNOPSIS
    AssemblyName.ProcessorArchitecture of a managed assembly, or $null if the file is not one.
  .DESCRIPTION
    Reads metadata only, so it does not execute or load the target. MSIL means AnyCPU, X86 means the
    project was compiled with <PlatformTarget>x86</PlatformTarget>, Amd64 means x64.
  #>
  param([string]$Path)
  try {
    return [Reflection.AssemblyName]::GetAssemblyName($Path).ProcessorArchitecture.ToString()
  } catch {
    return $null
  }
}

"architecture.ps1 - configuration '$Configuration'"
"binaries under: $BinRoot"
""

if (-not [IO.Directory]::Exists($BinRoot)) {
  Write-Host "FAIL: no build output at $BinRoot. Run build.ps1 first - an empty directory is not a pass." -ForegroundColor Red
  exit 1
}

# Anything that is an apphost has a managed dll of the same name next to it; that filter drops the
# third-party native dlls and the resource-only exes that were copied in, none of which MP2 compiles.
$apphosts = @(Get-ChildItem $BinRoot -Recurse -Filter '*.exe' -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -match [regex]::Escape("\$Configuration\") -or $_.FullName -match [regex]::Escape("/$Configuration/") } |
  Where-Object { Test-Path (Join-Path $_.DirectoryName ($_.BaseName + '.dll')) })

if ($apphosts.Count -eq 0) {
  Write-Host "FAIL: no apphost executables found for '$Configuration'. That is a build that did not happen, not a clean tree." -ForegroundColor Red
  exit 1
}

$violations = @()

"{0,-34} {1,-10} {2,-10} {3,-20} {4}" -f 'executable', 'apphost', 'managed', 'written', 'location'
"{0,-34} {1,-10} {2,-10} {3,-20} {4}" -f '----------------------------------', '----------', '----------', '--------------------', '--------'

# Newest write per file name; anything older than that under the same name is a leftover.
$newest = @{}
foreach ($apphost in $apphosts) {
  if (-not $newest.ContainsKey($apphost.Name) -or $apphost.LastWriteTime -gt $newest[$apphost.Name]) {
    $newest[$apphost.Name] = $apphost.LastWriteTime
  }
}

foreach ($apphost in ($apphosts | Sort-Object Name)) {
  $pe = Get-PeArchitecture $apphost.FullName
  $dll = Join-Path $apphost.DirectoryName ($apphost.BaseName + '.dll')
  $managed = Get-ManagedArchitecture $dll

  # The timestamp is part of the verdict, not decoration: output directories of target frameworks a
  # project no longer builds survive every rebuild, so a leftover 'bin\Debug\net48' from before the
  # retarget sits next to the real one. When one name appears twice the newest write is the build;
  # the others are marked, because a row from an old build read as a statement about today's source
  # is a wrong statement. A marked row is still counted if it is 32-bit - dead x86 output in the
  # binary tree should be deleted, not explained.
  $relative = $apphost.DirectoryName.Replace($BinRoot, '').TrimStart('\', '/')
  $written = $apphost.LastWriteTime.ToString('yyyy-MM-dd HH:mm')
  if ($apphost.LastWriteTime -lt $newest[$apphost.Name]) { $written += ' stale' }
  "{0,-34} {1,-10} {2,-10} {3,-20} {4}" -f $apphost.Name, $pe, $managed, $written, $relative

  if ($pe -ne 'x64') {
    $violations += "$($apphost.Name): apphost PE Machine is $pe, not x64 ($relative)"
  }
  if ($managed -eq 'X86') {
    $violations += "$($apphost.BaseName).dll: compiled with PlatformTarget=x86 ($relative)"
  }
}

""

# Which projects any solution in the tree actually builds. A declaration in a project no solution
# references is dead weight, not a violation, and saying so is the difference between a gate and noise:
# 20 old-style projects still carry <PlatformTarget>x86</PlatformTarget> and none of them can produce a
# net10.0 output at all.
$built = @{}
foreach ($sln in (Get-ChildItem $root -Recurse -Include '*.sln' -ErrorAction SilentlyContinue)) {
  foreach ($m in [regex]::Matches([IO.File]::ReadAllText($sln.FullName), '(?m)^Project\([^\)]*\)\s*=\s*"[^"]*",\s*"([^"]+)"')) {
    $target = [IO.Path]::GetFullPath([IO.Path]::Combine($sln.DirectoryName, ($m.Groups[1].Value -replace '\\', '\')))
    $built[$target] = $sln.Name
  }
}

# Source-level declarations. These are not the truth - the binaries above are - but a declaration that
# is being ignored is one rebuild away from being honoured again, and on a .NET Framework leg it was.
function Remove-XmlComments {
  <#
  Blanks out <!-- ... --> runs while keeping the line count, so a match keeps reporting the line the
  editor will show. Comment text about a removed setting used to be reported as a declaration, which is
  a bad way to teach somebody that the gate is lying.

  Written as an explicit loop over the matches rather than [regex]::Replace with a script block as the
  MatchEvaluator: the script-block-as-delegate conversion is where this script died when build.ps1 ran
  it ("the property 'Value' was not found on this object"), and the loop is readable enough that losing
  the delegate costs nothing.
  #>
  param([string]$Text)

  $builder = New-Object Text.StringBuilder($Text.Length)
  $position = 0
  foreach ($match in [regex]::Matches($Text, '<!--.*?-->', 'Singleline')) {
    [void]$builder.Append($Text.Substring($position, $match.Index - $position))
    $newlines = ([regex]::Matches($match.Value, "`n")).Count
    if ($newlines -gt 0) { [void]$builder.Append("`n" * $newlines) }
    $position = $match.Index + $match.Length
  }
  [void]$builder.Append($Text.Substring($position))
  return $builder.ToString()
}

$scanned = @(Get-ChildItem (Join-Path $root 'MediaPortal') -Recurse -Include '*.csproj', '*.props', '*.targets' -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\|\\packages\\' })

$declarations = @()
foreach ($file in $scanned) {
  $raw = [IO.File]::ReadAllText($file.FullName, [Text.Encoding]::UTF8)
  $text = Remove-XmlComments $raw
  $lines = $text -split "`r?`n"
  # An old-style (non-SDK) project cannot produce a net10.0 output at all, so its PlatformTarget is
  # unreachable rather than wrong. The distinction is measurable, so it is worth making: 20 of these
  # projects are the pre-SDK tools and database backends, and calling their leftovers violations would
  # put the real thing - a shipping project that builds 32-bit - under 42 lines of noise.
  $isSdk = $raw -match '<Project\s+Sdk='
  for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '<PlatformTarget>\s*x86\s*<|<Prefer32Bit>\s*true\s*<') {
      $declarations += [pscustomobject]@{
        File    = $file.FullName.Replace($root + '\', '')
        Line    = $i + 1
        What    = $lines[$i].Trim()
        Sdk     = $isSdk
        BuiltBy = if ($built.ContainsKey($file.FullName)) { $built[$file.FullName] } else { $null }
      }
    }
  }
}

$live = @($declarations | Where-Object { $_.Sdk })
$dead = @($declarations | Where-Object { -not $_.Sdk })

"declarations that ask for 32-bit (scanned $($scanned.Count) project files outside bin/obj):"
"  in SDK-style projects, which build on net10.0 and would really be 32-bit: $($live.Count)"
foreach ($d in ($live | Sort-Object File, Line)) {
  $by = if ($d.BuiltBy) { "built by $($d.BuiltBy)" } else { "no solution references it" }
  "    {0}:{1}  [{2}]  {3}" -f $d.File, $d.Line, $by, $d.What
}
"  in old-style projects, which cannot produce a net10.0 output and are reported for the purge list: $($dead.Count)"
foreach ($d in ($dead | Sort-Object File, Line | Select-Object -First 4)) {
  "    {0}:{1}  {2}" -f $d.File, $d.Line, $d.What
}
if ($dead.Count -gt 4) { "    ... and $($dead.Count - 4) more in the same category" }
$violations += @($live | ForEach-Object { "$($_.File):$($_.Line) declares 32-bit in a project that builds on net10.0" })
""

# The installer is deliberately not judged here. MP2BootstrapperApp carries RuntimeIdentifier win-x86
# and the four WiX projects in MP2-Setup.sln map their x86 platform to a real x86 build, but a managed
# bootstrapper application is loaded by burn.exe and has to match its bitness, so the host RID cannot
# move to win-x64 on its own - it moves with the bundle, in one change, verified by running the setup on
# a machine with WiX installed. Recorded rather than silently allowed.
$installerChain = @(
  [pscustomobject]@{ Item = 'MediaPortal\Setup\MP2BootstrapperApp\MP2BootstrapperApp.csproj'; Why = 'RuntimeIdentifier win-x86' }
  [pscustomobject]@{ Item = 'MediaPortal\Setup\Bootstrapper\*.wixproj'; Why = 'x86 solution platform resolves to a real x86 build' }
)
"installer chain, open and intentionally not gated here:"
foreach ($i in $installerChain) { "  {0}  ({1})" -f $i.Item, $i.Why }
"  bitness of a burn bundle belongs to burn.exe; the managed bootstrapper application must match it,"
"  so this is one change with the packaging, not a property edit."
""

if ($violations.Count -gt 0) {
  foreach ($v in $violations) { Write-Host "FAIL: $v" -ForegroundColor Red }
  if ($ReportOnly) { Write-Host "(ReportOnly: not failing)"; exit 0 }
  exit 1
}

"OK: all $($apphosts.Count) built executables are x64."
exit 0

<#
.SYNOPSIS
  Fails when a source file imports a namespace that exists ONLY in a *.Windows satellite while its
  project does not reference that satellite.

.DESCRIPTION
  Moving code to a satellite keeps its namespace, which is what makes the move cheap: consumers
  usually need only an assembly reference. The cost is that a namespace can disappear from an
  assembly without any call site changing, because a stale using-directive is itself a compile
  dependency. That was found the hard way here: MediaPortal.ServiceMonitor still had
  "using MediaPortal.Common.UI;" whose only consumer was a commented-out line, so moving
  FormDpiAwareness.cs out of MediaPortal.Common broke a project that never called the API - and
  no solution being built here would have noticed, because ServiceMonitor lives in a third one.

  A namespace is only evidence of a dependency when the satellite is its sole declaration site.
  MediaPortal.Common.Async for instance is declared by both MediaPortal.Common and
  MediaPortal.Common.Windows, so importing it proves nothing; the check therefore collects every
  declaration in the tree first and only reports namespaces that no other project declares.

  The point is coverage without building: this reads sources, so it also sees the projects whose
  solutions currently do not restore.

.EXAMPLE
  .\satellites.ps1
#>
[CmdletBinding()]
param(
  [switch]$AsJson
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$coreRoot = Join-Path $repoRoot 'MediaPortal\Source\Core'
$scanRoots = @('MediaPortal\Source', 'MediaPortal\Extensions', 'MediaPortal\Tests', 'MediaPortal\Incubator') |
  ForEach-Object { Join-Path $repoRoot $_ } | Where-Object { Test-Path $_ }

function Get-CodeOnlyText {
<#
    Strips comments so that prose mentioning a namespace is not mistaken for a declaration. Same
    heuristic as spine.ps1: drop /* */ blocks, then // to end of line, keeping URIs intact.
#>
  [CmdletBinding()]
  param([Parameter(Mandatory)][string] $Path)

  $text = [System.IO.File]::ReadAllText($Path)
  $noBlocks = [regex]::Replace($text, '/\*[\s\S]*?\*/', ' ')
  return [regex]::Replace($noBlocks, '(?<!:)//[^\r\n]*', ' ')
}

# A file belongs to its nearest enclosing project; that is the assembly the compiler puts it in.
$projectCache = @{}
function Get-OwningProject {
  [CmdletBinding()]
  param([Parameter(Mandatory)][System.IO.FileInfo] $File)

  $key = $File.DirectoryName
  if ($projectCache.ContainsKey($key)) { return $projectCache[$key] }

  $dir = $File.Directory
  $found = $null
  while ($dir) {
    $candidate = Get-ChildItem $dir.FullName -Filter *.csproj -File | Select-Object -First 1
    if ($candidate) { $found = $candidate; break }
    $dir = $dir.Parent
  }
  $projectCache[$key] = $found
  return $found
}

$satelliteDirs = Get-ChildItem $coreRoot -Directory -Filter '*.Windows'
$satelliteNames = @($satelliteDirs | ForEach-Object { $_.Name })

# Pass 1: what does every file declare and import?
$declaredBy = @{}   # namespace -> set of project names declaring it
$imports = @()      # file/project pairs to evaluate in pass 2

foreach ($root in $scanRoots) {
  foreach ($file in (Get-ChildItem $root -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj|Packages|packages|\.vs|Generated)\\' })) {
    $proj = Get-OwningProject -File $file
    $projName = if ($proj) { $proj.BaseName } else { "<no project: $($file.DirectoryName)>" }
    $text = Get-CodeOnlyText -Path $file.FullName

    foreach ($m in [regex]::Matches($text, '(?m)^\s*namespace\s+([A-Za-z_][\w.]*)')) {
      $ns = $m.Groups[1].Value
      if (-not $declaredBy.ContainsKey($ns)) { $declaredBy[$ns] = @{} }
      $declaredBy[$ns][$projName] = $true
    }

    foreach ($u in [regex]::Matches($text, '(?m)^\s*using\s+(?:static\s+)?([A-Za-z_][\w.]*)\s*;')) {
      $imports += [pscustomobject]@{ File = $file; Project = $proj; ProjectName = $projName; Namespace = $u.Groups[1].Value }
    }
  }
}

# Satellite namespaces with no second declaration site anywhere in the tree.
$satelliteExclusive = @{}
foreach ($dir in $satelliteDirs) {
  $csproj = Get-ChildItem $dir.FullName -Filter *.csproj -File | Select-Object -First 1
  if (-not $csproj) { continue }
  foreach ($ns in $declaredBy.Keys) {
    $owners = @($declaredBy[$ns].Keys)
    if ($owners.Count -eq 1 -and $owners[0] -eq $csproj.BaseName) { $satelliteExclusive[$ns] = $csproj }
  }
}

$violations = @()
$satisfied = 0
foreach ($imp in $imports) {
  if (-not $satelliteExclusive.ContainsKey($imp.Namespace)) { continue }
  $csproj = $satelliteExclusive[$imp.Namespace]
  if ($imp.ProjectName -eq $csproj.BaseName) { continue }
  if (-not $imp.Project) {
    $violations += [pscustomobject]@{ File = $imp.File.FullName.Substring($repoRoot.Length + 1); Reason = "imports $($imp.Namespace) (owned by $($csproj.Name)) but the file belongs to no project" }
    continue
  }
  $projText = [System.IO.File]::ReadAllText($imp.Project.FullName)
  if ($projText -match [regex]::Escape($csproj.Name)) { $satisfied++ }
  else {
    $violations += [pscustomobject]@{
      File   = $imp.File.FullName.Substring($repoRoot.Length + 1)
      Reason = "imports $($imp.Namespace) but $($imp.Project.Name) has no ProjectReference to $($csproj.Name)"
    }
  }
}

if ($AsJson) {
  ConvertTo-Json -InputObject @{ exclusive = @($satelliteExclusive.Keys); violations = $violations } -Depth 4
  if ($violations.Count -gt 0) { exit 1 }
  exit 0
}

Write-Host "namespaces owned exclusively by a Windows satellite:" -ForegroundColor Cyan
foreach ($ns in ($satelliteExclusive.Keys | Sort-Object)) { "  {0,-34} {1}" -f $ns, $satelliteExclusive[$ns].Name }
Write-Host "imports satisfied by a ProjectReference: $satisfied" -ForegroundColor DarkGray

if ($violations.Count -eq 0) {
  Write-Host "Satellite references OK." -ForegroundColor Green
  exit 0
}

Write-Host ""
Write-Host "Files that reach into a Windows satellite without their project referencing it:" -ForegroundColor Red
foreach ($v in $violations) { "  {0}`n      {1}" -f $v.File, $v.Reason }
Write-Host ""
Write-Host "Add the ProjectReference, or delete the using-directive if the types are no longer used." -ForegroundColor Yellow
exit 1

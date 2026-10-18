<#
.SYNOPSIS
  Deterministic package health report: which packages carry known advisories, which are only
  reachable through the .NET Framework fallback, and which references the SDK now supplies itself.

.DESCRIPTION
  The warning ratchet deliberately does NOT gate NuGet's restore-time codes. They are emitted into the
  build log by whichever project happens to re-evaluate its assets, so the same source scored 778 and
  then 776 findings on two consecutive runs with identical options (NU1510 21/20, NU1902 50/49). A
  floor that jitters makes the gate red at random, and a gate that is red at random gets ignored.

  The information itself is valuable, so it is read from the place where NuGet stores it as data
  instead of as log prose: the "logs" array of each obj\project.assets.json. Those files are the
  output of restore, they are per project, and they do not depend on the build order or on how many
  nodes MSBuild used.

  Locale: the message text is localized ("Das Paket „log4net“ …" vs "Package 'log4net' …"), so it is
  never a join key. What is read from it is the quoted package id (any kind of quotation mark), the
  version, and the advisory URL; the code and the project path come from structured fields. Severity
  is taken from the code itself, which is what NuGet uses to classify it: NU1901 low, NU1902 moderate,
  NU1903 high.

  Assets files are only as fresh as the last restore, and a tree that has been retargeted can still
  contain leftovers from the old target frameworks. A file is considered only if it actually describes
  a net10.0 target, so a stale pre-retarget file cannot inflate a number.

.EXAMPLE
  .\packages.ps1
  .\packages.ps1 -FailOn High     # for a pipeline that must not ship a high-severity advisory
#>
[CmdletBinding()]
param(
  [Parameter(ValueFromPipeline)][string[]]$Root,

  # Exit 1 when at least one package with this severity (or worse) is present. An advisory whose code
  # this script does not know is ranked as High, so -FailOn High catches it.
  [ValidateSet('None', 'Low', 'Moderate', 'High', 'Critical')]
  [string]$FailOn = 'None',

  [ValidateSet('table', 'json')]
  [string]$Format = 'table'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$roots = if ($Root) { $Root } else { @($repoRoot) }

# NuGet's audit codes and the severity each one means. NU1904 = critical was learned from the assets
# files themselves ("...weist eine bekannte Kritisch Schweregrad-Sicherheitsanfälligkeit auf"), after
# an earlier version of this script listed only NU1901..NU1903 and therefore dropped a real critical
# advisory (System.Drawing.Common 5.0.0, GHSA-rxg9-xrhp-64gj) in silence. Anything in the NU19xx
# family is now reported even if its severity is unknown here - see the advisoryRe below.
$severityByCode = @{ NU1901 = 'Low'; NU1902 = 'Moderate'; NU1903 = 'High'; NU1904 = 'Critical' }
$advisoryRe = [regex]'^NU19\d\d$'
$interesting = @($severityByCode.Keys) + @('NU1701', 'NU1510', 'NU1504')

# Package ids are quoted in every locale NuGet ships, but not with the same characters: ASCII quotes
# in the assets files as written here, typographic quotes in some localisations, and sometimes not at
# all ("PackageReference Microsoft.CSharp wird nicht gekürzt" - NuGet leaves that term untranslated).
# The shapes seen in this tree:
#   NU19xx  Das Paket "log4net" 2.0.14 weist ...    /  Package 'log4net' 2.0.14 has a known ...
#   NU1701  Das Paket "BASS.NET 2.4.14.1" wurde ...  -> id AND version inside one quoted token
#   NU1510  PackageReference Microsoft.CSharp wird nicht gekürzt  -> unquoted, term untranslated
# Nothing here keys on prose, only on quoting and on the invariant word "PackageReference".
$quoteChars = '\u201E\u201C\u201F"\u2018\u2019\u00AB\u00BB\u2039\u203A'''
$quotedRe   = [regex]("[$quoteChars](?<t>[^$quoteChars]+?)[$quoteChars]")
$pkgRefRe   = [regex]('(?i)package\s*reference\s*[' + $quoteChars + ']?(?<id>[A-Za-z0-9_.\-]+)')
$versionRe  = [regex]'(?<v>\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z\.\-]+)?)'
$urlRe      = [regex]'(?<u>https?://[^\s]+?)(?=[\s"”\u201C\u201D]|$)'

function Get-PackageFromMessage {
  param([string]$Code, [string]$Message)
  $id = $null
  $ver = $null
  foreach ($m in $quotedRe.Matches($Message)) {
    $token = $m.Groups['t'].Value.Trim()
    # "BASS.NET 2.4.14.1" - NU1701 puts both in one token.
    $parts = $token -split '\s+', 2
    $candidate = $parts[0]
    if ($candidate -match '^(net\d|\.NETFramework)') { continue }   # the target framework, not a package
    $id = $candidate
    if ($parts.Count -eq 2 -and $parts[1] -match '^\d') {
      $ver = $parts[1]
    } else {
      $vm = $versionRe.Match($Message, $m.Index + $m.Length)
      if ($vm.Success) { $ver = $vm.Groups['v'].Value }
    }
    break
  }
  if (-not $id) {
    $pm = $pkgRefRe.Match($Message)
    if ($pm.Success) { $id = $pm.Groups['id'].Value }
  }
  return @{ id = $id; version = $ver }
}

$perProject = @{}
$unknownCodes = [System.Collections.Generic.HashSet[string]]::new()
$assetsFiles = 0
$considered = 0

foreach ($r in $roots) {
  $full = (Resolve-Path $r).ProviderPath
  foreach ($f in Get-ChildItem $full -Recurse -Filter 'project.assets.json' -File |
                 Where-Object { $_.FullName -notmatch '\\(bin|obj\\.*\\obj|node_modules|packages)\\' }) {
    $assetsFiles++
    # Read as UTF-8 explicitly: these files have no BOM, and PowerShell 5.1 would otherwise decode
    # them as ANSI. That is not cosmetic - the message text quotes package ids with typographic
    # quotation marks in the German localisation, and a mangled quote is a package id that cannot be
    # found. The code and project path are structured fields and would have survived; the extraction
    # below would silently have produced "(no package id in message)".
    try { $doc = [System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8) | ConvertFrom-Json } catch { continue }

    $targets = @($doc.targets.PSObject.Properties.Name)
    if (-not ($targets | Where-Object { $_ -like 'net10.0*' })) { continue }
    $considered++

    $projPath = $doc.project.restore.projectPath
    if (-not $projPath) { $projPath = (Split-Path (Split-Path $f.FullName -Parent) -Parent) }
    $projName = [IO.Path]::GetFileNameWithoutExtension($projPath)

    foreach ($entry in @($doc.logs)) {
      if (-not $entry.code) { continue }
      if (($interesting -notcontains $entry.code) -and -not $advisoryRe.IsMatch($entry.code)) { continue }
      $msgUrl = $null
      $msgId = $null
      $msgVer = $null
      $found = Get-PackageFromMessage -Code $entry.code -Message $entry.message
      $msgId = $found.id
      $msgVer = $found.version
      $um = $urlRe.Match($entry.message)
      if ($um.Success) { $msgUrl = $um.Groups['u'].Value.TrimEnd('.') }

      if (-not $msgId) { $msgId = '(no package id in message)' }
      if (-not $msgVer) { $msgVer = '-' }
      $sev = '-'
      if ($severityByCode.ContainsKey($entry.code)) { $sev = $severityByCode[$entry.code] }
      elseif ($advisoryRe.IsMatch($entry.code)) {
        # A severity NuGet invented after this script was written. Named so a reader cannot mistake it
        # for "no severity", and ranked as High by -FailOn so an unknown advisory still stops a build.
        $sev = 'Advisory'
        [void]$unknownCodes.Add($entry.code)
      }

      $key = $entry.code + '|' + $msgId + '|' + $msgVer
      if (-not $perProject.ContainsKey($key)) {
        $perProject[$key] = [pscustomobject]@{
          Code = $entry.code
          Severity = $sev
          Package = $msgId
          Version = $msgVer
          Url = $msgUrl
          Projects = [System.Collections.Generic.HashSet[string]]::new()
        }
      }
      [void]$perProject[$key].Projects.Add($projName)
    }
  }
}

$rows = @($perProject.Values |
  Sort-Object @{E = { $_.Code } }, @{E = { -$_.Projects.Count } }, Package)

if ($Format -eq 'json') {
  # Emit, do not Write-Host. -Format json exists to be piped into ConvertFrom-Json or Out-File, and
  # Write-Host writes to the host rather than to the success stream: an earlier version of this script
  # produced an empty file for the caller, which then read exactly like "no advisories found".
  $rows | ForEach-Object {
    [pscustomobject]@{
      code = $_.Code; severity = $_.Severity; package = $_.Package; version = $_.Version
      url = $_.Url; projectCount = $_.Projects.Count; projects = @($_.Projects | Sort-Object)
    }
  } | ConvertTo-Json -Depth 4
} else {
  if ($unknownCodes.Count) {
    Write-Host "unknown audit codes treated as advisories (severity unknown): $($unknownCodes -join ', ')" -ForegroundColor Yellow
  }
  Write-Host "assets files scanned: $assetsFiles, describing a net10.0 target: $considered"
  if (-not $rows.Count) {
    Write-Host "Nothing to report - no NuGet restore diagnostics in the current assets files." -ForegroundColor Green
    exit 0
  }
  Write-Host ""
  foreach ($code in ($rows.Code | Sort-Object -Unique)) {
    $set = @($rows | Where-Object Code -eq $code)
    $projects = @($set.Projects | ForEach-Object { @($_) } | Sort-Object -Unique)
    Write-Host "$code  packages: $($set.Count)  projects touched: $($projects.Count)" -ForegroundColor Cyan
    $set | Select-Object Severity, Package, Version,
              @{n = 'Projects'; e = { $_.Projects.Count }},
              @{n = 'Advisory'; e = { if ($_.Url) { ($_.Url -split '/')[-1] } else { '' } }} |
      Format-Table -AutoSize | Out-String -Width 160 | Write-Host
  }
}

if ($FailOn -ne 'None') {
  $rank = @{ Low = 1; Moderate = 2; High = 3; Critical = 4; Advisory = 3 }
  $limit = $rank[$FailOn]
  foreach ($row in $rows) {
    $rowRank = $rank[$row.Severity]
    if ($rowRank -and $rowRank -ge $limit) {
      Write-Host "FAIL: $($row.Package) $($row.Version) carries a $($row.Severity) advisory ($($row.Url))" -ForegroundColor Red
      exit 1
    }
  }
}
exit 0

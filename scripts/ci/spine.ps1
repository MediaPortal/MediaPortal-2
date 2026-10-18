<#
.SYNOPSIS
  Counts the Windows-only "spine" in the portable core, and fails when it grows.

.DESCRIPTION
  Companion to warnings.ps1, because that one cannot see the whole problem.

  CA1416 only fires for Windows-only APIs that arrive from NuGet packages
  (System.Drawing.Common, System.Management, Microsoft.Win32.Registry). Types that the
  -windows target framework itself supplies - System.Windows.Forms, WPF - are considered
  Windows by construction, so calls to them produce no diagnostic at all. Removing three
  WinForms call sites from MediaPortal.Common changed CA1416 by exactly zero.

  So the portable core needs a second, deliberately crude metric: how many references to
  System.Windows.Forms and to Windows-only System.Drawing types are still compiled into the
  assemblies that must run on Linux. It cannot tell whether a specific call works, but it can
  only go down, and it is the number that has to reach zero before those assemblies can lose
  their -windows target.

  "System.Drawing" is not itself a Windows dependency, and treating it as one produced a false
  entry in the must-reach-zero list. It is two assemblies: System.Drawing.Primitives (Color,
  SizeF, Rectangle, ...) ships with the shared framework and was measured running on net8.0 in a
  Linux container, while System.Drawing.Common (Image, Bitmap, Graphics, ImageFormat, ...) is
  Windows-only and on net8.0 does not even compile without a package reference. Only the second
  one is counted as spine; the first is reported next to it for context. See Measure-DrawingRefs.

  A third category is counted because both other metrics are structurally blind to it. A
  [DllImport] declaration produces no CA1416 diagnostic - the analyzer reasons about types in
  referenced assemblies, and a DLL name is a string - and it is neither WinForms nor GDI+, so it
  appeared in none of the columns above. When the count was taken the "portable core" read 0 spine
  references while containing 95 P/Invoke declarations (MediaPortal.Utilities 74, MediaPortal.Common
  21, MediaPortal.Backend 0), e.g. NativeMethods.cs alone declares 43. A number that reads zero while
  Kernel32 is being loaded is not a portability measurement, so P/Invoke is gated like the others.

  Two ledger sections are counted:

    core       Common, Utilities, Backend - code that has to become portable. May only shrink.
    satellites *.Windows companion assemblies. Code moved here is quarantined, NOT fixed.
               They need their own column because a satellite declares
               [SupportedOSPlatform("windows")], which makes CA1416 treat its own call sites as
               platform-neutral: moving code there removes it from the CA1416 count without
               removing a single Windows dependency. Measured, not theorised - extracting
               SplashScreen.cs moved CA1416 by -20 while changing no behaviour at all.

  The two are gated as core-must-not-grow and core+satellites-must-not-grow, not each on its own.
  Gating a satellite's own total would forbid the operation this port consists of: taking Windows
  code out of the core and putting it where it belongs, which shrinks one and grows the other by
  the same amount. Relocating is allowed; adding is not.

  Counts are textual references in code (comments stripped). Crude on purpose: a grep cannot
  miss a namespace, whereas an analyzer can be silenced or satisfied by an annotation.

.EXAMPLE
  .\spine.ps1
  .\spine.ps1 -UpdateBaseline
#>
[CmdletBinding()]
param(
  [string]$BaselinePath,
  [switch]$UpdateBaseline
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CodeOnlyText {
<#
    Strips comments before counting. A comment that merely mentions a namespace is not a compile
    dependency, and counting one would put the floor out of sync with reality and train everyone to
    ignore this report. Heuristic: drop /* ... */ blocks, then drop // ... to end of line, but not
    when the slashes follow a colon so that URIs inside string literals survive.
#>
  [CmdletBinding()]
  param([Parameter(Mandatory)][string] $Text)

  $noBlocks = [regex]::Replace($Text, '/\*[\s\S]*?\*/', ' ')
  return [regex]::Replace($noBlocks, '(?<!:)//[^\r\n]*', ' ')
}

function Measure-DrawingRefs {
<#
    Splits the System.Drawing references in one comment-stripped file into the two kinds that
    have opposite portability. Counts are textual on purpose (same as the rest of this script),
    but "System.Drawing" alone is not a Windows dependency:

      System.Drawing.Primitives.dll  Color, KnownColor, Point/PointF, Size/SizeF,
                                     Rectangle/RectangleF, ContentAlignment
          Ships with the shared framework. Measured in a Linux container on net8.0: compiles and
          runs (Color.FromArgb(...), SizeF, Rectangle, KnownColor all print correct values).

      System.Drawing.Common.dll      Image, Bitmap, Graphics, ImageFormat, ImageCodecInfo, ...
          Windows-only. Measured in the same container: without a package reference those types do
          not even compile (CS1069 "type has been forwarded to System.Drawing.Common").

    Counting both together would put a value type used by a config class in the same "must reach
    zero" list as a GDI+ decode, and would push someone to change a public API for no portability
    gain. Returns a hashtable with Windows and Portable counts.
#>
  [CmdletBinding()]
  param([Parameter(Mandatory)][string] $Text)

  # The gated number stays "how many times the file mentions the System.Drawing namespace", so
  # this classifier decides who owns those mentions rather than changing the scale of the metric.
  # The gated number counts namespace mentions **and** GDI+ identifier occurrences. Mention
  # counting alone was measured to be insufficient: adding `typeof(Bitmap)` to a file that already
  # imported System.Drawing left the count unchanged, i.e. the gate could not see new coupling in
  # the one file that still has the most of it. Identifiers are only attributed to System.Drawing
  # inside a file that imports or qualifies the namespace, so that an unrelated `Color`/`Size`/
  # `Image` property elsewhere in the tree is not counted.
  $ns = ([regex]::Matches($Text, 'System\.Drawing')).Count
  if ($ns -eq 0) { return @{ Windows = 0; Portable = 0 } }

  # Qualified references to a Windows-only sub-namespace, or to a known GDI+ type.
  $windows = $false
  $unknown = $false
  foreach ($m in [regex]::Matches($Text, 'System\.Drawing\.([A-Za-z_]\w*)')) {
    $type = $m.Groups[1].Value
    if ($DrawingWindowsNamespaces -contains $type -or $DrawingWindowsTypes -contains $type) {
      $windows = $true
    } elseif ($DrawingPrimitiveTypes -notcontains $type) {
      $unknown = $true
    }
  }

  # Bare identifiers only count as System.Drawing when the file imports the namespace; otherwise
  # every unrelated Color/Size/Point in the tree would be attributed to it.
  $importsNamespace = [regex]::IsMatch($Text, '(^|\s)using\s+System\.Drawing\s*;')
  $identifierRefs = 0
  if ($importsNamespace) {
    foreach ($type in $DrawingWindowsTypes) {
      $identifierRefs += ([regex]::Matches($Text, "(?<![\.\w])$type\b")).Count
      if ($identifierRefs -gt 0) { $windows = $true }
    }
  }

  # A type outside both lists counts as Windows: guessing the other way would hide a real
  # dependency from the one metric that is supposed to be impossible to silence.
  if ($windows -or $unknown) { return @{ Windows = ($ns + $identifierRefs); Portable = 0 } }
  # Only value types (Color, SizeF, Rectangle, ...) behind the namespace: portable, but still
  # reported, because a plain 'using System.Drawing;' pulls the old System.Drawing assembly in
  # on net48 even when just primitives are used.
  return @{ Windows = 0; Portable = $ns }
}

function Measure-SpineArea {
<#
    Counts Windows references over a set of source directories.
    Returns Areas (per directory code counts), Files (per file totals), TotalRefs (Windows) and
    PortableRefs (System.Drawing primitives, reported for context, never gated).
#>
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)][string[]] $Dirs,
    [Parameter(Mandatory)][System.Collections.Specialized.OrderedDictionary] $Patterns
  )

  $areas = [ordered]@{}
  $files = @()
  $total = 0
  $portable = 0

  foreach ($dir in $Dirs) {
    $areaRoot = Join-Path $repoRoot $dir
    if (-not (Test-Path $areaRoot)) { throw "source area missing: $areaRoot" }
    $areaCounts = @{}
    foreach ($k in $Patterns.Keys) { $areaCounts[$k] = 0 }
    # Filled by Measure-DrawingRefs instead of a regex, but reported next to the others.
    $areaCounts['DrawingWindows'] = 0
    $areaCounts['DrawingPortable'] = 0

    foreach ($file in (Get-ChildItem $areaRoot -Recurse -Filter *.cs -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })) {
      $text = Get-CodeOnlyText -Text ([System.IO.File]::ReadAllText($file.FullName))
      $fileHits = 0
      foreach ($k in $Patterns.Keys) {
        if ($k -eq 'Drawing') {
          $d = Measure-DrawingRefs -Text $text
          $areaCounts['DrawingWindows'] += $d.Windows
          $areaCounts['DrawingPortable'] += $d.Portable
          $portable += $d.Portable
          $fileHits += $d.Windows
          continue
        }
        $n = ([regex]::Matches($text, $Patterns[$k])).Count
        $areaCounts[$k] += $n
        $fileHits += $n
      }
      if ($fileHits -gt 0) {
        $files += [pscustomobject]@{ File = $file.FullName.Substring($repoRoot.Length + 1); Refs = $fileHits }
        $total += $fileHits
      }
    }
    $areas[$dir] = $areaCounts
  }

  return [pscustomobject]@{ Areas = $areas; Files = $files; TotalRefs = $total; PortableRefs = $portable }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not $BaselinePath) { $BaselinePath = Join-Path $PSScriptRoot 'spine-baseline.json' }

# Assemblies that must eventually build without a -windows target.
$coreAreas = @(
  'MediaPortal\Source\Core\MediaPortal.Common',
  'MediaPortal\Source\Core\MediaPortal.Utilities',
  'MediaPortal\Source\Core\MediaPortal.Backend'
)

# Windows-only satellites are discovered, not listed, so a new one cannot escape the ledger.
$satelliteAreas = @(
  Get-ChildItem (Join-Path $repoRoot 'MediaPortal\Source\Core') -Directory -Filter '*.Windows' |
    ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1) } |
    Sort-Object
)

# System.Drawing type sets used to classify a reference as Windows-only or portable. Kept as
# data so the reason a reference was counted can be looked up; see Measure-DrawingRefs for the
# measurements behind the split.
$DrawingPrimitiveTypes = @(
  'Color', 'KnownColor', 'Point', 'PointF', 'Size', 'SizeF', 'Rectangle', 'RectangleF', 'ContentAlignment'
)
$DrawingWindowsTypes = @(
  'Bitmap', 'BitmapData', 'BufferedGraphics', 'Brush', 'ColorMap', 'ColorMatrix', 'CompositingMode',
  'CompositingQuality', 'Font', 'FontFamily', 'FrameDimension', 'Graphics', 'GraphicsUnit', 'Icon',
  'Image', 'ImageAnimator', 'ImageAttributes', 'ImageCodecInfo', 'ImageFormat', 'InterpolationMode',
  'Metafile', 'Pen', 'PixelFormat', 'PixelOffsetMode', 'PrintDocument', 'SolidBrush', 'SmoothingMode',
  'StringFormat', 'SystemIcons', 'TextureBrush'
)
# Namespaces under System.Drawing that are all in System.Drawing.Common.
$DrawingWindowsNamespaces = @('Imaging', 'Drawing2D', 'Text', 'Design', 'Printing')

$patterns = [ordered]@{
  WinForms = 'System\.Windows\.Forms'
  # 'Drawing' is not a regex - the value is unused, the key asks Measure-SpineArea to run
  # Measure-DrawingRefs, which fills DrawingWindows (gated) and DrawingPortable (reported).
  Drawing  = $null
  # A P/Invoke declaration is the one Windows dependency that neither this ledger nor CA1416 used to
  # see: DllImport produces no CA1416 diagnostic at all (the analyzer has no idea what is behind the
  # string), and it is neither WinForms nor GDI+. Measured when it was added: 95 declarations sat in
  # the "portable core" (MediaPortal.Common 21, MediaPortal.Utilities 74, MediaPortal.Backend 0)
  # while the core spine column read 0 - so 0 never meant portable, it meant "no WinForms or GDI+".
  PInvoke  = '\[DllImport\s*\('
}

$core = Measure-SpineArea -Dirs $coreAreas -Patterns $patterns
$sat = Measure-SpineArea -Dirs $satelliteAreas -Patterns $patterns

function Write-AreaReport {
  [CmdletBinding()]
  param([string] $Title, [string] $Colour, [pscustomobject] $Measure)

  Write-Host $Title -ForegroundColor $Colour
  foreach ($area in $Measure.Areas.Keys) {
    "  {0,-52} WinForms {1,4}   GDI+ {2,4}   P/Invoke {3,4}   Drawing primitives {4,4}" -f $area, `
      $Measure.Areas[$area]['WinForms'], $Measure.Areas[$area]['DrawingWindows'], $Measure.Areas[$area]['PInvoke'], $Measure.Areas[$area]['DrawingPortable']
  }
  "  {0,-52} files affected {1,4}   total refs {2,4}" -f '', $Measure.Files.Count, $Measure.TotalRefs
  $Measure.Files | Sort-Object Refs -Descending | ForEach-Object { "    {0,-80} {1,3}" -f $_.File, $_.Refs } | ForEach-Object { Write-Host $_ }
}

Write-AreaReport -Title 'Windows spine still compiled into the portable core (must become 0):' -Colour Cyan -Measure $core
Write-AreaReport -Title 'Windows-only satellites (quarantined, not fixed - must not grow on their own):' -Colour DarkGray -Measure $sat

# The two ledgers are only meaningful together. Gating each one on its own forbids the exact
# operation this port is made of: taking Windows code out of the core and putting it where it
# belongs, which shrinks the core and grows the satellite by the same amount. So the rule is
# core must never grow, and core + satellites must never grow either. A satellite may only get
# bigger because something left the core.
$combinedRefs = $core.TotalRefs + $sat.TotalRefs
$combinedFiles = $core.Files.Count + $sat.Files.Count

# Drops the placeholder 'Drawing' key from the report data written to the baseline; only the
# two classified columns are meaningful, and writing a permanent zero invites misreading.
function Format-AreaCounts {
  [CmdletBinding()]
  param([System.Collections.Specialized.OrderedDictionary] $Areas)

  $out = [ordered]@{}
  foreach ($dir in $Areas.Keys) {
    $cols = [ordered]@{}
    foreach ($col in @('WinForms', 'DrawingWindows', 'PInvoke', 'DrawingPortable')) { $cols[$col] = $Areas[$dir][$col] }
    $out[$dir] = $cols
  }
  return $out
}

if ($UpdateBaseline) {
  $payload = [ordered]@{
    generated = (Get-Date).ToString('yyyy-MM-dd')
    note      = 'Textual references to System.Windows.Forms and to the Windows-only half of System.Drawing, comments stripped. GDI+ here means System.Drawing.Common types (Image, Bitmap, Graphics, ImageFormat, ...): Windows-only, and on net8.0 they do not even compile without a package reference. The portable half (Color, SizeF, Rectangle, ... in System.Drawing.Primitives) is measured separately into drawingPortableRefs and is NOT gated, because it runs on Linux - measured, not assumed. core = code that must become portable, floor must reach 0. satellites = *.Windows companions holding quarantined code. Gate: core may not grow AND core + satellites (combinedTotalRefs) may not grow either, which is what allows Windows code to be relocated into a satellite without letting new Windows coupling in. A satellite declaration (SupportedOSPlatform) also hides its own call sites from CA1416, so this ledger is the only thing watching it. The P/Invoke column counts [DllImport] declarations: CA1416 never reports them (the analyzer cannot know what a DLL name implies) and they are neither WinForms nor GDI+, so before this column existed 95 of them sat in the core while the WinForms/GDI+ columns read 0. Counted from files on disk, so a file a csproj excludes with Compile Remove still counts - crude on purpose, see the header.'
    areas     = Format-AreaCounts -Areas $core.Areas
    filesAffected = $core.Files.Count
    totalRefs = $core.TotalRefs
    drawingPortableRefs = $core.PortableRefs
    satelliteAreas = Format-AreaCounts -Areas $sat.Areas
    satelliteFilesAffected = $sat.Files.Count
    satelliteTotalRefs = $sat.TotalRefs
    satelliteDrawingPortableRefs = $sat.PortableRefs
    combinedTotalRefs = $combinedRefs
    combinedFilesAffected = $combinedFiles
  }
  # No BOM: PowerShell 5.1 -Encoding utf8 would prepend one.
  [System.IO.File]::WriteAllText(
    [System.IO.Path]::GetFullPath($BaselinePath),
    ($payload | ConvertTo-Json -Depth 5),
    (New-Object System.Text.UTF8Encoding($false)))
  Write-Host "Spine baseline written: $BaselinePath" -ForegroundColor Green
  exit 0
}

if (-not (Test-Path $BaselinePath)) {
  Write-Host "No baseline at $BaselinePath - run with -UpdateBaseline to create one." -ForegroundColor Yellow
  exit 0
}

$doc = Get-Content $BaselinePath -Raw | ConvertFrom-Json
$fail = $false

function Test-Growth {
  [CmdletBinding()]
  param([string] $Label, [int] $Now, [int] $Floor, [int] $FilesNow, [int] $FilesFloor)

  if ($Now -gt $Floor) {
    Write-Host "REGRESSION: $Label has $Now Windows references, floor was $Floor." -ForegroundColor Red
    $script:fail = $true
  }
  if ($FilesNow -gt $FilesFloor) {
    Write-Host "REGRESSION: $Label spread to $FilesNow files, floor was $FilesFloor." -ForegroundColor Red
    $script:fail = $true
  }
  if ($Now -lt $Floor) {
    Write-Host "improved: $Label $Floor -> $Now (commit the new floor with -UpdateBaseline)" -ForegroundColor Green
  }
}

Test-Growth -Label 'portable core' -Now $core.TotalRefs -Floor ([int]$doc.totalRefs) -FilesNow $core.Files.Count -FilesFloor ([int]$doc.filesAffected)

if ($doc.PSObject.Properties.Name -contains 'combinedTotalRefs') {
  Test-Growth -Label 'core + satellites' -Now $combinedRefs -Floor ([int]$doc.combinedTotalRefs) -FilesNow $combinedFiles -FilesFloor ([int]$doc.combinedFilesAffected)
  if ($doc.PSObject.Properties.Name -contains 'satelliteTotalRefs') {
    $satDelta = $sat.TotalRefs - [int]$doc.satelliteTotalRefs
    $coreDelta = $core.TotalRefs - [int]$doc.totalRefs
    if ($satDelta -gt 0) {
      if ($coreDelta -le 0) {
        Write-Host "note: satellites grew by $satDelta while the core shrank by $([Math]::Abs($coreDelta)) - relocated, not fixed." -ForegroundColor DarkYellow
      } else {
        Write-Host "note: satellites grew by $satDelta and the core grew by $coreDelta - the core growth is what failed the gate." -ForegroundColor DarkYellow
      }
    }
  }
} else {
  Write-Host "baseline has no combined section yet - run -UpdateBaseline to start gating relocation" -ForegroundColor Yellow
}

if ($fail) {
  Write-Host ""
  Write-Host "Files carrying the spine (worst first):" -ForegroundColor Red
  @($core.Files) + @($sat.Files) | Sort-Object Refs -Descending | Select-Object -First 12 | Format-Table -AutoSize | Out-String | Write-Host
  Write-Host "In the portable core: replace with a portable equivalent, or move genuinely Win32 code to a *.Windows satellite." -ForegroundColor Red
  Write-Host "If core + satellites grew: you added Windows coupling without removing any - a satellite absorbs a dependency, it does not create one." -ForegroundColor Red
  exit 1
}

Write-Host "Spine OK." -ForegroundColor Green
exit 0

<#
.SYNOPSIS
  Runs the portability canary test suite inside a Linux container.
.EXAMPLE
  .\test-canary-linux.ps1
#>
[CmdletBinding()]
param([string]$ImageName = 'mp2-canary')

# Docker writes its BuildKit progress to stderr, which PowerShell turns into a terminating
# error record under 'Stop'. Failures are detected through $LASTEXITCODE instead.
$ErrorActionPreference = 'Continue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

& docker build -f (Join-Path $PSScriptRoot 'Dockerfile.canary') -t $ImageName $repoRoot
if ($LASTEXITCODE -ne 0) { throw "Container image build failed ($LASTEXITCODE)." }

& docker run --rm $ImageName
if ($LASTEXITCODE -ne 0) { throw "Linux test run failed ($LASTEXITCODE)." }

Write-Host "Canary green on Linux." -ForegroundColor Green

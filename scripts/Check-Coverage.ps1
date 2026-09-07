<#
.SYNOPSIS
  Enforces a Cobertura line-coverage gate with no external service.
.DESCRIPTION
  Finds every *.cobertura.xml under Root, reads each root line-rate,
  and exits 1 when any file is under Threshold or when no file exists.
.PARAMETER Threshold
  Minimum line-rate as a fraction (0.60 means 60 percent).
.PARAMETER Root
  Directory to search recursively. Defaults to the current directory.
.EXAMPLE
  ./scripts/Check-Coverage.ps1 -Threshold 0.30
#>
[CmdletBinding()]
param(
  [double]$Threshold = 0.30,
  [string]$Root = (Get-Location).Path
)

$files = Get-ChildItem -Path $Root -Recurse -Filter *.cobertura.xml -File -ErrorAction SilentlyContinue

if ($null -eq $files -or $files.Count -eq 0) {
  Write-Error "Coverage gate failed: no *.cobertura.xml found under $Root."
  exit 1
}

$failed = $false

foreach ($file in $files) {
  [xml]$xml = Get-Content -LiteralPath $file.FullName -Raw
  $rate = [double]::Parse($xml.coverage.'line-rate', [cultureinfo]::InvariantCulture)
  $pct = [math]::Round($rate * 100, 2)
  $need = [math]::Round($Threshold * 100, 2)

  if ($rate -lt $Threshold) {
    Write-Error "Coverage gate failed: $($file.FullName) has $pct% line coverage, needs $need%."
    $failed = $true
  }
  else {
    Write-Host "Coverage gate passed: $($file.FullName) has $pct% line coverage (needs $need%)."
  }
}

if ($failed) {
  exit 1
}

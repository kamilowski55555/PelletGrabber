param(
  [string]$Config = "config.yaml",
  [string]$RunPrefix = "camera",
  [switch]$Tensorboard,
  [int]$Port = 6006
)

$ErrorActionPreference = "Stop"

# katalog results
$resultsDir = Join-Path (Get-Location) "results"
if (!(Test-Path $resultsDir)) {
  New-Item -ItemType Directory -Path $resultsDir | Out-Null
}

# znajdź istniejące runy
$numbers = @()
Get-ChildItem -Path $resultsDir -Directory -ErrorAction SilentlyContinue | ForEach-Object {
  if ($_.Name -match "^$RunPrefix(\d+)$") {
    $numbers += [int]$Matches[1]
  }
}

# następny numer
if ($numbers.Count -eq 0) {
  $next = 1
} else {
  $next = ($numbers | Measure-Object -Maximum).Maximum + 1
}

# 🔥 ZERO FORMATOWANIA – ręczne zero-padding
if ($next -lt 10) {
  $runId = "$RunPrefix`0$next"
} else {
  $runId = "$RunPrefix$next"
}

Write-Host "==> Run ID: $runId"

# TensorBoard (opcjonalnie)
if ($Tensorboard) {
  Write-Host "==> Starting TensorBoard on port $Port"
  Start-Process powershell -ArgumentList "-NoExit", "-Command", "tensorboard --logdir results --port $Port"
  Start-Sleep -Seconds 1
  Write-Host "==> TensorBoard: http://localhost:$Port"
}

# trening
Write-Host "==> Starting training..."
mlagents-learn $Config --run-id $runId --debug

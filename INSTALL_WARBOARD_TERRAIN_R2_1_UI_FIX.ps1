$ErrorActionPreference = "Stop"
Write-Host ""
Write-Host "WARBOARD - TERRAIN R2.1 UI FIX" -ForegroundColor Cyan
Write-Host "-------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
function Find-Root([string]$start) {
    $p = (Resolve-Path $start).Path
    for ($i = 0; $i -lt 10; $i++) {
        if (Test-Path (Join-Path $p "Assets\Scripts\Core\GameController.cs")) { return $p }
        $parent = Split-Path -Parent $p
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $p) { break }
        $p = $parent
    }
    return $null
}

$ProjectRoot = Find-Root $ScriptDir
if (-not $ProjectRoot) { Write-Host "ERROR: Could not find Warboard project root." -ForegroundColor Red; Read-Host "Press Enter"; exit 1 }

$oldTooltip = Join-Path $ProjectRoot "Assets\Scripts\Core\WarboardTerrainTooltipR2.cs"
if (-not (Test-Path $oldTooltip)) { Write-Host "ERROR: Terrain Overhaul R2 is not installed." -ForegroundColor Red; Read-Host "Press Enter"; exit 1 }

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = Join-Path $ProjectRoot "Library\WarboardBackups\TERRAIN_R2_1_UI_FIX\$timestamp"
New-Item -ItemType Directory -Force -Path $backup | Out-Null

$targets = @(
    "Assets\Scripts\Core\WarboardTerrainTooltipR2.cs",
    "Assets\Scripts\Core\WarboardMissionCardRowR21.cs"
)

foreach ($rel in $targets) {
    $dest = Join-Path $ProjectRoot $rel
    if (Test-Path $dest) {
        $b = Join-Path $backup $rel
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $b) | Out-Null
        Copy-Item $dest $b -Force
    }
    $src = Join-Path $ScriptDir ("PATCH_PAYLOAD\" + $rel)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $src $dest -Force
    Write-Host "Installed: $rel" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "R2.1 installed successfully." -ForegroundColor Green
Write-Host "  - fixed terrain hover GUI exception"
Write-Host "  - mission cards are now on the SAME ROW as the scoreboard"
Write-Host "  - P1 cards left, P2 cards right"
Write-Host "  - full existing mission text stays on each card and shrinks when needed"
Write-Host ""
Write-Host "Backup: $backup" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile, then start a fresh battle." -ForegroundColor Cyan
Read-Host "Press Enter to close"

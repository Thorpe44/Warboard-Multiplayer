$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V54 - TERRAIN OVERHAUL" -ForegroundColor Cyan
Write-Host "--------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail {
    param([string]$Message)
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
        $core = Join-Path $candidate "Assets\Scripts\Core\GameController.Core.cs"
        if (Test-Path $core) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) {
            break
        }
        $candidate = $parent
    }

    foreach ($child in Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue) {
        $core = Join-Path $child.FullName "Assets\Scripts\Core\GameController.Core.cs"
        if (Test-Path $core) {
            return $child.FullName
        }
    }

    return $null
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir
if (-not $ProjectRoot) {
    Fail "Could not find the Warboard project root."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$PayloadRoot = Join-Path $ScriptDir "V54_PATCH_PAYLOAD"
if (-not (Test-Path $PayloadRoot)) {
    Fail "V54 patch payload folder is missing."
}

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot "Library\WarboardBackups\V54_TERRAIN_OVERHAUL\$Timestamp"
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

$Targets = @(
    "Assets\Scripts\Core\WarboardV54TerrainOverhaulBootstrap.cs",
    "Assets\Resources\WarboardV54Terrain\v54_floor_rust_a.png",
    "Assets\Resources\WarboardV54Terrain\v54_floor_rust_b.png",
    "Assets\Resources\WarboardV54Terrain\v54_floor_metal_a.png",
    "Assets\Resources\WarboardV54Terrain\v54_floor_stone_a.png"
)

foreach ($relative in $Targets) {
    $dest = Join-Path $ProjectRoot $relative
    if (Test-Path $dest) {
        $backupDest = Join-Path $BackupRoot $relative
        $backupDir = Split-Path -Parent $backupDest
        New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
        Copy-Item $dest $backupDest -Force
    }
}

foreach ($relative in $Targets) {
    $src = Join-Path $PayloadRoot $relative
    if (-not (Test-Path $src)) {
        Fail "Missing payload file: $relative"
    }

    $dest = Join-Path $ProjectRoot $relative
    $destDir = Split-Path -Parent $dest
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item $src $dest -Force
    Write-Host "Installed: $relative" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "V54 terrain overhaul installed successfully." -ForegroundColor Green
Write-Host "Backup (if any existing V54 files were replaced):" -ForegroundColor Green
Write-Host "  $BackupRoot" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile/reimport." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"

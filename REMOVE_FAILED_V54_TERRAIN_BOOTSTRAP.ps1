$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD - REMOVE FAILED V54 TERRAIN BOOTSTRAP" -ForegroundColor Cyan
Write-Host "------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
        if (Test-Path (Join-Path $candidate "Assets\Scripts\Core\GameController.cs")) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) {
            break
        }
        $candidate = $parent
    }

    foreach ($child in Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue) {
        if (Test-Path (Join-Path $child.FullName "Assets\Scripts\Core\GameController.cs")) {
            return $child.FullName
        }
    }

    return $null
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir
if (-not $ProjectRoot) {
    Write-Host "ERROR: Could not locate the Warboard project root." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Targets = @(
    "Assets\Scripts\Core\WarboardV54TerrainOverhaulBootstrap.cs",
    "Assets\Scripts\Core\WarboardV54TerrainOverhaulBootstrap.cs.meta",
    "Assets\Resources\WarboardV54Terrain"
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\REMOVE_FAILED_V54_TERRAIN\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

$removed = 0

foreach ($relative in $Targets) {
    $target = Join-Path $ProjectRoot $relative

    if (-not (Test-Path $target)) {
        continue
    }

    $backupTarget = Join-Path $BackupDir $relative
    $backupParent = Split-Path -Parent $backupTarget
    New-Item -ItemType Directory -Force -Path $backupParent | Out-Null

    $item = Get-Item $target

    if ($item.PSIsContainer) {
        Copy-Item $target $backupTarget -Recurse -Force
        Remove-Item $target -Recurse -Force
    }
    else {
        Copy-Item $target $backupTarget -Force
        Remove-Item $target -Force
    }

    Write-Host "Removed: $relative" -ForegroundColor DarkGray
    $removed++
}

Write-Host ""
if ($removed -eq 0) {
    Write-Host "No failed V54 bootstrap files were present." -ForegroundColor Yellow
}
else {
    Write-Host "Failed V54 terrain bootstrap removed." -ForegroundColor Green
}

Write-Host ""
Write-Host "Nothing else in the project was modified." -ForegroundColor Green
Write-Host "Current game/rules/terrain code and build number were left untouched." -ForegroundColor Green
Write-Host "Backup: $BackupDir" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it reload." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"

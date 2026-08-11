$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD - TERRAIN OVERHAUL R2 (CURRENT MAIN)" -ForegroundColor Cyan
Write-Host "----------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail {
    param([string]$Message)

    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "No intentional project changes were left half-installed." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
        $core = Join-Path $candidate "Assets\Scripts\Core\GameController.cs"

        if (Test-Path $core) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $candidate) {
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
    Fail "Could not find the Warboard Unity project root."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Payload = Join-Path $ScriptDir "PATCH_PAYLOAD"

$TerrainFile = Join-Path $ProjectRoot "Assets\Scripts\Core\GameController.V55CleanTerrain.cs"
$CardsFile = Join-Path $ProjectRoot "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs"

foreach ($required in @($TerrainFile, $CardsFile)) {
    if (-not (Test-Path $required)) {
        Fail "Required current-main file is missing: $required"
    }
}

$terrainText = Get-Content -Raw -Path $TerrainFile
$cardsText = Get-Content -Raw -Path $CardsFile

if ($terrainText -notmatch 'WARBOARD_V57_RUIN_TERRAIN_KIT' -or
    $terrainText -notmatch 'V55CreateTerrainFeatureVisual') {
    Fail "Current terrain source was not recognised. This installer targets the live main inspected on 2026-08-11."
}

if ($cardsText -notmatch 'WARBOARD_V55_WORLD_MISSION_CARDS' -or
    $cardsText -notmatch 'WoundDisplayBillboard') {
    Fail "Current mission-card source was not recognised. Nothing was changed."
}

$Targets = @(
    "Assets\Scripts\Core\GameController.V55CleanTerrain.cs",
    "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs",
    "Assets\Scripts\Core\WarboardTerrainTooltipR2.cs",
    "Assets\Resources\WarboardTerrainR2\floor_rubble_rust.png",
    "Assets\Resources\WarboardTerrainR2\floor_broken_concrete.png",
    "Assets\Resources\WarboardTerrainR2\floor_battle_rust.png",
    "Assets\Resources\WarboardTerrainR2\floor_industrial_plate.png"
)

foreach ($relative in $Targets) {
    $source = Join-Path $Payload $relative

    if (-not (Test-Path $source)) {
        Fail "Patch payload is incomplete: $relative"
    }
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot "Library\WarboardBackups\TERRAIN_OVERHAUL_R2\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

foreach ($relative in $Targets) {
    $dest = Join-Path $ProjectRoot $relative

    if (Test-Path $dest) {
        $backup = Join-Path $BackupRoot $relative
        $backupParent = Split-Path -Parent $backup
        New-Item -ItemType Directory -Force -Path $backupParent | Out-Null
        Copy-Item $dest $backup -Force
    }
}

# Also back up any old failed V54 overlay before removing it if somebody still
# has it locally.
$OldV54Files = @(
    "Assets\Scripts\Core\WarboardV54TerrainOverhaulBootstrap.cs",
    "Assets\Resources\WarboardV54Terrain"
)

foreach ($relative in $OldV54Files) {
    $old = Join-Path $ProjectRoot $relative

    if (Test-Path $old) {
        $backup = Join-Path $BackupRoot ("OLD_V54\" + $relative)
        $backupParent = Split-Path -Parent $backup
        New-Item -ItemType Directory -Force -Path $backupParent | Out-Null

        $item = Get-Item $old

        if ($item.PSIsContainer) {
            Copy-Item $old $backup -Recurse -Force
        }
        else {
            Copy-Item $old $backup -Force
        }
    }
}

try {
    foreach ($relative in $Targets) {
        $source = Join-Path $Payload $relative
        $dest = Join-Path $ProjectRoot $relative
        $destParent = Split-Path -Parent $dest
        New-Item -ItemType Directory -Force -Path $destParent | Out-Null
        Copy-Item $source $dest -Force
        Write-Host "Installed: $relative" -ForegroundColor DarkGray
    }

    foreach ($relative in $OldV54Files) {
        $old = Join-Path $ProjectRoot $relative

        if (Test-Path $old) {
            Remove-Item $old -Recurse -Force
            Write-Host "Removed stale V54 overlay: $relative" -ForegroundColor DarkGray
        }
    }

    $verifyTerrain = Get-Content -Raw -Path $TerrainFile
    $verifyCards = Get-Content -Raw -Path $CardsFile
    $verifyTooltip = Get-Content -Raw -Path (Join-Path $ProjectRoot "Assets\Scripts\Core\WarboardTerrainTooltipR2.cs")

    if ($verifyTerrain -notmatch 'WARBOARD_TERRAIN_OVERHAUL_R2' -or
        $verifyTerrain -notmatch 'R2CreateGothicWallRun' -or
        $verifyTerrain -notmatch 'R2CreateAreaSurface') {
        throw "Terrain replacement verification failed."
    }

    if ($verifyCards -notmatch 'WARBOARD_MISSION_CARD_RACK_R2' -or
        $verifyCards -match 'WoundDisplayBillboard') {
        throw "Mission-card rack verification failed."
    }

    if ($verifyTooltip -notmatch 'WARBOARD_TERRAIN_TOOLTIP_R2') {
        throw "Terrain tooltip verification failed."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring backed-up files..." -ForegroundColor Red

    foreach ($relative in $Targets) {
        $backup = Join-Path $BackupRoot $relative
        $dest = Join-Path $ProjectRoot $relative

        if (Test-Path $backup) {
            $destParent = Split-Path -Parent $dest
            New-Item -ItemType Directory -Force -Path $destParent | Out-Null
            Copy-Item $backup $dest -Force
        }
        elseif (Test-Path $dest) {
            Remove-Item $dest -Force
        }
    }

    Write-Host $_.Exception.Message
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "Terrain Overhaul R2 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Main changes:"
Write-Host "  - replaces V57's small disconnected procedural pieces"
Write-Host "  - large L / U / corner / triangular ruined buildings"
Write-Host "  - clearer doors and window gaps"
Write-Host "  - industrial barricade lanes for narrow Terrain Areas"
Write-Host "  - old cyan footprint fill removed; outline made subtle"
Write-Host "  - textured Terrain Area bases using the supplied footprint references"
Write-Host "  - hover terrain for an in-game rules explanation"
Write-Host "  - four giant mission billboards replaced by smaller physical cards on a wooden rack"
Write-Host ""
Write-Host "The installer deliberately DOES NOT modify WarboardBuildInfo.cs." -ForegroundColor Yellow
Write-Host "The live repo currently reports v57 there even though the running screenshot displayed v67." -ForegroundColor Yellow
Write-Host ""
Write-Host "Backup: $BackupRoot" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile/reimport, then start a fresh battle." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"

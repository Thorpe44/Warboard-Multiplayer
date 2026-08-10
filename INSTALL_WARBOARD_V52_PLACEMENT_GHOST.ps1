$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V52 - PLACEMENT / MOVEMENT GHOST PREVIEW" -ForegroundColor Cyan
Write-Host "-------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 7; $i++) {
        $gc = Join-Path $candidate "Assets\Scripts\Core\GameController.cs"
        $mv = Join-Path $candidate "Assets\Scripts\Core\GameController.Movement.cs"

        if ((Test-Path $gc) -and (Test-Path $mv)) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) {
            break
        }

        $candidate = $parent
    }

    $childCandidates = Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue
    foreach ($child in $childCandidates) {
        $gc = Join-Path $child.FullName "Assets\Scripts\Core\GameController.cs"
        $mv = Join-Path $child.FullName "Assets\Scripts\Core\GameController.Movement.cs"

        if ((Test-Path $gc) -and (Test-Path $mv)) {
            return $child.FullName
        }
    }

    return $null
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Write-Host "ERROR: Could not find the Warboard Unity project root." -ForegroundColor Red
    Write-Host "Extract this patch into the Warboard project root and run the BAT again."
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$CoreDir = Join-Path $ProjectRoot "Assets\Scripts\Core"
$BuildInfo = Join-Path $CoreDir "WarboardBuildInfo.cs"
$Movement = Join-Path $CoreDir "GameController.Movement.cs"
$ModelToken = Join-Path $CoreDir "ModelToken.cs"
$PayloadFile = Join-Path $ScriptDir "V52_PATCH_PAYLOAD\GameController.V52PlacementGhost.cs"
$TargetFile = Join-Path $CoreDir "GameController.V52PlacementGhost.cs"

foreach ($required in @($BuildInfo, $Movement, $ModelToken, $PayloadFile)) {
    if (-not (Test-Path $required)) {
        Write-Host "ERROR: Missing required file:" -ForegroundColor Red
        Write-Host "  $required"
        Read-Host "Press Enter to close"
        exit 1
    }
}

$buildText = Get-Content -Raw -Path $BuildInfo

if ($buildText -notmatch 'CurrentVersion\s*=\s*"v(50|51|52)"') {
    Write-Host "ERROR: V52 expected Warboard v50/v51 (or an already-installed v52)." -ForegroundColor Red
    Write-Host "Current WarboardBuildInfo.cs was not recognised."
    Read-Host "Press Enter to close"
    exit 1
}

$movementText = Get-Content -Raw -Path $Movement
$modelText = Get-Content -Raw -Path $ModelToken

$checks = @(
    @{ Name = "whole-squad movement state"; Text = $movementText; Pattern = "wholeSquadMoveMode" },
    @{ Name = "movement legality"; Text = $movementText; Pattern = "CanTranslateWithinNormalMove" },
    @{ Name = "single-model placement legality"; Text = $movementText; Pattern = "CanPlaceModel" },
    @{ Name = "real miniature visual support"; Text = $modelText; Pattern = "HasCustomVisual" },
    @{ Name = "base radius"; Text = $modelText; Pattern = "BaseRadiusInches" }
)

foreach ($check in $checks) {
    if ($check.Text -notmatch $check.Pattern) {
        Write-Host "ERROR: Current project does not contain expected $($check.Name) support." -ForegroundColor Red
        Write-Host "Nothing has been changed."
        Read-Host "Press Enter to close"
        exit 1
    }
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\V52\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Copy-Item $BuildInfo (Join-Path $BackupDir "WarboardBuildInfo.cs") -Force

if (Test-Path $TargetFile) {
    Copy-Item $TargetFile (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") -Force
}

Write-Host "Backup: $BackupDir" -ForegroundColor DarkGray

Copy-Item $PayloadFile $TargetFile -Force

$updated = Get-Content -Raw -Path $BuildInfo
$updated = [regex]::Replace(
    $updated,
    'CurrentVersion\s*=\s*"v(?:50|51|52)"',
    'CurrentVersion = "v52"'
)
Set-Content -Path $BuildInfo -Value $updated -Encoding UTF8

$verifyModule = Get-Content -Raw -Path $TargetFile
$verifyBuild = Get-Content -Raw -Path $BuildInfo

if ($verifyModule -notmatch "WARBOARD V52" -or
    $verifyModule -notmatch "DrawPlacementGhostPreview52" -or
    $verifyBuild -notmatch 'CurrentVersion\s*=\s*"v52"') {
    Write-Host "ERROR: Verification failed. Restoring backup." -ForegroundColor Red

    Copy-Item (Join-Path $BackupDir "WarboardBuildInfo.cs") $BuildInfo -Force

    $oldModule = Join-Path $BackupDir "GameController.V52PlacementGhost.cs"
    if (Test-Path $oldModule) {
        Copy-Item $oldModule $TargetFile -Force
    }
    elseif (Test-Path $TargetFile) {
        Remove-Item $TargetFile -Force
    }

    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "V52 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "What changed:"
Write-Host "  - Cursor-following translucent ghost for initial deployment."
Write-Host "  - Ghost for single-model normal moves."
Write-Host "  - Full formation ghost for whole-unit translation."
Write-Host "  - Ghost for reserves/reinforcements and special whole-unit moves."
Write-Host "  - Ghost for pile-in and consolidate placement."
Write-Host "  - Actual miniature mesh + base footprint are previewed."
Write-Host "  - GREEN = legal, RED = illegal, CYAN = preview where special rules still decide legality."
Write-Host "  - Preview is local-only and never moves/syncs authoritative models."
Write-Host ""
Write-Host "Now return to Unity and let it compile." -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to close"

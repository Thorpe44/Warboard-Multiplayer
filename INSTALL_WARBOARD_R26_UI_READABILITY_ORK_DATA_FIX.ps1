$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R2.6 - UI READABILITY + ORK DATA FIX" -ForegroundColor Cyan
Write-Host "-----------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail {
    param([string]$Message)
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "Source files were restored from backup where possible." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path
    for ($i = 0; $i -lt 12; $i++) {
        if ((Test-Path (Join-Path $candidate "Assets\Scripts\Core\GameController.cs")) -and
            (Test-Path (Join-Path $candidate "ProjectSettings\ProjectVersion.txt"))) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) { break }
        $candidate = $parent
    }

    foreach ($child in Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue) {
        if ((Test-Path (Join-Path $child.FullName "Assets\Scripts\Core\GameController.cs")) -and
            (Test-Path (Join-Path $child.FullName "ProjectSettings\ProjectVersion.txt"))) {
            return $child.FullName
        }
    }

    return $null
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir
if (-not $ProjectRoot) { Fail "Could not locate the Warboard Unity project root." }

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Core = Join-Path $ProjectRoot "Assets\Scripts\Core"
$CardsDest = Join-Path $Core "WarboardV55MissionCardsWorld.cs"
$ReadabilityDest = Join-Path $Core "WarboardUiReadabilityR26.cs"

$PayloadCore = Join-Path $ScriptDir "PATCH_PAYLOAD\Assets\Scripts\Core"
$CardsSource = Join-Path $PayloadCore "WarboardV55MissionCardsWorld.cs"
$ReadabilitySource = Join-Path $PayloadCore "WarboardUiReadabilityR26.cs"

foreach ($required in @($CardsSource, $ReadabilitySource)) {
    if (-not (Test-Path $required)) {
        Fail "Patch payload missing: $required"
    }
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$Backup = Join-Path $ProjectRoot "Library\WarboardBackups\R2_6_UI_READABILITY\$timestamp"
New-Item -ItemType Directory -Force -Path $Backup | Out-Null

$backupRel = @(
    "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs",
    "Assets\Scripts\Core\WarboardUiReadabilityR26.cs"
)

foreach ($relative in $backupRel) {
    $source = Join-Path $ProjectRoot $relative
    if (-not (Test-Path $source)) { continue }
    $dest = Join-Path $Backup $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $source $dest -Force
}

try {
    New-Item -ItemType Directory -Force -Path $Core | Out-Null
    Copy-Item $CardsSource $CardsDest -Force
    Copy-Item $ReadabilitySource $ReadabilityDest -Force

    $verifyCards = Get-Content -Raw -Path $CardsDest
    $verifyReadability = Get-Content -Raw -Path $ReadabilityDest

    if ($verifyCards -notmatch 'WARBOARD_MISSION_CARD_ROW_R2_6') {
        throw "Mission card readability file verification failed."
    }

    if ($verifyReadability -notmatch 'WARBOARD_UI_READABILITY_R2_6' -or
        $verifyReadability -notmatch 'BuildPlayerSummary') {
        throw "UI readability/Ork player-data file verification failed."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring backup..." -ForegroundColor Red

    foreach ($relative in $backupRel) {
        $saved = Join-Path $Backup $relative
        $dest = Join-Path $ProjectRoot $relative

        if (Test-Path $saved) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Copy-Item $saved $dest -Force
        }
        elseif ($relative -like "*WarboardUiReadabilityR26.cs" -and (Test-Path $dest)) {
            Remove-Item $dest -Force
        }
    }

    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "R2.6 installed." -ForegroundColor Green
Write-Host ""
Write-Host "What changed:" -ForegroundColor Cyan
Write-Host "  - bigger mission-card and scoreboard text"
Write-Host "  - new symmetric player summary bar so Player 2 / Ork data is visible"
Write-Host "  - underscored/raw rule tokens are hidden or humanised in UI"
Write-Host "  - small readability bump for bottom battle-log text"
Write-Host ""
Write-Host "Backup: $Backup" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity, let it compile, then start a fresh battle to verify." -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to close"
exit 0

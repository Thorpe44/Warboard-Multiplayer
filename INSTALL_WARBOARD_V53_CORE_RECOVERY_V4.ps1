$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V53 - CORE RECOVERY V4" -ForegroundColor Cyan
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

function Test-GoodCore {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $false
    }

    $text = Get-Content -Raw -Path $Path

    if ([string]::IsNullOrWhiteSpace($text)) {
        return $false
    }

    # These are representative methods that vanished together in the user's
    # cascade. A valid pre-V53 Core file contains all of them.
    $required = @(
        "HorizontalDistance",
        "JoinedModels",
        "IsEngaged",
        "UnitsAreEngaged",
        "CanPlaceModel",
        "AllModelsInsideBoard",
        "AllModelsHaveLegalPlacement",
        "SetObjectColor",
        "ReanimateUnit",
        "UnitWithinTerrainArea",
        "IsPositionInOpponentTerritory",
        "ModelCanSeeModel",
        "ModelCanSeeUnit",
        "RecordModelDestroyed",
        "ResolveDeadlyDemise",
        "ClearSelection",
        "HandleCamera",
        "NextPhase"
    )

    foreach ($name in $required) {
        if ($text -notmatch ([regex]::Escape($name) + '\s*\(')) {
            return $false
        }
    }

    # Also reject obviously truncated files.
    if ($text.Length -lt 50000) {
        return $false
    }

    return $true
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir
if (-not $ProjectRoot) {
    Fail "Could not find the Warboard project root."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$CoreDir = Join-Path $ProjectRoot "Assets\Scripts\Core"
$CoreFile = Join-Path $CoreDir "GameController.Core.cs"
$V53File = Join-Path $CoreDir "GameController.V53SolidSceneryPlacement.cs"
$PayloadV53 = Join-Path $ScriptDir "V53_PATCH_PAYLOAD\GameController.V53SolidSceneryPlacement.cs"

# V53 Recovery V2 created this backup BEFORE it touched Core.cs.
$BackupRoot = Join-Path $ProjectRoot "Library\WarboardBackups\V53_RECOVERY_V2"

if (-not (Test-Path $BackupRoot)) {
    Fail "The V53_RECOVERY_V2 backup folder was not found. Do not replace Core.cs manually; send me the project Core file if this happens."
}

$Candidates =
    Get-ChildItem -Path $BackupRoot -Directory -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending

$GoodBackup = $null

foreach ($folder in $Candidates) {
    $candidate = Join-Path $folder.FullName "GameController.Core.cs"

    if (Test-GoodCore -Path $candidate) {
        $GoodBackup = $candidate
        break
    }
}

if (-not $GoodBackup) {
    Fail "I found V53_RECOVERY_V2 backups, but none passed the intact-Core validation. Nothing was changed."
}

Write-Host "Good pre-V53 Core backup:" -ForegroundColor Green
Write-Host "  $GoodBackup" -ForegroundColor DarkGray

# Validate the V53 payload before any writes.
if (-not (Test-Path $PayloadV53)) {
    Fail "Corrected V53 helper payload is missing."
}

$payloadText = Get-Content -Raw -Path $PayloadV53

if ($payloadText -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery\s*<\s*T\s*>' -or
    $payloadText -match 'List\s*<\s*PlacementGhostCandidate52\s*>') {
    Fail "Corrected V53 helper payload failed validation."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$SafetyBackup = Join-Path $ProjectRoot "Library\WarboardBackups\V53_CORE_RECOVERY_V4\$timestamp"
New-Item -ItemType Directory -Force -Path $SafetyBackup | Out-Null

Copy-Item $CoreFile (Join-Path $SafetyBackup "BROKEN_GameController.Core.cs") -Force

if (Test-Path $V53File) {
    Copy-Item $V53File (Join-Path $SafetyBackup "PREVIOUS_GameController.V53SolidSceneryPlacement.cs") -Force
}

try {
    # 1. Restore the known-good, pre-V53 Core in full.
    Copy-Item $GoodBackup $CoreFile -Force

    $restored = Get-Content -Raw -Path $CoreFile

    if (-not (Test-GoodCore -Path $CoreFile)) {
        throw "Restored Core failed validation."
    }

    # 2. Re-apply ONE small authoritative V53 hook, using only the method
    # signature. No brace surgery, deletion, or comment anchors.
    if ($restored -notmatch 'V53ModelBaseOverlapsSolidAreaScenery\s*\(\s*movingModel\s*,\s*destination\s*\)') {

        $pattern = '(?s)(private\s+bool\s+CanPlaceModel\s*\(\s*ModelToken\s+movingModel\s*,\s*Vector3\s+destination\s*\)\s*\{)'

        $matches = [regex]::Matches($restored, $pattern)

        if ($matches.Count -ne 1) {
            throw "Expected exactly one CanPlaceModel method; found $($matches.Count)."
        }

        $hook = @'

        // V53: the Terrain Area footprint is legal standing space, but the
        // model BASE may not deploy/end a move overlapping the actual scenery.
        if (V53ModelBaseOverlapsSolidAreaScenery(
                movingModel,
                destination))
        {
            return false;
        }
'@

        $restored =
            [regex]::Replace(
                $restored,
                $pattern,
                ('$1' + $hook),
                1
            )

        Set-Content -Path $CoreFile -Value $restored -Encoding UTF8
    }

    # 3. Guarantee the corrected V53 helper is installed.
    Copy-Item $PayloadV53 $V53File -Force

    # 4. Final integrity validation AFTER patch.
    if (-not (Test-GoodCore -Path $CoreFile)) {
        throw "Core lost required methods after V53 hook insertion."
    }

    $verifyCore = Get-Content -Raw -Path $CoreFile
    $verifyV53 = Get-Content -Raw -Path $V53File

    if ($verifyCore -notmatch 'V53ModelBaseOverlapsSolidAreaScenery\s*\(\s*movingModel\s*,\s*destination\s*\)') {
        throw "V53 authoritative placement hook was not installed."
    }

    if ($verifyV53 -match 'List\s*<\s*PlacementGhostCandidate52\s*>' -or
        $verifyV53 -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery\s*<\s*T\s*>') {
        throw "Corrected V53 helper verification failed."
    }

    # The patched file should remain roughly the same size as the known-good
    # backup; this catches accidental truncation immediately.
    $goodLength = (Get-Content -Raw -Path $GoodBackup).Length
    $finalLength = $verifyCore.Length

    if ($finalLength -lt $goodLength -or
        $finalLength -gt ($goodLength + 3000)) {
        throw "Core size sanity check failed. Good=$goodLength Final=$finalLength"
    }
}
catch {
    Write-Host ""
    Write-Host "Recovery failed. Restoring the pre-recovery files..." -ForegroundColor Red

    Copy-Item (Join-Path $SafetyBackup "BROKEN_GameController.Core.cs") $CoreFile -Force

    $oldV53 = Join-Path $SafetyBackup "PREVIOUS_GameController.V53SolidSceneryPlacement.cs"
    if (Test-Path $oldV53) {
        Copy-Item $oldV53 $V53File -Force
    }

    Write-Host $_.Exception.Message
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "CORE RECOVERY V4 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "What this did:"
Write-Host "  1. Restored the intact GameController.Core.cs saved BEFORE V53 modified it."
Write-Host "  2. Verified the large set of missing core methods exists again."
Write-Host "  3. Re-applied only the tiny solid-scenery CanPlaceModel hook."
Write-Host "  4. Re-installed the corrected V53 helper with no PlacementGhostCandidate52 dependency."
Write-Host "  5. Checked the final Core file was not truncated."
Write-Host ""
Write-Host "Safety backup of the broken state:"
Write-Host "  $SafetyBackup" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"

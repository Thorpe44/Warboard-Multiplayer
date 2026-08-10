$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V53 - SOLID SCENERY END-POSITION FIX" -ForegroundColor Cyan
Write-Host "---------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 8; $i++) {
        $core = Join-Path $candidate "Assets\Scripts\Core\GameController.Core.cs"
        $v50 = Join-Path $candidate "Assets\Scripts\Core\GameController.V50TerrainAreaBattlefield.cs"
        $v52 = Join-Path $candidate "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"

        if ((Test-Path $core) -and
            (Test-Path $v50) -and
            (Test-Path $v52)) {
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
        $core = Join-Path $child.FullName "Assets\Scripts\Core\GameController.Core.cs"
        $v50 = Join-Path $child.FullName "Assets\Scripts\Core\GameController.V50TerrainAreaBattlefield.cs"
        $v52 = Join-Path $child.FullName "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"

        if ((Test-Path $core) -and
            (Test-Path $v50) -and
            (Test-Path $v52)) {
            return $child.FullName
        }
    }

    return $null
}

function Fail-And-Wait {
    param([string]$Message)

    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "Nothing has been changed."
    Read-Host "Press Enter to close"
    exit 1
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Fail-And-Wait "Could not find the Warboard project root containing V50 + V52."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$CoreDir = Join-Path $ProjectRoot "Assets\Scripts\Core"
$CoreFile = Join-Path $CoreDir "GameController.Core.cs"
$GhostFile = Join-Path $CoreDir "GameController.V52PlacementGhost.cs"
$BuildInfo = Join-Path $CoreDir "WarboardBuildInfo.cs"
$PayloadFile = Join-Path $ScriptDir "V53_PATCH_PAYLOAD\GameController.V53SolidSceneryPlacement.cs"
$TargetHelper = Join-Path $CoreDir "GameController.V53SolidSceneryPlacement.cs"

foreach ($required in @($CoreFile, $GhostFile, $BuildInfo, $PayloadFile)) {
    if (-not (Test-Path $required)) {
        Fail-And-Wait "Missing required file: $required"
    }
}

$coreText = Get-Content -Raw -Path $CoreFile
$ghostText = Get-Content -Raw -Path $GhostFile
$buildText = Get-Content -Raw -Path $BuildInfo

if ($coreText -notmatch 'private bool CanPlaceModel\s*\(') {
    Fail-And-Wait "Could not locate CanPlaceModel in GameController.Core.cs."
}

if ($ghostText -notmatch 'WARBOARD V52' -or
    $ghostText -notmatch 'DrawPlacementGhostPreview52') {
    Fail-And-Wait "The installed V52 ghost module was not recognised."
}

if ($buildText -notmatch 'CurrentVersion\s*=\s*"v(?:51|52|53)"') {
    Fail-And-Wait "Expected Warboard v51/v52/v53 build identity was not found."
}

$alreadyCore =
    $coreText -match 'V53ModelBaseOverlapsSolidAreaScenery\s*\('

$alreadyWholeGhost =
    $ghostText -match 'V53GhostCandidatesClearOfSolidAreaScenery\s*\(\s*candidates\s*\)'

if (-not $alreadyCore) {
    $anchor = @'
        // Destination may not sit inside terrain.
        Vector3 testPoint =
'@

    if (-not $coreText.Contains($anchor)) {
        Fail-And-Wait "Could not locate the V52 CanPlaceModel terrain anchor. Project was not modified."
    }
}

# Verify the two V52 branches before writing anything.
$wholeOld = @'
                bool legal =
                    CandidateBoardLegal52(
                        candidates) &&
                    selectedSquad
                        .CanTranslateWithinNormalMove(
                            delta
                        );
'@

$wholeNew = @'
                bool legal =
                    CandidateBoardLegal52(
                        candidates) &&
                    V53GhostCandidatesClearOfSolidAreaScenery(
                        candidates) &&
                    selectedSquad
                        .CanTranslateWithinNormalMove(
                            delta
                        );
'@

$specialOld = @'
            bool basicLegal =
                distance <=
                    specialMoveMaxDistance +
                    0.001f &&
                CandidateBoardLegal52(
                    candidates
                );
'@

$specialNew = @'
            bool basicLegal =
                distance <=
                    specialMoveMaxDistance +
                    0.001f &&
                CandidateBoardLegal52(
                    candidates
                ) &&
                V53GhostCandidatesClearOfSolidAreaScenery(
                    candidates
                );
'@

if (-not $alreadyWholeGhost) {
    if (-not $ghostText.Contains($wholeOld)) {
        Fail-And-Wait "Could not locate the V52 whole-unit ghost legality block."
    }

    if (-not $ghostText.Contains($specialOld)) {
        Fail-And-Wait "Could not locate the V52 special-move ghost legality block."
    }
}

# All validation passed. Back up before writes.
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\V53\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Copy-Item $CoreFile (Join-Path $BackupDir "GameController.Core.cs") -Force
Copy-Item $GhostFile (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") -Force
Copy-Item $BuildInfo (Join-Path $BackupDir "WarboardBuildInfo.cs") -Force

if (Test-Path $TargetHelper) {
    Copy-Item $TargetHelper (Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs") -Force
}

try {
    Copy-Item $PayloadFile $TargetHelper -Force

    if (-not $alreadyCore) {
        $insert = @'
        // V53: Terrain Area footprints are legal, but the model's circular
        // base may not finish/deploy overlapping the actual ruin/wall/rubble
        // geometry sitting on that footprint.
        if (V53ModelBaseOverlapsSolidAreaScenery(
                movingModel,
                destination))
        {
            return false;
        }

        // Destination may not sit inside terrain.
        Vector3 testPoint =
'@

        $coreText = $coreText.Replace($anchor, $insert)
        Set-Content -Path $CoreFile -Value $coreText -Encoding UTF8
    }

    if (-not $alreadyWholeGhost) {
        $ghostText = $ghostText.Replace($wholeOld, $wholeNew)
        $ghostText = $ghostText.Replace($specialOld, $specialNew)
        Set-Content -Path $GhostFile -Value $ghostText -Encoding UTF8
    }

    $buildText = Get-Content -Raw -Path $BuildInfo
    $buildText = [regex]::Replace(
        $buildText,
        'CurrentVersion\s*=\s*"v(?:51|52|53)"',
        'CurrentVersion = "v53"'
    )
    Set-Content -Path $BuildInfo -Value $buildText -Encoding UTF8

    # Verification
    $verifyCore = Get-Content -Raw -Path $CoreFile
    $verifyGhost = Get-Content -Raw -Path $GhostFile
    $verifyHelper = Get-Content -Raw -Path $TargetHelper
    $verifyBuild = Get-Content -Raw -Path $BuildInfo

    if ($verifyCore -notmatch 'V53ModelBaseOverlapsSolidAreaScenery\s*\(' -or
        $verifyGhost -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery\s*\(' -or
        $verifyHelper -notmatch 'WARBOARD V53' -or
        $verifyBuild -notmatch 'CurrentVersion\s*=\s*"v53"') {
        throw "Post-install verification failed."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring V52 backup..." -ForegroundColor Red

    Copy-Item (Join-Path $BackupDir "GameController.Core.cs") $CoreFile -Force
    Copy-Item (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") $GhostFile -Force
    Copy-Item (Join-Path $BackupDir "WarboardBuildInfo.cs") $BuildInfo -Force

    $oldHelper = Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs"

    if (Test-Path $oldHelper) {
        Copy-Item $oldHelper $TargetHelper -Force
    }
    elseif (Test-Path $TargetHelper) {
        Remove-Item $TargetHelper -Force
    }

    Write-Host $_.Exception.Message
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "V53 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Fixed:"
Write-Host "  - Terrain Area footprint remains legal deployment/movement space."
Write-Host "  - A model BASE can no longer finish/deploy through a V50 ruin/wall/rubble piece."
Write-Host "  - The ghost turns RED when its base overlaps solid scenery."
Write-Host "  - Whole-unit and special-move ghosts now check the same solid scenery."
Write-Host "  - Existing movement-path rules are untouched, so permitted units can still move THROUGH ruin walls."
Write-Host ""
Write-Host "Build identity: v53"
Write-Host "Backup: $BackupDir" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile, then try the same placement again." -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to close"

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V53 RECOVERY V2 - SOLID SCENERY END-POSITION FIX" -ForegroundColor Cyan
Write-Host "----------------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail-And-Wait {
    param([string]$Message)

    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "Project files were not intentionally changed."
    Read-Host "Press Enter to close"
    exit 1
}

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
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

# Returns the opening and matching closing brace of a method. This deliberately
# avoids depending on exact whitespace/comments from previous patch versions.
function Get-MethodRange {
    param(
        [string]$Text,
        [string]$SignatureRegex
    )

    $match =
        [regex]::Match(
            $Text,
            $SignatureRegex,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    if (-not $match.Success) {
        return $null
    }

    $open =
        $Text.IndexOf(
            "{",
            $match.Index + $match.Length
        )

    if ($open -lt 0) {
        return $null
    }

    $depth = 0

    for ($i = $open; $i -lt $Text.Length; $i++) {
        $ch = $Text[$i]

        if ($ch -eq "{") {
            $depth++
        }
        elseif ($ch -eq "}") {
            $depth--

            if ($depth -eq 0) {
                return @{
                    Open = $open
                    Close = $i
                }
            }
        }
    }

    return $null
}

function Insert-AfterMethodOpen {
    param(
        [string]$Text,
        [hashtable]$Range,
        [string]$Insertion
    )

    $before =
        $Text.Substring(
            0,
            $Range.Open + 1
        )

    $after =
        $Text.Substring(
            $Range.Open + 1
        )

    return
        $before +
        "`r`n" +
        $Insertion +
        $after
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Fail-And-Wait "Could not find the Warboard project root containing V50 and V52."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$CoreDir = Join-Path $ProjectRoot "Assets\Scripts\Core"
$CoreFile = Join-Path $CoreDir "GameController.Core.cs"
$GhostFile = Join-Path $CoreDir "GameController.V52PlacementGhost.cs"
$BuildInfo = Join-Path $CoreDir "WarboardBuildInfo.cs"
$PayloadFile = Join-Path $ScriptDir "V53_PATCH_PAYLOAD\GameController.V53SolidSceneryPlacement.cs"
$TargetHelper = Join-Path $CoreDir "GameController.V53SolidSceneryPlacement.cs"

foreach ($required in @(
    $CoreFile,
    $GhostFile,
    $BuildInfo,
    $PayloadFile
)) {
    if (-not (Test-Path $required)) {
        Fail-And-Wait "Missing required file: $required"
    }
}

$coreText = Get-Content -Raw -Path $CoreFile
$ghostText = Get-Content -Raw -Path $GhostFile
$buildText = Get-Content -Raw -Path $BuildInfo

$coreRange =
    Get-MethodRange `
        -Text $coreText `
        -SignatureRegex '\bprivate\s+bool\s+CanPlaceModel\s*\('

if (-not $coreRange) {
    Fail-And-Wait "Could not locate the CanPlaceModel method structurally."
}

$ghostRange =
    Get-MethodRange `
        -Text $ghostText `
        -SignatureRegex '\b(?:private|public)\s+bool\s+CandidateBoardLegal52\s*\('

if (-not $ghostRange) {
    Fail-And-Wait "Could not locate the V52 CandidateBoardLegal52 method structurally."
}

if ($ghostText -notmatch 'WARBOARD V52' -and
    $ghostText -notmatch 'PlacementGhostCandidate52') {
    Fail-And-Wait "The installed V52 placement ghost module was not recognised."
}

if ($buildText -notmatch 'CurrentVersion\s*=\s*"v(?:51|52|53)"') {
    Fail-And-Wait "Expected v51/v52/v53 build identity was not found."
}

# Validate the helper payload BEFORE creating a backup/writing.
$helperText = Get-Content -Raw -Path $PayloadFile

if ($helperText -notmatch 'WARBOARD V53 RECOVERY V2' -or
    $helperText -notmatch 'V53ModelBaseOverlapsSolidAreaScenery' -or
    $helperText -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery') {
    Fail-And-Wait "The V53 helper payload failed validation."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\V53_RECOVERY_V2\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Copy-Item $CoreFile (Join-Path $BackupDir "GameController.Core.cs") -Force
Copy-Item $GhostFile (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") -Force
Copy-Item $BuildInfo (Join-Path $BackupDir "WarboardBuildInfo.cs") -Force

if (Test-Path $TargetHelper) {
    Copy-Item $TargetHelper (Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs") -Force
}

try {
    Copy-Item $PayloadFile $TargetHelper -Force

    # Patch the actual authoritative placement function.
    if ($coreText -notmatch 'V53ModelBaseOverlapsSolidAreaScenery\s*\(\s*movingModel\s*,\s*destination\s*\)') {
        $coreInsertion = @'

        // V53: the Terrain Area itself is legal, but a model base may not
        // deploy/end a move overlapping the actual scenery on that area.
        if (V53ModelBaseOverlapsSolidAreaScenery(
                movingModel,
                destination))
        {
            return false;
        }
'@

        $coreText =
            Insert-AfterMethodOpen `
                -Text $coreText `
                -Range $coreRange `
                -Insertion $coreInsertion

        Set-Content -Path $CoreFile -Value $coreText -Encoding UTF8
    }

    # Patch V52 at the CENTRAL candidate legality function. This is much more
    # robust than matching individual whole-unit/special-move preview branches.
    $ghostText = Get-Content -Raw -Path $GhostFile

    if ($ghostText -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery\s*\(\s*candidates\s*\)') {
        $ghostRange =
            Get-MethodRange `
                -Text $ghostText `
                -SignatureRegex '\b(?:private|public)\s+bool\s+CandidateBoardLegal52\s*\('

        if (-not $ghostRange) {
            throw "CandidateBoardLegal52 disappeared before write."
        }

        $ghostInsertion = @'

        // V53: keep V52's preview in sync with authoritative placement.
        if (!V53GhostCandidatesClearOfSolidAreaScenery(
                candidates))
        {
            return false;
        }
'@

        $ghostText =
            Insert-AfterMethodOpen `
                -Text $ghostText `
                -Range $ghostRange `
                -Insertion $ghostInsertion

        Set-Content -Path $GhostFile -Value $ghostText -Encoding UTF8
    }

    $buildText = Get-Content -Raw -Path $BuildInfo

    $buildText =
        [regex]::Replace(
            $buildText,
            'CurrentVersion\s*=\s*"v(?:51|52|53)"',
            'CurrentVersion = "v53"'
        )

    Set-Content -Path $BuildInfo -Value $buildText -Encoding UTF8

    # Final verification.
    $verifyCore = Get-Content -Raw -Path $CoreFile
    $verifyGhost = Get-Content -Raw -Path $GhostFile
    $verifyHelper = Get-Content -Raw -Path $TargetHelper
    $verifyBuild = Get-Content -Raw -Path $BuildInfo

    if ($verifyCore -notmatch 'V53ModelBaseOverlapsSolidAreaScenery\s*\(\s*movingModel\s*,\s*destination\s*\)') {
        throw "Core placement hook verification failed."
    }

    if ($verifyGhost -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery\s*\(\s*candidates\s*\)') {
        throw "V52 ghost legality hook verification failed."
    }

    if ($verifyHelper -notmatch 'V53BaseCircleIntersectsColliderXZ') {
        throw "V53 helper verification failed."
    }

    if ($verifyBuild -notmatch 'CurrentVersion\s*=\s*"v53"') {
        throw "Build identity verification failed."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring backup..." -ForegroundColor Red

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
Write-Host "V53 RECOVERY V2 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "What changed:"
Write-Host "  - No fragile text/comment anchor is used anymore."
Write-Host "  - CanPlaceModel is patched structurally."
Write-Host "  - V52 CandidateBoardLegal52 is patched centrally."
Write-Host "  - Terrain Area footprint remains legal."
Write-Host "  - Base overlap with actual V50 scenery is illegal."
Write-Host "  - Ghost should turn RED over the wall/ruin."
Write-Host "  - Moving THROUGH permitted ruin walls is unchanged."
Write-Host ""
Write-Host "Build identity: v53"
Write-Host "Backup: $BackupDir" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity, let it compile, and repeat the placement test." -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to close"

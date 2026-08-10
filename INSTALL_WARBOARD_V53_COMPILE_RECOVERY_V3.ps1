$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V53 - COMPILE RECOVERY V3" -ForegroundColor Cyan
Write-Host "-----------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
        $target = Join-Path $candidate "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs"
        if (Test-Path $target) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) {
            break
        }

        $candidate = $parent
    }

    foreach ($child in Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue) {
        $target = Join-Path $child.FullName "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs"
        if (Test-Path $target) {
            return $child.FullName
        }
    }

    return $null
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Write-Host "ERROR: Could not find GameController.V53SolidSceneryPlacement.cs" -ForegroundColor Red
    Write-Host "Extract this ZIP into the Warboard project root and run the BAT again."
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Target = Join-Path $ProjectRoot "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs"
$Payload = Join-Path $ScriptDir "V53_PATCH_PAYLOAD\GameController.V53SolidSceneryPlacement.cs"

if (-not (Test-Path $Payload)) {
    Write-Host "ERROR: Corrected V53 payload is missing." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\V53_CompileRecoveryV3\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
Copy-Item $Target (Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs") -Force

try {
    Copy-Item $Payload $Target -Force

    $verify = Get-Content -Raw -Path $Target

    if ([string]::IsNullOrWhiteSpace($verify)) {
        throw "Replacement V53 file is empty."
    }

    if ($verify -match 'List\s*<\s*PlacementGhostCandidate52\s*>') {
        throw "Old direct PlacementGhostCandidate52 dependency is still present."
    }

    if ($verify -notmatch 'V53GhostCandidatesClearOfSolidAreaScenery\s*<\s*T\s*>') {
        throw "Corrected generic ghost helper was not installed."
    }

    if ($verify -notmatch 'V53ModelBaseOverlapsSolidAreaScenery') {
        throw "Solid scenery placement helper is missing."
    }
}
catch {
    Write-Host ""
    Write-Host "Replacement failed. Restoring backup..." -ForegroundColor Red
    Copy-Item (Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs") $Target -Force
    Write-Host $_.Exception.Message
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "Compile Recovery V3 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Fixed:"
Write-Host "  - V53 no longer directly references PlacementGhostCandidate52."
Write-Host "  - No edits to the V52 file are required."
Write-Host "  - Existing solid-scenery placement behaviour is retained."
Write-Host ""
Write-Host "Return to Unity and let it compile." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"

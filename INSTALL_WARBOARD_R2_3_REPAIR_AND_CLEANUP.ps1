$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R2.3 - MODEL/UI REPAIR + PROJECT CLEANUP" -ForegroundColor Cyan
Write-Host "-------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail {
    param([string]$Message)
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "Any changed source files were restored from the backup where possible." -ForegroundColor Red
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

function Remove-DisposableProjectArtifacts {
    param(
        [string]$ProjectRoot,
        [string]$CurrentPs1
    )

    Write-Host ""
    Write-Host "Cleaning old patch/install files..." -ForegroundColor Cyan

    $rootPatterns = @(
        "INSTALL_WARBOARD_*.bat",
        "INSTALL_WARBOARD_*.ps1",
        "FIX_WARBOARD_*.bat",
        "FIX_WARBOARD_*.ps1",
        "RECOVER_WARBOARD_*.bat",
        "RECOVER_WARBOARD_*.ps1",
        "RESET_WARBOARD_*.bat",
        "RESET_WARBOARD_*.ps1",
        "CLEAN_WARBOARD_*.bat",
        "CLEAN_WARBOARD_*.ps1",
        "WARBOARD_*.zip",
        "Warboard_*.zip",
        "V*_README.txt",
        "R*_README.txt",
        "*_PATCH_README.txt",
        "CLEANUP_README.txt"
    )

    $currentPs1Full = $null
    try { $currentPs1Full = (Resolve-Path $CurrentPs1).Path } catch {}

    foreach ($pattern in $rootPatterns) {
        Get-ChildItem -Path $ProjectRoot -File -Filter $pattern -ErrorAction SilentlyContinue |
            ForEach-Object {
                $full = $_.FullName

                if ($currentPs1Full -and $full -eq $currentPs1Full) {
                    return
                }

                try {
                    Remove-Item $full -Force -ErrorAction Stop
                    Write-Host "  removed $($_.Name)" -ForegroundColor DarkGray
                }
                catch {
                    Write-Host "  could not remove $($_.Name): $($_.Exception.Message)" -ForegroundColor Yellow
                }
            }
    }

    $payloadDirs = @(
        (Join-Path $ProjectRoot "PATCH_PAYLOAD")
    )

    Get-ChildItem -Path $ProjectRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like "V*_PATCH_PAYLOAD" -or
            $_.Name -like "R*_PATCH_PAYLOAD"
        } |
        ForEach-Object {
            $payloadDirs += $_.FullName
        }

    foreach ($dir in ($payloadDirs | Select-Object -Unique)) {
        if (Test-Path $dir) {
            try {
                Remove-Item $dir -Recurse -Force -ErrorAction Stop
                Write-Host "  removed $(Split-Path -Leaf $dir)" -ForegroundColor DarkGray
            }
            catch {
                Write-Host "  could not remove $(Split-Path -Leaf $dir): $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }

    Get-ChildItem -Path (Join-Path $ProjectRoot "Assets") -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -like "*.bak" -or
            $_.Name -like "*.bak.meta" -or
            $_.Name -like "before_*.bak"
        } |
        ForEach-Object {
            try {
                Remove-Item $_.FullName -Force -ErrorAction Stop
                Write-Host "  removed Assets backup: $($_.Name)" -ForegroundColor DarkGray
            }
            catch {}
        }

    $markerDir = Join-Path $ProjectRoot "Library\WarboardInstallerMarkers"
    if (Test-Path $markerDir) {
        try {
            Remove-Item $markerDir -Recurse -Force -ErrorAction Stop
            Write-Host "  removed installer markers" -ForegroundColor DarkGray
        }
        catch {}
    }

    Write-Host "Cleanup complete. Game code/assets, Packages, ProjectSettings, model packs and Library backups were preserved." -ForegroundColor Green
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir
if (-not $ProjectRoot) { Fail "Could not locate the Warboard Unity project root." }

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Core = Join-Path $ProjectRoot "Assets\Scripts\Core"
$Cards = Join-Path $Core "WarboardV55MissionCardsWorld.cs"
$Squad = Join-Path $Core "SquadController.cs"
$Resolver = Join-Path $Core "NecronModelPackResolverR22.cs"
$OriginRepair = Join-Path $Core "NecronVisualOriginRepairR23.cs"

foreach ($required in @($Cards, $Squad, $Resolver)) {
    if (-not (Test-Path $required)) {
        Fail "R2.2 prerequisite missing: $required"
    }
}

$cardsExisting = Get-Content -Raw -Path $Cards
$resolverExisting = Get-Content -Raw -Path $Resolver
$squadExisting = Get-Content -Raw -Path $Squad

if ($cardsExisting -notmatch 'WARBOARD_MISSION_CARD_ROW_R2_2' -and
    $cardsExisting -notmatch 'WARBOARD_MISSION_CARD_ROW_R2_3') {
    Fail "Mission card file is not the expected R2.2/R2.3 source."
}

if ($resolverExisting -notmatch 'WARBOARD_NECRON_MODEL_RESOLVER_R2_2') {
    Fail "Necron R2.2 resolver is not installed."
}

if ($squadExisting -notmatch 'NecronModelPackResolverR22\.TryResolve') {
    Fail "SquadController is not wired to the Necron model pack."
}

$PayloadCore = Join-Path $ScriptDir "PATCH_PAYLOAD\Assets\Scripts\Core"
$PayloadCards = Join-Path $PayloadCore "WarboardV55MissionCardsWorld.cs"
$PayloadOrigin = Join-Path $PayloadCore "NecronVisualOriginRepairR23.cs"

foreach ($required in @($PayloadCards, $PayloadOrigin)) {
    if (-not (Test-Path $required)) {
        Fail "Patch payload missing: $required"
    }
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$Backup = Join-Path $ProjectRoot "Library\WarboardBackups\R2_3_REPAIR\$timestamp"
New-Item -ItemType Directory -Force -Path $Backup | Out-Null

$backupRel = @(
    "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs",
    "Assets\Scripts\Core\SquadController.cs",
    "Assets\Scripts\Core\NecronVisualOriginRepairR23.cs"
)

foreach ($relative in $backupRel) {
    $source = Join-Path $ProjectRoot $relative
    if (-not (Test-Path $source)) { continue }

    $dest = Join-Path $Backup $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $source $dest -Force
}

try {
    Copy-Item $PayloadCards $Cards -Force
    Copy-Item $PayloadOrigin $OriginRepair -Force

    $squadText = Get-Content -Raw -Path $Squad

    if ($squadText -notmatch 'NecronVisualOriginRepairR23\.Reanchor') {
        $pattern = '(?s)if\s*\(\s*visual\s*!=\s*null\s*\)\s*\{\s*token\.AttachVisual\(\s*visual,\s*baseColor\s*\);\s*\}'

        $replacement = @'
if (visual != null)
            {
                bool visualAttached =
                    token.AttachVisual(
                        visual,
                        baseColor
                    );

                if (visualAttached)
                {
                    NecronVisualOriginRepairR23.Reanchor(
                        FactionId,
                        token
                    );
                }
            }
'@

        $matches = [regex]::Matches($squadText, $pattern)
        if ($matches.Count -ne 1) {
            throw "Expected one final token.AttachVisual block in SquadController; found $($matches.Count)."
        }

        $squadText = [regex]::Replace($squadText, $pattern, $replacement, 1)
        Set-Content -Path $Squad -Value $squadText -Encoding UTF8
    }

    $verifyCards = Get-Content -Raw -Path $Cards
    $verifyOrigin = Get-Content -Raw -Path $OriginRepair
    $verifySquad = Get-Content -Raw -Path $Squad

    if ($verifyCards -notmatch 'WARBOARD_MISSION_CARD_ROW_R2_3' -or
        $verifyCards -notmatch 'SetParent\(scoreboardRoot,\s*false\)') {
        throw "R2.3 scoreboard-relative mission-card verification failed."
    }

    if ($verifyOrigin -notmatch 'WARBOARD_NECRON_VISUAL_ORIGIN_REPAIR_R2_3' -or
        $verifyOrigin -notmatch 'combined\.min\.y') {
        throw "R2.3 Necron OBJ-origin repair verification failed."
    }

    $repairCalls = ([regex]::Matches($verifySquad, 'NecronVisualOriginRepairR23\.Reanchor')).Count
    if ($repairCalls -ne 1) {
        throw "Expected one R2.3 Necron visual repair call in SquadController; found $repairCalls."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring source backup..." -ForegroundColor Red

    foreach ($relative in $backupRel) {
        $saved = Join-Path $Backup $relative
        $dest = Join-Path $ProjectRoot $relative

        if (Test-Path $saved) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Copy-Item $saved $dest -Force
        }
        elseif ($relative -like "*NecronVisualOriginRepairR23.cs" -and (Test-Path $dest)) {
            Remove-Item $dest -Force
        }
    }

    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

# Source install passed. Now clean disposable patch clutter from the project root.
Remove-DisposableProjectArtifacts -ProjectRoot $ProjectRoot -CurrentPs1 $MyInvocation.MyCommand.Path

# Also remove this extracted package's disposable payload/readme. The BAT wrapper
# removes the PS1 and BAT themselves after PowerShell has fully exited.
$localPayload = Join-Path $ScriptDir "PATCH_PAYLOAD"
if (Test-Path $localPayload) {
    try { Remove-Item $localPayload -Recurse -Force } catch {}
}

$localReadme = Join-Path $ScriptDir "CLEANUP_README.txt"
if (Test-Path $localReadme) {
    try { Remove-Item $localReadme -Force } catch {}
}

Write-Host ""
Write-Host "R2.3 installed and project patch files cleaned." -ForegroundColor Green
Write-Host ""
Write-Host "What changed:" -ForegroundColor Cyan
Write-Host "  - mission cards are children of the actual World Scoreboard"
Write-Host "  - their Y/Z/orientation therefore cannot drift above it"
Write-Host "  - obviously displaced Necron OBJ geometry is recentered onto its token/base"
Write-Host "  - old INSTALL/FIX/RECOVER/RESET/CLEAN scripts, patch ZIPs/payloads and .bak files were removed"
Write-Host ""
Write-Host "Preserved:" -ForegroundColor Cyan
Write-Host "  - all Assets game code and model packs"
Write-Host "  - Packages and ProjectSettings"
Write-Host "  - .git/.gitignore"
Write-Host "  - Library/WarboardBackups (including this R2.3 rollback backup)"
Write-Host ""
Write-Host "Backup: $Backup" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity, allow compilation, then START A FRESH BATTLE." -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to close"
exit 0

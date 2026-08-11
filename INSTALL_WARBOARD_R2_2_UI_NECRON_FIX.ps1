$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R2.2 - UI + NECRON MODEL FIX" -ForegroundColor Cyan
Write-Host "--------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Fail {
    param([string]$Message)
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "Nothing was intentionally left half-installed." -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
        if (Test-Path (Join-Path $candidate "Assets\Scripts\Core\GameController.cs")) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) { break }
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
if (-not $ProjectRoot) { Fail "Could not locate the Warboard project root." }

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Core = Join-Path $ProjectRoot "Assets\Scripts\Core"
$Squad = Join-Path $Core "SquadController.cs"
$Tooltip = Join-Path $Core "WarboardTerrainTooltipR2.cs"
$Cards = Join-Path $Core "WarboardV55MissionCardsWorld.cs"
$NecronIndex = Join-Path $ProjectRoot "Assets\Resources\Armies\Models\Necrons\ModelIndex.json"
$NecronPool = Join-Path $ProjectRoot "Assets\Resources\Armies\Models\Necrons\ModelPool"

foreach ($required in @($Squad, $Tooltip, $Cards, $NecronIndex, $NecronPool)) {
    if (-not (Test-Path $required)) { Fail "Missing required current file: $required" }
}

$tooltipText = Get-Content -Raw -Path $Tooltip
$cardsText = Get-Content -Raw -Path $Cards
$squadText = Get-Content -Raw -Path $Squad

if ($tooltipText -notmatch 'WARBOARD_TERRAIN_TOOLTIP_R2') {
    Fail "The installed terrain tooltip was not recognised as R2."
}

if ($cardsText -notmatch 'WARBOARD_MISSION_CARD_RACK_R2' -and
    $cardsText -notmatch 'WARBOARD_MISSION_CARD_ROW_R2_1' -and
    $cardsText -notmatch 'WARBOARD_V55_WORLD_MISSION_CARDS') {
    Fail "The installed mission-card source was not recognised."
}

if ($squadText -notmatch 'ModelVisualRegistry\.Resolve\s*\(') {
    Fail "SquadController model-visual call was not found."
}

$objCount = @(Get-ChildItem -Path $NecronPool -Filter "*.obj" -File -ErrorAction SilentlyContinue).Count
if ($objCount -lt 1) {
    Fail "Necron ModelPool exists but contains no OBJ files."
}

$Payload = Join-Path $ScriptDir "PATCH_PAYLOAD"
$PayloadTooltip = Join-Path $Payload "Assets\Scripts\Core\WarboardTerrainTooltipR2.cs"
$PayloadCards = Join-Path $Payload "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs"
$PayloadResolver = Join-Path $Payload "Assets\Scripts\Core\NecronModelPackResolverR22.cs"

foreach ($required in @($PayloadTooltip, $PayloadCards, $PayloadResolver)) {
    if (-not (Test-Path $required)) { Fail "Patch payload is incomplete: $required" }
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$Backup = Join-Path $ProjectRoot "Library\WarboardBackups\R2_2_UI_NECRON_FIX\$timestamp"
New-Item -ItemType Directory -Force -Path $Backup | Out-Null

$backupFiles = @(
    "Assets\Scripts\Core\SquadController.cs",
    "Assets\Scripts\Core\WarboardTerrainTooltipR2.cs",
    "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs",
    "Assets\Scripts\Core\NecronModelPackResolverR22.cs",
    "Assets\Scripts\Core\WarboardMissionCardRowR21.cs"
)

foreach ($relative in $backupFiles) {
    $source = Join-Path $ProjectRoot $relative
    if (-not (Test-Path $source)) { continue }

    $dest = Join-Path $Backup $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $source $dest -Force
}

try {
    Copy-Item $PayloadTooltip $Tooltip -Force
    Copy-Item $PayloadCards $Cards -Force
    Copy-Item $PayloadResolver (Join-Path $Core "NecronModelPackResolverR22.cs") -Force

    # Remove a stale companion-row file from the earlier failed R2.1 approach if present.
    $staleRow = Join-Path $Core "WarboardMissionCardRowR21.cs"
    if (Test-Path $staleRow) {
        Remove-Item $staleRow -Force
    }

    $squadText = Get-Content -Raw -Path $Squad

    if ($squadText -notmatch 'NecronModelPackResolverR22\.TryResolve') {
        $previewPattern = '(?s)ModelVisualDefinition\s+previewVisual\s*=\s*ModelVisualRegistry\.Resolve\(\s*DisplayName,\s*previewRoleName,\s*i\s*\);'
        $previewReplacement = @'
ModelVisualDefinition previewVisual =
                NecronModelPackResolverR22.TryResolve(
                    FactionId,
                    DisplayName,
                    previewRoleName,
                    i
                ) ??
                ModelVisualRegistry.Resolve(
                    DisplayName,
                    previewRoleName,
                    i
                );
'@

        $previewMatches = [regex]::Matches($squadText, $previewPattern)
        if ($previewMatches.Count -ne 1) {
            throw "Expected one preview ModelVisualRegistry.Resolve call; found $($previewMatches.Count)."
        }

        $squadText = [regex]::Replace($squadText, $previewPattern, $previewReplacement, 1)

        $visualPattern = '(?s)ModelVisualDefinition\s+visual\s*=\s*ModelVisualRegistry\.Resolve\(\s*DisplayName,\s*roleName,\s*i\s*\);'
        $visualReplacement = @'
ModelVisualDefinition visual =
                NecronModelPackResolverR22.TryResolve(
                    FactionId,
                    DisplayName,
                    roleName,
                    i
                ) ??
                ModelVisualRegistry.Resolve(
                    DisplayName,
                    roleName,
                    i
                );
'@

        $visualMatches = [regex]::Matches($squadText, $visualPattern)
        if ($visualMatches.Count -ne 1) {
            throw "Expected one final ModelVisualRegistry.Resolve call; found $($visualMatches.Count)."
        }

        $squadText = [regex]::Replace($squadText, $visualPattern, $visualReplacement, 1)
        Set-Content -Path $Squad -Value $squadText -Encoding UTF8
    }

    $verifyTooltip = Get-Content -Raw -Path $Tooltip
    $verifyCards = Get-Content -Raw -Path $Cards
    $verifySquad = Get-Content -Raw -Path $Squad
    $verifyResolver = Get-Content -Raw -Path (Join-Path $Core "NecronModelPackResolverR22.cs")

    if ($verifyTooltip -notmatch 'WARBOARD_TERRAIN_TOOLTIP_R2_2' -or
        $verifyTooltip -match 'private\s+void\s+Awake\s*\(') {
        throw "GUI-safe tooltip verification failed."
    }

    if ($verifyCards -notmatch 'WARBOARD_MISSION_CARD_ROW_R2_2') {
        throw "Mission-card row verification failed."
    }

    $resolverCalls = ([regex]::Matches($verifySquad, 'NecronModelPackResolverR22\.TryResolve')).Count
    if ($resolverCalls -ne 2) {
        throw "Necron resolver should be wired into both SquadController visual passes; found $resolverCalls calls."
    }

    if ($verifyResolver -notmatch 'WARBOARD_NECRON_MODEL_RESOLVER_R2_2' -or
        $verifyResolver -notmatch 'rawPosition\s*-\s*anchor') {
        throw "Necron resolver verification failed."
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring backup..." -ForegroundColor Red

    foreach ($relative in $backupFiles) {
        $saved = Join-Path $Backup $relative
        $dest = Join-Path $ProjectRoot $relative

        if (Test-Path $saved) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Copy-Item $saved $dest -Force
        }
        elseif ((Test-Path $dest) -and ($relative -match 'NecronModelPackResolverR22|WarboardMissionCardRowR21')) {
            Remove-Item $dest -Force
        }
    }

    Write-Host $_.Exception.Message
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "R2.2 installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Fixed:"
Write-Host "  - terrain hover GUI exception (GUI.skin now only used inside OnGUI)"
Write-Host "  - mission cards + scoreboard are one horizontal row"
Write-Host "  - full primary/secondary mission text remains on each card"
Write-Host "  - Necron model pack is now actually wired into SquadController"
Write-Host "  - Main/colourful Necron source is preferred; Backup fills missing units"
Write-Host "  - raw TTS component positions are re-anchored onto the game token"
Write-Host ""
Write-Host "Necron OBJ files detected: $objCount" -ForegroundColor DarkGray
Write-Host "Backup: $Backup" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity and let it compile/reimport, then START A FRESH BATTLE." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to close"

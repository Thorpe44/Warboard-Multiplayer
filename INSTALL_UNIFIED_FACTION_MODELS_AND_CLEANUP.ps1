$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD - UNIFIED FACTION MODEL PIPELINE + CLEANUP" -ForegroundColor Cyan
Write-Host "----------------------------------------------------" -ForegroundColor DarkCyan
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

    for ($i = 0; $i -lt 12; $i++) {
        if ((Test-Path (Join-Path $candidate "Assets\Scripts\Core\SquadController.cs")) -and
            (Test-Path (Join-Path $candidate "Assets\Scripts\Core\ModelToken.cs")) -and
            (Test-Path (Join-Path $candidate "Assets\Scripts\Core\ModelVisualRegistry.cs"))) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $candidate) {
            break
        }

        $candidate = $parent
    }

    return $null
}

function Safe-Remove {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    try {
        Remove-Item $Path -Recurse -Force -ErrorAction Stop
        Write-Host "  removed: $Path" -ForegroundColor DarkGray
    }
    catch {
        Write-Host "  could not remove: $Path" -ForegroundColor Yellow
    }
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Fail "Could not locate the Warboard project root."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Core = Join-Path $ProjectRoot "Assets\Scripts\Core"
$Squad = Join-Path $Core "SquadController.cs"
$ModelToken = Join-Path $Core "ModelToken.cs"
$AeldariRegistry = Join-Path $Core "ModelVisualRegistry.cs"
$CustodesResolver = Join-Path $Core "CustodesModelPackResolver.cs"
$GenericResolver = Join-Path $Core "FactionModelPackResolver.cs"

foreach ($required in @(
    $Squad,
    $ModelToken,
    $AeldariRegistry,
    $CustodesResolver
)) {
    if (-not (Test-Path $required)) {
        Fail "Missing required working model-pipeline file: $required"
    }
}

# Validate the proven model path before changing anything.
$modelTokenText = Get-Content -Raw -Path $ModelToken
$aeldariText = Get-Content -Raw -Path $AeldariRegistry
$custodesText = Get-Content -Raw -Path $CustodesResolver
$squadText = Get-Content -Raw -Path $Squad

if ($modelTokenText -notmatch 'public\s+bool\s+AttachVisual\s*\(' -or
    $modelTokenText -notmatch 'instance\.transform\.localPosition\s*=\s*component\.LocalPosition') {
    Fail "ModelToken.AttachVisual no longer matches the proven Aeldari/Custodes local-transform pipeline."
}

if ($aeldariText -notmatch 'AeldariPackIndexResource' -or
    $aeldariText -notmatch 'ModelVisualDefinition') {
    Fail "Aeldari model registry was not recognised."
}

if ($custodesText -notmatch 'CustodesModelPackResolver' -or
    $custodesText -notmatch 'ModelVisualComponentDefinition') {
    Fail "Custodes model resolver was not recognised."
}

if ($squadText -notmatch 'NecronModelPackResolverR22\.TryResolve' -or
    $squadText -notmatch 'NecronVisualOriginRepairR23\.Reanchor') {
    Fail "The current R2.2/R2.3 Necron hacks were not found. Nothing changed."
}

$PayloadResolver = Join-Path $ScriptDir "PATCH_PAYLOAD\Assets\Scripts\Core\FactionModelPackResolver.cs"

if (-not (Test-Path $PayloadResolver)) {
    Fail "FactionModelPackResolver.cs is missing from the patch payload."
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot "Library\WarboardBackups\UNIFIED_FACTION_MODELS\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

$backupFiles = @(
    "Assets\Scripts\Core\SquadController.cs",
    "Assets\Scripts\Core\NecronModelPackResolverR22.cs",
    "Assets\Scripts\Core\NecronModelPackResolverR22.cs.meta",
    "Assets\Scripts\Core\NecronVisualOriginRepairR23.cs",
    "Assets\Scripts\Core\NecronVisualOriginRepairR23.cs.meta",
    "Assets\Scripts\Core\FactionModelPackResolver.cs",
    "Assets\Scripts\Core\FactionModelPackResolver.cs.meta"
)

foreach ($relative in $backupFiles) {
    $source = Join-Path $ProjectRoot $relative

    if (-not (Test-Path $source)) {
        continue
    }

    $dest = Join-Path $BackupRoot $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item $source $dest -Force
}

try {
    Copy-Item $PayloadResolver $GenericResolver -Force

    # Replace BOTH preview and final model resolution calls. The signature is
    # intentionally identical to the old Necron resolver, so this is a tiny,
    # auditable change.
    $squadText = $squadText.Replace(
        "NecronModelPackResolverR22.TryResolve(",
        "FactionModelPackResolver.TryResolve("
    )

    # Remove the after-the-fact renderer-bounds recenter hack completely.
    $repairPattern = '(?s)if\s*\(\s*visual\s*!=\s*null\s*\)\s*\{\s*bool\s+visualAttached\s*=\s*token\.AttachVisual\(\s*visual,\s*baseColor\s*\);\s*if\s*\(\s*visualAttached\s*\)\s*\{\s*NecronVisualOriginRepairR23\.Reanchor\(\s*FactionId,\s*token\s*\);\s*\}\s*\}'

    $replacement = @'
if (visual != null)
            {
                token.AttachVisual(
                    visual,
                    baseColor
                );
            }
'@

    $matches = [regex]::Matches(
        $squadText,
        $repairPattern
    )

    if ($matches.Count -ne 1) {
        throw "Expected exactly one R2.3 origin-repair block; found $($matches.Count)."
    }

    $squadText = [regex]::Replace(
        $squadText,
        $repairPattern,
        $replacement,
        1
    )

    Set-Content -Path $Squad -Value $squadText -Encoding UTF8

    # The bad faction-specific model hacks are no longer part of the runtime.
    Safe-Remove (Join-Path $Core "NecronModelPackResolverR22.cs")
    Safe-Remove (Join-Path $Core "NecronModelPackResolverR22.cs.meta")
    Safe-Remove (Join-Path $Core "NecronVisualOriginRepairR23.cs")
    Safe-Remove (Join-Path $Core "NecronVisualOriginRepairR23.cs.meta")

    $verifySquad = Get-Content -Raw -Path $Squad
    $verifyResolver = Get-Content -Raw -Path $GenericResolver

    $genericCalls =
        ([regex]::Matches(
            $verifySquad,
            'FactionModelPackResolver\.TryResolve'
        )).Count

    if ($genericCalls -ne 2) {
        throw "Expected exactly two unified model resolver calls in SquadController; found $genericCalls."
    }

    if ($verifySquad -match 'NecronModelPackResolverR22' -or
        $verifySquad -match 'NecronVisualOriginRepairR23') {
        throw "Old Necron-specific runtime references still remain in SquadController."
    }

    if ($verifyResolver -notmatch 'WARBOARD_UNIFIED_FACTION_MODEL_RESOLVER' -or
        $verifyResolver -notmatch 'ComponentLocalPosition' -or
        $verifyResolver -notmatch 'rootComponent') {
        throw "Unified faction resolver verification failed."
    }

    # Confirm all three current extra faction indexes are actually present.
    foreach ($folder in @("Necrons", "Orks", "Tyranids")) {
        $indexPath = Join-Path $ProjectRoot ("Assets\Resources\Armies\Models\" + $folder + "\ModelIndex.json")

        if (-not (Test-Path $indexPath)) {
            Write-Host "WARNING: $folder ModelIndex.json is not currently installed." -ForegroundColor Yellow
        }
        else {
            Write-Host "Found model pack: $folder" -ForegroundColor Green
        }
    }
}
catch {
    Write-Host ""
    Write-Host "Install failed. Restoring model-pipeline backup..." -ForegroundColor Red

    foreach ($relative in $backupFiles) {
        $saved = Join-Path $BackupRoot $relative
        $dest = Join-Path $ProjectRoot $relative

        if (Test-Path $saved) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Copy-Item $saved $dest -Force
        }
    }

    if (-not (Test-Path (Join-Path $BackupRoot "Assets\Scripts\Core\FactionModelPackResolver.cs")) -and
        (Test-Path $GenericResolver)) {
        Remove-Item $GenericResolver -Force -ErrorAction SilentlyContinue
    }

    Write-Host $_.Exception.Message -ForegroundColor Red
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "Unified model pipeline installed." -ForegroundColor Green
Write-Host ""
Write-Host "Cleaning disposable patch files from the project..." -ForegroundColor Cyan

# Remove obsolete one-off helper code if it exists locally from previous attempts.
$obsoleteCode = @(
    "Assets\Scripts\Core\WarboardMissionCardRowR21.cs",
    "Assets\Scripts\Core\WarboardMissionCardRowR21.cs.meta"
)

foreach ($relative in $obsoleteCode) {
    Safe-Remove (Join-Path $ProjectRoot $relative)
}

# Project-root patch/install clutter only. Do NOT touch real Assets, model packs,
# Packages, ProjectSettings, .git or the Library backups.
$patterns = @(
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

$currentPs1 = $MyInvocation.MyCommand.Path

foreach ($pattern in $patterns) {
    Get-ChildItem -Path $ProjectRoot -File -Filter $pattern -ErrorAction SilentlyContinue |
        ForEach-Object {
            if ($_.FullName -eq $currentPs1) {
                return
            }

            Safe-Remove $_.FullName
        }
}

Get-ChildItem -Path $ProjectRoot -Directory -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -eq "PATCH_PAYLOAD" -or
        $_.Name -like "V*_PATCH_PAYLOAD" -or
        $_.Name -like "R*_PATCH_PAYLOAD"
    } |
    ForEach-Object {
        Safe-Remove $_.FullName
    }

$assetsRoot = Join-Path $ProjectRoot "Assets"

Get-ChildItem -Path $assetsRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.Name -like "*.bak" -or
        $_.Name -like "*.bak.meta"
    } |
    ForEach-Object {
        Safe-Remove $_.FullName
    }

$installerMarkers =
    Join-Path $ProjectRoot "Library\WarboardInstallerMarkers"

Safe-Remove $installerMarkers

Write-Host ""
Write-Host "DONE." -ForegroundColor Green
Write-Host ""
Write-Host "Model logic now:" -ForegroundColor Cyan
Write-Host "  Aeldari  -> existing proven ModelVisualRegistry pipeline"
Write-Host "  Custodes -> existing proven CustodesModelPackResolver pipeline"
Write-Host "  Necrons  -> same local-transform contract via FactionModelPackResolver"
Write-Host "  Orks     -> same local-transform contract via FactionModelPackResolver"
Write-Host "  Tyranids -> same local-transform contract via FactionModelPackResolver"
Write-Host ""
Write-Host "Important extraction fix:" -ForegroundColor Cyan
Write-Host "  Root TTS X/Z table coordinates are discarded BEFORE ModelToken.AttachVisual."
Write-Host "  Child component transforms remain local exactly like the working packs."
Write-Host "  No renderer-bounds recentering is used."
Write-Host ""
Write-Host "Cleanup preserved all real game/model assets and Library/WarboardBackups." -ForegroundColor Green
Write-Host "Backup: $BackupRoot" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Return to Unity, let it compile, and START A FRESH BATTLE." -ForegroundColor Yellow
Write-Host ""
Read-Host "Press Enter to close"

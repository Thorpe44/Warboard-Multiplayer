$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R28.2 - UNIFIED MODEL RESOLVER (NO SQUADCONTROLLER PATCHING)" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host ""

$ProjectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $ProjectRoot "Assets"))) {
    $ProjectRoot = $PSScriptRoot
}

$CoreRoot = Join-Path $ProjectRoot "Assets\Scripts\Core"
$OldResolverPath = Join-Path $CoreRoot "ExtendedFactionModelPackResolverR25.cs"
$UnifiedPath = Join-Path $CoreRoot "UnifiedModelVisualResolverR28.cs"

$PayloadRoot = Join-Path $PSScriptRoot "R28_2_PAYLOAD\Assets\Scripts\Core"
$PayloadOldResolver = Join-Path $PayloadRoot "ExtendedFactionModelPackResolverR25.cs"
$PayloadUnified = Join-Path $PayloadRoot "UnifiedModelVisualResolverR28.cs"

if (-not (Test-Path $CoreRoot)) {
    throw "Could not find Assets\Scripts\Core. Put this installer in the Warboard project root."
}

if (-not (Test-Path $OldResolverPath)) {
    throw "Could not find ExtendedFactionModelPackResolverR25.cs. This installer expects your current R25/R27 project."
}

if (-not (Test-Path $PayloadOldResolver) -or
    -not (Test-Path $PayloadUnified)) {
    throw "R28.2 payload is incomplete."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green
Write-Host "SquadController.cs will NOT be edited." -ForegroundColor Green

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot ("R28_2_BACKUP_" + $Timestamp)
New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

Copy-Item $OldResolverPath (Join-Path $BackupRoot "ExtendedFactionModelPackResolverR25.cs") -Force
if (Test-Path $UnifiedPath) {
    Copy-Item $UnifiedPath (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs") -Force
}

try {
    Copy-Item $PayloadOldResolver $OldResolverPath -Force
    Copy-Item $PayloadUnified $UnifiedPath -Force

    $Shim = [System.IO.File]::ReadAllText($OldResolverPath)
    $Unified = [System.IO.File]::ReadAllText($UnifiedPath)

    if (-not $Shim.Contains("UnifiedModelVisualResolverR28.TryResolve")) {
        throw "Verification failed: R25 compatibility shim is not routing to R28."
    }

    if ($Shim.Contains("no strong Necron/Ork/Tyranid match")) {
        throw "Verification failed: old R25 warning implementation still exists."
    }

    if (-not $Unified.Contains("ResolutionCache")) {
        throw "Verification failed: unified resolver cache is missing."
    }

    if (-not $Unified.Contains('new PackSpec(')) {
        throw "Verification failed: unified faction-pack definitions are missing."
    }

    Write-Host ""
    Write-Host "R28.2 installed successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "Architecture now:" -ForegroundColor Cyan
    Write-Host " SquadController"
    Write-Host "   -> existing R25 API name (compatibility shim)"
    Write-Host "      -> R28 unified faction-aware resolver"
    Write-Host "         -> Aeldari / Custodes / Necrons / Orks / Tyranids ModelIndex"
    Write-Host ""
    Write-Host "Important:" -ForegroundColor Cyan
    Write-Host " - SquadController was untouched."
    Write-Host " - R27 Leader changes were untouched."
    Write-Host " - Existing call sites still compile."
    Write-Host " - Repeated model resolutions are cached."
    Write-Host " - Normal no-match cases are silent."
    Write-Host " - Broken matched OBJ resources still warn."
    Write-Host ""
    Write-Host "Backup: $BackupRoot" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Open Unity and let it recompile." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "R28.2 install failed. Restoring resolver backup..." -ForegroundColor Red

    if (Test-Path (Join-Path $BackupRoot "ExtendedFactionModelPackResolverR25.cs")) {
        Copy-Item (Join-Path $BackupRoot "ExtendedFactionModelPackResolverR25.cs") $OldResolverPath -Force
    }

    if (Test-Path (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs")) {
        Copy-Item (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs") $UnifiedPath -Force
    }
    elseif (Test-Path $UnifiedPath) {
        Remove-Item $UnifiedPath -Force
    }

    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

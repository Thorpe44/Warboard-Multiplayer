$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R28 - UNIFIED MODEL RESOLVER" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$ProjectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $ProjectRoot "Assets"))) {
    $ProjectRoot = $PSScriptRoot
}

$SquadPath = Join-Path $ProjectRoot "Assets\Scripts\Core\SquadController.cs"
$ResolverPath = Join-Path $ProjectRoot "Assets\Scripts\Core\UnifiedModelVisualResolverR28.cs"
$PayloadResolver = Join-Path $PSScriptRoot "R28_PAYLOAD\Assets\Scripts\Core\UnifiedModelVisualResolverR28.cs"

if (-not (Test-Path $SquadPath)) {
    throw "Could not find Assets\Scripts\Core\SquadController.cs. Put this installer in the Warboard project root and run it again."
}

if (-not (Test-Path $PayloadResolver)) {
    throw "R28 payload is missing: $PayloadResolver"
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot ("R28_BACKUP_" + $Timestamp)
New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

Copy-Item $SquadPath (Join-Path $BackupRoot "SquadController.cs") -Force
if (Test-Path $ResolverPath) {
    Copy-Item $ResolverPath (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs") -Force
}

try {
    $Text = [System.IO.File]::ReadAllText($SquadPath)

    if ($Text.Contains("WARBOARD_R28_RESOLVE_ONCE")) {
        Write-Host "SquadController already contains the R28 resolve-once path." -ForegroundColor Yellow
    }
    else {
        $OldSpacing = @'
        float layoutSpacing =
            spacing;

        for (int i = 0;
'@

        $NewSpacing = @'
        float layoutSpacing =
            spacing;

        // WARBOARD_R28_RESOLVE_ONCE
        // Resolve every miniature exactly once. The same definition is reused
        // for formation spacing and for the actual visual attachment.
        ModelVisualDefinition[] resolvedVisuals =
            new ModelVisualDefinition[StartingModels];

        for (int i = 0;
'@

        if (-not $Text.Contains($OldSpacing)) {
            throw "Could not locate the CreateModels layout-spacing block in SquadController.cs."
        }

        $Text = $Text.Replace($OldSpacing, $NewSpacing)

        $OldPreview = @'
            ModelVisualDefinition previewVisual =
                ExtendedFactionModelPackResolverR25.TryResolve(
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

            if (previewVisual != null)
'@

        $NewPreview = @'
            ModelVisualDefinition previewVisual =
                UnifiedModelVisualResolverR28.TryResolve(
                    FactionId,
                    DisplayName,
                    previewRoleName,
                    i
                );

            resolvedVisuals[i] =
                previewVisual;

            if (previewVisual != null)
'@

        if (-not $Text.Contains($OldPreview)) {
            throw "Could not locate the preview model-resolver block in SquadController.cs."
        }

        $Text = $Text.Replace($OldPreview, $NewPreview)

        $OldAttach = @'
            ModelVisualDefinition visual =
                ExtendedFactionModelPackResolverR25.TryResolve(
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

            if (visual != null)
'@

        $NewAttach = @'
            ModelVisualDefinition visual =
                resolvedVisuals[i];

            if (visual != null)
'@

        if (-not $Text.Contains($OldAttach)) {
            throw "Could not locate the visual-attachment resolver block in SquadController.cs."
        }

        $Text = $Text.Replace($OldAttach, $NewAttach)

        [System.IO.File]::WriteAllText(
            $SquadPath,
            $Text,
            (New-Object System.Text.UTF8Encoding($false))
        )
    }

    Copy-Item $PayloadResolver $ResolverPath -Force

    $Verify = [System.IO.File]::ReadAllText($SquadPath)

    if (-not $Verify.Contains("UnifiedModelVisualResolverR28.TryResolve")) {
        throw "R28 verification failed: SquadController is not calling the unified resolver."
    }

    if ($Verify.Contains("ExtendedFactionModelPackResolverR25.TryResolve")) {
        throw "R28 verification failed: SquadController still calls the old R25 resolver."
    }

    if (-not (Test-Path $ResolverPath)) {
        throw "R28 verification failed: UnifiedModelVisualResolverR28.cs was not installed."
    }

    Write-Host ""
    Write-Host "R28 installed successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "What changed:" -ForegroundColor Cyan
    Write-Host " - Aeldari only searches the Aeldari model pack."
    Write-Host " - Custodes only searches the Custodes model pack."
    Write-Host " - Necrons, Orks and Tyranids only search their own packs."
    Write-Host " - Generic Player 1/2 ids use strict exact-match inference."
    Write-Host " - Missing models silently keep the capsule fallback."
    Write-Host " - Broken matched OBJ assets still produce a real warning."
    Write-Host " - Every model is resolved once, not twice."
    Write-Host ""
    Write-Host "Backup: $BackupRoot" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Open Unity and let it recompile. The old R25 cross-faction warning spam should be gone." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "R28 install failed. Restoring backup..." -ForegroundColor Red

    if (Test-Path (Join-Path $BackupRoot "SquadController.cs")) {
        Copy-Item (Join-Path $BackupRoot "SquadController.cs") $SquadPath -Force
    }

    if (Test-Path (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs")) {
        Copy-Item (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs") $ResolverPath -Force
    }
    elseif (Test-Path $ResolverPath) {
        Remove-Item $ResolverPath -Force
    }

    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

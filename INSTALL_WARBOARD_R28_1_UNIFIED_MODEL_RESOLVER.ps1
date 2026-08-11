$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD R28.1 - UNIFIED MODEL RESOLVER" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
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
    throw "R28.1 payload is missing: $PayloadResolver"
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot ("R28_BACKUP_" + $Timestamp)
New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

Copy-Item $SquadPath (Join-Path $BackupRoot "SquadController.cs") -Force
if (Test-Path $ResolverPath) {
    Copy-Item $ResolverPath (Join-Path $BackupRoot "UnifiedModelVisualResolverR28.cs") -Force
}

function Replace-ExactlyOnce {
    param(
        [string] $InputText,
        [string] $Pattern,
        [string] $Replacement,
        [string] $Description,
        [System.Text.RegularExpressions.RegexOptions] $Options = [System.Text.RegularExpressions.RegexOptions]::None
    )

    $Regex = New-Object System.Text.RegularExpressions.Regex($Pattern, $Options)
    $Matches = $Regex.Matches($InputText)

    if ($Matches.Count -ne 1) {
        throw "Could not safely locate $Description. Expected exactly 1 match, found $($Matches.Count). No source changes were kept."
    }

    return $Regex.Replace($InputText, $Replacement, 1)
}

try {
    $Text = [System.IO.File]::ReadAllText($SquadPath)

    if ($Text.Contains("WARBOARD_R28_RESOLVE_ONCE") -and
        $Text.Contains("UnifiedModelVisualResolverR28.TryResolve") -and
        -not $Text.Contains("ExtendedFactionModelPackResolverR25.TryResolve")) {

        Write-Host "SquadController already has the R28 unified resolver path. Refreshing resolver file only." -ForegroundColor Yellow
    }
    else {
        if (-not $Text.Contains("WARBOARD_R28_RESOLVE_ONCE")) {
            $LayoutPattern = '(?m)^(?<indent>[ \t]*)float[ \t]+layoutSpacing[ \t]*=[ \t]*\r?\n[ \t]*spacing[ \t]*;[ \t]*$'

            $LayoutReplacement = '${indent}float layoutSpacing =' + "`r`n" +
                                 '${indent}    spacing;' + "`r`n`r`n" +
                                 '${indent}// WARBOARD_R28_RESOLVE_ONCE' + "`r`n" +
                                 '${indent}// Resolve every miniature exactly once. The same definition is reused' + "`r`n" +
                                 '${indent}// for formation spacing and for the actual visual attachment.' + "`r`n" +
                                 '${indent}ModelVisualDefinition[] resolvedVisuals =' + "`r`n" +
                                 '${indent}    new ModelVisualDefinition[StartingModels];'

            $Text = Replace-ExactlyOnce `
                -InputText $Text `
                -Pattern $LayoutPattern `
                -Replacement $LayoutReplacement `
                -Description "the CreateModels layoutSpacing declaration" `
                -Options ([System.Text.RegularExpressions.RegexOptions]::Multiline)
        }

        if (-not ($Text -match 'ModelVisualDefinition\s+previewVisual\s*=\s*UnifiedModelVisualResolverR28\.TryResolve')) {
            $PreviewPattern = '(?ms)^(?<indent>[ \t]*)ModelVisualDefinition[ \t]+previewVisual[ \t]*=[\s\S]*?;[ \t]*\r?\n(?=[ \t]*if[ \t]*\([ \t]*previewVisual[ \t]*!=[ \t]*null[ \t]*\))'

            $PreviewReplacement = '${indent}ModelVisualDefinition previewVisual =' + "`r`n" +
                                  '${indent}    UnifiedModelVisualResolverR28.TryResolve(' + "`r`n" +
                                  '${indent}        FactionId,' + "`r`n" +
                                  '${indent}        DisplayName,' + "`r`n" +
                                  '${indent}        previewRoleName,' + "`r`n" +
                                  '${indent}        i' + "`r`n" +
                                  '${indent}    );' + "`r`n`r`n" +
                                  '${indent}resolvedVisuals[i] =' + "`r`n" +
                                  '${indent}    previewVisual;' + "`r`n"

            $Text = Replace-ExactlyOnce `
                -InputText $Text `
                -Pattern $PreviewPattern `
                -Replacement $PreviewReplacement `
                -Description "the preview model-resolver assignment" `
                -Options ([System.Text.RegularExpressions.RegexOptions]::Multiline)
        }

        if (-not ($Text -match 'ModelVisualDefinition\s+visual\s*=\s*resolvedVisuals\s*\[\s*i\s*\]')) {
            $AttachPattern = '(?ms)^(?<indent>[ \t]*)ModelVisualDefinition[ \t]+visual[ \t]*=[\s\S]*?;[ \t]*\r?\n(?=[ \t]*if[ \t]*\([ \t]*visual[ \t]*!=[ \t]*null[ \t]*\))'

            $AttachReplacement = '${indent}ModelVisualDefinition visual =' + "`r`n" +
                                 '${indent}    resolvedVisuals[i];' + "`r`n"

            $Text = Replace-ExactlyOnce `
                -InputText $Text `
                -Pattern $AttachPattern `
                -Replacement $AttachReplacement `
                -Description "the final model visual-resolver assignment" `
                -Options ([System.Text.RegularExpressions.RegexOptions]::Multiline)
        }

        [System.IO.File]::WriteAllText(
            $SquadPath,
            $Text,
            (New-Object System.Text.UTF8Encoding($false))
        )
    }

    Copy-Item $PayloadResolver $ResolverPath -Force

    $Verify = [System.IO.File]::ReadAllText($SquadPath)
    $UnifiedCalls = ([regex]::Matches($Verify, 'UnifiedModelVisualResolverR28\.TryResolve')).Count
    $OldCalls = ([regex]::Matches($Verify, 'ExtendedFactionModelPackResolverR25\.TryResolve')).Count
    $CachedAttach = $Verify -match 'ModelVisualDefinition\s+visual\s*=\s*resolvedVisuals\s*\[\s*i\s*\]'

    if ($UnifiedCalls -lt 1) {
        throw "R28.1 verification failed: SquadController is not calling the unified resolver."
    }

    if ($OldCalls -ne 0) {
        throw "R28.1 verification failed: SquadController still contains $OldCalls runtime call(s) to the old R25 resolver."
    }

    if (-not $CachedAttach) {
        throw "R28.1 verification failed: the second model-resolution pass was not replaced by the cached result."
    }

    if (-not (Test-Path $ResolverPath)) {
        throw "R28.1 verification failed: UnifiedModelVisualResolverR28.cs was not installed."
    }

    Write-Host ""
    Write-Host "R28.1 installed successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "Verified:" -ForegroundColor Cyan
    Write-Host " - SquadController calls the unified resolver."
    Write-Host " - No SquadController calls to ExtendedFactionModelPackResolverR25 remain."
    Write-Host " - The resolved visual is reused instead of resolving every model twice."
    Write-Host " - R27 changes elsewhere in SquadController were left alone."
    Write-Host ""
    Write-Host "Backup: $BackupRoot" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Open Unity and let it recompile." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "R28.1 install failed. Restoring backup..." -ForegroundColor Red

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

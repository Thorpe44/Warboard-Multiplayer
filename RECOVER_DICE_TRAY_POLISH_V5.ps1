$ErrorActionPreference = "Stop"

function Find-WarboardRoot {
    param([string]$Start)

    $dir = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 8; $i++) {
        if ((Test-Path (Join-Path $dir "Assets")) -and
            (Test-Path (Join-Path $dir "Packages")) -and
            (Test-Path (Join-Path $dir "ProjectSettings"))) {
            return $dir
        }

        $parent = Split-Path -Parent $dir

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    return $null
}

function Read-Text {
    param([string]$Path)
    return [System.IO.File]::ReadAllText($Path)
}

function Write-Text {
    param([string]$Path, [string]$Content)
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8)
}

function Regex-Patch-Once {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Replacement,
        [string]$AlreadyMarker,
        [string]$Description
    )

    if ($Content.Contains($AlreadyMarker)) {
        Write-Host ("[ALREADY] " + $Description)
        return $Content
    }

    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $Pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $Content,
            $Replacement,
            1
        )

    if ($updated -eq $Content) {
        throw ("Could not find patch target: " + $Description)
    }

    Write-Host ("[PATCHED] " + $Description)
    return $updated
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Could not find Warboard project root."
    exit 1
}

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""

$dicePath =
    Join-Path $Root "Assets\Scripts\Core\TraditionalDiceTray3D.cs"

$helperDest =
    Join-Path $Root "Assets\Scripts\Core\TraditionalDiceTray3D.Polish.cs"

$helperSource =
    Join-Path $PSScriptRoot "Assets\Scripts\Core\TraditionalDiceTray3D.Polish.cs"

if (!(Test-Path $dicePath)) {
    Write-Host "FAILED: TraditionalDiceTray3D.cs not found."
    exit 2
}

if (!(Test-Path $helperSource)) {
    Write-Host "FAILED: Corrected TraditionalDiceTray3D.Polish.cs missing from package."
    exit 3
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"

Copy-Item `
    $dicePath `
    ($dicePath + ".before_dice_polish_recovery_" + $stamp + ".bak") `
    -Force

if (Test-Path $helperDest) {
    Copy-Item `
        $helperDest `
        ($helperDest + ".before_dice_polish_recovery_" + $stamp + ".bak") `
        -Force
}

# The ZIP is normally extracted directly into the project root, which means
# helperSource and helperDest can be the SAME physical file. PowerShell refuses
# to Copy-Item a file over itself, so explicitly handle both layouts.
$sourceResolved = (Resolve-Path $helperSource).Path
$destResolved = $null

if (Test-Path $helperDest) {
    $destResolved = (Resolve-Path $helperDest).Path
}

if ($sourceResolved -eq $destResolved) {
    Write-Host "[OK] Corrected helper already at final destination"
}
else {
    Copy-Item $helperSource $helperDest -Force
    Write-Host "[FIXED] Corrected TraditionalDiceTray3D.Polish.cs installed"
}

$dice = Read-Text $dicePath

# Ensure the type remains partial for multiplayer + polish helpers.
$dice = [regex]::Replace(
    $dice,
    'public\s+class\s+TraditionalDiceTray3D\s*:\s*MonoBehaviour',
    'public partial class TraditionalDiceTray3D : MonoBehaviour',
    1
)

# ------------------------------------------------------------------
# Existing partial V1 work
# ------------------------------------------------------------------

if (!$dice.Contains("ApplyDiceTrayPolish();")) {
    # Insert after tray world scale.
    $pattern = '(trayRoot\.transform\.localScale\s*=\s*new\s+Vector3\s*\(\s*1\.72f\s*,\s*1\.0f\s*,\s*0\.74f\s*\)\s*;)'
    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $dice,
            '$1' + [Environment]::NewLine + [Environment]::NewLine +
            '        ApplyDiceTrayPolish();',
            1
        )

    if ($updated -eq $dice) {
        # If the larger scale was not applied, patch original scale robustly.
        $pattern = 'trayRoot\.transform\.localScale\s*=\s*new\s+Vector3\s*\(\s*1\.55f\s*,\s*1\.0f\s*,\s*0\.58f\s*\)\s*;'
        $replacement =
            'trayRoot.transform.localScale =' + [Environment]::NewLine +
            '            new Vector3(' + [Environment]::NewLine +
            '                1.72f,' + [Environment]::NewLine +
            '                1.0f,' + [Environment]::NewLine +
            '                0.74f' + [Environment]::NewLine +
            '            );' + [Environment]::NewLine +
            [Environment]::NewLine +
            '        ApplyDiceTrayPolish();'

        $fallbackRegex =
            New-Object System.Text.RegularExpressions.Regex(
                $pattern,
                [System.Text.RegularExpressions.RegexOptions]::Singleline
            )

        $updated =
            $fallbackRegex.Replace(
                $dice,
                $replacement,
                1
            )
    }

    if ($updated -eq $dice) {
        throw "Could not verify/install larger tray scale."
    }

    $dice = $updated
    Write-Host "[PATCHED] larger world-space tray"
}
else {
    Write-Host "[ALREADY] larger world-space tray"
}

# Ensure polish also runs at initial tray construction.
$applyCount = ([regex]::Matches($dice, 'ApplyDiceTrayPolish\s*\(\s*\)\s*;')).Count

if ($applyCount -lt 2) {
    $pattern =
        '(dicePhysics\.bounceCombine\s*=\s*PhysicsMaterialCombine\.Average\s*;)'

    $replacement =
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        ApplyDiceTrayPolish();'

    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $dice,
            $replacement,
            1
        )

    if ($updated -eq $dice) {
        throw "Could not add tray polish after physics creation."
    }

    $dice = $updated
    Write-Host "[PATCHED] containment geometry hook"
}
else {
    Write-Host "[ALREADY] containment geometry hook"
}

# ------------------------------------------------------------------
# Complete the parts V1 never reached
# ------------------------------------------------------------------

# Escape recovery after world dice clicking.
if (!$dice.Contains("ContainEscapedDice();")) {
    $pattern =
        '(if\s*\(\s*worldSpaceMode\s*\)\s*HandleWorldDiceClick\s*\(\s*\)\s*;)'

    $replacement =
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        ContainEscapedDice();'

    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $dice,
            $replacement,
            1
        )

    if ($updated -eq $dice) {
        throw "Could not find Update() worldSpaceMode block for escape recovery."
    }

    $dice = $updated
    Write-Host "[PATCHED] escape recovery"
}
else {
    Write-Host "[ALREADY] escape recovery"
}

# Bigger dice with parent-scale compensation.
if (!$dice.Contains("DiceWorldScaleCompensated();")) {
    $pattern =
        'dieObject\.transform\.localScale\s*=\s*Vector3\.one\s*;'

    $replacement =
        'dieObject.transform.localScale =' +
        [Environment]::NewLine +
        '            DiceWorldScaleCompensated();'

    $updated = [regex]::Replace(
        $dice,
        $pattern,
        $replacement,
        1
    )

    if ($updated -eq $dice) {
        throw "Could not patch dice scale."
    }

    $dice = $updated
    Write-Host "[PATCHED] larger undistorted dice"
}
else {
    Write-Host "[ALREADY] larger undistorted dice"
}

# Lower / safer initial spawn height and slightly expand x range.
$dice = [regex]::Replace(
    $dice,
    'Random\.Range\s*\(\s*-5\.0f\s*,\s*5\.0f\s*\)',
    'Random.Range(-5.8f, 5.8f)'
)

$dice = [regex]::Replace(
    $dice,
    'Random\.Range\s*\(\s*4\.4f\s*,\s*7\.8f\s*\)',
    'Random.Range(3.8f, 6.3f)'
)

$dice = [regex]::Replace(
    $dice,
    'Random\.Range\s*\(\s*-2\.5f\s*,\s*2\.5f\s*\)',
    'Random.Range(-2.35f, 2.35f)'
)

# Reroll spawn area.
$dice = [regex]::Replace(
    $dice,
    'Random\.Range\s*\(\s*-4\.8f\s*,\s*4\.8f\s*\)',
    'Random.Range(-5.6f, 5.6f)'
)

$dice = [regex]::Replace(
    $dice,
    'Random\.Range\s*\(\s*4\.6f\s*,\s*7\.5f\s*\)',
    'Random.Range(3.9f, 6.2f)'
)

Write-Host "[PATCHED] safer dice launch ranges"

# Brighter ivory material.
if (!$dice.Contains("0.985f")) {
    $pattern =
        'private\s+Color\s+DieColor\s*\(\s*int\s+sides\s*\)\s*\{\s*return\s+new\s+Color\s*\(\s*0\.92f\s*,\s*0\.92f\s*,\s*0\.94f\s*,\s*1f\s*\)\s*;\s*\}'

    $replacement = @'
private Color DieColor(
        int sides)
    {
        return new Color(
            0.985f,
            0.975f,
            0.90f,
            1f
        );
    }
'@

    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $dice,
            $replacement,
            1
        )

    if ($updated -eq $dice) {
        throw "Could not patch DieColor."
    }

    $dice = $updated
    Write-Host "[PATCHED] brighter ivory dice"
}
else {
    Write-Host "[ALREADY] brighter ivory dice"
}

# Larger high contrast face labels.
if (!$dice.Contains("text.fontSize = 72;")) {
    $pattern =
        'text\.fontSize\s*=\s*56\s*;\s*text\.characterSize\s*=\s*sides\s*>=\s*12\s*\?\s*0\.047f\s*:\s*sides\s*>=\s*8\s*\?\s*0\.055f\s*:\s*0\.065f\s*;\s*text\.color\s*=\s*Color\.black\s*;'

    $replacement = @'
text.fontSize = 72;

        text.characterSize =
            sides >= 12
            ? 0.058f
            : sides >= 8
                ? 0.068f
                : 0.082f;

        text.color =
            new Color(
                0.015f,
                0.018f,
                0.022f,
                1f
            );
'@

    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $dice,
            $replacement,
            1
        )

    if ($updated -eq $dice) {
        throw "Could not patch face label sizing."
    }

    $dice = $updated
    Write-Host "[PATCHED] clearer face numbers"
}
else {
    Write-Host "[ALREADY] clearer face numbers"
}

# Dedicated result overlay at end of DrawGUI.
if (!$dice.Contains("DrawDiceResultOverlay();")) {
    $pattern =
        '("Dice are physical objects below the battlefield\. Click a die there to select it for reroll\."\s*\)\s*;)'

    $replacement =
        '$1' +
        [Environment]::NewLine +
        [Environment]::NewLine +
        '        DrawDiceResultOverlay();'

    $regexObject =
        New-Object System.Text.RegularExpressions.Regex(
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $updated =
        $regexObject.Replace(
            $dice,
            $replacement,
            1
        )

    if ($updated -eq $dice) {
        throw "Could not add result overlay."
    }

    $dice = $updated
    Write-Host "[PATCHED] result overlay"
}
else {
    Write-Host "[ALREADY] result overlay"
}

Write-Text $dicePath $dice

# ------------------------------------------------------------------
# Verification
# ------------------------------------------------------------------

$verify = Read-Text $dicePath
$helperVerify = Read-Text $helperDest

$errors = @()

foreach ($needle in @(
    "public partial class TraditionalDiceTray3D",
    "ApplyDiceTrayPolish();",
    "ContainEscapedDice();",
    "DiceWorldScaleCompensated();",
    "text.fontSize = 72;",
    "DrawDiceResultOverlay();",
    "0.985f"
)) {
    if (!$verify.Contains($needle)) {
        $errors += ("Missing from TraditionalDiceTray3D.cs: " + $needle)
    }
}

if (!$helperVerify.Contains("trayRoot.transform.lossyScale")) {
    $errors += "Corrected transform.lossyScale helper missing."
}

if ($helperVerify.Contains("trayRoot.lossyScale")) {
    $errors += "Old invalid trayRoot.lossyScale still exists."
}

Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "RECOVERY INCOMPLETE:"
    foreach ($error in $errors) {
        Write-Host ("  - " + $error)
    }
    exit 4
}

Write-Host "SUCCESS - DICE POLISH RECOVERY V5 VERIFIED"
Write-Host ""
Write-Host "The partial V1 install has been completed safely."
Write-Host "Return to Unity and allow scripts to compile."

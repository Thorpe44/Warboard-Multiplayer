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

function Replace-Once {
    param(
        [string]$Content,
        [string]$Old,
        [string]$New,
        [string]$Description
    )

    if ($Content.Contains($New)) {
        Write-Host ("[ALREADY] " + $Description)
        return $Content
    }

    if (!$Content.Contains($Old)) {
        throw ("Could not find patch target: " + $Description)
    }

    Write-Host ("[PATCH] " + $Description)
    return $Content.Replace($Old, $New)
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Could not find Warboard project root."
    exit 1
}

$dicePath =
    Join-Path $Root "Assets\Scripts\Core\TraditionalDiceTray3D.cs"

$helperSource =
    Join-Path $PSScriptRoot "Assets\Scripts\Core\TraditionalDiceTray3D.Polish.cs"

$helperDest =
    Join-Path $Root "Assets\Scripts\Core\TraditionalDiceTray3D.Polish.cs"

if (!(Test-Path $dicePath)) {
    Write-Host "FAILED: TraditionalDiceTray3D.cs not found."
    exit 2
}

if (!(Test-Path $helperSource) -and
    !(Test-Path $helperDest)) {
    Write-Host "FAILED: TraditionalDiceTray3D.Polish.cs missing."
    exit 3
}

$stamp =
    Get-Date -Format "yyyyMMdd_HHmmss"

Copy-Item `
    $dicePath `
    ($dicePath + ".before_dice_polish_" + $stamp + ".bak") `
    -Force

if (Test-Path $helperSource) {
    $sourceResolved =
        (Resolve-Path $helperSource).Path

    $destResolved = $null

    if (Test-Path $helperDest) {
        $destResolved =
            (Resolve-Path $helperDest).Path
    }

    if ($sourceResolved -ne $destResolved) {
        Copy-Item $helperSource $helperDest -Force
    }
}

$dice = Read-Text $dicePath

# 1) Make the world tray longer and noticeably wider.
$old = @'
        trayRoot.transform.localScale =
            new Vector3(
                1.55f,
                1.0f,
                0.58f
            );
'@

$new = @'
        trayRoot.transform.localScale =
            new Vector3(
                1.72f,
                1.0f,
                0.74f
            );

        ApplyDiceTrayPolish();
'@

$dice = Replace-Once $dice $old $new "larger world-space tray"

# 2) Apply geometry + invisible catch walls after the tray is built.
$old = @'
        dicePhysics.bounceCombine =
            PhysicsMaterialCombine.Average;
    }
'@

$new = @'
        dicePhysics.bounceCombine =
            PhysicsMaterialCombine.Average;

        ApplyDiceTrayPolish();
    }
'@

$dice = Replace-Once $dice $old $new "containment walls / enlarged tray geometry"

# 3) Run an emergency containment check every frame.
$old = @'
        if (worldSpaceMode)
            HandleWorldDiceClick();

        if (!rollInProgress ||
'@

$new = @'
        if (worldSpaceMode)
            HandleWorldDiceClick();

        ContainEscapedDice();

        if (!rollInProgress ||
'@

$dice = Replace-Once $dice $old $new "escape recovery"

# 4) Make dice bigger without allowing the non-uniform tray transform to
# squash/stretch the polyhedra.
$old = @'
        dieObject.transform.localScale =
            Vector3.one;
'@

$new = @'
        dieObject.transform.localScale =
            DiceWorldScaleCompensated();
'@

$dice = Replace-Once $dice $old $new "larger undistorted dice"

# 5) Lower the launch and keep it more central. This reduces accidental
# wall-clearing while still giving a satisfying physics roll.
$dice = $dice.Replace(
    'Random.Range(' + [Environment]::NewLine +
    '                    -5.0f,' + [Environment]::NewLine +
    '                    5.0f',
    'Random.Range(' + [Environment]::NewLine +
    '                    -5.8f,' + [Environment]::NewLine +
    '                    5.8f'
)

$dice = $dice.Replace(
    'Random.Range(' + [Environment]::NewLine +
    '                    4.4f,' + [Environment]::NewLine +
    '                    7.8f',
    'Random.Range(' + [Environment]::NewLine +
    '                    3.8f,' + [Environment]::NewLine +
    '                    6.3f'
)

$dice = $dice.Replace(
    'Random.Range(' + [Environment]::NewLine +
    '                    -2.5f,' + [Environment]::NewLine +
    '                    2.5f',
    'Random.Range(' + [Environment]::NewLine +
    '                    -2.35f,' + [Environment]::NewLine +
    '                    2.35f'
)

# Reroll launch area too.
$dice = $dice.Replace(
    'Random.Range(' + [Environment]::NewLine +
    '                        -4.8f,' + [Environment]::NewLine +
    '                        4.8f',
    'Random.Range(' + [Environment]::NewLine +
    '                        -5.6f,' + [Environment]::NewLine +
    '                        5.6f'
)

$dice = $dice.Replace(
    'Random.Range(' + [Environment]::NewLine +
    '                        4.6f,' + [Environment]::NewLine +
    '                        7.5f',
    'Random.Range(' + [Environment]::NewLine +
    '                        3.9f,' + [Environment]::NewLine +
    '                        6.2f'
)

# 6) Brighter ivory dice.
$old = @'
        return new Color(
            0.92f,
            0.92f,
            0.94f,
            1f
        );
'@

$new = @'
        return new Color(
            0.985f,
            0.975f,
            0.90f,
            1f
        );
'@

$dice = Replace-Once $dice $old $new "higher-contrast ivory dice"

# 7) Bigger face numerals.
$old = @'
        text.fontSize = 56;

        text.characterSize =
            sides >= 12
            ? 0.047f
            : sides >= 8
                ? 0.055f
                : 0.065f;

        text.color = Color.black;
'@

$new = @'
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

$dice = Replace-Once $dice $old $new "larger clearer face numbers"

# 8) Dedicated result display.
$old = @'
        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 140f,
                panel.width - 24f,
                22f
            ),
            "Dice are physical objects below the battlefield. Click a die there to select it for reroll."
        );
    }

}
'@

$new = @'
        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 140f,
                panel.width - 24f,
                22f
            ),
            "Dice are physical objects below the battlefield. Click a die there to select it for reroll."
        );

        DrawDiceResultOverlay();
    }

}
'@

$dice = Replace-Once $dice $old $new "dedicated dice result display"

Write-Text $dicePath $dice

# Verification
$verify = Read-Text $dicePath
$errors = @()

foreach ($needle in @(
    "ApplyDiceTrayPolish();",
    "ContainEscapedDice();",
    "DiceWorldScaleCompensated();",
    "text.fontSize = 72;",
    "DrawDiceResultOverlay();"
)) {
    if (!$verify.Contains($needle)) {
        $errors += ("Missing: " + $needle)
    }
}

if (!(Test-Path $helperDest)) {
    $errors += "TraditionalDiceTray3D.Polish.cs not installed."
}

Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "INSTALL INCOMPLETE:"
    foreach ($error in $errors) {
        Write-Host ("  - " + $error)
    }
    exit 4
}

Write-Host "SUCCESS - DICE VISIBILITY + TRAY POLISH VERIFIED"
Write-Host ""
Write-Host "Changes:"
Write-Host "  - larger undistorted dice"
Write-Host "  - larger darker face numbers"
Write-Host "  - brighter ivory dice material"
Write-Host "  - longer + wider physical tray"
Write-Host "  - tall invisible catch walls + ceiling"
Write-Host "  - emergency escape recovery"
Write-Host "  - dedicated RESULT display"
Write-Host ""
Write-Host "Return to Unity and let it compile."

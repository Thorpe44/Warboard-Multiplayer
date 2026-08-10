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

function Inject-Guard {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Guard,
        [string]$Marker
    )

    if ($Content.Contains($Marker)) {
        return $Content
    }

    $match = [regex]::Match(
        $Content,
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    if (!$match.Success) {
        throw "Could not find method for patch marker: $Marker"
    }

    $replacement =
        $match.Value +
        [Environment]::NewLine +
        "        " + $Guard +
        [Environment]::NewLine

    return $Content.Substring(0, $match.Index) +
           $replacement +
           $Content.Substring(
               $match.Index + $match.Length
           )
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Warboard project root not found."
    exit 1
}

Write-Host "Warboard root:"
Write-Host "  $Root"
Write-Host ""

$dicePath =
    Join-Path $Root "Assets\Scripts\Core\TraditionalDiceTray3D.cs"

$bootstrapPath =
    Join-Path $Root "Assets\Scripts\Multiplayer\WarboardMultiplayerBootstrap.cs"

$mpDir =
    Join-Path $Root "Assets\Scripts\Multiplayer"

if (!(Test-Path $dicePath)) {
    Write-Host "FAILED: TraditionalDiceTray3D.cs not found."
    exit 2
}

if (!(Test-Path $bootstrapPath)) {
    Write-Host "FAILED: WarboardMultiplayerBootstrap.cs not found."
    exit 3
}

$stamp =
    Get-Date -Format "yyyyMMdd_HHmmss"

Copy-Item $dicePath ($dicePath + ".before_shared_dice_" + $stamp + ".bak") -Force
Copy-Item $bootstrapPath ($bootstrapPath + ".before_shared_dice_" + $stamp + ".bak") -Force

$dice = Read-Text $dicePath

# Make the dice tray extensible by the new multiplayer partial.
$dice = [regex]::Replace(
    $dice,
    'public\s+class\s+TraditionalDiceTray3D\s*:\s*MonoBehaviour',
    'public partial class TraditionalDiceTray3D : MonoBehaviour',
    1
)

# Client-side dice actions become host requests.
$dice = Inject-Guard `
    $dice `
    'private\s+void\s+RollAll\s*\(\s*\)\s*\{' `
    'if (WarboardDiceNetworkBridge.TryInterceptRoll(this)) return;' `
    'TryInterceptRoll(this)'

$dice = Inject-Guard `
    $dice `
    'private\s+void\s+RerollSelected\s*\(\s*\)\s*\{' `
    'if (WarboardDiceNetworkBridge.TryInterceptReroll(this)) return;' `
    'TryInterceptReroll(this)'

$dice = Inject-Guard `
    $dice `
    'private\s+void\s+ClearDice\s*\(\s*\)\s*\{' `
    'if (WarboardDiceNetworkBridge.TryInterceptClear(this)) return;' `
    'TryInterceptClear(this)'

$dice = Inject-Guard `
    $dice `
    'private\s+void\s+AdjustSelectedPool\s*\(\s*int\s+delta\s*\)\s*\{' `
    'if (WarboardDiceNetworkBridge.TryInterceptPoolAdjustment(this, selectedSides, delta)) return;' `
    'TryInterceptPoolAdjustment(this'

# Route the two click-selection sites through the shared selector.
$dice = [regex]::Replace(
    $dice,
    'marker\.SetSelected\s*\(\s*!marker\.Selected\s*\)\s*;',
    'SetDieSelectedShared(marker, !marker.Selected);'
)

Write-Text $dicePath $dice
Write-Host "[PATCHED] TraditionalDiceTray3D.cs"

# Install the new source files.
Copy-Item `
    (Join-Path $PSScriptRoot "WarboardDiceNetworkBridge.cs") `
    (Join-Path $mpDir "WarboardDiceNetworkBridge.cs") `
    -Force

Copy-Item `
    (Join-Path $PSScriptRoot "TraditionalDiceTray3D.Multiplayer.cs") `
    (Join-Path $mpDir "TraditionalDiceTray3D.Multiplayer.cs") `
    -Force

Write-Host "[ADDED] WarboardDiceNetworkBridge.cs"
Write-Host "[ADDED] TraditionalDiceTray3D.Multiplayer.cs"

# Add the bridge to the multiplayer runtime bootstrap.
$bootstrap = Read-Text $bootstrapPath

if (!$bootstrap.Contains("WarboardDiceNetworkBridge")) {
    $pattern =
        'root\.AddComponent<\s*WarboardNetworkBridge\s*>\s*\(\s*\)\s*;'

    $match =
        [regex]::Match(
            $bootstrap,
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    if (!$match.Success) {
        throw "Could not locate WarboardNetworkBridge bootstrap registration."
    }

    $addition =
        $match.Value +
        [Environment]::NewLine +
        [Environment]::NewLine +
        "        root.AddComponent<" +
        [Environment]::NewLine +
        "            WarboardDiceNetworkBridge>();"

    $bootstrap =
        $bootstrap.Substring(0, $match.Index) +
        $addition +
        $bootstrap.Substring(
            $match.Index + $match.Length
        )

    Write-Text $bootstrapPath $bootstrap
}

Write-Host "[PATCHED] WarboardMultiplayerBootstrap.cs"

# Verification.
$diceVerify = Read-Text $dicePath
$bootstrapVerify = Read-Text $bootstrapPath

$errors = @()

if ($diceVerify -notmatch 'public partial class TraditionalDiceTray3D') {
    $errors += "TraditionalDiceTray3D was not made partial."
}

foreach ($marker in @(
    "TryInterceptRoll(this)",
    "TryInterceptReroll(this)",
    "TryInterceptClear(this)",
    "TryInterceptPoolAdjustment(this",
    "SetDieSelectedShared(marker, !marker.Selected)"
)) {
    if (!$diceVerify.Contains($marker)) {
        $errors += "Missing dice patch: $marker"
    }
}

if (!$bootstrapVerify.Contains("WarboardDiceNetworkBridge")) {
    $errors += "Dice network bridge was not added to bootstrap."
}

if (!(Test-Path (Join-Path $mpDir "WarboardDiceNetworkBridge.cs"))) {
    $errors += "WarboardDiceNetworkBridge.cs missing."
}

if (!(Test-Path (Join-Path $mpDir "TraditionalDiceTray3D.Multiplayer.cs"))) {
    $errors += "TraditionalDiceTray3D.Multiplayer.cs missing."
}

Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "FIX INCOMPLETE:"
    foreach ($entry in $errors) {
        Write-Host ("  - " + $entry)
    }
    exit 4
}

Write-Host "SUCCESS - SHARED MULTIPLAYER DICE INSTALLED"
Write-Host ""
Write-Host "Next:"
Write-Host "  1. Return to Unity and let it compile."
Write-Host "  2. Rebuild the Windows test EXE."
Write-Host "  3. Connect Editor host + EXE client."
Write-Host "  4. Roll from BOTH machines."

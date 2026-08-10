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

$bridgeDest =
    Join-Path $mpDir "WarboardDiceNetworkBridge.cs"

$partialDest =
    Join-Path $mpDir "TraditionalDiceTray3D.Multiplayer.cs"

# V2 fix: source files are stored in Assets\Scripts\Multiplayer in the package,
# not beside the installer.
$bridgeSource =
    Join-Path $PSScriptRoot "Assets\Scripts\Multiplayer\WarboardDiceNetworkBridge.cs"

$partialSource =
    Join-Path $PSScriptRoot "Assets\Scripts\Multiplayer\TraditionalDiceTray3D.Multiplayer.cs"

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

Copy-Item $dicePath ($dicePath + ".before_shared_dice_v2_" + $stamp + ".bak") -Force
Copy-Item $bootstrapPath ($bootstrapPath + ".before_shared_dice_v2_" + $stamp + ".bak") -Force

# The first failed installer may already have copied/extracted these files into
# the destination. Only copy from package source when that source is distinct.
if (Test-Path $bridgeSource) {
    $sourceResolved = (Resolve-Path $bridgeSource).Path
    $destResolved = $null

    if (Test-Path $bridgeDest) {
        $destResolved = (Resolve-Path $bridgeDest).Path
    }

    if ($sourceResolved -ne $destResolved) {
        Copy-Item $bridgeSource $bridgeDest -Force
    }
}

if (Test-Path $partialSource) {
    $sourceResolved = (Resolve-Path $partialSource).Path
    $destResolved = $null

    if (Test-Path $partialDest) {
        $destResolved = (Resolve-Path $partialDest).Path
    }

    if ($sourceResolved -ne $destResolved) {
        Copy-Item $partialSource $partialDest -Force
    }
}

# If the package was extracted directly into the project root, the files are
# already at their final destinations, which is also valid.
if (!(Test-Path $bridgeDest)) {
    Write-Host "FAILED: WarboardDiceNetworkBridge.cs is missing."
    exit 4
}

if (!(Test-Path $partialDest)) {
    Write-Host "FAILED: TraditionalDiceTray3D.Multiplayer.cs is missing."
    exit 5
}

Write-Host "[OK] Multiplayer dice source files present"

$dice = Read-Text $dicePath

# Safe to rerun after V1: every operation below is idempotent.
$dice = [regex]::Replace(
    $dice,
    'public\s+class\s+TraditionalDiceTray3D\s*:\s*MonoBehaviour',
    'public partial class TraditionalDiceTray3D : MonoBehaviour',
    1
)

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

$dice = [regex]::Replace(
    $dice,
    'marker\.SetSelected\s*\(\s*!marker\.Selected\s*\)\s*;',
    'SetDieSelectedShared(marker, !marker.Selected);'
)

Write-Text $dicePath $dice
Write-Host "[OK] TraditionalDiceTray3D.cs patched"

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

Write-Host "[OK] WarboardMultiplayerBootstrap.cs patched"

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

if (!(Test-Path $bridgeDest)) {
    $errors += "WarboardDiceNetworkBridge.cs missing."
}

if (!(Test-Path $partialDest)) {
    $errors += "TraditionalDiceTray3D.Multiplayer.cs missing."
}

Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "FIX INCOMPLETE:"
    foreach ($entry in $errors) {
        Write-Host ("  - " + $entry)
    }
    exit 6
}

Write-Host "SUCCESS - SHARED MULTIPLAYER DICE V2 VERIFIED"
Write-Host ""
Write-Host "Return to Unity and let it compile."
Write-Host "Then REBUILD the Windows EXE before multiplayer testing."

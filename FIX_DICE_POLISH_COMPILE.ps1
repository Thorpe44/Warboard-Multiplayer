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
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $dir) {
            break
        }

        $dir = $parent
    }

    return $null
}

$Root = Find-WarboardRoot $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($Root)) {
    Write-Host "FAILED: Warboard root not found."
    exit 1
}

$Path = Join-Path $Root "Assets\Scripts\Core\TraditionalDiceTray3D.Polish.cs"

if (!(Test-Path $Path)) {
    Write-Host "FAILED: TraditionalDiceTray3D.Polish.cs not found."
    exit 2
}

$text = [System.IO.File]::ReadAllText($Path)

if ($text.Contains("trayRoot.transform.lossyScale")) {
    Write-Host "ALREADY FIXED."
}
elseif ($text.Contains("trayRoot.lossyScale")) {
    $text = $text.Replace(
        "trayRoot.lossyScale",
        "trayRoot.transform.lossyScale"
    )

    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $text, $utf8)

    Write-Host "[FIXED] trayRoot.lossyScale -> trayRoot.transform.lossyScale"
}
else {
    Write-Host "FAILED: Expected lossyScale line not found."
    exit 3
}

$verify = [System.IO.File]::ReadAllText($Path)

if (!$verify.Contains("trayRoot.transform.lossyScale")) {
    Write-Host "FAILED: Verification failed."
    exit 4
}

Write-Host ""
Write-Host "SUCCESS - DICE POLISH COMPILE HOTFIX VERIFIED"

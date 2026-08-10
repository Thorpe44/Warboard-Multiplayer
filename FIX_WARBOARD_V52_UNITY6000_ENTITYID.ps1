$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V52 - UNITY 6000.5 ENTITY ID FIX" -ForegroundColor Cyan
Write-Host "------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 7; $i++) {
        $target = Join-Path $candidate "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"

        if (Test-Path $target) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate

        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent -eq $candidate) {
            break
        }

        $candidate = $parent
    }

    foreach ($child in Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue) {
        $target = Join-Path $child.FullName "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"

        if (Test-Path $target) {
            return $child.FullName
        }
    }

    return $null
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir

if (-not $ProjectRoot) {
    Write-Host "ERROR: Could not find GameController.V52PlacementGhost.cs" -ForegroundColor Red
    Write-Host "Put this fix in the Warboard project root and run it again."
    Read-Host "Press Enter to close"
    exit 1
}

$Target = Join-Path $ProjectRoot "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"
$Text = Get-Content -Raw -Path $Target

if ($Text -notmatch "GetInstanceID\(\)") {
    if ($Text -match "GetHashCode\(\)") {
        Write-Host "The Unity 6000.5 fix is already installed." -ForegroundColor Green
        Read-Host "Press Enter to close"
        exit 0
    }

    Write-Host "ERROR: Expected GetInstanceID() call was not found." -ForegroundColor Red
    Write-Host "Nothing was changed."
    Read-Host "Press Enter to close"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\V52_EntityIdFix\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
Copy-Item $Target (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") -Force

$Text = $Text.Replace("source.GetInstanceID()", "source.GetHashCode()")
Set-Content -Path $Target -Value $Text -Encoding UTF8

$Verify = Get-Content -Raw -Path $Target

if ($Verify -match "GetInstanceID\(\)" -or
    $Verify -notmatch "source\.GetHashCode\(\)") {
    Write-Host "ERROR: Verification failed. Restoring backup." -ForegroundColor Red
    Copy-Item (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") $Target -Force
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host "Fixed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Changed:"
Write-Host "  source.GetInstanceID()"
Write-Host "to:"
Write-Host "  source.GetHashCode()"
Write-Host ""
Write-Host "This removes the Unity 6000.5 CS0619 compile error."
Write-Host "Return to Unity and let it recompile." -ForegroundColor Cyan
Write-Host ""

Read-Host "Press Enter to close"

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "WARBOARD V53 - PLACEMENT GHOST TYPE COMPILE FIX" -ForegroundColor Cyan
Write-Host "------------------------------------------------" -ForegroundColor DarkCyan
Write-Host ""

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Find-WarboardRoot {
    param([string]$Start)

    $candidate = (Resolve-Path $Start).Path

    for ($i = 0; $i -lt 10; $i++) {
        $v52 = Join-Path $candidate "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"
        $v53 = Join-Path $candidate "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs"

        if ((Test-Path $v52) -and (Test-Path $v53)) {
            return $candidate
        }

        $parent = Split-Path -Parent $candidate
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $candidate) {
            break
        }

        $candidate = $parent
    }

    foreach ($child in Get-ChildItem -Path $Start -Directory -ErrorAction SilentlyContinue) {
        $v52 = Join-Path $child.FullName "Assets\Scripts\Core\GameController.V52PlacementGhost.cs"
        $v53 = Join-Path $child.FullName "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs"

        if ((Test-Path $v52) -and (Test-Path $v53)) {
            return $child.FullName
        }
    }

    return $null
}

function Fail {
    param([string]$Message)
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    Write-Host "No project files were changed."
    Read-Host "Press Enter to close"
    exit 1
}

$ProjectRoot = Find-WarboardRoot -Start $ScriptDir
if (-not $ProjectRoot) {
    Fail "Could not find V52 and V53 source files."
}

Write-Host "Project: $ProjectRoot" -ForegroundColor Green

$CoreDir = Join-Path $ProjectRoot "Assets\Scripts\Core"
$V52 = Join-Path $CoreDir "GameController.V52PlacementGhost.cs"
$V53 = Join-Path $CoreDir "GameController.V53SolidSceneryPlacement.cs"

$v52Text = Get-Content -Raw -Path $V52
$v53Text = Get-Content -Raw -Path $V53

# The compile error is caused by V53 declaring a method whose parameter type is
# the V52-only nested preview candidate class. We remove that cross-file type
# dependency entirely and keep the scenery check inside V52, where the type is
# definitely in scope.

$callOld = @'
        // V53: keep V52's preview in sync with authoritative placement.
        if (!V53GhostCandidatesClearOfSolidAreaScenery(
                candidates))
        {
            return false;
        }
'@

$callNew = @'
        // V53: keep the ghost preview in sync with authoritative placement.
        // This loop lives in V52 so PlacementGhostCandidate52 is always in scope.
        foreach (PlacementGhostCandidate52 candidate
            in candidates)
        {
            if (candidate == null ||
                candidate.Model == null)
            {
                continue;
            }

            if (V53ModelBaseOverlapsSolidAreaScenery(
                    candidate.Model,
                    candidate.Destination))
            {
                return false;
            }
        }
'@

if (-not $v52Text.Contains($callOld) -and
    -not $v52Text.Contains($callNew)) {
    Fail "Could not locate the V53 ghost legality hook in V52."
}

# Remove the entire helper method from V53 by brace matching.
function Remove-Method {
    param(
        [string]$Text,
        [string]$Signature
    )

    $m = [regex]::Match(
        $Text,
        $Signature,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    if (-not $m.Success) {
        return $Text
    }

    $open = $Text.IndexOf("{", $m.Index + $m.Length)
    if ($open -lt 0) {
        throw "Could not find opening brace for V53 ghost helper."
    }

    $depth = 0
    $close = -1

    for ($i = $open; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq "{") {
            $depth++
        }
        elseif ($Text[$i] -eq "}") {
            $depth--
            if ($depth -eq 0) {
                $close = $i
                break
            }
        }
    }

    if ($close -lt 0) {
        throw "Could not find closing brace for V53 ghost helper."
    }

    $start = $m.Index

    # Eat preceding whitespace/newlines cleanly.
    while ($start -gt 0 -and
           ($Text[$start - 1] -eq "`r" -or
            $Text[$start - 1] -eq "`n" -or
            $Text[$start - 1] -eq " " -or
            $Text[$start - 1] -eq "`t")) {
        $start--
    }

    $end = $close + 1

    while ($end -lt $Text.Length -and
           ($Text[$end] -eq "`r" -or
            $Text[$end] -eq "`n")) {
        $end++
    }

    return $Text.Substring(0, $start) + $Text.Substring($end)
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $ProjectRoot "Library\WarboardBackups\V53_TypeFix\$timestamp"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
Copy-Item $V52 (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") -Force
Copy-Item $V53 (Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs") -Force

try {
    if ($v52Text.Contains($callOld)) {
        $v52Text = $v52Text.Replace($callOld, $callNew)
    }

    $v53Text = Remove-Method `
        -Text $v53Text `
        -Signature '\bprivate\s+bool\s+V53GhostCandidatesClearOfSolidAreaScenery\s*\('

    Set-Content -Path $V52 -Value $v52Text -Encoding UTF8
    Set-Content -Path $V53 -Value $v53Text -Encoding UTF8

    $verify52 = Get-Content -Raw -Path $V52
    $verify53 = Get-Content -Raw -Path $V53

    if ($verify53 -match 'PlacementGhostCandidate52') {
        throw "PlacementGhostCandidate52 still appears in the V53 helper file."
    }

    if ($verify52 -notmatch 'foreach\s*\(\s*PlacementGhostCandidate52\s+candidate') {
        throw "V52 inline ghost legality loop was not installed."
    }

    if ($verify52 -notmatch 'V53ModelBaseOverlapsSolidAreaScenery') {
        throw "V52 no longer calls the V53 solid-scenery test."
    }
}
catch {
    Write-Host ""
    Write-Host "Fix failed. Restoring backup..." -ForegroundColor Red
    Copy-Item (Join-Path $BackupDir "GameController.V52PlacementGhost.cs") $V52 -Force
    Copy-Item (Join-Path $BackupDir "GameController.V53SolidSceneryPlacement.cs") $V53 -Force
    Write-Host $_.Exception.Message
    Read-Host "Press Enter to close"
    exit 1
}

Write-Host ""
Write-Host "V53 compile fix installed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Fixed CS0246:"
Write-Host "  PlacementGhostCandidate52 is no longer referenced from the V53 file."
Write-Host "  The ghost legality loop now lives inside V52, where that type is defined."
Write-Host ""
Write-Host "No gameplay behaviour was removed." -ForegroundColor Cyan
Write-Host "Return to Unity and let it compile."
Write-Host ""
Read-Host "Press Enter to close"

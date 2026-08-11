param(
    [Parameter(Mandatory=$true)]
    [string]$RepoRoot
)

$ErrorActionPreference = "Stop"

if ($null -eq $RepoRoot) {
    throw "No repository path was supplied."
}

$RepoRoot = $RepoRoot.Trim()

if ($RepoRoot.Length -ge 2 -and
    $RepoRoot.StartsWith('"') -and
    $RepoRoot.EndsWith('"')) {
    $RepoRoot =
        $RepoRoot.Substring(
            1,
            $RepoRoot.Length - 2
        )
}

$resolvedRepo =
    Resolve-Path -LiteralPath $RepoRoot -ErrorAction Stop

$RepoRoot = $resolvedRepo.Path
$BundleRoot = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($BundleRoot)) {
    $BundleRoot =
        Split-Path -Parent $MyInvocation.MyCommand.Path
}

$expected = Join-Path $RepoRoot "Assets\Scripts\Core\GameController.UI.cs"
if (-not (Test-Path -LiteralPath $expected)) {
    throw "GameController.UI.cs was not found under: $RepoRoot"
}

Write-Host ""
Write-Host "Target repo: $RepoRoot"
Write-Host "Targeted source: Thorpe44/Warboard-Multiplayer main (V61 cleaned)"
Write-Host ""

if (Get-Command git.exe -ErrorAction SilentlyContinue) {
    try {
        $remote = (& git.exe -C $RepoRoot remote get-url origin 2>$null)
        if ($remote) {
            Write-Host "Git origin: $remote"
            if ($remote -notmatch "Thorpe44[\\/]Warboard-Multiplayer(\.git)?$") {
                Write-Warning "Origin is not the expected Thorpe44/Warboard-Multiplayer repository."
            }
        }

        $head = (& git.exe -C $RepoRoot rev-parse --short HEAD 2>$null)
        if ($head) { Write-Host "Current HEAD: $head" }
    } catch {
        Write-Warning "Could not inspect git metadata; continuing with source-marker validation."
    }
}


# V3 preflight: make sure the local checkout still has the reviewed UI shape.
$preflightChecks = @(
    @{
        File = "Assets/Scripts/Core/GameController.UI.cs"
        Pattern = 'private void DrawStatusToast\(\)'
        Label = "status toast"
    },
    @{
        File = "Assets/Scripts/Core/GameController.UI.cs"
        Pattern = 'private void DrawTopCommandBar\(\)'
        Label = "top command bar"
    },
    @{
        File = "Assets/Scripts/Factions/Aeldari/AeldariSetupUI.cs"
        Pattern = 'DrawLockedDetachmentBadges\(aeldari\);'
        Label = "Aeldari locked badge"
    },
    @{
        File = "Assets/Scripts/Factions/AdeptusCustodes/CustodesSetupUI.cs"
        Pattern = 'DrawLockedBadges\(controllers\);'
        Label = "Custodes locked badge"
    },
    @{
        File = "Assets/Scripts/Factions/Necrons/NecronsSetupUI.cs"
        Pattern = 'DrawLockedBadges\(controllers\);'
        Label = "Necrons locked badge"
    }
)

foreach ($check in $preflightChecks) {
    $checkPath =
        Join-Path $RepoRoot (
            $check.File -replace "/", "\"
        )

    if (-not (Test-Path -LiteralPath $checkPath)) {
        throw "Preflight failed: required file missing: $($check.File)"
    }

    $checkText =
        [System.IO.File]::ReadAllText(
            $checkPath
        )

    if (-not [regex]::IsMatch(
            $checkText,
            $check.Pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )) {
        throw "Preflight failed: $($check.Label) does not match the reviewed main source. Nothing was changed."
    }
}

Write-Host "[preflight] source markers verified"
Write-Host ""

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupRoot = Join-Path $RepoRoot ("_warboard_ui_fix_backup_" + $stamp)
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null

$createdFiles = New-Object System.Collections.Generic.List[string]
$backedUpFiles = New-Object System.Collections.Generic.List[string]

function Get-RepoPath([string]$relative) {
    return Join-Path $RepoRoot ($relative -replace "/", "\")
}

function Backup-One([string]$relative) {
    $src = Get-RepoPath $relative
    if (Test-Path -LiteralPath $src) {
        $dst = Join-Path $backupRoot ($relative -replace "/", "\")
        $dstDir = Split-Path -Parent $dst
        New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
        Copy-Item -LiteralPath $src -Destination $dst -Force
        $backedUpFiles.Add($relative) | Out-Null
    }
}

function Read-Text([string]$relative) {
    $path = Get-RepoPath $relative
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required file missing: $relative"
    }
    return [System.IO.File]::ReadAllText($path)
}

function Write-Text([string]$relative, [string]$text) {
    $path = Get-RepoPath $relative
    $dir = Split-Path -Parent $path
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $text, $utf8)
}

function Replace-RegexOnce(
    [string]$relative,
    [string]$pattern,
    [string]$replacement,
    [string]$label,
    [string]$alreadyPattern = ""
) {
    $text = Read-Text $relative

    if ($alreadyPattern -and [regex]::IsMatch($text, $alreadyPattern)) {
        Write-Host "[already] $label"
        return
    }

    $matches = [regex]::Matches(
        $text,
        $pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    if ($matches.Count -ne 1) {
        throw "${label}: expected exactly 1 source match in $relative, found $($matches.Count). No unsafe edit was made for this step."
    }

    $newText = [regex]::Replace(
        $text,
        $pattern,
        $replacement,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    Write-Text $relative $newText
    Write-Host "[patched] $label"
}

function Remove-LineOnce(
    [string]$relative,
    [string]$pattern,
    [string]$label
) {
    $text = Read-Text $relative
    $matches = [regex]::Matches(
        $text,
        $pattern,
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    )

    if ($matches.Count -eq 0) {
        Write-Host "[already] $label"
        return
    }

    if ($matches.Count -ne 1) {
        throw "${label}: expected 1 line in $relative, found $($matches.Count)."
    }

    $newText = [regex]::Replace(
        $text,
        $pattern,
        "        // WARBOARD_V62_SETUP_BADGE_REMOVED`r`n",
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    )

    Write-Text $relative $newText
    Write-Host "[patched] $label"
}

$ui = "Assets/Scripts/Core/GameController.UI.cs"
$aeldari = "Assets/Scripts/Factions/Aeldari/AeldariSetupUI.cs"
$custodes = "Assets/Scripts/Factions/AdeptusCustodes/CustodesSetupUI.cs"
$necrons = "Assets/Scripts/Factions/Necrons/NecronsSetupUI.cs"

$targets = @($ui, $aeldari, $custodes, $necrons)
foreach ($target in $targets) {
    Backup-One $target
}

try {
    # 1) Kill the old permanent faction/detachment LOCKED bars completely.
    Remove-LineOnce `
        $aeldari `
        '^[ \t]*DrawLockedDetachmentBadges\(aeldari\);[ \t]*\r?\n' `
        "remove Aeldari LOCKED player bar"

    Remove-LineOnce `
        $custodes `
        '^[ \t]*DrawLockedBadges\(controllers\);[ \t]*\r?\n' `
        "remove Custodes LOCKED player bar"

    Remove-LineOnce `
        $necrons `
        '^[ \t]*DrawLockedBadges\(controllers\);[ \t]*\r?\n' `
        "remove Necrons LOCKED player bar"

    # 2) Make the top-left navigation breathe and stop clipping.
    Replace-RegexOnce `
        $ui `
        'Rect bar\s*=\s*new Rect\(\s*8f,\s*6f,\s*Screen\.width - 16f,\s*76f\s*\);' `
        @'
Rect bar =
            new Rect(
                14f,
                6f,
                Screen.width - 28f,
                76f
            );
'@ `
        "add safe inset to top command bar" `
        'Rect bar\s*=\s*new Rect\(\s*14f,\s*6f,\s*Screen\.width - 28f,\s*76f\s*\);'

    Replace-RegexOnce `
        $ui `
        'float leftMetaX\s*=\s*bar\.x \+ 12f;' `
        @'
float leftMetaX =
            bar.x + 18f;
'@ `
        "move top-left mode label inward" `
        'float leftMetaX\s*=\s*bar\.x \+ 18f;'

    Replace-RegexOnce `
        $ui `
        'float leftX\s*=\s*bar\.x \+ 120f;' `
        @'
float leftX =
            bar.x + 138f;
'@ `
        "move top-left navigation inward" `
        'float leftX\s*=\s*bar\.x \+ 138f;'

    Replace-RegexOnce `
        $ui `
        '(new Rect\(\s*leftX,\s*bar\.y \+ 8f,\s*)86f(,\s*34f\s*\),\s*"WARBOARD")' `
        '${1}96f${2}' `
        "widen WARBOARD button" `
        'new Rect\(\s*leftX,\s*bar\.y \+ 8f,\s*96f,\s*34f\s*\),\s*"WARBOARD"'

    Replace-RegexOnce `
        $ui `
        'leftX \+= 86f \+ leftGap;' `
        'leftX += 96f + leftGap;' `
        "advance layout after WARBOARD button" `
        'leftX \+= 96f \+ leftGap;'

    Replace-RegexOnce `
        $ui `
        '(new Rect\(\s*leftX,\s*bar\.y \+ 8f,\s*)80f(,\s*34f\s*\),\s*"MISSION INFO")' `
        '${1}108f${2}' `
        "widen MISSION INFO button" `
        'new Rect\(\s*leftX,\s*bar\.y \+ 8f,\s*108f,\s*34f\s*\),\s*"MISSION INFO"'

    Replace-RegexOnce `
        $ui `
        'leftX \+= 80f \+ leftGap;' `
        'leftX += 108f + leftGap;' `
        "advance layout after MISSION INFO button" `
        'leftX \+= 108f \+ leftGap;'

    # 3) Replace the permanent bottom status frame with a small timed toast.
    $toastMethod = @'
    private void DrawStatusToast()
    {
        // WARBOARD_V62_TRANSIENT_STATUS_TOAST
        bool showTransientToast =
            ShouldDrawTransientStatusToast(
                status
            );

        if (battleSetupMode ||
            armyImportMode ||
            missionSetupMode ||
            battleOver ||
            deploymentMode ||
            showBattleLog ||
            showMissionPanel ||
            showDatasheet ||
            showRuleChoiceWindow ||
            interactiveAttack != null ||
            showStratagemReaction ||
            showStratagemMenu ||
            !showTransientToast)
        {
            return;
        }

        float width =
            Mathf.Min(
                620f,
                Screen.width - 40f
            );

        Rect panel =
            new Rect(
                (Screen.width -
                 width) *
                    0.5f,
                Screen.height - 52f,
                width,
                36f
            );

        DrawTintedBox(
            panel,
            new Color(
                0.035f,
                0.04f,
                0.055f,
                0.92f
            )
        );

        GUIStyle style =
            new GUIStyle(
                GUI.skin.label
            );

        style.alignment =
            TextAnchor.MiddleCenter;
        style.fontStyle =
            FontStyle.Bold;
        style.fontSize = 12;

        GUI.Label(
            new Rect(
                panel.x + 12f,
                panel.y + 4f,
                panel.width - 24f,
                panel.height - 8f
            ),
            status,
            style
        );
    }

    private void DrawTintedBox
'@

    Replace-RegexOnce `
        $ui `
        '    private void DrawStatusToast\(\)\s*\{.*?\r?\n    \}\s*\r?\n\s*    private void DrawTintedBox' `
        $toastMethod `
        "make bottom status a temporary toast" `
        'WARBOARD_V62_TRANSIENT_STATUS_TOAST'

    # 4) Explicitly make the selected-unit card lifecycle selection-only.
    Replace-RegexOnce `
        $ui `
        '        DrawV45SelectedUnitCard\(\);' `
        @'
        if (selectedSquad != null)
            DrawV45SelectedUnitCard();
'@ `
        "draw selected-unit card only when a unit is selected" `
        'if \(selectedSquad != null\)\s*\r?\n\s*DrawV45SelectedUnitCard\(\);'

    # Confirm the card implementation itself has its own null guard too.
    $cardPath = "Assets/Scripts/Core/GameController.V45Presentation.cs"
    if (Test-Path -LiteralPath (Get-RepoPath $cardPath)) {
        $cardText = Read-Text $cardPath
        if ($cardText -notmatch 'if \(selectedSquad == null \|\|') {
            throw "Selected-unit card no longer contains its expected no-selection guard. Refusing to guess."
        }
        Write-Host "[verified] selected-unit card already collapses when nothing is selected"
    }

    # 5) Install the small toast state helper + bespoke faction-rules router.
    $payloadFiles = @(
        "Assets/Scripts/Core/GameController.V62UILifecycle.cs",
        "Assets/Scripts/Factions/WarboardBespokeFactionRulesUI.cs"
    )

    foreach ($relative in $payloadFiles) {
        $src = Join-Path $BundleRoot ("Payload\" + ($relative -replace "/", "\"))
        if (-not (Test-Path -LiteralPath $src)) {
            throw "Bundle payload missing: $relative"
        }

        $dst = Get-RepoPath $relative
        if (Test-Path -LiteralPath $dst) {
            Backup-One $relative
        } else {
            $createdFiles.Add($relative) | Out-Null
        }

        $dstDir = Split-Path -Parent $dst
        New-Item -ItemType Directory -Path $dstDir -Force | Out-Null
        Copy-Item -LiteralPath $src -Destination $dst -Force
        Write-Host "[installed] $relative"
    }

    # Write undo metadata only after the patch itself succeeded.
    $metadata = [ordered]@{
        RepoRoot = $RepoRoot
        BackupRoot = $backupRoot
        BackedUpFiles = @($backedUpFiles)
        CreatedFiles = @($createdFiles)
        AppliedAt = (Get-Date).ToString("o")
        Target = "Thorpe44/Warboard-Multiplayer main V61"
    }

    $metadataPath = Join-Path $RepoRoot "_warboard_ui_fix_backup_latest.json"
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

    Write-Host ""
    Write-Host "============================================================"
    Write-Host "PATCHED SUCCESSFULLY"
    Write-Host "============================================================"
    Write-Host "Backup: $backupRoot"
    Write-Host ""
    Write-Host "Changes applied:"
    Write-Host "  - removed Aeldari/Custodes/Necron LOCKED deployment bars"
    Write-Host "  - bottom status is now a short-lived toast"
    Write-Host "  - 'No squad selected.' produces no bottom UI"
    Write-Host "  - selected-unit card is explicitly selection-only"
    Write-Host "  - top-left WARBOARD / MISSION INFO layout is inset + widened"
    Write-Host "  - bespoke Aeldari/Custodes/Necron FACTION RULES button restored"
    Write-Host "  - Standard11 still handles Orks/Tyranids/Space Marines"
    Write-Host "  - deployment-zone lines were NOT changed"
    Write-Host ""

    if (Get-Command git.exe -ErrorAction SilentlyContinue) {
        Write-Host "Local diff summary:"
        & git.exe -C $RepoRoot diff --stat
        Write-Host ""
        Write-Host "Git whitespace check:"
        & git.exe -C $RepoRoot diff --check
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "git diff --check reported an issue. Inspect the diff before committing."
        }
    }

    exit 0
}
catch {
    Write-Host ""
    Write-Error $_
    Write-Host ""
    Write-Host "Patch stopped safely."
    if ($null -ne $backupRoot -and
        (Test-Path -LiteralPath $backupRoot)) {
        Write-Host "Backup folder:"
        Write-Host "  $backupRoot"
        Write-Host ""
    }
    exit 1
}

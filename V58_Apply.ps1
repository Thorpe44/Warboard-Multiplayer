$ErrorActionPreference = "Stop"

function Find-ProjectRoot {
    $scriptDir = Split-Path -Parent $MyInvocation.ScriptName

    $candidates = @(
        $scriptDir,
        (Split-Path -Parent $scriptDir),
        (Split-Path -Parent (Split-Path -Parent $scriptDir))
    ) | Select-Object -Unique

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }

        $probe = Join-Path $candidate "Assets\Scripts\Core\GameController.cs"
        if (Test-Path $probe) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw @"
Could not find the Warboard Unity project.

Put the WarboardV58 folder either:
  1. inside the Warboard project root (next to Assets), or
  2. extract the contents directly into the project root.

Then run V58_Apply.bat again.
"@
}

$ProjectRoot = Find-ProjectRoot
Write-Host "Project: $ProjectRoot" -ForegroundColor Cyan

$Required = @(
    "Assets\Scripts\Core\GameController.V55MissionCards.cs",
    "Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs",
    "Assets\Scripts\Core\GameController.UI.cs",
    "Assets\Scripts\Core\GameController.cs",
    "Assets\Scripts\Core\WarboardBuildInfo.cs"
)

foreach ($relative in $Required) {
    $path = Join-Path $ProjectRoot $relative
    if (-not (Test-Path $path)) {
        throw "Required V57 file is missing: $relative"
    }
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupRoot = Join-Path $ProjectRoot ("WarboardV58_Backup_" + $stamp)

function Backup-File([string]$relative) {
    $source = Join-Path $ProjectRoot $relative
    $destination = Join-Path $BackupRoot $relative
    $destinationDir = Split-Path -Parent $destination
    New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

foreach ($relative in $Required) {
    Backup-File $relative
}

Write-Host "Backup:  $BackupRoot" -ForegroundColor DarkGray

$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Normalized([string]$relative) {
    $path = Join-Path $ProjectRoot $relative
    return [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
}

function Write-Normalized([string]$relative, [string]$text) {
    $path = Join-Path $ProjectRoot $relative
    [IO.File]::WriteAllText($path, $text, $Utf8NoBom)
}

function Replace-ExactSingle(
    [string]$relative,
    [string]$oldText,
    [string]$newText,
    [string]$label,
    [string]$alreadyMarker = ""
) {
    $text = Read-Normalized $relative

    if (-not [string]::IsNullOrWhiteSpace($alreadyMarker) -and
        $text.Contains($alreadyMarker)) {
        Write-Host "[SKIP] $label already present." -ForegroundColor Yellow
        return
    }

    $oldText = $oldText.Replace("`r`n", "`n")
    $newText = $newText.Replace("`r`n", "`n")

    $first = $text.IndexOf($oldText, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "${label}: expected V57 source block was not found. No unsafe replacement was attempted."
    }

    $second = $text.IndexOf(
        $oldText,
        $first + $oldText.Length,
        [StringComparison]::Ordinal
    )

    if ($second -ge 0) {
        throw "${label}: source block appeared more than once. No unsafe replacement was attempted."
    }

    $patched =
        $text.Substring(0, $first) +
        $newText +
        $text.Substring($first + $oldText.Length)

    Write-Normalized $relative $patched
    Write-Host "[OK]   $label" -ForegroundColor Green
}

function Replace-RegexSingle(
    [string]$relative,
    [string]$pattern,
    [string]$replacement,
    [string]$label,
    [string]$alreadyMarker = ""
) {
    $text = Read-Normalized $relative

    if (-not [string]::IsNullOrWhiteSpace($alreadyMarker) -and
        $text.Contains($alreadyMarker)) {
        Write-Host "[SKIP] $label already present." -ForegroundColor Yellow
        return
    }

    # Patterns passed to this helper already declare their own inline
    # regex modes where needed, e.g. (?ms). Construct the Regex directly
    # instead of combining enum values through New-Object, which Windows
    # PowerShell 5.1 can parse as an Object[] and then fail on -bor.
    $rx = New-Object System.Text.RegularExpressions.Regex($pattern)

    $matches = $rx.Matches($text)
    if ($matches.Count -ne 1) {
        throw "${label}: expected exactly one V57 source block, found $($matches.Count). No unsafe replacement was attempted."
    }

    $m = $matches[0]
    $replacement = $replacement.Replace("`r`n", "`n")

    $patched =
        $text.Substring(0, $m.Index) +
        $replacement +
        $text.Substring($m.Index + $m.Length)

    Write-Normalized $relative $patched
    Write-Host "[OK]   $label" -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# 1) V58: complete the visible scoring summary for all registered secondaries.
# ---------------------------------------------------------------------------

$secondaryMethod = @'
    // WARBOARD_V58_SECONDARY_CARD_TEXT
    private string V55SecondarySummary(
        string card,
        bool fixedMode)
    {
        switch (card)
        {
            case "A Grievous Blow":
                return fixedMode
                    ? "End of a turn: 4VP for each enemy unit with Starting Strength 13+ destroyed this turn."
                    : "End of a turn: 5VP if one or more enemy units with Starting Strength 13+ were destroyed this turn.";

            case "A Tempting Target":
                return
                    "End of your turn: control the objective your opponent selected as the Tempting Target for 5VP.";

            case "Assassination":
                return fixedMode
                    ? "3VP for each enemy Character model destroyed this turn; +1VP for each such model with 4+ Wounds."
                    : "End of either player's turn: 5VP if an enemy Character was destroyed this turn, or all enemy Characters have been destroyed.";

            case "Beacon":
                return
                    "At the end of the opponent's turn or battle round 5: beacon unit on battlefield outside your deployment zone = 3VP; outside your territory = 5VP.";

            case "Behind Enemy Lines":
                return
                    "End of your turn: 3VP per eligible unit wholly in the enemy deployment zone, maximum 5VP.";

            case "Bring it Down":
                return fixedMode
                    ? "End of a turn: 4VP for each enemy model with 10+ Wounds destroyed this turn."
                    : "End of a turn: 5VP if one or more enemy models with 10+ Wounds were destroyed this turn.";

            case "Burden of Trust":
                return
                    "End of the opponent's turn or battle round 5: 2VP per objective you are still guarding, maximum 5VP.";

            case "Centre Ground":
                return
                    "End of your turn: eligible unit within 3\" of centre = 3VP if no enemy is within 3\"; 5VP if no enemy is within 6\".";

            case "Cleanse":
                return
                    "Complete Cleanse actions on objectives: end of your turn, 1 cleansed objective = 2VP; 2+ cleansed objectives = 5VP.";

            case "Defend Stronghold":
                return
                    "From round 2, end of the opponent's turn or battle round 5: control your home objective = 3VP; +2VP if no enemy units are in your deployment zone.";

            case "Display of Might":
                return
                    "Have more eligible units than the enemy wholly in No Man's Land: end of your turn = 2VP; end of opponent's turn = 5VP.";

            case "Engage on All Fronts":
                return fixedMode
                    ? "End of your turn: presence in 3 table quarters = 2VP; all 4 quarters = 4VP."
                    : "End of your turn: presence in 3 table quarters = 3VP; all 4 quarters = 5VP.";

            case "Forward Position":
                return
                    "End of your turn: control the opponent's home objective, all expansion objectives, or both for 5VP.";

            case "No Prisoners":
                return
                    "End of a turn: 2VP per enemy unit destroyed this turn, maximum 5VP.";

            case "Outflank":
                return
                    "End of your turn: eligible unit within 6\" of a battlefield edge and outside your territory = 3VP; eligible units at opposite edges, with one outside your territory = 5VP.";

            case "Overwhelming Force":
                return
                    "End of a turn: 3VP per enemy unit destroyed that started the turn within range of an objective, maximum 5VP.";

            case "Plunder":
                return
                    "Complete a Plunder action in terrain outside your territory; end of your turn, if terrain was plundered this turn = 5VP.";

            case "Secure No Man's Land":
                return
                    "End of your turn: control 2 or more objectives in No Man's Land, excluding your home objective, for 5VP.";

            default:
                return
                    "Open MISSION INFO for this card's scoring condition.";
        }
    }

'@

Replace-RegexSingle `
    "Assets\Scripts\Core\GameController.V55MissionCards.cs" `
    '(?ms)^    private string V55SecondarySummary\(.*?^    \}\n\n(?=    private string V55PrimarySummary\()' `
    $secondaryMethod `
    "Secondary mission card summaries" `
    "WARBOARD_V58_SECONDARY_CARD_TEXT"

# ---------------------------------------------------------------------------
# 2) V58: Standard11 rules UI may only bind to the currently active Standard11
# faction. Remove both arbitrary 'first controller' fallbacks.
# ---------------------------------------------------------------------------

$oldRulesPanelFallback = @'
        if (showRules)
        {
            StandardFactionGameController
                controller =
                    controllers.FirstOrDefault(
                        value =>
                            string.Equals(
                                value.FactionId,
                                rulesFaction,
                                StringComparison.OrdinalIgnoreCase))
                    ?? controllers.FirstOrDefault();

            if (controller != null)
                DrawRulesPanel(controller);
        }
'@

$newRulesPanelFallback = @'
        // WARBOARD_V58_ACTIVE_FACTION_RULES_ONLY
        if (showRules)
        {
            StandardFactionGameController
                controller =
                    controllers.FirstOrDefault(
                        value =>
                            string.Equals(
                                value.FactionId,
                                rulesFaction,
                                StringComparison.OrdinalIgnoreCase));

            if (controller != null)
            {
                DrawRulesPanel(controller);
            }
            else
            {
                // A stale rulesFaction must never fall through to an
                // unrelated Standard11 army (for example Orks).
                showRules = false;
                rulesFaction = "";
            }
        }
'@

Replace-ExactSingle `
    "Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs" `
    $oldRulesPanelFallback `
    $newRulesPanelFallback `
    "Remove stale Standard11 rules-panel fallback" `
    "WARBOARD_V58_ACTIVE_FACTION_RULES_ONLY"

$oldActiveFactionFallback = @'
        StandardFactionGameController
            active =
                controllers
                    .FirstOrDefault(
                        value =>
                            string.Equals(
                                value.FactionId,
                                game.ActiveFactionId,
                                StringComparison.OrdinalIgnoreCase))
                ?? controllers[0];

        Rect button =
'@

$newActiveFactionFallback = @'
        StandardFactionGameController
            active =
                controllers
                    .FirstOrDefault(
                        value =>
                            string.Equals(
                                value.FactionId,
                                game.ActiveFactionId,
                                StringComparison.OrdinalIgnoreCase));

        // WARBOARD_V58_STANDARD_RULES_ACTIVE_FACTION_GUARD
        if (active == null)
        {
            // The active player is handled by a bespoke faction controller
            // (Aeldari, Custodes, Necrons, etc.) or there is no matching
            // Standard11 controller. Do not show another army's rules.
            if (showRules)
            {
                showRules = false;
                rulesFaction = "";
            }

            return;
        }

        Rect button =
'@

Replace-ExactSingle `
    "Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs" `
    $oldActiveFactionFallback `
    $newActiveFactionFallback `
    "Bind Standard11 faction-rules button to active faction only" `
    "WARBOARD_V58_STANDARD_RULES_ACTIVE_FACTION_GUARD"

# ---------------------------------------------------------------------------
# 3) V58: deployment status becomes an auto-expiring toast instead of a
# permanent bottom bar.
# ---------------------------------------------------------------------------

$oldStatusField = @'
    private string status = "Ready.";
'@

$newStatusField = @'
    private string status = "Ready.";

    // WARBOARD_V58_TRANSIENT_STATUS_TOAST_STATE
    private string v58LastStatusToastText = "";
    private float v58StatusToastVisibleUntil = -1f;
'@

Replace-ExactSingle `
    "Assets\Scripts\Core\GameController.cs" `
    $oldStatusField `
    $newStatusField `
    "Add transient status-toast state" `
    "WARBOARD_V58_TRANSIENT_STATUS_TOAST_STATE"

$oldStatusPanel = @'
        Rect statusPanel =
            new Rect(
                panel.x + 18f,
                panel.y +
                    panel.height -
                    64f,
                panel.width - 36f,
                46f
            );

        DrawTintedBox(
            statusPanel,
            new Color(
                0.055f,
                0.06f,
                0.075f,
                1.0f
            )
        );

        GUIStyle statusStyle =
            new GUIStyle(
                GUI.skin.label
            );

        statusStyle.wordWrap = true;
        statusStyle.alignment =
            TextAnchor.MiddleLeft;
        statusStyle.fontSize = 12;

        GUI.Label(
            new Rect(
                statusPanel.x + 12f,
                statusPanel.y + 5f,
                statusPanel.width - 24f,
                statusPanel.height - 10f
            ),
            status,
            statusStyle
        );
'@

$newStatusPanel = @'
        // WARBOARD_V58_TRANSIENT_STATUS_TOAST
        // Status is assigned throughout the existing controller. Detecting a
        // changed string here means those call sites do not need invasive
        // rewrites just to drive a notification timer.
        if (v58LastStatusToastText != status)
        {
            v58LastStatusToastText =
                status ?? "";

            v58StatusToastVisibleUntil =
                Time.unscaledTime + 3.5f;
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Time.unscaledTime <=
                v58StatusToastVisibleUntil)
        {
            float toastWidth =
                Mathf.Min(
                    panel.width - 36f,
                    620f
                );

            Rect statusPanel =
                new Rect(
                    panel.x +
                        (panel.width -
                         toastWidth) *
                        0.5f,
                    panel.y +
                        panel.height -
                        64f,
                    toastWidth,
                    46f
                );

            DrawTintedBox(
                statusPanel,
                new Color(
                    0.055f,
                    0.06f,
                    0.075f,
                    0.94f
                )
            );

            GUIStyle statusStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            statusStyle.wordWrap = true;
            statusStyle.alignment =
                TextAnchor.MiddleCenter;
            statusStyle.fontSize = 12;
            statusStyle.fontStyle =
                FontStyle.Bold;

            GUI.Label(
                new Rect(
                    statusPanel.x + 12f,
                    statusPanel.y + 5f,
                    statusPanel.width - 24f,
                    statusPanel.height - 10f
                ),
                status,
                statusStyle
            );
        }
'@

Replace-ExactSingle `
    "Assets\Scripts\Core\GameController.UI.cs" `
    $oldStatusPanel `
    $newStatusPanel `
    "Replace permanent deployment status bar with 3.5-second toast" `
    "WARBOARD_V58_TRANSIENT_STATUS_TOAST"

# ---------------------------------------------------------------------------
# 4) Visible build identity.
# ---------------------------------------------------------------------------

$buildFile = "Assets\Scripts\Core\WarboardBuildInfo.cs"
$buildText = Read-Normalized $buildFile

if ($buildText.Contains('CurrentVersion = "v58"')) {
    Write-Host "[SKIP] Visible build identity already v58." -ForegroundColor Yellow
}
elseif ($buildText.Contains('CurrentVersion = "v57"')) {
    $buildText = $buildText.Replace(
        'CurrentVersion = "v57"',
        'CurrentVersion = "v58"'
    )
    Write-Normalized $buildFile $buildText
    Write-Host "[OK]   Visible build identity -> v58" -ForegroundColor Green
}
else {
    throw "WarboardBuildInfo.cs is not v57 or v58. Refusing to guess the base version."
}

# ---------------------------------------------------------------------------
# Verification.
# ---------------------------------------------------------------------------

$checks = @(
    @("Assets\Scripts\Core\GameController.V55MissionCards.cs", "WARBOARD_V58_SECONDARY_CARD_TEXT"),
    @("Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs", "WARBOARD_V58_ACTIVE_FACTION_RULES_ONLY"),
    @("Assets\Scripts\Factions\Standard11\StandardFactionSetupUI.cs", "WARBOARD_V58_STANDARD_RULES_ACTIVE_FACTION_GUARD"),
    @("Assets\Scripts\Core\GameController.cs", "WARBOARD_V58_TRANSIENT_STATUS_TOAST_STATE"),
    @("Assets\Scripts\Core\GameController.UI.cs", "WARBOARD_V58_TRANSIENT_STATUS_TOAST"),
    @("Assets\Scripts\Core\WarboardBuildInfo.cs", 'CurrentVersion = "v58"')
)

foreach ($check in $checks) {
    $text = Read-Normalized $check[0]
    if (-not $text.Contains($check[1])) {
        throw "Post-patch verification failed: $($check[0]) is missing $($check[1])"
    }
}

Write-Host ""
Write-Host "Warboard Multiplayer v58 patch complete." -ForegroundColor Green
Write-Host "Original files were backed up to:" -ForegroundColor Green
Write-Host "  $BackupRoot" -ForegroundColor Green
Write-Host ""
Write-Host "Open Unity and let it recompile. If Unity reports a compiler error, copy it exactly and send it back."

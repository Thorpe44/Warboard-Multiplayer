$ErrorActionPreference = "Stop"
# WARBOARD_V54A_TOOLTIP_MATCH_COUNT_FIX
# WARBOARD_V54B_PATCHER_RETURN_FIX

$Root = (Resolve-Path $PSScriptRoot).Path
$Stamp = Get-Date -Format "yyyyMMdd_HHmmss"

$StageRoot =
    Join-Path $Root (
        "Library\WarboardV54Staging_" +
        $Stamp
    )

$BackupRoot =
    Join-Path $Root (
        "Library\WarboardBackups\V54_" +
        $Stamp
    )

$PayloadRoot =
    Join-Path $Root "V54_PAYLOAD"

$existingFiles =
    New-Object System.Collections.Generic.List[string]

$newFiles =
    New-Object System.Collections.Generic.List[string]

$commitStarted = $false

function Normalize-Newlines(
    [string]$Text)
{
    if ($null -eq $Text)
    {
        return ""
    }

    return $Text.Replace(
        "`r`n",
        "`n"
    ).Replace(
        "`r",
        "`n"
    )
}

function Read-Text(
    [string]$Path)
{
    if (-not (Test-Path $Path))
    {
        throw (
            "Expected file is missing: " +
            $Path
        )
    }

    $value =
        [System.IO.File]::ReadAllText(
            $Path
        )

    return Normalize-Newlines $value
}

function Write-Utf8(
    [string]$Path,
    [string]$Text)
{
    $parent =
        Split-Path -Parent $Path

    if (-not (Test-Path $parent))
    {
        New-Item `
            -ItemType Directory `
            -Path $parent `
            -Force |
            Out-Null
    }

    $encoding =
        New-Object System.Text.UTF8Encoding(
            $false
        )

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Text),
        $encoding
    )
}

function Backup-Once(
    [string]$Relative)
{
    $source =
        Join-Path $Root $Relative

    if (-not (Test-Path $source))
    {
        throw (
            "Expected project file is missing: " +
            $Relative
        )
    }

    if ($existingFiles.Contains(
            $Relative))
    {
        return
    }

    $backup =
        Join-Path $BackupRoot $Relative

    $parent =
        Split-Path -Parent $backup

    if (-not (Test-Path $parent))
    {
        New-Item `
            -ItemType Directory `
            -Path $parent `
            -Force |
            Out-Null
    }

    Copy-Item `
        -LiteralPath $source `
        -Destination $backup `
        -Force

    $existingFiles.Add(
        $Relative
    ) | Out-Null
}

function Replace-RegexOnce(
    [string]$Text,
    [string]$Pattern,
    [string]$Replacement,
    [string]$Label,
    [string]$AlreadyMarker = "")
{
    $value =
        Normalize-Newlines $Text

    if (-not [string]::IsNullOrWhiteSpace(
            $AlreadyMarker) -and
        $value.Contains(
            $AlreadyMarker))
    {
        Write-Host (
            "[OK] " +
            $Label +
            " already present."
        ) -ForegroundColor DarkGreen

        return $value
    }

    $regex =
        New-Object System.Text.RegularExpressions.Regex(
            $Pattern,
            [System.Text.RegularExpressions.RegexOptions]::Singleline
        )

    $matches =
        $regex.Matches(
            $value
        )

    if ($matches.Count -ne 1)
    {
        throw (
            "Expected exactly one match for " +
            $Label +
            "; found " +
            $matches.Count
        )
    }

    Write-Host (
        "[FIXED] " +
        $Label
    ) -ForegroundColor Green

    return $regex.Replace(
        $value,
        $Replacement,
        1
    )
}

function Replace-LiteralAllExpected(
    [string]$Text,
    [string]$Old,
    [string]$New,
    [int]$Expected,
    [string]$Label)
{
    $value =
        Normalize-Newlines $Text

    $oldValue =
        Normalize-Newlines $Old

    $newValue =
        Normalize-Newlines $New

    $count = 0
    $start = 0

    while ($true)
    {
        $index =
            $value.IndexOf(
                $oldValue,
                $start,
                [System.StringComparison]::Ordinal
            )

        if ($index -lt 0)
        {
            break
        }

        $count++
        $start =
            $index +
            $oldValue.Length
    }

    if ($count -ne $Expected)
    {
        throw (
            "Expected " +
            $Expected +
            " match(es) for " +
            $Label +
            "; found " +
            $count
        )
    }

    Write-Host (
        "[FIXED] " +
        $Label
    ) -ForegroundColor Green

    return $value.Replace(
        $oldValue,
        $newValue
    )
}

function Stage-Patch(
    [string]$Relative,
    [scriptblock]$Patcher)
{
    Backup-Once $Relative

    $source =
        Join-Path $Root $Relative

    $text =
        Read-Text $source

    $patched =
        & $Patcher $text

    if ($null -eq $patched)
    {
        throw (
            "Patcher returned null for: " +
            $Relative
        )
    }

    $stage =
        Join-Path $StageRoot $Relative

    Write-Utf8 `
        $stage `
        ([string]$patched)
}

function Stage-New(
    [string]$Relative)
{
    $payload =
        Join-Path $PayloadRoot $Relative

    if (-not (Test-Path $payload))
    {
        throw (
            "Missing v54 payload file: " +
            $Relative
        )
    }

    $target =
        Join-Path $Root $Relative

    if (Test-Path $target)
    {
        Backup-Once $Relative
    }
    else
    {
        $newFiles.Add(
            $Relative
        ) | Out-Null
    }

    $stage =
        Join-Path $StageRoot $Relative

    Write-Utf8 `
        $stage `
        (Read-Text $payload)
}

function Require-Marker(
    [string]$Relative,
    [string]$Marker)
{
    $stage =
        Join-Path $StageRoot $Relative

    $text =
        Read-Text $stage

    if (-not $text.Contains(
            $Marker))
    {
        throw (
            "Validation marker missing in " +
            $Relative +
            ": " +
            $Marker
        )
    }
}

function Restore-Project()
{
    foreach ($relative
        in $existingFiles)
    {
        $backup =
            Join-Path $BackupRoot $relative

        $target =
            Join-Path $Root $relative

        if (Test-Path $backup)
        {
            Copy-Item `
                -LiteralPath $backup `
                -Destination $target `
                -Force
        }
    }

    foreach ($relative
        in $newFiles)
    {
        $target =
            Join-Path $Root $relative

        if (Test-Path $target)
        {
            Remove-Item `
                -LiteralPath $target `
                -Force
        }
    }
}

try
{
    Write-Host (
        "WARBOARD v54b PATCHER RETURN FIX"
    ) -ForegroundColor Cyan

    Write-Host (
        "Target: Thorpe44/Warboard-Multiplayer"
    ) -ForegroundColor Cyan

    Write-Host ""

    $required = @(
        "Assets\Scripts\Core\GameController.cs",
        "Assets\Scripts\Core\GameController.UI.cs",
        "Assets\Scripts\Core\GameController.Fight11.cs",
        "Assets\Scripts\Core\GameController.CustodesFaction11.cs",
        "Assets\Scripts\Core\GameController.AeldariFaction11.cs",
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs",
        "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs",
        "Assets\Scripts\Core\GameController.V45Presentation.cs",
        "Assets\Scripts\Core\WarboardBuildInfo.cs"
    )

    foreach ($relative
        in $required)
    {
        if (-not (
            Test-Path (
                Join-Path $Root $relative
            )))
        {
            throw (
                "Not the Warboard-Multiplayer project root; missing " +
                $relative
            )
        }
    }

    New-Item `
        -ItemType Directory `
        -Path $StageRoot `
        -Force |
        Out-Null

    New-Item `
        -ItemType Directory `
        -Path $BackupRoot `
        -Force |
        Out-Null

    $self =
        Replace-RegexOnce `
            -Text "alpha`r`nbeta" `
            -Pattern 'alpha\s+beta' `
            -Replacement "ALPHA`nBETA" `
            -Label "installer newline self-test"

    if ($self -ne "ALPHA`nBETA")
    {
        throw (
            "v54 patch-engine self-test failed."
        )
    }

    Write-Host (
        "[OK] v54 patch engine self-test passed."
    ) -ForegroundColor Cyan

    Write-Host ""

    # GameController update hooks.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.cs" `
        {
            param($text)

            $updateReplacement = @'
private void Update()
    {
        // WARBOARD_V54_GHOST_UPDATE_HOOK
        V54UpdatePlacementGhost();

        Custodes11PumpDeferredReactions();
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'private void Update\(\)\s*\{\s*Custodes11PumpDeferredReactions\(\);' `
                    -Replacement $updateReplacement `
                    -Label "placement ghost update hook" `
                    -AlreadyMarker "WARBOARD_V54_GHOST_UPDATE_HOOK"

            $destroyReplacement = @'
private void OnDestroy()
    {
        // WARBOARD_V54_GHOST_DESTROY_HOOK
        V54ClearPlacementGhosts();

        Core11Uninstall();
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'private void OnDestroy\(\)\s*\{\s*Core11Uninstall\(\);' `
                    -Replacement $destroyReplacement `
                    -Label "placement ghost cleanup hook" `
                    -AlreadyMarker "WARBOARD_V54_GHOST_DESTROY_HOOK"

            return $text
        }

    # Current HUD/fight/mission/Stratagem fixes.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.UI.cs" `
        {
            param($text)

            $fightReplacement = @'
// WARBOARD_V45_5_MERGED_CONTEXT_BAR
        // WARBOARD_V54_FIGHT_CONTEXT_VISIBLE
        V54DrawFightControls();
        DrawV45SelectedUnitCard();
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern '// WARBOARD_V45_5_MERGED_CONTEXT_BAR\s*DrawV45SelectedUnitCard\(\);' `
                    -Replacement $fightReplacement `
                    -Label "visible Fight/Pile-In controls" `
                    -AlreadyMarker "WARBOARD_V54_FIGHT_CONTEXT_VISIBLE"

            if (-not $text.Contains(
                    "WARBOARD_V54_LARGE_STRATAGEM_RULE_BOX"))
            {
                $oldTooltip = @'
        GUI.Label(
            new Rect(
                panel.x + 18f,
                panel.y + height - 94f,
                panel.width - 36f,
                24f
            ),
            "HOVER A STRATAGEM FOR ITS RULE",
            section
        );

        DrawCurrentTooltip(
            new Rect(
                panel.x + 18f,
                panel.y + height - 66f,
                panel.width - 36f,
                52f
            )
        );
'@

                $newTooltip = @'
        // WARBOARD_V54_LARGE_STRATAGEM_RULE_BOX
        GUI.Label(
            new Rect(
                panel.x + 18f,
                panel.y + height - 192f,
                panel.width - 36f,
                24f
            ),
            "HOVER A STRATAGEM FOR ITS RULE",
            section
        );

        DrawCurrentTooltip(
            new Rect(
                panel.x + 18f,
                panel.y + height - 164f,
                panel.width - 36f,
                150f
            )
        );
'@

                $tooltipCount = 0
                $tooltipStart = 0

                while ($true)
                {
                    $tooltipIndex =
                        $text.IndexOf(
                            (Normalize-Newlines $oldTooltip),
                            $tooltipStart,
                            [System.StringComparison]::Ordinal
                        )

                    if ($tooltipIndex -lt 0)
                    {
                        break
                    }

                    $tooltipCount++
                    $tooltipStart =
                        $tooltipIndex +
                        (Normalize-Newlines $oldTooltip).Length
                }

                if ($tooltipCount -lt 1)
                {
                    throw (
                        "Expected at least one match for larger Stratagem hover rule text; found 0"
                    )
                }

                $text =
                    $text.Replace(
                        (Normalize-Newlines $oldTooltip),
                        (Normalize-Newlines $newTooltip)
                    )

                Write-Host (
                    "[FIXED] larger Stratagem hover rule text (" +
                    $tooltipCount +
                    " block(s))"
                ) -ForegroundColor Green
            }

            $factionReplacement = @'
// WARBOARD_V54_FACTION_RULE_SUMMARY
                V54FactionRuleSummary(faction)
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern '"Faction rules: "\s*\+\s*\(factionRules != null\s*\? factionRules\.RuleSummary\(\s*faction\s*\)\s*:\s*"Generic Core"\)' `
                    -Replacement $factionReplacement `
                    -Label "mission-setup faction rule summary" `
                    -AlreadyMarker "WARBOARD_V54_FACTION_RULE_SUMMARY"

            if (-not $text.Contains(
                    "WARBOARD_V54_BRIGHT_MISSION_PREVIEW"))
            {
                $previewHead = @'
    private void DrawMissionBattlefieldPreview(
        Rect rect,
        MissionBattlefieldDefinition definition)
    {
'@

                $previewNew = @'
    private void DrawMissionBattlefieldPreview(
        Rect rect,
        MissionBattlefieldDefinition definition)
    {
        // WARBOARD_V54_BRIGHT_MISSION_PREVIEW
'@

                $text =
                    Replace-LiteralAllExpected `
                        -Text $text `
                        -Old $previewHead `
                        -New $previewNew `
                        -Expected 1 `
                        -Label "mission preview marker"

                $oldPreviewBackground = @'
                0.035f,
                0.04f,
                0.052f,
'@

                $newPreviewBackground = @'
                0.10f,
                0.12f,
                0.15f,
'@

                $text =
                    Replace-LiteralAllExpected `
                        -Text $text `
                        -Old $oldPreviewBackground `
                        -New $newPreviewBackground `
                        -Expected 1 `
                        -Label "mission preview background"

                $oldBoard = @'
                0.28f,
                0.31f,
                0.30f,
'@

                $newBoard = @'
                0.43f,
                0.47f,
                0.45f,
'@

                $text =
                    Replace-LiteralAllExpected `
                        -Text $text `
                        -Old $oldBoard `
                        -New $newBoard `
                        -Expected 1 `
                        -Label "mission preview board"

                $oldBlocking = @'
                    0.10f,
                    0.11f,
                    0.12f,
'@

                $newBlocking = @'
                    0.28f,
                    0.30f,
                    0.34f,
'@

                $text =
                    Replace-LiteralAllExpected `
                        -Text $text `
                        -Old $oldBlocking `
                        -New $newBlocking `
                        -Expected 1 `
                        -Label "mission preview blocking terrain"

                $oldOtherTerrain = @'
                    0.20f,
                    0.24f,
                    0.20f,
'@

                $newOtherTerrain = @'
                    0.36f,
                    0.44f,
                    0.35f,
'@

                $text =
                    Replace-LiteralAllExpected `
                        -Text $text `
                        -Old $oldOtherTerrain `
                        -New $newOtherTerrain `
                        -Expected 1 `
                        -Label "mission preview other terrain"

                $oldObjectiveRect = @'
                    centre.x - 4f,
                    centre.y - 4f,
                    8f,
                    8f
'@

                $newObjectiveRect = @'
                    centre.x - 5f,
                    centre.y - 5f,
                    10f,
                    10f
'@

                $text =
                    Replace-LiteralAllExpected `
                        -Text $text `
                        -Old $oldObjectiveRect `
                        -New $newObjectiveRect `
                        -Expected 1 `
                        -Label "mission preview objectives"
            }

            return $text
        }

    # Fight controls are at y=84..128.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.V45Presentation.cs" `
        {
            param($text)

            $replacement = @'
// WARBOARD_V54_FIGHT_CARD_OFFSET
        Rect card =
            new Rect(
                12f,
                phase == Phase.Fight
                ? 134f
                : 82f,
'@

            $patchedText =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'Rect card\s*=\s*new Rect\(\s*12f,\s*82f,' `
                    -Replacement $replacement `
                    -Label "Fight-phase selected-card offset" `
                    -AlreadyMarker "WARBOARD_V54_FIGHT_CARD_OFFSET"

            return $patchedText
        }

    # Traditional mode should not auto-interrupt with inferred faction reactions.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.CustodesFaction11.cs" `
        {
            param($text)

            $offerReplacement = @'
public void Custodes11OfferEventRules(
        CustodesGameController faction,
        GameEventContext context)
    {
        // WARBOARD_V54_TRADITIONAL_NO_CUSTODES_REACTION_POPUPS
        if (!IsXcomMode)
            return;
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'public void Custodes11OfferEventRules\(\s*CustodesGameController faction,\s*GameEventContext context\)\s*\{' `
                    -Replacement $offerReplacement `
                    -Label "Traditional Custodes reaction suppression" `
                    -AlreadyMarker "WARBOARD_V54_TRADITIONAL_NO_CUSTODES_REACTION_POPUPS"

            $pumpReplacement = @'
public void Custodes11PumpDeferredReactions()
    {
        // WARBOARD_V54_TRADITIONAL_CLEAR_CUSTODES_REACTIONS
        if (!IsXcomMode)
        {
            custodes11DeferredReactions.Clear();
            return;
        }
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'public void Custodes11PumpDeferredReactions\(\)\s*\{' `
                    -Replacement $pumpReplacement `
                    -Label "Traditional Custodes deferred-reaction clear" `
                    -AlreadyMarker "WARBOARD_V54_TRADITIONAL_CLEAR_CUSTODES_REACTIONS"

            return $text
        }

    Stage-Patch `
        "Assets\Scripts\Core\GameController.AeldariFaction11.cs" `
        {
            param($text)

            $offerReplacement = @'
public void Aeldari11OfferEventRules(AeldariGameController faction, GameEventContext context)
    {
        // WARBOARD_V54_TRADITIONAL_NO_AELDARI_REACTION_POPUPS
        if (!IsXcomMode)
            return;
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'public void Aeldari11OfferEventRules\(AeldariGameController faction, GameEventContext context\)\s*\{' `
                    -Replacement $offerReplacement `
                    -Label "Traditional Aeldari reaction suppression" `
                    -AlreadyMarker "WARBOARD_V54_TRADITIONAL_NO_AELDARI_REACTION_POPUPS"

            $pumpReplacement = @'
public void Aeldari11PumpDeferredReactions()
    {
        // WARBOARD_V54_TRADITIONAL_CLEAR_AELDARI_REACTIONS
        if (!IsXcomMode)
        {
            aeldari11DeferredReactions.Clear();
            return;
        }
'@

            $text =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'public void Aeldari11PumpDeferredReactions\(\)\s*\{' `
                    -Replacement $pumpReplacement `
                    -Label "Traditional Aeldari deferred-reaction clear" `
                    -AlreadyMarker "WARBOARD_V54_TRADITIONAL_CLEAR_AELDARI_REACTIONS"

            return $text
        }

    # Core Fire Overwatch reaction is manual-only in Traditional.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs" `
        {
            param($text)

            $replacement = @'
private bool V48OpenFireOverwatchWindow()
    {
        // WARBOARD_V54_TRADITIONAL_NO_OVERWATCH_POPUP
        if (!IsXcomMode)
        {
            v48EndMoveOverwatchResolved = true;
            return false;
        }
'@

            $patchedText =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'private bool V48OpenFireOverwatchWindow\(\)\s*\{' `
                    -Replacement $replacement `
                    -Label "Traditional Fire Overwatch popup suppression" `
                    -AlreadyMarker "WARBOARD_V54_TRADITIONAL_NO_OVERWATCH_POPUP"

            return $patchedText
        }

    # Trigger click surfaces are not physical scenery.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs" `
        {
            param($text)

            $replacement = @'
foreach (Collider col in overlaps)
        {
            if (col == null)
                continue;

            // WARBOARD_V54_OBJECTIVE_TRIGGER_NOT_SOLID
            // Click/area triggers stay queryable but never block a model base.
            if (col.isTrigger)
                continue;
'@

            $patchedText =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'foreach \(Collider col in overlaps\)\s*\{\s*if \(col == null\)\s*continue;' `
                    -Replacement $replacement `
                    -Label "objective/terrain trigger placement legality" `
                    -AlreadyMarker "WARBOARD_V54_OBJECTIVE_TRIGGER_NOT_SOLID"

            return $patchedText
        }

    # New source files.
    Stage-New `
        "Assets\Scripts\Core\ModelToken.V54PlacementGhost.cs"

    Stage-New `
        "Assets\Scripts\Core\GameController.V54PlacementGhost.cs"

    Stage-New `
        "Assets\Scripts\Core\GameController.V54UIHelpers.cs"

    # Visible build identity.
    Stage-Patch `
        "Assets\Scripts\Core\WarboardBuildInfo.cs" `
        {
            param($text)

            $patchedText =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'public const string CurrentVersion\s*=\s*"v[^"]+";' `
                    -Replacement 'public const string CurrentVersion = "v54";' `
                    -Label "build identity v54" `
                    -AlreadyMarker 'CurrentVersion = "v54";'

            return $patchedText
        }

    # Validate staged output.
    Require-Marker `
        "Assets\Scripts\Core\GameController.cs" `
        "WARBOARD_V54_GHOST_UPDATE_HOOK"

    Require-Marker `
        "Assets\Scripts\Core\GameController.UI.cs" `
        "WARBOARD_V54_FIGHT_CONTEXT_VISIBLE"

    Require-Marker `
        "Assets\Scripts\Core\GameController.UI.cs" `
        "WARBOARD_V54_LARGE_STRATAGEM_RULE_BOX"

    Require-Marker `
        "Assets\Scripts\Core\GameController.UI.cs" `
        "WARBOARD_V54_FACTION_RULE_SUMMARY"

    Require-Marker `
        "Assets\Scripts\Core\GameController.UI.cs" `
        "WARBOARD_V54_BRIGHT_MISSION_PREVIEW"

    Require-Marker `
        "Assets\Scripts\Core\GameController.CustodesFaction11.cs" `
        "WARBOARD_V54_TRADITIONAL_NO_CUSTODES_REACTION_POPUPS"

    Require-Marker `
        "Assets\Scripts\Core\GameController.AeldariFaction11.cs" `
        "WARBOARD_V54_TRADITIONAL_NO_AELDARI_REACTION_POPUPS"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs" `
        "WARBOARD_V54_TRADITIONAL_NO_OVERWATCH_POPUP"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V53SolidSceneryPlacement.cs" `
        "WARBOARD_V54_OBJECTIVE_TRIGGER_NOT_SOLID"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V54PlacementGhost.cs" `
        "WARBOARD_V54_PLACEMENT_GHOST_SYSTEM"

    Require-Marker `
        "Assets\Scripts\Core\ModelToken.V54PlacementGhost.cs" `
        "WARBOARD_V54_PLACEMENT_GHOST_VISUAL"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V54UIHelpers.cs" `
        "WARBOARD_V54_UI_HELPERS"

    Require-Marker `
        "Assets\Scripts\Core\WarboardBuildInfo.cs" `
        'CurrentVersion = "v54";'

    Write-Host ""
    Write-Host (
        "All v54 transforms validated in staging. Committing..."
    ) -ForegroundColor Cyan

    $commitStarted = $true

    foreach ($relative
        in $existingFiles)
    {
        $stage =
            Join-Path $StageRoot $relative

        if (-not (Test-Path $stage))
        {
            throw (
                "Staged existing file missing: " +
                $relative
            )
        }

        $target =
            Join-Path $Root $relative

        Copy-Item `
            -LiteralPath $stage `
            -Destination $target `
            -Force
    }

    foreach ($relative
        in $newFiles)
    {
        $stage =
            Join-Path $StageRoot $relative

        if (-not (Test-Path $stage))
        {
            throw (
                "Staged new file missing: " +
                $relative
            )
        }

        $target =
            Join-Path $Root $relative

        Copy-Item `
            -LiteralPath $stage `
            -Destination $target `
            -Force
    }

    $commitStarted = $false

    if (Test-Path $StageRoot)
    {
        Remove-Item `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force
    }

    Write-Host ""
    Write-Host (
        "WARBOARD v54 INSTALL COMPLETE"
    ) -ForegroundColor Green

    Write-Host (
        "Backup: " +
        $BackupRoot
    ) -ForegroundColor DarkGray

    exit 0
}
catch
{
    Write-Host ""
    Write-Host (
        "WARBOARD v54 INSTALL FAILED"
    ) -ForegroundColor Red

    Write-Host (
        "---------------------------"
    ) -ForegroundColor Red

    Write-Host (
        $_.Exception.Message
    ) -ForegroundColor Red

    Write-Host ""

    if ($commitStarted)
    {
        Write-Host (
            "Rolling project files back from v54 backup..."
        ) -ForegroundColor Yellow

        try
        {
            Restore-Project

            Write-Host (
                "Rollback complete."
            ) -ForegroundColor Yellow
        }
        catch
        {
            Write-Host (
                "ROLLBACK ERROR: " +
                $_.Exception.Message
            ) -ForegroundColor Red
        }
    }
    else
    {
        Write-Host (
            "Project source was not committed; failure occurred during staging/validation."
        ) -ForegroundColor Yellow
    }

    if (Test-Path $StageRoot)
    {
        Remove-Item `
            -LiteralPath $StageRoot `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }

    exit 1
}

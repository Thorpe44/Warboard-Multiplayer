$ErrorActionPreference = "Stop"

$Root = (Resolve-Path $PSScriptRoot).Path
$Stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$StageRoot = Join-Path $Root ("Library\WarboardV51Staging_" + $Stamp)
$BackupRoot = Join-Path $Root ("Library\WarboardBackups\V51_" + $Stamp)

$commitStarted = $false
$changedFiles = New-Object System.Collections.Generic.List[string]

function Write-Utf8NoBom([string]$Path, [string]$Text)
{
    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent) -and
        -not (Test-Path $parent))
    {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

function Read-Text([string]$Path)
{
    if (-not (Test-Path $Path))
    {
        throw ("Expected project file is missing: " + $Path)
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Relative-StagePath([string]$Relative)
{
    return Join-Path $StageRoot $Relative
}

function Backup-File([string]$Relative)
{
    $source = Join-Path $Root $Relative

    if (-not (Test-Path $source))
    {
        throw ("Expected project file is missing: " + $Relative)
    }

    $backup = Join-Path $BackupRoot $Relative
    $parent = Split-Path -Parent $backup

    if (-not (Test-Path $parent))
    {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    Copy-Item -LiteralPath $source -Destination $backup -Force
}

function Replace-ExactOnce(
    [string]$Text,
    [string]$Old,
    [string]$New,
    [string]$Label,
    [string]$AlreadyMarker = "")
{
    if (-not [string]::IsNullOrWhiteSpace($AlreadyMarker) -and
        $Text.Contains($AlreadyMarker))
    {
        Write-Host ("[OK] " + $Label + " already present.") -ForegroundColor DarkGreen
        return $Text
    }

    $first = $Text.IndexOf($Old, [System.StringComparison]::Ordinal)

    if ($first -lt 0)
    {
        throw ("Could not find anchor for " + $Label)
    }

    $second = $Text.IndexOf(
        $Old,
        $first + $Old.Length,
        [System.StringComparison]::Ordinal
    )

    if ($second -ge 0)
    {
        throw ("Anchor was ambiguous for " + $Label)
    }

    Write-Host ("[FIXED] " + $Label) -ForegroundColor Green

    $result =
        $Text.Substring(0, $first) +
        $New +
        $Text.Substring($first + $Old.Length)

    return $result
}

function Replace-RegexOnce(
    [string]$Text,
    [string]$Pattern,
    [string]$Replacement,
    [string]$Label,
    [string]$AlreadyMarker = "")
{
    if (-not [string]::IsNullOrWhiteSpace($AlreadyMarker) -and
        $Text.Contains($AlreadyMarker))
    {
        Write-Host ("[OK] " + $Label + " already present.") -ForegroundColor DarkGreen
        return $Text
    }

    $regex = New-Object System.Text.RegularExpressions.Regex(
        $Pattern,
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    $matches = $regex.Matches($Text)

    if ($matches.Count -ne 1)
    {
        throw (
            "Expected exactly one regex match for " +
            $Label +
            "; found " +
            $matches.Count
        )
    }

    Write-Host ("[FIXED] " + $Label) -ForegroundColor Green
    return $regex.Replace($Text, $Replacement, 1)
}

function Stage-PatchedFile(
    [string]$Relative,
    [scriptblock]$Patcher)
{
    $source = Join-Path $Root $Relative
    Backup-File $Relative

    $text = Read-Text $source
    $patched = & $Patcher $text

    if ($null -eq $patched)
    {
        throw ("Patcher returned null for " + $Relative)
    }

    $staged = Relative-StagePath $Relative
    Write-Utf8NoBom $staged ([string]$patched)
    $changedFiles.Add($Relative) | Out-Null
}

function Require-Marker(
    [string]$Relative,
    [string]$Marker)
{
    $staged = Relative-StagePath $Relative
    $text = Read-Text $staged

    if (-not $text.Contains($Marker))
    {
        throw (
            "Validation marker missing in " +
            $Relative +
            ": " +
            $Marker
        )
    }
}

function Restore-Backups()
{
    foreach ($relative in $changedFiles)
    {
        $backup = Join-Path $BackupRoot $relative
        $target = Join-Path $Root $relative

        if (Test-Path $backup)
        {
            $parent = Split-Path -Parent $target
            if (-not (Test-Path $parent))
            {
                New-Item -ItemType Directory -Path $parent -Force | Out-Null
            }

            Copy-Item -LiteralPath $backup -Destination $target -Force
        }
    }
}

try
{
    Write-Host "WARBOARD v51 GAMEPLAY / UI BUGFIXES" -ForegroundColor Cyan
    Write-Host "Repository target: Thorpe44/Warboard-Multiplayer" -ForegroundColor Cyan
    Write-Host ""

    $requiredRootFiles = @(
        "Assets\Scripts\Core\GameController.UI.cs",
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs",
        "Assets\Scripts\Core\YellowScribeImporter.cs",
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesGameController.cs"
    )

    foreach ($relative in $requiredRootFiles)
    {
        if (-not (Test-Path (Join-Path $Root $relative)))
        {
            throw (
                "This does not look like the Warboard-Multiplayer project root. Missing: " +
                $relative
            )
        }
    }

    if (Test-Path $StageRoot)
    {
        Remove-Item -LiteralPath $StageRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Path $StageRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $BackupRoot -Force | Out-Null

    # Installer self-tests.
    $selfExact = Replace-ExactOnce `
        -Text "alpha beta gamma" `
        -Old "beta" `
        -New "BETA" `
        -Label "installer exact self-test"

    if ($selfExact -ne "alpha BETA gamma")
    {
        throw "Installer exact replacement self-test failed."
    }

    $selfRegex = Replace-RegexOnce `
        -Text "one   two" `
        -Pattern 'one\s+two' `
        -Replacement "ONE TWO" `
        -Label "installer regex self-test"

    if ($selfRegex -ne "ONE TWO")
    {
        throw "Installer regex replacement self-test failed."
    }

    Write-Host "[OK] v51 patch engine self-test passed." -ForegroundColor Cyan
    Write-Host ""

    # ---------------------------------------------------------
    # 1. Traditional charge soft-lock + Mission Info tab.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Core\GameController.UI.cs" `
        {
            param($text)

            $oldChargeCondition = @'
        if (traditionalChargePending &&
            traditionalChargeAttacker != null &&
            traditionalChargeTarget != null)
'@

            $newChargeCondition = @'
        // WARBOARD_V51_TRADITIONAL_CHARGE_PROMPT
        if (traditionalChargePending &&
            traditionalChargeAttacker != null)
'@

            $text = Replace-ExactOnce `
                -Text $text `
                -Old $oldChargeCondition `
                -New $newChargeCondition `
                -Label "failed-charge Traditional prompt" `
                -AlreadyMarker "WARBOARD_V51_TRADITIONAL_CHARGE_PROMPT"

            $oldChargeLabel = @'
                traditionalChargeAttacker.DisplayName +
                "  ->  " +
                traditionalChargeTarget.DisplayName +
                "\nRoll 2D6 yourself and apply any tabletop rerolls yourself. Enter only the final total.",
'@

            $newChargeLabel = @'
                traditionalChargeAttacker.DisplayName +
                (traditionalChargeTarget != null
                    ? "  ->  " +
                      traditionalChargeTarget.DisplayName
                    : "  |  choose target(s) after this roll") +
                "\nRoll 2D6 yourself and apply any tabletop rerolls yourself. Enter only the final total.",
'@

            if (-not $text.Contains("choose target(s) after this roll"))
            {
                $text = Replace-ExactOnce `
                    -Text $text `
                    -Old $oldChargeLabel `
                    -New $newChargeLabel `
                    -Label "null-safe v48 charge label"
            }

            $missionPattern =
                '"MISSION"\)\)\s*\{\s*showMissionPanel\s*=\s*!showMissionPanel;'

            $missionReplacement = @'
"MISSION INFO"))
        {
            // WARBOARD_V51_MISSION_INFO_TAB
            showMissionPanel =
                !showMissionPanel;
'@

            $text = Replace-RegexOnce `
                -Text $text `
                -Pattern $missionPattern `
                -Replacement $missionReplacement `
                -Label "Mission Info top-bar tab" `
                -AlreadyMarker "WARBOARD_V51_MISSION_INFO_TAB"

            if (-not $text.Contains("MISSION INFO / RULES  -  ROUND "))
            {
                $text = Replace-ExactOnce `
                    -Text $text `
                    -Old '"MISSION  -  ROUND " +' `
                    -New '"MISSION INFO / RULES  -  ROUND " +' `
                    -Label "Mission rules panel heading"
            }

            return $text
        }

    # ---------------------------------------------------------
    # 2. Clicked attached-model identity (Yvraine etc.).
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Core\GameController.V45Presentation.cs" `
        {
            param($text)

            if (-not $text.Contains("WARBOARD_V51_CLICKED_MODEL_IDENTITY"))
            {
                $anchor = @'
        float width =
'@

                $insertion = @'
        // WARBOARD_V51_CLICKED_MODEL_IDENTITY
        // Keep gameplay actions on the joined unit, but show the actual
        // physical model/datasheet identity the player clicked.
        SquadController cardSquad =
            selectedModel != null &&
            selectedModel.Squad != null
            ? selectedModel.Squad
            : selectedSquad;

        float width =
'@

                $text = Replace-ExactOnce `
                    -Text $text `
                    -Old $anchor `
                    -New $insertion `
                    -Label "clicked-model selected card identity"
            }

            $oldTitle = @'
            selectedSquad.DisplayName,
'@

            if (-not $text.Contains("            cardSquad.DisplayName,"))
            {
                $newTitle = @'
            cardSquad.DisplayName,
'@

                $text = Replace-ExactOnce `
                    -Text $text `
                    -Old $oldTitle `
                    -New $newTitle `
                    -Label "selected card title identity"
            }

            $oldModels = @'
        string modelText =
            selectedSquad.LivingModels +
            "/" +
            selectedSquad.StartingModels +
            " MODELS";
'@

            $newModels = @'
        string modelText =
            cardSquad.LivingModels +
            "/" +
            cardSquad.StartingModels +
            " MODELS" +
            (cardSquad != selectedSquad
                ? " | JOINED " +
                  selectedSquad.LivingModels +
                  "/" +
                  selectedSquad.StartingModels
                : "");
'@

            if (-not $text.Contains('" | JOINED " +'))
            {
                $text = Replace-ExactOnce `
                    -Text $text `
                    -Old $oldModels `
                    -New $newModels `
                    -Label "attached-model count display"
            }

            $text = $text.Replace(
                "            selectedSquad.GetMove()`r`n",
                "            cardSquad.GetMove()`r`n"
            )

            $text = $text.Replace(
                "            selectedSquad.GetMove()`n",
                "            cardSquad.GetMove()`n"
            )

            $text = $text.Replace(
                "            selectedSquad.Toughness +",
                "            cardSquad.Toughness +"
            )

            $text = $text.Replace(
                "            selectedSquad.BaseSave +",
                "            cardSquad.BaseSave +"
            )

            return $text
        }

    # ---------------------------------------------------------
    # 3. YellowScribe group loadout distribution.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Core\YellowScribeImporter.cs" `
        {
            param($text)

            $oldCopies = @'
                    int copies =
                        Mathf.Max(
                            1,
                            IntValue(
                                Get(
                                    weaponEntry,
                                    "number"
                                ),
                                count
                            )
                        );
'@

            $newCopies = @'
                    // WARBOARD_V51_GROUP_LOADOUT_DISTRIBUTION
                    // YellowScribe's weapon quantity is per model in this
                    // model-profile group, not a single aggregate copy for
                    // the entire group.
                    int copiesPerModel =
                        Mathf.Max(
                            1,
                            IntValue(
                                Get(
                                    weaponEntry,
                                    "number"
                                ),
                                1
                            )
                        );

                    int copies =
                        copiesPerModel *
                        count;
'@

            $text = Replace-ExactOnce `
                -Text $text `
                -Old $oldCopies `
                -New $newCopies `
                -Label "per-model YellowScribe weapon quantities" `
                -AlreadyMarker "WARBOARD_V51_GROUP_LOADOUT_DISTRIBUTION"

            $oldComment = @'
                    // YellowScribe's model group gives a count plus aggregate
                    // weapon copy counts. Round-robin distribution gives the
                    // expected result for the common case:
                    // 5 models + 5 rifles => one rifle each;
                    // 1 model + 2 guns => both guns on that model.
'@

            $newComment = @'
                    // Expand the per-model quantity by the model-group count,
                    // then round-robin it across those models. This preserves
                    // multiple weapons per model while ensuring common
                    // wargear appears on every model in the profile group.
'@

            if ($text.Contains($oldComment))
            {
                $text = $text.Replace(
                    $oldComment,
                    $newComment
                )
            }

            return $text
        }

    # ---------------------------------------------------------
    # 4. Aeldari locked row: Player 1 / Player 2 side-by-side.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Factions\Aeldari\AeldariSetupUI.cs" `
        {
            param($text)

            $oldLayout = @'
            float width =
                Mathf.Min(
                    760f,
                    Screen.width - 24f);

            Rect badge =
                new Rect(
                    Screen.width - width - 12f,
                    48f + index * 36f,
                    width - 74f,
                    30f);
'@

            $newLayout = @'
            // WARBOARD_V51_SIDE_BY_SIDE_FACTION_BADGES
            float badgeMargin = 12f;
            float badgeGap = 8f;
            float badgeSlotWidth =
                Mathf.Max(
                    220f,
                    (Screen.width -
                     badgeMargin * 2f -
                     badgeGap) *
                    0.5f);

            bool badgePlayerTwo =
                (controller.FactionId ?? "")
                    .EndsWith("2");

            float badgeX =
                badgePlayerTwo
                ? badgeMargin +
                  badgeSlotWidth +
                  badgeGap
                : badgeMargin;

            Rect badge =
                new Rect(
                    badgeX,
                    48f,
                    Mathf.Max(
                        146f,
                        badgeSlotWidth - 74f),
                    30f);
'@

            return Replace-ExactOnce `
                -Text $text `
                -Old $oldLayout `
                -New $newLayout `
                -Label "Aeldari side-by-side player row" `
                -AlreadyMarker "WARBOARD_V51_SIDE_BY_SIDE_FACTION_BADGES"
        }

    # ---------------------------------------------------------
    # 5. Custodes locked row: Player 1 / Player 2 side-by-side.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesSetupUI.cs" `
        {
            param($text)

            $oldLayout = @'
            float width =
                Mathf.Min(
                    760f,
                    Screen.width - 24f);

            Rect badge =
                new Rect(
                    Screen.width - width - 12f,
                    48f +
                    (occupiedRows + custodesRow) *
                    36f,
                    width - 74f,
                    30f);
'@

            $newLayout = @'
            // WARBOARD_V51_SIDE_BY_SIDE_CUSTODES_BADGE
            float badgeMargin = 12f;
            float badgeGap = 8f;
            float badgeSlotWidth =
                Mathf.Max(
                    220f,
                    (Screen.width -
                     badgeMargin * 2f -
                     badgeGap) *
                    0.5f);

            bool badgePlayerTwo =
                (controller.FactionId ?? "")
                    .EndsWith("2");

            float badgeX =
                badgePlayerTwo
                ? badgeMargin +
                  badgeSlotWidth +
                  badgeGap
                : badgeMargin;

            Rect badge =
                new Rect(
                    badgeX,
                    48f,
                    badgeSlotWidth,
                    30f);
'@

            return Replace-ExactOnce `
                -Text $text `
                -Old $oldLayout `
                -New $newLayout `
                -Label "Custodes side-by-side player row" `
                -AlreadyMarker "WARBOARD_V51_SIDE_BY_SIDE_CUSTODES_BADGE"
        }

    # ---------------------------------------------------------
    # 6. Lions of the Emperor: make the already-implemented
    #    Against All Odds modifier visible in attack breakdowns.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesFactionPack11.cs" `
        {
            param($text)

            $oldLions = @'
            if (Has(
                    faction,
                    CustodesDetachment
                        .LionsOfTheEmperor) &&
                !attacker.HasKeyword(
                    "vehicle") &&
                !HasOtherFriendlyWithin(
                    game,
                    attacker,
                    6f))
            {
                state.hitRollModifier += 1;
                state.woundRollModifier += 1;
            }
'@

            $newLions = @'
            if (Has(
                    faction,
                    CustodesDetachment
                        .LionsOfTheEmperor) &&
                !attacker.HasKeyword(
                    "vehicle") &&
                !HasOtherFriendlyWithin(
                    game,
                    attacker,
                    6f))
            {
                // WARBOARD_V51_LIONS_AGAINST_ALL_ODDS
                state.hitRollModifier += 1;
                state.woundRollModifier += 1;
                state.notes.Add(
                    "Against All Odds: +1 Hit, +1 Wound"
                );
            }
'@

            return Replace-ExactOnce `
                -Text $text `
                -Old $oldLions `
                -New $newLions `
                -Label "Lions of the Emperor combat visibility" `
                -AlreadyMarker "WARBOARD_V51_LIONS_AGAINST_ALL_ODDS"
        }

    # ---------------------------------------------------------
    # 7. Blade Champion leader/bodyguard compatibility.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Resources\Core\LeaderCompatibilityOverrides.json" `
        {
            param($text)

            if ($text -match '"leaderName"\s*:\s*"Blade Champion"')
            {
                Write-Host "[OK] Blade Champion compatibility already present." -ForegroundColor DarkGreen
                return $text
            }

            $pattern = '\r?\n\s*\]\s*\r?\n\}\s*$'

            $addition = @'
,
    {
      "leaderName": "Blade Champion",
      "bodyguardNames": [
        "Custodian Guard",
        "Custodian Wardens"
      ]
    }
  ]
}
'@

            return Replace-RegexOnce `
                -Text $text `
                -Pattern $pattern `
                -Replacement $addition `
                -Label "Blade Champion leader compatibility"
        }

    # ---------------------------------------------------------
    # 8. Visible build identity.
    # ---------------------------------------------------------
    Stage-PatchedFile `
        "Assets\Scripts\Core\WarboardBuildInfo.cs" `
        {
            param($text)

            if ($text.Contains('CurrentVersion = "v51";'))
            {
                Write-Host "[OK] build identity already v51." -ForegroundColor DarkGreen
                return $text
            }

            return Replace-RegexOnce `
                -Text $text `
                -Pattern 'public const string CurrentVersion\s*=\s*"v[^"]+";' `
                -Replacement 'public const string CurrentVersion = "v51";' `
                -Label "build identity v51"
        }

    # ---------------------------------------------------------
    # Staged validation before touching project source.
    # ---------------------------------------------------------
    Require-Marker `
        "Assets\Scripts\Core\GameController.UI.cs" `
        "WARBOARD_V51_TRADITIONAL_CHARGE_PROMPT"

    Require-Marker `
        "Assets\Scripts\Core\GameController.UI.cs" `
        "WARBOARD_V51_MISSION_INFO_TAB"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V45Presentation.cs" `
        "WARBOARD_V51_CLICKED_MODEL_IDENTITY"

    Require-Marker `
        "Assets\Scripts\Core\YellowScribeImporter.cs" `
        "WARBOARD_V51_GROUP_LOADOUT_DISTRIBUTION"

    Require-Marker `
        "Assets\Scripts\Factions\Aeldari\AeldariSetupUI.cs" `
        "WARBOARD_V51_SIDE_BY_SIDE_FACTION_BADGES"

    Require-Marker `
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesSetupUI.cs" `
        "WARBOARD_V51_SIDE_BY_SIDE_CUSTODES_BADGE"

    Require-Marker `
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesFactionPack11.cs" `
        "WARBOARD_V51_LIONS_AGAINST_ALL_ODDS"

    $leaderStage = Read-Text (
        Relative-StagePath `
            "Assets\Resources\Core\LeaderCompatibilityOverrides.json"
    )

    if ($leaderStage -notmatch '"leaderName"\s*:\s*"Blade Champion"')
    {
        throw "Blade Champion compatibility validation failed."
    }

    $buildStage = Read-Text (
        Relative-StagePath `
            "Assets\Scripts\Core\WarboardBuildInfo.cs"
    )

    if (-not $buildStage.Contains('CurrentVersion = "v51";'))
    {
        throw "v51 build identity validation failed."
    }

    Write-Host ""
    Write-Host "All v51 transforms validated in staging. Committing..." -ForegroundColor Cyan

    $commitStarted = $true

    foreach ($relative in $changedFiles)
    {
        $staged = Relative-StagePath $relative
        $target = Join-Path $Root $relative
        $parent = Split-Path -Parent $target

        if (-not (Test-Path $parent))
        {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }

        Copy-Item -LiteralPath $staged -Destination $target -Force
    }

    $commitStarted = $false

    if (Test-Path $StageRoot)
    {
        Remove-Item -LiteralPath $StageRoot -Recurse -Force
    }

    Write-Host ""
    Write-Host "WARBOARD v51 INSTALL COMPLETE" -ForegroundColor Green
    Write-Host ("Backup: " + $BackupRoot) -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "IMPORTANT: reload/re-import both rosters once so the corrected per-model weapon loadouts are rebuilt." -ForegroundColor Yellow
    exit 0
}
catch
{
    Write-Host ""
    Write-Host "WARBOARD v51 INSTALL FAILED" -ForegroundColor Red
    Write-Host "---------------------------" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""

    if ($commitStarted)
    {
        Write-Host "Rolling committed project files back from the v51 backup..." -ForegroundColor Yellow

        try
        {
            Restore-Backups
            Write-Host "Rollback complete." -ForegroundColor Yellow
        }
        catch
        {
            Write-Host ("ROLLBACK ERROR: " + $_.Exception.Message) -ForegroundColor Red
        }
    }
    else
    {
        Write-Host "Project source was not committed; the failure occurred during staging/validation." -ForegroundColor Yellow
    }

    if (Test-Path $StageRoot)
    {
        Remove-Item -LiteralPath $StageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    exit 1
}

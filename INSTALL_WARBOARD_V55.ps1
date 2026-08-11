$ErrorActionPreference = "Stop"

# WARBOARD_V55_INSTALLER
$Root = (Resolve-Path $PSScriptRoot).Path
$Stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$StageRoot =
    Join-Path $Root (
        "Library\WarboardV55Staging_" +
        $Stamp
    )
$BackupRoot =
    Join-Path $Root (
        "Library\WarboardBackups\V55_" +
        $Stamp
    )
$PayloadRoot =
    Join-Path $Root "V55_PAYLOAD"

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
    if ($existingFiles.Contains(
            $Relative))
    {
        return
    }

    $source =
        Join-Path $Root $Relative

    if (-not (Test-Path $source))
    {
        throw (
            "Expected project file is missing: " +
            $Relative
        )
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

function Replace-LiteralExpected(
    [string]$Text,
    [string]$Old,
    [string]$New,
    [int]$Expected,
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
    $source =
        Join-Path $PayloadRoot $Relative

    if (-not (Test-Path $source))
    {
        throw (
            "Missing v55 payload: " +
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
        (Read-Text $source)
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
        "WARBOARD v55a TERRAIN INSTALLER FIX"
    ) -ForegroundColor Cyan

    Write-Host (
        "Target: Thorpe44/Warboard-Multiplayer"
    ) -ForegroundColor Cyan

    Write-Host ""

    $required = @(
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs",
        "Assets\Scripts\Core\GameController.Combat.cs",
        "Assets\Scripts\Core\GameController.V50TerrainAreaBattlefield.cs",
        "Assets\Scripts\Core\TerrainAreaFootprint50.cs",
        "Assets\Scripts\Core\GameController.V54UIHelpers.cs",
        "Assets\Scripts\Core\GameController.V45Presentation.cs",
        "Assets\Scripts\Factions\Aeldari\AeldariSetupUI.cs",
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesSetupUI.cs",
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
                "Not the current Warboard-Multiplayer root; missing " +
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
            "v55 patch engine self-test failed."
        )
    }

    Write-Host (
        "[OK] v55 patch engine self-test passed."
    ) -ForegroundColor Cyan

    Write-Host ""

    # ---------------------------------------------------------
    # Charge: route confirmation to bounded solver.
    # ---------------------------------------------------------
    Stage-Patch `
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs" `
        {
            param($text)

            $old = @'
        if (!V48SolveChargeMove(
                v48ChargeUnit,
'@

            $new = @'
        // WARBOARD_V55_BOUNDED_CHARGE_CALL
        if (!V55SolveChargeMoveBounded(
                v48ChargeUnit,
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Expected 1 `
                    -Label "bounded charge solver call" `
                    -AlreadyMarker "WARBOARD_V55_BOUNDED_CHARGE_CALL"

            return $patchedText
        }

    # ---------------------------------------------------------
    # Shooting: squad/target -> grouped weapon pool by default.
    # ---------------------------------------------------------
    Stage-Patch `
        "Assets\Scripts\Core\GameController.Combat.cs" `
        {
            param($text)

            $old = @'
        if (selectedModel == null ||
            selectedModel.Squad == null ||
            selectedModel.Squad
                .JoinedActionController() !=
                attacker.JoinedActionController())
        {
            status =
                "SHOOTING: click the specific model that will fire, then click its target.";
            return;
        }

        List<WeaponAttackSelection> eligibleWeapons =
            GetEligibleRangedWeapons(
                attacker,
                target,
                engaged
            )
            .Where(
                selection =>
                    selection != null &&
                    selection.model ==
                        selectedModel
            )
            .ToList();

        if (eligibleWeapons.Count == 0)
        {
            status =
                selectedModel.RoleName +
                " has no unused ranged weapon with legal range/line of sight to " +
                target.DisplayName +
                ".";
            return;
        }

        OpenModelWeaponChoice(
            attacker,
            selectedModel,
            target,
            eligibleWeapons,
            engaged
        );
'@

            $new = @'
        // WARBOARD_V55_SQUAD_SHOOTING_ENTRY
        List<WeaponAttackSelection> eligibleWeapons =
            GetEligibleRangedWeapons(
                attacker,
                target,
                engaged
            );

        if (eligibleWeapons.Count == 0)
        {
            status =
                attacker.DisplayName +
                " has no unused ranged weapon with legal range/line of sight to " +
                target.DisplayName +
                ".";
            return;
        }

        V55OpenSquadWeaponChoice(
            attacker,
            target,
            eligibleWeapons,
            engaged
        );
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Expected 1 `
                    -Label "squad-level shooting entry" `
                    -AlreadyMarker "WARBOARD_V55_SQUAD_SHOOTING_ENTRY"

            return $patchedText
        }

    # ---------------------------------------------------------
    # Terrain: route generated scenery to clean v55 geometry.
    # ---------------------------------------------------------
    Stage-Patch `
        "Assets\Scripts\Core\GameController.V50TerrainAreaBattlefield.cs" `
        {
            param($text)

            if ($text.Contains(
                    "WARBOARD_V55_CLEAN_TERRAIN_CALLS"))
            {
                return $text
            }

            # WARBOARD_V55A_TERRAIN_CALL_MATCH_FIX
            # Two calls inside the major-terrain branch are indented by
            # 12 spaces; the non-major call is indented by 8. Patch them
            # separately so source layout cannot reject a valid file.
            $oldMajor =
                "            CreateTerrainFeatureVisual50("

            $newMajor =
                "            V55CreateTerrainFeatureVisual("

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $oldMajor `
                    -New $newMajor `
                    -Expected 2 `
                    -Label "clean terrain major generator calls"

            $oldMinor =
                "        CreateTerrainFeatureVisual50("

            $newMinor =
                "        V55CreateTerrainFeatureVisual("

            $patchedText =
                Replace-LiteralExpected `
                    -Text $patchedText `
                    -Old $oldMinor `
                    -New $newMinor `
                    -Expected 1 `
                    -Label "clean terrain minor generator call"

            $markerAnchor = @'
    private void CreateTerrainFeatureVisual50(
'@

            $markerReplacement = @'
    // WARBOARD_V55_CLEAN_TERRAIN_CALLS
    private void CreateTerrainFeatureVisual50(
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $patchedText `
                    -Old $markerAnchor `
                    -New $markerReplacement `
                    -Expected 1 `
                    -Label "clean terrain generator marker"

            return $patchedText
        }

    # Terrain footprints now read visually as walkable rules areas.
    Stage-Patch `
        "Assets\Scripts\Core\TerrainAreaFootprint50.cs" `
        {
            param($text)

            if ($text.Contains(
                    "WARBOARD_V55_WALKABLE_FOOTPRINT_VISUAL"))
            {
                return $text
            }

            $oldFill = @'
        Color fill =
            IsObjective
            ? new Color(
                0.46f,
                0.33f,
                0.08f,
                0.32f
              )
            : new Color(
                0.26f,
                0.30f,
                0.31f,
                0.27f
              );
'@

            $newFill = @'
        // WARBOARD_V55_WALKABLE_FOOTPRINT_VISUAL
        // The tinted floor is the walkable Terrain Area. Only the solid
        // scenery visibly sitting on it blocks a model's final base.
        Color fill =
            IsObjective
            ? new Color(
                0.16f,
                0.25f,
                0.27f,
                0.28f
              )
            : new Color(
                0.11f,
                0.22f,
                0.25f,
                0.23f
              );
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $oldFill `
                    -New $newFill `
                    -Expected 1 `
                    -Label "walkable terrain-area fill"

            $oldOutline = @'
            : new Color(
                0.46f,
                0.55f,
                0.58f,
                0.78f
              );
'@

            $newOutline = @'
            : new Color(
                0.26f,
                0.76f,
                0.82f,
                0.92f
              );
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $patchedText `
                    -Old $oldOutline `
                    -New $newOutline `
                    -Expected 1 `
                    -Label "walkable terrain-area outline"

            return $patchedText
        }

    # ---------------------------------------------------------
    # Detachment bars below scoreboard.
    # ---------------------------------------------------------
    Stage-Patch `
        "Assets\Scripts\Factions\Aeldari\AeldariSetupUI.cs" `
        {
            param($text)

            if ($text.Contains(
                    "WARBOARD_V55_BADGE_BELOW_SCOREBOARD"))
            {
                return $text
            }

            $old = @'
            Rect badge =
                new Rect(
                    badgeX,
                    48f,
'@

            $new = @'
            // WARBOARD_V55_BADGE_BELOW_SCOREBOARD
            Rect badge =
                new Rect(
                    badgeX,
                    76f,
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Expected 1 `
                    -Label "Aeldari detachment bar below scoreboard"

            return $patchedText
        }

    Stage-Patch `
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesSetupUI.cs" `
        {
            param($text)

            if ($text.Contains(
                    "WARBOARD_V55_CUSTODES_BADGE_BELOW_SCOREBOARD"))
            {
                return $text
            }

            $old = @'
            Rect badge =
                new Rect(
                    badgeX,
                    48f,
'@

            $new = @'
            // WARBOARD_V55_CUSTODES_BADGE_BELOW_SCOREBOARD
            Rect badge =
                new Rect(
                    badgeX,
                    76f,
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Expected 1 `
                    -Label "Custodes detachment bar below scoreboard"

            return $patchedText
        }

    # Move Fight controls and selected card beneath the new detachment row.
    Stage-Patch `
        "Assets\Scripts\Core\GameController.V54UIHelpers.cs" `
        {
            param($text)

            if ($text.Contains(
                    "WARBOARD_V55_FIGHT_BAR_BELOW_BADGES"))
            {
                return $text
            }

            $old = @'
        Rect bar =
            new Rect(
                12f,
                84f,
'@

            $new = @'
        // WARBOARD_V55_FIGHT_BAR_BELOW_BADGES
        Rect bar =
            new Rect(
                12f,
                112f,
'@

            $patchedText =
                Replace-LiteralExpected `
                    -Text $text `
                    -Old $old `
                    -New $new `
                    -Expected 1 `
                    -Label "Fight bar below detachment rows"

            return $patchedText
        }

    Stage-Patch `
        "Assets\Scripts\Core\GameController.V45Presentation.cs" `
        {
            param($text)

            $replacement = @'
// WARBOARD_V55_SELECTED_CARD_BELOW_BADGES
        Rect card =
            new Rect(
                12f,
                phase == Phase.Fight
                ? 164f
                : 112f,
'@

            $patchedText =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern '(?:// WARBOARD_V54_FIGHT_CARD_OFFSET\s*)?Rect card\s*=\s*new Rect\(\s*12f,\s*phase == Phase\.Fight\s*\?\s*134f\s*:\s*82f\s*,' `
                    -Replacement $replacement `
                    -Label "selected card below detachment rows" `
                    -AlreadyMarker "WARBOARD_V55_SELECTED_CARD_BELOW_BADGES"

            return $patchedText
        }

    # ---------------------------------------------------------
    # Add v55 systems.
    # ---------------------------------------------------------
    Stage-New `
        "Assets\Scripts\Core\GameController.V55ChargeSolver.cs"

    Stage-New `
        "Assets\Scripts\Core\GameController.V55SquadShooting.cs"

    Stage-New `
        "Assets\Scripts\Core\GameController.V55CleanTerrain.cs"

    Stage-New `
        "Assets\Scripts\Core\GameController.V55MissionCards.cs"

    Stage-New `
        "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs"

    # ---------------------------------------------------------
    # Build identity.
    # ---------------------------------------------------------
    Stage-Patch `
        "Assets\Scripts\Core\WarboardBuildInfo.cs" `
        {
            param($text)

            $patchedText =
                Replace-RegexOnce `
                    -Text $text `
                    -Pattern 'public const string CurrentVersion\s*=\s*"v[^"]+";' `
                    -Replacement 'public const string CurrentVersion = "v55";' `
                    -Label "build identity v55" `
                    -AlreadyMarker 'CurrentVersion = "v55";'

            return $patchedText
        }

    # ---------------------------------------------------------
    # Staged validation.
    # ---------------------------------------------------------
    Require-Marker `
        "Assets\Scripts\Core\GameController.V48CoreAlignment.cs" `
        "WARBOARD_V55_BOUNDED_CHARGE_CALL"

    Require-Marker `
        "Assets\Scripts\Core\GameController.Combat.cs" `
        "WARBOARD_V55_SQUAD_SHOOTING_ENTRY"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V50TerrainAreaBattlefield.cs" `
        "WARBOARD_V55_CLEAN_TERRAIN_CALLS"

    Require-Marker `
        "Assets\Scripts\Core\TerrainAreaFootprint50.cs" `
        "WARBOARD_V55_WALKABLE_FOOTPRINT_VISUAL"

    Require-Marker `
        "Assets\Scripts\Factions\Aeldari\AeldariSetupUI.cs" `
        "WARBOARD_V55_BADGE_BELOW_SCOREBOARD"

    Require-Marker `
        "Assets\Scripts\Factions\AdeptusCustodes\CustodesSetupUI.cs" `
        "WARBOARD_V55_CUSTODES_BADGE_BELOW_SCOREBOARD"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V54UIHelpers.cs" `
        "WARBOARD_V55_FIGHT_BAR_BELOW_BADGES"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V45Presentation.cs" `
        "WARBOARD_V55_SELECTED_CARD_BELOW_BADGES"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V55ChargeSolver.cs" `
        "WARBOARD_V55_BOUNDED_CHARGE_SOLVER"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V55SquadShooting.cs" `
        "WARBOARD_V55_SQUAD_WEAPON_SHOOTING"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V55CleanTerrain.cs" `
        "WARBOARD_V55_CLEAN_TERRAIN_KIT"

    Require-Marker `
        "Assets\Scripts\Core\GameController.V55MissionCards.cs" `
        "WARBOARD_V55_MISSION_CARD_TEXT"

    Require-Marker `
        "Assets\Scripts\Core\WarboardV55MissionCardsWorld.cs" `
        "WARBOARD_V55_WORLD_MISSION_CARDS"

    Require-Marker `
        "Assets\Scripts\Core\WarboardBuildInfo.cs" `
        'CurrentVersion = "v55";'

    Write-Host ""
    Write-Host (
        "All v55 transforms validated in staging. Committing..."
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
        "WARBOARD v55 INSTALL COMPLETE"
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
        "WARBOARD v55 INSTALL FAILED"
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
            "Rolling project files back from the v55 backup..."
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

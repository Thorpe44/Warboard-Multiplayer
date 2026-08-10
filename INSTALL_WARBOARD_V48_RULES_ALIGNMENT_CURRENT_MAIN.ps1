$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    throw $Message
}

function Get-Text([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { Fail "Missing required file: $Path" }
    return [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path))
}

function Set-Text([string]$Path, [string]$Text) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $dir = [System.IO.Path]::GetDirectoryName($full)
    if (-not [System.IO.Directory]::Exists($dir)) {
        [System.IO.Directory]::CreateDirectory($dir) | Out-Null
    }
    [System.IO.File]::WriteAllText($full, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Find-CSharpBlockEnd([string]$Text, [int]$OpenBrace) {
    $depth = 0
    $state = 'normal'
    $i = $OpenBrace

    while ($i -lt $Text.Length) {
        $c = $Text[$i]
        $n = if ($i + 1 -lt $Text.Length) { $Text[$i + 1] } else { [char]0 }

        switch ($state) {
            'normal' {
                if ($c -eq '/' -and $n -eq '/') { $state = 'line'; $i += 2; continue }
                if ($c -eq '/' -and $n -eq '*') { $state = 'block'; $i += 2; continue }
                if ($c -eq '@' -and $n -eq '"') { $state = 'verbatim'; $i += 2; continue }
                if ($c -eq '"') { $state = 'string'; $i++; continue }
                if ($c -eq "'") { $state = 'char'; $i++; continue }
                if ($c -eq '{') { $depth++ }
                elseif ($c -eq '}') {
                    $depth--
                    if ($depth -eq 0) { return $i }
                }
                $i++
                continue
            }
            'string' {
                if ($c -eq '\') { $i += 2; continue }
                if ($c -eq '"') { $state = 'normal' }
                $i++
                continue
            }
            'verbatim' {
                if ($c -eq '"' -and $n -eq '"') { $i += 2; continue }
                if ($c -eq '"') { $state = 'normal' }
                $i++
                continue
            }
            'char' {
                if ($c -eq '\') { $i += 2; continue }
                if ($c -eq "'") { $state = 'normal' }
                $i++
                continue
            }
            'line' {
                if ($c -eq "`n") { $state = 'normal' }
                $i++
                continue
            }
            'block' {
                if ($c -eq '*' -and $n -eq '/') { $state = 'normal'; $i += 2; continue }
                $i++
                continue
            }
        }
    }

    Fail "Could not find matching closing brace."
}

function Replace-CSharpBlock(
    [string]$Path,
    [string]$SignatureRegex,
    [string]$Replacement
) {
    $text = Get-Text $Path
    $match = [regex]::Match($text, $SignatureRegex, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) { Fail "Could not locate C# member '$SignatureRegex' in $Path" }

    $open = $text.IndexOf('{', $match.Index + $match.Length)
    if ($open -lt 0) { Fail "Could not find opening brace for '$SignatureRegex' in $Path" }
    $end = Find-CSharpBlockEnd $text $open

    $before = $text.Substring(0, $match.Index)
    $after = $text.Substring($end + 1)
    Set-Text $Path ($before + $Replacement + $after)
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [string]$Label
) {
    $text = Get-Text $Path
    if (-not $text.Contains($Old)) { Fail "Could not locate expected v47 text for $Label in $Path" }
    Set-Text $Path ($text.Replace($Old, $New))
}

function Backup-File([string]$Root, [string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { Fail "Cannot back up missing file: $Path" }
    $dest = Join-Path $Root $Path
    $destDir = Split-Path -Parent $dest
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item -LiteralPath $Path -Destination $dest -Force
}

try {
    $repo = (Get-Location).Path
    $buildPath = 'Assets/Scripts/Core/WarboardBuildInfo.cs'
    if (-not (Test-Path $buildPath)) {
        Fail 'Run this installer from the main Warboard project folder (the folder containing Assets).'
    }

    $buildText = Get-Text $buildPath
    $supportedBuild =
        $buildText.Contains('public const string CurrentVersion = "v47";') -or
        $buildText.Contains('public const string Version = "v47";') -or
        $buildText.Contains('public const string CurrentVersion = "v48";') -or
        $buildText.Contains('public const string Version = "v48";')

    if (-not $supportedBuild) {
        Fail 'This recovery patch expects the current Warboard v47 build or a partially-installed v48 build.'
    }

    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupRoot = Join-Path 'Library/WarboardBackups/V48' $stamp

    $touched = @(
        'Assets/Scripts/Core/WarboardBuildInfo.cs',
        'Assets/Scripts/Core/GameController.Core.cs',
        'Assets/Scripts/Core/GameController.Charge.cs',
        'Assets/Scripts/Core/GameController.CoreCompletion11.cs',
        'Assets/Scripts/Core/GameController.Traditional.cs',
        'Assets/Scripts/Core/GameController.UI.cs',
        'Assets/Scripts/Core/CoreRules11Completion.cs',
        'Assets/Scripts/Core/InteractiveAttackController.cs',
        'Assets/Scripts/Core/RulesEngine.cs',
        'Assets/Scripts/Factions/Standard11/StandardFactionGameController.cs'
    )

    foreach ($file in $touched) { Backup-File $backupRoot $file }

    Write-Host "Warboard v48 - 11th Edition Rules Alignment (Current Main Recovery)" -ForegroundColor Cyan
    Write-Host "Backup: $backupRoot"

    # 1) Add v48 rules modules.
    Copy-Item -LiteralPath 'V48_PATCH_PAYLOAD/GameController.V48CoreAlignment.cs' `
        -Destination 'Assets/Scripts/Core/GameController.V48CoreAlignment.cs' -Force
    Copy-Item -LiteralPath 'V48_PATCH_PAYLOAD/InteractiveAttackController.V48Alignment.cs' `
        -Destination 'Assets/Scripts/Core/InteractiveAttackController.V48Alignment.cs' -Force

    # 2) Build identity.
    # Current Warboard main uses CurrentVersion rather than the older
    # Version + Label shape this patch was originally authored against.
    $build = Get-Text $buildPath

    if ($build.Contains('public const string CurrentVersion = "v47";')) {
        $build = $build.Replace(
            'public const string CurrentVersion = "v47";',
            'public const string CurrentVersion = "v48";'
        )
        Write-Host '[PATCH] WarboardBuildInfo CurrentVersion v47 -> v48'
    }
    elseif ($build.Contains('public const string Version = "v47";')) {
        $build = $build.Replace(
            'public const string Version = "v47";',
            'public const string Version = "v48";'
        )
        Write-Host '[PATCH] legacy WarboardBuildInfo Version v47 -> v48'
    }
    elseif (
        $build.Contains('public const string CurrentVersion = "v48";') -or
        $build.Contains('public const string Version = "v48";')
    ) {
        Write-Host '[ALREADY] WarboardBuildInfo already reports v48'
    }
    else {
        Fail 'Could not locate a supported v47/v48 build version declaration in WarboardBuildInfo.cs'
    }

    # Label existed in older builds only. Preserve it if present, but do not
    # require it in the current CurrentVersion-only build.
    if ($build.Contains('public const string Label = "11th Edition Full Faction Rules";')) {
        $build = $build.Replace(
            'public const string Label = "11th Edition Full Faction Rules";',
            'public const string Label = "11th Edition Rules Alignment";'
        )
    }

    Set-Text $buildPath $build

    # 3) Charge sequence: roll before target selection, multi-target charge support.
    Replace-CSharpBlock 'Assets/Scripts/Core/GameController.Charge.cs' `
        'private\s+void\s+TryCharge\s*\(\s*SquadController\s+attacker\s*,\s*SquadController\s+target\s*\)' `
@'
    private void TryCharge(
        SquadController attacker,
        SquadController target)
    {
        V48TryCharge(attacker, target);
    }
'@

    Replace-CSharpBlock 'Assets/Scripts/Core/GameController.Traditional.cs' `
        'private\s+void\s+CompleteTraditionalCharge\s*\(\s*\)' `
@'
    private void CompleteTraditionalCharge()
    {
        V48CompleteTraditionalCharge();
    }
'@

    # 4) Correct Command phase event/order and reset v48 phase windows.
    $corePath = 'Assets/Scripts/Core/GameController.Core.cs'
    $core = Get-Text $corePath
    $orderPattern = '(?s)\s*// 11e Core CP: both players gain 1 CP at the start of each\s*// Command phase\..*?string battleShockSummary\s*=\s*ResolveCommandPhaseBattleShock\(\);'
    $orderMatch = [regex]::Match($core, $orderPattern)
    if (-not $orderMatch.Success) { Fail 'Could not locate v47 Command-phase order block.' }
    $orderReplacement = @'

        GameEventBus.Raise(
            new GameEventContext
            {
                Type =
                    GameEventType.TurnStarted,
                Game = this,
                ActingFaction =
                    activeFaction,
                Phase = phase,
                Note =
                    firstTurn
                    ? "First turn"
                    : "New turn"
            }
        );

        // 11e sequence: Start of Command phase first, then Core CP, then
        // Battle-shock, then Command Abilities.
        RaisePhaseStarted();

        foreach (string faction
            in factions)
        {
            GainCommandPoints(
                faction,
                1
            );
        }

        string battleShockSummary =
            ResolveCommandPhaseBattleShock();
'@
    $core = $core.Substring(0, $orderMatch.Index) + $orderReplacement + $core.Substring($orderMatch.Index + $orderMatch.Length)
    Set-Text $corePath $core

    Replace-CSharpBlock $corePath `
        'private\s+void\s+RaisePhaseStarted\s*\(\s*\)' `
@'
    private void RaisePhaseStarted()
    {
        V48ResetPhaseWindows();

        GameEventBus.Raise(
            new GameEventContext
            {
                Type =
                    GameEventType.PhaseStarted,
                Game = this,
                ActingFaction =
                    activeFaction,
                Phase = phase
            }
        );
    }
'@

    # 5) End-of-phase Core windows: Fire Overwatch before Rapid Ingress; fixed Heroic sequence.
    $completionPath = 'Assets/Scripts/Core/GameController.CoreCompletion11.cs'
    Replace-CSharpBlock $completionPath `
        'private\s+bool\s+Core11CanAdvancePhase\s*\(\s*out\s+string\s+reason\s*\)' `
@'
    private bool Core11CanAdvancePhase(
        out string reason)
    {
        reason = "";

        Core11ResolvePendingDestroyedTransports();

        if (core11DisembarkPassenger != null)
        {
            reason = "Finish the pending disembark placement before changing phase.";
            return false;
        }

        if (core11HeroicChargeUnit != null)
        {
            reason = "Finish the Heroic Intervention charge move before changing phase.";
            return false;
        }

        if (core11EmergencyDisembarkQueue.Count > 0)
        {
            reason = "Finish all emergency disembark placements before continuing.";
            return false;
        }

        if (core11CounteroffensiveDecisionPending)
        {
            reason = "Resolve the pending Counteroffensive decision before continuing.";
            return false;
        }

        if (reservePlacementSquad != null)
        {
            reason = "Finish the reserve/ingress placement before changing phase.";
            return false;
        }

        if (phase == Phase.Move &&
            !v48EndMoveOverwatchResolved &&
            V48OpenFireOverwatchWindow())
        {
            reason = "Resolve the end-of-Movement Fire Overwatch window first.";
            return false;
        }

        if (phase == Phase.Move &&
            !core11EndMoveWindowResolved &&
            Core11OpenRapidIngressWindow())
        {
            reason = "Resolve the end-of-Movement Rapid Ingress window first.";
            return false;
        }

        if (phase == Phase.Charge &&
            !core11EndChargeWindowResolved &&
            Core11OpenHeroicInterventionWindow())
        {
            reason = "Resolve the end-of-Charge Heroic Intervention window first.";
            return false;
        }

        return true;
    }
'@

    Replace-CSharpBlock $completionPath `
        'private\s+void\s+Core11ChooseHeroicTarget\s*\(\s*SquadController\s+unit\s*\)' `
@'
    private void Core11ChooseHeroicTarget(
        SquadController unit)
    {
        V48ChooseHeroicMode(unit);
    }
'@

    Replace-CSharpBlock $completionPath `
        'private\s+void\s+Core11ResolveHeroicIntervention\s*\(\s*SquadController\s+unit\s*,\s*SquadController\s+target\s*,\s*bool\s+intoFray\s*\)' `
@'
    private void Core11ResolveHeroicIntervention(
        SquadController unit,
        SquadController target,
        bool intoFray)
    {
        V48BeginHeroicCharge(unit, intoFray);
    }
'@

    Replace-CSharpBlock $completionPath `
        'private\s+bool\s+Core11CanUseExplosives\s*\(\s*SquadController\s+unit\s*\)' `
@'
    private bool Core11CanUseExplosives(
        SquadController unit)
    {
        return V48CanUseExplosives(unit);
    }
'@

    Replace-CSharpBlock $completionPath `
        'private\s+void\s+Core11UseExplosives\s*\(\s*SquadController\s+unit\s*\)' `
@'
    private void Core11UseExplosives(
        SquadController unit)
    {
        V48UseExplosives(unit);
    }
'@

    Replace-CSharpBlock $completionPath `
        'private\s+void\s+Core11UseCrushingImpact\s*\(\s*SquadController\s+unit\s*\)' `
@'
    private void Core11UseCrushingImpact(
        SquadController unit)
    {
        V48UseCrushingImpact(unit);
    }
'@

    # Battle-shocked units cannot be targeted by Heroic Intervention.
    $heroicText = Get-Text $completionPath
    $heroicOld = @'
                        !unit.IsAttachedLeader &&
                        unit.FactionId == opposingFaction &&
                        !IsEngaged(unit) &&
'@
    $heroicNew = @'
                        !unit.IsAttachedLeader &&
                        unit.FactionId == opposingFaction &&
                        !unit.IsBattleShocked &&
                        !IsEngaged(unit) &&
'@
    if ($heroicText.Contains($heroicOld)) {
        Set-Text $completionPath ($heroicText.Replace($heroicOld, $heroicNew))
    }

    # 6) Dense terrain <=2 inches can be crossed horizontally by all models.
    Replace-CSharpBlock 'Assets/Scripts/Core/CoreRules11Completion.cs' `
        'public\s+static\s+bool\s+MovementDestinationAllowsTerrain\s*\(\s*SquadController\s+movingUnit\s*,\s*TerrainFeature\s+terrain\s*\)' `
@'
    public static bool MovementDestinationAllowsTerrain(
        SquadController movingUnit,
        TerrainFeature terrain)
    {
        if (terrain == null)
            return true;

        CoreTerrainCategory11 category =
            Category(terrain);

        if (category == CoreTerrainCategory11.Exposed ||
            category == CoreTerrainCategory11.Light)
        {
            return true;
        }

        if (movingUnit == null)
            return false;

        movingUnit = movingUnit.JoinedActionController();

        if (CoreRules11FlightRegistry.IsTakingToSkies(
                movingUnit))
        {
            return true;
        }

        if (category == CoreTerrainCategory11.Dense &&
            WarboardV48CoreRules.DenseSectionIsLow(terrain))
        {
            return true;
        }

        return
            movingUnit.HasKeyword("INFANTRY") ||
            movingUnit.HasKeyword("BEASTS") ||
            movingUnit.HasKeyword("SWARM") ||
            movingUnit.HasKeyword("MOBILE");
    }
'@

    # 7) Remove the invented Incursion 3DP exception.
    $standardPath = 'Assets/Scripts/Factions/Standard11/StandardFactionGameController.cs'
    $standard = Get-Text $standardPath
    $exceptionPattern = '(?s)\s*bool\s+incursionThreePointException\s*=.*?definitions\[0\]\.dp\s*==\s*3\s*;'
    $exceptionMatch = [regex]::Match($standard, $exceptionPattern)
    if (-not $exceptionMatch.Success) { Fail 'Could not locate the v47 Incursion 3DP exception.' }
    $standard = $standard.Remove($exceptionMatch.Index, $exceptionMatch.Length)
    $conditionPattern = 'if\s*\(\s*limit\s*>\s*0\s*&&\s*total\s*>\s*limit\s*&&\s*!incursionThreePointException\s*\)'
    if (-not [regex]::IsMatch($standard, $conditionPattern)) { Fail 'Could not locate Incursion DP limit condition.' }
    $standard = [regex]::Replace(
        $standard,
        $conditionPattern,
        "if (limit > 0 &&`r`n            total > limit)",
        1)
    $standard = $standard.Replace('"DP. Incursion permits a single 3DP Detachment as the exception."', '"DP."')
    if ($standard -match 'incursionThreePointException') { Fail 'Incursion 3DP exception was not fully removed.' }
    Set-Text $standardPath $standard

    # 8) Interactive/XCOM attack flow: optional Lethal/Precision, dynamic allocation,
    # lowest-to-highest saves, selected one-die Command Re-roll, mixed-unit Hazardous.
    $attackPath = 'Assets/Scripts/Core/InteractiveAttackController.cs'
    Replace-Exact $attackPath 'public class InteractiveAttackController' 'public partial class InteractiveAttackController' 'partial attack controller'

    Replace-CSharpBlock $attackPath 'private\s+void\s+RecalculateHitResults\s*\(\s*\)' @'
    private void RecalculateHitResults()
    {
        V48RecalculateHitResults();
    }
'@
    Replace-CSharpBlock $attackPath 'private\s+void\s+RollSaves\s*\(\s*\)' @'
    private void RollSaves()
    {
        V48RollSaves();
    }
'@
    Replace-CSharpBlock $attackPath 'private\s+void\s+RecalculateSaveResults\s*\(\s*\)' @'
    private void RecalculateSaveResults()
    {
        V48RecalculateSaveResults();
    }
'@
    Replace-CSharpBlock $attackPath 'private\s+void\s+RollDamage\s*\(\s*\)' @'
    private void RollDamage()
    {
        V48RollDamageCompatibility();
    }
'@
    Replace-CSharpBlock $attackPath 'private\s+void\s+ApplyDamage\s*\(\s*\)' @'
    private void ApplyDamage()
    {
        V48ApplyDamageCompatibility();
    }
'@
    Replace-CSharpBlock $attackPath 'private\s+void\s+AdvanceToNextVolley\s*\(\s*\)' @'
    private void AdvanceToNextVolley()
    {
        V48AdvanceToNextVolley();
    }
'@
    Replace-CSharpBlock $attackPath 'private\s+int\s+FindRerollableDieIndex\s*\(\s*\)' @'
    private int FindRerollableDieIndex()
    {
        return V48FindRerollableDieIndex();
    }
'@
    Replace-CSharpBlock $attackPath 'public\s+void\s+Continue\s*\(\s*\)' @'
    public void Continue()
    {
        V48Continue();
    }
'@
    Replace-CSharpBlock $attackPath 'public\s+bool\s+DeclineDecisionAndFastResolve\s*\(\s*\)' @'
    public bool DeclineDecisionAndFastResolve()
    {
        return V48DeclineDecisionAndFastResolve();
    }
'@
    Replace-CSharpBlock $attackPath 'public\s+bool\s+UseCommandReroll\s*\(\s*\)' @'
    public bool UseCommandReroll()
    {
        return V48UseCommandReroll();
    }
'@
    Replace-CSharpBlock $attackPath 'public\s+bool\s+HasMeaningfulDecision\s*' @'
    public bool HasMeaningfulDecision
    {
        get
        {
            return
                V48LethalDecisionPending ||
                V48PrecisionDecisionPending ||
                CanUsePartingTheVeil ||
                CanUseMacabreResilience ||
                (IsReviewStage && CanCommandReroll);
        }
    }
'@

    # Mark dice that have already been rerolled by automatic/faction rules, so
    # Command Re-roll cannot reroll the same die twice.
    $attack = Get-Text $attackPath
    if ($attack -notmatch 'V48MarkHitRerolled\(volley, i\)') {
        $attack = [regex]::Replace(
            $attack,
            '(?m)^(\s*)volley\.hitRolls\[i\]\s*=\s*DiceRoller\.',
            '$1V48MarkHitRerolled(volley, i);' + "`r`n" + '$1volley.hitRolls[i] = DiceRoller.')
    }
    if ($attack -notmatch 'V48MarkWoundRerolled\(volley, i\)') {
        $attack = [regex]::Replace(
            $attack,
            '(?m)^(\s*)volley\.woundRolls\[i\]\s*=\s*DiceRoller\.',
            '$1V48MarkWoundRerolled(volley, i);' + "`r`n" + '$1volley.woundRolls[i] = DiceRoller.')
    }
    Set-Text $attackPath $attack

    # 9) v48 XCOM decision UI for the newly explicit rule choices.
    $uiReplacement = Get-Text 'V48_PATCH_PAYLOAD/DrawXcomAttackDecisionWindow.v48.txt'
    Replace-CSharpBlock 'Assets/Scripts/Core/GameController.UI.cs' `
        'private\s+void\s+DrawXcomAttackDecisionWindow\s*\(\s*\)' `
        $uiReplacement

    # 10) Hazardous compatibility path: 3 MW only if every model is MONSTER/VEHICLE.
    $rulesPath = 'Assets/Scripts/Core/RulesEngine.cs'
    $rules = Get-Text $rulesPath
    $hazardPattern = '(?s)int\s+mortalWounds\s*=\s*attacker\.HasKeyword\(\s*"monster"\s*\)\s*\|\|\s*attacker\.HasKeyword\(\s*"vehicle"\s*\)\s*\?\s*3\s*:\s*1\s*;'
    if (-not [regex]::IsMatch($rules, $hazardPattern)) { Fail 'Could not locate v47 RulesEngine Hazardous damage test.' }
    $rules = [regex]::Replace(
        $rules,
        $hazardPattern,
        "int mortalWounds =`r`n                WarboardV48CoreRules.AllModelsMonsterOrVehicle(attacker)`r`n                ? 3`r`n                : 1;",
        1)
    Set-Text $rulesPath $rules

    # Final static checks.
    $checks = @(
        @{ Path = $buildPath; Text = '"v48"' },
        @{ Path = $attackPath; Text = 'public partial class InteractiveAttackController' },
        @{ Path = $attackPath; Text = 'V48RecalculateHitResults();' },
        @{ Path = $completionPath; Text = 'V48OpenFireOverwatchWindow()' },
        @{ Path = $standardPath; Text = 'total > limit)' },
        @{ Path = 'Assets/Scripts/Core/CoreRules11Completion.cs'; Text = 'DenseSectionIsLow' }
    )

    foreach ($check in $checks) {
        $text = Get-Text $check.Path
        if (-not $text.Contains($check.Text)) {
            Fail "Static verification failed: '$($check.Text)' missing from $($check.Path)"
        }
    }

    Write-Host ""
    Write-Host 'v48 patch installed successfully.' -ForegroundColor Green
    Write-Host 'Open Unity and allow scripts to compile. Test one XCOM and one Traditional battle before pushing to GitHub.'
    Write-Host "Backup is at: $backupRoot"
}
catch {
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host 'No automatic rollback was attempted; restore from Library/WarboardBackups/V48 if needed.' -ForegroundColor Yellow
    exit 1
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// WARBOARD_V48_11E_CORE_ALIGNMENT
// June 2026 11th Edition Core Rules alignment for sequencing/reaction rules.
public partial class GameController
{
    private SquadController v48ChargeUnit;
    private int v48ChargeRawRoll;
    private bool v48ChargeCommandRerolled;
    private bool v48ChargeFactionRerollResolved;
    private bool v48ChargeHeroic;
    private bool v48ChargeHeroicIntoFray;
    private readonly HashSet<SquadController> v48ChargeTargets =
        new HashSet<SquadController>();

    private bool v48EndMoveOverwatchResolved;

    private void V48ResetPhaseWindows()
    {
        if (phase == Phase.Move)
            v48EndMoveOverwatchResolved = false;
    }

    private void V48TryCharge(
        SquadController attacker,
        SquadController clickedEnemy)
    {
        if (attacker == null ||
            !attacker.IsAlive ||
            !attacker.IsOnBattlefield ||
            attacker.FactionId != activeFaction)
        {
            return;
        }

        attacker = attacker.JoinedActionController();

        if (!Aeldari11CanCharge(attacker))
        {
            status = attacker.DisplayName +
                " cannot declare a charge this turn because of an Aeldari rule.";
            return;
        }

        if (core11CannotChargeThisTurn.Contains(attacker))
        {
            status = attacker.DisplayName +
                " cannot declare a charge this turn.";
            return;
        }

        if (missionSystem != null &&
            missionSystem.UnitHasStartedAction(attacker))
        {
            status = "That unit started a mission action this turn and cannot declare a charge.";
            return;
        }

        if (showStratagemReaction ||
            showRuleChoiceWindow)
        {
            status = "Resolve the current rules window first.";
            return;
        }

        bool canChargeAfterFallBack =
            Necrons11CanChargeAfterFallBack(attacker) ||
            Custodes11CanChargeAfterFallBack(attacker) ||
            WarboardFactionExtensionHub.CanChargeAfterFallBack(attacker) ||
            (aeldariRules != null &&
             aeldariRules.CanChargeAfterFallBack(attacker));

        bool canChargeAfterAdvance =
            Necrons11CanChargeAfterAdvance(attacker) ||
            Custodes11CanChargeAfterAdvance(attacker) ||
            WarboardFactionExtensionHub.CanChargeAfterAdvance(attacker) ||
            (aeldariRules != null &&
             aeldariRules.CanChargeAfterAdvance(attacker));

        if (attacker.HasCharged)
        {
            status = "That unit has already declared a charge this phase.";
            return;
        }

        if (attacker.HasFallenBack && !canChargeAfterFallBack)
        {
            status = "That unit Fell Back this turn and cannot declare a charge.";
            return;
        }

        if (attacker.HasAdvanced && !canChargeAfterAdvance)
        {
            status = "That unit Advanced this turn and cannot declare a charge.";
            return;
        }

        if (IsEngaged(attacker))
        {
            status = "That unit is already engaged.";
            return;
        }

        bool enemyWithinTwelve =
            squads.Any(
                enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    !enemy.IsAttachedLeader &&
                    enemy.FactionId != attacker.FactionId &&
                    JoinedDistance(attacker, enemy) <= 12.001f);

        if (!enemyWithinTwelve)
        {
            status = "That unit cannot declare a charge because no enemy unit is within 12 inches.";
            return;
        }

        // 11.02: declaring the charge selects only the charging unit.
        // Targets are selected after the charge roll, immediately before moving.
        attacker.HasCharged = true;
        NotifyChargeDeclared(attacker, null);

        v48ChargeUnit = attacker;
        v48ChargeTargets.Clear();
        v48ChargeCommandRerolled = false;
        v48ChargeFactionRerollResolved = false;
        v48ChargeHeroic = false;
        v48ChargeHeroicIntoFray = false;

        if (!IsXcomMode)
        {
            traditionalChargePending = true;
            traditionalChargeAttacker = attacker;
            traditionalChargeTarget = null;
            traditionalChargeResult = 2;

            OpenTraditionalDicePrompt(2);
            AppendBattleLog(
                "CHARGE",
                attacker.DisplayName,
                "Charge declared. Roll 2D6 first. Charge targets are selected only after the final charge roll is known."
            );
            status =
                "TRADITIONAL CHARGE: roll 2D6 and resolve any rerolls. Enter the final charge total; Warboard will then ask for charge target(s).";
            return;
        }

        V48OfferCommandRerollForCharge(
            attacker,
            RollChargeDice());
    }

    private void V48CompleteTraditionalCharge()
    {
        if (!traditionalChargePending ||
            traditionalChargeAttacker == null)
        {
            traditionalChargePending = false;
            traditionalChargeAttacker = null;
            traditionalChargeTarget = null;
            return;
        }

        SquadController attacker =
            traditionalChargeAttacker.JoinedActionController();
        int result = Mathf.Clamp(traditionalChargeResult, 2, 12);

        traditionalChargePending = false;
        traditionalChargeAttacker = null;
        traditionalChargeTarget = null;

        AppendBattleLog(
            "CHARGE",
            attacker.DisplayName,
            "Player-entered final Charge total: " + result +
            ". Target selection now follows the charge roll as required by 11e."
        );

        v48ChargeUnit = attacker;
        v48ChargeRawRoll = result;
        v48ChargeTargets.Clear();
        v48ChargeCommandRerolled = true; // manual player already handled rerolls
        v48ChargeFactionRerollResolved = true;
        v48ChargeHeroic = false;
        v48ChargeHeroicIntoFray = false;

        V48OpenChargeTargetSelection();
    }

    private void V48OfferCommandRerollForCharge(
        SquadController attacker,
        int roll)
    {
        v48ChargeRawRoll = roll;

        GameEventContext context =
            new GameEventContext
            {
                Type = GameEventType.ChargeRolled,
                Game = this,
                ActingFaction = attacker.FactionId,
                Phase = phase,
                Source = attacker,
                Target = null,
                RollTotal = roll,
                PreviousRollTotal = 0,
                IsReroll = false
            };

        GameEventBus.Raise(context);

        IStratagem commandReroll = StratagemRegistry.Get("command_reroll");

        if (!attacker.IsBattleShocked &&
            commandReroll != null &&
            commandReroll.CanUse(this, attacker.FactionId, context))
        {
            List<RuleChoiceOption> options =
                new List<RuleChoiceOption>();

            options.Add(
                new RuleChoiceOption(
                    "Keep " + roll,
                    () =>
                    {
                        CloseRuleChoice();
                        V48OpenChargeTargetSelection();
                    }));

            options.Add(
                new RuleChoiceOption(
                    "Command Re-roll full 2D6 (1CP)",
                    () =>
                    {
                        if (!commandReroll.Use(this, attacker.FactionId, context))
                        {
                            status = "Command Re-roll is not available.";
                            return;
                        }

                        CloseRuleChoice();
                        int reroll = RollChargeDice();
                        v48ChargeRawRoll = reroll;
                        v48ChargeCommandRerolled = true;

                        GameEventBus.Raise(
                            new GameEventContext
                            {
                                Type = GameEventType.ChargeRolled,
                                Game = this,
                                ActingFaction = attacker.FactionId,
                                Phase = phase,
                                Source = attacker,
                                Target = null,
                                RollTotal = reroll,
                                PreviousRollTotal = roll,
                                IsReroll = true,
                                Note = "Command Re-roll"
                            });

                        V48OpenChargeTargetSelection();
                    }));

            OpenRuleChoice(
                "CHARGE ROLL - " + roll,
                "11e charge sequence: resolve the charge roll first. After this decision you will select one or more charge targets.",
                options);
            return;
        }

        V48OpenChargeTargetSelection();
    }

    private List<SquadController> V48ChargeTargetCandidates()
    {
        if (v48ChargeUnit == null)
            return new List<SquadController>();

        float declarationLimit =
            v48ChargeHeroic && v48ChargeHeroicIntoFray
            ? 6f
            : 12f;

        return squads
            .Where(
                enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    !enemy.IsAttachedLeader &&
                    enemy.FactionId != v48ChargeUnit.FactionId &&
                    JoinedDistance(v48ChargeUnit, enemy) <= declarationLimit + 0.001f &&
                    (!v48ChargeHeroic ||
                     v48ChargeHeroicIntoFray ||
                     enemy.MadeChargeMove))
            .Select(enemy => enemy.JoinedActionController())
            .Distinct()
            .OrderBy(enemy => JoinedDistance(v48ChargeUnit, enemy))
            .ToList();
    }

    private void V48OpenChargeTargetSelection()
    {
        if (v48ChargeUnit == null || !v48ChargeUnit.IsAlive)
            return;

        List<SquadController> candidates = V48ChargeTargetCandidates();
        if (candidates.Count == 0)
        {
            status = v48ChargeUnit.DisplayName +
                " rolled " + v48ChargeRawRoll +
                " but has no legal charge targets. The charge is resolved and no models move.";
            V48FinishChargeState();
            return;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController enemy in candidates)
        {
            SquadController captured = enemy;
            bool selected = v48ChargeTargets.Contains(captured);
            string reason;
            bool targetable =
                WarboardV47FactionRules.CanAttackTarget(
                    v48ChargeUnit,
                    captured,
                    AttackMode.Melee,
                    out reason);

            options.Add(
                new RuleChoiceOption(
                    (selected ? "[X] " : "[ ] ") +
                    captured.DisplayName +
                    " - " +
                    JoinedDistance(v48ChargeUnit, captured).ToString("0.0") +
                    " in" +
                    (targetable ? "" : " (not legal)"),
                    () =>
                    {
                        if (!targetable)
                        {
                            status = reason;
                            return;
                        }

                        if (!Core11AircraftChargeAllowed(
                                v48ChargeUnit,
                                captured))
                        {
                            return;
                        }

                        CloseRuleChoice();
                        if (!v48ChargeTargets.Add(captured))
                            v48ChargeTargets.Remove(captured);
                        V48OpenChargeTargetSelection();
                    }));
        }

        options.Add(
            new RuleChoiceOption(
                "CONFIRM " + v48ChargeTargets.Count + " TARGET(S)",
                () =>
                {
                    if (v48ChargeTargets.Count == 0)
                    {
                        status = "Select at least one charge target, or choose Do not make the Charge move.";
                        return;
                    }

                    CloseRuleChoice();
                    V48ConfirmChargeTargets();
                }));

        options.Add(
            new RuleChoiceOption(
                "Do not make the Charge move",
                () =>
                {
                    CloseRuleChoice();
                    status = v48ChargeUnit.DisplayName +
                        " does not make a Charge move. The charge is resolved.";
                    V48FinishChargeState();
                }));

        OpenRuleChoice(
            v48ChargeHeroic ? "HEROIC INTERVENTION - SELECT TARGETS" : "CHARGE - SELECT TARGETS",
            "Charge roll " + v48ChargeRawRoll +
            ". Select one or more enemy units. Final legality is checked using the fully modified maximum distance, and the Charge move must engage every selected target and no unselected enemy.",
            options);
    }

    private int V48ChargeMaximumDistance()
    {
        if (v48ChargeUnit == null)
            return 0;

        int result = v48ChargeRawRoll;

        result += CustodesFactionPack11.ChargeRollModifier(v48ChargeUnit);

        int necronModifier = 0;
        int standardModifier = 0;

        foreach (SquadController target in v48ChargeTargets)
        {
            necronModifier = Mathf.Max(
                necronModifier,
                Necrons11ChargeRollModifier(v48ChargeUnit, target));

            standardModifier = Mathf.Max(
                standardModifier,
                WarboardFactionExtensionHub.ChargeRollModifier(
                    this,
                    v48ChargeUnit,
                    target));
        }

        result += necronModifier + standardModifier;

        if (CoreRules11FlightRegistry.IsTakingToSkies(v48ChargeUnit))
            result = Mathf.Max(0, result - 2);

        if (v48ChargeHeroic && v48ChargeHeroicIntoFray)
            result = Mathf.Min(6, result);

        return Mathf.Max(0, result);
    }

    private void V48ConfirmChargeTargets()
    {
        if (v48ChargeUnit == null || v48ChargeTargets.Count == 0)
            return;

        int maximum = V48ChargeMaximumDistance();

        SquadController tooFar =
            v48ChargeTargets.FirstOrDefault(
                target =>
                    JoinedDistance(v48ChargeUnit, target) > maximum + 0.001f);

        if (tooFar != null)
        {
            status = tooFar.DisplayName +
                " is outside the final charge maximum of " + maximum +
                " inches. Change the selected targets.";
            V48OpenChargeTargetSelection();
            return;
        }

        // Target-dependent faction rerolls become knowable only now because
        // 11e selects targets after the roll. If used, target selection is
        // cleared and repeated against the new roll.
        if (!v48ChargeFactionRerollResolved && IsXcomMode)
        {
            SquadController rerollTarget =
                v48ChargeTargets.FirstOrDefault(
                    target =>
                        WarboardFactionExtensionHub.CanRerollCharge(
                            this,
                            v48ChargeUnit,
                            target));

            if (rerollTarget != null)
            {
                int original = v48ChargeRawRoll;
                List<RuleChoiceOption> options =
                    new List<RuleChoiceOption>
                    {
                        new RuleChoiceOption(
                            "Keep " + original,
                            () =>
                            {
                                CloseRuleChoice();
                                v48ChargeFactionRerollResolved = true;
                                V48ConfirmChargeTargets();
                            }),
                        new RuleChoiceOption(
                            "Use faction Charge re-roll",
                            () =>
                            {
                                CloseRuleChoice();
                                v48ChargeFactionRerollResolved = true;
                                v48ChargeRawRoll = RollChargeDice();
                                v48ChargeTargets.Clear();
                                V48OpenChargeTargetSelection();
                            })
                    };

                OpenRuleChoice(
                    "OPTIONAL FACTION CHARGE RE-ROLL",
                    v48ChargeUnit.DisplayName +
                    " has an eligible faction Charge re-roll. A reroll replaces the full Charge roll and targets are then selected again.",
                    options);
                return;
            }
        }

        foreach (SquadController selected in v48ChargeTargets)
        {
            WarboardRuleEventBus47.RaiseTargetSelected(
                this,
                v48ChargeUnit,
                selected,
                AttackMode.Melee);
        }

        // WARBOARD_V55_BOUNDED_CHARGE_CALL
        if (!V55SolveChargeMoveBounded(
                v48ChargeUnit,
                v48ChargeTargets.ToList(),
                maximum))
        {
            status = v48ChargeUnit.DisplayName +
                " has no legal Charge move that engages every selected target without engaging an unselected enemy. No models move.";
            V48FinishChargeState();
            return;
        }

        v48ChargeUnit.HasMoved = true;
        v48ChargeUnit.HasCharged = true;
        v48ChargeUnit.MarkMadeChargeMove();
        Custodes11AfterSuccessfulCharge(v48ChargeUnit);

        if (v48ChargeUnit.AttachedLeader != null)
        {
            v48ChargeUnit.AttachedLeader.HasMoved = true;
            v48ChargeUnit.AttachedLeader.HasCharged = true;
        }

        status = v48ChargeUnit.DisplayName +
            " completed a Charge move of up to " + maximum +
            " inches into " +
            string.Join(", ", v48ChargeTargets.Select(t => t.DisplayName).ToArray()) +
            ".";

        V48FinishChargeState();
    }

    private bool V48SolveChargeMove(
        SquadController charger,
        List<SquadController> targets,
        float maximum)
    {
        if (charger == null || targets == null || targets.Count == 0)
            return false;

        Dictionary<ModelToken, Vector3> original =
            CaptureJoinedPositions(charger);
        List<ModelToken> models = JoinedModels(charger);
        if (models.Count == 0)
            return false;

        Vector3 centre = Vector3.zero;
        foreach (ModelToken model in models)
            centre += model.transform.position;
        centre /= models.Count;

        List<Vector3> directions = new List<Vector3>();
        Vector3 average = Vector3.zero;

        foreach (SquadController target in targets)
        {
            Vector3 dir = target.CurrentCentre() - centre;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                directions.Add(dir);
                average += dir;
            }
        }

        if (average.sqrMagnitude > 0.001f)
        {
            average.Normalize();
            directions.Insert(0, average);
        }

        float step = Mathf.Max(0.25f, maximum / 20f);
        List<Vector3> deltas = new List<Vector3> { Vector3.zero };

        foreach (Vector3 baseDirection in directions)
        {
            for (int angle = -70; angle <= 70; angle += 10)
            {
                Vector3 direction =
                    Quaternion.Euler(0f, angle, 0f) * baseDirection;

                for (float distance = step;
                     distance <= maximum + 0.001f;
                     distance += step)
                {
                    deltas.Add(direction * distance);
                }
            }
        }

        deltas = deltas
            .OrderByDescending(delta => delta.magnitude)
            .ToList();

        foreach (Vector3 delta in deltas)
        {
            RestoreJoinedPositions(original);
            TranslateJoinedModels(charger, delta);
            Physics.SyncTransforms();

            V48RefineChargeTowardTargets(
                charger,
                targets,
                maximum,
                original);

            if (!V48ChargeEndStateIsLegal(
                    charger,
                    targets,
                    maximum,
                    original))
            {
                continue;
            }

            return true;
        }

        RestoreJoinedPositions(original);
        return false;
    }

    private void V48RefineChargeTowardTargets(
        SquadController charger,
        List<SquadController> targets,
        float maximum,
        Dictionary<ModelToken, Vector3> original)
    {
        // Individual models are allowed to use different paths/distances.
        // Several passes let a unit spread into a legal multi-target charge
        // while the final legality check still enforces coherency.
        for (int pass = 0; pass < 3; pass++)
        {
            foreach (ModelToken model in JoinedModels(charger))
            {
                Vector3 start;
                if (!original.TryGetValue(model, out start))
                    continue;

                Vector3 current = model.transform.position;
                Vector3 best = current;
                float bestDistance = targets.Min(
                    target => DistancePointToSquad(current, target));

                foreach (SquadController target in targets)
                {
                    Vector3 candidate;
                    if (!TryFindBestChargeDestination(
                            charger,
                            model,
                            target,
                            start,
                            maximum,
                            out candidate))
                    {
                        continue;
                    }

                    float candidateDistance = targets.Min(
                        enemy => DistancePointToSquad(candidate, enemy));

                    if (candidateDistance < bestDistance - 0.001f)
                    {
                        bestDistance = candidateDistance;
                        best = candidate;
                    }
                }

                if (HorizontalDistance(best, current) <= 0.001f)
                    continue;

                model.transform.position = best;
                Physics.SyncTransforms();

                if (!AllModelsInsideBoard(charger) ||
                    !AllModelsHaveLegalPlacement(charger))
                {
                    model.transform.position = current;
                    Physics.SyncTransforms();
                }
            }
        }
    }

    private bool V48ChargeEndStateIsLegal(
        SquadController charger,
        List<SquadController> targets,
        float maximum,
        Dictionary<ModelToken, Vector3> original)
    {
        if (!charger.IsCoherent() ||
            !AllModelsInsideBoard(charger) ||
            !AllModelsHaveLegalPlacement(charger))
        {
            return false;
        }

        foreach (SquadController target in targets)
        {
            if (!UnitsAreEngaged(charger, target))
                return false;
        }

        foreach (SquadController enemy in squads)
        {
            if (enemy == null ||
                !enemy.IsAlive ||
                !enemy.IsOnBattlefield ||
                enemy.FactionId == charger.FactionId ||
                enemy.IsAttachedLeader ||
                targets.Contains(enemy.JoinedActionController()))
            {
                continue;
            }

            if (UnitsAreEngaged(charger, enemy))
                return false;
        }

        foreach (ModelToken model in JoinedModels(charger))
        {
            Vector3 start;
            if (!original.TryGetValue(model, out start))
                return false;

            if (HorizontalDistance(start, model.transform.position) >
                maximum + 0.01f)
            {
                return false;
            }

            float before = targets.Min(t => DistancePointToSquad(start, t));
            float after = targets.Min(t => DistancePointToSquad(model.transform.position, t));

            if (after >= before - 0.001f)
                return false;

            bool canReachOne =
                targets.Any(
                    t => ChargingModelCanReachDistance(
                        charger,
                        model,
                        t,
                        start,
                        maximum,
                        1.0f));

            if (canReachOne && after > 1.05f)
                return false;

            bool canEngage =
                !canReachOne &&
                targets.Any(
                    t => ChargingModelCanReachDistance(
                        charger,
                        model,
                        t,
                        start,
                        maximum,
                        EngagementRange));

            if (canEngage && after > EngagementRange + 0.05f)
                return false;
        }

        return true;
    }

    private void V48FinishChargeState()
    {
        v48ChargeUnit = null;
        v48ChargeTargets.Clear();
        v48ChargeRawRoll = 0;
        v48ChargeCommandRerolled = false;
        v48ChargeFactionRerollResolved = false;
        v48ChargeHeroic = false;
        v48ChargeHeroicIntoFray = false;
    }

    private void V48ChooseHeroicMode(
        SquadController unit)
    {
        if (unit == null)
            return;

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        bool leapTargets = squads.Any(
            enemy =>
                enemy != null &&
                enemy.IsAlive &&
                enemy.IsOnBattlefield &&
                enemy.FactionId != unit.FactionId &&
                enemy.MadeChargeMove &&
                JoinedDistance(unit, enemy) <= 12.001f);

        bool frayTargets = squads.Any(
            enemy =>
                enemy != null &&
                enemy.IsAlive &&
                enemy.IsOnBattlefield &&
                enemy.FactionId != unit.FactionId &&
                JoinedDistance(unit, enemy) <= 6.001f);

        if (leapTargets && GetCommandPoints(unit.FactionId) >= 1)
        {
            options.Add(new RuleChoiceOption(
                "Leap to Defend (1CP)",
                () =>
                {
                    CloseRuleChoice();
                    V48BeginHeroicCharge(unit, false);
                }));
        }

        if (frayTargets && GetCommandPoints(unit.FactionId) >= 2)
        {
            options.Add(new RuleChoiceOption(
                "Into the Fray (2CP total)",
                () =>
                {
                    CloseRuleChoice();
                    V48BeginHeroicCharge(unit, true);
                }));
        }

        options.Add(new RuleChoiceOption(
            "Cancel",
            () =>
            {
                CloseRuleChoice();
                core11EndChargeWindowResolved = true;
            }));

        OpenRuleChoice(
            "HEROIC INTERVENTION MODE",
            "Choose the Heroic Intervention mode before the charge roll. Charge targets are selected after the roll.",
            options);
    }

    private void V48BeginHeroicCharge(
        SquadController unit,
        bool intoFray)
    {
        int cost = intoFray ? 2 : 1;
        if (!SpendFactionStratagemCP(unit, cost, "Heroic Intervention"))
        {
            core11EndChargeWindowResolved = true;
            return;
        }

        v48ChargeUnit = unit.JoinedActionController();
        v48ChargeTargets.Clear();
        v48ChargeHeroic = true;
        v48ChargeHeroicIntoFray = intoFray;
        v48ChargeCommandRerolled = true;
        v48ChargeFactionRerollResolved = true;
        core11EndChargeWindowResolved = true;

        Action<int> afterRoll =
            result =>
            {
                v48ChargeRawRoll = Mathf.Clamp(result, 2, 12);
                V48OpenChargeTargetSelection();
            };

        if (!IsXcomMode)
        {
            OpenTraditionalNumericPrompt(
                "HEROIC INTERVENTION CHARGE",
                "Roll 2D6. Into the Fray is capped at 6 only after modifiers. Enter the rolled total; select targets afterwards.",
                2,
                12,
                7,
                2,
                afterRoll);
            return;
        }

        afterRoll(RollChargeDice());
    }

    private bool V48OpenFireOverwatchWindow()
    {
        // WARBOARD_V54_TRADITIONAL_NO_OVERWATCH_POPUP
        if (!IsXcomMode)
        {
            v48EndMoveOverwatchResolved = true;
            return false;
        }
        if (phase != Phase.Move)
            return false;

        string defendingFaction =
            factions.FirstOrDefault(
                faction =>
                    !string.Equals(
                        faction,
                        activeFaction,
                        StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(defendingFaction))
        {
            v48EndMoveOverwatchResolved = true;
            return false;
        }

        List<SquadController> eligible =
            squads
                .Where(
                    unit =>
                        unit != null &&
                        unit.IsAlive &&
                        unit.IsOnBattlefield &&
                        !unit.IsAttachedLeader &&
                        unit.FactionId == defendingFaction &&
                        !unit.IsBattleShocked &&
                        !unit.HasKeyword("TITANIC") &&
                        !IsEngaged(unit) &&
                        GetCommandPoints(defendingFaction) >= 1 &&
                        V48OverwatchTargets(unit).Count > 0)
                .ToList();

        if (eligible.Count == 0)
        {
            v48EndMoveOverwatchResolved = true;
            return false;
        }

        List<RuleChoiceOption> options =
            new List<RuleChoiceOption>();

        foreach (SquadController unit in eligible)
        {
            SquadController captured = unit;
            options.Add(new RuleChoiceOption(
                "Fire Overwatch: " + captured.DisplayName + " (1CP)",
                () =>
                {
                    CloseRuleChoice();
                    V48ChooseOverwatchTarget(captured);
                }));
        }

        options.Add(new RuleChoiceOption(
            "No Fire Overwatch",
            () =>
            {
                CloseRuleChoice();
                v48EndMoveOverwatchResolved = true;
                status = "Fire Overwatch window passed. Press NEXT PHASE again.";
            }));

        OpenRuleChoice(
            "FIRE OVERWATCH - END OF MOVEMENT",
            "The defending player may spend 1CP. The selected unit must be unengaged, non-TITANIC, and within 24 inches of a visible enemy unit.",
            options);

        return true;
    }

    private List<SquadController> V48OverwatchTargets(
        SquadController shooter)
    {
        return squads
            .Where(
                enemy =>
                    enemy != null &&
                    enemy.IsAlive &&
                    enemy.IsOnBattlefield &&
                    !enemy.IsAttachedLeader &&
                    enemy.FactionId != shooter.FactionId &&
                    JoinedDistance(shooter, enemy) <= 24.001f &&
                    V48OverwatchTargetIsLegal(shooter, enemy) &&
                    V48GetOverwatchWeapons(shooter, enemy).Count > 0)
            .ToList();
    }

    private bool V48OverwatchTargetIsLegal(
        SquadController shooter,
        SquadController target)
    {
        if (shooter == null || target == null)
            return false;

        string reason;
        if (!WarboardV47FactionRules.CanAttackTarget(
                shooter,
                target,
                AttackMode.Ranged,
                out reason))
        {
            return false;
        }

        if (!Necrons11CanAttackTarget(
                shooter,
                target,
                AttackMode.Ranged,
                out reason))
        {
            return false;
        }

        if (!Custodes11CanAttackTarget(
                shooter,
                target,
                AttackMode.Ranged,
                out reason))
        {
            return false;
        }

        float distance = JoinedDistance(shooter, target);

        if (TargetHasLoneOperativeProtection(target) &&
            distance > 12.001f)
        {
            return false;
        }

        if (aeldariRules != null &&
            aeldariRules.HasRange18Protection(target) &&
            distance > 18.001f)
        {
            return false;
        }

        return true;
    }

    private List<WeaponAttackSelection> V48GetOverwatchWeapons(
        SquadController shooter,
        SquadController target)
    {
        List<WeaponAttackSelection> result =
            new List<WeaponAttackSelection>();

        foreach (ModelToken model in JoinedModels(shooter))
        {
            foreach (WeaponData weapon in model.RangedWeapons)
            {
                if (weapon == null ||
                    !model.CanUseWeapon(weapon))
                {
                    continue;
                }

                float effectiveRange = Mathf.Min(
                    24f,
                    weapon.range + AeldariRangedRangeModifier(shooter, weapon));

                if (ModelDistanceToSquad(model, target) > effectiveRange + 0.001f)
                    continue;

                if (!ModelHasLineOfSight(model, target))
                    continue;

                result.Add(new WeaponAttackSelection(model, weapon));
            }
        }

        return result;
    }

    private void V48ChooseOverwatchTarget(
        SquadController shooter)
    {
        List<SquadController> targets = V48OverwatchTargets(shooter);
        if (targets.Count == 0)
        {
            v48EndMoveOverwatchResolved = true;
            status = "Fire Overwatch has no legal visible target within 24 inches.";
            return;
        }

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (SquadController target in targets)
        {
            SquadController captured = target;
            options.Add(new RuleChoiceOption(
                captured.DisplayName,
                () =>
                {
                    CloseRuleChoice();
                    if (!SpendFactionStratagemCP(shooter, 1, "Fire Overwatch"))
                    {
                        v48EndMoveOverwatchResolved = true;
                        return;
                    }

                    shooter.SnapShootingActive = true;
                    List<WeaponAttackSelection> weapons =
                        V48GetOverwatchWeapons(shooter, captured);

                    interactiveAttackConsumesNormalAction = false;
                    interactiveAttackSuppressesPostReactions = true;
                    interactiveAttackCompletionCallback =
                        () =>
                        {
                            shooter.SnapShootingActive = false;
                            v48EndMoveOverwatchResolved = true;
                            status = "Fire Overwatch resolved. Press NEXT PHASE again.";
                        };

                    BeginInteractiveAttack(
                        shooter,
                        captured,
                        weapons,
                        AttackMode.Ranged,
                        false);
                }));
        }

        options.Add(new RuleChoiceOption(
            "Cancel",
            () =>
            {
                CloseRuleChoice();
                v48EndMoveOverwatchResolved = true;
            }));

        OpenRuleChoice(
            "FIRE OVERWATCH - SELECT TARGET",
            "Select one visible enemy unit within 24 inches. The Overwatch attack uses Snap Shooting.",
            options);
    }

    private bool V48CanUseExplosives(
        SquadController unit)
    {
        if (unit == null ||
            !unit.IsAlive ||
            !unit.IsOnBattlefield ||
            unit.HasShot ||
            unit.IsBattleShocked ||
            GetCommandPoints(unit.FactionId) < 1)
        {
            return false;
        }

        // The Core Stratagem requires the unit to be eligible to shoot.
        if (IsEngaged(unit))
            return false;

        if (missionSystem != null &&
            missionSystem.UnitHasStartedAction(unit) &&
            !unit.HasKeyword("TITANIC"))
        {
            return false;
        }

        if (unit.HasFallenBack &&
            !Necrons11CanShootAfterFallBack(unit) &&
            !Custodes11CanShootAfterFallBack(unit) &&
            !WarboardFactionExtensionHub.CanShootAfterFallBack(unit) &&
            !(aeldariRules != null &&
              aeldariRules.CanShootAfterFallBack(unit)))
        {
            return false;
        }

        if (unit.HasAdvanced)
        {
            bool canShootAfterAdvance =
                WarboardFactionExtensionHub.CanShootAfterAdvance(unit) ||
                (aeldariRules != null &&
                 aeldariRules.VehicleRangedHasAssault(unit)) ||
                JoinedModels(unit).Any(
                    model =>
                        model != null &&
                        model.RangedWeapons.Any(
                            weapon =>
                                weapon != null &&
                                WeaponRuleParser.Has(weapon, "assault")));

            if (!canShootAfterAdvance)
                return false;
        }

        return JoinedModels(unit).Any(
            model =>
                model != null &&
                model.IsAlive &&
                model.Squad != null &&
                (model.Squad.HasKeyword("EXPLOSIVES") ||
                 model.Squad.HasKeyword("GRENADES")));
    }

    private void V48UseExplosives(
        SquadController unit)
    {
        if (!V48CanUseExplosives(unit))
            return;

        List<ModelToken> explosiveModels =
            JoinedModels(unit)
                .Where(
                    model =>
                        model != null &&
                        model.IsAlive &&
                        model.Squad != null &&
                        (model.Squad.HasKeyword("EXPLOSIVES") ||
                         model.Squad.HasKeyword("GRENADES")))
                .ToList();

        List<RuleChoiceOption> modelOptions = new List<RuleChoiceOption>();
        foreach (ModelToken model in explosiveModels)
        {
            ModelToken capturedModel = model;
            modelOptions.Add(new RuleChoiceOption(
                capturedModel.RoleName,
                () =>
                {
                    CloseRuleChoice();
                    V48ChooseExplosivesTarget(unit, capturedModel);
                }));
        }
        modelOptions.Add(new RuleChoiceOption("Cancel", CloseRuleChoice));

        OpenRuleChoice(
            "EXPLOSIVES - SELECT MODEL",
            "Select the EXPLOSIVES model. The enemy target must be visible to and within 8 inches of this model.",
            modelOptions);
    }

    private void V48ChooseExplosivesTarget(
        SquadController unit,
        ModelToken explosiveModel)
    {
        List<SquadController> targets =
            squads
                .Where(
                    enemy =>
                        enemy != null &&
                        enemy.IsAlive &&
                        enemy.IsOnBattlefield &&
                        !enemy.IsAttachedLeader &&
                        enemy.FactionId != unit.FactionId &&
                        !IsEngaged(enemy) &&
                        ModelHasLineOfSight(explosiveModel, enemy) &&
                        enemy.JoinedLivingModelTokens().Any(
                            targetModel =>
                                targetModel != null &&
                                CoreRules11Terrain.ModelDistance(
                                    explosiveModel,
                                    targetModel) <= 8.001f))
                .ToList();

        if (targets.Count == 0)
        {
            status = "Explosives: the selected model has no visible unengaged enemy within 8 inches.";
            return;
        }

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (SquadController target in targets)
        {
            SquadController captured = target;
            options.Add(new RuleChoiceOption(
                captured.DisplayName,
                () =>
                {
                    CloseRuleChoice();
                    if (!SpendFactionStratagemCP(unit, 1, "Explosives"))
                        return;

                    Action<int> apply =
                        successes =>
                        {
                            Core11ApplyMortalWounds(
                                captured,
                                Mathf.Clamp(successes, 0, 6),
                                "Explosives",
                                unit);
                            status = "Explosives inflicted " +
                                Mathf.Clamp(successes, 0, 6) +
                                " mortal wound(s) on " + captured.DisplayName + ".";
                        };

                    if (!IsXcomMode)
                    {
                        OpenTraditionalNumericPrompt(
                            "EXPLOSIVES",
                            "Roll six D6 and enter the number of 4+ results.",
                            0, 6, 0, 6, apply);
                        return;
                    }

                    int successes = 0;
                    for (int i = 0; i < 6; i++)
                        if (DiceRoller.RollD6("Explosives") >= 4)
                            successes++;
                    apply(successes);
                }));
        }
        options.Add(new RuleChoiceOption("Cancel", CloseRuleChoice));

        OpenRuleChoice(
            "EXPLOSIVES - SELECT ENEMY",
            "Select one visible unengaged enemy within 8 inches of " + explosiveModel.RoleName + ".",
            options);
    }

    private void V48UseCrushingImpact(
        SquadController unit)
    {
        if (unit == null ||
            !unit.MadeChargeMove ||
            (!unit.HasKeyword("MONSTER") && !unit.HasKeyword("VEHICLE")) ||
            EngagedEnemies(unit).Count == 0 ||
            GetCommandPoints(unit.FactionId) < 1)
        {
            return;
        }

        List<ModelToken> engagedModels =
            JoinedModels(unit)
                .Where(
                    model =>
                        model != null &&
                        EngagedEnemies(unit).Any(
                            enemy =>
                                enemy.JoinedLivingModelTokens().Any(
                                    enemyModel =>
                                        CoreRules11Geometry.ModelsEngaged(model, enemyModel))))
                .ToList();

        if (engagedModels.Count == 0)
            return;

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (ModelToken model in engagedModels)
        {
            ModelToken captured = model;
            options.Add(new RuleChoiceOption(
                captured.RoleName + " - T" + captured.Squad.Toughness,
                () =>
                {
                    CloseRuleChoice();
                    V48ChooseCrushingImpactTarget(unit, captured);
                }));
        }
        options.Add(new RuleChoiceOption("Cancel", CloseRuleChoice));

        OpenRuleChoice(
            "CRUSHING IMPACT - SELECT ENGAGED MODEL",
            "Select one engaged model. The number of dice rolled equals that model's Toughness characteristic.",
            options);
    }

    private void V48ChooseCrushingImpactTarget(
        SquadController unit,
        ModelToken sourceModel)
    {
        List<SquadController> targets =
            EngagedEnemies(unit)
                .Where(
                    enemy =>
                        enemy.JoinedLivingModelTokens().Any(
                            enemyModel =>
                                CoreRules11Geometry.ModelsEngaged(sourceModel, enemyModel)))
                .ToList();

        List<RuleChoiceOption> options = new List<RuleChoiceOption>();
        foreach (SquadController enemy in targets)
        {
            SquadController captured = enemy;
            options.Add(new RuleChoiceOption(
                captured.DisplayName,
                () =>
                {
                    CloseRuleChoice();
                    if (!SpendFactionStratagemCP(unit, 1, "Crushing Impact"))
                        return;

                    int dice = Mathf.Max(1, sourceModel.Squad.Toughness);
                    Action<int, int> apply =
                        (selfWounds, enemyWounds) =>
                        {
                            Core11ApplyMortalWounds(unit, selfWounds, "Crushing Impact recoil");
                            Core11ApplyMortalWounds(
                                captured,
                                Mathf.Min(6, enemyWounds),
                                "Crushing Impact",
                                unit);
                            status = "Crushing Impact: " + selfWounds +
                                " self mortal wound(s), " +
                                Mathf.Min(6, enemyWounds) +
                                " enemy mortal wound(s).";
                        };

                    if (!IsXcomMode)
                    {
                        OpenTraditionalNumericPrompt(
                            "CRUSHING IMPACT - 1s",
                            "Roll " + dice + " D6 and enter how many are 1s.",
                            0, dice, 0, dice,
                            selfWounds =>
                                OpenTraditionalNumericPrompt(
                                    "CRUSHING IMPACT - 5+",
                                    "Using the same dice, enter how many are 5+ (maximum 6 inflicted).",
                                    0, dice, 0, dice,
                                    enemyWounds => apply(selfWounds, enemyWounds)));
                        return;
                    }

                    int self = 0;
                    int enemyWounds = 0;
                    for (int i = 0; i < dice; i++)
                    {
                        int roll = DiceRoller.RollD6("Crushing Impact");
                        if (roll == 1) self++;
                        if (roll >= 5) enemyWounds++;
                    }
                    apply(self, enemyWounds);
                }));
        }
        options.Add(new RuleChoiceOption("Cancel", CloseRuleChoice));

        OpenRuleChoice(
            "CRUSHING IMPACT - SELECT ENEMY",
            "Select one enemy unit engaged with the chosen model.",
            options);
    }
}

public static class WarboardV48CoreRules
{
    public static bool AllModelsMonsterOrVehicle(
        SquadController unit)
    {
        if (unit == null)
            return false;

        List<ModelToken> models =
            unit.JoinedActionController()
                .JoinedLivingModelTokens()
                .Where(model => model != null && model.IsAlive)
                .ToList();

        return models.Count > 0 &&
            models.All(
                model =>
                    model.Squad != null &&
                    (model.Squad.HasKeyword("MONSTER") ||
                     model.Squad.HasKeyword("VEHICLE")));
    }

    public static bool DenseSectionIsLow(
        TerrainFeature terrain)
    {
        if (terrain == null)
            return false;

        Collider collider = terrain.GetComponent<Collider>();
        if (collider == null)
            collider = terrain.GetComponentInChildren<Collider>();

        return collider != null &&
            collider.bounds.size.y <= 2.001f;
    }
}

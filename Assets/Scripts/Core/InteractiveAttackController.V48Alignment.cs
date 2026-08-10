using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// WARBOARD_V48_11E_ATTACK_ALIGNMENT
// 05.03/05.04 allocation + save order, optional Precision/Lethal Hits,
// one-die Command Re-roll selection, and mixed-unit Hazardous handling.
public partial class InteractiveAttackController
{
    private sealed class V48SaveEntry
    {
        public int Roll;
        public bool Precision;
        public int SourceIndex;
    }

    private sealed class V48AllocationGroup
    {
        public bool Character;
        public readonly List<ModelToken> Models = new List<ModelToken>();
        public int Wounds;
        public int Save;
        public int Invulnerable;

        public bool Alive
        {
            get { return Models.Any(model => model != null && model.IsAlive); }
        }

        public bool HasWoundedModel
        {
            get
            {
                return Models.Any(
                    model =>
                        model != null &&
                        model.IsAlive &&
                        model.CurrentWounds < model.MaxWounds);
            }
        }
    }

    private readonly Dictionary<InteractiveWeaponVolley, HashSet<int>>
        v48HitRerolled =
            new Dictionary<InteractiveWeaponVolley, HashSet<int>>();

    private readonly Dictionary<InteractiveWeaponVolley, HashSet<int>>
        v48WoundRerolled =
            new Dictionary<InteractiveWeaponVolley, HashSet<int>>();

    private readonly Dictionary<InteractiveWeaponVolley, int>
        v48LethalChoice =
            new Dictionary<InteractiveWeaponVolley, int>();

    private readonly Dictionary<InteractiveWeaponVolley, int>
        v48LethalCriticalCount =
            new Dictionary<InteractiveWeaponVolley, int>();

    private readonly Dictionary<InteractiveWeaponVolley, bool>
        v48PrecisionResolved =
            new Dictionary<InteractiveWeaponVolley, bool>();

    private readonly Dictionary<InteractiveWeaponVolley, ModelToken>
        v48PrecisionCharacter =
            new Dictionary<InteractiveWeaponVolley, ModelToken>();

    private int v48LethalDraft;
    private int v48RerollSelection;
    private int v48PrecisionCycle;

    private List<V48SaveEntry> v48SaveQueue = new List<V48SaveEntry>();
    private int v48SaveCursor;
    private List<V48AllocationGroup> v48AllocationOrder =
        new List<V48AllocationGroup>();
    private int v48AllocationIndex;

    private int v48DevastatingRemaining;
    private int v48PrecisionDevastatingRemaining;
    private ModelToken v48PendingDamageModel;
    private int v48PendingDamage;
    private bool v48PendingDamageDevastating;
    private readonly List<int> v48ResolvedDamageValues = new List<int>();
    private bool v48ResolutionInitialized;
    private bool v48HazardousResolved;

    public bool V48LethalDecisionPending
    {
        get
        {
            InteractiveWeaponVolley volley = CurrentVolley;
            if (volley == null ||
                stage != InteractiveAttackStage.ReviewHits ||
                !volley.lethalHits)
            {
                return false;
            }

            int criticals;
            if (!v48LethalCriticalCount.TryGetValue(volley, out criticals) ||
                criticals <= 0)
            {
                return false;
            }

            return !v48LethalChoice.ContainsKey(volley);
        }
    }

    public int V48LethalChoiceMaximum
    {
        get
        {
            InteractiveWeaponVolley volley = CurrentVolley;
            int value;
            return volley != null &&
                v48LethalCriticalCount.TryGetValue(volley, out value)
                ? value
                : 0;
        }
    }

    public int V48LethalDraft
    {
        get { return Mathf.Clamp(v48LethalDraft, 0, V48LethalChoiceMaximum); }
    }

    public void V48AdjustLethalDraft(int delta)
    {
        v48LethalDraft = Mathf.Clamp(
            v48LethalDraft + delta,
            0,
            V48LethalChoiceMaximum);
    }

    public void V48ConfirmLethalChoice()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null || !V48LethalDecisionPending)
            return;

        v48LethalChoice[volley] = V48LethalDraft;
        V48RecalculateHitResults();
        lastActionText =
            "Lethal Hits: " + V48LethalDraft +
            " Critical Hit(s) chosen to wound automatically; the rest proceed as normal hits.";
    }

    private List<ModelToken> V48VisibleCharacterGroups()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null ||
            volley.precisionNormalWounds + volley.precisionDevastatingWounds <= 0)
        {
            return new List<ModelToken>();
        }

        return target.JoinedLivingModelTokens()
            .Where(
                model =>
                    model != null &&
                    model.IsAlive &&
                    model.Squad != null &&
                    model.Squad.HasKeyword("CHARACTER") &&
                    volley.selections.Any(
                        selection =>
                            selection != null &&
                            selection.model != null &&
                            game.ModelCanSeeModel(selection.model, model)))
            .Distinct()
            .ToList();
    }

    public bool V48PrecisionDecisionPending
    {
        get
        {
            InteractiveWeaponVolley volley = CurrentVolley;
            if (volley == null ||
                stage != InteractiveAttackStage.ReviewWounds ||
                volley.precisionNormalWounds + volley.precisionDevastatingWounds <= 0 ||
                V48VisibleCharacterGroups().Count == 0)
            {
                return false;
            }

            bool resolved;
            return !v48PrecisionResolved.TryGetValue(volley, out resolved) || !resolved;
        }
    }

    public string V48PrecisionCandidateLabel
    {
        get
        {
            List<ModelToken> options = V48VisibleCharacterGroups();
            if (options.Count == 0)
                return "No visible Character";

            v48PrecisionCycle = Mathf.Clamp(v48PrecisionCycle, 0, options.Count - 1);
            ModelToken model = options[v48PrecisionCycle];
            return model.Squad.DisplayName + " - " + model.RoleName;
        }
    }

    public void V48CyclePrecisionCharacter(int delta)
    {
        List<ModelToken> options = V48VisibleCharacterGroups();
        if (options.Count == 0)
            return;

        v48PrecisionCycle = (v48PrecisionCycle + delta) % options.Count;
        if (v48PrecisionCycle < 0)
            v48PrecisionCycle += options.Count;
    }

    public void V48UsePrecisionCharacter()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        List<ModelToken> options = V48VisibleCharacterGroups();
        if (volley == null || options.Count == 0)
            return;

        v48PrecisionCycle = Mathf.Clamp(v48PrecisionCycle, 0, options.Count - 1);
        v48PrecisionCharacter[volley] = options[v48PrecisionCycle];
        v48PrecisionResolved[volley] = true;
        lastActionText =
            "Precision allocation selected: " + V48PrecisionCandidateLabel + ".";
    }

    public void V48DeclinePrecision()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null)
            return;

        v48PrecisionCharacter.Remove(volley);
        v48PrecisionResolved[volley] = true;
        lastActionText = "Precision not used; normal allocation order applies.";
    }

    public void V48MarkHitRerolled(
        InteractiveWeaponVolley volley,
        int index)
    {
        if (volley == null || index < 0)
            return;

        HashSet<int> set;
        if (!v48HitRerolled.TryGetValue(volley, out set))
        {
            set = new HashSet<int>();
            v48HitRerolled[volley] = set;
        }
        set.Add(index);
    }

    public void V48MarkWoundRerolled(
        InteractiveWeaponVolley volley,
        int index)
    {
        if (volley == null || index < 0)
            return;

        HashSet<int> set;
        if (!v48WoundRerolled.TryGetValue(volley, out set))
        {
            set = new HashSet<int>();
            v48WoundRerolled[volley] = set;
        }
        set.Add(index);
    }

    private List<int> V48EligibleRerollIndices()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        List<int> result = new List<int>();
        if (volley == null)
            return result;

        if (stage == InteractiveAttackStage.ReviewHits)
        {
            if (volley.hitCommandRerollUsed ||
                volley.torrent ||
                volley.cannotRerollHits)
                return result;

            HashSet<int> rerolled;
            v48HitRerolled.TryGetValue(volley, out rerolled);
            for (int i = 0; i < volley.hitRolls.Count; i++)
                if (rerolled == null || !rerolled.Contains(i))
                    result.Add(i);
            return result;
        }

        if (stage == InteractiveAttackStage.ReviewWounds)
        {
            if (volley.woundCommandRerollUsed)
                return result;

            HashSet<int> rerolled;
            v48WoundRerolled.TryGetValue(volley, out rerolled);
            for (int i = 0; i < volley.woundRolls.Count; i++)
                if (rerolled == null || !rerolled.Contains(i))
                    result.Add(i);
            return result;
        }

        if (stage == InteractiveAttackStage.ReviewSaves)
        {
            if (volley.saveCommandRerollUsed)
                return result;

            for (int i = 0; i < volley.saveRolls.Count; i++)
                result.Add(i);
            return result;
        }

        if (stage == InteractiveAttackStage.ReviewDamage)
        {
            if (volley.damageCommandRerollUsed ||
                v48PendingDamageModel == null ||
                string.IsNullOrWhiteSpace(volley.weapon.damageExpression))
                return result;

            result.Add(0);
        }

        return result;
    }

    public string V48SelectedRerollLabel
    {
        get
        {
            List<int> eligible = V48EligibleRerollIndices();
            if (eligible.Count == 0)
                return "No eligible die";

            v48RerollSelection = Mathf.Clamp(v48RerollSelection, 0, eligible.Count - 1);
            int index = eligible[v48RerollSelection];
            IReadOnlyList<int> dice = CurrentDice;
            int value = index >= 0 && index < dice.Count ? dice[index] : 0;
            return "die " + (index + 1) + " = " + value;
        }
    }

    public void V48CycleCommandRerollDie(int delta)
    {
        List<int> eligible = V48EligibleRerollIndices();
        if (eligible.Count == 0)
            return;

        v48RerollSelection = (v48RerollSelection + delta) % eligible.Count;
        if (v48RerollSelection < 0)
            v48RerollSelection += eligible.Count;
    }

    private int V48FindRerollableDieIndex()
    {
        List<int> eligible = V48EligibleRerollIndices();
        if (eligible.Count == 0)
            return -1;

        v48RerollSelection = Mathf.Clamp(v48RerollSelection, 0, eligible.Count - 1);
        return eligible[v48RerollSelection];
    }

    private void V48RecalculateHitResults()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null)
            return;

        WarboardAttackDieLedger47.ClearAttackStage(
            attacker,
            target,
            volley.weapon,
            WarboardAttackDieStage47.Hit);

        int hits = 0;
        int criticalCount = 0;
        int precisionCriticalHits = 0;
        int chosenLethalForLedger = 0;
        int existingLethalChoice;
        if (v48LethalChoice.TryGetValue(volley, out existingLethalChoice))
            chosenLethalForLedger = Mathf.Max(0, existingLethalChoice);

        ModelToken sourceModel =
            volley.selections.Count > 0 ? volley.selections[0].model : null;

        foreach (int roll in volley.hitRolls)
        {
            bool success = false;
            if (roll != 1 &&
                (volley.minimumUnmodifiedHit <= 0 ||
                 roll >= volley.minimumUnmodifiedHit))
            {
                int modified = roll + volley.hitRollModifier;
                success = roll == 6 || modified >= volley.skill;
            }

            bool critical =
                WarboardV47FactionRules.IsCriticalHit(attacker, roll, success);
            bool precision =
                volley.precision ||
                (volley.precisionOnCriticalHit && critical);

            bool chosenLethal =
                success && critical && volley.lethalHits && chosenLethalForLedger > 0;
            if (chosenLethal)
                chosenLethalForLedger--;

            WarboardAttackDieLedger47.RecordHit(
                game,
                attacker,
                target,
                sourceModel,
                volley.weapon,
                mode,
                roll,
                success,
                critical,
                false,
                precision,
                chosenLethal,
                critical ? volley.sustainedHits : 0);

            if (!success)
                continue;

            hits++;
            if (critical)
            {
                criticalCount++;
                if (precision)
                    precisionCriticalHits++;
                if (volley.sustainedHits > 0)
                    hits += volley.sustainedHits;
            }
        }

        v48LethalCriticalCount[volley] =
            volley.lethalHits ? criticalCount : 0;

        int lethal = 0;
        if (volley.lethalHits)
        {
            int chosen;
            if (v48LethalChoice.TryGetValue(volley, out chosen))
                lethal = Mathf.Clamp(chosen, 0, criticalCount);
            else
                v48LethalDraft = criticalCount;
        }

        volley.hits = hits;
        volley.lethalAutoWounds = lethal;
        volley.precisionCriticalHits = precisionCriticalHits;
        volley.precisionLethalAutoWounds =
            (volley.precision || volley.precisionOnCriticalHit)
            ? Mathf.Min(lethal, precisionCriticalHits)
            : 0;
    }

    private void V48RollSaves()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null)
            return;

        volley.saveRolls.Clear();
        volley.saveTargetsPerDie.Clear();
        volley.savePrecisionFlags.Clear();
        volley.failedSavePrecisionFlags.Clear();
        volley.failedSaves = 0;

        if (volley.normalWounds <= 0)
        {
            lastActionText = "No normal save rolls required.";
            stage = InteractiveAttackStage.ReviewSaves;
            return;
        }

        int precisionCount = Mathf.Clamp(
            volley.precisionNormalWounds,
            0,
            volley.normalWounds);

        for (int i = 0; i < volley.normalWounds; i++)
        {
            bool precision = i < precisionCount;
            int roll = DiceRoller.RollD6(
                "Save roll: " + target.DisplayName);
            volley.saveRolls.Add(roll);
            volley.savePrecisionFlags.Add(precision);
        }

        // 05.04: the rolls are made together. They are not attached to a
        // specific model/save profile yet; allocation is evaluated only when
        // resolving them from lowest result to highest.
        lastActionText =
            "Rolled " + volley.normalWounds +
            " save dice. They will resolve lowest-to-highest against the current allocation group.";
        stage = InteractiveAttackStage.ReviewSaves;
    }

    private void V48RecalculateSaveResults()
    {
        // Save success is deliberately not precomputed. 11e applies each raw
        // result to whichever allocation group is current when that result is
        // reached, so a model death can change the profile used by later dice.
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley != null)
            volley.failedSaves = 0;
    }

    private int V48InvulnerableFor(ModelToken model)
    {
        if (model == null || model.Squad == null)
            return 0;

        int inv = model.InvulnerableSave;
        int aeldari = game != null
            ? game.AeldariInvulnerableOverride(model.Squad)
            : 0;
        int standard = game != null
            ? WarboardFactionExtensionHub.InvulnerableOverride(game, model.Squad)
            : 0;

        if (aeldari > 0)
            inv = inv > 0 ? Mathf.Min(inv, aeldari) : aeldari;
        if (standard > 0)
            inv = inv > 0 ? Mathf.Min(inv, standard) : standard;
        return inv;
    }

    private List<V48AllocationGroup> V48BuildAllocationOrder()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        SquadController attackOwner =
            volley != null && volley.selections.Count > 0 &&
            volley.selections[0].model != null
            ? volley.selections[0].model.Squad
            : attacker;

        List<V48AllocationGroup> groups = new List<V48AllocationGroup>();
        List<ModelToken> living =
            target.JoinedLivingModelTokens()
                .Where(model => model != null && model.IsAlive)
                .ToList();

        foreach (ModelToken character in living.Where(
            model => model.Squad != null && model.Squad.HasKeyword("CHARACTER")))
        {
            V48AllocationGroup group = new V48AllocationGroup();
            group.Character = true;
            group.Models.Add(character);
            group.Wounds = character.MaxWounds;
            group.Save = character.Squad.GetSave(attackOwner);
            group.Invulnerable = V48InvulnerableFor(character);
            groups.Add(group);
        }

        IEnumerable<IGrouping<string, ModelToken>> nonCharacterGroups =
            living
                .Where(model => model.Squad == null || !model.Squad.HasKeyword("CHARACTER"))
                .GroupBy(
                    model =>
                        model.MaxWounds + "|" +
                        model.Squad.GetSave(attackOwner) + "|" +
                        V48InvulnerableFor(model));

        foreach (IGrouping<string, ModelToken> models in nonCharacterGroups)
        {
            ModelToken first = models.First();
            V48AllocationGroup group = new V48AllocationGroup();
            group.Character = false;
            group.Models.AddRange(models);
            group.Wounds = first.MaxWounds;
            group.Save = first.Squad.GetSave(attackOwner);
            group.Invulnerable = V48InvulnerableFor(first);
            groups.Add(group);
        }

        // 05.03: wounded non-Character group first; all non-Character groups
        // before Character groups; wounded Characters before unwounded ones.
        return groups
            .OrderBy(group => group.Character ? 1 : 0)
            .ThenBy(group => group.Character ? (group.HasWoundedModel ? 0 : 1) : (group.HasWoundedModel ? 0 : 1))
            .ThenBy(group => group.Save)
            .ThenBy(group => group.Invulnerable <= 0 ? 7 : group.Invulnerable)
            .ThenBy(group => group.Wounds)
            .ToList();
    }

    private V48AllocationGroup V48CurrentNormalGroup()
    {
        while (v48AllocationIndex < v48AllocationOrder.Count &&
               !v48AllocationOrder[v48AllocationIndex].Alive)
        {
            v48AllocationIndex++;
        }

        return v48AllocationIndex < v48AllocationOrder.Count
            ? v48AllocationOrder[v48AllocationIndex]
            : null;
    }

    private ModelToken V48ModelFromGroup(V48AllocationGroup group)
    {
        if (group == null)
            return null;

        ModelToken wounded = group.Models.FirstOrDefault(
            model =>
                model != null &&
                model.IsAlive &&
                model.CurrentWounds < model.MaxWounds);
        if (wounded != null)
            return wounded;

        return group.Models.FirstOrDefault(
            model => model != null && model.IsAlive);
    }

    private ModelToken V48PrecisionModel()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        ModelToken model;
        if (volley != null &&
            v48PrecisionCharacter.TryGetValue(volley, out model) &&
            model != null && model.IsAlive)
        {
            return model;
        }
        return null;
    }

    private bool V48SaveSucceeds(
        int rawRoll,
        ModelToken allocated)
    {
        if (allocated == null || allocated.Squad == null)
            return true;

        int inv = V48InvulnerableFor(allocated);
        if (inv > 0 && rawRoll >= inv)
            return true;

        SquadController attackOwner =
            CurrentVolley.selections.Count > 0 &&
            CurrentVolley.selections[0].model != null
            ? CurrentVolley.selections[0].model.Squad
            : attacker;

        int save = allocated.Squad.GetSave(attackOwner);
        return rawRoll + CurrentVolley.effectiveAp >= save;
    }

    private void V48InitializeDamageResolution()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        v48SaveQueue.Clear();
        v48SaveCursor = 0;
        v48AllocationOrder = V48BuildAllocationOrder();
        v48AllocationIndex = 0;
        v48PendingDamageModel = null;
        v48PendingDamage = 0;
        v48PendingDamageDevastating = false;
        v48ResolvedDamageValues.Clear();
        v48ResolutionInitialized = true;

        for (int i = 0; i < volley.saveRolls.Count; i++)
        {
            v48SaveQueue.Add(
                new V48SaveEntry
                {
                    Roll = volley.saveRolls[i],
                    Precision = i < volley.savePrecisionFlags.Count && volley.savePrecisionFlags[i],
                    SourceIndex = i
                });
        }

        // Precision is a separate attack pool. Resolve it first if the attacker
        // chose to use Precision, then resolve the normal pool. Within each pool
        // save results are resolved lowest-to-highest.
        v48SaveQueue = v48SaveQueue
            .OrderBy(entry => entry.Precision ? 0 : 1)
            .ThenBy(entry => entry.Roll)
            .ThenBy(entry => entry.SourceIndex)
            .ToList();

        v48DevastatingRemaining = volley.devastatingWounds;
        v48PrecisionDevastatingRemaining =
            Mathf.Clamp(
                volley.precisionDevastatingWounds,
                0,
                volley.devastatingWounds);

        volley.failedSaves = 0;
        volley.damageValues.Clear();
        volley.woundsLost = 0;
        volley.modelsKilled = 0;
    }

    private int V48RollOneDamage()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        int damage = RollDamageCharacteristic(
            volley.weapon,
            "Damage: " + volley.weapon.displayName);

        damage += volley.meltaBonus;
        damage += game != null
            ? game.AeldariDamageModifier(attacker, volley.weapon, mode)
            : 0;
        damage += NecronsFactionPack11.DamageModifier(
            attacker,
            volley.selections.Count > 0 ? volley.selections[0].model : null,
            volley.weapon,
            mode);

        return Mathf.Max(0, damage);
    }

    private void V48PreparePendingDamage(
        ModelToken model,
        bool devastating)
    {
        v48PendingDamageModel = model;
        v48PendingDamageDevastating = devastating;
        v48PendingDamage = V48RollOneDamage();

        InteractiveWeaponVolley volley = CurrentVolley;
        volley.damageValues.Clear();
        volley.damageValues.Add(v48PendingDamage);
        volley.damageCommandRerollUsed = false;
        v48RerollSelection = 0;

        lastActionText =
            (devastating ? "Devastating Wounds" : "Failed save") +
            " allocated to " +
            (model != null ? model.RoleName : "model") +
            ": Damage " + v48PendingDamage + ".";
        stage = InteractiveAttackStage.ReviewDamage;
    }

    private void V48ResolveUntilDamageDecision()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null)
            return;

        while (v48SaveCursor < v48SaveQueue.Count)
        {
            V48SaveEntry entry = v48SaveQueue[v48SaveCursor++];
            ModelToken allocated =
                entry.Precision ? V48PrecisionModel() : null;

            if (allocated == null)
                allocated = V48ModelFromGroup(V48CurrentNormalGroup());

            if (allocated == null)
            {
                V48FinishVolleyResolution();
                return;
            }

            if (V48SaveSucceeds(entry.Roll, allocated))
                continue;

            volley.failedSaves++;
            V48PreparePendingDamage(allocated, false);
            return;
        }

        if (v48DevastatingRemaining > 0)
        {
            bool precision = v48PrecisionDevastatingRemaining > 0;
            ModelToken allocated = precision ? V48PrecisionModel() : null;
            if (allocated == null)
                allocated = V48ModelFromGroup(V48CurrentNormalGroup());

            if (allocated == null)
            {
                V48FinishVolleyResolution();
                return;
            }

            v48DevastatingRemaining--;
            if (precision && v48PrecisionDevastatingRemaining > 0)
                v48PrecisionDevastatingRemaining--;

            V48PreparePendingDamage(allocated, true);
            return;
        }

        V48FinishVolleyResolution();
    }

    private int V48ModifyIncomingDamage(
        ModelToken allocated,
        int attackDamage,
        bool normalAttackDamage)
    {
        InteractiveWeaponVolley volley = CurrentVolley;

        if (UniversalRuleRegistry.UnitHasRule(
                allocated.Squad,
                "Implacable Resilience"))
        {
            attackDamage = Mathf.Max(1, attackDamage - 1);
        }

        attackDamage = CustodesFactionPack11.ModifyIncomingDamage(
            allocated,
            attacker,
            volley.weapon,
            attackDamage,
            normalAttackDamage);

        attackDamage = NecronsFactionPack11.ModifyIncomingDamage(
            allocated.Squad,
            attackDamage);

        return Mathf.Max(0, attackDamage);
    }

    private void V48ApplyPendingDamageAndContinue()
    {
        if (!v48ResolutionInitialized ||
            v48PendingDamageModel == null)
        {
            V48ResolveUntilDamageDecision();
            return;
        }

        InteractiveWeaponVolley volley = CurrentVolley;
        ModelToken allocated = v48PendingDamageModel;
        int damage = volley.damageValues.Count > 0
            ? volley.damageValues[0]
            : v48PendingDamage;

        damage = V48ModifyIncomingDamage(
            allocated,
            damage,
            !v48PendingDamageDevastating);

        int incoming = Mathf.Min(
            allocated.CurrentWounds,
            damage);
        int afterFnp = UniversalRuleRegistry.ApplyFeelNoPain(
            allocated.Squad,
            incoming,
            v48PendingDamageDevastating
                ? "Devastating Wounds: " + volley.weapon.displayName
                : volley.weapon.displayName);

        bool wasAlive = allocated.IsAlive;
        int lost = allocated.ApplyDamage(afterFnp);
        volley.woundsLost += lost;
        v48ResolvedDamageValues.Add(damage);

        if (wasAlive && !allocated.IsAlive)
        {
            volley.modelsKilled++;
            if (!destroyedModels.Contains(allocated))
                destroyedModels.Add(allocated);

            game.RecordModelDestroyed(allocated, attacker);
            game.ResolveDeadlyDemise(allocated);
        }

        target.RefreshVisuals();
        if (target.AttachedLeader != null)
            target.AttachedLeader.RefreshVisuals();

        v48PendingDamageModel = null;
        v48PendingDamage = 0;
        v48PendingDamageDevastating = false;
        volley.damageValues.Clear();

        V48ResolveUntilDamageDecision();
    }

    private void V48FinishVolleyResolution()
    {
        InteractiveWeaponVolley volley = CurrentVolley;
        if (volley == null)
            return;

        volley.damageValues.Clear();
        volley.damageValues.AddRange(v48ResolvedDamageValues);

        totalWoundsLost += volley.woundsLost;
        totalModelsKilled += volley.modelsKilled;

        v48ResolutionInitialized = false;
        lastActionText =
            "Applied " + volley.woundsLost +
            " wound(s); " + volley.modelsKilled +
            " model(s) destroyed. Saves were resolved lowest-to-highest using live allocation groups.";
        stage = InteractiveAttackStage.WeaponComplete;
    }

    private void V48RollDamageCompatibility()
    {
        if (!v48ResolutionInitialized)
            V48InitializeDamageResolution();
        V48ResolveUntilDamageDecision();
    }

    private void V48ApplyDamageCompatibility()
    {
        if (!v48ResolutionInitialized)
            V48InitializeDamageResolution();

        if (v48PendingDamageModel != null)
            V48ApplyPendingDamageAndContinue();
        else
            V48ResolveUntilDamageDecision();
    }

    private void V48AdvanceToNextVolley()
    {
        if (!target.IsAlive &&
            (target.AttachedLeader == null || !target.AttachedLeader.IsAlive))
        {
            V48ResolveHazardousAll();
            stage = InteractiveAttackStage.AttackComplete;
            return;
        }

        volleyIndex++;
        v48ResolutionInitialized = false;
        v48RerollSelection = 0;
        v48PrecisionCycle = 0;

        if (volleyIndex >= volleys.Count)
        {
            V48ResolveHazardousAll();
            stage = InteractiveAttackStage.AttackComplete;
            return;
        }

        stage = InteractiveAttackStage.RollHits;
        lastActionText = "Next weapon profile.";
    }

    private void V48ResolveHazardousAll()
    {
        if (v48HazardousResolved)
            return;
        v48HazardousResolved = true;

        List<WeaponAttackSelection> hazardous =
            volleys
                .SelectMany(volley => volley.selections)
                .Where(
                    selection =>
                        selection != null &&
                        selection.weapon != null &&
                        WeaponRuleParser.Has(selection.weapon, "hazardous"))
                .ToList();

        if (hazardous.Count == 0)
            return;

        bool heavyUnit = WarboardV48CoreRules.AllModelsMonsterOrVehicle(attacker);
        List<int> rolls = DiceRoller.RollDice(
            hazardous.Count,
            6,
            "Hazardous - all used weapons").Results.ToList();

        int failures = rolls.Count(value => value <= 2);
        int woundsPerFailure = heavyUnit ? 3 : 1;

        for (int failure = 0; failure < failures; failure++)
        {
            int remaining = woundsPerFailure;
            while (remaining > 0 && attacker.IsAlive)
            {
                ModelToken model = attacker.GetAutomaticAllocationModel();
                if (model == null && attacker.AttachedLeader != null)
                    model = attacker.AttachedLeader.GetAutomaticAllocationModel();
                if (model == null)
                    break;

                int afterFnp = UniversalRuleRegistry.ApplyFeelNoPain(
                    model.Squad,
                    1,
                    "Hazardous");
                if (afterFnp > 0)
                {
                    bool alive = model.IsAlive;
                    model.ApplyDamage(1);
                    if (alive && !model.IsAlive)
                    {
                        game.RecordModelDestroyed(model, attacker);
                        game.ResolveDeadlyDemise(model);
                    }
                }
                remaining--;
            }
        }

        attacker.RefreshVisuals();
        if (attacker.AttachedLeader != null)
            attacker.AttachedLeader.RefreshVisuals();
    }

    private void V48Continue()
    {
        if (CurrentVolley == null)
        {
            stage = InteractiveAttackStage.AttackComplete;
            return;
        }

        switch (stage)
        {
            case InteractiveAttackStage.ReviewHits:
                if (V48LethalDecisionPending)
                    return;

                WarboardAttackDieLedger47.EmitStageEvents(
                    game,
                    attacker,
                    target,
                    CurrentVolley.weapon,
                    WarboardAttackDieStage47.Hit);
                stage = InteractiveAttackStage.RollWounds;
                break;

            case InteractiveAttackStage.ReviewWounds:
                if (V48PrecisionDecisionPending)
                    return;

                WarboardAttackDieLedger47.EmitStageEvents(
                    game,
                    attacker,
                    target,
                    CurrentVolley.weapon,
                    WarboardAttackDieStage47.Wound);

                if (CurrentVolley.normalWounds > 0)
                {
                    stage = InteractiveAttackStage.RollSaves;
                }
                else
                {
                    V48InitializeDamageResolution();
                    V48ResolveUntilDamageDecision();
                }
                break;

            case InteractiveAttackStage.ReviewSaves:
                V48InitializeDamageResolution();
                V48ResolveUntilDamageDecision();
                break;

            case InteractiveAttackStage.ReviewDamage:
                stage = InteractiveAttackStage.ApplyDamage;
                break;

            case InteractiveAttackStage.WeaponComplete:
                V48AdvanceToNextVolley();
                break;
        }
    }

    private bool V48DeclineDecisionAndFastResolve()
    {
        if (V48LethalDecisionPending)
        {
            v48LethalDraft = V48LethalChoiceMaximum;
            V48ConfirmLethalChoice();
        }
        else if (V48PrecisionDecisionPending)
        {
            V48DeclinePrecision();
        }
        else if (CanUsePartingTheVeil || CanUseMacabreResilience)
        {
            factionDefensiveReactionResolved = true;
            lastActionText = "Defensive faction reaction declined.";
        }
        else if (CanCommandReroll)
        {
            V48Continue();
        }

        return FastResolveUntilDecision();
    }

    private bool V48UseCommandReroll()
    {
        if (!CanCommandReroll)
            return false;

        SquadController rerollUnit =
            stage == InteractiveAttackStage.ReviewSaves
            ? target
            : attacker;

        if (!game.SpendStratagemCPForUnit(
                rerollUnit,
                1,
                "Command Re-roll"))
        {
            return false;
        }

        int index = V48FindRerollableDieIndex();
        if (index < 0)
            return false;

        InteractiveWeaponVolley volley = CurrentVolley;

        switch (stage)
        {
            case InteractiveAttackStage.ReviewHits:
            {
                int old = volley.hitRolls[index];
                int value = DiceRoller.RollD6(
                    "Command Re-roll Hit: " + volley.weapon.displayName);
                volley.hitRolls[index] = value;
                V48MarkHitRerolled(volley, index);
                volley.hitCommandRerollUsed = true;
                v48LethalChoice.Remove(volley);
                V48RecalculateHitResults();
                lastActionText = "Command Re-roll hit die " + (index + 1) +
                    ": " + old + " -> " + value;
                break;
            }

            case InteractiveAttackStage.ReviewWounds:
            {
                int old = volley.woundRolls[index];
                int value = DiceRoller.RollD6(
                    "Command Re-roll Wound: " + volley.weapon.displayName);
                volley.woundRolls[index] = value;
                V48MarkWoundRerolled(volley, index);
                volley.woundCommandRerollUsed = true;
                RecalculateWoundResults();
                v48PrecisionResolved.Remove(volley);
                v48PrecisionCharacter.Remove(volley);
                lastActionText = "Command Re-roll wound die " + (index + 1) +
                    ": " + old + " -> " + value;
                break;
            }

            case InteractiveAttackStage.ReviewSaves:
            {
                int old = volley.saveRolls[index];
                int value = DiceRoller.RollD6(
                    "Command Re-roll Save: " + target.DisplayName);
                volley.saveRolls[index] = value;
                volley.saveCommandRerollUsed = true;
                V48RecalculateSaveResults();
                lastActionText = "Command Re-roll save die " + (index + 1) +
                    ": " + old + " -> " + value;
                break;
            }

            case InteractiveAttackStage.ReviewDamage:
            {
                int old = volley.damageValues[0];
                int value = V48RollOneDamage();
                volley.damageValues[0] = value;
                v48PendingDamage = value;
                volley.damageCommandRerollUsed = true;
                lastActionText = "Command Re-roll damage: " + old + " -> " + value;
                break;
            }

            default:
                return false;
        }

        return true;
    }
}

using UnityEngine;

public partial class SquadController
{
    public WarboardSquadRuntimeSnapshot
        CaptureMultiplayerRuntime()
    {
        return
            new WarboardSquadRuntimeSnapshot
            {
                battlefieldState =
                    (int)BattlefieldState,

                temporaryDeepStrike =
                    TemporaryDeepStrike,

                isRepositionedReserve =
                    IsRepositionedReserve,

                hasMoved = HasMoved,
                hasShot = HasShot,
                hasCharged = HasCharged,
                hasFought = HasFought,
                hasFallenBack = HasFallenBack,
                hasAdvanced = HasAdvanced,
                advanceBonus = AdvanceBonus,
                wasSetUpThisTurn =
                    WasSetUpThisTurn,
                madeChargeMove =
                    MadeChargeMove,

                startedMissionActionThisTurn =
                    StartedMissionActionThisTurn,

                isPerformingMissionAction =
                    IsPerformingMissionAction,

                activeMissionActionId =
                    ActiveMissionActionId ?? "",

                isBattleShocked =
                    IsBattleShocked,

                lastBattleShockRoll =
                    LastBattleShockRoll,

                factionSoulsightActive =
                    FactionSoulsightActive,

                factionMacabreResilienceActive =
                    FactionMacabreResilienceActive,

                factionEmissariesRerollOnes =
                    FactionEmissariesRerollOnes,

                factionEmissariesRerollAll =
                    FactionEmissariesRerollAll,

                factionHungryVoidActive =
                    FactionHungryVoidActive,

                factionSuddenStormActive =
                    FactionSuddenStormActive,

                factionConqueringTyrantActive =
                    FactionConqueringTyrantActive,

                targetedByFactionStratagemThisPhase =
                    TargetedByFactionStratagemThisPhase,

                temporaryFightsFirst =
                    TemporaryFightsFirst,

                partingTheVeilActive =
                    PartingTheVeilActive,

                aeldariOffensiveHitModifier =
                    AeldariOffensiveHitModifier,

                aeldariOffensiveWoundModifier =
                    AeldariOffensiveWoundModifier,

                aeldariDefensiveHitModifier =
                    AeldariDefensiveHitModifier,

                aeldariDefensiveWoundModifier =
                    AeldariDefensiveWoundModifier,

                aeldariApModifier =
                    AeldariApModifier,

                aeldariDamageModifier =
                    AeldariDamageModifier,

                aeldariInvulnerableOverride =
                    AeldariInvulnerableOverride,

                aeldariSustainedHits =
                    AeldariSustainedHits,

                aeldariLethalHits =
                    AeldariLethalHits,

                aeldariDevastatingWounds =
                    AeldariDevastatingWounds,

                aeldariIgnoresCover =
                    AeldariIgnoresCover,

                aeldariRerollHitOnes =
                    AeldariRerollHitOnes,

                aeldariRerollWoundOnes =
                    AeldariRerollWoundOnes,

                aeldariRerollAllHits =
                    AeldariRerollAllHits,

                aeldariRerollAllWounds =
                    AeldariRerollAllWounds,

                aeldariPathChoiceMadeThisPhase =
                    AeldariPathChoiceMadeThisPhase,

                aeldariCanShootAfterFallBack =
                    AeldariCanShootAfterFallBack,

                aeldariCanChargeAfterFallBack =
                    AeldariCanChargeAfterFallBack,

                aeldariCanChargeAfterAdvance =
                    AeldariCanChargeAfterAdvance,

                aeldariRange18Protection =
                    AeldariRange18Protection,

                aeldariVectoredEnginesActive =
                    AeldariVectoredEnginesActive,

                aeldariVengefulDeadTokens =
                    AeldariVengefulDeadTokens,

                aeldariObjectiveControlOverride =
                    AeldariObjectiveControlOverride,

                agileManoeuvreUsedThisPhase =
                    AgileManoeuvreUsedThisPhase,

                flittingShadowsActive =
                    FlittingShadowsActive,

                starEnginesActive =
                    StarEnginesActive,

                suddenStrikeActive =
                    SuddenStrikeActive,

                snapShootingActive =
                    SnapShootingActive,

                katahSustainedActive =
                    KatahSustainedActive,

                katahLethalActive =
                    KatahLethalActive,

                katahChoiceMadeThisFight =
                    KatahChoiceMadeThisFight,

                battleFocusMoveBonus =
                    BattleFocusMoveBonus,

                resurrectionOrbUsed =
                    ResurrectionOrbUsed,

                eternalRevenantUsed =
                    EternalRevenantUsed,

                veilOfDarknessUsed =
                    VeilOfDarknessUsed,

                mustIngressFromVeil =
                    MustIngressFromVeil,

                tearsOfIshaUsedThisTurn =
                    TearsOfIshaUsedThisTurn,

                spiritMarkUsedThisTurn =
                    SpiritMarkUsedThisTurn,

                myWillBeDoneUsedRound =
                    MyWillBeDoneUsedRound
            };
    }

    public void ApplyMultiplayerRuntime(
        WarboardSquadRuntimeSnapshot state,
        Vector3 rootPosition,
        Quaternion rootRotation)
    {
        if (state == null)
            return;

        transform.position =
            rootPosition;

        transform.rotation =
            rootRotation;

        BattlefieldState =
            (SquadBattlefieldState)
                Mathf.Clamp(
                    state.battlefieldState,
                    0,
                    3
                );

        TemporaryDeepStrike =
            state.temporaryDeepStrike;

        IsRepositionedReserve =
            state.isRepositionedReserve;

        HasMoved = state.hasMoved;
        HasShot = state.hasShot;
        HasCharged = state.hasCharged;
        HasFought = state.hasFought;
        HasFallenBack = state.hasFallenBack;

        HasAdvanced = state.hasAdvanced;
        AdvanceBonus = state.advanceBonus;
        WasSetUpThisTurn =
            state.wasSetUpThisTurn;
        MadeChargeMove =
            state.madeChargeMove;

        StartedMissionActionThisTurn =
            state.startedMissionActionThisTurn;

        IsPerformingMissionAction =
            state.isPerformingMissionAction;

        ActiveMissionActionId =
            state.activeMissionActionId ??
            "";

        IsBattleShocked =
            state.isBattleShocked;

        LastBattleShockRoll =
            state.lastBattleShockRoll;

        FactionSoulsightActive =
            state.factionSoulsightActive;

        FactionMacabreResilienceActive =
            state.factionMacabreResilienceActive;

        FactionEmissariesRerollOnes =
            state.factionEmissariesRerollOnes;

        FactionEmissariesRerollAll =
            state.factionEmissariesRerollAll;

        FactionHungryVoidActive =
            state.factionHungryVoidActive;

        FactionSuddenStormActive =
            state.factionSuddenStormActive;

        FactionConqueringTyrantActive =
            state.factionConqueringTyrantActive;

        TargetedByFactionStratagemThisPhase =
            state.targetedByFactionStratagemThisPhase;

        TemporaryFightsFirst =
            state.temporaryFightsFirst;

        PartingTheVeilActive =
            state.partingTheVeilActive;

        AeldariOffensiveHitModifier =
            state.aeldariOffensiveHitModifier;

        AeldariOffensiveWoundModifier =
            state.aeldariOffensiveWoundModifier;

        AeldariDefensiveHitModifier =
            state.aeldariDefensiveHitModifier;

        AeldariDefensiveWoundModifier =
            state.aeldariDefensiveWoundModifier;

        AeldariApModifier =
            state.aeldariApModifier;

        AeldariDamageModifier =
            state.aeldariDamageModifier;

        AeldariInvulnerableOverride =
            state.aeldariInvulnerableOverride;

        AeldariSustainedHits =
            state.aeldariSustainedHits;

        AeldariLethalHits =
            state.aeldariLethalHits;

        AeldariDevastatingWounds =
            state.aeldariDevastatingWounds;

        AeldariIgnoresCover =
            state.aeldariIgnoresCover;

        AeldariRerollHitOnes =
            state.aeldariRerollHitOnes;

        AeldariRerollWoundOnes =
            state.aeldariRerollWoundOnes;

        AeldariRerollAllHits =
            state.aeldariRerollAllHits;

        AeldariRerollAllWounds =
            state.aeldariRerollAllWounds;

        AeldariPathChoiceMadeThisPhase =
            state.aeldariPathChoiceMadeThisPhase;

        AeldariCanShootAfterFallBack =
            state.aeldariCanShootAfterFallBack;

        AeldariCanChargeAfterFallBack =
            state.aeldariCanChargeAfterFallBack;

        AeldariCanChargeAfterAdvance =
            state.aeldariCanChargeAfterAdvance;

        AeldariRange18Protection =
            state.aeldariRange18Protection;

        AeldariVectoredEnginesActive =
            state.aeldariVectoredEnginesActive;

        AeldariVengefulDeadTokens =
            state.aeldariVengefulDeadTokens;

        AeldariObjectiveControlOverride =
            state.aeldariObjectiveControlOverride;

        AgileManoeuvreUsedThisPhase =
            state.agileManoeuvreUsedThisPhase;

        FlittingShadowsActive =
            state.flittingShadowsActive;

        StarEnginesActive =
            state.starEnginesActive;

        SuddenStrikeActive =
            state.suddenStrikeActive;

        SnapShootingActive =
            state.snapShootingActive;

        KatahSustainedActive =
            state.katahSustainedActive;

        KatahLethalActive =
            state.katahLethalActive;

        KatahChoiceMadeThisFight =
            state.katahChoiceMadeThisFight;

        BattleFocusMoveBonus =
            state.battleFocusMoveBonus;

        ResurrectionOrbUsed =
            state.resurrectionOrbUsed;

        EternalRevenantUsed =
            state.eternalRevenantUsed;

        VeilOfDarknessUsed =
            state.veilOfDarknessUsed;

        MustIngressFromVeil =
            state.mustIngressFromVeil;

        TearsOfIshaUsedThisTurn =
            state.tearsOfIshaUsedThisTurn;

        SpiritMarkUsedThisTurn =
            state.spiritMarkUsedThisTurn;

        MyWillBeDoneUsedRound =
            state.myWillBeDoneUsedRound;

        SetModelPresentation(
            BattlefieldState ==
                SquadBattlefieldState
                    .Battlefield
        );

        RefreshVisuals();
    }

    public void MultiplayerClearLinks()
    {
        AttachedLeader = null;
        AttachedBodyguard = null;

        EmbarkedTransport = null;
        embarkedPassengers.Clear();
    }

    public void MultiplayerSetLeader(
        SquadController leader)
    {
        AttachedLeader = leader;

        if (leader != null)
            leader.AttachedBodyguard = this;
    }

    public void MultiplayerSetEmbarkedTransport(
        SquadController transport)
    {
        EmbarkedTransport = transport;

        if (transport != null &&
            !transport.embarkedPassengers
                .Contains(this))
        {
            transport.embarkedPassengers
                .Add(this);
        }

        if (transport != null)
        {
            BattlefieldState =
                SquadBattlefieldState
                    .Embarked;

            SetModelPresentation(false);
        }
    }
}

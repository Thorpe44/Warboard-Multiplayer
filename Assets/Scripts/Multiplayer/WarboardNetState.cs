using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WarboardMatchSnapshot
{
    public int revision;

    public float boardWidth;
    public float boardDepth;
    public float deploymentZoneWidth;

    public string battleSizeName = "";
    public int battlePoints;
    public string missionPresetName = "";
    public int resolutionMode;

    public bool battleSetupMode;
    public bool armyImportMode;
    public bool missionSetupMode;
    public bool deploymentMode;
    public bool battleOver;

    public int round;
    public int phase;
    public int activeFactionIndex;
    public string activeFaction = "";

    public string[] factions = new string[0];

    public string playerOneRosterLabel = "";
    public string playerTwoRosterLabel = "";
    public bool playerOneLoaded;
    public bool playerTwoLoaded;

    public int missionDispositionPlayerOne;
    public int missionDispositionPlayerTwo;
    public int missionSecondaryPlayerOne;
    public int missionSecondaryPlayerTwo;
    public int missionLayoutIndex;
    public int missionAttackerIndex;
    public int firstTurnFactionIndex;
    public int turnsCompletedThisRound;
    public string firstTurnRollSummary = "";
    public string battleSummary = "";

    public int currentDeploymentSquadIndex = -1;
    public int reservePlacementSquadIndex = -1;
    public int reserveCycleIndex = -1;

    public List<WarboardNamedInt> scores =
        new List<WarboardNamedInt>();

    public List<WarboardNamedInt> commandPoints =
        new List<WarboardNamedInt>();

    public List<WarboardRoundScore> primaryScores =
        new List<WarboardRoundScore>();

    public List<WarboardRoundScore> secondaryScores =
        new List<WarboardRoundScore>();

    public List<WarboardCardScore> fixedSecondaryScores =
        new List<WarboardCardScore>();

    public List<WarboardSquadSnapshot> squads =
        new List<WarboardSquadSnapshot>();

    public List<WarboardObjectiveSnapshot> objectives =
        new List<WarboardObjectiveSnapshot>();

    public List<WarboardTerrainSnapshot> terrain =
        new List<WarboardTerrainSnapshot>();

    public WarboardMissionSystemSnapshot mission;
}

[Serializable]
public class WarboardNamedInt
{
    public string key = "";
    public int value;
}

[Serializable]
public class WarboardRoundScore
{
    public string faction = "";
    public int round;
    public int value;
}

[Serializable]
public class WarboardCardScore
{
    public string faction = "";
    public string card = "";
    public int value;
}

[Serializable]
public class WarboardSquadSnapshot
{
    public int index;
    public string stableId = "";
    public UnitData sourceData;

    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public WarboardSquadRuntimeSnapshot runtime =
        new WarboardSquadRuntimeSnapshot();

    public string attachedLeaderId = "";
    public string attachedBodyguardId = "";
    public string embarkedTransportId = "";

    public List<WarboardModelSnapshot> models =
        new List<WarboardModelSnapshot>();
}

[Serializable]
public class WarboardSquadRuntimeSnapshot
{
    public int battlefieldState;
    public bool temporaryDeepStrike;
    public bool isRepositionedReserve;

    public bool hasMoved;
    public bool hasShot;
    public bool hasCharged;
    public bool hasFought;
    public bool hasFallenBack;
    public bool hasAdvanced;
    public int advanceBonus;
    public bool wasSetUpThisTurn;
    public bool madeChargeMove;

    public bool startedMissionActionThisTurn;
    public bool isPerformingMissionAction;
    public string activeMissionActionId = "";

    public bool isBattleShocked;
    public int lastBattleShockRoll;

    public bool factionSoulsightActive;
    public bool factionMacabreResilienceActive;
    public bool factionEmissariesRerollOnes;
    public bool factionEmissariesRerollAll;
    public bool factionHungryVoidActive;
    public bool factionSuddenStormActive;
    public bool factionConqueringTyrantActive;
    public bool targetedByFactionStratagemThisPhase;

    public bool temporaryFightsFirst;
    public bool partingTheVeilActive;

    public int aeldariOffensiveHitModifier;
    public int aeldariOffensiveWoundModifier;
    public int aeldariDefensiveHitModifier;
    public int aeldariDefensiveWoundModifier;
    public int aeldariApModifier;
    public int aeldariDamageModifier;
    public int aeldariInvulnerableOverride;
    public int aeldariSustainedHits;

    public bool aeldariLethalHits;
    public bool aeldariDevastatingWounds;
    public bool aeldariIgnoresCover;
    public bool aeldariRerollHitOnes;
    public bool aeldariRerollWoundOnes;
    public bool aeldariRerollAllHits;
    public bool aeldariRerollAllWounds;
    public bool aeldariPathChoiceMadeThisPhase;
    public bool aeldariCanShootAfterFallBack;
    public bool aeldariCanChargeAfterFallBack;
    public bool aeldariCanChargeAfterAdvance;
    public bool aeldariRange18Protection;
    public bool aeldariVectoredEnginesActive;

    public int aeldariVengefulDeadTokens;
    public int aeldariObjectiveControlOverride;

    public bool agileManoeuvreUsedThisPhase;
    public bool flittingShadowsActive;
    public bool starEnginesActive;
    public bool suddenStrikeActive;
    public bool snapShootingActive;
    public bool katahSustainedActive;
    public bool katahLethalActive;
    public bool katahChoiceMadeThisFight;
    public float battleFocusMoveBonus;

    public bool resurrectionOrbUsed;
    public bool eternalRevenantUsed;
    public bool veilOfDarknessUsed;
    public bool mustIngressFromVeil;
    public bool tearsOfIshaUsedThisTurn;
    public bool spiritMarkUsedThisTurn;
    public int myWillBeDoneUsedRound;
}

[Serializable]
public class WarboardModelSnapshot
{
    public int index;
    public string name = "";
    public string roleName = "";

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 turnStartPosition;

    public int currentWounds;
    public bool alive;
    public bool completedShooting;

    public string[] oneShotWeaponsUsed =
        new string[0];

    public string[] rangedWeaponsFiredThisTurn =
        new string[0];

    public string rangedFireGroupThisTurn = "";
}

[Serializable]
public class WarboardObjectiveSnapshot
{
    public int index;
    public string securedByFaction = "";
    public string[] missionStates = new string[0];
}

[Serializable]
public class WarboardTerrainSnapshot
{
    public string missionTerrainId = "";
    public Vector3 position;
    public string operationMarkerOwner = "";
    public string trappedByFaction = "";
    public int trappedRound;
}

[Serializable]
public class WarboardMissionSystemSnapshot
{
    public string currentTurnFaction = "";
    public int layoutIndex;
    public int attackerIndex;

    public List<WarboardMissionPlayerSnapshot> players =
        new List<WarboardMissionPlayerSnapshot>();

    public List<WarboardNamedInt> destroyedVictimsPreviousTurn =
        new List<WarboardNamedInt>();

    public List<WarboardNamedInt> destroyedVictimsCurrentTurn =
        new List<WarboardNamedInt>();

    public List<WarboardNamedInt> characterModelsDestroyedThisTurn =
        new List<WarboardNamedInt>();

    public List<WarboardNamedInt> characterW4ModelsDestroyedThisTurn =
        new List<WarboardNamedInt>();

    public List<WarboardNamedInt> largeModelsDestroyedThisTurn =
        new List<WarboardNamedInt>();

    public List<int> surveilledSquadIndices =
        new List<int>();
}

[Serializable]
public class WarboardMissionPlayerSnapshot
{
    public string factionId = "";
    public int disposition;
    public string primaryMission = "";
    public int secondaryMode;

    public string[] secondaryDeck = new string[0];
    public string[] secondaryHand = new string[0];
    public string[] fixedSecondaries = new string[0];

    public int[] controlledAtTurnStartObjectiveIndices =
        new int[0];

    public int persistentMarks;
    public int persistentBonusMarks;
    public int operationMarkers;
    public int turnActionCompletions;
    public int turnBonusCompletions;
    public int turnSpecialEvents;
    public int enemyUnitsDestroyedThisTurn;
    public int friendlyUnitsDestroyedPreviousTurn;
    public int fixedSecondaryOneIndex;
    public int fixedSecondaryTwoIndex;
}

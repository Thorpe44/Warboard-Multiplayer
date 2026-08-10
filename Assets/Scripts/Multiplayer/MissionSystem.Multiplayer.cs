using System.Collections.Generic;
using System.Linq;

public partial class MissionSystem
{
    public WarboardMissionSystemSnapshot
        CaptureMultiplayerMissionSnapshot()
    {
        WarboardMissionSystemSnapshot snapshot =
            new WarboardMissionSystemSnapshot
            {
                currentTurnFaction =
                    currentTurnFaction ?? "",
                layoutIndex =
                    LayoutIndex,
                attackerIndex =
                    AttackerIndex
            };

        foreach (
            KeyValuePair<
                string,
                MissionPlayerState
            > pair
            in playerStates)
        {
            MissionPlayerState state =
                pair.Value;

            if (state == null)
                continue;

            snapshot.players.Add(
                new WarboardMissionPlayerSnapshot
                {
                    factionId =
                        state.FactionId ?? "",
                    disposition =
                        (int)state.Disposition,
                    primaryMission =
                        state.PrimaryMission ?? "",
                    secondaryMode =
                        (int)state.SecondaryMode,
                    secondaryDeck =
                        state.SecondaryDeck
                            .ToArray(),
                    secondaryHand =
                        state.SecondaryHand
                            .ToArray(),
                    fixedSecondaries =
                        state.FixedSecondaries
                            .ToArray(),
                    controlledAtTurnStartObjectiveIndices =
                        state.ControlledAtTurnStart
                            .Where(
                                objective =>
                                    objective != null
                            )
                            .Select(
                                objective =>
                                    objectives.IndexOf(
                                        objective
                                    )
                            )
                            .Where(
                                index =>
                                    index >= 0
                            )
                            .ToArray(),
                    persistentMarks =
                        state.PersistentMarks,
                    persistentBonusMarks =
                        state.PersistentBonusMarks,
                    operationMarkers =
                        state.OperationMarkers,
                    turnActionCompletions =
                        state.TurnActionCompletions,
                    turnBonusCompletions =
                        state.TurnBonusCompletions,
                    turnSpecialEvents =
                        state.TurnSpecialEvents,
                    enemyUnitsDestroyedThisTurn =
                        state.EnemyUnitsDestroyedThisTurn,
                    friendlyUnitsDestroyedPreviousTurn =
                        state.FriendlyUnitsDestroyedPreviousTurn,
                    fixedSecondaryOneIndex =
                        state.FixedSecondaryOneIndex,
                    fixedSecondaryTwoIndex =
                        state.FixedSecondaryTwoIndex
                }
            );
        }

        CopyDictionary(
            destroyedVictimsPreviousTurn,
            snapshot.destroyedVictimsPreviousTurn
        );

        CopyDictionary(
            destroyedVictimsCurrentTurn,
            snapshot.destroyedVictimsCurrentTurn
        );

        CopyDictionary(
            characterModelsDestroyedThisTurn,
            snapshot.characterModelsDestroyedThisTurn
        );

        CopyDictionary(
            characterW4ModelsDestroyedThisTurn,
            snapshot.characterW4ModelsDestroyedThisTurn
        );

        CopyDictionary(
            largeModelsDestroyedThisTurn,
            snapshot.largeModelsDestroyedThisTurn
        );

        snapshot.surveilledSquadIndices =
            surveilledThisTurn
                .Where(unit => unit != null)
                .Select(
                    unit =>
                        squads.IndexOf(unit)
                )
                .Where(index => index >= 0)
                .ToList();

        return snapshot;
    }

    public void ApplyMultiplayerMissionSnapshot(
        WarboardMissionSystemSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        currentTurnFaction =
            snapshot.currentTurnFaction ??
            "";

        LayoutIndex =
            snapshot.layoutIndex;

        AttackerIndex =
            snapshot.attackerIndex;

        if (snapshot.players != null)
        {
            foreach (
                WarboardMissionPlayerSnapshot
                    player
                in snapshot.players)
            {
                if (player == null ||
                    string.IsNullOrWhiteSpace(
                        player.factionId))
                {
                    continue;
                }

                MissionPlayerState state;

                if (!playerStates.TryGetValue(
                        player.factionId,
                        out state))
                {
                    state =
                        new MissionPlayerState
                        {
                            FactionId =
                                player.factionId
                        };

                    playerStates[
                        player.factionId
                    ] = state;
                }

                state.Disposition =
                    (ForceDisposition)
                        player.disposition;

                state.PrimaryMission =
                    player.primaryMission ??
                    "";

                state.SecondaryMode =
                    (MissionSecondaryMode)
                        player.secondaryMode;

                state.SecondaryDeck.Clear();
                state.SecondaryHand.Clear();
                state.FixedSecondaries.Clear();
                state.ControlledAtTurnStart.Clear();

                if (player.secondaryDeck != null)
                    state.SecondaryDeck.AddRange(
                        player.secondaryDeck
                    );

                if (player.secondaryHand != null)
                    state.SecondaryHand.AddRange(
                        player.secondaryHand
                    );

                if (player.fixedSecondaries != null)
                    state.FixedSecondaries.AddRange(
                        player.fixedSecondaries
                    );

                if (player
                    .controlledAtTurnStartObjectiveIndices !=
                    null)
                {
                    foreach (
                        int index
                        in player
                            .controlledAtTurnStartObjectiveIndices)
                    {
                        if (index >= 0 &&
                            index < objectives.Count &&
                            objectives[index] != null)
                        {
                            state.ControlledAtTurnStart
                                .Add(
                                    objectives[index]
                                );
                        }
                    }
                }

                state.PersistentMarks =
                    player.persistentMarks;

                state.PersistentBonusMarks =
                    player.persistentBonusMarks;

                state.OperationMarkers =
                    player.operationMarkers;

                state.TurnActionCompletions =
                    player.turnActionCompletions;

                state.TurnBonusCompletions =
                    player.turnBonusCompletions;

                state.TurnSpecialEvents =
                    player.turnSpecialEvents;

                state.EnemyUnitsDestroyedThisTurn =
                    player.enemyUnitsDestroyedThisTurn;

                state.FriendlyUnitsDestroyedPreviousTurn =
                    player.friendlyUnitsDestroyedPreviousTurn;

                state.FixedSecondaryOneIndex =
                    player.fixedSecondaryOneIndex;

                state.FixedSecondaryTwoIndex =
                    player.fixedSecondaryTwoIndex;
            }
        }

        RestoreDictionary(
            destroyedVictimsPreviousTurn,
            snapshot.destroyedVictimsPreviousTurn
        );

        RestoreDictionary(
            destroyedVictimsCurrentTurn,
            snapshot.destroyedVictimsCurrentTurn
        );

        RestoreDictionary(
            characterModelsDestroyedThisTurn,
            snapshot.characterModelsDestroyedThisTurn
        );

        RestoreDictionary(
            characterW4ModelsDestroyedThisTurn,
            snapshot.characterW4ModelsDestroyedThisTurn
        );

        RestoreDictionary(
            largeModelsDestroyedThisTurn,
            snapshot.largeModelsDestroyedThisTurn
        );

        surveilledThisTurn.Clear();

        if (snapshot.surveilledSquadIndices !=
            null)
        {
            foreach (
                int index
                in snapshot.surveilledSquadIndices)
            {
                if (index >= 0 &&
                    index < squads.Count &&
                    squads[index] != null)
                {
                    surveilledThisTurn.Add(
                        squads[index]
                    );
                }
            }
        }
    }

    private static void CopyDictionary(
        Dictionary<string, int> source,
        List<WarboardNamedInt> target)
    {
        target.Clear();

        foreach (
            KeyValuePair<string, int> pair
            in source)
        {
            target.Add(
                new WarboardNamedInt
                {
                    key = pair.Key ?? "",
                    value = pair.Value
                }
            );
        }
    }

    private static void RestoreDictionary(
        Dictionary<string, int> target,
        List<WarboardNamedInt> source)
    {
        target.Clear();

        if (source == null)
            return;

        foreach (
            WarboardNamedInt entry
            in source)
        {
            if (entry == null)
                continue;

            target[entry.key ?? ""] =
                entry.value;
        }
    }
}

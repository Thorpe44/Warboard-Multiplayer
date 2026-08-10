using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameController
{
    public bool MultiplayerApplyingSnapshot
    {
        get;
        private set;
    }

    public WarboardMatchSnapshot
        CaptureMultiplayerSnapshot(
            int revision)
    {
        WarboardMatchSnapshot snapshot =
            new WarboardMatchSnapshot
            {
                revision = revision,

                boardWidth =
                    BoardWidth,
                boardDepth =
                    BoardDepth,
                deploymentZoneWidth =
                    DeploymentZoneWidth,

                battleSizeName =
                    battleSizeName ?? "",
                battlePoints =
                    battlePoints,
                missionPresetName =
                    missionPresetName ?? "",
                resolutionMode =
                    (int)ResolutionMode,

                battleSetupMode =
                    battleSetupMode,
                armyImportMode =
                    armyImportMode,
                missionSetupMode =
                    missionSetupMode,
                deploymentMode =
                    deploymentMode,
                battleOver =
                    battleOver,

                round = round,
                phase = (int)phase,
                activeFactionIndex =
                    activeFactionIndex,
                activeFaction =
                    activeFaction ?? "",

                factions =
                    factions.ToArray(),

                playerOneRosterLabel =
                    playerOneRosterLabel ?? "",
                playerTwoRosterLabel =
                    playerTwoRosterLabel ?? "",
                playerOneLoaded =
                    playerOneLoaded,
                playerTwoLoaded =
                    playerTwoLoaded,

                missionDispositionPlayerOne =
                    (int)missionDispositionPlayerOne,
                missionDispositionPlayerTwo =
                    (int)missionDispositionPlayerTwo,
                missionSecondaryPlayerOne =
                    (int)missionSecondaryPlayerOne,
                missionSecondaryPlayerTwo =
                    (int)missionSecondaryPlayerTwo,
                missionLayoutIndex =
                    missionLayoutIndex,
                missionAttackerIndex =
                    missionAttackerIndex,
                firstTurnFactionIndex =
                    firstTurnFactionIndex,
                turnsCompletedThisRound =
                    turnsCompletedThisRound,
                firstTurnRollSummary =
                    firstTurnRollSummary ?? "",
                battleSummary =
                    battleSummary ?? "",

                currentDeploymentSquadIndex =
                    currentDeploymentSquad != null
                    ? squads.IndexOf(
                        currentDeploymentSquad
                      )
                    : -1,

                reservePlacementSquadIndex =
                    reservePlacementSquad != null
                    ? squads.IndexOf(
                        reservePlacementSquad
                      )
                    : -1,

                reserveCycleIndex =
                    reserveCycleIndex
            };

        CopyDictionary(
            score,
            snapshot.scores
        );

        CopyDictionary(
            commandPoints,
            snapshot.commandPoints
        );

        CopyRoundDictionary(
            primaryScoreByRound,
            snapshot.primaryScores
        );

        CopyRoundDictionary(
            secondaryScoreByRound,
            snapshot.secondaryScores
        );

        foreach (
            KeyValuePair<
                string,
                Dictionary<string, int>
            > faction
            in fixedSecondaryScoreByCard)
        {
            foreach (
                KeyValuePair<string, int>
                    card
                in faction.Value)
            {
                snapshot.fixedSecondaryScores
                    .Add(
                        new WarboardCardScore
                        {
                            faction =
                                faction.Key ?? "",
                            card =
                                card.Key ?? "",
                            value =
                                card.Value
                        }
                    );
            }
        }

        for (int i = 0;
             i < squads.Count;
             i++)
        {
            SquadController squad =
                squads[i];

            if (squad == null)
                continue;

            WarboardSquadSnapshot unit =
                new WarboardSquadSnapshot
                {
                    index = i,
                    stableId =
                        MultiplayerSquadId(
                            i,
                            squad
                        ),
                    sourceData =
                        squad.SourceData,
                    rootPosition =
                        squad.transform.position,
                    rootRotation =
                        squad.transform.rotation,
                    runtime =
                        squad.CaptureMultiplayerRuntime()
                };

            if (squad.AttachedLeader != null)
            {
                int leaderIndex =
                    squads.IndexOf(
                        squad.AttachedLeader
                    );

                unit.attachedLeaderId =
                    leaderIndex >= 0
                    ? MultiplayerSquadId(
                        leaderIndex,
                        squad.AttachedLeader
                      )
                    : "";
            }

            if (squad.AttachedBodyguard !=
                null)
            {
                int bodyguardIndex =
                    squads.IndexOf(
                        squad.AttachedBodyguard
                    );

                unit.attachedBodyguardId =
                    bodyguardIndex >= 0
                    ? MultiplayerSquadId(
                        bodyguardIndex,
                        squad.AttachedBodyguard
                      )
                    : "";
            }

            if (squad.EmbarkedTransport !=
                null)
            {
                int transportIndex =
                    squads.IndexOf(
                        squad.EmbarkedTransport
                    );

                unit.embarkedTransportId =
                    transportIndex >= 0
                    ? MultiplayerSquadId(
                        transportIndex,
                        squad.EmbarkedTransport
                      )
                    : "";
            }

            ModelToken[] models =
                squad.GetComponentsInChildren<
                    ModelToken>(true);

            for (int modelIndex = 0;
                 modelIndex < models.Length;
                 modelIndex++)
            {
                unit.models.Add(
                    models[modelIndex]
                        .CaptureMultiplayerModelSnapshot(
                            modelIndex
                        )
                );
            }

            snapshot.squads.Add(unit);
        }

        for (int i = 0;
             i < objectives.Count;
             i++)
        {
            ObjectiveController objective =
                objectives[i];

            if (objective != null)
            {
                snapshot.objectives.Add(
                    objective
                        .CaptureMultiplayerObjectiveSnapshot(
                            i
                        )
                );
            }
        }

        TerrainFeature[] terrain =
            FindObjectsByType<TerrainFeature>()
            .OrderBy(
                value =>
                    value.MissionTerrainId ??
                    ""
            )
            .ThenBy(
                value =>
                    value.transform.position.x
            )
            .ThenBy(
                value =>
                    value.transform.position.z
            )
            .ToArray();

        foreach (TerrainFeature feature
            in terrain)
        {
            snapshot.terrain.Add(
                feature
                    .CaptureMultiplayerTerrainSnapshot()
            );
        }

        if (missionSystem != null)
        {
            snapshot.mission =
                missionSystem
                    .CaptureMultiplayerMissionSnapshot();
        }

        return snapshot;
    }

    public void ApplyMultiplayerSnapshot(
        WarboardMatchSnapshot snapshot)
    {
        if (snapshot == null ||
            MultiplayerApplyingSnapshot)
        {
            return;
        }

        MultiplayerApplyingSnapshot =
            true;

        try
        {
            ResolutionMode =
                (WarboardResolutionMode)
                    snapshot.resolutionMode;

            bool remoteHasBattlefield =
                !snapshot.battleSetupMode;

            bool localNeedsBattlefield =
                remoteHasBattlefield &&
                battleSetupMode;

            if (localNeedsBattlefield)
            {
                ConfigureBattle(
                    snapshot.battleSizeName,
                    snapshot.battlePoints,
                    snapshot.boardWidth,
                    snapshot.boardDepth,
                    snapshot.deploymentZoneWidth,
                    snapshot.missionPresetName
                );
            }

            battleSizeName =
                snapshot.battleSizeName ??
                "";

            battlePoints =
                snapshot.battlePoints;

            missionPresetName =
                snapshot.missionPresetName ??
                "";

            EnsureMultiplayerRosters(
                snapshot
            );

            factions.Clear();

            if (snapshot.factions != null)
                factions.AddRange(
                    snapshot.factions
                );

            playerOneRosterLabel =
                snapshot.playerOneRosterLabel ??
                "";

            playerTwoRosterLabel =
                snapshot.playerTwoRosterLabel ??
                "";

            playerOneLoaded =
                snapshot.playerOneLoaded;

            playerTwoLoaded =
                snapshot.playerTwoLoaded;

            missionDispositionPlayerOne =
                (ForceDisposition)
                    snapshot
                        .missionDispositionPlayerOne;

            missionDispositionPlayerTwo =
                (ForceDisposition)
                    snapshot
                        .missionDispositionPlayerTwo;

            missionSecondaryPlayerOne =
                (MissionSecondaryMode)
                    snapshot
                        .missionSecondaryPlayerOne;

            missionSecondaryPlayerTwo =
                (MissionSecondaryMode)
                    snapshot
                        .missionSecondaryPlayerTwo;

            missionLayoutIndex =
                snapshot.missionLayoutIndex;

            missionAttackerIndex =
                snapshot.missionAttackerIndex;

            firstTurnFactionIndex =
                snapshot.firstTurnFactionIndex;

            turnsCompletedThisRound =
                snapshot.turnsCompletedThisRound;

            firstTurnRollSummary =
                snapshot.firstTurnRollSummary ??
                "";

            battleSummary =
                snapshot.battleSummary ??
                "";

            bool needsMissionRuntime =
                !snapshot.battleSetupMode &&
                !snapshot.armyImportMode &&
                !snapshot.missionSetupMode &&
                (snapshot.deploymentMode ||
                 snapshot.round > 0);

            if (needsMissionRuntime &&
                missionSystem == null &&
                squads.Count > 0 &&
                factions.Count >= 2)
            {
                FinishArmyLoading();
            }

            RestoreDictionary(
                score,
                snapshot.scores
            );

            RestoreDictionary(
                commandPoints,
                snapshot.commandPoints
            );

            RestoreRoundDictionary(
                primaryScoreByRound,
                snapshot.primaryScores
            );

            RestoreRoundDictionary(
                secondaryScoreByRound,
                snapshot.secondaryScores
            );

            fixedSecondaryScoreByCard.Clear();

            if (snapshot.fixedSecondaryScores !=
                null)
            {
                foreach (
                    WarboardCardScore entry
                    in snapshot.fixedSecondaryScores)
                {
                    if (entry == null)
                        continue;

                    Dictionary<string, int> cards;

                    if (!fixedSecondaryScoreByCard
                        .TryGetValue(
                            entry.faction ?? "",
                            out cards))
                    {
                        cards =
                            new Dictionary<
                                string,
                                int
                            >();

                        fixedSecondaryScoreByCard[
                            entry.faction ?? ""
                        ] = cards;
                    }

                    cards[
                        entry.card ?? ""
                    ] =
                        entry.value;
                }
            }

            Dictionary<string, SquadController>
                byId =
                    new Dictionary<
                        string,
                        SquadController
                    >();

            if (snapshot.squads != null)
            {
                foreach (
                    WarboardSquadSnapshot unit
                    in snapshot.squads
                        .OrderBy(
                            value =>
                                value.index
                        ))
                {
                    if (unit == null ||
                        unit.index < 0 ||
                        unit.index >=
                            squads.Count)
                    {
                        continue;
                    }

                    SquadController squad =
                        squads[unit.index];

                    if (squad == null)
                        continue;

                    squad.ApplyMultiplayerRuntime(
                        unit.runtime,
                        unit.rootPosition,
                        unit.rootRotation
                    );

                    ModelToken[] models =
                        squad
                            .GetComponentsInChildren<
                                ModelToken>(true);

                    if (unit.models != null)
                    {
                        foreach (
                            WarboardModelSnapshot
                                model
                            in unit.models)
                        {
                            if (model == null ||
                                model.index < 0 ||
                                model.index >=
                                    models.Length)
                            {
                                continue;
                            }

                            models[model.index]
                                .ApplyMultiplayerModelSnapshot(
                                    model
                                );
                        }
                    }

                    squad.MultiplayerClearLinks();

                    byId[
                        unit.stableId ?? ""
                    ] = squad;
                }

                foreach (
                    WarboardSquadSnapshot unit
                    in snapshot.squads)
                {
                    if (unit == null)
                        continue;

                    SquadController squad;

                    if (!byId.TryGetValue(
                            unit.stableId ?? "",
                            out squad) ||
                        squad == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            unit.attachedLeaderId))
                    {
                        SquadController leader;

                        if (byId.TryGetValue(
                                unit.attachedLeaderId,
                                out leader))
                        {
                            squad.MultiplayerSetLeader(
                                leader
                            );
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(
                            unit.embarkedTransportId))
                    {
                        SquadController transport;

                        if (byId.TryGetValue(
                                unit.embarkedTransportId,
                                out transport))
                        {
                            squad
                                .MultiplayerSetEmbarkedTransport(
                                    transport
                                );
                        }
                    }
                }
            }

            if (snapshot.objectives != null)
            {
                foreach (
                    WarboardObjectiveSnapshot
                        objective
                    in snapshot.objectives)
                {
                    if (objective == null ||
                        objective.index < 0 ||
                        objective.index >=
                            objectives.Count ||
                        objectives[
                            objective.index
                        ] == null)
                    {
                        continue;
                    }

                    objectives[
                        objective.index
                    ]
                    .ApplyMultiplayerObjectiveSnapshot(
                        objective
                    );
                }
            }

            TerrainFeature[] localTerrain =
                FindObjectsByType<TerrainFeature>()
                .OrderBy(
                    value =>
                        value.MissionTerrainId ??
                        ""
                )
                .ThenBy(
                    value =>
                        value.transform.position.x
                )
                .ThenBy(
                    value =>
                        value.transform.position.z
                )
                .ToArray();

            if (snapshot.terrain != null)
            {
                foreach (
                    WarboardTerrainSnapshot
                        remoteTerrain
                    in snapshot.terrain)
                {
                    if (remoteTerrain == null)
                        continue;

                    TerrainFeature local =
                        localTerrain
                            .FirstOrDefault(
                                value =>
                                    value.MissionTerrainId ==
                                        remoteTerrain
                                            .missionTerrainId &&
                                    Vector3.Distance(
                                        value.transform.position,
                                        remoteTerrain.position
                                    ) <
                                    0.05f
                            );

                    if (local != null)
                    {
                        local.ApplyMultiplayerTerrainSnapshot(
                            remoteTerrain
                        );
                    }
                }
            }

            if (missionSystem != null &&
                snapshot.mission != null)
            {
                missionSystem
                    .ApplyMultiplayerMissionSnapshot(
                        snapshot.mission
                    );
            }

            round =
                snapshot.round;

            phase =
                (Phase)Mathf.Clamp(
                    snapshot.phase,
                    0,
                    (int)Phase.End
                );

            activeFactionIndex =
                snapshot.activeFactionIndex;

            activeFaction =
                snapshot.activeFaction ??
                "";

            battleSetupMode =
                snapshot.battleSetupMode;

            armyImportMode =
                snapshot.armyImportMode;

            missionSetupMode =
                snapshot.missionSetupMode;

            deploymentMode =
                snapshot.deploymentMode;

            battleOver =
                snapshot.battleOver;

            currentDeploymentSquad =
                SquadAtIndex(
                    snapshot
                        .currentDeploymentSquadIndex
                );

            reservePlacementSquad =
                SquadAtIndex(
                    snapshot
                        .reservePlacementSquadIndex
                );

            reserveCycleIndex =
                snapshot.reserveCycleIndex;

            RefreshObjectiveDisplays();

            foreach (
                SquadController squad
                in squads)
            {
                if (squad != null)
                    squad.RefreshVisuals();
            }
        }
        finally
        {
            MultiplayerApplyingSnapshot =
                false;
        }
    }

    private void EnsureMultiplayerRosters(
        WarboardMatchSnapshot snapshot)
    {
        if (snapshot.squads == null)
            return;

        bool same =
            squads.Count ==
                snapshot.squads.Count;

        if (same)
        {
            for (int i = 0;
                 i < squads.Count;
                 i++)
            {
                SquadController local =
                    squads[i];

                WarboardSquadSnapshot remote =
                    snapshot.squads
                        .FirstOrDefault(
                            value =>
                                value.index ==
                                i
                        );

                if (local == null ||
                    remote == null ||
                    remote.sourceData == null ||
                    local.SourceData == null ||
                    local.DisplayName !=
                        remote.sourceData.displayName ||
                    local.FactionId !=
                        remote.sourceData.factionId)
                {
                    same = false;
                    break;
                }
            }
        }

        if (same)
            return;

        foreach (
            SquadController squad
            in squads)
        {
            if (squad != null)
                Destroy(
                    squad.gameObject
                );
        }

        squads.Clear();

        foreach (
            WarboardSquadSnapshot remote
            in snapshot.squads
                .OrderBy(
                    value =>
                        value.index
                ))
        {
            if (remote == null ||
                remote.sourceData == null)
            {
                continue;
            }

            SpawnSquad(
                remote.sourceData,
                Vector3.zero
            );
        }
    }

    private SquadController SquadAtIndex(
        int index)
    {
        return
            index >= 0 &&
            index < squads.Count
            ? squads[index]
            : null;
    }

    private string MultiplayerSquadId(
        int index,
        SquadController squad)
    {
        return
            index +
            "|" +
            (squad != null
                ? squad.FactionId
                : "") +
            "|" +
            (squad != null
                ? squad.UnitId
                : "");
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

            target[
                entry.key ?? ""
            ] =
                entry.value;
        }
    }

    private static void CopyRoundDictionary(
        Dictionary<
            string,
            Dictionary<int, int>
        > source,
        List<WarboardRoundScore> target)
    {
        target.Clear();

        foreach (
            KeyValuePair<
                string,
                Dictionary<int, int>
            > faction
            in source)
        {
            foreach (
                KeyValuePair<int, int>
                    round
                in faction.Value)
            {
                target.Add(
                    new WarboardRoundScore
                    {
                        faction =
                            faction.Key ?? "",
                        round =
                            round.Key,
                        value =
                            round.Value
                    }
                );
            }
        }
    }

    private static void RestoreRoundDictionary(
        Dictionary<
            string,
            Dictionary<int, int>
        > target,
        List<WarboardRoundScore> source)
    {
        target.Clear();

        if (source == null)
            return;

        foreach (
            WarboardRoundScore entry
            in source)
        {
            if (entry == null)
                continue;

            Dictionary<int, int> rounds;

            if (!target.TryGetValue(
                    entry.faction ?? "",
                    out rounds))
            {
                rounds =
                    new Dictionary<
                        int,
                        int
                    >();

                target[
                    entry.faction ?? ""
                ] = rounds;
            }

            rounds[entry.round] =
                entry.value;
        }
    }
}

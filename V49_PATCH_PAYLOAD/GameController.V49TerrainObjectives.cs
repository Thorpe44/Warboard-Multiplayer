using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameController
{
    private void BindObjectivesToTerrainAreas11()
    {
        if (objectives == null ||
            objectives.Count == 0)
        {
            return;
        }

        List<TerrainFeature> terrain =
            Object.FindObjectsByType<
                TerrainFeature
            >(
                FindObjectsSortMode.None
            )
            .Where(
                feature =>
                    feature != null &&
                    !string.IsNullOrWhiteSpace(
                        feature.MissionTerrainId
                    )
            )
            .ToList();

        if (terrain.Count == 0)
            return;

        HashSet<TerrainFeature> preferred =
            new HashSet<TerrainFeature>(
                terrain.Where(
                    feature =>
                        feature.IsMissionObjectiveArea
                )
            );

        foreach (TerrainFeature feature
            in terrain)
        {
            feature.SetMissionObjectiveArea(false);
        }

        HashSet<TerrainFeature> used =
            new HashSet<TerrainFeature>();

        List<ObjectiveController> ordered =
            objectives
                .Where(
                    objective =>
                        objective != null
                )
                .OrderBy(
                    objective =>
                        ObjectiveTerrainBindPriority(
                            objective.MissionRole
                        )
                )
                .ToList();

        int bound = 0;

        foreach (ObjectiveController objective
            in ordered)
        {
            TerrainFeature best = null;
            float bestScore = float.MaxValue;

            foreach (TerrainFeature feature
                in terrain)
            {
                if (used.Contains(feature))
                    continue;

                float score =
                    feature.HorizontalDistanceTo(
                        objective.transform.position
                    );

                // Existing mission data already identifies useful terrain
                // objective candidates. Treat that as a preference rather
                // than a hard requirement so every normal objective can be
                // represented by a unique terrain area.
                if (preferred.Contains(feature))
                    score -= 2.0f;

                if ((objective.MissionRole ==
                        MissionObjectiveRole.PlayerOneHome ||
                     objective.MissionRole ==
                        MissionObjectiveRole.PlayerTwoHome) &&
                    feature.Trait ==
                        TerrainTrait.Blocking)
                {
                    score -= 0.75f;
                }

                if (score < bestScore)
                {
                    best = feature;
                    bestScore = score;
                }
            }

            if (best == null)
                continue;

            used.Add(best);
            objective.BindTerrainObjectiveArea(best);
            bound++;
        }

        Debug.Log(
            "WARBOARD V49: bound " +
            bound +
            " / " +
            ordered.Count +
            " objectives to terrain areas."
        );
    }

    private static int ObjectiveTerrainBindPriority(
        MissionObjectiveRole role)
    {
        switch (role)
        {
            case MissionObjectiveRole.Central:
                return 0;

            case MissionObjectiveRole.Expansion:
                return 1;

            case MissionObjectiveRole.PlayerOneHome:
            case MissionObjectiveRole.PlayerTwoHome:
                return 2;

            default:
                return 3;
        }
    }
}

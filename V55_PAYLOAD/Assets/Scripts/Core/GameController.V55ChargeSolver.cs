using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// WARBOARD_V55_BOUNDED_CHARGE_SOLVER
public partial class GameController : MonoBehaviour
{
    private sealed class V55ChargeCandidate
    {
        public Vector3 Delta;
        public float Score;
    }

    private bool V55SolveChargeMoveBounded(
        SquadController charger,
        List<SquadController> targets,
        float maximum)
    {
        if (charger == null ||
            targets == null ||
            targets.Count == 0 ||
            maximum <= 0f)
        {
            return false;
        }

        targets =
            targets
                .Where(target =>
                    target != null &&
                    target.IsAlive &&
                    target.IsOnBattlefield)
                .Select(target =>
                    target.JoinedActionController())
                .Distinct()
                .ToList();

        if (targets.Count == 0)
            return false;

        Dictionary<ModelToken, Vector3> original =
            CaptureJoinedPositions(
                charger
            );

        List<ModelToken> models =
            JoinedModels(
                charger
            )
            .Where(model =>
                model != null &&
                model.IsAlive)
            .ToList();

        if (models.Count == 0)
            return false;

        // The old v48 solver refined every model against every target for
        // hundreds/thousands of candidate translations. A normal multi-model
        // charge could therefore freeze or crash Unity.
        //
        // First use the existing single-target formation solver, which is
        // considerably cheaper and gives excellent common-case placement.
        if (targets.Count == 1)
        {
            SquadController target =
                targets[0];

            Vector3 formationDelta;

            RestoreJoinedPositions(
                original
            );

            if (TryFindFormationChargeTranslation(
                    charger,
                    target,
                    maximum,
                    out formationDelta))
            {
                TranslateJoinedModels(
                    charger,
                    formationDelta
                );

                Physics.SyncTransforms();

                RefineChargePlacementToCurrentRules(
                    charger,
                    target,
                    maximum,
                    original
                );

                if (V48ChargeEndStateIsLegal(
                        charger,
                        targets,
                        maximum,
                        original))
                {
                    return true;
                }
            }

            RestoreJoinedPositions(
                original
            );
        }

        Vector3 centre =
            charger.CurrentCentre();

        List<Vector3> directions =
            new List<Vector3>();

        Vector3 average =
            Vector3.zero;

        foreach (SquadController target
            in targets)
        {
            Vector3 direction =
                target.CurrentCentre() -
                centre;

            direction.y = 0f;

            if (direction.sqrMagnitude <
                0.001f)
            {
                continue;
            }

            direction.Normalize();

            bool duplicate =
                directions.Any(existing =>
                    Vector3.Dot(
                        existing,
                        direction
                    ) > 0.985f);

            if (!duplicate)
                directions.Add(direction);

            average += direction;
        }

        if (average.sqrMagnitude >
            0.001f)
        {
            average.Normalize();

            directions.Insert(
                0,
                average
            );
        }

        if (directions.Count == 0)
        {
            RestoreJoinedPositions(
                original
            );

            return false;
        }

        // Hard limits make solver cost deterministic.
        int[] angleOffsets =
        {
            0,
            -18,
            18,
            -36,
            36
        };

        float[] fractions =
        {
            1.00f,
            0.84f,
            0.68f,
            0.52f
        };

        List<V55ChargeCandidate> candidates =
            new List<V55ChargeCandidate>();

        int candidateBudget = 72;

        foreach (Vector3 baseDirection
            in directions.Take(4))
        {
            foreach (int angle
                in angleOffsets)
            {
                Vector3 direction =
                    Quaternion.Euler(
                        0f,
                        angle,
                        0f
                    ) *
                    baseDirection;

                foreach (float fraction
                    in fractions)
                {
                    if (candidates.Count >=
                        candidateBudget)
                    {
                        break;
                    }

                    candidates.Add(
                        new V55ChargeCandidate
                        {
                            Delta =
                                direction *
                                maximum *
                                fraction
                        }
                    );
                }
            }
        }

        // Include a few shorter translations for already-close charges.
        foreach (Vector3 direction
            in directions.Take(3))
        {
            if (candidates.Count >=
                candidateBudget)
            {
                break;
            }

            candidates.Add(
                new V55ChargeCandidate
                {
                    Delta =
                        direction *
                        Mathf.Min(
                            maximum,
                            2.25f
                        )
                }
            );
        }

        List<V55ChargeCandidate> viable =
            new List<V55ChargeCandidate>();

        foreach (V55ChargeCandidate candidate
            in candidates)
        {
            RestoreJoinedPositions(
                original
            );

            TranslateJoinedModels(
                charger,
                candidate.Delta
            );

            Physics.SyncTransforms();

            if (!charger.IsCoherent() ||
                !AllModelsInsideBoard(
                    charger
                ) ||
                !AllModelsHaveLegalPlacement(
                    charger
                ))
            {
                continue;
            }

            bool movedLegally =
                true;

            foreach (ModelToken model
                in models)
            {
                Vector3 start;

                if (!original.TryGetValue(
                        model,
                        out start) ||
                    HorizontalDistance(
                        start,
                        model.transform.position) >
                    maximum + 0.01f)
                {
                    movedLegally = false;
                    break;
                }

                float before =
                    targets.Min(target =>
                        DistancePointToSquad(
                            start,
                            target
                        ));

                float after =
                    targets.Min(target =>
                        DistancePointToSquad(
                            model.transform.position,
                            target
                        ));

                if (after >=
                    before - 0.001f)
                {
                    movedLegally = false;
                    break;
                }
            }

            if (!movedLegally)
                continue;

            if (V48ChargeEndStateIsLegal(
                    charger,
                    targets,
                    maximum,
                    original))
            {
                return true;
            }

            float score = 0f;

            foreach (SquadController target
                in targets)
            {
                float distance =
                    JoinedDistance(
                        charger,
                        target
                    );

                score +=
                    Mathf.Min(
                        20f,
                        distance
                    );

                if (distance <=
                    EngagementRange +
                    0.05f)
                {
                    score -= 25f;
                }
            }

            candidate.Score = score;
            viable.Add(candidate);
        }

        // Only the best few translations get the expensive individual-model
        // refinement pass. v48 refined every single translation.
        foreach (V55ChargeCandidate candidate
            in viable
                .OrderBy(value =>
                    value.Score)
                .Take(5))
        {
            RestoreJoinedPositions(
                original
            );

            TranslateJoinedModels(
                charger,
                candidate.Delta
            );

            Physics.SyncTransforms();

            V48RefineChargeTowardTargets(
                charger,
                targets,
                maximum,
                original
            );

            if (V48ChargeEndStateIsLegal(
                    charger,
                    targets,
                    maximum,
                    original))
            {
                return true;
            }
        }

        RestoreJoinedPositions(
            original
        );

        Debug.LogWarning(
            "WARBOARD V55: bounded charge solver exhausted " +
            candidates.Count +
            " translations and " +
            Mathf.Min(
                5,
                viable.Count
            ) +
            " refinement candidate(s) for " +
            charger.DisplayName +
            "."
        );

        return false;
    }
}

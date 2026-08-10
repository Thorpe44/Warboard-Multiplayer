using System.Collections.Generic;
using UnityEngine;

// WARBOARD V53 RECOVERY V2
//
// Terrain Area footprint = legal space.
// Actual ruin / wall / rubble geometry = illegal END POSITION for a model base.
//
// This is intentionally an end-position test. It does not alter movement-path
// permissions, so units that are allowed to move through a ruin wall can still
// pass through it; they simply cannot finish/deploy with their base inside it.

public partial class GameController : MonoBehaviour
{
    private bool V53ModelBaseOverlapsSolidAreaScenery(
        ModelToken model,
        Vector3 destination)
    {
        if (model == null)
            return false;

        float radius =
            Mathf.Max(
                0.05f,
                model.BaseRadiusInches
            );

        Vector3 probe =
            new Vector3(
                destination.x,
                0.12f,
                destination.z
            );

        Collider[] overlaps =
            Physics.OverlapSphere(
                probe,
                radius + 0.08f,
                ~0,
                QueryTriggerInteraction.Collide
            );

        foreach (Collider col in overlaps)
        {
            if (col == null)
                continue;

            // Ignore the moving model's entire hierarchy.
            ModelToken owner =
                col.GetComponentInParent<ModelToken>();

            if (owner == model)
                continue;

            // The footprint itself is legal standing space.
            if (col.GetComponent<
                    TerrainAreaFootprint50>() != null)
            {
                continue;
            }

            // V45 presentation can create child geometry beneath the V50
            // TerrainFeature object, so search upward rather than only on the
            // collider's own GameObject.
            TerrainFeature feature =
                col.GetComponentInParent<
                    TerrainFeature>();

            if (feature == null)
                continue;

            TerrainAreaFootprint50 area =
                feature.GetComponentInParent<
                    TerrainAreaFootprint50>();

            // Only apply this new rule to scenery physically sitting on a V50
            // Terrain Area. Legacy terrain keeps its existing placement logic.
            if (area == null)
                continue;

            if (V53BaseCircleIntersectsColliderXZ(
                    destination,
                    radius,
                    col))
            {
                return true;
            }
        }

        return false;
    }

    private bool V53BaseCircleIntersectsColliderXZ(
        Vector3 baseCentre,
        float baseRadius,
        Collider col)
    {
        if (col == null)
            return false;

        // Tiny tolerance prevents exact edge contact from flickering red.
        float allowedRadius =
            Mathf.Max(
                0f,
                baseRadius - 0.01f
            );

        BoxCollider box =
            col as BoxCollider;

        if (box != null)
        {
            // V50 scenery is primarily cube/BoxCollider geometry. Transform
            // the base centre into collider local space so rotated ruins are
            // tested correctly.
            Vector3 worldAtBoxHeight =
                new Vector3(
                    baseCentre.x,
                    box.bounds.center.y,
                    baseCentre.z
                );

            Vector3 local =
                box.transform
                    .InverseTransformPoint(
                        worldAtBoxHeight
                    );

            Vector3 half =
                box.size * 0.5f;

            Vector3 min =
                box.center - half;

            Vector3 max =
                box.center + half;

            float closestX =
                Mathf.Clamp(
                    local.x,
                    min.x,
                    max.x
                );

            float closestZ =
                Mathf.Clamp(
                    local.z,
                    min.z,
                    max.z
                );

            Vector3 closestWorld =
                box.transform.TransformPoint(
                    new Vector3(
                        closestX,
                        box.center.y,
                        closestZ
                    )
                );

            float dx =
                closestWorld.x -
                baseCentre.x;

            float dz =
                closestWorld.z -
                baseCentre.z;

            return
                dx * dx +
                dz * dz <
                allowedRadius *
                allowedRadius;
        }

        // Conservative fallback for future non-box scenery.
        Bounds bounds =
            col.bounds;

        float closestWorldX =
            Mathf.Clamp(
                baseCentre.x,
                bounds.min.x,
                bounds.max.x
            );

        float closestWorldZ =
            Mathf.Clamp(
                baseCentre.z,
                bounds.min.z,
                bounds.max.z
            );

        float fallbackDx =
            closestWorldX -
            baseCentre.x;

        float fallbackDz =
            closestWorldZ -
            baseCentre.z;

        return
            fallbackDx * fallbackDx +
            fallbackDz * fallbackDz <
            allowedRadius *
            allowedRadius;
    }

    private bool V53GhostCandidatesClearOfSolidAreaScenery(
        List<PlacementGhostCandidate52> candidates)
    {
        if (candidates == null)
            return true;

        foreach (PlacementGhostCandidate52 candidate
            in candidates)
        {
            if (candidate == null ||
                candidate.Model == null)
            {
                continue;
            }

            if (V53ModelBaseOverlapsSolidAreaScenery(
                    candidate.Model,
                    candidate.Destination))
            {
                return false;
            }
        }

        return true;
    }
}

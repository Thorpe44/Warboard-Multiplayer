using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// WARBOARD V53 CORE RECOVERY V4
//
// Terrain Area footprint = legal space.
// Actual ruin / wall / rubble geometry = illegal END POSITION for a model base.

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

            // WARBOARD_V54_OBJECTIVE_TRIGGER_NOT_SOLID
            // Click/area triggers stay queryable but never block a model base.
            if (col.isTrigger)
                continue;

            ModelToken owner =
                col.GetComponentInParent<ModelToken>();

            if (owner == model)
                continue;

            if (col.GetComponent<
                    TerrainAreaFootprint50>() != null)
            {
                continue;
            }

            TerrainFeature feature =
                col.GetComponentInParent<
                    TerrainFeature>();

            if (feature == null)
                continue;

            TerrainAreaFootprint50 area =
                feature.GetComponentInParent<
                    TerrainAreaFootprint50>();

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

        float allowedRadius =
            Mathf.Max(
                0f,
                baseRadius - 0.01f
            );

        BoxCollider box =
            col as BoxCollider;

        if (box != null)
        {
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

    // Generic on purpose: V53 never names V52's nested
    // PlacementGhostCandidate52 type.
    private bool V53GhostCandidatesClearOfSolidAreaScenery<T>(
        IEnumerable<T> candidates)
    {
        if (candidates == null)
            return true;

        foreach (T candidate in candidates)
        {
            if (candidate == null)
                continue;

            object boxed = candidate;
            System.Type type =
                boxed.GetType();

            FieldInfo modelField =
                type.GetField(
                    "Model",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            FieldInfo destinationField =
                type.GetField(
                    "Destination",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            if (modelField == null ||
                destinationField == null)
            {
                continue;
            }

            ModelToken model =
                modelField.GetValue(
                    boxed
                ) as ModelToken;

            if (model == null)
                continue;

            object destinationValue =
                destinationField.GetValue(
                    boxed
                );

            if (!(destinationValue is Vector3))
                continue;

            Vector3 destination =
                (Vector3)destinationValue;

            if (V53ModelBaseOverlapsSolidAreaScenery(
                    model,
                    destination))
            {
                return false;
            }
        }

        return true;
    }
}

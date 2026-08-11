using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameController
{
    private sealed class TerrainAreaPlacement50
    {
        public TerrainFeature Feature;
        public TerrainAreaFootprint50 Footprint;
    }

    /// <summary>
    /// Replaces the old loose-scatter V48/V49 terrain with a proper
    /// 11th-edition Terrain Area set: 16 footprints in the five standard
    /// matched-play sizes. Objectives are bound directly to designated
    /// large terrain areas instead of being guessed from nearby scenery.
    /// </summary>
    private void BuildAndBindStandardTerrainAreas50()
    {
        if (objectives == null ||
            objectives.Count == 0)
        {
            return;
        }

        // Remove the old mission-terrain objects. ObjectiveController objects
        // are separate and are preserved.
        TerrainFeature[] oldTerrain =
            Object.FindObjectsByType<
                TerrainFeature
            >(
                FindObjectsSortMode.None
            );

        foreach (TerrainFeature feature
            in oldTerrain)
        {
            if (feature != null)
                Destroy(feature.gameObject);
        }

        List<TerrainAreaPlacement50> created =
            new List<TerrainAreaPlacement50>();

        List<ObjectiveController> central =
            objectives
                .Where(
                    objective =>
                        objective != null &&
                        objective.MissionRole ==
                            MissionObjectiveRole.Central
                )
                .ToList();

        List<ObjectiveController> otherObjectives =
            objectives
                .Where(
                    objective =>
                        objective != null &&
                        objective.MissionRole !=
                            MissionObjectiveRole.Central
                )
                .Concat(central.Skip(2))
                .ToList();

        int largeTrianglesUsed = 0;
        int largeRectanglesUsed = 0;
        int objectiveIndex = 0;

        // The two large right-angle triangles are excellent mid-board
        // objective terrain areas and match the official 11e terrain set.
        foreach (ObjectiveController objective
            in central.Take(2))
        {
            float rotation =
                objectiveIndex % 2 == 0
                ? 45f
                : 225f;

            Vector3 objectiveAreaPosition =
                FindObjectiveTerrainAreaPosition50(
                    created,
                    objective.transform.position,
                    8f,
                    11.5f,
                    objectiveIndex
                );

            TerrainAreaPlacement50 area =
                CreateTerrainArea50(
                    "OBJ_TRI_" + objectiveIndex,
                    objectiveAreaPosition,
                    TerrainAreaShape50.RightTriangle,
                    8f,
                    11.5f,
                    rotation,
                    true,
                    objective.MissionRole,
                    0
                );

            created.Add(area);
            objective.BindTerrainObjectiveArea(
                area.Feature
            );

            largeTrianglesUsed++;
            objectiveIndex++;
        }

        // All remaining normal objectives use the four 7 x 11.5 large
        // rectangles. Current Chapter Approved matchups use 5-6 objectives;
        // if a future mission asks for more, the fallback section below uses
        // medium areas rather than reintroducing circular markers.
        foreach (ObjectiveController objective
            in otherObjectives)
        {
            TerrainAreaShape50 shape;
            float width;
            float depth;

            if (largeRectanglesUsed < 4)
            {
                shape = TerrainAreaShape50.Rectangle;
                width = 7f;
                depth = 11.5f;
                largeRectanglesUsed++;
            }
            else
            {
                shape = TerrainAreaShape50.Rectangle;
                width = 6f;
                depth = 4f;
            }

            float rotation =
                ObjectiveAreaRotation50(
                    objective,
                    objectiveIndex
                );

            Vector3 objectiveAreaPosition =
                FindObjectiveTerrainAreaPosition50(
                    created,
                    objective.transform.position,
                    width,
                    depth,
                    objectiveIndex
                );

            TerrainAreaPlacement50 area =
                CreateTerrainArea50(
                    "OBJ_RECT_" + objectiveIndex,
                    objectiveAreaPosition,
                    shape,
                    width,
                    depth,
                    rotation,
                    true,
                    objective.MissionRole,
                    objectiveIndex
                );

            created.Add(area);
            objective.BindTerrainObjectiveArea(
                area.Feature
            );

            objectiveIndex++;
        }

        // If there was only one/zero central objective in a custom mission,
        // preserve the complete standard set by adding unused large shapes as
        // ordinary terrain areas.
        while (largeTrianglesUsed < 2)
        {
            AddNonObjectiveArea50(
                created,
                TerrainAreaShape50.RightTriangle,
                8f,
                11.5f,
                40 + largeTrianglesUsed
            );
            largeTrianglesUsed++;
        }

        while (largeRectanglesUsed < 4)
        {
            AddNonObjectiveArea50(
                created,
                TerrainAreaShape50.Rectangle,
                7f,
                11.5f,
                50 + largeRectanglesUsed
            );
            largeRectanglesUsed++;
        }

        // Remaining official standard set:
        // four 6 x 4 medium rectangles,
        // two 10 x 2.5 long lines,
        // four 6 x 2 short lines.
        for (int i = 0; i < 4; i++)
        {
            AddNonObjectiveArea50(
                created,
                TerrainAreaShape50.Rectangle,
                6f,
                4f,
                60 + i
            );
        }

        for (int i = 0; i < 2; i++)
        {
            AddNonObjectiveArea50(
                created,
                TerrainAreaShape50.Rectangle,
                10f,
                2.5f,
                70 + i
            );
        }

        for (int i = 0; i < 4; i++)
        {
            AddNonObjectiveArea50(
                created,
                TerrainAreaShape50.Rectangle,
                6f,
                2f,
                80 + i
            );
        }

        Physics.SyncTransforms();

        Debug.Log(
            "WARBOARD V50: built " +
            created.Count +
            " standard 11e terrain-area footprints; " +
            objectives.Count +
            " are terrain objectives."
        );
    }

    private TerrainAreaPlacement50 CreateTerrainArea50(
        string id,
        Vector3 requestedPosition,
        TerrainAreaShape50 shape,
        float width,
        float depth,
        float rotationY,
        bool objective,
        MissionObjectiveRole role,
        int visualSeed)
    {
        Vector3 position =
            ClampTerrainAreaCentre50(
                requestedPosition,
                width,
                depth
            );

        GameObject root =
            new GameObject(
                "Terrain Area " + id
            );

        root.transform.position =
            new Vector3(
                position.x,
                0f,
                position.z
            );

        root.transform.rotation =
            Quaternion.Euler(
                0f,
                rotationY,
                0f
            );

        TerrainAreaFootprint50 footprint =
            root.AddComponent<
                TerrainAreaFootprint50
            >();

        footprint.Initialize(
            shape,
            width,
            depth
        );

        footprint.SetObjective(
            objective,
            role
        );

        // The footprint is area geometry, not a wall. Traversable prevents
        // the old movement blocker from treating the whole footprint as a
        // solid obstacle; the decorative feature children below carry the
        // actual blocking/cover colliders.
        TerrainFeature feature =
            root.AddComponent<TerrainFeature>();

        feature.Initialize(
            TerrainTrait.Traversable,
            "V50_" + id,
            objective
        );

        CreateSceneryOnTerrainArea50(
            root.transform,
            shape,
            width,
            depth,
            visualSeed,
            role
        );

        return new TerrainAreaPlacement50
        {
            Feature = feature,
            Footprint = footprint
        };
    }

    private void AddNonObjectiveArea50(
        List<TerrainAreaPlacement50> created,
        TerrainAreaShape50 shape,
        float width,
        float depth,
        int seed)
    {
        Vector3 position;
        float rotation;

        FindFreeTerrainAreaPlacement50(
            created,
            width,
            depth,
            seed,
            out position,
            out rotation
        );

        TerrainAreaPlacement50 area =
            CreateTerrainArea50(
                "AREA_" + seed,
                position,
                shape,
                width,
                depth,
                rotation,
                false,
                MissionObjectiveRole.Neutral,
                seed
            );

        created.Add(area);
    }

    private Vector3 FindObjectiveTerrainAreaPosition50(
        List<TerrainAreaPlacement50> created,
        Vector3 requested,
        float width,
        float depth,
        int seed)
    {
        if (created == null || created.Count == 0)
        {
            return ClampTerrainAreaCentre50(
                requested,
                width,
                depth
            );
        }

        Vector2 baseDirection =
            new Vector2(
                requested.x,
                requested.z
            );

        if (baseDirection.sqrMagnitude < 0.01f)
        {
            float angle =
                (seed * 137f + 35f) *
                Mathf.Deg2Rad;

            baseDirection =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );
        }

        baseDirection.Normalize();

        Vector2 perpendicular =
            new Vector2(
                -baseDirection.y,
                baseDirection.x
            );

        Vector2[] offsets =
            new[]
            {
                Vector2.zero,
                baseDirection * 2.5f,
                baseDirection * 4.5f,
                baseDirection * 6.0f,
                perpendicular * 3.0f,
                -perpendicular * 3.0f,
                baseDirection * 4.0f + perpendicular * 2.0f,
                baseDirection * 4.0f - perpendicular * 2.0f
            };

        Vector3 best = requested;
        float bestClearance = -float.MaxValue;

        foreach (Vector2 offset in offsets)
        {
            Vector3 candidate =
                new Vector3(
                    requested.x + offset.x,
                    0f,
                    requested.z + offset.y
                );

            candidate =
                ClampTerrainAreaCentre50(
                    candidate,
                    width,
                    depth
                );

            float clearance =
                MinimumTerrainAreaClearance50(
                    created,
                    candidate,
                    width,
                    depth
                );

            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                best = candidate;
            }
        }

        return best;
    }

    private void FindFreeTerrainAreaPlacement50(
        List<TerrainAreaPlacement50> created,
        float width,
        float depth,
        int seed,
        out Vector3 position,
        out float rotation)
    {
        float halfX = BoardWidth * 0.5f;
        float halfZ = BoardDepth * 0.5f;

        Vector2[] normalizedCandidates =
            new[]
            {
                new Vector2(-0.72f, -0.62f),
                new Vector2( 0.72f,  0.62f),
                new Vector2(-0.72f,  0.62f),
                new Vector2( 0.72f, -0.62f),
                new Vector2(-0.43f, -0.76f),
                new Vector2( 0.43f,  0.76f),
                new Vector2(-0.43f,  0.76f),
                new Vector2( 0.43f, -0.76f),
                new Vector2(-0.84f, -0.20f),
                new Vector2( 0.84f,  0.20f),
                new Vector2(-0.84f,  0.20f),
                new Vector2( 0.84f, -0.20f),
                new Vector2(-0.25f, -0.42f),
                new Vector2( 0.25f,  0.42f),
                new Vector2(-0.25f,  0.42f),
                new Vector2( 0.25f, -0.42f),
                new Vector2(-0.55f,  0.05f),
                new Vector2( 0.55f, -0.05f),
                new Vector2(-0.08f, -0.80f),
                new Vector2( 0.08f,  0.80f),
                new Vector2(-0.92f,  0.52f),
                new Vector2( 0.92f, -0.52f),
                new Vector2(-0.92f, -0.52f),
                new Vector2( 0.92f,  0.52f)
            };

        int offset =
            Mathf.Abs(seed * 7 + missionLayoutIndex * 3) %
            normalizedCandidates.Length;

        rotation =
            ((seed + missionLayoutIndex) % 2 == 0)
            ? 0f
            : 90f;

        Vector3 bestPosition = Vector3.zero;
        float bestClearance = -1f;

        for (int c = 0;
             c < normalizedCandidates.Length;
             c++)
        {
            Vector2 normalized =
                normalizedCandidates[
                    (c + offset) %
                    normalizedCandidates.Length
                ];

            Vector3 candidate =
                new Vector3(
                    normalized.x *
                        Mathf.Max(1f, halfX - 4f),
                    0f,
                    normalized.y *
                        Mathf.Max(1f, halfZ - 4f)
                );

            candidate =
                ClampTerrainAreaCentre50(
                    candidate,
                    width,
                    depth
                );

            float clearance =
                MinimumTerrainAreaClearance50(
                    created,
                    candidate,
                    width,
                    depth
                );

            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                bestPosition = candidate;
            }

            if (clearance >= 1.10f)
            {
                position = candidate;
                return;
            }
        }

        position = bestPosition;
    }

    private float MinimumTerrainAreaClearance50(
        List<TerrainAreaPlacement50> created,
        Vector3 candidate,
        float width,
        float depth)
    {
        if (created == null || created.Count == 0)
            return float.MaxValue;

        float radius =
            0.5f *
            Mathf.Sqrt(
                width * width +
                depth * depth
            );

        float best = float.MaxValue;

        foreach (TerrainAreaPlacement50 area
            in created)
        {
            if (area == null ||
                area.Footprint == null)
            {
                continue;
            }

            Bounds bounds =
                area.Footprint.WorldBounds;

            float otherRadius =
                0.5f *
                Mathf.Sqrt(
                    bounds.size.x * bounds.size.x +
                    bounds.size.z * bounds.size.z
                );

            float centreDistance =
                HorizontalDistance(
                    candidate,
                    bounds.center
                );

            float clearance =
                centreDistance -
                radius -
                otherRadius;

            best = Mathf.Min(best, clearance);
        }

        return best;
    }

    private Vector3 ClampTerrainAreaCentre50(
        Vector3 position,
        float width,
        float depth)
    {
        float halfX = BoardWidth * 0.5f;
        float halfZ = BoardDepth * 0.5f;

        // Use the larger half-dimension because the area may be rotated.
        float margin =
            Mathf.Max(width, depth) * 0.5f +
            0.35f;

        position.x =
            Mathf.Clamp(
                position.x,
                -halfX + margin,
                halfX - margin
            );

        position.z =
            Mathf.Clamp(
                position.z,
                -halfZ + margin,
                halfZ - margin
            );

        position.y = 0f;
        return position;
    }

    private float ObjectiveAreaRotation50(
        ObjectiveController objective,
        int index)
    {
        Vector3 position =
            objective != null
            ? objective.transform.position
            : Vector3.zero;

        if (Mathf.Abs(position.x) >
            Mathf.Abs(position.z))
        {
            return 90f;
        }

        return
            (index + missionLayoutIndex) % 2 == 0
            ? 0f
            : 180f;
    }

    private void CreateSceneryOnTerrainArea50(
        Transform areaRoot,
        TerrainAreaShape50 shape,
        float width,
        float depth,
        int seed,
        MissionObjectiveRole role)
    {
        if (areaRoot == null)
            return;

        bool major =
            width >= 7f ||
            depth >= 7f ||
            shape == TerrainAreaShape50.RightTriangle;

        if (major)
        {
            TerrainTrait mainTrait =
                (role == MissionObjectiveRole.Central ||
                 seed % 3 == 0)
                ? TerrainTrait.Blocking
                : TerrainTrait.Cover;

            V55CreateTerrainFeatureVisual(
                areaRoot,
                "Major Feature",
                new Vector3(
                    -width * 0.12f,
                    0f,
                    depth * 0.08f
                ),
                new Vector3(
                    Mathf.Max(2.8f, width * 0.62f),
                    2f,
                    Mathf.Max(2.2f, depth * 0.30f)
                ),
                mainTrait,
                seed
            );

            V55CreateTerrainFeatureVisual(
                areaRoot,
                "Minor Feature",
                new Vector3(
                    width * 0.22f,
                    0f,
                    -depth * 0.25f
                ),
                new Vector3(
                    Mathf.Max(1.8f, width * 0.32f),
                    1.25f,
                    Mathf.Max(1.4f, depth * 0.18f)
                ),
                TerrainTrait.Cover,
                seed + 1
            );

            return;
        }

        TerrainTrait trait =
            depth <= 2.6f
            ? TerrainTrait.Cover
            : (seed % 2 == 0
                ? TerrainTrait.Blocking
                : TerrainTrait.Cover);

        V55CreateTerrainFeatureVisual(
            areaRoot,
            "Area Feature",
            Vector3.zero,
            new Vector3(
                Mathf.Max(1.5f, width * 0.72f),
                trait == TerrainTrait.Blocking
                    ? 1.8f
                    : 1.1f,
                Mathf.Max(1.0f, depth * 0.52f)
            ),
            trait,
            seed
        );
    }

    // WARBOARD_V55_CLEAN_TERRAIN_CALLS
    private void CreateTerrainFeatureVisual50(
        Transform areaRoot,
        string label,
        Vector3 localPosition,
        Vector3 size,
        TerrainTrait trait,
        int seed)
    {
        GameObject terrain =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        terrain.name =
            "V50 " + label + " " + seed;

        terrain.transform.SetParent(
            areaRoot,
            false
        );

        terrain.transform.localPosition =
            new Vector3(
                localPosition.x,
                size.y * 0.5f,
                localPosition.z
            );

        terrain.transform.localRotation =
            Quaternion.Euler(
                0f,
                (seed * 17) % 32 - 16f,
                0f
            );

        terrain.transform.localScale = size;

        TerrainFeature feature =
            terrain.AddComponent<TerrainFeature>();

        // Empty mission ID prevents the old V49 nearest-terrain binder from
        // ever selecting decorative feature children as objective areas.
        feature.Initialize(
            trait,
            "",
            false
        );

        SetObjectColor(
            terrain,
            trait == TerrainTrait.Blocking
            ? new Color(0.26f, 0.26f, 0.28f)
            : new Color(0.38f, 0.34f, 0.24f)
        );

        WarboardV45Presentation.StyleTerrain(
            terrain,
            trait,
            terrain.name,
            size
        );
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

// WARBOARD_TERRAIN_OVERHAUL_R2
//
// This is the current-main replacement for the old V57 procedural terrain kit.
// V50 still owns the rules footprints and placement. This file only decides
// what scenery is built on each footprint.
//
// Design goals:
// - the ruin reads first; the rules footprint reads second
// - one coherent terrain kit per Terrain Area, not disconnected cube piles
// - large areas get proper L/U/corner ruined buildings with doors/windows
// - long narrow areas get industrial cover lanes
// - solid visible geometry keeps TerrainFeature colliders, decorative rubble does not
// - footprint art comes from the terrain-base references supplied for this pass

public partial class GameController : MonoBehaviour
{
    private const string R2TerrainRootName =
        "R2 Battlefield Terrain";

    private readonly Dictionary<string, Material>
        r2TerrainMaterials =
            new Dictionary<string, Material>();

    private void V55CreateTerrainFeatureVisual(
        Transform areaRoot,
        string label,
        Vector3 localPosition,
        Vector3 size,
        TerrainTrait trait,
        int seed)
    {
        if (areaRoot == null)
            return;

        // V50 calls this twice on some major areas. R2 deliberately builds one
        // coherent kit for the whole Terrain Area on the first call.
        if (areaRoot.Find(R2TerrainRootName) != null)
            return;

        TerrainAreaFootprint50 footprint =
            areaRoot.GetComponent<TerrainAreaFootprint50>();

        if (footprint == null)
        {
            R2BuildFallback(
                areaRoot,
                localPosition,
                size,
                trait,
                seed
            );
            return;
        }

        R2RestyleFootprint(
            areaRoot,
            footprint,
            seed
        );

        GameObject kit =
            new GameObject(
                R2TerrainRootName
            );

        kit.transform.SetParent(
            areaRoot,
            false
        );

        kit.transform.localPosition =
            Vector3.zero;

        kit.transform.localRotation =
            Quaternion.identity;

        kit.transform.localScale =
            Vector3.one;

        R2CreateAreaSurface(
            kit.transform,
            footprint,
            seed
        );

        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        bool longAlongX =
            footprint.Width >=
            footprint.Depth;

        if (footprint.Shape ==
            TerrainAreaShape50.RightTriangle)
        {
            R2BuildTriangleRuin(
                kit.transform,
                footprint,
                seed
            );
        }
        else if (longSide >= 10f &&
                 shortSide <= 3.2f)
        {
            R2BuildIndustrialLane(
                kit.transform,
                footprint,
                longAlongX,
                seed,
                true
            );
        }
        else if (longSide >= 7f &&
                 shortSide >= 6.5f)
        {
            if ((seed & 1) == 0)
            {
                R2BuildLargeLRuin(
                    kit.transform,
                    footprint,
                    longAlongX,
                    seed
                );
            }
            else
            {
                R2BuildLargeURuin(
                    kit.transform,
                    footprint,
                    longAlongX,
                    seed
                );
            }
        }
        else if (longSide >= 6f &&
                 shortSide >= 3.6f)
        {
            R2BuildCornerRuin(
                kit.transform,
                footprint,
                longAlongX,
                seed
            );
        }
        else if (longSide >= 5f &&
                 shortSide <= 2.6f)
        {
            R2BuildIndustrialLane(
                kit.transform,
                footprint,
                longAlongX,
                seed,
                false
            );
        }
        else
        {
            R2BuildSmallRuin(
                kit.transform,
                footprint,
                longAlongX,
                seed
            );
        }
    }

    private void R2RestyleFootprint(
        Transform areaRoot,
        TerrainAreaFootprint50 footprint,
        int seed)
    {
        MeshRenderer fill =
            areaRoot.GetComponent<MeshRenderer>();

        // The old cyan tinted prism was the main reason the battlefield looked
        // like a debugging view. R2 supplies its own textured card surface.
        if (fill != null)
            fill.enabled = false;

        LineRenderer[] lines =
            areaRoot.GetComponentsInChildren<
                LineRenderer
            >(true);

        foreach (LineRenderer line
            in lines)
        {
            if (line == null)
                continue;

            line.widthMultiplier =
                footprint.IsObjective
                ? 0.040f
                : 0.025f;

            Color colour =
                footprint.IsObjective
                ? new Color(
                    0.92f,
                    0.66f,
                    0.18f,
                    0.72f
                  )
                : new Color(
                    0.43f,
                    0.47f,
                    0.48f,
                    0.38f
                  );

            line.startColor = colour;
            line.endColor = colour;
        }
    }

    private void R2CreateAreaSurface(
        Transform parent,
        TerrainAreaFootprint50 footprint,
        int seed)
    {
        string texturePath =
            R2FloorTexturePath(
                footprint,
                seed
            );

        Material floorMaterial =
            R2Material(
                "floor|" +
                    texturePath +
                    "|" +
                    footprint.IsObjective,
                footprint.IsObjective
                    ? new Color(
                        0.74f,
                        0.68f,
                        0.58f,
                        1f
                      )
                    : new Color(
                        0.70f,
                        0.71f,
                        0.70f,
                        1f
                      ),
                texturePath,
                0.04f,
                0.12f
            );

        if (footprint.Shape ==
            TerrainAreaShape50.RightTriangle)
        {
            GameObject triangle =
                new GameObject(
                    "R2 Terrain Area Surface"
                );

            triangle.transform.SetParent(
                parent,
                false
            );

            Mesh mesh =
                new Mesh();

            float hw =
                footprint.Width *
                0.49f;

            float hd =
                footprint.Depth *
                0.49f;

            mesh.vertices =
                new[]
                {
                    new Vector3(
                        -hw,
                        0.058f,
                        -hd
                    ),
                    new Vector3(
                        -hw,
                        0.058f,
                        hd
                    ),
                    new Vector3(
                        hw,
                        0.058f,
                        -hd
                    )
                };

            mesh.uv =
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 0f)
                };

            mesh.triangles =
                new[]
                {
                    0,
                    1,
                    2
                };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter =
                triangle.AddComponent<
                    MeshFilter
                >();

            filter.sharedMesh = mesh;

            MeshRenderer renderer =
                triangle.AddComponent<
                    MeshRenderer
                >();

            renderer.sharedMaterial =
                floorMaterial;

            return;
        }

        GameObject surface =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        surface.name =
            "R2 Terrain Area Surface";

        surface.transform.SetParent(
            parent,
            false
        );

        surface.transform.localPosition =
            new Vector3(
                0f,
                0.052f,
                0f
            );

        surface.transform.localRotation =
            Quaternion.identity;

        surface.transform.localScale =
            new Vector3(
                footprint.Width *
                    0.98f,
                0.020f,
                footprint.Depth *
                    0.98f
            );

        Collider collider =
            surface.GetComponent<Collider>();

        if (collider != null)
            UnityEngine.Object.Destroy(
                collider
            );

        Renderer surfaceRenderer =
            surface.GetComponent<Renderer>();

        if (surfaceRenderer != null)
        {
            surfaceRenderer.sharedMaterial =
                floorMaterial;
        }
    }

    private string R2FloorTexturePath(
        TerrainAreaFootprint50 footprint,
        int seed)
    {
        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        if (shortSide <= 2.7f)
            return
                "WarboardTerrainR2/" +
                "floor_industrial_plate";

        int pick =
            Mathf.Abs(seed) %
            3;

        if (pick == 0)
            return
                "WarboardTerrainR2/" +
                "floor_rubble_rust";

        if (pick == 1)
            return
                "WarboardTerrainR2/" +
                "floor_broken_concrete";

        return
            "WarboardTerrainR2/" +
            "floor_battle_rust";
    }

    private void R2BuildLargeLRuin(
        Transform root,
        TerrainAreaFootprint50 footprint,
        bool longAlongX,
        int seed)
    {
        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        float height =
            4.45f +
            (Mathf.Abs(seed) % 3) *
            0.30f;

        R2CreateGothicWallRun(
            root,
            "Main L Ruin",
            longAlongX,
            R2Local(
                longAlongX,
                0f,
                shortSide * 0.31f
            ),
            longSide * 0.78f,
            height,
            seed
        );

        R2CreateGothicWallRun(
            root,
            "Return L Ruin",
            !longAlongX,
            R2Local(
                longAlongX,
                -longSide * 0.34f,
                0.02f
            ),
            shortSide * 0.64f,
            height * 0.82f,
            seed + 11
        );

        R2CreateCornerButtress(
            root,
            R2Local(
                longAlongX,
                -longSide * 0.34f,
                shortSide * 0.31f
            ),
            height
        );

        R2CreateRubbleCluster(
            root,
            R2Local(
                longAlongX,
                longSide * 0.18f,
                -shortSide * 0.18f
            ),
            seed,
            7,
            1.5f
        );

        R2CreatePipeRun(
            root,
            R2Local(
                longAlongX,
                longSide * 0.08f,
                -shortSide * 0.29f
            ),
            longAlongX,
            2.4f,
            seed
        );
    }

    private void R2BuildLargeURuin(
        Transform root,
        TerrainAreaFootprint50 footprint,
        bool longAlongX,
        int seed)
    {
        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        float height =
            4.25f +
            (Mathf.Abs(seed) % 3) *
            0.28f;

        R2CreateGothicWallRun(
            root,
            "Rear U Ruin",
            longAlongX,
            R2Local(
                longAlongX,
                0f,
                shortSide * 0.32f
            ),
            longSide * 0.80f,
            height,
            seed
        );

        R2CreateGothicWallRun(
            root,
            "Left U Ruin",
            !longAlongX,
            R2Local(
                longAlongX,
                -longSide * 0.35f,
                0.03f
            ),
            shortSide * 0.60f,
            height * 0.76f,
            seed + 17
        );

        R2CreateGothicWallRun(
            root,
            "Right U Ruin",
            !longAlongX,
            R2Local(
                longAlongX,
                longSide * 0.35f,
                0.08f
            ),
            shortSide * 0.46f,
            height * 0.68f,
            seed + 23
        );

        R2CreateCornerButtress(
            root,
            R2Local(
                longAlongX,
                -longSide * 0.35f,
                shortSide * 0.32f
            ),
            height
        );

        R2CreateCornerButtress(
            root,
            R2Local(
                longAlongX,
                longSide * 0.35f,
                shortSide * 0.32f
            ),
            height * 0.86f
        );

        R2CreateRubbleCluster(
            root,
            R2Local(
                longAlongX,
                0f,
                -shortSide * 0.18f
            ),
            seed + 9,
            8,
            1.8f
        );

        R2CreateCrates(
            root,
            R2Local(
                longAlongX,
                longSide * 0.20f,
                -shortSide * 0.28f
            ),
            seed,
            3
        );
    }

    private void R2BuildTriangleRuin(
        Transform root,
        TerrainAreaFootprint50 footprint,
        int seed)
    {
        float width =
            footprint.Width;

        float depth =
            footprint.Depth;

        float height =
            4.15f +
            (Mathf.Abs(seed) % 2) *
            0.30f;

        // The triangle's right angle is the local (-X,-Z) corner.
        R2CreateGothicWallRun(
            root,
            "Triangle Ruin X",
            true,
            new Vector3(
                -width * 0.02f,
                0f,
                -depth * 0.39f
            ),
            width * 0.72f,
            height,
            seed
        );

        R2CreateGothicWallRun(
            root,
            "Triangle Ruin Z",
            false,
            new Vector3(
                -width * 0.39f,
                0f,
                -depth * 0.03f
            ),
            depth * 0.66f,
            height * 0.80f,
            seed + 19
        );

        R2CreateCornerButtress(
            root,
            new Vector3(
                -width * 0.39f,
                0f,
                -depth * 0.39f
            ),
            height
        );

        R2CreateRubbleCluster(
            root,
            new Vector3(
                -width * 0.08f,
                0f,
                -depth * 0.08f
            ),
            seed,
            7,
            1.45f
        );
    }

    private void R2BuildCornerRuin(
        Transform root,
        TerrainAreaFootprint50 footprint,
        bool longAlongX,
        int seed)
    {
        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        float height =
            3.35f +
            (Mathf.Abs(seed) % 2) *
            0.28f;

        R2CreateGothicWallRun(
            root,
            "Corner Main",
            longAlongX,
            R2Local(
                longAlongX,
                -longSide * 0.06f,
                shortSide * 0.30f
            ),
            longSide * 0.68f,
            height,
            seed
        );

        R2CreateGothicWallRun(
            root,
            "Corner Return",
            !longAlongX,
            R2Local(
                longAlongX,
                -longSide * 0.31f,
                0f
            ),
            shortSide * 0.54f,
            height * 0.76f,
            seed + 13
        );

        R2CreateRubbleCluster(
            root,
            R2Local(
                longAlongX,
                longSide * 0.16f,
                -shortSide * 0.18f
            ),
            seed,
            5,
            1.0f
        );
    }

    private void R2BuildSmallRuin(
        Transform root,
        TerrainAreaFootprint50 footprint,
        bool longAlongX,
        int seed)
    {
        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        R2CreateGothicWallRun(
            root,
            "Small Ruin",
            longAlongX,
            R2Local(
                longAlongX,
                0f,
                shortSide * 0.16f
            ),
            longSide * 0.66f,
            2.75f,
            seed
        );

        R2CreateLowBarricade(
            root,
            R2Local(
                longAlongX,
                longSide * 0.16f,
                -shortSide * 0.26f
            ),
            longAlongX,
            Mathf.Max(
                1.2f,
                longSide * 0.28f
            ),
            seed
        );

        R2CreateRubbleCluster(
            root,
            R2Local(
                longAlongX,
                -longSide * 0.14f,
                -shortSide * 0.20f
            ),
            seed + 5,
            4,
            0.75f
        );
    }

    private void R2BuildIndustrialLane(
        Transform root,
        TerrainAreaFootprint50 footprint,
        bool longAlongX,
        int seed,
        bool major)
    {
        float longSide =
            Mathf.Max(
                footprint.Width,
                footprint.Depth
            );

        float shortSide =
            Mathf.Min(
                footprint.Width,
                footprint.Depth
            );

        float panelLength =
            longSide *
            (major
                ? 0.30f
                : 0.27f);

        float gap =
            Mathf.Max(
                1.15f,
                longSide * 0.16f
            );

        float axisOffset =
            gap * 0.5f +
            panelLength * 0.5f;

        R2CreateArmouredBarricade(
            root,
            R2Local(
                longAlongX,
                -axisOffset,
                0f
            ),
            longAlongX,
            panelLength,
            major
                ? 1.25f
                : 1.02f,
            seed
        );

        R2CreateArmouredBarricade(
            root,
            R2Local(
                longAlongX,
                axisOffset,
                0f
            ),
            longAlongX,
            panelLength,
            major
                ? 1.18f
                : 0.98f,
            seed + 1
        );

        if (major)
        {
            R2CreatePipeRun(
                root,
                R2Local(
                    longAlongX,
                    0f,
                    -shortSide * 0.25f
                ),
                longAlongX,
                Mathf.Min(
                    3.0f,
                    longSide * 0.30f
                ),
                seed
            );
        }

        R2CreateCrates(
            root,
            R2Local(
                longAlongX,
                longSide * 0.32f,
                -shortSide * 0.20f
            ),
            seed + 7,
            major
                ? 3
                : 2
        );
    }

    private void R2CreateGothicWallRun(
        Transform root,
        string name,
        bool alongX,
        Vector3 centre,
        float length,
        float height,
        int seed)
    {
        length =
            Mathf.Max(
                2.2f,
                length
            );

        int bays =
            length >= 6.8f
                ? 5
                : (length >= 4.4f
                    ? 4
                    : 3);

        float thickness =
            0.30f;

        float bayLength =
            length /
            bays;

        int doorBay =
            Mathf.Abs(
                seed * 17 + 3
            ) %
            bays;

        Color stone =
            new Color(
                0.29f,
                0.30f,
                0.30f,
                1f
            );

        Color stoneLight =
            new Color(
                0.39f,
                0.39f,
                0.37f,
                1f
            );

        Color dark =
            new Color(
                0.16f,
                0.17f,
                0.18f,
                1f
            );

        // Structural piers make each opening obvious and give the wall the
        // vertical mass expected of 40K ruins.
        for (int i = 0;
             i <= bays;
             i++)
        {
            float axis =
                -length * 0.5f +
                i * bayLength;

            float pierHeight =
                height *
                (0.82f +
                 0.18f *
                 R2Noise01(
                     seed +
                     i * 11
                 ));

            Vector3 pos =
                centre +
                R2AlongVector(
                    alongX,
                    axis,
                    0f
                );

            R2CreateSolid(
                root,
                name +
                    " Pier " +
                    i,
                pos +
                    Vector3.up *
                    (pierHeight * 0.5f),
                R2ScaleAlong(
                    alongX,
                    0.30f,
                    pierHeight,
                    thickness * 1.55f
                ),
                Vector3.zero,
                TerrainTrait.Blocking,
                stoneLight,
                0.04f,
                0.06f
            );

            R2CreateButtressBlock(
                root,
                pos,
                alongX,
                pierHeight,
                stoneLight,
                i
            );
        }

        for (int bay = 0;
             bay < bays;
             bay++)
        {
            float axis =
                -length * 0.5f +
                (bay + 0.5f) *
                bayLength;

            float usable =
                Mathf.Max(
                    0.42f,
                    bayLength -
                    0.34f
                );

            Vector3 bayCentre =
                centre +
                R2AlongVector(
                    alongX,
                    axis,
                    0f
                );

            if (bay == doorBay)
            {
                float lintelHeight =
                    Mathf.Max(
                        0.45f,
                        height - 2.55f
                    );

                R2CreateSolid(
                    root,
                    name +
                        " Door Header " +
                        bay,
                    bayCentre +
                        Vector3.up *
                        (2.55f +
                         lintelHeight *
                         0.5f),
                    R2ScaleAlong(
                        alongX,
                        usable,
                        lintelHeight,
                        thickness
                    ),
                    Vector3.zero,
                    TerrainTrait.Blocking,
                    dark,
                    0.18f,
                    0.10f
                );

                continue;
            }

            float lowerHeight =
                0.92f;

            R2CreateSolid(
                root,
                name +
                    " Window Sill " +
                    bay,
                bayCentre +
                    Vector3.up *
                    (lowerHeight * 0.5f),
                R2ScaleAlong(
                    alongX,
                    usable,
                    lowerHeight,
                    thickness
                ),
                Vector3.zero,
                TerrainTrait.Blocking,
                stone,
                0.03f,
                0.05f
            );

            float upperBottom =
                2.42f;

            float upperHeight =
                Mathf.Max(
                    0.42f,
                    height -
                    upperBottom
                );

            R2CreateSolid(
                root,
                name +
                    " Window Crown " +
                    bay,
                bayCentre +
                    Vector3.up *
                    (upperBottom +
                     upperHeight *
                     0.5f),
                R2ScaleAlong(
                    alongX,
                    usable,
                    upperHeight,
                    thickness
                ),
                new Vector3(
                    0f,
                    0f,
                    (bay & 1) == 0
                        ? 0.8f
                        : -0.8f
                ),
                TerrainTrait.Blocking,
                (bay & 1) == 0
                    ? stone
                    : stoneLight,
                0.03f,
                0.05f
            );

            // Decorative diagonal window braces are intentionally non-solid.
            float braceYaw =
                alongX
                ? 0f
                : 90f;

            R2CreateDecorative(
                root,
                name +
                    " Window Brace " +
                    bay,
                PrimitiveType.Cube,
                bayCentre +
                    Vector3.up *
                    1.66f,
                R2ScaleAlong(
                    alongX,
                    usable * 0.82f,
                    0.09f,
                    0.07f
                ),
                new Vector3(
                    alongX
                        ? 0f
                        : 20f,
                    braceYaw,
                    alongX
                        ? 20f
                        : 0f
                ),
                dark,
                0.35f,
                0.16f
            );
        }

        // Broken parapet chunks on top break the silhouette.
        for (int i = 0;
             i < 3;
             i++)
        {
            float axis =
                Mathf.Lerp(
                    -length * 0.36f,
                    length * 0.34f,
                    i / 2f
                );

            float pieceHeight =
                0.28f +
                0.20f *
                R2Noise01(
                    seed +
                    101 +
                    i
                );

            R2CreateDecorative(
                root,
                name +
                    " Broken Parapet " +
                    i,
                PrimitiveType.Cube,
                centre +
                    R2AlongVector(
                        alongX,
                        axis,
                        0f
                    ) +
                    Vector3.up *
                    (height +
                     pieceHeight *
                     0.5f),
                R2ScaleAlong(
                    alongX,
                    bayLength * 0.42f,
                    pieceHeight,
                    thickness * 1.05f
                ),
                new Vector3(
                    0f,
                    0f,
                    (i - 1) * 5f
                ),
                stoneLight,
                0.02f,
                0.04f
            );
        }
    }

    private void R2CreateCornerButtress(
        Transform root,
        Vector3 basePosition,
        float height)
    {
        R2CreateSolid(
            root,
            "Corner Buttress",
            basePosition +
                Vector3.up *
                (height * 0.42f),
            new Vector3(
                0.62f,
                height * 0.84f,
                0.62f
            ),
            new Vector3(
                0f,
                45f,
                0f
            ),
            TerrainTrait.Blocking,
            new Color(
                0.42f,
                0.41f,
                0.38f,
                1f
            ),
            0.04f,
            0.05f
        );
    }

    private void R2CreateButtressBlock(
        Transform root,
        Vector3 basePosition,
        bool wallAlongX,
        float wallHeight,
        Color colour,
        int index)
    {
        Vector3 offset =
            wallAlongX
            ? new Vector3(
                0f,
                0f,
                0.22f
              )
            : new Vector3(
                0.22f,
                0f,
                0f
              );

        Vector3 scale =
            wallAlongX
            ? new Vector3(
                0.46f,
                wallHeight * 0.46f,
                0.62f
              )
            : new Vector3(
                0.62f,
                wallHeight * 0.46f,
                0.46f
              );

        R2CreateSolid(
            root,
            "Wall Buttress " +
                index,
            basePosition +
                offset +
                Vector3.up *
                (scale.y * 0.5f),
            scale,
            Vector3.zero,
            TerrainTrait.Blocking,
            colour,
            0.03f,
            0.04f
        );
    }

    private void R2CreateArmouredBarricade(
        Transform root,
        Vector3 centre,
        bool alongX,
        float length,
        float height,
        int seed)
    {
        Color metal =
            new Color(
                0.20f,
                0.23f,
                0.24f,
                1f
            );

        Color metalLight =
            new Color(
                0.33f,
                0.35f,
                0.34f,
                1f
            );

        Color rust =
            new Color(
                0.42f,
                0.22f,
                0.11f,
                1f
            );

        R2CreateSolid(
            root,
            "Armoured Barricade",
            centre +
                Vector3.up *
                (height * 0.5f),
            R2ScaleAlong(
                alongX,
                length,
                height,
                0.34f
            ),
            Vector3.zero,
            TerrainTrait.Cover,
            metal,
            0.26f,
            0.14f
        );

        float postHeight =
            height * 1.36f;

        for (int side = -1;
             side <= 1;
             side += 2)
        {
            R2CreateSolid(
                root,
                "Barricade Post",
                centre +
                    R2AlongVector(
                        alongX,
                        side *
                        length *
                        0.43f,
                        0f
                    ) +
                    Vector3.up *
                    (postHeight * 0.5f),
                new Vector3(
                    0.25f,
                    postHeight,
                    0.25f
                ),
                new Vector3(
                    0f,
                    45f,
                    0f
                ),
                TerrainTrait.Cover,
                metalLight,
                0.28f,
                0.16f
            );
        }

        R2CreateDecorative(
            root,
            "Hazard Panel",
            PrimitiveType.Cube,
            centre +
                Vector3.up *
                (height * 0.62f) +
                (alongX
                    ? new Vector3(
                        0f,
                        0f,
                        -0.19f
                      )
                    : new Vector3(
                        -0.19f,
                        0f,
                        0f
                      )),
            R2ScaleAlong(
                alongX,
                length * 0.42f,
                0.13f,
                0.035f
            ),
            Vector3.zero,
            rust,
            0.24f,
            0.12f
        );
    }

    private void R2CreateLowBarricade(
        Transform root,
        Vector3 centre,
        bool alongX,
        float length,
        int seed)
    {
        R2CreateArmouredBarricade(
            root,
            centre,
            alongX,
            length,
            0.82f,
            seed
        );
    }

    private void R2CreatePipeRun(
        Transform root,
        Vector3 centre,
        bool alongX,
        float length,
        int seed)
    {
        Color pipe =
            new Color(
                0.20f,
                0.18f,
                0.16f,
                1f
            );

        Vector3 scale =
            alongX
            ? new Vector3(
                0.18f,
                length * 0.5f,
                0.18f
              )
            : new Vector3(
                0.18f,
                length * 0.5f,
                0.18f
              );

        Vector3 euler =
            alongX
            ? new Vector3(
                0f,
                0f,
                90f
              )
            : new Vector3(
                90f,
                0f,
                0f
              );

        R2CreateDecorative(
            root,
            "Rusted Pipe",
            PrimitiveType.Cylinder,
            centre +
                Vector3.up *
                0.20f,
            scale,
            euler,
            pipe,
            0.52f,
            0.18f
        );
    }

    private void R2CreateCrates(
        Transform root,
        Vector3 centre,
        int seed,
        int count)
    {
        for (int i = 0;
             i < count;
             i++)
        {
            float x =
                (i -
                 (count - 1) *
                 0.5f) *
                0.58f;

            float z =
                ((i & 1) == 0
                    ? 0f
                    : 0.44f);

            float height =
                0.52f +
                0.10f *
                R2Noise01(
                    seed +
                    i * 7
                );

            R2CreateSolid(
                root,
                "Supply Crate " +
                    i,
                centre +
                    new Vector3(
                        x,
                        height * 0.5f,
                        z
                    ),
                new Vector3(
                    0.52f,
                    height,
                    0.52f
                ),
                new Vector3(
                    0f,
                    seed * 7f +
                        i * 19f,
                    0f
                ),
                TerrainTrait.Cover,
                new Color(
                    0.30f,
                    0.25f,
                    0.18f,
                    1f
                ),
                0.12f,
                0.08f
            );
        }
    }

    private void R2CreateRubbleCluster(
        Transform root,
        Vector3 centre,
        int seed,
        int count,
        float spread)
    {
        Color[] colours =
            new[]
            {
                new Color(
                    0.31f,
                    0.31f,
                    0.30f,
                    1f
                ),
                new Color(
                    0.42f,
                    0.39f,
                    0.34f,
                    1f
                ),
                new Color(
                    0.36f,
                    0.25f,
                    0.17f,
                    1f
                )
            };

        for (int i = 0;
             i < count;
             i++)
        {
            float x =
                Mathf.Lerp(
                    -spread,
                    spread,
                    R2Noise01(
                        seed +
                        i * 13
                    )
                );

            float z =
                Mathf.Lerp(
                    -spread * 0.70f,
                    spread * 0.70f,
                    R2Noise01(
                        seed +
                        i * 17 +
                        9
                    )
                );

            float sx =
                Mathf.Lerp(
                    0.18f,
                    0.62f,
                    R2Noise01(
                        seed +
                        i * 23
                    )
                );

            float sy =
                Mathf.Lerp(
                    0.08f,
                    0.24f,
                    R2Noise01(
                        seed +
                        i * 29
                    )
                );

            float sz =
                Mathf.Lerp(
                    0.18f,
                    0.54f,
                    R2Noise01(
                        seed +
                        i * 31
                    )
                );

            R2CreateDecorative(
                root,
                "Rubble " +
                    i,
                PrimitiveType.Cube,
                centre +
                    new Vector3(
                        x,
                        sy * 0.5f,
                        z
                    ),
                new Vector3(
                    sx,
                    sy,
                    sz
                ),
                new Vector3(
                    0f,
                    R2Noise01(
                        seed +
                        i * 37
                    ) *
                    70f -
                    35f,
                    R2Noise01(
                        seed +
                        i * 41
                    ) *
                    12f -
                    6f
                ),
                colours[
                    i %
                    colours.Length
                ],
                0.02f,
                0.03f
            );
        }
    }

    private GameObject R2CreateSolid(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        TerrainTrait trait,
        Color colour,
        float metallic,
        float smoothness)
    {
        GameObject piece =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        piece.name =
            "R2 Solid - " +
            name;

        piece.transform.SetParent(
            parent,
            false
        );

        piece.transform.localPosition =
            localPosition;

        piece.transform.localEulerAngles =
            localEuler;

        piece.transform.localScale =
            localScale;

        TerrainFeature feature =
            piece.AddComponent<
                TerrainFeature
            >();

        feature.Initialize(
            trait,
            "",
            false
        );

        R2ApplyMaterial(
            piece,
            colour,
            metallic,
            smoothness
        );

        return piece;
    }

    private void R2CreateDecorative(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        Color colour,
        float metallic,
        float smoothness)
    {
        GameObject piece =
            GameObject.CreatePrimitive(
                primitive
            );

        piece.name =
            "R2 Detail - " +
            name;

        piece.transform.SetParent(
            parent,
            false
        );

        piece.transform.localPosition =
            localPosition;

        piece.transform.localEulerAngles =
            localEuler;

        piece.transform.localScale =
            localScale;

        Collider collider =
            piece.GetComponent<Collider>();

        if (collider != null)
            UnityEngine.Object.Destroy(
                collider
            );

        R2ApplyMaterial(
            piece,
            colour,
            metallic,
            smoothness
        );
    }

    private void R2ApplyMaterial(
        GameObject piece,
        Color colour,
        float metallic,
        float smoothness)
    {
        if (piece == null)
            return;

        Renderer renderer =
            piece.GetComponent<Renderer>();

        if (renderer == null)
            return;

        Material material =
            R2Material(
                "solid|" +
                    Mathf.RoundToInt(
                        colour.r * 255f
                    ) +
                    "|" +
                    Mathf.RoundToInt(
                        colour.g * 255f
                    ) +
                    "|" +
                    Mathf.RoundToInt(
                        colour.b * 255f
                    ) +
                    "|" +
                    metallic +
                    "|" +
                    smoothness,
                colour,
                null,
                metallic,
                smoothness
            );

        renderer.sharedMaterial =
            material;
    }

    private Material R2Material(
        string key,
        Color colour,
        string resourceTexture,
        float metallic,
        float smoothness)
    {
        Material cached;

        if (r2TerrainMaterials.TryGetValue(
                key,
                out cached) &&
            cached != null)
        {
            return cached;
        }

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Lit"
            );

        if (shader == null)
            shader =
                Shader.Find(
                    "Standard"
                );

        if (shader == null)
            shader =
                Shader.Find(
                    "Sprites/Default"
                );

        if (shader == null)
            return null;

        Material material =
            new Material(shader);

        material.name =
            "Warboard Terrain R2 " +
            key;

        material.color =
            colour;

        if (!string.IsNullOrWhiteSpace(
                resourceTexture))
        {
            Texture2D texture =
                Resources.Load<Texture2D>(
                    resourceTexture
                );

            if (texture != null)
            {
                material.mainTexture =
                    texture;

                material.mainTextureScale =
                    new Vector2(
                        1.15f,
                        1.15f
                    );
            }
        }

        if (material.HasProperty(
                "_Metallic"))
        {
            material.SetFloat(
                "_Metallic",
                metallic
            );
        }

        if (material.HasProperty(
                "_Glossiness"))
        {
            material.SetFloat(
                "_Glossiness",
                smoothness
            );
        }

        if (material.HasProperty(
                "_Smoothness"))
        {
            material.SetFloat(
                "_Smoothness",
                smoothness
            );
        }

        r2TerrainMaterials[key] =
            material;

        return material;
    }

    private void R2BuildFallback(
        Transform areaRoot,
        Vector3 localPosition,
        Vector3 size,
        TerrainTrait trait,
        int seed)
    {
        GameObject fallback =
            new GameObject(
                R2TerrainRootName
            );

        fallback.transform.SetParent(
            areaRoot,
            false
        );

        R2CreateSolid(
            fallback.transform,
            "Fallback Feature",
            new Vector3(
                localPosition.x,
                size.y * 0.5f,
                localPosition.z
            ),
            size,
            Vector3.zero,
            trait,
            trait ==
                TerrainTrait.Blocking
                ? new Color(
                    0.29f,
                    0.30f,
                    0.30f,
                    1f
                  )
                : new Color(
                    0.32f,
                    0.27f,
                    0.21f,
                    1f
                  ),
            0.05f,
            0.06f
        );
    }

    private Vector3 R2Local(
        bool longAlongX,
        float along,
        float across)
    {
        return
            longAlongX
            ? new Vector3(
                along,
                0f,
                across
              )
            : new Vector3(
                across,
                0f,
                along
              );
    }

    private Vector3 R2AlongVector(
        bool alongX,
        float along,
        float across)
    {
        return
            alongX
            ? new Vector3(
                along,
                0f,
                across
              )
            : new Vector3(
                across,
                0f,
                along
              );
    }

    private Vector3 R2ScaleAlong(
        bool alongX,
        float length,
        float height,
        float thickness)
    {
        return
            alongX
            ? new Vector3(
                length,
                height,
                thickness
              )
            : new Vector3(
                thickness,
                height,
                length
              );
    }

    private float R2Noise01(
        int seed)
    {
        unchecked
        {
            int n =
                seed;

            n =
                (n << 13) ^
                n;

            int value =
                n *
                (n * n *
                 15731 +
                 789221) +
                1376312589;

            value &=
                0x7fffffff;

            return
                value /
                2147483647f;
        }
    }
}

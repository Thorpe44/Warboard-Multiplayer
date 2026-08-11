using UnityEngine;

// WARBOARD_V55_CLEAN_TERRAIN_KIT
public partial class GameController : MonoBehaviour
{
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

        GameObject kit =
            new GameObject(
                "V55 Clean Terrain - " +
                label +
                " " +
                seed
            );

        kit.transform.SetParent(
            areaRoot,
            false
        );

        kit.transform.localPosition =
            localPosition;

        kit.transform.localRotation =
            Quaternion.identity;

        bool longAlongX =
            size.x >= size.z;

        if (trait ==
            TerrainTrait.Blocking)
        {
            float wallHeight =
                Mathf.Max(
                    2.2f,
                    size.y
                );

            float longLength =
                Mathf.Max(
                    1.6f,
                    (longAlongX
                        ? size.x
                        : size.z) *
                    0.86f
                );

            float shortLength =
                Mathf.Max(
                    1.4f,
                    (longAlongX
                        ? size.z
                        : size.x) *
                    0.72f
                );

            // Clean L-shaped ruin. The actual cubes are the colliders, so
            // players can immediately see where a base may or may not finish.
            if (longAlongX)
            {
                V55CreateSolidTerrainPiece(
                    kit.transform,
                    "Tall Ruin Wall",
                    new Vector3(
                        0f,
                        wallHeight * 0.5f,
                        size.z * 0.30f
                    ),
                    new Vector3(
                        longLength,
                        wallHeight,
                        0.30f
                    ),
                    TerrainTrait.Blocking,
                    new Color(
                        0.47f,
                        0.49f,
                        0.52f
                    )
                );

                V55CreateSolidTerrainPiece(
                    kit.transform,
                    "Ruin Return Wall",
                    new Vector3(
                        -longLength * 0.42f,
                        wallHeight * 0.40f,
                        0f
                    ),
                    new Vector3(
                        0.30f,
                        wallHeight * 0.80f,
                        shortLength
                    ),
                    TerrainTrait.Blocking,
                    new Color(
                        0.42f,
                        0.44f,
                        0.47f
                    )
                );
            }
            else
            {
                V55CreateSolidTerrainPiece(
                    kit.transform,
                    "Tall Ruin Wall",
                    new Vector3(
                        size.x * 0.30f,
                        wallHeight * 0.5f,
                        0f
                    ),
                    new Vector3(
                        0.30f,
                        wallHeight,
                        longLength
                    ),
                    TerrainTrait.Blocking,
                    new Color(
                        0.47f,
                        0.49f,
                        0.52f
                    )
                );

                V55CreateSolidTerrainPiece(
                    kit.transform,
                    "Ruin Return Wall",
                    new Vector3(
                        0f,
                        wallHeight * 0.40f,
                        -longLength * 0.42f
                    ),
                    new Vector3(
                        shortLength,
                        wallHeight * 0.80f,
                        0.30f
                    ),
                    TerrainTrait.Blocking,
                    new Color(
                        0.42f,
                        0.44f,
                        0.47f
                    )
                );
            }

            // One obvious low broken section, kept against the ruin edge.
            V55CreateSolidTerrainPiece(
                kit.transform,
                "Broken Low Wall",
                longAlongX
                ? new Vector3(
                    longLength * 0.22f,
                    0.38f,
                    -size.z * 0.23f
                  )
                : new Vector3(
                    -size.x * 0.23f,
                    0.38f,
                    longLength * 0.22f
                  ),
                longAlongX
                ? new Vector3(
                    Mathf.Min(
                        1.7f,
                        longLength * 0.35f
                    ),
                    0.76f,
                    0.34f
                  )
                : new Vector3(
                    0.34f,
                    0.76f,
                    Mathf.Min(
                        1.7f,
                        longLength * 0.35f
                    )
                  ),
                TerrainTrait.Cover,
                new Color(
                    0.35f,
                    0.37f,
                    0.40f
                )
            );

            return;
        }

        // Cover pieces are intentionally low and sparse, with a central gap.
        float barricadeHeight =
            Mathf.Clamp(
                size.y * 0.55f,
                0.65f,
                0.95f
            );

        float available =
            longAlongX
            ? size.x
            : size.z;

        float sectionLength =
            Mathf.Max(
                0.9f,
                available * 0.34f
            );

        float separation =
            sectionLength * 0.62f;

        for (int i = -1;
             i <= 1;
             i += 2)
        {
            Vector3 position =
                longAlongX
                ? new Vector3(
                    i * separation,
                    barricadeHeight *
                        0.5f,
                    0f
                  )
                : new Vector3(
                    0f,
                    barricadeHeight *
                        0.5f,
                    i * separation
                  );

            Vector3 pieceSize =
                longAlongX
                ? new Vector3(
                    sectionLength,
                    barricadeHeight,
                    0.36f
                  )
                : new Vector3(
                    0.36f,
                    barricadeHeight,
                    sectionLength
                  );

            V55CreateSolidTerrainPiece(
                kit.transform,
                i < 0
                ? "Barricade A"
                : "Barricade B",
                position,
                pieceSize,
                TerrainTrait.Cover,
                new Color(
                    0.39f,
                    0.42f,
                    0.43f
                )
            );
        }
    }

    private void V55CreateSolidTerrainPiece(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        TerrainTrait trait,
        Color color)
    {
        GameObject piece =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        piece.name =
            "V55 " +
            name;

        piece.transform.SetParent(
            parent,
            false
        );

        piece.transform.localPosition =
            localPosition;

        piece.transform.localRotation =
            Quaternion.identity;

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

        SetObjectColor(
            piece,
            color
        );

        Renderer renderer =
            piece.GetComponent<
                Renderer
            >();

        if (renderer != null)
        {
            Material material =
                renderer.material;

            if (material != null)
            {
                material.color =
                    color;

                material.SetFloat(
                    "_Glossiness",
                    0.05f
                );
            }
        }
    }
}

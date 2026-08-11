using UnityEngine;

// WARBOARD_V57_RUIN_TERRAIN_KIT
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
                "V57 Battlefield Terrain - " +
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

        int variant =
            Mathf.Abs(
                seed * 31 +
                (label != null
                    ? label.Length * 7
                    : 0)
            ) % 4;

        if (trait ==
            TerrainTrait.Blocking)
        {
            V57BuildRuinedStructure(
                kit.transform,
                size,
                longAlongX,
                variant
            );

            return;
        }

        V57BuildIndustrialCover(
            kit.transform,
            size,
            longAlongX,
            variant
        );
    }

    private void V57BuildRuinedStructure(
        Transform root,
        Vector3 size,
        bool longAlongX,
        int variant)
    {
        Color stone =
            new Color(
                0.25f,
                0.27f,
                0.29f
            );

        Color stoneLight =
            new Color(
                0.36f,
                0.37f,
                0.38f
            );

        Color metal =
            new Color(
                0.17f,
                0.19f,
                0.21f
            );

        Color rust =
            new Color(
                0.38f,
                0.19f,
                0.10f
            );

        float height =
            Mathf.Clamp(
                Mathf.Max(
                    2.6f,
                    size.y +
                    0.4f
                ) +
                variant * 0.12f,
                2.6f,
                3.65f
            );

        float longLength =
            Mathf.Max(
                2.4f,
                (longAlongX
                    ? size.x
                    : size.z) *
                0.84f
            );

        float returnLength =
            Mathf.Max(
                1.8f,
                (longAlongX
                    ? size.z
                    : size.x) *
                0.68f
            );

        float longOffset =
            (longAlongX
                ? size.z
                : size.x) *
            0.24f;

        float returnOffset =
            -longLength *
            0.36f;

        V57CreateBrokenWallRun(
            root,
            "Main Ruin",
            longAlongX,
            longAlongX
                ? new Vector3(
                    0f,
                    0f,
                    longOffset
                  )
                : new Vector3(
                    longOffset,
                    0f,
                    0f
                  ),
            longLength,
            height,
            1.12f +
                variant * 0.08f,
            stone,
            stoneLight,
            metal,
            variant
        );

        V57CreateBrokenWallRun(
            root,
            "Return Ruin",
            !longAlongX,
            longAlongX
                ? new Vector3(
                    returnOffset,
                    0f,
                    -returnLength *
                        0.10f
                  )
                : new Vector3(
                    -returnLength *
                        0.10f,
                    0f,
                    returnOffset
                  ),
            returnLength,
            height *
                (variant % 2 == 0
                    ? 0.78f
                    : 0.68f),
            0.90f,
            stone,
            stoneLight,
            metal,
            variant + 1
        );

        // Corner column makes the L-shape read as a single ruined structure.
        Vector3 corner =
            longAlongX
            ? new Vector3(
                returnOffset,
                height * 0.46f,
                longOffset
              )
            : new Vector3(
                longOffset,
                height * 0.46f,
                returnOffset
              );

        V57CreateSolidPrimitive(
            root,
            PrimitiveType.Cylinder,
            "Corner Buttress",
            corner,
            new Vector3(
                0.42f,
                height * 0.46f,
                0.42f
            ),
            new Vector3(
                0f,
                22.5f,
                0f
            ),
            TerrainTrait.Blocking,
            stoneLight,
            0.02f,
            0.0f
        );

        // Low rubble is intentionally kept against the wall edges. It is
        // visible solid geometry, so there is no mystery about final placement.
        for (int i = 0;
             i < 3;
             i++)
        {
            float t =
                -0.30f +
                i * 0.29f;

            Vector3 rubblePosition =
                longAlongX
                ? new Vector3(
                    longLength * t,
                    0.18f +
                        i * 0.04f,
                    longOffset -
                        0.52f
                  )
                : new Vector3(
                    longOffset -
                        0.52f,
                    0.18f +
                        i * 0.04f,
                    longLength * t
                  );

            Vector3 rubbleScale =
                new Vector3(
                    0.55f +
                        i * 0.12f,
                    0.30f +
                        i * 0.05f,
                    0.42f
                );

            if (!longAlongX)
            {
                rubbleScale =
                    new Vector3(
                        rubbleScale.z,
                        rubbleScale.y,
                        rubbleScale.x
                    );
            }

            V57CreateSolidPrimitive(
                root,
                PrimitiveType.Cube,
                "Rubble " + i,
                rubblePosition,
                rubbleScale,
                new Vector3(
                    0f,
                    (variant * 17f) +
                        i * 23f,
                    (i - 1) * 5f
                ),
                TerrainTrait.Cover,
                i == 1
                    ? rust
                    : stoneLight,
                0.02f,
                i == 1
                    ? 0.10f
                    : 0f
            );
        }

        // Rusted structural beam attached to the ruin. Decorative only:
        // collider is removed, so it never changes the obvious solid footprint.
        Vector3 beamPosition =
            longAlongX
            ? new Vector3(
                longLength * 0.12f,
                height * 0.72f,
                longOffset - 0.17f
              )
            : new Vector3(
                longOffset - 0.17f,
                height * 0.72f,
                longLength * 0.12f
              );

        Vector3 beamScale =
            longAlongX
            ? new Vector3(
                Mathf.Min(
                    2.4f,
                    longLength * 0.42f
                ),
                0.12f,
                0.10f
              )
            : new Vector3(
                0.10f,
                0.12f,
                Mathf.Min(
                    2.4f,
                    longLength * 0.42f
                )
              );

        V57CreateDecorativePrimitive(
            root,
            PrimitiveType.Cube,
            "Rusted Beam",
            beamPosition,
            beamScale,
            Vector3.zero,
            rust,
            0.36f,
            0.20f
        );
    }

    private void V57CreateBrokenWallRun(
        Transform root,
        string name,
        bool alongX,
        Vector3 basePosition,
        float length,
        float height,
        float opening,
        Color stone,
        Color stoneLight,
        Color metal,
        int variant)
    {
        float thickness = 0.28f;

        opening =
            Mathf.Clamp(
                opening,
                0.72f,
                Mathf.Max(
                    0.72f,
                    length * 0.42f
                )
            );

        float remaining =
            Mathf.Max(
                0.8f,
                length - opening
            );

        float leftLength =
            remaining *
            (variant % 2 == 0
                ? 0.54f
                : 0.45f);

        float rightLength =
            remaining -
            leftLength;

        float leftHeight =
            height;

        float rightHeight =
            height *
            (variant % 3 == 0
                ? 0.70f
                : 0.82f);

        float leftCentre =
            -opening * 0.5f -
            leftLength * 0.5f;

        float rightCentre =
            opening * 0.5f +
            rightLength * 0.5f;

        Vector3 leftPosition =
            basePosition +
            (alongX
                ? new Vector3(
                    leftCentre,
                    leftHeight * 0.5f,
                    0f
                  )
                : new Vector3(
                    0f,
                    leftHeight * 0.5f,
                    leftCentre
                  ));

        Vector3 rightPosition =
            basePosition +
            (alongX
                ? new Vector3(
                    rightCentre,
                    rightHeight * 0.5f,
                    0f
                  )
                : new Vector3(
                    0f,
                    rightHeight * 0.5f,
                    rightCentre
                  ));

        Vector3 leftScale =
            alongX
            ? new Vector3(
                leftLength,
                leftHeight,
                thickness
              )
            : new Vector3(
                thickness,
                leftHeight,
                leftLength
              );

        Vector3 rightScale =
            alongX
            ? new Vector3(
                rightLength,
                rightHeight,
                thickness
              )
            : new Vector3(
                thickness,
                rightHeight,
                rightLength
              );

        V57CreateSolidPrimitive(
            root,
            PrimitiveType.Cube,
            name + " Left",
            leftPosition,
            leftScale,
            Vector3.zero,
            TerrainTrait.Blocking,
            stone,
            0.03f,
            0f
        );

        V57CreateSolidPrimitive(
            root,
            PrimitiveType.Cube,
            name + " Right",
            rightPosition,
            rightScale,
            new Vector3(
                0f,
                0f,
                variant % 2 == 0
                    ? 1.5f
                    : -1.5f
            ),
            TerrainTrait.Blocking,
            stoneLight,
            0.03f,
            0f
        );

        // A damaged lintel leaves an obvious model-width doorway beneath it.
        float lintelHeight = 0.25f;

        Vector3 lintelPosition =
            basePosition +
            (alongX
                ? new Vector3(
                    0f,
                    height -
                        lintelHeight *
                        0.5f,
                    0f
                  )
                : new Vector3(
                    0f,
                    height -
                        lintelHeight *
                        0.5f,
                    0f
                  ));

        Vector3 lintelScale =
            alongX
            ? new Vector3(
                opening +
                    0.26f,
                lintelHeight,
                thickness
              )
            : new Vector3(
                thickness,
                lintelHeight,
                opening +
                    0.26f
              );

        V57CreateSolidPrimitive(
            root,
            PrimitiveType.Cube,
            name + " Lintel",
            lintelPosition,
            lintelScale,
            new Vector3(
                0f,
                0f,
                variant % 2 == 0
                    ? 2.5f
                    : -2.5f
            ),
            TerrainTrait.Blocking,
            metal,
            0.28f,
            0.16f
        );

        // Buttresses make the ruin less like a plain slab.
        for (int side = -1;
             side <= 1;
             side += 2)
        {
            float axis =
                side *
                (length * 0.5f -
                 0.15f);

            Vector3 position =
                basePosition +
                (alongX
                    ? new Vector3(
                        axis,
                        height * 0.33f,
                        0f
                      )
                    : new Vector3(
                        0f,
                        height * 0.33f,
                        axis
                      ));

            V57CreateSolidPrimitive(
                root,
                PrimitiveType.Cylinder,
                name +
                    " Buttress " +
                    side,
                position,
                new Vector3(
                    0.30f,
                    height * 0.33f,
                    0.30f
                ),
                new Vector3(
                    0f,
                    22.5f,
                    0f
                ),
                TerrainTrait.Blocking,
                stoneLight,
                0.02f,
                0f
            );
        }
    }

    private void V57BuildIndustrialCover(
        Transform root,
        Vector3 size,
        bool longAlongX,
        int variant)
    {
        Color armour =
            new Color(
                0.20f,
                0.23f,
                0.24f
            );

        Color armourLight =
            new Color(
                0.31f,
                0.34f,
                0.35f
            );

        Color rust =
            new Color(
                0.43f,
                0.22f,
                0.10f
            );

        float available =
            longAlongX
            ? size.x
            : size.z;

        float panelLength =
            Mathf.Clamp(
                available * 0.29f,
                0.95f,
                2.4f
            );

        float gap =
            Mathf.Clamp(
                available * 0.18f,
                0.85f,
                1.45f
            );

        float height =
            0.78f +
            variant * 0.05f;

        for (int side = -1;
             side <= 1;
             side += 2)
        {
            float axis =
                side *
                (gap * 0.5f +
                 panelLength * 0.5f);

            Vector3 panelPosition =
                longAlongX
                ? new Vector3(
                    axis,
                    height * 0.5f,
                    0f
                  )
                : new Vector3(
                    0f,
                    height * 0.5f,
                    axis
                  );

            Vector3 panelScale =
                longAlongX
                ? new Vector3(
                    panelLength,
                    height,
                    0.30f
                  )
                : new Vector3(
                    0.30f,
                    height,
                    panelLength
                  );

            V57CreateSolidPrimitive(
                root,
                PrimitiveType.Cube,
                "Armoured Barricade " +
                    side,
                panelPosition,
                panelScale,
                Vector3.zero,
                TerrainTrait.Cover,
                side < 0
                    ? armour
                    : armourLight,
                0.24f,
                0.18f
            );

            // Angled support foot, attached to each barricade.
            Vector3 footPosition =
                panelPosition +
                (longAlongX
                    ? new Vector3(
                        side *
                            panelLength *
                            0.28f,
                        -height *
                            0.30f,
                        0.26f
                      )
                    : new Vector3(
                        0.26f,
                        -height *
                            0.30f,
                        side *
                            panelLength *
                            0.28f
                      ));

            Vector3 footScale =
                longAlongX
                ? new Vector3(
                    0.34f,
                    0.28f,
                    0.62f
                  )
                : new Vector3(
                    0.62f,
                    0.28f,
                    0.34f
                  );

            V57CreateSolidPrimitive(
                root,
                PrimitiveType.Cube,
                "Barricade Foot " +
                    side,
                footPosition,
                footScale,
                new Vector3(
                    longAlongX
                        ? 18f
                        : 0f,
                    0f,
                    longAlongX
                        ? 0f
                        : -18f
                ),
                TerrainTrait.Cover,
                armour,
                0.18f,
                0.16f
            );
        }

        // Supply crate at one outer edge adds a readable bit of life without
        // filling the clear central passage.
        Vector3 cratePosition =
            longAlongX
            ? new Vector3(
                available * 0.37f,
                0.32f,
                -0.34f
              )
            : new Vector3(
                -0.34f,
                0.32f,
                available * 0.37f
              );

        V57CreateSolidPrimitive(
            root,
            PrimitiveType.Cube,
            "Supply Crate",
            cratePosition,
            new Vector3(
                0.72f,
                0.64f,
                0.72f
            ),
            new Vector3(
                0f,
                12f +
                    variant * 7f,
                0f
            ),
            TerrainTrait.Cover,
            rust,
            0.08f,
            0.02f
        );

        // Decorative hazard stripe attached to one barricade. No collider.
        Vector3 stripePosition =
            longAlongX
            ? new Vector3(
                -gap * 0.5f -
                    panelLength * 0.55f,
                height * 0.55f,
                -0.17f
              )
            : new Vector3(
                -0.17f,
                height * 0.55f,
                -gap * 0.5f -
                    panelLength * 0.55f
              );

        Vector3 stripeScale =
            longAlongX
            ? new Vector3(
                panelLength * 0.38f,
                0.10f,
                0.035f
              )
            : new Vector3(
                0.035f,
                0.10f,
                panelLength * 0.38f
              );

        V57CreateDecorativePrimitive(
            root,
            PrimitiveType.Cube,
            "Hazard Stripe",
            stripePosition,
            stripeScale,
            Vector3.zero,
            new Color(
                0.82f,
                0.52f,
                0.12f
            ),
            0.04f,
            0f
        );
    }

    private GameObject V57CreateSolidPrimitive(
        Transform parent,
        PrimitiveType primitive,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        TerrainTrait trait,
        Color color,
        float metallic,
        float smoothness)
    {
        GameObject piece =
            GameObject.CreatePrimitive(
                primitive
            );

        piece.name =
            "V57 Solid - " +
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

        V57ApplyTerrainMaterial(
            piece,
            color,
            metallic,
            smoothness
        );

        return piece;
    }

    private void V57CreateDecorativePrimitive(
        Transform parent,
        PrimitiveType primitive,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        Color color,
        float metallic,
        float smoothness)
    {
        GameObject piece =
            GameObject.CreatePrimitive(
                primitive
            );

        piece.name =
            "V57 Detail - " +
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
            piece.GetComponent<
                Collider
            >();

        if (collider != null)
            Destroy(collider);

        V57ApplyTerrainMaterial(
            piece,
            color,
            metallic,
            smoothness
        );
    }

    private void V57ApplyTerrainMaterial(
        GameObject piece,
        Color color,
        float metallic,
        float smoothness)
    {
        if (piece == null)
            return;

        SetObjectColor(
            piece,
            color
        );

        Renderer renderer =
            piece.GetComponent<
                Renderer
            >();

        if (renderer == null)
            return;

        Material material =
            renderer.material;

        if (material == null)
            return;

        material.color = color;

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
    }
}

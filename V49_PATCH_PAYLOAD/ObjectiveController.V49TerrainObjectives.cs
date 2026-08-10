using UnityEngine;

public partial class ObjectiveController
{
    private TerrainFeature terrainObjectiveArea;
    private LineRenderer terrainObjectiveOutline;

    public TerrainFeature TerrainObjectiveArea
    {
        get { return terrainObjectiveArea; }
    }

    public bool UsesTerrainObjectiveArea
    {
        get { return terrainObjectiveArea != null; }
    }

    public void BindTerrainObjectiveArea(
        TerrainFeature feature)
    {
        if (feature == null)
            return;

        terrainObjectiveArea = feature;
        feature.SetMissionObjectiveArea(true);

        ObjectiveTerrainLink49 link =
            feature.GetComponent<
                ObjectiveTerrainLink49
            >();

        if (link == null)
        {
            link =
                feature.gameObject.AddComponent<
                    ObjectiveTerrainLink49
                >();
        }

        link.Initialize(this);

        Bounds bounds =
            feature.ObjectiveAreaBounds;

        transform.position =
            new Vector3(
                bounds.center.x,
                0f,
                bounds.center.z
            );

        SphereCollider clickCollider =
            GetComponent<SphereCollider>();

        if (clickCollider != null)
        {
            clickCollider.radius =
                Mathf.Max(
                    1.15f,
                    Mathf.Max(
                        bounds.extents.x,
                        bounds.extents.z
                    )
                );
        }

        if (markerRenderer != null)
            markerRenderer.gameObject.SetActive(false);

        Transform oldNode =
            transform.Find(
                "V45_ObjectiveNode"
            );

        if (oldNode != null)
            oldNode.gameObject.SetActive(false);

        WarboardV45ObjectivePulse pulse =
            GetComponent<
                WarboardV45ObjectivePulse
            >();

        if (pulse != null)
            pulse.enabled = false;

        if (statusText != null)
        {
            statusText.transform.position =
                new Vector3(
                    bounds.center.x,
                    bounds.max.y + 0.55f,
                    bounds.center.z
                );
        }

        CreateTerrainObjectiveOutline(
            bounds
        );
    }

    private void CreateTerrainObjectiveOutline(
        Bounds bounds)
    {
        if (terrainObjectiveOutline != null)
            Object.Destroy(
                terrainObjectiveOutline.gameObject
            );

        GameObject outlineObject =
            new GameObject(
                "V49 Terrain Objective Area"
            );

        outlineObject.transform.SetParent(
            transform,
            true
        );

        terrainObjectiveOutline =
            outlineObject.AddComponent<
                LineRenderer
            >();

        terrainObjectiveOutline.useWorldSpace = true;
        terrainObjectiveOutline.loop = true;
        terrainObjectiveOutline.positionCount = 4;
        terrainObjectiveOutline.widthMultiplier = 0.10f;
        terrainObjectiveOutline.numCornerVertices = 2;
        terrainObjectiveOutline.numCapVertices = 2;

        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );

        if (shader != null)
        {
            terrainObjectiveOutline.material =
                new Material(shader);
        }

        Color color =
            new Color(
                0.95f,
                0.72f,
                0.18f,
                0.90f
            );

        terrainObjectiveOutline.startColor = color;
        terrainObjectiveOutline.endColor = color;

        float y = 0.065f;

        terrainObjectiveOutline.SetPosition(
            0,
            new Vector3(
                bounds.min.x,
                y,
                bounds.min.z
            )
        );

        terrainObjectiveOutline.SetPosition(
            1,
            new Vector3(
                bounds.max.x,
                y,
                bounds.min.z
            )
        );

        terrainObjectiveOutline.SetPosition(
            2,
            new Vector3(
                bounds.max.x,
                y,
                bounds.max.z
            )
        );

        terrainObjectiveOutline.SetPosition(
            3,
            new Vector3(
                bounds.min.x,
                y,
                bounds.max.z
            )
        );
    }
}

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

        TerrainAreaFootprint50 footprint =
            feature.GetComponent<
                TerrainAreaFootprint50
            >();

        if (footprint != null)
        {
            footprint.SetObjective(
                true,
                MissionRole
            );
        }

        LinkTerrainObjectToObjective(feature);

        TerrainFeature[] childFeatures =
            feature.GetComponentsInChildren<
                TerrainFeature
            >(true);

        foreach (TerrainFeature child
            in childFeatures)
        {
            LinkTerrainObjectToObjective(child);
        }

        Bounds bounds =
            feature.ObjectiveAreaBounds;

        transform.position =
            new Vector3(
                bounds.center.x,
                0f,
                bounds.center.z
            );

        // V50: the terrain-area collider is the clickable target. The old
        // spherical objective collider would create an invisible circular
        // click/control zone, which is exactly what 11th edition removes.
        SphereCollider clickCollider =
            GetComponent<SphereCollider>();

        if (clickCollider != null)
            clickCollider.enabled = false;

        if (markerRenderer != null)
            markerRenderer.gameObject.SetActive(false);

        Transform oldMarker =
            transform.Find("ObjectiveMarker");

        if (oldMarker != null)
            oldMarker.gameObject.SetActive(false);

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

        if (terrainObjectiveOutline != null)
        {
            Object.Destroy(
                terrainObjectiveOutline.gameObject
            );
            terrainObjectiveOutline = null;
        }

        if (statusText != null)
        {
            statusText.transform.position =
                new Vector3(
                    bounds.center.x,
                    0.62f,
                    bounds.center.z
                );

            statusText.characterSize = 0.038f;
        }
    }

    private void LinkTerrainObjectToObjective(
        TerrainFeature feature)
    {
        if (feature == null)
            return;

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
    }

    // Kept only for compatibility with an already-installed V49 project.
    // V50 uses the footprint's own visible border instead.
    private void CreateTerrainObjectiveOutline(
        Bounds bounds)
    {
    }
}

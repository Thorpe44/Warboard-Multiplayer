using UnityEngine;

public partial class TerrainFeature
{
    public void SetMissionObjectiveArea(bool value)
    {
        IsMissionObjectiveArea = value;
    }

    public Bounds ObjectiveAreaBounds
    {
        get
        {
            TerrainAreaFootprint50 footprint =
                GetComponent<
                    TerrainAreaFootprint50
                >();

            if (footprint != null)
                return footprint.WorldBounds;

            Collider col = GetComponent<Collider>();

            if (col != null)
                return col.bounds;

            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
                return renderer.bounds;

            return new Bounds(
                transform.position,
                transform.lossyScale
            );
        }
    }

    public float HorizontalDistanceTo(Vector3 point)
    {
        TerrainAreaFootprint50 footprint =
            GetComponent<
                TerrainAreaFootprint50
            >();

        if (footprint != null)
            return footprint.HorizontalDistanceTo(point);

        Bounds bounds = ObjectiveAreaBounds;

        float closestX =
            Mathf.Clamp(
                point.x,
                bounds.min.x,
                bounds.max.x
            );

        float closestZ =
            Mathf.Clamp(
                point.z,
                bounds.min.z,
                bounds.max.z
            );

        float dx = point.x - closestX;
        float dz = point.z - closestZ;

        return Mathf.Sqrt(
            dx * dx +
            dz * dz
        );
    }

    public bool ModelTouchesObjectiveArea(
        ModelToken model)
    {
        if (model == null ||
            !model.IsAlive)
        {
            return false;
        }

        TerrainAreaFootprint50 footprint =
            GetComponent<
                TerrainAreaFootprint50
            >();

        if (footprint != null)
            return footprint.ModelTouches(model);

        Bounds bounds = ObjectiveAreaBounds;
        Vector3 point = model.transform.position;

        float closestX =
            Mathf.Clamp(
                point.x,
                bounds.min.x,
                bounds.max.x
            );

        float closestZ =
            Mathf.Clamp(
                point.z,
                bounds.min.z,
                bounds.max.z
            );

        float dx = point.x - closestX;
        float dz = point.z - closestZ;

        float radius =
            Mathf.Max(
                0f,
                model.BaseRadiusInches
            );

        return
            dx * dx +
            dz * dz <=
            radius * radius +
            0.0001f;
    }
}

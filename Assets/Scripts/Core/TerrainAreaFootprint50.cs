using System.Collections.Generic;
using UnityEngine;

public enum TerrainAreaShape50
{
    Rectangle,
    RightTriangle
}

/// <summary>
/// Physical 11th-edition Terrain Area footprint.
/// The footprint, not the decorative scenery sitting on it, defines the area.
/// Standard matched-play sizes are created by GameController V50.
/// </summary>
public sealed class TerrainAreaFootprint50 : MonoBehaviour
{
    public TerrainAreaShape50 Shape { get; private set; }
    public float Width { get; private set; }
    public float Depth { get; private set; }
    public bool IsObjective { get; private set; }
    public MissionObjectiveRole ObjectiveRole { get; private set; }

    private readonly List<Vector2> localPolygon =
        new List<Vector2>();

    private MeshRenderer fillRenderer;
    private LineRenderer outlineRenderer;
    private MeshCollider clickCollider;

    public void Initialize(
        TerrainAreaShape50 shape,
        float width,
        float depth)
    {
        Shape = shape;
        Width = Mathf.Max(0.25f, width);
        Depth = Mathf.Max(0.25f, depth);

        BuildLocalPolygon();
        BuildMeshAndCollider();
        BuildOutline();
        ApplyVisualState();
    }

    public void SetObjective(
        bool value,
        MissionObjectiveRole role)
    {
        IsObjective = value;
        ObjectiveRole = role;
        ApplyVisualState();
    }

    public Bounds WorldBounds
    {
        get
        {
            Vector3[] points = WorldPolygon();

            if (points.Length == 0)
            {
                return new Bounds(
                    transform.position,
                    Vector3.zero
                );
            }

            Bounds bounds =
                new Bounds(
                    points[0],
                    Vector3.zero
                );

            for (int i = 1; i < points.Length; i++)
                bounds.Encapsulate(points[i]);

            bounds.Encapsulate(
                new Vector3(
                    bounds.center.x,
                    0.08f,
                    bounds.center.z
                )
            );

            return bounds;
        }
    }

    public float HorizontalDistanceTo(
        Vector3 worldPoint)
    {
        Vector3 local3 =
            transform.InverseTransformPoint(
                worldPoint
            );

        Vector2 localPoint =
            new Vector2(local3.x, local3.z);

        if (PointInside(localPoint))
            return 0f;

        float best = float.MaxValue;

        for (int i = 0; i < localPolygon.Count; i++)
        {
            Vector2 a = localPolygon[i];
            Vector2 b =
                localPolygon[
                    (i + 1) % localPolygon.Count
                ];

            best =
                Mathf.Min(
                    best,
                    DistancePointToSegment(
                        localPoint,
                        a,
                        b
                    )
                );
        }

        return best;
    }

    public bool ModelTouches(
        ModelToken model)
    {
        if (model == null || !model.IsAlive)
            return false;

        Vector3 local3 =
            transform.InverseTransformPoint(
                model.transform.position
            );

        Vector2 localPoint =
            new Vector2(local3.x, local3.z);

        if (PointInside(localPoint))
            return true;

        float radius =
            Mathf.Max(
                0f,
                model.BaseRadiusInches
            );

        if (radius <= 0f)
            return false;

        for (int i = 0; i < localPolygon.Count; i++)
        {
            Vector2 a = localPolygon[i];
            Vector2 b =
                localPolygon[
                    (i + 1) % localPolygon.Count
                ];

            if (DistancePointToSegment(
                    localPoint,
                    a,
                    b) <=
                radius + 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3[] WorldPolygon()
    {
        Vector3[] result =
            new Vector3[localPolygon.Count];

        for (int i = 0; i < localPolygon.Count; i++)
        {
            Vector2 point = localPolygon[i];

            result[i] =
                transform.TransformPoint(
                    new Vector3(
                        point.x,
                        0.055f,
                        point.y
                    )
                );
        }

        return result;
    }

    private void BuildLocalPolygon()
    {
        localPolygon.Clear();

        float halfW = Width * 0.5f;
        float halfD = Depth * 0.5f;

        if (Shape == TerrainAreaShape50.RightTriangle)
        {
            localPolygon.Add(
                new Vector2(-halfW, -halfD)
            );
            localPolygon.Add(
                new Vector2(-halfW, halfD)
            );
            localPolygon.Add(
                new Vector2(halfW, -halfD)
            );
            return;
        }

        localPolygon.Add(
            new Vector2(-halfW, -halfD)
        );
        localPolygon.Add(
            new Vector2(-halfW, halfD)
        );
        localPolygon.Add(
            new Vector2(halfW, halfD)
        );
        localPolygon.Add(
            new Vector2(halfW, -halfD)
        );
    }

    private void BuildMeshAndCollider()
    {
        Mesh mesh = BuildPrismMesh();

        MeshFilter filter =
            gameObject.GetComponent<MeshFilter>();

        if (filter == null)
            filter = gameObject.AddComponent<MeshFilter>();

        filter.sharedMesh = mesh;

        fillRenderer =
            gameObject.GetComponent<MeshRenderer>();

        if (fillRenderer == null)
            fillRenderer =
                gameObject.AddComponent<MeshRenderer>();

        Shader shader =
            Shader.Find("Sprites/Default") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Standard");

        if (shader != null)
        {
            Material material =
                new Material(shader);

            fillRenderer.sharedMaterial = material;
        }

        // V51 DEPLOYMENT FIX:
        // The terrain-area footprint is a rules/click surface, not physical
        // scenery. CanPlaceModel checks TerrainFeature on the collider object
        // itself; keeping this MeshCollider on the same GameObject as the
        // TerrainFeature made the whole footprint participate in placement
        // validation. Put the click collider on a child with no TerrainFeature
        // so models can deploy/move across clear parts of the footprint while
        // raycasts still resolve the parent TerrainFeature via
        // GetComponentInParent<TerrainFeature>(). Actual scenery children keep
        // their own TerrainFeature + colliders and still block where required.
        MeshCollider legacyRootCollider =
            gameObject.GetComponent<MeshCollider>();

        if (legacyRootCollider != null)
            Object.Destroy(legacyRootCollider);

        Transform oldClickSurface =
            transform.Find("V51 Terrain Area Click Surface");

        GameObject clickSurface;

        if (oldClickSurface != null)
        {
            clickSurface = oldClickSurface.gameObject;
        }
        else
        {
            clickSurface =
                new GameObject(
                    "V51 Terrain Area Click Surface"
                );

            clickSurface.transform.SetParent(
                transform,
                false
            );
        }

        clickSurface.transform.localPosition =
            Vector3.zero;
        clickSurface.transform.localRotation =
            Quaternion.identity;
        clickSurface.transform.localScale =
            Vector3.one;

        clickCollider =
            clickSurface.GetComponent<MeshCollider>();

        if (clickCollider == null)
            clickCollider =
                clickSurface.AddComponent<MeshCollider>();

        clickCollider.sharedMesh = mesh;
        clickCollider.convex = true;
        clickCollider.isTrigger = true;
    }

    private Mesh BuildPrismMesh()
    {
        int count = localPolygon.Count;
        float bottomY = 0.012f;
        float topY = 0.045f;

        Vector3[] vertices =
            new Vector3[count * 2];

        for (int i = 0; i < count; i++)
        {
            Vector2 p = localPolygon[i];

            vertices[i] =
                new Vector3(
                    p.x,
                    bottomY,
                    p.y
                );

            vertices[i + count] =
                new Vector3(
                    p.x,
                    topY,
                    p.y
                );
        }

        List<int> triangles =
            new List<int>();

        // Top face.
        for (int i = 1; i < count - 1; i++)
        {
            triangles.Add(count);
            triangles.Add(count + i);
            triangles.Add(count + i + 1);
        }

        // Bottom face (reverse winding).
        for (int i = 1; i < count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i);
        }

        // Side faces.
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            triangles.Add(i);
            triangles.Add(next + count);
            triangles.Add(i + count);

            triangles.Add(i);
            triangles.Add(next);
            triangles.Add(next + count);
        }

        Mesh mesh = new Mesh();
        mesh.name = "V50 Terrain Area Footprint";
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void BuildOutline()
    {
        GameObject go =
            new GameObject("V50 Terrain Area Outline");

        go.transform.SetParent(
            transform,
            false
        );

        outlineRenderer =
            go.AddComponent<LineRenderer>();

        outlineRenderer.useWorldSpace = false;
        outlineRenderer.loop = true;
        outlineRenderer.positionCount =
            localPolygon.Count;
        outlineRenderer.widthMultiplier = 0.075f;
        outlineRenderer.numCornerVertices = 2;
        outlineRenderer.numCapVertices = 2;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
            outlineRenderer.material =
                new Material(shader);

        for (int i = 0; i < localPolygon.Count; i++)
        {
            Vector2 p = localPolygon[i];

            outlineRenderer.SetPosition(
                i,
                new Vector3(
                    p.x,
                    0.060f,
                    p.y
                )
            );
        }
    }

    private void ApplyVisualState()
    {
        // WARBOARD_V55_WALKABLE_FOOTPRINT_VISUAL
        // The tinted floor is the walkable Terrain Area. Only the solid
        // scenery visibly sitting on it blocks a model's final base.
        Color fill =
            IsObjective
            ? new Color(
                0.16f,
                0.25f,
                0.27f,
                0.28f
              )
            : new Color(
                0.11f,
                0.22f,
                0.25f,
                0.23f
              );

        Color outline =
            IsObjective
            ? new Color(
                1.00f,
                0.73f,
                0.16f,
                0.98f
              )
            : new Color(
                0.26f,
                0.76f,
                0.82f,
                0.92f
              );

        if (fillRenderer != null &&
            fillRenderer.sharedMaterial != null)
        {
            fillRenderer.sharedMaterial.color = fill;
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.startColor = outline;
            outlineRenderer.endColor = outline;
        }
    }

    private bool PointInside(Vector2 point)
    {
        bool inside = false;
        int j = localPolygon.Count - 1;

        for (int i = 0; i < localPolygon.Count; i++)
        {
            Vector2 pi = localPolygon[i];
            Vector2 pj = localPolygon[j];

            bool crosses =
                ((pi.y > point.y) !=
                 (pj.y > point.y)) &&
                (point.x <
                 (pj.x - pi.x) *
                 (point.y - pi.y) /
                 (pj.y - pi.y) +
                 pi.x);

            if (crosses)
                inside = !inside;

            j = i;
        }

        return inside;
    }

    private static float DistancePointToSegment(
        Vector2 point,
        Vector2 a,
        Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSq = ab.sqrMagnitude;

        if (lengthSq <= 0.00001f)
            return Vector2.Distance(point, a);

        float t =
            Vector2.Dot(
                point - a,
                ab
            ) /
            lengthSq;

        t = Mathf.Clamp01(t);

        Vector2 closest =
            a + ab * t;

        return Vector2.Distance(
            point,
            closest
        );
    }
}

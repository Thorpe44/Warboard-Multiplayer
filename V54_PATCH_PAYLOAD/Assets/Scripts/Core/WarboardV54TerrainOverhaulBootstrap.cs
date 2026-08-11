using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WarboardV54TerrainDescriptor : MonoBehaviour
{
    [TextArea]
    public string Title;

    [TextArea]
    public string RulesText;

    public bool SolidEndPosition;
}

public sealed class WarboardV54MissionCardAdjusted : MonoBehaviour
{
    private bool builtStand;

    public void Apply(Bounds boardBounds, Material woodMaterial)
    {
        Transform t = transform;
        t.localScale *= 0.18f;

        float z = boardBounds.max.z + 2.2f;
        t.position = new Vector3(0f, 3.2f, z);
        t.rotation = Quaternion.Euler(75f, 180f, 0f);

        if (builtStand)
            return;

        builtStand = true;

        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "V54 Mission Card Stand";
        stand.transform.SetParent(t.parent, true);
        stand.transform.position = t.position + new Vector3(0f, -0.85f, 0.25f);
        stand.transform.rotation = Quaternion.identity;
        stand.transform.localScale = new Vector3(2.1f, 0.18f, 0.9f);

        MeshRenderer mr = stand.GetComponent<MeshRenderer>();
        if (mr != null && woodMaterial != null)
            mr.sharedMaterial = woodMaterial;

        Collider col = stand.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }
    }
}

public sealed class WarboardV54TerrainOverhaulBootstrap : MonoBehaviour
{
    private const string VisualRootName = "V54 Visual Root";
    private const string ManagedTag = "V54_Managed";

    private string lastSignature = string.Empty;
    private float nextRefreshTime;
    private WarboardV54TerrainDescriptor hoveredDescriptor;
    private GUIStyle tooltipStyle;

    private Texture2D[] terrainTextures;
    private Material stoneMaterial;
    private Material rustMaterial;
    private Material metalMaterial;
    private Material barricadeMaterial;
    private Material woodMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object.FindAnyObjectByType<WarboardV54TerrainOverhaulBootstrap>() != null)
            return;

        GameObject go = new GameObject("WARBOARD V54 Terrain Overhaul");
        DontDestroyOnLoad(go);
        go.AddComponent<WarboardV54TerrainOverhaulBootstrap>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        terrainTextures = Resources.LoadAll<Texture2D>("WarboardV54Terrain");

        tooltipStyle = new GUIStyle(GUI.skin.label);
        tooltipStyle.wordWrap = true;
        tooltipStyle.richText = true;
        tooltipStyle.alignment = TextAnchor.UpperLeft;
        tooltipStyle.normal.textColor = Color.white;
        tooltipStyle.fontSize = 15;
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return new WaitForSeconds(0.35f);
        ForceRefresh();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + 0.9f;
            RefreshIfNeeded();
        }

        UpdateHoveredTerrain();
    }

    private void OnGUI()
    {
        if (hoveredDescriptor == null)
            return;

        string body = hoveredDescriptor.Title;
        if (!string.IsNullOrWhiteSpace(hoveredDescriptor.RulesText))
            body += "\n\n" + hoveredDescriptor.RulesText;

        float width = Mathf.Min(470f, Screen.width * 0.34f);
        float height = tooltipStyle.CalcHeight(new GUIContent(body), width - 20f) + 20f;

        Rect panel = new Rect(Screen.width - width - 24f, 92f, width, height);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(
            new Rect(panel.x + 10f, panel.y + 10f, panel.width - 20f, panel.height - 20f),
            body,
            tooltipStyle
        );
    }

    private void RefreshIfNeeded()
    {
        TerrainAreaFootprint50[] footprints = FindFootprints();
        string signature = BuildSignature(footprints);
        if (signature == lastSignature)
            return;

        lastSignature = signature;
        ApplyTerrainOverhaul(footprints);
        ApplyMissionCardFix(footprints);
    }

    private void ForceRefresh()
    {
        TerrainAreaFootprint50[] footprints = FindFootprints();
        lastSignature = BuildSignature(footprints);
        ApplyTerrainOverhaul(footprints);
        ApplyMissionCardFix(footprints);
    }

    private TerrainAreaFootprint50[] FindFootprints()
    {
        TerrainAreaFootprint50[] result =
            UnityEngine.Object.FindObjectsByType<TerrainAreaFootprint50>(FindObjectsInactive.Exclude);

        Array.Sort(
            result,
            delegate (TerrainAreaFootprint50 a, TerrainAreaFootprint50 b)
            {
                if (a == null && b == null) return 0;
                if (a == null) return -1;
                if (b == null) return 1;

                int nameCmp = string.CompareOrdinal(a.name, b.name);
                if (nameCmp != 0) return nameCmp;

                int xCmp = a.transform.position.x.CompareTo(b.transform.position.x);
                if (xCmp != 0) return xCmp;

                return a.transform.position.z.CompareTo(b.transform.position.z);
            }
        );

        return result;
    }

    private string BuildSignature(TerrainAreaFootprint50[] footprints)
    {
        if (footprints == null || footprints.Length == 0)
            return "NONE";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(footprints.Length);

        for (int i = 0; i < footprints.Length; i++)
        {
            TerrainAreaFootprint50 fp = footprints[i];
            if (fp == null)
                continue;

            Vector3 p = fp.transform.position;
            sb.Append('|');
            sb.Append((int)(p.x * 10f));
            sb.Append(',');
            sb.Append((int)(p.z * 10f));
            sb.Append(',');
            sb.Append((int)fp.transform.eulerAngles.y);
            sb.Append(',');
            sb.Append(fp.Shape.ToString());
            sb.Append(',');
            sb.Append((int)(fp.Width * 10f));
            sb.Append(',');
            sb.Append((int)(fp.Depth * 10f));
            sb.Append(',');
            sb.Append(fp.IsObjective ? 1 : 0);
        }

        return sb.ToString();
    }

    private void ApplyTerrainOverhaul(TerrainAreaFootprint50[] footprints)
    {
        if (footprints == null)
            return;

        for (int i = 0; i < footprints.Length; i++)
        {
            TerrainAreaFootprint50 footprint = footprints[i];
            if (footprint == null)
                continue;

            StyleFootprintPresentation(footprint, i);
            SuppressLegacyScenery(footprint.transform);
            RebuildVisualRoot(footprint, i);
            AttachAreaDescriptor(footprint, i);
        }

        Debug.Log("WARBOARD V54: terrain visual overhaul applied to " + footprints.Length + " terrain areas.");
    }

    private void StyleFootprintPresentation(TerrainAreaFootprint50 footprint, int index)
    {
        MeshRenderer renderer = footprint.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (renderer.sharedMaterial == null)
                renderer.sharedMaterial = CreateMaterial(new Color(0.25f, 0.27f, 0.29f, 0.12f), null);

            renderer.sharedMaterial.color = footprint.IsObjective
                ? new Color(0.34f, 0.28f, 0.12f, 0.14f)
                : new Color(0.18f, 0.21f, 0.23f, 0.10f);

            Texture2D tex = PickTexture(index);
            if (tex != null)
            {
                renderer.sharedMaterial.mainTexture = tex;
                renderer.sharedMaterial.mainTextureScale = new Vector2(1.1f, 1.1f);
            }
        }

        LineRenderer[] outlines = footprint.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            LineRenderer line = outlines[i];
            if (line == null)
                continue;

            line.widthMultiplier = 0.035f;
            Color outline = footprint.IsObjective
                ? new Color(0.84f, 0.66f, 0.18f, 0.55f)
                : new Color(0.45f, 0.52f, 0.57f, 0.32f);
            line.startColor = outline;
            line.endColor = outline;
        }
    }

    private void SuppressLegacyScenery(Transform areaRoot)
    {
        if (areaRoot == null)
            return;

        for (int i = 0; i < areaRoot.childCount; i++)
        {
            Transform child = areaRoot.GetChild(i);
            if (child == null)
                continue;

            if (child.name == "V50 Terrain Area Outline" ||
                child.name == VisualRootName)
            {
                continue;
            }

            if (child.name.StartsWith("V50 ", StringComparison.Ordinal) ||
                child.GetComponent<TerrainFeature>() != null)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void RebuildVisualRoot(TerrainAreaFootprint50 footprint, int index)
    {
        Transform areaRoot = footprint.transform;
        Transform visualRoot = areaRoot.Find(VisualRootName);
        if (visualRoot == null)
        {
            GameObject go = new GameObject(VisualRootName);
            go.transform.SetParent(areaRoot, false);
            visualRoot = go.transform;
        }

        ClearChildren(visualRoot);
        BuildAreaFloorPlates(visualRoot, footprint, index);

        TerrainVisualKind kind = ChooseVisualKind(footprint, index);
        switch (kind)
        {
            case TerrainVisualKind.LargeL:
                BuildLargeLRuin(visualRoot, footprint);
                break;
            case TerrainVisualKind.LargeU:
                BuildLargeURuin(visualRoot, footprint);
                break;
            case TerrainVisualKind.CornerRuin:
                BuildCornerRuin(visualRoot, footprint);
                break;
            case TerrainVisualKind.TriangleRuin:
                BuildTriangleRuin(visualRoot, footprint);
                break;
            case TerrainVisualKind.LongBarricades:
                BuildLongBarricades(visualRoot, footprint);
                break;
            case TerrainVisualKind.SmallStructure:
                BuildSmallRuin(visualRoot, footprint);
                break;
            default:
                BuildScatterSet(visualRoot, footprint);
                break;
        }
    }

    private void ClearChildren(Transform root)
    {
        List<GameObject> pending = new List<GameObject>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null)
                pending.Add(child.gameObject);
        }

        for (int i = 0; i < pending.Count; i++)
        {
            if (Application.isPlaying)
                Destroy(pending[i]);
            else
                DestroyImmediate(pending[i]);
        }
    }

    private void AttachAreaDescriptor(TerrainAreaFootprint50 footprint, int index)
    {
        WarboardV54TerrainDescriptor descriptor = footprint.GetComponent<WarboardV54TerrainDescriptor>();
        if (descriptor == null)
            descriptor = footprint.gameObject.AddComponent<WarboardV54TerrainDescriptor>();

        descriptor.Title = BuildAreaTitle(footprint, index);
        descriptor.RulesText = BuildAreaRules(footprint);
        descriptor.SolidEndPosition = false;
    }

    private string BuildAreaTitle(TerrainAreaFootprint50 footprint, int index)
    {
        TerrainVisualKind kind = ChooseVisualKind(footprint, index);
        switch (kind)
        {
            case TerrainVisualKind.LargeL: return "Large L-shaped ruin";
            case TerrainVisualKind.LargeU: return "Large U-shaped ruin";
            case TerrainVisualKind.CornerRuin: return "Corner ruin";
            case TerrainVisualKind.TriangleRuin: return "Triangular ruin / corner sector";
            case TerrainVisualKind.LongBarricades: return "Industrial barricade lane";
            case TerrainVisualKind.SmallStructure: return "Small ruined structure";
            default: return "Scatter cover set";
        }
    }

    private string BuildAreaRules(TerrainAreaFootprint50 footprint)
    {
        string text =
            "Terrain Area footprint: this subtle tinted base shows the legal occupiable area of the ruin. " +
            "The walls, columns and barricades inside it are the actual scenery.\n\n" +
            "Intended table meaning: <b>obscuring / cover / movement-shaping ruin</b>. Hover the solid pieces to see which ones block legal end positions.";

        if (footprint.IsObjective)
            text += "\n\nThis terrain area is linked to an objective, so the objective is read from the ruin area rather than from a detached marker.";

        return text;
    }

    private enum TerrainVisualKind
    {
        LargeL,
        LargeU,
        CornerRuin,
        TriangleRuin,
        LongBarricades,
        SmallStructure,
        ScatterSet
    }

    private TerrainVisualKind ChooseVisualKind(TerrainAreaFootprint50 footprint, int index)
    {
        float longSide = Mathf.Max(footprint.Width, footprint.Depth);
        float shortSide = Mathf.Min(footprint.Width, footprint.Depth);

        if (footprint.Shape == TerrainAreaShape50.RightTriangle)
            return TerrainVisualKind.TriangleRuin;

        if (longSide >= 10f && shortSide <= 3.2f)
            return TerrainVisualKind.LongBarricades;

        if (longSide >= 7f && shortSide >= 7f)
            return (index % 2 == 0) ? TerrainVisualKind.LargeL : TerrainVisualKind.LargeU;

        if (longSide >= 6f && shortSide >= 3.8f)
            return TerrainVisualKind.CornerRuin;

        if (longSide >= 5.0f && shortSide >= 2.2f)
            return TerrainVisualKind.SmallStructure;

        return TerrainVisualKind.ScatterSet;
    }

    private void BuildAreaFloorPlates(Transform root, TerrainAreaFootprint50 footprint, int index)
    {
        float width = footprint.Width;
        float depth = footprint.Depth;
        bool major = Mathf.Max(width, depth) >= 6f || footprint.Shape == TerrainAreaShape50.RightTriangle;

        CreateFloorPiece(
            root,
            "Main Floor",
            Vector3.zero,
            new Vector3(
                Mathf.Max(1.4f, width * (major ? 0.72f : 0.62f)),
                0.06f,
                Mathf.Max(1.1f, depth * (major ? 0.60f : 0.48f))
            ),
            index,
            footprint.IsObjective ? GetRustMaterial() : GetStoneMaterial()
        );

        if (major)
        {
            CreateFloorPiece(
                root,
                "Secondary Floor",
                new Vector3(width * 0.18f, 0f, -depth * 0.12f),
                new Vector3(Mathf.Max(1.1f, width * 0.28f), 0.055f, Mathf.Max(1.1f, depth * 0.22f)),
                index + 11,
                GetMetalMaterial()
            );
        }
    }

    private void BuildLargeLRuin(Transform root, TerrainAreaFootprint50 footprint)
    {
        float w = footprint.Width;
        float d = footprint.Depth;

        CreateWallWithDoorGap(root, new Vector3(-w * 0.08f, 0f, d * 0.27f), Mathf.Max(3.6f, w * 0.72f), 3.0f, 0.28f, 0f, 1.2f, true);
        CreateWallWithDoorGap(root, new Vector3(-w * 0.31f, 0f, -d * 0.02f), Mathf.Max(3.8f, d * 0.62f), 2.8f, 0.28f, 90f, 1.05f, true);

        CreateColumn(root, new Vector3(-w * 0.36f, 0f, d * 0.29f), 3.2f);
        CreateColumn(root, new Vector3(-w * 0.02f, 0f, d * 0.29f), 2.5f);
        CreateColumn(root, new Vector3(-w * 0.31f, 0f, -d * 0.22f), 2.7f);

        CreateRubbleField(root, new Vector3(w * 0.18f, 0f, -d * 0.18f), 6, new Vector2(1.8f, 1.2f));
        CreateRubbleField(root, new Vector3(-w * 0.15f, 0f, d * 0.02f), 4, new Vector2(1.3f, 0.9f));
    }

    private void BuildLargeURuin(Transform root, TerrainAreaFootprint50 footprint)
    {
        float w = footprint.Width;
        float d = footprint.Depth;

        CreateWallWithDoorGap(root, new Vector3(0f, 0f, d * 0.29f), Mathf.Max(3.8f, w * 0.76f), 3.1f, 0.28f, 0f, 1.15f, true);
        CreateWallSegment(root, new Vector3(-w * 0.34f, 0f, d * 0.04f), new Vector3(0.28f, 2.8f, Mathf.Max(2.2f, d * 0.48f)), 0f, true, "Left Ruin Wall");
        CreateWallSegment(root, new Vector3(w * 0.34f, 0f, d * 0.04f), new Vector3(0.28f, 2.55f, Mathf.Max(1.8f, d * 0.38f)), 0f, true, "Right Ruin Wall");

        CreateColumn(root, new Vector3(-w * 0.34f, 0f, d * 0.29f), 3.2f);
        CreateColumn(root, new Vector3(w * 0.34f, 0f, d * 0.29f), 2.7f);
        CreateColumn(root, new Vector3(w * 0.34f, 0f, -d * 0.16f), 2.0f);

        CreateRubbleField(root, new Vector3(0f, 0f, -d * 0.16f), 8, new Vector2(1.8f, 1.4f));
        CreateBarricade(root, new Vector3(-w * 0.06f, 0f, -d * 0.28f), 1.8f, 0.7f, 0f, false);
    }

    private void BuildCornerRuin(Transform root, TerrainAreaFootprint50 footprint)
    {
        float w = footprint.Width;
        float d = footprint.Depth;

        CreateWallSegment(root, new Vector3(-w * 0.18f, 0f, d * 0.18f), new Vector3(Mathf.Max(2.2f, w * 0.55f), 2.45f, 0.25f), 0f, true, "Rear Wall");
        CreateWallSegment(root, new Vector3(-w * 0.28f, 0f, -d * 0.05f), new Vector3(0.25f, 2.2f, Mathf.Max(1.8f, d * 0.48f)), 0f, true, "Side Wall");
        CreateColumn(root, new Vector3(-w * 0.28f, 0f, d * 0.18f), 2.8f);
        CreateRubbleField(root, new Vector3(w * 0.08f, 0f, -d * 0.04f), 5, new Vector2(1.2f, 0.85f));
        CreateBarricade(root, new Vector3(w * 0.18f, 0f, -d * 0.20f), 1.6f, 0.72f, 14f, false);
    }

    private void BuildTriangleRuin(Transform root, TerrainAreaFootprint50 footprint)
    {
        float w = footprint.Width;
        float d = footprint.Depth;

        CreateWallSegment(root, new Vector3(-w * 0.26f, 0f, d * 0.10f), new Vector3(0.26f, 2.7f, Mathf.Max(2.4f, d * 0.56f)), 0f, true, "Corner Wall A");
        CreateWallSegment(root, new Vector3(-w * 0.02f, 0f, d * 0.28f), new Vector3(Mathf.Max(2.8f, w * 0.60f), 2.5f, 0.26f), 0f, true, "Corner Wall B");
        CreateColumn(root, new Vector3(-w * 0.26f, 0f, d * 0.28f), 3.0f);
        CreateBarricade(root, new Vector3(w * 0.08f, 0f, -d * 0.04f), 1.9f, 0.8f, -22f, false);
        CreateRubbleField(root, new Vector3(-w * 0.06f, 0f, -d * 0.18f), 6, new Vector2(1.8f, 1.0f));
    }

    private void BuildLongBarricades(Transform root, TerrainAreaFootprint50 footprint)
    {
        float span = Mathf.Max(footprint.Width, footprint.Depth);
        bool horizontal = footprint.Width >= footprint.Depth;
        float yaw = horizontal ? 0f : 90f;

        CreateBarricade(root, new Vector3(0f, 0f, horizontal ? 0.28f : 0f), Mathf.Max(3.2f, span * 0.58f), 0.95f, yaw, true);
        CreateBarricade(root, new Vector3(0f, 0f, horizontal ? -0.32f : 0f), Mathf.Max(2.8f, span * 0.52f), 0.85f, yaw, true);
        CreateColumn(root, new Vector3(horizontal ? -span * 0.26f : 0f, 0f, horizontal ? 0f : -span * 0.26f), 1.3f);
        CreateColumn(root, new Vector3(horizontal ? span * 0.26f : 0f, 0f, horizontal ? 0f : span * 0.26f), 1.15f);
    }

    private void BuildSmallRuin(Transform root, TerrainAreaFootprint50 footprint)
    {
        float w = footprint.Width;
        float d = footprint.Depth;

        CreateWallWithDoorGap(root, new Vector3(0f, 0f, d * 0.08f), Mathf.Max(1.8f, w * 0.52f), 2.1f, 0.22f, 0f, 0.8f, true);
        CreateWallSegment(root, new Vector3(-w * 0.18f, 0f, -d * 0.12f), new Vector3(0.22f, 1.95f, Mathf.Max(1.2f, d * 0.36f)), 0f, true, "Support Wall");
        CreateBarricade(root, new Vector3(w * 0.18f, 0f, -d * 0.12f), 1.2f, 0.66f, 10f, false);
        CreateRubbleField(root, new Vector3(0f, 0f, -d * 0.18f), 4, new Vector2(0.9f, 0.7f));
    }

    private void BuildScatterSet(Transform root, TerrainAreaFootprint50 footprint)
    {
        float w = footprint.Width;
        float d = footprint.Depth;
        CreateBarricade(root, new Vector3(-w * 0.12f, 0f, 0f), Mathf.Max(0.9f, w * 0.32f), 0.66f, 0f, false);
        CreateBarricade(root, new Vector3(w * 0.14f, 0f, -d * 0.08f), Mathf.Max(0.95f, d * 0.35f), 0.72f, 28f, false);
        CreateRubbleField(root, new Vector3(0f, 0f, d * 0.10f), 5, new Vector2(1.0f, 0.7f));
    }

    private void CreateFloorPiece(Transform root, string name, Vector3 localPosition, Vector3 size, int seed, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = ManagedTag + " " + name;
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(localPosition.x, size.y * 0.5f + 0.018f, localPosition.z);
        go.transform.localRotation = Quaternion.Euler(0f, (seed * 37) % 12 - 6f, 0f);
        go.transform.localScale = size;

        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            if (Application.isPlaying)
                Destroy(col);
            else
                DestroyImmediate(col);
        }

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = material;
    }

    private void CreateWallWithDoorGap(Transform root, Vector3 localCentre, float runLength, float height, float thickness, float yaw, float gap, bool blocking)
    {
        float sideLength = Mathf.Max(0.45f, (runLength - gap) * 0.5f);
        float offset = gap * 0.5f + sideLength * 0.5f;
        Vector3 axis = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        CreateWallSegment(root, localCentre - axis * offset, new Vector3(sideLength, height, thickness), yaw, blocking, "Ruin Wall");
        CreateWallSegment(root, localCentre + axis * offset, new Vector3(sideLength, Mathf.Max(1.4f, height - 0.4f), thickness), yaw, blocking, "Ruin Wall");
    }

    private void CreateWallSegment(Transform root, Vector3 localPosition, Vector3 scale, float yaw, bool blocking, string label)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = ManagedTag + " " + label;
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(localPosition.x, scale.y * 0.5f, localPosition.z);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = scale;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = GetStoneMaterial();

        TerrainFeature feature = go.GetComponent<TerrainFeature>();
        if (feature == null)
            feature = go.AddComponent<TerrainFeature>();
        feature.Initialize(blocking ? TerrainTrait.Blocking : TerrainTrait.Cover, string.Empty, false);

        WarboardV54TerrainDescriptor descriptor = go.AddComponent<WarboardV54TerrainDescriptor>();
        descriptor.Title = label;
        descriptor.RulesText = blocking
            ? "Solid scenery piece. This wall is meant to be the actual ruin body: it blocks legal end positions and shapes line of sight through the terrain area."
            : "Low cover scenery piece. Use as cover / low obstacle inside the terrain area.";
        descriptor.SolidEndPosition = blocking;
    }

    private void CreateColumn(Transform root, Vector3 localPosition, float height)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = ManagedTag + " Ruin Column";
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(localPosition.x, height * 0.5f, localPosition.z);
        go.transform.localScale = new Vector3(0.34f, height, 0.34f);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = GetStoneMaterial();

        TerrainFeature feature = go.GetComponent<TerrainFeature>();
        if (feature == null)
            feature = go.AddComponent<TerrainFeature>();
        feature.Initialize(TerrainTrait.Blocking, string.Empty, false);

        WarboardV54TerrainDescriptor descriptor = go.AddComponent<WarboardV54TerrainDescriptor>();
        descriptor.Title = "Ruin column";
        descriptor.RulesText = "Solid scenery piece. Counts as part of the ruin body and blocks legal end positions where the base would overlap it.";
        descriptor.SolidEndPosition = true;
    }

    private void CreateBarricade(Transform root, Vector3 localPosition, float length, float height, float yaw, bool industrial)
    {
        GameObject main = GameObject.CreatePrimitive(PrimitiveType.Cube);
        main.name = ManagedTag + (industrial ? " Industrial Barricade" : " Barricade");
        main.transform.SetParent(root, false);
        main.transform.localPosition = new Vector3(localPosition.x, height * 0.5f, localPosition.z);
        main.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        main.transform.localScale = new Vector3(length, height, 0.28f);

        MeshRenderer mr = main.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = industrial ? GetMetalMaterial() : GetBarricadeMaterial();

        TerrainFeature feature = main.GetComponent<TerrainFeature>();
        if (feature == null)
            feature = main.AddComponent<TerrainFeature>();
        feature.Initialize(TerrainTrait.Cover, string.Empty, false);

        WarboardV54TerrainDescriptor descriptor = main.AddComponent<WarboardV54TerrainDescriptor>();
        descriptor.Title = industrial ? "Industrial barricade" : "Barricade";
        descriptor.RulesText = "Low cover scenery. It should read as a clear piece of cover rather than as a random cube pile.";
        descriptor.SolidEndPosition = false;

        if (industrial)
        {
            Vector3 left = Quaternion.Euler(0f, yaw, 0f) * new Vector3(-length * 0.34f, 0f, 0f);
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * new Vector3(length * 0.34f, 0f, 0f);
            CreateBarricadePost(root, localPosition + left, 1.2f);
            CreateBarricadePost(root, localPosition + right, 1.1f);
        }
    }

    private void CreateBarricadePost(Transform root, Vector3 localPosition, float height)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = ManagedTag + " Barricade Post";
        post.transform.SetParent(root, false);
        post.transform.localPosition = new Vector3(localPosition.x, height * 0.5f, localPosition.z);
        post.transform.localScale = new Vector3(0.18f, height, 0.18f);

        MeshRenderer mr = post.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = GetMetalMaterial();

        TerrainFeature feature = post.GetComponent<TerrainFeature>();
        if (feature == null)
            feature = post.AddComponent<TerrainFeature>();
        feature.Initialize(TerrainTrait.Cover, string.Empty, false);
    }

    private void CreateRubbleField(Transform root, Vector3 centre, int count, Vector2 spread)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = ManagedTag + " Rubble";
            rock.transform.SetParent(root, false);

            float px = centre.x + Mathf.Lerp(-spread.x, spread.x, PseudoRandom01(i * 13 + 7));
            float pz = centre.z + Mathf.Lerp(-spread.y, spread.y, PseudoRandom01(i * 17 + 3));
            float sx = Mathf.Lerp(0.18f, 0.55f, PseudoRandom01(i * 19 + 2));
            float sy = Mathf.Lerp(0.10f, 0.22f, PseudoRandom01(i * 23 + 5));
            float sz = Mathf.Lerp(0.18f, 0.52f, PseudoRandom01(i * 29 + 11));
            float ry = Mathf.Lerp(-30f, 30f, PseudoRandom01(i * 31 + 1));
            float rz = Mathf.Lerp(-6f, 6f, PseudoRandom01(i * 37 + 4));

            rock.transform.localPosition = new Vector3(px, sy * 0.5f, pz);
            rock.transform.localRotation = Quaternion.Euler(0f, ry, rz);
            rock.transform.localScale = new Vector3(sx, sy, sz);

            Collider col = rock.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            MeshRenderer mr = rock.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sharedMaterial = (i % 2 == 0) ? GetStoneMaterial() : GetBarricadeMaterial();
        }
    }

    private float PseudoRandom01(int seed)
    {
        seed = (seed << 13) ^ seed;
        int value = seed * (seed * seed * 15731 + 789221) + 1376312589;
        value &= 0x7fffffff;
        return value / 2147483647f;
    }

    private Texture2D PickTexture(int index)
    {
        if (terrainTextures == null || terrainTextures.Length == 0)
            return null;
        return terrainTextures[Mathf.Abs(index) % terrainTextures.Length];
    }

    private Shader PickShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit") ??
               Shader.Find("Standard") ??
               Shader.Find("Sprites/Default");
    }

    private Material CreateMaterial(Color tint, Texture2D texture)
    {
        Material mat = new Material(PickShader());
        mat.color = tint;
        if (texture != null)
        {
            mat.mainTexture = texture;
            mat.mainTextureScale = new Vector2(1.1f, 1.1f);
        }
        return mat;
    }

    private Material GetStoneMaterial()
    {
        if (stoneMaterial == null)
            stoneMaterial = CreateMaterial(new Color(0.56f, 0.58f, 0.60f, 1f), PickTexture(0));
        return stoneMaterial;
    }

    private Material GetRustMaterial()
    {
        if (rustMaterial == null)
            rustMaterial = CreateMaterial(new Color(0.58f, 0.49f, 0.41f, 1f), PickTexture(1));
        return rustMaterial;
    }

    private Material GetMetalMaterial()
    {
        if (metalMaterial == null)
            metalMaterial = CreateMaterial(new Color(0.46f, 0.49f, 0.52f, 1f), PickTexture(2));
        return metalMaterial;
    }

    private Material GetBarricadeMaterial()
    {
        if (barricadeMaterial == null)
            barricadeMaterial = CreateMaterial(new Color(0.38f, 0.31f, 0.22f, 1f), PickTexture(3));
        return barricadeMaterial;
    }

    private Material GetWoodMaterial()
    {
        if (woodMaterial == null)
            woodMaterial = CreateMaterial(new Color(0.36f, 0.24f, 0.14f, 1f), null);
        return woodMaterial;
    }

    private void UpdateHoveredTerrain()
    {
        hoveredDescriptor = null;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0)
            return;

        float bestDistance = float.MaxValue;
        WarboardV54TerrainDescriptor best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)
                continue;

            WarboardV54TerrainDescriptor descriptor = col.GetComponent<WarboardV54TerrainDescriptor>();
            if (descriptor == null)
                descriptor = col.GetComponentInParent<WarboardV54TerrainDescriptor>();
            if (descriptor == null)
                continue;

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                best = descriptor;
            }
        }

        hoveredDescriptor = best;
    }

    private void ApplyMissionCardFix(TerrainAreaFootprint50[] footprints)
    {
        if (footprints == null || footprints.Length == 0)
            return;

        Bounds boardBounds = ComputeFootprintBounds(footprints);
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.transform == null)
                continue;

            string n = r.gameObject.name.ToLowerInvariant();
            if (!n.Contains("mission") && !n.Contains("card"))
                continue;

            if (r.gameObject.GetComponent<WarboardV54MissionCardAdjusted>() != null)
                continue;

            Vector3 size = r.bounds.size;
            float maxPlanar = Mathf.Max(size.x, size.z);
            bool looksHuge = maxPlanar > 5f || r.transform.position.y > 4.5f;
            if (!looksHuge)
                continue;

            WarboardV54MissionCardAdjusted adjusted = r.gameObject.AddComponent<WarboardV54MissionCardAdjusted>();
            adjusted.Apply(boardBounds, GetWoodMaterial());
        }
    }

    private Bounds ComputeFootprintBounds(TerrainAreaFootprint50[] footprints)
    {
        bool any = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);

        for (int i = 0; i < footprints.Length; i++)
        {
            TerrainAreaFootprint50 fp = footprints[i];
            if (fp == null)
                continue;

            if (!any)
            {
                bounds = fp.WorldBounds;
                any = true;
            }
            else
            {
                bounds.Encapsulate(fp.WorldBounds);
            }
        }

        return bounds;
    }
}

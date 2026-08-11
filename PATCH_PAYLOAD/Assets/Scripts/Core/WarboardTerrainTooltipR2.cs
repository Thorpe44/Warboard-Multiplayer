using UnityEngine;

// WARBOARD_TERRAIN_TOOLTIP_R2_1
// Fixes: ArgumentException: You can only call GUI functions from inside OnGUI.

[DefaultExecutionOrder(25000)]
public sealed class WarboardTerrainTooltipR2 : MonoBehaviour
{
    private string title = "";
    private string body = "";
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle panelStyle;
    private bool stylesReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<WarboardTerrainTooltipR2>() != null) return;
        GameObject root = new GameObject("Warboard Terrain Tooltip R2.1");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<WarboardTerrainTooltipR2>();
    }

    private void Update()
    {
        title = "";
        body = "";
        Camera camera = Camera.main;
        if (camera == null) return;

        RaycastHit hit;
        if (!Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, 250f, ~0, QueryTriggerInteraction.Collide)) return;

        TerrainAreaFootprint50 area = hit.collider.GetComponentInParent<TerrainAreaFootprint50>();
        TerrainFeature feature = hit.collider.GetComponentInParent<TerrainFeature>();
        if (area == null && feature == null) return;

        if (feature != null && (area == null || feature.transform != area.transform))
        {
            if (feature.Trait == TerrainTrait.Blocking)
            {
                title = "SOLID RUIN FEATURE";
                body = "Visible wall/column geometry. It shapes line of sight and a model base cannot finish overlapping it.";
            }
            else if (feature.Trait == TerrainTrait.Cover)
            {
                title = "COVER FEATURE";
                body = "Low cover inside the Terrain Area. Clear surrounding floor remains occupiable; the solid feature itself does not.";
            }
            else
            {
                title = "TERRAIN AREA";
                body = "Rules footprint for battlefield terrain. Clear parts are legal standing space.";
            }
            if (area != null && area.IsObjective) body += "\n\nThis feature sits inside an objective Terrain Area.";
            return;
        }

        if (area != null)
        {
            float shortSide = Mathf.Min(area.Width, area.Depth);
            if (shortSide <= 2.7f)
            {
                title = area.IsObjective ? "OBJECTIVE - INDUSTRIAL COVER AREA" : "INDUSTRIAL COVER AREA";
                body = "The textured base is the Terrain Area footprint. Models can stand on clear parts of it; visible barricades and machinery are the physical scenery.";
            }
            else
            {
                title = area.IsObjective ? "OBJECTIVE - RUINS TERRAIN AREA" : "RUINS TERRAIN AREA";
                body = "<b>Obscuring.</b> The Terrain Area is the footprint beneath the ruined building. Clear floor is occupiable; solid visible wall geometry is not a legal final base position.\n\n<b>Benefit of Cover / Hidden:</b> use Warboard's implemented 11e terrain rules.";
            }
            if (area.IsObjective) body += "\n\n<b>Objective terrain:</b> this Terrain Area is also the mission objective.";
        }
    }

    private void EnsureStyles()
    {
        if (stylesReady) return;
        // GUI.skin may only be touched while Unity is inside OnGUI.
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 17;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.88f, 0.93f, 0.95f, 1f);

        bodyStyle = new GUIStyle(GUI.skin.label);
        bodyStyle.fontSize = 14;
        bodyStyle.wordWrap = true;
        bodyStyle.richText = true;
        bodyStyle.normal.textColor = new Color(0.88f, 0.89f, 0.90f, 1f);

        panelStyle = new GUIStyle(GUI.skin.box);
        stylesReady = true;
    }

    private void OnGUI()
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        EnsureStyles();

        float width = Mathf.Min(430f, Screen.width * 0.32f);
        float height = bodyStyle.CalcHeight(new GUIContent(body), width - 28f) + 64f;
        Rect panel = new Rect(Screen.width - width - 22f, 96f, width, height);

        Color old = GUI.color;
        GUI.color = new Color(0.03f, 0.035f, 0.045f, 0.95f);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.color = old;

        GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 26f), title, titleStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 38f, panel.width - 28f, panel.height - 48f), body, bodyStyle);
    }
}

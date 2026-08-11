using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

// WARBOARD_UI_READABILITY_R2_6
// - bigger readable world-board text
// - custom symmetric player info bar so Ork / Player 2 data is always visible
// - removes raw underscored/internal rule tokens from datasheet-like UI

[DefaultExecutionOrder(32010)]
public sealed class WarboardUiReadabilityR26 : MonoBehaviour
{
    private GameController game;
    private Canvas overlayCanvas;
    private Text leftText;
    private Text rightText;
    private Image background;

    private float nextHudRefresh;
    private float nextScanRefresh;

    private static Type tmpTextType;
    private static PropertyInfo tmpTextProperty;
    private static PropertyInfo tmpFontSizeProperty;
    private static PropertyInfo tmpEnabledProperty;
    private static MethodInfo tmpSetVerticesDirty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<WarboardUiReadabilityR26>() != null)
            return;

        GameObject root = new GameObject("Warboard UI Readability R2.6");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<WarboardUiReadabilityR26>();
    }

    private void Awake()
    {
        ResolveTmpReflection();
        BuildOverlay();
    }

    private void LateUpdate()
    {
        if (game == null)
            game = GameController.Current;

        if (Time.unscaledTime >= nextHudRefresh)
        {
            nextHudRefresh = Time.unscaledTime + 0.20f;
            UpdateOverlay();
        }

        if (Time.unscaledTime >= nextScanRefresh)
        {
            nextScanRefresh = Time.unscaledTime + 0.40f;
            ImproveWorldTextMeshes();
            SanitizeUiTexts();
        }
    }

    private void BuildOverlay()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("Warboard R2.6 Player Summary Canvas");
        canvasObject.transform.SetParent(transform, false);
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 190;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Player Summary Bar");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.10f, 1f);
        panelRect.anchorMax = new Vector2(0.90f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(0f, 28f);
        panelRect.anchoredPosition = new Vector2(0f, -56f);

        background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.62f);

        leftText = CreateOverlayText(panelObject.transform, font, "Left Player Summary", new Vector2(0f, 0f), new Vector2(0.5f, 1f), TextAnchor.MiddleLeft);
        rightText = CreateOverlayText(panelObject.transform, font, "Right Player Summary", new Vector2(0.5f, 0f), new Vector2(1f, 1f), TextAnchor.MiddleRight);
    }

    private Text CreateOverlayText(Transform parent, Font font, string name, Vector2 anchorMin, Vector2 anchorMax, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(10f, 1f);
        rect.offsetMax = new Vector2(-10f, -1f);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 18;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.color = new Color(0.92f, 0.95f, 1f, 1f);
        text.text = "";
        return text;
    }

    private void UpdateOverlay()
    {
        bool visible = false;
        string left = "";
        string right = "";

        if (game != null && game.WorldUiFactionCount >= 2)
        {
            left = BuildPlayerSummary(0, "Player 1");
            right = BuildPlayerSummary(1, "Player 2");
            visible = !string.IsNullOrWhiteSpace(left) || !string.IsNullOrWhiteSpace(right);
        }

        if (overlayCanvas != null)
            overlayCanvas.enabled = visible;

        if (!visible)
            return;

        if (leftText != null)
            leftText.text = left;

        if (rightText != null)
            rightText.text = right;
    }

    private string BuildPlayerSummary(int index, string fallbackLabel)
    {
        string primary = SafeSanitize(game.WorldUiPrimaryCardText55(index));
        string secondary = SafeSanitize(game.WorldUiSecondaryCardText55(index));

        string faction = fallbackLabel;
        string detachment = "";
        string disposition = "";
        string primaryName = "";
        string secondaryMode = "";

        foreach (string raw in (primary + "\n" + secondary).Split('\n'))
        {
            string line = (raw ?? "").Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.Contains(":"))
            {
                if (faction == fallbackLabel)
                    faction = line;
                continue;
            }

            string[] split = line.Split(new[] { ':' }, 2);
            string key = split[0].Trim();
            string value = split.Length > 1 ? split[1].Trim() : "";

            if (key.Equals("Force Disposition", StringComparison.OrdinalIgnoreCase))
            {
                disposition = value;
            }
            else if (key.Equals("Primary", StringComparison.OrdinalIgnoreCase))
            {
                primaryName = value;
            }
            else if (key.StartsWith("Secondar", StringComparison.OrdinalIgnoreCase))
            {
                secondaryMode = value;
            }
            else if (key.Equals("AELDARI", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("ORKS", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("NECRONS", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("TYRANIDS", StringComparison.OrdinalIgnoreCase) ||
                     key.Equals("CUSTODES", StringComparison.OrdinalIgnoreCase))
            {
                detachment = value;
            }
            else if (faction == fallbackLabel)
            {
                faction = key;
            }
        }

        List<string> bits = new List<string>();
        bits.Add(fallbackLabel);
        bits.Add(faction);
        if (!string.IsNullOrWhiteSpace(detachment)) bits.Add(detachment);
        if (!string.IsNullOrWhiteSpace(disposition)) bits.Add(disposition);
        if (!string.IsNullOrWhiteSpace(primaryName)) bits.Add(primaryName);
        if (!string.IsNullOrWhiteSpace(secondaryMode)) bits.Add(secondaryMode);
        return string.Join("  •  ", bits.ToArray());
    }

    private void ImproveWorldTextMeshes()
    {
        TextMesh[] meshes = Resources.FindObjectsOfTypeAll<TextMesh>();
        foreach (TextMesh mesh in meshes)
        {
            if (mesh == null || mesh.gameObject == null || !mesh.gameObject.scene.IsValid())
                continue;

            string name = mesh.gameObject.name ?? "";
            string text = mesh.text ?? "";
            string parentName = mesh.transform.parent != null ? mesh.transform.parent.name : "";

            if (name.Contains("Primary Card Text") || name.Contains("Secondary Card Text"))
            {
                mesh.fontSize = Mathf.Max(mesh.fontSize, 38);
                if (mesh.characterSize < 0.028f)
                    mesh.characterSize = 0.028f;
                mesh.lineSpacing = 0.88f;
                continue;
            }

            if (name.Contains("Primary Card Type") || name.Contains("Secondary Card Type"))
            {
                mesh.fontSize = Mathf.Max(mesh.fontSize, 36);
                if (mesh.characterSize < 0.032f)
                    mesh.characterSize = 0.032f;
                continue;
            }

            if (text.Contains("MATCH SCOREBOARD") || parentName.Contains("World Scoreboard") || name.Contains("Scoreboard"))
            {
                mesh.fontSize = Mathf.Max(mesh.fontSize, 54);
                if (mesh.characterSize < 0.068f)
                    mesh.characterSize = 0.068f;
                mesh.lineSpacing = 0.90f;
                mesh.anchor = TextAnchor.UpperCenter;
                mesh.alignment = TextAlignment.Center;
            }
        }
    }

    private void SanitizeUiTexts()
    {
        foreach (Text uiText in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (uiText == null || uiText.gameObject == null || !uiText.gameObject.scene.IsValid())
                continue;

            string original = uiText.text ?? "";
            string cleaned = SafeSanitize(original);
            if (!string.Equals(original, cleaned, StringComparison.Ordinal))
                uiText.text = cleaned;

            string lowered = cleaned ?? "";
            if ((lowered.Contains("Player 1 •") || lowered.Contains("Player 2 •")) && uiText.transform.parent != null && uiText.transform.root != transform.root)
            {
                uiText.enabled = false;
                continue;
            }

            if ((cleaned.Contains("won the manually resolved first-turn roll-off") || cleaned.Contains("BOTH players gain +1 CP")) && uiText.fontSize < 18)
                uiText.fontSize = 18;
        }

        if (tmpTextType == null || tmpTextProperty == null)
            return;

        UnityEngine.Object[] tmpTexts = Resources.FindObjectsOfTypeAll(tmpTextType);
        foreach (UnityEngine.Object obj in tmpTexts)
        {
            if (obj == null)
                continue;

            Component component = obj as Component;
            if (component == null || component.gameObject == null || !component.gameObject.scene.IsValid())
                continue;

            string original = (string)tmpTextProperty.GetValue(obj, null) ?? "";
            string cleaned = SafeSanitize(original);
            if (!string.Equals(original, cleaned, StringComparison.Ordinal))
                tmpTextProperty.SetValue(obj, cleaned, null);

            if ((cleaned.Contains("Player 1 •") || cleaned.Contains("Player 2 •")) && component.transform.root != transform.root)
            {
                if (tmpEnabledProperty != null)
                    tmpEnabledProperty.SetValue(obj, false, null);
                continue;
            }

            if (tmpFontSizeProperty != null)
            {
                try
                {
                    float fontSize = Convert.ToSingle(tmpFontSizeProperty.GetValue(obj, null));
                    if ((cleaned.Contains("won the manually resolved first-turn roll-off") || cleaned.Contains("BOTH players gain +1 CP")) && fontSize < 18f)
                        tmpFontSizeProperty.SetValue(obj, 18f, null);
                }
                catch { }
            }

            if (tmpSetVerticesDirty != null)
            {
                try { tmpSetVerticesDirty.Invoke(obj, null); }
                catch { }
            }
        }
    }

    private static void ResolveTmpReflection()
    {
        if (tmpTextType != null)
            return;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType("TMPro.TMP_Text");
            if (type == null)
                continue;

            tmpTextType = type;
            tmpTextProperty = type.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            tmpFontSizeProperty = type.GetProperty("fontSize", BindingFlags.Public | BindingFlags.Instance);
            tmpEnabledProperty = type.GetProperty("enabled", BindingFlags.Public | BindingFlags.Instance);
            tmpSetVerticesDirty = type.GetMethod("SetVerticesDirty", BindingFlags.Public | BindingFlags.Instance);
            break;
        }
    }

    private static string SafeSanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? "";

        string[] lines = input.Replace("\r", "").Split('\n');
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i] ?? "";
            string cleaned = CleanRuleLine(line);
            builder.Append(cleaned);
            if (i < lines.Length - 1)
                builder.Append('\n');
        }

        string output = builder.ToString();
        output = Regex.Replace(output, @"[ \t]{2,}", " ");
        output = output.Replace(" ,", ",");
        output = output.Replace(" |", "|");
        return output;
    }

    private static string CleanRuleLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return line;

        string trimmed = line.Trim();
        if (trimmed.Contains("|") )
        {
            string[] parts = trimmed.Split(new[] { '|' }, 2);
            if (LooksLikeRawRuleList(parts[0]) && !LooksLikeRawRuleList(parts[1]))
                return parts[1].Trim();
        }

        if (LooksLikeRawRuleList(trimmed))
        {
            string[] tokens = trimmed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> human = new List<string>();
            foreach (string token in tokens)
            {
                string pretty = HumanizeRuleToken(token.Trim());
                if (!string.IsNullOrWhiteSpace(pretty))
                    human.Add(pretty);
            }
            if (human.Count > 0)
                return string.Join(", ", human.ToArray());
        }

        return Regex.Replace(trimmed, @"\b([a-z]+(?:_[a-z0-9]+)+)\b", delegate(Match match)
        {
            return HumanizeRuleToken(match.Value);
        });
    }

    private static bool LooksLikeRawRuleList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string v = value.Trim();
        if (!v.Contains("_"))
            return false;

        return Regex.IsMatch(v, @"^[a-z0-9_+,\-\s]+$");
    }

    private static string HumanizeRuleToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return token;

        string v = token.Trim().ToLowerInvariant();
        switch (v)
        {
            case "devastating_wounds": return "Devastating Wounds";
            case "lethal_hits": return "Lethal Hits";
            case "sustained_hits_1": return "Sustained Hits 1";
            case "sustained_hits_2": return "Sustained Hits 2";
            case "sustained_hits_d3": return "Sustained Hits D3";
            case "deep_strike": return "Deep Strike";
            case "fights_first": return "Fights First";
            case "feel_no_pain_5": return "Feel No Pain 5+";
            case "feel_no_pain_6": return "Feel No Pain 6+";
            case "deadly_demise_d3": return "Deadly Demise D3";
            case "deadly_demise_d6": return "Deadly Demise D6";
            case "torrent": return "Torrent";
            case "psychic": return "Psychic";
        }

        Match anti = Regex.Match(v, @"^anti_([a-z]+)_([0-9]+)$");
        if (anti.Success)
        {
            return "Anti-" + TitleCaseWithHyphen(anti.Groups[1].Value) + " " + anti.Groups[2].Value + "+";
        }

        Match crit = Regex.Match(v, @"^critical_hits_([0-9]+)$");
        if (crit.Success)
        {
            return "Critical Hits " + crit.Groups[1].Value + "+";
        }

        string[] bits = v.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < bits.Length; i++)
        {
            if (bits[i].Length == 0)
                continue;
            bits[i] = char.ToUpperInvariant(bits[i][0]) + bits[i].Substring(1);
        }
        return string.Join(" ", bits);
    }

    private static string TitleCaseWithHyphen(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        string[] bits = value.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < bits.Length; i++)
        {
            bits[i] = char.ToUpperInvariant(bits[i][0]) + bits[i].Substring(1);
        }
        return string.Join("-", bits);
    }
}

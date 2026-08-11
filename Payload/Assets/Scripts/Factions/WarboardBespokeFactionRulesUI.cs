using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// In-battle FACTION RULES UI for bespoke faction controllers.
/// StandardFactionSetupUI continues to own Orks, Tyranids and Space Marines.
/// This router owns Aeldari/Ynnari, Adeptus Custodes and Necrons so the
/// bottom-right rules button exists for the active faction instead of
/// disappearing when Standard11 has no matching controller.
/// </summary>
[DefaultExecutionOrder(-31960)]
public sealed class WarboardBespokeFactionRulesUI :
    MonoBehaviour
{
    private bool showRules;
    private string rulesFaction = "";
    private Vector2 scroll;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object
            .FindAnyObjectByType<
                WarboardBespokeFactionRulesUI>() != null)
        {
            return;
        }

        GameObject go =
            new GameObject(
                "WarboardBespokeFactionRulesUI");

        UnityEngine.Object
            .DontDestroyOnLoad(go);

        go.AddComponent<
            WarboardBespokeFactionRulesUI>();
    }

    private void OnGUI()
    {
        GameController game =
            GameController.Current;

        FactionControllerHost host =
            FactionControllerHost.Instance;

        if (game == null ||
            host == null ||
            game.BattleRound <= 0)
        {
            CloseRules();
            return;
        }

        string activeFaction =
            game.ActiveFactionId ?? "";

        AeldariGameController aeldari =
            host.Controllers.Values
                .OfType<AeldariGameController>()
                .FirstOrDefault(
                    value =>
                        value != null &&
                        string.Equals(
                            value.FactionId,
                            activeFaction,
                            StringComparison.OrdinalIgnoreCase));

        CustodesGameController custodes =
            host.Controllers.Values
                .OfType<CustodesGameController>()
                .FirstOrDefault(
                    value =>
                        value != null &&
                        string.Equals(
                            value.FactionId,
                            activeFaction,
                            StringComparison.OrdinalIgnoreCase));

        NecronGameController necrons =
            host.Controllers.Values
                .OfType<NecronGameController>()
                .FirstOrDefault(
                    value =>
                        value != null &&
                        string.Equals(
                            value.FactionId,
                            activeFaction,
                            StringComparison.OrdinalIgnoreCase));

        bool bespokeActive =
            aeldari != null ||
            custodes != null ||
            necrons != null;

        if (!bespokeActive)
        {
            // StandardFactionSetupUI owns the same bottom-right button
            // for Orks, Tyranids and Space Marines.
            CloseRules();
            return;
        }

        if (showRules &&
            !string.Equals(
                rulesFaction,
                activeFaction,
                StringComparison.OrdinalIgnoreCase))
        {
            CloseRules();
        }

        Rect button =
            new Rect(
                Screen.width - 154f,
                Screen.height - 42f,
                142f,
                30f);

        if (GUI.Button(
                button,
                showRules
                ? "CLOSE FACTION RULES"
                : "FACTION RULES"))
        {
            if (showRules)
            {
                CloseRules();
            }
            else
            {
                showRules = true;
                rulesFaction = activeFaction;
                scroll = Vector2.zero;
            }
        }

        if (!showRules)
            return;

        if (aeldari != null)
        {
            DrawAeldariRules(aeldari);
            return;
        }

        if (custodes != null)
        {
            DrawCustodesRules(custodes);
            return;
        }

        if (necrons != null)
        {
            DrawNecronRules(necrons);
        }
    }

    private void CloseRules()
    {
        showRules = false;
        rulesFaction = "";
        scroll = Vector2.zero;
    }

    private Rect BeginRulesPanel(
        string title,
        out GUIStyle heading,
        out GUIStyle body,
        out Rect scrollOuter)
    {
        float width =
            Mathf.Min(
                760f,
                Screen.width - 36f);

        float height =
            Mathf.Min(
                760f,
                Screen.height - 100f);

        Rect panel =
            new Rect(
                Screen.width -
                    width -
                    18f,
                86f,
                width,
                height);

        GUI.Box(panel, "");

        GUIStyle titleStyle =
            new GUIStyle(
                GUI.skin.label);

        titleStyle.fontSize = 20;
        titleStyle.fontStyle =
            FontStyle.Bold;

        heading =
            new GUIStyle(
                GUI.skin.label);

        heading.fontSize = 15;
        heading.fontStyle =
            FontStyle.Bold;
        heading.wordWrap = true;

        body =
            new GUIStyle(
                GUI.skin.label);

        body.fontSize = 12;
        body.wordWrap = true;

        GUI.Label(
            new Rect(
                panel.x + 16f,
                panel.y + 10f,
                panel.width - 80f,
                28f),
            title + " - FACTION RULES",
            titleStyle);

        if (GUI.Button(
                new Rect(
                    panel.x +
                        panel.width -
                        52f,
                    panel.y + 8f,
                    36f,
                    30f),
                "X"))
        {
            CloseRules();
        }

        scrollOuter =
            new Rect(
                panel.x + 14f,
                panel.y + 50f,
                panel.width - 28f,
                panel.height - 66f);

        return panel;
    }

    private float DrawWrapped(
        float y,
        float width,
        string text,
        GUIStyle style,
        float indent = 4f)
    {
        string safe = text ?? "";
        float usable =
            Mathf.Max(
                80f,
                width - indent - 8f);

        float height =
            Mathf.Max(
                20f,
                style.CalcHeight(
                    new GUIContent(safe),
                    usable));

        GUI.Label(
            new Rect(
                indent,
                y,
                usable,
                height),
            safe,
            style);

        return y + height + 7f;
    }

    private float DrawSectionTitle(
        float y,
        float width,
        string text,
        GUIStyle heading)
    {
        GUI.Label(
            new Rect(
                4f,
                y,
                width - 8f,
                24f),
            text,
            heading);

        return y + 28f;
    }

    private void DrawAeldariRules(
        AeldariGameController controller)
    {
        GUIStyle heading;
        GUIStyle body;
        Rect outer;

        BeginRulesPanel(
            "AELDARI",
            out heading,
            out body,
            out outer);

        if (!showRules)
            return;

        Rect inner =
            new Rect(
                0f,
                0f,
                outer.width - 20f,
                12000f);

        scroll =
            GUI.BeginScrollView(
                outer,
                scroll,
                inner);

        float y = 4f;

        y = DrawSectionTitle(
            y,
            inner.width,
            "ARMY RULE - BATTLE FOCUS",
            heading);

        y = DrawWrapped(
            y,
            inner.width,
            "Current Battle Focus: " +
            controller.BattleFocusTokens +
            " token(s).",
            body);

        foreach (AeldariDetachment detachment
            in AeldariDetachmentRuntime
                .GetSelected(
                    controller.FactionId))
        {
            AeldariDetachmentRule11 rule =
                AeldariFactionPack11
                    .DetachmentRule(
                        detachment);

            y += 8f;

            y = DrawSectionTitle(
                y,
                inner.width,
                AeldariDetachmentRuntime
                    .Name(
                        detachment) +
                (rule != null
                    ? " - " + rule.Name
                    : ""),
                heading);

            if (rule != null)
            {
                y = DrawWrapped(
                    y,
                    inner.width,
                    rule.Rule,
                    body,
                    12f);
            }
        }

        var enhancements =
            AeldariFactionPack11
                .EnhancementsFor(
                    controller.FactionId);

        if (enhancements.Count > 0)
        {
            y += 10f;
            y = DrawSectionTitle(
                y,
                inner.width,
                "ENHANCEMENTS",
                heading);

            foreach (AeldariEnhancement11 item
                in enhancements)
            {
                y = DrawSectionTitle(
                    y,
                    inner.width,
                    item.Name +
                    " - " +
                    item.Points +
                    " PTS",
                    heading);

                y = DrawWrapped(
                    y,
                    inner.width,
                    item.Rule,
                    body,
                    12f);
            }
        }

        var stratagems =
            AeldariFactionPack11
                .StratagemsFor(
                    controller.FactionId);

        if (stratagems.Count > 0)
        {
            y += 10f;
            y = DrawSectionTitle(
                y,
                inner.width,
                "STRATAGEMS",
                heading);

            foreach (AeldariStratagem11 item
                in stratagems)
            {
                y = DrawSectionTitle(
                    y,
                    inner.width,
                    item.Name +
                    " - " +
                    item.Cost +
                    "CP",
                    heading);

                y = DrawWrapped(
                    y,
                    inner.width,
                    item.FullRule,
                    body,
                    12f);
            }
        }

        GUI.EndScrollView();
    }

    private void DrawCustodesRules(
        CustodesGameController controller)
    {
        GUIStyle heading;
        GUIStyle body;
        Rect outer;

        BeginRulesPanel(
            "ADEPTUS CUSTODES",
            out heading,
            out body,
            out outer);

        if (!showRules)
            return;

        Rect inner =
            new Rect(
                0f,
                0f,
                outer.width - 20f,
                12000f);

        scroll =
            GUI.BeginScrollView(
                outer,
                scroll,
                inner);

        float y = 4f;

        y = DrawSectionTitle(
            y,
            inner.width,
            "ARMY RULE - MARTIAL KA'TAH",
            heading);

        y = DrawWrapped(
            y,
            inner.width,
            "Rules below are read from the active Adeptus Custodes faction pack and selected detachment implementation.",
            body);

        foreach (CustodesDetachment detachment
            in CustodesDetachmentRuntime
                .GetSelected(
                    controller.FactionId))
        {
            CustodesDetachmentRule11 rule =
                CustodesFactionPack11
                    .DetachmentRule(
                        detachment);

            y += 8f;

            y = DrawSectionTitle(
                y,
                inner.width,
                CustodesDetachmentRuntime
                    .Name(
                        detachment) +
                (rule != null
                    ? " - " + rule.Name
                    : ""),
                heading);

            if (rule != null)
            {
                y = DrawWrapped(
                    y,
                    inner.width,
                    rule.Rule,
                    body,
                    12f);
            }
        }

        var enhancements =
            CustodesFactionPack11
                .EnhancementsFor(
                    controller.FactionId);

        if (enhancements.Count > 0)
        {
            y += 10f;
            y = DrawSectionTitle(
                y,
                inner.width,
                "ENHANCEMENTS",
                heading);

            foreach (CustodesEnhancement11 item
                in enhancements)
            {
                y = DrawSectionTitle(
                    y,
                    inner.width,
                    item.Name +
                    " - " +
                    item.Points +
                    " PTS",
                    heading);

                y = DrawWrapped(
                    y,
                    inner.width,
                    item.Rule,
                    body,
                    12f);
            }
        }

        var stratagems =
            CustodesFactionPack11
                .StratagemsFor(
                    controller.FactionId);

        if (stratagems.Count > 0)
        {
            y += 10f;
            y = DrawSectionTitle(
                y,
                inner.width,
                "STRATAGEMS",
                heading);

            foreach (CustodesStratagem11 item
                in stratagems)
            {
                y = DrawSectionTitle(
                    y,
                    inner.width,
                    item.Name +
                    " - " +
                    item.Cost +
                    "CP",
                    heading);

                y = DrawWrapped(
                    y,
                    inner.width,
                    item.FullRule,
                    body,
                    12f);
            }
        }

        GUI.EndScrollView();
    }

    private void DrawNecronRules(
        NecronGameController controller)
    {
        GUIStyle heading;
        GUIStyle body;
        Rect outer;

        BeginRulesPanel(
            "NECRONS",
            out heading,
            out body,
            out outer);

        if (!showRules)
            return;

        Rect inner =
            new Rect(
                0f,
                0f,
                outer.width - 20f,
                12000f);

        scroll =
            GUI.BeginScrollView(
                outer,
                scroll,
                inner);

        float y = 4f;

        y = DrawSectionTitle(
            y,
            inner.width,
            "ARMY RULE - REANIMATION PROTOCOLS",
            heading);

        y = DrawWrapped(
            y,
            inner.width,
            "Rules below are read from the active Necrons faction pack and selected detachment implementation.",
            body);

        foreach (NecronDetachment detachment
            in NecronDetachmentRuntime
                .GetSelected(
                    controller.FactionId))
        {
            NecronDetachmentRule11 rule =
                NecronsFactionPack11
                    .DetachmentRule(
                        detachment);

            y += 8f;

            y = DrawSectionTitle(
                y,
                inner.width,
                NecronDetachmentRuntime
                    .Name(
                        detachment) +
                (rule != null
                    ? " - " + rule.Name
                    : ""),
                heading);

            if (rule != null)
            {
                y = DrawWrapped(
                    y,
                    inner.width,
                    rule.Rule,
                    body,
                    12f);
            }
        }

        var enhancements =
            NecronsFactionPack11
                .EnhancementsFor(
                    controller.FactionId);

        if (enhancements.Count > 0)
        {
            y += 10f;
            y = DrawSectionTitle(
                y,
                inner.width,
                "ENHANCEMENTS",
                heading);

            foreach (NecronEnhancement11 item
                in enhancements)
            {
                y = DrawSectionTitle(
                    y,
                    inner.width,
                    item.Name +
                    " - " +
                    item.Points +
                    " PTS",
                    heading);

                y = DrawWrapped(
                    y,
                    inner.width,
                    item.Rule,
                    body,
                    12f);
            }
        }

        var stratagems =
            NecronsFactionPack11
                .StratagemsFor(
                    controller.FactionId);

        if (stratagems.Count > 0)
        {
            y += 10f;
            y = DrawSectionTitle(
                y,
                inner.width,
                "STRATAGEMS",
                heading);

            foreach (NecronStratagem11 item
                in stratagems)
            {
                y = DrawSectionTitle(
                    y,
                    inner.width,
                    item.Name +
                    " - " +
                    item.Cost +
                    "CP",
                    heading);

                y = DrawWrapped(
                    y,
                    inner.width,
                    item.FullRule,
                    body,
                    12f);
            }
        }

        GUI.EndScrollView();
    }
}

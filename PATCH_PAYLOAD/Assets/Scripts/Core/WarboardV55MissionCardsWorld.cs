using UnityEngine;

// WARBOARD_MISSION_CARD_ROW_R2_2
// One physical world-UI row:
// [P1 PRIMARY] [P1 SECONDARY] [MATCH SCOREBOARD] [P2 PRIMARY] [P2 SECONDARY]

[DefaultExecutionOrder(-31880)]
public sealed class WarboardV55MissionCardsWorld : MonoBehaviour
{
    private sealed class Card
    {
        public GameObject Root;
        public TextMesh Text;
        public string LastText = "";
    }

    private GameController game;

    private Card p1Primary;
    private Card p1Secondary;
    private Card p2Primary;
    private Card p2Secondary;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<WarboardV55MissionCardsWorld>() != null)
            return;

        GameObject root = new GameObject("Warboard Mission Card Row R2.2");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<WarboardV55MissionCardsWorld>();
    }

    private void Start()
    {
        game = GameController.Current;
        BuildCards();
    }

    private void LateUpdate()
    {
        if (game == null)
            game = GameController.Current;

        if (game == null)
        {
            SetVisible(false);
            return;
        }

        if (p1Primary == null)
            BuildCards();

        bool ready = game.WorldUiFactionCount >= 2;
        SetVisible(ready);

        if (!ready)
            return;

        UpdateCard(p1Primary, game.WorldUiPrimaryCardText55(0));
        UpdateCard(p1Secondary, game.WorldUiSecondaryCardText55(0));
        UpdateCard(p2Primary, game.WorldUiPrimaryCardText55(1));
        UpdateCard(p2Secondary, game.WorldUiSecondaryCardText55(1));
    }

    private void BuildCards()
    {
        if (p1Primary != null)
            return;

        // BattlefieldWorldUI scoreboard is centred at:
        // (0, 5, BoardDepth/2 + 4), width 15.5, height 5.2.
        // Cards deliberately use that exact Y/Z and height.
        float z = GameController.BoardDepth * 0.5f + 4.0f;
        float y = 5.0f;

        p1Primary = CreateCard(
            "Player 1 Primary Card",
            new Vector3(-16.20f, y, z),
            new Color(0.24f, 0.48f, 0.62f),
            "PRIMARY");

        p1Secondary = CreateCard(
            "Player 1 Secondary Card",
            new Vector3(-10.65f, y, z),
            new Color(0.48f, 0.34f, 0.62f),
            "SECONDARY");

        p2Primary = CreateCard(
            "Player 2 Primary Card",
            new Vector3(10.65f, y, z),
            new Color(0.24f, 0.48f, 0.62f),
            "PRIMARY");

        p2Secondary = CreateCard(
            "Player 2 Secondary Card",
            new Vector3(16.20f, y, z),
            new Color(0.48f, 0.34f, 0.62f),
            "SECONDARY");
    }

    private Card CreateCard(
        string name,
        Vector3 position,
        Color accent,
        string typeLabel)
    {
        Card card = new Card();

        card.Root = new GameObject(name);
        card.Root.transform.position = position;

        // Match the scoreboard's camera-facing presentation exactly.
        card.Root.AddComponent<WoundDisplayBillboard>();

        CreateBlock(
            card.Root.transform,
            name + " Wood Frame",
            new Vector3(0f, 0f, 0.08f),
            new Vector3(5.18f, 5.38f, 0.13f),
            new Color(0.30f, 0.18f, 0.085f, 1f));

        CreateBlock(
            card.Root.transform,
            name + " Background",
            Vector3.zero,
            new Vector3(4.90f, 5.12f, 0.13f),
            new Color(0.050f, 0.052f, 0.058f, 1f));

        CreateBlock(
            card.Root.transform,
            name + " Accent",
            new Vector3(0f, 2.43f, -0.10f),
            new Vector3(4.78f, 0.13f, 0.055f),
            accent);

        CreateBlock(
            card.Root.transform,
            name + " Wooden Ledge",
            new Vector3(0f, -2.67f, -0.04f),
            new Vector3(5.30f, 0.22f, 0.34f),
            new Color(0.37f, 0.22f, 0.105f, 1f));

        CreateTypeLabel(
            card.Root.transform,
            name,
            typeLabel,
            accent);

        GameObject textObject = new GameObject(name + " Text");
        textObject.transform.SetParent(card.Root.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 2.07f, -0.12f);

        card.Text = textObject.AddComponent<TextMesh>();
        ApplyFont(card.Text, textObject);

        card.Text.anchor = TextAnchor.UpperCenter;
        card.Text.alignment = TextAlignment.Center;
        card.Text.fontSize = 36;
        card.Text.characterSize = 0.033f;
        card.Text.lineSpacing = 0.88f;
        card.Text.color = new Color(0.94f, 0.95f, 0.97f, 1f);

        return card;
    }

    private void CreateTypeLabel(
        Transform parent,
        string name,
        string typeLabel,
        Color accent)
    {
        GameObject labelObject = new GameObject(name + " Type");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(-2.20f, 2.28f, -0.13f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        ApplyFont(label, labelObject);

        label.anchor = TextAnchor.UpperLeft;
        label.alignment = TextAlignment.Left;
        label.fontSize = 32;
        label.characterSize = 0.027f;
        label.color = accent;
        label.text = typeLabel;
    }

    private GameObject CreateBlock(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Color colour)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;

        Collider collider = block.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = colour;

        return block;
    }

    private void ApplyFont(TextMesh text, GameObject objectWithRenderer)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            return;

        text.font = font;

        MeshRenderer renderer = objectWithRenderer.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.sharedMaterial = font.material;
    }

    private void UpdateCard(Card card, string text)
    {
        if (card == null || card.Text == null)
            return;

        text = text ?? "";

        if (card.LastText == text)
            return;

        card.LastText = text;
        card.Text.text = text;
        FitText(card.Text, text);
    }

    private void FitText(TextMesh textMesh, string value)
    {
        int lines = 1;
        int current = 0;
        int maxLine = 0;

        foreach (char character in value)
        {
            if (character == '\n')
            {
                lines++;
                maxLine = Mathf.Max(maxLine, current);
                current = 0;
            }
            else
            {
                current++;
            }
        }

        maxLine = Mathf.Max(maxLine, current);

        if (lines >= 13 || maxLine >= 52)
        {
            textMesh.fontSize = 29;
            textMesh.characterSize = 0.021f;
        }
        else if (lines >= 10 || maxLine >= 42)
        {
            textMesh.fontSize = 31;
            textMesh.characterSize = 0.024f;
        }
        else if (lines >= 8 || maxLine >= 34)
        {
            textMesh.fontSize = 34;
            textMesh.characterSize = 0.028f;
        }
        else
        {
            textMesh.fontSize = 36;
            textMesh.characterSize = 0.033f;
        }
    }

    private void SetVisible(bool visible)
    {
        SetCardVisible(p1Primary, visible);
        SetCardVisible(p1Secondary, visible);
        SetCardVisible(p2Primary, visible);
        SetCardVisible(p2Secondary, visible);
    }

    private void SetCardVisible(Card card, bool visible)
    {
        if (card == null || card.Root == null)
            return;

        if (card.Root.activeSelf != visible)
            card.Root.SetActive(visible);
    }
}

using UnityEngine;

// WARBOARD_MISSION_CARD_ROW_R2_1
// [P1 PRIMARY] [P1 SECONDARY] [SCOREBOARD] [P2 PRIMARY] [P2 SECONDARY]

[DefaultExecutionOrder(-31870)]
public sealed class WarboardMissionCardRowR21 : MonoBehaviour
{
    private sealed class Card
    {
        public GameObject Root;
        public TextMesh Text;
        public string LastText = "";
    }

    private GameController game;
    private Card p1Primary, p1Secondary, p2Primary, p2Secondary;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<WarboardMissionCardRowR21>() != null) return;
        GameObject root = new GameObject("Warboard Mission Card Row R2.1");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<WarboardMissionCardRowR21>();
    }

    private void Start()
    {
        WarboardV55MissionCardsWorld old = Object.FindAnyObjectByType<WarboardV55MissionCardsWorld>();
        if (old != null) old.enabled = false;

        string[] oldNames = { "Player 1 Primary Card", "Player 1 Secondary Card", "Player 2 Primary Card", "Player 2 Secondary Card", "Mission Card Wooden Rack" };
        foreach (string n in oldNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.Destroy(go);
        }

        game = GameController.Current;
        BuildCards();
    }

    private void LateUpdate()
    {
        if (game == null) game = GameController.Current;
        if (game == null) { SetVisible(false); return; }
        if (p1Primary == null) BuildCards();

        bool ready = game.WorldUiFactionCount >= 2;
        SetVisible(ready);
        if (!ready) return;

        UpdateCard(p1Primary, game.WorldUiPrimaryCardText55(0));
        UpdateCard(p1Secondary, game.WorldUiSecondaryCardText55(0));
        UpdateCard(p2Primary, game.WorldUiPrimaryCardText55(1));
        UpdateCard(p2Secondary, game.WorldUiSecondaryCardText55(1));
    }

    private void BuildCards()
    {
        if (p1Primary != null) return;
        float z = GameController.BoardDepth * 0.5f + 4.0f;
        float y = 5.0f;

        p1Primary   = CreateCard("R2.1 P1 Primary",   new Vector3(-13.72f, y, z), new Color(0.24f,0.48f,0.62f), "PRIMARY");
        p1Secondary = CreateCard("R2.1 P1 Secondary", new Vector3(-9.82f, y, z),  new Color(0.48f,0.34f,0.62f), "SECONDARY");
        p2Primary   = CreateCard("R2.1 P2 Primary",   new Vector3(9.82f, y, z),   new Color(0.24f,0.48f,0.62f), "PRIMARY");
        p2Secondary = CreateCard("R2.1 P2 Secondary", new Vector3(13.72f, y, z),  new Color(0.48f,0.34f,0.62f), "SECONDARY");
    }

    private Card CreateCard(string name, Vector3 pos, Color accent, string type)
    {
        Card card = new Card();
        card.Root = new GameObject(name);
        card.Root.transform.position = pos;
        card.Root.AddComponent<WoundDisplayBillboard>();

        CreateBlock(card.Root.transform, "Wood Back", Vector3.zero, new Vector3(3.82f,4.48f,0.12f), new Color(0.30f,0.18f,0.085f,1f));
        CreateBlock(card.Root.transform, "Card", new Vector3(0f,0f,-0.08f), new Vector3(3.55f,4.15f,0.12f), new Color(0.05f,0.052f,0.058f,1f));
        CreateBlock(card.Root.transform, "Accent", new Vector3(0f,2.00f,-0.16f), new Vector3(3.45f,0.13f,0.055f), accent);
        CreateBlock(card.Root.transform, "Wood Ledge", new Vector3(0f,-2.24f,-0.08f), new Vector3(3.92f,0.22f,0.34f), new Color(0.36f,0.22f,0.11f,1f));

        CreateLabel(card.Root.transform, type, new Vector3(-1.55f,1.84f,-0.19f), accent);

        GameObject textObject = new GameObject(name + " Text");
        textObject.transform.SetParent(card.Root.transform, false);
        textObject.transform.localPosition = new Vector3(0f,1.55f,-0.19f);
        card.Text = textObject.AddComponent<TextMesh>();
        ApplyFont(card.Text, textObject);
        card.Text.anchor = TextAnchor.UpperCenter;
        card.Text.alignment = TextAlignment.Center;
        card.Text.fontSize = 36;
        card.Text.characterSize = 0.030f;
        card.Text.lineSpacing = 0.88f;
        card.Text.color = new Color(0.94f,0.95f,0.97f,1f);
        return card;
    }

    private GameObject CreateBlock(Transform parent, string name, Vector3 pos, Vector3 scale, Color colour)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.material.color = colour;
        return go;
    }

    private void CreateLabel(Transform parent, string value, Vector3 pos, Color colour)
    {
        GameObject go = new GameObject(value + " Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        TextMesh t = go.AddComponent<TextMesh>();
        ApplyFont(t, go);
        t.anchor = TextAnchor.UpperLeft;
        t.alignment = TextAlignment.Left;
        t.fontSize = 32;
        t.characterSize = 0.027f;
        t.color = colour;
        t.text = value;
    }

    private void ApplyFont(TextMesh text, GameObject go)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) return;
        text.font = font;
        MeshRenderer r = go.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = font.material;
    }

    private void UpdateCard(Card card, string text)
    {
        if (card == null || card.Text == null) return;
        text = text ?? "";
        if (card.LastText == text) return;
        card.LastText = text;
        card.Text.text = text;

        int lines = 1, current = 0, maxLine = 0;
        foreach (char c in text)
        {
            if (c == '\n') { lines++; maxLine = Mathf.Max(maxLine,current); current = 0; }
            else current++;
        }
        maxLine = Mathf.Max(maxLine,current);

        if (lines >= 12 || maxLine >= 42) { card.Text.fontSize = 31; card.Text.characterSize = 0.022f; }
        else if (lines >= 9 || maxLine >= 34) { card.Text.fontSize = 33; card.Text.characterSize = 0.025f; }
        else { card.Text.fontSize = 36; card.Text.characterSize = 0.030f; }
    }

    private void SetVisible(bool visible)
    {
        SetCardVisible(p1Primary,visible); SetCardVisible(p1Secondary,visible);
        SetCardVisible(p2Primary,visible); SetCardVisible(p2Secondary,visible);
    }

    private void SetCardVisible(Card card, bool visible)
    {
        if (card == null || card.Root == null) return;
        if (card.Root.activeSelf != visible) card.Root.SetActive(visible);
    }
}

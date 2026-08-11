using UnityEngine;

// WARBOARD_MISSION_CARD_ROW_R2_1
//
// One horizontal command row:
// [P1 PRIMARY] [P1 SECONDARY] [MATCH SCOREBOARD] [P2 PRIMARY] [P2 SECONDARY]
// Each card has its own wood frame/ledge and faces the camera like the scoreboard.

[DefaultExecutionOrder(-31880)]
public sealed class WarboardV55MissionCardsWorld :
    MonoBehaviour
{
    private sealed class Card
    {
        public GameObject Root;
        public TextMesh Text;
        public Renderer Background;
        public string LastText = "";
    }

    private GameController game;

    private Card p1Primary;
    private Card p1Secondary;
    private Card p2Primary;
    private Card p2Secondary;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType
            .AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindAnyObjectByType<
                WarboardV55MissionCardsWorld>() !=
            null)
        {
            return;
        }

        GameObject root =
            new GameObject(
                "Warboard Mission Card Rack R2"
            );

        Object.DontDestroyOnLoad(
            root
        );

        root.AddComponent<
            WarboardV55MissionCardsWorld
        >();
    }

    private void Start()
    {
        game =
            GameController.Current;

        BuildCards();
    }

    private void LateUpdate()
    {
        if (game == null)
            game =
                GameController.Current;

        if (game == null)
        {
            SetVisible(false);
            return;
        }

        if (p1Primary == null)
            BuildCards();

        bool ready =
            game.WorldUiFactionCount >= 2;

        SetVisible(
            ready
        );

        if (!ready)
            return;

        UpdateCard(
            p1Primary,
            game.WorldUiPrimaryCardText55(
                0
            )
        );

        UpdateCard(
            p1Secondary,
            game.WorldUiSecondaryCardText55(
                0
            )
        );

        UpdateCard(
            p2Primary,
            game.WorldUiPrimaryCardText55(
                1
            )
        );

        UpdateCard(
            p2Secondary,
            game.WorldUiSecondaryCardText55(
                1
            )
        );
    }

    private void BuildCards()
    {
        if (p1Primary != null)
            return;

        // Match BattlefieldWorldUI scoreboard exactly:
        // scoreboard centre = (0, 5.0, BoardDepth/2 + 4.0)
        float z =
            GameController.BoardDepth *
                0.5f +
            4.0f;

        float y =
            5.0f;

        // Existing scoreboard width is 15.5 (edges at +/-7.75).
        // These card centres create one continuous row with a small gap.
        p1Primary =
            CreateCard(
                "Player 1 Primary Card",
                new Vector3(
                    -13.72f,
                    y,
                    z
                ),
                new Color(
                    0.24f,
                    0.48f,
                    0.62f
                )
            );

        p1Secondary =
            CreateCard(
                "Player 1 Secondary Card",
                new Vector3(
                    -9.82f,
                    y,
                    z
                ),
                new Color(
                    0.48f,
                    0.34f,
                    0.62f
                )
            );

        p2Primary =
            CreateCard(
                "Player 2 Primary Card",
                new Vector3(
                    9.82f,
                    y,
                    z
                ),
                new Color(
                    0.24f,
                    0.48f,
                    0.62f
                )
            );

        p2Secondary =
            CreateCard(
                "Player 2 Secondary Card",
                new Vector3(
                    13.72f,
                    y,
                    z
                ),
                new Color(
                    0.48f,
                    0.34f,
                    0.62f
                )
            );
    }

    private Card CreateCard(
        string name,
        Vector3 position,
        Color accent)
    {
        Card card =
            new Card();

        card.Root =
            new GameObject(name);

        card.Root.transform.position =
            position;

        // Same camera-facing behaviour as the scoreboard so all five
        // information panels stay visually aligned as one row.
        card.Root.AddComponent<
            WoundDisplayBillboard
        >();

        GameObject woodBack =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        woodBack.name =
            name +
            " Wood Frame";

        woodBack.transform.SetParent(
            card.Root.transform,
            false
        );

        woodBack.transform.localPosition =
            new Vector3(
                0f,
                0f,
                0.08f
            );

        woodBack.transform.localScale =
            new Vector3(
                3.82f,
                4.48f,
                0.10f
            );

        Collider woodCollider =
            woodBack.GetComponent<
                Collider
            >();

        if (woodCollider != null)
            Object.Destroy(
                woodCollider
            );

        Renderer woodRenderer =
            woodBack.GetComponent<
                Renderer
            >();

        if (woodRenderer != null)
        {
            woodRenderer.material.color =
                new Color(
                    0.30f,
                    0.18f,
                    0.085f,
                    1f
                );
        }

        GameObject background =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        background.name =
            name +
            " Background";

        background.transform.SetParent(
            card.Root.transform,
            false
        );

        background.transform.localScale =
            new Vector3(
                3.55f,
                4.15f,
                0.12f
            );

        Collider collider =
            background.GetComponent<
                Collider
            >();

        if (collider != null)
            Object.Destroy(
                collider
            );

        card.Background =
            background.GetComponent<
                Renderer
            >();

        if (card.Background != null)
        {
            card.Background.material.color =
                new Color(
                    0.050f,
                    0.052f,
                    0.058f,
                    1f
                );
        }

        CreateCardTrim(
            card.Root.transform,
            name,
            accent
        );

        GameObject textObject =
            new GameObject(
                name +
                " Text"
            );

        textObject.transform.SetParent(
            card.Root.transform,
            false
        );

        textObject.transform.localPosition =
            new Vector3(
                0f,
                1.70f,
                -0.078f
            );

        card.Text =
            textObject.AddComponent<
                TextMesh
            >();

        Font font =
            Resources.GetBuiltinResource<
                Font
            >(
                "LegacyRuntime.ttf"
            );

        if (font != null)
        {
            card.Text.font =
                font;

            MeshRenderer renderer =
                textObject.GetComponent<
                    MeshRenderer
                >();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    font.material;
            }
        }

        card.Text.anchor =
            TextAnchor.UpperCenter;

        card.Text.alignment =
            TextAlignment.Center;

        card.Text.fontSize =
            34;

        card.Text.characterSize =
            0.033f;

        card.Text.color =
            new Color(
                0.94f,
                0.95f,
                0.97f,
                1f
            );

        GameObject ledge =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        ledge.name =
            name +
            " Wooden Ledge";

        ledge.transform.SetParent(
            card.Root.transform,
            false
        );

        ledge.transform.localPosition =
            new Vector3(
                0f,
                -2.25f,
                -0.03f
            );

        ledge.transform.localScale =
            new Vector3(
                3.92f,
                0.22f,
                0.34f
            );

        Collider ledgeCollider =
            ledge.GetComponent<
                Collider
            >();

        if (ledgeCollider != null)
            Object.Destroy(
                ledgeCollider
            );

        Renderer ledgeRenderer =
            ledge.GetComponent<
                Renderer
            >();

        if (ledgeRenderer != null)
        {
            ledgeRenderer.material.color =
                new Color(
                    0.36f,
                    0.22f,
                    0.11f,
                    1f
                );
        }

        return card;
    }

    private void CreateCardTrim(
        Transform root,
        string name,
        Color accent)
    {
        CreateCardTrimPiece(
            root,
            name + " Accent",
            new Vector3(
                0f,
                2.02f,
                -0.080f
            ),
            new Vector3(
                3.55f,
                0.12f,
                0.05f
            ),
            accent
        );

        Color border =
            new Color(
                0.34f,
                0.23f,
                0.12f,
                1f
            );

        CreateCardTrimPiece(
            root,
            name + " Left Trim",
            new Vector3(
                -1.80f,
                0f,
                -0.070f
            ),
            new Vector3(
                0.10f,
                4.22f,
                0.07f
            ),
            border
        );

        CreateCardTrimPiece(
            root,
            name + " Right Trim",
            new Vector3(
                1.80f,
                0f,
                -0.070f
            ),
            new Vector3(
                0.10f,
                4.22f,
                0.07f
            ),
            border
        );

        CreateCardTrimPiece(
            root,
            name + " Bottom Trim",
            new Vector3(
                0f,
                -2.08f,
                -0.070f
            ),
            new Vector3(
                3.68f,
                0.10f,
                0.07f
            ),
            border
        );
    }

    private void CreateCardTrimPiece(
        Transform root,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Color colour)
    {
        GameObject piece =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        piece.name =
            name;

        piece.transform.SetParent(
            root,
            false
        );

        piece.transform.localPosition =
            localPosition;

        piece.transform.localScale =
            localScale;

        Collider collider =
            piece.GetComponent<Collider>();

        if (collider != null)
            Object.Destroy(
                collider
            );

        Renderer renderer =
            piece.GetComponent<Renderer>();

        if (renderer != null)
            renderer.material.color =
                colour;
    }

    private void UpdateCard(
        Card card,
        string text)
    {
        if (card == null ||
            card.Text == null)
        {
            return;
        }

        text =
            text ?? "";

        if (card.LastText == text)
            return;

        card.LastText =
            text;

        card.Text.text =
            text;

        int lines = 1;
        int longest = 0;
        int current = 0;

        for (int i = 0;
             i < text.Length;
             i++)
        {
            if (text[i] == '\n')
            {
                lines++;
                longest =
                    Mathf.Max(
                        longest,
                        current
                    );
                current = 0;
            }
            else
            {
                current++;
            }
        }

        longest =
            Mathf.Max(
                longest,
                current
            );

        if (lines >= 12 ||
            longest >= 42)
        {
            card.Text.characterSize =
                0.023f;
        }
        else if (lines >= 9 ||
                 longest >= 34)
        {
            card.Text.characterSize =
                0.026f;
        }
        else
        {
            card.Text.characterSize =
                0.030f;
        }
    }

    private void SetVisible(
        bool visible)
    {
        SetCardVisible(
            p1Primary,
            visible
        );

        SetCardVisible(
            p1Secondary,
            visible
        );

        SetCardVisible(
            p2Primary,
            visible
        );

        SetCardVisible(
            p2Secondary,
            visible
        );
    }

    private void SetCardVisible(
        Card card,
        bool visible)
    {
        if (card == null ||
            card.Root == null)
        {
            return;
        }

        if (card.Root.activeSelf !=
            visible)
        {
            card.Root.SetActive(
                visible
            );
        }
    }
}

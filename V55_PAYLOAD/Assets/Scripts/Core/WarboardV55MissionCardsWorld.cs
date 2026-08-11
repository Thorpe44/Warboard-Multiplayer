using UnityEngine;

// WARBOARD_V55_WORLD_MISSION_CARDS
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
                "Warboard V55 Mission Cards"
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
            game = GameController.Current;

        if (game == null)
        {
            SetVisible(false);
            return;
        }

        if (p1Primary == null)
            BuildCards();

        bool ready =
            game.WorldUiFactionCount >= 2;

        SetVisible(ready);

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

        float z =
            GameController.BoardDepth *
                0.5f +
            4.15f;

        float y = 3.25f;

        p1Primary =
            CreateCard(
                "Player 1 Primary Card",
                new Vector3(
                    -15.4f,
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
                    -10.2f,
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
                    10.2f,
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
                    15.4f,
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

        card.Root.AddComponent<
            WoundDisplayBillboard
        >();

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
                4.8f,
                5.4f,
                0.16f
            );

        Collider collider =
            background.GetComponent<
                Collider
            >();

        if (collider != null)
            Object.Destroy(collider);

        card.Background =
            background.GetComponent<
                Renderer
            >();

        if (card.Background != null)
        {
            card.Background.material.color =
                new Color(
                    0.035f,
                    0.040f,
                    0.052f,
                    0.98f
                );
        }

        GameObject stripe =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        stripe.name =
            name +
            " Accent";

        stripe.transform.SetParent(
            card.Root.transform,
            false
        );

        stripe.transform.localPosition =
            new Vector3(
                0f,
                2.62f,
                -0.10f
            );

        stripe.transform.localScale =
            new Vector3(
                4.8f,
                0.12f,
                0.05f
            );

        Collider stripeCollider =
            stripe.GetComponent<
                Collider
            >();

        if (stripeCollider != null)
            Object.Destroy(
                stripeCollider
            );

        Renderer stripeRenderer =
            stripe.GetComponent<
                Renderer
            >();

        if (stripeRenderer != null)
            stripeRenderer.material.color =
                accent;

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
                2.38f,
                -0.12f
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
            card.Text.font = font;

            MeshRenderer renderer =
                textObject.GetComponent<
                    MeshRenderer
                >();

            if (renderer != null)
                renderer.sharedMaterial =
                    font.material;
        }

        card.Text.anchor =
            TextAnchor.UpperCenter;

        card.Text.alignment =
            TextAlignment.Center;

        card.Text.fontSize = 38;
        card.Text.characterSize =
            0.043f;

        card.Text.color =
            new Color(
                0.94f,
                0.95f,
                0.98f
            );

        return card;
    }

    private void UpdateCard(
        Card card,
        string text)
    {
        if (card == null ||
            card.Text == null ||
            card.LastText == text)
        {
            return;
        }

        card.LastText =
            text ?? "";

        card.Text.text =
            card.LastText;
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

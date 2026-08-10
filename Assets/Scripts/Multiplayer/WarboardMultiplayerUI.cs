using Unity.Netcode;
using UnityEngine;

public class WarboardMultiplayerUI : MonoBehaviour
{
    private enum LaunchMode
    {
        NotChosen,
        SinglePlayer,
        MultiplayerHost,
        MultiplayerClient
    }

    private LaunchMode launchMode =
        LaunchMode.NotChosen;

    private string joinCode = "";

    private bool statusPanelVisible = true;

    private Rect statusRect =
        new Rect(
            20f,
            90f,
            360f,
            190f
        );

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle fieldStyle;
    private GUIStyle statusStyle;

    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D fieldTexture;

    private void Update()
    {
        if (launchMode !=
                LaunchMode.NotChosen &&
            Input.GetKeyDown(KeyCode.F8))
        {
            statusPanelVisible =
                !statusPanelVisible;
        }
    }

    private void OnGUI()
    {
        // Smaller GUI.depth values render above larger ones.
        // Keep the launch screen above Warboard's existing IMGUI.
        int previousDepth =
            GUI.depth;

        GUI.depth = -10000;

        EnsureStyles();

        if (launchMode ==
            LaunchMode.NotChosen)
        {
            DrawLaunchScreen();
        }
        else if (
            launchMode !=
                LaunchMode.SinglePlayer &&
            statusPanelVisible)
        {
            DrawConnectedPanel();
        }

        GUI.depth = previousDepth;
    }

    private void DrawLaunchScreen()
    {
        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height
            ),
            overlayTexture
        );

        float width =
            Mathf.Min(
                620f,
                Screen.width - 40f
            );

        float height =
            470f;

        Rect panel =
            new Rect(
                (Screen.width - width) *
                    0.5f,
                (Screen.height - height) *
                    0.5f,
                width,
                height
            );

        GUI.DrawTexture(
            panel,
            panelTexture
        );

        GUI.Label(
            new Rect(
                panel.x + 32f,
                panel.y + 28f,
                panel.width - 64f,
                48f
            ),
            "WARBOARD",
            titleStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 32f,
                panel.y + 78f,
                panel.width - 64f,
                42f
            ),
            "Choose how you want to play",
            subtitleStyle
        );

        float buttonWidth =
            panel.width - 64f;

        if (GUI.Button(
            new Rect(
                panel.x + 32f,
                panel.y + 140f,
                buttonWidth,
                58f
            ),
            "SINGLE PLAYER",
            buttonStyle))
        {
            launchMode =
                LaunchMode.SinglePlayer;
        }

        WarboardSessionService session =
            WarboardSessionService.Instance;

        bool busy =
            session != null &&
            session.IsBusy;

        GUI.enabled = !busy;

        if (GUI.Button(
            new Rect(
                panel.x + 32f,
                panel.y + 214f,
                buttonWidth,
                58f
            ),
            busy
                ? "CONNECTING..."
                : "HOST MULTIPLAYER",
            buttonStyle))
        {
            if (session != null)
                Host(session);
        }

        GUI.Label(
            new Rect(
                panel.x + 32f,
                panel.y + 294f,
                buttonWidth,
                24f
            ),
            "Join code",
            statusStyle
        );

        joinCode =
            GUI.TextField(
                new Rect(
                    panel.x + 32f,
                    panel.y + 322f,
                    panel.width - 220f,
                    48f
                ),
                joinCode,
                20,
                fieldStyle
            )
            .ToUpperInvariant();

        if (GUI.Button(
            new Rect(
                panel.x +
                    panel.width -
                    172f,
                panel.y + 322f,
                140f,
                48f
            ),
            busy
                ? "WAIT..."
                : "JOIN",
            buttonStyle))
        {
            if (session != null)
                Join(session);
        }

        GUI.enabled = true;

        if (session != null &&
            !string.IsNullOrWhiteSpace(
                session.LastError))
        {
            GUI.Label(
                new Rect(
                    panel.x + 32f,
                    panel.y + 386f,
                    buttonWidth,
                    54f
                ),
                session.LastError,
                statusStyle
            );
        }
        else
        {
            GUI.Label(
                new Rect(
                    panel.x + 32f,
                    panel.y + 390f,
                    buttonWidth,
                    44f
                ),
                "Multiplayer uses a private 2-player Relay session. " +
                "The host receives a join code for Player 2.",
                statusStyle
            );
        }
    }

    private void DrawConnectedPanel()
    {
        WarboardSessionService session =
            WarboardSessionService.Instance;

        if (session == null)
            return;

        statusRect =
            GUI.Window(
                440045,
                statusRect,
                DrawStatusWindow,
                "WARBOARD MULTIPLAYER"
            );
    }

    private void DrawStatusWindow(
        int id)
    {
        WarboardSessionService session =
            WarboardSessionService.Instance;

        NetworkManager network =
            NetworkManager.Singleton;

        if (session == null)
            return;

        string role =
            launchMode ==
                LaunchMode.MultiplayerHost
            ? "HOST / PLAYER 1"
            : "CLIENT / PLAYER 2";

        string state =
            network != null &&
            network.IsListening
            ? "CONNECTED"
            : "CONNECTING";

        GUI.Label(
            new Rect(
                14f,
                34f,
                330f,
                24f
            ),
            role + "  |  " + state
        );

        GUI.Label(
            new Rect(
                14f,
                62f,
                330f,
                24f
            ),
            "JOIN CODE: " +
            session.JoinCode
        );

        if (launchMode ==
                LaunchMode.MultiplayerHost &&
            GUI.Button(
                new Rect(
                    14f,
                    94f,
                    150f,
                    32f
                ),
                "COPY JOIN CODE"))
        {
            GUIUtility.systemCopyBuffer =
                session.JoinCode;
        }

        if (GUI.Button(
            new Rect(
                178f,
                94f,
                150f,
                32f
            ),
            "LEAVE MULTIPLAYER"))
        {
            Leave(session);
        }

        WarboardNetworkBridge bridge =
            WarboardNetworkBridge.Instance;

        GUI.Label(
            new Rect(
                14f,
                138f,
                330f,
                38f
            ),
            bridge != null
            ? "State revision: " +
              bridge.CurrentRevision +
              "   |   F8 hides this panel"
            : "State bridge starting..."
        );

        GUI.DragWindow(
            new Rect(
                0f,
                0f,
                360f,
                26f
            )
        );
    }

    private async void Host(
        WarboardSessionService session)
    {
        string code =
            await session.HostAsync();

        if (!string.IsNullOrWhiteSpace(
                code))
        {
            launchMode =
                LaunchMode.MultiplayerHost;

            statusPanelVisible = true;
        }
    }

    private async void Join(
        WarboardSessionService session)
    {
        bool joined =
            await session.JoinAsync(
                joinCode
            );

        if (joined)
        {
            launchMode =
                LaunchMode.MultiplayerClient;

            statusPanelVisible = true;
        }
    }

    private async void Leave(
        WarboardSessionService session)
    {
        await session.LeaveAsync();

        launchMode =
            LaunchMode.NotChosen;

        statusPanelVisible = true;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        overlayTexture =
            SolidTexture(
                new Color(
                    0.01f,
                    0.015f,
                    0.02f,
                    0.94f
                )
            );

        panelTexture =
            SolidTexture(
                new Color(
                    0.055f,
                    0.065f,
                    0.08f,
                    0.98f
                )
            );

        buttonTexture =
            SolidTexture(
                new Color(
                    0.10f,
                    0.13f,
                    0.17f,
                    1f
                )
            );

        buttonHoverTexture =
            SolidTexture(
                new Color(
                    0.16f,
                    0.22f,
                    0.29f,
                    1f
                )
            );

        fieldTexture =
            SolidTexture(
                new Color(
                    0.025f,
                    0.03f,
                    0.04f,
                    1f
                )
            );

        titleStyle =
            new GUIStyle(
                GUI.skin.label
            );

        titleStyle.fontSize = 34;
        titleStyle.fontStyle =
            FontStyle.Bold;
        titleStyle.alignment =
            TextAnchor.MiddleCenter;
        titleStyle.normal.textColor =
            Color.white;

        subtitleStyle =
            new GUIStyle(
                GUI.skin.label
            );

        subtitleStyle.fontSize = 18;
        subtitleStyle.alignment =
            TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor =
            new Color(
                0.72f,
                0.78f,
                0.84f
            );

        buttonStyle =
            new GUIStyle(
                GUI.skin.button
            );

        buttonStyle.fontSize = 16;
        buttonStyle.fontStyle =
            FontStyle.Bold;

        buttonStyle.normal.background =
            buttonTexture;

        buttonStyle.hover.background =
            buttonHoverTexture;

        buttonStyle.active.background =
            buttonHoverTexture;

        buttonStyle.normal.textColor =
            Color.white;

        buttonStyle.hover.textColor =
            Color.white;

        buttonStyle.active.textColor =
            Color.white;

        fieldStyle =
            new GUIStyle(
                GUI.skin.textField
            );

        fieldStyle.fontSize = 19;
        fieldStyle.alignment =
            TextAnchor.MiddleCenter;

        fieldStyle.normal.background =
            fieldTexture;

        fieldStyle.focused.background =
            fieldTexture;

        fieldStyle.normal.textColor =
            Color.white;

        fieldStyle.focused.textColor =
            Color.white;

        statusStyle =
            new GUIStyle(
                GUI.skin.label
            );

        statusStyle.fontSize = 13;
        statusStyle.wordWrap = true;

        statusStyle.normal.textColor =
            new Color(
                0.72f,
                0.78f,
                0.84f
            );
    }

    private Texture2D SolidTexture(
        Color color)
    {
        Texture2D texture =
            new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false
            );

        texture.SetPixel(
            0,
            0,
            color
        );

        texture.Apply();

        return texture;
    }

    private void OnDestroy()
    {
        if (overlayTexture != null)
            Destroy(overlayTexture);

        if (panelTexture != null)
            Destroy(panelTexture);

        if (buttonTexture != null)
            Destroy(buttonTexture);

        if (buttonHoverTexture != null)
            Destroy(buttonHoverTexture);

        if (fieldTexture != null)
            Destroy(fieldTexture);
    }
}

using Unity.Netcode;
using UnityEngine;

public class WarboardMultiplayerUI : MonoBehaviour
{
    private Rect windowRect =
        new Rect(
            20f,
            120f,
            330f,
            190f
        );

    private string joinCode = "";
    private bool visible = true;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        windowRect =
            GUI.Window(
                440045,
                windowRect,
                DrawWindow,
                "WARBOARD MULTIPLAYER"
            );
    }

    private void DrawWindow(
        int id)
    {
        WarboardSessionService session =
            WarboardSessionService.Instance;

        NetworkManager network =
            NetworkManager.Singleton;

        if (session == null)
        {
            GUI.Label(
                new Rect(
                    12f,
                    32f,
                    300f,
                    24f
                ),
                "Multiplayer runtime not ready."
            );

            GUI.DragWindow();
            return;
        }

        if (!session.IsInSession)
        {
            GUI.Label(
                new Rect(
                    12f,
                    32f,
                    300f,
                    22f
                ),
                "Create a 2-player Relay session or join one."
            );

            if (GUI.Button(
                new Rect(
                    12f,
                    62f,
                    130f,
                    34f
                ),
                session.IsBusy
                    ? "WORKING..."
                    : "HOST GAME"))
            {
                if (!session.IsBusy)
                    Host(session);
            }

            joinCode =
                GUI.TextField(
                    new Rect(
                        12f,
                        106f,
                        150f,
                        30f
                    ),
                    joinCode,
                    16
                );

            if (GUI.Button(
                new Rect(
                    172f,
                    106f,
                    130f,
                    30f
                ),
                session.IsBusy
                    ? "WORKING..."
                    : "JOIN CODE"))
            {
                if (!session.IsBusy)
                    Join(session);
            }

            if (!string.IsNullOrWhiteSpace(
                    session.LastError))
            {
                GUI.Label(
                    new Rect(
                        12f,
                        145f,
                        300f,
                        40f
                    ),
                    session.LastError
                );
            }
        }
        else
        {
            string role =
                session.IsHost
                ? "HOST / PLAYER 1"
                : "CLIENT / PLAYER 2";

            string networkState =
                network != null &&
                network.IsListening
                ? "CONNECTED"
                : "CONNECTING";

            GUI.Label(
                new Rect(
                    12f,
                    32f,
                    300f,
                    22f
                ),
                role +
                "  |  " +
                networkState
            );

            GUI.Label(
                new Rect(
                    12f,
                    60f,
                    300f,
                    22f
                ),
                "JOIN CODE: " +
                session.JoinCode
            );

            if (session.IsHost &&
                GUI.Button(
                    new Rect(
                        12f,
                        90f,
                        138f,
                        30f
                    ),
                    "COPY JOIN CODE"))
            {
                GUIUtility.systemCopyBuffer =
                    session.JoinCode;
            }

            if (GUI.Button(
                new Rect(
                    164f,
                    90f,
                    138f,
                    30f
                ),
                "LEAVE"))
            {
                Leave(session);
            }

            WarboardNetworkBridge bridge =
                WarboardNetworkBridge.Instance;

            GUI.Label(
                new Rect(
                    12f,
                    132f,
                    300f,
                    42f
                ),
                bridge != null
                ? "STATE REVISION: " +
                  bridge.CurrentRevision +
                  "\nF8 hides/shows this panel."
                : "State bridge starting..."
            );
        }

        GUI.DragWindow(
            new Rect(
                0f,
                0f,
                330f,
                24f
            )
        );
    }

    private async void Host(
        WarboardSessionService session)
    {
        await session.HostAsync();
    }

    private async void Join(
        WarboardSessionService session)
    {
        await session.JoinAsync(
            joinCode
        );
    }

    private async void Leave(
        WarboardSessionService session)
    {
        await session.LeaveAsync();
    }
}

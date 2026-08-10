using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public static class WarboardMultiplayerBootstrap
{
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        EnsureNetworkManager();

        if (Object.FindAnyObjectByType<
                WarboardSessionService>() != null)
        {
            return;
        }

        GameObject root =
            new GameObject(
                "Warboard Multiplayer Runtime"
            );

        Object.DontDestroyOnLoad(root);

        root.AddComponent<
            WarboardSessionService>();

        root.AddComponent<
            WarboardNetworkBridge>();

        root.AddComponent<
            WarboardDiceNetworkBridge>();

        root.AddComponent<
            WarboardMultiplayerUI>();
    }

    private static void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null)
            return;

        // NetworkManager.NetworkConfig is not automatically constructed
        // when NetworkManager is added dynamically at runtime.
        //
        // Build it while the GameObject is inactive so NetworkManager.Awake
        // sees a fully configured NetworkConfig when the object is activated.
        GameObject go =
            new GameObject(
                "Warboard NetworkManager"
            );

        go.SetActive(false);

        UnityTransport transport =
            go.AddComponent<UnityTransport>();

        NetworkManager manager =
            go.AddComponent<NetworkManager>();

        manager.NetworkConfig =
            new NetworkConfig
            {
                NetworkTransport =
                    transport,

                // Warboard owns its scene/world reconstruction through the
                // snapshot bridge rather than NGO scene management.
                EnableSceneManagement =
                    false,

                // Warboard miniatures are normal runtime GameObjects and are
                // synchronized by snapshots, not NetworkPrefab spawning.
                ForceSamePrefabs =
                    false,

                ConnectionApproval =
                    false,

                TickRate =
                    30
            };

        go.SetActive(true);

        Object.DontDestroyOnLoad(go);
    }
}

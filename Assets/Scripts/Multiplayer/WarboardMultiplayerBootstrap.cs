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

        if (Object.FindFirstObjectByType<
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
            WarboardMultiplayerUI>();
    }

    private static void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null)
            return;

        GameObject go =
            new GameObject(
                "Warboard NetworkManager"
            );

        Object.DontDestroyOnLoad(go);

        UnityTransport transport =
            go.AddComponent<UnityTransport>();

        NetworkManager manager =
            go.AddComponent<NetworkManager>();

        manager.NetworkConfig.NetworkTransport =
            transport;

        // Warboard currently runs a single gameplay scene and builds the
        // battlefield at runtime. State replication handles world state.
        manager.NetworkConfig.EnableSceneManagement =
            false;
    }
}

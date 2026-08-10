using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class WarboardDiceNetworkBridge : MonoBehaviour
{
    private const string CommandMessage = "WB_DICE_COMMAND";
    private const string StateMessage = "WB_DICE_STATE";
    private const string RequestMessage = "WB_DICE_REQUEST";

    private const float HostPollInterval = 0.10f;
    private const float ClientRequestInterval = 0.75f;
    private const float SafetyBroadcastInterval = 2.0f;

    public static WarboardDiceNetworkBridge Instance
    {
        get;
        private set;
    }

    private NetworkManager manager;
    private TraditionalDiceTray3D tray;

    private bool handlersRegistered;
    private bool hasAuthoritativeState;

    private float nextHostPoll;
    private float nextClientRequest;
    private float nextSafetyBroadcast;

    private string lastHostHash = "";
    private int authoritativeSequence;
    private bool lastHostRolling;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        ResolveRuntime();

        if (manager == null ||
            !manager.IsListening ||
            manager.CustomMessagingManager == null)
        {
            return;
        }

        EnsureHandlers();

        if (manager.IsServer)
        {
            HostUpdate();
        }
        else
        {
            ClientUpdate();
        }
    }

    private void ResolveRuntime()
    {
        if (manager == null)
            manager = NetworkManager.Singleton;

        if (tray == null)
            tray = FindAnyObjectByType<TraditionalDiceTray3D>();
    }

    private void EnsureHandlers()
    {
        if (handlersRegistered)
            return;

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            CommandMessage,
            ReceiveCommand);

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            StateMessage,
            ReceiveState);

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            RequestMessage,
            ReceiveStateRequest);

        manager.OnClientConnectedCallback += OnClientConnected;
        manager.OnClientDisconnectCallback += OnClientDisconnected;

        handlersRegistered = true;

        Debug.Log(
            "[Warboard Dice MP] bridge registered as " +
            (manager.IsServer ? "HOST" : "CLIENT")
        );
    }

    private void OnDestroy()
    {
        if (manager != null &&
            handlersRegistered &&
            manager.CustomMessagingManager != null)
        {
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(
                CommandMessage);

            manager.CustomMessagingManager.UnregisterNamedMessageHandler(
                StateMessage);

            manager.CustomMessagingManager.UnregisterNamedMessageHandler(
                RequestMessage);

            manager.OnClientConnectedCallback -= OnClientConnected;
            manager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (Instance == this)
            Instance = null;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (manager == null)
            return;

        if (manager.IsServer &&
            clientId != NetworkManager.ServerClientId)
        {
            SendCurrentState(clientId);
        }
        else if (!manager.IsServer &&
                 clientId == manager.LocalClientId)
        {
            hasAuthoritativeState = false;
            nextClientRequest = 0f;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (manager != null &&
            !manager.IsServer &&
            clientId == manager.LocalClientId)
        {
            hasAuthoritativeState = false;
            nextClientRequest = 0f;
        }
    }

    private void HostUpdate()
    {
        if (tray == null)
        {
            ResolveRuntime();
            return;
        }

        if (Time.unscaledTime < nextHostPoll)
            return;

        nextHostPoll =
            Time.unscaledTime + HostPollInterval;

        bool rolling = tray.MultiplayerDiceRolling;

        if (rolling && !lastHostRolling)
            authoritativeSequence++;

        lastHostRolling = rolling;

        WarboardDiceSnapshot state =
            tray.CaptureMultiplayerDiceSnapshot(
                authoritativeSequence);

        string json = JsonUtility.ToJson(state);
        string hash = Hash128.Compute(json).ToString();

        if (hash != lastHostHash)
        {
            lastHostHash = hash;
            BroadcastStateJson(json);
            nextSafetyBroadcast =
                Time.unscaledTime + SafetyBroadcastInterval;
            return;
        }

        if (Time.unscaledTime >= nextSafetyBroadcast &&
            manager.ConnectedClientsIds.Count > 1)
        {
            BroadcastStateJson(json);
            nextSafetyBroadcast =
                Time.unscaledTime + SafetyBroadcastInterval;
        }
    }

    private void ClientUpdate()
    {
        if (manager == null ||
            !manager.IsConnectedClient ||
            hasAuthoritativeState)
        {
            return;
        }

        if (Time.unscaledTime < nextClientRequest)
            return;

        nextClientRequest =
            Time.unscaledTime + ClientRequestInterval;

        using (FastBufferWriter writer =
               new FastBufferWriter(8, Allocator.Temp))
        {
            writer.WriteValueSafe((byte)1);

            manager.CustomMessagingManager.SendNamedMessage(
                RequestMessage,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }
    }

    private void ReceiveStateRequest(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer)
            return;

        SendCurrentState(senderId);
    }

    private void ReceiveCommand(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null ||
            !manager.IsServer ||
            senderId == NetworkManager.ServerClientId)
        {
            return;
        }

        string json;
        reader.ReadValueSafe(out json);

        WarboardDiceCommand command =
            JsonUtility.FromJson<WarboardDiceCommand>(json);

        if (command == null)
            return;

        ResolveRuntime();

        if (tray == null)
            return;

        switch (command.type)
        {
            case WarboardDiceCommand.Roll:
                tray.MultiplayerAuthoritativeRoll(
                    command.pool);
                break;

            case WarboardDiceCommand.Reroll:
                tray.MultiplayerAuthoritativeReroll(
                    command.selectedIds);
                break;

            case WarboardDiceCommand.Clear:
                tray.MultiplayerAuthoritativeClear();
                break;

            case WarboardDiceCommand.AdjustPool:
                tray.MultiplayerAuthoritativeAdjustPool(
                    command.sides,
                    command.delta);
                break;

            case WarboardDiceCommand.Select:
                tray.MultiplayerAuthoritativeSelect(
                    command.dieId,
                    command.selected);
                break;
        }

        nextHostPoll = 0f;
    }

    private void ReceiveState(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null ||
            manager.IsServer ||
            senderId != NetworkManager.ServerClientId)
        {
            return;
        }

        string json;
        reader.ReadValueSafe(out json);

        WarboardDiceSnapshot state =
            JsonUtility.FromJson<WarboardDiceSnapshot>(json);

        if (state == null)
            return;

        ResolveRuntime();

        if (tray == null)
            return;

        tray.ApplyMultiplayerDiceSnapshot(state);
        hasAuthoritativeState = true;
    }

    private void SendCurrentState(ulong clientId)
    {
        ResolveRuntime();

        if (tray == null ||
            manager == null ||
            !manager.IsServer)
        {
            return;
        }

        WarboardDiceSnapshot state =
            tray.CaptureMultiplayerDiceSnapshot(
                authoritativeSequence);

        SendStateJson(
            clientId,
            JsonUtility.ToJson(state));
    }

    private void BroadcastStateJson(string json)
    {
        if (manager == null || !manager.IsServer)
            return;

        foreach (ulong clientId in manager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
                continue;

            SendStateJson(clientId, json);
        }
    }

    private void SendStateJson(
        ulong clientId,
        string json)
    {
        int capacity = Mathf.Max(
            32768,
            (json != null ? json.Length * 4 : 0) + 2048);

        using (FastBufferWriter writer =
               new FastBufferWriter(
                   capacity,
                   Allocator.Temp))
        {
            writer.WriteValueSafe(json ?? "");

            manager.CustomMessagingManager.SendNamedMessage(
                StateMessage,
                clientId,
                writer,
                NetworkDelivery.ReliableFragmentedSequenced);
        }
    }

    private void SendCommand(
        WarboardDiceCommand command)
    {
        if (manager == null)
            manager = NetworkManager.Singleton;

        if (manager == null ||
            !manager.IsConnectedClient ||
            manager.IsServer ||
            manager.CustomMessagingManager == null)
        {
            return;
        }

        string json = JsonUtility.ToJson(command);

        int capacity = Mathf.Max(
            4096,
            json.Length * 4 + 512);

        using (FastBufferWriter writer =
               new FastBufferWriter(
                   capacity,
                   Allocator.Temp))
        {
            writer.WriteValueSafe(json);

            manager.CustomMessagingManager.SendNamedMessage(
                CommandMessage,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }
    }

    private static bool ShouldIntercept(
        TraditionalDiceTray3D value)
    {
        if (value == null ||
            value.MultiplayerDiceNetworkBypass)
        {
            return false;
        }

        NetworkManager network =
            NetworkManager.Singleton;

        return
            network != null &&
            network.IsListening &&
            network.IsConnectedClient &&
            !network.IsServer &&
            Instance != null;
    }

    public static bool TryInterceptRoll(
        TraditionalDiceTray3D value)
    {
        if (!ShouldIntercept(value))
            return false;

        Instance.SendCommand(
            new WarboardDiceCommand
            {
                type = WarboardDiceCommand.Roll,
                pool = value.MultiplayerCaptureDicePool()
            });

        return true;
    }

    public static bool TryInterceptReroll(
        TraditionalDiceTray3D value)
    {
        if (!ShouldIntercept(value))
            return false;

        Instance.SendCommand(
            new WarboardDiceCommand
            {
                type = WarboardDiceCommand.Reroll,
                selectedIds =
                    value.MultiplayerSelectedDiceIds()
            });

        return true;
    }

    public static bool TryInterceptClear(
        TraditionalDiceTray3D value)
    {
        if (!ShouldIntercept(value))
            return false;

        Instance.SendCommand(
            new WarboardDiceCommand
            {
                type = WarboardDiceCommand.Clear
            });

        return true;
    }

    public static bool TryInterceptPoolAdjustment(
        TraditionalDiceTray3D value,
        int sides,
        int delta)
    {
        if (!ShouldIntercept(value))
            return false;

        Instance.SendCommand(
            new WarboardDiceCommand
            {
                type = WarboardDiceCommand.AdjustPool,
                sides = sides,
                delta = delta
            });

        return true;
    }

    public static bool TryInterceptSelection(
        TraditionalDiceTray3D value,
        int dieId,
        bool selected)
    {
        if (!ShouldIntercept(value))
            return false;

        // Immediate local highlight, then the host echoes the authoritative
        // selection to both players.
        Instance.SendCommand(
            new WarboardDiceCommand
            {
                type = WarboardDiceCommand.Select,
                dieId = dieId,
                selected = selected
            });

        return true;
    }
}

[Serializable]
public class WarboardDiceCommand
{
    public const int Roll = 1;
    public const int Reroll = 2;
    public const int Clear = 3;
    public const int AdjustPool = 4;
    public const int Select = 5;

    public int type;
    public WarboardDicePoolEntry[] pool =
        new WarboardDicePoolEntry[0];
    public int[] selectedIds =
        new int[0];
    public int sides;
    public int delta;
    public int dieId;
    public bool selected;
}

[Serializable]
public class WarboardDiceSnapshot
{
    public int sequence;
    public bool rolling;
    public string settledText = "";
    public WarboardDicePoolEntry[] pool =
        new WarboardDicePoolEntry[0];
    public WarboardDieSnapshot[] dice =
        new WarboardDieSnapshot[0];
}

[Serializable]
public class WarboardDicePoolEntry
{
    public int sides;
    public int count;
}

[Serializable]
public class WarboardDieSnapshot
{
    public int id;
    public int sides;
    public int value;
    public bool selected;
    public Vector3 position;
    public Quaternion rotation;
}

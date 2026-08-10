using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class WarboardNetworkBridge : MonoBehaviour
{
    private const string StateMessage = "WB_STATE_CHUNK";
    private const string ProposalMessage = "WB_PROPOSAL_CHUNK";
    private const string RequestStateMessage = "WB_REQUEST_STATE";
    private const int ChunkCharacters = 6000;

    public static WarboardNetworkBridge Instance { get; private set; }

    public int CurrentRevision { get { return currentRevision; } }

    public bool HasCanonicalState
    {
        get { return !string.IsNullOrWhiteSpace(canonicalHash); }
    }

    public string DebugStatus
    {
        get
        {
            if (manager == null) return "NO NETWORK MANAGER";
            if (!manager.IsListening) return "NETWORK NOT LISTENING";

            if (manager.IsServer)
            {
                return "HOST | NGO clients=" +
                       manager.ConnectedClientsIds.Count +
                       " | rev=" + currentRevision +
                       " | stateTX=" + stateMessagesSent +
                       " | proposalRX=" + proposalsReceived;
            }

            return (manager.IsConnectedClient ? "CLIENT CONNECTED" : "CLIENT CONNECTING") +
                   " | canonical=" + (HasCanonicalState ? "YES" : "NO") +
                   " | rev=" + currentRevision +
                   " | requests=" + stateRequestsSent +
                   " | stateRX=" + stateMessagesReceived;
        }
    }

    private class IncomingTransfer
    {
        public string[] Chunks;
        public int Received;
    }

    private readonly Dictionary<string, IncomingTransfer> incoming =
        new Dictionary<string, IncomingTransfer>();

    private NetworkManager manager;
    private GameController game;
    private bool handlersRegistered;

    private int currentRevision;
    private string canonicalHash = "";
    private string submittedHash = "";

    private float nextSyncTime;
    private float nextInitialRequestTime;
    private float nextSafetyBroadcastTime;
    private float suppressClientSubmissionUntil;

    private bool lastServerRole;

    private int stateRequestsSent;
    private int stateMessagesSent;
    private int stateMessagesReceived;
    private int proposalsReceived;

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

        if (manager == null || !manager.IsListening)
            return;

        EnsureHandlers();

        bool serverNow = manager.IsServer;
        if (serverNow != lastServerRole)
        {
            lastServerRole = serverNow;
            submittedHash = "";
            canonicalHash = "";
            nextSyncTime = 0f;
            nextInitialRequestTime = 0f;
            nextSafetyBroadcastTime = 0f;
        }

        // The original bridge requested state once as soon as IsListening was
        // true. That can be before NGO is actually connected. Retry until the
        // first authoritative snapshot is received.
        if (!manager.IsServer &&
            manager.IsConnectedClient &&
            !HasCanonicalState &&
            Time.unscaledTime >= nextInitialRequestTime)
        {
            nextInitialRequestTime = Time.unscaledTime + 0.75f;
            RequestCanonicalState();
        }

        if (Time.unscaledTime < nextSyncTime)
            return;

        nextSyncTime = Time.unscaledTime + 0.18f;

        if (game == null || game.MultiplayerApplyingSnapshot)
            return;

        if (manager.IsServer)
            HostPump();
        else
            ClientPump();
    }

    public void NotifyHostChanged()
    {
        canonicalHash = "";
        submittedHash = "";
        nextSyncTime = 0f;
        nextInitialRequestTime = 0f;
        nextSafetyBroadcastTime = 0f;
    }

    private void ResolveRuntime()
    {
        if (manager == null)
            manager = NetworkManager.Singleton;

        if (game == null)
            game = FindAnyObjectByType<GameController>();
    }

    private void EnsureHandlers()
    {
        if (handlersRegistered ||
            manager == null ||
            manager.CustomMessagingManager == null)
            return;

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            StateMessage, ReceiveStateChunk);

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            ProposalMessage, ReceiveProposalChunk);

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            RequestStateMessage, ReceiveStateRequest);

        manager.OnClientConnectedCallback += OnClientConnected;
        manager.OnClientDisconnectCallback += OnClientDisconnected;
        handlersRegistered = true;

        Debug.Log("[Warboard MP] Message bridge registered as " +
                  (manager.IsServer ? "HOST" : "CLIENT"));
    }

    private void OnDestroy()
    {
        if (manager == null || !handlersRegistered ||
            manager.CustomMessagingManager == null)
            return;

        manager.CustomMessagingManager.UnregisterNamedMessageHandler(StateMessage);
        manager.CustomMessagingManager.UnregisterNamedMessageHandler(ProposalMessage);
        manager.CustomMessagingManager.UnregisterNamedMessageHandler(RequestStateMessage);
        manager.OnClientConnectedCallback -= OnClientConnected;
        manager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("[Warboard MP] NGO client connected id=" + clientId);

        if (manager != null && manager.IsServer &&
            clientId != NetworkManager.ServerClientId)
        {
            SendCurrentState(clientId);
        }

        if (manager != null && !manager.IsServer &&
            clientId == manager.LocalClientId)
        {
            nextInitialRequestTime = 0f;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("[Warboard MP] NGO client disconnected id=" + clientId);

        List<string> stale = incoming.Keys
            .Where(k => k.StartsWith(clientId + "|", StringComparison.Ordinal))
            .ToList();

        foreach (string key in stale)
            incoming.Remove(key);

        if (manager != null && !manager.IsServer)
        {
            canonicalHash = "";
            submittedHash = "";
            nextInitialRequestTime = 0f;
        }
    }

    private void HostPump()
    {
        WarboardMatchSnapshot snapshot =
            game.CaptureMultiplayerSnapshot(currentRevision);

        string hash = SnapshotHash(snapshot);

        if (hash != canonicalHash)
        {
            currentRevision++;
            snapshot.revision = currentRevision;
            canonicalHash = hash;
            submittedHash = "";

            Debug.Log("[Warboard MP] Host state -> revision " + currentRevision);

            BroadcastSnapshot(snapshot);
            nextSafetyBroadcastTime = Time.unscaledTime + 2f;
            return;
        }

        // Safety resend in case the initial connection callback was missed.
        if (Time.unscaledTime >= nextSafetyBroadcastTime &&
            manager.ConnectedClientsIds.Any(
                id => id != NetworkManager.ServerClientId))
        {
            snapshot.revision = currentRevision;
            BroadcastSnapshot(snapshot);
            nextSafetyBroadcastTime = Time.unscaledTime + 2f;
        }
    }

    private void ClientPump()
    {
        if (!manager.IsConnectedClient ||
            !HasCanonicalState ||
            Time.unscaledTime < suppressClientSubmissionUntil)
            return;

        WarboardMatchSnapshot snapshot =
            game.CaptureMultiplayerSnapshot(currentRevision);

        string hash = SnapshotHash(snapshot);

        if (hash == canonicalHash || hash == submittedHash)
            return;

        submittedHash = hash;

        Debug.Log("[Warboard MP] Client proposing revision " + currentRevision);

        SendChunked(
            ProposalMessage,
            NetworkManager.ServerClientId,
            JsonUtility.ToJson(snapshot));
    }

    private void RequestCanonicalState()
    {
        if (manager == null || manager.IsServer ||
            !manager.IsConnectedClient ||
            manager.CustomMessagingManager == null)
            return;

        using (FastBufferWriter writer =
               new FastBufferWriter(8, Allocator.Temp))
        {
            writer.WriteValueSafe((byte)1);

            manager.CustomMessagingManager.SendNamedMessage(
                RequestStateMessage,
                NetworkManager.ServerClientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        stateRequestsSent++;
        Debug.Log("[Warboard MP] Client requested canonical state #" +
                  stateRequestsSent);
    }

    private void ReceiveStateRequest(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer)
            return;

        Debug.Log("[Warboard MP] Host received state request from " + senderId);
        SendCurrentState(senderId);
    }

    private void ReceiveStateChunk(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null || manager.IsServer ||
            senderId != NetworkManager.ServerClientId)
            return;

        ReceiveChunk(
            StateMessage,
            senderId,
            reader,
            ApplyCanonicalJson);
    }

    private void ReceiveProposalChunk(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer ||
            senderId == NetworkManager.ServerClientId)
            return;

        ReceiveChunk(
            ProposalMessage,
            senderId,
            reader,
            json => ApplyClientProposalJson(senderId, json));
    }

    private void ReceiveChunk(
        string messageName,
        ulong senderId,
        FastBufferReader reader,
        Action<string> completed)
    {
        string transferId;
        int index;
        int total;
        string payload;

        reader.ReadValueSafe(out transferId);
        reader.ReadValueSafe(out index);
        reader.ReadValueSafe(out total);
        reader.ReadValueSafe(out payload);

        if (string.IsNullOrWhiteSpace(transferId) ||
            total <= 0 || index < 0 || index >= total)
            return;

        string key =
            senderId + "|" + messageName + "|" + transferId;

        IncomingTransfer transfer;
        if (!incoming.TryGetValue(key, out transfer) ||
            transfer.Chunks == null ||
            transfer.Chunks.Length != total)
        {
            transfer = new IncomingTransfer
            {
                Chunks = new string[total]
            };
            incoming[key] = transfer;
        }

        if (transfer.Chunks[index] == null)
        {
            transfer.Chunks[index] = payload ?? "";
            transfer.Received++;
        }

        if (transfer.Received != total)
            return;

        incoming.Remove(key);
        completed(string.Concat(transfer.Chunks));
    }

    private void ApplyCanonicalJson(string json)
    {
        if (game == null)
            ResolveRuntime();

        if (game == null)
        {
            Debug.LogWarning(
                "[Warboard MP] Snapshot arrived before GameController existed.");
            canonicalHash = "";
            nextInitialRequestTime = 0f;
            return;
        }

        WarboardMatchSnapshot snapshot =
            JsonUtility.FromJson<WarboardMatchSnapshot>(json);

        if (snapshot == null)
        {
            Debug.LogError("[Warboard MP] Snapshot JSON failed to deserialize.");
            return;
        }

        currentRevision = Mathf.Max(currentRevision, snapshot.revision);

        game.ApplyMultiplayerSnapshot(snapshot);

        canonicalHash = SnapshotHash(snapshot);
        submittedHash = "";
        suppressClientSubmissionUntil = Time.unscaledTime + 0.55f;
        stateMessagesReceived++;

        Debug.Log("[Warboard MP] Client applied canonical revision " +
                  currentRevision + " bytes=" + json.Length);
    }

    private void ApplyClientProposalJson(
        ulong senderId,
        string json)
    {
        if (game == null)
            ResolveRuntime();

        if (game == null || manager == null || !manager.IsServer)
            return;

        WarboardMatchSnapshot proposal =
            JsonUtility.FromJson<WarboardMatchSnapshot>(json);

        if (proposal == null)
            return;

        proposalsReceived++;

        if (proposal.revision != currentRevision)
        {
            Debug.Log("[Warboard MP] Stale proposal clientRev=" +
                      proposal.revision + " hostRev=" + currentRevision);
            SendCurrentState(senderId);
            return;
        }

        game.ApplyMultiplayerSnapshot(proposal);
        currentRevision++;

        WarboardMatchSnapshot canonical =
            game.CaptureMultiplayerSnapshot(currentRevision);

        canonical.revision = currentRevision;
        canonicalHash = SnapshotHash(canonical);
        submittedHash = "";

        Debug.Log("[Warboard MP] Host accepted proposal -> revision " +
                  currentRevision);

        BroadcastSnapshot(canonical);
    }

    private void SendCurrentState(ulong clientId)
    {
        if (game == null)
            ResolveRuntime();

        if (game == null || manager == null || !manager.IsServer)
            return;

        WarboardMatchSnapshot snapshot =
            game.CaptureMultiplayerSnapshot(currentRevision);

        snapshot.revision = currentRevision;
        canonicalHash = SnapshotHash(snapshot);

        string json = JsonUtility.ToJson(snapshot);

        SendChunked(StateMessage, clientId, json);

        Debug.Log("[Warboard MP] Host sent canonical rev=" +
                  currentRevision + " client=" + clientId +
                  " bytes=" + json.Length);
    }

    private void BroadcastSnapshot(WarboardMatchSnapshot snapshot)
    {
        if (manager == null || !manager.IsServer)
            return;

        string json = JsonUtility.ToJson(snapshot);

        foreach (ulong clientId in manager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
                continue;

            SendChunked(StateMessage, clientId, json);
        }
    }

    private void SendChunked(
        string messageName,
        ulong clientId,
        string json)
    {
        if (manager == null ||
            manager.CustomMessagingManager == null)
            return;

        json = json ?? "";
        string transferId = Guid.NewGuid().ToString("N");

        int total = Mathf.Max(
            1,
            Mathf.CeilToInt(
                json.Length / (float)ChunkCharacters));

        for (int index = 0; index < total; index++)
        {
            int start = index * ChunkCharacters;
            int length = Mathf.Min(
                ChunkCharacters,
                json.Length - start);

            string chunk = length > 0
                ? json.Substring(start, length)
                : "";

            int capacity = Mathf.Max(
                16384,
                chunk.Length * 4 + 1024);

            using (FastBufferWriter writer =
                   new FastBufferWriter(
                       capacity,
                       Allocator.Temp))
            {
                writer.WriteValueSafe(transferId);
                writer.WriteValueSafe(index);
                writer.WriteValueSafe(total);
                writer.WriteValueSafe(chunk);

                manager.CustomMessagingManager.SendNamedMessage(
                    messageName,
                    clientId,
                    writer,
                    NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        if (messageName == StateMessage)
            stateMessagesSent++;
    }

    private string SnapshotHash(WarboardMatchSnapshot snapshot)
    {
        if (snapshot == null)
            return "";

        int revision = snapshot.revision;
        snapshot.revision = 0;
        string json = JsonUtility.ToJson(snapshot);
        snapshot.revision = revision;

        return Hash128.Compute(json).ToString();
    }
}

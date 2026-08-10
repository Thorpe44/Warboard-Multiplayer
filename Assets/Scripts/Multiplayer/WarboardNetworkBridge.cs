using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class WarboardNetworkBridge : MonoBehaviour
{
    private const string StateMessage =
        "WB_STATE_CHUNK";

    private const string ProposalMessage =
        "WB_PROPOSAL_CHUNK";

    private const string RequestStateMessage =
        "WB_REQUEST_STATE";

    private const int ChunkCharacters = 6000;

    public static WarboardNetworkBridge Instance
    {
        get;
        private set;
    }

    public int CurrentRevision
    {
        get { return currentRevision; }
    }

    public bool HasCanonicalState
    {
        get
        {
            return !string.IsNullOrWhiteSpace(
                canonicalHash
            );
        }
    }

    private class IncomingTransfer
    {
        public string[] Chunks;
        public int Received;
    }

    private readonly Dictionary<
        string,
        IncomingTransfer
    > incoming =
        new Dictionary<
            string,
            IncomingTransfer
        >();

    private NetworkManager manager;
    private GameController game;

    private bool handlersRegistered;
    private bool requestedInitialState;

    private int currentRevision;

    private string canonicalHash = "";
    private string submittedHash = "";

    private float nextSyncTime;
    private float suppressClientSubmissionUntil;

    private bool lastServerRole;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
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
            !manager.IsListening)
        {
            handlersRegistered = false;
            requestedInitialState = false;
            return;
        }

        EnsureHandlers();

        bool serverNow =
            manager.IsServer;

        if (serverNow != lastServerRole)
        {
            lastServerRole = serverNow;
            submittedHash = "";
            canonicalHash = "";
            nextSyncTime = 0f;
        }

        if (!manager.IsServer &&
            !requestedInitialState)
        {
            requestedInitialState = true;
            RequestCanonicalState();
        }

        if (Time.unscaledTime <
            nextSyncTime)
        {
            return;
        }

        nextSyncTime =
            Time.unscaledTime + 0.18f;

        if (game == null ||
            game.MultiplayerApplyingSnapshot)
        {
            return;
        }

        if (manager.IsServer)
        {
            HostPump();
        }
        else
        {
            ClientPump();
        }
    }

    public void NotifyHostChanged()
    {
        canonicalHash = "";
        submittedHash = "";
        requestedInitialState = false;
        nextSyncTime = 0f;
    }

    private void ResolveRuntime()
    {
        if (manager == null)
            manager = NetworkManager.Singleton;

        if (game == null)
        {
            game =
                FindAnyObjectByType<
                    GameController>();
        }
    }

    private void EnsureHandlers()
    {
        if (handlersRegistered ||
            manager == null ||
            manager.CustomMessagingManager ==
                null)
        {
            return;
        }

        manager.CustomMessagingManager
            .RegisterNamedMessageHandler(
                StateMessage,
                ReceiveStateChunk
            );

        manager.CustomMessagingManager
            .RegisterNamedMessageHandler(
                ProposalMessage,
                ReceiveProposalChunk
            );

        manager.CustomMessagingManager
            .RegisterNamedMessageHandler(
                RequestStateMessage,
                ReceiveStateRequest
            );

        manager.OnClientConnectedCallback +=
            OnClientConnected;

        manager.OnClientDisconnectCallback +=
            OnClientDisconnected;

        handlersRegistered = true;
    }

    private void OnDestroy()
    {
        if (manager == null ||
            !handlersRegistered ||
            manager.CustomMessagingManager ==
                null)
        {
            return;
        }

        manager.CustomMessagingManager
            .UnregisterNamedMessageHandler(
                StateMessage
            );

        manager.CustomMessagingManager
            .UnregisterNamedMessageHandler(
                ProposalMessage
            );

        manager.CustomMessagingManager
            .UnregisterNamedMessageHandler(
                RequestStateMessage
            );

        manager.OnClientConnectedCallback -=
            OnClientConnected;

        manager.OnClientDisconnectCallback -=
            OnClientDisconnected;
    }

    private void OnClientConnected(
        ulong clientId)
    {
        if (manager != null &&
            manager.IsServer &&
            clientId != NetworkManager.ServerClientId)
        {
            SendCurrentState(clientId);
        }
    }

    private void OnClientDisconnected(
        ulong clientId)
    {
        List<string> stale =
            incoming.Keys
                .Where(
                    key =>
                        key.StartsWith(
                            clientId + "|",
                            StringComparison.Ordinal
                        )
                )
                .ToList();

        foreach (string key in stale)
            incoming.Remove(key);
    }

    private void HostPump()
    {
        WarboardMatchSnapshot snapshot =
            game.CaptureMultiplayerSnapshot(
                currentRevision
            );

        string hash =
            SnapshotHash(snapshot);

        if (hash == canonicalHash)
            return;

        currentRevision++;

        snapshot.revision =
            currentRevision;

        canonicalHash = hash;
        submittedHash = "";

        BroadcastSnapshot(snapshot);
    }

    private void ClientPump()
    {
        if (!HasCanonicalState ||
            Time.unscaledTime <
                suppressClientSubmissionUntil)
        {
            return;
        }

        WarboardMatchSnapshot snapshot =
            game.CaptureMultiplayerSnapshot(
                currentRevision
            );

        string hash =
            SnapshotHash(snapshot);

        if (hash == canonicalHash ||
            hash == submittedHash)
        {
            return;
        }

        submittedHash = hash;

        string json =
            JsonUtility.ToJson(snapshot);

        SendChunked(
            ProposalMessage,
            NetworkManager.ServerClientId,
            json
        );
    }

    private void ReceiveStateRequest(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null ||
            !manager.IsServer)
        {
            return;
        }

        SendCurrentState(senderId);
    }

    private void RequestCanonicalState()
    {
        if (manager == null ||
            manager.IsServer ||
            manager.CustomMessagingManager ==
                null)
        {
            return;
        }

        using (
            FastBufferWriter writer =
                new FastBufferWriter(
                    8,
                    Allocator.Temp
                )
        )
        {
            writer.WriteValueSafe(
                (byte)1
            );

            manager.CustomMessagingManager
                .SendNamedMessage(
                    RequestStateMessage,
                    NetworkManager.ServerClientId,
                    writer
                );
        }
    }

    private void ReceiveStateChunk(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null ||
            manager.IsServer ||
            senderId != NetworkManager.ServerClientId)
        {
            return;
        }

        ReceiveChunk(
            StateMessage,
            senderId,
            reader,
            ApplyCanonicalJson
        );
    }

    private void ReceiveProposalChunk(
        ulong senderId,
        FastBufferReader reader)
    {
        if (manager == null ||
            !manager.IsServer ||
            senderId == NetworkManager.ServerClientId)
        {
            return;
        }

        ReceiveChunk(
            ProposalMessage,
            senderId,
            reader,
            json =>
                ApplyClientProposalJson(
                    senderId,
                    json
                )
        );
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

        reader.ReadValueSafe(
            out transferId
        );

        reader.ReadValueSafe(
            out index
        );

        reader.ReadValueSafe(
            out total
        );

        reader.ReadValueSafe(
            out payload
        );

        if (string.IsNullOrWhiteSpace(
                transferId) ||
            total <= 0 ||
            index < 0 ||
            index >= total)
        {
            return;
        }

        string key =
            senderId +
            "|" +
            messageName +
            "|" +
            transferId;

        IncomingTransfer transfer;

        if (!incoming.TryGetValue(
                key,
                out transfer) ||
            transfer.Chunks == null ||
            transfer.Chunks.Length != total)
        {
            transfer =
                new IncomingTransfer
                {
                    Chunks =
                        new string[total]
                };

            incoming[key] =
                transfer;
        }

        if (transfer.Chunks[index] == null)
        {
            transfer.Chunks[index] =
                payload ?? "";

            transfer.Received++;
        }

        if (transfer.Received != total)
            return;

        incoming.Remove(key);

        completed(
            string.Concat(
                transfer.Chunks
            )
        );
    }

    private void ApplyCanonicalJson(
        string json)
    {
        if (game == null)
            ResolveRuntime();

        if (game == null)
            return;

        WarboardMatchSnapshot snapshot =
            JsonUtility.FromJson<
                WarboardMatchSnapshot>(
                json
            );

        if (snapshot == null)
            return;

        currentRevision =
            Mathf.Max(
                currentRevision,
                snapshot.revision
            );

        game.ApplyMultiplayerSnapshot(
            snapshot
        );

        canonicalHash =
            SnapshotHash(snapshot);

        submittedHash = "";

        suppressClientSubmissionUntil =
            Time.unscaledTime + 0.45f;
    }

    private void ApplyClientProposalJson(
        ulong senderId,
        string json)
    {
        if (game == null)
            ResolveRuntime();

        if (game == null ||
            manager == null ||
            !manager.IsServer)
        {
            return;
        }

        WarboardMatchSnapshot proposal =
            JsonUtility.FromJson<
                WarboardMatchSnapshot>(
                json
            );

        if (proposal == null)
            return;

        // Optimistic concurrency:
        // a client may only modify the state version it most recently saw.
        if (proposal.revision !=
            currentRevision)
        {
            SendCurrentState(senderId);
            return;
        }

        game.ApplyMultiplayerSnapshot(
            proposal
        );

        currentRevision++;

        WarboardMatchSnapshot canonical =
            game.CaptureMultiplayerSnapshot(
                currentRevision
            );

        canonical.revision =
            currentRevision;

        canonicalHash =
            SnapshotHash(canonical);

        submittedHash = "";

        BroadcastSnapshot(canonical);
    }

    private void SendCurrentState(
        ulong clientId)
    {
        if (game == null)
            ResolveRuntime();

        if (game == null ||
            manager == null ||
            !manager.IsServer)
        {
            return;
        }

        WarboardMatchSnapshot snapshot =
            game.CaptureMultiplayerSnapshot(
                currentRevision
            );

        snapshot.revision =
            currentRevision;

        canonicalHash =
            SnapshotHash(snapshot);

        string json =
            JsonUtility.ToJson(snapshot);

        SendChunked(
            StateMessage,
            clientId,
            json
        );
    }

    private void BroadcastSnapshot(
        WarboardMatchSnapshot snapshot)
    {
        if (manager == null ||
            !manager.IsServer)
        {
            return;
        }

        string json =
            JsonUtility.ToJson(snapshot);

        foreach (
            ulong clientId
            in manager.ConnectedClientsIds)
        {
            if (clientId ==
                NetworkManager.ServerClientId)
            {
                continue;
            }

            SendChunked(
                StateMessage,
                clientId,
                json
            );
        }
    }

    private void SendChunked(
        string messageName,
        ulong clientId,
        string json)
    {
        if (manager == null ||
            manager.CustomMessagingManager ==
                null)
        {
            return;
        }

        json = json ?? "";

        string transferId =
            Guid.NewGuid()
                .ToString("N");

        int total =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    json.Length /
                    (float)ChunkCharacters
                )
            );

        for (int index = 0;
             index < total;
             index++)
        {
            int start =
                index *
                ChunkCharacters;

            int length =
                Mathf.Min(
                    ChunkCharacters,
                    json.Length - start
                );

            string chunk =
                length > 0
                ? json.Substring(
                    start,
                    length
                  )
                : "";

            int capacity =
                Mathf.Max(
                    16384,
                    chunk.Length * 4 +
                    512
                );

            using (
                FastBufferWriter writer =
                    new FastBufferWriter(
                        capacity,
                        Allocator.Temp
                    )
            )
            {
                writer.WriteValueSafe(
                    transferId
                );

                writer.WriteValueSafe(
                    index
                );

                writer.WriteValueSafe(
                    total
                );

                writer.WriteValueSafe(
                    chunk
                );

                manager.CustomMessagingManager
                    .SendNamedMessage(
                        messageName,
                        clientId,
                        writer
                    );
            }
        }
    }

    private string SnapshotHash(
        WarboardMatchSnapshot snapshot)
    {
        if (snapshot == null)
            return "";

        int revision =
            snapshot.revision;

        snapshot.revision = 0;

        string json =
            JsonUtility.ToJson(snapshot);

        snapshot.revision =
            revision;

        return
            Hash128.Compute(json)
                .ToString();
    }
}

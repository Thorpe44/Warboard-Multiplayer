using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class TraditionalDiceTray3D
{
    private bool multiplayerDiceNetworkBypass;
    private int multiplayerLastAppliedSequence = -1;

    public bool MultiplayerDiceNetworkBypass
    {
        get { return multiplayerDiceNetworkBypass; }
    }

    public bool MultiplayerDiceRolling
    {
        get { return rollInProgress; }
    }

    public WarboardDicePoolEntry[]
        MultiplayerCaptureDicePool()
    {
        EnsurePoolInitialized();

        List<WarboardDicePoolEntry> result =
            new List<WarboardDicePoolEntry>();

        foreach (int sides in SupportedSides)
        {
            result.Add(
                new WarboardDicePoolEntry
                {
                    sides = sides,
                    count = requestedPool[sides]
                });
        }

        return result.ToArray();
    }

    public int[] MultiplayerSelectedDiceIds()
    {
        return dice
            .Where(
                value =>
                    value != null &&
                    value.Selected)
            .Select(value => value.Id)
            .ToArray();
    }

    public WarboardDiceSnapshot
        CaptureMultiplayerDiceSnapshot(
            int sequence)
    {
        EnsurePoolInitialized();

        WarboardDiceSnapshot state =
            new WarboardDiceSnapshot
            {
                sequence = sequence,
                rolling = rollInProgress,
                settledText = settledText ?? "",
                pool = MultiplayerCaptureDicePool()
            };

        // Do not network transient Rigidbody transforms. The host is the
        // physical authority. While rolling, clients run a decorative local
        // animation. Once the host settles, its exact final transforms are
        // distributed and become canonical.
        if (rollInProgress)
        {
            state.dice =
                new WarboardDieSnapshot[0];
            return state;
        }

        state.dice =
            dice
                .Where(value => value != null)
                .OrderBy(value => value.Id)
                .Select(
                    value =>
                        new WarboardDieSnapshot
                        {
                            id = value.Id,
                            sides = value.Sides,
                            value = value.TopValue(),
                            selected = value.Selected,
                            position =
                                value.transform.position,
                            rotation =
                                value.transform.rotation
                        })
                .ToArray();

        return state;
    }

    public void MultiplayerAuthoritativeRoll(
        WarboardDicePoolEntry[] pool)
    {
        MultiplayerApplyPool(pool);

        multiplayerDiceNetworkBypass = true;

        try
        {
            RollAll();
        }
        finally
        {
            multiplayerDiceNetworkBypass = false;
        }
    }

    public void MultiplayerAuthoritativeReroll(
        int[] selectedIds)
    {
        HashSet<int> selected =
            new HashSet<int>(
                selectedIds ??
                new int[0]);

        foreach (TraditionalDiceMarker die in dice)
        {
            if (die != null)
            {
                die.SetSelected(
                    selected.Contains(die.Id));
            }
        }

        multiplayerDiceNetworkBypass = true;

        try
        {
            RerollSelected();
        }
        finally
        {
            multiplayerDiceNetworkBypass = false;
        }
    }

    public void MultiplayerAuthoritativeClear()
    {
        multiplayerDiceNetworkBypass = true;

        try
        {
            ClearDice();
        }
        finally
        {
            multiplayerDiceNetworkBypass = false;
        }
    }

    public void MultiplayerAuthoritativeAdjustPool(
        int sides,
        int delta)
    {
        EnsurePoolInitialized();

        if (!requestedPool.ContainsKey(sides))
            return;

        int current = requestedPool[sides];

        if (delta > 0)
        {
            int room =
                MaxDice -
                RequestedPoolTotal();

            delta =
                Mathf.Min(delta, room);
        }

        requestedPool[sides] =
            Mathf.Clamp(
                current + delta,
                0,
                MaxDice);
    }

    public void MultiplayerAuthoritativeSelect(
        int dieId,
        bool selected)
    {
        TraditionalDiceMarker die =
            dice.FirstOrDefault(
                value =>
                    value != null &&
                    value.Id == dieId);

        if (die != null)
            die.SetSelected(selected);
    }

    public void SetDieSelectedShared(
        TraditionalDiceMarker marker,
        bool selected)
    {
        if (marker == null)
            return;

        bool intercepted =
            WarboardDiceNetworkBridge
                .TryInterceptSelection(
                    this,
                    marker.Id,
                    selected);

        marker.SetSelected(selected);

        // Host/single-player changes are picked up by the host state poll.
        // Client changes are echoed back by the authoritative host.
        if (intercepted)
            return;
    }

    public void ApplyMultiplayerDiceSnapshot(
        WarboardDiceSnapshot state)
    {
        if (state == null)
            return;

        MultiplayerApplyPool(state.pool);

        if (state.rolling)
        {
            if (state.sequence !=
                multiplayerLastAppliedSequence)
            {
                multiplayerLastAppliedSequence =
                    state.sequence;

                multiplayerDiceNetworkBypass = true;

                try
                {
                    RollAll();

                    // This is only a visual copy of the host roll. Do not
                    // create a duplicate battle-log entry locally.
                    rollLogged = true;
                }
                finally
                {
                    multiplayerDiceNetworkBypass = false;
                }
            }

            settledText =
                string.IsNullOrWhiteSpace(
                    state.settledText)
                ? "Opponent roll in progress..."
                : state.settledText;

            return;
        }

        multiplayerLastAppliedSequence =
            Mathf.Max(
                multiplayerLastAppliedSequence,
                state.sequence);

        WarboardDieSnapshot[] remote =
            state.dice ??
            new WarboardDieSnapshot[0];

        bool canApplyInPlace =
            dice.Count == remote.Length;

        if (canApplyInPlace)
        {
            Dictionary<int, TraditionalDiceMarker> localById =
                dice
                    .Where(value => value != null)
                    .ToDictionary(
                        value => value.Id,
                        value => value);

            foreach (WarboardDieSnapshot dieState in remote)
            {
                TraditionalDiceMarker local;

                if (!localById.TryGetValue(
                        dieState.id,
                        out local) ||
                    local.Sides != dieState.sides)
                {
                    canApplyInPlace = false;
                    break;
                }
            }
        }

        multiplayerDiceNetworkBypass = true;

        try
        {
            if (!canApplyInPlace)
            {
                ClearDice();

                nextDieId = 1;

                for (int i = 0;
                     i < remote.Length;
                     i++)
                {
                    WarboardDieSnapshot dieState =
                        remote[i];

                    SpawnDie(
                        dieState.sides,
                        i);

                    TraditionalDiceMarker local =
                        dice[dice.Count - 1];

                    local.Id = dieState.id;
                }
            }

            Dictionary<int, TraditionalDiceMarker> byId =
                dice
                    .Where(value => value != null)
                    .ToDictionary(
                        value => value.Id,
                        value => value);

            int maxId = 0;

            foreach (WarboardDieSnapshot dieState in remote)
            {
                TraditionalDiceMarker local;

                if (!byId.TryGetValue(
                        dieState.id,
                        out local))
                {
                    continue;
                }

                maxId =
                    Mathf.Max(
                        maxId,
                        dieState.id);

                local.transform.position =
                    dieState.position;

                local.transform.rotation =
                    dieState.rotation;

                local.SetSelected(
                    dieState.selected);

                if (local.Body != null)
                {
                    local.Body.linearVelocity =
                        Vector3.zero;

                    local.Body.angularVelocity =
                        Vector3.zero;

                    local.Body.isKinematic = true;
                    local.Body.Sleep();
                }
            }

            nextDieId =
                Mathf.Max(
                    nextDieId,
                    maxId + 1);

            rollInProgress = false;
            rollLogged = true;
            settledSince = -1f;
            settledText =
                state.settledText ?? "";

            if (remote.Length == 0 &&
                string.IsNullOrWhiteSpace(
                    settledText))
            {
                settledText = "Tray empty";
            }
        }
        finally
        {
            multiplayerDiceNetworkBypass = false;
        }
    }

    private void MultiplayerApplyPool(
        WarboardDicePoolEntry[] pool)
    {
        EnsurePoolInitialized();

        foreach (int sides in SupportedSides)
            requestedPool[sides] = 0;

        if (pool == null)
            return;

        foreach (WarboardDicePoolEntry entry in pool)
        {
            if (entry == null ||
                !requestedPool.ContainsKey(
                    entry.sides))
            {
                continue;
            }

            requestedPool[entry.sides] =
                Mathf.Clamp(
                    entry.count,
                    0,
                    MaxDice);
        }
    }
}

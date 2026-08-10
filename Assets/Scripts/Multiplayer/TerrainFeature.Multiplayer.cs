public partial class TerrainFeature
{
    public WarboardTerrainSnapshot
        CaptureMultiplayerTerrainSnapshot()
    {
        return
            new WarboardTerrainSnapshot
            {
                missionTerrainId =
                    MissionTerrainId ?? "",
                position =
                    transform.position,
                operationMarkerOwner =
                    OperationMarkerOwner ?? "",
                trappedByFaction =
                    TrappedByFaction ?? "",
                trappedRound =
                    TrappedRound
            };
    }

    public void ApplyMultiplayerTerrainSnapshot(
        WarboardTerrainSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        OperationMarkerOwner =
            snapshot.operationMarkerOwner ??
            "";

        TrappedByFaction =
            snapshot.trappedByFaction ??
            "";

        TrappedRound =
            snapshot.trappedRound;
    }
}

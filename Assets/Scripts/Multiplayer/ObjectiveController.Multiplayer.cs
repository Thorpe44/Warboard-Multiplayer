using System.Linq;

public partial class ObjectiveController
{
    public WarboardObjectiveSnapshot
        CaptureMultiplayerObjectiveSnapshot(
            int index)
    {
        return
            new WarboardObjectiveSnapshot
            {
                index = index,
                securedByFaction =
                    securedByFaction ?? "",
                missionStates =
                    missionStates.ToArray()
            };
    }

    public void ApplyMultiplayerObjectiveSnapshot(
        WarboardObjectiveSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        securedByFaction =
            string.IsNullOrWhiteSpace(
                snapshot.securedByFaction)
            ? null
            : snapshot.securedByFaction;

        missionStates.Clear();

        if (snapshot.missionStates !=
            null)
        {
            foreach (
                string state
                in snapshot.missionStates)
            {
                if (!string.IsNullOrWhiteSpace(
                        state))
                {
                    missionStates.Add(
                        state
                    );
                }
            }
        }
    }
}

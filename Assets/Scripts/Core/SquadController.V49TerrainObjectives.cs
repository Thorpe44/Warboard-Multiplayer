using UnityEngine;

public partial class SquadController
{
    public int TotalObjectiveControlWithinTerrain(
        TerrainFeature terrain)
    {
        if (terrain == null)
            return 0;

        SquadController actionUnit =
            JoinedActionController();

        int total = 0;

        foreach (ModelToken model
            in actionUnit.JoinedLivingModelTokens())
        {
            if (model == null ||
                !model.IsAlive ||
                !terrain.ModelTouchesObjectiveArea(
                    model))
            {
                continue;
            }

            total +=
                Mathf.Max(
                    0,
                    actionUnit
                        .EffectiveObjectiveControl(
                            model
                        )
                );
        }

        return total;
    }
}

using UnityEngine;

public class ObjectiveTerrainLink49 : MonoBehaviour
{
    public ObjectiveController Objective
    {
        get;
        private set;
    }

    public void Initialize(
        ObjectiveController objective)
    {
        Objective = objective;
    }
}

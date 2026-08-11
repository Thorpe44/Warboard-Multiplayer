using System.Collections.Generic;
using UnityEngine;

// WARBOARD_NECRON_VISUAL_ORIGIN_REPAIR_R2_3
// Some TTS-derived OBJ files contain mesh vertices offset from the imported
// GameObject origin. R2.2 corrected component transforms, but that cannot fix
// geometry baked into the OBJ itself. This repairs only obviously displaced
// Necron visuals after ModelToken has instantiated them.

public static class NecronVisualOriginRepairR23
{
    public static void Reanchor(
        string factionId,
        ModelToken token)
    {
        if (token == null || !IsNecronFaction(factionId))
            return;

        Transform visualRoot =
            token.transform.Find("Visual Model");

        if (visualRoot == null)
            return;

        List<Transform> miniatureRoots =
            new List<Transform>();

        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);

            if (child != null &&
                child.name.StartsWith("Miniature Visual"))
            {
                miniatureRoots.Add(child);
            }
        }

        if (miniatureRoots.Count == 0)
            return;

        bool haveBounds = false;
        Bounds combined = new Bounds();

        foreach (Transform miniature in miniatureRoots)
        {
            Renderer[] renderers =
                miniature.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!haveBounds)
                {
                    combined = renderer.bounds;
                    haveBounds = true;
                }
                else
                {
                    combined.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!haveBounds)
            return;

        Vector3 rootWorld = visualRoot.position;

        Vector3 deltaWorld =
            new Vector3(
                rootWorld.x - combined.center.x,
                rootWorld.y - combined.min.y,
                rootWorld.z - combined.center.z
            );

        float horizontal =
            new Vector2(
                deltaWorld.x,
                deltaWorld.z
            ).magnitude;

        float vertical =
            Mathf.Abs(deltaWorld.y);

        // Do not "improve" properly authored miniatures. Only intervene when
        // the imported geometry is clearly detached from its token/base.
        if (horizontal < 1.0f && vertical < 1.0f)
            return;

        Vector3 deltaLocal =
            visualRoot.InverseTransformVector(deltaWorld);

        foreach (Transform miniature in miniatureRoots)
        {
            miniature.localPosition += deltaLocal;
        }

        Debug.Log(
            "Warboard R2.3: repaired displaced Necron OBJ origin for " +
            token.gameObject.name +
            " (offset " +
            deltaWorld.ToString("F2") +
            ").");
    }

    private static bool IsNecronFaction(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string lower = value.ToLowerInvariant();

        return lower.Contains("necron");
    }
}

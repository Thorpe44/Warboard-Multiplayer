using UnityEngine;

// WARBOARD_UI_READABILITY_R2_7
//
// R27 removes R26's separate player-summary Canvas overlay.
// The real top bar is fixed directly in GameController.UI instead.
// This component now has one job only: keep the physical world-board
// mission-card / scoreboard typography readable.

[DefaultExecutionOrder(32010)]
public sealed class WarboardUiReadabilityR26 :
    MonoBehaviour
{
    private float nextScanRefresh;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType
            .AfterSceneLoad)]
    private static void Install()
    {
        if (UnityEngine.Object
            .FindAnyObjectByType<
                WarboardUiReadabilityR26>() !=
            null)
        {
            return;
        }

        GameObject root =
            new GameObject(
                "Warboard UI Readability R2.7"
            );

        UnityEngine.Object
            .DontDestroyOnLoad(
                root
            );

        root.AddComponent<
            WarboardUiReadabilityR26
        >();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime <
            nextScanRefresh)
        {
            return;
        }

        nextScanRefresh =
            Time.unscaledTime +
            0.50f;

        ImproveWorldTextMeshes();
    }

    private void ImproveWorldTextMeshes()
    {
        TextMesh[] meshes =
            Resources
                .FindObjectsOfTypeAll<
                    TextMesh
                >();

        foreach (TextMesh mesh
            in meshes)
        {
            if (mesh == null ||
                mesh.gameObject == null ||
                !mesh.gameObject.scene
                    .IsValid())
            {
                continue;
            }

            string name =
                mesh.gameObject.name ??
                "";

            string text =
                mesh.text ??
                "";

            string parentName =
                mesh.transform.parent !=
                    null
                ? mesh.transform.parent.name
                : "";

            if (name.Contains(
                    "Primary Card Text") ||
                name.Contains(
                    "Secondary Card Text"))
            {
                mesh.fontSize =
                    Mathf.Max(
                        mesh.fontSize,
                        40
                    );

                if (mesh.characterSize <
                    0.032f)
                {
                    mesh.characterSize =
                        0.032f;
                }

                mesh.lineSpacing =
                    0.88f;

                continue;
            }

            if (name.Contains(
                    "Primary Card Type") ||
                name.Contains(
                    "Secondary Card Type"))
            {
                mesh.fontSize =
                    Mathf.Max(
                        mesh.fontSize,
                        36
                    );

                if (mesh.characterSize <
                    0.033f)
                {
                    mesh.characterSize =
                        0.033f;
                }

                continue;
            }

            if (text.Contains(
                    "MATCH SCOREBOARD") ||
                parentName.Contains(
                    "World Scoreboard") ||
                name.Contains(
                    "Scoreboard"))
            {
                mesh.fontSize =
                    Mathf.Max(
                        mesh.fontSize,
                        54
                    );

                if (mesh.characterSize <
                    0.068f)
                {
                    mesh.characterSize =
                        0.068f;
                }

                mesh.lineSpacing =
                    0.90f;

                mesh.anchor =
                    TextAnchor.UpperCenter;

                mesh.alignment =
                    TextAlignment.Center;
            }
        }
    }
}

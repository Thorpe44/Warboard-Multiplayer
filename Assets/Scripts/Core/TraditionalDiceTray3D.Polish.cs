using System.Linq;
using UnityEngine;

public partial class TraditionalDiceTray3D
{
    private const float PolishedDiceScale = 1.28f;

    private GUIStyle diceResultTitleStyle;
    private GUIStyle diceResultValueStyle;
    private Texture2D diceResultBackground;

    private Vector3 DiceWorldScaleCompensated()
    {
        if (trayRoot == null)
            return Vector3.one * PolishedDiceScale;

        Vector3 scale =
            trayRoot.transform.lossyScale;

        return new Vector3(
            Mathf.Abs(scale.x) > 0.0001f
                ? PolishedDiceScale / scale.x
                : PolishedDiceScale,
            Mathf.Abs(scale.y) > 0.0001f
                ? PolishedDiceScale / scale.y
                : PolishedDiceScale,
            Mathf.Abs(scale.z) > 0.0001f
                ? PolishedDiceScale / scale.z
                : PolishedDiceScale
        );
    }

    private void ApplyDiceTrayPolish()
    {
        if (trayRoot == null)
            return;

        ResizeTrayPart(
            "Floor",
            Vector3.zero + new Vector3(0f, -0.35f, 0f),
            new Vector3(14.6f, 0.5f, 8.9f)
        );

        ResizeTrayPart(
            "Left Wall",
            new Vector3(-7.38f, 0.70f, 0f),
            new Vector3(0.44f, 2.45f, 9.3f)
        );

        ResizeTrayPart(
            "Right Wall",
            new Vector3(7.38f, 0.70f, 0f),
            new Vector3(0.44f, 2.45f, 9.3f)
        );

        ResizeTrayPart(
            "Near Wall",
            new Vector3(0f, 0.70f, -4.52f),
            new Vector3(14.9f, 2.45f, 0.44f)
        );

        ResizeTrayPart(
            "Far Wall",
            new Vector3(0f, 0.70f, 4.52f),
            new Vector3(14.9f, 2.45f, 0.44f)
        );

        // Tall invisible catch walls. The visible walls stay low enough to
        // look like a dice tray, while these colliders stop high-energy dice
        // from escaping over them.
        EnsureInvisibleBarrier(
            "Dice Catch Left",
            new Vector3(-7.55f, 4.0f, 0f),
            new Vector3(0.35f, 8.5f, 9.5f)
        );

        EnsureInvisibleBarrier(
            "Dice Catch Right",
            new Vector3(7.55f, 4.0f, 0f),
            new Vector3(0.35f, 8.5f, 9.5f)
        );

        EnsureInvisibleBarrier(
            "Dice Catch Near",
            new Vector3(0f, 4.0f, -4.70f),
            new Vector3(15.3f, 8.5f, 0.35f)
        );

        EnsureInvisibleBarrier(
            "Dice Catch Far",
            new Vector3(0f, 4.0f, 4.70f),
            new Vector3(15.3f, 8.5f, 0.35f)
        );

        // Invisible ceiling catches the rare die launched almost vertically.
        EnsureInvisibleBarrier(
            "Dice Catch Ceiling",
            new Vector3(0f, 9.3f, 0f),
            new Vector3(15.3f, 0.35f, 9.5f)
        );
    }

    private void ResizeTrayPart(
        string name,
        Vector3 localPosition,
        Vector3 localScale)
    {
        Transform value =
            trayRoot.transform.Find(name);

        if (value == null)
            return;

        value.localPosition =
            localPosition;

        value.localScale =
            localScale;
    }

    private void EnsureInvisibleBarrier(
        string name,
        Vector3 localPosition,
        Vector3 localScale)
    {
        Transform existing =
            trayRoot.transform.Find(name);

        GameObject barrier;

        if (existing != null)
        {
            barrier =
                existing.gameObject;
        }
        else
        {
            barrier =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            barrier.name = name;

            barrier.transform.SetParent(
                trayRoot.transform,
                false
            );
        }

        barrier.transform.localPosition =
            localPosition;

        barrier.transform.localScale =
            localScale;

        SetLayerRecursive(
            barrier,
            DiceLayer
        );

        Renderer renderer =
            barrier.GetComponent<Renderer>();

        if (renderer != null)
            renderer.enabled = false;
    }

    private void ContainEscapedDice()
    {
        if (trayRoot == null ||
            dice == null ||
            dice.Count == 0)
        {
            return;
        }

        // Bounds in WORLD space after the polished tray scaling.
        const float halfWidth = 12.1f;
        const float halfDepth = 3.35f;

        foreach (TraditionalDiceMarker die
            in dice)
        {
            if (die == null)
                continue;

            Vector3 offset =
                die.transform.position -
                trayOrigin;

            bool escaped =
                Mathf.Abs(offset.x) > halfWidth ||
                Mathf.Abs(offset.z) > halfDepth ||
                offset.y < -1.4f ||
                offset.y > 10.5f;

            if (!escaped)
                continue;

            die.transform.position =
                trayOrigin +
                new Vector3(
                    Mathf.Clamp(
                        offset.x,
                        -5.8f,
                        5.8f
                    ),
                    4.5f,
                    Mathf.Clamp(
                        offset.z,
                        -2.3f,
                        2.3f
                    )
                );

            die.transform.rotation =
                Random.rotation;

            if (die.Body != null)
            {
                die.Body.isKinematic = false;

                die.Body.linearVelocity =
                    new Vector3(
                        Random.Range(-1.4f, 1.4f),
                        -0.2f,
                        Random.Range(-1.2f, 1.2f)
                    );

                die.Body.angularVelocity =
                    Random.onUnitSphere *
                    Random.Range(7f, 12f);

                die.Body.WakeUp();
            }

            settledSince = -1f;
            rollInProgress = true;
        }
    }

    private void DrawDiceResultOverlay()
    {
        if (!worldSpaceMode)
            return;

        EnsureDiceResultStyles();

        float width =
            Mathf.Min(
                620f,
                Screen.width * 0.42f
            );

        float x =
            (Screen.width - width) *
            0.5f;

        // Sits visually beneath the physical tray and above the very bottom
        // edge of the window on normal desktop aspect ratios.
        float y =
            Screen.height - 118f;

        Rect box =
            new Rect(
                x,
                y,
                width,
                54f
            );

        GUI.DrawTexture(
            box,
            diceResultBackground
        );

        GUI.Label(
            new Rect(
                box.x + 12f,
                box.y + 4f,
                86f,
                20f
            ),
            "RESULT",
            diceResultTitleStyle
        );

        string value =
            string.IsNullOrWhiteSpace(
                settledText)
            ? "Tray empty"
            : settledText;

        GUI.Label(
            new Rect(
                box.x + 12f,
                box.y + 22f,
                box.width - 24f,
                28f
            ),
            value,
            diceResultValueStyle
        );
    }

    private void EnsureDiceResultStyles()
    {
        if (diceResultTitleStyle != null)
            return;

        diceResultBackground =
            new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false
            );

        diceResultBackground.SetPixel(
            0,
            0,
            new Color(
                0.02f,
                0.025f,
                0.035f,
                0.94f
            )
        );

        diceResultBackground.Apply();

        diceResultTitleStyle =
            new GUIStyle(
                GUI.skin.label
            );

        diceResultTitleStyle.fontSize = 12;
        diceResultTitleStyle.fontStyle =
            FontStyle.Bold;
        diceResultTitleStyle.normal.textColor =
            new Color(
                0.44f,
                0.86f,
                1f,
                1f
            );

        diceResultValueStyle =
            new GUIStyle(
                GUI.skin.label
            );

        diceResultValueStyle.fontSize = 15;
        diceResultValueStyle.fontStyle =
            FontStyle.Bold;
        diceResultValueStyle.alignment =
            TextAnchor.MiddleCenter;
        diceResultValueStyle.clipping =
            TextClipping.Clip;
        diceResultValueStyle.normal.textColor =
            Color.white;
    }
}

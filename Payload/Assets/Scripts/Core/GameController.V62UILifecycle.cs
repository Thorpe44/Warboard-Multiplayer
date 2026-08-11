using System;
using UnityEngine;

// WARBOARD_V62_UI_LIFECYCLE
public partial class GameController : MonoBehaviour
{
    private string v62LastToastStatus = "";
    private float v62ToastExpiresAt = -1f;

    private bool ShouldDrawTransientStatusToast(
        string value)
    {
        string next = value ?? "";

        bool suppressed =
            string.IsNullOrWhiteSpace(next) ||
            string.Equals(
                next,
                "Ready.",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                next,
                "No squad selected.",
                StringComparison.OrdinalIgnoreCase);

        if (suppressed)
        {
            v62LastToastStatus = next;
            v62ToastExpiresAt = -1f;
            return false;
        }

        if (!string.Equals(
                v62LastToastStatus,
                next,
                StringComparison.Ordinal))
        {
            v62LastToastStatus = next;
            v62ToastExpiresAt =
                Time.unscaledTime + 4f;
        }

        if (v62ToastExpiresAt >= 0f &&
            Time.unscaledTime >
                v62ToastExpiresAt)
        {
            status = "";
            v62LastToastStatus = "";
            v62ToastExpiresAt = -1f;
            return false;
        }

        return
            v62ToastExpiresAt >= 0f;
    }
}

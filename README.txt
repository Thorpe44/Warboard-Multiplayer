WARBOARD DICE POLISH RECOVERY V2

Use this after the original DICE VISIBILITY + TRAY POLISH installer partially
ran and stopped at:

  Could not find patch target: escape recovery

The original installer already applied some changes before failing. This V2
recovery is deliberately idempotent: it checks what is already installed and
only completes what is missing.

It also includes the corrected:
  trayRoot.transform.lossyScale

so the earlier CS1061 compile error is fixed automatically.

INSTALL
-------
1. Do NOT rerun the old polish installer.
2. Extract this ZIP into the Warboard-Multiplayer project root.
3. Run:
     RECOVER_DICE_TRAY_POLISH_V2.bat
4. Wait for:
     SUCCESS - DICE POLISH RECOVERY V2 VERIFIED
5. Return to Unity and let it compile.

No scene changes or rebuilds are required just to test in the Editor.

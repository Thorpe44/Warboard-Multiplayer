WARBOARD DICE POLISH RECOVERY V3

This fixes the V2 recovery installer error:

  Cannot overwrite ...TraditionalDiceTray3D.Polish.cs with itself.

WHY
---
When this ZIP is extracted directly into the Warboard project root, the helper
file inside the ZIP lands immediately at:

  Assets\Scripts\Core\TraditionalDiceTray3D.Polish.cs

That is already its final destination. V2 incorrectly tried to Copy-Item that
file onto itself.

V3 detects that situation and simply continues.

INSTALL
-------
1. Extract this ZIP into the CURRENT Warboard-Multiplayer project root.
2. Overwrite files when asked.
3. Run:
     RECOVER_DICE_TRAY_POLISH_V3.bat
4. Wait for:
     SUCCESS - DICE POLISH RECOVERY V3 VERIFIED
5. Return to Unity and let it compile.

V3 is safe after both the partial original installer and the failed V2 attempt.

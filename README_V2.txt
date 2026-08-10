WARBOARD SHARED MULTIPLAYER DICE V2

This replaces the broken V1 installer.

V1 BUG
------
The ZIP stored the multiplayer source files under:
  Assets\Scripts\Multiplayer\

but the PowerShell installer incorrectly searched for them beside the BAT file.

V2 is safe to run after the failed V1 attempt. It detects the patches/files that
V1 already applied and completes only what is missing.

INSTALL
-------
Extract this ZIP into the Warboard-Multiplayer project root, overwriting when
asked, then run:

  INSTALL_SHARED_MULTIPLAYER_DICE_V2.bat

Wait for:

  SUCCESS - SHARED MULTIPLAYER DICE V2 VERIFIED

Then return to Unity, let it compile, and rebuild the Windows EXE.

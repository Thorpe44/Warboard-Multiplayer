WARBOARD V53 CORE RECOVERY V4

This is a recovery for the large cascade of 'GameController does not contain a
definition for...' errors that appeared after V53.

The cascade means GameController.Core.cs is no longer intact. Do NOT fix the
hundreds of individual errors.

This installer:
- Finds the latest intact GameController.Core.cs backup created automatically by
  V53_RECOVERY_V2 before that patch touched the file.
- Validates that the backup contains representative core methods.
- Restores the entire known-good Core file.
- Re-applies only the single V53 CanPlaceModel scenery hook.
- Replaces the V53 helper with the corrected generic version.
- Refuses to finish if the restored/patched Core looks truncated.

Run:
INSTALL_WARBOARD_V53_CORE_RECOVERY_V4.bat

Then return to Unity and allow it to compile.

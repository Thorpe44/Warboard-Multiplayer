WARBOARD V48 CURRENT MAIN RECOVERY

Use this instead of the original V48 installer after the error:

  Could not locate expected v47 text for build version in
  Assets/Scripts/Core/WarboardBuildInfo.cs

CAUSE
-----
Current Warboard main uses:
  public const string CurrentVersion = "v47";

The original V48 patch expected:
  public const string Version = "v47";
  public const string Label = ...

This recovery supports the current CurrentVersion format and also tolerates
the two V48 module files already having been copied by the failed first run.

RUN
---
1. Leave the partially failed V48 files in place.
2. Extract this ZIP into the Warboard-Multiplayer project root.
3. Overwrite the V48_PATCH_PAYLOAD folder when asked.
4. Run:
   INSTALL_WARBOARD_V48_RULES_ALIGNMENT_CURRENT_MAIN.bat
5. Do NOT run cleanup until this reports success and Unity compiles.

The installer still creates its normal Library/WarboardBackups/V48 backup.

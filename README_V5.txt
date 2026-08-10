WARBOARD DICE POLISH RECOVERY V5

This fixes the final remaining bad Regex.Replace call in V4.

V4 FAILURE
----------
Line 180 still used:
  [regex]::Replace(input, pattern, replacement, 1, RegexOptions.Singleline)

That overload does not exist, so PowerShell interpreted Singleline as a
matchTimeout value and stopped.

V5 converts that fallback to a real Regex instance:
  $regex = New-Object System.Text.RegularExpressions.Regex(pattern, Singleline)
  $regex.Replace(input, replacement, 1)

The package contains no V1/V2/V3/V4 BAT files, so there is only one installer
to run.

INSTALL
-------
1. Extract into the current Warboard-Multiplayer root.
2. Overwrite files when asked.
3. Run:
     RECOVER_DICE_TRAY_POLISH_V5.bat
4. Wait for:
     SUCCESS - DICE POLISH RECOVERY V5 VERIFIED
5. Return to Unity and let it compile.

No scene remake required.

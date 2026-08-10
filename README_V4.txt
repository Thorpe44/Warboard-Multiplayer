WARBOARD DICE POLISH RECOVERY V4

This specifically fixes the V3 PowerShell error:

  Cannot convert argument "matchTimeout", with value: "Singleline"

CAUSE
-----
V3 used a non-existent five-argument static Regex.Replace overload.
PowerShell therefore tried to interpret RegexOptions.Singleline as a TimeSpan.

V4 uses an actual System.Text.RegularExpressions.Regex instance and calls:
  regex.Replace(input, replacement, 1)

It also retains the V3 fix for the helper-file self-copy issue.

SAFE STATE
----------
V4 is intended to run after:
- the partially successful original dice polish installer
- the compile hotfix
- failed Recovery V2
- failed Recovery V3

INSTALL
-------
1. Extract this ZIP into the current Warboard-Multiplayer root.
2. Overwrite when asked.
3. Run:
     RECOVER_DICE_TRAY_POLISH_V4.bat
4. Wait for:
     SUCCESS - DICE POLISH RECOVERY V4 VERIFIED
5. Only then return to Unity and let it compile.

Do not run the older V1/V2/V3 installers again.

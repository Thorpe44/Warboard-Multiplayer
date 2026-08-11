WARBOARD MULTIPLAYER V58
========================

BASE
----
Built against the current Warboard Multiplayer source whose visible
WarboardBuildInfo version is v57.

WHAT V58 CHANGES
----------------
1. SECONDARY MISSION CARD TEXT
   GameController.V55MissionCards.cs now has a visible scoring summary for
   all 18 secondary missions registered by MissionRegistry, including:
   - Forward Position
   - Overwhelming Force
   - A Grievous Blow
   - A Tempting Target
   - Beacon
   - Burden of Trust
   - Cleanse
   - Defend Stronghold
   - Display of Might
   - Outflank
   - Plunder
   plus the cards that already had summaries.

2. WRONG FACTION RULES PANEL
   StandardFactionSetupUI no longer falls back to controllers[0] or the first
   Standard11 faction when it cannot resolve the active faction. This prevents
   a stale/non-matching faction from opening an unrelated rules pack such as
   Orks.

3. BOTTOM DEPLOYMENT STATUS BAR
   The permanent status bar at the bottom of the deployment panel is replaced
   by a centered notification toast. A changed status is visible for 3.5
   seconds and then disappears.

4. VERSION
   WarboardBuildInfo.CurrentVersion changes from v57 to v58.

INSTALL
-------
1. Close Unity, or at minimum make sure Play Mode is stopped.
2. Put the WarboardV58 folder inside the Warboard project folder, next to
   Assets. You can also extract the files directly into the project root.
3. Double-click V58_Apply.bat.
4. Wait for "Warboard Multiplayer v58 patch complete."
5. Open Unity and let it compile.

SAFETY
------
The installer:
- checks all five expected source files exist;
- backs them up before editing;
- requires the exact expected V57 source blocks;
- aborts instead of guessing if those blocks changed;
- verifies all V58 markers after writing.

Backups are stored in the project root as:
  WarboardV58_Backup_YYYYMMDD_HHMMSS

FILES TOUCHED
-------------
Assets/Scripts/Core/GameController.V55MissionCards.cs
Assets/Scripts/Factions/Standard11/StandardFactionSetupUI.cs
Assets/Scripts/Core/GameController.UI.cs
Assets/Scripts/Core/GameController.cs
Assets/Scripts/Core/WarboardBuildInfo.cs


INSTALLER FIX
-------------
This package includes the corrected PowerShell installer. The original package
had a PowerShell parser error caused by "$label:" inside error-message strings.
No game files were changed by that parser failure.


INSTALLER FIX 2
---------------
Corrected Windows PowerShell 5.1 regex construction. The previous installer
could parse the RegexOptions -bor expression as an Object[] and abort before
any game-file edits were applied.

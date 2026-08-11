WARBOARD MULTIPLAYER - UI LIFECYCLE FIX V3
========================================

TARGET
------
Repository: Thorpe44/Warboard-Multiplayer
Branch/source reviewed: main, V61 cleaned
Current reviewed HEAD when this bundle was made:
c186ff90ebf0992a6cf9096b6eee96d2bc095645

HOW TO USE
----------
1. Extract this ZIP anywhere.
2. Double-click APPLY_UI_FIXES.bat.
3. If it cannot find the repo automatically, paste the full path to your
   local Warboard-Multiplayer folder.
4. The script creates a timestamped backup in the repo BEFORE editing.
5. Open Unity and wait for the scripts to compile.
6. Test it locally.
7. Only then commit/push it yourself if you are happy.

You can also open Command Prompt in the extracted folder and run:
  APPLY_UI_FIXES.bat "C:\full\path\to\Warboard-Multiplayer"

WHAT IT CHANGES
---------------
- Deletes the permanent Aeldari/Custodes/Necron "... DP ... LOCKED" player bars.
- Leaves deployment-zone lines alone, as requested.
- Makes the lower status message a compact 4-second toast.
- "No squad selected." and "Ready." draw nothing at the bottom.
- Explicitly draws the selected-unit card only when selectedSquad != null.
- Verifies the selected-unit card's own null-selection guard is still present.
- Insets the top command bar and widens WARBOARD / MISSION INFO so the
  left-side navigation is not clipped.
- Restores a bottom-right FACTION RULES button for bespoke factions:
    Aeldari/Ynnari -> AeldariFactionPack11
    Custodes       -> CustodesFactionPack11
    Necrons        -> NecronsFactionPack11
- StandardFactionSetupUI continues to handle:
    Orks
    Tyranids
    Space Marines

WHAT IT DOES NOT DO
-------------------
- Does not change deployment-zone lines.
- Does not commit.
- Does not push.
- Does not touch models, board rendering, combat logic, networking, missions,
  scoring logic, or New Recruit import logic.

UNDO
----
Run UNDO_UI_FIXES.bat and point it at the same repo.

The apply script writes:
  _warboard_ui_fix_backup_latest.json

and makes a timestamped backup folder such as:
  _warboard_ui_fix_backup_20260811_234500

UNDO restores the backed-up source files and removes the two new patch files.
The timestamped backup folder is deliberately left in place.

FILES ADDED TO THE UNITY PROJECT
--------------------------------
Assets/Scripts/Core/GameController.V62UILifecycle.cs
Assets/Scripts/Factions/WarboardBespokeFactionRulesUI.cs


V3 INSTALLER FIXES
------------------
- No BundleRoot command-line argument; Apply-UIFixes.ps1 finds its own payload
  with $PSScriptRoot.
- Repository path is resolved with Resolve-Path, avoiding the Windows trailing
  backslash quoting bug seen in V2.
- Added source preflight before any repo file is edited.
- Bespoke faction rules now use verified runtime detachment-name APIs.

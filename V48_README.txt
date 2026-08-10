WARBOARD v48 - 11TH EDITION RULES ALIGNMENT
============================================

BASE REQUIRED
-------------
Working Warboard v47 project.

INSTALL
-------
1. Extract this ZIP into the MAIN Warboard project folder (the folder containing Assets).
2. Run: INSTALL_WARBOARD_V48_RULES_ALIGNMENT.bat
3. Leave the window open until it says INSTALL COMPLETE.
4. Open Unity and allow scripts to compile.

BACKUP
------
The installer backs up every edited v47 source file under:
Library/WarboardBackups/V48/<timestamp>/

RULES ALIGNMENT INCLUDED
------------------------
- Charge sequence: roll first, then select one or more legal charge targets.
- Multi-target Charge move legality: must engage every selected target and no unselected enemy.
- Heroic Intervention sequence aligned to the same roll-then-target structure.
- Command phase ordering: phase start -> Core CP -> Battle-shock -> Command abilities.
- Removes the incorrect Incursion single-3DP exception; Incursion uses the 2DP maximum.
- XCOM attack resolution uses 11e allocation groups and resolves save dice lowest-to-highest.
- Precision is an explicit optional Character allocation choice.
- Lethal Hits is optional rather than forced on every Critical Hit.
- Command Re-roll lets the player choose one eligible die from a multi-die roll.
- A die already rerolled by another rule cannot be Command Re-rolled again.
- Hazardous uses 3 mortal wounds only when every model in the unit is MONSTER/VEHICLE.
- Fire Overwatch is added to the end-of-Movement Core reaction sequence using Snap Shooting.
- Crushing Impact selects the engaged model and uses that model's Toughness for its dice pool.
- Explosives selects the actual EXPLOSIVES/GRENADES model and validates shooting eligibility/visibility/range.
- Dense terrain sections 2 inches or lower are horizontally traversable by all models.

TEST FIRST
----------
Before pushing v48 to GitHub, run at least:
- one XCOM shooting attack with Lethal Hits and/or Precision,
- one multi-profile save sequence,
- one normal Charge,
- one Fire Overwatch window,
- one Traditional charge/attack sequence.

This patch aligns the specific mismatches found in the v47 audit against the supplied June 2026 Core Rules and July 2026 faction packs. It does not claim that an untested Unity compile is automatically bug-free; report any compiler error with a screenshot and the full Console message.

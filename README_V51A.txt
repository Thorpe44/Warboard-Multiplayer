WARBOARD v51a — WINDOWS LINE-ENDING-SAFE GAMEPLAY / UI BUGFIXES
=====================================

Target repository
-----------------
Thorpe44/Warboard-Multiplayer

Install
-------
1. Extract this ZIP directly over the Warboard-Multiplayer project root.
2. Run INSTALL_WARBOARD_V51A.bat.
3. Let Unity compile.
4. Reload/re-import both rosters once. This is required for the corrected
   per-model YellowScribe loadout distribution to rebuild unit model data.

v51a contains the same gameplay/UI fixes as v51, plus a corrected Windows installer.
The original v51 installer compared LF patch anchors against a CRLF Windows checkout,
so it could fail before staging the first source edit. v51a normalises CRLF/LF during
anchor matching. The failed v51 attempt did not commit project source.

Gameplay/UI fixes:

1. Traditional failed-charge soft lock
   The v48 charge flow correctly rolls before selecting targets, but the
   Traditional state panel still required a target before it would draw its
   APPLY CHARGE TOTAL controls. The panel is now valid while the target is
   intentionally null. After the final 2D6 total is entered, v48 proceeds to
   legal target selection or resolves the failed charge cleanly.

2. Clicked attached-model identity
   The selected-unit card now displays the physical model/datasheet unit that
   was clicked. Clicking Yvraine inside a joined Kabalite unit therefore shows
   Yvraine rather than labelling her as a Kabalite Warrior. Gameplay actions
   still remain attached to the joined action unit.

3. YellowScribe per-model weapons
   Weapon quantity is expanded across every model in the YellowScribe model
   profile group. A five-model Wraithblade group with one Ghostaxe per model
   now receives five Ghostaxe instances rather than one model receiving the
   only melee weapon.

4. Blade Champion attachment
   Adds explicit Blade Champion compatibility for Custodian Guard and
   Custodian Wardens.

5. Player rows
   Aeldari and Adeptus Custodes locked setup rows now use Player 1 and Player 2
   side-by-side slots directly below the top HUD rather than stacking at the
   upper-right.

6. Lions of the Emperor
   Against All Odds was already present in the current Custodes combat engine.
   v51 makes its active +1 Hit / +1 Wound modifier visible in the attack-rule
   breakdown, so it is obvious when the isolated-unit condition is actually
   applying.

7. Mission rules/info
   The top HUD tab is labelled MISSION INFO and the opened panel identifies
   itself as MISSION INFO / RULES while retaining the existing primary mission
   summaries, scoring state and secondary controls.

Smoke test
----------
- Traditional: declare a charge, roll too low, enter the final total. The
  charge state must clear instead of leaving an empty black panel.
- Traditional: make a successful charge. Enter the roll first, then choose the
  legal target(s).
- Click Yvraine while attached to Ynnari Kabalite Warriors. The selected card
  title should say Yvraine and the DATASHEET button should still open Yvraine.
- Reload the roster and inspect a 5-model Wraithblade unit. Common melee gear
  should appear on all five models.
- Attach a Blade Champion to Custodian Guard or Custodian Wardens.
- Load Custodes vs Aeldari. The two locked faction rows should sit side by side
  underneath the top bar.
- With Lions of the Emperor active, attack using a non-VEHICLE Custodes unit
  with no other friendly unit within 6". The attack breakdown should display:
  Against All Odds: +1 Hit, +1 Wound.
- Open MISSION INFO from the top bar and confirm the active mission summaries
  and controls are readable.

Safety
------
The installer patches copies in Library/WarboardV51Staging_* first, validates
all v51 markers, then commits them. It creates a timestamped backup under:
Library/WarboardBackups/V51_*

If staging fails, project source is untouched. If commit fails, the installer
restores the backup.

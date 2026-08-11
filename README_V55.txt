WARBOARD v55a INSTALLER FIX
============================
Fixes: Expected 3 match(es) for clean terrain generator calls; found 2.

Cause: two terrain call sites use 12-space indentation and the third uses
8-space indentation. v55 assumed all three were identical. v55a patches
the 2 + 1 call sites separately. Gameplay payload is otherwise unchanged.

WARBOARD v55 — USABILITY / TERRAIN / CHARGE
============================================
Target: Thorpe44/Warboard-Multiplayer
Expected base: working v54 local build.

Install
-------
1. Extract over the Warboard-Multiplayer project root.
2. Run INSTALL_WARBOARD_V55.bat.
3. Let Unity compile.

Changes
-------
CHARGE CRASH / FREEZE
- v48's auto solver could evaluate hundreds of formation translations and run
  the expensive individual-model refinement pass on every one.
- v55 uses a hard candidate budget (72 translations) and only refines the best
  5. Normal single-target charges first use the cheaper existing formation
  solver.
- Full v48 final legality checks still decide whether a charge succeeds.

SQUAD SHOOTING
- Default flow is now squad -> target -> grouped weapon pool.
- Example: FIRE 5x Shuriken Catapult | 5 models | 18" A 2 S 4 AP -1.
- All eligible copies of that weapon resolve together.
- Select a physical model first to get ADVANCED / SPLIT FIRE, which preserves
  the existing model-level shooting path where required.
- Weapon-use tracking remains per model, so a unit is not marked done until its
  actual firing models are complete.

TERRAIN REDESIGN
- The old decorative V45 rubble dressing is no longer used for V50 generated
  terrain.
- Blocking pieces become clean L-shaped ruin walls.
- Cover areas use low paired barricades with a visible central gap.
- Visible solid pieces are the actual collider geometry: what looks solid is
  solid; the tinted Terrain Area floor is walkable.
- Normal Terrain Area outlines are brighter cyan; objective Terrain Areas retain
  their gold outline.

MISSION CARDS
- Four physical world cards are added beside the scoreboard: Primary +
  Secondary for each player.
- Each card is headed by the player's displayed faction.
- Active Tactical/Fixed secondaries show a concise rule explanation for the
  seven card types currently verified by Warboard's automatic scoring engine.
- Unverified/manual card types explicitly say to use the official card text
  instead of Warboard inventing a rule.

HUD
- Aeldari and Custodes detachment bars move down below the score strip.
- Selected-unit/Fight controls move down with them so they do not overlap.

Smoke test
----------
1. XCOM charge a 5+ model unit and press CONFIRM TARGET(S). Unity should not
   hang/crash. Legal charges still need to satisfy every selected target.
2. Shooting: select a squad, click a target. You should get grouped weapon
   counts without having to select each firing model.
3. Terrain: start a fresh mission so terrain regenerates. Clear tinted floor is
   walkable; only the visible ruin/barricade pieces block final placement.
4. Tactical missions: draw a secondary. The physical Secondary card should
   update automatically with its title and explanation.
5. Top HUD: score line remains visible above the two detachment bars.

Safety
------
Installer is CRLF/LF-safe and Windows PowerShell 5.1 conservative. Existing
files are backed up under Library/WarboardBackups/V55_<timestamp>. Everything
is staged and marker-validated before commit. Failed commits are rolled back.

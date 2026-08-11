WARBOARD R2.2 - UI + NECRON MODEL FIX

Fixes the three issues visible after Terrain R2:

1. TERRAIN HOVER ERROR
The old R2 tooltip created GUIStyles from GUI.skin in Awake(), causing:
ArgumentException: You can only call GUI functions from inside OnGUI.
R2.2 creates/caches those GUI styles only from OnGUI.

2. SCOREBOARD + MISSION CARD LAYOUT
The physical row is now:
[P1 PRIMARY] [P1 SECONDARY] [MATCH SCOREBOARD] [P2 PRIMARY] [P2 SECONDARY]

The existing scoreboard stays in the centre. All four mission cards use the same
Y/Z and billboard behaviour as BattlefieldWorldUI's scoreboard. Each card has
its own wooden frame/ledge and keeps the full existing mission text. Long card
text automatically shrinks.

3. NECRON MODELS
The Necron pack was present under Resources/Armies/Models/Necrons, but the live
model visual path only resolved Custodes and Aeldari. R2.2 adds a Necron resolver
and wires it into both SquadController visual-resolution passes.

The resolver:
- reads Necrons/ModelIndex.json
- matches canonical unit names
- prefers source Main (the colourful/preferred source)
- falls back to Backup when Main does not cover a unit
- cycles available variants across models in the squad
- re-anchors raw TTS world positions so the OBJ appears on its Warboard token
- retains the gameplay capsule fallback if no loadable OBJ exists

Terrain geometry from Terrain Overhaul R2 is NOT changed.
WarboardBuildInfo.cs is NOT changed.

INSTALL
1. Extract into the Warboard-Multiplayer project root.
2. Run INSTALL_WARBOARD_R2_2_UI_NECRON_FIX.bat
3. Let Unity compile/reimport.
4. Start a fresh battle (existing spawned squads will not magically rebuild their visuals).

Backup:
Library/WarboardBackups/R2_2_UI_NECRON_FIX/<timestamp>

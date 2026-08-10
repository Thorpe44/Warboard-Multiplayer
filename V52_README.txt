WARBOARD V52 - PLACEMENT / MOVEMENT GHOST PREVIEW

Install:
1. Extract this ZIP into the Warboard Unity project root.
2. Run INSTALL_WARBOARD_V52_PLACEMENT_GHOST.bat
3. Return to Unity and allow scripts to compile.

Behaviour:
- Initial deployment: full unit ghost follows the cursor.
- Attached unit deployment: previews the generated joined formation.
- Normal Move: selected model ghost follows the cursor.
- Whole-unit translation: the whole formation is ghosted.
- Reserves/reinforcements: full unit ghost is shown.
- Special whole-unit moves: full formation ghost is shown.
- Pile In / Consolidate: selected model ghost is shown.

Colours:
- GREEN: existing Warboard validator says the candidate is legal.
- RED: existing Warboard validator says it is illegal.
- CYAN: physical preview only; a special setup/ingress rule still determines final legality.

The preview uses Graphics.DrawMesh only. It creates no colliders, does not move real models,
does not alter rules state and does not send multiplayer state.

Backup:
Library/WarboardBackups/V52/<timestamp>

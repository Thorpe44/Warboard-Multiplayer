WARBOARD V49 - 11TH EDITION TERRAIN OBJECTIVES
================================================

Purpose
-------
Warboard V48 still displayed/scored normal mission objectives as traditional
3-inch-radius circular markers. In 11th edition, normal battlefield objectives
are primarily terrain areas.

V49 changes
-----------
- Binds every normal mission objective to a unique existing mission terrain area.
- Hides the old circular objective marker/beacon visuals once terrain is bound.
- Draws a thin gold outline around the objective terrain footprint.
- Objective Control is counted against the terrain-area footprint, with model
  base radius included at the footprint edge.
- Existing ObjectiveController roles, mission state, secured control and scoring
  are preserved.
- Mission-action clicks on objective terrain resolve back to the correct
  ObjectiveController.
- If a future mission has no available terrain area for an objective, the old
  marker remains as a safe fallback for special marker-style objectives.
- Build identity becomes v49.

Install
-------
1. Close Play Mode in Unity (closing Unity entirely is safest).
2. Extract this ZIP anywhere inside your Warboard project, or into the project root.
3. Run INSTALL_WARBOARD_V49_TERRAIN_OBJECTIVES.bat
4. Reopen/return to Unity and let it compile.
5. Start a mission and confirm objectives are terrain footprints, not circles.

Safety
------
The installer validates all current-main patch anchors before writing anything.
It creates a backup under Library/WarboardBackups/V49/<timestamp>.
If a write-stage error occurs, it restores the backed-up source files.

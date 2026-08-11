WARBOARD TERRAIN R2.1 UI FIX

Fixes the red Unity error:
ArgumentException: You can only call GUI functions from inside OnGUI.

Also changes the world UI to:
[P1 PRIMARY] [P1 SECONDARY] [MATCH SCOREBOARD] [P2 PRIMARY] [P2 SECONDARY]

The scoreboard itself is untouched.
The old mission-card component is disabled at runtime and its old card objects are removed.
Each new card keeps Warboard's full existing mission-card text and auto-shrinks long text.
Terrain geometry from R2 is unchanged.

Run INSTALL_WARBOARD_TERRAIN_R2_1_UI_FIX.bat

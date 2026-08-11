WARBOARD R2.6 - UI READABILITY + ORK DATA FIX

Run:
  INSTALL_WARBOARD_R26_UI_READABILITY_ORK_DATA_FIX.bat

This patch does four things:
  1. Makes the physical mission cards and world scoreboard text much larger.
  2. Adds a new top summary bar that shows BOTH players' mission/faction data,
     so Player 2 / Ork information is visible instead of disappearing.
  3. Hides or humanises raw underscored/internal rule tokens in datasheet-like UI.
  4. Slightly improves a few other small readability points.

A rollback backup is created inside:
  Library\WarboardBackups\R2_6_UI_READABILITY\<timestamp>

WARBOARD R28.1 - UNIFIED MODEL RESOLVER
=======================================

This is the corrected installer for R28.

The first R28 installer expected one exact formatting of the existing
previewVisual block. R28.1 uses tightly anchored, whitespace-tolerant matching
instead and still aborts/restores if it cannot identify exactly one safe target.

Extract into:
C:\Users\ellio\Documents\GitHub\Multiplayer\

Run:
INSTALL_WARBOARD_R28_1_UNIFIED_MODEL_RESOLVER.bat

R27 changes elsewhere in SquadController.cs are preserved.

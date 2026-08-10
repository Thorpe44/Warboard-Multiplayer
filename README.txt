WARBOARD DICE VISIBILITY + TRAY POLISH

Designed for the current Warboard-Multiplayer main build.

CHANGES
-------
- Dice are ~28% larger in world space.
- Corrects the non-uniform tray scaling so polyhedral dice are not visually
  stretched/squashed by their parent transform.
- Dice are brighter ivory with much larger high-contrast face numbers.
- Physical dice tray is longer and significantly wider.
- Adds tall invisible catch-wall colliders and an invisible ceiling so dice
  cannot jump over the visible lip.
- Adds an emergency bounds recovery: even if physics somehow tunnels through
  a collider, the die is returned to the tray instead of being lost.
- Adds a dedicated RESULT display using the already-synchronized settledText,
  so multiplayer clients see the same final result text.

MULTIPLAYER
-----------
No changes to the working host-authoritative shared dice networking.
The result display reads the synchronized settledText that is already included
in WarboardDiceSnapshot.

INSTALL
-------
1. Extract into the Warboard-Multiplayer project root.
2. Run INSTALL_DICE_TRAY_POLISH.bat
3. Wait for:
   SUCCESS - DICE VISIBILITY + TRAY POLISH VERIFIED
4. Return to Unity and let it compile.
5. Test in Editor first.
6. Rebuild the Windows EXE only after the Editor version looks right.

No scene remake is required.

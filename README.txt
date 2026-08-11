WARBOARD V54 - TERRAIN OVERHAUL

This patch is a presentation-heavy terrain pass aimed at making the battlefield
read much more like Warhammer 40,000 terrain:

- replaces the old abstract cube/debris look with a small library of clear
  ruin/barricade silhouettes
- tones down the footprint fill/outline so the scenery reads first and the rules
  footprint reads second
- gives large footprints recognisable L / U / corner ruin layouts
- gives long narrow footprints industrial barricade lanes instead of random blobs
- adds hover text so when you mouse over terrain you get a quick explanation of
  what the piece is meant to represent
- includes a heuristic shrink / reposition pass for oversized floating mission
  cards, placing them onto a small stand if they are detected
- includes a few texture crops generated from the footprint/base images you sent,
  so the floors feel less like plain grey boxes

IMPORTANT:
This patch does not edit GameController.Core.cs or the V50/V53 terrain logic.
Instead it adds a runtime bootstrap script that restyles and rebuilds the visual
scenery after the terrain areas already exist. That makes it much safer than yet
another invasive core patch.

INSTALL:
1. Extract this ZIP into your Warboard project root.
2. Run INSTALL_WARBOARD_V54_TERRAIN_OVERHAUL.bat
3. Open Unity and let it reimport/compile.
4. Enter play mode and check the table.

If you like the direction, the next pass can be a stricter art pass with more
specific gothic/imperial wall kits and more exact mission-card handling.

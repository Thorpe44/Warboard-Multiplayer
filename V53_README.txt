WARBOARD V53 - SOLID SCENERY END-POSITION FIX

Problem fixed:
V52's ghost could show GREEN while a model's base was visibly intersecting the
actual ruin/wall geometry sitting on a Terrain Area.

Correct behaviour:
- The Terrain Area footprint itself remains legal space.
- A base cannot be DEPLOYED or END A MOVE overlapping solid scenery on that area.
- The placement ghost turns RED for those overlaps.
- This does NOT change movement-path permissions. Units that are allowed to move
  through ruin walls can still pass through them; they simply cannot finish inside them.

Install:
1. Extract into the Warboard Unity project root.
2. Run INSTALL_WARBOARD_V53_SOLID_SCENERY_ENDPOINT_FIX.bat
3. Let Unity compile.
4. Start/continue a battle and test the same wall-overlap placement.

Backup:
Library/WarboardBackups/V53/<timestamp>

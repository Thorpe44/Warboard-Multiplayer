WARBOARD V50 - 11TH EDITION TERRAIN AREA BATTLEFIELD REPAIR
===========================================================

Why this exists
---------------
V49 fixed objective-control geometry, but it still tried to bind objectives to
V48's old loose-scatter terrain. That is not what 11th edition battlefields look
like. 11e separates terrain FEATURES from terrain AREAS, and standard mission
layouts use 16 defined terrain-area footprints.

V50 rebuilds that layer correctly:
- 16 visible terrain-area footprints
- official standard footprint sizes:
  * 4 x 7" x 11.5" large rectangles
  * 2 x 8" x 11.5" right-angle triangles
  * 4 x 6" x 4" medium rectangles
  * 2 x 10" x 2.5" long lines
  * 4 x 6" x 2" short lines
- scenery features sit ON the footprint instead of being the footprint
- normal objectives are the terrain areas themselves
- objective areas are clearly highlighted; no 3" circular objective aura
- OC checks the actual rectangle/triangle footprint
- terrain-area click targets drive mission actions
- old V49/V48 objective marker visuals are disabled

Installer
---------
1. Close Play Mode.
2. Extract this ZIP into the Warboard project root.
3. Run INSTALL_WARBOARD_V50_TERRAIN_AREAS.bat
4. Let Unity compile.
5. Start a fresh battle so the battlefield is regenerated.

The installer accepts V49 (normal path) and V48 (recovery path), validates its
anchors before writing, and backs up changed files under:
Library/WarboardBackups/V50/<timestamp>

Important scope
---------------
This repairs the core 11e terrain-area representation and standard footprint
set. The project's existing Force-Disposition mission positioning remains the
source of objective/deployment locations. Exact Event Companion map-by-map
coordinates can be imported later without changing this architecture.

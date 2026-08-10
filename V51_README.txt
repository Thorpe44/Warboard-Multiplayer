WARBOARD V51 - TERRAIN AREA DEPLOYMENT FIX

Problem fixed
-------------
V50 put the Terrain Area's trigger MeshCollider on the same GameObject as the
TerrainFeature. Warboard's placement validator therefore treated the whole
footprint as terrain during deployment/movement checks.

V51 moves ONLY the terrain-area click collider onto a child GameObject that has
no TerrainFeature component. This means:

- clear parts of Terrain Areas can be deployed into and moved across;
- objective/terrain-area clicks still work because raycasts resolve the parent
  TerrainFeature;
- the actual ruin/wall/scenery children retain their own TerrainFeature and
  colliders, so physical scenery still blocks/restricts placement as intended;
- objective-control geometry remains the V50 terrain-area footprint.

INSTALL
-------
1. Extract this ZIP into the Warboard project root.
2. Run INSTALL_WARBOARD_V51_TERRAIN_DEPLOYMENT_FIX.bat
3. Return to Unity and let it compile.
4. Start a FRESH battle so the terrain-area runtime objects regenerate.

The installer accepts v50 and safe v51 reruns and backs up changed files under
Library/WarboardBackups/V51/<timestamp>.

WARBOARD v57 — TERRAIN + DEPLOYMENT GHOSTS
================================================
Target: Thorpe44/Warboard-Multiplayer
Base: working v54/v55/v56 local build.

Install
-------
1. Extract over the Warboard-Multiplayer project root.
2. Run INSTALL_WARBOARD_V57.bat.
3. Let Unity compile.
4. Start a fresh mission to regenerate terrain.

Deployment ghosts
-----------------
Undeployed units have their normal renderers deliberately hidden by
StageForDeployment(). v54's generic ghost path did not treat deployment as a
special presentation state.

v57 now:
- ray-projects the mouse directly onto the tabletop during deployment;
- previews currentDeploymentSquad even while its real models are hidden;
- includes an attached Leader before either unit is on the battlefield;
- explicitly re-enables cloned renderers;
- does not suppress deployment ghosts merely because a setup/rule-choice UI is
  open;
- keeps green = legal and red = illegal.

Terrain redesign
----------------
The v55 grey slab kit is replaced. The terrain-area footprint remains the
rules area and is walkable. Only the clearly visible 3D objects are solid.

Blocking terrain now uses:
- broken two-section wall runs;
- genuine model-width gaps/doorways;
- damaged lintels;
- corner columns/buttresses;
- darker concrete/stone materials;
- rusted structural accents;
- limited low rubble kept against the ruin edge.

Cover terrain now uses:
- paired dark industrial barricades;
- an obvious central passage;
- angled support feet;
- occasional supply crates;
- small hazard-colour details.

The solid 3D pieces retain TerrainFeature colliders. Decorative attached trim
has no collider, so it does not secretly change placement legality.

Smoke test
----------
1. Start a new mission.
2. During deployment, choose an undeployed unit and move the cursor across the
   deployment zone. The complete translucent unit should follow the cursor.
3. Attach a Leader and deploy the joined unit: the preview should include the
   Leader as well as the bodyguard.
4. Check green/red deployment legality near zone edges and solid terrain.
5. New terrain should look like ruined structures/barricades, not blank grey
   slabs. Move into the tinted terrain footprint; only the visible 3D solid
   pieces should block the final base.

Safety
------
This installer replaces only two Warboard-generated patch files:
- GameController.V54PlacementGhost.cs
- GameController.V55CleanTerrain.cs

It backs those files and WarboardBuildInfo.cs up under
Library/WarboardBackups/V57_<timestamp>, stages replacements first, validates
markers, then commits.

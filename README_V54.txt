WARBOARD v54b — PATCHER RETURN FIX
==================================
v54a passed all source anchors through the mission-preview patches, then the
GameController.V45Presentation.cs callback returned null.

Cause:
    return
        Replace-RegexOnce ...

In Windows PowerShell, that newline ends the `return` statement, so the helper
result is discarded and Stage-Patch receives $null.

v54b fixes ALL FOUR remaining callbacks that used this pattern:
- GameController.V45Presentation.cs
- GameController.V48CoreAlignment.cs
- GameController.V53SolidSceneryPlacement.cs
- WarboardBuildInfo.cs

Each now assigns the patched source to $patchedText and explicitly returns
`$patchedText`.

The failed v54a screenshot states the project source was not committed, so
there is nothing to roll back before installing v54b.

WARBOARD v54a — INSTALLER MATCH FIX
====================================
This is the full v54 package with one installer correction.

The first v54 installer required exactly two copies of the Stratagem hover
rule block. The user's current working v51a local source contains one, so
the installer correctly stopped during staging before committing anything.
v54a now requires at least one matching block and patches every match found.

WARBOARD v54 — TEST SESSION BUGFIXES
====================================
Target: Thorpe44/Warboard-Multiplayer
Base: user's working v51a local build.

Install
-------
1. Extract over the Warboard-Multiplayer project root.
2. Run INSTALL_WARBOARD_V54.bat.
3. Let Unity compile.

Fixes
-----
- Fight phase: dedicated visible Fight bar exposes DONE SIDE PILE-IN,
  DONE PILE-IN, DONE ATTACKS and consolidation controls that already existed
  in the 11e Fight engine but were hidden by the redesigned HUD.
- Traditional/manual: inferred Custodes and Aeldari reaction popups are
  suppressed. Fire Overwatch no longer auto-pops at end of Movement.
  STRATAGEMS remains the manual way to use those rules.
- Stratagem hover rule box grows from 52px to 150px.
- Mission setup: modular Custodes, Necrons, Orks, Tyranids and base Space
  Marines show selected army/detachment rule summaries instead of generic.
- Mission map preview is brighter and objective dots are larger.
- Models can enter clear objective/terrain footprints. Trigger click/area
  surfaces no longer count as solid scenery, while actual ruin/wall/rubble
  geometry still blocks a base.
- Transparent placement/movement previews are restored with actual cloned
  miniature visuals, no colliders or interaction, Ignore Raycast, and
  green/red legal-position tint. Covered contexts: deployment, ingress/reserve
  placement, special moves, whole-unit moves, single-model moves, pile-in and
  consolidation.
- Build watermark becomes v54.

Smoke test
----------
1. Fight phase -> PILE IN bar appears below top HUD and DONE SIDE PILE-IN works.
2. Traditional -> no automatic Fire Overwatch or Custodes/Aeldari reaction
   popup.
3. Hover Stratagem -> the rule text area is visibly much taller.
4. Mission setup -> Custodes rule is no longer generic; preview map is clear.
5. Move into a clear objective footprint -> allowed unless touching actual
   scenery.
6. Select/move/deploy a model -> translucent model follows cursor and is
   green/red based on legality.

Safety
------
Installer is CRLF/LF-safe. Existing files are backed up under
Library/WarboardBackups/V54_<timestamp>. All changes are staged and marker
validated before commit. A failed commit restores backups and removes newly
added v54 files.

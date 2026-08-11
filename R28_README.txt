WARBOARD R28 - UNIFIED MODEL RESOLVER
=====================================

Purpose
-------
Replaces the runtime chain:
  ExtendedFactionModelPackResolverR25 -> Custodes resolver -> Aeldari registry
with one faction-aware model resolver.

This fixes the warning spam where Aeldari units were being tested against the
Necron / Ork / Tyranid packs before falling back to the Aeldari pack.

Files changed
-------------
1. Adds:
   Assets/Scripts/Core/UnifiedModelVisualResolverR28.cs

2. Patches only the CreateModels() visual-resolution section of:
   Assets/Scripts/Core/SquadController.cs

The installer does NOT replace the entire SquadController.cs file, so your R27
Leader changes elsewhere in that file are preserved.

Runtime behaviour
-----------------
- Explicit Aeldari -> Aeldari pack only
- Explicit Custodes -> Custodes pack only
- Explicit Necrons -> Necrons pack only
- Explicit Orks -> Orks pack only
- Explicit Tyranids -> Tyranids pack only
- Player 1 / Player 2 / unknown faction ids may infer a pack only from strict
  exact or singular-exact unit/role matches.
- A normal missing miniature is silent and leaves the gameplay capsule.
- A matched entry whose OBJ cannot be loaded still warns.
- Each model visual is resolved once and reused for spacing + attachment.

Safety
------
The installer creates an R28_BACKUP_YYYYMMDD_HHMMSS folder in the project root.
If installation fails, it automatically restores SquadController.cs and the
previous R28 resolver (if one existed).

Installation
------------
Extract this folder into the Warboard project root, for example:

C:\Users\ellio\Documents\GitHub\Multiplayer\

Run:
INSTALL_WARBOARD_R28_UNIFIED_MODEL_RESOLVER.bat

Then open Unity and allow scripts to recompile.

Expected console result
-----------------------
You may see one ordinary "Warboard R28 model pack loaded" log per faction pack
that is actually used.

You should NOT see:
"no strong Necron/Ork/Tyranid match for 'Aeldari ...'"

Old resolver files remain in the project for compatibility with any legacy code,
but SquadController no longer invokes them.

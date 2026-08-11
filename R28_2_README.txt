WARBOARD R28.2 - UNIFIED MODEL RESOLVER
=======================================

This version deliberately DOES NOT PATCH SquadController.cs.

Why
---
R28 and R28.1 failed because your local SquadController has evolved beyond the
exact formatting present on GitHub. Text-surgery on that file is unnecessary
and too brittle.

R28.2 instead preserves the existing API already called by SquadController:

    ExtendedFactionModelPackResolverR25.TryResolve(...)

That old class is replaced with a tiny compatibility shim. The shim forwards
the call to:

    UnifiedModelVisualResolverR28.TryResolve(...)

The unified resolver:
- selects only the correct faction pack when faction is known;
- supports Aeldari, Custodes, Necrons, Orks and Tyranids;
- caches successful resolutions and misses, so SquadController's existing
  preview + attach calls do not repeat expensive matching/resource work;
- silently returns null for an ordinary missing miniature;
- still warns when an indexed/matched OBJ is actually broken.

R27 gameplay/Leader changes are untouched.

Install
-------
Extract into:
C:\Users\ellio\Documents\GitHub\Multiplayer\

Run:
INSTALL_WARBOARD_R28_2_UNIFIED_MODEL_RESOLVER.bat

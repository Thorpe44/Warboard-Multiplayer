WARBOARD MULTIPLAYER PACKAGE COMPATIBILITY FIX
==============================================

WHY
---
The earlier source drop pinned:
- Netcode for GameObjects 2.7.0
- Multiplayer Services 1.2.0

Those package builds compile against older Unity 6 editor APIs and fail under
Unity 6000.5 with EntityId / EndNameEditAction errors.

THIS FIX PINS
-------------
- com.unity.netcode.gameobjects: 2.13.1
- com.unity.services.multiplayer: 2.0.0
- com.unity.services.authentication: 3.5.2

INSTALL
-------
1. CLOSE UNITY.

2. Extract this ZIP into the Warboard project root.
   Allow it to replace:
       Packages/manifest.json

3. Run:
       RESET_MULTIPLAYER_PACKAGE_CACHE.bat

4. Re-open the project in Unity.

5. Wait for Package Manager and script compilation to finish.

DO NOT EDIT FILES INSIDE Library/PackageCache.
They are generated package files.

The CS0618 warnings in Warboard source are separate deprecation warnings and
do not stop compilation. Fix package errors first.

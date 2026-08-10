WARBOARD DIRECT RELAY COMPATIBILITY FIX
=======================================

This removes the unified Multiplayer Services package from Warboard.

WHY
---
Unity 6000.5 is rejecting obsolete Editor APIs inside the Multiplayer
Services package's Matchmaker authoring code.

Warboard does not need Matchmaker to get online multiplayer working.

This replacement uses:
- Unity Relay 1.2.0
- Authentication 3.5.2
- Netcode for GameObjects 2.13.1

The host still gets a short Relay join code.
The second player still types that join code and connects over the internet.

INSTALL
-------
1. CLOSE UNITY.

2. Extract this ZIP into the Warboard root.

3. Allow these files to replace the existing copies:
   Packages/manifest.json
   Assets/Scripts/Multiplayer/WarboardSessionService.cs

4. Run:
   RESET_TO_DIRECT_RELAY.bat

5. Reopen Unity.

6. Let Package Manager finish resolving and compiling.

This intentionally removes the broken com.unity.services.multiplayer package.
Do not edit files inside Library/PackageCache manually.

NOTE
----
This removes Unity Sessions-based automatic host migration for now.
The actual Warboard state snapshot/reconnect architecture remains in the
project. We can add a custom host migration layer after the base Relay game
is compiling and connecting correctly.

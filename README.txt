WARBOARD MULTIPLAYER START MENU FIX

Fixes:
- NullReferenceException in WarboardMultiplayerBootstrap.EnsureNetworkManager
- no multiplayer/single-player launch screen

INSTALL
1. Extract into the Warboard-Multiplayer project root.
2. Allow files under Assets/Scripts/Multiplayer to replace existing files.
3. Return to Unity and let scripts recompile.
4. Press Play.

Expected startup screen:
- SINGLE PLAYER
- HOST MULTIPLAYER
- JOIN code field + JOIN button

WHY THE NULL REFERENCE HAPPENED
NetworkManager was dynamically added at runtime. Its NetworkConfig field was
null, so the old bootstrap immediately dereferenced it.

The replacement creates the NetworkManager on an inactive GameObject, assigns
a new NetworkConfig and UnityTransport, then activates the object. This ensures
NetworkManager.Awake runs with valid configuration.

F8 toggles the small multiplayer status panel after multiplayer is selected.

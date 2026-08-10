WARBOARD SHARED MULTIPLAYER DICE

ROOT CAUSE
----------
TraditionalDiceTray3D was entirely local:
- ROLL POOL called RollAll() locally.
- SpawnDie() used Unity Random and local Rigidbody physics.
- Dice state was not present in WarboardMatchSnapshot.

Therefore two connected machines could sync the game perfectly while their
dice trays remained independent.

THIS FIX
--------
Adds a dedicated host-authoritative dice channel.

Either player can:
- change pool counts
- roll
- select dice
- reroll selected dice
- clear the tray

For a roll:
1. The controlling player sends a request to the host.
2. The host performs the authoritative physical roll.
3. Both machines run a local rolling animation.
4. When the host dice settle, exact final positions/rotations/results are sent.
5. The client snaps to the authoritative final dice.

This intentionally does NOT try to stream Rigidbody transforms every frame.
Unity physics is not deterministic across separate processes, so doing that
would be wasteful and still prone to divergence.

INSTALL
-------
1. Extract this ZIP into the Warboard-Multiplayer project root.
2. Run INSTALL_SHARED_MULTIPLAYER_DICE.bat.
3. Wait for:
   SUCCESS - SHARED MULTIPLAYER DICE INSTALLED
4. Return to Unity and let it compile.
5. REBUILD the Windows EXE.
6. Test Editor host + rebuilt EXE client.

No scene changes are required.

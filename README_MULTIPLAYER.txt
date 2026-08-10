WARBOARD MULTIPLAYER SOURCE DROP
================================

TARGET
------
Unity 6000.5.x
Multiplayer Services SDK 1.2.0
Netcode for GameObjects 2.7.0
Authentication 3.5.2

WHAT THIS CODE DOES
-------------------
- Anonymous UGS authentication
- Unity Multiplayer Services session creation
- Relay-backed HOST GAME
- Join-by-code
- Automatic NGO NetworkManager bootstrap
- Leave session
- Session/host-change event wiring
- Chunked custom NGO messages
- Full Warboard match snapshots
- Automatic host -> client state replication
- Client -> host state proposals using optimistic revision checks
- Mid-game state rebuild for board/rosters
- Unit/model positions
- wounds/deaths
- reserves/deployment/embarked state
- leader attachments
- core phase/round/faction state
- CP/VP and round scoring
- fixed-secondary scoring
- mission player state/decks/counters
- objective mission-state flags
- terrain operation/trap state
- large set of squad/faction runtime flags
- one-shot/fired weapon state
- reconnect/host-change state bridge foundation
- F8 multiplayer control panel

INSTALL
-------
1. Be on your Multiplayer-foundation branch.

2. Replace Packages/manifest.json with the one in this ZIP.
   If you already added extra packages, MERGE the three multiplayer package
   entries instead of replacing your whole manifest.

3. Copy:
      Assets/Scripts/Multiplayer/
   into your project.

4. Make the five "partial" edits listed in:
      REQUIRED_EXISTING_FILE_EDITS.txt

5. Open Unity and let Package Manager resolve everything.

6. Link the Unity project to a Unity Cloud project:
      Edit -> Project Settings -> Services
   Multiplayer Services / Relay must be available for that project.

7. Press Play.

8. Press F8 if the multiplayer panel is hidden.

9. On PC 1:
      HOST GAME
   Copy the join code.

10. On PC 2:
      enter the join code
      JOIN CODE

TEST ORDER
----------
First test:
- both clients connect
- host changes battle size
- client mirrors it
- load roster(s)
- client gets same models
- deploy one unit
- move one model
- damage one model
- next phase
- CP/VP changes

Then test a complete battle.

IMPORTANT ARCHITECTURE NOTE
---------------------------
This source drop gets the existing Warboard codebase networked without first
rewriting every UI button into an RPC/command. It does that by syncing complete
authoritative snapshots and allowing the non-host to propose a changed state
against the exact revision it last received.

That is ideal for getting the current game online quickly and for cooperative /
trusted tabletop play.

For a public competitive release, progressively convert sensitive actions
(movement, dice, CP spending, attacks, stratagems) to explicit host-validated
Warboard commands. The snapshot layer should remain: it is still useful for
resync, reconnect, save games, and host migration.

HOST MIGRATION
--------------
The session code listens for host changes and the state bridge can resume from
the most recent canonical snapshot held by the new host.

Unity's NGO/MPS host-migration behaviour is still evolving. Test hard host loss
separately from voluntary leave. The Warboard snapshot is deliberately kept
independent of Netcode NetworkObjects so it can be persisted or handed to a
new host later.

KNOWN ITEMS NOT YET SERIALIZED
------------------------------
- a combat/dice popup that is literally half-complete at the instant of a
  disconnect
- arbitrary System.Action callbacks in rule-choice windows
- every private dictionary inside every future faction module

Completed game results and persistent squad/mission state are synchronized.
For disconnect-during-popup recovery, either restart the pending choice or add
a serializable "pending interaction" record for that specific system.

WHY THERE ARE NO NETWORKOBJECTS ON EVERY MINIATURE
--------------------------------------------------
Warboard is turn-based and already has a large mature local rules engine.
Networking every capsule/miniature as a separate NetworkObject would force a
large rewrite and create ownership problems.

This system synchronizes Warboard's authoritative game state instead. That
lets the existing rules engine, New Recruit importer, model resolver, mission
system and faction modules remain largely intact.

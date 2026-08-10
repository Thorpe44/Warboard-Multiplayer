WARBOARD V53 RECOVERY V2

Why this exists:
The first V53 installer depended on an exact text/comment anchor inside
GameController.Core.cs. Your current V52 file is functionally compatible but its
text differs, so the installer correctly aborted before changing anything.

Recovery V2:
- Finds CanPlaceModel structurally by method + brace matching.
- Finds V52 CandidateBoardLegal52 structurally.
- Adds the scenery rule centrally instead of patching individual ghost branches.
- Keeps Terrain Area footprints legal.
- Prevents a model base from DEPLOYING or ENDING a move through actual V50
  wall/ruin/rubble geometry.
- Keeps movement-through permissions unchanged.

Install:
1. Extract this ZIP into the Warboard project root.
2. Run INSTALL_WARBOARD_V53_RECOVERY_V2.bat
3. Let Unity compile.
4. Repeat the wall-overlap placement test.

Backup:
Library/WarboardBackups/V53_RECOVERY_V2/<timestamp>

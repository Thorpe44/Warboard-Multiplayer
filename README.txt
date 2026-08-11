WARBOARD R25.1 - ORK/NID + AELDARI GHOST BASE + MISSION UI FIX

Install:
1. Extract into Warboard-Multiplayer project root.
2. Run INSTALL_WARBOARD_R25_1_MODELS_UI_FIX.bat
3. Let Unity compile.
4. START A FRESH BATTLE.

MODEL FIX

Necrons / Orks / Tyranids:
- use one resolver before ModelToken.AttachVisual
- root-only TTS objects are re-anchored exactly like the successful Necron logic:
  the first TTS table/world position becomes local zero
- parent+child TTS objects discard the source-table parent/wrapper and retain the
  child's local transforms
- no renderer-bounds / after-spawn recenter hack
- source order in each ModelIndex is preserved

Aeldari:
- keeps the existing successful ModelVisualRegistry matcher
- Wraithknight-style world-positioned root wrappers are skipped when proper
  local child meshes exist
- Yvraine-style single root meshes keep the mesh but discard source TTS X/Z
- obviously detached child components are ignored
- Custodes code is untouched

MISSION SETUP UI

The Layout button was only 210px wide even though it contains text such as:
LAYOUT A | SWEEPING ENGAGEMENT

R25 widens it to 300px and moves the Attacker button beside it.

Backup:
Library/WarboardBackups/R25_1_MODEL_UI_FIX/<timestamp>

R25.1 INSTALLER CORRECTION

R25 itself restored its backup correctly on failure. The failure was in the
installer's source-search code: it passed -1 as String.IndexOf(startIndex)
before the fallback search could run. R25.1 validates the anchor first and
uses a line-ending/whitespace tolerant regex fallback.

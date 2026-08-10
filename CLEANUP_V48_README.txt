WARBOARD V48-AWARE SAFE CLEANUP

This version is tailored to WARBOARD_V48_11E_RULES_ALIGNMENT_PATCH.

The patch ZIP contains:
- V48_PATCH_PAYLOAD/
- INSTALL_WARBOARD_V48_RULES_ALIGNMENT.bat
- INSTALL_WARBOARD_V48_RULES_ALIGNMENT.ps1
- V48_README.txt

The older generic cleanup would leave V48_PATCH_PAYLOAD and V48_README.txt.

This cleanup first verifies:
- Assets/Scripts/Core/GameController.V48CoreAlignment.cs exists
- Assets/Scripts/Core/InteractiveAttackController.V48Alignment.cs exists
- WarboardBuildInfo.cs says v48

Only then will it delete V48_PATCH_PAYLOAD.

It keeps the real installed V48 source under Assets and keeps the rollback
backup under Library/WarboardBackups/V48.

Run from the Warboard project root after the V48 patch has installed and
Unity has compiled successfully.

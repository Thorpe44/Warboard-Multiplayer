Warboard GitHub large-file fix

The >100 MB .SearchIndexArtifactImporter....index file is a Unity-generated cache/search index.
It should not be committed and does not need Git LFS.

This fix:
- adds Unity-generated folders such as Library/, Temp/, Obj/, Logs/, and UserSettings/ to .gitignore
- removes those folders from Git tracking without deleting them from your computer
- leaves Assets/, Packages/, and ProjectSettings/ untouched

Usage:
1. Extract this ZIP into your Warboard repository folder.
2. Run FIX_UNITY_GITHUB_LARGE_FILES.bat.
3. Check GitHub Desktop.
4. Commit and push normally.

If another >100 MB file remains, send its full path before choosing Git LFS.

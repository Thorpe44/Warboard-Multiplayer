@echo off
setlocal EnableExtensions
title Warboard Multiplayer - Safe Repo Cleanup

rem ============================================================================
rem WARBOARD MULTIPLAYER - SAFE REPO CLEANUP
rem
rem Scanned against Thorpe44/Warboard-Multiplayer main on 11 Aug 2026.
rem
rem DEFAULT CLEANUP:
rem   - old root patch installers / payloads / manifests / tree dumps
rem   - extracted patch folders
rem   - tracked/untracked Unity Logs
rem   - Temp / obj / .vs / MemoryCaptures
rem   - duplicate root WarboardSessionService.cs, ONLY if the real Assets copy exists
rem   - untracks UserSettings but PRESERVES the local UserSettings folder
rem   - adds root patch-artifact patterns to .gitignore
rem
rem NOT TOUCHED:
rem   - Assets
rem   - Packages
rem   - ProjectSettings
rem   - Docs
rem   - current versioned C# runtime files (R25/V45/V54/V55/etc.)
rem   - model packs / faction packs
rem   - Build / Builds
rem   - Library unless you explicitly answer Y
rem   - rollback backup folders unless you explicitly answer Y
rem
rem Tracked junk is removed with git rm so it is staged for deletion.
rem Modified tracked files are NOT force-removed; they are skipped.
rem ============================================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%.") do set "SCRIPT_DIR_FULL=%%~fI"

call :FindProjectRoot
if not defined PROJECT_ROOT (
    echo.
    echo [ERROR] Could not locate the Warboard Multiplayer project.
    echo.
    echo Put this BAT in:
    echo   C:\Users\ellio\Documents\GitHub\Multiplayer\
    echo and run it again.
    echo.
    pause
    exit /b 1
)

cd /d "%PROJECT_ROOT%"

echo.
echo ============================================================
echo          WARBOARD MULTIPLAYER - SAFE REPO CLEANUP
echo ============================================================
echo.
echo Project:
echo   %PROJECT_ROOT%
echo.

where git >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Git is not available in PATH.
    echo Install/open Git for Windows and run this again.
    echo.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo [ERROR] This is not a Git working tree.
    echo.
    pause
    exit /b 1
)

tasklist /FI "IMAGENAME eq Unity.exe" 2>nul | find /I "Unity.exe" >nul
if not errorlevel 1 (
    echo [ERROR] Unity is currently running.
    echo Close the Unity Editor first so generated files are not recreated
    echo while the cleanup is running.
    echo.
    pause
    exit /b 2
)

if not exist "Assets\Scripts\Core\GameController.cs" (
    echo [ERROR] GameController.cs is missing. Refusing to clean this folder.
    echo.
    pause
    exit /b 1
)

echo This cleanup is deliberately conservative.
echo It removes installer/build debris OUTSIDE Assets, not old-looking runtime code.
echo.
echo Current Git status is shown below:
echo ------------------------------------------------------------
git status --short
echo ------------------------------------------------------------
echo.
set /p "CONFIRM=Type CLEAN to continue: "
if /I not "%CONFIRM%"=="CLEAN" (
    echo.
    echo Cancelled. Nothing was changed.
    echo.
    pause
    exit /b 0
)

echo.
echo [1/7] Removing historical patch payload folders...
for /d %%D in (V*_PAYLOAD) do if exist "%%D\" call :RemoveArtifactDir "%%D"
for /d %%D in (R*_PAYLOAD) do if exist "%%D\" call :RemoveArtifactDir "%%D"

echo.
echo [2/7] Removing extracted patch/installer folders...
for %%D in (WarboardV54 WarboardV55 WarboardV57 WarboardV58) do if exist "%%D\" call :RemoveArtifactDir "%%D"

echo.
echo [3/7] Removing root patch installers and patch-only documents...
for %%F in (
    INSTALL_WARBOARD_*.bat
    INSTALL_WARBOARD_*.ps1
    install_v*_*.bat
    install_v*_*.ps1
    V*_Apply.bat
    V*_Apply.ps1
    README_V*.txt
    README_R*.txt
    R*_README*.txt
    R*_LEADER_ATTACHMENTS_README.txt
    V*_preinstall_manifest.txt
    V*_DIFF_SUMMARY.txt
    V*_CLEANUP_NOTES.txt
    after_v*_tree.txt
    full_after_tree.txt
    build_diff.txt
    WARBOARD_R*.zip
    WARBOARD_V*.zip
    WarboardV*.zip
) do if exist "%%F" call :RemoveArtifactFile "%%F"

rem Root duplicate from old installer payloads. The actual Unity source copy must exist.
if exist "WarboardSessionService.cs" (
    if exist "Assets\Scripts\Core\WarboardSessionService.cs" (
        call :RemoveArtifactFile "WarboardSessionService.cs"
    ) else (
        echo [KEEP] WarboardSessionService.cs - no Assets copy found, so it was NOT removed.
    )
)

echo.
echo [4/7] Cleaning Unity-generated repository debris...
if exist "Logs\" call :RemoveArtifactDir "Logs"
call :RemoveGeneratedDir "Temp"
call :RemoveGeneratedDir "obj"
call :RemoveGeneratedDir ".vs"
call :RemoveGeneratedDir "MemoryCaptures"

rem UserSettings is ignored by the repo and should not be versioned, but preserving
rem the local directory avoids unnecessarily resetting editor/user preferences.
git ls-files -- "UserSettings/*" | findstr /R "." >nul 2>&1
if not errorlevel 1 (
    git rm -r --cached -- "UserSettings" >nul 2>&1
    if errorlevel 1 (
        echo [KEEP] UserSettings is tracked but could not be safely untracked.
    ) else (
        echo [GIT ] UserSettings untracked; local editor settings preserved.
    )
) else (
    if exist "UserSettings\" echo [KEEP] Local UserSettings preserved.
)

echo.
echo [5/7] Preventing patch debris from being re-added...
call :EnsureGitIgnoreBlock

echo.
echo [6/7] Optional rollback-backup cleanup...
echo.
echo Patch backup folders can contain rollback copies of locally modified source.
echo Git usually makes them unnecessary, but they are the ONE category this
echo cleaner will not delete without a separate confirmation.
echo.
set /p "BACKUPS=Delete R/V/Warboard patch BACKUP folders too? [y/N]: "
if /I "%BACKUPS%"=="Y" (
    for /d %%D in (R*_BACKUP_*) do if exist "%%D\" call :RemoveArtifactDir "%%D"
    for /d %%D in (V*_BACKUP_*) do if exist "%%D\" call :RemoveArtifactDir "%%D"
    for /d %%D in (WarboardV*_Backup_*) do if exist "%%D\" call :RemoveArtifactDir "%%D"
) else (
    echo [KEEP] Patch rollback backup folders preserved.
)

echo.
echo [7/7] Optional Unity Library cleanup...
echo.
echo Library is regenerated by Unity and can be very large, but deleting it
echo forces a full asset reimport next time you open the project.
echo.
set /p "LIBRARY=Delete local Library cache too? [y/N]: "
if /I "%LIBRARY%"=="Y" (
    call :RemoveGeneratedDir "Library"
) else (
    if exist "Library\" echo [KEEP] Library preserved.
)

echo.
echo ============================================================
echo CLEANUP COMPLETE
echo ============================================================
echo.
echo Final Git status:
echo ------------------------------------------------------------
git status --short
echo ------------------------------------------------------------
echo.
echo Notes:
echo   - D entries staged by git rm are intentional cleanup deletions.
echo   - M .gitignore is the new protection against future patch debris.
echo   - Existing source modifications were not discarded.
echo   - Any tracked file with local edits that Git refused to remove was kept.
echo.
echo Review the status above before committing.
echo This cleaner DOES NOT commit anything and does not stage unrelated source edits.
echo.
echo Staged cleanup summary:
git diff --cached --stat
echo.
pause
exit /b 0


rem ============================================================================
rem Helpers
rem ============================================================================

:FindProjectRoot
set "PROJECT_ROOT="

rem 1) BAT is in project root.
if exist "%SCRIPT_DIR%Assets\Scripts\Core\GameController.cs" (
    for %%I in ("%SCRIPT_DIR%.") do set "PROJECT_ROOT=%%~fI"
    exit /b 0
)

rem 2) BAT is one folder below project root.
if exist "%SCRIPT_DIR%..\Assets\Scripts\Core\GameController.cs" (
    for %%I in ("%SCRIPT_DIR%..") do set "PROJECT_ROOT=%%~fI"
    exit /b 0
)

rem 3) Known local Warboard checkout used by this project.
if exist "%USERPROFILE%\Documents\GitHub\Multiplayer\Assets\Scripts\Core\GameController.cs" (
    for %%I in ("%USERPROFILE%\Documents\GitHub\Multiplayer") do set "PROJECT_ROOT=%%~fI"
    exit /b 0
)

rem 4) Current directory is inside the repo.
for /f "usebackq delims=" %%R in (`git rev-parse --show-toplevel 2^>nul`) do (
    if exist "%%R\Assets\Scripts\Core\GameController.cs" set "PROJECT_ROOT=%%R"
)
exit /b 0


:RemoveArtifactFile
set "REL=%~1"
if not exist "%REL%" exit /b 0

git ls-files --error-unmatch -- "%REL%" >nul 2>&1
if errorlevel 1 (
    del /f /q "%REL%" >nul 2>&1
    if exist "%REL%" (
        echo [KEEP] %REL% - could not delete.
    ) else (
        echo [LOCAL] %REL%
    )
    exit /b 0
)

git rm -- "%REL%" >nul 2>&1
if errorlevel 1 (
    echo [KEEP] %REL% - tracked file has local/index changes; not force-removed.
) else (
    echo [GIT ] %REL%
)
exit /b 0


:RemoveArtifactDir
set "REL=%~1"
if not exist "%REL%\" exit /b 0

for %%I in ("%REL%") do set "TARGET_FULL=%%~fI"
if /I "%TARGET_FULL%"=="%SCRIPT_DIR_FULL%" (
    echo [KEEP] %REL% - cleaner is currently running from this folder.
    exit /b 0
)

set "HAS_TRACKED="
for /f "delims=" %%G in ('git ls-files -- "%REL%/*" 2^>nul') do set "HAS_TRACKED=1"

if not defined HAS_TRACKED (
    rd /s /q "%REL%" >nul 2>&1
    if exist "%REL%\" (
        echo [KEEP] %REL%\ - could not delete.
    ) else (
        echo [LOCAL] %REL%\
    )
    exit /b 0
)

git rm -r -- "%REL%" >nul 2>&1
if errorlevel 1 (
    echo [KEEP] %REL%\ - contains tracked local/index changes; not force-removed.
    exit /b 0
)

rem A tracked folder may also contain ignored/untracked patch debris.
if exist "%REL%\" rd /s /q "%REL%" >nul 2>&1
echo [GIT ] %REL%\
exit /b 0


:RemoveGeneratedDir
set "REL=%~1"
if not exist "%REL%\" exit /b 0

rem Generated folders should normally be ignored. If anything is tracked,
rem refuse to blindly delete it and let the targeted cleanup handle it instead.
set "HAS_TRACKED="
for /f "delims=" %%G in ('git ls-files -- "%REL%/*" 2^>nul') do set "HAS_TRACKED=1"

if defined HAS_TRACKED (
    echo [KEEP] %REL%\ contains tracked files; not treated as disposable cache.
    exit /b 0
)

rd /s /q "%REL%" >nul 2>&1
if exist "%REL%\" (
    echo [KEEP] %REL%\ - could not delete.
) else (
    echo [CACHE] %REL%\
)
exit /b 0


:EnsureGitIgnoreBlock
if not exist ".gitignore" (
    echo [KEEP] .gitignore missing; no ignore rules added.
    exit /b 0
)

findstr /C:"# WARBOARD LOCAL PATCH ARTIFACTS" ".gitignore" >nul 2>&1
if not errorlevel 1 (
    echo [SKIP] Warboard patch-artifact ignore rules already present.
    exit /b 0
)

>>".gitignore" echo.
>>".gitignore" echo # WARBOARD LOCAL PATCH ARTIFACTS
>>".gitignore" echo # Local/versioned installer debris; never required by the Unity runtime.
>>".gitignore" echo /R*_BACKUP_*/
>>".gitignore" echo /R*_PAYLOAD/
>>".gitignore" echo /V*_BACKUP_*/
>>".gitignore" echo /V*_PAYLOAD/
>>".gitignore" echo /WarboardV*_Backup_*/
>>".gitignore" echo /WarboardV*/
>>".gitignore" echo /INSTALL_WARBOARD_*.bat
>>".gitignore" echo /INSTALL_WARBOARD_*.ps1
>>".gitignore" echo /install_v*_*.bat
>>".gitignore" echo /install_v*_*.ps1
>>".gitignore" echo /V*_Apply.bat
>>".gitignore" echo /V*_Apply.ps1
>>".gitignore" echo /V*_preinstall_manifest.txt
>>".gitignore" echo /V*_DIFF_SUMMARY.txt
>>".gitignore" echo /V*_CLEANUP_NOTES.txt
>>".gitignore" echo /after_v*_tree.txt
>>".gitignore" echo /full_after_tree.txt
>>".gitignore" echo /build_diff.txt
>>".gitignore" echo /WARBOARD_R*.zip
>>".gitignore" echo /WARBOARD_V*.zip
>>".gitignore" echo /WarboardV*.zip
>>".gitignore" echo /WarboardSessionService.cs

echo [EDIT] .gitignore - added Warboard patch-artifact rules.
exit /b 0

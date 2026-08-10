@echo off
setlocal
cd /d "%~dp0"

echo ============================================================
echo WARBOARD MULTIPLAYER - UNITY 6000.5 PACKAGE CACHE RESET
echo ============================================================
echo.

if not exist "Assets\" (
    echo ERROR: Run this from the Warboard project root.
    pause
    exit /b 1
)

if not exist "Packages\manifest.json" (
    echo ERROR: Packages\manifest.json was not found.
    pause
    exit /b 1
)

echo This will remove only Unity-generated package resolution/cache data.
echo Your Assets, ProjectSettings and source code are not touched.
echo.

if exist "Packages\packages-lock.json" (
    del /q "Packages\packages-lock.json"
    echo [REMOVED] Packages\packages-lock.json
)

if exist "Library\PackageCache" (
    rmdir /s /q "Library\PackageCache"
    echo [REMOVED] Library\PackageCache
)

if exist "Library\ScriptAssemblies" (
    rmdir /s /q "Library\ScriptAssemblies"
    echo [REMOVED] Library\ScriptAssemblies
)

echo.
echo DONE.
echo Now open the project in Unity and allow Package Manager to resolve.
echo.
pause

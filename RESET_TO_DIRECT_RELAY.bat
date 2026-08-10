@echo off
setlocal
cd /d "%~dp0"

echo ============================================================
echo WARBOARD - REMOVE BROKEN MPS / SWITCH TO DIRECT RELAY
echo ============================================================
echo.

if not exist "Assets\" (
    echo ERROR: This BAT must be in the Warboard project root.
    pause
    exit /b 1
)

if exist "Packages\packages-lock.json" (
    del /q "Packages\packages-lock.json"
    echo [REMOVED] Packages\packages-lock.json
)

for /d %%D in ("Library\PackageCache\com.unity.services.multiplayer@*") do (
    if exist "%%D" (
        rmdir /s /q "%%D"
        echo [REMOVED] %%D
    )
)

for /d %%D in ("Library\PackageCache\com.unity.netcode.gameobjects@*") do (
    if exist "%%D" (
        rmdir /s /q "%%D"
        echo [REMOVED] %%D
    )
)

if exist "Library\ScriptAssemblies" (
    rmdir /s /q "Library\ScriptAssemblies"
    echo [REMOVED] Library\ScriptAssemblies
)

echo.
echo SUCCESS - OLD MULTIPLAYER PACKAGE CACHE REMOVED
echo.
echo Reopen Unity now. Package Manager will install Relay + NGO.
echo.
pause

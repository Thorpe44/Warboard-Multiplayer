@echo off
setlocal EnableExtensions
title Warboard Multiplayer - Undo UI Lifecycle Fix V3

echo.
echo ============================================================
echo   WARBOARD MULTIPLAYER - UNDO UI LIFECYCLE FIX V3
echo ============================================================
echo.

for %%I in ("%~dp0.") do set "SCRIPT_DIR=%%~fI"
set "ROOT=%~1"

if defined ROOT goto :normalise_root

if exist "%CD%\Assets\Scripts\Core\GameController.UI.cs" (
    set "ROOT=%CD%"
    goto :normalise_root
)

if exist "%SCRIPT_DIR%\Assets\Scripts\Core\GameController.UI.cs" (
    set "ROOT=%SCRIPT_DIR%"
    goto :normalise_root
)

if exist "%SCRIPT_DIR%\..\Assets\Scripts\Core\GameController.UI.cs" (
    for %%I in ("%SCRIPT_DIR%\..") do set "ROOT=%%~fI"
    goto :normalise_root
)

set /p "ROOT=Paste the full path to your Warboard-Multiplayer repo: "

:normalise_root
if not defined ROOT (
    echo ERROR: No repo path supplied.
    pause
    exit /b 1
)

for %%I in ("%ROOT%\.") do set "ROOT=%%~fI"

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\Undo-UIFixes.ps1" -RepoRoot "%ROOT%"
set "ERR=%ERRORLEVEL%"

echo.
if "%ERR%"=="0" (
    echo UNDO COMPLETE.
) else (
    echo UNDO FAILED - see the message above.
)
echo.
pause
exit /b %ERR%

@echo off
setlocal EnableExtensions
title Warboard Multiplayer - Apply UI Lifecycle Fix V3

echo.
echo ============================================================
echo   WARBOARD MULTIPLAYER - UI LIFECYCLE FIX V3
echo ============================================================
echo   Local files only. This does NOT commit or push to GitHub.
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

if not exist "%ROOT%\Assets\Scripts\Core\GameController.UI.cs" (
    echo.
    echo ERROR: This does not look like the Warboard-Multiplayer repo:
    echo   %ROOT%
    echo.
    pause
    exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%\Apply-UIFixes.ps1" -RepoRoot "%ROOT%"
set "ERR=%ERRORLEVEL%"

echo.
if not "%ERR%"=="0" (
    echo PATCH FAILED.
    echo No Git commit or push was performed.
) else (
    echo PATCH COMPLETE.
    echo Open Unity and let it compile, then test before you commit/push.
)
echo.
pause
exit /b %ERR%

@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V52 placement ghost installer...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V52_PLACEMENT_GHOST.ps1"
if errorlevel 1 (
    echo.
    echo V52 installer reported an error.
    pause
    exit /b 1
)
endlocal

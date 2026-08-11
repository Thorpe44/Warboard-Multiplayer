@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V54 terrain overhaul...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V54_TERRAIN_OVERHAUL.ps1"
if errorlevel 1 (
    echo.
    echo V54 terrain overhaul reported an error.
    pause
    exit /b 1
)
endlocal

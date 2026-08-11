@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard Terrain Overhaul R2...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_TERRAIN_OVERHAUL_R2.ps1"
if errorlevel 1 (
    echo.
    echo Terrain Overhaul R2 reported an error.
    pause
    exit /b 1
)
endlocal

@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard unified faction model fix...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_UNIFIED_FACTION_MODELS_AND_CLEANUP.ps1"
if errorlevel 1 (
    echo.
    echo Unified faction model installer reported an error.
    pause
    exit /b 1
)
endlocal

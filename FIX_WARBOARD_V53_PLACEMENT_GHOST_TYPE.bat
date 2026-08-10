@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V53 placement ghost type compile fix...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_WARBOARD_V53_PLACEMENT_GHOST_TYPE.ps1"
if errorlevel 1 (
    echo.
    echo Fix reported an error.
    pause
    exit /b 1
)
endlocal

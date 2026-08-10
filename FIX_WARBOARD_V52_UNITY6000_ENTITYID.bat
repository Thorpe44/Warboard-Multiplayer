@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_WARBOARD_V52_UNITY6000_ENTITYID.ps1"
if errorlevel 1 (
    echo.
    echo Fix reported an error.
    pause
    exit /b 1
)
endlocal

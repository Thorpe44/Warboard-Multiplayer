@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V53 Recovery V2...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V53_RECOVERY_V2.ps1"
if errorlevel 1 (
    echo.
    echo V53 Recovery V2 reported an error.
    pause
    exit /b 1
)
endlocal

@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V53 Core Recovery V4...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V53_CORE_RECOVERY_V4.ps1"
if errorlevel 1 (
    echo.
    echo Core Recovery V4 reported an error.
    pause
    exit /b 1
)
endlocal

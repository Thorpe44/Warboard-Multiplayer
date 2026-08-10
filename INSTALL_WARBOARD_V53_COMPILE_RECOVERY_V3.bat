@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V53 Compile Recovery V3...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V53_COMPILE_RECOVERY_V3.ps1"
if errorlevel 1 (
    echo.
    echo V53 Compile Recovery V3 reported an error.
    pause
    exit /b 1
)
endlocal

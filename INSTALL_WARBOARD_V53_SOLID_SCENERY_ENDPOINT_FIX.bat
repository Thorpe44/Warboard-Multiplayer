@echo off
setlocal
cd /d "%~dp0"
echo.
echo Starting Warboard V53 solid scenery endpoint fix...
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V53_SOLID_SCENERY_ENDPOINT_FIX.ps1"
if errorlevel 1 (
    echo.
    echo V53 installer reported an error.
    pause
    exit /b 1
)
endlocal

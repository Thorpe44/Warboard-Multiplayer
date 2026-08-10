@echo off
setlocal
cd /d "%~dp0"
PowerShell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V51_TERRAIN_DEPLOYMENT_FIX.ps1"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
    echo V51 INSTALL FAILED with error %ERR%.
) else (
    echo V51 INSTALL COMPLETE.
)
pause
exit /b %ERR%

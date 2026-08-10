@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V50_TERRAIN_AREAS.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" echo Installer failed with exit code %ERR%.
pause
exit /b %ERR%

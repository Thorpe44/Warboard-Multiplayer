@echo off
setlocal
cd /d "%~dp0"
echo Starting WARBOARD v55a terrain-installer fix...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V55.ps1"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
    echo V55 INSTALL FAILED.
) else (
    echo V55 INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

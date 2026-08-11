@echo off
setlocal
cd /d "%~dp0"
echo Starting WARBOARD v57 terrain + deployment ghost install...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V57.ps1"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
    echo V57 INSTALL FAILED.
) else (
    echo V57 INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

@echo off
setlocal
cd /d "%~dp0"
echo Starting WARBOARD v51 gameplay/UI bugfix install...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V51.ps1"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
    echo V51 INSTALL FAILED.
    echo The error above will remain visible.
) else (
    echo V51 INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

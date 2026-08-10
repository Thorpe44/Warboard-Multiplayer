@echo off
setlocal
cd /d "%~dp0"
echo Starting WARBOARD v51a Windows line-ending-safe gameplay/UI bugfix install...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V51A.ps1"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
    echo V51A INSTALL FAILED.
    echo The error above will remain visible.
) else (
    echo V51A INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

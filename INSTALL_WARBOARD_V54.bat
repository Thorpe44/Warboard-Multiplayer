@echo off
setlocal
cd /d "%~dp0"
echo Starting WARBOARD v54b patcher return fix...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V54.ps1"
set "ERR=%ERRORLEVEL%"
echo.
if not "%ERR%"=="0" (
    echo V54 INSTALL FAILED.
) else (
    echo V54 INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

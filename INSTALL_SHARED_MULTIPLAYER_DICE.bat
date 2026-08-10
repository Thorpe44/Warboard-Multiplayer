@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - SHARED MULTIPLAYER DICE
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_SHARED_MULTIPLAYER_DICE.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo INSTALL FAILED - error code %ERR%
    echo Send ChatGPT a screenshot of this window.
) else (
    echo INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

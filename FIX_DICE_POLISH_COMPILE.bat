@echo off
setlocal
cd /d "%~dp0"
echo ============================================================
echo WARBOARD - DICE POLISH COMPILE HOTFIX
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0FIX_DICE_POLISH_COMPILE.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo HOTFIX FAILED - error code %ERR%
) else (
    echo HOTFIX COMPLETE.
)
echo.
pause
exit /b %ERR%

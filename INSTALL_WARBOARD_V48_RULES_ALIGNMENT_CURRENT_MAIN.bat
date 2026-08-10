@echo off
setlocal
cd /d "%~dp0"
echo.
echo ============================================
echo  WARBOARD v48 - 11E RULES ALIGNMENT PATCH
echo ============================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_V48_RULES_ALIGNMENT_CURRENT_MAIN.ps1"
set ERR=%ERRORLEVEL%
echo.
if not "%ERR%"=="0" (
    echo INSTALL FAILED with exit code %ERR%.
) else (
    echo INSTALL COMPLETE.
)
echo.
pause
exit /b %ERR%

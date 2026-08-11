@echo off
setlocal
cd /d "%~dp0"
echo Starting Warboard R28 unified model resolver install...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R28_UNIFIED_MODEL_RESOLVER.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
if not "%EXITCODE%"=="0" (
    echo R28 reported an error. Installer files were kept for inspection.
) else (
    echo R28 finished successfully.
)
pause
exit /b %EXITCODE%

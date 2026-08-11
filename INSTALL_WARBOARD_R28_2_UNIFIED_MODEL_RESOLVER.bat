@echo off
setlocal
cd /d "%~dp0"
echo Starting Warboard R28.2 unified model resolver install...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0INSTALL_WARBOARD_R28_2_UNIFIED_MODEL_RESOLVER.ps1"
set EXITCODE=%ERRORLEVEL%
echo.
if not "%EXITCODE%"=="0" (
    echo R28.2 reported an error. Resolver backup was restored.
) else (
    echo R28.2 finished successfully.
)
pause
exit /b %EXITCODE%

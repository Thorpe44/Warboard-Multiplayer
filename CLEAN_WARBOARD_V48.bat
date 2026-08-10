@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

echo ============================================================
echo WARBOARD - V48 AWARE SAFE CLEANUP
echo ============================================================
echo.

set "ROOT=%CD%"

:findroot
if exist "%ROOT%\Assets" if exist "%ROOT%\Packages" if exist "%ROOT%\ProjectSettings" goto found
for %%I in ("%ROOT%\..") do set "PARENT=%%~fI"
if /I "%PARENT%"=="%ROOT%" goto noroot
set "ROOT=%PARENT%"
goto findroot

:found
cd /d "%ROOT%"

echo Warboard root:
echo   %CD%
echo.
echo Verifying V48 installation before deleting its patch payload...
echo.

set "V48OK=0"

if exist "Assets\Scripts\Core\GameController.V48CoreAlignment.cs" (
    if exist "Assets\Scripts\Core\InteractiveAttackController.V48Alignment.cs" (
        findstr /c:"public const string Version = \"v48\";" "Assets\Scripts\Core\WarboardBuildInfo.cs" >nul 2>&1
        if not errorlevel 1 (
            set "V48OK=1"
        )
    )
)

if "%V48OK%"=="1" (
    echo [PASS] V48 is installed in Assets.
) else (
    echo [WARNING] Could not verify a complete V48 install.
    echo V48_PATCH_PAYLOAD will NOT be deleted.
)

echo.
set /a REMOVED=0

REM Root one-off installers / hotfixes / recovery scripts.
for %%F in (
    "INSTALL_*.bat"
    "INSTALL_*.ps1"
    "FIX_*.bat"
    "FIX_*.ps1"
    "RECOVER_*.bat"
    "RECOVER_*.ps1"
    "RESET_*.bat"
    "RESET_*.ps1"
) do (
    for %%G in (%%F) do (
        if exist "%%~fG" (
            if /I not "%%~nxG"=="CLEAN_WARBOARD_V48.bat" (
                del /q "%%~fG" >nul 2>&1
                if not exist "%%~fG" (
                    echo [REMOVED] %%~nxG
                    set /a REMOVED+=1
                )
            )
        )
    )
)

REM Patch readmes.
for %%F in (
    "README_V*.txt"
    "V*_README.txt"
    "README_DIRECT_RELAY_FIX.txt"
    "README_SHARED*.txt"
    "README_DICE*.txt"
    "README_MULTIPLAYER*.txt"
    "*SPLIT_INSTRUCTIONS.txt"
) do (
    for %%G in (%%F) do (
        if exist "%%~fG" (
            del /q "%%~fG" >nul 2>&1
            if not exist "%%~fG" (
                echo [REMOVED] %%~nxG
                set /a REMOVED+=1
            )
        )
    )
)

REM Generic installer README only.
if exist "README.txt" (
    findstr /i /c:"INSTALL" /c:"HOTFIX" /c:"RECOVERY" /c:"WARBOARD MULTIPLAYER" "README.txt" >nul 2>&1
    if not errorlevel 1 (
        del /q "README.txt" >nul 2>&1
        if not exist "README.txt" (
            echo [REMOVED] README.txt
            set /a REMOVED+=1
        )
    )
)

REM V48 payload: only after successful verification.
if "%V48OK%"=="1" (
    if exist "V48_PATCH_PAYLOAD" (
        rmdir /s /q "V48_PATCH_PAYLOAD"
        if not exist "V48_PATCH_PAYLOAD" (
            echo [REMOVED] V48_PATCH_PAYLOAD\
            set /a REMOVED+=1
        )
    )
)

REM Old patch ZIPs accidentally extracted/downloaded into repo.
for %%F in (
    "WARBOARD_V48_11E_RULES_ALIGNMENT_PATCH*.zip"
    "Warboard_*Fix*.zip"
    "Warboard_*Hotfix*.zip"
    "Warboard_*Recovery*.zip"
    "Warboard_*Polish*.zip"
    "Warboard_*Warning*.zip"
    "Warboard_*Compatibility*.zip"
    "Warboard_*Package*.zip"
) do (
    for %%G in (%%F) do (
        if exist "%%~fG" (
            del /q "%%~fG" >nul 2>&1
            if not exist "%%~fG" (
                echo [REMOVED] %%~nxG
                set /a REMOVED+=1
            )
        )
    )
)

REM Timestamped patch backups outside Library.
for /r "%ROOT%\Assets" %%G in (*.bak) do (
    if exist "%%~fG" (
        del /q "%%~fG" >nul 2>&1
        if not exist "%%~fG" (
            echo [REMOVED BACKUP] %%~fG
            set /a REMOVED+=1
        )
    )
)

for /r "%ROOT%\Assets" %%G in (*.bak.meta) do (
    if exist "%%~fG" (
        del /q "%%~fG" >nul 2>&1
        if not exist "%%~fG" (
            echo [REMOVED BACKUP META] %%~fG
            set /a REMOVED+=1
        )
    )
)

REM Installer marker files in root.
for %%F in (
    "*_INSTALLED.txt"
    "*_VERIFIED.txt"
) do (
    for %%G in (%%F) do (
        if exist "%%~fG" (
            del /q "%%~fG" >nul 2>&1
            if not exist "%%~fG" (
                echo [REMOVED] %%~nxG
                set /a REMOVED+=1
            )
        )
    )
)

echo.
echo ============================================================
echo CLEANUP COMPLETE
echo Removed !REMOVED! clutter item(s).
echo ============================================================
echo.
echo KEPT:
echo   Assets\ actual V48 game code
echo   GameController.V48CoreAlignment.cs
echo   InteractiveAttackController.V48Alignment.cs
echo   Multiplayer and shared dice code
echo   Packages\
echo   ProjectSettings\
echo   Library\
echo   Library\WarboardBackups\V48\  ^(rollback backup^)
echo.
echo The V48 installer payload was removed only if V48 was verified.
echo.
pause
exit /b 0

:noroot
echo ERROR: Could not find Unity project root.
echo Extract this ZIP into the Warboard project root and run it again.
echo.
pause
exit /b 1

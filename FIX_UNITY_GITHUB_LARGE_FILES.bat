@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo.
echo ============================================
echo   Warboard Unity GitHub Large File Fix
echo ============================================
echo.

set "ROOT=%CD%"
:findroot
if exist "%ROOT%\.git" goto found
for %%I in ("%ROOT%\..") do set "PARENT=%%~fI"
if /I "%PARENT%"=="%ROOT%" goto noroot
set "ROOT=%PARENT%"
goto findroot

:found
cd /d "%ROOT%"
echo Repo: %CD%
echo.

if not exist ".gitignore" type nul > ".gitignore"

findstr /x /c:"/[Ll]ibrary/" ".gitignore" >nul 2>&1 || (
  echo.>>".gitignore"
  echo # Unity generated folders - do not commit>>".gitignore"
  echo /[Ll]ibrary/>>".gitignore"
  echo /[Tt]emp/>>".gitignore"
  echo /[Oo]bj/>>".gitignore"
  echo /[Bb]uild/>>".gitignore"
  echo /[Bb]uilds/>>".gitignore"
  echo /[Ll]ogs/>>".gitignore"
  echo /[Uu]ser[Ss]ettings/>>".gitignore"
  echo /[Mm]emoryCaptures/>>".gitignore"
  echo /.vs/>>".gitignore"
  echo .idea/>>".gitignore"
  echo *.csproj>>".gitignore"
  echo *.sln>>".gitignore"
  echo *.suo>>".gitignore"
  echo *.tmp>>".gitignore"
  echo *.user>>".gitignore"
  echo *.userprefs>>".gitignore"
  echo *.pidb>>".gitignore"
  echo *.booproj>>".gitignore"
  echo *.svd>>".gitignore"
  echo *.pdb>>".gitignore"
  echo *.mdb>>".gitignore"
  echo *.opendb>>".gitignore"
  echo *.VC.db>>".gitignore"
)

echo Added Unity-generated folders to .gitignore.
echo.

git rm -r --cached --ignore-unmatch Library Temp Obj Build Builds Logs UserSettings MemoryCaptures .vs >nul 2>&1
git add .gitignore

echo Generated Unity cache folders are now ignored/untracked.
echo Your Assets, Packages and ProjectSettings folders are untouched.
echo.
echo NEXT:
echo   1. Open GitHub Desktop.
echo   2. The huge SearchIndexArtifactImporter file should be gone.
echo   3. Commit the remaining changes normally.
echo   4. Push.
echo.
echo If GitHub Desktop still shows a file over 100 MB,
echo send a screenshot showing its FULL PATH.
echo.
pause
exit /b 0

:noroot
echo ERROR: Could not find a .git folder.
echo Put this BAT inside your Warboard repository folder and run it again.
echo.
pause
exit /b 1

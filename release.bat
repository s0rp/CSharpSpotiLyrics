@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title CSharpSpotiLyrics Release Manager

:MENU
cls
echo =======================================================
echo          CSharpSpotiLyrics Release Manager
echo =======================================================
echo.
echo  [1] Normal Release           (main branch  - Stable vX.Y.Z)
echo  [2] Canary Release           (canary branch - Pre-release vX.Y.Z-canary.N)
echo  [3] Merge Canary into Main   (Merge Canary -^> Main)
echo  [4] Clean Canary Tags        (Delete Canary Tags ^& Releases)
echo  [5] Sync Git Repository      (Fetch ^& Pull)
echo  [6] Exit
echo.
set /p CHOICE="Please select an option (1-6): "

if "%CHOICE%"=="1" goto STABLE_RELEASE
if "%CHOICE%"=="2" goto CANARY_RELEASE
if "%CHOICE%"=="3" goto MERGE_CANARY
if "%CHOICE%"=="4" goto CLEAN_CANARY
if "%CHOICE%"=="5" goto SYNC_GIT
if "%CHOICE%"=="6" exit /b
goto MENU

:STABLE_RELEASE
set TARGET_BRANCH=main
set IS_CANARY=0
echo.
echo === [ NORMAL / STABLE RELEASE ] ===
goto PROCESS_RELEASE

:CANARY_RELEASE
set TARGET_BRANCH=canary
set IS_CANARY=1
echo.
echo === [ CANARY / PRE-RELEASE ] ===
goto PROCESS_RELEASE

:PROCESS_RELEASE
echo.
echo [1/6] Checking git status...
git rev-parse --is-inside-work-tree >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Current directory is not a Git repository!
    pause
    goto MENU
)

:: Ensure current work is committed or stashed
git diff-index --quiet HEAD -- 2>nul
if %errorlevel% neq 0 (
    echo [WARNING] You have uncommitted changes. Staging all changes first...
)

:: Branch checkout / creation
echo [2/6] Switching to branch '%TARGET_BRANCH%'...
git checkout %TARGET_BRANCH% 2>nul
if %errorlevel% neq 0 (
    echo Branch '%TARGET_BRANCH%' not found locally. Creating from current state...
    git checkout -b %TARGET_BRANCH%
)

:: Version Prompt
echo.
if "%IS_CANARY%"=="0" (
    echo Example Stable Version: 2.0.3
    set /p RAW_VER="Enter release version (e.g. 2.0.3): "
    set TAG_NAME=v!RAW_VER!
    set CLEAN_VER=!RAW_VER!
) else (
    echo Example Canary Version: 2.1.0-canary.1
    set /p RAW_VER="Enter canary version (e.g. 2.1.0-canary.1): "
    set TAG_NAME=v!RAW_VER!
    for /f "tokens=1 delims=-" %%a in ("!RAW_VER!") do set CLEAN_VER=%%a
)

if "%RAW_VER%"=="" (
    echo [ERROR] Version cannot be empty!
    pause
    goto MENU
)

:: Automatically update Version tags in .csproj files
echo.
echo [3/6] Updating .csproj files with version !RAW_VER!...
powershell -Command "(Get-Content CSharpSpotiLyrics\CSharpSpotiLyrics.csproj) -replace '<Version>.*?</Version>', '<Version>%RAW_VER%</Version>' -replace '<AssemblyVersion>.*?</AssemblyVersion>', '<AssemblyVersion>%CLEAN_VER%.0</AssemblyVersion>' -replace '<FileVersion>.*?</FileVersion>', '<FileVersion>%CLEAN_VER%.0</FileVersion>' | Set-Content CSharpSpotiLyrics\CSharpSpotiLyrics.csproj"
powershell -Command "(Get-Content CSharpSpotiLyricsCLI\CSharpSpotiLyricsCLI.csproj) -replace '<Version>.*?</Version>', '<Version>%RAW_VER%</Version>' -replace '<AssemblyVersion>.*?</AssemblyVersion>', '<AssemblyVersion>%CLEAN_VER%.0</AssemblyVersion>' -replace '<FileVersion>.*?</FileVersion>', '<FileVersion>%CLEAN_VER%.0</FileVersion>' | Set-Content CSharpSpotiLyricsCLI\CSharpSpotiLyricsCLI.csproj"

:: Commit Message
set /p COMMIT_MSG="Commit message (Press Enter for 'Release %TAG_NAME%'): "
if "%COMMIT_MSG%"=="" set COMMIT_MSG=Release %TAG_NAME%

echo.
echo [4/6] Staging and committing changes...
git add .
git commit -m "%COMMIT_MSG%"

echo.
echo [5/6] Pushing branch '%TARGET_BRANCH%' to GitHub...
git push -u origin %TARGET_BRANCH%

echo.
echo [6/6] Creating and pushing git tag '%TAG_NAME%'...
git tag -a %TAG_NAME% -m "%COMMIT_MSG%"
git push origin %TAG_NAME%

echo.
echo =======================================================
echo  RELEASE PIPELINE TRIGGERED SUCCESSFULLY!
echo  Tag: %TAG_NAME%
echo =======================================================
echo.
pause
goto MENU

:MERGE_CANARY
echo.
echo =======================================================
echo          Merge Canary into Main Branch
echo =======================================================
echo.
echo WARNING: This will merge changes from 'canary' into 'main'.
echo Only run this AFTER you have made and tested commits on 'canary'.
echo.
set /p PROCEED="Do you want to continue? (Y/N): "
if /i not "%PROCEED%"=="Y" (
    echo Merge cancelled.
    pause
    goto MENU
)

echo.
echo [1/3] Switching to 'main' branch...
git checkout main
if %errorlevel% neq 0 (
    echo [ERROR] Could not switch to 'main'!
    pause
    goto MENU
)
git pull origin main 2>nul

echo.
echo [2/3] Merging 'canary' into 'main'...
git merge canary -m "chore: merge canary updates into main"
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Merge conflict detected!
    echo Please resolve the conflicts manually.
    pause
    goto MENU
)

echo.
echo [3/3] Pushing updated 'main' branch to GitHub...
git push origin main

echo.
echo =======================================================
echo  [SUCCESS] Canary merged into Main successfully!
echo  Now choose [1] to release a Stable version.
echo =======================================================
echo.
pause
goto MENU

:CLEAN_CANARY
echo.
echo =======================================================
echo          Canary Tags ^& Releases Cleaner
echo =======================================================
echo.
echo WARNING: This will permanently delete ALL git tags and
echo GitHub releases containing 'canary' from local and remote!
echo.
set /p CONFIRM="Are you sure you want to proceed? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo Operation cancelled.
    pause
    goto MENU
)

echo.
echo [1/2] Finding and deleting Canary tags/releases...
where gh >nul 2>&1
if %errorlevel% equ 0 (
    echo GitHub CLI (gh) found. Deleting both GitHub Releases and remote tags...
    powershell -Command "git tag -l '*canary*' | ForEach-Object { Write-Host 'Deleting: ' $_; gh release delete $_ --cleanup-tag -y 2>$null; git tag -d $_ 2>$null; git push origin --delete $_ 2>$null }"
) else (
    echo GitHub CLI (gh) not found. Deleting local and remote git tags only...
    powershell -Command "git tag -l '*canary*' | ForEach-Object { Write-Host 'Deleting Tag: ' $_; git tag -d $_; git push origin --delete $_ }"
)

echo.
echo [2/2] Cleanup completed!
echo.
pause
goto MENU

:SYNC_GIT
echo.
echo Syncing Git repository...
git fetch --all --prune
git pull
echo.
echo Synchronization completed.
pause
goto MENU
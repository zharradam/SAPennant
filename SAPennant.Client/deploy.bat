@echo off
REM Delayed expansion is deliberately OFF so "[!]" prints literally.
setlocal EnableExtensions
title SAPennant manual client deploy

REM ===========================================================================
REM  FALLBACK ONLY - normally you do NOT need this.
REM
REM  Pushing to main deploys the front end automatically via
REM  .github/workflows/deploy-client.yml. Use this script only when GitHub
REM  Actions is unavailable and you need to publish the client by hand.
REM
REM  It mirrors the workflow's steps: npm ci -> npm run build (prebuild stamps
REM  the commit, postbuild writes the per-route pages) -> CNAME, 404.html
REM  SPA fallback, sitemap.xml and the Google verification file -> gh-pages.
REM
REM  NOTE: this publishes your WORKING TREE, not what is on origin/main, so
REM  uncommitted work can reach production without being in git.
REM
REM  It deploys the front end only. The API deploys separately from
REM  .github/workflows/main_sapennant-api.yml when you push.
REM ===========================================================================

echo.
echo  SAPennant - MANUAL client deploy (fallback)
echo  ===========================================
echo.

cd /d "%~dp0"

REM ---- warn about anything that makes this differ from a workflow deploy ----
set "_warn="

for /f %%i in ('git status --porcelain 2^>nul') do set "_warn=1"
if defined _warn (
    echo  [!] You have UNCOMMITTED changes. They will be published.
)

for /f %%i in ('git log origin/main..HEAD --oneline 2^>nul') do set "_ahead=1"
if defined _ahead (
    echo  [!] You have commits that are not pushed to origin/main.
)

set "_branch="
for /f "usebackq delims=" %%b in (`git rev-parse --abbrev-ref HEAD 2^>nul`) do set "_branch=%%b"
if not "%_branch%"=="main" (
    echo  [!] You are on branch "%_branch%", not main.
)

echo.
set /p "_go=Deploy the current working tree to sapennantgolf.com? (y/N): "
if /I not "%_go%"=="y" (
    echo  Cancelled - nothing was deployed.
    goto :end
)

echo.
echo  Installing dependencies (npm ci)...
call npm ci
if errorlevel 1 (
    echo  npm ci failed - aborting.
    goto :end
)

echo.
echo  Building...
call npm run build
if errorlevel 1 (
    echo  Build failed - aborting.
    goto :end
)

set "_out=dist\SAPennant\browser"

if not exist "%_out%\index.html" (
    echo  Build produced no index.html - aborting.
    goto :end
)

echo.
echo  Adding SPA fallback and root files...
(echo sapennantgolf.com)> "%_out%\CNAME"
copy /y "%_out%\index.html" "%_out%\404.html" >nul
copy /y "..\sitemap.xml" "%_out%\sitemap.xml" >nul
copy /y "..\google4134bd2769fffc0b.html" "%_out%\google4134bd2769fffc0b.html" >nul

echo.
echo  Publishing to gh-pages...
call npx angular-cli-ghpages --dir=%_out% --cname=sapennantgolf.com
if errorlevel 1 (
    echo  Publish failed.
    goto :end
)

echo.
echo  Done. https://sapennantgolf.com ^(GitHub Pages can take a minute^)

:end
echo.
pause

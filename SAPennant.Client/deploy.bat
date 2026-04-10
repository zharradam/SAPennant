@echo off
echo Building...
cmd /c ng build --configuration production --base-href "https://sapennantgolf.com/"
echo Build step complete.
if exist "dist\SAPennant\browser\index.html" (
  echo sapennantgolf.com> dist\SAPennant\browser\CNAME
  echo Deploying to GitHub Pages...
  cmd /c npx angular-cli-ghpages --dir=dist/SAPennant/browser
  echo Done.
) else (
  echo Build failed - index.html not found. Aborting.
)
pause
@echo off
echo Building...
ng build --configuration production --base-href "https://zharradam.github.io/SAPennant/"

if %ERRORLEVEL% NEQ 0 (
  echo Build failed. Aborting deploy.
  exit /b 1
)

echo Deploying to GitHub Pages...
npx angular-cli-ghpages --dir=dist/SAPennant/browser

echo Done.
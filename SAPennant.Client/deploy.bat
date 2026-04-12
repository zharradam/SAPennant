@echo off
echo Building...
cmd /c npm run build
echo Build step complete.
if exist "dist\SAPennant\browser\index.html" (
  echo sapennantgolf.com> dist\SAPennant\browser\CNAME
  copy "..\google4134bd2769fffc0b.html" "dist\SAPennant\browser\google4134bd2769fffc0b.html"
  copy "..\sitemap.xml" "dist\SAPennant\browser\sitemap.xml"
  echo Deploying to GitHub Pages...
  cmd /c npx angular-cli-ghpages --dir=dist/SAPennant/browser
  echo Done.
) else (
  echo Build failed - index.html not found. Aborting.
)
pause
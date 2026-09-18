@echo off
setlocal EnableExtensions
title SAPennant dev launcher

REM Starts the API (https://localhost:7007) and the Angular front end
REM (http://localhost:4200), each in its own window, then opens the browser.
REM Safe to run repeatedly: anything already listening is left alone, so this
REM co-exists with an API you started in Visual Studio.

echo.
echo  SAPennant dev launcher
echo  ----------------------

call :isListening 7007
if "%ERRORLEVEL%"=="0" (
    echo  [skip]  API already running on port 7007
) else (
    echo  [start] API      -^> https://localhost:7007
    start "SAPennant API" cmd /k "cd /d "%~dp0SAPennant.API" && dotnet run --launch-profile https"
)

call :isListening 4200
if "%ERRORLEVEL%"=="0" (
    echo  [skip]  Front end already running on port 4200
) else (
    echo  [start] Frontend -^> http://localhost:4200
    start "SAPennant Client" cmd /k "cd /d "%~dp0SAPennant.Client" && npm start"
)

echo.
echo  Waiting for the front end to compile...

REM Poll for up to ~90 seconds; the first Angular build takes a little while.
set /a _tries=0
:wait
call :isListening 4200
if "%ERRORLEVEL%"=="0" goto ready
set /a _tries+=1
if %_tries% GEQ 45 goto timedout
call :sleep 2
goto wait

:ready
echo  Ready. Opening http://localhost:4200
start "" http://localhost:4200
goto done

:timedout
echo  Front end did not come up in time - check the "SAPennant Client" window
echo  for build errors, then browse to http://localhost:4200 yourself.

:done
echo.
echo  Close the "SAPennant API" and "SAPennant Client" windows to stop them.
echo.
call :sleep 5
exit /b 0

REM ---- helpers -------------------------------------------------------------
REM Sets ERRORLEVEL 0 when something is LISTENING on the given port, 1 if not.
:isListening
netstat -an | findstr /C:":%~1 " | findstr /I "LISTENING" >nul 2>&1
exit /b %ERRORLEVEL%

REM Wait N seconds. Uses ping rather than timeout, which refuses to run when
REM the script's output is redirected or piped.
:sleep
ping -n %~1 127.0.0.1 >nul 2>&1
exit /b 0

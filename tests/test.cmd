@echo off
rem Builds and runs the automated checks. During the typing test a small window appears for a couple
rem of seconds and the program types into it by itself: do not touch keyboard or mouse.
rem "test.cmd ci", as GitHub runs it before a release: without the typing test, which needs a desktop to
rem type into, and without the pauses. A check that crashes ends with a negative code, which "if errorlevel 1"
rem would take for success: hence "neq 0".
setlocal
set HERE=%~dp0
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
rem As in build.cmd: the Windows Runtime types used from the Store package and to read the text of pictures.
for %%I in ("%CSC%") do set FRAMEWORK=%%~dpI
set META=%WINDIR%\System32\WinMetadata\Windows
set WINRT=/r:"%META%.ApplicationModel.winmd" /r:"%META%.Foundation.winmd" /r:"%META%.Globalization.winmd" ^
  /r:"%META%.Graphics.winmd" /r:"%META%.Media.winmd" /r:"%META%.Security.winmd" /r:"%META%.Storage.winmd" ^
  /r:"%FRAMEWORK%System.Runtime.dll"

echo Compilo i controlli...
"%CSC%" /nologo /codepage:65001 /target:exe /main:TestProgram /out:"%HERE%tests.exe" ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll %WINRT% "%HERE%..\src\*.cs" "%HERE%Test.cs"
if %errorlevel% neq 0 goto failed

echo.
echo Controllo la mappa dei tasti...
"%HERE%tests.exe" map
if %errorlevel% neq 0 goto failed

echo.
echo Controllo l'elenco dei testi...
"%HERE%tests.exe" texts
if %errorlevel% neq 0 goto failed

echo.
echo Controllo il database...
"%HERE%tests.exe" data
if %errorlevel% neq 0 goto failed
if /i "%~1"=="ci" goto passed

echo.
echo Controllo la digitazione...
"%HERE%tests.exe" type
if %errorlevel% neq 0 goto failed

:passed
echo.
echo TUTTI I CONTROLLI SUPERATI.
if /i not "%~1"=="ci" pause
exit /b 0

:failed
echo.
echo CONTROLLI NON SUPERATI.
if /i not "%~1"=="ci" pause
exit /b 1

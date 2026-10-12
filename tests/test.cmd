@echo off
rem Builds and runs the automated checks. During the typing test a small window appears for a couple
rem of seconds and the program types into it by itself: do not touch keyboard or mouse.
setlocal
set NAME=PasswordPaste
set HERE=%~dp0
set APP=%~dp0..\%NAME%
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

echo Compilo i controlli...
"%CSC%" /nologo /codepage:65001 /target:exe /main:TestProgram /out:"%HERE%tests.exe" ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll "%APP%.cs" "%HERE%Test.cs"
if errorlevel 1 goto failed

echo.
echo Controllo la mappa dei tasti...
"%HERE%tests.exe" map
if errorlevel 1 goto failed

echo.
echo Controllo l'elenco dei testi...
"%HERE%tests.exe" texts
if errorlevel 1 goto failed

echo.
echo Controllo la digitazione...
"%HERE%tests.exe" type
if errorlevel 1 goto failed

echo.
echo TUTTI I CONTROLLI SUPERATI.
pause
exit /b 0

:failed
echo.
echo CONTROLLI NON SUPERATI.
pause
exit /b 1

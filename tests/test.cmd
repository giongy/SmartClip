@echo off
rem Compila ed esegue i controlli automatici. Durante la prova di digitazione compare per un paio
rem di secondi una finestrella in cui il programma scrive da solo: non toccare tastiera e mouse.
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

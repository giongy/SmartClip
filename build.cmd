@echo off
rem Builds with the C# compiler included in Windows (.NET Framework 4), without installing anything.
setlocal
set NAME=SmartClip
set APP=%~dp0%NAME%
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

rem The icon is optional: without the .ico file the program uses the default one.
set ICON=
if exist "%APP%.ico" set ICON=/win32icon:"%APP%.ico" /resource:"%APP%.ico",%NAME%.ico

echo Compilo %NAME%.cs...
if defined ICON (echo Icona: %NAME%.ico) else (echo Icona: quella predefinita, %NAME%.ico non trovato)

"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /out:"%APP%.exe" %ICON% ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll "%APP%.cs"
if errorlevel 1 goto failed

echo.
echo Fatto: creato %APP%.exe
pause
exit /b 0

:failed
echo.
echo COMPILAZIONE NON RIUSCITA. Se %NAME% e' aperto, chiudilo e riprova.
pause
exit /b 1

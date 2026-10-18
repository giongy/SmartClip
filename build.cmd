@echo off
rem Builds with the C# compiler included in Windows (.NET Framework 4), without installing anything.
setlocal
set NAME=SmartClip
set APP=%~dp0%NAME%
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
rem The Windows Runtime types used from the Store package (src\Packaged.cs) and to read the text of pictures
rem (src\Ocr.cs): their metadata comes with Windows 10 and 11, the facade beside the compiler.
for %%I in ("%CSC%") do set FRAMEWORK=%%~dpI
set META=%WINDIR%\System32\WinMetadata\Windows
set WINRT=/r:"%META%.ApplicationModel.winmd" /r:"%META%.Foundation.winmd" /r:"%META%.Globalization.winmd" ^
  /r:"%META%.Graphics.winmd" /r:"%META%.Media.winmd" /r:"%META%.Security.winmd" /r:"%META%.Storage.winmd" ^
  /r:"%FRAMEWORK%System.Runtime.dll"

rem The icon is optional: without the .ico file the program uses the default one.
set ICON=
if exist "%APP%.ico" set ICON=/win32icon:"%APP%.ico" /resource:"%APP%.ico",%NAME%.ico

echo Compilo src\*.cs...
if defined ICON (echo Icona: %NAME%.ico) else (echo Icona: quella predefinita, %NAME%.ico non trovato)

"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /out:"%APP%.exe" %ICON% ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll %WINRT% "%~dp0src\*.cs"
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

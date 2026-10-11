@echo off
rem Compila con il compilatore C# incluso in Windows (.NET Framework 4), senza installare nulla.
setlocal
set NAME=PasswordPaste
set APP=%~dp0%NAME%
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

rem L'icona e' facoltativa: senza il file .ico il programma usa quella predefinita.
set ICON=
if exist "%APP%.ico" set ICON=/win32icon:"%APP%.ico" /resource:"%APP%.ico",%NAME%.ico

"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /out:"%APP%.exe" %ICON% ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll "%APP%.cs"
if errorlevel 1 pause

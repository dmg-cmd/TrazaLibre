@echo off
setlocal
cd /d "%~dp0"

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WPF=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF

if not exist bin mkdir bin

echo Compilando Trazalibre en UTF-8 con icono oficial...
"%CSC%" /target:winexe /optimize+ /platform:anycpu ^
    /codepage:65001 ^
    /win32icon:src\app.ico ^
    /r:"%WPF%\PresentationCore.dll" ^
    /r:"%WPF%\PresentationFramework.dll" ^
    /r:"%WPF%\WindowsBase.dll" ^
    /r:System.dll ^
    /r:System.Drawing.dll ^
    /r:System.Xaml.dll ^
    /out:bin\Trazalibre.exe ^
    /recurse:src\*.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo [EXITO] Binario modular generado: bin\Trazalibre.exe
    echo ========================================================
) else (
    echo.
    echo [ERROR] Fallo la compilacion de Trazalibre.
)

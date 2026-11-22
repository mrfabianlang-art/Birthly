@echo off
cd /d "%~dp0"
echo Compiling LiquidGlassWidget...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /optimize+ /out:"LiquidGlassWidget.exe" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "Birthly.cs"
if %ERRORLEVEL% EQU 0 (
    echo Build succeeded! Launching LiquidGlassWidget.exe...
    start "" "LiquidGlassWidget.exe"
) else (
    echo Build failed.
    pause
)

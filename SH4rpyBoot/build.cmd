@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe

%CSC% /nologo /target:winexe /optimize+ /platform:anycpu ^
  /win32manifest:app.manifest ^
  /out:SH4rpyBoot.exe ^
  /main:SH4rpyBoot.Program ^
  /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Management.dll ^
  /r:lib\MetroFramework.dll ^
  src\Program.cs src\Native.cs src\Proc.cs src\UsbDetector.cs src\DiskOps.cs src\RawWriter.cs src\WindowsMaker.cs src\MainForm.cs

if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)

copy /y lib\MetroFramework.dll . >nul
copy /y lib\MetroFramework.Fonts.dll . >nul
echo Build OK: SH4rpyBoot.exe
exit /b 0

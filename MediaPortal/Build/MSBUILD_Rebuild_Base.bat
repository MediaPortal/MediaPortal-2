@echo off
rem Prefer newest MSBUILD version of VS 2017, standalone package or by Community edition.
rem See https://aka.ms/vs/15/release/vs_buildtools.exe for 15.0 (VS 2017) standalone installer
set                   MB="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBUILD.exe"
if not exist %MB% set MB="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBUILD.exe"
if not exist %MB% set MB="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBUILD.exe"
if not exist %MB% set MB="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBUILD.exe"

if not exist %MB% echo "No supported MSBUILD version found. Exiting here." && exit 1
echo Start building using %MB%

%MB% RestorePackages.targets /target:RestoreBuildPackages

set PathToBuildReport=.\..\Packages\BuildReport.1.0.0
xcopy /I /Y %PathToBuildReport%\_BuildReport_Files .\_BuildReport_Files

set xml=%1.xml
set html=%1.html

set logger=/l:XmlFileLogger,"%PathToBuildReport%\MSBuild.ExtensionPack.Loggers.dll";logfile=%xml%
%MB% /m Build.proj %logger% %2

%PathToBuildReport%\msxsl %xml% _BuildReport_Files\BuildReport.xslt -o %html%

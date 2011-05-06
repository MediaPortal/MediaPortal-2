@echo off

set ProgramDir=%ProgramFiles%
if exist "%ProgramFiles(x86)%" set ProgramDir=%ProgramFiles(x86)%

"%ProgramDir%\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com" /rebuild Release ..\Tools\BuildReport\BuildReport.sln

xcopy /Y ..\Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\Tools\BuildReport\images\*.* .\images\

"%ProgramDir%\Microsoft Visual Studio 10.0\Common7\IDE\devenv.com" ..\Source\MP2-Client.sln /Rebuild "Release|x86" > VS2010_Build_Report_Release_x86.txt

..\Tools\BuildReport\bin\x86\Release\BuildReport.exe --vs2010 --Input=VS2010_Build_Report_Release_x86.txt --Output=VS2010_Build_Report_Release_x86.html --Solution=MP2-Client.sln --Title="MediaPortal 2 Client - Build Report"
@echo off

call FindProgramDir.bat

"%ProgramDir%\Microsoft Visual Studio 9.0\Common7\IDE\devenv.com" /rebuild Release ..\MediaPortal.Tools\BuildReport\BuildReport.sln

xcopy /Y ..\MediaPortal.Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\MediaPortal.Tools\BuildReport\images\*.* .\images\

"%ProgramDir%\Microsoft Visual Studio 9.0\Common7\IDE\devenv.com" ..\MediaPortal-VS2008.sln /Rebuild "Release|x86" > VS2008_Build_Report_Release_x86.txt

..\MediaPortal.Tools\BuildReport\bin\Release\BuildReport.exe /VS2008 /Input=VS2008_Build_Report_Release_x86.txt /Output=VS2008_Build_Report_Release_x86.html /Solution=MediaPortal-VS2008.sln /Title="MediaPortal II - Build Report"
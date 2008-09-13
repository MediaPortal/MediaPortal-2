@ECHO OFF

call FindProgramDir.bat

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" /rebuild Release ..\MediaPortal.Tools\BuildReport\BuildReport.sln

xcopy /Y ..\MediaPortal.Tools\BuildReport\css\*.* .\css\
xcopy /Y ..\MediaPortal.Tools\BuildReport\images\*.* .\images\

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" ..\MediaPortal-VS2005.sln /Rebuild "Release|x86" > VS2005_Build_Report_Release_x86.txt

..\MediaPortal.Tools\BuildReport\bin\Release\BuildReport.exe /VS2005 /Input=VS2005_Build_Report_Release_x86.txt /Output=VS2005_Build_Report_Release_x86.html /Solution=MediaPortal-VS2005.sln /Title="MediaPortal II - Build Report"
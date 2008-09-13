@echo off

call FindProgramDir.bat

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" ..\MediaPortal-VS2005.sln /Rebuild "Release|x86" /Out build.log
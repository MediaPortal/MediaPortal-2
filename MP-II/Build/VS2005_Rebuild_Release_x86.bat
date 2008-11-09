@echo off

call FindProgramDir.bat

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" ..\Source\MP-II-Client-VS2005.sln /Rebuild "Release|x86" /Out build.log
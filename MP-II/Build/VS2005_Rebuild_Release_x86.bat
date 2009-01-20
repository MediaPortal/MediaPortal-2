@echo off

set ProgramDir=%ProgramFiles%
if exist "%ProgramFiles(x86)%" set ProgramDir=%ProgramFiles(x86)%

"%ProgramDir%\Microsoft Visual Studio 8\Common7\IDE\devenv.com" ..\Source\MP-II-Client-VS2005.sln /Rebuild "Release|x86" /Out build.log
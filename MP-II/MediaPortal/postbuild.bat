REM %1 = Solution Directory
REM %2 = $(ConfigurationName) Debug/Release

REM Check for Microsoft Antispyware .BAT bug
if exist .\kernel32.dll exit 1

echo %2
del /Q models\*.* 
del /Q skin\default\Scripts\*.* 
del /Q skin\default\Scripts\Precompiled\*.* 
xcopy %1\MediaPortal.Base\*.* . /E /Y /D
copy %1\vmr9Helper\release\vmr9helper.dll .
REM ------------------------- Models ------------------------------------------
copy %1\MediaPortal.Models\helloworld\bin\x86\%2\myhelloworld*.* models
copy %1\MediaPortal.Models\movies\bin\x86\%2\mymovies*.* models
copy %1\MediaPortal.Models\pictures\bin\x86\%2\mypic*.* models
copy %1\MediaPortal.Models\music\bin\x86\%2\mymusic*.* models
copy %1\MediaPortal.Models\media\bin\x86\%2\mymedia*.* models
copy %1\MediaPortal.Models\weather\bin\x86\%2\myweather*.* models
copy %1\MediaPortal.Models\login\bin\x86\%2\mylogin*.* models
copy %1\MediaPortal.Models\menu\bin\x86\%2\menu*.* models
copy %1\MediaPortal.Models\Settings\bin\x86\%2\Settings*.* models
copy %1\MediaPortal.Models\PlayList\bin\x86\%2\PlayList*.* models
copy %1\MediaPortal.Models\extensions\bin\x86\%2\myextensions*.* models
copy %1\MediaPortal.Models\shares\bin\x86\%2\shares*.* models
REM ---------------------- End of Models ------------------------------------
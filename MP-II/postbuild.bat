REM %1 = Solution Directory
REM %2 = $(ConfigurationName) Debug/Release

REM Check for Microsoft Antispyware .BAT bug
if exist .\kernel32.dll exit 1

echo %2
del /Q models\*.* 
del /Q skin\default\Scripts\*.* 
del /Q skin\default\Scripts\Precompiled\*.* 
xcopy %1\MediaPortal.Base\*.* . /E /Y /D
copy %1\mymovies\bin\x86\%2\mymovies*.* models
copy %1\mypictures\bin\x86\%2\mypic*.* models
copy %1\mymusic\bin\x86\%2\mymusic*.* models
copy %1\mymedia\bin\x86\%2\mymedia*.* models
copy %1\myweather\bin\x86\%2\myweather*.* models
copy %1\mylogin\bin\x86\%2\mylogin*.* models
copy %1\menu\bin\x86\%2\menu*.* models
copy %1\Settings\bin\x86\%2\Settings*.* models
copy %1\PlayList\bin\x86\%2\PlayList*.* models
copy %1\vmr9Helper\release\vmr9helper.dll .
copy %1\myextensions\bin\x86\%2\myextensions*.* models
copy %1\shares\bin\x86\%2\shares*.* models
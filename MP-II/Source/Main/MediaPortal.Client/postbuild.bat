@REM Parameter 1: Solution source directory, needs to have a trailing \
@REM Parameter 2: Target build directory, needs to have a trailing \

set solution_dir=%~f1.
set target_dir=%~f2.

echo Solution directory = %solution_dir%, target directory = %target_dir%
RoboCopy "%solution_dir%\Base\Client" "%target_dir%" /E /NP /XD .svn
copy "%solution_dir%\Ui\Players\DShowHelper\release\DShowHelper.dll" "%target_dir%"

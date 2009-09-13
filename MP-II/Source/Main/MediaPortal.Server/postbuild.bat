@REM Parameter 1: Solution source directory, needs to have a trailing \
@REM Parameter 2: Target build directory, needs to have a trailing \

set solution_dir=%~f1.
set target_dir=%~f2.

echo Solution directory = %solution_dir%, target directory = %target_dir%
robocopy "%solution_dir%\Base\Server" "%target_dir%" /E /XD .svn

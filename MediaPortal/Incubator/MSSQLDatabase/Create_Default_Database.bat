@ECHO OFF
Sqlcmd.exe -E -S .\SQLExpress -i "%~dp0\Database-create-script.sql"
IF ERRORLEVEL 1 GOTO error
GOTO done

:error
ECHO Failed to create the database
goto end

:done 
net stop MSSQL$SQLEXPRESS
net start MSSQL$SQLEXPRESS
ECHO Database created successfully

:end
PAUSE

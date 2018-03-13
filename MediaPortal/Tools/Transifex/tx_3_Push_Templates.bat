call tx_0_initbuild.bat || exit 1
%MB% ..\..\Build\Transifex.targets /target:UploadEnglishLanguageFiles /l:FileLogger,Microsoft.Build.Engine;logfile=tx_3_Push_Templates.log

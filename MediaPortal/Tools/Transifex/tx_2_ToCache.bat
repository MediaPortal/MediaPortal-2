call tx_0_initbuild.bat || exit 1
%MB% ..\..\Build\Transifex.targets /target:CopyEnglishLanguageFiles /l:FileLogger,Microsoft.Build.Engine;logfile=tx_2_ToCache.log

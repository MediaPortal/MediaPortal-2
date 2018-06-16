call tx_0_initbuild.bat || exit 1
%MB% ..\..\Build\Transifex.targets /target:CopyTranslations /l:FileLogger,Microsoft.Build.Engine;logfile=tx_6_FromCache.log

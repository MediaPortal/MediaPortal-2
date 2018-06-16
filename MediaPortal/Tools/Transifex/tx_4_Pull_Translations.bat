call tx_0_initbuild.bat || exit 1
%MB% ..\..\Build\Transifex.targets /target:DownloadTranslations /l:FileLogger,Microsoft.Build.Engine;logfile=tx_4_Pull_Translations.log

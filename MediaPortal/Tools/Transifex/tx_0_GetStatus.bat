call tx_0_initbuild.bat || exit 1
%MB% ..\..\Build\Transifex.targets /target:DownloadTransifexClient
tx status

pause
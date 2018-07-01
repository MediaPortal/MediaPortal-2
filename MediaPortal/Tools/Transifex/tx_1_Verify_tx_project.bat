call tx_0_initbuild.bat || exit 1
%MB% ..\..\Build\Transifex.targets /target:DownloadTransifexClient
%MB% ..\..\Build\Transifex.targets /target:BuildTransifexHelper
..\TransifexHelper\bin\x86\Release\TransifexHelper.exe --verify -t "%cd%\..\.." > verify.log

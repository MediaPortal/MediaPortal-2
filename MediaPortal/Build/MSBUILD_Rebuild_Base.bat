set MB="%ProgramFiles(x86)%\MSBUILD\12.0\Bin\MSBUILD.exe"

%MB% RestorePackages.targets /target:RestoreBuildPackages

set PathToBuildReport=.\..\Packages\BuildReport.1.0.0
xcopy /I /Y %PathToBuildReport%\_BuildReport_Files .\_BuildReport_Files

set xml=%1.xml
set html=%1.html

set logger=/l:XmlFileLogger,"%PathToBuildReport%\MSBuild.ExtensionPack.Loggers.dll";logfile=%xml%
%MB% /m Build.proj %logger% %2

%PathToBuildReport%\msxsl %xml% _BuildReport_Files\BuildReport.xslt -o %html%

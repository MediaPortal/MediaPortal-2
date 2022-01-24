using System;
using System.IO;

namespace MP2BootstrapperApp.ChainPackages
{
  public class Vc2019x64 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2019x64(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      // ToDO: We should probably block this x64 package entirely earlier
      // on in the installation if this is a 32 bit OS.
      if (!Environment.Is64BitOperatingSystem)
        return new Version();

      const string mfc140Dll = "mfc140.dll";
      string systemDirectory;
      // Installer is x64, system directory will point to the correct x64 system directory for the x64 version of the dll. 
      if (Environment.Is64BitProcess)
        systemDirectory = Environment.SystemDirectory;
      else //Installer is x86, so we need to use the special sysnative path to get the x64 system directory
        systemDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");

      string vc2019Path = Path.Combine(systemDirectory, mfc140Dll);

      if (!_packageChecker.Exists(vc2019Path))
      {
        return new Version();
      }
      int majorVersion = _packageChecker.GetFileMajorVersion(vc2019Path);
      int minorVersion = _packageChecker.GetFileMinorVersion(vc2019Path);
      int buildVersion = _packageChecker.GetFileBuildVersion(vc2019Path);
      int revision = _packageChecker.GetFilePrivateVersion(vc2019Path);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}

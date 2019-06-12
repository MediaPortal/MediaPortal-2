using System;
using System.IO;

namespace MP2BootstrapperApp.ChainPackages
{
  public class Vc2017 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2017(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      const string mfc140Dll = "mfc140.dll";
      string vc2017Path = Path.Combine(Environment.SystemDirectory, mfc140Dll);

      if (!_packageChecker.Exists(vc2017Path))
      {
        return new Version();
      }
      int majorVersion = _packageChecker.GetFileMajorVersion(vc2017Path);
      int minorVersion = _packageChecker.GetFileMinorVersion(vc2017Path);
      int buildVersion = _packageChecker.GetFileBuildVersion(vc2017Path);
      int revision = _packageChecker.GetFilePrivateVersion(vc2017Path);
      
      return new Version(majorVersion, minorVersion, buildVersion, revision);
    }
  }
}

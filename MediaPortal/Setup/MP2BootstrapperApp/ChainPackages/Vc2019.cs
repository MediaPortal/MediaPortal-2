using System;
using System.IO;

namespace MP2BootstrapperApp.ChainPackages
{
  public class Vc2019 : IPackage
  {
    private readonly IPackageChecker _packageChecker;

    public Vc2019(IPackageChecker packageChecker)
    {
      _packageChecker = packageChecker;
    }

    public Version GetInstalledVersion()
    {
      const string mfc140Dll = "mfc140.dll";
      string vc2019Path = Path.Combine(Environment.SystemDirectory, mfc140Dll);

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

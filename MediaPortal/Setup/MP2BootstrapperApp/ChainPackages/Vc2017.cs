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

    public bool IsInstalled()
    {
      string vc2017Path = Path.Combine(Environment.SystemDirectory, "mfc140.dll");

      if (!_packageChecker.Exists(vc2017Path))
      {
        return false;
      }
      return _packageChecker.IsEqualOrHigherVersion(vc2017Path, new Version(14, 11, 25325, 0));
    }
  }
}

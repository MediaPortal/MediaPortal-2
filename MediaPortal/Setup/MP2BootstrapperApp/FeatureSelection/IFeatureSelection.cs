using MP2BootstrapperApp.ChainPackages;
using MP2BootstrapperApp.Models;
using System.Collections.Generic;

namespace MP2BootstrapperApp.FeatureSelection
{
  public interface IFeatureSelection
  {
    ISet<string> ExcludeFeatures { get; }
    ISet<PackageId> ExcludePackages { get; }

    void SetInstallType(IEnumerable<BundlePackage> bundlePackages);
  }
}

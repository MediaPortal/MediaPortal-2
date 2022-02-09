using MP2BootstrapperApp.ChainPackages;
using System.Collections.Generic;

namespace MP2BootstrapperApp.FeatureSelection
{
  public interface IFeatureSelection
  {
    ISet<string> ExcludeFeatures { get; }
    ISet<PackageId> ExcludePackages { get; }
  }
}
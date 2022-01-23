
using MP2BootstrapperApp.Models;
using System.Collections.ObjectModel;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallationInProgressStep : AbstractInstallStep, IStep
  {
    public InstallationInProgressStep(ReadOnlyCollection<BundlePackage> bundlePackages)
      : base(bundlePackages)
    {
    }

    public IStep Next()
    {
      // not allowed
      return null;
    }

    public bool CanGoNext()
    {
      return false;
    }

    public bool CanGoBack()
    {
      return false;
    }
  }
}


using MP2BootstrapperApp.Models;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallationInProgressStep : AbstractInstallStep, IStep
  {
    public InstallationInProgressStep(IBootstrapperApplicationModel bootstrapperApplicationModel)
      : base(bootstrapperApplicationModel)
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

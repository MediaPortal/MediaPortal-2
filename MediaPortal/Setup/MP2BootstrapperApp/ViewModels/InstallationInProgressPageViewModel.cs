
using MP2BootstrapperApp.WizardSteps;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallationInProgressPageViewModel : InstallWizardPageViewModelBase
  {
    public InstallationInProgressPageViewModel(InstallationInProgressStep step)
      : base(step)
    {
    }

    protected override void UpdateWizardViewModel(InstallWizardViewModel viewModel)
    {
      base.UpdateWizardViewModel(viewModel);
      viewModel.Header = "Installing";
      viewModel.ButtonNextContent = "Install";
    }
  }
}

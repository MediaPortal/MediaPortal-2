
using MP2BootstrapperApp.ViewModels;

namespace MP2BootstrapperApp.WizardSteps
{
  public class InstallationInProgressStep : IStep
  {
    public InstallationInProgressStep(InstallWizardViewModel viewModel)
    {
      InstallWizardViewModel wizardViewModel = viewModel;
      wizardViewModel.CurrentPage = new InstallationInProgressPageViewModel(viewModel);
      wizardViewModel.Install();
    }
    
    public void Next(Wizard wizard)
    {
      // not allowed
    }

    public void Back(Wizard wizard)
    {
      // not allowed
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

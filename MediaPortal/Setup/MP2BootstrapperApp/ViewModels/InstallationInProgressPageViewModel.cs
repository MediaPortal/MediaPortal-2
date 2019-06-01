using System.Collections.ObjectModel;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.ViewModels
{
  public class InstallationInProgressPageViewModel : InstallWizardPageViewModelBase
  {
    public InstallationInProgressPageViewModel(InstallWizardViewModel viewModel)
    {
      viewModel.Header = "Installing";
      viewModel.ButtonNextContent = "Install";
      viewModel.ButtonBackContent = "Back";
      viewModel.ButtonCancelContent = "Abort";
    }
  }
}

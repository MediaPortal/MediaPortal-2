using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BootstrapperWrapper;
using System.Collections.ObjectModel;
using System.Windows;

namespace MP2BootstrapperApp.Models
{
  public interface IBootstrapperApplicationModel
  {
    IBootstrapperApp BootstrapperApplication { get; }
    ReadOnlyCollection<BundlePackage> BundlePackages { get; }
    int FinalResult { get; set; }
    void SetWindowHandle(Window view);
    void PlanAction(LaunchAction action);
    void ApplyAction();
    void LogMessage(LogLevel logLevel, string message);
  }
}

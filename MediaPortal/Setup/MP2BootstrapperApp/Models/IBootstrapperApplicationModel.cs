using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.BootstrapperWrapper;
using System.Collections.ObjectModel;
using System.Windows;

namespace MP2BootstrapperApp.Models
{
  public interface IBootstrapperApplicationModel
  {
    /// <summary>
    /// The main installation engine.
    /// </summary>
    IBootstrapperApp BootstrapperApplication { get; }

    /// <summary>
    /// The packages included in the bundle.
    /// </summary>
    ReadOnlyCollection<BundlePackage> BundlePackages { get; }

    /// <summary>
    /// The detected state of this bundle.
    /// <see cref="DetectionState.Absent"/> if no related bundles are installed,
    /// <see cref="DetectionState.Newer"/> if this is a newer bundle than that installed,
    /// <see cref="DetectionState.Older"/> if this is an older bundle than that installed,
    /// <see cref="DetectionState.Present"/> if this same bundle is already installed.
    /// </summary>
    DetectionState DetectionState { get; }

    /// <summary>
    /// The action that has been planned.
    /// </summary>
    LaunchAction PlannedAction { get; }

    /// <summary>
    /// The current state of the bootstrapper application.
    /// </summary>
    InstallState InstallState { get; }

    /// <summary>
    /// Whether the user has requested the cancellation of the installation.
    /// </summary>
    bool Cancelled { get; set; }

    /// <summary>
    /// The final result of the apply phase, returned to the engine on close.
    /// </summary>
    int FinalResult { get; }

    /// <summary>
    /// Sets the window handle of the main window, passed to the engine.
    /// </summary>
    /// <param name="view"></param>
    void SetWindowHandle(Window view);

    /// <summary>
    /// Plans the specified action and applies it if the plan was successful.
    /// </summary>
    /// <param name="action"></param>
    void PlanAction(LaunchAction action);

    /// <summary>
    /// Logs the specified message to the setup's log file.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    void LogMessage(LogLevel logLevel, string message);
  }
}

using System;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public abstract class BootstrapperApplicationWrapper : BootstrapperApplication, IBootstrapperApp
  {
    protected override void OnStartup(StartupEventArgs args)
    {
      Engine.Log(LogLevel.Debug, $"BootstrapperApplication Startup: Action: {Command.Action}, Display: {Command.Display}, Command Line Args: {string.Join(",", Command.GetCommandLineArgs())}");
      LaunchAction = Command.Action;
      Display = Command.Display;
      CommandLineArguments = Command.GetCommandLineArgs();
      base.OnStartup(args);
    }

    public LaunchAction LaunchAction { get; protected set; }

    public Display Display { get; protected set; }

    public string[] CommandLineArguments { get; protected set; }

    public void Detect()
    {
      Engine.Detect();
    }

    public void Plan(LaunchAction action)
    {
      Engine.Plan(action);
    }

    public void Apply(IntPtr hwndParent)
    {
      Engine.Apply(hwndParent);
    }

    public void Log(LogLevel level, string message)
    {
      Engine.Log(level, message);
    }
  }
}

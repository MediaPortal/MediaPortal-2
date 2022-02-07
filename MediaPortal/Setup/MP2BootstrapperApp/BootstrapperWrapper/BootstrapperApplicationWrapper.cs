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

    public event EventHandler<DetectBeginEventArgs> WrapperDetectBegin
    {
      add { DetectBegin += value; }
      remove { DetectBegin -= value; }
    }

    public event EventHandler<DetectRelatedBundleEventArgs> WrapperDetectRelatedBundle
    {
      add { DetectRelatedBundle += value; }
      remove { DetectRelatedBundle -= value; }
    }

    public event EventHandler<DetectPackageCompleteEventArgs> WrapperDetectPackageComplete
    {
      add { DetectPackageComplete += value; }
      remove { DetectPackageComplete -= value; }
    }

    public event EventHandler<DetectCompleteEventArgs> WrapperDetectComplete
    {
      add { DetectComplete += value; }
      remove { DetectComplete -= value; }
    }

    public event EventHandler<PlanBeginEventArgs> WrapperPlanBegin
    {
      add { PlanBegin += value; }
      remove { PlanBegin -= value; }
    }

    public event EventHandler<PlanCompleteEventArgs> WrapperPlanComplete
    {
      add { PlanComplete += value; }
      remove { PlanComplete -= value; }
    }

    public event EventHandler<ApplyCompleteEventArgs> WrapperApplyComplete
    {
      add { ApplyComplete += value; }
      remove { ApplyComplete -= value; }
    }

    public event EventHandler<ApplyBeginEventArgs> WrapperApplyBegin
    {
      add { ApplyBegin += value; }
      remove { ApplyBegin -= value; }
    }

    public event EventHandler<ExecutePackageBeginEventArgs> WrapperExecutePackageBegin
    {
      add { ExecutePackageBegin += value; }
      remove { ExecutePackageBegin -= value; }
    }

    public event EventHandler<ExecutePackageCompleteEventArgs> WrapperExecutePackageComplete
    {
      add { ExecutePackageComplete += value; }
      remove { ExecutePackageComplete -= value; }
    }

    public event EventHandler<PlanPackageBeginEventArgs> WrapperPlanPackageBegin
    {
      add { PlanPackageBegin += value; }
      remove { PlanPackageBegin -= value; }
    }

    public event EventHandler<ResolveSourceEventArgs> WrapperResolveSource
    {
      add { ResolveSource += value; }
      remove { ResolveSource -= value; }
    }

    public event EventHandler<ApplyPhaseCountArgs> WrapperApplyPhaseCount
    {
      add { ApplyPhaseCount += value; }
      remove { ApplyPhaseCount -= value; }
    }

    public event EventHandler<CacheAcquireProgressEventArgs> WrapperCacheAcquireProgress
    {
      add { CacheAcquireProgress += value; }
      remove { CacheAcquireProgress -= value; }
    }

    public event EventHandler<ExecuteProgressEventArgs> WrapperExecuteProgress
    {
      add { ExecuteProgress += value; }
      remove { ExecuteProgress -= value; }
    }

    public event EventHandler<ErrorEventArgs> WrapperError
    {
      add { Error += value; }
      remove { Error -= value; }
    }
  }
}

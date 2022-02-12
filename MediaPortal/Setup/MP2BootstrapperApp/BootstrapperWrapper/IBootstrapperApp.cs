using System;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public interface IBootstrapperApp
  {
    LaunchAction LaunchAction { get; }
    Display Display { get; }
    string[] CommandLineArguments { get; }

    void Detect();
    void Plan(LaunchAction action);
    void Apply(IntPtr hwndParent);
    void Log(LogLevel level, string message);

    event EventHandler<DetectBeginEventArgs> DetectBegin;
    event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;
    event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;
    event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;
    event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;
    event EventHandler<DetectCompleteEventArgs> DetectComplete;
    event EventHandler<PlanBeginEventArgs> PlanBegin;
    event EventHandler<PlanCompleteEventArgs> PlanComplete;
    event EventHandler<ApplyCompleteEventArgs> ApplyComplete;
    event EventHandler<ApplyBeginEventArgs> ApplyBegin;
    event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;
    event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;
    event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;
    event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;
    event EventHandler<ResolveSourceEventArgs> ResolveSource;
    event EventHandler<ApplyPhaseCountArgs> ApplyPhaseCount;
    event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;
    event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;
    event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;
    event EventHandler<ErrorEventArgs> Error;
  }
}

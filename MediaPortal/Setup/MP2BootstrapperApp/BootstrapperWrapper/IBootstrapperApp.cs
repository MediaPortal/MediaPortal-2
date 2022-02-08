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

    event EventHandler<DetectBeginEventArgs> WrapperDetectBegin;
    event EventHandler<DetectRelatedBundleEventArgs> WrapperDetectRelatedBundle;
    event EventHandler<DetectMsiFeatureEventArgs> WrapperDetectMsiFeature;
    event EventHandler<DetectPackageCompleteEventArgs> WrapperDetectPackageComplete;
    event EventHandler<DetectCompleteEventArgs> WrapperDetectComplete;
    event EventHandler<PlanBeginEventArgs> WrapperPlanBegin;
    event EventHandler<PlanCompleteEventArgs> WrapperPlanComplete;
    event EventHandler<ApplyCompleteEventArgs> WrapperApplyComplete;
    event EventHandler<ApplyBeginEventArgs> WrapperApplyBegin;
    event EventHandler<ExecutePackageBeginEventArgs> WrapperExecutePackageBegin;
    event EventHandler<ExecutePackageCompleteEventArgs> WrapperExecutePackageComplete;
    event EventHandler<PlanPackageBeginEventArgs> WrapperPlanPackageBegin;
    event EventHandler<PlanMsiFeatureEventArgs> WrapperPlanMsiFeature;
    event EventHandler<ResolveSourceEventArgs> WrapperResolveSource;
    event EventHandler<ApplyPhaseCountArgs> WrapperApplyPhaseCount;
    event EventHandler<CacheAcquireProgressEventArgs> WrapperCacheAcquireProgress;
    event EventHandler<ExecuteProgressEventArgs> WrapperExecuteProgress;
    event EventHandler<ErrorEventArgs> WrapperError;
  }
}

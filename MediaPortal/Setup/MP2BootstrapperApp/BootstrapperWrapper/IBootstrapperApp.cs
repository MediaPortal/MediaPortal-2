using System;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public interface IBootstrapperApp
  {
    Engine Engine { get; }
    Command Command { get; }
    event EventHandler<DetectBeginEventArgs> WrapperDetectBegin;
    event EventHandler<DetectRelatedBundleEventArgs> WrapperDetectRelatedBundle;
    event EventHandler<DetectPackageCompleteEventArgs> WrapperDetectPackageComplete;
    event EventHandler<DetectCompleteEventArgs> WrapperDetectComplete;
    event EventHandler<PlanCompleteEventArgs> WrapperPlanComplete;
    event EventHandler<ApplyCompleteEventArgs> WrapperApplyComplete;
    event EventHandler<ApplyBeginEventArgs> WrapperApplyBegin;
    event EventHandler<ExecutePackageBeginEventArgs> WrapperExecutePackageBegin;
    event EventHandler<ExecutePackageCompleteEventArgs> WrapperExecutePackageComplete;
    event EventHandler<PlanPackageBeginEventArgs> WrapperPlanPackageBegin;
    event EventHandler<ResolveSourceEventArgs> WrapperResolveSource;
    event EventHandler<ApplyPhaseCountArgs> WrapperApplyPhaseCount;
    event EventHandler<CacheAcquireProgressEventArgs> WrapperCacheAcquireProgress;
    event EventHandler<ExecuteProgressEventArgs> WrapperExecuteProgress;
  }
}

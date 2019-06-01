using System;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public interface IBootstrapperApp
  {
    Engine Engine { get; }
    event EventHandler<DetectRelatedBundleEventArgs> WrapperDetectRelatedBundle;
    event EventHandler<DetectPackageCompleteEventArgs> WrapperDetectPackageComplete;
    event EventHandler<PlanCompleteEventArgs> WrapperPlanComplete;
    event EventHandler<ApplyCompleteEventArgs> WrapperApplyComplete;
    event EventHandler<ApplyBeginEventArgs> WrapperApplyBegin;
    event EventHandler<ExecutePackageBeginEventArgs> WrapperExecutePackageBegin;
    event EventHandler<ExecutePackageCompleteEventArgs> WrapperExecutePackageComplete;
    event EventHandler<PlanPackageBeginEventArgs> WrapperPlanPackageBegin;
    event EventHandler<ResolveSourceEventArgs> WrapperResolveSource;
    event EventHandler<CacheAcquireProgressEventArgs> WrapperCacheAcquireProgress;
    event EventHandler<ExecuteProgressEventArgs> WrapperExecuteProgress;
  }
}

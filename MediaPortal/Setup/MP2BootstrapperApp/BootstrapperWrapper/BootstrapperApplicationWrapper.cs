using System;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public abstract class BootstrapperApplicationWrapper : BootstrapperApplication, IBootstrapperApp
  {
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
  }
}

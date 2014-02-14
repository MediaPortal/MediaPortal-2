using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement
{
  class ImporterWorkerNewGen : IImporterWorker, IDisposable
  {
    #region Interface implementations

    #region IImporterWorker implementation

    public bool IsSuspended
    {
      get
      {
        return false;
      }
      internal set
      {
        if (value)
        {
          
        }
      }
    }

    public ICollection<ImportJobInformation> ImportJobs
    {
      get
      {
        return new List<ImportJobInformation>();
      }
    }

    public void Startup()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Starting up...");
    }

    public void Shutdown()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Shutting down...");
    }

    public void Activate(IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Activating...");
    }

    public void Suspend()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Suspendind...");
    }

    public void CancelPendingJobs()
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Cancelling pending jobs...");
    }

    public void CancelJobsForPath(ResourcePath path)
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Cancelling jobs for path '{0}'...", path);
    }

    public void ScheduleImport(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Scheduling import for path '{0}', MediaCategories: '{1}', {2}including subdirectories...", path, String.Join(",", mediaCategories), includeSubDirectories ? "" : "not ");
    }

    public void ScheduleRefresh(ResourcePath path, IEnumerable<string> mediaCategories, bool includeSubDirectories)
    {
      ServiceRegistration.Get<ILogger>().Info("ImporterWorker: Scheduling refresh for path '{0}', MediaCategories: '{1}', {2}including subdirectories...", path, String.Join(",", mediaCategories), includeSubDirectories ? "" : "not ");
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
    }

    #endregion

    #endregion
  }
}

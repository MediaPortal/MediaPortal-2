#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// An <see cref="ImportJobController"/> is responsible for handling a single ImportJob
  /// </summary>
  /// <remarks>
  /// It holds the TPL Datflow network for the ImportJob and is responsible for cancelling
  /// and suspending the particular ImportJob. If it determines that the <see cref="ImporterWorkerNewGen"/>
  /// shoud be suspended (e.g. because on the MP2 Client side the MP2 Server is disconnected) it notifies the
  /// <see cref="ImporterWorkerNewGen"/> (_parent) which will then make sure that all <see cref="ImportJobController"/>s
  /// are suspended.
  /// ToDo: Handle cancellation with a <see cref="CancellationToken"/>
  /// ToDo: Handle suspension
  /// ToDo: Make this class (or a separate status class) serializable by the XMLSerializer to be able to save the status on shutdown
  /// </remarks>
  public class ImportJobController
  {
    #region Variables

    private readonly ImportJobInformation _importJobInformation;
    private readonly ImporterWorkerNewGen _parentImporterWorker;
    private readonly TaskCompletionSource<object> _tcs;
    private readonly int _importJobNumber;

    private readonly ConcurrentDictionary<ResourcePath, PendingImportResourceNewGen> _pendingImportResources;

    private DirectoryUnfoldBlock _directoryUnfoldBlock;

    #endregion

    #region Constructor

    public ImportJobController(ImportJobInformation importJobInformation, int importJobNumber, ImporterWorkerNewGen parentImporterWorker)
    {
      _importJobInformation = importJobInformation;
      _importJobNumber = importJobNumber;
      _parentImporterWorker = parentImporterWorker;

      _pendingImportResources = new ConcurrentDictionary<ResourcePath, PendingImportResourceNewGen>();
      _tcs = new TaskCompletionSource<object>();

      SetupDataflowBlocks();
      LinkDataflowBlocks();

      // Todo: This continuation shall happen after the last DataflowBlock has finished
      _directoryUnfoldBlock.Completion.ContinueWith(OnFinished);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Returns a <see cref="Task"/> that represents the status of the ImportJob
    /// </summary>
    public Task Completion
    {
      get { return _tcs.Task; }      
    }

    #endregion

    #region Public methods

    public void Cancel()
    {      
    }

    public void Suspend()
    {      
    }

    public void RegisterPendingImportResource(PendingImportResourceNewGen pendingImportResource)
    {
      if (!_pendingImportResources.TryAdd(pendingImportResource.ResourceAccessor.CanonicalLocalResourcePath, pendingImportResource))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: Could not register {1}", this, pendingImportResource);
    }

    public void UnregisterPendingImportResource(PendingImportResourceNewGen pendingImportResource)
    {
      PendingImportResourceNewGen removedPendingImportResource;
      if(!_pendingImportResources.TryRemove(pendingImportResource.ResourceAccessor.CanonicalLocalResourcePath, out removedPendingImportResource))
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: Could not unregister {1}", this, pendingImportResource);
    }

    #endregion

    #region Private methods

    private void OnFinished(Task previousTask)
    {
      if (_pendingImportResources.Count > 0)
      {
        // The ImportJob has finished, but we have PendingImportJobResources left that have not been disposed.
        // This should only happen when the ImportJob finishes in cancelled oder faulted state. When the ImportJob
        // ran to completion, the DataflowBlocks should have disposed all the PendingImportResources.
        if(!previousTask.IsCanceled && !previousTask.IsFaulted)
          ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0}: The ImportJob ran to completion but there are {1} undisposed PendingImportResources left. Disposing them now...", this, _pendingImportResources.Count);
        
        var pendingImportReouces = new List<PendingImportResourceNewGen>(_pendingImportResources.Values);
        foreach (var pendingImportResource in pendingImportReouces)
          pendingImportResource.Dispose();
      }

      if (previousTask.IsFaulted)
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(previousTask.Exception);
      else if (previousTask.IsCanceled)
        _tcs.SetCanceled();
      else
        _tcs.SetResult(null);
    }

    /// <summary>
    /// Instantiates all the necessary DataflowBlocks for the given ImportJob
    /// </summary>
    /// <remarks>
    /// ToDo: We need to handle three cases here:
    /// - BasePath points to a single resource
    /// - BasePath points to a directory which is not a single resource and the ImportJob does not include subdirectories
    /// - BasePath points to a directory which is not a single resource and the ImportJob does include subdirectories
    /// </remarks>
    private void SetupDataflowBlocks()
    {
      _directoryUnfoldBlock = new DirectoryUnfoldBlock(_importJobInformation.BasePath, this);
    }

    private void LinkDataflowBlocks()
    {
      _directoryUnfoldBlock.LinkTo(DataflowBlock.NullTarget<PendingImportResourceNewGen>());
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return String.Format("ImportJob #{0}", _importJobNumber);
    }

    #endregion
  }
}

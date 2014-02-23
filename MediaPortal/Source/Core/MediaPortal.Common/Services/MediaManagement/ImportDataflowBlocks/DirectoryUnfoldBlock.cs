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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes one directory and provides this directory and all its direct and indirect subdirectories
  /// except those that are below single resource directory (like e.g. a DVD directory)
  /// </summary>
  /// <remarks>
  /// Uses a TransformBlock and recursively posts the subdirectories of a given directory to this block
  /// ToDo: Add an IsSingleResource method to the IMetadatExtractor interface and all its implementations
  /// ToDo: If at least one of the MetadataExtractors to be applied returns true, the directory is
  /// ToDo: treated as a single resource, not as a directory containing sub-items or subdirectories.
  /// ToDo: Handle Cancellation
  /// ToDo: Handle Suspension (This DataflowBlock is quick, so most likely we cancel and start over again on re-activation
  /// ToDo: or we could even block suspension until this DataflowBlock has finished)
  /// </remarks>
  class DirectoryUnfoldBlock : ISourceBlock<PendingImportResourceNewGen>
  {
    #region Variables

    private readonly TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> _innerBlock;
    private readonly Task _completion;
    private readonly int _maxDegreeOfParallelism;
    private readonly Stopwatch _stopWatch;
    private int _directoriesProcessed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates and starts the DirectoryUnfoldBlock
    /// </summary>
    /// <param name="path">Root path of the unfolding process</param>
    /// <remarks>
    /// <param name="path"></param> must point to a resource (a) for which we can create an IFileSystemResourceAccessor
    /// and (b) which is a directory
    /// </remarks>
    public DirectoryUnfoldBlock(ResourcePath path)
    {
      if (path.IsNetworkResource)
        _maxDegreeOfParallelism = Environment.ProcessorCount * 10;
      else
        _maxDegreeOfParallelism = Environment.ProcessorCount;
      
      _innerBlock = new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(p => ProcessDirectory(p), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism });
      _completion = _innerBlock.Completion.ContinueWith(OnFinished, TaskContinuationOptions.AttachedToParent);
      IResourceAccessor ra;
      path.TryCreateLocalResourceAccessor(out ra);
      var fsra = ra as IFileSystemResourceAccessor;
      var rootImportResource = new PendingImportResourceNewGen(null, fsra);

      _stopWatch = Stopwatch.StartNew();
      _innerBlock.Post(rootImportResource);
    }

    #endregion

    #region Private methods

    private PendingImportResourceNewGen ProcessDirectory(PendingImportResourceNewGen importResource)
    {
      Interlocked.Increment(ref _directoriesProcessed);

      //ToDo: Only do this if Directory is NOT a single resource (such as a DVD directory)
      importResource.IsIngleResource = false;

      if (!importResource.IsIngleResource)
      {
        ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(importResource.ResourceAccessor, false);
        if (directories != null)
          foreach (var subDirectory in directories)
            _innerBlock.Post(new PendingImportResourceNewGen(importResource.ResourceAccessor, subDirectory));
      }

      if (_innerBlock.InputCount == 0)
        _innerBlock.Complete();

      // ToDo: Remove this - just here to free the resources for now
      importResource.ResourceAccessor.Dispose();

      return importResource;
    }

    /// <summary>
    /// Runs when the _innerBlock is finished
    /// </summary>
    /// <remarks>
    /// We just log a short message on the status of the _innerBlock. Potential exceptions are not logged, but
    /// rethrown here so that the ImportJobController and finally the ImporterWorker know
    /// about the status of the _innerBlock. The exceptions themselves are logged by ImporterWorker.
    /// </remarks>
    /// <param name="previousTask">_innerBlock.Completion</param>
    private void OnFinished(Task previousTask)
    {
      _stopWatch.Stop();

      if (previousTask.IsFaulted)
        ServiceRegistration.Get<ILogger>().Error("DirectoryUnfoldBlock: Error while unfolding {0} directories; time elapsed: {1}; MaxDegreeOfParallelism = {2}", _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
      else if (previousTask.IsCanceled)
        ServiceRegistration.Get<ILogger>().Info("DirectoryUnfoldBlock: Canceled after unfolding {0} directories; time elapsed: {1}; MaxDegreeOfParallelism = {2}", _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
      else
        ServiceRegistration.Get<ILogger>().Info("DirectoryUnfoldBlock: Unfolded {0} directories; time elapsed: {1}; MaxDegreeOfParallelism = {2}", _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
 
      // We rethrow potential exceptions from previousTask (i.e. our _innerBlock.Completion task)
      // to make the ImporterWorker aware of the status of this ImportJob
      // ToDo: When creating this task, we need to pass the same CancelationToken we passed to the previousTask
      // ToDo: so that this task gets a canceled status not a faulted status when we throw the OperationCanceledException here
      if (previousTask.Exception != null)
        throw previousTask.Exception;
    }

    #endregion

    #region Interface implementations

    public void Complete()
    {
      _innerBlock.Complete();
    }

    public void Fault(Exception exception)
    {
      (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).Fault(exception);
    }

    public Task Completion
    {
      get { return _completion; }
    }

    public IDisposable LinkTo(ITargetBlock<PendingImportResourceNewGen> target, DataflowLinkOptions linkOptions)
    {
      return _innerBlock.LinkTo(target, linkOptions);
    }

    public PendingImportResourceNewGen ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target, out bool messageConsumed)
    {
      return (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      return (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).ReserveMessage(messageHeader, target);
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      (_innerBlock as ISourceBlock<PendingImportResourceNewGen>).ReleaseReservation(messageHeader, target);
    }

    #endregion
  }
}

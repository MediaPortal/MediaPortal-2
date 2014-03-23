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
  /// except those that are below single resource directories (like e.g. a DVD directory)
  /// </summary>
  /// <remarks>
  /// Uses a BufferBlock to keep the <see cref="PendingImportResourceNewGen"/>s to be processed when the
  /// <see cref="ImporterWorkerNewGen"/> is suspended. The actual work is done in a TransformBlock
  /// to which the subdirectories of a given directory are posted recursively. The TransformBlock must be
  /// run with a MaxDegreeOfParallelism of 1 to ensure that the PendingImportResourceNumber of the
  /// PendingImportResources to be processed are in the right order also after this ImportJob was persisted
  /// to disk and reloaded. The reason is that we have to make sure that parent directories are always saved
  /// to the database before their respective child directories as we store the parent directory's MediaItemId
  /// in the respective child directory's MediaItem and we only get this MediaItemId when the parent directory's
  /// MediaItem was stored in the database.
  /// ToDo: Add an IsSingleResource method to the IMetadatExtractor interface and all its implementations
  ///       If at least one of the MetadataExtractors to be applied returns true, the directory is
  ///       treated as a single resource, not as a directory containing sub-items or subdirectories.
  /// ToDo: Handle database-deletion of no longer existing directories during refresh imports
  /// </remarks>
  class DirectoryUnfoldBlock : ISourceBlock<PendingImportResourceNewGen>
  {
    #region Variables

    private readonly BufferBlock<PendingImportResourceNewGen> _suspensionBufferBlock;
    private readonly TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> _innerBlock;
    private IDisposable _suspensionLink;
    private readonly ImportJobController _parentImportJobController;
    private readonly TaskCompletionSource<object> _tcs;
    private readonly int _maxDegreeOfParallelism;
    private readonly Stopwatch _stopWatch;
    private int _directoriesProcessed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the DirectoryUnfoldBlock
    /// </summary>
    /// <param name="ct">CancellationToken used to cancel this block</param>
    /// <param name="parentImportJobController">ImportJobController to which this DirectoryUnfoldBlock belongs</param>
    public DirectoryUnfoldBlock(CancellationToken ct, ImportJobController parentImportJobController)
    {
      _parentImportJobController = parentImportJobController;
      _maxDegreeOfParallelism = 1;
      
      _tcs = new TaskCompletionSource<object>();
      _suspensionBufferBlock = new BufferBlock<PendingImportResourceNewGen>(new DataflowBlockOptions { CancellationToken = ct });
      _innerBlock = new TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(p => ProcessDirectory(p), new ExecutionDataflowBlockOptions { BoundedCapacity = _maxDegreeOfParallelism, MaxDegreeOfParallelism = _maxDegreeOfParallelism, CancellationToken = ct });
      _innerBlock.Completion.ContinueWith(OnFinished);

      _stopWatch = new Stopwatch();
    }

    #endregion

    #region Private methods

    private IEnumerable<PendingImportResourceNewGen> ProcessDirectory(PendingImportResourceNewGen importResource)
    {
      try
      {
        Interlocked.Increment(ref _directoriesProcessed);

        //ToDo: Only do this if Directory is NOT a single resource (such as a DVD directory)
        importResource.IsSingleResource = false;

        if (!importResource.IsSingleResource)
        {
          ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(importResource.ResourceAccessor, false);
          if (directories != null)
            foreach (var subDirectory in directories)
              _suspensionBufferBlock.Post(new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, subDirectory, PendingImportResourceNewGen.DataflowNetworkPosition.None, _parentImportJobController));
        }

        if (_suspensionBufferBlock.Count == 0)
          _suspensionBufferBlock.Complete();

        importResource.LastFinishedBlock = PendingImportResourceNewGen.DataflowNetworkPosition.DirectoryUnfoldBlock;

        // ToDo: Remove this and do it later
        importResource.Dispose();

        return new HashSet<PendingImportResourceNewGen> { importResource };
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker / {0} / DirectoryUnfoldBlock: Error while processing {1}", ex, _parentImportJobController, importResource);
        return new HashSet<PendingImportResourceNewGen>();
      }
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
      {
        ServiceRegistration.Get<ILogger>().Error("ImporterWorker / {0} / DirectoryUnfoldBlock: Error while unfolding {1} directories; time elapsed: {2}; MaxDegreeOfParallelism = {3}", _parentImportJobController, _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
        // ReSharper disable once AssignNullToNotNullAttribute
        _tcs.SetException(previousTask.Exception);
      }
      else if (previousTask.IsCanceled)
      {
        ServiceRegistration.Get<ILogger>().Debug("ImporterWorker / {0} / DirectoryUnfoldBlock: Canceled after unfolding {1} directories; time elapsed: {2}; MaxDegreeOfParallelism = {3}", _parentImportJobController, _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
        _tcs.SetCanceled();
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Debug("ImporterWorker / {0} / DirectoryUnfoldBlock: Unfolded {1} directories; time elapsed: {2}; MaxDegreeOfParallelism = {3}", _parentImportJobController, _directoriesProcessed, _stopWatch.Elapsed, _maxDegreeOfParallelism);
        _tcs.SetResult(null);
      }
    }

    #endregion

    #region Public methods

    public void Activate()
    {
      _suspensionLink = _suspensionBufferBlock.LinkTo(_innerBlock, new DataflowLinkOptions { PropagateCompletion = true });
      _stopWatch.Start();
    }

    public void Suspend()
    {
      if (_suspensionLink != null)
      {
        _suspensionLink.Dispose();
        _suspensionLink = null;
      }
      _stopWatch.Stop();
    }

    public bool Post(PendingImportResourceNewGen importResource)
    {
      return _suspensionBufferBlock.Post(importResource);
    }

    #endregion

    #region Interface implementations

    public void Complete()
    {
      _suspensionBufferBlock.Complete();
    }

    void IDataflowBlock.Fault(Exception exception)
    {
      (_suspensionBufferBlock as IDataflowBlock).Fault(exception);
    }

    public Task Completion
    {
      get { return _tcs.Task; }
    }

    public IDisposable LinkTo(ITargetBlock<PendingImportResourceNewGen> target, DataflowLinkOptions linkOptions)
    {
      return _innerBlock.LinkTo(target, linkOptions);
    }

    PendingImportResourceNewGen ISourceBlock<PendingImportResourceNewGen>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target, out bool messageConsumed)
    {
      return (_suspensionBufferBlock as ISourceBlock<PendingImportResourceNewGen>).ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    bool ISourceBlock<PendingImportResourceNewGen>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      return (_suspensionBufferBlock as ISourceBlock<PendingImportResourceNewGen>).ReserveMessage(messageHeader, target);
    }

    void ISourceBlock<PendingImportResourceNewGen>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResourceNewGen> target)
    {
      (_suspensionBufferBlock as ISourceBlock<PendingImportResourceNewGen>).ReleaseReservation(messageHeader, target);
    }

    #endregion
  }
}

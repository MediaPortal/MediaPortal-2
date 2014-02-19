#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Web.Configuration;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  class DirectoryUnfoldBlock : ISourceBlock<PendingImportResource>
  {
    #region Variables

    private readonly TransformManyBlock<IFileSystemResourceAccessor, PendingImportResource> _innerBlock;
    private readonly Stopwatch _stopWatch;
    private int _directoriesProcessed = 0;

    #endregion

    #region Constructor

    public DirectoryUnfoldBlock(ResourcePath path)
    {
      _innerBlock = new TransformManyBlock<IFileSystemResourceAccessor, PendingImportResource>(p => ProcessDirectory(p), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });
      IResourceAccessor ra;
      path.TryCreateLocalResourceAccessor(out ra);
      var fsra = ra as IFileSystemResourceAccessor;

      _stopWatch = Stopwatch.StartNew();
      _innerBlock.Post(fsra);
    }

    #endregion

    #region Private methods

    private IEnumerable<PendingImportResource> ProcessDirectory(IFileSystemResourceAccessor fsra)
    {
      Interlocked.Increment(ref _directoriesProcessed);

      //ToDo: Only add if Directory is NOT a single resource (such as a DVD directory)
      var result = new HashSet<PendingImportResource> { new PendingImportResource(Guid.Empty, fsra) };

      ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(fsra, false);
      if(directories != null)
        foreach (var subDirectory in directories)
          _innerBlock.Post(subDirectory);

      if (_innerBlock.InputCount == 0)
      {
        _innerBlock.Complete();
        _stopWatch.Stop();
        ServiceRegistration.Get<ILogger>().Info("DirectoryUnfoldBlock: Unfolded {0} directories. Time elapsed: {1}", _directoriesProcessed, _stopWatch.Elapsed);
      }

      // ToDo: Remove this - just here to free the resources for now
      fsra.Dispose();

      return result;
    }

    #endregion

    #region Interface implementations

    public void Complete()
    {
      _innerBlock.Complete();
    }

    public void Fault(Exception exception)
    {
      (_innerBlock as ISourceBlock<PendingImportResource>).Fault(exception);
    }

    public Task Completion
    {
      get { return _innerBlock.Completion; }
    }

    public IDisposable LinkTo(ITargetBlock<PendingImportResource> target, DataflowLinkOptions linkOptions)
    {
      return _innerBlock.LinkTo(target, linkOptions);
    }

    public PendingImportResource ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResource> target, out bool messageConsumed)
    {
      return (_innerBlock as ISourceBlock<PendingImportResource>).ConsumeMessage(messageHeader, target, out messageConsumed);
    }

    public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResource> target)
    {
      return (_innerBlock as ISourceBlock<PendingImportResource>).ReserveMessage(messageHeader, target);
    }

    public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PendingImportResource> target)
    {
      (_innerBlock as ISourceBlock<PendingImportResource>).ReleaseReservation(messageHeader, target);
    }

    #endregion
  }
}

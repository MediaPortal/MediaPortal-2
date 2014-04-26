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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes MediaItems and saves their aspects to the Database
  /// </summary>
  class MediaItemSaveBlock : ImporterWorkerDataflowBlockBase
  {
    #region Consts

    public const String BLOCK_NAME = "MediaItemSaveBlock";

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the MediaItemSaveBlock
    /// </summary>
    /// <param name="ct">CancellationToken used to cancel this block</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DirectoryUnfoldBlock belongs</param>
    public MediaItemSaveBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController) : base(
      importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      BLOCK_NAME, false, parentImportJobController)
    {
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main method that is called for every <see cref="PendingImportResourceNewGen"/> in this block
    /// </summary>
    /// <param name="importResource">Directory resource to be saved to the MediaLibrary</param>
    /// <returns></returns>
    private async Task<PendingImportResourceNewGen> ProcessMediaItem(PendingImportResourceNewGen importResource)
    {
      try
      {
        // ReSharper disable once PossibleInvalidOperationException
        await UpdateMediaItem(importResource.ParentDirectoryId.Value, importResource.PendingResourcePath, importResource.Aspects.Values);

        if (ImportJobInformation.JobType == ImportJobType.Refresh)
          if(importResource.IsSingleResource)
            await DeleteUnderPath(importResource.PendingResourcePath);

        importResource.IsValid = false;
        return importResource;
      }
      catch (TaskCanceledException)
      {
        return importResource;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}.{1}: Error while processing {2}", ex, ParentImportJobController, ToString(), importResource);
        importResource.IsValid = false;
        return importResource;
      }
    }

    #endregion

    #region Base overrides

    protected override IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock()
    {
      return new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, Task<PendingImportResourceNewGen>>(ProcessMediaItem), InnerBlockOptions);
    }

    #endregion
  }
}

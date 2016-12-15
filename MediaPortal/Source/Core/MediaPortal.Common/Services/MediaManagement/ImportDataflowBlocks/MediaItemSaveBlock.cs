#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes MediaItems and saves their MIAs to the Database
  /// </summary>
  /// <remarks>
  /// This DataflowBlock expects that the <see cref="PendingImportResourceNewGen"/>s passed to it do not have
  /// a <c>null</c> value in their Aspects property
  /// ToDo: Extend this DataflowBlock for the saving process of SecondPassImports
  ///       (in particular give it a unique BLOCK_NAME in case of multiple instances)
  /// </remarks>
  class MediaItemSaveBlock : ImporterWorkerDataflowBlockBase
  {
    #region Consts

    public const String BLOCK_NAME = "MediaItemSaveBlock";

    #endregion

    #region Variables

    private readonly CancellationToken _ct;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the MediaItemSaveBlock
    /// </summary>
    /// <remarks>
    /// The preceding MetadataExtractorBlock has a BoundedCapacity. To avoid that this limitation does not have any effect
    /// because all the items are immediately passed to an unbounded InputBlock of this MediaItemSaveBlock, we
    /// have to set the BoundedCapacity of the InputBlock to 1. The BoundedCapacity of the InnerBlock is set to 500,
    /// which is a good trade-off between speed and memory usage. The OutputBlock disposes the PendingImportResources and 
    /// therefore does not need a BoundedCapacity.
    /// </remarks>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public MediaItemSaveBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 500 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      BLOCK_NAME, false, parentImportJobController)
    {
      _ct = ct;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// - Saves all MediaItems with their MIAs to the Database
    /// - In case of SingleResources in RefreshImports it also deletes all MediaItems below the current one in the database
    ///   (necessary if in a previous import this MediaItem was saved to the Database as a directory instead of a SingleResource)
    /// </remarks>
    /// <param name="importResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns><see cref="PendingImportResourceNewGen"/> after processing</returns>
    private async Task<PendingImportResourceNewGen> ProcessMediaItem(PendingImportResourceNewGen importResource)
    {
      try
      {
        // ReSharper disable once PossibleInvalidOperationException
        await UpdateMediaItem(importResource.ParentDirectoryId.Value, importResource.PendingResourcePath, MediaItemAspect.GetAspects(importResource.Aspects), ImportJobInformation, importResource.MediaItemId.HasValue, _ct);

        if (ImportJobInformation.JobType == ImportJobType.Refresh)
        {
          if (importResource.IsSingleResource && importResource.Aspects.ContainsKey(DirectoryAspect.ASPECT_ID))
            await DeleteUnderPath(importResource.PendingResourcePath);
        }

        importResource.Aspects.Clear();
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
        importResource.Aspects.Clear();
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

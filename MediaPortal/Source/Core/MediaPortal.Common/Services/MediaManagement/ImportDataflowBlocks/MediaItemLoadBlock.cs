#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes MediaItems and loads their MIAs from the Database
  /// </summary>
  /// <remarks>
  /// This DataflowBlock will load existing Aspects into the <see cref="PendingImportResourceNewGen"/> passed to it
  /// if it is a SecondPassImport.
  /// ToDo: Give it a unique BLOCK_NAME in case of multiple instances
  /// </remarks>
  class MediaItemLoadBlock : ImporterWorkerDataflowBlockBase
  {
    #region Consts

    public const String BLOCK_NAME = "MediaItemLoadBlock";

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the MediaItemLoadBlock
    /// </summary>
    /// <remarks>
    /// The preceding FileUnfoldBlock has a BoundedCapacity. To avoid that this limitation does not have any effect
    /// because all the items are immediately passed to an unbounded InputBlock of this MetadataExtractorBlock, we
    /// have to set the BoundedCapacity of the InputBlock to 1. The BoundedCapacity of the InnerBlock is set to 100,
    /// which is a good trade-off between speed and memory usage. For the reason mentioned before, we also have to
    /// set the BoundedCapacity of the OutputBlock to 1.
    /// </remarks>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public MediaItemLoadBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 100 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      BLOCK_NAME, false, parentImportJobController)
    {
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// - Loads MediaItems with their MIAs from the Database for RefreshImports if last update was more than a day ago.
    /// </remarks>
    /// <param name="importResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns><see cref="PendingImportResourceNewGen"/> after processing</returns>
    private async Task<PendingImportResourceNewGen> ProcessMediaItem(PendingImportResourceNewGen importResource)
    {
      try
      {
        if (ImportJobInformation.JobType == ImportJobType.Refresh)
        {
          // Load Aspects if MI was changed or is more than a day old
          if (importResource.DateOfLastImport < importResource.ResourceAccessor.LastChanged ||
                (DateTime.Now - importResource.DateOfLastImport).TotalHours > MINIMUM_IMPORT_AGE)
          {
            ICollection<Guid> aspects = await GetAllManagedMediaItemAspectTypes();

            List<Guid> optionalAspects = new List<Guid>(aspects);
            if (optionalAspects.Contains(ProviderResourceAspect.ASPECT_ID))
              optionalAspects.Remove(ProviderResourceAspect.ASPECT_ID);

            List<Guid> requiredAspects = new List<Guid>();
            requiredAspects.Add(ProviderResourceAspect.ASPECT_ID);

            // ReSharper disable once PossibleInvalidOperationException
            MediaItem mediaItem = await LoadLocalItem(importResource.PendingResourcePath, requiredAspects.AsEnumerable(), optionalAspects.AsEnumerable());
            if(mediaItem != null)
            {
              importResource.ExistingAspects = mediaItem.Aspects;
              importResource.MediaItemId = mediaItem.MediaItemId;
            }
          }
        }
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

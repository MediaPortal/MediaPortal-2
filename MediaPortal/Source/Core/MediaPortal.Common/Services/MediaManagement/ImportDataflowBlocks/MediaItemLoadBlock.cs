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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;

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

    #region Variables

    private readonly Lazy<Task<DateTime>> _mostRecentMiaCreationDate;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the MediaItemLoadBlock
    /// </summary>
    /// <remarks>
    /// The preceding FileUnfoldBlock has a BoundedCapacity. To avoid that this limitation does not have any effect
    /// because all the items are immediately passed to an unbounded InputBlock of this MetadataExtractorBlock, we
    /// have to set the BoundedCapacity of the InputBlock to 1. The BoundedCapacity of the InnerBlock is set to 500,
    /// which is a good trade-off between speed and memory usage. For the reason mentioned before, we also have to
    /// set the BoundedCapacity of the OutputBlock to 1.
    /// </remarks>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public MediaItemLoadBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 500 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      BLOCK_NAME, false, parentImportJobController)
    {
      _mostRecentMiaCreationDate = new Lazy<Task<DateTime>>(GetMostRecentMiaCreationDate);
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
          // Do not import again, if the file or directory wasn't changed since the last import
          // and there were no new relevant MIAs added since then.
          // ToDo: We should only omit MDEs that get their data from the file or directory itself. All others should be called anyway.
          if (importResource.DateOfLastImport > importResource.ResourceAccessor.LastChanged &&
              importResource.DateOfLastImport > await _mostRecentMiaCreationDate.Value  &&
              importResource.MediaItemId.HasValue)
          {
            importResource.IsValid = false;
            return importResource;
          }

          //if (importResource.DateOfLastImport == DateTime.MinValue)
          //  ServiceRegistration.Get<ILogger>().Info("File {0} force import", importResource.PendingResourcePath);
          //else if (importResource.DateOfLastImport < importResource.ResourceAccessor.LastChanged)
          //  ServiceRegistration.Get<ILogger>().Info("File {0} changed after import {1} < {2}", importResource.PendingResourcePath, importResource.DateOfLastImport, importResource.ResourceAccessor.LastChanged);
          //else if (importResource.DateOfLastImport < await _mostRecentMiaCreationDate.Value)
          //  ServiceRegistration.Get<ILogger>().Info("File {0} changed before aspect change", importResource.PendingResourcePath);
          //else if (!importResource.MediaItemId.HasValue)
          //  ServiceRegistration.Get<ILogger>().Info("File {0} needs import", importResource.PendingResourcePath);

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
            SingleMediaItemAspect directoryAspect;
            if (MediaItemAspect.TryGetAspect(mediaItem.Aspects, DirectoryAspect.Metadata, out directoryAspect))
            {
              importResource.IsValid = false;
              return importResource;
            }

            importResource.ExistingAspects = mediaItem.Aspects;
            importResource.MediaItemId = mediaItem.MediaItemId;
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

    /// <summary>
    /// Returns the most recent creation date of the MIAs relevant to this ImportJob
    /// </summary>
    /// <returns>Most recent creation date</returns>
    /// <remarks>
    /// We first get all MDEs to be applied in this ImportJob. Then we determine which MIAs are imported by these relevant
    /// MDEs. Then we fetch from the MediaLibrary the dates on which these MIAs have ben created and take the most
    /// recent one of these dates.
    /// </remarks>
    private async Task<DateTime> GetMostRecentMiaCreationDate()
    {
      if (ImportJobInformation.MetadataExtractorIds.Count == 0)
        return DateTime.MinValue;
      var mediaAccessor = ServiceRegistration.Get<IMediaAccessor>();
      var relevantMdes = mediaAccessor.LocalMetadataExtractors.Where(kvp => ImportJobInformation.MetadataExtractorIds.Contains(kvp.Key)).Select(kvp => kvp.Value).ToList();
      var relevantMiaIds = relevantMdes.SelectMany(mde => mde.Metadata.ExtractedAspectTypes.Keys).Distinct();

      var creationDates = await GetManagedMediaItemAspectCreationDates();

      var mostRecentRelevantDate = creationDates.Where(kvp => relevantMiaIds.Contains(kvp.Key)).Select(kvp => kvp.Value).Max();
      ServiceRegistration.Get<ILogger>().Debug("ImporterWorker.{0}.{1}: Most recent creation date of the MIAs relevant to this ImportJob: {2}", ParentImportJobController, ToString(), mostRecentRelevantDate);
      return mostRecentRelevantDate;
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

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
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes MediaItems of any kind and tries to extract Metadata for them
  /// </summary>
  class MetadataExtractorBlock : ImporterWorkerDataflowBlockBase
  {
    #region Consts

    public const String BLOCK_NAME = "MetadataExtractorBlock";

    #endregion

    #region Variables

    private readonly Lazy<Task<DateTime>> _mostRecentMiaCreationDate;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the MetadataExtractorBlock
    /// </summary>
    /// <remarks>
    /// The preceding MediaItemLoadBlock has a BoundedCapacity. To avoid that this limitation does not have any effect
    /// because all the items are immediately passed to an unbounded InputBlock of this MetadataExtractorBlock, we
    /// have to set the BoundedCapacity of the InputBlock to 1. The BoundedCapacity of the InnerBlock is set to 100,
    /// which is a good trade-off between speed and memory usage. For the reason mentioned before, we also have to
    /// set the BoundedCapacity of the OutputBlock to 1.
    /// </remarks>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public MetadataExtractorBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, MaxDegreeOfParallelism = Environment.ProcessorCount * 5, BoundedCapacity = 100 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      BLOCK_NAME, true, parentImportJobController)
    {
      _mostRecentMiaCreationDate = new Lazy<Task<DateTime>>(GetMostRecentMiaCreationDate);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// - It does nothing but drop the current <see cref="PendingImportResourceNewGen"/> if
    ///   (a) this is a RefreshImport,
    ///   (b) the DateOfLastImport is more current than the LastChanged date of the resource;
    ///       the DateOfLastImport is only set for files because for directories we don't know to what filesystem date we
    ///       should compare (the LastChanged date of the directory may be unchanged although fils below have changed), and
    ///   (c) the DateOfLastImport is more current than the most recent creation date of all MIAs, wich are imported in this
    ///       ImportJob. This ensures that if a new MIA is added to the system that should be imported for this ImportJob, the
    ///       Import is actually performed although this is a RefreshImport and the MediaItems have not changed since the last Import.
    /// - For all other resources it calls all the aplicable MDEs
    /// - It then also drops the respective <see cref="PendingImportResourceNewGen"/> if nothing could be extracted;
    ///   This is currently in particular the case for
    ///   - directories that are not SingleResources (as we don't have a MDE that extracts metadata for such directories, yet
    ///   - file types that non of the applicable MDEs can handle
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
              importResource.DateOfLastImport > await _mostRecentMiaCreationDate.Value &&
              ((DateTime.Now - importResource.DateOfLastImport).TotalHours <= MINIMUM_IMPORT_AGE || importResource.ExistingAspects == null))
          {
            importResource.IsValid = false;
            return importResource;
          }
        }

        importResource.Aspects = await ExtractMetadata(importResource.ResourceAccessor, importResource.ExistingAspects, ImportJobInformation.JobType == ImportJobType.Import);
        if (importResource.Aspects == null)
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

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
  /// Takes MediaItems of any kind and tries to extract Metadata for them
  /// </summary>
  class MetadataExtractorBlock : ImporterWorkerDataflowBlockBase
  {
    #region Consts

    public const String BLOCK_NAME_QUICK = "MetadataExtractorBlock_Quick";
    public const String BLOCK_NAME_FULL = "MetadataExtractorBlock_Full";

    #endregion

    #region Variables

    private readonly bool _forceQuickMode;

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the MetadataExtractorBlock
    /// </summary>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    /// <param name="forceQuickMode"><c>true</c> if this is the MetadataExtractorBlock used for FirstPassImports, else <c>false</c></param>
    public MetadataExtractorBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController, bool forceQuickMode)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, MaxDegreeOfParallelism = Environment.ProcessorCount * 5 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      forceQuickMode ? BLOCK_NAME_QUICK : BLOCK_NAME_FULL, true, parentImportJobController)
    {
      _forceQuickMode = forceQuickMode;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// - It does nothing but drop the current <see cref="PendingImportResourceNewGen"/> if
    ///   (a) this is a RefreshImport, and
    ///   (b) the DateOfLastImport is more current than the LastChanged date of the resource;
    ///       the DateOfLastImport is only set for files because for directories we don't know to what filesystem date we
    ///       should compare (the LastChanged date of the directory may be unchanged although fils below have changed)
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
          // This is the behavior of the old ImporterWorker.
          // ToDo: We should only omit MDEs that get their data from the file or directory itself. All others should be called anyway.
          if (importResource.DateOfLastImport > importResource.ResourceAccessor.LastChanged)
          {
            importResource.IsValid = false;
            return importResource;
          }
        }
        
        importResource.Aspects = await ExtractMetadata(importResource.ResourceAccessor, _forceQuickMode);
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

    #endregion

    #region Base overrides

    protected override IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock()
    {
      return new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, Task<PendingImportResourceNewGen>>(ProcessMediaItem), InnerBlockOptions);
    }

    #endregion
  }
}

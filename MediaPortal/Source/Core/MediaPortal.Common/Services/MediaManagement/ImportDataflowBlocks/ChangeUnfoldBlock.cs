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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Takes a directory and provides this directory and all the files in that directory
  /// </summary>
  /// <remarks>
  /// ToDo: Handle multi file MediaItems
  /// </remarks>
  class ChangeUnfoldBlock : ImporterWorkerDataflowBlockBase
  {
    #region Constants

    public const String BLOCK_NAME = "ChangeUnfoldBlock";

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the ChangeUnfoldBlock
    /// </summary>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public ChangeUnfoldBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 500 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      BLOCK_NAME, true, parentImportJobController)
    {
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// - SingleResources are just passed to the next DataflowBlock
    /// - If it's not a SingleResource
    ///   - it finds all the files in the current directory,
    ///   - in case of a RefreshImport
    ///     - it deletes all the files in the MediaLibrary that do not exist anymore in the filesystem,
    ///     - it stores the DateOfLastImport of all the files in the <see cref="PendingImportResourceNewGen"/>
    /// </remarks>
    /// <param name="importResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns>
    /// a HashSet of <see cref="PendingImportResourceNewGen"/>s containing the current <see cref="PendingImportResource"/>
    /// after processing as well as <see cref="PendingImportResourceNewGen"/>s for all files in the current directory
    /// </returns>
    private async Task<IEnumerable<PendingImportResourceNewGen>> ProcessChanges(PendingImportResourceNewGen importResource)
    {
      var result = new HashSet<PendingImportResourceNewGen> { importResource };
      try
      {
        if (ImportJobInformation.JobType == ImportJobType.Refresh)
        {
        // ReSharper disable once PossibleInvalidOperationException
        IEnumerable<MediaItem> mediaItems = await GetUpdatableMediaItems(PROVIDERRESOURCE_IMPORTER_MIA_ID_ENUMERATION, null);
        if (mediaItems != null)
        {
          foreach (MediaItem mi in mediaItems)
          {
            IList<MultipleMediaItemAspect> providerAspects = null;
              if (MediaItemAspect.TryGetAspects(mi.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
              {
                ResourcePath path = ResourcePath.Deserialize(providerAspects[0].GetAttributeValue<String>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
                Guid? directoryId = providerAspects[0].GetAttributeValue<Guid?>(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID);
                IResourceAccessor ra;
                if (path.TryCreateLocalResourceAccessor(out ra) && ra is IFileSystemResourceAccessor)
                {
                  IFileSystemResourceAccessor f = ra as IFileSystemResourceAccessor;
                  string dirPath = ResourcePathHelper.GetDirectoryName(ra.Path);
                  ResourcePath dirRa = ResourcePath.BuildBaseProviderPath(ra.ParentProvider.Metadata.ResourceProviderId, dirPath);
                  PendingImportResourceNewGen pir = new PendingImportResourceNewGen(dirRa, f, ToString(), ParentImportJobController, directoryId, mi.MediaItemId);
                  pir.DateOfLastImport = DateTime.MinValue; //Force update
                  result.Add(pir);
                }
              }
            }
          }
        }
        return result;
      }
      catch (TaskCanceledException)
      {
        return result;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImporterWorker.{0}.{1}: Error while processing {2}", ex, ParentImportJobController, ToString(), importResource);
        importResource.IsValid = false;
        return result;
      }
    }

    #endregion

    #region Base overrides

    protected override IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock()
    {
      return new TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, Task<IEnumerable<PendingImportResourceNewGen>>>(ProcessChanges), InnerBlockOptions);
    }

    #endregion
  }
}

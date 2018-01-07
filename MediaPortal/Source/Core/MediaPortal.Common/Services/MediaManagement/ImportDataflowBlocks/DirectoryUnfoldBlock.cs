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
using System.Linq;
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
  /// Takes one directory and provides this directory and all its direct and indirect subdirectories
  /// except those that are below single resource directories (like e.g. a DVD directory)
  /// </summary>
  /// <remarks>
  /// The actual work of this block is done in a TransformBlock. It posts the subdirectories of a given
  /// directory recursively to the InputBlock. The TransformBlock must be run with a MaxDegreeOfParallelism of 1
  /// and a BoundedCapacity of 1 to ensure that the <see cref="PendingImportResourceNewGen"/>s are processed one
  /// by one in the right order - also after this ImportJob was persisted to disk and restored from disk.
  /// The reason is that we have to make sure that parent directories are always saved
  /// to the database before their respective child directories as we store the parent directory's MediaItemId
  /// in the respective child directory's MediaItem and we only get this MediaItemId when the parent directory's
  /// MediaItem was stored in the database.
  /// ToDo: Add an IsSingleResource method to the IMetadatExtractor interface and all its implementations
  ///       If at least one of the MetadataExtractors to be applied returns true, the directory is
  ///       treated as a single resource, not as a directory containing sub-items or subdirectories.
  /// ToDo: Handle multi directory MediaItems (such as DVD1 and DVD2 of a single movie) here
  /// </remarks>
  class DirectoryUnfoldBlock : ImporterWorkerDataflowBlockBase
  {
    #region Constants

    public const String BLOCK_NAME = "DirectoryUnfoldBlock";

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the DirectoryUnfoldBlock
    /// </summary>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public DirectoryUnfoldBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
      : base(importJobInformation,
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      BLOCK_NAME, true, parentImportJobController)
    {
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Main process method for the InnerBlock
    /// </summary>
    /// <remarks>
    /// - Checks whether the current resource is a SingleResource.
    /// - If this is not the case (i.e. there are subitems under this resource)
    ///   - it posts all the subdirectories under this resource to this DataflowBlock
    ///   - and in case of a RefreshImport, it deletes all subdirectories of the current resource in the MediaLibrary
    ///     that do not exist anymore.
    ///  - If there are no more resources to be processed after this resource, it completes this DataflowBlock
    /// </remarks>
    /// <param name="importResource"><see cref="PendingImportResourceNewGen"/> to be processed</param>
    /// <returns><see cref="PendingImportResourceNewGen"/> after processing</returns>
    private async Task<PendingImportResourceNewGen> ProcessDirectory(PendingImportResourceNewGen importResource)
    {
      try
      {
        importResource.IsSingleResource = await IsSingleResource(importResource.ResourceAccessor);

        if (!importResource.IsSingleResource && ImportJobInformation.IncludeSubDirectories)
        {
          var subDirectories = FileSystemResourceNavigator.GetChildDirectories(importResource.ResourceAccessor, false) ?? new HashSet<IFileSystemResourceAccessor>();

          // This may throw an exception in case of cancellation and therefore needs to be done before
          // posting the subdirectories to the InputBufferBlock to avoid duplicate ImportResources when
          // reactivating this block after it has been deserialized from disk.
          if (ImportJobInformation.JobType == ImportJobType.Refresh)
            await DeleteNoLongerExistingSubdirectoriesFromMediaLibrary(importResource, subDirectories);

          foreach (var subDirectory in subDirectories)
            this.Post(new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, subDirectory, ToString(), ParentImportJobController));
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
      finally
      {
        var inputBlock = (TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>)InputBlock;
        if (inputBlock.InputCount == 0 && inputBlock.OutputCount == 0)
          ParentImportJobController.FirstBlockHasFinished();        
      }
    }

    /// <summary>
    /// Deletes no longer existing subdirectories from the MediaLibrary
    /// </summary>
    /// <param name="importResource"><see cref="PendingImportResourceNewGen"/>representing the currently processed directory</param>
    /// <param name="subDirectories">Existing subdirectories of the currently processed directory</param>
    /// <returns></returns>
    private async Task DeleteNoLongerExistingSubdirectoriesFromMediaLibrary(PendingImportResourceNewGen importResource, IEnumerable<IFileSystemResourceAccessor> subDirectories)
    {
      var mediaItem = await LoadLocalItem(importResource.PendingResourcePath, EMPTY_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION);
      
      // If the currently processed directory does not yet exist in the MediaLibrary,
      // there is no need to check for existing subdirectories in the MediaLibrary.
      if (mediaItem == null)
        return;
      var directoryId = mediaItem.MediaItemId;
      
      // Get the subdirectories stored in the MediaLibrary for the currently procesed directory
    // TODO: Rework this
      var subDirectoryResourcePathsInMediaLibrary = (await Browse(directoryId, PROVIDERRESOURCE_DIRECTORY_MIA_ID_ENUMERATION, EMPTY_MIA_ID_ENUMERATION)).Select(mi => ResourcePath.Deserialize(mi[ProviderResourceAspect.ASPECT_ID][0].GetAttributeValue<String>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))).ToList();
      
      // If there are no subdirectories stored in the MediaLibrary, there is no need to delete anything
      if (!subDirectoryResourcePathsInMediaLibrary.Any())
        return;
      
      // Find out which subdirectories are stored in the MediaLibrary that do not exist anymore
      // in the filesystem and delete them (including all subdirectories and subitems)
      var subDirectoryResourcePathsInFileSystem = subDirectories.Select(ra => ra.CanonicalLocalResourcePath);
      var noLongerExistingSubdirectoryResourcePaths = subDirectoryResourcePathsInMediaLibrary.Except(subDirectoryResourcePathsInFileSystem).ToList();
      foreach (var noLongerExistingSubdirectoryResourcePath in noLongerExistingSubdirectoryResourcePaths)
        await DeleteMediaItem(noLongerExistingSubdirectoryResourcePath);
    }

    #endregion

    #region Base overrides

    protected override IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock()
    {
      return new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, Task<PendingImportResourceNewGen>>(ProcessDirectory), InnerBlockOptions);
    }

    #endregion
  }
}

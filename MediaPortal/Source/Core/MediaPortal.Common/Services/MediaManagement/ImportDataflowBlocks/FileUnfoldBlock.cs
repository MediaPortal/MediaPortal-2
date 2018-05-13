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
  /// Takes a directory and provides this directory and all the files in that directory
  /// </summary>
  /// <remarks>
  /// ToDo: Handle multi file MediaItems
  /// </remarks>
  class FileUnfoldBlock : ImporterWorkerDataflowBlockBase
  {
    #region Constants

    public const String BLOCK_NAME = "FileUnfoldBlock";

    #endregion

    #region Constructor

    /// <summary>
    /// Initiates the FileUnfoldBlock
    /// </summary>
    /// <remarks>
    /// The BoundedCapacity of the InnerBlock is limited to 500 items, which has proven to be a good trade-off
    /// between speed and memory usage. To avoid that this limitation does not have any effect because all the items
    /// are immediately passed to an unbounded OutputBlock, we have to set the BoundedCapacity of the OutputBlock to 1.
    /// </remarks>
    /// <param name="ct">CancellationToken used to cancel this DataflowBlock</param>
    /// <param name="importJobInformation"><see cref="ImportJobInformation"/> of the ImportJob this DataflowBlock belongs to</param>
    /// <param name="parentImportJobController">ImportJobController to which this DataflowBlock belongs</param>
    public FileUnfoldBlock(CancellationToken ct, ImportJobInformation importJobInformation, ImportJobController parentImportJobController)
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
    private async Task<IEnumerable<PendingImportResourceNewGen>> ProcessDirectory(PendingImportResourceNewGen importResource)
    {
      var result = new HashSet<PendingImportResourceNewGen> { importResource };
      try
      {
        ICollection<IFileSystemResourceAccessor> files;
        ICollection<IFileSystemResourceAccessor> stubFiles = new HashSet<IFileSystemResourceAccessor>();
        IDictionary<ResourcePath, DateTime> path2LastImportDate = new Dictionary<ResourcePath, DateTime>();
        IDictionary<ResourcePath, Guid> path2MediaItem = new Dictionary<ResourcePath, Guid>();
        IEnumerable<MediaItem> mediaItems = null;
        if (importResource.IsSingleResource)
        {
          files = new HashSet<IFileSystemResourceAccessor> { importResource.ResourceAccessor };
          MediaItem mi = await LoadLocalItem(importResource.PendingResourcePath, PROVIDERRESOURCE_IMPORTER_MIA_ID_ENUMERATION, null).ConfigureAwait(false);
          if (mi != null)
          {
            mediaItems = new List<MediaItem>(new MediaItem[] { mi });
            importResource.MediaItemId = mi.MediaItemId;
          }
        }
        else
        {
          files = FileSystemResourceNavigator.GetFiles(importResource.ResourceAccessor, false, false) ?? new HashSet<IFileSystemResourceAccessor>();
          SingleMediaItemAspect directoryAspect;
          // ReSharper disable once PossibleInvalidOperationException
          // TODO: Rework this
          mediaItems = (await Browse(importResource.MediaItemId.Value, PROVIDERRESOURCE_IMPORTER_MIA_ID_ENUMERATION, DIRECTORY_MIA_ID_ENUMERATION).ConfigureAwait(false))
            .Where(mi => !MediaItemAspect.TryGetAspect(mi.Aspects, DirectoryAspect.Metadata, out directoryAspect));
        }
        if (mediaItems != null)
        {
          foreach (MediaItem mi in mediaItems)
          {
            //Check metadata and files:
            // 1. Last import date is lower than file change date => Refresh needed
            // 2. Media item ID is empty => Reimport/import needed
            // 3. Media item is dirty => Reimport/import needed
            IList<MultipleMediaItemAspect> providerAspects = null;
            if (MediaItemAspect.TryGetAspects(mi.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
            {
              foreach (var pra in providerAspects)
              {
                bool isStub = pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB;
                ResourcePath path = ResourcePath.Deserialize(pra.GetAttributeValue<String>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
                if (!path2LastImportDate.ContainsKey(path) && importResource.PendingResourcePath.IsSameOrParentOf(path))
                {
                  //If last refresh is equal to added date, it has never been through the refresh cycle, so set low last change date
                  //All media items must be added because the paths are later used to delete no longer existing media items
                  var lastImportDate = mi.Aspects[ImporterAspect.ASPECT_ID][0].GetAttributeValue<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE);
                  if (mi.Aspects[ImporterAspect.ASPECT_ID][0].GetAttributeValue<bool>(ImporterAspect.ATTR_DIRTY)) //If it is dirty, refresh is needed
                    path2LastImportDate.Add(path, DateTime.MinValue);
                  else
                    path2LastImportDate.Add(path, lastImportDate);
                }
                if (!importResource.IsSingleResource && !isStub && !path2MediaItem.ContainsKey(path))
                {
                  path2MediaItem.Add(path, mi.MediaItemId);
                }

                // Stub items need their media item id because the do no have a unique path
                // So add them now as long as the needed info is known
                if (isStub)
                {
                  IFileSystemResourceAccessor file = null;
                  try
                  {
                    IResourceAccessor ra;
                    if (path.TryCreateLocalResourceAccessor(out ra))
                      file = ra as IFileSystemResourceAccessor;
                  }
                  catch { }

                  // Only add it if it still exists
                  if (files.Where(f => file != null && f.CanonicalLocalResourcePath == file.CanonicalLocalResourcePath).Any())
                  {
                    stubFiles.Add(file);

                    DateTime dateTime;
                    PendingImportResourceNewGen pir = new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, file, ToString(), 
                      ParentImportJobController, importResource.MediaItemId, mi.MediaItemId, true);
                    pir.ExistingAspects = mi.Aspects;
                    if (ImportJobInformation.JobType == ImportJobType.Refresh)
                    {
                      if (path2LastImportDate.TryGetValue(pir.PendingResourcePath, out dateTime))
                        pir.DateOfLastImport = dateTime;
                    }
                    result.Add(pir);
                  }
                }
              }
            }
          }
          await DeleteNoLongerExistingFilesFromMediaLibrary(files, path2LastImportDate.Keys).ConfigureAwait(false);
        }

        //Add new stub items
        foreach (var file in files.Where(f => !path2LastImportDate.Keys.Contains(f.CanonicalLocalResourcePath)))
        {
          if (await IsStubResource(file).ConfigureAwait(false))
          {
            stubFiles.Add(file);

            DateTime dateTime;
            var stubAspects = await ExtractStubItems(file).ConfigureAwait(false);
            if (stubAspects != null)
            {
              foreach (var aspects in stubAspects)
              {
                PendingImportResourceNewGen pir = new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, file, ToString(),
                  ParentImportJobController, importResource.MediaItemId, null, true);
                pir.ExistingAspects = aspects;
                if (ImportJobInformation.JobType == ImportJobType.Refresh)
                {
                  if (path2LastImportDate.TryGetValue(pir.PendingResourcePath, out dateTime))
                    pir.DateOfLastImport = dateTime;
                }
                result.Add(pir);
              }
            }
          }
        }

        //Remove stub files from files collection so they don't get added again
        foreach (IFileSystemResourceAccessor file in stubFiles)
        {
          IFileSystemResourceAccessor stub = files.Where(f => f.CanonicalLocalResourcePath == file.CanonicalLocalResourcePath).FirstOrDefault();
          if (stub != null)
            files.Remove(stub);
        }

        //Add importers for files if any
        if (!importResource.IsSingleResource)
        {
          if (ImportJobInformation.JobType == ImportJobType.Import)
          {
            //Only import new files so only add non existing paths
            result.UnionWith(files.Where(f => !path2LastImportDate.Keys.Contains(f.CanonicalLocalResourcePath)).
              Select(f => new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, f, ToString(), ParentImportJobController, importResource.MediaItemId)));
          }
          else
          {
            result.UnionWith(files.Select(f => new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, f, ToString(), ParentImportJobController, importResource.MediaItemId,
              path2MediaItem.ContainsKey(f.CanonicalLocalResourcePath) ? path2MediaItem[f.CanonicalLocalResourcePath] : (Guid?)null)));
          }
        }

        // If this is a RefreshImport and we found files of the current directory in the MediaLibrary,
        // store the DateOfLastImport in the PendingImportResource
        if (ImportJobInformation.JobType == ImportJobType.Refresh)
        {
          DateTime dateTime;
          if (path2LastImportDate != null)
            foreach (var pir in result)
              if (path2LastImportDate.TryGetValue(pir.PendingResourcePath, out dateTime))
                pir.DateOfLastImport = dateTime;
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

    /// <summary>
    /// Deletes no longer existing files from the MediaLibrary
    /// </summary>
    /// <param name="filesInDirectory">Existing filesInDirectory in the currently processed directory</param>
    /// <param name="fileResourcePathsInMediaLibrary">ResourcePaths of file MediaItems stored as sub-items of this importResource in the MediaLibrary</param>
    private async Task DeleteNoLongerExistingFilesFromMediaLibrary(IEnumerable<IFileSystemResourceAccessor> filesInDirectory, ICollection<ResourcePath> fileResourcePathsInMediaLibrary)
    {
      // If there are no files stored in the MediaLibrary, there is no need to delete anything
      if (!fileResourcePathsInMediaLibrary.Any())
        return;

      // Find out which files are stored in the MediaLibrary that do not exist anymore
      // in the filesystem and delete them
      var fileResourcePathsInFileSystem = filesInDirectory.Select(ra => ra.CanonicalLocalResourcePath).ToList();
      var noLongerExistingFileResourcePaths = new List<ResourcePath>();
      foreach (var mlResource in fileResourcePathsInMediaLibrary)
      {
        // Existing media items can have chained resource paths (i.e. BD ISO, or video inside .zip archive)
        if (fileResourcePathsInFileSystem.Any(fsResource => mlResource == fsResource || mlResource.BasePathSegment == fsResource.BasePathSegment))
          continue;
        if (await IsSingleResourcePath(mlResource).ConfigureAwait(false))
          continue;

        noLongerExistingFileResourcePaths.Add(mlResource);
      }

      foreach (var noLongerExistingFileResourcePath in noLongerExistingFileResourcePaths)
        await DeleteMediaItem(noLongerExistingFileResourcePath).ConfigureAwait(false);
    }

    #endregion

    #region Base overrides

    protected override IPropagatorBlock<PendingImportResourceNewGen, PendingImportResourceNewGen> CreateInnerBlock()
    {
      return new TransformManyBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, Task<IEnumerable<PendingImportResourceNewGen>>>(ProcessDirectory), InnerBlockOptions);
    }

    #endregion
  }
}

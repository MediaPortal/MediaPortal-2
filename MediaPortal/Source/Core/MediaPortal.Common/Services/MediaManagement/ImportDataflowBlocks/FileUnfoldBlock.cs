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
        if (importResource.IsSingleResource)
          return result;

        // FileSystemResourceNavigator.GetFiles also returns files that were identified as virtual directories by
        // FileSystemResourceNavigator.GetChildDirectories (such as zip-files).
        // ToDo: Clarify if this is a bug
        var files = FileSystemResourceNavigator.GetFiles(importResource.ResourceAccessor, false) ?? new HashSet<IFileSystemResourceAccessor>();
        IDictionary<ResourcePath, DateTime> path2LastImportDate = null;

        if (ImportJobInformation.JobType == ImportJobType.Refresh)
        {
          MediaItemAspect directoryAspect;
          // ReSharper disable once PossibleInvalidOperationException
          path2LastImportDate = (await Browse(importResource.MediaItemId.Value, PROVIDERRESOURCE_IMPORTER_MIA_ID_ENUMERATION, DIRECTORY_MIA_ID_ENUMERATION))
            .Where(mi => !mi.Aspects.TryGetValue(DirectoryAspect.ASPECT_ID, out directoryAspect))
            .ToDictionary(mi => ResourcePath.Deserialize(mi[ProviderResourceAspect.ASPECT_ID].GetAttributeValue<String>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)), mi => mi[ImporterAspect.ASPECT_ID].GetAttributeValue<DateTime>(ImporterAspect.ATTR_LAST_IMPORT_DATE));
          await DeleteNoLongerExistingFilesFromMediaLibrary(files, path2LastImportDate.Keys);
        }

        result.UnionWith(files.Select(f => new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, f, ToString(), ParentImportJobController, importResource.MediaItemId)));

        // If this is a RefreshImport and we found files of the current directory in the MediaLibrary,
        // store the DateOfLastImport in the PendingImportResource
        DateTime dateTime;
        if (path2LastImportDate != null)
          foreach (var pir in result)
            if(path2LastImportDate.TryGetValue(pir.PendingResourcePath, out dateTime))
              pir.DateOfLastImport = dateTime;

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
      var fileResourcePathsInFileSystem = filesInDirectory.Select(ra => ra.CanonicalLocalResourcePath);
      var noLongerExistingFileResourcePaths = fileResourcePathsInMediaLibrary.Except(fileResourcePathsInFileSystem);
      foreach (var noLongerExistingFileResourcePath in noLongerExistingFileResourcePaths)
        await DeleteMediaItem(noLongerExistingFileResourcePath);
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

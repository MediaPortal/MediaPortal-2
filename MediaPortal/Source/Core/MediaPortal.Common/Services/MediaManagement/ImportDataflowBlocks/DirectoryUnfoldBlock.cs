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
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MediaPortal.Common.Logging;
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
  /// ToDo: Handle database-deletion of no longer existing directories during refresh imports
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
    /// <param name="ct">CancellationToken used to cancel this block</param>
    /// <param name="parentImportJobController">ImportJobController to which this DirectoryUnfoldBlock belongs</param>
    public DirectoryUnfoldBlock(CancellationToken ct, ImportJobController parentImportJobController) : base(
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      new ExecutionDataflowBlockOptions { CancellationToken = ct, BoundedCapacity = 1 },
      new ExecutionDataflowBlockOptions { CancellationToken = ct },
      BLOCK_NAME, true, parentImportJobController)
    {
    }

    #endregion

    #region Private methods

    private PendingImportResourceNewGen ProcessDirectory(PendingImportResourceNewGen importResource)
    {
      try
      {
        //ToDo: Only do this if Directory is NOT a single resource (such as a DVD directory)
        importResource.IsSingleResource = false;

        if (!importResource.IsSingleResource)
        {
          ICollection<IFileSystemResourceAccessor> directories = FileSystemResourceNavigator.GetChildDirectories(importResource.ResourceAccessor, false);
          if (directories != null)
            foreach (var subDirectory in directories)
              this.Post(new PendingImportResourceNewGen(importResource.ResourceAccessor.CanonicalLocalResourcePath, subDirectory, ToString(), ParentImportJobController));
        }

        var inputBlock = (TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>)InputBlock;
        if (inputBlock.InputCount == 0 && inputBlock.OutputCount == 0)
          InputBlock.Complete();

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
      return new TransformBlock<PendingImportResourceNewGen, PendingImportResourceNewGen>(new Func<PendingImportResourceNewGen, PendingImportResourceNewGen>(ProcessDirectory), InnerBlockOptions);
    }

    #endregion
  }
}

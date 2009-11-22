#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Core.ImporterWorker
{
  public interface IMediaLibraryCallback
  {
    ICollection<MediaItem> Browse(ResourcePath path, IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        IEnumerable<Guid> optionalRequestedMIATypeIDs);
  }

  public interface IImportResultCallback
  {
    /// <summary>
    /// Will be called at the start of import of the given location. Will be called once for the complete structure, even
    /// if the import job includes subdirectories.
    /// </summary>
    /// <param name="path">Location which is imported.</param>
    void StartImport(ResourcePath path);

    /// <summary>
    /// Will be called when the import of the given location has finished. Will be called once for the complete structure, even
    /// if the import job includes subdirectories.
    /// </summary>
    /// <param name="path">Location which was imported.</param>
    void EndImport(ResourcePath path);

    /// <summary>
    /// Adds or updates the metadata of the specified media item.
    /// </summary>
    /// <param name="path">Path of the media item's resource.</param>
    /// <param name="updatedAspects">Enumeration of updated media item aspects.</param>
    void UpdateMediaItem(ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects);

    /// <summary>
    /// Deletes the media item of the given location.
    /// </summary>
    /// <param name="path">Location of the media item to delete.</param>
    void DeleteMediaItem(ResourcePath path);

    /// <summary>
    /// Called when an error occurs while importing the given <paramref name="resource"/>.
    /// </summary>
    /// <param name="resource">Resource which could not be imported.</param>
    void ImportError(IResourceAccessor resource);
  }

  public interface IImporterWorker
  {
    /// <summary>
    /// Returns the information if the importer worker is able to process import requests.
    /// </summary>
    bool IsActive { get; }

    void Startup();

    void Shutdown();

    /// <summary>
    /// Schedules an asynchronous import of the local resource specified by <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Resource path of the directory or file to be imported.</param>
    /// <param name="mediaCategories">Media categories to choose metadata extractors for.</param>
    /// <param name="includeSubDirectories">If the given <paramref name="path"/> is a directory, this parameter controls if
    /// subdirectories are imported or not.</param>
    /// <param name="resultCallback">Callback interface to be notified on import results.</param>
    void ScheduleImport(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories,
        IImportResultCallback resultCallback);

    /// <summary>
    /// Schedules an asynchronous refresh of the local resource specified by <paramref name="path"/>.
    /// </summary>
    /// <remarks>
    /// This method will request the media library for the current data of all modules before starting the import to
    /// check if the module was changed against the media library.
    /// </remarks>
    /// <param name="path">Resource path of the directory or file to be imported.</param>
    /// <param name="mediaCategories">Media categories to choose metadata extractors for.</param>
    /// <param name="includeSubDirectories">If the given <paramref name="path"/> is a directory, this parameter controls if
    /// subdirectories are imported or not.</param>
    /// <param name="mediaLibraryCallback">Callback interface which will be called to request the current state of media items.</param>
    /// <param name="resultCallback">Callback interface to be notified on import results.</param>
    void ScheduleRefresh(ResourcePath path, ICollection<string> mediaCategories, bool includeSubDirectories,
        IMediaLibraryCallback mediaLibraryCallback, IImportResultCallback resultCallback);
  }
}

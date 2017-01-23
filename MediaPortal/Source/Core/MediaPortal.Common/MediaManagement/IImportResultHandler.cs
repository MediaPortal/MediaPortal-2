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
using MediaPortal.Common.ResourceAccess;
using System.Threading;

namespace MediaPortal.Common.MediaManagement
{
  public interface IImportResultHandler
  {
    /// <summary>
    /// Adds or updates the metadata of the specified media item located on the local system.
    /// </summary>
    /// <param name="parentDirectoryId">Id of the parent directory's media item or <see cref="Guid.Empty"/>, if the
    /// parent directory is not present in the media library.</param>
    /// <param name="path">Path of the media item's resource.</param>
    /// <param name="updatedAspects">Enumeration of updated media item aspects.</param>
    /// <returns>Id of the media item which has been added or updated.</returns>
    /// <exception cref="DisconnectedException">If the connection to the media library was disconnected.</exception>
    Guid UpdateMediaItem(Guid parentDirectoryId, ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects, bool isRefresh, ResourcePath basePath, CancellationToken cancelToken);

    /// <summary>
    /// Deletes the media item of the given location located on the local system.
    /// </summary>
    /// <param name="path">Location of the media item to delete.</param>
    /// <exception cref="DisconnectedException">If the connection to the media library was disconnected.</exception>
    void DeleteMediaItem(ResourcePath path);

    /// <summary>
    /// Deletes all media items whose path starts with the given <paramref name="path"/> located on the local system,
    /// except the media item whose path is exactly the given <paramref name="path"/>.
    /// </summary>
    /// <param name="path">Start path of media items to be deleted.</param>
    /// <exception cref="DisconnectedException">If the connection to the media library was disconnected.</exception>
    void DeleteUnderPath(ResourcePath path);
  }
}

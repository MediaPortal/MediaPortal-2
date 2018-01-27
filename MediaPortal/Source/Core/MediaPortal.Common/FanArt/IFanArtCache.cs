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
using System.Threading.Tasks;

namespace MediaPortal.Common.FanArt
{
  /// <summary>
  /// Interface for storage and retrieval of fanart images for media items.
  /// </summary>
  public interface IFanArtCache
  {
    /// <summary>
    /// Creates a subdirectory in the fanart cache directory for the media item with the specified id. 
    /// </summary>
    /// <param name="mediaItemId">The id of the media item to init the directory for.</param>
    void InitFanArtCache(Guid mediaItemId);

    /// <summary>
    /// Creates a subdirectory in the fanart cache directory for the media item with the specified id
    /// and adds an info file containing the specified title.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item to init the directory for.</param>
    /// <param name="title">The title of the media item.</param>
    void InitFanArtCache(Guid mediaItemId, string title);

    /// <summary>
    /// Gets the path to the directory to store fanart of the specified type for the specified media item.
    /// This directory may not be created by this method so the caller should ensure it exists if required.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <returns></returns>
    string GetFanArtDirectory(Guid mediaItemId, string fanArtType);

    /// <summary>
    /// Gets the configured maximum number of fanart images to store for fanart of the specified type
    /// or <paramref name="defaultCount"/> if no maximum has been configured.
    /// </summary>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <param name="defaultCount">The default count if no count has been configured.</param>
    /// <returns>The maximum count or default.</returns>
    int GetMaxFanArtCount(string fanArtType, int defaultCount = 3);

    /// <summary>
    /// Gets a list of paths to all fanart of the specified type for the specified media item.
    /// </summary>///
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <returns>List of fanart paths.</returns>
    IList<string> GetFanArtFiles(Guid mediaItemId, string fanArtType);

    /// <summary>
    /// Gets a list of all media item ids which have fanart.
    /// </summary>
    /// <returns>List of media item ids with fanart.</returns>
    ICollection<Guid> GetAllFanArtIds();

    /// <summary>
    /// Deletes all fanart for the specified media item.
    /// </summary>
    /// <param name="mediaItemId"></param>
    void DeleteFanArtFiles(Guid mediaItemId);

    /// <summary>
    /// Obtains a lock on the cache for the specified media item and fanart type which can be used
    /// to check the current fanart count and block other callers from updating the count of the same
    /// media item and fanart type.
    /// Callers should ensure that the FanArtCountLock returned is disposed when finished to release the lock.
    /// </summary>
    /// <param name="mediaItemId">The id of the media item.</param>
    /// <param name="fanArtType">The type of fanart.</param>
    /// <returns>FanArtCountLock object containing the current count.</returns>
    Task<FanArtCache.FanArtCountLock> GetFanArtCountLock(Guid mediaItemId, string fanArtType);
  }
}

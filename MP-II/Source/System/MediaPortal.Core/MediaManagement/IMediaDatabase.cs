#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core.MediaManagement.MLQueries;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// The MediaDatabase provides access to a database of all registered media files in the current
  /// MP-II system. It provides an interface to the locally or remotely located MediaLibrary.
  /// </summary>
  public interface IMediaDatabase
  {
    // *********************************
    // Media item aspect management
    // *********************************

    /// <summary>
    /// Registers the specified <paramref name="aspectMetadata"/> at the system's media library.
    /// After the specified aspect was registered, it can be used by imports of item aspects
    /// provided by metadata extractors.
    /// </summary>
    /// <param name="aspectMetadata">The media item aspect's metadata to register.</param>
    void RegisterMediaItemAspect(MediaItemAspectMetadata aspectMetadata);

    /// <summary>
    /// Removes the media item aspect with the specified <paramref name="mediaItemAspectId"/> from
    /// the system's media library. All aspect data stored in the specified aspect will be removed from
    /// the media library.
    /// </summary>
    /// <param name="mediaItemAspectId">The id of the media item aspect to remove.</param>
    void RemoveMediaItemAspect(Guid mediaItemAspectId);

    /// <summary>
    /// Returns the media item aspect with the specified <paramref name="mediaItemAspectId"/>, if it
    /// is registered at the system's media library.
    /// </summary>
    /// <param name="mediaItemAspectId">The id of the media item aspect to return.</param>
    /// <returns>Media item aspect instance or <c>null</c>, if the item aspect isn't registered.</returns>
    MediaItemAspect GetRegisteredMediaItemAspect(Guid mediaItemAspectId);

    /// <summary>
    /// Returns a collection of ids of all media item aspects registered at the system's media library.
    /// </summary>
    /// <returns>Ids of all registered media item aspects.</returns>
    ICollection<Guid> GetRegisteredMediaItemAspects();

    // *********************************
    // Media library query
    // *********************************

    /// <summary>
    /// Evaluates the specified query on this media database and returns the qualifying media items.
    /// </summary>
    /// <param name="query">The query to evaluate on this media database.</param>
    /// <returns>List of qualifying media items.</returns>
    IList<MediaItem> Evaluate(IQuery query);

    // *********************************
    // Media library updates
    // *********************************

    /// <summary>
    /// Updates the specified media item <paramref name="aspect"/> in the media database.
    /// </summary>
    /// <remarks>
    /// This method should be used for updates of single item aspects. Batch-updates should be done
    /// via the batch update API.
    /// </remarks>
    void UpdateAspect(MediaItemAspect aspect);

    // TODO: Batch-updates
  }
}

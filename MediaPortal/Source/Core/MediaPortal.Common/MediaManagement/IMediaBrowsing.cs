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

namespace MediaPortal.Common.MediaManagement
{
  public interface IMediaBrowsing
  {
    /// <summary>
    /// Loads the specified media item located on the local system.
    /// </summary>
    /// <param name="path">Path of the media item.</param>
    /// <param name="necessaryRequestedMIATypeIDs">Necessary MIA ids the returned item must support.</param>
    /// <param name="optionalRequestedMIATypeIDs">Optional MIA ids the returned item can support.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <returns>Loaded media item.</returns>
    /// <exception cref="DisconnectedException">If the connection to the media library was disconnected.</exception>
    MediaItem LoadLocalItem(ResourcePath path,
        IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfile = null);

    /// <summary>
    /// Loads the media items in the directory with the given <paramref name="parentDirectoryId"/>.
    /// </summary>
    /// <param name="parentDirectoryId">Id of the directory whose contents should be loaded.</param>
    /// <param name="necessaryRequestedMIATypeIDs">Necessary MIA ids the returned items must support.</param>
    /// <param name="optionalRequestedMIATypeIDs">Optional MIA ids the returned items can support.</param>
    /// <param name="userProfile">User profile to load any user specific media item data for.</param>
    /// <param name="offset">Number of items to skip when retrieving MediaItems.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>Collection of media items.</returns>
    /// <exception cref="DisconnectedException">If the connection to the media library was disconnected.</exception>
    IList<MediaItem> Browse(Guid parentDirectoryId, IEnumerable<Guid> necessaryRequestedMIATypeIDs,
        IEnumerable<Guid> optionalRequestedMIATypeIDs, Guid? userProfile, bool includeVirtual, uint? offset = null, uint? limit = null);

    /// <summary>
    /// Finds media items that have updated metadata available.
    /// </summary>
    /// <param name="necessaryRequestedMIATypeIDs">Necessary MIA ids the returned items must support.</param>
    /// <param name="optionalRequestedMIATypeIDs">Optional MIA ids the returned items can support.</param>
    /// <returns>Collection of media items.</returns>
    /// <exception cref="DisconnectedException">If the connection to the media library was disconnected.</exception>
    IList<MediaItem> GetUpdatableMediaItems(IEnumerable<Guid> necessaryRequestedMIATypeIDs, IEnumerable<Guid> optionalRequestedMIATypeIDs);

    /// <summary>
    /// Loads the creation dates of all managed MIAs in the MediaLibrary
    /// </summary>
    /// <returns>Dictionary with MIA IDs as keys and the respective creation dates as values</returns>
    IDictionary<Guid, DateTime> GetManagedMediaItemAspectCreationDates();

    /// <summary>
    /// Loads all managed MIA types from the MediaLibrary
    /// </summary>
    /// <returns>Collection with MIA IDs</returns>
    ICollection<Guid> GetAllManagedMediaItemAspectTypes();
  }
}

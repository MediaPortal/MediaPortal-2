#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using System.Collections.Generic;
using TvMosaic.API;

namespace TvMosaicMetadataExtractor.ResourceAccess
{
  /// <summary>
  /// Interface for a class that can be used by the <see cref="TvMosaicResourceAccessor"/> to navigate the TvMosaic API.
  /// </summary>
  public interface ITvMosaicNavigator
  {
    /// <summary>
    /// Gets the object ids of the top level object containers. 
    /// </summary>
    /// <returns>List of known root containers.</returns>
    ICollection<string> GetRootContainerIds();

    /// <summary>
    /// Gets the child items of the container with the specified id.
    /// </summary>
    /// <param name="containerId">Id of the container object.</param>
    /// <returns>If the <paramref name="containerId"/> is valid, <see cref="Items"/> containing the container's child items; else <c>null</c>.</returns>
    Items GetChildItems(string containerId);

    /// <summary>
    /// Gets the item with the specified id.
    /// </summary>
    /// <param name="itemId">Id of the item object.</param>
    /// <returns>Is the <paramref name="itemId"/> is valid, the <see cref="RecordedTV"/> item; else <c>null</c>.</returns>
    RecordedTV GetItem(string itemId);

    /// <summary>
    /// Determines whether an object (container or item) with the specified id exists.
    /// </summary>
    /// <param name="objectId">The id to check.</param>
    /// <returns><c>true</c> if the object exists.</returns>
    bool ObjectExists(string objectId);

    /// <summary>
    /// Gets a human readable name for the object.
    /// </summary>
    /// <param name="objectId">The id of the object to get the name of.</param>
    /// <returns>The object name</returns>
    string GetObjectFriendlyName(string objectId);
  }
}

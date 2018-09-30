#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  /// <summary>
  /// <see cref="IFanArtLibraryManager"/> manages the collection and deletion of fanart.
  /// </summary>
  public interface IFanArtLibraryManager : IDisposable
  {
    /// <summary>
    /// Schedules a cleanup of all fanart where the corresponding media item no longer exists.
    /// </summary>
    void ScheduleFanArtCleanup();

    /// <summary>
    /// Schedules the collection of fanart for the media item with the specified <paramref name="mediaItemId"/> and <paramref name="aspects"/>.
    /// </summary>
    /// <param name="mediaItemId">The media item id of the media item to collect fanart for.</param>
    /// <param name="aspects">The media item aspects of the media item to collect fanart for.</param>
    void ScheduleFanArtCollection(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects);

    /// <summary>
    /// Schedules the deletion of fanart for the media item with the specified <paramref name="mediaItemId"/>.
    /// </summary>
    /// <param name="mediaItemId">The media item id of the media item to delete fanart for.</param>
    void ScheduleFanArtDeletion(Guid mediaItemId);
  }
}
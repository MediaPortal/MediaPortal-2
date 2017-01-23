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

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// A media FanArt handler is responsible for collecting all available FanArt for a media item
  /// </summary>
  public interface IMediaFanArtHandler
  {
    /// <summary>
    /// Returns the metadata descriptor for this FanArt handler.
    /// </summary>
    FanArtHandlerMetadata Metadata { get; }

    /// <summary>
    /// Aspects that this handler can handle
    /// </summary>
    Guid[] FanArtAspects { get; }

    /// <summary>
    /// Collect all FanArt from a source and stores it in the cache
    /// </summary>
    void CollectFanArt(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects);

    /// <summary>
    /// Deletes no longer needed FanArt from the cache
    /// </summary>
    void DeleteFanArt(Guid mediaItemId);

    /// <summary>
    /// Clears the internal cache of checked items
    /// </summary>
    void ClearCache();
  }
}

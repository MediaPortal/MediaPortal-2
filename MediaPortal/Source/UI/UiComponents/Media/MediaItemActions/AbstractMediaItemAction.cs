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
using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UiComponents.Media.Extensions;

namespace MediaPortal.UiComponents.Media.MediaItemActions
{
  /// <summary>
  /// Base class for all <see cref="IMediaItemAction"/>s.
  /// </summary>
  public abstract class AbstractMediaItemAction : IMediaItemAction, IUserRestriction
  {
    /// <summary>
    /// Indicates if the given <paramref name="mediaItem"/> is managed by the MediaLibrary. That's the case if it has a <see cref="MediaItem.MediaItemId"/> other than <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="mediaItem">MediaItem.</param>
    /// <returns><c>true</c> if known by MediaLibrary.</returns>
    public virtual bool IsManagedByMediaLibrary(MediaItem mediaItem)
    {
      return mediaItem != null && mediaItem.MediaItemId != Guid.Empty;
    }

    public abstract Task<bool> IsAvailableAsync(MediaItem mediaItem);
    public abstract Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem);
    public string RestrictionGroup { get; set; }
  }
}

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

using MediaPortal.Common.MediaManagement;

namespace MediaPortal.UiComponents.Media.Extensions
{
  /// <summary>
  /// Extension interface to add actions for <see cref="MediaItem"/>s. Plugins can implement this interface and register the class in
  /// <c>plugin.xml</c> <see cref="MediaItemActionBuilder.MEDIA_EXTENSION_PATH"/> path.
  /// </summary>
  public interface IMediaItemAction
  {
    /// <summary>
    /// Checks if this action is available for the given <paramref name="mediaItem"/>.
    /// </summary>
    /// <param name="mediaItem">MediaItem</param>
    /// <returns><c>true</c> if available</returns>
    bool IsAvailable(MediaItem mediaItem);

    /// <summary>
    /// Executes the action for the given MediaItem.
    /// </summary>
    /// <param name="mediaItem">MediaItem</param>
    /// <param name="changeType">Outputs what kind of changed was done on MediaItem.</param>
    /// <returns><c>true</c> if successful</returns>
    bool Process(MediaItem mediaItem, out ContentDirectoryMessaging.MediaItemChangeType changeType);
  }
}

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

using System.Threading.Tasks;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;

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
    Task<bool> IsAvailableAsync(MediaItem mediaItem);

    /// <summary>
    /// Executes the action for the given MediaItem.
    /// </summary>
    /// <param name="mediaItem">MediaItem</param>
    /// <returns>
    /// <see cref="AsyncResult{T}.Success"/> <c>true</c> if successful.
    /// <see cref="AsyncResult{T}.Result"/> returns what kind of changes was done on MediaItem.
    /// </returns>
    Task<AsyncResult<ContentDirectoryMessaging.MediaItemChangeType>> ProcessAsync(MediaItem mediaItem);
  }

  /// <summary>
  /// Defined for actions that need a confirmation before.
  /// </summary>
  public interface IMediaItemActionConfirmation : IMediaItemAction
  {
    /// <summary>
    /// Gets the confirmation message.
    /// </summary>
    string ConfirmationMessage { get; }
  }

  /// <summary>
  /// Marker interface for actions that need a deferred execution in the former NavigationContext.
  /// </summary>
  public interface IDeferredMediaItemAction : IMediaItemAction
  {
  }
}

#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.MediaManagement;

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// Mode to control when a media item will be started relative to an already played media item.
  /// </summary>
  public enum StartTime
  {
    /// <summary>
    /// The new item will be started at once.
    /// </summary>
    AtOnce,

    /// <summary>
    /// The new item will be played when the current item ends.
    /// </summary>
    Enqueue,
  }

  public delegate void RequestNextItemDlgt(IPlayer player);

  /// <summary>
  /// Players with this interface implemented are able to reconfigure their input source. This can be sensible
  /// if the player needs a long time to build its media graph or other structures, or when it is able to cross-fade.
  /// </summary>
  public interface IReusablePlayer : IPlayer
  {
    /// <summary>
    /// Event which is fired when the current media item is about to end. The player raises this event when it needs
    /// the next media item in its input queue. If the player does a cross-fading, the time when this event is raised might
    /// be some seconds before a currently playing item ends.
    /// </summary>
    event RequestNextItemDlgt NextItemRequest;

    /// <summary>
    /// Schedules the specified next item in this player.
    /// </summary>
    /// <remarks>
    /// The caller only should call this method in two situations:
    /// <list type="bullet">
    /// <item>When the currently playing item should be replaced by the new item at once</item>
    /// <item>When this player raised its event <see cref="NextItemRequest"/></item>
    /// </list>
    /// That means, this method is not intended to model a play list in the player; especially is it not provided for
    /// a management of the next media item once it is added by this method; the new item should be regarded as the new
    /// "current" item.
    /// </remarks>
    /// <param name="locator">Media locator to the media resource to be played.</param>
    /// <param name="mimeType">MimeType of the content to be played, if available. Else, this
    /// parameter should be set to <c>null</c>.</param>
    /// <param name="startTime">Time when to start the new media item. If cross-fading is enabled, the player will
    /// try to cross-fade the item into the current item.</param>
    /// <returns><c>true</c>, if this player is able to play the specified next item, else <c>false</c>. In case
    /// <c>false</c> is returned, the player will continue to play as before.</returns>
    bool NextItem(IResourceLocator locator, string mimeType, StartTime startTime);
  }
}
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

using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// Players with this interface implemented are able to reconfigure their input source. This can be sensible
  /// if the player needs a long time to build its media graph or other structures, or when it can do cross-fading.
  /// </summary>
  public interface IReusablePlayer : IPlayer
  {
    /// <summary>
    /// Plays the specified next item in this player.
    /// </summary>
    /// <param name="locator">Media locator to the media resource to be played.</param>
    /// <param name="mimeType">MimeType of the content to be played, if available. Else, this
    /// parameter should be set to <c>null</c>.</param>
    /// <returns><c>true</c>, if this player is able to play the specified next item, else <c>false</c>. In case
    /// <c>false</c> is returned, the player will continue to play as before.</returns>
    bool NextItem(IMediaItemLocator locator, string mimeType);
  }
}
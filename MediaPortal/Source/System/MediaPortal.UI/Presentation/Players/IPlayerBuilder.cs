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
  /// Builder interface managing players which potentially are able to play a given media resource.
  /// </summary>
  /// <remarks>
  /// When a media file should be played, the system has to find an appropriate player which is able
  /// to play the given media item resource.
  /// Instances of this interface are able to decide if one of their managed players is able to play
  /// the given resource and can build a player for the resource.
  /// Typically, this interface will be implemented by one single class in each plugin which provides
  /// one or more players to the system.
  /// To get an appropriate player for a given media resource from outside, don't use instances of
  /// this interface directly.
  /// Use the <see cref="IPlayerManager"/> service instead.
  /// </remarks>
  public interface IPlayerBuilder
  {
    /// <summary>
    /// Returns an appropriate player to play the specified media resource.
    /// </summary>
    /// <param name="locator">Resource locator to get access to the media resource.</param>
    /// <param name="mimeType">Mime type of the media item, if known. If this parameter is given, the
    /// method might be able to return faster. If this parameter is set to <c>null</c>,
    /// this method will potentially need to look into the given resource before it is able to choose
    /// an appropriate player for it.</param>
    /// <returns>Player instance which is able to play the specified resource, or <c>null</c>, if there
    /// is no player available in this player builder which can play it.</returns>
    IPlayer GetPlayer(IResourceLocator locator, string mimeType);
  }
}

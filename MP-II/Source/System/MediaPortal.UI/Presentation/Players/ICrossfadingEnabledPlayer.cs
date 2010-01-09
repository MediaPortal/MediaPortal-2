#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using System;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// Mode to control how the duration of crossfading will be calculated.
  /// </summary>
  public enum CrossFadeMode
  {
    /// <summary>
    /// The cross-fading will last a defined duration.
    /// </summary>
    FadeDuration,

    /// <summary>
    /// The cross-fading will last until the end of the currently playing item.
    /// </summary>
    UntilEnd,
  }

  /// <summary>
  /// Player which is able to cross-fade the next media item into the currently playing item.
  /// </summary>
  public interface ICrossfadingEnabledPlayer : IPlayer
  {
    /// <summary>
    /// Tries to crossfade the currently playing media item with the mediaitem specified by
    /// the <paramref name="locator"/>.
    /// </summary>
    /// <remarks>
    /// This method should start the crossfading immediately, with the specified <paramref name="fadeMode"/>
    /// and <paramref name="fadeDuration"/>.
    /// </remarks>
    /// <param name="locator">Resource locator specifying the media item to be faded in.</param>
    /// <param name="mimeType">MimeType of the content to be faded in, if available. Else, this
    /// parameter should be set to <c>null</c>.</param>
    /// <param name="fadeMode">Mode specifying how the fade duration shold be calculated.</param>
    /// <param name="fadeDuration">If <paramref name="fadeMode"/> == <see cref="CrossFadeMode.FadeDuration"/>,
    /// this parameter specifies the duration to fade the new item in.</param>
    /// <returns><c>true</c>, if this player is able to cross-fade the specified item, else <c>false</c>. In case
    /// <c>false</c> is returned, the player will continue to play as before.</returns>
    bool Crossfade(IResourceLocator locator, string mimeType, CrossFadeMode fadeMode, TimeSpan fadeDuration);

    /// <summary>
    /// Returns the information if this player is currently executing a cross-fading action.
    /// </summary>
    bool IsCrossFading { get;  }
  }
}
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

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// Interface which can be implemented by player classes which are able to produce subtitles to its current content.
  /// This interface works additive to other implemented player interfaces.
  /// </summary>
  public interface ISubtitlePlayer
  {
    /// <summary>
    /// Returns list of available, localized names of subtitle streams. The list may be ordered
    /// by relevance or by some other criterion.
    /// </summary>
    string[] Subtitles { get; }

    /// <summary>
    /// Sets the current subtitle.
    /// </summary>
    /// <param name="subtitle">The name of the subtitle to set. Must be one of the values from the
    /// <see cref="Subtitles"/> list.</param>
    void SetSubtitle(string subtitle);

    /// <summary>
    /// Disables the subtitle, i.e. switches it off.
    /// </summary>
    void DisableSubtitle();

    /// <summary>
    /// Gets the current subtitle. Returns <c>null</c> if no subtitle is currently set.
    /// </summary>
    string CurrentSubtitle { get; }
  }
}
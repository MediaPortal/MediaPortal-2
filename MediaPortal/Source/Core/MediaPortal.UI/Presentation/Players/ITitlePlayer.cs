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
  /// Interface for players that support different titles. Titles are usually supported by DVD, BluRay or Matroska video files that contain editions.
  /// </summary>
  public interface ITitlePlayer
  {
    /// <summary>
    /// Gets an ordered list of localized titles/editions.
    /// </summary>
    string[] Titles { get; }

    /// <summary>
    /// Plays the given title/edition.
    /// </summary>
    /// <param name="title">The name of the title/edition to set. Must be one of the title/edition names from the <see cref="Titles"/> list.</param>
    void SetTitle(string title);

    /// <summary>
    /// Gets the current title/edition.
    /// </summary>
    string CurrentTitle { get; }
  }
}

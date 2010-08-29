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

namespace MediaPortal.UI.Presentation.Players
{
  /// <summary>
  /// Interface of a DVD player.
  /// </summary>
  public interface IDVDPlayer : IPlayer
  {
    /// <summary>
    /// Gets an ordered list of localized DVD titles.
    /// </summary>
    string[] DvdTitles { get; }

    /// <summary>
    /// Plays the given DVD title.
    /// </summary>
    /// <param name="title">The name of the title to set. Must be one of the title names from the
    /// <see cref="DvdTitles"/> list.</param>
    void SetDvdTitle(string title);

    /// <summary>
    /// Gets the current DVD title.
    /// </summary>
    string CurrentDvdTitle { get; }

    /// <summary>
    /// Gets an ordered list of localized DVD chapters for the current title.
    /// </summary>
    string[] DvdChapters { get; }

    /// <summary>
    /// Plays the given DVD chapter.
    /// </summary>
    /// <param name="chapter">Name of the chapter to set. Must be one of the chapter names from the
    /// <see cref="DvdChapters"/> list.</param>
    void SetDvdChapter(string chapter);

    /// <summary>
    /// Gets the current DVD chapter.
    /// </summary>
    string CurrentDvdChapter { get; }

    /// <summary>
    /// Gets the information whether we are in the in DVD menu.
    /// </summary>
    bool InDvdMenu { get; }

    /// <summary>
    /// Enters the DVD menu.
    /// </summary>
    void ShowDvdMenu();
  }
}
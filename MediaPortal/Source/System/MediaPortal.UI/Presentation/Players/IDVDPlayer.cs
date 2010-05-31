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
  /// Interface of each DVD player.
  /// </summary>
  public interface IDVDPlayer : IPlayer
  {
    // TODO: Tidy up from here
    /// <summary>
    /// Gets the DVD titles.
    /// </summary>
    /// <value>The DVD titles.</value>
    string[] DvdTitles { get; }

    /// <summary>
    /// Sets the DVD title.
    /// </summary>
    /// <param name="title">The title.</param>
    void SetDvdTitle(string title);

    /// <summary>
    /// Gets the current DVD title.
    /// </summary>
    /// <value>The current DVD title.</value>
    string CurrentDvdTitle { get; }


    /// <summary>
    /// Gets the DVD chapters for current title
    /// </summary>
    /// <value>The DVD chapters.</value>
    string[] DvdChapters { get; }

    /// <summary>
    /// Sets the DVD chapter.
    /// </summary>
    /// <param name="title">The title.</param>
    void SetDvdChapter(string title);

    /// <summary>
    /// Gets the current DVD chapter.
    /// </summary>
    /// <value>The current DVD chapter.</value>
    string CurrentDvdChapter { get; }

    /// <summary>
    /// Gets a value indicating whether we are in the in DVD menu.
    /// </summary>
    /// <value><c>true</c> if [in DVD menu]; otherwise, <c>false</c>.</value>
    bool InDvdMenu { get; }
  }
}
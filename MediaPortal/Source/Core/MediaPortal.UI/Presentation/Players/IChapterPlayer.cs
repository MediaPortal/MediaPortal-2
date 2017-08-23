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
  /// Interface for players that support chapters handling. Chapters are usually supported by DVD, BluRay or video files with embedded metadata (like mkv).
  /// </summary>
  public interface IChapterPlayer 
  {
    /// <summary>
    /// Gets an ordered list of chapters.
    /// </summary>
    string[] Chapters { get; }

    /// <summary>
    /// Plays the given chapter.
    /// </summary>
    /// <param name="chapter">Name of the chapter to set. Must be one of the chapter names from the
    /// <see cref="Chapters"/> list.</param>
    void SetChapter(string chapter);

    /// <summary>
    /// Indicate if chapters are available.
    /// </summary>
    bool ChaptersAvailable { get; }

    /// <summary>
    /// Skips to the next chapter.
    /// </summary>
    void NextChapter();

    /// <summary>
    /// Skips to the previous chapter.
    /// </summary>
    void PrevChapter();

    /// <summary>
    /// Gets the current chapter.
    /// </summary>
    string CurrentChapter { get; }
  }
}
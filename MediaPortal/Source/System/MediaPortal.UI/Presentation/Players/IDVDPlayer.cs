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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.UI.Control.InputManager;

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
    /// Gets the current DVD chapter.
    /// </summary>
    string CurrentDvdChapter { get; }

    /// <summary>
    /// Gets the information whether the DVD player currently handles key and mouse events. This is the case if we
    /// are currently in DVD menu.
    /// </summary>
    bool IsHandlingUserInput { get; }

    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    void ShowDvdMenu();

    /// <summary>
    /// If <see cref="IsHandlingUserInput"/> is <c>true</c>, this method should be called on mouse moves to control the DVD menu.
    /// </summary>
    /// <param name="x">Relative X coordinate of the mouse cursor. Ranges from 0 to 1 relative to the DVD video picture size.</param>
    /// <param name="y">Relative Y coordinate of the mouse cursor. Ranges from 0 to 1 relative to the DVD video picture size.</param>
    void OnMouseMove(float x, float y);

    /// <summary>
    /// If <see cref="IsHandlingUserInput"/> is <c>true</c>, this method should be called on mouse clicks to control the DVD menu.
    /// </summary>
    /// <param name="x">Relative X coordinate of the mouse cursor. Ranges from 0 to 1 relative to the DVD video picture size.</param>
    /// <param name="y">Relative Y coordinate of the mouse cursor. Ranges from 0 to 1 relative to the DVD video picture size.</param>
    void OnMouseClick(float x, float y);

    /// <summary>
    /// If <see cref="IsHandlingUserInput"/> is <c>true</c>, this method has to be called on key events to control the DVD menu.
    /// </summary>
    /// <param name="key">The key which was pressed.</param>
    void OnKeyPress(Key key);
  }
}
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

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// Interface for each player class which is able to produce subtitles to its current content.
  /// This interface works additive to other implemented player interfaces.
  /// </summary>
  public interface ISubtitlePlayer
  {
    // TODO: Tidy up from here
    /// <summary>
    /// returns list of available subtitle streams
    /// </summary>
    string[] Subtitles { get; }

    /// <summary>
    /// sets the current subtitle
    /// </summary>
    /// <param name="subtitle">subtitle</param>
    void SetSubtitle(string subtitle);

    /// <summary>
    /// Gets the current subtitle.
    /// </summary>
    /// <value>The current subtitle.</value>
    string CurrentSubtitle { get; }
  }
}
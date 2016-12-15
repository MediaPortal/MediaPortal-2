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
  /// Interface for players that support resuming of playback.
  /// </summary>
  public interface IResumablePlayer
  {
    /// <summary>
    /// Gets a <see cref="IResumeState"/> from the player.
    /// </summary>
    /// <param name="state">Outputs resume state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    bool GetResumeState(out IResumeState state);

    /// <summary>
    /// Sets a <see cref="IResumeState"/> to the player. The player is responsible to make the required initializations.
    /// </summary>
    /// <param name="state">Resume state.</param>
    /// <returns><c>true</c> if successful, otherwise <c>false</c>.</returns>
    bool SetResumeState(IResumeState state);
  }

  /// <summary>
  /// Marker interface for resume state classes.
  /// </summary>
  public interface IResumeState
  { }
}

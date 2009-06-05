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

using System;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// Interface for controlling a player's position and playback speed.
  /// </summary>
  /// <remarks>
  /// This interface works additive to other implemented player interfaces.
  /// </remarks>
  public interface IMediaPlaybackControl
  {
    /// <summary>
    /// Returns the current play time.
    /// </summary>
    TimeSpan CurrentTime { get; set; }

    /// <summary>
    /// Returns the playing duration of the media item.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets or sets the playback rate as a ratio to the normal speed. Thus, 1.0 means normal playback speed,
    /// 2.0 is double speed, 0.5 is half playback speed.
    /// </summary>
    double PlaybackRate { get; set; }

    /// <summary>
    /// Returns the information whether this player is in paused state.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets the information if we can seek forward, i.e. set a <see cref="PlaybackRate"/> bigger than 1.
    /// </summary>
    bool CanSeekForward { get; }

    /// <summary>
    /// Gets the information if we can seek backward, i.e. set a <see cref="PlaybackRate"/> below 0.
    /// </summary>
    bool CanSeekBackward { get; }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes playback.
    /// </summary>
    void Resume();

    /// <summary>
    /// Restarts playback from the beginning.
    /// </summary>
    void Restart();
  }
}

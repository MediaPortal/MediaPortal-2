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

using System.Drawing;

namespace MediaPortal.Presentation.Players
{
  /// <summary>
  /// General interface for a video player. Holds most of the methods which are common to all video players.
  /// </summary>
  /// <remarks>
  /// This interface doesn't support methods responsible to render content to the screen - video players
  /// need to implement one or more additional interface(s) to support rendering capabilities. Those
  /// additional interface(s) will be provided by the skin engine, the player is written for.
  /// This means, a player always is written for a special skin engine.
  /// </remarks>
  public interface IVideoPlayer : IPlayer, IVolumeControl
  {
    /// <summary>
    /// Returns the original size of the video picture.
    /// </summary>
    Size VideoSize { get; }

    /// <summary>
    /// Returns the aspect ratio of the video.
    /// </summary>
    Size VideoAspectRatio { get;  }

    // TODO: Tidy up from here
    /// <summary>
    /// returns list of available audio streams
    /// </summary>
    string[] AudioStreams { get; }

    /// <summary>
    /// sets the current audio stream
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    void SetAudioStream(string audioStream);

    /// <summary>
    /// Gets the current audio stream.
    /// </summary>
    /// <value>The current audio stream.</value>
    string CurrentAudioStream { get; }
  }
}
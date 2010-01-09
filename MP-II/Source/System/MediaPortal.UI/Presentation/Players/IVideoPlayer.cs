#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.Presentation.Geometries;

namespace MediaPortal.UI.Presentation.Players
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

    /// <summary>
    /// Gets or sets a geometry to be used for this player. If this property is not set, a default geometry will be used.
    /// In this case, this property is <c>null</c>.
    /// </summary>
    IGeometry GeometryOverride { get; set; }

    /// <summary>
    /// Returns a list of names of available audio streams. The list may be ordered by relevance or by some other criterion.
    /// </summary>
    string[] AudioStreams { get; }

    /// <summary>
    /// Sets the current audio stream.
    /// </summary>
    /// <param name="audioStream">Name of the audio stream to set. The name should be equal to some of the stream names returned
    /// by the <see cref="AudioStreams"/> property.</param>
    void SetAudioStream(string audioStream);

    /// <summary>
    /// Gets the name of the current audio stream.
    /// </summary>
    string CurrentAudioStream { get; }
  }
}
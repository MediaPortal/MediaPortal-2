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
using System.Drawing;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Presentation.Players
{
  public enum PlaybackState
  {
    Playing,
    Paused,
    Stopped,
    Ended
  };

  /// <summary>
  /// Interface for an audio or video player which is already prepared to play a media resource.
  /// </summary>
  public interface IPlayer
  {
    /// <summary>
    /// Gets the Name of the Player
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns a unique id for this player.
    /// </summary>
    Guid PlayerId { get; }

    /// <summary>
    /// Releases any GUI resources.
    /// </summary>
    void ReleaseGUIResources();

    /// <summary>
    /// Reallocs any GUI resources.
    /// </summary>
    void ReallocGUIResources();

    /// <summary>
    /// Gets the playback state of this player.
    /// </summary>
    PlaybackState State { get; }

    // TODO: go on with rework of interface from here

    /// <summary>
    /// gets/sets the width/height for the video window
    /// </summary>
    Size Size { get; set; }

    /// <summary>
    /// gets/sets the position on screen where the video should be drawn
    /// </summary>
    Point Position { get; set; }
    /// <summary>
    /// Gets the onscreen rectangle where movie gets rendered
    /// </summary>
    /// <value>The movie rectangle.</value>
    Rectangle MovieRectangle { get;set;}

    /// <summary>
    /// gets/sets the alphamask
    /// </summary>
    Rectangle AlphaMask { get; set; }

    /// <summary>
    /// Plays the file
    /// </summary>
    /// <param name="fileName"></param>
    void Play(MediaItem item);

    /// <summary>
    /// stops playback
    /// </summary>
    void Stop();

    /// <summary>
    /// Render the video
    /// </summary>
    void Render();
    void BeginRender(object effect);
    void EndRender(object effect);

    /// <summary>
    /// gets/sets wheter video is paused
    /// </summary>
    bool Paused { get; set; }

    /// <summary>
    /// called when windows message is received
    /// </summary>
    /// <param name="m">message</param>
    void OnMessage(object m);

    /// <summary>
    /// called when application is idle
    /// </summary>
    void OnIdle();

    /// <summary>
    /// returns the current play time
    /// </summary>
    TimeSpan CurrentTime { get; set; }
    TimeSpan StreamPosition { get;  }

    /// <summary>
    /// returns the duration of the movie
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// returns list of available audio streams
    /// </summary>
    string[] AudioStreams { get; }

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

    Size VideoSize { get; }
    Size VideoAspectRatio { get;  }

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

    /// <summary>
    /// Gets the name of the file.
    /// </summary>
    /// <value>The name of the file.</value>
    Uri FileName { get; }

    /// <summary>
    /// Gets the media-item.
    /// </summary>
    /// <value>The media-item.</value>
    MediaItem MediaItem { get;}

    /// <summary>
    /// Restarts playback from the start.
    /// </summary>
    void Restart();

    /// <summary>
    ///Resumes playback from previous session
    /// </summary>
    void ResumeSession();

    /// <summary>
    /// True if resume data exists (from previous session)
    /// </summary>
    bool CanResumeSession(Uri fileName);

    /// <summary>
    /// Gets or sets the volume (0-100)
    /// </summary>
    /// <value>The volume.</value>
    int Volume { get;set;}

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="IPlayer"/> is mute.
    /// </summary>
    /// <value><c>true</c> if muted; otherwise, <c>false</c>.</value>
    bool Mute { get;set;}

    /// <summary>
    /// Gets a value indicating whether this player is a video player.
    /// </summary>
    /// <value><c>true</c> if this player is a video player; otherwise, <c>false</c>.</value>
    bool IsVideo { get;}

    /// <summary>
    /// Gets a value indicating whether this player is a audio player.
    /// </summary>
    /// <value><c>true</c> if this player is a audio player; otherwise, <c>false</c>.</value>
    bool IsAudio { get;}

    /// <summary>
    /// Gets a value indicating whether this player is a picture player.
    /// </summary>
    /// <value><c>true</c> if this player is a picture player; otherwise, <c>false</c>.</value>
    bool IsImage { get;}
  }
}

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
using MediaPortal.Core;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Messaging;

using MediaPortal.Media.MediaManagement;
using MediaPortal.Presentation.Screens;

namespace Media.Players.PicturePlayer
{
  public class PicturePlayer : IPlayer
  {
    public const string PICTUREVIEWERQUEUE_NAME = "PictureViewer";

    IMediaItem _mediaItem;
    PlaybackState _state;

    #region IPlayer Members

    /// <summary>
    /// Plays the file
    /// </summary>
    /// <param name="item"></param>
    public void Play(IMediaItem item)
    {
      //here we simply show the picture viewer window
      //from the my pictures window
      //and tell it to show the image
      _state = PlaybackState.Playing;
      _mediaItem = item;
      ServiceScope.Get<IScreenManager>().PrepareScreen("pictureviewer");
      IMessageBroker msgBroker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = msgBroker.GetOrCreate(PICTUREVIEWERQUEUE_NAME);
      QueueMessage msg = new QueueMessage();
      msg.MessageData["action"] = "show";
      msg.MessageData["mediaitem"] = item;
      queue.Send(msg);
      ServiceScope.Get<IScreenManager>().ShowScreen("pictureviewer");
    }

    /// <summary>
    /// stops playback
    /// </summary>
    public void Stop()
    {
      //remove this player
      ServiceScope.Get<IPlayerCollection>().Remove(this);
      //goto the previous window
      ServiceScope.Get<IScreenManager>().ShowPreviousScreen();
    }
    /// <summary>
    /// called when windows message is received
    /// </summary>
    /// <param name="m">message</param>
    public void OnMessage(object m)
    {
      if (_state == PlaybackState.Playing)
      {
        if (ServiceScope.Get<IScreenManager>().CurrentScreenName != "pictureviewer")
        {
          _state = PlaybackState.Stopped;
          ServiceScope.Get<IPlayerCollection>().Remove(this);
        }
      }
    }
    #endregion

    #region IPlayer properties

    /// <summary>
    /// gets the Name of the Player
    /// </summary>
    /// <value></value>
    public string Name
    {
      get
      {
        return "PicturePlayer";
      }
    }

    /// <summary>
    /// Releases any gui resources.
    /// </summary>
    public void ReleaseResources()
    {

    }

    /// <summary>
    /// Reallocs any gui resources.
    /// </summary>
    public void ReallocResources()
    {

    }

    /// <summary>
    /// gets the playback state
    /// </summary>
    /// <value></value>
    public PlaybackState State
    {
      get { return _state; }
    }

    /// <summary>
    /// gets/sets the width/height for the video window
    /// </summary>
    /// <value></value>
    public System.Drawing.Size Size
    {
      get
      {
        return new System.Drawing.Size(0, 0);

      }
      set
      {

      }
    }

    public Size VideoSize { get { return new Size(0, 0); } }
    public Size VideoAspectRatio { get { return new Size(0, 0); } }
    /// <summary>
    /// gets/sets the position on screen where the video should be drawn
    /// </summary>
    /// <value></value>
    public System.Drawing.Point Position
    {
      get
      {

        return new System.Drawing.Point(0, 0);
      }
      set
      {

      }
    }

    /// <summary>
    /// Gets the onscreen rectangle where movie gets rendered
    /// </summary>
    /// <value>The movie rectangle.</value>
    public System.Drawing.Rectangle MovieRectangle
    {
      get
      {
        return new System.Drawing.Rectangle(0, 0, 0, 0);
      }
      set
      {
      }
    }

    /// <summary>
    /// gets/sets the alphamask
    /// </summary>
    /// <value></value>
    public System.Drawing.Rectangle AlphaMask
    {
      get
      {
        return new System.Drawing.Rectangle(0, 0, 0, 0);
      }
      set
      {

      }
    }


    /// <summary>
    /// Render the video
    /// </summary>
    public void Render()
    {
    }

    /// <summary>
    /// gets/sets wheter video is paused
    /// </summary>
    /// <value></value>
    public bool Paused
    {
      get
      {
        return false;
      }
      set
      {

      }
    }


    /// <summary>
    /// called when application is idle
    /// </summary>
    public void OnIdle()
    {

    }

    /// <summary>
    /// returns the current play time
    /// </summary>
    /// <value></value>
    public TimeSpan CurrentTime
    {
      get
      {
        return new TimeSpan(0, 0, 0);
      }
      set
      {

      }
    }


    public void BeginRender(object effect)
    {
    }
    public void EndRender(object effect)
    {
    }

    /// <summary>
    /// Gets the stream position.
    /// </summary>
    /// <value>The stream position.</value>
    public TimeSpan StreamPosition
    {
      get { return new TimeSpan(0, 0, 0); }
    }

    /// <summary>
    /// returns the duration of the movie
    /// </summary>
    /// <value></value>
    public TimeSpan Duration
    {
      get { return new TimeSpan(0, 0, 3); }
    }

    /// <summary>
    /// returns list of available audio streams
    /// </summary>
    /// <value></value>
    public string[] AudioStreams
    {
      get { return null; }
    }

    /// <summary>
    /// returns list of available subtitle streams
    /// </summary>
    /// <value></value>
    public string[] Subtitles
    {
      get { return null; }
    }

    /// <summary>
    /// sets the current subtitle
    /// </summary>
    /// <param name="subtitle">subtitle</param>
    public void SetSubtitle(string subtitle)
    {

    }

    /// <summary>
    /// Gets the current subtitle.
    /// </summary>
    /// <value>The current subtitle.</value>
    public string CurrentSubtitle
    {
      get { return ""; }
    }

    /// <summary>
    /// sets the current audio stream
    /// </summary>
    /// <param name="audioStream">audio stream</param>
    public void SetAudioStream(string audioStream)
    {

    }

    /// <summary>
    /// Gets the current audio stream.
    /// </summary>
    /// <value>The current audio stream.</value>
    public string CurrentAudioStream
    {
      get { return ""; }
    }

    /// <summary>
    /// Gets the DVD titles.
    /// </summary>
    /// <value>The DVD titles.</value>
    public string[] DvdTitles
    {
      get { return null; }
    }

    /// <summary>
    /// Sets the DVD title.
    /// </summary>
    /// <param name="title">The title.</param>
    public void SetDvdTitle(string title)
    {

    }

    /// <summary>
    /// Gets the current DVD title.
    /// </summary>
    /// <value>The current DVD title.</value>
    public string CurrentDvdTitle
    {
      get { return ""; }
    }

    /// <summary>
    /// Gets the DVD chapters for current title
    /// </summary>
    /// <value>The DVD chapters.</value>
    public string[] DvdChapters
    {
      get { return null; }
    }

    /// <summary>
    /// Sets the DVD chapter.
    /// </summary>
    /// <param name="title">The title.</param>
    public void SetDvdChapter(string title)
    {

    }

    /// <summary>
    /// Gets the current DVD chapter.
    /// </summary>
    /// <value>The current DVD chapter.</value>
    public string CurrentDvdChapter
    {
      get { return ""; }
    }

    /// <summary>
    /// Gets a value indicating whether we are in the in DVD menu.
    /// </summary>
    /// <value><c>true</c> if [in DVD menu]; otherwise, <c>false</c>.</value>
    public bool InDvdMenu
    {
      get { return false; }
    }

    /// <summary>
    /// Gets the name of the file.
    /// </summary>
    /// <value>The name of the file.</value>
    public Uri FileName
    {
      get { return _mediaItem.ContentUri; }
    }

    /// <summary>
    /// Gets the media-item.
    /// </summary>
    /// <value>The media-item.</value>
    public IMediaItem MediaItem
    {
      get { return _mediaItem; }
    }

    /// <summary>
    /// Restarts playback from the start.
    /// </summary>
    public void Restart()
    {

    }

    /// <summary>
    /// Resumes playback from previous session
    /// </summary>
    public void ResumeSession()
    {

    }

    /// <summary>
    /// True if resume data exists (from previous session)
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public bool CanResumeSession(Uri fileName)
    {
      return false;
    }

    /// <summary>
    /// Gets or sets the volume (0-100)
    /// </summary>
    /// <value>The volume.</value>
    public int Volume
    {
      get
      {
        return 0;
      }
      set
      {

      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="IPlayer"/> is mute.
    /// </summary>
    /// <value><c>true</c> if muted; otherwise, <c>false</c>.</value>
    public bool Mute
    {
      get
      {
        return false;
      }
      set
      {

      }
    }

    /// <summary>
    /// Gets a value indicating whether this player is a video player.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this player is a video player; otherwise, <c>false</c>.
    /// </value>
    public bool IsVideo
    {
      get { return false; }
    }

    /// <summary>
    /// Gets a value indicating whether this player is a audio player.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this player is a audio player; otherwise, <c>false</c>.
    /// </value>
    public bool IsAudio
    {
      get { return false; }
    }

    /// <summary>
    /// Gets a value indicating whether this player is a picture player.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this player is a picture player; otherwise, <c>false</c>.
    /// </value>
    public bool IsImage
    {
      get
      {
        return true;
      }
    }

    #endregion
  }
}

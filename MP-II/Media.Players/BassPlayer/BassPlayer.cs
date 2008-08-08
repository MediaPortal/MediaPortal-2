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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Media.MediaManager;

using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace Media.Players.BassPlayer
{
  public class BassPlayer : IPlayer
  {
    #region IPlayer Members
    IPlayer _playerInstance;

    public BassPlayer(IPlayer instance)
    {
      _playerInstance = instance;
    }

    public string Name
    {
      get
      {
        return _playerInstance.Name;
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
      get
      {
        return false;
      }
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
        return false;
      }
    }
    /// <summary>
    /// Gets a value indicating whether this player is a audio player.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this player is a audio player; otherwise, <c>false</c>.
    /// </value>
    public bool IsAudio
    {
      get
      {
        return true;
      }
    }
    public void BeginRender(object effect)
    {
    }
    public void EndRender(object effect)
    {
    }

    public void ReleaseResources()
    {
    }

    public void ReallocResources()
    {
    }

    public PlaybackState State
    {
      get
      {
        return _playerInstance.State;
      }
    }

    public Size Size
    {
      get
      {
        return _playerInstance.Size;
      }
      set
      {
        _playerInstance.Size = value;
      }
    }

    public Point Position
    {
      get
      {
        return _playerInstance.Position;
      }
      set
      {
        _playerInstance.Position = value;
      }
    }

    public Rectangle MovieRectangle
    {
      get
      {
        return _playerInstance.MovieRectangle;
      }
      set
      {
        _playerInstance.MovieRectangle = value;
      }
    }

    public Rectangle AlphaMask
    {
      get
      {
        return _playerInstance.AlphaMask;
      }
      set
      {
        _playerInstance.AlphaMask = value;
      }
    }

    public void Play(IMediaItem item)
    {
      _playerInstance.Play(item);
    }

    public void Stop()
    {
      _playerInstance.Stop();
      ServiceScope.Get<PlayerCollection>().Remove(this);
    }

    public void Render()
    {
      _playerInstance.Render();
    }

    public bool Paused
    {
      get
      {
        return _playerInstance.Paused;
      }
      set
      {
        _playerInstance.Paused = value;
      }
    }

    public void OnMessage(object m)
    {
      _playerInstance.OnMessage(m);
    }

    public void OnIdle()
    {
      _playerInstance.OnIdle();
    }

    public TimeSpan CurrentTime
    {
      get
      {
        return _playerInstance.CurrentTime;
      }
      set
      {
        _playerInstance.CurrentTime = value;
      }
    }

    public TimeSpan StreamPosition
    {
      get
      {
        return _playerInstance.StreamPosition;
      }
    }

    public TimeSpan Duration
    {
      get
      {
        return _playerInstance.Duration;
      }
    }

    public string[] AudioStreams
    {
      get
      {
        return _playerInstance.AudioStreams;
      }
    }

    public string[] Subtitles
    {
      get
      {
        return _playerInstance.Subtitles;
      }
    }

    public void SetSubtitle(string subtitle)
    {
      _playerInstance.SetSubtitle(subtitle);
    }

    public string CurrentSubtitle
    {
      get
      {
        return _playerInstance.CurrentSubtitle;
      }
    }

    public void SetAudioStream(string audioStream)
    {
      _playerInstance.SetAudioStream(audioStream);
    }

    public string CurrentAudioStream
    {
      get
      {
        return _playerInstance.CurrentAudioStream;
      }
    }

    public string[] DvdTitles
    {
      get
      {
        return _playerInstance.DvdTitles;
      }
    }

    public void SetDvdTitle(string title)
    {
      _playerInstance.SetDvdTitle(title);
    }

    public string CurrentDvdTitle
    {
      get
      {
        return _playerInstance.CurrentDvdTitle;
      }
    }

    public string[] DvdChapters
    {
      get
      {
        return _playerInstance.DvdChapters;
      }
    }

    public void SetDvdChapter(string title)
    {
      _playerInstance.SetDvdChapter(title);
    }

    public string CurrentDvdChapter
    {
      get
      {
        return _playerInstance.CurrentDvdChapter;
      }
    }

    public bool InDvdMenu
    {
      get
      {
        return _playerInstance.InDvdMenu;
      }
    }

    public Uri FileName
    {
      get
      {
        return _playerInstance.FileName;
      }
    }

    public IMediaItem MediaItem
    {
      get
      {
        return _playerInstance.MediaItem;
      }
    }

    public void Restart()
    {
      _playerInstance.Restart();
    }

    public void ResumeSession()
    {
      _playerInstance.ResumeSession();
    }

    public bool CanResumeSession(Uri fileName)
    {
      return _playerInstance.CanResumeSession(fileName);
    }

    public int Volume
    {
      get
      {
        return _playerInstance.Volume;
      }
      set
      {
        _playerInstance.Volume = value;
      }
    }

    public bool Mute
    {
      get
      {
        return _playerInstance.Mute;
      }
      set
      {
        _playerInstance.Mute = value;
      }
    }

    public Size VideoSize { get { return new Size(0, 0); } }
    public Size VideoAspectRatio { get { return new Size(0, 0); } }
    #endregion
  }
}

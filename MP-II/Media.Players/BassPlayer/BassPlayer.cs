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
using MediaPortal.Core;
using MediaPortal.Core.Settings;
using MediaPortal.Presentation.Players;

namespace Media.Players.BassPlayer
{
  /// <summary>
  /// Music player based on the Un4seen Bass library.
  /// </summary>
  public partial class BassPlayer : IPlayer, IDisposable
  {
    #region Static members

    /// <summary>
    /// Creates and initializes an new instance.
    /// </summary>
    /// <returns>The new instance.</returns>
    public static BassPlayer Create(BassPlayerPlugin plugin)
    {
      BassPlayer player = new BassPlayer(plugin);
      player.Initialize();
      return player;
    }

    #endregion

    #region Fields

    private BassPlayerPlugin _Plugin;
    private BassLibraryManager _BassLibraryManager;
    private BassPlayerSettings _Settings;
    private Controller _Controller;
    private Monitor _Monitor;
    private InputSourceFactory _InputSourceFactory;
    private InputSourceQueue _InputSourceQueue;
    private InputSourceSwitcher _InputSourceSwitcher;
    private UpDownMixer _UpDownMixer;
    private VSTProcessor _VSTProcessor;
    private WinAmpDSPProcessor _WinAmpDSPProcessor;
    private PlaybackBuffer _PlaybackBuffer;
    private OutputDeviceManager _OutputDeviceManager;
    private PlaybackSession _PlaybackSession;

    #endregion

    #region Public members

    public BassPlayerSettings Settings
    {
      get { return _Settings; }
    }

    #endregion

    #region IPlayer Members

    public bool IsVideo
    {
      get { return false; }
    }

    public bool IsImage
    {
      get { return false; }
    }

    public bool IsAudio
    {
      get { return true; }
    }

    public bool Paused
    {
      get { return (_Controller.ExternalState == PlaybackState.Paused); }
      set
      {
        if (value)
          _Controller.Pause();
        else
          _Controller.Resume();
      }
    }

    public PlaybackState State
    {
      get { return _Controller.ExternalState; }
    }

    public TimeSpan CurrentTime
    {
      get { return _Monitor.CurrentPosition; }
      set { throw new NotImplementedException(); }
    }

    public TimeSpan Duration
    {
      get { return _Monitor.Duration; }
    }

    public void Play(MediaPortal.Media.MediaManager.IMediaItem item)
    {
      Log.Debug("Play()");
      _Controller.Play(item);
    }

    public void Stop()
    {
      Log.Debug("Stop()");
      _Controller.Stop();
    }

    #endregion

    #region Unused IPlayer Members

    public bool Mute
    {
      get { return false; }
      set { }
    }

    public int Volume
    {
      get { return 0; }
      set { }
    }

    public System.Drawing.Rectangle AlphaMask
    {
      get { return new System.Drawing.Rectangle(); }
      set { }
    }

    public string[] AudioStreams
    {
      get { return new string[0]; }
    }

    public void BeginRender(object effect)
    {
    }

    public bool CanResumeSession(Uri fileName)
    {
      return false;
    }

    public string CurrentAudioStream
    {
      get { return null; }
    }

    public string CurrentDvdChapter
    {
      get { return null; }
    }

    public string CurrentDvdTitle
    {
      get { return null; }
    }

    public string CurrentSubtitle
    {
      get { return null; }
    }

    public string[] DvdChapters
    {
      get { return new string[0]; }
    }

    public string[] DvdTitles
    {
      get { return new string[0]; }
    }

    public void EndRender(object effect)
    {
    }

    public Uri FileName
    {
      get { return null; }
    }

    public bool InDvdMenu
    {
      get { return false; }
    }

    public MediaPortal.Media.MediaManager.IMediaItem MediaItem
    {
      get { return null; }
    }

    public System.Drawing.Rectangle MovieRectangle
    {
      get
      {
        return new System.Drawing.Rectangle();
      }
      set
      {
      }
    }

    public string Name
    {
      get { return null; }
    }

    public void OnIdle()
    {
    }

    public void OnMessage(object m)
    {
    }


    public System.Drawing.Point Position
    {
      get
      {
        return new System.Drawing.Point();
      }
      set
      {
      }
    }

    public void ReallocResources()
    {
    }

    public void ReleaseResources()
    {
    }

    public void Render()
    {
    }

    public void Restart()
    {
    }

    public void ResumeSession()
    {
    }

    public void SetAudioStream(string audioStream)
    {
    }

    public void SetDvdChapter(string title)
    {
    }

    public void SetDvdTitle(string title)
    {
    }

    public void SetSubtitle(string subtitle)
    {
    }

    public System.Drawing.Size Size
    {
      get
      {
        return new System.Drawing.Size();
      }
      set
      {
      }
    }

    public string[] Subtitles
    {
      get { return new string[0]; }
    }

    public TimeSpan StreamPosition
    {
      get { return TimeSpan.Zero; }
    }

    public System.Drawing.Size VideoAspectRatio
    {
      get { return new System.Drawing.Size(); }
    }

    public System.Drawing.Size VideoSize
    {
      get { return new System.Drawing.Size(); }
    }
    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      Log.Debug("Disposing BassPlayer");

      _Controller.TerminateThread();
      _Monitor.TerminateThread();

      _OutputDeviceManager.Dispose();
      _PlaybackBuffer.Dispose();
      _WinAmpDSPProcessor.Dispose();
      _VSTProcessor.Dispose();
      _UpDownMixer.Dispose();
      _InputSourceSwitcher.Dispose();
      _InputSourceQueue.Dispose();
      _InputSourceFactory.Dispose();
      _Monitor.Dispose();
      _Controller.Dispose();

      _BassLibraryManager.Dispose();
    }

    #endregion

    #region Private members

    private BassPlayer(BassPlayerPlugin plugin)
    {
      _Plugin = plugin;
    }

    private void Initialize()
    {
      Log.Debug("Initializing BassPlayer");

      _BassLibraryManager = BassLibraryManager.Create();

      _Settings = _Plugin.Settings;

      _InputSourceFactory = new InputSourceFactory(this);

      _Controller = Controller.Create(this);
      _Monitor = Monitor.Create(this);

      _InputSourceQueue = new InputSourceQueue();
      _InputSourceSwitcher = InputSourceSwitcher.Create(this);
      _UpDownMixer = UpDownMixer.Create(this);
      _VSTProcessor = VSTProcessor.Create(this);
      _WinAmpDSPProcessor = WinAmpDSPProcessor.Create(this);
      _PlaybackBuffer = PlaybackBuffer.Create(this);
      _OutputDeviceManager = OutputDeviceManager.Create(this);
    }

    #endregion
  }
}

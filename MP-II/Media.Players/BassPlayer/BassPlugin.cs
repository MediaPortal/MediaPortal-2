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
using System.Threading;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.Settings;
using MediaPortal.Media.MediaManager;

using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;

namespace Media.Players.BassPlayer
{
  public class BassPlugin : IPluginStateTracker, IPlayer, IPlayerBuilder
  {
    #region Variables
    private BassPlayerSettings _settings;       // The Settings of the Player
    private List<Zone> _zones;                  // Available Zones == Audio Devices
    private bool _initialised = false;          // Is Bass Initialised

    private const int MAXSTREAMS = 2;           // Maximum Number of Streams active at the same time
    private List<Stream> Streams = new List<Stream>(MAXSTREAMS);  // List with Streams
    private int CurrentStreamIndex = 0;         // Index of the active strem
    private Stream _bufferedStream = null;       // Buffer Stream 
    private bool _xfading = false;               // are we crossfading?

    private PlaybackState _state;               // The Playback State
    private IMediaItem _mediaItem;              // The MediaItem that was passed to the Player
    private Uri _fileName;                      // The Filename being played
    private bool _paused = false;               // Is the Player Paused
    private bool _IsMuted = false;              // Is the Player Muted
    private int _volume = 85;                   // The Volume set for Playback
    private TimeSpan _currentTime;              // THe Current Playtime

    private int _mixer = 0;                     // 44khz Mixer for playback of standard files
    private float[,] _MixingMatrix = new float[8, 2]{ 
        {1, 0}, // left front out = left in
        {0, 1}, // right front out = right in
        {1, 0}, // centre out = left in
        {0, 1}, // LFE out = right in
        {1, 0}, // left rear/side out = left in
        {0, 1}, // right rear/side out = right in
        {1, 0}, // left-rear center out = left in
        {0, 1}  // right-rear center out = right in
    };
    #endregion

    #region IPluginStateTracker implementation

    void IPluginStateTracker.Activated()
    {
      // Load the Settings
      LoadSettings();

      _state = PlaybackState.Ended;
      // Setup internal message queue for receiving Messages from the various Streams
      IMessageQueue queueBass = ServiceScope.Get<IMessageBroker>().GetOrCreate("bass");
      queueBass.OnMessageReceive += new MessageReceivedHandler(bass_OnMessageReceive);
    }

    bool IPluginStateTracker.RequestEnd()
    {
      return false; // FIXME: The player plugin should be able to be disabled
    }

    void IPluginStateTracker.Stop() { }

    void IPluginStateTracker.Continue() { }

    void IPluginStateTracker.Shutdown() { }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      Bass.BASS_Free();
    }

    //#region IAutoStart Members

    //public void Startup()
    //{
    //  IPlayerFactory factory = ServiceScope.Get<IPlayerFactory>();
    //  factory.Register(this);
    //}

    //#endregion
    #endregion

    #region IPlayerBuilder
    public bool CanPlay(IMediaItem mediaItem, Uri uri)
    {
      return IsAudioFile(mediaItem, uri.AbsolutePath);
    }

    public IPlayer GetPlayer(IMediaItem mediaItem, Uri uri)
    {
      if (IsAudioFile(mediaItem, uri.AbsolutePath))
      {
        if (!_initialised)
        {
          // Register BASS.Net to get rid of the splash screen
          BassRegistration.BassRegistration.Register();

          // Load the Bass Plugins and Init Zones
          LoadBass();
        }
        return new BassPlayer(this);
      }
      return null;
    }

    bool IsAudioFile(IMediaItem mediaItem, string filename)
    {
      string ext = System.IO.Path.GetExtension(filename);

      // First check the Mime Type
      if (mediaItem.MetaData.ContainsKey("MimeType"))
      {
        string mimeType = mediaItem.MetaData["MimeType"] as string;
        if (mimeType != null)
        {
          if (mimeType.Contains("audio"))
          {
            if (_settings.SupportedExtensions.IndexOf(ext) > -1)
              return true;
          }
        }
      }

      if (_settings.SupportedExtensions.IndexOf(ext) > -1)
        return true;

      return false;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Returns the Current Stream 
    /// </summary>
    /// <returns></returns>
    private Stream GetCurrentStream()
    {
      if (CurrentStreamIndex < 0)
        CurrentStreamIndex = 0;

      else if (CurrentStreamIndex >= Streams.Count)
        CurrentStreamIndex = Streams.Count - 1;

      return Streams[CurrentStreamIndex];
    }

    /// <summary>
    /// Returns the Next Stream
    /// </summary>
    /// <returns></returns>
    private Stream GetNextStream()
    {
      Stream currentStream = GetCurrentStream();

      if (currentStream == null)
        return null;

      if (currentStream.ID != 0 || Bass.BASS_ChannelIsActive(currentStream.ID) != BASSActive.BASS_ACTIVE_STOPPED)
      {
        CurrentStreamIndex++;

        if (CurrentStreamIndex >= Streams.Count)
          CurrentStreamIndex = 0;
      }
      return Streams[CurrentStreamIndex];
    }

    /// <summary>
    /// Handle Bass Player Internal messages
    /// </summary>
    /// <param name="message"></param>

    private void bass_OnMessageReceive(QueueMessage message)
    {
      string action = message.MessageData["action"] as string;

      switch (action)
      {
        // Message sent by the Stream Object, when a song is starting to Crossfade
        case "xfading":
          {
            _xfading = true;
            // Inform the Playlist Manager, that we want the next song
            SendInternalMessage("nextfile");
            break;
          }

        // Message sent by the Stream Object, when a song has ended
        case "ended":
          {
            if (_bufferedStream.ID != 0)
            {
              if (_settings.GaplessPlayback)
              {
                AttachStreamToMixer(_bufferedStream);
              }
            }
            else
            {
              ServiceScope.Get<ILogger>().Debug("BASS: Finished Playing. Sending ended message");
              // Broadcast a message that the song has ended
              SendInternalMessage("ended");
            }
            break;
          }
        
        // Settings have been changed
        case "settingschanged":
          {
            LoadSettings();
            break;
          }
      }
    }

    private void AttachStreamToMixer(Stream stream)
    {
      Stream nextstream = GetNextStream();
      if (nextstream != null)
        if (Bass.BASS_ChannelIsActive(nextstream.ID) == BASSActive.BASS_ACTIVE_PLAYING)
          FreeStream(nextstream);

      Streams[CurrentStreamIndex] = stream;
      int streamID = stream.ID;
      if (streamID != 0)
      {
        // Plugin the stream into the mixer and set the mixing matrix
        BassMix.BASS_Mixer_ChannelSetMatrix(streamID, _MixingMatrix);
        BassMix.BASS_Mixer_StreamAddChannel(_mixer, streamID, BASSFlag.BASS_MIXER_MATRIX | BASSFlag.BASS_MIXER_NORAMPIN | BASSFlag.BASS_STREAM_AUTOFREE);
        
        // Do we need to Fade In
        if (!_settings.GaplessPlayback && _settings.Crossfade > 0)
        {

        }


        if (Bass.BASS_ChannelIsActive(_mixer) != BASSActive.BASS_ACTIVE_PLAYING)
        {
          Bass.BASS_Start();
          if (!Bass.BASS_ChannelPlay(_mixer, false))
            ServiceScope.Get<ILogger>().Error("BASS: Failed starting playback. Reason: {0}", Enum.GetName(typeof(BASSError), Bass.BASS_ErrorGetCode()));
        }

        _state = PlaybackState.Playing;

        SendInternalMessage("started");
      }
    }

    private void RemoveStreamFromMixer()
    {
      Stream stream = GetCurrentStream();

      if (stream == null)
        return;

      Bass.BASS_ChannelStop(stream.ID);
      BassMix.BASS_Mixer_ChannelRemove(stream.ID);
    }

    private void FreeStream(Stream stream)
    {
      Bass.BASS_StreamFree(stream.ID);
      stream = null;
    }

    private void SendInternalMessage(string action)
    {
      QueueMessage msg = new QueueMessage();
      msg.MessageData["player"] = this;
      msg.MessageData["action"] = action;

      Thread asyncMsgThread = new Thread(new ParameterizedThreadStart(AsyncMessageSend));
      asyncMsgThread.Start(msg);
    }

    private void AsyncMessageSend(object message)
    {
      QueueMessage msg = (QueueMessage)message;
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players-internal");
      queue.Send(msg);
    }
    #endregion

    #region IPlayer members
    public PlaybackState State
    {
      get { return _state; }
      set { _state = value; }
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

    /// <summary>
    /// Plays the file
    /// </summary>
    /// <param name="fileName"></param>
    public void Play(IMediaItem mediaItem)
    {
      ServiceScope.Get<ILogger>().Info("BASS: Play {0}", mediaItem.ContentUri);
      if (!InitBass())
        return;

      _mediaItem = mediaItem;
      _fileName = _mediaItem.ContentUri;

      // Do we need to stop the current Stream First?
      // This happens when Play has been pressed again or we were llaed by Next / Prev from the Playlist
      if (!_xfading)
        RemoveStreamFromMixer();

      _bufferedStream = new Stream(_mediaItem, _settings);

      if (!_settings.GaplessPlayback || _state == PlaybackState.Stopped || _state == PlaybackState.Ended)
        AttachStreamToMixer(_bufferedStream);

      _xfading = false;
    }

    /// <summary>
    /// stops playback
    /// </summary>
    public void Stop()
    {
      ServiceScope.Get<ILogger>().Debug("BASS: Stopping Playback of Current Stream");
      Stream stream = GetCurrentStream();

      if (_settings.SoftStop)
      {
        Bass.BASS_ChannelSlideAttribute(stream.ID, BASSAttribute.BASS_ATTRIB_VOL, 0, 500);

        // Wait until the slide is done
        while (Bass.BASS_ChannelIsSliding(stream.ID, BASSAttribute.BASS_ATTRIB_VOL))
          System.Threading.Thread.Sleep(20);
      }

      Bass.BASS_ChannelStop(stream.ID);
      BassMix.BASS_Mixer_ChannelRemove(stream.ID);
      _state = PlaybackState.Stopped;
    }

    /// <summary>
    /// Gets the player name.
    /// </summary>
    /// <value>The media-item.</value>
    public string Name
    {
      get
      {
        return "Bass";
      }
    }

    /// <summary>
    /// Gets the media-item.
    /// </summary>
    /// <value>The media-item.</value>
    public IMediaItem MediaItem
    {
      get
      {
        Stream stream = GetCurrentStream();
        if (stream == null) return null;
        return stream.MediaItem;
      }

    }
    /// <summary>
    /// gets/sets wheter video is paused
    /// </summary>
    public bool Paused
    {
      get { return _paused; }
      set
      {
        _paused = value;
        if (_paused)
        {
          Bass.BASS_Pause();
          _state = PlaybackState.Paused;
        }
        else
        {
          Bass.BASS_Start();
          _state = PlaybackState.Playing;
        }
      }
    }

    /// <summary>
    /// called when windows message is received
    /// </summary>
    /// <param name="m">message</param>
    public void OnMessage(object m)
    {
    }

    /// <summary>
    /// called when application is idle
    /// </summary>
    public void OnIdle()
    {
      Stream stream = GetCurrentStream();
      if (stream != null)
      {
        long pos = Bass.BASS_ChannelGetPosition(stream.ID);           // position in bytes
        double curPosition = Bass.BASS_ChannelBytes2Seconds(stream.ID, pos); // the elapsed time length
        _currentTime = new TimeSpan(0, 0, (int)curPosition);
      }
    }

    public TimeSpan StreamPosition
    {
      get
      {
        return _currentTime;
      }
    }
    /// <summary>
    /// Returns the current play time. Also allows absolute Positioning
    /// </summary>
    public TimeSpan CurrentTime
    {
      get
      {
        return _currentTime;
      }
      set
      {
        ServiceScope.Get<ILogger>().Debug("BassPlayer: seek to {0} / {1}", value.ToString(), Duration.ToString());

        if (_state != PlaybackState.Playing)
          return;

        try
        {
          Stream stream = GetCurrentStream();
          long len = Bass.BASS_ChannelGetLength(stream.ID);                 // length in bytes
          double totaltime = Bass.BASS_ChannelBytes2Seconds(stream.ID, len); // the total time length
          long pos = BassMix.BASS_Mixer_ChannelGetPosition(stream.ID);

          float offsetSecs = (float)value.TotalSeconds;

          if (offsetSecs >= totaltime)
            return;

          BassMix.BASS_Mixer_ChannelSetPosition(stream.ID, Bass.BASS_ChannelSeconds2Bytes(stream.ID, offsetSecs));
        }
        catch
        { }
      }
    }

    /// <summary>
    /// returns the duration of the movie
    /// </summary>
    public TimeSpan Duration
    {
      get
      {
        Stream stream = GetCurrentStream();
        if (stream != null)
        {
          // length in bytes
          long len = Bass.BASS_ChannelGetLength(stream.ID);
          // the total time length
          double totaltime = Bass.BASS_ChannelBytes2Seconds(stream.ID, len);
          return new TimeSpan(0, 0, (int)totaltime);
        }
        return new TimeSpan(0, 0, 0);
      }
    }

    /// <summary>
    /// Gets the name of the file.
    /// </summary>
    /// <value>The name of the file.</value>
    public Uri FileName
    {
      get { return _fileName; }
    }

    /// <summary>
    /// Restarts playback from the start.
    /// </summary>
    public void Restart()
    {
      Bass.BASS_ChannelPlay(_mixer, true);
    }

    /// <summary>
    /// Gets or sets the volume (0-100)
    /// </summary>
    /// <value>The volume.</value>
    public int Volume
    {
      get { return _volume; }
      set
      {
        if (_volume != value)
        {
          if (value < 100)
            value = 100;

          if (value < 0)
            value = 0;

          _volume = value;
          Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, _volume);
        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="IPlayer"/> is mute.
    /// </summary>
    /// <value><c>true</c> if muted; otherwise, <c>false</c>.</value>
    public bool Mute
    {
      get { return _IsMuted; }
      set
      {
        _IsMuted = value;
      }
    }
    #endregion

    #region Initialisation
    /// <summary>
    /// Loads the BassPlayer spscific settings
    /// </summary>
    private void LoadSettings()
    {
      _settings = new BassPlayerSettings();
      ServiceScope.Get<ISettingsManager>().Load(_settings);
    }

    /// <summary>
    /// Load the BASS Environment
    /// </summary>
    private void LoadBass()
    {
      _volume = _settings.Volume;

      // Init the Streams List
      for (int i = 0; i < MAXSTREAMS; i++)
        Streams.Add(null);

      // Load Audio Plugins
      LoadAudioDecoderPlugins();

      _zones = new List<Zone>();

      // Retrieve available Sound Devices and assign them to Zones
      for (int i = 0; i < Bass.BASS_GetDeviceCount(); i++)
      {
        BASS_DEVICEINFO info = Bass.BASS_GetDeviceInfo(i);
        if (info != null)
        {
          Zone zone = new Zone(i, info.name);
          _zones.Add(zone);
        }
      }

      bool initOK = InitBass();

    }

    /// <summary>
    /// Re-Init the Bass Environment after it got disposed on stop
    /// </summary>
    /// <returns></returns>
    private bool InitBass()
    {
      // Are we already Initialised
      if (_initialised)
        return true;

      ServiceScope.Get<ILogger>().Info("BASS: Initialise BASS environment.");
      // Todo: ASIO Handling
      bool initOK = false;
      initOK = (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT | BASSInit.BASS_DEVICE_LATENCY, IntPtr.Zero, null));
      if (initOK)
      {
        // Todo: ASIO support
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 500);
        Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, _volume);

        _mixer = BassMix.BASS_Mixer_StreamCreate(44100, 8, BASSFlag.BASS_MIXER_NONSTOP | BASSFlag.BASS_STREAM_AUTOFREE);
        ServiceScope.Get<ILogger>().Info("BASS: Initialisation done.");
        _initialised = true;
      }
      else
      {
        _initialised = false;
        int error = (int)Bass.BASS_ErrorGetCode();
        ServiceScope.Get<ILogger>().Error("BASS: Error initializing BASS audio engine {0}", Enum.GetName(typeof(BASSError), error));
      }
      return initOK;
    }

    /// <summary>
    /// Load External BASS Audio Decoder Plugins
    /// </summary>
    private void LoadAudioDecoderPlugins()
    {
      ServiceScope.Get<ILogger>().Info("BASS: Loading audio decoder add-ins...");

      string appPath = System.Windows.Forms.Application.StartupPath;
      string decoderFolderPath = Path.Combine(appPath, @"musicplayer\plugins\audio decoders");

      if (!Directory.Exists(decoderFolderPath))
      {
        ServiceScope.Get<ILogger>().Error(@"BASS: Unable to find \musicplayer\plugins\audio decoders folder in MediaPortal.exe path.");
        return;
      }

      DirectoryInfo dirInfo = new DirectoryInfo(decoderFolderPath);
      FileInfo[] decoders = dirInfo.GetFiles();

      int pluginHandle = 0;
      int decoderCount = 0;

      foreach (FileInfo file in decoders)
      {
        if (Path.GetExtension(file.Name).ToLower() != ".dll")
          continue;

        pluginHandle = Bass.BASS_PluginLoad(file.FullName);

        if (pluginHandle != 0)
        {
          decoderCount++;
          ServiceScope.Get<ILogger>().Debug("BASS: Added Audio Plugin: {0}", file.FullName);
        }

        else
        {
          ServiceScope.Get<ILogger>().Error("BASS: Unable to load: {0}", file.FullName);
        }
      }

      if (decoderCount > 0)
        ServiceScope.Get<ILogger>().Info("BASS: Loaded {0} Audio Decoders.", decoderCount);

      else
        ServiceScope.Get<ILogger>().Error(@"BASS: No audio decoders were loaded. Confirm decoders are present in \musicplayer\plugins\audio decoders folder.");
    }
    #endregion

    #region Unused Properties
    /// <summary>
    /// gets/sets the width/height for the video window
    /// </summary>
    public Size Size
    {
      get { return new Size(); }
      set { }
    }


    public void BeginRender(object effect)
    {
    }
    public void EndRender(object effect)
    {
    }

    /// <summary>
    /// gets/sets the position on screen where the video should be drawn
    /// </summary>
    public Point Position
    {
      get { return new Point(); }
      set { }
    }

    /// <summary>
    /// gets/sets the alphamask
    /// </summary>
    public Rectangle AlphaMask
    {
      get { return new Rectangle(); }
      set { }
    }

    /// <summary>
    /// Render the video
    /// </summary>
    public void Render()
    {
    }

    public Size VideoSize { get { return new Size(0, 0); } }
    public Size VideoAspectRatio { get { return new Size(0, 0); } }
    /// <summary>
    /// returns list of available audio streams
    /// </summary>
    public string[] AudioStreams
    {
      get { return new string[0]; }
    }

    /// <summary>
    /// returns list of available subtitle streams
    /// </summary>
    public string[] Subtitles
    {
      get { return new string[0]; }
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
      get { return new string[0]; }
    }

    /// <summary>
    /// Sets the DVD title.
    /// </summary>
    /// <param name="title">The title.</param>
    public void SetDvdTitle(string title)
    { }

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
      get { return new string[0]; }
    }

    /// <summary>
    /// Sets the DVD chapter.
    /// </summary>
    /// <param name="title">The title.</param>
    public void SetDvdChapter(string title)
    { }

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
    ///Resumes playback from previous session
    /// </summary>
    public void ResumeSession()
    { }

    /// <summary>
    /// True if resume data exists (from previous session)
    /// </summary>
    public bool CanResumeSession(Uri fileName)
    { return false; }

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

    public Rectangle MovieRectangle
    {
      get
      {
        return new Rectangle(0, 0, 0, 0);
      }
      set
      {
      }
    }
    #endregion

  }
}


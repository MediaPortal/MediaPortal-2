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
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Players;
using MediaPortal.Core.Messaging;
using MediaPortal.SkinEngine.Commands;

namespace MediaPortal.SkinEngine.Players
{
  public class MediaPlayers : IPlayerCollection
  {

    #region variables
    private Seeking _seeking;

    private StringId _pip = new StringId("playback", "30");

    private Property _activePlayersProperty;
    private List<IPlayer> _players;
    private ListItem _pipMenu;
    private Property _videoPaused;
    private Property _videoPlaying;
    private Property _muted;
    private Settings _playbackSettings = new Settings();
    private OsdProperties _osdProperties;

    #endregion

    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="IPlayerCollection"/> class.
    /// </summary>
    public MediaPlayers()
    {

      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players-internal");
      queue.OnMessageReceive += OnInternalPlayerMessageReceived;
      _osdProperties = new OsdProperties(this);
      _videoPaused = new Property(typeof(bool), false);
      _videoPlaying = new Property(typeof(bool), false);
      _players = new List<IPlayer>();
      _muted = new Property(typeof(bool), false);
      _activePlayersProperty = new Property(typeof(int), 0);


      Application.Idle += OnIdle;
      _seeking = new Seeking();

      _pipMenu = new ListItem("Name", _pip.ToString());
      _pipMenu.Command = new ReflectionCommand("ScreenManager.ShowDialog");
      _pipMenu.CommandParameter = new StringParameter("dialogPictureInPicture");
    }

    #endregion
    /// <summary>
    /// release any gui dependent resources
    /// </summary>
    public void ReleaseResources()
    {
      for (int i = 0; i < Count; ++i)
      {
        this[i].ReleaseResources();
      }
    }
    /// <summary>
    /// realloc any gui dependent resources
    /// </summary>
    public void ReallocResources()
    {
      for (int i = 0; i < Count; ++i)
      {
        this[i].ReallocResources();
      }
    }

    #region collection methods
    /// <summary>
    /// Called when application is idle
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void OnIdle(object sender, EventArgs e)
    {
      for (int i = 0; i < Count; ++i)
      {
        this[i].OnIdle();
      }
      OSD.UpdateProperties();
    }

    /// <summary>
    /// Called when a new windows message is received
    /// </summary>
    /// <param name="m">The message</param>
    FIXME: Has to be called (commented out in SkinEngine's MainForm)
    public void OnMessage(object m)
    {
      for (int i = 0; i < Count; ++i)
      {
        this[i].OnMessage(m);
      }
    }

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
      for (int i = 0; i < Count; ++i)
      {
        this[i].Stop();
      }

      _players.Clear();
      Paused = false;
      Playing = false;
      ActivePlayers = 0;
    }


    /// <summary>
    /// Adds the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    public void Add(IPlayer player)
    {
      _players.Add(player);
      ActivePlayers = _players.Count;
      if (_players.Count == 2)
      {
        //add pip menu
        OSD.ContextMenu.Add(_pipMenu);
      }
    }

    /// <summary>
    /// Removes the specified player.
    /// </summary>
    /// <param name="player">The player.</param>
    public void Remove(IPlayer player)
    {
      _players.Remove(player);
      ActivePlayers = _players.Count;
      if (_players.Count < 2)
      {
        //remove pip menu
        OSD.ContextMenu.Remove(_pipMenu);
      }
      if (_players.Count == 0)
      {
        Paused = false;
        Playing = false;
      }
    }

    /// <summary>
    /// Does the collection already contain the specified player.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool CollectionContainsPlayer(IPlayer player)
    {
      for (int i = 0; i < Count; ++i)
      {
        if (this[i].Name == player.Name)
          return true;
      }
      return false;
    }

    /// <summary>
    /// Gets or sets the <see cref="MediaPortal.Presentation.Players.IPlayer"/> at the specified index.
    /// </summary>
    /// <value></value>
    public IPlayer this[int index]
    {
      get { return _players[index]; }
      set { _players[index] = value; }
    }

    /// <summary>
    /// Gets the number of players.
    /// </summary>
    /// <value>The number of players.</value>
    public int Count
    {
      get { return _players.Count; }
    }
    #endregion

    #region DVD

    /// <summary>
    /// Shows the DVD menu.
    /// </summary>
    public void ShowDvdMenu()
    {
      for (int i = 0; i < Count; ++i)
      {
        if ((this[i] as DvdPlayer) != null)
        {
          DvdPlayer dvd = (DvdPlayer)this[i];
          dvd.ShowDvdMenu();
          return;
        }
      }
    }

    /// <summary>
    /// method to navigate the dvd menu
    /// </summary>
    /// <param name="direction">The direction.</param>
    public void DvdNavigate(string direction)
    {
      Key key = Key.None;
      if (direction == Key.Left.ToString())
      {
        key = Key.DvdLeft;
      }
      if (direction == Key.Right.ToString())
      {
        key = Key.DvdRight;
      }
      if (direction == Key.Up.ToString())
      {
        key = Key.DvdUp;
      }
      if (direction == Key.Down.ToString())
      {
        key = Key.DvdDown;
      }
      for (int i = 0; i < Count; ++i)
      {
        if ((this[i] as DvdPlayer) != null)
        {
          DvdPlayer dvd = (DvdPlayer)this[i];
          dvd.Navigate(key);
          return;
        }
      }
    }

    /// <summary>
    /// selects the current menu item in the dvd menu.
    /// </summary>
    public void DvdSelect()
    {
      for (int i = 0; i < Count; ++i)
      {
        if ((this[i] as DvdPlayer) != null)
        {
          DvdPlayer dvd = (DvdPlayer)this[i];
          dvd.Navigate(Key.DvdSelect);
          return;
        }
      }
    }

    #endregion

    #region properties
    public Seeking Seeking
    {
      get
      {
        return _seeking;
      }
    }
    /// <summary>
    /// Gets the default settings.
    /// </summary>
    /// <value>The default settings.</value>
    public Settings Settings
    {
      get
      {
        return _playbackSettings;
      }
    }

    public OsdProperties OSD
    {
      get
      {
        return _osdProperties;
      }
    }

    /// <summary>
    /// Gets or sets the active players property.
    /// </summary>
    /// <value>The active players property.</value>
    public Property ActivePlayersProperty
    {
      get { return _activePlayersProperty; }
      set { _activePlayersProperty = value; }
    }

    /// <summary>
    /// Gets or sets the number of active players.
    /// </summary>
    /// <value>The active players.</value>
    public int ActivePlayers
    {
      get { return (int)_activePlayersProperty.GetValue(); }
      set { _activePlayersProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback is muted.
    /// </summary>
    /// <value><c>true</c> if  playback is muted; otherwise, <c>false</c>.</value>
    public bool IsMuted
    {
      get
      {
        return (bool)_muted.GetValue();
      }
      set
      {
        _muted.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the is muted property.
    /// </summary>
    /// <value>The is muted property.</value>
    public Property IsMutedProperty
    {
      get
      {
        return _muted;
      }
      set
      {
        _muted = value;
      }
    }

    /// <summary>
    /// Mutes/unmutes the player.
    /// </summary>
    public void Mute()
    {
      if (Count != 0)
      {
        this[0].Mute = !this[0].Mute;
        IsMuted = this[0].Mute;
      }
    }


    /// <summary>
    /// Gets or sets the paused property.
    /// </summary>
    /// <value>The paused property.</value>
    public Property PausedProperty
    {
      get { return _videoPaused; }
      set { _videoPaused = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether playback is paused.
    /// </summary>
    /// <value><c>true</c> if paused; otherwise, <c>false</c>.</value>
    public bool Paused
    {
      get { return (bool)_videoPaused.GetValue(); }
      set { _videoPaused.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the playing property.
    /// </summary>
    /// <value>The playing property.</value>
    public Property PlayingProperty
    {
      get { return _videoPlaying; }
      set { _videoPlaying = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="MediaPlayers"/> is playing.
    /// </summary>
    /// <value><c>true</c> if playing; otherwise, <c>false</c>.</value>
    public bool Playing
    {
      get { return (bool)_videoPlaying.GetValue(); }
      set { _videoPlaying.SetValue(value); }
    }
    #endregion

    #region PIP

    /// <summary>
    /// Stops the picture in picture streams.
    /// </summary>
    public void StopPIP()
    {
      while (true)
      {
        if (_players.Count < 2)
        {
          return;
        }
        _players[1].Stop();
      }
    }

    /// <summary>
    /// Switches between the PIP and main video.
    /// </summary>
    public void SwitchPIP()
    {
      if (_players.Count > 1)
      {
        IPlayer player1 = _players[0];
        IPlayer player2 = _players[1];
        _players.Clear();
        _players.Add(player2);
        _players.Add(player1);
      }
    }

    #endregion

    #region seeking

    /// <summary>
    /// Seeks  forward.
    /// </summary>
    public void SeekForward()
    {
      _seeking.OnSeek(Key.Right);
    }

    /// <summary>
    /// Seeks  backward.
    /// </summary>
    public void SeekBackward()
    {
      _seeking.OnSeek(Key.Left);
    }

    #endregion

    #region subtitles

    /// <summary>
    /// Gets the subtitles.
    /// </summary>
    /// <value>The subtitles.</value>
    public ItemsList Subtitles
    {
      get
      {
        string currentSubtitle = this[0].CurrentSubtitle;
        ItemsList items = new ItemsList();
        if (Count > 0)
        {
          string[] subs = this[0].Subtitles;
          for (int i = 0; i < subs.Length; ++i)
          {
            ListItem item = new ListItem("Name", subs[i]);
            if (currentSubtitle == subs[i])
            {
              item.Selected = true;
            }
            items.Add(item);
          }
        }
        return items;
      }
    }

    /// <summary>
    /// Sets the subtitle.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetSubtitle(ListItem item)
    {
      if (Count > 0)
      {
        this[0].SetSubtitle(item.Label("Name", "").Evaluate());
      }
    }

    #endregion

    #region audio streams

    /// <summary>
    /// Gets the audio streams.
    /// </summary>
    /// <value>The audio streams.</value>
    public ItemsList AudioStreams
    {
      get
      {
        string currentStream = this[0].CurrentAudioStream;
        ItemsList items = new ItemsList();
        if (Count > 0)
        {
          string[] streams = this[0].AudioStreams;
          for (int i = 0; i < streams.Length; ++i)
          {
            ListItem item = new ListItem("Name", streams[i]);
            if (currentStream == streams[i])
            {
              item.Selected = true;
            }
            items.Add(item);
          }
        }
        return items;
      }
    }

    /// <summary>
    /// Sets the audio stream.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetAudioStream(ListItem item)
    {
      if (Count > 0)
      {
        this[0].SetAudioStream(item.Label("Name", "").Evaluate());
      }
    }

    #endregion

    #region dvd titles

    /// <summary>
    /// Gets the dvd titles.
    /// </summary>
    /// <value>The dvd titles.</value>
    public ItemsList DvdTitles
    {
      get
      {
        string currentStream = this[0].CurrentDvdTitle;
        ItemsList items = new ItemsList();
        if (Count > 0)
        {
          string[] streams = this[0].DvdTitles;
          for (int i = 0; i < streams.Length; ++i)
          {
            ListItem item = new ListItem("Name", streams[i]);
            if (currentStream == streams[i])
            {
              item.Selected = true;
            }
            items.Add(item);
          }
        }
        return items;
      }
    }

    /// <summary>
    /// Sets the dvd title.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetDvdTitle(ListItem item)
    {
      if (Count > 0)
      {
        this[0].SetDvdTitle(item.Label("Name", "").Evaluate());
      }
    }

    /// <summary>
    /// Gets the DVD chapters.
    /// </summary>
    /// <value>The DVD chapters.</value>
    public ItemsList DvdChapters
    {
      get
      {
        string currentStream = this[0].CurrentDvdChapter;
        ItemsList items = new ItemsList();
        if (Count > 0)
        {
          string[] streams = this[0].DvdChapters;
          for (int i = 0; i < streams.Length; ++i)
          {
            ListItem item = new ListItem("Name", streams[i]);
            if (currentStream == streams[i])
            {
              item.Selected = true;
            }
            items.Add(item);
          }
        }
        return items;
      }
    }

    /// <summary>
    /// Sets the dvd title.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetDvdChapter(ListItem item)
    {
      if (Count > 0)
      {
        this[0].SetDvdChapter(item.Label("Name", "").Evaluate());
      }
    }

    #endregion

    #region basic methods for stop/pause/restart/rewind/forward/resume
    /// <summary>
    /// fast rewind playback.
    /// </summary>
    public void Rewind()
    {
      SeekBackward();
    }

    /// <summary>
    /// fast forward playback .
    /// </summary>
    public void Forward()
    {
      SeekForward();
    }

    /// <summary>
    /// Stops this instance.
    /// </summary>
    public void Stop()
    {
      if (Count != 0)
      {
        this[0].Stop();
      }
    }

    /// <summary>
    /// Pauses / resumes playback
    /// </summary>
    public void TogglePause()
    {
      if (Count != 0)
      {
        this[0].Paused = !this[0].Paused;

        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players");
        QueueMessage msg = new QueueMessage();
        msg.MessageData["player"] = this[0];
        if (this[0].Paused)
          msg.MessageData["action"] = "paused";
        else
          msg.MessageData["action"] = "continue";
      }
    }

    /// <summary>
    /// Pauses  playback
    /// </summary>
    public void Pause()
    {
      if (Count != 0)
      {
        this[0].Paused = true;

        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players");
        QueueMessage msg = new QueueMessage();
        msg.MessageData["player"] = this[0];
        msg.MessageData["action"] = "paused";
      }
    }

    /// <summary>
    /// Restarts playback from the start.
    /// </summary>
    public void Restart()
    {
      if (Count != 0)
      {
        this[0].Restart();
        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players");
        QueueMessage msg = new QueueMessage();
        msg.MessageData["player"] = this[0];
        msg.MessageData["action"] = "restart";
      }
    }


    /// <summary>
    /// Resumes playback.
    /// </summary>
    public void Resume()
    {
      if (Count != 0)
      {
        this[0].Paused = false;

        IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players");
        QueueMessage msg = new QueueMessage();
        msg.MessageData["player"] = this[0];
        msg.MessageData["action"] = "resume";
      }
    }

    public void ResumeSession()
    {
      if (Count != 0)
      {
        this[0].Paused = false;
        Playing = true;
        this[0].ResumeSession();
      }
    }
    public void RestartSession()
    {
      if (Count != 0)
      {
        this[0].Paused = false;
        Playing = true;
        this[0].Restart();
      }
    }
    void OnInternalPlayerMessageReceived(QueueMessage message)
    {
      IMessageQueue queue = ServiceScope.Get<IMessageBroker>().GetOrCreate("players");

      string action = message.MessageData["action"] as string;
      IPlayer player = message.MessageData["player"] as IPlayer;

      if (action == "ended")
      {
        if (player == this[0])
        {
          queue.Send(message);
          if (message.MessageData.ContainsKey("handled"))
          {
            if (message.MessageData["handled"].ToString() == "true") return;
          }
        }
        player.Stop();

      }
      else
      {
        if (action == "started")
        {
          if (player == this[0])
          {
            Playing = true;
          }
        }
        if (action == "paused")
        {
          if (player == this[0])
          {
            Playing = false;
            Paused = true;
          }
        }
        if (action == "playing")
        {
          if (player == this[0])
          {
            Playing = true;
            Paused = false;
          }
        }
        queue.Send(message);
      }
    }
    #endregion
  }
}

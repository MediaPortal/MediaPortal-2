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
using MediaPortal.Core;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.MenuManager;
using MediaPortal.Core.Settings;
using MediaPortal.Core.Messaging;
using MediaPortal.Media.MediaManager;
using MediaPortal.SkinEngine.SkinManagement;


namespace MediaPortal.SkinEngine.Players
{
  public class OsdProperties
  {
    #region variables
    private Property _topBarVisible;
    private Property _currentTime;
    private Property _remainingTime;
    private Property _percentPlayed;
    private Property _title;
    private Property _volume;
    private Property _zoomMode;
    private Property _croppingTopLeft;
    private Property _croppingBottomRight;
    private Property _duration;
    private Property _isVideo;
    private Property _isAudio;
    private Property _pipUsed;
    private DateTime _timer = new DateTime();
    private MediaPlayers _players;
    private ItemsCollection _contextMenu;
    private ListItem _mediaItem;
    private IMediaItem _prevMediaItem;
    private DateTime _updateTimer;
    #endregion

    #region ctor
    public OsdProperties(MediaPlayers players)
    {
      _updateTimer = DateTime.Now;
      _mediaItem = new ListItem();
      _players = players;
      _topBarVisible = new Property(typeof(bool), false);
      _currentTime = new Property(typeof(string), "");
      _remainingTime = new Property(typeof(string), "");
      _duration = new Property(typeof(string), "");
      _percentPlayed = new Property(typeof(float), 0.0f);
      _pipUsed = new Property(typeof(bool), false);
      _volume = new Property(typeof(int), 100);
      _title = new Property(typeof(string), "");
      _zoomMode = new Property(typeof(string), "");
      _croppingTopLeft = new Property(typeof(string), "");
      _croppingBottomRight = new Property(typeof(string), "");
      _isVideo = new Property(typeof(bool), false);
      _isAudio = new Property(typeof(bool), false);
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      if (settings.Geometry == "")
      {
        settings.Geometry = SkinContext.Geometry.Geometries[0].Name;
        settings.Crop = SkinContext.CropSettings;
        ServiceScope.Get<ISettingsManager>().Save(settings);
      }
      SkinContext.CropSettings = settings.Crop;
      SkinContext.Geometry.Select(settings.Geometry);

      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      IMessageQueue queue = broker.GetOrCreate("players-internal");
      queue.OnMessageReceive += new MessageReceivedHandler(OnPlayerMessageReceived);
    }
    #endregion

    public Property IsMouseUsed
    {
      get
      {
        return SkinContext.MouseUsedProperty;
      }
    }

    /// <summary>
    /// Gets or sets the title of current media.
    /// </summary>
    /// <value>The title.</value>
    public ListItem MediaItem
    {
      get { return _mediaItem; }
    }

    /// <summary>
    /// Returns if PIP is in use
    /// </summary>
    /// <value>true if PIP is in use.</value>
    public bool PIP
    {
      get { return (bool)_pipUsed.GetValue(); }
      set { _pipUsed.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the pip property.
    /// </summary>
    /// <value>The pip property.</value>
    public Property PIPProperty
    {
      get { return _pipUsed; }
      set { _pipUsed = value; }
    }


    public string Title
    {
      get { return (string)_title.GetValue(); }
      set { _title.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the title property.
    /// </summary>
    /// <value>The title property.</value>
    public Property TitleProperty
    {
      get { return _title; }
      set { _title = value; }
    }

    /// <summary>
    /// Gets a value indicating whether this the top bar is visible.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if topbar is visible; otherwise, <c>false</c>.
    /// </value>
    public bool IsTopBarVisible
    {
      get { return (bool)_topBarVisible.GetValue(); }
      set { _topBarVisible.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the TopBarVisible property.
    /// </summary>
    /// <value>The is top bar visible property.</value>
    public Property IsTopBarVisibleProperty
    {
      get { return _topBarVisible; }
      set { _topBarVisible = value; }
    }

    /// <summary>
    /// Determines if topbar is visble.
    /// </summary>
    private void DetermineIfTopbarIsVisble()
    {
      if (_players.Count == 0)
      {
        IsTopBarVisible = false;
        _timer = DateTime.MinValue;
        return;
      }
      if (_players.Seeking.IsSeeking)
      {
        IsTopBarVisible = true;
        return;
      }
      for (int i = 0; i < _players.Count; ++i)
      {
        if (_players[i].Paused)
        {
          IsTopBarVisible = true;
          return;
        }
      }
      TimeSpan ts = SkinContext.Now - _timer;
      if (ts.TotalSeconds <= 3)
      {
        IsTopBarVisible = true;
        return;
      }
      IsTopBarVisible = false;
    }

    /// <summary>
    /// Gets the current time.
    /// </summary>
    /// <value>The current time.</value>
    public string CurrentTime
    {
      get { return (string)_currentTime.GetValue(); }
      set { _currentTime.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the current time property.
    /// </summary>
    /// <value>The current time property.</value>
    public Property CurrentTimeProperty
    {
      get { return _currentTime; }
      set { _currentTime = value; }
    }

    /// <summary>
    /// Gets or sets the percentage played (0-100%).
    /// </summary>
    /// <value>The percent played.</value>
    public float PercentPlayed
    {
      get { return (float)_percentPlayed.GetValue(); }
      set { _percentPlayed.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the percent played property.
    /// </summary>
    /// <value>The percent played property.</value>
    public Property PercentPlayedProperty
    {
      get { return _percentPlayed; }
      set { _percentPlayed = value; }
    }


    /// <summary>
    /// Gets the remaining time.
    /// </summary>
    /// <value>The remaining time.</value>
    public string RemainingTime
    {
      get { return (string)_remainingTime.GetValue(); }
      set { _remainingTime.SetValue(value); }
    }

    public Property IsVideoProperty
    {
      get { return _isVideo; }
      set { _isVideo = value; }
    }

    public bool IsVideo
    {
      get { return (bool)_isVideo.GetValue(); }
      set { _isVideo.SetValue(value); }
    }

    public Property IsAudioProperty
    {
      get { return _isAudio; }
      set { _isAudio = value; }
    }

    public bool IsAudio
    {
      get { return (bool)_isAudio.GetValue(); }
      set { _isAudio.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the remaining time property.
    /// </summary>
    /// <value>The remaining time property.</value>
    public Property RemainingTimeProperty
    {
      get { return _remainingTime; }
      set { _remainingTime = value; }
    }

    /// <summary>
    /// gets or sets the duration
    /// </summary>
    public Property DurationProperty
    {
      get
      {
        return _duration;
      }
      set { _duration = value; }
    }
    /// <summary>
    /// Gets the duration.
    /// </summary>
    /// <value>The duration.</value>
    public string Duration
    {
      get
      {
        return (string)_duration.GetValue();
      }
      set
      {
        _duration.SetValue(value);
      }
    }

    /// <summary>
    /// Gets the seek time.
    /// </summary>
    /// <value>The seek time.</value>
    public string SeekTime
    {
      get { return _players.Seeking.SeekTime; }
    }

    /// <summary>
    /// Gets the seek time property.
    /// </summary>
    /// <value>The seek time property.</value>
    public Property SeekTimeProperty
    {
      get { return _players.Seeking.SeekTimeProperty; }
    }

    /// <summary>
    /// Gets the context menu.
    /// </summary>
    /// <value>The context menu.</value>
    public ItemsCollection ContextMenu
    {
      get
      {
        if (_contextMenu == null)
        {
          IMenuCollection menuCollect = ServiceScope.Get<IMenuCollection>();
          _contextMenu = MenuHelper.WrapMenu(menuCollect.GetMenu("fullscreenVideocontext"));
        }
        return _contextMenu;
      }
    }

    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    /// <value>The volume.</value>
    public int Volume
    {
      get
      {
        return (int)_volume.GetValue();
      }
      set
      {
        if (_players.Count != 0)
        {
          _players[0].Volume = value;
          _volume.SetValue(value);
        }
      }
    }

    /// <summary>
    /// Updates the progress properties
    /// </summary>
    private void UpdateProgressProperies()
    {
      TimeSpan ts = DateTime.Now - _updateTimer;
      if (ts.TotalSeconds >= 1)
      {
        IsVideo = _players[0].IsVideo;
        IsAudio = _players[0].IsAudio;


        PIP = (IsVideo && _players.Count > 1);

        ts = _players[0].Duration - _players[0].CurrentTime;
        if (ts.Hours > 0)
        {
          RemainingTime = String.Format("{0}:{1}:{2}", ts.Hours, ts.Minutes.ToString("0#"), ts.Seconds.ToString("0#"));
        }
        else
        {
          RemainingTime = String.Format("{0}:{1}", ts.Minutes, ts.Seconds.ToString("0#"));
        }
        ts = _players[0].CurrentTime;
        if (ts.Hours > 0)
        {
          CurrentTime = String.Format("{0}:{1}:{2}", ts.Hours, ts.Minutes.ToString("0#"), ts.Seconds.ToString("0#"));
        }
        else
        {
          CurrentTime = String.Format("{0}:{1}", ts.Minutes, ts.Seconds.ToString("0#"));
        }

        ts = _players[0].Duration;
        if (ts.Hours > 0)
        {
          Duration = String.Format("{0}:{1}:{2}", ts.Hours, ts.Minutes.ToString("0#"), ts.Seconds.ToString("0#"));
        }
        else
        {
          Duration = String.Format("{0}:{1}", ts.Minutes, ts.Seconds.ToString("0#"));
        }
        _updateTimer = DateTime.Now;
      }

      double current = _players[0].CurrentTime.TotalMilliseconds;
      double duration = _players[0].Duration.TotalMilliseconds;
      current /= duration;
      current *= 100.0f;
      if (Math.Abs(PercentPlayed - (float)current) >= 1)
      {
        PercentPlayed = (float)current;
      }
    }

    /// <summary>
    /// Gets or sets the volume property.
    /// </summary>
    /// <value>The volume property.</value>
    public Property VolumeProperty
    {
      get
      {
        return _volume;
      }
      set
      {
        _volume = value;
      }
    }

    /// <summary>
    /// Updates the properties.
    /// </summary>
    public void UpdateProperties()
    {
      DetermineIfTopbarIsVisble();

      if (_players.Count == 0)
      {
        RemainingTime = "";
        CurrentTime = "";
        PercentPlayed = 0.0f;

        IsVideo = false;
        IsAudio = false;
        PIP = false;
        if (_mediaItem != null && _mediaItem.Labels.Count > 0)
        {
          _mediaItem.Labels.Clear();
          _mediaItem.FireChange();
        }
        return;
      }

      // Don't update the properties if we are paused
      if (_players.Paused == false)
      {
        UpdateProgressProperies();
      }

      CurrentZoomMode = SkinContext.Geometry.Geometries[SkinContext.Geometry.Index].Name;
      TopLeftCropping = String.Format("({0},{1})", SkinContext.CropSettings.Left, SkinContext.CropSettings.Top);
      BottomRightCropping = String.Format("({0},{1})", SkinContext.CropSettings.Right, SkinContext.CropSettings.Bottom);

      IMediaItem mediaItem = _players[0].MediaItem;
      if (_prevMediaItem != mediaItem)
      {
        _prevMediaItem = mediaItem;
        if (mediaItem != null)
        {

          Title = mediaItem.Title;
          Dictionary<string, object>.Enumerator enumer = mediaItem.MetaData.GetEnumerator();
          while (enumer.MoveNext())
          {
            object v = enumer.Current.Value;
            if (v != null)
            {
              //_mediaItem.Labels[enumer.Current.Key] = v.ToString();
            }
          }
        }
        else
        {
          Title = "";
          _mediaItem.Labels.Clear();
        }
        _mediaItem.FireChange();
      }
    }


    #region zoom modes

    /// <summary>
    /// Gets the zoom modes.
    /// </summary>
    /// <value>The zoom modes.</value>
    public ItemsCollection ZoomModes
    {
      get
      {
        ItemsCollection items = new ItemsCollection();
        for (int i = 0; i < SkinContext.Geometry.Geometries.Count; ++i)
        {
          ListItem item = new ListItem("Name", SkinContext.Geometry.Geometries[i].Name);
          item.Selected = (i == SkinContext.Geometry.Index);
          items.Add(item);
        }
        return items;
      }
    }

    /// <summary>
    /// Sets the zoom mode.
    /// </summary>
    /// <param name="item">The item.</param>
    public void SetZoomMode(ListItem item)
    {
      string name = item.Label("Name", "").Evaluate();
      for (int i = 0; i < SkinContext.Geometry.Geometries.Count; ++i)
        if (SkinContext.Geometry.Geometries[i].Name == name)
        {
          SkinContext.Geometry.Index = i;
          break;
        }
      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.Geometry = SkinContext.Geometry.Geometries[SkinContext.Geometry.Index].Name;
      ServiceScope.Get<ISettingsManager>().Save(settings);
      _timer = SkinContext.Now;
    }

    /// <summary>
    /// Selects the next zoom mode.
    /// </summary>
    public void NextZoomMode()
    {
      _timer = SkinContext.Now;
      int index = SkinContext.Geometry.Index + 1;
      if (index >= SkinContext.Geometry.Geometries.Count)
      {
        index = 0;
      }
      SkinContext.Geometry.Index = index;

      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.Geometry = SkinContext.Geometry.Geometries[SkinContext.Geometry.Index].Name;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Gets the current zoom mode.
    /// </summary>
    /// <value>The current zoom mode.</value>
    public string CurrentZoomMode
    {
      get { return (string)_zoomMode.GetValue(); }
      set { _zoomMode.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the current zoom mode property.
    /// </summary>
    /// <value>The current zoom mode property.</value>
    public Property CurrentZoomModeProperty
    {
      get { return _zoomMode; }
      set { _zoomMode = value; }
    }

    #endregion

    #region cropping

    /// <summary>
    /// Sets the cropping.
    /// </summary>
    /// <param name="crop">The crop.</param>
    public void SetCrop(string crop)
    {
      string[] parts = crop.Split(new char[] { ',' });
      int[] skip = new int[4];
      for (int i = 0; i < 4; ++i)
      {
        skip[i] = Int32.Parse(parts[i]);
      }

      SkinContext.CropSettings.Left += skip[0];
      SkinContext.CropSettings.Top += skip[1];
      SkinContext.CropSettings.Right += skip[2];
      SkinContext.CropSettings.Bottom += skip[3];
      if (SkinContext.CropSettings.Left < 0)
      {
        SkinContext.CropSettings.Left = 0;
      }
      if (SkinContext.CropSettings.Top < 0)
      {
        SkinContext.CropSettings.Top = 0;
      }
      if (SkinContext.CropSettings.Right < 0)
      {
        SkinContext.CropSettings.Right = 0;
      }
      if (SkinContext.CropSettings.Bottom < 0)
      {
        SkinContext.CropSettings.Bottom = 0;
      }

      VideoSettings settings = ServiceScope.Get<ISettingsManager>().Load<VideoSettings>();
      settings.Crop = SkinContext.CropSettings;
      SkinContext.CropSettings = settings.Crop;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Gets or sets the top left cropping.
    /// </summary>
    /// <value>The top left cropping.</value>
    public string TopLeftCropping
    {
      get { return (string)_croppingTopLeft.GetValue(); }
      set { _croppingTopLeft.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the top left cropping property.
    /// </summary>
    /// <value>The top left cropping property.</value>
    public Property TopLeftCroppingProperty
    {
      get { return _croppingTopLeft; }
      set { _croppingTopLeft = value; }
    }

    /// <summary>
    /// Gets or sets the bottom right cropping.
    /// </summary>
    /// <value>The bottom right cropping.</value>
    public string BottomRightCropping
    {
      get { return (string)_croppingBottomRight.GetValue(); }
      set { _croppingBottomRight.SetValue(value); }
    }

    /// <summary>
    /// Gets or sets the bottom right cropping property.
    /// </summary>
    /// <value>The bottom right cropping property.</value>
    public Property BottomRightCroppingProperty
    {
      get { return _croppingBottomRight; }
      set { _croppingBottomRight = value; }
    }

    #endregion

    void OnPlayerMessageReceived(QueueMessage message)
    {
      _updateTimer = DateTime.MinValue;
      UpdateProperties();
      if (_players.Count > 0)
      {
        IsVideo = _players[0].IsVideo;
        IsAudio = _players[0].IsAudio;

        PIP = (IsVideo && _players.Count > 1);
      }
    }

  }
}

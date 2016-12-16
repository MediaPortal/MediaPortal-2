#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.Players.Video;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Players;
using MediaPortal.UiComponents.BackgroundManager.Helper;
using MediaPortal.UiComponents.BackgroundManager.Settings;

namespace MediaPortal.UiComponents.BackgroundManager.Models
{
  public class VideoBackgroundModel : IDisposable
  {
    #region Consts and static fields

    public const string MODEL_ID_STR = "441288AC-F88D-4186-8993-6E259F7C75D8";
    public static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    protected static IVideoPlayerSynchronizationStrategy _backgroundPlayerStrategy = new BackgroundVideoPlayerSynchronizationStrategy();

    #endregion

    #region Protected fields

    protected string _videoFilename;
    protected VideoPlayer _videoPlayer;
    protected AbstractProperty _videoPlayerProperty;
    protected AbstractProperty _isEnabledProperty;
    protected IPlayerSlotController _backgroundPsc = null;
    protected MediaItem _video;
    private readonly SettingsChangeWatcher<BackgroundManagerSettings> _settings = new SettingsChangeWatcher<BackgroundManagerSettings>();

    #endregion

    #region Static members which also can be used from other models

    public static VideoBackgroundModel GetCurrentInstance()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return (VideoBackgroundModel) workflowManager.GetModel(MODEL_ID);
    }

    #endregion

    public AbstractProperty VideoPlayerProperty
    {
      get { return _videoPlayerProperty; }
    }

    public ISharpDXVideoPlayer VideoPlayer
    {
      get { return (ISharpDXVideoPlayer) _videoPlayerProperty.GetValue(); }
      set { _videoPlayerProperty.SetValue(value); }
    }

    public IPlayerSlotController PlayerSlotController
    {
      get { return _backgroundPsc; }
    }

    public AbstractProperty IsEnabledProperty
    {
      get { return _isEnabledProperty; }
    }

    public bool IsEnabled
    {
      get { return (bool) _isEnabledProperty.GetValue(); }
      set { _isEnabledProperty.SetValue(value); }
    }

    public VideoBackgroundModel()
    {
      _videoPlayerProperty = new WProperty(typeof(ISharpDXVideoPlayer), null);
      _isEnabledProperty = new WProperty(typeof(bool), false);
      _settings.SettingsChanged += RefreshSettings;
      RefreshSettings();
    }

    private void RefreshSettings(object sender, EventArgs e)
    {
      RefreshSettings(true);
    }

    /// <summary>
    /// Loads settings on startup or when changed inside configuration.
    /// </summary>
    protected void RefreshSettings(bool refresh = false)
    {
      EndBackgroundPlayback();
      if (_settings.Settings.EnableVideoBackground)
      {
        _videoFilename = _settings.Settings.VideoBackgroundFileName;
        _video = string.IsNullOrWhiteSpace(_videoFilename) ? null : MediaItemHelper.CreateMediaItem(_videoFilename);
        IsEnabled = MediaItemHelper.IsValidVideo(_video);
      }
      else 
        IsEnabled = false;

      if (IsEnabled && refresh)
        StartBackgroundPlayback();
    }

    public void Dispose()
    {
      _settings.Dispose();
      EndBackgroundPlayback();
    }

    public void EndBackgroundPlayback()
    {
      if (_backgroundPsc != null)
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        playerManager.CloseSlot(_backgroundPsc);
        _backgroundPsc = null;
        VideoPlayer = null;
      }
    }

    public void StartBackgroundPlayback()
    {
      if (!IsEnabled)
        return;

      IScreenControl screenControl = ServiceRegistration.Get<IScreenControl>();
      IVideoPlayerSynchronizationStrategy current = screenControl.VideoPlayerSynchronizationStrategy;
      if (current != _backgroundPlayerStrategy)
        // We replace the default strategy with our own to prefer the video background player.
        screenControl.VideoPlayerSynchronizationStrategy = _backgroundPlayerStrategy;

      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      if (_backgroundPsc == null)
        _backgroundPsc = playerManager.OpenSlot();

      if (_backgroundPsc == null)
        return;

      // If we already have a player active, don't start a new one.
      IPlayer currentPlayer = _backgroundPsc.CurrentPlayer;
      if (currentPlayer != null && currentPlayer.State == PlayerState.Active)
        return;

      try
      {
        _backgroundPsc.Play(_video, StartTime.AtOnce);
        BaseDXPlayer player = _backgroundPsc.CurrentPlayer as BaseDXPlayer;
        if (player != null)
          player.AutoRepeat = true;

        VideoPlayer = player as ISharpDXVideoPlayer;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("VideoBackgroundModel: Error opening MediaItem {0} for background playback!", ex, _videoFilename);
      }
    }
  }
}

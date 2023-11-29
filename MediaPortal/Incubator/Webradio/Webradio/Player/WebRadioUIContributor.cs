#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.UserServices.FanArtService.Client.Models;
using MediaPortal.UI.Players.BassPlayer.Interfaces;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.SkinEngine.Controls.ImageSources;
using MediaPortal.UiComponents.Media.Models;
using System;
using System.Timers;
using Webradio.Helper;
using Webradio.Models;

namespace Webradio.Player
{
  public class WebRadioUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    #region Protected fields

    protected bool _updating = false;
    protected PlayerChoice _playerContext;
    protected MediaWorkflowStateType _mediaWorkflowStateType;
    protected ITagSource _tagSource;
 
    #endregion

    private string _artist = string.Empty;
    private string _name = string.Empty;

    private Timer _timer = new Timer();
    private int _fanartIdx;
    private NavigationContext _context;
    private TrackInfo _info = new TrackInfo();

    #region Constructor

    public WebRadioUIContributor() : base(true, 1000)
    {
      _context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;
    }

    #endregion

    // Update GUI properties
    protected override void Update()
    {
      if (_updating)
      {
        ServiceRegistration.Get<ILogger>().Warn("WebRadioUIContributor: last update cycle still not finished.");
        return;
      }
      _updating = true;

      try
      {
        if (_tagSource != null)
        {
          var tags = _tagSource.Tags;

          if (tags == null)
            return;
          
          WebradioDataModel.TrackArtist = tags.artist;
          WebradioDataModel.TrackName = tags.title;
          WebradioDataModel.StreamBitrate = tags.bitrate + " Kbps";

          if ((WebradioDataModel.TrackArtist != _artist) | (WebradioDataModel.TrackName != _name))
          {
            _artist = WebradioDataModel.TrackArtist;
            _name = WebradioDataModel.TrackName;

            if ((WebradioDataModel.TrackName != "") & (WebradioDataModel.TrackArtist != ""))
            {
              _info = new TrackInfo(WebradioDataModel.TrackArtist, WebradioDataModel.TrackName);

              if ((_info.FrontCover == null) | (_info.FrontCover == ""))
                WebradioDataModel.CurrentStreamLogo = WebradioDataModel.DefaultStreamLogo;
              else
                WebradioDataModel.CurrentStreamLogo = _info.FrontCover;

              if (_info.ArtistBackgrounds.Count > 0)
              {
                var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
                if (fanArtBgModel == null) return;
                fanArtBgModel.ImageSource = new MultiImageSource { UriSource = _info.ArtistBackgrounds[0] };
                _fanartIdx++;
                _timer.Start();
              }
              else
              {
                ClearFanart();
              }
            }
          }

          return;
        }

        WebradioDataModel.TrackArtist = string.Empty;
        WebradioDataModel.TrackName = string.Empty;
        WebradioDataModel.StreamBitrate = string.Empty;

        ClearFanart();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("WebRadioUIContributor: Error updating properties.", ex);
      }
      finally
      {
        _updating = false;
      }

    }

    private void ClearFanart()
    {
      _timer.Stop();
      var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
      if (fanArtBgModel != null) fanArtBgModel.ImageSource = new MultiImageSource { UriSource = null };
    }

    private void OnTimedEvent(object sender, ElapsedEventArgs e)
    {
      var context = ServiceRegistration.Get<IWorkflowManager>().CurrentNavigationContext;

      if (context == null)
      {
        _timer.Stop();
        return;
      }

      if (context.WorkflowModelId != _context.WorkflowModelId)
      {
        _timer.Stop();
        return;
      }

      if (_info.ArtistBackgrounds.Count > _fanartIdx)
      {
        var fanArtBgModel = (FanArtBackgroundModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(FanArtBackgroundModel.FANART_MODEL_ID);
        if (fanArtBgModel == null) return;
        var uriSource = _info.ArtistBackgrounds[_fanartIdx];
        fanArtBgModel.ImageSource = uriSource != "" ? new MultiImageSource { UriSource = uriSource } : new MultiImageSource { UriSource = null };
      }

      if (_fanartIdx == _info.ArtistBackgrounds.Count)
      {
        _fanartIdx = 0;
      }
      else
      {
        _fanartIdx += 1;
      }
    }

    #region const

    protected const string SCREEN_FULLSCREEN_AUDIO = "webradio_FullScreenContent";
    protected const string SCREEN_CURRENTLY_PLAYING_AUDIO = "webradio_CurrentlyPlaying";

    #endregion

    #region IPlayerUIContributor implementation

    public MediaWorkflowStateType MediaWorkflowStateType
    {
      get { return _mediaWorkflowStateType; }
    }

    public string Screen
    {
      get
      {
        if (_mediaWorkflowStateType == MediaWorkflowStateType.CurrentlyPlaying)
          return SCREEN_CURRENTLY_PLAYING_AUDIO;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return SCREEN_FULLSCREEN_AUDIO;
        return null;
      }
    }

    public bool BackgroundDisabled
    {
      get { return _mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent; }
    }

    public virtual void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _playerContext = stateType == MediaWorkflowStateType.CurrentlyPlaying ? PlayerChoice.CurrentPlayer : PlayerChoice.PrimaryPlayer;
      _mediaWorkflowStateType = stateType;
      _tagSource = player as ITagSource;

      WebradioDataModel.StationName = WebradioDataModel.SelectedStream.Name;
      WebradioDataModel.StationCountry = "[Country." + WebradioDataModel.SelectedStream.Country + "]";
      WebradioDataModel.StationCity = WebradioDataModel.SelectedStream.City;
      WebradioDataModel.StationLanguage = "[Language." + WebradioDataModel.SelectedStream.Language + "]";

      _timer.Elapsed += OnTimedEvent;
      _timer.Interval = 15000;
    }

    #endregion
  }
}

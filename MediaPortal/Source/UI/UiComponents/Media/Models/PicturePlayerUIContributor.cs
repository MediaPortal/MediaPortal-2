#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models
{
  public class PicturePlayerUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    #region Protected fields

    protected bool _updating = false;
    protected PlayerChoice _playerContext;
    protected MediaItem _currentMediaItem = null;

    protected MediaWorkflowStateType _mediaWorkflowStateType;
    protected IPicturePlayer _player;
    protected AbstractProperty _pictureWidthProperty;
    protected AbstractProperty _pictureHeightProperty;
    protected AbstractProperty _pictureMakeProperty;
    protected AbstractProperty _pictureModelProperty;
    protected AbstractProperty _pictureExposureBiasProperty;
    protected AbstractProperty _pictureExposureTimeProperty;
    protected AbstractProperty _pictureFlashModeProperty;
    protected AbstractProperty _pictureFNumberProperty;
    protected AbstractProperty _pictureDimensionsProperty;
    protected AbstractProperty _pictureISOSpeedProperty;
    protected AbstractProperty _pictureMeteringModeProperty;

    #endregion

    #region Constructor

    public PicturePlayerUIContributor() : base(300)
    {}

    #endregion

    #region Properties

    public IPicturePlayer PicturePlayerInstance
    {
      get { return _player; }
    }

    #region Picture Metadata related properties

    public AbstractProperty PictureWidthProperty
    {
      get { return _pictureWidthProperty; }
    }

    public int PictureWidth
    {
      get { return (int) _pictureWidthProperty.GetValue(); }
      set { _pictureWidthProperty.SetValue(value); }
    }

    public AbstractProperty PictureHeightProperty
    {
      get { return _pictureHeightProperty; }
    }

    public int PictureHeight
    {
      get { return (int) _pictureHeightProperty.GetValue(); }
      set { _pictureHeightProperty.SetValue(value); }
    }

    public AbstractProperty PictureMakeProperty
    {
      get { return _pictureMakeProperty; }
    }

    public string PictureMake
    {
      get { return (string) _pictureMakeProperty.GetValue(); }
      set { _pictureMakeProperty.SetValue(value); }
    }

    public AbstractProperty PictureModelProperty
    {
      get { return _pictureModelProperty; }
    }

    public string PictureModel
    {
      get { return (string) _pictureModelProperty.GetValue(); }
      set { _pictureModelProperty.SetValue(value); }
    }

    public AbstractProperty PictureExposureBiasProperty
    {
      get { return _pictureExposureBiasProperty; }
    }

    public double PictureExposureBias
    {
      get { return (double) _pictureExposureBiasProperty.GetValue(); }
      set { _pictureExposureBiasProperty.SetValue(value); }
    }

    public AbstractProperty PictureExposureTimeProperty
    {
      get { return _pictureExposureTimeProperty; }
    }

    public string PictureExposureTime
    {
      get { return (string) _pictureExposureTimeProperty.GetValue(); }
      set { _pictureExposureTimeProperty.SetValue(value); }
    }

    public AbstractProperty PictureFlashModeProperty
    {
      get { return _pictureFlashModeProperty; }
    }

    public string PictureFlashMode
    {
      get { return (string) _pictureFlashModeProperty.GetValue(); }
      set { _pictureFlashModeProperty.SetValue(value); }
    }

    public AbstractProperty PictureFNumberProperty
    {
      get { return _pictureFNumberProperty; }
    }

    public string PictureFNumber
    {
      get { return (string) _pictureFNumberProperty.GetValue(); }
      set { _pictureFNumberProperty.SetValue(value); }
    }

    public AbstractProperty PictureDimensionsProperty
    {
      get { return _pictureDimensionsProperty; }
    }

    public string PictureDimensions
    {
      get { return (string) _pictureDimensionsProperty.GetValue(); }
      set { _pictureDimensionsProperty.SetValue(value); }
    }

    public AbstractProperty PictureISOSpeedProperty
    {
      get { return _pictureISOSpeedProperty; }
    }

    public string PictureISOSpeed
    {
      get { return (string) _pictureISOSpeedProperty.GetValue(); }
      set { _pictureISOSpeedProperty.SetValue(value); }
    }

    public AbstractProperty PictureMeteringModeProperty
    {
      get { return _pictureMeteringModeProperty; }
    }

    public string PictureMeteringMode
    {
      get { return (string) _pictureMeteringModeProperty.GetValue(); }
      set { _pictureMeteringModeProperty.SetValue(value); }
    }

    #endregion

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
          return Consts.SCREEN_CURRENTLY_PLAYING_PICTURE;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return Consts.SCREEN_FULLSCREEN_PICTURE;
        return null;
      }
    }

    public bool BackgroundDisabled
    {
      get { return _mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent; }
    }

    public void Initialize(MediaWorkflowStateType stateType, IPlayer player)
    {
      _playerContext = stateType == MediaWorkflowStateType.CurrentlyPlaying ? PlayerChoice.CurrentPlayer : PlayerChoice.PrimaryPlayer;
      _mediaWorkflowStateType = stateType;
      _player = player as IPicturePlayer;

      _pictureWidthProperty = new WProperty(typeof(int), null);
      _pictureHeightProperty = new WProperty(typeof(int), null);
      _pictureMakeProperty = new WProperty(typeof(string), string.Empty);
      _pictureModelProperty = new WProperty(typeof(string), string.Empty);
      _pictureExposureBiasProperty = new WProperty(typeof(double), null);
      _pictureExposureTimeProperty = new WProperty(typeof(string), string.Empty);
      _pictureFlashModeProperty = new WProperty(typeof(string), string.Empty);
      _pictureFNumberProperty = new WProperty(typeof(string), string.Empty);
      _pictureDimensionsProperty = new WProperty(typeof(string), string.Empty);
      _pictureISOSpeedProperty = new WProperty(typeof(string), string.Empty);
      _pictureMeteringModeProperty = new WProperty(typeof(string), string.Empty);
    }

    #endregion

    #region Base overrides

    protected override void Update()
    {
      if (_updating)
        return;
      _updating = true;
      try
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        IPlayerContext playerContext = playerContextManager.GetPlayerContext(_playerContext);

        _currentMediaItem = playerContext == null ? null : playerContext.CurrentMediaItem;
        MediaItemAspect pictureAspect;
        if (_currentMediaItem == null || !_currentMediaItem.Aspects.TryGetValue(PictureAspect.ASPECT_ID, out pictureAspect))
          pictureAspect = null;

        if (pictureAspect == null)
        {
          PictureDimensions = string.Empty;
          PictureMake = string.Empty;
          PictureModel = string.Empty;
          PictureISOSpeed = string.Empty;
          PictureExposureTime = string.Empty;
          PictureFNumber = string.Empty;
          PictureFlashMode = string.Empty;
          PictureMeteringMode = string.Empty;
        }
        else
        {
          PictureDimensions = String.Format("{0} x {1}", pictureAspect[PictureAspect.ATTR_WIDTH],pictureAspect[PictureAspect.ATTR_HEIGHT]);
          PictureMake = (string) pictureAspect[PictureAspect.ATTR_MAKE];
          PictureModel = (string) pictureAspect[PictureAspect.ATTR_MODEL];
          PictureISOSpeed = (string) pictureAspect[PictureAspect.ATTR_ISO_SPEED];
          PictureExposureTime = (string) pictureAspect[PictureAspect.ATTR_EXPOSURE_TIME];
          PictureFNumber = (string) pictureAspect[PictureAspect.ATTR_FNUMBER];
          PictureFlashMode = (string) pictureAspect[PictureAspect.ATTR_FLASH_MODE];
          PictureMeteringMode = (string) pictureAspect[PictureAspect.ATTR_METERING_MODE];
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerControl: Error updating properties", e);
      }
      finally
      {
        _updating = false;
      }
    }

    #endregion
  }
}
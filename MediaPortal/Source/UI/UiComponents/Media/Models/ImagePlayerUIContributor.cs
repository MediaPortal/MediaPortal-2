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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.UiComponents.Media.Models
{
  public class ImagePlayerUIContributor : BaseTimerControlledModel, IPlayerUIContributor
  {
    #region Protected fields

    protected bool _updating = false;
    protected PlayerChoice _playerContext;
    protected MediaItem _currentMediaItem = null;

    protected MediaWorkflowStateType _mediaWorkflowStateType;
    protected IImagePlayer _player;
    protected AbstractProperty _transitionDurationProperty;
    protected AbstractProperty _imageWidthProperty;
    protected AbstractProperty _imageHeightProperty;
    protected AbstractProperty _cameraMakeProperty;
    protected AbstractProperty _cameraModelProperty;
    protected AbstractProperty _imageExposureBiasProperty;
    protected AbstractProperty _imageExposureTimeProperty;
    protected AbstractProperty _imageFlashModeProperty;
    protected AbstractProperty _imageFNumberProperty;
    protected AbstractProperty _imageDimensionsProperty;
    protected AbstractProperty _imageISOSpeedProperty;
    protected AbstractProperty _imageMeteringModeProperty;
    protected AbstractProperty _imageCountryProperty;
    protected AbstractProperty _imageStateProperty;
    protected AbstractProperty _imageCityProperty;

    #endregion

    #region Constructor

    public ImagePlayerUIContributor() : base(true, 300)
    {
      _transitionDurationProperty = new WProperty(typeof(double), 2d);
      _imageWidthProperty = new WProperty(typeof(int), null);
      _imageHeightProperty = new WProperty(typeof(int), null);
      _cameraMakeProperty = new WProperty(typeof(string), string.Empty);
      _cameraModelProperty = new WProperty(typeof(string), string.Empty);
      _imageExposureBiasProperty = new WProperty(typeof(double), null);
      _imageExposureTimeProperty = new WProperty(typeof(string), string.Empty);
      _imageFlashModeProperty = new WProperty(typeof(string), string.Empty);
      _imageFNumberProperty = new WProperty(typeof(string), string.Empty);
      _imageDimensionsProperty = new WProperty(typeof(string), string.Empty);
      _imageISOSpeedProperty = new WProperty(typeof(string), string.Empty);
      _imageMeteringModeProperty = new WProperty(typeof(string), string.Empty);
      _imageCountryProperty = new WProperty(typeof(string), string.Empty);
      _imageStateProperty = new WProperty(typeof(string), string.Empty);
      _imageCityProperty = new WProperty(typeof(string), string.Empty);
    }

    #endregion

    #region Properties

    public IImagePlayer ImagePlayerInstance
    {
      get { return _player; }
    }

    #region Image metadata related properties

    public AbstractProperty TransitionDurationProperty
    {
      get { return _transitionDurationProperty; }
    }

    public double TransitionDuration
    {
      get { return (double)_transitionDurationProperty.GetValue(); }
      set { _transitionDurationProperty.SetValue(value); }
    }

    public AbstractProperty ImageWidthProperty
    {
      get { return _imageWidthProperty; }
    }

    public int ImageWidth
    {
      get { return (int) _imageWidthProperty.GetValue(); }
      set { _imageWidthProperty.SetValue(value); }
    }

    public AbstractProperty ImageHeightProperty
    {
      get { return _imageHeightProperty; }
    }

    public int ImageHeight
    {
      get { return (int) _imageHeightProperty.GetValue(); }
      set { _imageHeightProperty.SetValue(value); }
    }

    public AbstractProperty CameraMakeProperty
    {
      get { return _cameraMakeProperty; }
    }

    public string CameraMake
    {
      get { return (string) _cameraMakeProperty.GetValue(); }
      set { _cameraMakeProperty.SetValue(value); }
    }

    public AbstractProperty CameraModelProperty
    {
      get { return _cameraModelProperty; }
    }

    public string CameraModel
    {
      get { return (string) _cameraModelProperty.GetValue(); }
      set { _cameraModelProperty.SetValue(value); }
    }

    public AbstractProperty ImageExposureBiasProperty
    {
      get { return _imageExposureBiasProperty; }
    }

    public double ImageExposureBias
    {
      get { return (double) _imageExposureBiasProperty.GetValue(); }
      set { _imageExposureBiasProperty.SetValue(value); }
    }

    public AbstractProperty ImageExposureTimeProperty
    {
      get { return _imageExposureTimeProperty; }
    }

    public string ImageExposureTime
    {
      get { return (string) _imageExposureTimeProperty.GetValue(); }
      set { _imageExposureTimeProperty.SetValue(value); }
    }

    public AbstractProperty ImageFlashModeProperty
    {
      get { return _imageFlashModeProperty; }
    }

    public string ImageFlashMode
    {
      get { return (string) _imageFlashModeProperty.GetValue(); }
      set { _imageFlashModeProperty.SetValue(value); }
    }

    public AbstractProperty ImageFNumberProperty
    {
      get { return _imageFNumberProperty; }
    }

    public string ImageFNumber
    {
      get { return (string) _imageFNumberProperty.GetValue(); }
      set { _imageFNumberProperty.SetValue(value); }
    }

    public AbstractProperty ImageDimensionsProperty
    {
      get { return _imageDimensionsProperty; }
    }

    public string ImageDimensions
    {
      get { return (string) _imageDimensionsProperty.GetValue(); }
      set { _imageDimensionsProperty.SetValue(value); }
    }

    public AbstractProperty ImageISOSpeedProperty
    {
      get { return _imageISOSpeedProperty; }
    }

    public string ImageISOSpeed
    {
      get { return (string) _imageISOSpeedProperty.GetValue(); }
      set { _imageISOSpeedProperty.SetValue(value); }
    }

    public AbstractProperty ImageMeteringModeProperty
    {
      get { return _imageMeteringModeProperty; }
    }

    public string ImageMeteringMode
    {
      get { return (string) _imageMeteringModeProperty.GetValue(); }
      set { _imageMeteringModeProperty.SetValue(value); }
    }

    public AbstractProperty CountryProperty
    {
      get { return _imageCountryProperty; }
    }

    public string Country
    {
      get { return (string) _imageCountryProperty.GetValue(); }
      set { _imageCountryProperty.SetValue(value); }
    }

    public AbstractProperty StateProperty
    {
      get { return _imageStateProperty; }
    }

    public string State
    {
      get { return (string) _imageStateProperty.GetValue(); }
      set { _imageStateProperty.SetValue(value); }
    }

    public AbstractProperty CityProperty
    {
      get { return _imageCityProperty; }
    }

    public string City
    {
      get { return (string) _imageCityProperty.GetValue(); }
      set { _imageCityProperty.SetValue(value); }
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
          return Consts.SCREEN_CURRENTLY_PLAYING_IMAGE;
        if (_mediaWorkflowStateType == MediaWorkflowStateType.FullscreenContent)
          return Consts.SCREEN_FULLSCREEN_IMAGE;
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
      _player = player as IImagePlayer;
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
        SingleMediaItemAspect imageAspect;
        if (_currentMediaItem == null || !MediaItemAspect.TryGetAspect(_currentMediaItem.Aspects, ImageAspect.Metadata, out imageAspect))
          imageAspect = null;

        if (imageAspect == null)
        {
          ImageDimensions = string.Empty;
          CameraMake = string.Empty;
          CameraModel = string.Empty;
          ImageISOSpeed = string.Empty;
          ImageExposureTime = string.Empty;
          ImageFNumber = string.Empty;
          ImageFlashMode = string.Empty;
          ImageMeteringMode = string.Empty;
          Country = string.Empty;
          State = string.Empty;
          City = string.Empty;
        }
        else
        {
          var mpc = playerContext != null ?playerContext.CurrentPlayer as IMediaPlaybackControl : null;
          if (mpc != null)
            TransitionDuration = mpc.IsPaused ? 0.1d : 2.0d;
          ImageDimensions = String.Format("{0} x {1}", imageAspect[ImageAspect.ATTR_WIDTH], imageAspect[ImageAspect.ATTR_HEIGHT]);
          CameraMake = (string) imageAspect[ImageAspect.ATTR_MAKE];
          CameraModel = (string) imageAspect[ImageAspect.ATTR_MODEL];
          ImageISOSpeed = (string) imageAspect[ImageAspect.ATTR_ISO_SPEED];
          ImageExposureTime = (string) imageAspect[ImageAspect.ATTR_EXPOSURE_TIME];
          ImageFNumber = (string) imageAspect[ImageAspect.ATTR_FNUMBER];
          ImageFlashMode = (string) imageAspect[ImageAspect.ATTR_FLASH_MODE];
          ImageMeteringMode = (string) imageAspect[ImageAspect.ATTR_METERING_MODE];
          Country = (string) imageAspect[ImageAspect.ATTR_COUNTRY];
          State = (string) imageAspect[ImageAspect.ATTR_STATE];
          City = (string) imageAspect[ImageAspect.ATTR_CITY];
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ImagePlayerUIContributor: Error updating properties", e);
      }
      finally
      {
        _updating = false;
      }
    }

    #endregion
  }
}

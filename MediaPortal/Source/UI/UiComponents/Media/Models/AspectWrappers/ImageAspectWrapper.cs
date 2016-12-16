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
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// ImageAspectWrapper wraps the contents of <see cref="ImageAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class ImageAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _aspectWidthProperty;
protected AbstractProperty _aspectHeightProperty;
protected AbstractProperty _equipmentMakeProperty;
protected AbstractProperty _equipmentModelProperty;
protected AbstractProperty _exposureBiasProperty;
protected AbstractProperty _exposureTimeProperty;
protected AbstractProperty _flashModeProperty;
protected AbstractProperty _fNumberProperty;
protected AbstractProperty _iSOSpeedRatingProperty;
protected AbstractProperty _orientationProperty;
protected AbstractProperty _meteringModeProperty;
protected AbstractProperty _latitudeProperty;
protected AbstractProperty _longitudeProperty;
protected AbstractProperty _cityProperty;
protected AbstractProperty _stateProperty;
protected AbstractProperty _countryProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty AspectWidthProperty
{
  get{ return _aspectWidthProperty; }
}

public int? AspectWidth
{
  get { return (int?) _aspectWidthProperty.GetValue(); }
  set { _aspectWidthProperty.SetValue(value); }
}

public AbstractProperty AspectHeightProperty
{
  get{ return _aspectHeightProperty; }
}

public int? AspectHeight
{
  get { return (int?) _aspectHeightProperty.GetValue(); }
  set { _aspectHeightProperty.SetValue(value); }
}

public AbstractProperty EquipmentMakeProperty
{
  get{ return _equipmentMakeProperty; }
}

public string EquipmentMake
{
  get { return (string) _equipmentMakeProperty.GetValue(); }
  set { _equipmentMakeProperty.SetValue(value); }
}

public AbstractProperty EquipmentModelProperty
{
  get{ return _equipmentModelProperty; }
}

public string EquipmentModel
{
  get { return (string) _equipmentModelProperty.GetValue(); }
  set { _equipmentModelProperty.SetValue(value); }
}

public AbstractProperty ExposureBiasProperty
{
  get{ return _exposureBiasProperty; }
}

public string ExposureBias
{
  get { return (string) _exposureBiasProperty.GetValue(); }
  set { _exposureBiasProperty.SetValue(value); }
}

public AbstractProperty ExposureTimeProperty
{
  get{ return _exposureTimeProperty; }
}

public string ExposureTime
{
  get { return (string) _exposureTimeProperty.GetValue(); }
  set { _exposureTimeProperty.SetValue(value); }
}

public AbstractProperty FlashModeProperty
{
  get{ return _flashModeProperty; }
}

public string FlashMode
{
  get { return (string) _flashModeProperty.GetValue(); }
  set { _flashModeProperty.SetValue(value); }
}

public AbstractProperty FNumberProperty
{
  get{ return _fNumberProperty; }
}

public string FNumber
{
  get { return (string) _fNumberProperty.GetValue(); }
  set { _fNumberProperty.SetValue(value); }
}

public AbstractProperty ISOSpeedRatingProperty
{
  get{ return _iSOSpeedRatingProperty; }
}

public string ISOSpeedRating
{
  get { return (string) _iSOSpeedRatingProperty.GetValue(); }
  set { _iSOSpeedRatingProperty.SetValue(value); }
}

public AbstractProperty OrientationProperty
{
  get{ return _orientationProperty; }
}

public int? Orientation
{
  get { return (int?) _orientationProperty.GetValue(); }
  set { _orientationProperty.SetValue(value); }
}

public AbstractProperty MeteringModeProperty
{
  get{ return _meteringModeProperty; }
}

public string MeteringMode
{
  get { return (string) _meteringModeProperty.GetValue(); }
  set { _meteringModeProperty.SetValue(value); }
}

public AbstractProperty LatitudeProperty
{
  get{ return _latitudeProperty; }
}

public double? Latitude
{
  get { return (double?) _latitudeProperty.GetValue(); }
  set { _latitudeProperty.SetValue(value); }
}

public AbstractProperty LongitudeProperty
{
  get{ return _longitudeProperty; }
}

public double? Longitude
{
  get { return (double?) _longitudeProperty.GetValue(); }
  set { _longitudeProperty.SetValue(value); }
}

public AbstractProperty CityProperty
{
  get{ return _cityProperty; }
}

public string City
{
  get { return (string) _cityProperty.GetValue(); }
  set { _cityProperty.SetValue(value); }
}

public AbstractProperty StateProperty
{
  get{ return _stateProperty; }
}

public string State
{
  get { return (string) _stateProperty.GetValue(); }
  set { _stateProperty.SetValue(value); }
}

public AbstractProperty CountryProperty
{
  get{ return _countryProperty; }
}

public string Country
{
  get { return (string) _countryProperty.GetValue(); }
  set { _countryProperty.SetValue(value); }
}

public AbstractProperty MediaItemProperty
{
  get{ return _mediaItemProperty; }
}

public MediaItem MediaItem
{
  get { return (MediaItem) _mediaItemProperty.GetValue(); }
  set { _mediaItemProperty.SetValue(value); }
}

#endregion

#region Constructor

public ImageAspectWrapper()
{
  _aspectWidthProperty = new SProperty(typeof(int?));
  _aspectHeightProperty = new SProperty(typeof(int?));
  _equipmentMakeProperty = new SProperty(typeof(string));
  _equipmentModelProperty = new SProperty(typeof(string));
  _exposureBiasProperty = new SProperty(typeof(string));
  _exposureTimeProperty = new SProperty(typeof(string));
  _flashModeProperty = new SProperty(typeof(string));
  _fNumberProperty = new SProperty(typeof(string));
  _iSOSpeedRatingProperty = new SProperty(typeof(string));
  _orientationProperty = new SProperty(typeof(int?));
  _meteringModeProperty = new SProperty(typeof(string));
  _latitudeProperty = new SProperty(typeof(double?));
  _longitudeProperty = new SProperty(typeof(double?));
  _cityProperty = new SProperty(typeof(string));
  _stateProperty = new SProperty(typeof(string));
  _countryProperty = new SProperty(typeof(string));
  _mediaItemProperty = new SProperty(typeof(MediaItem));
  _mediaItemProperty.Attach(MediaItemChanged);
}

#endregion

#region Members

private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

public void Init(MediaItem mediaItem)
{
  SingleMediaItemAspect aspect;
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, ImageAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  AspectWidth = (int?) aspect[ImageAspect.ATTR_WIDTH];
  AspectHeight = (int?) aspect[ImageAspect.ATTR_HEIGHT];
  EquipmentMake = (string) aspect[ImageAspect.ATTR_MAKE];
  EquipmentModel = (string) aspect[ImageAspect.ATTR_MODEL];
  ExposureBias = (string) aspect[ImageAspect.ATTR_EXPOSURE_BIAS];
  ExposureTime = (string) aspect[ImageAspect.ATTR_EXPOSURE_TIME];
  FlashMode = (string) aspect[ImageAspect.ATTR_FLASH_MODE];
  FNumber = (string) aspect[ImageAspect.ATTR_FNUMBER];
  ISOSpeedRating = (string) aspect[ImageAspect.ATTR_ISO_SPEED];
  Orientation = (int?) aspect[ImageAspect.ATTR_ORIENTATION];
  MeteringMode = (string) aspect[ImageAspect.ATTR_METERING_MODE];
  Latitude = (double?) aspect[ImageAspect.ATTR_LATITUDE];
  Longitude = (double?) aspect[ImageAspect.ATTR_LONGITUDE];
  City = (string) aspect[ImageAspect.ATTR_CITY];
  State = (string) aspect[ImageAspect.ATTR_STATE];
  Country = (string) aspect[ImageAspect.ATTR_COUNTRY];
}

public void SetEmpty()
{
  AspectWidth = null;
  AspectHeight = null;
  EquipmentMake = null;
  EquipmentModel = null;
  ExposureBias = null;
  ExposureTime = null;
  FlashMode = null;
  FNumber = null;
  ISOSpeedRating = null;
  Orientation = null;
  MeteringMode = null;
  Latitude = null;
  Longitude = null;
  City = null;
  State = null;
  Country = null;
}

#endregion

}

}

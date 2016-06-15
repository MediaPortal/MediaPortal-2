#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
/// VideoAspectWrapper wraps the contents of <see cref="VideoAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class VideoAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _resourceIndexProperty;
protected AbstractProperty _streamIndexProperty;
protected AbstractProperty _durationProperty;
protected AbstractProperty _audioStreamCountProperty;
protected AbstractProperty _videoEncodingProperty;
protected AbstractProperty _videoBitRateProperty;
protected AbstractProperty _aspectWidthProperty;
protected AbstractProperty _aspectHeightProperty;
protected AbstractProperty _aspectRatioProperty;
protected AbstractProperty _fPSProperty;
protected AbstractProperty _isDVDProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty ResourceIndexProperty
{
  get{ return _resourceIndexProperty; }
}

public int? ResourceIndex
{
  get { return (int?) _resourceIndexProperty.GetValue(); }
  set { _resourceIndexProperty.SetValue(value); }
}

public AbstractProperty StreamIndexProperty
{
  get{ return _streamIndexProperty; }
}

public int? StreamIndex
{
  get { return (int?) _streamIndexProperty.GetValue(); }
  set { _streamIndexProperty.SetValue(value); }
}

public AbstractProperty DurationProperty
{
  get{ return _durationProperty; }
}

public long? Duration
{
  get { return (long?) _durationProperty.GetValue(); }
  set { _durationProperty.SetValue(value); }
}

public AbstractProperty AudioStreamCountProperty
{
  get{ return _audioStreamCountProperty; }
}

public int? AudioStreamCount
{
  get { return (int?) _audioStreamCountProperty.GetValue(); }
  set { _audioStreamCountProperty.SetValue(value); }
}

public AbstractProperty VideoEncodingProperty
{
  get{ return _videoEncodingProperty; }
}

public string VideoEncoding
{
  get { return (string) _videoEncodingProperty.GetValue(); }
  set { _videoEncodingProperty.SetValue(value); }
}

public AbstractProperty VideoBitRateProperty
{
  get{ return _videoBitRateProperty; }
}

public long? VideoBitRate
{
  get { return (long?) _videoBitRateProperty.GetValue(); }
  set { _videoBitRateProperty.SetValue(value); }
}

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

public AbstractProperty AspectRatioProperty
{
  get{ return _aspectRatioProperty; }
}

public float? AspectRatio
{
  get { return (float?) _aspectRatioProperty.GetValue(); }
  set { _aspectRatioProperty.SetValue(value); }
}

public AbstractProperty FPSProperty
{
  get{ return _fPSProperty; }
}

public float? FPS
{
  get { return (float?) _fPSProperty.GetValue(); }
  set { _fPSProperty.SetValue(value); }
}

public AbstractProperty IsDVDProperty
{
  get{ return _isDVDProperty; }
}

public bool? IsDVD
{
  get { return (bool?) _isDVDProperty.GetValue(); }
  set { _isDVDProperty.SetValue(value); }
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

public VideoAspectWrapper()
{
  _resourceIndexProperty = new SProperty(typeof(int?));
  _streamIndexProperty = new SProperty(typeof(int?));
  _durationProperty = new SProperty(typeof(long?));
  _audioStreamCountProperty = new SProperty(typeof(int?));
  _videoEncodingProperty = new SProperty(typeof(string));
  _videoBitRateProperty = new SProperty(typeof(long?));
  _aspectWidthProperty = new SProperty(typeof(int?));
  _aspectHeightProperty = new SProperty(typeof(int?));
  _aspectRatioProperty = new SProperty(typeof(float?));
  _fPSProperty = new SProperty(typeof(float?));
  _isDVDProperty = new SProperty(typeof(bool?));
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
  IList<MultipleMediaItemAspect> aspects;
  if (mediaItem == null ||!MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoAspect.Metadata, out aspects))
  {
     SetEmpty();
     return;
  }

  ResourceIndex = (int?) aspects[0][VideoAspect.ATTR_RESOURCE_INDEX];
  StreamIndex = (int?) aspects[0][VideoAspect.ATTR_STREAM_INDEX];
  Duration = (long?) aspects[0][VideoAspect.ATTR_DURATION];
  AudioStreamCount = (int?) aspects[0][VideoAspect.ATTR_AUDIOSTREAMCOUNT];
  VideoEncoding = (string) aspects[0][VideoAspect.ATTR_VIDEOENCODING];
  VideoBitRate = (long?) aspects[0][VideoAspect.ATTR_VIDEOBITRATE];
  AspectWidth = (int?) aspects[0][VideoAspect.ATTR_WIDTH];
  AspectHeight = (int?) aspects[0][VideoAspect.ATTR_HEIGHT];
  AspectRatio = (float?) aspects[0][VideoAspect.ATTR_ASPECTRATIO];
  FPS = (float?) aspects[0][VideoAspect.ATTR_FPS];
  IsDVD = (bool?) aspects[0][VideoAspect.ATTR_ISDVD];
}

public void SetEmpty()
{
  ResourceIndex = null;
  StreamIndex = null;
  Duration = null;
  AudioStreamCount = null;
  VideoEncoding = null;
  VideoBitRate = null;
  AspectWidth = null;
  AspectHeight = null;
  AspectRatio = null;
  FPS = null;
  IsDVD = null;
}


#endregion

}

}

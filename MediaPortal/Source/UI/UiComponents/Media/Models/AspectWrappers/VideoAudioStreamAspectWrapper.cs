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
/// VideoAudioStreamAspectWrapper wraps the contents of <see cref="VideoAudioStreamAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class VideoAudioStreamAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _resourceIndexProperty;
protected AbstractProperty _streamIndexProperty;
protected AbstractProperty _audioEncodingProperty;
protected AbstractProperty _audioBitRateProperty;
protected AbstractProperty _audioChannelsProperty;
protected AbstractProperty _audioLanguageProperty;
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

public AbstractProperty AudioEncodingProperty
{
  get{ return _audioEncodingProperty; }
}

public string AudioEncoding
{
  get { return (string) _audioEncodingProperty.GetValue(); }
  set { _audioEncodingProperty.SetValue(value); }
}

public AbstractProperty AudioBitRateProperty
{
  get{ return _audioBitRateProperty; }
}

public long? AudioBitRate
{
  get { return (long?) _audioBitRateProperty.GetValue(); }
  set { _audioBitRateProperty.SetValue(value); }
}

public AbstractProperty AudioChannelsProperty
{
  get{ return _audioChannelsProperty; }
}

public int? AudioChannels
{
  get { return (int?) _audioChannelsProperty.GetValue(); }
  set { _audioChannelsProperty.SetValue(value); }
}

public AbstractProperty AudioLanguageProperty
{
  get{ return _audioLanguageProperty; }
}

public string AudioLanguage
{
  get { return (string) _audioLanguageProperty.GetValue(); }
  set { _audioLanguageProperty.SetValue(value); }
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

public VideoAudioStreamAspectWrapper()
{
  _resourceIndexProperty = new SProperty(typeof(int?));
  _streamIndexProperty = new SProperty(typeof(int?));
  _audioEncodingProperty = new SProperty(typeof(string));
  _audioBitRateProperty = new SProperty(typeof(long?));
  _audioChannelsProperty = new SProperty(typeof(int?));
  _audioLanguageProperty = new SProperty(typeof(string));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspects(mediaItem.Aspects, VideoAudioStreamAspect.Metadata, out aspects))
  {
     SetEmpty();
     return;
  }

  ResourceIndex = (int?) aspects[0][VideoAudioStreamAspect.ATTR_RESOURCE_INDEX];
  StreamIndex = (int?) aspects[0][VideoAudioStreamAspect.ATTR_STREAM_INDEX];
  AudioEncoding = (string) aspects[0][VideoAudioStreamAspect.ATTR_AUDIOENCODING];
  AudioBitRate = (long?) aspects[0][VideoAudioStreamAspect.ATTR_AUDIOBITRATE];
  AudioChannels = (int?) aspects[0][VideoAudioStreamAspect.ATTR_AUDIOCHANNELS];
  AudioLanguage = (string) aspects[0][VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE];
}

public void SetEmpty()
{
  ResourceIndex = null;
  StreamIndex = null;
  AudioEncoding = null;
  AudioBitRate = null;
  AudioChannels = null;
  AudioLanguage = null;
}


#endregion

}

}

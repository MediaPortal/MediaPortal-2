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
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UiComponents.Media.Models.AspectWrappers
{
/// <summary>
/// SubtitleAspectWrapper wraps the contents of <see cref="SubtitleAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class SubtitleAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _resourceIndexProperty;
protected AbstractProperty _videoResourceIndexProperty;
protected AbstractProperty _streamIndexProperty;
protected AbstractProperty _subtitleEncodingProperty;
protected AbstractProperty _subtitleFormatProperty;
protected AbstractProperty _isInternalProperty;
protected AbstractProperty _isDefaultProperty;
protected AbstractProperty _isForcedProperty;
protected AbstractProperty _subtitleLanguageProperty;
protected AbstractProperty _mediaItemProperty;
protected AbstractProperty _aspectIndexProperty;
protected AbstractProperty _aspectCountProperty;

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

public AbstractProperty VideoResourceIndexProperty
{
  get{ return _videoResourceIndexProperty; }
}

public int? VideoResourceIndex
{
  get { return (int?) _videoResourceIndexProperty.GetValue(); }
  set { _videoResourceIndexProperty.SetValue(value); }
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

public AbstractProperty SubtitleEncodingProperty
{
  get{ return _subtitleEncodingProperty; }
}

public string SubtitleEncoding
{
  get { return (string) _subtitleEncodingProperty.GetValue(); }
  set { _subtitleEncodingProperty.SetValue(value); }
}

public AbstractProperty SubtitleFormatProperty
{
  get{ return _subtitleFormatProperty; }
}

public string SubtitleFormat
{
  get { return (string) _subtitleFormatProperty.GetValue(); }
  set { _subtitleFormatProperty.SetValue(value); }
}

public AbstractProperty IsInternalProperty
{
  get{ return _isInternalProperty; }
}

public bool? IsInternal
{
  get { return (bool?) _isInternalProperty.GetValue(); }
  set { _isInternalProperty.SetValue(value); }
}

public AbstractProperty IsDefaultProperty
{
  get{ return _isDefaultProperty; }
}

public bool? IsDefault
{
  get { return (bool?) _isDefaultProperty.GetValue(); }
  set { _isDefaultProperty.SetValue(value); }
}

public AbstractProperty IsForcedProperty
{
  get{ return _isForcedProperty; }
}

public bool? IsForced
{
  get { return (bool?) _isForcedProperty.GetValue(); }
  set { _isForcedProperty.SetValue(value); }
}

public AbstractProperty SubtitleLanguageProperty
{
  get{ return _subtitleLanguageProperty; }
}

public string SubtitleLanguage
{
  get { return (string) _subtitleLanguageProperty.GetValue(); }
  set { _subtitleLanguageProperty.SetValue(value); }
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

public AbstractProperty AspectIndexProperty
{
  get{ return _aspectIndexProperty; }
}

public int AspectIndex
{
  get { return (int) _aspectIndexProperty.GetValue(); }
  set { _aspectIndexProperty.SetValue(value); }
}

public AbstractProperty AspectCountProperty
{
  get{ return _aspectCountProperty; }
}

public int AspectCount
{
  get { return (int) _aspectCountProperty.GetValue(); }
  set { _aspectCountProperty.SetValue(value); }
}

#endregion

#region Constructor

public SubtitleAspectWrapper()
{
  _resourceIndexProperty = new SProperty(typeof(int?));
  _videoResourceIndexProperty = new SProperty(typeof(int?));
  _streamIndexProperty = new SProperty(typeof(int?));
  _subtitleEncodingProperty = new SProperty(typeof(string));
  _subtitleFormatProperty = new SProperty(typeof(string));
  _isInternalProperty = new SProperty(typeof(bool?));
  _isDefaultProperty = new SProperty(typeof(bool?));
  _isForcedProperty = new SProperty(typeof(bool?));
  _subtitleLanguageProperty = new SProperty(typeof(string));
  _mediaItemProperty = new SProperty(typeof(MediaItem));
  _mediaItemProperty.Attach(MediaItemChanged);
  _aspectIndexProperty = new SProperty(typeof(int));
  _aspectIndexProperty.Attach(AspectIndexChanged);
  _aspectCountProperty = new SProperty(typeof(int));
}

#endregion

#region Members

private void MediaItemChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

private void AspectIndexChanged(AbstractProperty property, object oldvalue)
{
  Init(MediaItem);
}

public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
{
  Detach();
  base.DeepCopy(source, copyManager);
  var aw = (SubtitleAspectWrapper)source;
  AspectIndex = aw.AspectIndex;
  Attach();
}

private void Attach()
{
  _aspectIndexProperty.Attach(AspectIndexChanged);
}

private void Detach()
{
  _aspectIndexProperty.Detach(AspectIndexChanged);
}

public void Init(MediaItem mediaItem)
{
  IList<MultipleMediaItemAspect> aspects;
  if (mediaItem == null || !MediaItemAspect.TryGetAspects(mediaItem.Aspects, SubtitleAspect.Metadata, out aspects) ||
      AspectIndex < 0 || AspectIndex >= aspects.Count)
  {
     SetEmpty();
     return;
  }

  AspectCount = aspects.Count;
  ResourceIndex = (int?) aspects[AspectIndex][SubtitleAspect.ATTR_RESOURCE_INDEX];
  VideoResourceIndex = (int?) aspects[AspectIndex][SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX];
  StreamIndex = (int?) aspects[AspectIndex][SubtitleAspect.ATTR_STREAM_INDEX];
  SubtitleEncoding = (string) aspects[AspectIndex][SubtitleAspect.ATTR_SUBTITLE_ENCODING];
  SubtitleFormat = (string) aspects[AspectIndex][SubtitleAspect.ATTR_SUBTITLE_FORMAT];
  IsInternal = (bool?) aspects[AspectIndex][SubtitleAspect.ATTR_INTERNAL];
  IsDefault = (bool?) aspects[AspectIndex][SubtitleAspect.ATTR_DEFAULT];
  IsForced = (bool?) aspects[AspectIndex][SubtitleAspect.ATTR_FORCED];
  SubtitleLanguage = (string) aspects[AspectIndex][SubtitleAspect.ATTR_SUBTITLE_LANGUAGE];
}

public void SetEmpty()
{
  AspectCount = 0;
  ResourceIndex = null;
  VideoResourceIndex = null;
  StreamIndex = null;
  SubtitleEncoding = null;
  SubtitleFormat = null;
  IsInternal = null;
  IsDefault = null;
  IsForced = null;
  SubtitleLanguage = null;
}

#endregion

}

}

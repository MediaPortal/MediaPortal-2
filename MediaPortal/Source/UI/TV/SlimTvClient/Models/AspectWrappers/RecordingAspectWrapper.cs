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
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.Plugins.SlimTv.Client.Models.AspectWrappers
{
/// <summary>
/// RecordingAspectWrapper wraps the contents of <see cref="RecordingAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class RecordingAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _channelProperty;
protected AbstractProperty _startTimeProperty;
protected AbstractProperty _endTimeProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty ChannelProperty
{
  get{ return _channelProperty; }
}

public string Channel
{
  get { return (string) _channelProperty.GetValue(); }
  set { _channelProperty.SetValue(value); }
}

public AbstractProperty StartTimeProperty
{
  get{ return _startTimeProperty; }
}

public DateTime? StartTime
{
  get { return (DateTime?) _startTimeProperty.GetValue(); }
  set { _startTimeProperty.SetValue(value); }
}

public AbstractProperty EndTimeProperty
{
  get{ return _endTimeProperty; }
}

public DateTime? EndTime
{
  get { return (DateTime?) _endTimeProperty.GetValue(); }
  set { _endTimeProperty.SetValue(value); }
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

public RecordingAspectWrapper()
{
  _channelProperty = new SProperty(typeof(string));
  _startTimeProperty = new SProperty(typeof(DateTime?));
  _endTimeProperty = new SProperty(typeof(DateTime?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, RecordingAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  Channel = (string) aspect[RecordingAspect.ATTR_CHANNEL];
  StartTime = (DateTime?) aspect[RecordingAspect.ATTR_STARTTIME];
  EndTime = (DateTime?) aspect[RecordingAspect.ATTR_ENDTIME];
}

public void SetEmpty()
{
  Channel = null;
  StartTime = null;
  EndTime = null;
}

#endregion

}

}

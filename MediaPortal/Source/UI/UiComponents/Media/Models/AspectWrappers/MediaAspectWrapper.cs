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
/// MediaAspectWrapper wraps the contents of <see cref="MediaAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class MediaAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _titleProperty;
protected AbstractProperty _sortTitleProperty;
protected AbstractProperty _recordingTimeProperty;
protected AbstractProperty _ratingProperty;
protected AbstractProperty _commentProperty;
protected AbstractProperty _playCountProperty;
protected AbstractProperty _lastPlayedProperty;
protected AbstractProperty _isVirtualProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty TitleProperty
{
  get{ return _titleProperty; }
}

public string Title
{
  get { return (string) _titleProperty.GetValue(); }
  set { _titleProperty.SetValue(value); }
}

public AbstractProperty SortTitleProperty
{
  get{ return _sortTitleProperty; }
}

public string SortTitle
{
  get { return (string) _sortTitleProperty.GetValue(); }
  set { _sortTitleProperty.SetValue(value); }
}

public AbstractProperty RecordingTimeProperty
{
  get{ return _recordingTimeProperty; }
}

public DateTime? RecordingTime
{
  get { return (DateTime?) _recordingTimeProperty.GetValue(); }
  set { _recordingTimeProperty.SetValue(value); }
}

public AbstractProperty RatingProperty
{
  get{ return _ratingProperty; }
}

public int? Rating
{
  get { return (int?) _ratingProperty.GetValue(); }
  set { _ratingProperty.SetValue(value); }
}

public AbstractProperty CommentProperty
{
  get{ return _commentProperty; }
}

public string Comment
{
  get { return (string) _commentProperty.GetValue(); }
  set { _commentProperty.SetValue(value); }
}

public AbstractProperty PlayCountProperty
{
  get{ return _playCountProperty; }
}

public int? PlayCount
{
  get { return (int?) _playCountProperty.GetValue(); }
  set { _playCountProperty.SetValue(value); }
}

public AbstractProperty LastPlayedProperty
{
  get{ return _lastPlayedProperty; }
}

public DateTime? LastPlayed
{
  get { return (DateTime?) _lastPlayedProperty.GetValue(); }
  set { _lastPlayedProperty.SetValue(value); }
}

public AbstractProperty IsVirtualProperty
{
  get{ return _isVirtualProperty; }
}

public bool? IsVirtual
{
  get { return (bool?) _isVirtualProperty.GetValue(); }
  set { _isVirtualProperty.SetValue(value); }
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

public MediaAspectWrapper()
{
  _titleProperty = new SProperty(typeof(string));
  _sortTitleProperty = new SProperty(typeof(string));
  _recordingTimeProperty = new SProperty(typeof(DateTime?));
  _ratingProperty = new SProperty(typeof(int?));
  _commentProperty = new SProperty(typeof(string));
  _playCountProperty = new SProperty(typeof(int?));
  _lastPlayedProperty = new SProperty(typeof(DateTime?));
  _isVirtualProperty = new SProperty(typeof(bool?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, MediaAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  Title = (string) aspect[MediaAspect.ATTR_TITLE];
  SortTitle = (string) aspect[MediaAspect.ATTR_SORT_TITLE];
  RecordingTime = (DateTime?) aspect[MediaAspect.ATTR_RECORDINGTIME];
  Rating = (int?) aspect[MediaAspect.ATTR_RATING];
  Comment = (string) aspect[MediaAspect.ATTR_COMMENT];
  PlayCount = (int?) aspect[MediaAspect.ATTR_PLAYCOUNT];
  LastPlayed = (DateTime?) aspect[MediaAspect.ATTR_LASTPLAYED];
  IsVirtual = (bool?) aspect[MediaAspect.ATTR_ISVIRTUAL];
}

public void SetEmpty()
{
  Title = null;
  SortTitle = null;
  RecordingTime = null;
  Rating = null;
  Comment = null;
  PlayCount = null;
  LastPlayed = null;
  IsVirtual = null;
}

#endregion

}

}

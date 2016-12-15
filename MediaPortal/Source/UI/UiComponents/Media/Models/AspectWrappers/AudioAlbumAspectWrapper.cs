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
/// AudioAlbumAspectWrapper wraps the contents of <see cref="AudioAlbumAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class AudioAlbumAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _albumProperty;
protected AbstractProperty _descriptionProperty;
protected AbstractProperty _artistsProperty;
protected AbstractProperty _labelsProperty;
protected AbstractProperty _awardsProperty;
protected AbstractProperty _isCompilationProperty;
protected AbstractProperty _numTracksProperty;
protected AbstractProperty _discIdProperty;
protected AbstractProperty _numDiscsProperty;
protected AbstractProperty _salesProperty;
protected AbstractProperty _totalRatingProperty;
protected AbstractProperty _ratingCountProperty;
protected AbstractProperty _availTracksProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty AlbumProperty
{
  get{ return _albumProperty; }
}

public string Album
{
  get { return (string) _albumProperty.GetValue(); }
  set { _albumProperty.SetValue(value); }
}

public AbstractProperty DescriptionProperty
{
  get{ return _descriptionProperty; }
}

public string Description
{
  get { return (string) _descriptionProperty.GetValue(); }
  set { _descriptionProperty.SetValue(value); }
}

public AbstractProperty ArtistsProperty
{
  get{ return _artistsProperty; }
}

public IEnumerable<string> Artists
{
  get { return (IEnumerable<string>) _artistsProperty.GetValue(); }
  set { _artistsProperty.SetValue(value); }
}

public AbstractProperty LabelsProperty
{
  get{ return _labelsProperty; }
}

public IEnumerable<string> Labels
{
  get { return (IEnumerable<string>) _labelsProperty.GetValue(); }
  set { _labelsProperty.SetValue(value); }
}

public AbstractProperty AwardsProperty
{
  get{ return _awardsProperty; }
}

public IEnumerable<string> Awards
{
  get { return (IEnumerable<string>) _awardsProperty.GetValue(); }
  set { _awardsProperty.SetValue(value); }
}

public AbstractProperty IsCompilationProperty
{
  get{ return _isCompilationProperty; }
}

public bool? IsCompilation
{
  get { return (bool?) _isCompilationProperty.GetValue(); }
  set { _isCompilationProperty.SetValue(value); }
}

public AbstractProperty NumTracksProperty
{
  get{ return _numTracksProperty; }
}

public int? NumTracks
{
  get { return (int?) _numTracksProperty.GetValue(); }
  set { _numTracksProperty.SetValue(value); }
}

public AbstractProperty DiscIdProperty
{
  get{ return _discIdProperty; }
}

public int? DiscId
{
  get { return (int?) _discIdProperty.GetValue(); }
  set { _discIdProperty.SetValue(value); }
}

public AbstractProperty NumDiscsProperty
{
  get{ return _numDiscsProperty; }
}

public int? NumDiscs
{
  get { return (int?) _numDiscsProperty.GetValue(); }
  set { _numDiscsProperty.SetValue(value); }
}

public AbstractProperty SalesProperty
{
  get{ return _salesProperty; }
}

public long? Sales
{
  get { return (long?) _salesProperty.GetValue(); }
  set { _salesProperty.SetValue(value); }
}

public AbstractProperty TotalRatingProperty
{
  get{ return _totalRatingProperty; }
}

public double? TotalRating
{
  get { return (double?) _totalRatingProperty.GetValue(); }
  set { _totalRatingProperty.SetValue(value); }
}

public AbstractProperty RatingCountProperty
{
  get{ return _ratingCountProperty; }
}

public int? RatingCount
{
  get { return (int?) _ratingCountProperty.GetValue(); }
  set { _ratingCountProperty.SetValue(value); }
}

public AbstractProperty AvailTracksProperty
{
  get{ return _availTracksProperty; }
}

public int? AvailTracks
{
  get { return (int?) _availTracksProperty.GetValue(); }
  set { _availTracksProperty.SetValue(value); }
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

public AudioAlbumAspectWrapper()
{
  _albumProperty = new SProperty(typeof(string));
  _descriptionProperty = new SProperty(typeof(string));
  _artistsProperty = new SProperty(typeof(IEnumerable<string>));
  _labelsProperty = new SProperty(typeof(IEnumerable<string>));
  _awardsProperty = new SProperty(typeof(IEnumerable<string>));
  _isCompilationProperty = new SProperty(typeof(bool?));
  _numTracksProperty = new SProperty(typeof(int?));
  _discIdProperty = new SProperty(typeof(int?));
  _numDiscsProperty = new SProperty(typeof(int?));
  _salesProperty = new SProperty(typeof(long?));
  _totalRatingProperty = new SProperty(typeof(double?));
  _ratingCountProperty = new SProperty(typeof(int?));
  _availTracksProperty = new SProperty(typeof(int?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, AudioAlbumAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  Album = (string) aspect[AudioAlbumAspect.ATTR_ALBUM];
  Description = (string) aspect[AudioAlbumAspect.ATTR_DESCRIPTION];
  Artists = (IEnumerable<string>) aspect[AudioAlbumAspect.ATTR_ARTISTS] ?? EMPTY_STRING_COLLECTION;
  Labels = (IEnumerable<string>) aspect[AudioAlbumAspect.ATTR_LABELS] ?? EMPTY_STRING_COLLECTION;
  Awards = (IEnumerable<string>) aspect[AudioAlbumAspect.ATTR_AWARDS] ?? EMPTY_STRING_COLLECTION;
  IsCompilation = (bool?) aspect[AudioAlbumAspect.ATTR_COMPILATION];
  NumTracks = (int?) aspect[AudioAlbumAspect.ATTR_NUMTRACKS];
  DiscId = (int?) aspect[AudioAlbumAspect.ATTR_DISCID];
  NumDiscs = (int?) aspect[AudioAlbumAspect.ATTR_NUMDISCS];
  Sales = (long?) aspect[AudioAlbumAspect.ATTR_SALES];
  TotalRating = (double?) aspect[AudioAlbumAspect.ATTR_TOTAL_RATING];
  RatingCount = (int?) aspect[AudioAlbumAspect.ATTR_RATING_COUNT];
  AvailTracks = (int?) aspect[AudioAlbumAspect.ATTR_AVAILABLE_TRACKS];
}

public void SetEmpty()
{
  Album = null;
  Description = null;
  Artists = EMPTY_STRING_COLLECTION;
  Labels = EMPTY_STRING_COLLECTION;
  Awards = EMPTY_STRING_COLLECTION;
  IsCompilation = null;
  NumTracks = null;
  DiscId = null;
  NumDiscs = null;
  Sales = null;
  TotalRating = null;
  RatingCount = null;
  AvailTracks = null;
}

#endregion

}

}

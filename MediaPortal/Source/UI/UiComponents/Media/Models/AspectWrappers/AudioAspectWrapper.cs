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
/// AudioAspectWrapper wraps the contents of <see cref="AudioAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class AudioAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _trackNameProperty;
protected AbstractProperty _artistsProperty;
protected AbstractProperty _albumProperty;
protected AbstractProperty _isCompilationProperty;
protected AbstractProperty _durationProperty;
protected AbstractProperty _lyricsProperty;
protected AbstractProperty _isCDProperty;
protected AbstractProperty _trackProperty;
protected AbstractProperty _numTracksProperty;
protected AbstractProperty _albumArtistsProperty;
protected AbstractProperty _composersProperty;
protected AbstractProperty _encodingProperty;
protected AbstractProperty _bitRateProperty;
protected AbstractProperty _channelsProperty;
protected AbstractProperty _sampleRateProperty;
protected AbstractProperty _discIdProperty;
protected AbstractProperty _numDiscsProperty;
protected AbstractProperty _totalRatingProperty;
protected AbstractProperty _ratingCountProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty TrackNameProperty
{
  get{ return _trackNameProperty; }
}

public string TrackName
{
  get { return (string) _trackNameProperty.GetValue(); }
  set { _trackNameProperty.SetValue(value); }
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

public AbstractProperty AlbumProperty
{
  get{ return _albumProperty; }
}

public string Album
{
  get { return (string) _albumProperty.GetValue(); }
  set { _albumProperty.SetValue(value); }
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

public AbstractProperty DurationProperty
{
  get{ return _durationProperty; }
}

public long? Duration
{
  get { return (long?) _durationProperty.GetValue(); }
  set { _durationProperty.SetValue(value); }
}

public AbstractProperty LyricsProperty
{
  get{ return _lyricsProperty; }
}

public string Lyrics
{
  get { return (string) _lyricsProperty.GetValue(); }
  set { _lyricsProperty.SetValue(value); }
}

public AbstractProperty IsCDProperty
{
  get{ return _isCDProperty; }
}

public bool? IsCD
{
  get { return (bool?) _isCDProperty.GetValue(); }
  set { _isCDProperty.SetValue(value); }
}

public AbstractProperty TrackProperty
{
  get{ return _trackProperty; }
}

public int? Track
{
  get { return (int?) _trackProperty.GetValue(); }
  set { _trackProperty.SetValue(value); }
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

public AbstractProperty AlbumArtistsProperty
{
  get{ return _albumArtistsProperty; }
}

public IEnumerable<string> AlbumArtists
{
  get { return (IEnumerable<string>) _albumArtistsProperty.GetValue(); }
  set { _albumArtistsProperty.SetValue(value); }
}

public AbstractProperty ComposersProperty
{
  get{ return _composersProperty; }
}

public IEnumerable<string> Composers
{
  get { return (IEnumerable<string>) _composersProperty.GetValue(); }
  set { _composersProperty.SetValue(value); }
}

public AbstractProperty EncodingProperty
{
  get{ return _encodingProperty; }
}

public string Encoding
{
  get { return (string) _encodingProperty.GetValue(); }
  set { _encodingProperty.SetValue(value); }
}

public AbstractProperty BitRateProperty
{
  get{ return _bitRateProperty; }
}

public int? BitRate
{
  get { return (int?) _bitRateProperty.GetValue(); }
  set { _bitRateProperty.SetValue(value); }
}

public AbstractProperty ChannelsProperty
{
  get{ return _channelsProperty; }
}

public int? Channels
{
  get { return (int?) _channelsProperty.GetValue(); }
  set { _channelsProperty.SetValue(value); }
}

public AbstractProperty SampleRateProperty
{
  get{ return _sampleRateProperty; }
}

public long? SampleRate
{
  get { return (long?) _sampleRateProperty.GetValue(); }
  set { _sampleRateProperty.SetValue(value); }
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

public AudioAspectWrapper()
{
  _trackNameProperty = new SProperty(typeof(string));
  _artistsProperty = new SProperty(typeof(IEnumerable<string>));
  _albumProperty = new SProperty(typeof(string));
  _isCompilationProperty = new SProperty(typeof(bool?));
  _durationProperty = new SProperty(typeof(long?));
  _lyricsProperty = new SProperty(typeof(string));
  _isCDProperty = new SProperty(typeof(bool?));
  _trackProperty = new SProperty(typeof(int?));
  _numTracksProperty = new SProperty(typeof(int?));
  _albumArtistsProperty = new SProperty(typeof(IEnumerable<string>));
  _composersProperty = new SProperty(typeof(IEnumerable<string>));
  _encodingProperty = new SProperty(typeof(string));
  _bitRateProperty = new SProperty(typeof(int?));
  _channelsProperty = new SProperty(typeof(int?));
  _sampleRateProperty = new SProperty(typeof(long?));
  _discIdProperty = new SProperty(typeof(int?));
  _numDiscsProperty = new SProperty(typeof(int?));
  _totalRatingProperty = new SProperty(typeof(double?));
  _ratingCountProperty = new SProperty(typeof(int?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, AudioAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  TrackName = (string) aspect[AudioAspect.ATTR_TRACKNAME];
  Artists = (IEnumerable<string>) aspect[AudioAspect.ATTR_ARTISTS] ?? EMPTY_STRING_COLLECTION;
  Album = (string) aspect[AudioAspect.ATTR_ALBUM];
  IsCompilation = (bool?) aspect[AudioAspect.ATTR_COMPILATION];
  Duration = (long?) aspect[AudioAspect.ATTR_DURATION];
  Lyrics = (string) aspect[AudioAspect.ATTR_LYRICS];
  IsCD = (bool?) aspect[AudioAspect.ATTR_ISCD];
  Track = (int?) aspect[AudioAspect.ATTR_TRACK];
  NumTracks = (int?) aspect[AudioAspect.ATTR_NUMTRACKS];
  AlbumArtists = (IEnumerable<string>) aspect[AudioAspect.ATTR_ALBUMARTISTS] ?? EMPTY_STRING_COLLECTION;
  Composers = (IEnumerable<string>) aspect[AudioAspect.ATTR_COMPOSERS] ?? EMPTY_STRING_COLLECTION;
  Encoding = (string) aspect[AudioAspect.ATTR_ENCODING];
  BitRate = (int?) aspect[AudioAspect.ATTR_BITRATE];
  SampleRate = (long?) aspect[AudioAspect.ATTR_SAMPLERATE];
  Channels = (int?) aspect[AudioAspect.ATTR_CHANNELS];
  DiscId = (int?) aspect[AudioAspect.ATTR_DISCID];
  NumDiscs = (int?) aspect[AudioAspect.ATTR_NUMDISCS];
  TotalRating = (double?) aspect[AudioAspect.ATTR_TOTAL_RATING];
  RatingCount = (int?) aspect[AudioAspect.ATTR_RATING_COUNT];
}

public void SetEmpty()
{
  TrackName = null;
  Artists = EMPTY_STRING_COLLECTION;
  Album = null;
  IsCompilation = null;
  Duration = null;
  Lyrics = null;
  IsCD = null;
  Track = null;
  NumTracks = null;
  AlbumArtists = EMPTY_STRING_COLLECTION;
  Composers = EMPTY_STRING_COLLECTION;
  Encoding = null;
  BitRate = null;
  SampleRate = null;
  Channels = null;
  DiscId = null;
  NumDiscs = null;
  TotalRating = null;
  RatingCount = null;
}

#endregion

}

}

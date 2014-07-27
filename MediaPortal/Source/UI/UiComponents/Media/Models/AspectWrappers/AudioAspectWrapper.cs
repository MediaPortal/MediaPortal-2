#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
  public class AudioAspectWrapper : Control
  {
    #region Constants

    public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

    #endregion

    #region Fields

    protected AbstractProperty _artistsProperty;
    protected AbstractProperty _albumProperty;
    protected AbstractProperty _genresProperty;
    protected AbstractProperty _durationProperty;
    protected AbstractProperty _trackProperty;
    protected AbstractProperty _numTracksProperty;
    protected AbstractProperty _albumArtistsProperty;
    protected AbstractProperty _composersProperty;
    protected AbstractProperty _encodingProperty;
    protected AbstractProperty _bitRateProperty;
    protected AbstractProperty _discIdProperty;
    protected AbstractProperty _numDiscsProperty;
    protected AbstractProperty _mediaItemProperty;

    #endregion

    #region Properties

    public AbstractProperty ArtistsProperty
    {
      get { return _artistsProperty; }
    }

    public IEnumerable<string> Artists
    {
      get { return (IEnumerable<string>)_artistsProperty.GetValue(); }
      set { _artistsProperty.SetValue(value); }
    }

    public AbstractProperty AlbumProperty
    {
      get { return _albumProperty; }
    }

    public string Album
    {
      get { return (string)_albumProperty.GetValue(); }
      set { _albumProperty.SetValue(value); }
    }

    public AbstractProperty GenresProperty
    {
      get { return _genresProperty; }
    }

    public IEnumerable<string> Genres
    {
      get { return (IEnumerable<string>)_genresProperty.GetValue(); }
      set { _genresProperty.SetValue(value); }
    }

    public AbstractProperty DurationProperty
    {
      get { return _durationProperty; }
    }

    public long? Duration
    {
      get { return (long?)_durationProperty.GetValue(); }
      set { _durationProperty.SetValue(value); }
    }

    public AbstractProperty TrackProperty
    {
      get { return _trackProperty; }
    }

    public int? Track
    {
      get { return (int?)_trackProperty.GetValue(); }
      set { _trackProperty.SetValue(value); }
    }

    public AbstractProperty NumTracksProperty
    {
      get { return _numTracksProperty; }
    }

    public int? NumTracks
    {
      get { return (int?)_numTracksProperty.GetValue(); }
      set { _numTracksProperty.SetValue(value); }
    }

    public AbstractProperty AlbumArtistsProperty
    {
      get { return _albumArtistsProperty; }
    }

    public IEnumerable<string> AlbumArtists
    {
      get { return (IEnumerable<string>)_albumArtistsProperty.GetValue(); }
      set { _albumArtistsProperty.SetValue(value); }
    }

    public AbstractProperty ComposersProperty
    {
      get { return _composersProperty; }
    }

    public IEnumerable<string> Composers
    {
      get { return (IEnumerable<string>)_composersProperty.GetValue(); }
      set { _composersProperty.SetValue(value); }
    }

    public AbstractProperty EncodingProperty
    {
      get { return _encodingProperty; }
    }

    public string Encoding
    {
      get { return (string)_encodingProperty.GetValue(); }
      set { _encodingProperty.SetValue(value); }
    }

    public AbstractProperty BitRateProperty
    {
      get { return _bitRateProperty; }
    }

    public int? BitRate
    {
      get { return (int?)_bitRateProperty.GetValue(); }
      set { _bitRateProperty.SetValue(value); }
    }

    public AbstractProperty DiscIdProperty
    {
      get { return _discIdProperty; }
    }

    public int? DiscId
    {
      get { return (int?)_discIdProperty.GetValue(); }
      set { _discIdProperty.SetValue(value); }
    }

    public AbstractProperty NumDiscsProperty
    {
      get { return _numDiscsProperty; }
    }

    public int? NumDiscs
    {
      get { return (int?)_numDiscsProperty.GetValue(); }
      set { _numDiscsProperty.SetValue(value); }
    }

    public AbstractProperty MediaItemProperty
    {
      get { return _mediaItemProperty; }
    }

    public MediaItem MediaItem
    {
      get { return (MediaItem)_mediaItemProperty.GetValue(); }
      set { _mediaItemProperty.SetValue(value); }
    }

    #endregion

    #region Constructor

    public AudioAspectWrapper()
    {
      _artistsProperty = new SProperty(typeof(IEnumerable<string>));
      _albumProperty = new SProperty(typeof(string));
      _genresProperty = new SProperty(typeof(IEnumerable<string>));
      _durationProperty = new SProperty(typeof(long?));
      _trackProperty = new SProperty(typeof(int?));
      _numTracksProperty = new SProperty(typeof(int?));
      _albumArtistsProperty = new SProperty(typeof(IEnumerable<string>));
      _composersProperty = new SProperty(typeof(IEnumerable<string>));
      _encodingProperty = new SProperty(typeof(string));
      _bitRateProperty = new SProperty(typeof(int?));
      _discIdProperty = new SProperty(typeof(int?));
      _numDiscsProperty = new SProperty(typeof(int?));
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
      MediaItemAspect aspect;
      if (mediaItem == null || !mediaItem.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out aspect))
      {
        SetEmpty();
        return;
      }

      Artists = (IEnumerable<string>)aspect[AudioAspect.ATTR_ARTISTS] ?? EMPTY_STRING_COLLECTION;
      Album = (string)aspect[AudioAspect.ATTR_ALBUM];
      Genres = (IEnumerable<string>)aspect[AudioAspect.ATTR_GENRES] ?? EMPTY_STRING_COLLECTION;
      Duration = (long?)aspect[AudioAspect.ATTR_DURATION];
      Track = (int?)aspect[AudioAspect.ATTR_TRACK];
      NumTracks = (int?)aspect[AudioAspect.ATTR_NUMTRACKS];
      AlbumArtists = (IEnumerable<string>)aspect[AudioAspect.ATTR_ALBUMARTISTS] ?? EMPTY_STRING_COLLECTION;
      Composers = (IEnumerable<string>)aspect[AudioAspect.ATTR_COMPOSERS] ?? EMPTY_STRING_COLLECTION;
      Encoding = (string)aspect[AudioAspect.ATTR_ENCODING];
      BitRate = (int?)aspect[AudioAspect.ATTR_BITRATE];
      DiscId = (int?)aspect[AudioAspect.ATTR_DISCID];
      NumDiscs = (int?)aspect[AudioAspect.ATTR_NUMDISCS];
    }

    public void SetEmpty()
    {
      Artists = EMPTY_STRING_COLLECTION;
      Album = null;
      Genres = EMPTY_STRING_COLLECTION;
      Duration = null;
      Track = null;
      NumTracks = null;
      AlbumArtists = EMPTY_STRING_COLLECTION;
      Composers = EMPTY_STRING_COLLECTION;
      Encoding = null;
      BitRate = null;
      DiscId = null;
      NumDiscs = null;
    }


    #endregion

  }
}

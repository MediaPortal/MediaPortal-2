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
/// AudioAlbumAspectWrapper wraps the contents of <see cref="AudioAlbumAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class AudioAlbumAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _descriptionProperty;
protected AbstractProperty _artistsProperty;
protected AbstractProperty _genresProperty;
protected AbstractProperty _numTracksProperty;
protected AbstractProperty _discIdProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

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

public AbstractProperty GenresProperty
{
  get{ return _genresProperty; }
}

public IEnumerable<string> Genres
{
  get { return (IEnumerable<string>) _genresProperty.GetValue(); }
  set { _genresProperty.SetValue(value); }
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
  _descriptionProperty = new SProperty(typeof(string));
  _artistsProperty = new SProperty(typeof(IEnumerable<string>));
  _genresProperty = new SProperty(typeof(IEnumerable<string>));
  _numTracksProperty = new SProperty(typeof(int?));
  _discIdProperty = new SProperty(typeof(int?));
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

  Description = (string) aspect[AudioAlbumAspect.ATTR_DESCRIPTION];
  Artists = (IEnumerable<string>) aspect[AudioAlbumAspect.ATTR_ARTISTS] ?? EMPTY_STRING_COLLECTION;
  Genres = (IEnumerable<string>) aspect[AudioAlbumAspect.ATTR_GENRES] ?? EMPTY_STRING_COLLECTION;
  NumTracks = (int?) aspect[AudioAlbumAspect.ATTR_NUMTRACKS];
  DiscId = (int?) aspect[AudioAlbumAspect.ATTR_DISCID];
}

public void SetEmpty()
{
  Description = null;
  Artists = EMPTY_STRING_COLLECTION;
  Genres = EMPTY_STRING_COLLECTION;
  NumTracks = null;
  DiscId = null;
}


#endregion

}

}

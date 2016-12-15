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
/// MovieCollectionAspectWrapper wraps the contents of <see cref="MovieCollectionAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class MovieCollectionAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _collectionNameProperty;
protected AbstractProperty _availMoviesProperty;
protected AbstractProperty _numMoviesProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty CollectionNameProperty
{
  get{ return _collectionNameProperty; }
}

public string CollectionName
{
  get { return (string) _collectionNameProperty.GetValue(); }
  set { _collectionNameProperty.SetValue(value); }
}

public AbstractProperty AvailMoviesProperty
{
  get{ return _availMoviesProperty; }
}

public int? AvailMovies
{
  get { return (int?) _availMoviesProperty.GetValue(); }
  set { _availMoviesProperty.SetValue(value); }
}

public AbstractProperty NumMoviesProperty
{
  get{ return _numMoviesProperty; }
}

public int? NumMovies
{
  get { return (int?) _numMoviesProperty.GetValue(); }
  set { _numMoviesProperty.SetValue(value); }
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

public MovieCollectionAspectWrapper()
{
  _collectionNameProperty = new SProperty(typeof(string));
  _availMoviesProperty = new SProperty(typeof(int?));
  _numMoviesProperty = new SProperty(typeof(int?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, MovieCollectionAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  CollectionName = (string) aspect[MovieCollectionAspect.ATTR_COLLECTION_NAME];
  AvailMovies = (int?) aspect[MovieCollectionAspect.ATTR_AVAILABLE_MOVIES];
  NumMovies = (int?) aspect[MovieCollectionAspect.ATTR_NUM_MOVIES];
}

public void SetEmpty()
{
  CollectionName = null;
  AvailMovies = null;
  NumMovies = null;
}

#endregion

}

}

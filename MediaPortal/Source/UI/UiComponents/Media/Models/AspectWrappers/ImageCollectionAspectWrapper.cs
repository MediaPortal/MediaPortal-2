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
/// ImageCollectionAspectWrapper wraps the contents of <see cref="ImageCollectionAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class ImageCollectionAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _collectionNameProperty;
protected AbstractProperty _collectionDateProperty;
protected AbstractProperty _collectionTypeProperty;
protected AbstractProperty _latitudeProperty;
protected AbstractProperty _longitudeProperty;
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

public AbstractProperty CollectionDateProperty
{
  get{ return _collectionDateProperty; }
}

public DateTime? CollectionDate
{
  get { return (DateTime?) _collectionDateProperty.GetValue(); }
  set { _collectionDateProperty.SetValue(value); }
}

public AbstractProperty CollectionTypeProperty
{
  get{ return _collectionTypeProperty; }
}

public string CollectionType
{
  get { return (string) _collectionTypeProperty.GetValue(); }
  set { _collectionTypeProperty.SetValue(value); }
}

public AbstractProperty LatitudeProperty
{
  get{ return _latitudeProperty; }
}

public double? Latitude
{
  get { return (double?) _latitudeProperty.GetValue(); }
  set { _latitudeProperty.SetValue(value); }
}

public AbstractProperty LongitudeProperty
{
  get{ return _longitudeProperty; }
}

public double? Longitude
{
  get { return (double?) _longitudeProperty.GetValue(); }
  set { _longitudeProperty.SetValue(value); }
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

public ImageCollectionAspectWrapper()
{
  _collectionNameProperty = new SProperty(typeof(string));
  _collectionDateProperty = new SProperty(typeof(DateTime?));
  _collectionTypeProperty = new SProperty(typeof(string));
  _latitudeProperty = new SProperty(typeof(double?));
  _longitudeProperty = new SProperty(typeof(double?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, ImageCollectionAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  CollectionName = (string) aspect[ImageCollectionAspect.ATTR_COLLECTION_NAME];
  CollectionDate = (DateTime?) aspect[ImageCollectionAspect.ATTR_COLLECTION_DATE];
  CollectionType = (string) aspect[ImageCollectionAspect.ATTR_COLLECTION_TYPE];
  Latitude = (double?) aspect[ImageCollectionAspect.ATTR_LATITUDE];
  Longitude = (double?) aspect[ImageCollectionAspect.ATTR_LONGITUDE];
}

public void SetEmpty()
{
  CollectionName = null;
  CollectionDate = null;
  CollectionType = null;
  Latitude = null;
  Longitude = null;
}


#endregion

}

}

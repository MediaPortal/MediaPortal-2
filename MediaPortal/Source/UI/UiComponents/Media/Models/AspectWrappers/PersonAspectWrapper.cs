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
/// PersonAspectWrapper wraps the contents of <see cref="PersonAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class PersonAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _personNameProperty;
protected AbstractProperty _occupationProperty;
protected AbstractProperty _biographyProperty;
protected AbstractProperty _originProperty;
protected AbstractProperty _bornDateProperty;
protected AbstractProperty _deathDateProperty;
protected AbstractProperty _isGroupProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty PersonNameProperty
{
  get{ return _personNameProperty; }
}

public string PersonName
{
  get { return (string) _personNameProperty.GetValue(); }
  set { _personNameProperty.SetValue(value); }
}

public AbstractProperty OccupationProperty
{
  get{ return _occupationProperty; }
}

public string Occupation
{
  get { return (string) _occupationProperty.GetValue(); }
  set { _occupationProperty.SetValue(value); }
}

public AbstractProperty BiographyProperty
{
  get{ return _biographyProperty; }
}

public string Biography
{
  get { return (string) _biographyProperty.GetValue(); }
  set { _biographyProperty.SetValue(value); }
}

public AbstractProperty OriginProperty
{
  get{ return _originProperty; }
}

public string Origin
{
  get { return (string) _originProperty.GetValue(); }
  set { _originProperty.SetValue(value); }
}

public AbstractProperty BornDateProperty
{
  get{ return _bornDateProperty; }
}

public DateTime? BornDate
{
  get { return (DateTime?) _bornDateProperty.GetValue(); }
  set { _bornDateProperty.SetValue(value); }
}

public AbstractProperty DeathDateProperty
{
  get{ return _deathDateProperty; }
}

public DateTime? DeathDate
{
  get { return (DateTime?) _deathDateProperty.GetValue(); }
  set { _deathDateProperty.SetValue(value); }
}

public AbstractProperty IsGroupProperty
{
  get{ return _isGroupProperty; }
}

public bool? IsGroup
{
  get { return (bool?) _isGroupProperty.GetValue(); }
  set { _isGroupProperty.SetValue(value); }
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

public PersonAspectWrapper()
{
  _personNameProperty = new SProperty(typeof(string));
  _occupationProperty = new SProperty(typeof(string));
  _biographyProperty = new SProperty(typeof(string));
  _originProperty = new SProperty(typeof(string));
  _bornDateProperty = new SProperty(typeof(DateTime?));
  _deathDateProperty = new SProperty(typeof(DateTime?));
  _isGroupProperty = new SProperty(typeof(bool?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, PersonAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  PersonName = (string) aspect[PersonAspect.ATTR_PERSON_NAME];
  Occupation = (string) aspect[PersonAspect.ATTR_OCCUPATION];
  Biography = (string) aspect[PersonAspect.ATTR_BIOGRAPHY];
  Origin = (string) aspect[PersonAspect.ATTR_ORIGIN];
  BornDate = (DateTime?) aspect[PersonAspect.ATTR_DATEOFBIRTH];
  DeathDate = (DateTime?) aspect[PersonAspect.ATTR_DATEOFDEATH];
  IsGroup = (bool?) aspect[PersonAspect.ATTR_GROUP];
}

public void SetEmpty()
{
  PersonName = null;
  Occupation = null;
  Biography = null;
  Origin = null;
  BornDate = null;
  DeathDate = null;
  IsGroup = null;
}

#endregion

}

}

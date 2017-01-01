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
/// ImporterAspectWrapper wraps the contents of <see cref="ImporterAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class ImporterAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _lastImportDateProperty;
protected AbstractProperty _dirtyProperty;
protected AbstractProperty _dateAddedProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty LastImportDateProperty
{
  get{ return _lastImportDateProperty; }
}

public DateTime? LastImportDate
{
  get { return (DateTime?) _lastImportDateProperty.GetValue(); }
  set { _lastImportDateProperty.SetValue(value); }
}

public AbstractProperty DirtyProperty
{
  get{ return _dirtyProperty; }
}

public bool? Dirty
{
  get { return (bool?) _dirtyProperty.GetValue(); }
  set { _dirtyProperty.SetValue(value); }
}

public AbstractProperty DateAddedProperty
{
  get{ return _dateAddedProperty; }
}

public DateTime? DateAdded
{
  get { return (DateTime?) _dateAddedProperty.GetValue(); }
  set { _dateAddedProperty.SetValue(value); }
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

public ImporterAspectWrapper()
{
  _lastImportDateProperty = new SProperty(typeof(DateTime?));
  _dirtyProperty = new SProperty(typeof(bool?));
  _dateAddedProperty = new SProperty(typeof(DateTime?));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, ImporterAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  LastImportDate = (DateTime?) aspect[ImporterAspect.ATTR_LAST_IMPORT_DATE];
  Dirty = (bool?) aspect[ImporterAspect.ATTR_DIRTY];
  DateAdded = (DateTime?) aspect[ImporterAspect.ATTR_DATEADDED];
}

public void SetEmpty()
{
  LastImportDate = null;
  Dirty = null;
  DateAdded = null;
}

#endregion

}

}

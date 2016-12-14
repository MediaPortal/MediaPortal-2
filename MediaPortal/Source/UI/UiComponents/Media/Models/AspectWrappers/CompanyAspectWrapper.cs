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
/// CompanyAspectWrapper wraps the contents of <see cref="CompanyAspect"/> into properties that can be bound from xaml controls.
/// Note: this code was automatically created by the MediaItemAspectModelBuilder helper tool under Resources folder.
/// </summary>
public class CompanyAspectWrapper: Control
{
#region Constants

public static readonly ICollection<string> EMPTY_STRING_COLLECTION = new List<string>().AsReadOnly();

#endregion

#region Fields

protected AbstractProperty _companyNameProperty;
protected AbstractProperty _descriptionProperty;
protected AbstractProperty _companyTypeProperty;
protected AbstractProperty _mediaItemProperty;

#endregion

#region Properties

public AbstractProperty CompanyNameProperty
{
  get{ return _companyNameProperty; }
}

public string CompanyName
{
  get { return (string) _companyNameProperty.GetValue(); }
  set { _companyNameProperty.SetValue(value); }
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

public AbstractProperty CompanyTypeProperty
{
  get{ return _companyTypeProperty; }
}

public string CompanyType
{
  get { return (string) _companyTypeProperty.GetValue(); }
  set { _companyTypeProperty.SetValue(value); }
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

public CompanyAspectWrapper()
{
  _companyNameProperty = new SProperty(typeof(string));
  _descriptionProperty = new SProperty(typeof(string));
  _companyTypeProperty = new SProperty(typeof(string));
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
  if (mediaItem == null ||!MediaItemAspect.TryGetAspect(mediaItem.Aspects, CompanyAspect.Metadata, out aspect))
  {
     SetEmpty();
     return;
  }

  CompanyName = (string) aspect[CompanyAspect.ATTR_COMPANY_NAME];
  Description = (string) aspect[CompanyAspect.ATTR_DESCRIPTION];
  CompanyType = (string) aspect[CompanyAspect.ATTR_COMPANY_TYPE];
}

public void SetEmpty()
{
  CompanyName = null;
  Description = null;
  CompanyType = null;
}

#endregion

}

}

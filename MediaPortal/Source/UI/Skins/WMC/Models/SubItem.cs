#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class SubItem : ListItem
  {
    protected AbstractProperty _bringIntoViewProperty = new WProperty(typeof(bool), false);

    public SubItem(string name, string value) :
      base(name, value)
    {
    }
    public SubItem(string name, IResourceString value) :
      base(name, value)
    {
    }
    public SubItem()
    {
    }

    public AbstractProperty BringIntoViewProperty
    {
      get { return _bringIntoViewProperty; }
    }

    public bool BringIntoView
    {
      get { return (bool)_bringIntoViewProperty.GetValue(); }
      set { _bringIntoViewProperty.SetValue(value); }
    }
  }
}
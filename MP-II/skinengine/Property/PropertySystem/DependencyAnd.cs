#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core.Properties;

namespace SkinEngine.Properties
{
  public class DependencyAnd : Dependency
  {
    List<Property> _properties = new List<Property>();

    public DependencyAnd() {}

    public DependencyAnd(Property prop)
      :base(prop)
    {
      DependencyObject = prop;
      OnValueChanged(prop);
    }

    public void Add(Property prop)
    {
      _properties.Add(prop);
      prop.Attach(_handler);
      OnValueChanged(prop);
    }

    protected override void OnValueChanged(Property property)
    {
      bool result = true;
      result &= (bool)_dependency.GetValue();
      for (int i = 0; i < _properties.Count; ++i)
      {
        result &= (bool)_properties[i].GetValue();
      }
      SetValue(result);
    }
  }
}

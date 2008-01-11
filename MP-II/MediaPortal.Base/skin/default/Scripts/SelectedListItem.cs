#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System;
using SkinEngine;
using SkinEngine.Controls;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Properties;
using SkinEngine.Properties;


public class Scriptlet : IScriptProperty
{
  public Property Get(IControl control, string param)
  {
    Control c = control as Control;
    if (c == null) return null;
    ListContainer list = c as ListContainer;
    if (list == null)
      return Get(c.Container, param);

    return new SelectedListItemLabelProperty(list.SelectedItemProperty, param);
  }
}

public class SelectedListItemLabelProperty : Dependency
{
  Property _property;
  string _propname;
  public SelectedListItemLabelProperty(Property property, string name)
    : base(property)
  {
    base.Name = "SelectedListItemLabelProperty";
    _propname = name;
    OnValueChanged(property);
  }

  protected override void OnValueChanged(Property property)
  {
    ListItem item = property.GetValue() as ListItem;

    if (item == null)
    {
      SetValue("");
      return;
    }
    if (item.Contains(_propname))
    {
      SetValue(item.Labels[_propname].Evaluate(null, null));
      return;
    }
    SetValue("");
  }
};

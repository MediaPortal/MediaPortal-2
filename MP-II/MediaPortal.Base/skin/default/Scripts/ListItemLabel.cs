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
using System.Diagnostics;
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
    if (c.ListItemProperty != null)
    {
      return new ListItemLabelProperty(c.ListItemProperty, param);
    }

    c = c.Container as Control;
    if (c == null) return null;
    if (c.ListItemProperty != null)
    {
      return new ListItemLabelProperty(c.ListItemProperty, param);
    }

    return null;
  }
}

public class ListItemLabelProperty : Dependency
{
  Property _property;
  string _propname;
  ListItemChangedHandler _itemChangeHandler;
  ListItem _item;
  public ListItemLabelProperty(Property property, string name)
    : base(property)
  {
    _itemChangeHandler = new ListItemChangedHandler(OnItemChanged);
    base.Name = "ListItemLabelProperty";
    _propname = name;
    OnValueChanged(property);
  }

  protected override void OnValueChanged(Property property)
  {
    if (_item != null)
    {
      _item.OnChanged -= _itemChangeHandler;
    }
    ListItem item = property.GetValue() as ListItem;
    _item = item;
    if (item == null)
    {
      SetValue("");
      return;
    }
    _item.OnChanged += _itemChangeHandler;
    if (item.Contains(_propname))
    {
      SetValue(item.Labels[_propname].Evaluate(null, null));
      return;
    }
    SetValue("");
  }

  void OnItemChanged(ListItem item)
  {
    if (item.Contains(_propname))
    {
      SetValue(item.Labels[_propname].Evaluate(null, null));
      return;
    }
    SetValue("");
  }
};

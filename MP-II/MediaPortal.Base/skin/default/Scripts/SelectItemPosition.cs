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
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;


public class Scriptlet : IScriptProperty
{
  public Property Get(IControl control, string param)
  {
    Control c = control as Control;
    if (c == null) return null;
    ListContainer list = c as ListContainer;
    if (list == null)
      return Get(c.Container, param);

    return new SelectedListItemPositionProperty(list,list.SelectedItemProperty);
  }
}

public class SelectedListItemPositionProperty : Dependency
{
  Property _property;
  ListContainer _list;
  public SelectedListItemPositionProperty(ListContainer list,Property property)
    : base(property)
  {
    _list = list;
    base.Name = "SelectedListItemPositionProperty";
    OnValueChanged(property);
  }

  protected override void OnValueChanged(Property property)
  {
    Control c = _list.FocusedControl as Control;

    if (c == null)
    {
      SetValue( new Vector3(-1000,-1000,-1000));
      return;
    }
    Vector3 pos = c.Position;
    Control parent = c.Parent as Control;
    if (parent != null)
    {
      pos.Subtract(parent.Position);
    }
    SetValue(pos);
  }
};

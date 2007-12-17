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

    return new ListItemDependency(list, Int32.Parse(param));
  }

  public class ListItemDependency : Dependency
  {
    ListContainer _list;
    int _index;
    public ListItemDependency(ListContainer list, int index)
    {
      _index = index;
      _list = list;
      this.DependencyObject = _list.SelectedItemProperty;
      _list.SelectedSubItemIndexProperty.Attach(new PropertyChangedHandler(OnValueChanged));
    }

    protected override void OnValueChanged(Property property)
    {
      if (_list == null) return;
      if (_list.Items == null) return;
      if (_list.Items.Count == 0) return;
      int off = _list.PageOffset + 1;
      while (off > _list.Items.Count)
        off -= _list.Items.Count;
      if (off >= 0 && off < _list.Items.Count)
      {
        ListItem item = _list.Items[off];
        if (item.SubItems == null) return;
        if (item.SubItems.Count == 0) return;
        int index = 0;
        if (index >= 0 && index < item.SubItems.Count)
        {
          SetValue(item.SubItems[index]);
        }
        else
        {
          SetValue(null);
        }
      }
    }
  }

}
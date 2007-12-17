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
      this.DependencyObject = _list.ItemsProperty;
      //_list.SelectedItemProperty.Attach(new PropertyChangedHandler(onSelectedItemChanged));
      _list.Styles.SelectedStyleIndexProperty.Attach(new PropertyChangedHandler(onStyleChanged));
      _list.PageSizeProperty.Attach(new PropertyChangedHandler(onPageSizeChanged));
      _list.PageOffsetProperty.Attach(new PropertyChangedHandler(onPageOffsetChanged));
    }

    void onSelectedItemChanged(Property property)
    {
      OnValueChanged(property);
    }
    void onStyleChanged(Property property)
    {
      OnValueChanged(property);
    }
    void onPageSizeChanged(Property property)
    {
      OnValueChanged(property);
    }
    void onPageOffsetChanged(Property property)
    {
      OnValueChanged(property);
    }
    protected override void OnValueChanged(Property property)
    {
      int itemNr = _list.PageOffset + _index;
      if (_list.Wrap)
      {
        while (itemNr < 0) itemNr += _list.Items.Count;
        while (itemNr >= _list.Items.Count) itemNr -= _list.Items.Count;
      }
      if (itemNr >= 0 && itemNr < _list.Items.Count)
      {
        SetValue(_list.Items[itemNr]);
      }
      else
      {
        SetValue(null);
      }
    }
  }

}
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
    if (c == null)
    {
      return new Property("");
    }

    ListContainer list = c as ListContainer;
    if (list != null)
    {
      return new UpDownLabelProperty(list);
    }
    return Get(c.Container, param);
  }

  public class UpDownLabelProperty : Dependency
  {
    ListContainer _list;
    public UpDownLabelProperty(ListContainer list)
    {
      _list = list;
      base.DependencyObject = list.PageOffsetProperty;
      list.PageSizeProperty.Attach(new PropertyChangedHandler(OnValueChanged));
      list.PageOffsetProperty.Attach(new PropertyChangedHandler(OnValueChanged));
      OnValueChanged(list.PageOffsetProperty);
    }


    protected override void OnValueChanged(Property property)
    {
      if (_list.Items == null)
      {
        SetValue("1/1");
        return;
      }
      int itemCount = _list.Items.Count;
      int pageSize = _list.PageSize;
      if (pageSize != 0)
      {
        int pageNr = (_list.PageOffset / pageSize);
        if ((_list.PageOffset % pageSize) != 0) pageNr++;
        int totalPages = (itemCount / pageSize);
        if ((itemCount % pageSize) != 0) totalPages++;
        SetValue(String.Format("{0}/{1}", 1 + pageNr, totalPages));
      }
      else
      {
        SetValue(String.Format("{0}-{1}-{2}", itemCount, pageSize, _list.PageOffset));
      }
    }
  };

}

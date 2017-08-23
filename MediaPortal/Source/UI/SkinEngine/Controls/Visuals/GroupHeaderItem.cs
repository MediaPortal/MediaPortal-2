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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class GroupHeaderItem : DependencyObject
  {
    protected AbstractProperty _groupingValueProperty;

    public GroupHeaderItem()
    {
      _groupingValueProperty = new SProperty(typeof(object), null);
    }

    public object FirstItem { get; set; }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);

      var ghi = (GroupHeaderItem)source;
      FirstItem = ghi.FirstItem;
      GroupingValue = ghi.GroupingValue;
    }

    public AbstractProperty GroupingValueProperty
    {
      get { return _groupingValueProperty; }
    }

    public object GroupingValue
    {
      get { return (object)_groupingValueProperty.GetValue(); }
      set { _groupingValueProperty.SetValue(value); }
    }
  }
}
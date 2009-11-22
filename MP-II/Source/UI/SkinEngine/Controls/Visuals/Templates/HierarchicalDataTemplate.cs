#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System.Collections;
using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Templates
{
  public class HierarchicalDataTemplate : DataTemplate
  {
    #region Protected fields

    protected Property _itemsSourceProperty;

    #endregion

    #region Ctor

    public HierarchicalDataTemplate()
    {
      Init();
    }

    void Init()
    {
      _itemsSourceProperty = new Property(typeof(IEnumerable), null);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      HierarchicalDataTemplate hdt = (HierarchicalDataTemplate) source;
      ItemsSource = copyManager.GetCopy(hdt.ItemsSource);
    }

    #endregion

    #region Public properties

    public Property ItemsSourceProperty
    {
      get { return _itemsSourceProperty; }
    }

    public IEnumerable ItemsSource
    {
      get { return (IEnumerable) _itemsSourceProperty.GetValue(); }
      set { _itemsSourceProperty.SetValue(value); }
    }

    #endregion
  }
}

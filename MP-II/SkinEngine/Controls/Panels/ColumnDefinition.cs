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

using MediaPortal.Utilities.DeepCopy;
using MediaPortal.Presentation.DataObjects;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  public class ColumnDefinition : DefinitionBase
  {
    Property _widthProperty;
    public ColumnDefinition()
    {
      Init();
    }

    void Init()
    {
      _widthProperty = new Property(typeof(GridLength), new GridLength());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      ColumnDefinition d = (ColumnDefinition) source;
      Width = copyManager.GetCopy(d.Width);
    }

    public Property WidthProperty
    {
      get { return _widthProperty; }
    }

    public GridLength Width
    {
      get { return _widthProperty.GetValue() as GridLength; }
      set { _widthProperty.SetValue(value); }
    }
  }
}

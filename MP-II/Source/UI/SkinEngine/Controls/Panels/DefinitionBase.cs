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

using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class DefinitionBase : IDeepCopyable
  {
    Property _nameProperty;
    Property _lengthProperty;

    public DefinitionBase()
    {
      Init();
    }

    void Init()
    {
      _nameProperty = new Property(typeof(string), "");
      _lengthProperty = new Property(typeof(GridLength), new GridLength());
    }

    public virtual void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      DefinitionBase db = (DefinitionBase) source;
      Name = copyManager.GetCopy(db.Name);
      Length = copyManager.GetCopy(db.Length);
    }

    public Property NameProperty
    {
      get { return _nameProperty; }
    }

    public string Name
    {
      get { return _nameProperty.GetValue() as string; }
      set { _nameProperty.SetValue(value); }
    }

    public Property LengthProperty
    {
      get { return _lengthProperty; }
    }

    public GridLength Length
    {
      get { return (GridLength) _lengthProperty.GetValue(); }
      set { _lengthProperty.SetValue(value); }
    }
  }
}

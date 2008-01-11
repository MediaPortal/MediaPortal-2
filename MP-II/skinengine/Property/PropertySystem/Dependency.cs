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

using MediaPortal.Core.Properties;

namespace SkinEngine.Properties
{
  public class Dependency : Property
  {
    protected PropertyChangedHandler _handler;
    protected Property _dependency;

    public Dependency() { }

    public Dependency(Property prop)
    {
      DependencyObject = prop;
    }

    public object DependencyValue
    {
      get { return _dependency.GetValue(); }
    }
    public virtual void Reset()
    {
      if (_dependency != null)
        OnValueChanged(_dependency);
    }
    public Property DependencyObject
    {
      set
      {
        if (_dependency != null)
        {
          _dependency.Detach(_handler);
        }
        _dependency = value;
        if (_dependency != null)
        {
          _object = _dependency.GetValue();
          _handler = new PropertyChangedHandler(onPropertyChanged);
          _dependency.Attach(_handler);
        }
      }
    }

    private void onPropertyChanged(Property property)
    {
      OnValueChanged(property);
    }

    protected virtual void OnValueChanged(Property property)
    {
      SetValue(property.GetValue());
    }

  }
}

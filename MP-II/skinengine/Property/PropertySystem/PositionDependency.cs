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
using SlimDX;
using SlimDX.Direct3D9;

namespace SkinEngine.Properties
{
  public class PositionDependency : Dependency
  {
    private Vector3 _posParent;
    private Vector3 _posControl;
    private Vector3 _offset;

    public PositionDependency(Property propertyParent, Property propertyControl)
      : base(propertyParent)
    {
      propertyControl.Attach(new PropertyChangedHandler(OnControlValueChanged));
      OnValueChanged(propertyParent);
      OnControlValueChanged(propertyControl);
    }

    protected override void OnValueChanged(Property property)
    {
      _posParent = (Vector3)property.GetValue();
      Vector3 finalPos = _posControl;
      finalPos+=(_posParent);
      finalPos += (_offset);
      SetValue(finalPos);
    }

    protected void OnControlValueChanged(Property property)
    {
      _posControl = (Vector3)property.GetValue();
      Vector3 finalPos = _posControl;
      finalPos += (_posParent);
      finalPos += (_offset);
      SetValue(finalPos);
    }

    public Vector3 Offset
    {
      get
      {
        return _offset;
      }
      set
      {
        if (_offset != value)
        {
          _offset = value;
          OnValueChanged((Property)base._dependency);
        }
      }
    }
  }
}

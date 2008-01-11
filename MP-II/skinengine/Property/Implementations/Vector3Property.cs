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
using Microsoft.DirectX;
using SkinEngine.Controls;

namespace SkinEngine
{
  public class Vector3Property : IVector3Property
  {
    private Vector3 _vector;
    private IVector3Property _property;
    private Control _container;

    public Vector3Property(Vector3 vector)
    {
      _vector = vector;
    }

    public Vector3Property(IVector3Property property, IControl container)
    {
      _property = property;
      _container = (Control) container;
    }

    public Vector3 Evaluate(IControl container)
    {
      if (_property == null)
      {
        return _vector;
      }
      else
      {
        return _property.Evaluate(_container);
      }
    }

    public Vector3 Vector
    {
      get
      {
        if (_property == null)
        {
          return _vector;
        }
        else
        {
          return _property.Evaluate(_container);
        }
      }
      set { _vector = value; }
    }
  }
}

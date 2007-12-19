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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;

namespace SkinEngine.Controls.Transforms
{
  public class TransformGroup : Transform
  {
    Property _childrenProperty;
    public TransformGroup()
    {
      _childrenProperty = new Property(new TransformCollection());
    }

    public Property ChildrenProperty
    {
      get
      {
        return _childrenProperty;
      }
      set
      {
        _childrenProperty = value;
      }
    }

    public TransformCollection Children
    {
      get
      {
        return (TransformCollection)_childrenProperty.GetValue();
      }
      set
      {
        _childrenProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    public override void UpdateTransform()
    {
      _matrix = Matrix.Identity;
      foreach (Transform t in Children)
      {
        Matrix m;
        t.GetTransform(out m);
        _matrix.Multiply(m);
      }
    }
  }
}

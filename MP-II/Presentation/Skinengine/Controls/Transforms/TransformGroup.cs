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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D9;
using MyXaml.Core;
namespace Presentation.SkinEngine.Controls.Transforms
{
  public class TransformGroup : Transform, IAddChild
  {
    Property _childrenProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformGroup"/> class.
    /// </summary>
    public TransformGroup()
    {
      Init();
    }
    public TransformGroup(TransformGroup g)
      : base(g)
    {
      Init();
      foreach (Transform t in g.Children)
      {
        Children.Add((Transform)t.Clone());
      }
    }
    void Init()
    {
      _childrenProperty = new Property(new TransformCollection());
      _childrenProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      Children.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new TransformGroup(this);
    }

    protected void OnPropertyChanged(Property property)
    {
      _needUpdate = true;
      _needUpdateRel = true;
      Fire();
    }

    /// <summary>
    /// Gets or sets the children property.
    /// </summary>
    /// <value>The children property.</value>
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

    /// <summary>
    /// Gets or sets the children.
    /// </summary>
    /// <value>The children.</value>
    public TransformCollection Children
    {
      get
      {
        return (TransformCollection)_childrenProperty.GetValue();
      }
    }

    /// <summary>
    /// Updates the transform.
    /// </summary>
    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Identity;
      foreach (Transform t in Children)
      {
        Matrix m;
        t.GetTransform(out m);
        _matrix *= m;
      }
    }

    public override void UpdateTransformRel()
    {
      base.UpdateTransformRel();
      _matrixRel = Matrix.Identity;
      foreach (Transform t in Children)
      {
        Matrix m;
        t.GetTransformRel(out m);
        _matrixRel *= m;
      }
    }


    #region IAddChild Members

    public void AddChild(object o)
    {
      Children.Add((Transform)o);
    }

    #endregion
  }
}

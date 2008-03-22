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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Properties;
using SlimDX;
namespace Presentation.SkinEngine.Controls.Transforms
{
  public class TranslateTransform : Transform
  {
    Property _XProperty;
    Property _YProperty;
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateTransform"/> class.
    /// </summary>
    public TranslateTransform()
    {
      Init();
    }
    public TranslateTransform(TranslateTransform g)
      : base(g)
    {
      Init();
      X = g.X;
      Y = g.Y;
    }
    void Init()
    {
      _YProperty = new Property((double)0.0);
      _XProperty = new Property((double)0.0);
      _YProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _XProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new TranslateTransform(this);
    }

    protected void OnPropertyChanged(Property property)
    {
      _needUpdate = true;
      Fire();
    }
    /// <summary>
    /// Gets or sets the X property.
    /// </summary>
    /// <value>The X property.</value>
    public Property XProperty
    {
      get
      {
        return _XProperty;
      }
      set
      {
        _XProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the X.
    /// </summary>
    /// <value>The X.</value>
    public double X
    {
      get
      {
        return (double)_XProperty.GetValue();
      }
      set
      {
        _XProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the Y property.
    /// </summary>
    /// <value>The Y property.</value>
    public Property YProperty
    {
      get
      {
        return _YProperty;
      }
      set
      {
        _YProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the Y.
    /// </summary>
    /// <value>The Y.</value>
    public double Y
    {
      get
      {
        return (double)_YProperty.GetValue();
      }
      set
      {
        _YProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Updates the transform.
    /// </summary>
    public override void UpdateTransform()
    {
      base.UpdateTransform();
      _matrix = Matrix.Translation((float)X * SkinContext.Zoom.Width, (float)Y * SkinContext.Zoom.Width, 0);
    }

    public override void UpdateTransformRel()
    {
      base.UpdateTransformRel();
      _matrixRel = Matrix.Translation((float)X , (float)Y , 0);
    }

  }
}

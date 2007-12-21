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
using MediaPortal.Core;
using MediaPortal.Core.Properties;

using SkinEngine.Controls.Transforms;
using SkinEngine.Controls.Visuals;
using SkinEngine.Effects;
using SkinEngine;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Brushes
{
  /// <summary>
  /// todo:
  ///   - transforms
  ///   - stretchmode
  ///   - tilemode
  ///   - alignmentx/alignmenty
  ///   - viewbox
  /// </summary>
  public class Brush : Property
  {
    Property _opacityProperty;
    Property _relativeTransformProperty;
    Property _transformProperty;

    public Brush()
    {
      _opacityProperty = new Property((double)1.0f);
      _relativeTransformProperty = new Property(new TransformGroup());
      _transformProperty = new Property(new TransformGroup());
    }

    public void OnPropertyChanged()
    {
      Fire();
    }

    public virtual void Scale(ref float u, ref float v, ref ColorValue color)
    {
    }

    /// <summary>
    /// Gets or sets the opacity property.
    /// </summary>
    /// <value>The opacity property.</value>
    public Property OpacityProperty
    {
      get
      {
        return _opacityProperty;
      }
      set
      {
        _opacityProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the opacity.
    /// </summary>
    /// <value>The opacity.</value>
    public double Opacity
    {
      get
      {
        return (double)_opacityProperty.GetValue();
      }
      set
      {
        _opacityProperty.SetValue(value);
        OnPropertyChanged();
      }
    }


    /// <summary>
    /// Gets or sets the relative transform property.
    /// </summary>
    /// <value>The relative transform property.</value>
    public Property RelativeTransformProperty
    {
      get
      {
        return _relativeTransformProperty;
      }
      set
      {
        _relativeTransformProperty = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets the relative transform.
    /// </summary>
    /// <value>The relative transform.</value>
    public TransformGroup RelativeTransform
    {
      get
      {
        return (TransformGroup)_relativeTransformProperty.GetValue();
      }
      set
      {
        _relativeTransformProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets the transform property.
    /// </summary>
    /// <value>The transform property.</value>
    public Property TransformProperty
    {
      get
      {
        return _transformProperty;
      }
      set
      {
        _transformProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the transform.
    /// </summary>
    /// <value>The transform.</value>
    public TransformGroup Transform
    {
      get
      {
        return (TransformGroup)_transformProperty.GetValue();
      }
      set
      {
        _transformProperty.SetValue(value);
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    public virtual void SetupBrush(FrameworkElement element)
    {

    }

    public virtual void BeginRender()
    {
    }

    public virtual void EndRender()
    {
    }
  }
}

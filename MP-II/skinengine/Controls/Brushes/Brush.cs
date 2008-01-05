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
using SkinEngine.DirectX;
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
  ///   - resource cleanup (textures & vertexbuffers)
  /// </summary>
  public class Brush : Property, ICloneable
  {
    Property _opacityProperty;
    Property _relativeTransformProperty;
    Property _transformProperty;
    Property _keyProperty;
    bool _isOpacity;
    /// <summary>
    /// Initializes a new instance of the <see cref="Brush"/> class.
    /// </summary>
    public Brush()
    {
      Init();
    }

    public Brush(Brush b)
    {
      Init();
      Key = b.Key;
      Opacity = b.Opacity;
      RelativeTransform = (TransformGroup)b.RelativeTransform.Clone();
      Transform = (TransformGroup)b.Transform.Clone();
    }
    void Init()
    {
      _isOpacity = false;
      _keyProperty = new Property("");
      _opacityProperty = new Property((double)1.0f);
      _relativeTransformProperty = new Property(new TransformGroup());
      _transformProperty = new Property(new TransformGroup());
      _opacityProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public virtual object Clone()
    {
      return new Brush(this);
    }

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The prop.</param>
    protected virtual void OnPropertyChanged(Property prop)
    {
    }

    /// <summary>
    /// Scales the specified u/v coordinates.
    /// </summary>
    /// <param name="u">The u.</param>
    /// <param name="v">The v.</param>
    /// <param name="color">The color.</param>
    public virtual void Scale(ref float u, ref float v, ref ColorValue color)
    {
    }

    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string Key
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
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
      }
    }
    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    public virtual void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      for (int i = 0; i < verts.Length; ++i)
      {
        float u, v;
        float x1, y1;
        y1 = (float)verts[i].Y;
        v = (float)(y1 - element.ActualPosition.Y);
        v /= (float)(element.ActualHeight);

        x1 = (float)verts[i].X;
        u = (float)(x1 - element.ActualPosition.X);
        u /= (float)(element.ActualWidth);

        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;
        unchecked
        {
          ColorValue color = ColorValue.FromArgb((int)0xffffffff);
          color.Alpha *= (float)Opacity;
          verts[i].Color = color.ToArgb();
        }
        verts[i].Tu1 = u;
        verts[i].Tv1 = v;
        verts[i].Tu2 = u;
        verts[i].Tv2 = v;
      }
    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    public virtual void BeginRender()
    {
    }
    public virtual void BeginRender(Texture tex)
    {
    }

    /// <summary>
    /// Ends the render.
    /// </summary>
    public virtual void EndRender()
    {
    }

    public bool IsOpacityBrush
    {
      get
      {
        return _isOpacity;
      }
      set
      {
        _isOpacity = value;
      }
    }

    public virtual Texture Texture
    {
      get
      {
        return null;
      }
    }

  }
}

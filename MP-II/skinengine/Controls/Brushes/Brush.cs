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
    Property _freezableProperty;
    bool _isOpacity;
    protected System.Drawing.RectangleF _bounds;
    protected System.Drawing.PointF _orginalPosition;
    protected System.Drawing.PointF _minPosition;

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
      Freezable = b.Freezable;
    }

    void Init()
    {
      _isOpacity = false;
      _keyProperty = new Property("");
      _opacityProperty = new Property((double)1.0f);
      _relativeTransformProperty = new Property(new TransformGroup());
      _transformProperty = new Property(new TransformGroup());
      _opacityProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _freezableProperty = new Property(false);
      _bounds = new System.Drawing.RectangleF(0, 0, 0, 0);
      _orginalPosition = new System.Drawing.PointF(0, 0);
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
    /// Gets or sets the freezable property.
    /// </summary>
    /// <value>The freezable property.</value>
    public Property FreezableProperty
    {
      get
      {
        return _freezableProperty;
      }
      set
      {
        _freezableProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="Brush"/> is freezable.
    /// </summary>
    /// <value><c>true</c> if freezable; otherwise, <c>false</c>.</value>
    public bool Freezable
    {
      get
      {
        return (bool)_freezableProperty.GetValue();
      }
      set
      {
        _freezableProperty.SetValue(value);
      }
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
      float w = (float)element.ActualWidth;
      float h = (float)element.ActualHeight;
      float xoff = _bounds.X;
      float yoff = _bounds.Y;
      if (element.FinalLayoutTransform != null)
      {
        w = _bounds.Width;
        h = _bounds.Height;
        element.FinalLayoutTransform.TransformXY(ref w, ref h);
        element.FinalLayoutTransform.TransformXY(ref xoff, ref yoff);
      }
      for (int i = 0; i < verts.Length; ++i)
      {
        float u, v;
        float x1, y1;
        y1 = (float)verts[i].Y;
        v = (float)(y1 - (element.ActualPosition.Y + yoff));
        v /= (float)(h);

        x1 = (float)verts[i].X;
        u = (float)(x1 - (element.ActualPosition.X + xoff));
        u /= (float)(w);

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

    protected void UpdateBounds(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      float minx = float.MaxValue;
      float miny = float.MaxValue;
      float maxx = 0;
      float maxy = 0;
      for (int i = 0; i < verts.Length; ++i)
      {
        if (verts[i].X < minx) minx = verts[i].X;
        if (verts[i].Y < miny) miny = verts[i].Y;

        if (verts[i].X > maxx) maxx = verts[i].X;
        if (verts[i].Y > maxy) maxy = verts[i].Y;
      }
      if (element.FinalLayoutTransform != null)
      {
        maxx -= minx;
        maxy -= miny;
        minx -= (float)element.ActualPosition.X;
        miny -= (float)element.ActualPosition.Y;
        element.FinalLayoutTransform.InvertXY(ref minx, ref miny);
        element.FinalLayoutTransform.InvertXY(ref maxx, ref maxy);

        _orginalPosition.X = (float)element.ActualPosition.X;
        _orginalPosition.Y = (float)element.ActualPosition.Y;
        _minPosition.X = _orginalPosition.X+minx;
        _minPosition.Y = _orginalPosition.Y+miny;
      }
      _bounds = new System.Drawing.RectangleF(minx, miny, maxx, maxy);

    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    /// <param name="vertexBuffer">The vertex buffer.</param>
    public virtual void BeginRender(VertexBuffer vertexBuffer, int primitiveCount,PrimitiveType primitiveType)
    {
    }
    /// <summary>
    /// Begins the render.
    /// </summary>
    /// <param name="tex">The tex.</param>
    public virtual void BeginRender(Texture tex)
    {
    }

    /// <summary>
    /// Ends the render.
    /// </summary>
    public virtual void EndRender()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is an opacity brush.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is an opacity brush; otherwise, <c>false</c>.
    /// </value>
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

    /// <summary>
    /// Gets the texture.
    /// </summary>
    /// <value>The texture.</value>
    public virtual Texture Texture
    {
      get
      {
        return null;
      }
    }



  }
}

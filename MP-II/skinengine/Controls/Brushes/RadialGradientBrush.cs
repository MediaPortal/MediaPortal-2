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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine.Effects;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;


namespace SkinEngine.Controls.Brushes
{
  public class RadialGradientBrush : GradientBrush, IAsset
  {
    Texture _cacheTexture;
    double _height;
    double _width;
    Vector3 _position;
    EffectAsset _effect;
    DateTime _lastTimeUsed;

    Property _centerProperty;
    Property _gradientOriginProperty;
    Property _radiusXProperty;
    Property _radiusYProperty;
    float[] _offsets = new float[6];
    ColorValue[] _colors = new ColorValue[6];
    bool _refresh = false;
    bool _singleColor = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadialGradientBrush"/> class.
    /// </summary>
    public RadialGradientBrush()
    {
      Init();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadialGradientBrush"/> class.
    /// </summary>
    /// <param name="b">The b.</param>
    public RadialGradientBrush(RadialGradientBrush b)
      : base(b)
    {
      Init();
      Center = b.Center;
      GradientOrigin = b.GradientOrigin;
      RadiusX = b.RadiusX;
      RadiusY = b.RadiusY;
    }

    /// <summary>
    /// Inits this instance.
    /// </summary>
    void Init()
    {
      _centerProperty = new Property(new Vector2(0.5f, 0.5f));
      _gradientOriginProperty = new Property(new Vector2(0.5f, 0.5f));
      _radiusXProperty = new Property((double)0.5f);
      _radiusYProperty = new Property((double)0.5f);
      ContentManager.Add(this);
      _centerProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _gradientOriginProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _radiusXProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _radiusYProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns></returns>
    public override object Clone()
    {
      return new RadialGradientBrush(this);
    }
    /// <summary>
    /// Gets or sets the center property.
    /// </summary>
    /// <value>The center property.</value>
    public Property CenterProperty
    {
      get
      {
        return _centerProperty;
      }
      set
      {
        _centerProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the center.
    /// </summary>
    /// <value>The center.</value>
    public Vector2 Center
    {
      get
      {
        return (Vector2)_centerProperty.GetValue();
      }
      set
      {
        _centerProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the gradient origin property.
    /// </summary>
    /// <value>The gradient origin property.</value>
    public Property GradientOriginProperty
    {
      get
      {
        return _gradientOriginProperty;
      }
      set
      {
        _gradientOriginProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the gradient origin.
    /// </summary>
    /// <value>The gradient origin.</value>
    public Vector2 GradientOrigin
    {
      get
      {
        return (Vector2)_gradientOriginProperty.GetValue();
      }
      set
      {
        _gradientOriginProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the radius X property.
    /// </summary>
    /// <value>The radius X property.</value>
    public Property RadiusXProperty
    {
      get
      {
        return _radiusXProperty;
      }
      set
      {
        _radiusXProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the radius X.
    /// </summary>
    /// <value>The radius X.</value>
    public double RadiusX
    {
      get
      {
        return (double)_radiusXProperty.GetValue();
      }
      set
      {
        _radiusXProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the radius Y property.
    /// </summary>
    /// <value>The radius Y property.</value>
    public Property RadiusYProperty
    {
      get
      {
        return _radiusYProperty;
      }
      set
      {
        _radiusYProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the radius Y.
    /// </summary>
    /// <value>The radius Y.</value>
    public double RadiusY
    {
      get
      {
        return (double)_radiusYProperty.GetValue();
      }
      set
      {
        _radiusYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The prop.</param>
    protected override void OnPropertyChanged(Property prop)
    {
      _refresh = true;
    }

    /// <summary>
    /// Setups the brush.
    /// </summary>
    /// <param name="element">The element.</param>
    public override void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      //      Trace.WriteLine("RadialGradientBrush.SetupBrush()");
      _verts = verts;
      //if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        UpdateBounds(element, ref verts);
        if (!IsOpacityBrush)
          base.SetupBrush(element, ref verts);

        _height = element.ActualHeight;
        _width = element.ActualWidth;
        _position = new Vector3((float)element.ActualPosition.X, (float)element.ActualPosition.Y, (float)element.ActualPosition.Z); ;

        if (_gradientTexture == null)
        {
          _gradientTexture = new Texture(GraphicsDevice.Device, 256, 2, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
        }
        if (_cacheTexture != null)
        {
          _cacheTexture.Dispose();
          _cacheTexture = null;
        }
        _refresh = true;
      }
    }


    /// <summary>
    /// Begins the render.
    /// </summary>
    public override void BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      if (_gradientTexture == null) return;
      if (_refresh)
      {
        _refresh = false;
        int index = 0;
        foreach (GradientStop stop in GradientStops)
        {
          _offsets[index] = (float)stop.Offset;
          _colors[index] = ColorConverter.FromColor(stop.Color);
          _colors[index].Alpha *= (float)Opacity;
          index++;
        }
        _singleColor = true;
        for (int i = 0; i < GradientStops.Count - 1; ++i)
        {
          if (_colors[i].ToArgb() != _colors[i + 1].ToArgb())
          {
            _singleColor = false;
            break;
          }
        }
        CreateGradient();
        if (_singleColor)
        {
          SetColor(vertexBuffer);
          _effect = ContentManager.GetEffect("solidbrush");
        }
        else
        {
          _effect = ContentManager.GetEffect("radialgradient");
        }
        if (_cacheTexture != null)
        {
          _cacheTexture.Dispose();
          _cacheTexture = null;
        }
      }

      float[] g_focus = new float[2] { GradientOrigin.X, GradientOrigin.Y };
      float[] g_center = new float[2] { Center.X, Center.Y };
      float[] g_radius = new float[2] { (float)RadiusX, (float)RadiusY };
      if (MappingMode == BrushMappingMode.Absolute)
      {
        g_focus[0] = (float)((GradientOrigin.X - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_focus[1] = (float)((GradientOrigin.Y - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_center[0] = (float)((Center.X - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_center[1] = (float)((Center.Y - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_radius[0] = (float)(RadiusX / _bounds.Width);
        g_radius[1] = (float)(RadiusY / _bounds.Height);
      }

      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      if (!_singleColor)
      {
        if (Freezable)
        {
          if (_cacheTexture == null)
          {
            Trace.WriteLine("RadialGradientBrush:Create cached texture");
            _effect = ContentManager.GetEffect("radialgradient");
            float w = (float)_width;
            float h = (float)_height;
            float cx = ((float)GraphicsDevice.Width) / ((float)SkinContext.Width);
            float cy = ((float)GraphicsDevice.Height) / ((float)SkinContext.Height);

            bool copy = true;
            if ((int)w == SkinContext.Width && (int)h == SkinContext.Height)
            {
              copy = false;
              w /= 2;
              h /= 2;
            }
            ExtendedMatrix m = new ExtendedMatrix();
            m.Matrix *= SkinContext.FinalMatrix.Matrix;
            //next put the control at position (0,0,0)
            //and scale it correctly since the backbuffer now has the dimensions of the control
            //instead of the skin width/height dimensions
            m.Matrix *= Matrix.Translation(new Vector3(-(float)(_position.X + 1), -(float)(_position.Y + 1), 0));
            m.Matrix *= Matrix.Scaling((float)((((float)SkinContext.Width) * cx) / w), (float)((((float)SkinContext.Height * cy)) / h), 1.0f);

            SkinContext.AddTransform(m);

            GraphicsDevice.Device.EndScene();
            _cacheTexture = new Texture(GraphicsDevice.Device, (int)w, (int)h, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            //get the current backbuffer
            using (Surface backBuffer = GraphicsDevice.Device.GetRenderTarget(0))
            {
              //get the surface of our opacity texture
              using (Surface cacheSurface = _cacheTexture.GetSurfaceLevel(0))
              {
                if (copy)
                {
                  //copy the correct rectangle from the backbuffer in the opacitytexture
                  GraphicsDevice.Device.StretchRect(backBuffer,
                                                         new System.Drawing.Rectangle((int)(_position.X * cx), (int)(_position.Y * cy), (int)(_width * cx), (int)(_height * cy)),
                                                         cacheSurface,
                                                         new System.Drawing.Rectangle((int)0, (int)0, (int)(w), (int)(h)),
                                                         TextureFilter.None);

                }
                //change the rendertarget to the opacitytexture
                GraphicsDevice.Device.SetRenderTarget(0, cacheSurface);

                //render the control (will be rendered into the opacitytexture)
                GraphicsDevice.Device.BeginScene();
                GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
                //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;


                _effect.Parameters["g_focus"] = g_focus;
                _effect.Parameters["g_center"] = g_center;
                _effect.Parameters["g_radius"] = g_radius;
                _effect.Parameters["g_opacity"] = (float)Opacity;
                Matrix mrel = Matrix.Identity;
                RelativeTransform.GetTransform(out mrel);
                mrel = Matrix.Invert(mrel);
                _effect.Parameters["RelativeTransform"] = mrel;

                _effect.StartRender(_gradientTexture);

                GraphicsDevice.Device.SetStreamSource(0, vertexBuffer, 0, PositionColored2Textured.StrideSize);
                GraphicsDevice.Device.DrawPrimitives(primitiveType, 0, primitiveCount);

                _effect.EndRender();

                GraphicsDevice.Device.EndScene();
                SkinContext.RemoveTransform();

                //restore the backbuffer
                GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
              }
              //TextureLoader.Save(@"C:\1\1.png", ImageFileFormat.Png, _cacheTexture);
              _effect = ContentManager.GetEffect("normal");

            }
            GraphicsDevice.Device.BeginScene();
          }
          _effect.StartRender(_cacheTexture);
          //GraphicsDevice.Device.SetTexture(0, _cacheTexture);
          _lastTimeUsed = SkinContext.Now;

        }
        else
        {

          _effect.Parameters["g_focus"] = g_focus;
          _effect.Parameters["g_center"] = g_center;
          _effect.Parameters["g_radius"] = g_radius;
          _effect.Parameters["g_opacity"] = (float)Opacity;
          Matrix m = Matrix.Identity;
          RelativeTransform.GetTransform(out m);
          m = Matrix.Invert(m);
          _effect.Parameters["RelativeTransform"] = m;

          _effect.StartRender(_gradientTexture);
          _lastTimeUsed = SkinContext.Now;
        }
      }
      else
      {
        ColorValue v = ColorConverter.FromColor(GradientStops[0].Color);
        _effect.Parameters["g_solidColor"] = v;
        _effect.StartRender(null);
        _lastTimeUsed = SkinContext.Now;
      }
    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    /// <param name="tex">The tex.</param>
    public override void BeginRender(Texture tex)
    {
      if (tex == null)
      {
        return;
      }
      if (_refresh)
      {
        _refresh = false;
        int index = 0;
        foreach (GradientStop stop in GradientStops)
        {
          _offsets[index] = (float)stop.Offset;
          _colors[index] = ColorConverter.FromColor(stop.Color);
          _colors[index].Alpha *= (float)Opacity;
          index++;
        }
        _singleColor = true;
        for (int i = 0; i < GradientStops.Count - 1; ++i)
        {
          if (_colors[i].ToArgb() != _colors[i + 1].ToArgb())
          {
            _singleColor = false;
            break;
          }
        }
        CreateGradient();
        if (_singleColor)
        {
          //SetColor(vertexBuffer);
        }
        _effect = ContentManager.GetEffect("radialopacitygradient");
      }

      float[] g_focus = new float[2] { GradientOrigin.X, GradientOrigin.Y };
      float[] g_center = new float[2] { Center.X, Center.Y };
      float[] g_radius = new float[2] { (float)RadiusX, (float)RadiusY };

      if (MappingMode == BrushMappingMode.Absolute)
      {
        g_focus[0] = (float)((GradientOrigin.X - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_focus[1] = (float)((GradientOrigin.Y - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_center[0] = (float)((Center.X - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_center[1] = (float)((Center.Y - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_radius[0] = (float)(RadiusX / _bounds.Width);
        g_radius[1] = (float)(RadiusY / _bounds.Height);
      }
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      if (!_singleColor)
      {
        _effect.Parameters["g_alphatex"] = _gradientTexture;
        _effect.Parameters["g_focus"] = g_focus;
        _effect.Parameters["g_center"] = g_center;
        _effect.Parameters["g_radius"] = g_radius;
        Matrix m = Matrix.Identity;
        RelativeTransform.GetTransform(out m);
        m = Matrix.Invert(m);
        _effect.Parameters["RelativeTransform"] = m;

        _effect.StartRender(tex);
        _lastTimeUsed = SkinContext.Now;
      }
      else
      {
        _effect.StartRender(null);
        _lastTimeUsed = SkinContext.Now;
      }
    }

    /// <summary>
    /// Ends the render.
    /// </summary>
    public override void EndRender()
    {
      if (_effect != null)
      {
        _effect.EndRender();
      }
    }


    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public bool IsAllocated
    {
      get
      {
        return (_gradientTexture != null || _cacheTexture != null);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.Now - _lastTimeUsed;
        if (ts.TotalSeconds >= 1)
        {
          return true;
        }

        return false;
      }
    }

    /// <summary>
    /// Frees this asset.
    /// </summary>
    public void Free()
    {
      if (_gradientTexture != null)
      {
        _gradientTexture.Dispose();
        _gradientTexture = null;
      }
      if (_cacheTexture != null)
      {
        _cacheTexture.Dispose();
        _cacheTexture = null;
      }
    }

    #endregion

    /// <summary>
    /// Gets the texture.
    /// </summary>
    /// <value>The texture.</value>
    public override Texture Texture
    {
      get
      {
        return _gradientTexture;
      }
    }
  }
}


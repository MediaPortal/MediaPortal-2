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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine.Effects;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine;

namespace SkinEngine.Controls.Brushes
{
  public class LinearGradientBrush : GradientBrush, IAsset
  {
    Texture _gradientTexture;
    Texture _cacheTexture;
    double _height;
    double _width;
    Vector3 _position;
    EffectAsset _effect;
    DateTime _lastTimeUsed;

    Property _startPointProperty;
    Property _endPointProperty;
    float[] _offsets = new float[6];
    ColorValue[] _colors = new ColorValue[6];
    bool _refresh = false;
    bool _singleColor = true;
    PositionColored2Textured[] _verts;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearGradientBrush"/> class.
    /// </summary>
    public LinearGradientBrush()
    {
      Init();
    }
    public LinearGradientBrush(LinearGradientBrush b)
      : base(b)
    {
      Init();
      StartPoint = b.StartPoint;
      EndPoint = b.EndPoint;
    }
    void Init()
    {
      _startPointProperty = new Property(new Vector2(0.0f, 0.0f));
      _endPointProperty = new Property(new Vector2(1.0f, 1.0f));
      ContentManager.Add(this);
      _startPointProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _endPointProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new LinearGradientBrush(this);
    }

    /// <summary>
    /// Gets or sets the start point property.
    /// </summary>
    /// <value>The start point property.</value>
    public Property StartPointProperty
    {
      get
      {
        return _startPointProperty;
      }
      set
      {
        _startPointProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the start point.
    /// </summary>
    /// <value>The start point.</value>
    public Vector2 StartPoint
    {
      get
      {
        return (Vector2)_startPointProperty.GetValue();
      }
      set
      {
        _startPointProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the end point property.
    /// </summary>
    /// <value>The end point property.</value>
    public Property EndPointProperty
    {
      get
      {
        return _endPointProperty;
      }
      set
      {
        _endPointProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the end point.
    /// </summary>
    /// <value>The end point.</value>
    public Vector2 EndPoint
    {
      get
      {
        return (Vector2)_endPointProperty.GetValue();
      }
      set
      {
        _endPointProperty.SetValue(value);
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
//      Trace.WriteLine("LinearGradientBrush.SetupBrush()");
      _verts = verts;
      // if (_texture == null || element.ActualHeight != _height || element.ActualWidth != _width)
      {
        UpdateBounds(element,ref verts);
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
    void CreateGradient()
    {
      int[,] buffer = (int[,])_gradientTexture.LockRectangle(typeof(int), 0, LockFlags.None, new int[] { (int)2, (int)256 });
      float width = 256.0f;
      for (int i = 0; i < GradientStops.Count - 1; ++i)
      {
        GradientStop stopbegin = GradientStops[i];
        GradientStop stopend = GradientStops[i + 1];
        ColorValue colorStart = ColorValue.FromColor(stopbegin.Color);
        ColorValue colorEnd = ColorValue.FromColor(stopend.Color);
        int offsetStart = (int)(stopbegin.Offset * width);
        int offsetEnd = (int)(stopend.Offset * width);

        float distance = offsetEnd - offsetStart;
        for (int x = offsetStart; x < offsetEnd; ++x)
        {
          float step = (x - offsetStart) / distance;
          float r = step * (colorEnd.Red - colorStart.Red);
          r += colorStart.Red;

          float g = step * (colorEnd.Green - colorStart.Green);
          g += colorStart.Green;

          float b = step * (colorEnd.Blue - colorStart.Blue);
          b += colorStart.Blue;

          float a = step * (colorEnd.Alpha - colorStart.Alpha);
          a += colorStart.Alpha;

          ColorValue color = new ColorValue(r, g, b, a);
          if (IsOpacityBrush)
            color = new ColorValue(a, a, a, 1);
          buffer[0, x] = color.ToArgb();
          buffer[1, x] = color.ToArgb();
        }
      }

      _gradientTexture.UnlockRectangle(0);
      //TextureLoader.Save(@"c:\1\gradient.png",ImageFileFormat.Png,_texture);
    }

    void SetColor(VertexBuffer vertexbuffer)
    {
      ColorValue color = ColorValue.FromColor(GradientStops[0].Color);
      color.Alpha *= (float)Opacity;
      for (int i = 0; i < _verts.Length; ++i)
      {
        _verts[i].Color = color.ToArgb();
      }
      vertexbuffer.SetData(_verts, 0, LockFlags.None);
    }

    /// <summary>
    /// Begins the render.
    /// </summary>
    public override void BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      if (_gradientTexture == null)
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
          _colors[index] = ColorValue.FromColor(stop.Color);
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
        }
        if (_cacheTexture != null)
        {
          _cacheTexture.Dispose();
          _cacheTexture = null;
        }
      }

      float[] g_startpoint = new float[2] { StartPoint.X, StartPoint.Y };
      float[] g_endpoint = new float[2] { EndPoint.X, EndPoint.Y };
      if (MappingMode == BrushMappingMode.Absolute)
      {
        g_startpoint[0] = (float)((StartPoint.X - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_startpoint[1] = (float)((StartPoint.Y - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_endpoint[0] = (float)((EndPoint.X - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_endpoint[1] = (float)((EndPoint.Y - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);
      }
      GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
      if (!_singleColor)
      {
        if (Freezable)
        {
          if (_cacheTexture == null)
          {
            Trace.WriteLine("LinearGradientBrush:Create cached texture");
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
                  GraphicsDevice.Device.StretchRectangle(backBuffer,
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
                GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;

                _effect = ContentManager.GetEffect("lineargradient");

                _effect.Parameters["g_opacity"] = (float)Opacity;
                _effect.Parameters["g_StartPoint"] = g_startpoint;
                _effect.Parameters["g_EndPoint"] = g_endpoint;
                Matrix mrel = Matrix.Identity;
                RelativeTransform.GetTransform(out mrel);
                mrel = Matrix.Invert(mrel);
                _effect.Parameters["RelativeTransform"] = mrel;

                _effect.StartRender(_gradientTexture);

                GraphicsDevice.Device.SetStreamSource(0, vertexBuffer, 0);
                GraphicsDevice.Device.DrawPrimitives(primitiveType, 0, primitiveCount);

                _effect.EndRender();

                GraphicsDevice.Device.EndScene();
                SkinContext.RemoveTransform();

                //restore the backbuffer
                GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
              }
              _effect = null;

              //TextureLoader.Save(@"C:\1\1.png", ImageFileFormat.Png, _cacheTexture);
            }
            GraphicsDevice.Device.BeginScene();
          }
          GraphicsDevice.Device.SetTexture(0, _cacheTexture);
          _lastTimeUsed = SkinContext.Now;
        }
        else
        {
          if (IsOpacityBrush)
          {
            _effect = ContentManager.GetEffect("linearopacitygradient");
          }
          else
          {
            _effect = ContentManager.GetEffect("lineargradient");
          }
          _effect.Parameters["g_opacity"] = (float)Opacity;
          _effect.Parameters["g_StartPoint"] = g_startpoint;
          _effect.Parameters["g_EndPoint"] = g_endpoint;
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
        GraphicsDevice.Device.SetTexture(0, null);
        _lastTimeUsed = SkinContext.Now;
      }
    }

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
          _colors[index] = ColorValue.FromColor(stop.Color);
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
      }

      float[] g_startpoint = new float[2] { StartPoint.X, StartPoint.Y };
      float[] g_endpoint = new float[2] { EndPoint.X, EndPoint.Y };
      if (MappingMode == BrushMappingMode.Absolute)
      {
        g_startpoint[0] = (float)((StartPoint.X - (_bounds.X - _orginalPosition.X)) / _bounds.Width);
        g_startpoint[1] = (float)((StartPoint.Y - (_bounds.Y - _orginalPosition.Y)) / _bounds.Height);

        g_endpoint[0] = (float)((EndPoint.X - (_bounds.X - _orginalPosition.X)) / _bounds.Width);
        g_endpoint[1] = (float)((EndPoint.Y - (_bounds.Y - _orginalPosition.Y)) / _bounds.Height);
      }

      GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix;
      if (!_singleColor)
      {
        if (IsOpacityBrush)
        {
          _effect = ContentManager.GetEffect("linearopacitygradient");
        }
        else
        {
          _effect = ContentManager.GetEffect("lineargradient");
        }
        _effect.Parameters["g_alphatex"] = _gradientTexture;
        _effect.Parameters["g_StartPoint"] = g_startpoint;
        _effect.Parameters["g_EndPoint"] = g_endpoint;
        Matrix m = Matrix.Identity;
        RelativeTransform.GetTransform(out m);
        m = Matrix.Invert(m);
        _effect.Parameters["RelativeTransform"] = m;

        _effect.StartRender(tex);
        _lastTimeUsed = SkinContext.Now;
      }
      else
      {
        GraphicsDevice.Device.SetTexture(0, null);
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
        _effect = null;
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

    public override Texture Texture
    {
      get
      {
        return _gradientTexture;
      }
    }
  }
}

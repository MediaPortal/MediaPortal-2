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
  public class LinearGradientBrush : GradientBrush, IAsset
  {
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
    EffectHandleAsset _handleRelativeTransform;
    EffectHandleAsset _handleOpacity;
    EffectHandleAsset _handleStartPoint;
    EffectHandleAsset _handleEndPoint;
    EffectHandleAsset _handleSolidColor;
    EffectHandleAsset _handleAlphaTexture;
    BrushTexture _brushTexture;
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
        UpdateBounds(element, ref verts);
        if (!IsOpacityBrush)
          base.SetupBrush(element, ref verts);

        _height = element.ActualHeight;
        _width = element.ActualWidth;
        _position = new Vector3((float)element.ActualPosition.X, (float)element.ActualPosition.Y, (float)element.ActualPosition.Z); ;
        if (_brushTexture == null)
        {
          _brushTexture = BrushCache.Instance.GetGradientBrush(GradientStops, IsOpacityBrush);
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
    public override bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount, PrimitiveType primitiveType)
    {
      if (Transform != null)
      {
        ExtendedMatrix mTrans;
        Transform.GetTransform(out mTrans);
        SkinContext.AddTransform(mTrans);
      }
      if (_brushTexture == null)
      {
        return false;
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
        _brushTexture = BrushCache.Instance.GetGradientBrush(GradientStops, IsOpacityBrush);
        if (_singleColor)
        {
          SetColor(vertexBuffer);
          _effect = ContentManager.GetEffect("solidbrush");
          _handleSolidColor = _effect.GetParameterHandle("g_solidColor");
        }
        else
        {
          _effect = ContentManager.GetEffect("lineargradient");
          _handleRelativeTransform = _effect.GetParameterHandle("RelativeTransform");
          _handleOpacity = _effect.GetParameterHandle("g_opacity");
          _handleStartPoint = _effect.GetParameterHandle("g_StartPoint");
          _handleEndPoint = _effect.GetParameterHandle("g_EndPoint");
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
        g_startpoint[0] = (float)(((StartPoint.X * SkinContext.Zoom.Width) - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_startpoint[1] = (float)(((StartPoint.Y * SkinContext.Zoom.Height) - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_endpoint[0] = (float)(((EndPoint.X * SkinContext.Zoom.Width) - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_endpoint[1] = (float)(((EndPoint.Y * SkinContext.Zoom.Height) - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);
      }
      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      if (!_singleColor)
      {
        if (Freezable)
        {
          if (_cacheTexture == null)
          {
            _effect = ContentManager.GetEffect("lineargradient");
            _handleRelativeTransform = _effect.GetParameterHandle("RelativeTransform");
            _handleOpacity = _effect.GetParameterHandle("g_opacity");
            _handleStartPoint = _effect.GetParameterHandle("g_StartPoint");
            _handleEndPoint = _effect.GetParameterHandle("g_EndPoint");

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


                Matrix mrel = Matrix.Identity;
                RelativeTransform.GetTransformRel(out mrel);
                mrel = Matrix.Invert(mrel);
                _handleRelativeTransform.SetParameter(mrel);
                _handleOpacity.SetParameter((float)(Opacity * SkinContext.Opacity));
                _handleStartPoint.SetParameter(g_startpoint);
                _handleEndPoint.SetParameter(g_endpoint);
                _effect.StartRender(_brushTexture.Texture);

                GraphicsDevice.Device.SetStreamSource(0, vertexBuffer, 0, PositionColored2Textured.StrideSize);
                GraphicsDevice.Device.DrawPrimitives(primitiveType, 0, primitiveCount);

                _effect.EndRender();

                GraphicsDevice.Device.EndScene();
                SkinContext.RemoveTransform();

                //restore the backbuffer
                GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
                _effect = ContentManager.GetEffect("normal");
              }


              //TextureLoader.Save(@"C:\1\1.png", ImageFileFormat.Png, _cacheTexture);
            }
            GraphicsDevice.Device.BeginScene();
          }
          _effect.StartRender(_cacheTexture);
          //GraphicsDevice.Device.SetTexture(0, _cacheTexture);
          _lastTimeUsed = SkinContext.Now;
        }
        else
        {
          Matrix m = Matrix.Identity;
          RelativeTransform.GetTransformRel(out m);
          m = Matrix.Invert(m);

          _handleRelativeTransform.SetParameter(m);
          _handleOpacity.SetParameter((float)(Opacity * SkinContext.Opacity));
          _handleStartPoint.SetParameter(g_startpoint);
          _handleEndPoint.SetParameter(g_endpoint);
          _effect.StartRender(_brushTexture.Texture);
          _lastTimeUsed = SkinContext.Now;
        }
      }
      else
      {
        ColorValue v = ColorConverter.FromColor(GradientStops[0].Color);
        _handleSolidColor.SetParameter(v);
        _effect.StartRender(null);
        _lastTimeUsed = SkinContext.Now;
      }
      return true;
    }

    public override void BeginRender(Texture tex)
    {
      if (Transform != null)
      {
        ExtendedMatrix mTrans;
        Transform.GetTransform(out mTrans);
        SkinContext.AddTransform(mTrans);
      }
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
        _brushTexture = BrushCache.Instance.GetGradientBrush(GradientStops, IsOpacityBrush);
        if (_singleColor)
        {
          //SetColor(vertexBuffer);
        }
        _effect = ContentManager.GetEffect("linearopacitygradient");
        _handleRelativeTransform = _effect.GetParameterHandle("RelativeTransform");
        _handleOpacity = _effect.GetParameterHandle("g_opacity");
        _handleStartPoint = _effect.GetParameterHandle("g_StartPoint");
        _handleEndPoint = _effect.GetParameterHandle("g_EndPoint");
        _handleAlphaTexture = _effect.GetParameterHandle("g_alphatex");
      }

      float[] g_startpoint = new float[2] { StartPoint.X, StartPoint.Y };
      float[] g_endpoint = new float[2] { EndPoint.X, EndPoint.Y };
      if (MappingMode == BrushMappingMode.Absolute)
      {
        g_startpoint[0] = (float)(((StartPoint.X * SkinContext.Zoom.Width) - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_startpoint[1] = (float)(((StartPoint.Y * SkinContext.Zoom.Height) - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);

        g_endpoint[0] = (float)(((EndPoint.X * SkinContext.Zoom.Width) - (_minPosition.X - _orginalPosition.X)) / _bounds.Width);
        g_endpoint[1] = (float)(((EndPoint.Y * SkinContext.Zoom.Height) - (_minPosition.Y - _orginalPosition.Y)) / _bounds.Height);
      }

      //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
      if (!_singleColor)
      {
        Matrix m = Matrix.Identity;
        RelativeTransform.GetTransformRel(out m);
        m = Matrix.Invert(m);
        _handleRelativeTransform.SetParameter(m);
        _handleOpacity.SetParameter((float)(Opacity * SkinContext.Opacity));
        _handleStartPoint.SetParameter(g_startpoint);
        _handleEndPoint.SetParameter(g_endpoint);
        _handleAlphaTexture.SetParameter(_brushTexture.Texture);

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
      if (Transform != null)
      {
        SkinContext.RemoveTransform();
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
        return (_cacheTexture != null);
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
    public void Free(bool force)
    {
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
        return _brushTexture.Texture;
      }
    }
  }
}

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
using MediaPortal.Core.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using MediaPortal.Core.InputManager;
using SkinEngine;
using SkinEngine.DirectX;
using Rectangle = System.Drawing.Rectangle;

namespace SkinEngine.Controls.Visuals
{
  public enum VerticalAlignmentEnum
  {
    Top = 0,
    Center = 1,
    Bottom = 2,
    Stretch = 3,
  };

  public enum HorizontalAlignmentEnum
  {
    Left = 0,
    Center = 1,
    Right = 2,
    Stretch = 3,
  };

  public class FrameworkElement : UIElement, IAsset
  {
    Property _widthProperty;
    Property _heightProperty;

    Property _acutalWidthProperty;
    Property _actualHeightProperty;
    Property _horizontalAlignmentProperty;
    Property _verticalAlignmentProperty;
    VertexBuffer _vertexOpacityMaskBorder;
    Texture _textureOpacity;
    DateTime _lastTimeUsed;
    bool _updateOpacityMask;
    bool _mouseOver = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkElement"/> class.
    /// </summary>
    public FrameworkElement()
    {
      Init();
    }

    public FrameworkElement(FrameworkElement el)
      : base((UIElement)el)
    {
      Init();
      Width = el.Width;
      Height = el.Height;
      ActualWidth = el.ActualWidth;
      ActualHeight = el.ActualHeight;
      this.HorizontalAlignment = el.HorizontalAlignment;
      this.VerticalAlignment = el.VerticalAlignment;
    }
    void Init()
    {
      _widthProperty = new Property((double)0.0f);
      _heightProperty = new Property((double)0.0f);


      _acutalWidthProperty = new Property((double)0.0f);
      _actualHeightProperty = new Property((double)0.0f);
      _horizontalAlignmentProperty = new Property(HorizontalAlignmentEnum.Center);
      _verticalAlignmentProperty = new Property(VerticalAlignmentEnum.Center);


      _widthProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _heightProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _actualHeightProperty.Attach(new PropertyChangedHandler(OnActualHeightChanged));
      _acutalWidthProperty.Attach(new PropertyChangedHandler(OnActualWidthChanged));
      ContentManager.Add(this);

    }
    void OnActualHeightChanged(Property property)
    {
      _updateOpacityMask = true;
    }
    void OnActualWidthChanged(Property property)
    {
      _updateOpacityMask = true;
    }
    /// <summary>
    /// Called when a property value has been changed
    /// Since all UIElement properties are layout properties
    /// we're simply calling Invalidate() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    void OnPropertyChanged(Property property)
    {
      Invalidate();
    }

    #region properties
    /// <summary>
    /// Gets or sets the width property.
    /// </summary>
    /// <value>The width property.</value>
    public Property WidthProperty
    {
      get
      {
        return _widthProperty;
      }
      set
      {
        _widthProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public double Width
    {
      get
      {
        return (double)_widthProperty.GetValue();
      }
      set
      {
        _widthProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the height property.
    /// </summary>
    /// <value>The height property.</value>
    public Property HeightProperty
    {
      get
      {
        return _heightProperty;
      }
      set
      {
        _heightProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public double Height
    {
      get
      {
        return (double)_heightProperty.GetValue();
      }
      set
      {
        _heightProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the width property.
    /// </summary>
    /// <value>The width property.</value>
    public Property ActualWidthProperty
    {
      get
      {
        return _acutalWidthProperty;
      }
      set
      {
        _acutalWidthProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    /// <value>The width.</value>
    public double ActualWidth
    {
      get
      {
        return (double)_acutalWidthProperty.GetValue();
      }
      set
      {
        _acutalWidthProperty.SetValue(value);
      }
    }
    /// <summary>
    /// Gets or sets the height property.
    /// </summary>
    /// <value>The height property.</value>
    public Property ActualHeightProperty
    {
      get
      {
        return _actualHeightProperty;
      }
      set
      {
        _actualHeightProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    /// <value>The height.</value>
    public double ActualHeight
    {
      get
      {
        return (double)_actualHeightProperty.GetValue();
      }
      set
      {
        _actualHeightProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the horizontal alignment property.
    /// </summary>
    /// <value>The horizontal alignment property.</value>
    public Property HorizontalAlignmentProperty
    {
      get
      {
        return _horizontalAlignmentProperty;
      }
      set
      {
        _horizontalAlignmentProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the horizontal alignment.
    /// </summary>
    /// <value>The horizontal alignment.</value>
    public HorizontalAlignmentEnum HorizontalAlignment
    {
      get
      {
        return (HorizontalAlignmentEnum)_horizontalAlignmentProperty.GetValue();
      }
      set
      {
        _horizontalAlignmentProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the vertical alignment property.
    /// </summary>
    /// <value>The vertical alignment property.</value>
    public Property VerticalAlignmentProperty
    {
      get
      {
        return _verticalAlignmentProperty;
      }
      set
      {
        _verticalAlignmentProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the vertical alignment.
    /// </summary>
    /// <value>The vertical alignment.</value>
    public VerticalAlignmentEnum VerticalAlignment
    {
      get
      {
        return (VerticalAlignmentEnum)_verticalAlignmentProperty.GetValue();
      }
      set
      {
        _verticalAlignmentProperty.SetValue(value);
      }
    }
    #endregion


    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      if (x >= ActualPosition.X && x < ActualPosition.X + ActualWidth)
      {
        if (y >= ActualPosition.Y && y < ActualPosition.Y + ActualHeight)
        {
          if (!_mouseOver)
          {
            _mouseOver = true;
            FireEvent("OnMouseEnter");
          }
          if (IsEnabled && Focusable && !HasFocus)
          {
            HasFocus = true;
          }
          return;
        }
      }
      if (_mouseOver)
      {
        _mouseOver = false;
        FireEvent("OnMouseLeave");
      }
      if (IsEnabled && Focusable && HasFocus)
      {
        HasFocus = false;
      }
    }


    #region focus & control predicition

    /// <summary>
    /// Predicts the next control which is position above this control
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsEnabled && Focusable)
      {
        if (ActualPosition.Y < focusedFrameworkElement.ActualPosition.Y)
        {
          if (!strict)
          {
            return this;
          }
          //           |-------------------------------|  
          //   |----------------------------------------------|
          //   |----------------------|
          //                          |-----|
          //                          |-----------------------|
          if ((ActualPosition.X >= focusedFrameworkElement.ActualPosition.X && Position.X <= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth) ||
              (ActualPosition.X <= focusedFrameworkElement.ActualPosition.X &&
               ActualPosition.X + ActualWidth >= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth) ||
              (ActualPosition.X + ActualWidth >= focusedFrameworkElement.ActualPosition.X &&
               ActualPosition.X + ActualWidth <= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth))
          {
            return this;
          }
        }
      }
      return null;
    }


    /// <summary>
    /// Predicts the next control which is position below this control
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsEnabled && Focusable)
      {
        if (ActualPosition.Y > focusedFrameworkElement.ActualPosition.Y)
        {
          if (!strict)
          {
            return this;
          }
          if ((ActualPosition.X >= focusedFrameworkElement.ActualPosition.X && Position.X <= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth) ||
              (ActualPosition.X <= focusedFrameworkElement.ActualPosition.X &&
               ActualPosition.X + ActualWidth >= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth) ||
              (ActualPosition.X + ActualWidth >= focusedFrameworkElement.ActualPosition.X &&
               ActualPosition.X + ActualWidth <= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth))
          {
            return this;
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Predicts the next control which is position left of this control
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsEnabled && Focusable)
      {
        if (ActualPosition.X < focusedFrameworkElement.ActualPosition.X)
        {
          return this;
        }
      }
      return null;
    }

    /// <summary>
    /// Predicts the next control which is position right of this control
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused control.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public virtual FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (!IsVisible)
      {
        return null;
      }
      if (IsEnabled && Focusable)
      {
        if (ActualPosition.X > focusedFrameworkElement.ActualPosition.X)
        {
          return this;
        }
      }
      return null;
    }


    /// <summary>
    /// Calculates the distance between 2 controls
    /// </summary>
    /// <param name="c1">The c1.</param>
    /// <param name="c2">The c2.</param>
    /// <returns></returns>
    public float Distance(FrameworkElement c1, FrameworkElement c2)
    {
      float y = Math.Abs(c1.ActualPosition.Y - c2.ActualPosition.Y);
      float x = Math.Abs(c1.ActualPosition.X - c2.ActualPosition.X);
      float distance = (float)Math.Sqrt(y * y + x * x);
      return distance;
    }

    #endregion


    /// <summary>
    /// Renders this instance.
    /// </summary>
    public override void Render()
    {
      UpdateLayout();
      ExtendedMatrix m;

      if (OpacityMask != null)
      {
        // control has an opacity mask
        // What we do here is that
        // 1. we create a new opacitytexture which has the same dimensions as the control
        // 2. we copy the part of the current backbuffer where the control is rendered to the opacitytexture
        // 3. we set the rendertarget to the opacitytexture
        // 4. we render the control, since the rendertarget is the opacitytexture we render the control in the opacitytexture
        // 5. we restore the rendertarget to the backbuffer
        // 6. we render the opacitytexture using the opacitymask brush
        UpdateOpacityMask();

        float w = (float)ActualWidth;
        float h = (float)ActualHeight;
        float cx = ((float)GraphicsDevice.Width) / ((float)SkinContext.Width);
        float cy = ((float)GraphicsDevice.Height) / ((float)SkinContext.Height);


        m = new ExtendedMatrix();
        m.Matrix *= SkinContext.FinalMatrix.Matrix;

        //Apply the rendertransform
        if (RenderTransform != null)
        {
          Vector2 center = new Vector2((float)(this.ActualPosition.X + this.ActualWidth * RenderTransformOrigin.X), (float)(this.ActualPosition.Y + this.ActualHeight * RenderTransformOrigin.Y));
          m.Matrix *= Matrix.Translation(new Vector3(-center.X, -center.Y, 0));
          Matrix mNew;
          RenderTransform.GetTransform(out mNew);
          m.Matrix *= mNew;
          m.Matrix *= Matrix.Translation(new Vector3(center.X, center.Y, 0));
        }

        //next put the control at position (0,0,0)
        //and scale it correctly since the backbuffer now has the dimensions of the control
        //instead of the skin width/height dimensions
        m.Matrix *= Matrix.Translation(new Vector3(-(float)ActualPosition.X, -(float)ActualPosition.Y, 0));
        m.Matrix *= Matrix.Scaling((float)(((float)SkinContext.Width) / w), (float)(((float)SkinContext.Height) / h), 1);

        SkinContext.AddTransform(m);

        GraphicsDevice.Device.EndScene();

        //get the current backbuffer
        using (Surface backBuffer = GraphicsDevice.Device.GetRenderTarget(0))
        {
          //get the surface of our opacity texture
          using (Surface textureOpacitySurface = _textureOpacity.GetSurfaceLevel(0))
          {
            //copy the correct rectangle from the backbuffer in the opacitytexture
            GraphicsDevice.Device.StretchRect(backBuffer,
                                                   new System.Drawing.Rectangle((int)(ActualPosition.X * cx), (int)(ActualPosition.Y * cy), (int)(ActualWidth * cx), (int)(ActualHeight * cy)),
                                                   textureOpacitySurface,
                                                   new System.Drawing.Rectangle((int)0, (int)0, (int)(ActualWidth), (int)(ActualHeight)),
                                                   TextureFilter.None);


            //change the rendertarget to the opacitytexture
            GraphicsDevice.Device.SetRenderTarget(0, textureOpacitySurface);

            //render the control (will be rendered into the opacitytexture)
            GraphicsDevice.Device.BeginScene();
            GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
            //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
            DoRender();
            GraphicsDevice.Device.EndScene();
            SkinContext.RemoveTransform();

            //restore the backbuffer
            GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
          }

          //TextureLoader.Save(@"C:\erwin\trunk\MP-II\MediaPortal\bin\x86\Debug\text.png", ImageFileFormat.Png, _textureOpacity);

        }

        //now render the opacitytexture with the opacitymask brush
        GraphicsDevice.Device.BeginScene();
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        OpacityMask.BeginRender(_textureOpacity);
        GraphicsDevice.Device.SetStreamSource(0, _vertexOpacityMaskBorder, 0, PositionColored2Textured.StrideSize);
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
        OpacityMask.EndRender();

        _lastTimeUsed = DateTime.Now;
      }
      else
      {

        //no opacity mask
        //apply rendertransform
        if (RenderTransform != null)
        {
          m = new ExtendedMatrix();
          m.Matrix *= SkinContext.FinalMatrix.Matrix;
          Vector2 center = new Vector2((float)(this.ActualPosition.X + this.ActualWidth * RenderTransformOrigin.X), (float)(this.ActualPosition.Y + this.ActualHeight * RenderTransformOrigin.Y));
          m.Matrix *= Matrix.Translation(new Vector3(-center.X, -center.Y, 0));
          Matrix mNew;
          RenderTransform.GetTransform(out mNew);
          m.Matrix *= mNew;
          m.Matrix *= Matrix.Translation(new Vector3(center.X, center.Y, 0));
          SkinContext.AddTransform(m);
        }

        //render the control
        DoRender();

        //remove the rendertransform
        if (RenderTransform != null)
        {
          SkinContext.RemoveTransform();
        }
      }
    }

    #region opacitymask

    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public virtual bool IsAllocated
    {
      get
      {
        return (_vertexOpacityMaskBorder != null || _textureOpacity != null);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public virtual bool CanBeDeleted
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
    public virtual void Free()
    {
      if (_vertexOpacityMaskBorder != null)
      {
        _vertexOpacityMaskBorder.Dispose();
        _vertexOpacityMaskBorder = null;
      }

      if (_textureOpacity != null)
      {
        _textureOpacity.Dispose();
        _textureOpacity = null;
      }
    }

    #endregion

    /// <summary>
    /// Updates the opacity mask texture
    /// </summary>
    void UpdateOpacityMask()
    {
      if (OpacityMask == null) return;
      if (_vertexOpacityMaskBorder == null)
      {
        _updateOpacityMask = true;

        _vertexOpacityMaskBorder = PositionColored2Textured.Create(4);
      }
      if (!_updateOpacityMask) return;
      Trace.WriteLine("FrameworkElement.UpdateOpacityMask");
      if (_textureOpacity != null)
      {
        _textureOpacity.Dispose();
        _textureOpacity = null;
      }

      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      _textureOpacity = new Texture(GraphicsDevice.Device, (int)w, (int)h, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);

      PositionColored2Textured[] verts = new PositionColored2Textured[4];
      
      ColorValue col = ColorConverter.FromColor(System.Drawing.Color.White);
      col.Alpha *= (float)Opacity;
      int color = (int)col.ToArgb();
      SurfaceDescription desc = _textureOpacity.GetLevelDescription(0);

      float maxU = w / ((float)desc.Width);
      float maxV = h / ((float)desc.Height);
      //upperleft
      verts[0].X = (float)this.ActualPosition.X;
      verts[0].Y = (float)this.ActualPosition.Y;
      verts[0].Z = 1.0f;
      verts[0].Color = color;
      verts[0].Tu1 = 0;
      verts[0].Tv1 = 0;
      verts[0].Tu2 = 0;
      verts[0].Tv2 = 0;

      //bottom left
      verts[1].X = (float)(this.ActualPosition.X);
      verts[1].Y = (float)(this.ActualPosition.Y + this.ActualHeight);
      verts[1].Z = 1.0f;
      verts[1].Color = color;
      verts[1].Tu1 = 0;
      verts[1].Tv1 = maxV;
      verts[1].Tu2 = 0;
      verts[1].Tv2 = maxV;

      //bottomright
      verts[2].X = (float)(this.ActualPosition.X + this.ActualWidth);
      verts[2].Y = (float)(this.ActualPosition.Y + this.ActualHeight);
      verts[2].Z = 1.0f;
      verts[2].Color = color;
      verts[2].Tu1 = maxU;
      verts[2].Tv1 = maxV;
      verts[2].Tu2 = maxU;
      verts[2].Tv2 = maxV;

      //upper right
      verts[3].X = (float)(this.ActualPosition.X + this.ActualWidth);
      verts[3].Y = (float)(this.ActualPosition.Y);
      verts[3].Z = 1.0f;
      verts[3].Color = color;
      verts[3].Tu1 = maxU;
      verts[3].Tv1 = 0;
      verts[3].Tu2 = maxU;
      verts[3].Tv2 = 0;

      // Fill the vertex buffer
      OpacityMask.IsOpacityBrush = true;
      OpacityMask.SetupBrush(this, ref verts);
      PositionColored2Textured.Set(_vertexOpacityMaskBorder, ref verts);

      _updateOpacityMask = false;
    }
    #endregion
  }
}

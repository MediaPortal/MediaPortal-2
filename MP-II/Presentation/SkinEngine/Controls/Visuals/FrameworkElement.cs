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
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine;
using Presentation.SkinEngine.DirectX;
using Presentation.SkinEngine.Controls.Visuals.Styles;

namespace Presentation.SkinEngine.Controls.Visuals
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

  public class FrameworkElement: UIElement
  {

    Property _widthProperty;
    Property _heightProperty;

    Property _acutalWidthProperty;
    Property _actualHeightProperty;
    Property _horizontalAlignmentProperty;
    Property _verticalAlignmentProperty;
    Property _styleProperty;
    bool _updateOpacityMask;
    bool _mouseOver = false;
    bool _inRender = false;
    VerticalAlignmentEnum _verticalAlignmentCache = VerticalAlignmentEnum.Center;
    HorizontalAlignmentEnum _horizontalAlignmentCache = HorizontalAlignmentEnum.Center;
    double _actualWidthCache;
    double _actualHeightCache;
    VisualAssetContext _opacityMaskContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkElement"/> class.
    /// </summary>
    public FrameworkElement()
    {
      Init();
      Attach();
    }

    public FrameworkElement(FrameworkElement el)
      : base((UIElement)el)
    {
      Init();
      Width = el.Width;
      Height = el.Height;
      Style = el.Style;
      Attach();
      ActualWidth = el.ActualWidth;
      ActualHeight = el.ActualHeight;
      this.HorizontalAlignment = el.HorizontalAlignment;
      this.VerticalAlignment = el.VerticalAlignment;
    }

    void Init()
    {
      _widthProperty = new Property(typeof(double), 0.0);
      _heightProperty = new Property(typeof(double), 0.0);


      _acutalWidthProperty = new Property(typeof(double), 0.0);
      _actualHeightProperty = new Property(typeof(double), 0.0);
      _styleProperty = new Property(typeof(Style), null);
      _horizontalAlignmentProperty = new Property(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Center);
      _verticalAlignmentProperty = new Property(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Center);
    }

    void Attach()
    {
      _widthProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _heightProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _actualHeightProperty.Attach(new PropertyChangedHandler(OnActualHeightChanged));
      _acutalWidthProperty.Attach(new PropertyChangedHandler(OnActualWidthChanged));
      _styleProperty.Attach(new PropertyChangedHandler(OnStyleChanged));
      _horizontalAlignmentProperty.Attach(new PropertyChangedHandler(OnHorizontalAlignmentChanged));
      _verticalAlignmentProperty.Attach(new PropertyChangedHandler(OnVerticalAlignmentChanged));
    }

    protected void OnHorizontalAlignmentChanged(Property property)
    {
      _horizontalAlignmentCache = (HorizontalAlignmentEnum)_horizontalAlignmentProperty.GetValue();
    }
    protected void OnVerticalAlignmentChanged(Property property)
    {
      _verticalAlignmentCache = (VerticalAlignmentEnum)_verticalAlignmentProperty.GetValue();
    }

    protected virtual void OnStyleChanged(Property property)
    {
      ///@optimize: 
      Style.Set(this);
      Invalidate();
    }
    void OnActualHeightChanged(Property property)
    {
      _actualHeightCache = (double)_actualHeightProperty.GetValue();
      _updateOpacityMask = true;
    }
    void OnActualWidthChanged(Property property)
    {
      _actualWidthCache = (double)_acutalWidthProperty.GetValue();
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
        return _actualWidthCache;
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
        return _actualHeightCache;
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
        return _horizontalAlignmentCache;
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
        return _verticalAlignmentCache;
      }
      set
      {
        _verticalAlignmentProperty.SetValue(value);
      }
    }
    #endregion

    /// <summary>
    /// Gets or sets the control style property.
    /// </summary>
    /// <value>The control style property.</value>
    public Property StyleProperty
    {
      get
      {
        return _styleProperty;
      }
      set
      {
        _styleProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public Style Style
    {
      get
      {
        return _styleProperty.GetValue() as Style;
      }
      set
      {
        _styleProperty.SetValue(value);
      }
    }


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
          if ((ActualPosition.X >= focusedFrameworkElement.ActualPosition.X &&
               ActualPosition.X <= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth) ||
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
          if ((ActualPosition.X >= focusedFrameworkElement.ActualPosition.X &&
               ActualPosition.X <= focusedFrameworkElement.ActualPosition.X + focusedFrameworkElement.ActualWidth) ||
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
      try
      {
        SkinContext.Z -= 0.1f;
        _inRender = true;
        UpdateLayout();
        ExtendedMatrix matrix;

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
          float cx = 1.0f;// ((float)GraphicsDevice.Width) / ((float)SkinContext.Width);
          float cy = 1.0f;//((float)GraphicsDevice.Height) / ((float)SkinContext.Height);

          List<ExtendedMatrix> originalTransforms = SkinContext.Transforms;
          SkinContext.Transforms = new List<ExtendedMatrix>();
          matrix = new ExtendedMatrix();

          //Apply the rendertransform
          if (RenderTransform != null)
          {
            Vector2 center = new Vector2((float)(this.ActualPosition.X + this.ActualWidth * RenderTransformOrigin.X), (float)(this.ActualPosition.Y + this.ActualHeight * RenderTransformOrigin.Y));
            matrix.Matrix *= Matrix.Translation(new Vector3(-center.X, -center.Y, 0));
            Matrix mNew;
            RenderTransform.GetTransform(out mNew);
            matrix.Matrix *= mNew;
            matrix.Matrix *= Matrix.Translation(new Vector3(center.X, center.Y, 0));
          }

          //next put the control at position (0,0,0)
          //and scale it correctly since the backbuffer now has the dimensions of the control
          //instead of the skin width/height dimensions
          matrix.Matrix *= Matrix.Translation(new Vector3(-(float)ActualPosition.X, -(float)ActualPosition.Y, 0));
          matrix.Matrix *= Matrix.Scaling((float)(((float)GraphicsDevice.Width) / w), (float)(((float)GraphicsDevice.Height) / h), 1);

          SkinContext.AddTransform(matrix);

          GraphicsDevice.Device.EndScene();

          //get the current backbuffer
          using (Surface backBuffer = GraphicsDevice.Device.GetRenderTarget(0))
          {
            SurfaceDescription desc = backBuffer.Description;
            //get the surface of our opacity texture
            using (Surface textureOpacitySurface = _opacityMaskContext.Texture.GetSurfaceLevel(0))
            {
              //copy the correct rectangle from the backbuffer in the opacitytexture
              if (desc.Width == GraphicsDevice.Width && desc.Height == GraphicsDevice.Height)
              {

                //copy the correct rectangle from the backbuffer in the opacitytexture
                GraphicsDevice.Device.StretchRect(backBuffer,
                                                       new System.Drawing.Rectangle((int)(ActualPosition.X * cx), (int)(ActualPosition.Y * cy), (int)(ActualWidth * cx), (int)(ActualHeight * cy)),
                                                       textureOpacitySurface,
                                                       new System.Drawing.Rectangle((int)0, (int)0, (int)(ActualWidth), (int)(ActualHeight)),
                                                       TextureFilter.None);
              }
              else
              {
                GraphicsDevice.Device.StretchRect(backBuffer,
                                                       new System.Drawing.Rectangle(0, 0, desc.Width, desc.Height),
                                                       textureOpacitySurface,
                                                       new System.Drawing.Rectangle((int)0, (int)0, (int)(ActualWidth), (int)(ActualHeight)),
                                                       TextureFilter.None);

              }


              //change the rendertarget to the opacitytexture
              GraphicsDevice.Device.SetRenderTarget(0, textureOpacitySurface);

              //render the control (will be rendered into the opacitytexture)
              GraphicsDevice.Device.BeginScene();
              //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
              //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
              DoRender();
              GraphicsDevice.Device.EndScene();
              SkinContext.RemoveTransform();

              //restore the backbuffer
              GraphicsDevice.Device.SetRenderTarget(0, backBuffer);
            }

            //TextureLoader.Save(@"C:\erwin\trunk\MP-II\MediaPortal\bin\x86\Debug\text.png", ImageFileFormat.Png, _textureOpacity);

          }

          SkinContext.Transforms = originalTransforms;
          //now render the opacitytexture with the opacitymask brush
          GraphicsDevice.Device.BeginScene();
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          OpacityMask.BeginRender(_opacityMaskContext.Texture);
          GraphicsDevice.Device.SetStreamSource(0, _opacityMaskContext.VertexBuffer, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
          OpacityMask.EndRender();

          _opacityMaskContext.LastTimeUsed = SkinContext.Now;
        }
        else
        {

          //no opacity mask
          //apply rendertransform
          if (RenderTransform != null)
          {
            matrix = new ExtendedMatrix();
            matrix.Matrix *= SkinContext.FinalMatrix.Matrix;
            Vector2 center = new Vector2((float)(this.ActualPosition.X + this.ActualWidth * RenderTransformOrigin.X), (float)(this.ActualPosition.Y + this.ActualHeight * RenderTransformOrigin.Y));
            matrix.Matrix *= Matrix.Translation(new Vector3(-center.X, -center.Y, 0));
            Matrix mNew;
            RenderTransform.GetTransform(out mNew);
            matrix.Matrix *= mNew;
            matrix.Matrix *= Matrix.Translation(new Vector3(center.X, center.Y, 0));
            SkinContext.AddTransform(matrix);
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
      finally
      {
        _inRender = false;
      }
    }
    public override void BuildRenderTree()
    {
      if (!IsVisible) return;
      SkinContext.Z -= 0.1f;
      UpdateLayout();
      SkinContext.AddOpacity(this.Opacity);
      if (RenderTransform != null)
      {
        ExtendedMatrix matrix = new ExtendedMatrix();
        matrix.Matrix *= SkinContext.FinalMatrix.Matrix;
        Vector2 center = new Vector2((float)(this.ActualPosition.X + this.ActualWidth * RenderTransformOrigin.X), (float)(this.ActualPosition.Y + this.ActualHeight * RenderTransformOrigin.Y));
        matrix.Matrix *= Matrix.Translation(new Vector3(-center.X, -center.Y, 0));
        Matrix mNew;
        RenderTransform.GetTransform(out mNew);
        matrix.Matrix *= mNew;
        matrix.Matrix *= Matrix.Translation(new Vector3(center.X, center.Y, 0));
        SkinContext.AddTransform(matrix);
      }
      //render the control
      DoBuildRenderTree();
      //remove the rendertransform
      if (RenderTransform != null)
      {
        SkinContext.RemoveTransform();
      }
      SkinContext.RemoveOpacity();
    }

    #region opacitymask

    #region IAsset Members




    #endregion

    /// <summary>
    /// Updates the opacity mask texture
    /// </summary>
    void UpdateOpacityMask()
    {
      if (OpacityMask == null) return;
      if (_opacityMaskContext == null)
      {
        Trace.WriteLine("FrameworkElement:allocate _opacityMaskContext");
        _opacityMaskContext = new VisualAssetContext("FrameworkElement.OpacityMaskContext:" + this.Name);
        ContentManager.Add(_opacityMaskContext);
      }
      if (_opacityMaskContext.VertexBuffer == null)
      {
        _updateOpacityMask = true;

        _opacityMaskContext.VertexBuffer = PositionColored2Textured.Create(6);
      }
      if (!_updateOpacityMask) return;
      Trace.WriteLine("FrameworkElement.UpdateOpacityMask");
      _opacityMaskContext.LastTimeUsed = SkinContext.Now;
      if (_opacityMaskContext.Texture != null)
      {
        _opacityMaskContext.Texture.Dispose();
        _opacityMaskContext.Texture = null;
      }

      float w = (float)ActualWidth;
      float h = (float)ActualHeight;
      _opacityMaskContext.Texture = new Texture(GraphicsDevice.Device, (int)w, (int)h, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);

      PositionColored2Textured[] verts = new PositionColored2Textured[6];

      ColorValue col = ColorConverter.FromColor(System.Drawing.Color.White);
      col.Alpha *= (float)Opacity;
      int color = (int)col.ToArgb();
      SurfaceDescription desc = _opacityMaskContext.Texture.GetLevelDescription(0);

      float maxU = w / ((float)desc.Width);
      float maxV = h / ((float)desc.Height);
      //upperleft
      verts[0].X = (float)this.ActualPosition.X - 0.5f;
      verts[0].Y = (float)this.ActualPosition.Y - 0.5f;
      verts[0].Color = color;
      verts[0].Tu1 = 0;
      verts[0].Tv1 = 0;
      verts[0].Z=SkinContext.Z;

      //bottom left
      verts[1].X = (float)(this.ActualPosition.X) - 0.5f;
      verts[1].Y = (float)(this.ActualPosition.Y + this.ActualHeight) + 0.5f;
      verts[1].Color = color;
      verts[1].Tu1 = 0;
      verts[1].Tv1 = maxV;
      verts[1].Z = SkinContext.Z;

      //bottomright
      verts[2].X = (float)(this.ActualPosition.X + this.ActualWidth) + 0.5f;
      verts[2].Y = (float)(this.ActualPosition.Y + this.ActualHeight) + 0.5f;
      verts[2].Color = color;
      verts[2].Tu1 = maxU;
      verts[2].Tv1 = maxV;
      verts[2].Z = SkinContext.Z;

      //upperleft
      verts[3].X = (float)this.ActualPosition.X - 0.5f;
      verts[3].Y = (float)this.ActualPosition.Y - 0.5f;
      verts[3].Color = color;
      verts[3].Tu1 = 0;
      verts[3].Tv1 = 0;
      verts[3].Z = SkinContext.Z;

      //upper right
      verts[4].X = (float)(this.ActualPosition.X + this.ActualWidth) + 0.5f;
      verts[4].Y = (float)(this.ActualPosition.Y) - 0.5f;
      verts[4].Color = color;
      verts[4].Tu1 = maxU;
      verts[4].Tv1 = 0;
      verts[4].Z = SkinContext.Z;

      //bottomright
      verts[5].X = (float)(this.ActualPosition.X + this.ActualWidth) + 0.5f;
      verts[5].Y = (float)(this.ActualPosition.Y + this.ActualHeight) + 0.5f;
      verts[5].Color = color;
      verts[5].Tu1 = maxU;
      verts[5].Tv1 = maxV;

      // Fill the vertex buffer
      OpacityMask.IsOpacityBrush = true;
      OpacityMask.SetupBrush(this, ref verts);
      PositionColored2Textured.Set(_opacityMaskContext.VertexBuffer, ref verts);

      _updateOpacityMask = false;
    }
    #endregion

    public override void Allocate()
    {
      if (_opacityMaskContext != null)
      {
        ContentManager.Add(_opacityMaskContext);
      }
    }

    public override void Deallocate()
    {
      if (_opacityMaskContext != null)
      {
        Trace.WriteLine("FrameworkElement:deallocate _opacityMaskContext");
        _opacityMaskContext.Free(true);
        ContentManager.Remove(_opacityMaskContext);
        _opacityMaskContext = null;
      }
    }
  }
}

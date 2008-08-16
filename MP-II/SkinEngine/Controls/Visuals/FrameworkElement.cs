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
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals
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
    #region Private fields

    Property _widthProperty;
    Property _heightProperty;

    Property _actualWidthProperty;
    Property _actualHeightProperty;
    Property _horizontalAlignmentProperty;
    Property _verticalAlignmentProperty;
    Property _styleProperty;
    bool _updateOpacityMask;
    bool _mouseOver = false;
    VisualAssetContext _opacityMaskContext;

    #endregion

    #region Ctor

    public FrameworkElement()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _widthProperty = new Property(typeof(double), Double.NaN);
      _heightProperty = new Property(typeof(double), Double.NaN);

      _actualWidthProperty = new Property(typeof(double), Double.NaN);
      _actualHeightProperty = new Property(typeof(double), Double.NaN);
      _styleProperty = new Property(typeof(Style), null);
      _horizontalAlignmentProperty = new Property(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Center);
      _verticalAlignmentProperty = new Property(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Center);
    }

    void Attach()
    {
      _widthProperty.Attach(OnPropertyChanged);
      _heightProperty.Attach(OnPropertyChanged);
      _actualHeightProperty.Attach(OnActualHeightChanged);
      _actualWidthProperty.Attach(OnActualWidthChanged);
      _styleProperty.Attach(OnStyleChanged);
    }

    void Detach()
    {
      _widthProperty.Detach(OnPropertyChanged);
      _heightProperty.Detach(OnPropertyChanged);
      _actualHeightProperty.Detach(OnActualHeightChanged);
      _actualWidthProperty.Detach(OnActualWidthChanged);
      _styleProperty.Detach(OnStyleChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      FrameworkElement fe = source as FrameworkElement;
      Width = copyManager.GetCopy(fe.Width);
      Height = copyManager.GetCopy(fe.Height);
      Style = copyManager.GetCopy(fe.Style);
      ActualWidth = copyManager.GetCopy(fe.ActualWidth);
      ActualHeight = copyManager.GetCopy(fe.ActualHeight);
      HorizontalAlignment = copyManager.GetCopy(fe.HorizontalAlignment);
      VerticalAlignment = copyManager.GetCopy(fe.VerticalAlignment);
      Attach();
    }

    #endregion

    protected virtual void OnStyleChanged(Property property)
    {
      ///@optimize: 
      Style.Set(this);
      Invalidate();
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

    #region Public properties

    public Property WidthProperty
    {
      get { return _widthProperty; }
    }

    public double Width
    {
      get { return (double)_widthProperty.GetValue(); }
      set { _widthProperty.SetValue(value); }
    }

    public Property HeightProperty
    {
      get { return _heightProperty; }
    }

    public double Height
    {
      get { return (double)_heightProperty.GetValue(); }
      set { _heightProperty.SetValue(value); }
    }

    public Property ActualWidthProperty
    {
      get { return _actualWidthProperty; }
    }

    public double ActualWidth
    {
      get { return (double) _actualWidthProperty.GetValue(); }
      set { _actualWidthProperty.SetValue(value); }
    }

    public Property ActualHeightProperty
    {
      get { return _actualHeightProperty; }
    }

    public double ActualHeight
    {
      get { return (double)_actualHeightProperty.GetValue(); }
      set { _actualHeightProperty.SetValue(value); }
    }

    public Property HorizontalAlignmentProperty
    {
      get { return _horizontalAlignmentProperty; }
    }

    public HorizontalAlignmentEnum HorizontalAlignment
    {
      get { return (HorizontalAlignmentEnum) _horizontalAlignmentProperty.GetValue(); }
      set { _horizontalAlignmentProperty.SetValue(value); }
    }

    public Property VerticalAlignmentProperty
    {
      get { return _verticalAlignmentProperty; }
    }

    public VerticalAlignmentEnum VerticalAlignment
    {
      get { return (VerticalAlignmentEnum) _verticalAlignmentProperty.GetValue(); }
      set { _verticalAlignmentProperty.SetValue(value);  }
    }

    public Property StyleProperty
    {
      get { return _styleProperty; }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public Style Style
    {
      get { return _styleProperty.GetValue() as Style; }
      set { _styleProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Adds the element's margin to totalSize.
    /// </summary>
    public void AddMargin(ref SizeF totalSize)
    {
      if(!Double.IsNaN(totalSize.Width))
        totalSize.Width += (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      if (!Double.IsNaN(totalSize.Height))
        totalSize.Height += (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
    }

    /// <summary>
    /// Computes the inner rectangle from the outer rectangle. i.e. the rectangle 
    /// without the margins.
    /// </summary>
    public void ComputeInnerRectangle(ref RectangleF outerRect)
    {
      outerRect.X += Margin.Left * SkinContext.Zoom.Width;
      outerRect.Y += Margin.Top * SkinContext.Zoom.Height;

      outerRect.Width -= (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      outerRect.Height -= (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
    }

    /// <summary>
    /// Computes the total desired width.
    /// </summary>
    public void TotalDesiredSize(ref SizeF totalSize)
    {
      totalSize.Width = _desiredSize.Width + (Margin.Left + Margin.Right) * SkinContext.Zoom.Width;
      totalSize.Height = _desiredSize.Height + (Margin.Top + Margin.Bottom) * SkinContext.Zoom.Height;
    }

    /// <summary>
    /// Arranges the child horizontal and vertical.
    /// </summary>
    public void ArrangeChild(FrameworkElement child, ref PointF p, ref SizeF availableSize)
    {
      SizeF childSize = new SizeF();
      child.TotalDesiredSize(ref childSize);

      if (Double.IsNaN(childSize.Width))
        return;
      if (Double.IsNaN(childSize.Height))
        return;

      if (child.GetType() == typeof(MediaPortal.SkinEngine.Controls.Panels.DockPanel))
        return;

      if(childSize.Width < availableSize.Width)
      {
        if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
        {
          p.X += (float)((availableSize.Width - childSize.Width) / 2);
        }
        else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
        {
          p.X += (float)(availableSize.Width - childSize.Width);
        }
        availableSize.Width = childSize.Width;
      }

      if (childSize.Height < availableSize.Height)
      {
        if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
        {
          p.Y += (float)((availableSize.Height - childSize.Height) / 2);
        }
        else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
        {
          p.Y += (float)(availableSize.Height - childSize.Height);
        }
        availableSize.Height = childSize.Height;
      }
      
    }

    /// <summary>
    /// Arranges the child horizontal.
    /// </summary>
    public void ArrangeChildHorizontal(FrameworkElement child, ref PointF p, ref SizeF availableSize)
    {
      SizeF childSize = new SizeF();

      child.TotalDesiredSize(ref childSize);

      if (!Double.IsNaN(child.Width) && childSize.Width < availableSize.Width)
      {

        if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
        {
          p.X += (float)((availableSize.Width - childSize.Width) / 2);
        }
        else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
        {
          p.X += (float)(availableSize.Width - childSize.Width);
        }
        availableSize.Width = childSize.Width;
      }
    }

    /// <summary>
    /// Arranges the child vertical.
    /// </summary>
    public void ArrangeChildVertical(FrameworkElement child, ref PointF p, ref SizeF availableSize)
    {
      SizeF childSize = new SizeF();

      child.TotalDesiredSize(ref childSize);

      if (!Double.IsNaN(child.Width) && childSize.Height < availableSize.Height)
      {
        if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
        {
          p.Y += (float)((availableSize.Height - childSize.Height) / 2);
        }
        else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
        {
          p.Y += (float)(availableSize.Height - childSize.Height);
        }
        availableSize.Height = childSize.Height;
      }
    }

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


    #region Focus & control predicition

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

    public override void Render()
    {

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
        float cx = 1.0f;// GraphicsDevice.Width / (float) SkinContext.SkinWidth;
        float cy = 1.0f;// GraphicsDevice.Height / (float) SkinContext.SkinHeight;

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
        matrix.Matrix *= Matrix.Translation(new Vector3(-ActualPosition.X, -ActualPosition.Y, 0));
        matrix.Matrix *= Matrix.Scaling((GraphicsDevice.Width / w), (GraphicsDevice.Height / h), 1);

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

    public override void BuildRenderTree()
    {
      if (!IsVisible) 
        return;
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

    #region Opacitymask

    /// <summary>
    /// Updates the opacity mask texture
    /// </summary>
    void UpdateOpacityMask()
    {
      if (OpacityMask == null) return;
      if (_opacityMaskContext == null)
      {
        //Trace.WriteLine("FrameworkElement: Allocate _opacityMaskContext");
        _opacityMaskContext = new VisualAssetContext("FrameworkElement.OpacityMaskContext:" + this.Name);
        ContentManager.Add(_opacityMaskContext);
      }
      if (_opacityMaskContext.VertexBuffer == null)
      {
        _updateOpacityMask = true;

        _opacityMaskContext.VertexBuffer = PositionColored2Textured.Create(6);
      }
      if (!_updateOpacityMask) return;
      //Trace.WriteLine("FrameworkElement.UpdateOpacityMask");
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
      verts[0].Z = ActualPosition.Z;

      //bottom left
      verts[1].X = (float)(this.ActualPosition.X) - 0.5f;
      verts[1].Y = (float)(this.ActualPosition.Y + this.ActualHeight) + 0.5f;
      verts[1].Color = color;
      verts[1].Tu1 = 0;
      verts[1].Tv1 = maxV;
      verts[1].Z = ActualPosition.Z;

      //bottomright
      verts[2].X = (float)(this.ActualPosition.X + this.ActualWidth) + 0.5f;
      verts[2].Y = (float)(this.ActualPosition.Y + this.ActualHeight) + 0.5f;
      verts[2].Color = color;
      verts[2].Tu1 = maxU;
      verts[2].Tv1 = maxV;
      verts[2].Z = ActualPosition.Z;

      //upperleft
      verts[3].X = (float)this.ActualPosition.X - 0.5f;
      verts[3].Y = (float)this.ActualPosition.Y - 0.5f;
      verts[3].Color = color;
      verts[3].Tu1 = 0;
      verts[3].Tv1 = 0;
      verts[3].Z = ActualPosition.Z;

      //upper right
      verts[4].X = (float)(this.ActualPosition.X + this.ActualWidth) + 0.5f;
      verts[4].Y = (float)(this.ActualPosition.Y) - 0.5f;
      verts[4].Color = color;
      verts[4].Tu1 = maxU;
      verts[4].Tv1 = 0;
      verts[4].Z = ActualPosition.Z;

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
        //Trace.WriteLine("FrameworkElement: Deallocate _opacityMaskContext");
        _opacityMaskContext.Free(true);
        ContentManager.Remove(_opacityMaskContext);
        _opacityMaskContext = null;
      }
    }
  }
}

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
using System.Drawing.Drawing2D;
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine.Controls.Brushes;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;
using SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;
using MyXaml.Core;

namespace SkinEngine.Controls.Visuals
{
  public class Border : Shape, IAddChild
  {
    Property _backgroundProperty;
    Property _borderProperty;
    Property _borderThicknessProperty;
    Property _cornerRadiusProperty;
    FrameworkElement _content;
    VisualAssetContext _backgroundAsset;
    VisualAssetContext _borderAsset;
    #region ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="Border"/> class.
    /// </summary>
    public Border()
    {
      Init();
    }

    public Border(Border b)
      : base(b)
    {
      Init();
      if (b.BorderBrush != null)
        this.BorderBrush = (Brush)b.BorderBrush.Clone();
      if (b.Background != null)
        this.Background = (Brush)b.Background.Clone();
      BorderThickness = b.BorderThickness;
      CornerRadius = b.CornerRadius;
    }

    void Init()
    {
      _borderProperty = new Property(null);
      _backgroundProperty = new Property(null);
      _borderThicknessProperty = new Property((double)1.0);
      _cornerRadiusProperty = new Property((double)0);

      _borderProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _backgroundProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _borderThicknessProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _cornerRadiusProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    public override object Clone()
    {
      return new Border(this);
    }

    void OnPropertyChanged(Property property)
    {
      //Free(false);
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property BackgroundProperty
    {
      get
      {
        return _backgroundProperty;
      }
      set
      {
        _backgroundProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public Brush Background
    {
      get
      {
        return _backgroundProperty.GetValue() as Brush;
      }
      set
      {
        _backgroundProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the Border property.
    /// </summary>
    /// <value>The Border property.</value>
    public Property BorderBrushProperty
    {
      get
      {
        return _borderProperty;
      }
      set
      {
        _borderProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the Border brush
    /// </summary>
    /// <value>The Border.</value>
    public Brush BorderBrush
    {
      get
      {
        return _borderProperty.GetValue() as Brush;
      }
      set
      {
        _borderProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property BorderThicknessProperty
    {
      get
      {
        return _borderThicknessProperty;
      }
      set
      {
        _borderThicknessProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public double BorderThickness
    {
      get
      {
        return (double)_borderThicknessProperty.GetValue();
      }
      set
      {
        _borderThicknessProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property CornerRadiusProperty
    {
      get
      {
        return _cornerRadiusProperty;
      }
      set
      {
        _cornerRadiusProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public double CornerRadius
    {
      get
      {
        return (double)_cornerRadiusProperty.GetValue();
      }
      set
      {
        _cornerRadiusProperty.SetValue(value);
      }
    }
    #endregion

    #region measure&arrange
    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);

      ActualPosition = new Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      IsArrangeValid = true;
      InitializeBindings();
      InitializeTriggers();
      _isLayoutInvalid = false;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      }
      if (_content != null)
      {
        PointF location = new PointF(layoutRect.Location.X, layoutRect.Location.Y);
        ArrangeContent(_content, ref location, layoutRect.Size);
        _content.Arrange(new RectangleF(location, _content.DesiredSize));
      }
    }

    protected virtual void ArrangeContent(FrameworkElement child, ref System.Drawing.PointF p, SizeF s)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {
        if (s.Width > 0)
          p.X += ((s.Width - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        if (s.Width > 0)
          p.X += (s.Width - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        if (s.Height > 0)
          p.Y += ((s.Height - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        if (s.Height > 0)
          p.Y += (s.Height - child.DesiredSize.Height);
      }
    }

    public override void Measure(SizeF availableSize)
    {
      base.Measure(availableSize);
      if (_content != null)
      {
        float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
        float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
        SizeF size = new SizeF(_desiredSize.Width - marginWidth, _desiredSize.Height - marginHeight);
        _content.Measure(size);
      }
    }
    #endregion

    #region rendering
    public override void DoRender()
    {
      if (!IsVisible) return;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        if (Background != null && _backgroundAsset != null && _backgroundAsset.IsAllocated == false)
          _performLayout = true;
        if (BorderBrush != null && _borderAsset != null && _borderAsset.IsAllocated == false)
          _performLayout = true;
        if (_performLayout)
        {
          PerformLayout();
          _performLayout = false;
        }


        SkinContext.AddOpacity(this.Opacity);
        ExtendedMatrix m = new ExtendedMatrix();
        m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
        SkinContext.AddTransform(m);
        if (Background != null)
        {
          //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          if (Background.BeginRender(_backgroundAsset.VertexBuffer, _verticesCountFill, PrimitiveType.TriangleFan))
          {
            GraphicsDevice.Device.SetStreamSource(0, _backgroundAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
            GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, _verticesCountFill);
            Background.EndRender();
          }
          _backgroundAsset.LastTimeUsed = SkinContext.Now;
        }
        if (BorderBrush != null && BorderThickness > 0)
        {
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          if (BorderBrush.BeginRender(_borderAsset.VertexBuffer, _verticesCountBorder, PrimitiveType.TriangleList))
          {
            GraphicsDevice.Device.SetStreamSource(0, _borderAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
            GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
            BorderBrush.EndRender();
          }
          _borderAsset.LastTimeUsed = SkinContext.Now;
        }
        SkinContext.RemoveTransform();
      }

      if (_content != null)
      {
        _content.DoRender();
      }
      SkinContext.RemoveOpacity();
    }

    public override void Animate()
    {
      base.Animate();
      if (_content != null)
      {
        _content.Animate();
      }
    }
    #endregion

    #region input handling
    public override void OnKeyPressed(ref MediaPortal.Core.InputManager.Key key)
    {
      base.OnKeyPressed(ref key);
      if (_content != null)
      {
        _content.OnKeyPressed(ref key);
      }
    }
    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      if (_content != null)
      {
        _content.OnMouseMove(x, y);
      }
    }
    public override void Reset()
    {
      base.Reset();
      if (_content != null)
      {
        _content.Reset();
      }
    }
    #endregion


    #region findXXX methods
    /// <summary>
    /// Fires an event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    public override void FireEvent(string eventName)
    {
      if (_content != null)
      {
        _content.FireEvent(eventName);
      }
      base.FireEvent(eventName);
    }

    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      if (_content != null)
      {
        UIElement o = _content.FindElement(name);
        if (o != null) return o;
      }
      return base.FindElement(name);
    }

    public override UIElement FindElementType(Type t)
    {
      if (_content != null)
      {
        UIElement o = _content.FindElementType(t);
        if (o != null) return o;
      }
      return base.FindElementType(t);
    }

    public override UIElement FindItemsHost()
    {
      if (_content != null)
      {
        UIElement o = _content.FindItemsHost();
        if (o != null) return o;
      }
      return base.FindItemsHost(); ;
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public override UIElement FindFocusedItem()
    {
      if (HasFocus) return this;
      if (_content != null)
      {
        UIElement o = _content.FindFocusedItem();
        if (o != null) return o;
      }
      return null;
    }
    #endregion

    #region focus prediction

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_content == null) return null;
      FrameworkElement element = ((FrameworkElement)_content).PredictFocusUp(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_content == null) return null;
      FrameworkElement element = ((FrameworkElement)_content).PredictFocusDown(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_content == null) return null;
      FrameworkElement element = ((FrameworkElement)_content).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      if (_content == null) return null;
      FrameworkElement element = ((FrameworkElement)_content).PredictFocusRight(focusedFrameworkElement, ref key, strict);
      if (element != null) return element;
      return base.PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }


    #endregion

    #region layouting
    /// <summary>
    /// Performs the layout.
    /// </summary>
    protected override void PerformLayout()
    {
      //Trace.WriteLine("Border.PerformLayout() " + this.Name);
      
      double w = ActualWidth;
      double h = ActualHeight;
      float centerX, centerY;
      SizeF rectSize = new SizeF((float)w, (float)h);

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix *= _finalLayoutTransform.Matrix;
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        m.Matrix *= em.Matrix;
      }
      m.InvertSize(ref rectSize);
      System.Drawing.RectangleF rect = new System.Drawing.RectangleF(-0.5f, -0.5f, rectSize.Width + 0.5f, rectSize.Height + 0.5f);

      PositionColored2Textured[] verts;
      GraphicsPath path;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        using (path = GetRoundedRect(rect, (float)CornerRadius))
        {
          CalcCentroid(path, out centerX, out centerY);
          if (Background != null)
          {
            if (_backgroundAsset == null)
            {
              _backgroundAsset = new VisualAssetContext("Border._backgroundAsset:" + this.Name);
              ContentManager.Add(_backgroundAsset);
            }
            _backgroundAsset.VertexBuffer = ConvertPathToTriangleFan(path, centerX, centerY, out verts);
            if (_backgroundAsset.VertexBuffer != null)
            {
              Background.SetupBrush(this, ref verts);


              PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
              _verticesCountFill = (verts.Length - 2);
            }
          }

          if (BorderBrush != null && BorderThickness > 0)
          {
            if (_borderAsset == null)
            {
              _borderAsset = new VisualAssetContext("Border._borderAsset:" + this.Name);
              ContentManager.Add(_borderAsset);
            }
            _borderAsset.VertexBuffer = ConvertPathToTriangleStrip(path, (float)BorderThickness, true, out verts, _finalLayoutTransform);
            if (_borderAsset.VertexBuffer != null)
            {
              BorderBrush.SetupBrush(this, ref verts);

              PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
              _verticesCountBorder = (verts.Length / 3);
            }

          }
        }
      }
    }


    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetRoundedRect(RectangleF baseRect, float CornerRadius)
    {
      // if corner radius is less than or equal to zero, 

      // return the original rectangle 

      if (CornerRadius <= 0.0f && CornerRadius <= 0.0f)
      {
        GraphicsPath mPath = new GraphicsPath();
        mPath.AddRectangle(baseRect);
        mPath.CloseFigure();
        System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
        m.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
        m.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
        if (LayoutTransform != null)
        {
          ExtendedMatrix em;
          LayoutTransform.GetTransform(out em);
          m.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
        }
        m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
        mPath.Transform(m);
        mPath.Flatten();
        return mPath;
      }

      // if the corner radius is greater than or equal to 

      // half the width, or height (whichever is shorter) 

      // then return a capsule instead of a lozenge 

      if (CornerRadius >= (Math.Min(baseRect.Width, baseRect.Height)) / 2.0)
        return GetCapsule(baseRect);

      // create the arc for the rectangle sides and declare 

      // a graphics path object for the drawing 

      float diameter = CornerRadius * 2.0F;
      SizeF sizeF = new SizeF(diameter, diameter);
      RectangleF arc = new RectangleF(baseRect.Location, sizeF);
      GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

      // top left arc 


      path.AddArc(arc, 180, 90);

      // top right arc 

      arc.X = baseRect.Right - diameter;
      path.AddArc(arc, 270, 90);

      // bottom right arc 

      arc.Y = baseRect.Bottom - diameter;
      path.AddArc(arc, 0, 90);

      // bottom left arc

      arc.X = baseRect.Left;
      path.AddArc(arc, 90, 90);

      path.CloseFigure();
      System.Drawing.Drawing2D.Matrix mtx = new System.Drawing.Drawing2D.Matrix();
      mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        mtx.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);

      path.Flatten();
      return path;
    }
    #endregion

    #region Gets the desired Capsular path.
    private GraphicsPath GetCapsule(RectangleF baseRect)
    {
      float diameter;
      RectangleF arc;
      GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
      try
      {
        if (baseRect.Width > baseRect.Height)
        {
          // return horizontal capsule 

          diameter = baseRect.Height;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 90, 180);
          arc.X = baseRect.Right - diameter;
          path.AddArc(arc, 270, 180);
        }
        else if (baseRect.Width < baseRect.Height)
        {
          // return vertical capsule 

          diameter = baseRect.Width;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 180, 180);
          arc.Y = baseRect.Bottom - diameter;
          path.AddArc(arc, 0, 180);
        }
        else
        {
          // return circle 

          path.AddEllipse(baseRect);
        }
      }
      catch (Exception)
      {
        path.AddEllipse(baseRect);
      }
      finally
      {
        path.CloseFigure();
      }
      System.Drawing.Drawing2D.Matrix mtx = new System.Drawing.Drawing2D.Matrix();
      mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      mtx.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        mtx.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      path.Transform(mtx);
      return path;
    }

    #endregion
    #endregion

    #region IAddChild Members

    public void AddChild(object o)
    {
      _content = (FrameworkElement)o;
      _content.VisualParent = this;
    }

    #endregion

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        this.BorderBrush.Deallocate();
      if (Background != null)
        this.Background.Deallocate();
      if (_content != null)
      {
        _content.Deallocate();
      }
      if (_borderAsset != null)
      {
        _borderAsset.Free(true);
        ContentManager.Remove(_borderAsset);
        _borderAsset = null;
      }
      if (_backgroundAsset != null)
      {
        _backgroundAsset.Free(true);
        ContentManager.Remove(_backgroundAsset);
        _backgroundAsset = null;
      }
      _performLayout = true;
    }
    public override void Allocate()
    {
      base.Allocate();
      if (BorderBrush != null)
        this.BorderBrush.Allocate();
      if (Background != null)
        this.Background.Allocate();
      if (_content != null)
      {
        _content.Allocate();
      }
    }
  }
}

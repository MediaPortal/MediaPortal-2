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
using System.Drawing.Drawing2D;
using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Brushes;
using MediaPortal.SkinEngine.DirectX.Triangulate;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.Rendering;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

// changes possible:
// - opacity
// - vertices
// - effect / effect parameters
// - rendertransform
// - visibility

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class Border : FrameworkElement, IAddChild<FrameworkElement>, IUpdateEventHandler
  {
    #region Protected fields

    protected Property _backgroundProperty;
    protected Property _borderProperty;
    protected Property _borderThicknessProperty;
    protected Property _cornerRadiusProperty;
    protected FrameworkElement _content;
    protected VisualAssetContext _backgroundAsset;
    protected int _verticesCountFill;
    protected VisualAssetContext _borderAsset;
    protected int _verticesCountBorder;
    protected PrimitiveContext _backgroundContext;
    protected PrimitiveContext _borderContext;
    protected UIEvent _lastEvent = UIEvent.None;
    protected bool _performLayout;
    
    #endregion

    #region Ctor

    public Border()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _borderProperty = new Property(typeof(Brush), null);
      _backgroundProperty = new Property(typeof(Brush), null);
      _borderThicknessProperty = new Property(typeof(double), 1.0);
      _cornerRadiusProperty = new Property(typeof(double), 0.0);
    }

    void Attach()
    {
      _borderProperty.Attach(OnBorderBrushPropertyChanged);
      _backgroundProperty.Attach(OnBackgroundBrushPropertyChanged);
      _borderThicknessProperty.Attach(OnLayoutPropertyChanged);
      _cornerRadiusProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _borderProperty.Detach(OnBorderBrushPropertyChanged);
      _backgroundProperty.Detach(OnBackgroundBrushPropertyChanged);
      _borderThicknessProperty.Detach(OnLayoutPropertyChanged);
      _cornerRadiusProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Border b = (Border) source;
      BorderBrush = copyManager.GetCopy(b.BorderBrush);
      Background = copyManager.GetCopy(b.Background);
      BorderThickness = copyManager.GetCopy(b.BorderThickness);
      CornerRadius = copyManager.GetCopy(b.CornerRadius);
      _content = copyManager.GetCopy(b._content);

      Attach();
    }

    #endregion

    #region Change event handlers

    void OnBackgroundBrushPropertyChanged(Property property, object oldValue)
    {
      Brush oldBrush = oldValue as Brush;
      if (oldBrush != null)
        oldBrush.ObjectChanged -= OnBackgroundBrushChanged;
      Brush brush = property.GetValue() as Brush;
      if (brush != null)
        brush.ObjectChanged += OnBackgroundBrushChanged;
    }

    void OnBorderBrushPropertyChanged(Property property, object oldValue)
    {
      Brush oldBrush = oldValue as Brush;
      if (oldBrush != null)
        oldBrush.ObjectChanged -= OnBorderBrushChanged;
      Brush brush = property.GetValue() as Brush;
      if (brush != null)
        brush.ObjectChanged += OnBorderBrushChanged;
    }

    void OnBackgroundBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.FillChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnBorderBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.StrokeChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnLayoutPropertyChanged(Property property, object oldValue)
    {
      _performLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    #endregion

    #region Properties

    public Property BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return _backgroundProperty.GetValue() as Brush; }
      set { _backgroundProperty.SetValue(value); }
    }

    public Property BorderBrushProperty
    {
      get { return _borderProperty; }
      set { _borderProperty = value; }
    }

    public Brush BorderBrush
    {
      get { return _borderProperty.GetValue() as Brush; }
      set { _borderProperty.SetValue(value); }
    }

    public Property BorderThicknessProperty
    {
      get { return _borderThicknessProperty; }
    }

    public double BorderThickness
    {
      get { return (double)_borderThicknessProperty.GetValue(); }
      set { _borderThicknessProperty.SetValue(value); }
    }

    public Property CornerRadiusProperty
    {
      get { return _cornerRadiusProperty; }
    }

    public double CornerRadius
    {
      get { return (double)_cornerRadiusProperty.GetValue(); }
      set { _cornerRadiusProperty.SetValue(value); }
    }

    #endregion

    #region Measure & Arrange

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height;

      SizeF childSize;
      if (_content != null && _content.IsVisible)
      {
         childSize = new SizeF(totalSize.Width, totalSize.Height);
        _content.Measure(ref childSize);
      }
      else
        childSize = new SizeF();

      float borderSize = (float) Math.Max(BorderThickness * 2, CornerRadius * 2);

      childSize.Width += borderSize;
      childSize.Height += borderSize;

      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;

      if (double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("Border.Measure: {0} returns {1}x{2}", Name, (int) totalSize.Width, (int) totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      RemoveMargin(ref finalRect);
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      Initialize();
      InitializeTriggers();
      IsInvalidLayout = false;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      }
      if (_content != null)
      {
        float borderInset = (float) Math.Max(BorderThickness, CornerRadius);
        PointF location = new PointF(layoutRect.Location.X + borderInset, layoutRect.Location.Y + borderInset);
        SizeF size = new SizeF(layoutRect.Width - borderInset * 2,
            layoutRect.Height - borderInset * 2);
        ArrangeChild(_content, ref location, ref size);
        _content.Arrange(new RectangleF(location, size));
      }
    }

    #endregion

    #region Rendering

    void SetupBrush(UIEvent uiEvent)
    {
      if ((uiEvent & UIEvent.OpacityChange) != 0 || (uiEvent & UIEvent.FillChange) != 0)
      {
        if (Background != null && _backgroundContext != null)
        {
          RenderPipeline.Instance.Remove(_backgroundContext);
          Background.SetupPrimitive(_backgroundContext);
          RenderPipeline.Instance.Add(_backgroundContext);
        }
      }

      if ((uiEvent & UIEvent.OpacityChange) != 0 || (uiEvent & UIEvent.StrokeChange) != 0)
      {
        if (BorderBrush != null && _borderContext != null)
        {
          RenderPipeline.Instance.Remove(_borderContext);
          BorderBrush.SetupPrimitive(_borderContext);
          RenderPipeline.Instance.Add(_borderContext);
        }
      }
    }

    public void Update()
    {
      UpdateLayout();
      if (_performLayout)
      {
        PerformLayout();
        _lastEvent = UIEvent.None;
      }
      else if (_lastEvent != UIEvent.None)
      {
        if ((_lastEvent & UIEvent.Hidden) != 0)
        {
          RenderPipeline.Instance.Remove(_backgroundContext);
          RenderPipeline.Instance.Remove(_borderContext);
          _backgroundContext = null;
          _borderContext = null;
          _performLayout = true;
        }
        else
          SetupBrush(_lastEvent);
        _lastEvent = UIEvent.None;
      }
    }

    public override void DoRender()
    {
      if (!IsVisible) return;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        if (Background != null && _backgroundAsset != null && _backgroundAsset.IsAllocated == false)
          _performLayout = true;
        if (BorderBrush != null && _borderAsset != null && _borderAsset.IsAllocated == false)
          _performLayout = true;
        PerformLayout();
        SkinContext.AddOpacity(Opacity);
        //ExtendedMatrix m = new ExtendedMatrix();
        //m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
        //SkinContext.AddTransform(m);
        if (Background != null)
        {
          //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
          //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
          if (Background.BeginRender(_backgroundAsset.VertexBuffer, _verticesCountFill, PrimitiveType.TriangleList))
          {
            GraphicsDevice.Device.SetStreamSource(0, _backgroundAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
            GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountFill);
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
        //SkinContext.RemoveTransform();
        SkinContext.RemoveOpacity();
      }

      if (_content != null)
      {
        SkinContext.AddOpacity(Opacity);
        _content.Render();
        SkinContext.RemoveOpacity();
      }
    }

    #endregion

    #region Input handling

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      base.FireUIEvent(eventType, source);

      if (SkinContext.UseBatching)
      {
        _lastEvent |= eventType;
        if (Screen != null) Screen.Invalidate(this);
      }
    }

    #endregion

    #region Children handling

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      if (_content != null)
        childrenOut.Add(_content);
    }

    #endregion

    #region Layouting

    protected virtual void PerformLayout()
    {
      if (!_performLayout)
        return;
      //Trace.WriteLine("Border.PerformLayout() " + Name);

      double w = ActualWidth;
      double h = ActualHeight;
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
      RectangleF rect = new RectangleF(-0.5f, -0.5f, rectSize.Width + 0.5f, rectSize.Height + 0.5f);
      rect.X += ActualPosition.X;
      rect.Y += ActualPosition.Y;
      PositionColored2Textured[] verts;
      if (Background != null || (BorderBrush != null && BorderThickness > 0))
      {
        GraphicsPath path;
        using (path = GetBorderRect(rect))
        {
          float centerX, centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          if (Background != null)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_backgroundAsset == null)
              {
                _backgroundAsset = new VisualAssetContext("Border._backgroundAsset:" + Name);
                ContentManager.Add(_backgroundAsset);
              }
              TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
              _backgroundAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
              if (_backgroundAsset.VertexBuffer != null)
              {
                Background.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
                _verticesCountFill = (verts.Length / 3);

              }
            }
            else
            {
              TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
              _verticesCountFill = (verts.Length / 3);
              Background.SetupBrush(this, ref verts);
              if (_backgroundContext == null)
              {
                _backgroundContext = new PrimitiveContext(_verticesCountFill, ref verts);
                Background.SetupPrimitive(_backgroundContext);
                RenderPipeline.Instance.Add(_backgroundContext);
              }
              else
                _backgroundContext.OnVerticesChanged(_verticesCountFill, ref verts);
            }
          }

          if (BorderBrush != null && BorderThickness > 0)
          {
            if (SkinContext.UseBatching == false)
            {
              if (_borderAsset == null)
              {
                _borderAsset = new VisualAssetContext("Border._borderAsset:" + Name);
                ContentManager.Add(_borderAsset);
              }
              TriangulateHelper.TriangulateStroke_TriangleList(path, (float)BorderThickness, true, out verts, _finalLayoutTransform, false);
              if (verts != null)
              {
                _borderAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
                BorderBrush.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
                _verticesCountBorder = (verts.Length / 3);
              }
            }
            else
            {
              TriangulateHelper.TriangulateStroke_TriangleList(path, (float)BorderThickness, true, out verts, _finalLayoutTransform);
              BorderBrush.SetupBrush(this, ref verts);
              _verticesCountBorder = (verts.Length / 3);
              if (_borderContext == null)
              {
                _borderContext = new PrimitiveContext(_verticesCountBorder, ref verts);
                BorderBrush.SetupPrimitive(_borderContext);
                RenderPipeline.Instance.Add(_borderContext);
              }
              else
                _borderContext.OnVerticesChanged(_verticesCountBorder, ref verts);
            }
          }
        }
      }
    }

    protected GraphicsPath GetBorderRect(RectangleF baseRect)
    {
      ExtendedMatrix layoutTransform = _finalLayoutTransform ?? new ExtendedMatrix();
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        layoutTransform = layoutTransform.Multiply(em);
      }
      return GraphicsPathHelper.CreateRoundedRectPath(baseRect, (float) CornerRadius, (float) CornerRadius, layoutTransform);
    }

    #endregion

    #region IAddChild Members

    public void AddChild(FrameworkElement o)
    {
      _content = o;
      _content.VisualParent = this;
    }

    #endregion

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        BorderBrush.Deallocate();
      if (Background != null)
        Background.Deallocate();
      if (_content != null)
        _content.Deallocate();
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
      if (_backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        _backgroundContext = null;
      }
      if (_borderContext != null)
      {
        RenderPipeline.Instance.Remove(_borderContext);
        _borderContext = null;
      }
    }

    public override void Allocate()
    {
      base.Allocate();
      if (BorderBrush != null)
        BorderBrush.Allocate();
      if (Background != null)
        Background.Allocate();
      if (_content != null)
        _content.Allocate();
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      PerformLayout();
      _lastEvent = UIEvent.None;
      if (_content != null)
        _content.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      if (_backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        _backgroundContext = null;
      }
      if (_borderContext != null)
      {
        RenderPipeline.Instance.Remove(_borderContext);
        _borderContext = null;
      }
      if (_content != null)
        _content.DestroyRenderTree();
    }
  }
}

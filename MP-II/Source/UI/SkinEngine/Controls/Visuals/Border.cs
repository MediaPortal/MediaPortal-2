#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

// changes possible:
// - opacity
// - vertices
// - effect / effect parameters
// - rendertransform
// - visibility

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Border : FrameworkElement, IAddChild<FrameworkElement>, IUpdateEventHandler
  {
    #region Protected fields

    protected AbstractProperty _backgroundProperty;
    protected AbstractProperty _borderProperty;
    protected AbstractProperty _borderThicknessProperty;
    protected AbstractProperty _cornerRadiusProperty;
    protected FrameworkElement _content;
    protected VisualAssetContext _backgroundAsset;
    protected int _verticesCountFill;
    protected VisualAssetContext _borderAsset;
    protected int _verticesCountBorder;
    protected PrimitiveContext _backgroundContext;
    protected PrimitiveContext _borderContext;
    protected UIEvent _lastEvent = UIEvent.None;
    protected bool _performLayout;
    protected RectangleF _borderRect;
    
    #endregion

    #region Ctor

    public Border()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _borderProperty = new SProperty(typeof(Brush), null);
      _backgroundProperty = new SProperty(typeof(Brush), null);
      _borderThicknessProperty = new SProperty(typeof(double), 1.0);
      _cornerRadiusProperty = new SProperty(typeof(double), 0.0);
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
      BorderThickness = b.BorderThickness;
      CornerRadius = b.CornerRadius;
      _content = copyManager.GetCopy(b._content);

      Attach();
    }

    #endregion

    #region Change event handlers

    void OnBackgroundBrushPropertyChanged(AbstractProperty property, object oldValue)
    {
      Brush oldBrush = oldValue as Brush;
      if (oldBrush != null)
        oldBrush.ObjectChanged -= OnBackgroundBrushChanged;
      Brush brush = property.GetValue() as Brush;
      if (brush != null)
        brush.ObjectChanged += OnBackgroundBrushChanged;
      OnBackgroundBrushChanged(brush);
    }

    void OnBorderBrushPropertyChanged(AbstractProperty property, object oldValue)
    {
      Brush oldBrush = oldValue as Brush;
      if (oldBrush != null)
        oldBrush.ObjectChanged -= OnBorderBrushChanged;
      Brush brush = property.GetValue() as Brush;
      if (brush != null)
        brush.ObjectChanged += OnBorderBrushChanged;
      OnBorderBrushChanged(brush);
    }

    void OnBackgroundBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.FillChange;
      if (Screen != null) Screen.Invalidate(this);
      _performLayout = true;
    }

    void OnBorderBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.StrokeChange;
      if (Screen != null) Screen.Invalidate(this);
      _performLayout = true;
    }

    void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      _performLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    #endregion

    #region Properties

    public AbstractProperty BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return _backgroundProperty.GetValue() as Brush; }
      set { _backgroundProperty.SetValue(value); }
    }

    public AbstractProperty BorderBrushProperty
    {
      get { return _borderProperty; }
      set { _borderProperty = value; }
    }

    public Brush BorderBrush
    {
      get { return _borderProperty.GetValue() as Brush; }
      set { _borderProperty.SetValue(value); }
    }

    public AbstractProperty BorderThicknessProperty
    {
      get { return _borderThicknessProperty; }
    }

    public double BorderThickness
    {
      get { return (double) _borderThicknessProperty.GetValue(); }
      set { _borderThicknessProperty.SetValue(value); }
    }

    public AbstractProperty CornerRadiusProperty
    {
      get { return _cornerRadiusProperty; }
    }

    public double CornerRadius
    {
      get { return (double) _cornerRadiusProperty.GetValue(); }
      set { _cornerRadiusProperty.SetValue(value); }
    }

    #endregion

    #region Measure & Arrange

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      MeasureBorder(totalSize);

      Thickness borderMargin = GetTotalBorderMargin();
      RemoveMargin(ref totalSize, borderMargin);

      if (_content != null && _content.IsVisible)
        _content.Measure(ref totalSize);
      else
        totalSize = new SizeF();

      AddMargin(ref totalSize, borderMargin);

      return totalSize;
    }

    protected override void ArrangeOverride(RectangleF finalRect)
    {
      float oldPosX = ActualPosition.X;
      float oldPosY = ActualPosition.Y;
      float oldWidth = _finalRect.Width;
      float oldHeight= _finalRect.Height;
      base.ArrangeOverride(finalRect);

      ArrangeBorder(finalRect);

      if (!finalRect.IsEmpty &&
          (oldPosX != finalRect.X || oldPosY != finalRect.Y ||
           oldWidth != finalRect.Width || oldHeight != finalRect.Height))
        _performLayout = true;
      _finalRect = finalRect;

      if (_content == null)
        return;
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      RemoveMargin(ref layoutRect, GetTotalBorderMargin());
      PointF location = new PointF(layoutRect.Location.X, layoutRect.Location.Y);
      SizeF size = new SizeF(layoutRect.Size);
      ArrangeChild(_content, ref location, ref size);
      _content.Arrange(new RectangleF(location, size));
    }

    /// <summary>
    /// Gets the size needed for this element's border in total. Will be subtracted from the total available area
    /// when our content will be layouted. The returned value is not zoomed by <see cref="SkinContext.Zoom"/>.
    /// </summary>
    protected virtual Thickness GetTotalBorderMargin()
    {
      float borderInsetsX = GetBorderInsetX()*2;
      float borderInsetsY = GetBorderInsetY()*2;
      return new Thickness(borderInsetsX, borderInsetsY, borderInsetsX, borderInsetsY);
    }

    protected virtual void MeasureBorder(SizeF totalSize)
    {
      // Used in subclasses to measure border elements
    }

    protected virtual void ArrangeBorder(RectangleF finalRect)
    {
      _borderRect = new RectangleF(finalRect.Location, finalRect.Size);
    }

    protected float GetBorderInsetX()
    {
      return (float) Math.Max(BorderThickness, CornerRadius);
    }

    protected float GetBorderInsetY()
    {
      return (float) Math.Max(BorderThickness, CornerRadius);
    }

    #endregion

    #region Layouting

    protected virtual void PerformLayout()
    {
      if (!_performLayout)
        return;
      _performLayout = false;
      //Trace.WriteLine("Border.PerformLayout() " + Name);

      SizeF rectSize = new SizeF(_borderRect.Size);

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
      PerformLayoutBackground(rect);
      PerformLayoutBorder(rect);
    }

    protected void PerformLayoutBackground(RectangleF rect)
    {
      if (Background != null)
        using (GraphicsPath path = CreateBorderRectPath(rect))
        {
          // Some backgrounds might not be closed (subclasses sometimes create open background shapes,
          // for example GroupBox). To create a completely filled background, we need a closed figure.
          path.CloseFigure();
          PositionColored2Textured[] verts;
          float centerX, centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          if (SkinContext.UseBatching)
          {
            TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
            _verticesCountFill = verts.Length / 3;
            Background.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);
            if (_backgroundContext == null)
            {
              _backgroundContext = new PrimitiveContext(_verticesCountFill, ref verts);
              Background.SetupPrimitive(_backgroundContext);
              RenderPipeline.Instance.Add(_backgroundContext);
            }
            else
              _backgroundContext.OnVerticesChanged(_verticesCountFill, ref verts);
          }
          else
          {
            if (_backgroundAsset == null)
            {
              _backgroundAsset = new VisualAssetContext("Border._backgroundAsset:" + Name, Screen.Name);
              ContentManager.Add(_backgroundAsset);
            }
            TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
            if (verts != null)
            {
              _backgroundAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
              Background.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);

              PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
              _verticesCountFill = verts.Length / 3;

            }
          }
        }
    }

    protected void PerformLayoutBorder(RectangleF rect)
    {
      if (BorderBrush != null && BorderThickness > 0)
        using (GraphicsPath path = CreateBorderRectPath(rect))
        {
          GraphicsPathIterator gpi = new GraphicsPathIterator(path);
          PositionColored2Textured[][] subPathVerts = new PositionColored2Textured[gpi.SubpathCount][];
          GraphicsPath subPath = new GraphicsPath();
          for (int i = 0; i < subPathVerts.Length; i++)
          {
            bool isClosed;
            gpi.NextSubpath(subPath, out isClosed);
            TriangulateHelper.TriangulateStroke_TriangleList(path, (float) BorderThickness, isClosed,
                out subPathVerts[i], _finalLayoutTransform);
          }
          PositionColored2Textured[] verts;
          GraphicsPathHelper.Flatten(subPathVerts, out verts);
          if (SkinContext.UseBatching)
          {
            BorderBrush.SetupBrush(_borderRect, FinalLayoutTransform, ActualPosition.Z, ref verts);
            _verticesCountBorder = verts.Length / 3;
            if (_borderContext == null)
            {
              _borderContext = new PrimitiveContext(_verticesCountBorder, ref verts);
              BorderBrush.SetupPrimitive(_borderContext);
              RenderPipeline.Instance.Add(_borderContext);
            }
            else
              _borderContext.OnVerticesChanged(_verticesCountBorder, ref verts);
          }
          else
          {
            if (_borderAsset == null)
            {
              _borderAsset = new VisualAssetContext("Border._borderAsset:" + Name, Screen.Name);
              ContentManager.Add(_borderAsset);
            }
            _borderAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
            BorderBrush.SetupBrush(_borderRect, FinalLayoutTransform, ActualPosition.Z, ref verts);

            PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
            _verticesCountBorder = verts.Length / 3;
          }
        }
    }

    protected virtual GraphicsPath CreateBorderRectPath(RectangleF baseRect)
    {
      ExtendedMatrix layoutTransform = _finalLayoutTransform ?? new ExtendedMatrix();
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        layoutTransform = layoutTransform.Multiply(em);
      }
      return GraphicsPathHelper.CreateRoundedRectPath(baseRect,
          (float) CornerRadius * SkinContext.Zoom.Width, (float) CornerRadius * SkinContext.Zoom.Width, layoutTransform);
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

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        BorderBrush.Deallocate();
      if (Background != null)
        Background.Deallocate();
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

    #region Children handling

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      if (_content != null)
        childrenOut.Add(_content);
    }

    #endregion

    #region IAddChild Members

    public void AddChild(FrameworkElement o)
    {
      _content = o;
      _content.VisualParent = this;
    }

    #endregion
  }
}

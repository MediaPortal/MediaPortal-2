#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.MpfElements;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Border : FrameworkElement, IAddChild<FrameworkElement>
  {
    #region Protected fields

    protected AbstractProperty _backgroundProperty;
    protected AbstractProperty _borderBrushProperty;
    protected AbstractProperty _borderThicknessProperty;
    protected AbstractProperty _cornerRadiusProperty;
    protected AbstractProperty _contentProperty;
    protected int _verticesCountBorder;
    protected PrimitiveBuffer _backgroundContext;
    protected PrimitiveBuffer _borderContext;
    protected bool _performLayout;
    protected RectangleF _borderRect;
    protected FrameworkElement _initializedContent = null; // We need to cache the Content because after it was set, it first needs to be initialized before it can be used

    #endregion

    #region Ctor

    public Border()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _borderBrushProperty = new SProperty(typeof(Brush), null);
      _backgroundProperty = new SProperty(typeof(Brush), null);
      _borderThicknessProperty = new SProperty(typeof(double), 1.0);
      _cornerRadiusProperty = new SProperty(typeof(double), 0.0);
      _contentProperty = new SProperty(typeof(FrameworkElement), null);
    }

    void Attach()
    {
      _borderBrushProperty.Attach(OnBorderBrushPropertyChanged);
      _backgroundProperty.Attach(OnBackgroundBrushPropertyChanged);
      _borderThicknessProperty.Attach(OnLayoutPropertyChanged);
      _cornerRadiusProperty.Attach(OnLayoutPropertyChanged);
      _contentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _borderBrushProperty.Detach(OnBorderBrushPropertyChanged);
      _backgroundProperty.Detach(OnBackgroundBrushPropertyChanged);
      _borderThicknessProperty.Detach(OnLayoutPropertyChanged);
      _cornerRadiusProperty.Detach(OnLayoutPropertyChanged);
      _contentProperty.Detach(OnContentChanged);
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
      Content = copyManager.GetCopy(b.Content);
      _initializedContent = copyManager.GetCopy(b._initializedContent);

      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Background);
      MPF.TryCleanupAndDispose(BorderBrush);
      base.Dispose();
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
      _performLayout = true;
    }

    void OnBorderBrushChanged(IObservable observable)
    {
      _performLayout = true;
    }

    void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      _performLayout = true;
    }

    void OnContentChanged(AbstractProperty property, object oldValue)
    {
      FrameworkElement oldContent = oldValue as FrameworkElement;
      if (oldContent != null)
        oldContent.CleanupAndDispose();

      FrameworkElement content = Content;
      if (content != null)
      {
        content.VisualParent = this;
        content.SetElementState(_elementState);
        if (IsAllocated)
          content.Allocate();
      }
      _initializedContent = content;
      InvalidateLayout(true, true);
    }

    #endregion

    #region Properties

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public FrameworkElement Content
    {
      get { return (FrameworkElement) _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

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
      get { return _borderBrushProperty; }
      set { _borderBrushProperty = value; }
    }

    public Brush BorderBrush
    {
      get { return _borderBrushProperty.GetValue() as Brush; }
      set { _borderBrushProperty.SetValue(value); }
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

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      MeasureBorder(totalSize);

      Thickness borderMargin = GetTotalBorderMargin();
      RemoveMargin(ref totalSize, borderMargin);

      FrameworkElement content = _initializedContent;
      if (content != null && content.IsVisible)
        content.Measure(ref totalSize);
      else
        totalSize = SizeF.Empty;

      AddMargin(ref totalSize, borderMargin);

      return totalSize;
    }

    protected override void ArrangeOverride()
    {
      _performLayout = true;
      base.ArrangeOverride();
      ArrangeBorder(_innerRect);
      FrameworkElement content = _initializedContent;
      if (content == null)
        return;
      RectangleF layoutRect = new RectangleF(_innerRect.X, _innerRect.Y, _innerRect.Width, _innerRect.Height);
      RemoveMargin(ref layoutRect, GetTotalBorderMargin());
      PointF location = new PointF(layoutRect.Location.X, layoutRect.Location.Y);
      SizeF size = new SizeF(layoutRect.Size);
      ArrangeChild(content, content.HorizontalAlignment, content.VerticalAlignment, ref location, ref size);
      content.Arrange(new RectangleF(location, size));
    }

    /// <summary>
    /// Gets the size needed for this element's border in total. Will be subtracted from the total available area
    /// when our content will be layouted.
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

    protected virtual void PerformLayout(RenderContext context)
    {
      if (!_performLayout)
        return;
      _performLayout = false;

      RectangleF rect = new RectangleF(-0.5f, -0.5f, _borderRect.Size.Width + 0.5f, _borderRect.Size.Height + 0.5f);
      rect.X += ActualPosition.X;
      rect.Y += ActualPosition.Y;
      PerformLayoutBackground(rect, context);
      PerformLayoutBorder(rect, context);
    }

    protected void PerformLayoutBackground(RectangleF rect, RenderContext context)
    {
      // Setup background brush
      if (Background != null)
      {
        // TODO: Draw background only in the inner rectangle (outer rect minus BorderThickness)
        using (GraphicsPath path = CreateBorderRectPath(rect))
        {
          // Some backgrounds might not be closed (subclasses sometimes create open background shapes,
          // for example GroupBox). To create a completely filled background, we need a closed figure.
          path.CloseFigure();
          PositionColoredTextured[] verts;
          float centerX, centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);

          Background.SetupBrush(this, ref verts, context.ZOrder, true);
          PrimitiveBuffer.SetPrimitiveBuffer(ref _backgroundContext, ref verts, PrimitiveType.TriangleList);
        }
      }
      else
        PrimitiveBuffer.DisposePrimitiveBuffer(ref _backgroundContext);
    }

    protected void PerformLayoutBorder(RectangleF rect, RenderContext context)
    {
      // Setup border brush
      if (BorderBrush != null && BorderThickness > 0)
      {
        // TODO: Draw border with thickness BorderThickness - doesn't work yet, the drawn line is only one pixel thick
        using (GraphicsPath path = CreateBorderRectPath(rect))
        {
          using (GraphicsPathIterator gpi = new GraphicsPathIterator(path))
          {
            PositionColoredTextured[][] subPathVerts = new PositionColoredTextured[gpi.SubpathCount][];
            using (GraphicsPath subPath = new GraphicsPath())
            {
              for (int i = 0; i < subPathVerts.Length; i++)
              {
                bool isClosed;
                gpi.NextSubpath(subPath, out isClosed);
                TriangulateHelper.TriangulateStroke_TriangleList(subPath, (float) BorderThickness, isClosed,
                    out subPathVerts[i], null);
              }
            }
            PositionColoredTextured[] verts;
            GraphicsPathHelper.Flatten(subPathVerts, out verts);
            BorderBrush.SetupBrush(this, ref verts, context.ZOrder, true);

            PrimitiveBuffer.SetPrimitiveBuffer(ref _borderContext, ref verts, PrimitiveType.TriangleList);
          }
        }
      }
      else
        PrimitiveBuffer.DisposePrimitiveBuffer(ref _borderContext);
    }

    protected virtual GraphicsPath CreateBorderRectPath(RectangleF baseRect)
    {
      return GraphicsPathHelper.CreateRoundedRectPath(baseRect, (float) CornerRadius, (float) CornerRadius);
    }

    #endregion

    #region Rendering

    public override void DoRender(RenderContext localRenderContext)
    {
      PerformLayout(localRenderContext);

      if (_backgroundContext != null)
        if (Background.BeginRenderBrush(_backgroundContext, localRenderContext))
        {
          _backgroundContext.Render(0);
          Background.EndRender();
        }

      if (_borderContext != null)
        if (BorderBrush.BeginRenderBrush(_borderContext, localRenderContext))
        {
          _borderContext.Render(0);
          BorderBrush.EndRender();
        }

      FrameworkElement content = _initializedContent;
      if (content != null)
        content.Render(localRenderContext);
    }

    #endregion

    // Allocation/Deallocation of _initializedContent not necessary because UIElement handles all direct children

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        BorderBrush.Deallocate();
      if (Background != null)
        Background.Deallocate();
      _performLayout = true;
      PrimitiveBuffer.DisposePrimitiveBuffer(ref _backgroundContext);
      PrimitiveBuffer.DisposePrimitiveBuffer(ref _borderContext);
    }

    public override void Allocate()
    {
      base.Allocate();
      if (BorderBrush != null)
        BorderBrush.Allocate();
      if (Background != null)
        Background.Allocate();
    }

    #region Children handling

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement content = _initializedContent;
      if (content != null)
        childrenOut.Add(content);
    }

    #endregion

    #region IAddChild<FrameworkElement> implementation

    public void AddChild(FrameworkElement o)
    {
      Content = o;
    }

    #endregion
  }
}

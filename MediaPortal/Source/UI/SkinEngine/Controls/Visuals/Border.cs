#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Windows.Markup;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.MpfElements;
using SharpDX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using Brush = MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  [ContentProperty("Content")]
  public class Border : FrameworkElement, IAddChild<FrameworkElement>
  {
    #region Protected fields

    protected AbstractProperty _backgroundProperty;
    protected AbstractProperty _borderBrushProperty;
    protected AbstractProperty _borderThicknessProperty;
    protected AbstractProperty _borderLineJoinProperty;
    protected AbstractProperty _cornerRadiusProperty;
    protected AbstractProperty _contentProperty;
    protected bool _performLayout;
    protected RawRectangleF _outerBorderRect;
    protected FrameworkElement _initializedContent = null; // We need to cache the Content because after it was set, it first needs to be initialized before it can be used
    protected SharpDX.Direct2D1.Geometry _backgroundGeometry;
    protected SharpDX.Direct2D1.Geometry _borderGeometry;
    protected StrokeStyle _strokeStyle;
    protected RawRectangleF _strokeRect;

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
      _borderLineJoinProperty = new SProperty(typeof(LineJoin), LineJoin.Miter);
      _cornerRadiusProperty = new SProperty(typeof(double), 0.0);
      _contentProperty = new SProperty(typeof(FrameworkElement), null);
    }

    void Attach()
    {
      _borderBrushProperty.Attach(OnBorderBrushPropertyChanged);
      _backgroundProperty.Attach(OnBackgroundBrushPropertyChanged);
      _borderThicknessProperty.Attach(OnLayoutPropertyChanged);
      _borderLineJoinProperty.Attach(OnBorderLineJoinChanged);
      _cornerRadiusProperty.Attach(OnLayoutPropertyChanged);
      _contentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _borderBrushProperty.Detach(OnBorderBrushPropertyChanged);
      _backgroundProperty.Detach(OnBackgroundBrushPropertyChanged);
      _borderThicknessProperty.Detach(OnLayoutPropertyChanged);
      _borderLineJoinProperty.Detach(OnBorderLineJoinChanged);
      _cornerRadiusProperty.Detach(OnLayoutPropertyChanged);
      _contentProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Border b = (Border)source;
      BorderBrush = copyManager.GetCopy(b.BorderBrush);
      Background = copyManager.GetCopy(b.Background);
      BorderThickness = b.BorderThickness;
      BorderLineJoin = b.BorderLineJoin;
      CornerRadius = b.CornerRadius;
      Content = copyManager.GetCopy(b.Content);
      _initializedContent = copyManager.GetCopy(b._initializedContent);
      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Background);
      MPF.TryCleanupAndDispose(BorderBrush);
      TryDispose(ref _backgroundGeometry);
      TryDispose(ref _borderGeometry);
      TryDispose(ref _strokeStyle);
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

    void OnBorderLineJoinChanged(AbstractProperty property, object oldValue)
    {
      _performLayout = true;
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
        content.SetScreen(Screen);
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
      get { return (FrameworkElement)_contentProperty.GetValue(); }
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
      get { return (double)_borderThicknessProperty.GetValue(); }
      set { _borderThicknessProperty.SetValue(value); }
    }

    public AbstractProperty BorderLineJoinProperty
    {
      get { return _borderLineJoinProperty; }
    }

    public LineJoin BorderLineJoin
    {
      get { return (LineJoin)_borderLineJoinProperty.GetValue(); }
      set { _borderLineJoinProperty.SetValue(value); }
    }

    public AbstractProperty CornerRadiusProperty
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

    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      MeasureBorder(totalSize);

      Thickness enclosingMargin = GetTotalEnclosingMargin();
      RemoveMargin(ref totalSize, enclosingMargin);

      FrameworkElement content = _initializedContent;
      if (content != null && content.IsVisible)
        content.Measure(ref totalSize);
      else
        totalSize = new Size2F();

      AddMargin(ref totalSize, enclosingMargin);

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
      RectangleF layoutRect = _innerRect.ToRectangleF();
      RemoveMargin(ref layoutRect, GetTotalEnclosingMargin());
      Vector2 location = new Vector2(layoutRect.Location.X, layoutRect.Location.Y);
      Size2F size = layoutRect.Size;
      ArrangeChild(content, content.HorizontalAlignment, content.VerticalAlignment, ref location, ref size);
      content.Arrange(new RectangleF(location.X, location.Y, size.Width, size.Height));
    }

    /// <summary>
    /// Gets the size needed for this element's border/title in total. Will be subtracted from the total available area
    /// when our content is layouted.
    /// </summary>
    protected virtual Thickness GetTotalEnclosingMargin()
    {
      float borderInsetX = GetBorderCornerInsetX();
      float borderInsetY = GetBorderCornerInsetY();
      return new Thickness(borderInsetX, borderInsetY, borderInsetX, borderInsetY);
    }

    protected virtual void MeasureBorder(Size2F totalSize)
    {
      // Used in subclasses to measure border elements
    }

    protected virtual void ArrangeBorder(RawRectangleF finalRect)
    {
      _outerBorderRect = finalRect;
    }

    protected float GetBorderCornerInsetX()
    {
      return (float)Math.Max(BorderThickness, CornerRadius);
    }

    protected float GetBorderCornerInsetY()
    {
      return (float)Math.Max(BorderThickness, CornerRadius);
    }

    #endregion

    #region Layouting

    protected virtual void PerformLayout(RenderContext context)
    {
      if (!_performLayout)
        return;
      _performLayout = false;

      float borderThickness = (float)BorderThickness;
      RectangleF outerBorderRect = _outerBorderRect.ToRectangleF();
      RectangleF innerBorderRect = new RectangleF(outerBorderRect.X + borderThickness /*-0.5f*/, outerBorderRect.Y + borderThickness /*-0.5f*/,
          outerBorderRect.Size.Width - 2*borderThickness /*+ 0.5f*/, outerBorderRect.Size.Height - 2*borderThickness /*+ 0.5f*/);
      PerformLayoutBackground(innerBorderRect, context);
      PerformLayoutBorder(innerBorderRect, context);

      var fill = Background;
      if (fill != null)
        fill.SetupBrush(this, ref _innerRect, context.ZOrder, true);

      var stroke = BorderBrush;
      if (stroke != null)
        stroke.SetupBrush(this, ref _strokeRect, context.ZOrder, true);

      ReCreateStrokeStyle();
    }

    protected void PerformLayoutBackground(RectangleF innerBorderRect, RenderContext context)
    {
      // Setup background brush
      if (Background != null)
        _backgroundGeometry = CreateBackgroundRectPath(innerBorderRect);
      else
        TryDispose(ref _backgroundGeometry);
    }

    protected void PerformLayoutBorder(RectangleF innerBorderRect, RenderContext context)
    {
      // Setup background brush
      if (BorderBrush != null && BorderThickness > 0)
      {
        // Adjust border to outline of background, otherwise the stroke is centered
        innerBorderRect.X -= (float)BorderThickness / 2;
        innerBorderRect.Y -= (float)BorderThickness / 2;
        innerBorderRect.Width += (float)BorderThickness;
        innerBorderRect.Height += (float)BorderThickness;
        _borderGeometry = CreateBorderRectPath(innerBorderRect);
        _strokeRect = _borderGeometry.GetWidenedBounds((float)BorderThickness);
      }
      else
      {
        TryDispose(ref _borderGeometry);
        _strokeRect = innerBorderRect;
      }
    }

    protected virtual SharpDX.Direct2D1.Geometry CreateBackgroundRectPath(RectangleF innerBorderRect)
    {
      return GraphicsPathHelper.CreateRoundedRectPath(innerBorderRect, (float)CornerRadius, (float)CornerRadius);
    }

    protected virtual SharpDX.Direct2D1.Geometry CreateBorderRectPath(RectangleF innerBorderRect)
    {
      return GraphicsPathHelper.CreateRoundedRectPath(innerBorderRect, (float)CornerRadius, (float)CornerRadius);
    }

    #endregion

    #region Rendering

    public override void RenderOverride(RenderContext localRenderContext)
    {
      PerformLayout(localRenderContext);

      var background = Background;
      if (background != null && background.RenderBrush(localRenderContext) && _backgroundGeometry != null && background.TryAllocate())
      {
        GraphicsDevice11.Instance.Context2D1.FillGeometry(_backgroundGeometry, background, localRenderContext);
      }

      var border = BorderBrush;
      if (border != null && _borderGeometry != null && BorderThickness > 0 && border.RenderBrush(localRenderContext) && border.TryAllocate())
      {
        GraphicsDevice11.Instance.Context2D1.DrawGeometry(_borderGeometry, border.Brush2D, (float)BorderThickness, null, localRenderContext);
      }

      FrameworkElement content = _initializedContent;
      if (content != null)
        content.Render(localRenderContext);
    }

    #endregion

    /// <summary>
    /// (Re-)Creates the StrokeStyle. The D2D StrokeStyle is immutable and must be recreated on changes.
    /// </summary>
    protected void ReCreateStrokeStyle()
    {
      StrokeStyleProperties prop = new StrokeStyleProperties
      {
        LineJoin = BorderLineJoin,
        // TODO: add properties, where to find default values?
        //MiterLimit = StrokeMiterLimit,
        //DashCap = StrokeDashCap,
        //DashOffset = StrokeDashOffset,
        //DashStyle = StrokeDashStyle,
        //StartCap = StrokeStartCap,
        //EndCap = StrokeEndCap
      };

      TryDispose(ref _strokeStyle);
      _strokeStyle = new StrokeStyle(GraphicsDevice11.Instance.Context2D1.Factory, prop);
    }

    // Allocation/Deallocation of _initializedContent not necessary because UIElement handles all direct children

    public override void Deallocate()
    {
      base.Deallocate();
      if (BorderBrush != null)
        BorderBrush.Deallocate();
      if (Background != null)
        Background.Deallocate();
      TryDispose(ref _strokeStyle);
      _performLayout = true;
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

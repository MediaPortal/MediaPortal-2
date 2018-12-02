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

using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml;
using SharpDX;
using MediaPortal.Utilities.DeepCopy;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using Brush = MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Control : FrameworkElement
  {
    #region Protected fields

    protected AbstractProperty _templateProperty;
    protected AbstractProperty _templateControlProperty;
    protected AbstractProperty _backgroundProperty;
    protected AbstractProperty _borderBrushProperty;
    protected AbstractProperty _borderThicknessProperty;
    protected AbstractProperty _horizontalContentAlignmentProperty;
    protected AbstractProperty _verticalContentAlignmentProperty;

    protected bool _hidden = false;
    protected FrameworkElement _initializedTemplateControl = null; // We need to cache the TemplateControl because after it was set, it first needs to be initialized before it can be used
    protected volatile bool _performLayout = true; // Mark control to adapt background brush and related contents to the layout
    protected SharpDX.Direct2D1.Geometry _backgroundGeometry;

    #endregion

    #region Ctor

    public Control()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _templateProperty = new SProperty(typeof(ControlTemplate), null);
      _templateControlProperty = new SProperty(typeof(FrameworkElement), null);
      _borderBrushProperty = new SProperty(typeof(Brush), null);
      _backgroundProperty = new SProperty(typeof(Brush), null);
      _borderThicknessProperty = new SProperty(typeof(double), 1.0);
      _horizontalContentAlignmentProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Stretch);
      _verticalContentAlignmentProperty = new SProperty(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Stretch);
    }

    void Attach()
    {
      _templateProperty.Attach(OnTemplateChanged);
      _templateControlProperty.Attach(OnTemplateControlChanged);
      _backgroundProperty.Attach(OnBackgroundChanged);
      _horizontalContentAlignmentProperty.Attach(OnArrangeGetsInvalid);
      _verticalContentAlignmentProperty.Attach(OnArrangeGetsInvalid);
    }

    void Detach()
    {
      _templateProperty.Detach(OnTemplateChanged);
      _templateControlProperty.Detach(OnTemplateControlChanged);
      _backgroundProperty.Detach(OnBackgroundChanged);
      _horizontalContentAlignmentProperty.Detach(OnArrangeGetsInvalid);
      _verticalContentAlignmentProperty.Detach(OnArrangeGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Control c = (Control)source;
      BorderBrush = copyManager.GetCopy(c.BorderBrush);
      Background = copyManager.GetCopy(c.Background);
      BorderThickness = c.BorderThickness;
      Template = copyManager.GetCopy(c.Template);
      FrameworkElement oldTemplateControl = TemplateControl;
      if (oldTemplateControl != null)
        oldTemplateControl.VisualParent = null;
      TemplateControl = copyManager.GetCopy(c.TemplateControl);
      HorizontalContentAlignment = c.HorizontalContentAlignment;
      VerticalContentAlignment = c.VerticalContentAlignment;
      _initializedTemplateControl = copyManager.GetCopy(c._initializedTemplateControl);
      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Template);
      MPF.TryCleanupAndDispose(TemplateControl);
      MPF.TryCleanupAndDispose(BorderBrush);
      MPF.TryCleanupAndDispose(Background);
      base.Dispose();
    }

    #endregion

    #region Change handlers

    void OnBackgroundChanged(AbstractProperty property, object oldValue)
    {
      _performLayout = true;
    }

    void OnTemplateChanged(AbstractProperty property, object oldValue)
    {
      ControlTemplate template = Template;
      if (template != null)
      {
        Resources.Merge(template.Resources);
        FrameworkElement templateControl = template.LoadContent(this) as FrameworkElement;
        if (templateControl != null)
          templateControl.LogicalParent = this;
        TemplateControl = templateControl;
      }
      else
        TemplateControl = null;
    }

    void OnTemplateControlChanged(AbstractProperty property, object oldValue)
    {
      FrameworkElement oldTemplateControl = oldValue as FrameworkElement;
      MPF.TryCleanupAndDispose(oldTemplateControl);

      FrameworkElement element = TemplateControl;
      if (element != null)
      {
        element.VisualParent = this;
        element.SetScreen(Screen);
        element.SetElementState(_elementState);
        if (element.TemplateNameScope == null)
          // This might be the case if the TemplateControl is directly assigned, without the use of a FrameworkTemplate,
          // which normally sets the TemplateNameScope.
          element.TemplateNameScope = new NameScope();
        if (IsAllocated)
          element.Allocate();
      }
      _initializedTemplateControl = element;
      InvalidateLayout(true, true);
    }

    #endregion

    #region Public properties

    public AbstractProperty TemplateControlProperty
    {
      get { return _templateControlProperty; }
    }

    public FrameworkElement TemplateControl
    {
      get { return (FrameworkElement)_templateControlProperty.GetValue(); }
      set { _templateControlProperty.SetValue(value); }
    }

    public AbstractProperty BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return (Brush)_backgroundProperty.GetValue(); }
      set { _backgroundProperty.SetValue(value); }
    }

    public AbstractProperty BorderBrushProperty
    {
      get { return _borderBrushProperty; }
    }

    public Brush BorderBrush
    {
      get { return (Brush)_borderBrushProperty.GetValue(); }
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

    public AbstractProperty TemplateProperty
    {
      get { return _templateProperty; }
    }

    public ControlTemplate Template
    {
      get { return (ControlTemplate)_templateProperty.GetValue(); }
      set { _templateProperty.SetValue(value); }
    }

    public AbstractProperty HorizontalContentAlignmentProperty
    {
      get { return _horizontalContentAlignmentProperty; }
    }

    public HorizontalAlignmentEnum HorizontalContentAlignment
    {
      get { return (HorizontalAlignmentEnum)_horizontalContentAlignmentProperty.GetValue(); }
      set { _horizontalContentAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty VerticalContentAlignmentProperty
    {
      get { return _verticalContentAlignmentProperty; }
    }

    public VerticalAlignmentEnum VerticalContentAlignment
    {
      get { return (VerticalAlignmentEnum)_verticalContentAlignmentProperty.GetValue(); }
      set { _verticalContentAlignmentProperty.SetValue(value); }
    }

    #endregion

    #region Rendering

    public override void RenderOverride(RenderContext localRenderContext)
    {
      PerformLayout(localRenderContext);

      base.RenderOverride(localRenderContext);

      Brush background = Background;
      if (background != null && background.RenderBrush(localRenderContext) && background.TryAllocate() && _backgroundGeometry != null)
      {
        GraphicsDevice11.Instance.Context2D1.FillGeometry(_backgroundGeometry, background, localRenderContext);
      }

      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return;
      templateControl.Render(localRenderContext);
    }

    public void PerformLayout(RenderContext localRenderContext)
    {
      if (!_performLayout)
        return;
      _performLayout = false;

      // Setup background brush
      Brush background = Background;
      if (background != null)
      {
        Size2F actualSize = new Size2F((float)ActualWidth, (float)ActualHeight);

        RawRectangleF rect = new RectangleF(ActualPosition.X /*- 0.5f*/, ActualPosition.Y /*- 0.5f*/,
            actualSize.Width /*+ 0.5f*/, actualSize.Height /*+ 0.5f*/);

        _backgroundGeometry = new RectangleGeometry(GraphicsDevice11.Instance.Context2D1.Factory, rect);

        background.SetupBrush(this, ref rect, localRenderContext.ZOrder, true);
      }
      else
      {
        TryDispose(ref _backgroundGeometry);
      }
    }

    #endregion

    #region Measure & Arrange

    // Be careful to always call this method from subclasses. This is necessary to satisfy the Measure/Arrange contract for our
    // TemplateControl (if only ArrangeOverride() is called but not CalculateInnerDesiredSize(), the TemplateControl could be
    // arranged without having been measured - which is illegal).
    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return new Size2F();
      templateControl.Measure(ref totalSize);
      return totalSize;
    }

    protected override void ArrangeOverride()
    {
      Vector2 oldPosition = ActualPosition;
      double oldWidth = ActualWidth;
      double oldHeight = ActualHeight;
      base.ArrangeOverride();
      if (ActualWidth != 0 && ActualHeight != 0 &&
          (ActualWidth != oldWidth || ActualHeight != oldHeight || oldPosition != ActualPosition))
        _performLayout = true;
      ArrangeTemplateControl();
    }

    protected virtual void ArrangeTemplateControl()
    {
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return;
      templateControl.Arrange(_innerRect);
    }

    #endregion

    // Allocation/Deallocation of _initializedTemplateControl not necessary because UIElement handles all direct children

    public override void Deallocate()
    {
      base.Deallocate();
      TryDispose(ref _backgroundGeometry);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl != null)
        childrenOut.Add(templateControl);
    }
  }
}

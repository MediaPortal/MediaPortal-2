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
using System.Drawing;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class ContentPresenter : FrameworkElement
  {
    #region Protected fields

    protected AbstractProperty _contentProperty;
    protected AbstractProperty _contentTemplateProperty;
    protected AbstractProperty _horizontalContentAlignmentProperty;
    protected AbstractProperty _verticalContentAlignmentProperty;
    protected FrameworkElement _templateControl = null;
    protected object _content = null;

    #endregion

    #region Ctor

    public ContentPresenter()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _contentProperty = new SProperty(typeof(object), null);
      _contentTemplateProperty = new SProperty(typeof(DataTemplate), null);
      _horizontalContentAlignmentProperty = new SProperty(typeof(HorizontalAlignmentEnum), HorizontalAlignmentEnum.Stretch);
      _verticalContentAlignmentProperty = new SProperty(typeof(VerticalAlignmentEnum), VerticalAlignmentEnum.Stretch);
    }

    void Attach()
    {
      _contentProperty.Attach(OnContentChanged);
      _contentTemplateProperty.Attach(OnContentTemplateChanged);
      _horizontalContentAlignmentProperty.Attach(OnArrangeGetsInvalid);
      _verticalContentAlignmentProperty.Attach(OnArrangeGetsInvalid);
    }

    void Detach()
    {
      _contentProperty.Detach(OnContentChanged);
      _contentTemplateProperty.Detach(OnContentTemplateChanged);
      _horizontalContentAlignmentProperty.Detach(OnArrangeGetsInvalid);
      _verticalContentAlignmentProperty.Detach(OnArrangeGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ContentPresenter p = (ContentPresenter) source;
      Content = copyManager.GetCopy(p.Content);
      ContentTemplate = copyManager.GetCopy(p.ContentTemplate);
      HorizontalContentAlignment = p.HorizontalContentAlignment;
      VerticalContentAlignment = p.VerticalContentAlignment;
      _templateControl = copyManager.GetCopy(p._templateControl);
      _content = copyManager.GetCopy(p._content);
      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(_content);
      MPF.TryCleanupAndDispose(_templateControl);
      MPF.TryCleanupAndDispose(Content);
      MPF.TryCleanupAndDispose(ContentTemplate);
      base.Dispose();
    }

    #endregion

    void OnContentChanged(AbstractProperty property, object oldValue)
    {
      MPF.TryCleanupAndDispose(oldValue);
      MPF.TryCleanupAndDispose(_content);

      object content = Content;
      // Try to unwrap ResourceWrapper before _content is accessed elsewhere.
      // That's the only function we need from the ConvertType method, that's why we only call MPF.ConvertType
      // instead of TypeConverter.Convert.
      if (!MPF.ConvertType(content, typeof(object), out _content))
        _content = content;
      DependencyObject depObj;
      if (!ReferenceEquals(content, _content) && (depObj = _content as DependencyObject) != null)
        depObj.LogicalParent = this;

      if (ContentTemplate == null)
        // No ContentTemplate set
        InstallAutomaticContentDataTemplate();
      FrameworkElement templateControl = _templateControl;
      if (!(_content is UIElement) && templateControl != null) // If our content is an UIElement itself, it should only be used as template control but not as context
        // The controls in the DataTemplate access their "data" via their data context, so we must assign it
        templateControl.Context = _content;
    }

    void OnContentTemplateChanged(AbstractProperty property, object oldValue)
    {
      if (ContentTemplate == null)
      {
        InstallAutomaticContentDataTemplate();
        return;
      }
      FinishBindingsDlgt finishDlgt;
      IList<TriggerBase> triggers;
      SetTemplateControl(ContentTemplate.LoadContent(out triggers, out finishDlgt) as FrameworkElement, triggers);
      finishDlgt.Invoke();
    }

    /// <summary>
    /// Does an automatic search for an approppriate data template for our content, i.e. looks
    /// in our resources for a resource with the Content's type as key.
    /// </summary>
    void InstallAutomaticContentDataTemplate()
    {
      object content = _content;
      if (content == null)
      {
        SetTemplateControl(null);
        return;
      }
      Type type = content.GetType();
      DataTemplate dt = null;
      while (dt == null && type != null)
      {
        dt = FindResource(type) as DataTemplate;
        type = type.BaseType;
      }
      if (dt != null)
      {
        FinishBindingsDlgt finishDlgt;
        IList<TriggerBase> triggers;
        SetTemplateControl(dt.LoadContent(out triggers, out finishDlgt) as FrameworkElement, triggers);
        finishDlgt.Invoke();
        return;
      }
      // Try to convert our content to a FrameworkElement.
      if (TypeConverter.Convert(content, typeof(FrameworkElement), out content))
      {
        FrameworkElement templateControl = content as FrameworkElement;
        SetTemplateControl(templateControl);
      }
      // else: no content template to present the content
    }

    protected void SetTemplateControl(FrameworkElement templateControl, IList<TriggerBase> triggers)
    {
      SetTemplateControl(templateControl);
      UninitializeTriggers();
      CollectionUtils.AddAll(Triggers, triggers);
    }

    protected void SetTemplateControl(FrameworkElement templateControl)
    {
      FrameworkElement oldTemplateControl = _templateControl;
      if (ReferenceEquals(oldTemplateControl, templateControl))
        return;
      _templateControl = null;
      if (oldTemplateControl != null)
        oldTemplateControl.CleanupAndDispose();
      if (templateControl == null)
        return;
      object content = _content;
      if (!(content is UIElement)) // If our content is an UIElement itself, it should only be used as template control but not as context
        templateControl.Context = content;
      templateControl.VisualParent = this;
      templateControl.SetScreen(Screen);
      templateControl.SetElementState(_elementState);
      if (IsAllocated)
        templateControl.Allocate();
      _templateControl = templateControl;
      InvalidateLayout(true, true);
    }

    public FrameworkElement TemplateControl
    {
      get { return _templateControl; }
    }

    public AbstractProperty ContentProperty
    {
      get { return _contentProperty; }
    }

    public object Content
    {
      get { return _contentProperty.GetValue(); }
      set { _contentProperty.SetValue(value); }
    }

    public AbstractProperty ContentTemplateProperty
    {
      get { return _contentTemplateProperty; }
    }

    public DataTemplate ContentTemplate
    {
      get { return _contentTemplateProperty.GetValue() as DataTemplate; }
      set { _contentTemplateProperty.SetValue(value); }
    }

    public AbstractProperty HorizontalContentAlignmentProperty
    {
      get { return _horizontalContentAlignmentProperty; }
    }

    public HorizontalAlignmentEnum HorizontalContentAlignment
    {
      get { return (HorizontalAlignmentEnum) _horizontalContentAlignmentProperty.GetValue(); }
      set { _horizontalContentAlignmentProperty.SetValue(value); }
    }

    public AbstractProperty VerticalContentAlignmentProperty
    {
      get { return _verticalContentAlignmentProperty; }
    }

    public VerticalAlignmentEnum VerticalContentAlignment
    {
      get { return (VerticalAlignmentEnum) _verticalContentAlignmentProperty.GetValue(); }
      set { _verticalContentAlignmentProperty.SetValue(value); }
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      if (_templateControl == null)
        return SizeF.Empty;
      // Measure the child
      _templateControl.Measure(ref totalSize);
      return totalSize;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      ArrangeTemplateControl();
    }

    protected virtual void ArrangeTemplateControl()
    {
      if (_templateControl == null)
        return;
      PointF position = new PointF(_innerRect.X, _innerRect.Y);
      SizeF availableSize = new SizeF(_innerRect.Width, _innerRect.Height);
      ArrangeChild(_templateControl, HorizontalContentAlignment, VerticalContentAlignment,
          ref position, ref availableSize);
      RectangleF childRect = new RectangleF(position, availableSize);
      _templateControl.Arrange(childRect);
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      base.DoRender(localRenderContext);
      if (_templateControl != null)
        _templateControl.Render(localRenderContext);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      if (_templateControl != null)
        childrenOut.Add(_templateControl);
    }

    // Allocation/Deallocation of _templateControl not necessary because UIElement handles all direct children
  }
}

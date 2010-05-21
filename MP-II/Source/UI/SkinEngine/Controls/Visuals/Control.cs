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

using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.Rendering;
using SizeF = System.Drawing.SizeF;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Control : FrameworkElement
  {
    #region Private/protected fields

    AbstractProperty _templateProperty;
    AbstractProperty _templateControlProperty;
    AbstractProperty _backgroundProperty;
    AbstractProperty _borderProperty;
    AbstractProperty _borderThicknessProperty;
    AbstractProperty _cornerRadiusProperty;
    protected bool _hidden = false;
    protected FrameworkElement _initializedTemplateControl = null; // We need to cache the TemplateControl because after it was set, it first needs to be initialized before it can be used

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
      _borderProperty = new SProperty(typeof(Brush), null);
      _backgroundProperty = new SProperty(typeof(Brush), null);
      _borderThicknessProperty = new SProperty(typeof(double), 1.0);
      _cornerRadiusProperty = new SProperty(typeof(double), 0.0);
    }

    void Attach()
    {
      _templateProperty.Attach(OnTemplateChanged);
      _templateControlProperty.Attach(OnTemplateControlChanged);
    }

    void Detach()
    {
      _templateProperty.Detach(OnTemplateChanged);
      _templateControlProperty.Detach(OnTemplateControlChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Control c = (Control)source;
      BorderBrush = copyManager.GetCopy(c.BorderBrush);
      Background = copyManager.GetCopy(c.Background);
      BorderThickness = c.BorderThickness;
      CornerRadius = c.CornerRadius;
      Template = copyManager.GetCopy(c.Template);
      FrameworkElement oldTemplateControl = TemplateControl;
      if (oldTemplateControl != null)
        oldTemplateControl.VisualParent = null;
      TemplateControl = copyManager.GetCopy(c.TemplateControl);
      _initializedTemplateControl = copyManager.GetCopy(c._initializedTemplateControl);
      Attach();
    }

    #endregion

    #region Change handlers

    void OnTemplateChanged(AbstractProperty property, object oldValue)
    {
      if (Template != null)
      {
        Resources.Merge(Template.Resources);
        FinishBindingsDlgt finishDlgt;
        IList<TriggerBase> triggers;
        FrameworkElement templateControl = Template.LoadContent(out triggers, out finishDlgt) as FrameworkElement;
        TemplateControl = templateControl;
        finishDlgt.Invoke();
        foreach (TriggerBase trigger in triggers)
        {
          trigger.LogicalParent = templateControl;
          trigger.Setup(this);
          Triggers.Add(trigger);
        }
      }
      else
        TemplateControl = null;
    }

    void OnTemplateControlChanged(AbstractProperty property, object oldValue)
    {
      FrameworkElement oldTemplateControl = oldValue as FrameworkElement;
      if (oldTemplateControl != null)
        oldTemplateControl.VisualParent = null;

      FrameworkElement element = TemplateControl;
      if (element != null)
      {
        element.SetScreen(Screen);
        element.VisualParent = this;
      }
      _initializedTemplateControl = element;
      InvalidateLayout();
    }

    #endregion

    #region Public properties

    public AbstractProperty TemplateControlProperty
    {
      get { return _templateControlProperty; }
    }

    public FrameworkElement TemplateControl
    {
      get { return (FrameworkElement) _templateControlProperty.GetValue(); }
      set { _templateControlProperty.SetValue(value); }
    }

    public AbstractProperty BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return (Brush) _backgroundProperty.GetValue(); }
      set { _backgroundProperty.SetValue(value); }
    }

    public AbstractProperty BorderBrushProperty
    {
      get { return _borderProperty; }
    }

    public Brush BorderBrush
    {
      get { return (Brush) _borderProperty.GetValue(); }
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

    public AbstractProperty TemplateProperty
    {
      get { return _templateProperty; }
    }

    public ControlTemplate Template
    {
      get { return (ControlTemplate) _templateProperty.GetValue(); }
      set { _templateProperty.SetValue(value); }
    }

    #endregion

    #region Rendering

    public override void DoRender(RenderContext localRenderContext)
    {
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return;
      templateControl.Render(localRenderContext);
    }

    #endregion

    #region Measure & Arrange

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return new SizeF();
      templateControl.Measure(ref totalSize);
      return totalSize;
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return;
      templateControl.Arrange(_innerRect);
    }

    #endregion

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl != null)
        childrenOut.Add(templateControl);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl != null)
        templateControl.Deallocate();
    }

    public override void Allocate()
    {
      base.Allocate();
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl != null)
        templateControl.Allocate();
    }
  }
}

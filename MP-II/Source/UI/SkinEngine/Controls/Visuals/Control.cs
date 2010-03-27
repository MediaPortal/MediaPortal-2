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
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Triggers;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class Control : FrameworkElement, IUpdateEventHandler
  {
    #region Private/protected fields

    AbstractProperty _templateProperty;
    AbstractProperty _templateControlProperty;
    AbstractProperty _backgroundProperty;
    AbstractProperty _borderProperty;
    AbstractProperty _borderThicknessProperty;
    AbstractProperty _cornerRadiusProperty;
    protected UIEvent _lastEvent = UIEvent.None;
    protected bool _hidden = false;

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
      BorderThickness = copyManager.GetCopy(c.BorderThickness);
      CornerRadius = copyManager.GetCopy(c.CornerRadius);
      Template = copyManager.GetCopy(c.Template);
      TemplateControl = copyManager.GetCopy(c.TemplateControl);
      Attach();
    }

    #endregion

    #region Change handlers

    void OnTemplateChanged(AbstractProperty property, object oldValue)
    {
      if (Template != null)
      {
        Resources.Merge(Template.Resources);
        foreach (TriggerBase t in Template.Triggers)
          Triggers.Add(t);
        ///@optimize:
        TemplateControl = Template.LoadContent() as FrameworkElement;
      }
      else
        TemplateControl = null;
    }

    void OnTemplateControlChanged(AbstractProperty property, object oldValue)
    {
      FrameworkElement oldTemplateControl = oldValue as FrameworkElement;
      if (oldTemplateControl != null)
        oldTemplateControl.VisualParent = null;

      FrameworkElement element = property.GetValue() as FrameworkElement;
      if (element != null)
      {
        element.VisualParent = this;
        element.SetScreen(Screen);
      }
      Invalidate();
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

    public virtual void Update()
    {
      if (_hidden)
      {
        if ((_lastEvent & UIEvent.Visible) != 0)
        {
          _hidden = false;
          BecomesVisible();
        }
      }
      if (_hidden)
      {
        _lastEvent = UIEvent.None;
        return;
      }
      if ((_lastEvent & UIEvent.Hidden) != 0)
      {
        if (!_hidden)
        {
          _hidden = true;
          BecomesHidden();
        }
        _lastEvent = UIEvent.None;
        return;
      }

      UpdateLayout();
    }

    public override void DoRender()
    {
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
      {
        SkinContext.AddOpacity(Opacity);
        templateControl.Render();
        SkinContext.RemoveOpacity();
      }
    }

    #endregion

    #region Measure & Arrange

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width*SkinContext.Zoom.Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height*SkinContext.Zoom.Height;

      FrameworkElement templateControl = TemplateControl;
      SizeF childSize;

      if (templateControl != null)
      {
        childSize = new SizeF(totalSize.Width, totalSize.Height);
        if (LayoutTransform != null)
        {
          ExtendedMatrix m;
          LayoutTransform.GetTransform(out m);
          SkinContext.AddLayoutTransform(m);
        }

        templateControl.Measure(ref childSize);

        if (LayoutTransform != null)
          SkinContext.RemoveLayoutTransform();
      }
      else
        childSize = new SizeF();

      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
        _desiredSize.Width = childSize.Width;
      if (double.IsNaN(Height))
        _desiredSize.Height = childSize.Height;

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("Control.Measure returns '{0}' {1}x{2}", this.Name, totalSize.Width, totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      FrameworkElement templateControl = TemplateControl;
      //Trace.WriteLine(String.Format("Control.Arrange :{0} X {1},Y {2} W {3}xH {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      RemoveMargin(ref finalRect);

      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);

      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (templateControl != null)
      {
        templateControl.Arrange(layoutRect);
        ActualBounds = templateControl.ActualTotalBounds; // Need to add 
      }

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
        _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      base.Arrange(finalRect);
    }

    #endregion

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        childrenOut.Add(templateControl);
    }

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      if ((_lastEvent & UIEvent.Hidden) != 0 && eventType == UIEvent.Visible)
        _lastEvent = UIEvent.None;
      if ((_lastEvent & UIEvent.Visible) != 0 && eventType == UIEvent.Hidden)
        _lastEvent = UIEvent.None;
      base.FireUIEvent(eventType, source);

      if (SkinContext.UseBatching)
      {
        _lastEvent |= eventType;
        if (Screen != null) Screen.Invalidate(this);
      }
    }

    public override void Deallocate()
    {
      base.Deallocate();
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.Deallocate();
    }

    public override void Allocate()
    {
      base.Allocate();
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.Allocate();
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      FrameworkElement templateControl = TemplateControl;
      if (templateControl != null)
        templateControl.DestroyRenderTree();
      base.DestroyRenderTree();
    }

    public virtual void BecomesVisible()
    { }

    public virtual void BecomesHidden()
    { }
  }
}

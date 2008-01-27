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
using SkinEngine.Controls.Brushes;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;

namespace SkinEngine.Controls.Visuals
{
  public class ProgressBar : Control
  {
    Property _templateProperty;
    Property _styleProperty;
    Property _valueProperty;
    FrameworkElement _partIndicator;

    public ProgressBar()
    {
      Init();
    }

    public ProgressBar(ProgressBar b)
      : base(b)
    {
      Init(); ;
      Style = b.Style;
      Value = b.Value;
    }

    public override object Clone()
    {
      return new ProgressBar(this);
    }

    void Init()
    {
      _templateProperty = new Property(null);
      _styleProperty = new Property(null);
      _valueProperty = new Property(0.0f);
      Focusable = false;
      _styleProperty.Attach(new PropertyChangedHandler(OnStyleChanged));
      _valueProperty.Attach(new PropertyChangedHandler(OnValueChanged));
    }

    void OnStyleChanged(Property property)
    {
      Style.Set(this);
      this.Template.VisualParent = this;
      Invalidate();
      _partIndicator = this.Template.FindElement("PART_Indicator") as FrameworkElement;
    }
    void OnValueChanged(Property property)
    {
      if (_partIndicator != null)
      {
        double w = this.ActualWidth;
        w /= 100.0f;
        w *= (double)(this.Value);
        _partIndicator.Width = (double)w;

      }
    }


    /// <summary>
    /// Gets or sets the progress value property.
    /// </summary>
    /// <value>The progress value property.</value>
    public Property ValueProperty
    {
      get
      {
        return _valueProperty;
      }
      set
      {
        _valueProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the progress value.
    /// </summary>
    /// <value>The progress value.</value>
    public float Value
    {
      get
      {
        return (float)_valueProperty.GetValue();
      }
      set
      {
        _valueProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the control template property.
    /// </summary>
    /// <value>The control template property.</value>
    public Property TemplateProperty
    {
      get
      {
        return _templateProperty;
      }
      set
      {
        _templateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control template.
    /// </summary>
    /// <value>The control template.</value>
    public UIElement Template
    {
      get
      {
        return _templateProperty.GetValue() as UIElement;
      }
      set
      {
        _templateProperty.SetValue(value);
      }
    }


    /// <summary>
    /// Gets or sets the control style property.
    /// </summary>
    /// <value>The control style property.</value>
    public Property StyleProperty
    {
      get
      {
        return _styleProperty;
      }
      set
      {
        _styleProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public Style Style
    {
      get
      {
        return _styleProperty.GetValue() as Style;
      }
      set
      {
        _styleProperty.SetValue(value);
      }
    }
    public override void Reset()
    {
      base.Reset();
      if (Template != null)
        Template.Reset();
    }


    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      if (Template == null)
      {
        _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
        if (Width <= 0)
          _desiredSize.Width = (float)(availableSize.Width - marginWidth);
        if (Height <= 0)
          _desiredSize.Height = (float)(availableSize.Height - marginHeight);

        if (LayoutTransform != null)
        {
          ExtendedMatrix m = new ExtendedMatrix();
          LayoutTransform.GetTransform(out m);
          SkinContext.AddLayoutTransform(m);
        }
        SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
        _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
        if (LayoutTransform != null)
        {
          SkinContext.RemoveLayoutTransform();
        }
        _desiredSize.Width += marginWidth;
        _desiredSize.Height += marginHeight;
        _originalSize = _desiredSize;
        return;
      }

      _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width <= 0)
        _desiredSize.Width = (float)(availableSize.Width - marginWidth);
      if (Height <= 0)
        _desiredSize.Height = (float)(availableSize.Height - marginHeight);


      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      Template.Measure(_desiredSize);

      if (Width <= 0)
        _desiredSize.Width = Template.DesiredSize.Width;

      if (Height <= 0)
        _desiredSize.Height = Template.DesiredSize.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
      _originalSize = _desiredSize;


      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      if (Template != null)
      {
        Template.Arrange(layoutRect);
        ActualPosition = Template.ActualPosition;
        ActualWidth = ((FrameworkElement)Template).ActualWidth;
        ActualHeight = ((FrameworkElement)Template).ActualHeight;
      }

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      IsArrangeValid = true;
      InitializeBindings();
      InitializeTriggers();
      _isLayoutInvalid = false;
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      base.DoRender();
      if (Template != null)
      {
        ExtendedMatrix m = new ExtendedMatrix(this.Opacity);
        SkinContext.AddTransform(m);
        Template.DoRender();
        SkinContext.RemoveTransform();
      }
    }

    /// <summary>
    /// Fires an event.
    /// </summary>
    /// <param name="eventName">Name of the event.</param>
    public override void FireEvent(string eventName)
    {
      if (Template != null)
      {
        Template.FireEvent(eventName);
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
      if (Template != null)
      {
        UIElement o = Template.FindElement(name);
        if (o != null) return o;
      }
      return base.FindElement(name);
    }

    public override UIElement FindElementType(Type t)
    {
      if (Template != null)
      {
        UIElement o = Template.FindElementType(t);
        if (o != null) return o;
      }
      return base.FindElementType(t);
    }

    public override UIElement FindItemsHost()
    {
      if (Template != null)
      {
        UIElement o = Template.FindItemsHost();
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
      if (Template != null)
      {
        UIElement o = Template.FindFocusedItem();
        if (o != null) return o;
      }
      return null;
    }

    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      if (Template != null)
      {
        Template.OnMouseMove(x, y);
      }
      base.OnMouseMove(x, y);
    }

    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      if (Template != null)
      {
        Template.Animate();
      }
      base.Animate();
    }
  }
}


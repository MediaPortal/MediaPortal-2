#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;

namespace SkinEngine.Controls.Visuals
{
  public class Button : FrameworkElement
  {
    Property _templateProperty;
    Property _styleProperty;

    public Button()
    {
      Init();
    }

    public Button(Button b)
      : base(b)
    {
      Init();
      Template = (UIElement)b.Template.Clone();
      Style = b.Style;
    }

    public override object Clone()
    {
      return new Button(this);
    }

    void Init()
    {
      _templateProperty = new Property(null);
      _styleProperty = new Property(null);
      IsFocusable = true;
      _styleProperty.Attach(new PropertyChangedHandler(OnStyleChanged));
    }

    void OnStyleChanged(Property property)
    {
      Style.Set(this);
      Invalidate();
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

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.Size availableSize)
    {
      base.Measure(availableSize);
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width == 0) _desiredSize.Width = (int)availableSize.Width;
      if (Height == 0) _desiredSize.Height = (int)availableSize.Height;

      if (Template != null)
      {
        Template.Measure(_desiredSize);
        _desiredSize = Template.DesiredSize;
      }
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      if (Template != null)
      {
        Template.Arrange(finalRect);
        ActualPosition = Template.ActualPosition;
        ActualHeight = ((FrameworkElement)Template).ActualHeight;
        ActualWidth = ((FrameworkElement)Template).ActualWidth;
      }
      base.Arrange(finalRect);
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      if (Template != null)
      {
        Template.DoRender();
      }
      base.DoRender();
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
        UIElement o=Template.FindElement(name);
        if (o != null) return o;
      }
      return base.FindElement(name);
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

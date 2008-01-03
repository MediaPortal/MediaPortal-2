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
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;
using SkinEngine.Controls.Panels;

namespace SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl
  {
    Property _styleProperty;
    Property _templateProperty;
    ArrayList _items;

    public ListView()
    {
      Init();
    }

    public ListView(ListView c)
      : base(c)
    {
      Init();
      if (c.Style != null)
        Style = c.Style;
      if (c.Template != null)
        Template = (UIElement)c.Template.Clone();
    }

    public override object Clone()
    {
      return new ListView(this);
    }

    void Init()
    {
      _styleProperty = new Property(null);
      _templateProperty = new Property(null);
      _styleProperty.Attach(new PropertyChangedHandler(OnStyleChanged));

      _items = new ArrayList();
      _items.Add("item1");
      _items.Add("item2");
      _items.Add("item3");
      _items.Add("item4");
      _items.Add("item5");
      ItemsSource = _items;
    }

    void OnStyleChanged(Property property)
    {
      Style.Set(this);
      this.Template.VisualParent = this;
      ItemsPanel = (Panel)this.Template.FindItemsHost();
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
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width <= 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      if (Template != null)
      {
        Template.Measure(_desiredSize);
        _desiredSize = Template.DesiredSize;
      }
      if (Width > 0) _desiredSize.Width = (int)Width;
      if (Height > 0) _desiredSize.Height = (int)Height;
      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);
      _availableSize = new System.Drawing.Size(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      _availablePoint = new System.Drawing.Point(finalRect.Location.X, finalRect.Location.Y);
      System.Drawing.Rectangle layoutRect = new System.Drawing.Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (Template != null)
      {
        Template.Arrange(layoutRect);
        ActualPosition = Template.ActualPosition;
        ActualWidth = ((FrameworkElement)Template).ActualWidth;
        ActualHeight = ((FrameworkElement)Template).ActualHeight;
      }

      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeTriggers();
      }
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      base.DoRender();
      if (Template != null)
      {
        Template.DoRender();
      }
    }

    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      if (Template != null)
      {
        Template.OnMouseMove(x,y);
      }
    }
    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      base.Animate();
      if (Template != null)
      {
        Template.Animate();
      }
    }

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);
      if (Template != null)
      {
        Template.OnKeyPressed(ref key);
      }
    }

    #region focus prediction

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusUp(focusedFrameworkElement,ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Template).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }


    #endregion
  }
}

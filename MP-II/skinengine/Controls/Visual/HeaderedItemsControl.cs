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
using System.Collections;
using System.Text;
using System.Drawing.Drawing2D;
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine.Controls.Brushes;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine;
using SkinEngine.DirectX;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;

namespace SkinEngine.Controls.Visuals
{
  public class HeaderedItemsControl : ItemsControl
  {
    private Property _headerProperty;
    private Property _headerTemplateProperty;
    private Property _headerTemplateSelectorProperty;
    SizeF _baseDesiredSize;
    bool _wasExpanded = false;
    public string TempName;
    #region ctor
    public HeaderedItemsControl()
    {
      Init();
    }

    public HeaderedItemsControl(HeaderedItemsControl c)
      : base(c)
    {
      Init();
      if (c.Header != null)
      {
        Header = (FrameworkElement)c.Header.Clone();
        Header.VisualParent = this;
      }
      if (c.HeaderTemplate != null)
        HeaderTemplate = (DataTemplate)c.HeaderTemplate.Clone();
      if (c.HeaderTemplateSelector != null)
        HeaderTemplateSelector = c.HeaderTemplateSelector;
    }

    public override object Clone()
    {
      return new HeaderedItemsControl(this);
    }

    void Init()
    {
      _headerProperty = new Property(null);
      _headerTemplateProperty = new Property(null);
      _headerTemplateSelectorProperty = new Property(null);
      _headerProperty.Attach(new PropertyChangedHandler(OnContentChanged));
    }
    #endregion

    #region event handlers
    void OnContentChanged(Property property)
    {
      Header.VisualParent = this;
    }
    #endregion

    #region properties
    /// <summary>
    /// Gets or sets the header property.
    /// </summary>
    /// <value>The header property.</value>
    public Property HeaderProperty
    {
      get
      {
        return _headerProperty;
      }
      set
      {
        _headerProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the header.
    /// </summary>
    /// <value>The header.</value>
    public FrameworkElement Header
    {
      get
      {
        return _headerProperty.GetValue() as FrameworkElement;
      }
      set
      {
        _headerProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the header template property.
    /// </summary>
    /// <value>The header template property.</value>
    public Property HeaderTemplateProperty
    {
      get
      {
        return _headerTemplateProperty;
      }
      set
      {
        _headerTemplateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the header template.
    /// </summary>
    /// <value>The header template.</value>
    public DataTemplate HeaderTemplate
    {
      get
      {
        return _headerTemplateProperty.GetValue() as DataTemplate;
      }
      set
      {
        _headerTemplateProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the header template selector property.
    /// </summary>
    /// <value>The header template selector property.</value>
    public Property HeaderTemplateSelectorProperty
    {
      get
      {
        return _headerTemplateSelectorProperty;
      }
      set
      {
        _headerTemplateSelectorProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets the header template selector.
    /// </summary>
    /// <value>The header template selector.</value>
    public DataTemplateSelector HeaderTemplateSelector
    {
      get
      {
        return _headerTemplateSelectorProperty.GetValue() as DataTemplateSelector;
      }
      set
      {
        _headerTemplateSelectorProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is expanded.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is expanded; otherwise, <c>false</c>.
    /// </value>
    public bool IsExpanded
    {
      get
      {
        if (Header == null)
        {
          _wasExpanded = false;
          return false;
        }
        CheckBox expander = Header.FindElement("Expander") as CheckBox;
        if (Header == null)
        {
          _wasExpanded = false;
          return false;
        }
        if (_wasExpanded != expander.IsChecked)
        {
          if (_wasExpanded)
          {
            if (_itemsHostPanel != null)
            {
              _itemsHostPanel.SetChildren(new UIElementCollection(_itemsHostPanel));
            }
          }
          Invalidate();
        }
        _wasExpanded = expander.IsChecked;
        return _wasExpanded;
      }
    }
    #endregion

    #region measure&arrange
    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(SizeF availableSize)
    {
      MediaPortal.Core.Collections.ListItem listItem = (MediaPortal.Core.Collections.ListItem)Context;
      //      string name = listItem.Label("Name").Evaluate(null, null);
      //      Trace.WriteLine(String.Format("TreeView Item:Measure '{0}' {1}x{2} expanded:{3}", name, availableSize.Width, availableSize.Height, IsExpanded));

      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
      if (Header != null)
      {
        Header.Measure(new SizeF(availableSize.Width, 0));
        if (!_wasExpanded)
        {
          _desiredSize = Header.DesiredSize;
          _originalSize = _desiredSize;

          //          Trace.WriteLine(String.Format("TreeView Item:Measure '{0}' returns header:{1}x{2} not expanded",
          //              name, Header.DesiredSize.Width, Header.DesiredSize.Height));
          return;
        }
      }
      base.Measure(new SizeF(availableSize.Width, 0));
      _baseDesiredSize = new SizeF(_desiredSize.Width, _desiredSize.Height);
      if (Header != null)
      {
        _desiredSize.Height += Header.DesiredSize.Height;
      }
      _originalSize = _desiredSize;
      _availableSize = new System.Drawing.SizeF(availableSize.Width, availableSize.Height);
      //      Trace.WriteLine(String.Format("TreeView Item:Measure '{0}' returns header:{1}x{2} base:{3}x{4}",
      //          name, Header.DesiredSize.Width, Header.DesiredSize.Height,
      //        _baseDesiredSize.Width, _baseDesiredSize.Height));
    }
    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      PointF p = layoutRect.Location;

      MediaPortal.Core.Collections.ListItem listItem = (MediaPortal.Core.Collections.ListItem)Context;
      //      string name = listItem.Label("Name").Evaluate(null, null);
      //      Trace.WriteLine(String.Format("TreeView Item:Arrange {0} ({1},{2}) {2}x{3}", name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));


      if (Header != null)
      {
        //ArrangeChild(Header, ref p, layoutRect.Width, layoutRect.Height);
        Header.Arrange(new RectangleF(p, Header.DesiredSize));
        if (!_wasExpanded)
        {

          _finalLayoutTransform = SkinContext.FinalLayoutTransform;
          IsArrangeValid = true;
          InitializeBindings();
          InitializeTriggers();
          _isLayoutInvalid = false;
          if (!finalRect.IsEmpty)
          {
            if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
              _performLayout = true;
            _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
          }
          return;
        }
        p.Y += Header.DesiredSize.Height;

      }
      if (_wasExpanded)
      {
        //        Trace.WriteLine(String.Format("TreeView Item:Arrange {0} childs at({1},{2})", name, (int)p.X, (int)p.Y));
        base.Arrange(new RectangleF(p, _baseDesiredSize));
      }
    }

    protected void ArrangeChild(FrameworkElement child, ref System.Drawing.PointF p, double widthPerCell, double heightPerCell)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {

        p.X += (float)((widthPerCell - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        p.X += (float)(widthPerCell - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        p.Y += (float)((heightPerCell - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        p.Y += (float)(heightPerCell - child.DesiredSize.Height);
      }
    }
    #endregion

    #region rendering
    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      lock (_headerProperty)
      {
        if (Header != null)
        {
          SkinContext.AddOpacity(this.Opacity);
          Header.DoRender();
          SkinContext.RemoveOpacity();

        }
        if (IsExpanded)
        {
          SkinContext.AddOpacity(this.Opacity);
          base.DoRender();
          SkinContext.RemoveOpacity();
        }
      }
    }
    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      if (Header != null)
      {
        Header.Animate();
      }
      if (IsExpanded)
      {
        base.Animate();
      }
    }
    #endregion

    #region input handling
    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      if (!IsFocusScope) return;
      if (Header != null)
      {
        Header.OnMouseMove(x, y);
      }
      if (IsExpanded)
      {
        base.OnMouseMove(x, y);
      }
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref MediaPortal.Core.InputManager.Key key)
    {
      lock (_headerProperty)
      {
        if (Header != null)
        {
          Header.OnKeyPressed(ref key);
        }
        if (IsExpanded)
        {
          base.OnKeyPressed(ref key);
        }
      }
    }
    #endregion

    #region FindXXX methods
    protected override ItemsPresenter FindItemsPresenter()
    {
      if (TemplateControl == null)
      {
        return base.FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
      }
      return TemplateControl.FindElementType(typeof(ItemsPresenter)) as ItemsPresenter;
    }
    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      if (Header != null)
      {
        UIElement found = Header.FindElement(name);
        if (found != null) return found;
      }
      if (!IsExpanded) return null;
      return base.FindElement(name);
    }
    /// <summary>
    /// Finds the element of type t.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <returns></returns>
    public override UIElement FindElementType(Type t)
    {
      if (Header != null)
      {
        UIElement found = Header.FindElementType(t);
        if (found != null) return found;
      }
      if (!IsExpanded) return null;
      return base.FindElementType(t);
    }

    /// <summary>
    /// Finds the the element which is a ItemsHost
    /// </summary>
    /// <returns></returns>
    public override UIElement FindItemsHost()
    {
      if (Header != null)
      {
        UIElement found = Header.FindItemsHost();
        if (found != null) return found;
      }
      if (!IsExpanded) return null;
      return base.FindItemsHost();
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public override UIElement FindFocusedItem()
    {

      if (Header != null)
      {
        UIElement found = Header.FindFocusedItem();
        if (found != null) return found;
      }
      if (!IsExpanded) return null;
      return base.FindFocusedItem();
    }

    public override void Reset()
    {
      if (Header != null)
        Header.Reset();
      base.Reset();
    }
    public override void Deallocate()
    {
      base.Deallocate();
      if (Header != null)
      {
        Header.Deallocate();
      }
    }
    public override void Allocate()
    {
      base.Allocate();
      if (Header != null)
      {
        Header.Allocate();
      }
    }
    #endregion

    #region focus prediction

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement element;
      if (IsExpanded)
      {
        element = base.PredictFocusUp(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return ((FrameworkElement)Header).PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement element;
      if (IsExpanded)
      {
        element = base.PredictFocusDown(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return ((FrameworkElement)Header).PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement element;
      if (IsExpanded && base.FindFocusedItem() != null)
      {
        element = base.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return ((FrameworkElement)Header).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement element;
      if (IsExpanded && base.FindFocusedItem() != null)
      {
        element = base.PredictFocusRight(focusedFrameworkElement, ref key, strict);
        if (element != null) return element;
      }
      return ((FrameworkElement)Header).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }
    #endregion


  }
}

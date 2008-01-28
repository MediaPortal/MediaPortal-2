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
  public class TreeViewItem : HeaderedItemsControl
  {
    private Property _headerProperty;
    private Property _headerTemplateProperty;
    private Property _headerTemplateSelectorProperty;
    public TreeViewItem()
    {
      Init();
    }

    public TreeViewItem(TreeViewItem c)
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
      return new TreeViewItem(this);
    }


    void Init()
    {
      _headerProperty = new Property(null);
      _headerTemplateProperty = new Property(null);
      _headerTemplateSelectorProperty = new Property(null);
      _headerProperty.Attach(new PropertyChangedHandler(OnContentChanged));
    }

    void OnContentChanged(Property property)
    {
      Header.VisualParent = this;
    }

    /// <summary>
    /// Gets or sets the content property.
    /// </summary>
    /// <value>The content property.</value>
    public Property ContentProperty
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
    /// Gets or sets the content.
    /// </summary>
    /// <value>The content.</value>
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
    /// Gets or sets the content template property.
    /// </summary>
    /// <value>The content template property.</value>
    public Property ContentTemplateProperty
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
    /// Gets or sets the content template.
    /// </summary>
    /// <value>The content template.</value>
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
    /// Gets or sets the content template selector property.
    /// </summary>
    /// <value>The content template selector property.</value>
    public Property ContentTemplateSelectorProperty
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
    /// Gets or sets the content template selector.
    /// </summary>
    /// <value>The content template selector.</value>
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
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(SizeF availableSize)
    {
      base.Measure(_desiredSize);
      if (Header != null)
      {
        Header.Measure(_desiredSize);
        //_desiredSize = Header.DesiredSize;
      }
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      base.Arrange(finalRect);
      if (Header != null)
      {
        System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualWidth, (float)ActualHeight);

        layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
        layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
        layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
        layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);


        PointF p = layoutRect.Location;
        ArrangeChild(Header, ref p, layoutRect.Width, layoutRect.Height);
        Header.Arrange(new RectangleF(p, Header.DesiredSize));
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
    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      base.DoRender();
      if (Header != null)
      {
        ExtendedMatrix em = new ExtendedMatrix(this.Opacity);
        SkinContext.AddTransform(em);
        Header.DoRender();
        SkinContext.RemoveTransform();
      }

    }

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
      base.OnMouseMove(x, y);
    }
    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      base.Animate();
      if (Header != null)
      {
        Header.Animate();
      }
    }


    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref MediaPortal.Core.InputManager.Key key)
    {
      if (Header != null)
      {
        Header.OnKeyPressed(ref key);
      }
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
      return base.FindItemsHost();
    }

    /// <summary>
    /// Finds the focused item.
    /// </summary>
    /// <returns></returns>
    public override UIElement FindFocusedItem()
    {
      if (HasFocus) return this;

      if (Header != null)
      {
        UIElement found = Header.FindFocusedItem();
        if (found != null) return found;
      }
      return null;
    }
    public override void Reset()
    {
      base.Reset();
      if (Header != null)
        Header.Reset();
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
      return ((FrameworkElement)Header).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }


    #endregion

    #region IList Members

    public int Add(object value)
    {
      Header = (FrameworkElement)value;
      return 1;
    }

    public void Clear()
    {
    }

    public bool Contains(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int IndexOf(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void Insert(int index, object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsReadOnly
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object value)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public void RemoveAt(int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public object this[int index]
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
      set
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
      throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
    }

    #endregion
  }
}

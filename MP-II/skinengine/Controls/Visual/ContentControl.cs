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
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;


namespace SkinEngine.Controls.Visuals
{
  public class ContentControl : Control, IList
  {
    private Property _contentProperty;
    private Property _contentTemplateProperty;
    private Property _contentTemplateSelectorProperty;
    public ContentControl()
    {
      Init();
    }

    public ContentControl(ContentControl c)
      : base(c)
    {
      Init();
      if (c.Content != null)
      {
        Content = (FrameworkElement)c.Content.Clone();
        Content.VisualParent = this;
      }
      if (c.ContentTemplate != null)
        ContentTemplate = (DataTemplate)c.ContentTemplate.Clone();
      if (c.ContentTemplateSelector != null)
        ContentTemplateSelector = c.ContentTemplateSelector;
    }

    public override object Clone()
    {
      return new ContentControl(this);
    }


    void Init()
    {
      _contentProperty = new Property(null);
      _contentTemplateProperty = new Property(null);
      _contentTemplateSelectorProperty = new Property(null);
      _contentProperty.Attach(new PropertyChangedHandler(OnContentChanged));
    }

    void OnContentChanged(Property property)
    {
      Content.VisualParent = this;
    }

    /// <summary>
    /// Gets or sets the content property.
    /// </summary>
    /// <value>The content property.</value>
    public Property ContentProperty
    {
      get
      {
        return _contentProperty;
      }
      set
      {
        _contentProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    /// <value>The content.</value>
    public FrameworkElement Content
    {
      get
      {
        return _contentProperty.GetValue() as FrameworkElement;
      }
      set
      {
        _contentProperty.SetValue(value);
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
        return _contentTemplateProperty;
      }
      set
      {
        _contentTemplateProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the content template.
    /// </summary>
    /// <value>The content template.</value>
    public DataTemplate ContentTemplate
    {
      get
      {
        return _contentTemplateProperty.GetValue() as DataTemplate;
      }
      set
      {
        _contentTemplateProperty.SetValue(value);
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
        return _contentTemplateSelectorProperty;
      }
      set
      {
        _contentTemplateSelectorProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the content template selector.
    /// </summary>
    /// <value>The content template selector.</value>
    public DataTemplateSelector ContentTemplateSelector
    {
      get
      {
        return _contentTemplateSelectorProperty.GetValue() as DataTemplateSelector;
      }
      set
      {
        _contentTemplateSelectorProperty.SetValue(value);
      }
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(Size availableSize)
    {
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width <= 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      if (Content != null)
      {
        Content.Measure(_desiredSize);
        _desiredSize = Content.DesiredSize;
      }
      if (Width > 0) _desiredSize.Width = (int)Width;
      if (Height > 0) _desiredSize.Height = (int)Height;

      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;



      _availableSize = new Size(availableSize.Width, availableSize.Height);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(System.Drawing.Rectangle finalRect)
    {
      _finalRect = new System.Drawing.Rectangle(finalRect.Location, finalRect.Size);
      System.Drawing.Rectangle layoutRect = new System.Drawing.Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      if (Content != null)
      {
        Content.Arrange(layoutRect);
        ActualPosition = Content.ActualPosition;
        ActualWidth = ((FrameworkElement)Content).ActualWidth;
        ActualHeight = ((FrameworkElement)Content).ActualHeight;
      }


      if (!IsArrangeValid)
      {
        IsArrangeValid = true;
        InitializeBindings();
        InitializeTriggers();
      }
    }
    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      base.DoRender();
      if (Content != null)
      {
        Content.DoRender();
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
      if (Content != null)
      {
        Content.OnMouseMove(x, y);
      }
      base.OnMouseMove(x, y);
    }
    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      base.Animate();
      if (Content != null)
      {
        Content.Animate();
      }
    }


    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref MediaPortal.Core.InputManager.Key key)
    {
      if (Content != null)
      {
        Content.OnKeyPressed(ref key);
      }
    }
    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      if (Content != null)
      {
        UIElement found = Content.FindElement(name);
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
      if (Content != null)
      {
        UIElement found = Content.FindElementType(t);
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
      if (Content != null)
      {
        UIElement found = Content.FindItemsHost();
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

      if (Content != null)
      {
        UIElement found = Content.FindFocusedItem();
        if (found != null) return found;
      }
      return null;
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
      return ((FrameworkElement)Content).PredictFocusUp(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusDown(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusLeft(focusedFrameworkElement, ref key, strict);
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      return ((FrameworkElement)Content).PredictFocusRight(focusedFrameworkElement, ref key, strict);
    }


    #endregion

    #region IList Members

    public int Add(object value)
    {
      Content = (FrameworkElement)value;
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

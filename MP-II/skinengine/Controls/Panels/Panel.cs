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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using MediaPortal.Core.Properties;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SkinEngine.DirectX;
using SkinEngine.Controls.Brushes;
using SkinEngine;
using MediaPortal.Core.InputManager;
using Rectangle = System.Drawing.Rectangle;

namespace SkinEngine.Controls.Panels
{
  public class Panel : FrameworkElement, IAsset, IList
  {
    protected Property _alignmentXProperty;
    protected Property _alignmentYProperty;
    protected Property _childrenProperty;
    protected Property _backgroundProperty;
    protected VertexBuffer _vertexBufferBackground;
    protected DateTime _lastTimeUsed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Panel"/> class.
    /// </summary>
    public Panel()
    {
      Init();
    }
    public Panel(Panel p)
      : base(p)
    {
      Init();
      AlignmentX = p.AlignmentX;
      AlignmentY = p.AlignmentY;
      if (p.Background != null)
        Background = (Brush)p.Background.Clone();
      foreach (UIElement el in p.Children)
      {
        Children.Add((UIElement)el.Clone());
      }
    }

    void Init()
    {
      _childrenProperty = new Property(new UIElementCollection(this));
      _alignmentXProperty = new Property(AlignmentX.Center);
      _alignmentYProperty = new Property(AlignmentY.Top);
      _backgroundProperty = new Property(null);
      ContentManager.Add(this);

      _alignmentXProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _alignmentYProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _backgroundProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
    }

    /// <summary>
    /// Called when a layout property has changed
    /// we're simply calling Invalidate() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    protected void OnPropertyInvalidate(Property property)
    {
      Invalidate();
    }
    /// <summary>
    /// Called when a non layout property value has been changed
    /// we're simply calling Free() which will do a performlayout
    /// </summary>
    /// <param name="property">The property.</param>
    protected void OnPropertyChanged(Property property)
    {
      Free();
    }

    /// <summary>
    /// Gets or sets the background property.
    /// </summary>
    /// <value>The background property.</value>
    public Property BackgroundProperty
    {
      get
      {
        return _backgroundProperty;
      }
      set
      {
        _backgroundProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the background brush
    /// </summary>
    /// <value>The background.</value>
    public Brush Background
    {
      get
      {
        return _backgroundProperty.GetValue() as Brush;
      }
      set
      {
        _backgroundProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the children property.
    /// </summary>
    /// <value>The children property.</value>
    public Property ChildrenProperty
    {
      get
      {
        return _childrenProperty;
      }
      set
      {
        _childrenProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the children.
    /// </summary>
    /// <value>The children.</value>
    public UIElementCollection Children
    {
      get
      {
        return _childrenProperty.GetValue() as UIElementCollection;
      }
    }

    /// <summary>
    /// Gets or sets the alignment X property.
    /// </summary>
    /// <value>The alignment X property.</value>
    public Property AlignmentXProperty
    {
      get
      {
        return _alignmentXProperty;
      }
      set
      {
        _alignmentXProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the alignment X.
    /// </summary>
    /// <value>The alignment X.</value>
    public AlignmentX AlignmentX
    {
      get
      {
        return (AlignmentX)_alignmentXProperty.GetValue();
      }
      set
      {
        _alignmentXProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the alignment Y property.
    /// </summary>
    /// <value>The alignment Y property.</value>
    public Property AlignmentYProperty
    {
      get
      {
        return _alignmentYProperty;
      }
      set
      {
        _alignmentYProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the alignment Y.
    /// </summary>
    /// <value>The alignment Y.</value>
    public AlignmentY AlignmentY
    {
      get
      {
        return (AlignmentY)_alignmentYProperty.GetValue();
      }
      set
      {
        _alignmentYProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {

      if (_vertexBufferBackground == null)
      {
        PerformLayout();
      }
      if (Background != null)
      {
        Matrix mrel, mt;
        Background.RelativeTransform.GetTransform(out mrel);
        Background.Transform.GetTransform(out mt);
        GraphicsDevice.Device.Transform.World = SkinContext.FinalMatrix.Matrix * mrel * mt;
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBackground, 0);
        Background.BeginRender();
        GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
        Background.EndRender();
      }
      foreach (UIElement element in Children)
      {
        if (element.IsVisible)
        {
          element.Render();
        }
      }
      _lastTimeUsed = SkinContext.Now;
    }

    /// <summary>
    /// Performs the layout.
    /// </summary>
    public void PerformLayout()
    {
      Free();
      if (Background != null)
      {
        _vertexBufferBackground = new VertexBuffer(typeof(PositionColored2Textured), 4, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
        PositionColored2Textured[] verts = (PositionColored2Textured[])_vertexBufferBackground.Lock(0, 0);
        unchecked
        {
          verts[0].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X), (float)(ActualPosition.Y), 1.0f);
          verts[1].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X), (float)(ActualPosition.Y + ActualHeight), 1.0f);
          verts[2].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X + ActualWidth), (float)(ActualPosition.Y + ActualHeight), 1.0f);
          verts[3].Position = new Microsoft.DirectX.Vector3((float)(ActualPosition.X + ActualWidth), (float)(ActualPosition.Y), 1.0f);

        }
        Background.SetupBrush(this, ref verts);
        _vertexBufferBackground.Unlock();
      }
    }


    /// <summary>
    /// Frees this asset.
    /// </summary>
    public void Free()
    {
      if (_vertexBufferBackground != null)
      {
        _vertexBufferBackground.Dispose();
        _vertexBufferBackground = null;
      }
    }

    #region IAsset Members

    /// <summary>
    /// Gets a value indicating the asset is allocated
    /// </summary>
    /// <value><c>true</c> if this asset is allocated; otherwise, <c>false</c>.</value>
    public bool IsAllocated
    {
      get
      {
        return (_vertexBufferBackground != null);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this asset can be deleted.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this asset can be deleted; otherwise, <c>false</c>.
    /// </value>
    public bool CanBeDeleted
    {
      get
      {
        if (!IsAllocated)
        {
          return false;
        }
        TimeSpan ts = SkinContext.Now - _lastTimeUsed;
        if (ts.TotalSeconds >= 1)
        {
          return true;
        }

        return false;
      }
    }


    #endregion

    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnMouseMove(x, y);
      }
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnKeyPressed(ref key);
        if (key == MediaPortal.Core.InputManager.Key.None) return;
      }
    }

    /// <summary>
    /// Animates any timelines for this uielement.
    /// </summary>
    public override void Animate()
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.Animate();
      }
      base.Animate();
    }

    /// <summary>
    /// Find the element with name
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public override UIElement FindElement(string name)
    {
      foreach (UIElement element in Children)
      {
        UIElement found = element.FindElement(name);
        if (found != null) return found;
      }
      return base.FindElement(name);
    }

    public override UIElement FindElementType(Type t)
    {
      foreach (UIElement element in Children)
      {
        UIElement found = element.FindElementType(t);
        if (found != null) return found;
      }
      return base.FindElementType(t);
    }

    public override  UIElement FindItemsHost()
    {
      foreach (UIElement element in Children)
      {
        UIElement found = element.FindItemsHost();
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
      foreach (UIElement element in Children)
      {
        UIElement found = element.FindFocusedItem();
        if (found != null) return found;
      }
      return null;
    }

    #region IList Members

    public int Add(object value)
    {
      Children.Add((UIElement)value);
      return Children.Count;
    }

    public void Clear()
    {
      Children.Clear();
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
      get
      {
        return Children.Count;
      }
    }

    public bool IsSynchronized
    {
      get
      {
        return true;
      }
    }

    public object SyncRoot
    {
      get
      {
        throw new Exception("The method or operation is not implemented.");
      }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
      throw new Exception("The method or operation is not implemented.");
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
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusUp(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Core.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.Focusable)
          {
            if (match == focusedFrameworkElement)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.Y + match.ActualHeight >= bestMatch.ActualPosition.Y + bestMatch.ActualHeight)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusDown(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Core.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.Y <= bestMatch.ActualPosition.Y)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Core.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.X >= bestMatch.ActualPosition.X)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Core.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusRight(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Core.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.X <= bestMatch.ActualPosition.X)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }


    #endregion


    protected virtual void ArrangeChild(FrameworkElement child, ref System.Drawing.Point p)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {
        p.X += ((DesiredSize.Width - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        p.X += (DesiredSize.Width - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        p.Y += ((DesiredSize.Height - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        p.Y += (DesiredSize.Height - child.DesiredSize.Height);
      }
    }
  }
}

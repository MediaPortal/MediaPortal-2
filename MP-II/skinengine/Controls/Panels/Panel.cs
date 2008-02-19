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
using System.Collections.Generic;
using System.Text;
using SkinEngine.Controls.Visuals;
using MediaPortal.Core.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using SkinEngine.DirectX;
using SkinEngine.Controls.Brushes;
using SkinEngine;
using MediaPortal.Core.InputManager;
using Rectangle = System.Drawing.Rectangle;
using MyXaml.Core;
namespace SkinEngine.Controls.Panels
{
  public class Panel : FrameworkElement, IAddChild
  {
    protected Property _alignmentXProperty;
    protected Property _alignmentYProperty;
    protected Property _childrenProperty;
    protected Brush _backgroundProperty;
    protected bool _performLayout = true;
    protected List<UIElement> _renderOrder;
    bool _updateRenderOrder = true;
    protected VisualAssetContext _backgroundAsset;

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
      _backgroundProperty = null;

      _alignmentXProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _alignmentYProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      //_backgroundProperty.Attach(new PropertyChangedHandler(OnPropertyChanged));
      _renderOrder = new List<UIElement>();
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
      if (_backgroundAsset != null)
      {
        _backgroundAsset.Free(false);
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
        return _backgroundProperty;
      }
      set
      {
        _backgroundProperty = value;
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
    public void SetChildren(UIElementCollection children)
    {
      _childrenProperty.SetValue(children);
      _updateRenderOrder = true;
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
    public override void Invalidate()
    {
      base.Invalidate();
      _updateRenderOrder = true;
    }

    /// <summary>
    /// Renders the visual
    /// </summary>
    public override void DoRender()
    {
      UpdateRenderOrder();
      SkinContext.AddOpacity(this.Opacity);
      if (Background != null)
      {
        if (_performLayout || (_backgroundAsset == null) || (_backgroundAsset != null && !_backgroundAsset.IsAllocated))
        {
          PerformLayout();
        }

        ExtendedMatrix m = new ExtendedMatrix();
        m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
        SkinContext.AddTransform(m);
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Background.BeginRender(_backgroundAsset.VertexBuffer, 2, PrimitiveType.TriangleFan))
        {
          GraphicsDevice.Device.SetStreamSource(0, _backgroundAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleFan, 0, 2);
          Background.EndRender();
        }
        SkinContext.RemoveTransform();

        _backgroundAsset.LastTimeUsed = SkinContext.Now;
      }
      foreach (UIElement element in _renderOrder)
      {
        if (element.IsVisible)
        {
          element.Render();
        }
      }
      SkinContext.RemoveOpacity();
    }

    /// <summary>
    /// Performs the layout.
    /// </summary>
    public void PerformLayout()
    {
      //Trace.WriteLine("Panel.PerformLayout() " + this.Name + " -" + this.GetType().ToString());

      if (Background != null)
      {
        if (_backgroundAsset == null)
        {
          _backgroundAsset = new VisualAssetContext();
          ContentManager.Add(_backgroundAsset);
        }
        double w = ActualWidth;
        double h = ActualHeight;
        System.Drawing.SizeF rectSize = new System.Drawing.SizeF((float)w, (float)h);

        ExtendedMatrix m = new ExtendedMatrix();
        if (_finalLayoutTransform != null)
          m.Matrix *= _finalLayoutTransform.Matrix;
        if (LayoutTransform != null)
        {
          ExtendedMatrix em;
          LayoutTransform.GetTransform(out em);
          m.Matrix *= em.Matrix;
        }
        m.InvertSize(ref rectSize);
        System.Drawing.RectangleF rect = new System.Drawing.RectangleF(0, 0, rectSize.Width, rectSize.Height);
        _backgroundAsset.VertexBuffer = PositionColored2Textured.Create(4);
        PositionColored2Textured[] verts = new PositionColored2Textured[4];
        unchecked
        {
          verts[0].Position = m.Transform(new SlimDX.Vector3(rect.Left, rect.Top, 1.0f));
          verts[1].Position = m.Transform(new SlimDX.Vector3(rect.Left, rect.Bottom, 1.0f));
          verts[2].Position = m.Transform(new SlimDX.Vector3(rect.Right, rect.Bottom, 1.0f));
          verts[3].Position = m.Transform(new SlimDX.Vector3(rect.Right, rect.Top, 1.0f));

        }
        Background.SetupBrush(this, ref verts);

        PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
      }

      _performLayout = false;
    }
    protected void UpdateRenderOrder()
    {
      if (!_updateRenderOrder) return;
      _updateRenderOrder = false;
      if (_renderOrder != null && Children != null)
      {
        Children.FixZIndex();
        _renderOrder.Clear();
        foreach (UIElement element in Children)
        {
          _renderOrder.Add(element);
        }
        _renderOrder.Sort(new ZOrderComparer());
      }
    }


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
      foreach (UIElement element in _renderOrder)
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

    public override bool ReplaceElementType(Type t, UIElement newElement)
    {
      for (int i = 0; i < Children.Count; ++i)
      {
        if (Children[i].GetType() == t)
        {
          Children[i] = newElement;
          Children[i].VisualParent = this;
          return true;
        }
      }
      foreach (UIElement element in Children)
      {
        if (element.ReplaceElementType(t, newElement)) return true;
      }
      return false;
    }

    /// <summary>
    /// Finds the element of type t.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <returns></returns>
    public override UIElement FindElementType(Type t)
    {
      foreach (UIElement element in Children)
      {
        UIElement found = element.FindElementType(t);
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


    protected virtual void ArrangeChild(FrameworkElement child, ref System.Drawing.PointF p)
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
    public override void Reset()
    {
      base.Reset();

      foreach (UIElement element in Children)
      {
        element.Reset();
      }
    }
    public override void Deallocate()
    {
      base.Deallocate();
      foreach (FrameworkElement child in Children)
      {
        child.Deallocate();
      }
      if (_backgroundAsset != null)
      {
        _backgroundAsset.Free(true);
        ContentManager.Remove(_backgroundAsset);
        _backgroundAsset = null;
      }
      if (Background != null)
        Background.Deallocate();
    }
    public override void Allocate()
    {
      base.Allocate();
      foreach (FrameworkElement child in Children)
      {
        child.Allocate();
      }
      if (_backgroundAsset != null)
      {
        ContentManager.Add(_backgroundAsset);
      }
      if (Background != null)
        Background.Allocate();
    }

    #region IAddChild Members

    public void AddChild(object o)
    {
      Children.Add((UIElement)o);
    }

    #endregion
  }
}

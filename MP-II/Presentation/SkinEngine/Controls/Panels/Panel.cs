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
using System.Collections.Generic;
using Presentation.SkinEngine.Controls.Visuals;
using MediaPortal.Presentation.Properties;
using SlimDX.Direct3D9;
using Presentation.SkinEngine.DirectX;
using Presentation.SkinEngine.Rendering;
using Presentation.SkinEngine.Controls.Brushes;
using Presentation.SkinEngine;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine.XamlParser.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using Presentation.SkinEngine.SkinManagement;

namespace Presentation.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Finder implementation which looks for a panel which has its
  /// <see cref="Panel.ItemsHost"/> property set.
  /// </summary>
  public class ItemsHostFinder: IFinder
  {
    private static ItemsHostFinder _instance = null;

    public bool Query(UIElement current)
    {
      return current is Panel && ((Panel) current).IsItemsHost;
    }

    public static ItemsHostFinder Instance
    {
      get
      {
        if (_instance == null)
          _instance = new ItemsHostFinder();
        return _instance;
      }
    }
  }

  public class Panel : FrameworkElement, IAddChild<UIElement>, IUpdateEventHandler
  {
    #region Private/protected fields

    protected const string ZINDEX_ATTACHED_PROPERTY = "Panel.ZIndex";

    protected Property _alignmentXProperty;
    protected Property _alignmentYProperty;
    protected Property _childrenProperty;
    protected Property _backgroundProperty;
    protected bool _isItemsHost = false;
    protected bool _performLayout = true;
    protected List<UIElement> _renderOrder;
    bool _updateRenderOrder = true;
    protected VisualAssetContext _backgroundAsset;
    protected PrimitiveContext _backgroundContext;
    UIEvent _lastEvent = UIEvent.None;

    #endregion

    #region Ctor

    public Panel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _childrenProperty = new Property(typeof(UIElementCollection), new UIElementCollection(this));
      _alignmentXProperty = new Property(typeof(AlignmentX), AlignmentX.Center);
      _alignmentYProperty = new Property(typeof(AlignmentY), AlignmentY.Top);
      _backgroundProperty = new Property(typeof(Brush), null);
      _renderOrder = new List<UIElement>();
    }

    void Attach()
    {
      _alignmentXProperty.Attach(OnPropertyInvalidate);
      _alignmentYProperty.Attach(OnPropertyInvalidate);
    }

    void Detach()
    {
      _alignmentXProperty.Detach(OnPropertyInvalidate);
      _alignmentYProperty.Detach(OnPropertyInvalidate);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Panel p = source as Panel;
      AlignmentX = copyManager.GetCopy(p.AlignmentX);
      AlignmentY = copyManager.GetCopy(p.AlignmentY);
      Background = copyManager.GetCopy(p.Background);
      IsItemsHost = copyManager.GetCopy(p.IsItemsHost);
      foreach (UIElement el in p.Children)
        Children.Add(copyManager.GetCopy(el));
      Attach();
    }

    #endregion

    /// FIXME Albert78: this method is never called?
    void OnBrushPropertyChanged(Property property)
    {
      _lastEvent |= UIEvent.OpacityChange;
      if (Window!=null) Window.Invalidate(this);
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
    /// FIXME Albert78: this method is never called?
    protected void OnPropertyChanged(Property property)
    {
      if (_backgroundAsset != null)
      {
        _backgroundAsset.Free(false);
      }
    }

    public Property BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return (Brush) _backgroundProperty.GetValue(); }
      set {
        // FIXME Albert78: Is it necessary to attach to the brush here?
        // If yes, detach from old brush before attaching to new
        if (value != _backgroundProperty.GetValue())
        {
          _backgroundProperty.SetValue(value);
          value.Attach(OnBrushPropertyChanged);
        }
      }
    }

    public Property ChildrenProperty
    {
      get { return _childrenProperty; }
    }

    public UIElementCollection Children
    {
      get
      {
        return _childrenProperty == null ? null :
          _childrenProperty.GetValue() as UIElementCollection;
      }
    }

    public void SetChildren(UIElementCollection children)
    {
      _childrenProperty.SetValue(children);
      SetWindow(Window);
      _updateRenderOrder = true;
      if (Window!=null) Window.Invalidate(this);
    }

    public Property AlignmentXProperty
    {
      get { return _alignmentXProperty; }
    }

    public AlignmentX AlignmentX
    {
      get
      {
        return _alignmentXProperty == null ? AlignmentX.Center :
          (AlignmentX) _alignmentXProperty.GetValue();
      }
      set { _alignmentXProperty.SetValue(value); }
    }

    public Property AlignmentYProperty
    {
      get { return _alignmentYProperty; }
    }

    public AlignmentY AlignmentY
    {
      get
      {
        return _alignmentYProperty == null ? AlignmentY.Top :
          (AlignmentY) _alignmentYProperty.GetValue();
      }
      set { _alignmentYProperty.SetValue(value); }
    }

    public bool IsItemsHost
    {
      get { return _isItemsHost; }
      set { _isItemsHost = value; }
    }

    public override void Invalidate()
    {
      base.Invalidate();
      _updateRenderOrder = true;
      if (Window!=null) Window.Invalidate(this);
    }

    void SetupBrush()
    {
      if (Background != null && _backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        Background.SetupPrimitive(_backgroundContext);
        RenderPipeline.Instance.Add(_backgroundContext);
      }
    }

    protected virtual void RenderChildren()
    {
      foreach (UIElement element in _renderOrder)
      {
        if (element.IsVisible)
        {
          element.Render();
        }
      }
    }

    public void Update()
    {
      UpdateLayout();
      UpdateRenderOrder();
      if (_performLayout)
      {
        PerformLayout();
        _performLayout = false;
        _lastEvent = UIEvent.None;
      }
      else if (_lastEvent != UIEvent.None)
      {
        if ((_lastEvent & UIEvent.Hidden) != 0)
        {
          RenderPipeline.Instance.Remove(_backgroundContext);
          _backgroundContext = null;
          _performLayout = true;
        }
        if ((_lastEvent & UIEvent.OpacityChange) != 0)
        {
          SetupBrush();
        }
        _lastEvent = UIEvent.None;
      }
    }

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

        // ExtendedMatrix m = new ExtendedMatrix();
        //m.Matrix = Matrix.Translation(new Vector3((float)ActualPosition.X, (float)ActualPosition.Y, (float)ActualPosition.Z));
        //SkinContext.AddTransform(m);
        //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Background.BeginRender(_backgroundAsset.VertexBuffer, 2, PrimitiveType.TriangleList))
        {
          GraphicsDevice.Device.SetStreamSource(0, _backgroundAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);
          Background.EndRender();
        }
        // SkinContext.RemoveTransform();

        _backgroundAsset.LastTimeUsed = SkinContext.Now;
      }
      RenderChildren();
      SkinContext.RemoveOpacity();
    }

    public void PerformLayout()
    {
      //Trace.WriteLine("Panel.PerformLayout() " + this.Name + " -" + this.GetType().ToString());

      if (Background != null)
      {
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
        System.Drawing.RectangleF rect = new System.Drawing.RectangleF(-0.5f, -0.5f, rectSize.Width + 0.5f, rectSize.Height + 0.5f);
        rect.X += (float)ActualPosition.X;
        rect.Y += (float)ActualPosition.Y;
        PositionColored2Textured[] verts = new PositionColored2Textured[6];
        unchecked
        {
          verts[0].Position = m.Transform(new SlimDX.Vector3(rect.Left, rect.Top, 1.0f));
          verts[1].Position = m.Transform(new SlimDX.Vector3(rect.Left, rect.Bottom, 1.0f));
          verts[2].Position = m.Transform(new SlimDX.Vector3(rect.Right, rect.Bottom, 1.0f));
          verts[3].Position = m.Transform(new SlimDX.Vector3(rect.Left, rect.Top, 1.0f));
          verts[4].Position = m.Transform(new SlimDX.Vector3(rect.Right, rect.Top, 1.0f));
          verts[5].Position = m.Transform(new SlimDX.Vector3(rect.Right, rect.Bottom, 1.0f));

        }
        Background.SetupBrush(this, ref verts);
        if (SkinContext.UseBatching == false)
        {
          if (_backgroundAsset == null)
          {
            _backgroundAsset = new VisualAssetContext("Panel._backgroundAsset:" + this.Name);
            ContentManager.Add(_backgroundAsset);
          }
          _backgroundAsset.VertexBuffer = PositionColored2Textured.Create(6);
          PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
        }
        else
        {
          if (_backgroundContext == null)
          {
            _backgroundContext = new PrimitiveContext(2, ref verts);
            Background.SetupPrimitive(_backgroundContext);
            RenderPipeline.Instance.Add(_backgroundContext);
          }
          else
          {
            _backgroundContext.OnVerticesChanged(2, ref verts);
          }
        }
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


    public override void OnMouseMove(float x, float y)
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnMouseMove(x, y);
      }
    }

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      if (Children == null)
        return;
      foreach (UIElement element in Children)
      {
        element.FireUIEvent(eventType, source);
      }
      _lastEvent |= eventType;
      if (Window!=null) Window.Invalidate(this);
    }

    public override void OnKeyPressed(ref Key key)
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnKeyPressed(ref key);
        if (key == MediaPortal.Control.InputManager.Key.None) return;
      }
    }

    public override bool ReplaceElementType(Type t, UIElement newElement)
    {
      for (int i = 0; i < Children.Count; ++i)
      {
        if (Children[i].GetType() == t)
        {
          Children[i] = newElement;
          Children[i].VisualParent = this;
          Children[i].SetWindow(Window);
          return true;
        }
      }
      foreach (UIElement element in Children)
      {
        if (element.ReplaceElementType(t, newElement)) return true;
      }
      return false;
    }

    public override UIElement FindElement(IFinder finder)
    {
      UIElement found = base.FindElement(finder);
      if (found != null) return found;
      foreach (UIElement element in Children)
      {
        found = element.FindElement(finder);
        if (found != null) return found;
      }
      return null;
    }

    #region Focus prediction

    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusUp(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
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

    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusDown(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
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

    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
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

    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusRight(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
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

      if (_backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        _backgroundContext = null;
      }
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
      _performLayout = true;
    }

    #region IAddChild Members

    public void AddChild(UIElement o)
    {
      Children.Add(o);
    }

    #endregion

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_performLayout)
      {
        PerformLayout();
        _performLayout = false;
        _lastEvent = UIEvent.None;
      }
      foreach (UIElement child in Children)
      {
        child.BuildRenderTree();
      }
    }

    public override void DestroyRenderTree()
    {
      if (_backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        _backgroundContext = null;
      }
      foreach (UIElement child in Children)
      {
        child.DestroyRenderTree();
      }
    }

    public override void SetWindow(Window window)
    {
      base.SetWindow(window);
      foreach (UIElement child in Children)
      {
        child.SetWindow(window);
      }
    }

    #region Attached properties

    /// <summary>
    /// Getter method for the attached property <c>ZIndex</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be returned.</param>
    /// <returns>Value of the <c>ZIndex</c> property on the
    /// <paramref name="targetObject"/>.</returns>
    public static double GetZIndex(DependencyObject targetObject)
    {
      return targetObject.GetAttachedPropertyValue<double>(ZINDEX_ATTACHED_PROPERTY, 0.0);
    }

    /// <summary>
    /// Setter method for the attached property <c>ZIndex</c>.
    /// </summary>
    /// <param name="targetObject">The object whose property value will
    /// be set.</param>
    /// <param name="value">Value of the <c>ZIndex</c> property on the
    /// <paramref name="targetObject"/> to be set.</returns>
    public static void SetZIndex(DependencyObject targetObject, double value)
    {
      targetObject.SetAttachedPropertyValue<double>(ZINDEX_ATTACHED_PROPERTY, value);
    }

    /// <summary>
    /// Returns the <c>ZIndex</c> attached property for the
    /// <paramref name="targetObject"/>. When this method is called,
    /// the property will be created if it is not yet attached to the
    /// <paramref name="targetObject"/>.
    /// </summary>
    /// <param name="targetObject">The object whose attached
    /// property should be returned.</param>
    /// <returns>Attached <c>ZIndex</c> property.</returns>
    public static Property GetZIndexAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty(ZINDEX_ATTACHED_PROPERTY, -1.0);
    }

    #endregion
  }
}

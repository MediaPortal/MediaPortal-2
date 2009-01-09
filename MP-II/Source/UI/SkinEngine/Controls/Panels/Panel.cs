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

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using MediaPortal.SkinEngine;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;
using Brush=MediaPortal.SkinEngine.Controls.Brushes.Brush;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Finder implementation which looks for a panel which has its
  /// <see cref="Panel.IsItemsHost"/> property set.
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

    protected Property _childrenProperty;
    protected Property _backgroundProperty;
    protected bool _isItemsHost = false;
    protected bool _performLayout = true; // Mark panel to adapt background brush and related contents to the layout
    protected List<UIElement> _renderOrder; // Cache for the render order of our children
    protected bool _updateRenderOrder = true; // Mark panel to update its render order in the rendering thread
    protected VisualAssetContext _backgroundAsset;
    protected PrimitiveContext _backgroundContext;
    protected UIEvent _lastEvent = UIEvent.None;

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
      _backgroundProperty = new Property(typeof(Brush), null);
      _renderOrder = new List<UIElement>();
    }

    void Attach()
    {
      _backgroundProperty.Attach(OnBackgroundPropertyChanged);
    }

    void Detach()
    {
      _backgroundProperty.Detach(OnBackgroundPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Panel p = (Panel) source;
      Background = copyManager.GetCopy(p.Background);
      IsItemsHost = copyManager.GetCopy(p.IsItemsHost);
      foreach (UIElement el in p.Children)
        Children.Add(copyManager.GetCopy(el));
      Attach();
    }

    #endregion

    void OnBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.OpacityChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    /// <summary>
    /// Called when a layout property has changed
    /// we're simply calling Invalidate() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnLayoutPropertyChanged(Property property, object oldValue)
    {
      Invalidate();
    }

    protected void OnBackgroundPropertyChanged(Property property, object oldValue)
    {
      if (_backgroundAsset != null)
      {
        VisualAssetContext vac = _backgroundAsset;
        _backgroundAsset = null;
        vac.Free(false);
      }
      Brush oldBackground = oldValue as Brush;
      if (oldBackground != null)
        oldBackground.ObjectChanged -= OnBrushChanged;
      if (Background != null)
        Background.ObjectChanged += OnBrushChanged;
    }

    public Property BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return (Brush) _backgroundProperty.GetValue(); }
      set { _backgroundProperty.SetValue(value); }
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
      SetScreen(Screen);
      _updateRenderOrder = true;
      if (Screen != null) Screen.Invalidate(this);
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
      if (Screen != null) Screen.Invalidate(this);
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
        if (element.IsVisible)
          element.Render();
    }

    public void Update()
    {
      UpdateLayout();
      UpdateRenderOrder();
      if (_performLayout)
      {
        PerformLayout();
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
          SetupBrush();
        _lastEvent = UIEvent.None;
      }
    }

    public override void DoRender()
    {
      UpdateRenderOrder();

      SkinContext.AddOpacity(Opacity);
      if (Background != null)
      {
        if (_performLayout || (_backgroundAsset == null) || (_backgroundAsset != null && !_backgroundAsset.IsAllocated))
          PerformLayout();

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
      if (!_performLayout)
        return;
      _performLayout = false;

      //Trace.WriteLine("Panel.AdaptToLayoutChange() " + Name + " -" + GetType().ToString());

      if (Background != null)
      {
        SizeF actualSize = new SizeF((float) ActualWidth, (float) ActualHeight);

        ExtendedMatrix m = new ExtendedMatrix();
        if (_finalLayoutTransform != null)
          m.Matrix *= _finalLayoutTransform.Matrix;
        if (LayoutTransform != null)
        {
          ExtendedMatrix em;
          LayoutTransform.GetTransform(out em);
          m.Matrix *= em.Matrix;
        }
        m.InvertSize(ref actualSize);
        RectangleF rect = new RectangleF(-0.5f, -0.5f, actualSize.Width + 0.5f, actualSize.Height + 0.5f);
        rect.X += ActualPosition.X;
        rect.Y += ActualPosition.Y;
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
        if (SkinContext.UseBatching)
        {
          if (_backgroundContext == null)
          {
            _backgroundContext = new PrimitiveContext(2, ref verts);
            Background.SetupPrimitive(_backgroundContext);
            RenderPipeline.Instance.Add(_backgroundContext);
          }
          else
            _backgroundContext.OnVerticesChanged(2, ref verts);
        }
        else
        {
          if (_backgroundAsset == null)
          {
            _backgroundAsset = new VisualAssetContext("Panel._backgroundAsset:" + Name);
            ContentManager.Add(_backgroundAsset);
          }
          _backgroundAsset.VertexBuffer = PositionColored2Textured.Create(6);
          PositionColored2Textured.Set(_backgroundAsset.VertexBuffer, ref verts);
        }
      }
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
          _renderOrder.Add(element);
        _renderOrder.Sort(new ZOrderComparer());
      }
    }


    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      if (!ActualBounds.Contains(x, y))
        return;
      foreach (UIElement element in Children)
      {
        if (!element.IsVisible) continue;
        element.OnMouseMove(x, y);
      }
    }

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      if (Children == null)
        return;
      foreach (UIElement element in Children)
        element.FireUIEvent(eventType, source);
      _lastEvent |= eventType;
      if (Screen != null) Screen.Invalidate(this);
    }

    public override void OnKeyPressed(ref Key key)
    {
      foreach (UIElement element in Children)
      {
        if (!element.IsVisible) continue;
        element.OnKeyPressed(ref key);
        if (key == Key.None) return;
      }
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      CollectionUtils.AddAll(childrenOut, Children);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      foreach (FrameworkElement child in Children)
        child.Deallocate();
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
        child.Allocate();
      if (_backgroundAsset != null)
        ContentManager.Add(_backgroundAsset);
      if (Background != null)
        Background.Allocate();
      _performLayout = true;
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) return;
      if (_performLayout)
      {
        PerformLayout();
        _lastEvent = UIEvent.None;
      }
      foreach (UIElement child in Children)
        child.BuildRenderTree();
    }

    public override void DestroyRenderTree()
    {
      if (_backgroundContext != null)
      {
        RenderPipeline.Instance.Remove(_backgroundContext);
        _backgroundContext = null;
      }
      foreach (UIElement child in Children)
        child.DestroyRenderTree();
    }

    #region IAddChild<UIElement> Members

    public void AddChild(UIElement o)
    {
      Children.Add(o);
    }

    #endregion

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

#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;
using Brush=MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class ZOrderComparer : IComparer<UIElement>
  {
    #region IComparer<UIElement> Members

    public int Compare(UIElement x, UIElement y)
    {
      return Panel.GetZIndex(x).CompareTo(Panel.GetZIndex(y));
    }

    #endregion
  }

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

  public enum Orientation { Vertical, Horizontal };

  public class Panel : FrameworkElement, IAddChild<UIElement>
  {
    #region Constants

    protected const string ZINDEX_ATTACHED_PROPERTY = "Panel.ZIndex";

    #endregion

    #region Protected fields

    protected AbstractProperty _childrenProperty;
    protected AbstractProperty _backgroundProperty;
    protected bool _isItemsHost = false;
    protected volatile bool _performLayout = true; // Mark panel to adapt background brush and related contents to the layout
    protected List<UIElement> _renderOrder; // Cache for the render order of our children
    protected volatile bool _updateRenderOrder = true; // Mark panel to update its render order in the rendering thread
    protected PrimitiveContext _backgroundContext;

    #endregion

    #region Ctor

    public Panel()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      Children.Dispose();
    }

    void Init()
    {
      _childrenProperty = new SProperty(typeof(UIElementCollection), new UIElementCollection(this));
      _backgroundProperty = new SProperty(typeof(Brush), null);
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
      IsItemsHost = p.IsItemsHost;
      foreach (UIElement el in p.Children)
        Children.Add(copyManager.GetCopy(el));
      Attach();
    }

    #endregion

    void OnBrushChanged(IObservable observable)
    {
      _performLayout = true;
    }

    /// <summary>
    /// Called when a layout property has changed
    /// we're simply calling InvalidateLayout() here to invalidate the layout
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected void OnLayoutPropertyChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout();
    }

    protected void OnBackgroundPropertyChanged(AbstractProperty property, object oldValue)
    {
      Brush oldBackground = oldValue as Brush;
      if (oldBackground != null)
        oldBackground.ObjectChanged -= OnBrushChanged;
      if (Background != null)
        Background.ObjectChanged += OnBrushChanged;
      _performLayout = true;
    }

    public AbstractProperty BackgroundProperty
    {
      get { return _backgroundProperty; }
    }

    public Brush Background
    {
      get { return (Brush) _backgroundProperty.GetValue(); }
      set { _backgroundProperty.SetValue(value); }
    }

    public AbstractProperty ChildrenProperty
    {
      get { return _childrenProperty; }
    }

    public UIElementCollection Children
    {
      get { return (UIElementCollection) _childrenProperty.GetValue(); }
      internal set
      {
        UIElementCollection oldChildren = Children;
        _childrenProperty.SetValue(value);
        value.SetParent(this);
        _updateRenderOrder = true;
        oldChildren.SetParent(null);
        oldChildren.Dispose();
        InvalidateLayout();
      }
    }

    public bool IsItemsHost
    {
      get { return _isItemsHost; }
      set { _isItemsHost = value; }
    }

    protected override void ArrangeOverride()
    {
      RectangleF oldRect = new RectangleF(_innerRect.Location, _innerRect.Size);
      base.ArrangeOverride();
      if (!_innerRect.IsEmpty && oldRect != _innerRect)
        _performLayout = true;
      _updateRenderOrder = true;
    }

    protected virtual void RenderChildren(RenderContext localRenderContext)
    {
      foreach (UIElement element in _renderOrder)
        element.Render(localRenderContext);
    }

    public override void DoRender(RenderContext localRenderContext)
    {
      UpdateRenderOrder();

      PerformLayout(localRenderContext);

      if (_backgroundContext != null)
      {
        if (Background.BeginRenderBrush(_backgroundContext, localRenderContext))
        {
          GraphicsDevice.Device.VertexFormat = _backgroundContext.VertexFormat;
          GraphicsDevice.Device.SetStreamSource(0, _backgroundContext.VertexBuffer, 0, _backgroundContext.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(_backgroundContext.PrimitiveType, 0, 2);
          Background.EndRender();
        }
      }
      RenderChildren(localRenderContext);
    }

    public void PerformLayout(RenderContext localRenderContext)
    {
      if (!_performLayout)
        return;
      _performLayout = false;

      // Setup background brush
      DisposePrimitiveContext(ref _backgroundContext);
      if (Background != null)
      {
        SizeF actualSize = new SizeF((float) ActualWidth, (float) ActualHeight);

        RectangleF rect = new RectangleF(ActualPosition.X - 0.5f, ActualPosition.Y - 0.5f,
            actualSize.Width + 0.5f, actualSize.Height + 0.5f);

        PositionColored2Textured[] verts = new PositionColored2Textured[6];
        unchecked
        {
          verts[0].Position = new Vector3(rect.Left, rect.Top, 1.0f);
          verts[1].Position = new Vector3(rect.Left, rect.Bottom, 1.0f);
          verts[2].Position = new Vector3(rect.Right, rect.Bottom, 1.0f);
          verts[3].Position = new Vector3(rect.Left, rect.Top, 1.0f);
          verts[4].Position = new Vector3(rect.Right, rect.Top, 1.0f);
          verts[5].Position = new Vector3(rect.Right, rect.Bottom, 1.0f);
        }
        Background.SetupBrush(this, ref verts, localRenderContext.ZOrder, true);
        _backgroundContext = new PrimitiveContext(2, ref verts, PrimitiveType.TriangleList);
      }
    }

    protected IList<FrameworkElement> GetVisibleChildren()
    {
      IList<FrameworkElement> result = new List<FrameworkElement>(Children.Count);
      foreach (FrameworkElement child in Children)
        if (child.IsVisible)
          result.Add(child);
      return result;
    }

    protected virtual void UpdateRenderOrder()
    {
      if (!_updateRenderOrder) return;
      _updateRenderOrder = false;
      if (_renderOrder != null && Children != null)
      {
        Children.FixZIndex();
        _renderOrder.Clear();
        CollectionUtils.AddAll(_renderOrder, GetVisibleChildren());
        _renderOrder.Sort(new ZOrderComparer());
      }
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      base.AddChildren(childrenOut);
      CollectionUtils.AddAll(childrenOut, Children);
    }

    public override bool IsChildVisibleAt(UIElement child, float x, float y)
    {
      if (!child.IsInArea(x, y) || !IsInVisibleArea(x, y))
        return false;
      IList<UIElement> children = new List<UIElement>(GetChildren());
      // Iterate from last to first to find elements which are located on top
      for (int i = children.Count - 1; i <= 0; i--)
      {
        UIElement current = children[i];
        if (!current.IsVisible)
          continue;
        if (current == child)
          // Found our search target, all other children are located under the child so we can stop here
          break;
        if (current.IsInArea(x, y))
          return false;
      }
      return true;
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (Background != null)
        Background.Deallocate();

      DisposePrimitiveContext(ref _backgroundContext);
    }

    public override void Allocate()
    {
      base.Allocate();
      if (Background != null)
        Background.Allocate();
      _performLayout = true;
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
    public static AbstractProperty GetZIndexAttachedProperty(DependencyObject targetObject)
    {
      return targetObject.GetOrCreateAttachedProperty(ZINDEX_ATTACHED_PROPERTY, -1.0);
    }

    #endregion
  }
}

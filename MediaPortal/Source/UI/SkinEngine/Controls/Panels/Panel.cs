#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Utils;
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
  /// <summary>
  /// Matcher implementation which looks for a panel which has its
  /// <see cref="Panel.IsItemsHost"/> property set.
  /// </summary>
  public class ItemsHostMatcher: IMatcher
  {
    private static ItemsHostMatcher _instance = null;

    public bool Match(UIElement current)
    {
      return current is Panel && ((Panel) current).IsItemsHost;
    }

    public static ItemsHostMatcher Instance
    {
      get { return _instance ?? (_instance = new ItemsHostMatcher()); }
    }
  }

  public enum Orientation { Vertical, Horizontal };

  public abstract class Panel : FrameworkElement, IAddChild<FrameworkElement>
  {
    #region Constants

    protected const string ZINDEX_ATTACHED_PROPERTY = "Panel.ZIndex";

    #endregion

    #region Protected fields

    protected AbstractProperty _childrenProperty;
    protected AbstractProperty _backgroundProperty;
    protected bool _isItemsHost = false;
    protected volatile bool _performLayout = true; // Mark panel to adapt background brush and related contents to the layout
    protected List<FrameworkElement> _renderOrder = new List<FrameworkElement>(); // Cache for the render order of our children. Take care of locking out writing threads using the Children.SyncRoot.
    protected IList<AbstractProperty> _zIndexRegisteredProperties = new List<AbstractProperty>();
    protected volatile bool _updateRenderOrder = true; // Mark panel to update its render order in the rendering thread
    protected PrimitiveBuffer _backgroundContext;

    #endregion

    #region Ctor

    protected Panel()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      MPF.TryCleanupAndDispose(Background);
      Children.Dispose();
      base.Dispose();
    }

    void Init()
    {
      FrameworkElementCollection coll = new FrameworkElementCollection(this);
      coll.CollectionChanged += OnChildrenChanged;
      _childrenProperty = new SProperty(typeof(FrameworkElementCollection), coll);
      _backgroundProperty = new SProperty(typeof(Brush), null);
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
      FrameworkElementCollection children = Children;
      foreach (FrameworkElement el in p.Children)
        children.Add(copyManager.GetCopy(el), false);
      Attach();
    }

    #endregion

    void OnZIndexChanged(AbstractProperty property, object oldValue)
    {
      _updateRenderOrder = true;
    }

    void OnBrushChanged(IObservable observable)
    {
      _performLayout = true;
    }

    void OnChildrenChanged(FrameworkElementCollection coll)
    {
      _updateRenderOrder = true;
      InvalidateLayout(true, true);
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

    public FrameworkElementCollection Children
    {
      get { return (FrameworkElementCollection) _childrenProperty.GetValue(); }
    }

    public bool IsItemsHost
    {
      get { return _isItemsHost; }
      set { _isItemsHost = value; }
    }

    /// <summary>
    /// Returns the <paramref name="index"/>th child element. Actually, this could be the same as using
    /// the indexer <see cref="FrameworkElementCollection.this"/> on our <see cref="Children"/>, but <see cref="Children"/>
    /// are not filled by every panel instance. <see cref="VirtualizingStackPanel"/> for example doesn't materialize
    /// all children.
    /// </summary>
    /// <param name="index">Index of the child to return.</param>
    /// <returns>Child element or <c>null</c> if the index is out of range.</returns>
    public virtual FrameworkElement GetElement(int index)
    {
      lock (Children.SyncRoot)
      {
        if (index < 0 || index >= Children.Count)
          return null;
        return Children[index];
      }
    }

    /// <summary>
    /// Scrolls the panel's child with the given <paramref name="index"/> to the visible area, if possible.
    /// </summary>
    /// <param name="index">Index of the child to make visible. </param>
    public virtual void BringIntoView(int index)
    {
      FrameworkElement element = GetElement(index);
      if (element != null)
        BringIntoView(element, element.ActualBounds);
    }


    protected static float GetExtendsInOrientationDirection(Orientation orientation, SizeF size)
    {
      return orientation == Orientation.Vertical ? size.Height : size.Width;
    }

    protected static float GetExtendsInNonOrientationDirection(Orientation orientation, SizeF size)
    {
      return orientation == Orientation.Vertical ? size.Width : size.Height;
    }

    /// <summary>
    /// Summarizes the extends of the given <paramref name="elements"/> in orientation direction.
    /// </summary>
    /// <param name="elements">Elements to summarize.</param>
    /// <param name="orientation">Orientation in which the extends should be summarized.</param>
    /// <param name="startIndex">Index of the first element, inclusive.</param>
    /// <param name="endIndex">Index of the last element, exclusive.</param>
    /// <returns></returns>
    protected static double SumActualExtendsInOrientationDirection(IList<FrameworkElement> elements, Orientation orientation, int startIndex, int endIndex)
    {
      CalcHelper.Bound(ref startIndex, 0, elements.Count-1);
      CalcHelper.Bound(ref endIndex, 0, elements.Count); // End index is exclusive
      if (startIndex == endIndex || elements.Count == 0)
        return 0;
      bool invert = startIndex > endIndex;
      if (invert)
      {
        int tmp = startIndex;
        startIndex = endIndex;
        endIndex = tmp;
      }
      double result = 0;
      if (orientation == Orientation.Horizontal)
        for (int i = startIndex; i < endIndex; i++)
          result += elements[i].ActualWidth;
      else
        for (int i = startIndex; i < endIndex; i++)
          result += elements[i].ActualHeight;
      return invert ? -result : result;
    }

    protected override void ArrangeOverride()
    {
      PointF oldPosition = ActualPosition;
      double oldWidth = ActualWidth;
      double oldHeight = ActualHeight;
      base.ArrangeOverride();
      if (ActualWidth != 0 && ActualHeight != 0 &&
          (ActualWidth != oldWidth || ActualHeight != oldHeight || oldPosition != ActualPosition))
        _performLayout = true;
      _updateRenderOrder = true;
    }

    protected virtual void RenderChildren(RenderContext localRenderContext)
    {
      foreach (FrameworkElement element in _renderOrder)
        element.Render(localRenderContext);
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      UpdateRenderOrder();

      PerformLayout(localRenderContext);

      if (_backgroundContext != null && Background.BeginRenderBrush(_backgroundContext, localRenderContext))
      {
        _backgroundContext.Render(0);
        Background.EndRender();
      }

      RenderChildren(localRenderContext);
    }

    public void PerformLayout(RenderContext localRenderContext)
    {
      if (!_performLayout)
        return;
      _performLayout = false;

      // Setup background brush
      if (Background != null)
      {
        SizeF actualSize = new SizeF((float) ActualWidth, (float) ActualHeight);

        RectangleF rect = new RectangleF(ActualPosition.X - 0.5f, ActualPosition.Y - 0.5f,
            actualSize.Width + 0.5f, actualSize.Height + 0.5f);

        PositionColoredTextured[] verts = new PositionColoredTextured[6];
        verts[0].Position = new Vector3(rect.Left, rect.Top, 1.0f);
        verts[1].Position = new Vector3(rect.Left, rect.Bottom, 1.0f);
        verts[2].Position = new Vector3(rect.Right, rect.Bottom, 1.0f);
        verts[3].Position = new Vector3(rect.Left, rect.Top, 1.0f);
        verts[4].Position = new Vector3(rect.Right, rect.Top, 1.0f);
        verts[5].Position = new Vector3(rect.Right, rect.Bottom, 1.0f);
        Background.SetupBrush(this, ref verts, localRenderContext.ZOrder, true);
        PrimitiveBuffer.SetPrimitiveBuffer(ref _backgroundContext, ref verts, PrimitiveType.TriangleList);
      }
      else
        PrimitiveBuffer.DisposePrimitiveBuffer(ref _backgroundContext);
    }

    protected IList<FrameworkElement> GetVisibleChildren()
    {
      lock (Children.SyncRoot)
      {
        IList<FrameworkElement> result = new List<FrameworkElement>(Children.Count);
        foreach (FrameworkElement child in Children)
          if (child.IsVisible)
            result.Add(child);
        return result;
      }
    }

    protected void UpdateRenderOrder()
    {
      if (!_updateRenderOrder)
        return;
      _updateRenderOrder = false;
      lock (Children.SyncRoot)
      {
        foreach (AbstractProperty property in _zIndexRegisteredProperties)
          // Just detach from change handler and attach again later
          property.Detach(OnZIndexChanged);
        _zIndexRegisteredProperties.Clear();
        // The sort function which is used here must execute a stable sort, so don't use List.Sort, as that method is
        // specified to be unstable!
        IEnumerable<FrameworkElement> orderedElements = GetRenderedChildren().OrderBy(element =>
          {
            AbstractProperty prop = GetZIndexAttachedProperty_NoCreate(element);
            if (prop == null)
              return 0.0;
            prop.Attach(OnZIndexChanged);
            _zIndexRegisteredProperties.Add(prop);
            return (double) prop.GetValue();
          });
        _renderOrder.Clear();
        _renderOrder.AddRange(orderedElements);
      }
    }

    protected static void TryScheduleUpdateParentsRenderOrder(DependencyObject targetObject)
    {
      Visual v = targetObject as Visual;
      if (v != null)
      {
        Panel parent = v.VisualParent as Panel;
        if (parent != null)
          parent._updateRenderOrder = true;
      }
    }

    /// <summary>
    /// Returns all children which should be rendered.
    /// </summary>
    /// <remarks>
    /// The lock <see cref="FrameworkElementCollection.SyncRoot"/> is held on the <see cref="Children"/> collection while
    /// this method is called.
    /// </remarks>
    /// <returns>Enumeration of to-be-rendered children.</returns>
    protected virtual IEnumerable<FrameworkElement> GetRenderedChildren()
    {
      return GetVisibleChildren();
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      lock (Children.SyncRoot)
        CollectionUtils.AddAll(childrenOut, Children);
    }

    public override bool IsChildRenderedAt(UIElement child, float x, float y)
    {
      List<FrameworkElement> children;
      lock (Children.SyncRoot)
        children = new List<FrameworkElement>(_renderOrder);
      // Iterate from last to first to find elements which are located on top
      for (int i = children.Count - 1; i >= 0; i--)
      {
        FrameworkElement current = children[i];
        if (!current.IsVisible)
          continue;
        if (current == child)
          // Found our search target, so we are sure that the given coords aren't located inside another child's bounds
          break;
        // We found a child which is located on top of the given child and which contains the given coords
        if (current.IsInArea(x, y))
          return false;
      }
      return base.IsChildRenderedAt(child, x, y);
    }

    // Allocate/Deallocate of Children not necessary because UIElement handles all direct children

    public override void Deallocate()
    {
      base.Deallocate();
      if (Background != null)
        Background.Deallocate();

      PrimitiveBuffer.DisposePrimitiveBuffer(ref _backgroundContext);
    }

    public override void Allocate()
    {
      base.Allocate();
      if (Background != null)
        Background.Allocate();
      _performLayout = true;
    }

    #region IAddChild<UIElement> Members

    public void AddChild(FrameworkElement o)
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
    /// <paramref name="targetObject"/> to be set.</param>
    public static void SetZIndex(DependencyObject targetObject, double value)
    {
      targetObject.SetAttachedPropertyValue<double>(ZINDEX_ATTACHED_PROPERTY, value);
      // The parent will automatically attach to the ZIndex-Property when it updates its render order
      TryScheduleUpdateParentsRenderOrder(targetObject);
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
      AbstractProperty result = targetObject.GetAttachedProperty(ZINDEX_ATTACHED_PROPERTY);
      if (result != null)
        return result;
      // The parent will automatically attach to the ZIndex-Property when it updates its render order
      TryScheduleUpdateParentsRenderOrder(targetObject);
      return targetObject.GetOrCreateAttachedProperty(ZINDEX_ATTACHED_PROPERTY, 0.0);
    }

    public static AbstractProperty GetZIndexAttachedProperty_NoCreate(DependencyObject targetObject)
    {
      return targetObject.GetAttachedProperty(ZINDEX_ATTACHED_PROPERTY);
    }

    #endregion
  }
}

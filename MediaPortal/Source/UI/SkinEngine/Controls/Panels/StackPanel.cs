#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Utils;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Controls.Brushes;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class StackPanel : Panel, IScrollViewerFocusSupport, IScrollInfo
  {
    #region Consts

    /// <summary>
    /// To unburden our focus system, we only return a limited number of items when a focus movement request is made
    /// (Keys or PgDown/PgUp). Actually, we wouldn't need to consider more items than our visible range plus one item
    /// (Key up/down/left/right) resp. one page (PgUp/Down). But after the scroll command, the next element's arrangement
    /// doesn't happen before the next render pass, and thus, if our input thread is faster than our render thread, the focus
    /// might still be outside the visible range when the next scroll command arrives. This value is the number of items
    /// we consider for focus movement outside our visible range. It can be thought of as the number of render thread passes
    /// which might be missing when an input event arrives.
    /// </summary>
    public const int NUM_ADD_MORE_FOCUS_ELEMENTS = 2;

    #endregion

    #region Protected fields

    protected AbstractProperty _orientationProperty;
    protected AbstractProperty _loopScrollProperty;
    protected AbstractProperty _scrollMarginProperty;
    protected float _totalHeight;
    protected float _totalWidth;

    protected bool _doScroll = false; // Set to true by a scrollable container (ScrollViewer for example) if we should provide logical scrolling

    // Variables to pass a scroll job to the render thread. Method Arrange will process the request.
    protected int? _pendingScrollIndex = null;
    protected bool _scrollToFirst = true;

    // Index of the first visible item which will be drawn at our ActualPosition - updated by method Arrange.
    protected int _actualFirstVisibleChildIndex = 0;
    // Index before scrolling started
    protected int _originalFirstVisibleChildIndex = 0;

    // Index of the last visible child item. When scrolling, this index denotes the "opposite children" to the
    // child denoted by the _actualFirstVisibleChildIndex.
    protected int _actualLastVisibleChildIndex = -1;

    //Includes additional items that are rendered in the scroll margin
    protected int _actualFirstRenderedChildIndex;
    protected int _actualLastRenderedChildIndex;

    protected float _pendingPhysicalOffset = 0;
    protected float _actualPhysicalOffset = 0;
    protected bool _forcedOpacityMask = false;

    #endregion

    #region Ctor

    public StackPanel()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
    }

    void Init()
    {
      _orientationProperty = new SProperty(typeof(Orientation), Orientation.Vertical);
      _loopScrollProperty = new SProperty(typeof(bool), false);
      _scrollMarginProperty = new SProperty(typeof(Thickness), new Thickness());
    }

    void Attach()
    {
      _orientationProperty.Attach(OnMeasureGetsInvalid);
      _scrollMarginProperty.Attach(OnMeasureGetsInvalid);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnMeasureGetsInvalid);
      _scrollMarginProperty.Detach(OnMeasureGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      StackPanel p = (StackPanel) source;
      Orientation = p.Orientation;
      LoopScroll = p.LoopScroll;
      DoScroll = p.DoScroll;
      ScrollMargin = p.ScrollMargin;
      Attach();
    }

    #endregion

    #region Public properties

    public AbstractProperty OrientationProperty
    {
      get { return _orientationProperty; }
    }

    public Orientation Orientation
    {
      get { return (Orientation) _orientationProperty.GetValue(); }
      set { _orientationProperty.SetValue(value); }
    }

    public AbstractProperty LoopScrollProperty
    {
      get { return _loopScrollProperty; }
    }
    /// <summary>
    /// Whether to enable looping to first/last item when scrolling
    /// </summary>
    public bool LoopScroll
    {
      get { return (bool)_loopScrollProperty.GetValue(); }
      set { _loopScrollProperty.SetValue(value); }
    }

    public AbstractProperty ScrollMarginProperty
    {
      get { return _scrollMarginProperty; }
    }

    public Thickness ScrollMargin
    {
      get { return (Thickness)_scrollMarginProperty.GetValue(); }
      set { _scrollMarginProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    /// <summary>
    /// Sets the scrolling index to a value that the child with the given <paramref name="childIndex"/> is the
    /// first (in case <paramref name="first"/> is set to <c>true</c>) or last (<paramref name="first"/> is set to <c>false</c>)
    /// visible child.
    /// </summary>
    /// <remarks>
    /// The scroll index might be corrected by the layout system to a better value, if necessary.
    /// </remarks>
    /// <param name="childIndex">Index to scroll to.</param>
    /// <param name="first">Make the child with the given <paramref name="childIndex"/> the first or last shown element.</param>
    public void SetScrollIndex(int childIndex, bool first)
    {
      SetScrollIndex(childIndex, first, false);
    }

    /// <summary>
    /// Sets the scrolling index to a value that the child with the given <paramref name="childIndex"/> is the
    /// first (in case <paramref name="first"/> is set to <c>true</c>) or last (<paramref name="first"/> is set to <c>false</c>)
    /// visible child.
    /// </summary>
    /// <remarks>
    /// The scroll index might be corrected by the layout system to a better value, if necessary.
    /// </remarks>
    /// <param name="childIndex">Index to scroll to.</param>
    /// <param name="first">Make the child with the given <paramref name="childIndex"/> the first or last shown element.</param>
    /// <param name="force">Whether the scroll should happen immediately and not be delayed/animated.</param>
    public virtual void SetScrollIndex(double childIndex, bool first, bool force)
    {
      int index = (int)childIndex;
      float offset = (float)(childIndex % 1);
      lock (_renderLock)
      {
        if (_pendingScrollIndex == index && _pendingPhysicalOffset == offset && _scrollToFirst == first ||
            (!_pendingScrollIndex.HasValue && _actualPhysicalOffset == offset &&
             ((_scrollToFirst && _actualFirstVisibleChildIndex == index) ||
              (!_scrollToFirst && _actualLastVisibleChildIndex == index))))
          return;
        _pendingScrollIndex = index;
        _pendingPhysicalOffset = offset;
        _scrollToFirst = first;
      }
      InvalidateLayout(false, true);
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      float totalDesiredHeight = 0;
      float totalDesiredWidth = 0;
      SizeF childSize;
      if (Orientation == Orientation.Vertical)
        foreach (FrameworkElement child in GetVisibleChildren())
        {
          childSize = new SizeF(totalSize.Width, float.NaN);
          child.Measure(ref childSize);
          totalDesiredHeight += childSize.Height;
          if (childSize.Width > totalDesiredWidth)
            totalDesiredWidth = childSize.Width;
        }
      else
        foreach (FrameworkElement child in GetVisibleChildren())
        {
          childSize = new SizeF(float.NaN, totalSize.Height);
          child.Measure(ref childSize);
          totalDesiredWidth += childSize.Width;
          if (childSize.Height > totalDesiredHeight)
            totalDesiredHeight = childSize.Height;
        }
      return new SizeF(totalDesiredWidth, totalDesiredHeight);
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      ArrangeChildren();
    }

    protected virtual void ArrangeChildren()
    {
      bool fireScrolled = false;
      _totalHeight = 0;
      _totalWidth = 0;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int numVisibleChildren = visibleChildren.Count;
      if (numVisibleChildren > 0)
      {
        PointF actualPosition = ActualPosition;
        SizeF actualSize = new SizeF((float) ActualWidth, (float) ActualHeight);

        // For Orientation == vertical, this is ActualHeight, for horizontal it is ActualWidth
        float actualExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, actualSize);
        // For Orientation == vertical, this is ActualWidth, for horizontal it is ActualHeight
        float actualExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(Orientation, actualSize);
        //get scroll margins in scroll direction
        float scrollMarginBefore;
        float scrollMarginAfter;
        GetScrollMargin(out scrollMarginBefore, out scrollMarginAfter);
        // Hint: We cannot skip the arrangement of children above _actualFirstVisibleChildIndex or below _actualLastVisibleChildIndex
        // because the rendering and focus system also needs the bounds of the currently invisible children
        float startPosition = scrollMarginBefore;

        //Percentage of child size to offset child positions
        float physicalOffset = _actualPhysicalOffset;

        // If set to true, we'll check available space from the last to first visible child.
        // That is necessary if we want to scroll a specific child to the last visible position.
        bool invertLayouting = false;
        lock (_renderLock)
          if (_pendingScrollIndex.HasValue)
          {
            fireScrolled = true;
            int pendingSI = _pendingScrollIndex.Value;
            physicalOffset = _actualPhysicalOffset = _pendingPhysicalOffset;
            if (_scrollToFirst)
              _actualFirstVisibleChildIndex = pendingSI;
            else
            {
              _actualLastVisibleChildIndex = pendingSI;
              //If we have an offset then there will be part of an additional item visible
              if (physicalOffset != 0)
                _actualLastVisibleChildIndex++;
              invertLayouting = true;
            }
            _pendingScrollIndex = null;
          }

        // 1) Calculate scroll indices
        if (_doScroll)
        { // Calculate last visible child
          //Substract scroll margins from avalable space, additional items in the margin will be added later
          float spaceLeft = actualExtendsInOrientationDirection - scrollMarginBefore - scrollMarginAfter;
          if (invertLayouting)
          {
            CalcHelper.Bound(ref _actualLastVisibleChildIndex, 0, numVisibleChildren - 1);

            //Allow space for partially visible items at top and bottom
            if (physicalOffset != 0)
            {
              float offsetItemSize = GetExtendsInOrientationDirection(Orientation, visibleChildren[_actualLastVisibleChildIndex].DesiredSize);
              spaceLeft += offsetItemSize;
              startPosition -= offsetItemSize * physicalOffset;
            }

            _actualFirstVisibleChildIndex = _actualLastVisibleChildIndex + 1;
            while (_actualFirstVisibleChildIndex > 0)
            {
              FrameworkElement child = visibleChildren[_actualFirstVisibleChildIndex - 1];
              spaceLeft -= GetExtendsInOrientationDirection(Orientation, child.DesiredSize);
              if (spaceLeft + DELTA_DOUBLE < 0)
                break; // Found item which is not visible any more
              _actualFirstVisibleChildIndex--;
            }
            if (_actualFirstVisibleChildIndex > _actualLastVisibleChildIndex)
              // Happens if the item at _actualFirstVisibleChildIndex is bigger than the available space
              _actualFirstVisibleChildIndex = _actualLastVisibleChildIndex;
            if (spaceLeft > 0)
            { // Correct the last scroll index to fill the available space
              while (_actualLastVisibleChildIndex < numVisibleChildren - 1)
              {
                FrameworkElement child = visibleChildren[_actualLastVisibleChildIndex + 1];
                spaceLeft -= GetExtendsInOrientationDirection(Orientation, child.DesiredSize);
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break; // Found item which is not visible any more
                _actualLastVisibleChildIndex++;
              }
            }
          }
          else
          {
            CalcHelper.Bound(ref _actualFirstVisibleChildIndex, 0, numVisibleChildren - 1);

            //Allow space for partially visible items at top and bottom
            if (physicalOffset != 0)
            {
              float offsetItemSize = GetExtendsInOrientationDirection(Orientation, visibleChildren[_actualFirstVisibleChildIndex].DesiredSize);
              spaceLeft += offsetItemSize;
              startPosition -= offsetItemSize * physicalOffset;
            }

            _actualLastVisibleChildIndex = _actualFirstVisibleChildIndex - 1;
            while (_actualLastVisibleChildIndex < numVisibleChildren - 1)
            {
              FrameworkElement child = visibleChildren[_actualLastVisibleChildIndex + 1];
              spaceLeft -= GetExtendsInOrientationDirection(Orientation, child.DesiredSize);
              if (spaceLeft + DELTA_DOUBLE < 0)
                break; // Found item which is not visible any more
              _actualLastVisibleChildIndex++;
            }
            if (_actualLastVisibleChildIndex < _actualFirstVisibleChildIndex)
              // Happens if the item at _actualFirstVisibleChildIndex is bigger than the available space
              _actualLastVisibleChildIndex = _actualFirstVisibleChildIndex;
            if (spaceLeft > 0)
            { // Correct the first scroll index to fill the available space
              while (_actualFirstVisibleChildIndex > 0)
              {
                FrameworkElement child = visibleChildren[_actualFirstVisibleChildIndex - 1];
                spaceLeft -= GetExtendsInOrientationDirection(Orientation, child.DesiredSize);
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break; // Found item which is not visible any more
                _actualFirstVisibleChildIndex--;
              }
            }
          }
        }
        else
        {
          _actualFirstVisibleChildIndex = 0;
          _actualLastVisibleChildIndex = numVisibleChildren - 1;
        }

        _actualFirstRenderedChildIndex = _actualFirstVisibleChildIndex;
        _actualLastRenderedChildIndex = _actualLastVisibleChildIndex;
        //calculate additional items in the scroll margin
        float inactiveSpaceLeft = scrollMarginBefore;
        while (_actualFirstRenderedChildIndex > 0)
        {
          inactiveSpaceLeft -= GetExtendsInOrientationDirection(Orientation, visibleChildren[_actualFirstRenderedChildIndex - 1].DesiredSize);
          if (inactiveSpaceLeft + DELTA_DOUBLE < 0)
            break;
          _actualFirstRenderedChildIndex--;
        }
        inactiveSpaceLeft = scrollMarginAfter;
        while (_actualLastRenderedChildIndex < visibleChildren.Count - 1)
        {
          inactiveSpaceLeft -= GetExtendsInOrientationDirection(Orientation, visibleChildren[_actualLastRenderedChildIndex].DesiredSize);
          if (inactiveSpaceLeft + DELTA_DOUBLE < 0)
            break;
          _actualLastRenderedChildIndex++;
        }

        // 2) Calculate start position
        for (int i = 0; i < _actualFirstVisibleChildIndex; i++)
        {
          FrameworkElement child = visibleChildren[i];
          startPosition -= GetExtendsInOrientationDirection(Orientation, child.DesiredSize);
        }

        // 3) Arrange children
        if (Orientation == Orientation.Vertical)
          _totalWidth = actualExtendsInNonOrientationDirection;
        else
          _totalHeight = actualExtendsInNonOrientationDirection;
        foreach (FrameworkElement child in visibleChildren)
        {
          SizeF childSize = child.DesiredSize;
          // For Orientation == vertical, this is childSize.Height, for horizontal it is childSize.Width
          float desiredExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, childSize);
          if (Orientation == Orientation.Vertical)
          {
            PointF position = new PointF(actualPosition.X, actualPosition.Y + startPosition);

            childSize.Width = actualExtendsInNonOrientationDirection;

            ArrangeChildHorizontal(child, child.HorizontalAlignment, ref position, ref childSize);
            child.Arrange(SharpDXExtensions.CreateRectangleF(position, childSize));
            _totalHeight += desiredExtendsInOrientationDirection;

            startPosition += desiredExtendsInOrientationDirection;
          }
          else
          {
            PointF position = new PointF(actualPosition.X + startPosition, actualPosition.Y);

            childSize.Height = actualExtendsInNonOrientationDirection;

            ArrangeChildVertical(child, child.VerticalAlignment, ref position, ref childSize);
            child.Arrange(SharpDXExtensions.CreateRectangleF(position, childSize));
            _totalWidth += desiredExtendsInOrientationDirection;

            startPosition += desiredExtendsInOrientationDirection;
          }
        }
        // 4) Add size gap for the last item if we use logical scrolling. If we scroll to the bottom/to the right, there might be a gap from the last item
        //    to the end of the area. We need to add that gap to make the scroll bars show the correct size.
        if (_doScroll)
        {
          float spaceLeft = actualExtendsInOrientationDirection;
          for (int i = _actualFirstVisibleChildIndex; i <= _actualFirstVisibleChildIndex; i++)
          {
            FrameworkElement child = visibleChildren[i];
            float childSize = GetExtendsInOrientationDirection(Orientation, child.DesiredSize);
            if (childSize < spaceLeft + DELTA_DOUBLE)
              spaceLeft -= childSize;
            else
            {
              if (Orientation == Orientation.Vertical)
                _totalHeight += spaceLeft;
              else
                _totalWidth += spaceLeft;
              break;
            }
          }
        }
      }
      else
      {
        _actualFirstVisibleChildIndex = _actualFirstRenderedChildIndex = 0;
        _actualLastVisibleChildIndex = _actualLastRenderedChildIndex = -1;
      }
      if (fireScrolled)
        InvokeScrolled();
    }

    protected void GetScrollMargin(out float widthBefore, out float widthAfter)
    {
      Thickness thickness = ScrollMargin ?? new Thickness();
      if (Orientation == Orientation.Horizontal)
      {
        widthBefore = thickness.Left;
        widthAfter = thickness.Right;
      }
      else
      {
        widthBefore = thickness.Top;
        widthAfter = thickness.Bottom;
      }
    }

    protected void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      BringIntoView(element, ref elementBounds);
      base.BringIntoView(element, elementBounds);
    }

    protected virtual void BringIntoView(UIElement element, ref RectangleF elementBounds)
    {
      if (_doScroll)
      {
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        int index = 0;
        foreach (FrameworkElement currentChild in visibleChildren)
        {
          if (InVisualPath(currentChild, element))
          {
            int oldFirstVisibleChild = _actualFirstVisibleChildIndex;
            int oldLastVisibleChild = _actualLastVisibleChildIndex;
            bool first;
            if (index < oldFirstVisibleChild)
              first = true;
            else if (index <= oldLastVisibleChild)
              // Already visible
              break;
            else
              first = false;
            SetScrollIndex(index, first);
            // Adjust the scrolled element's bounds; Calculate the difference between positions of childen at old/new child indices
            float extendsInOrientationDirection = (float) SumActualExtendsInOrientationDirection(visibleChildren, Orientation,
                first ? oldFirstVisibleChild : oldLastVisibleChild, index);
            if (Orientation == Orientation.Horizontal)
              elementBounds.X -= extendsInOrientationDirection;
            else
              elementBounds.Y -= extendsInOrientationDirection;
            break;
          }
          index++;
        }
      }
    }

    public override bool IsChildRenderedAt(UIElement child, float x, float y)
    {
      if (_doScroll)
      { // If we can scroll, check if child is completely in our range -> if not, it won't be rendered and thus isn't visible
        RectangleF elementBounds = ((FrameworkElement) child).ActualBounds;
        RectangleF bounds = ActualBounds;
        if (elementBounds.Right > bounds.Right + DELTA_DOUBLE) return false;
        if (elementBounds.Left < bounds.Left - DELTA_DOUBLE) return false;
        if (elementBounds.Top < bounds.Top - DELTA_DOUBLE) return false;
        if (elementBounds.Bottom > bounds.Bottom + DELTA_DOUBLE) return false;
      }
      return base.IsChildRenderedAt(child, x, y);
    }

    #endregion

    #region Focus management

    public override void AddPotentialFocusableElements(RectangleF? startingRect, ICollection<FrameworkElement> elements)
    {
      AlignedPanelAddPotentialFocusNeighbors(startingRect, elements, false);
    }

    public virtual void AlignedPanelAddPotentialFocusNeighbors(RectangleF? startingRect, ICollection<FrameworkElement> outElements,
        bool elementsBeforeAndAfter)
    {
      if (!IsVisible)
        return;
      if (Focusable)
        outElements.Add(this);
      IList<FrameworkElement> children = GetVisibleChildren();
      int numElementsBeforeAndAfter = elementsBeforeAndAfter ? NUM_ADD_MORE_FOCUS_ELEMENTS : 0;
      AddFocusedElementRange(children, startingRect, _actualFirstVisibleChildIndex, _actualLastVisibleChildIndex,
          numElementsBeforeAndAfter, numElementsBeforeAndAfter, outElements);
    }

    protected void AddFocusedElementRange(IList<FrameworkElement> availableElements, RectangleF? startingRect,
        int firstElementIndex, int lastElementIndex, int elementsBefore, int elementsAfter, ICollection<FrameworkElement> outElements)
    {
      int numItems = availableElements.Count;
      if (numItems == 0)
        return;
      CalcHelper.Bound(ref firstElementIndex, 0, numItems - 1);
      CalcHelper.Bound(ref lastElementIndex, 0, numItems - 1);
      if (elementsBefore > 0)
      {
        // Find children before the first index which have focusable elements.
        int formerNumElements = outElements.Count;
        for (int i = firstElementIndex - 1; i >= 0; i--)
        {
          FrameworkElement fe = availableElements[i];
          fe.AddPotentialFocusableElements(startingRect, outElements);
          if (formerNumElements == outElements.Count)
            continue;
          // Found focusable elements
          elementsBefore--;
          if (elementsBefore == 0)
            break;
          formerNumElements = outElements.Count;
        }
      }
      for (int i = firstElementIndex; i <= lastElementIndex; i++)
      {
        FrameworkElement fe = availableElements[i];
        fe.AddPotentialFocusableElements(startingRect, outElements);
      }
      if (elementsAfter > 0)
      {
        // Find children after the last index which have focusable elements.
        int formerNumElements = outElements.Count;
        for (int i = lastElementIndex + 1; i < availableElements.Count; i++)
        {
          FrameworkElement fe = availableElements[i];
          fe.AddPotentialFocusableElements(startingRect, outElements);
          if (formerNumElements == outElements.Count)
            continue;
          // Found focusable elements
          elementsAfter--;
          if (elementsAfter == 0)
            break;
          formerNumElements = outElements.Count;
        }
      }
    }

    protected bool AlignedPanelMoveFocus1(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      RectangleF currentFocusRect = currentElement.ActualBounds;
      ICollection<FrameworkElement> focusableChildren = new List<FrameworkElement>();
      AlignedPanelAddPotentialFocusNeighbors(currentFocusRect, focusableChildren, true);
      // Check child controls
      if (focusableChildren.Count == 0)
        return false;
      FrameworkElement nextElement = FindNextFocusElement(focusableChildren, currentFocusRect, direction);
      return nextElement != null && nextElement.TrySetFocus(true);
    }

    /// <summary>
    /// Focuses the first item if the last item currently has focus
    /// </summary>
    /// <returns>true if the first item was focused</returns>
    protected virtual bool TryLoopToFirstItem()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      if (!InVisualPath(visibleChildren[visibleChildren.Count - 1], currentElement))
        return false;
      //last item has focus, focus first item
      SetScrollIndex(0, true);
      visibleChildren[0].SetFocusPrio = SetFocusPriority.Default;
      return true;
    }

    /// <summary>
    /// Focuses the last item if the first item currently has focus
    /// </summary>
    /// <returns>true if the last item was focused</returns>
    protected virtual bool TryLoopToLastItem()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      if (!InVisualPath(visibleChildren[0], currentElement))
        return false;
      //first item has focus, focus last item
      SetScrollIndex(int.MaxValue, false);
      visibleChildren[visibleChildren.Count - 1].SetFocusPrio = SetFocusPriority.Default;
      return true;
    }

    #endregion

    #region Base overrides

    public override void BringIntoView(int index)
    {
      if (index < _actualFirstVisibleChildIndex)
        SetScrollIndex(index, true, true);
      else if (index > _actualLastVisibleChildIndex)
        SetScrollIndex(index, false, true);
    }

    public override void SaveUIState(IDictionary<string, object> state, string prefix)
    {
      base.SaveUIState(state, prefix);
      state[prefix + "/FirstVisibleChild"] = _actualFirstVisibleChildIndex;
    }

    public override void RestoreUIState(IDictionary<string, object> state, string prefix)
    {
      base.RestoreUIState(state, prefix);
      object first;
      int? iFirst;
      if (state.TryGetValue(prefix + "/FirstVisibleChild", out first) && (iFirst = first as int?).HasValue)
        SetScrollIndex(iFirst.Value, true, true);
    }

    #endregion

    #region Rendering

    protected override IEnumerable<FrameworkElement> GetRenderedChildren()
    {
      return GetVisibleChildren().Skip(_actualFirstRenderedChildIndex).Take(_actualLastRenderedChildIndex - _actualFirstRenderedChildIndex + 1);
    }

    /// <summary>
    /// Required to clip any items that are partially within the panel's bounds
    /// </summary>
    /// <param name="parentRenderContext"></param>
    public override void Render(RenderContext parentRenderContext)
    {
      if (OpacityMask == null && _actualPhysicalOffset != 0)
      {
        SolidColorBrush brush = new SolidColorBrush { Color = Color.Black };
        OpacityMask = brush;
        _forcedOpacityMask = true;
      }
      else if (_forcedOpacityMask && _actualPhysicalOffset == 0 && OpacityMask != null)
      {
        OpacityMask.Dispose();
        OpacityMask = null;
        _opacityMaskContext.Dispose();
        _opacityMaskContext = null;
        _forcedOpacityMask = false;
      }
      base.Render(parentRenderContext);
    }

    public override void RenderOverride(RenderContext localRenderContext)
    {
      base.RenderOverride(localRenderContext); // Do the actual rendering
      // After rendering our children the following line resets the RenderContext's bounds so
      // that rendering with an OpacityMask will clip the final output correctly to our scrolled viewport.
      localRenderContext.SetUntransformedBounds(ActualBounds);
    }

    #endregion

    #region IScrollViewerFocusSupport implementation

    public virtual bool FocusUp()
    {
      if (Orientation == Orientation.Vertical)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Up) || (LoopScroll && TryLoopToLastItem());
      return false;
    }

    public virtual bool FocusDown()
    {
      if (Orientation == Orientation.Vertical)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Down) || (LoopScroll && TryLoopToFirstItem());
      return false;
    }

    public virtual bool FocusLeft()
    {
      if (Orientation == Orientation.Horizontal)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Left) || (LoopScroll && TryLoopToLastItem());
      return false;
    }

    public virtual bool FocusRight()
    {
      if (Orientation == Orientation.Horizontal)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Right) || (LoopScroll && TryLoopToFirstItem());
      return false;
    }

    public virtual bool FocusPageUp()
    {
      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        int firstVisibleChildIndex = _actualFirstVisibleChildIndex;
        CalcHelper.Bound(ref firstVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement firstVisibleChild = visibleChildren[firstVisibleChildIndex];
        float limitPosition;
        if (InVisualPath(firstVisibleChild, currentElement))
          // The topmost element is focused - move one page up
          limitPosition = firstVisibleChild.ActualBounds.Bottom - (float) ActualHeight;
        else
          // An element inside our visible range is focused - move to first element
          limitPosition = ActualPosition.Y;
        int firstPlusOne = firstVisibleChildIndex + 1;
        CalcHelper.Bound(ref firstPlusOne, 0, visibleChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren.Take(firstPlusOne), currentElement.ActualBounds, MoveFocusDirection.Up)) != null &&
            (nextElement.ActualPosition.Y > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusPageDown()
    {
      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        int lastVisibleChildIndex = _actualLastVisibleChildIndex;
        CalcHelper.Bound(ref lastVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement lastVisibleChild = visibleChildren[lastVisibleChildIndex];
        float limitPosition;
        if (InVisualPath(lastVisibleChild, currentElement))
          // The element at the bottom is focused - move one page down
          limitPosition = lastVisibleChild.ActualPosition.Y + (float) ActualHeight;
        else
          // An element inside our visible range is focused - move to last element
          limitPosition = ActualPosition.Y + (float) ActualHeight;
        int lastMinusOne = lastVisibleChildIndex - 1;
        CalcHelper.Bound(ref lastMinusOne, 0, visibleChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren.Skip(lastMinusOne), currentElement.ActualBounds, MoveFocusDirection.Down)) != null &&
            (nextElement.ActualBounds.Bottom < limitPosition + DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusPageLeft()
    {
      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        int firstVisibleChildIndex = _actualFirstVisibleChildIndex;
        CalcHelper.Bound(ref firstVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement firstVisibleChild = visibleChildren[firstVisibleChildIndex];
        float limitPosition;
        if (InVisualPath(firstVisibleChild, currentElement))
          // The leftmost element is focused - move one page left
          limitPosition = firstVisibleChild.ActualBounds.Right - (float) ActualWidth;
        else
          // An element inside our visible range is focused - move to first element
          limitPosition = ActualPosition.X;
        int firstPlusOne = firstVisibleChildIndex + 1;
        CalcHelper.Bound(ref firstPlusOne, 0, visibleChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren.Take(firstPlusOne), currentElement.ActualBounds, MoveFocusDirection.Left)) != null &&
            (nextElement.ActualPosition.X > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusPageRight()
    {
      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        int lastVisibleChildIndex = _actualLastVisibleChildIndex;
        CalcHelper.Bound(ref lastVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement lastVisibleChild = visibleChildren[lastVisibleChildIndex];
        float limitPosition;
        if (InVisualPath(lastVisibleChild, currentElement))
          // The element at the bottom is focused - move one page down
          limitPosition = lastVisibleChild.ActualPosition.X + (float) ActualWidth;
        else
          // An element inside our visible range is focused - move to last element
          limitPosition = ActualPosition.X + (float) ActualWidth;
        int lastMinusOne = lastVisibleChildIndex - 1;
        CalcHelper.Bound(ref lastMinusOne, 0, visibleChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren.Skip(lastMinusOne), currentElement.ActualBounds, MoveFocusDirection.Right)) != null &&
            (nextElement.ActualBounds.Right < limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusHome()
    {
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      SetScrollIndex(0, true, true);
      visibleChildren[0].SetFocusPrio = SetFocusPriority.Default;
      return true;
    }

    public virtual bool FocusEnd()
    {
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      SetScrollIndex(int.MaxValue, false, true);
      visibleChildren[visibleChildren.Count - 1].SetFocusPrio = SetFocusPriority.Default;
      return true;
    }

    public virtual bool ScrollDown(int numLines)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (IsViewPortAtBottom)
          return false;
        CalcHelper.Bound(ref numLines, 0, _actualLastVisibleChildIndex - _actualFirstVisibleChildIndex);
        if (numLines < 1)
          numLines = 1;
        SetScrollIndex(_actualFirstVisibleChildIndex + numLines, true);
        return true;
      }
      return false;
    }

    public virtual bool ScrollUp(int numLines)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (IsViewPortAtTop)
          return false;
        CalcHelper.Bound(ref numLines, 0, _actualLastVisibleChildIndex - _actualFirstVisibleChildIndex);
        if (numLines < 1)
          numLines = 1;
        SetScrollIndex(_actualFirstVisibleChildIndex - numLines, true);
        return true;
      }
      return false;
    }

    public bool Scroll(float deltaX, float deltaY)
    {
      SizeF actualSize = new SizeF((float)ActualWidth, (float)ActualHeight);

      // For Orientation == vertical, this is ActualHeight, for horizontal it is ActualWidth
      float actualExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, actualSize);
      int visibleLines = NumberOfVisibleLines;

      if (visibleLines == 0)
        return false;

      int numLines = Orientation == Orientation.Vertical ?
        (int)(deltaY / (actualExtendsInOrientationDirection / visibleLines)) :
        (int)(deltaX / (actualExtendsInOrientationDirection / visibleLines));

      SetScrollIndex(_originalFirstVisibleChildIndex - numLines, true);
      return false;
    }

    public bool BeginScroll()
    {
      _originalFirstVisibleChildIndex = _actualFirstVisibleChildIndex;
      return true;
    }

    public bool EndScroll()
    {
      _originalFirstVisibleChildIndex = 0;
      return true;
    }

    #endregion

    #region IScrollInfo implementation

    public event ScrolledDlgt Scrolled;

    public virtual bool DoScroll
    {
      get { return _doScroll; }
      set { _doScroll = value; }
    }

    public virtual float TotalWidth
    {
      get { return _totalWidth; }
    }

    public virtual float TotalHeight
    {
      get { return _totalHeight; }
    }

    public virtual float ViewPortWidth
    {
      get { return (float) ActualWidth; }
    }

    public virtual float ViewPortHeight
    {
      get { return (float) ActualHeight; }
    }

    public virtual float ViewPortStartX
    {
      get
      {
        return Orientation == Orientation.Vertical ? 0 : (float) SumActualExtendsInOrientationDirection(
            GetVisibleChildren(), Orientation, 0, _actualFirstVisibleChildIndex);
      }
    }

    public virtual float ViewPortStartY
    {
      get
      {
        return Orientation == Orientation.Horizontal ? 0 : (float) SumActualExtendsInOrientationDirection(
            GetVisibleChildren(), Orientation, 0, _actualFirstVisibleChildIndex);
      }
    }

    public virtual bool IsViewPortAtTop
    {
      get { return Orientation == Orientation.Horizontal || _actualFirstVisibleChildIndex == 0; }
    }

    public virtual bool IsViewPortAtBottom
    {
      get { return Orientation == Orientation.Horizontal || _actualLastVisibleChildIndex == GetVisibleChildren().Count - 1; }
    }

    public virtual bool IsViewPortAtLeft
    {
      get { return Orientation == Orientation.Vertical || _actualFirstVisibleChildIndex == 0; }
    }

    public virtual bool IsViewPortAtRight
    {
      get { return Orientation == Orientation.Vertical || _actualLastVisibleChildIndex == GetVisibleChildren().Count - 1; }
    }

    public int NumberOfVisibleLines
    {
      get
      {
        int numLines = _actualLastVisibleChildIndex - _actualFirstVisibleChildIndex + 1;
        return numLines > 0 ? numLines : 0;
      }
    }

    #endregion
  }
}

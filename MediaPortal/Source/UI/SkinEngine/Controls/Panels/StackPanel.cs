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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

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
    protected float _totalHeight;
    protected float _totalWidth;

    protected bool _canScroll = false; // Set to true by a scrollable container (ScrollViewer for example) if we should provide logical scrolling

    // Variables to pass a scroll job to the render thread
    protected int _pendingScrollIndex = -1;
    protected bool _scrollToFirst = true;

    // Index of the first visible item which will be drawn at our ActualPosition - when modified by method
    // SetScrollOffset, it will be applied the next time Arrange is called.
    protected int _actualFirstVisibleChild = 0;
    
    // Index of the last visible child item. When scrolling, this index denotes the "opposite children" to the
    // child denoted by the _scrollOffset.
    protected int _actualLastVisibleChild = -1;

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
    }

    void Attach()
    {
      _orientationProperty.Attach(OnMeasureGetsInvalid);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnMeasureGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      StackPanel p = (StackPanel) source;
      Orientation = p.Orientation;
      CanScroll = p.CanScroll;
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
    public virtual void SetScrollIndex(int childIndex, bool first)
    {
      lock (_renderLock)
      {
        if (_pendingScrollIndex == childIndex && _scrollToFirst == first ||
            (_pendingScrollIndex == -1 &&
             ((_scrollToFirst && _actualFirstVisibleChild == childIndex) ||
              (!_scrollToFirst && _actualLastVisibleChild == childIndex))))
          return;
        _pendingScrollIndex = childIndex;
        _scrollToFirst = first;
      }
      InvalidateLayout(false, true);
      InvokeScrolled();
    }

    protected float GetExtendsInOrientationDirection(SizeF size)
    {
      return Orientation == Orientation.Vertical ? size.Height : size.Width;
    }

    protected float GetExtendsInNonOrientationDirection(SizeF size)
    {
      return Orientation == Orientation.Vertical ? size.Width : size.Height;
    }

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
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
      _totalHeight = 0;
      _totalWidth = 0;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int numVisibleChildren = visibleChildren.Count;
      if (numVisibleChildren > 0)
      {
        SizeF actualSize = new SizeF((float) ActualWidth, (float) ActualHeight);

        // For Orientation == vertical, this is ActualHeight, for horizontal it is ActualWidth
        float actualExtendsInOrientationDirection = GetExtendsInOrientationDirection(actualSize);
        // For Orientation == vertical, this is ActualWidth, for horizontal it is ActualHeight
        float actualExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(actualSize);
        // Hint: We cannot skip the arrangement of children above _actualFirstVisibleChild or below _actualLastVisibleChild
        // because the rendering and focus system also needs the bounds of the currently invisible children
        float startPosition = 0;
        // If set to true, we'll check available space from the last to first visible child.
        // That is necessary if we want to scroll a specific child to the last visible position.
        bool invertLayouting = false;
        lock (_renderLock)
          if (_pendingScrollIndex != -1)
          {
            if (_scrollToFirst)
              _actualFirstVisibleChild = _pendingScrollIndex;
            else
            {
              _actualLastVisibleChild = _pendingScrollIndex;
              invertLayouting = true;
            }
            _pendingScrollIndex = -1;
          }
        if (!_canScroll)
        {
          _actualFirstVisibleChild = 0;
          invertLayouting = false;
        }
        
        // 1) Calculate scroll indices
        float spaceLeft = actualExtendsInOrientationDirection;

        if (invertLayouting)
        {
          Bound(ref _actualLastVisibleChild, 0, numVisibleChildren - 1);
          _actualFirstVisibleChild = _actualLastVisibleChild + 1;
          for (int i = _actualLastVisibleChild; i >= 0; i--)
          {
            FrameworkElement child = visibleChildren[i];
            spaceLeft -= GetExtendsInOrientationDirection(child.DesiredSize);
            if (spaceLeft + DELTA_DOUBLE < 0)
              break; // Found item which is not visible any more
            _actualFirstVisibleChild = i;
          }
          if (spaceLeft > 0)
          { // We need to correct the last scroll index
            for (int i = _actualLastVisibleChild + 1; i < numVisibleChildren; i++)
            {
              FrameworkElement child = visibleChildren[i];
              spaceLeft -= GetExtendsInOrientationDirection(child.DesiredSize);
              if (spaceLeft + DELTA_DOUBLE < 0)
                break; // Found item which is not visible any more
              _actualLastVisibleChild = i;
            }
          }
        }
        else
        {
          Bound(ref _actualFirstVisibleChild, 0, numVisibleChildren - 1);
          _actualLastVisibleChild = _actualFirstVisibleChild - 1;
          for (int i = _actualFirstVisibleChild; i < numVisibleChildren; i++)
          {
            FrameworkElement child = visibleChildren[i];
            spaceLeft -= GetExtendsInOrientationDirection(child.DesiredSize);
            if (spaceLeft + DELTA_DOUBLE < 0)
              break; // Found item which is not visible any more
            _actualLastVisibleChild = i;
          }
          if (spaceLeft > 0)
          { // We need to correct the first scroll index
            for (int i = _actualFirstVisibleChild - 1; i >= 0; i--)
            {
              FrameworkElement child = visibleChildren[i];
              spaceLeft -= GetExtendsInOrientationDirection(child.DesiredSize);
              if (spaceLeft + DELTA_DOUBLE < 0)
                break; // Found item which is not visible any more
              _actualFirstVisibleChild = i;
            }
          }
        }

        // 2) Calculate start position
        for (int i = 0; i < _actualFirstVisibleChild; i++)
        {
          FrameworkElement child = visibleChildren[i];
          startPosition -= GetExtendsInOrientationDirection(child.DesiredSize);
        }

        // 3) Arrange children
        if (Orientation == Orientation.Vertical)
          _totalWidth = actualExtendsInNonOrientationDirection;
        else
          _totalHeight = actualExtendsInNonOrientationDirection;
        for (int i = 0; i < numVisibleChildren; i++)
        {
          FrameworkElement child = visibleChildren[i];
          SizeF childSize = new SizeF(child.DesiredSize);
          // For Orientation == vertical, this is childSize.Height, for horizontal it is childSize.Width
          float desiredExtendsInOrientationDirection = GetExtendsInOrientationDirection(childSize);
          if (Orientation == Orientation.Vertical)
          {
            PointF position = new PointF(ActualPosition.X, ActualPosition.Y + startPosition);

            childSize.Width = actualExtendsInNonOrientationDirection;

            ArrangeChildHorizontal(child, child.HorizontalAlignment, ref position, ref childSize);
            child.Arrange(new RectangleF(position, childSize));
            _totalHeight += desiredExtendsInOrientationDirection;

            startPosition += desiredExtendsInOrientationDirection;
          }
          else
          {
            PointF position = new PointF(ActualPosition.X + startPosition, ActualPosition.Y);

            childSize.Height = actualExtendsInNonOrientationDirection;

            ArrangeChildVertical(child, child.VerticalAlignment, ref position, ref childSize);
            child.Arrange(new RectangleF(position, childSize));
            _totalWidth += desiredExtendsInOrientationDirection;

            startPosition += desiredExtendsInOrientationDirection;
          }
        }
      }
      else
      {
        _actualFirstVisibleChild = 0;
        _actualLastVisibleChild = -1;
      }
    }

    protected void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    protected static void Bound(ref int value, int lowerBound, int upperBound)
    {
      if (value < lowerBound)
        value = lowerBound;
      if (value > upperBound)
        value = upperBound;
    }

    protected static void LowerBound(ref int value, int lowerBound)
    {
      if (value < lowerBound)
        value = lowerBound;
    }

    protected static void UpperBound(ref int value, int upperBound)
    {
      if (value > upperBound)
        value = upperBound;
    }

    protected static double SumActualWidths(IList<FrameworkElement> elements, int startIndex, int endIndex)
    {
      Bound(ref startIndex, 0, elements.Count-1);
      Bound(ref endIndex, 0, elements.Count-1);
      if (startIndex == endIndex || elements.Count == 0)
        return 0f;
      bool invert = startIndex > endIndex;
      if (invert)
      {
        int tmp = startIndex;
        startIndex = endIndex;
        endIndex = tmp;
      }
      double result = 0;
      for (int i = startIndex; i < endIndex; i++)
        result += elements[i].ActualWidth;
      return invert ? -result : result;
    }

    protected static double SumActualHeights(IList<FrameworkElement> elements, int startIndex, int endIndex)
    {
      Bound(ref startIndex, 0, elements.Count-1);
      Bound(ref endIndex, 0, elements.Count-1);
      if (startIndex == endIndex || elements.Count == 0)
        return 0f;
      bool invert = startIndex > endIndex;
      if (invert)
      {
        int tmp = startIndex;
        startIndex = endIndex;
        endIndex = tmp;
      }
      double result = 0;
      for (int i = startIndex; i < endIndex; i++)
        result += elements[i].ActualHeight;
      return invert ? -result : result;
    }

    public override void MakeVisible(UIElement element, RectangleF elementBounds)
    {
      MakeChildVisible(element, ref elementBounds);
      base.MakeVisible(element, elementBounds);
    }

    protected virtual void MakeChildVisible(UIElement element, ref RectangleF elementBounds)
    {
      if (_canScroll)
      {
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        int index = 0;
        foreach (FrameworkElement currentChild in visibleChildren)
        {
          if (InVisualPath(currentChild, element))
          {
            int oldFirstVisibleChild = _actualFirstVisibleChild;
            int oldLastVisibleChild = _actualLastVisibleChild;
            bool first;
            if (index < oldFirstVisibleChild)
              first = true;
            else if (index <= oldLastVisibleChild)
              break;
            else
              first = false;
            SetScrollIndex(index, first);
            // Adjust the scrolled element's bounds; Calculate the difference between positions of childen at old/new child indices
            if (Orientation == Orientation.Horizontal)
              elementBounds.X -= (float) SumActualWidths(visibleChildren, first ? oldFirstVisibleChild : oldLastVisibleChild, index);
            else
              elementBounds.Y -= (float) SumActualHeights(visibleChildren, first ? oldFirstVisibleChild : oldLastVisibleChild, index);
            break;
          }
          index++;
        }
      }
    }

    public override bool IsChildRenderedAt(UIElement child, float x, float y)
    {
      if (_canScroll)
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

    public virtual void AlignedPanelAddPotentialFocusNeighbors(RectangleF? startingRect, ICollection<FrameworkElement> elements,
        bool elementsBeforeAndAfter)
    {
      if (!IsVisible)
        return;
      if (Focusable)
        elements.Add(this);
      IList<FrameworkElement> children = GetVisibleChildren();
      int numElementsBeforeAndAfter = elementsBeforeAndAfter ? NUM_ADD_MORE_FOCUS_ELEMENTS : 0;
      AddFocusedElementRange(children, startingRect, _actualFirstVisibleChild, _actualLastVisibleChild,
          numElementsBeforeAndAfter, numElementsBeforeAndAfter, elements);
    }

    protected void AddFocusedElementRange(IList<FrameworkElement> availableElements, RectangleF? startingRect,
        int first, int last, int elementsBefore, int elementsAfter, ICollection<FrameworkElement> outElements)
    {
      int numItems = availableElements.Count;
      if (numItems == 0)
        return;
      Bound(ref first, 0, numItems - 1);
      Bound(ref last, 0, numItems - 1);
      if (elementsBefore > 0)
      {
        // Find elements before the first index which have focusable elements.
        int formerNumElements = outElements.Count;
        for (int i = first - 1; i >= 0; i--)
        {
          FrameworkElement fe = availableElements[i];
          fe.AddPotentialFocusableElements(startingRect, outElements);
          if (formerNumElements != outElements.Count)
          {
            // Found focusable elements
            elementsBefore--;
            if (elementsBefore == 0)
              break;
            formerNumElements = outElements.Count;
          }
        }
      }
      for (int i = first; i <= last; i++)
      {
        FrameworkElement fe = availableElements[i];
        fe.AddPotentialFocusableElements(startingRect, outElements);
      }
      if (elementsAfter > 0)
      {
        // Find elements after the last index which have focusable elements.
        int formerNumElements = outElements.Count;
        for (int i = last + 1; i < availableElements.Count; i++)
        {
          FrameworkElement fe = availableElements[i];
          fe.AddPotentialFocusableElements(startingRect, outElements);
          if (formerNumElements != outElements.Count)
          {
            // Found focusable elements
            elementsAfter--;
            if (elementsAfter == 0)
              break;
            formerNumElements = outElements.Count;
          }
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
      if (nextElement == null)
        return false;
      return nextElement.TrySetFocus(true);
    }

    #endregion

    #region Base overrides

    public override void MakeItemVisible(int index)
    {
      if (index < _actualFirstVisibleChild)
        SetScrollIndex(index, true);
      else if (index > _actualLastVisibleChild)
        SetScrollIndex(index, false);
    }

    #endregion

    #region Rendering

    protected override void UpdateRenderOrder()
    {
      if (!_updateRenderOrder) return;
      _updateRenderOrder = false;
      lock (Children.SyncRoot) // We must aquire the children's lock when accessing the _renderOrder
      {
        Children.FixZIndex();
        _renderOrder.Clear();
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        for (int i = _actualFirstVisibleChild; i <= _actualLastVisibleChild; i++)
        {
          FrameworkElement element = visibleChildren[i];
          if (!element.IsVisible)
            continue;
          _renderOrder.Add(element);
        }
      }
    }

    #endregion

    #region IScrollViewerFocusSupport implementation

    public virtual bool FocusUp()
    {
      if (Orientation == Orientation.Vertical)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Up);
      return false;
    }

    public virtual bool FocusDown()
    {
      if (Orientation == Orientation.Vertical)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Down);
      return false;
    }

    public virtual bool FocusLeft()
    {
      if (Orientation == Orientation.Horizontal)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Left);
      return false;
    }

    public virtual bool FocusRight()
    {
      if (Orientation == Orientation.Horizontal)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Right);
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
        int firstVisibleChildIndex = _actualFirstVisibleChild;
        Bound(ref firstVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement firstVisibleChild = visibleChildren[firstVisibleChildIndex];
        if (firstVisibleChild == null)
          return false;
        float limitPosition;
        if (InVisualPath(firstVisibleChild, currentElement))
          // The topmost element is focused - move one page up
          limitPosition = firstVisibleChild.ActualBounds.Bottom - (float) ActualHeight;
        else
          // An element inside our visible range is focused - move to first element
          limitPosition = ActualPosition.Y;
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Up)) != null &&
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
        int lastVisibleChildIndex = _actualLastVisibleChild;
        Bound(ref lastVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement lastVisibleChild = visibleChildren[lastVisibleChildIndex];
        if (lastVisibleChild == null)
          return false;
        float limitPosition;
        if (InVisualPath(lastVisibleChild, currentElement))
          // The element at the bottom is focused - move one page down
          limitPosition = lastVisibleChild.ActualPosition.Y + (float) ActualHeight;
        else
          // An element inside our visible range is focused - move to last element
          limitPosition = ActualPosition.Y + (float) ActualHeight;
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Down)) != null &&
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
        int firstVisibleChildIndex = _actualFirstVisibleChild;
        Bound(ref firstVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement firstVisibleChild = visibleChildren[firstVisibleChildIndex];
        if (firstVisibleChild == null)
          return false;
        float limitPosition;
        if (InVisualPath(firstVisibleChild, currentElement))
          // The leftmost element is focused - move one page left
          limitPosition = firstVisibleChild.ActualBounds.Right - (float) ActualWidth;
        else
          // An element inside our visible range is focused - move to first element
          limitPosition = ActualPosition.X;
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Left)) != null &&
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
        int lastVisibleChildIndex = _actualLastVisibleChild;
        Bound(ref lastVisibleChildIndex, 0, visibleChildren.Count - 1);
        FrameworkElement lastVisibleChild = visibleChildren[lastVisibleChildIndex];
        if (lastVisibleChild == null)
          return false;
        float limitPosition;
        if (InVisualPath(lastVisibleChild, currentElement))
          // The element at the bottom is focused - move one page down
          limitPosition = lastVisibleChild.ActualPosition.X + (float) ActualWidth;
        else
          // An element inside our visible range is focused - move to last element
          limitPosition = ActualPosition.X + (float) ActualWidth;
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Right)) != null &&
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
      SetScrollIndex(0, true);
      visibleChildren[0].SetFocus = true;
      return true;
    }

    public virtual bool FocusEnd()
    {
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      SetScrollIndex(int.MaxValue, false);
      visibleChildren[visibleChildren.Count - 1].SetFocus = true;
      return true;
    }

    public virtual bool ScrollDown(int numLines)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (IsViewPortAtBottom)
          return false;
        SetScrollIndex(_actualFirstVisibleChild + numLines, true);
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
        SetScrollIndex(_actualFirstVisibleChild - numLines, true);
        return true;
      }
      return false;
    }

    #endregion

    #region IScrollInfo implementation

    public event ScrolledDlgt Scrolled;

    public virtual bool CanScroll
    {
      get { return _canScroll; }
      set { _canScroll = value; }
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
        if (Orientation == Orientation.Vertical)
          return 0;
        float spaceBefore = 0;
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        // Need to avoid threading issues. If the render thread is arranging at the same time, _actualFirstVisibleChild
        // might be adapted while this code executes
        int scrollIndex = _actualFirstVisibleChild;
        for (int i = 0; i < scrollIndex; i++)
        {
          FrameworkElement fe = visibleChildren[i];
          if (fe == null)
            continue;
          spaceBefore += fe.DesiredSize.Width;
        }
        return spaceBefore;
      }
    }

    public virtual float ViewPortStartY
    {
      get
      {
        if (Orientation == Orientation.Horizontal)
          return 0;
        float spaceBefore = 0;
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        // Need to avoid threading issues. If the render thread is arranging at the same time, _actualFirstVisibleChild
        // might be adapted while this code executes
        int scrollIndex = _actualFirstVisibleChild;
        for (int i = 0; i < scrollIndex; i++)
        {
          FrameworkElement fe = visibleChildren[i];
          if (fe == null)
            continue;
          spaceBefore += fe.DesiredSize.Height;
        }
        return spaceBefore;
      }
    }

    public virtual bool IsViewPortAtTop
    {
      get
      {
        if (Orientation == Orientation.Horizontal)
          return true;
        return _actualFirstVisibleChild == 0;
      }
    }

    public virtual bool IsViewPortAtBottom
    {
      get
      {
        if (Orientation == Orientation.Horizontal)
          return true;
        return _actualLastVisibleChild == GetVisibleChildren().Count - 1;
      }
    }

    public virtual bool IsViewPortAtLeft
    {
      get
      {
        if (Orientation == Orientation.Vertical)
          return true;
        return _actualFirstVisibleChild == 0;
      }
    }

    public virtual bool IsViewPortAtRight
    {
      get
      {
        if (Orientation == Orientation.Vertical)
          return true;
        return _actualLastVisibleChild == GetVisibleChildren().Count - 1;
      }
    }

    #endregion
  }
}

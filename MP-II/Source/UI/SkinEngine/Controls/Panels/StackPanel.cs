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

using System;
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.Utilities;
using SlimDX;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class StackPanel : Panel, IScrollViewerFocusSupport, IScrollInfo
  {
    #region Protected fields

    protected AbstractProperty _orientationProperty;
    protected float _totalHeight;
    protected float _totalWidth;

    protected bool _canScroll = false; // Set to true by a scrollable container (ScrollViewer for example) if we should provide logical scrolling

    // Index of the first visible item which will be drawn at our ActualPosition - when modified by method
    // SetScrollOffset, it will be applied the next time Arrange is called.
    protected int _scrollIndex = 0;
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
      _orientationProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      StackPanel p = (StackPanel) source;
      Orientation = copyManager.GetCopy(p.Orientation);
      CanScroll = copyManager.GetCopy(p.CanScroll);
      Attach();
    }

    #endregion

    #region Public properties & events

    public AbstractProperty OrientationProperty
    {
      get { return _orientationProperty; }
    }

    public Orientation Orientation
    {
      get { return (Orientation)_orientationProperty.GetValue(); }
      set { _orientationProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    protected IList<FrameworkElement> GetVisibleChildren()
    {
      IList<FrameworkElement> result = new List<FrameworkElement>(Children.Count);
      foreach (FrameworkElement child in Children)
        if (child.IsVisible)
          result.Add(child);
      return result;
    }

    public void SetScrollIndex(int scrollIndex)
    {
      if (_scrollIndex == scrollIndex)
        return;
      _scrollIndex = scrollIndex;
      Invalidate();
      InvokeScrolled();
    }

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      float totalDesiredHeight = 0;
      float totalDesiredWidth = 0;
      SizeF childSize;
      SizeF minSize = new SizeF(0, 0);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) 
          continue;
        if (Orientation == Orientation.Vertical)
        {
          childSize = new SizeF(totalSize.Width, float.NaN);
          child.Measure(ref childSize);
          totalDesiredHeight += childSize.Height;
          if (childSize.Width > totalDesiredWidth)
            totalDesiredWidth = childSize.Width;
        }
        else
        {
          childSize = new SizeF(float.NaN, totalSize.Height);
          child.Measure(ref childSize);
          totalDesiredWidth += childSize.Width;
          if (childSize.Height > totalDesiredHeight)
            totalDesiredHeight = childSize.Height;
        }
        if (childSize.Width > minSize.Width)
          minSize.Width = childSize.Width;
        if (childSize.Height > minSize.Height)
          minSize.Height = childSize.Height;
      }

      return new SizeF(totalDesiredWidth, totalDesiredHeight);
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("StackPanel.Arrange: {0} X {1}, Y {2} W {3} H {4}", Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      RemoveMargin(ref finalRect);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;
      RectangleF bounds = ActualBounds;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      _totalHeight = 0;
      _totalWidth = 0;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int visibleChildrenCount = visibleChildren.Count;
      if (visibleChildrenCount > 0)
      {
        // Hint: We cannot skip the arrangement of children above _scrollOffset or below the last visible child
        // because the rendering and focus system also needs the bounds of the currently invisible children
        switch (Orientation)
        {
          case Orientation.Vertical:
            {
              const float startPositionX = 0;
              float startPositionY = 0;
              if (_canScroll)
              {
                // Try to correct scroll offset (when scrolled by _scrollOffset, we might have space left)
                float spaceLeft = bounds.Height;
                for (int i = visibleChildrenCount - 1; i >= 0; i--)
                {
                  FrameworkElement child = visibleChildren[i];
                  spaceLeft -= child.DesiredSize.Height;
                  if (spaceLeft < 0)
                    break; // Nothing to correct
                  if (_scrollIndex > i)
                    // This child also fits into range
                    _scrollIndex = i;
                }
                // Calculate start position
                for (int i = 0; i < _scrollIndex; i++)
                {
                  FrameworkElement child = visibleChildren[i];
                  startPositionY -= child.DesiredSize.Height;
                }
              }
              else
                _scrollIndex = 0;
              int lastVisibleChild = -1;
              for (int i = 0; i < visibleChildrenCount; i++)
              {
                FrameworkElement child = visibleChildren[i];
                SizeF childSize = child.DesiredSize;
                if (!_canScroll || (i >= _scrollIndex && startPositionY + childSize.Height <= bounds.Height + 0.5))
                  lastVisibleChild = i;
                PointF location = new PointF(ActualPosition.X + startPositionX,
                    ActualPosition.Y + startPositionY);

                childSize.Height = Math.Min(childSize.Height, bounds.Height);
                childSize.Width = (float) ActualWidth;

                ArrangeChildHorizontal(child, ref location, ref childSize);

                child.Arrange(new RectangleF(location, childSize));
                _totalWidth = Math.Max(_totalWidth, child.ActualTotalBounds.Width);
                _totalHeight += child.ActualTotalBounds.Height;

                startPositionY += childSize.Height;
              }
              _actualLastVisibleChild = lastVisibleChild;
            }
            break;

          case Orientation.Horizontal:
            {
              float startPositionX = 0;
              const float startPositionY = 0;
              if (_canScroll)
              {
                // Try to correct scroll offset (when scrolled by _scrollOffset, we might have space left)
                float spaceLeft = bounds.Width;
                for (int i = visibleChildrenCount - 1; i >= 0; i--)
                {
                  FrameworkElement child = visibleChildren[i];
                  spaceLeft -= child.DesiredSize.Width;
                  if (spaceLeft < 0)
                    break; // Nothing to correct
                  if (_scrollIndex > i)
                    // This child also fits into range
                    _scrollIndex = i;
                }
                // Calculate start position
                for (int i = 0; i < _scrollIndex; i++)
                {
                  FrameworkElement child = visibleChildren[i];
                  startPositionX -= child.DesiredSize.Width;
                }
              }
              else
                _scrollIndex = 0;
              int lastVisibleChild = -1;
              for (int i = _scrollIndex; i < visibleChildrenCount; i++)
              {
                FrameworkElement child = visibleChildren[i];
                SizeF childSize = child.DesiredSize;
                if (!_canScroll || (i >= _scrollIndex && startPositionX + childSize.Width <= bounds.Width + 0.5))
                  lastVisibleChild = i;
                PointF location = new PointF(ActualPosition.X + startPositionX,
                    ActualPosition.Y + startPositionY);

                childSize.Height = (float) ActualHeight;
                childSize.Width = Math.Min(childSize.Width, bounds.Width);

                ArrangeChildVertical(child, ref location, ref childSize);

                child.Arrange(new RectangleF(location, childSize));
                _totalHeight = Math.Max(_totalHeight, child.ActualTotalBounds.Height);
                _totalWidth += child.ActualTotalBounds.Width;

                startPositionX += childSize.Width;
              }
              _actualLastVisibleChild = lastVisibleChild;
            }
            break;
          }
      }

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
        _performLayout = true;
      if (Screen != null) Screen.Invalidate(this);
      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      base.Arrange(finalRect);
      _updateRenderOrder = true;
    }

    private void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    private void ScrollChildToFirst(int childIndex, IList<FrameworkElement> visibleChildren)
    {
      SetScrollIndex(childIndex);
    }

    private void ScrollChildToLast(int childIndex, IList<FrameworkElement> visibleChildren)
    {
      RectangleF bounds = ActualBounds;
      switch (Orientation)
      {
        case Orientation.Vertical:
          {
            float spaceLeft = bounds.Height;
            int index = -1;
            int numVisible = 0;
            for (int i = childIndex; i >= 0; i--)
            {
              FrameworkElement child = visibleChildren[i];
              spaceLeft -= child.DesiredSize.Height;
              if (spaceLeft < 0 && numVisible > 0)
                break;
              numVisible++;
              index = i;
            }
            if (index < 0)
              index = 0;
            SetScrollIndex(index);
          }
          break;

        case Orientation.Horizontal:
          {
            float spaceLeft = bounds.Width;
            int index = childIndex;
            int numVisible = 0;
            for (int i = childIndex; i >= 0; i--)
            {
              FrameworkElement child = visibleChildren[i];
              spaceLeft -= child.DesiredSize.Width;
              if (spaceLeft < 0 && numVisible > 0)
                break;
              numVisible++;
              index -= 1;
            }
            if (index < 0)
              index = 0;
            SetScrollIndex(index);
          }
          break;
      }
    }

    protected static double SumActualWidths(IList<FrameworkElement> elements, int startIndex, int endIndex)
    {
      if (startIndex == endIndex || elements.Count == 0)
        return 0f;
      if (startIndex >= elements.Count)
        startIndex = elements.Count - 1;
      if (endIndex < 0)
        endIndex = 0;
      int direction = Math.Sign(endIndex - startIndex);
      double result = 0;
      for (int i = startIndex; direction * i < direction * endIndex; i += direction)
        result += direction*elements[i].ActualWidth;
      return result;
    }

    protected static double SumActualHeights(IList<FrameworkElement> elements, int startIndex, int endIndex)
    {
      if (startIndex == endIndex || elements.Count == 0)
        return 0f;
      if (startIndex >= elements.Count)
        startIndex = elements.Count - 1;
      if (endIndex < 0)
        endIndex = 0;
      int direction = Math.Sign(endIndex - startIndex);
      double result = 0;
      for (int i = startIndex; direction * i < direction * endIndex; i += direction)
        result += direction*elements[i].ActualHeight;
      return result;
    }

    public override void MakeVisible(UIElement element, RectangleF elementBounds)
    {
      if (_canScroll)
      {
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        int index = 0;
        foreach (FrameworkElement currentChild in visibleChildren)
        {
          if (InVisualPath(currentChild, element))
          {
            int oldScrollIndex = _scrollIndex;
            if (index < _scrollIndex)
              ScrollChildToFirst(index, visibleChildren);
            else if (index <= _actualLastVisibleChild)
              break;
            else
              ScrollChildToLast(index, visibleChildren);
            // Adjust the scrolled element's bounds
            if (Orientation == Orientation.Horizontal)
              elementBounds.X -= (float) SumActualWidths(visibleChildren, oldScrollIndex, _scrollIndex);
            else
              elementBounds.Y -= (float) SumActualHeights(visibleChildren, oldScrollIndex, _scrollIndex);
            break;
          }
          index++;
        }
      }
      base.MakeVisible(element, elementBounds);
    }

    public override bool IsChildVisibleAt(UIElement child, float x, float y)
    {
      if (!child.IsInArea(x, y) || !IsInVisibleArea(x, y))
        return false;
      if (_canScroll)
      { // If we can scroll, check if child is completely in our range -> if not, it won't be rendered and thus isn't visible
        RectangleF elementBounds = ((FrameworkElement) child).ActualBounds;
        RectangleF bounds = ActualBounds;
        if (elementBounds.Right > bounds.Right + DELTA_DOUBLE) return false;
        if (elementBounds.Left < bounds.Left - DELTA_DOUBLE) return false;
        if (elementBounds.Top < bounds.Top - DELTA_DOUBLE) return false;
        if (elementBounds.Bottom > bounds.Bottom + DELTA_DOUBLE) return false;
      }
      return true;
    }

    #endregion

    #region Rendering

    protected override void UpdateRenderOrder()
    {
      if (!_updateRenderOrder) return;
      _updateRenderOrder = false;
      if (_renderOrder == null || Children == null)
        return;
      _renderOrder.Clear();
      RectangleF bounds = ActualBounds;
      foreach (FrameworkElement element in Children)
      {
        if (!element.IsVisible)
          continue;
        if (_canScroll)
        { // Don't render elements which are not visible, if we can scroll
          RectangleF elementBounds = element.ActualBounds;
          if (elementBounds.Right > bounds.Right + DELTA_DOUBLE) continue;
          if (elementBounds.Left < bounds.Left - DELTA_DOUBLE) continue;
          if (elementBounds.Top < bounds.Top - DELTA_DOUBLE) continue;
          if (elementBounds.Bottom > bounds.Bottom + DELTA_DOUBLE) continue;
        }
        _renderOrder.Add(element);
      }
    }

    #endregion

    #region IScrollViewerFocusSupport implementation

    public bool FocusUp()
    {
      if (Orientation == Orientation.Vertical)
        return MoveFocus1(MoveFocusDirection.Up);
      return false;
    }

    public bool FocusDown()
    {
      if (Orientation == Orientation.Vertical)
        return MoveFocus1(MoveFocusDirection.Down);
      return false;
    }

    public bool FocusLeft()
    {
      if (Orientation == Orientation.Horizontal)
        return MoveFocus1(MoveFocusDirection.Left);
      return false;
    }

    public bool FocusRight()
    {
      if (Orientation == Orientation.Horizontal)
        return MoveFocus1(MoveFocusDirection.Right);
      return false;
    }

    public bool FocusPageUp()
    {
      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        FrameworkElement firstVisibleChild = CollectionUtils.SafeGet(visibleChildren, _scrollIndex);
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
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Up)) != null &&
            (nextElement.ActualPosition.Y > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public bool FocusPageDown()
    {
      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        FrameworkElement lastVisibleChild = CollectionUtils.SafeGet(visibleChildren, _actualLastVisibleChild);
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
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Down)) != null &&
            (nextElement.ActualBounds.Bottom < limitPosition + DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public bool FocusPageLeft()
    {
      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        FrameworkElement firstVisibleChild = CollectionUtils.SafeGet(visibleChildren, _scrollIndex);
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
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Left)) != null &&
            (nextElement.ActualPosition.X > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public bool FocusPageRight()
    {
      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        FrameworkElement lastVisibleChild = CollectionUtils.SafeGet(visibleChildren, _actualLastVisibleChild);
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
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Right)) != null &&
            (nextElement.ActualBounds.Right < limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public bool FocusHome()
    {
      return MoveFocusN(Orientation == Orientation.Horizontal ? MoveFocusDirection.Left : MoveFocusDirection.Up);
    }

    public bool FocusEnd()
    {
      return MoveFocusN(Orientation == Orientation.Horizontal ? MoveFocusDirection.Right : MoveFocusDirection.Down);
    }

    #endregion

    #region IScrollInfo implementation

    public event ScrolledDlgt Scrolled;

    public bool CanScroll
    {
      get { return _canScroll; }
      set { _canScroll = value; }
    }

    public float TotalWidth
    {
      get { return _totalWidth; }
    }

    public float TotalHeight
    {
      get { return _totalHeight; }
    }

    public float ViewPortWidth
    {
      get { return (float) ActualWidth; }
    }

    public float ViewPortStartX
    {
      get
      {
        float spaceBefore = 0;
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        // Need to avoid threading issues. If the render thread is arranging at the same time, _scrollIndex
        // might be adapted while this code executes
        int scrollIndex = _scrollIndex;
        for (int i = 0; i < scrollIndex; i++)
        {
          FrameworkElement fe = CollectionUtils.SafeGet(visibleChildren, i);
          if (fe == null)
            continue;
          spaceBefore += fe.DesiredSize.Width;
        }
        return spaceBefore;
      }
    }

    public float ViewPortHeight
    {
      get { return (float) ActualHeight; }
    }

    public float ViewPortStartY
    {
      get
      {
        float spaceBefore = 0;
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        // Need to avoid threading issues. If the render thread is arranging at the same time, _scrollIndex
        // might be adapted while this code executes
        int scrollIndex = _scrollIndex;
        for (int i = 0; i < scrollIndex; i++)
        {
          FrameworkElement fe = CollectionUtils.SafeGet(visibleChildren, i);
          if (fe == null)
            continue;
          spaceBefore += fe.DesiredSize.Height;
        }
        return spaceBefore;
      }
    }

    public bool IsViewPortAtTop
    {
      get
      {
        if (Orientation == Orientation.Horizontal)
          return true;
        return _scrollIndex == 0;
      }
    }

    public bool IsViewPortAtBottom
    {
      get
      {
        if (Orientation == Orientation.Horizontal)
          return true;
        return _actualLastVisibleChild == GetVisibleChildren().Count - 1;
      }
    }

    public bool IsViewPortAtLeft
    {
      get
      {
        if (Orientation == Orientation.Vertical)
          return true;
        return _scrollIndex == 0;
      }
    }

    public bool IsViewPortAtRight
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

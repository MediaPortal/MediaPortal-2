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
using System.Drawing;
using MediaPortal.Presentation.DataObjects;
using SlimDX;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  public class StackPanel : Panel, IScrollViewerFocusSupport, IScrollInfo
  {
    #region Protected fields

    protected Property _orientationProperty;
    protected float _totalHeight;
    protected float _totalWidth;

    protected bool _canScroll = false; // Set to true if we are located in a scrollable container (ScrollViewer for example)

    // Offset of the first visible item which will be drawn at our ActualPosition - when modified by method
    // SetScrollOffset, it will be applied the next time Arrange is called.
    protected int _scrollOffset = 0;
    // Offset of the last visible child item. When scrolling, this index denotes the "opposite children" to the
    // child denoted by the _scrollOffset.
    protected int _actualLastVisibleChild = -1;

    #endregion

    #region Ctor

    public StackPanel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _orientationProperty = new Property(typeof(Orientation), Orientation.Vertical);
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

    #region Public properties

    public Property OrientationProperty
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

    public void SetScrollOffset(int scrollOffset)
    {
      if (_scrollOffset == scrollOffset)
        return;
      _scrollOffset = scrollOffset;
      Invalidate();
    }

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      if (!double.IsNaN(Width))
        totalSize.Width = (float) Width;
      if (!double.IsNaN(Height))
        totalSize.Height = (float) Height;

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

      _desiredSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
        _desiredSize.Width = totalDesiredWidth;

      if (double.IsNaN(Height))
        _desiredSize.Height = totalDesiredHeight;

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("StackPanel.Measure: {0} returns {1}x{2}", Name, (int)totalSize.Width, (int)totalSize.Height));
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
                  spaceLeft -= child.TotalDesiredSize().Height;
                  if (spaceLeft < 0)
                    break; // Nothing to correct
                  if (_scrollOffset > i)
                    // This child also fits into range
                    _scrollOffset = i;
                }
                // Calculate start position
                for (int i = 0; i < _scrollOffset; i++)
                {
                  FrameworkElement child = visibleChildren[i];
                  startPositionY -= child.TotalDesiredSize().Height;
                }
              }
              else
                _scrollOffset = 0;
              _actualLastVisibleChild = -1;
              for (int i = 0; i < visibleChildrenCount; i++)
              {
                FrameworkElement child = visibleChildren[i];
                SizeF childSize = child.TotalDesiredSize();
                if (!_canScroll || (i >= _scrollOffset && startPositionY + childSize.Height <= bounds.Height))
                  _actualLastVisibleChild = i;
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
                  spaceLeft -= child.TotalDesiredSize().Width;
                  if (spaceLeft < 0)
                    break; // Nothing to correct
                  if (_scrollOffset > i)
                    // This child also fits into range
                    _scrollOffset = i;
                }
                // Calculate start position
                for (int i = 0; i < _scrollOffset; i++)
                {
                  FrameworkElement child = visibleChildren[i];
                  startPositionX -= child.TotalDesiredSize().Width;
                }
              }
              else
                _scrollOffset = 0;
              _actualLastVisibleChild = -1;
              for (int i = _scrollOffset; i < visibleChildrenCount; i++)
              {
                FrameworkElement child = visibleChildren[i];
                SizeF childSize = child.TotalDesiredSize();
                if (!_canScroll || (i >= _scrollOffset && startPositionX + childSize.Width <= bounds.Width))
                  _actualLastVisibleChild = i;
                PointF location = new PointF(ActualPosition.X + startPositionX,
                    ActualPosition.Y + startPositionY);

                childSize.Height = (float) ActualHeight;
                childSize.Width = Math.Min(childSize.Width, bounds.Width);

                ArrangeChildHorizontal(child, ref location, ref childSize);

                child.Arrange(new RectangleF(location, childSize));
                _totalHeight = Math.Max(_totalHeight, child.ActualTotalBounds.Height);
                _totalWidth += child.ActualTotalBounds.Width;

                startPositionX += childSize.Width;
              }
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
    }

    private void ScrollChildToFirst(int childIndex, IList<FrameworkElement> visibleChildren)
    {
      SetScrollOffset(childIndex);
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
              spaceLeft -= child.TotalDesiredSize().Height;
              if (spaceLeft < 0 && numVisible > 0)
                break;
              numVisible++;
              index = i;
            }
            if (index < 0)
              index = 0;
            SetScrollOffset(index);
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
              spaceLeft -= child.TotalDesiredSize().Width;
              if (spaceLeft < 0 && numVisible > 0)
                break;
              numVisible++;
              index -= 1;
            }
            if (index < 0)
              index = 0;
            SetScrollOffset(index);
          }
          break;
      }
    }

    public override void MakeVisible(UIElement element)
    {
      base.MakeVisible(this);
      if (!_canScroll)
        return;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int index = 0;
      foreach (FrameworkElement currentChild in visibleChildren)
      {
        if (InVisualPath(currentChild, element))
        {
          if (index < _scrollOffset)
            ScrollChildToFirst(index, visibleChildren);
          else if (index <= _actualLastVisibleChild)
            break;
          else
            ScrollChildToLast(index, visibleChildren);
          break;
        }
        index++;
      }
    }

    #endregion

    #region Rendering

    protected override void RenderChildren()
    {
      lock (_orientationProperty)
      {
        RectangleF bounds = ActualBounds;
        foreach (FrameworkElement element in _renderOrder)
        {
          if (!element.IsVisible) 
            continue;
          RectangleF elementBounds = element.ActualBounds;
          if (_canScroll)
          {
            // Don't render elements which are not visible, if we can scroll
            if (elementBounds.Right > bounds.Right) continue;
            if (elementBounds.Left < bounds.Left) continue;
            if (elementBounds.Top < bounds.Top) continue;
            if (elementBounds.Bottom > bounds.Bottom) continue;
          }
          element.Render();
        }
      }
    }

    #endregion

    #region Focus movement

    protected FrameworkElement GetFocusedElementOrChild()
    {
      FrameworkElement result = Screen == null ? null : Screen.FocusedElement;
      if (result == null)
        foreach (UIElement child in Children)
        {
          result = child as FrameworkElement;
          if (result != null)
            break;
        }
      return result;
    }

    /// <summary>
    /// Moves the focus from the currently focused element in the screen to the first child element in the given
    /// direction.
    /// </summary>
    /// <param name="direction">Direction to move the focus.</param>
    /// <returns><c>true</c>, if the focus could be moved to the desired child, else <c>false</c>.</returns>
    protected bool MoveFocus1(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      FrameworkElement nextElement = PredictFocus(currentElement.ActualBounds, direction);
      if (nextElement == null) return false;
      nextElement.TrySetFocus();
      return true;
    }

    /// <summary>
    /// Moves the focus from the currently focused element in the screen to our last child in the given
    /// direction. For example if <c>direction == MoveFocusDirection.Up</c>, this method tries to focus the
    /// topmost child.
    /// </summary>
    /// <param name="direction">Direction to move the focus.</param>
    /// <returns><c>true</c>, if the focus could be moved to the desired child, else <c>false</c>.</returns>
    protected bool MoveFocusN(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      FrameworkElement nextElement;
      while ((nextElement = PredictFocus(currentElement.ActualBounds, direction)) != null)
        currentElement = nextElement;
      currentElement.TrySetFocus();
      return true;
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
        FrameworkElement firstVisibleChild = visibleChildren[_scrollOffset];
        float limitPosition;
        if (InVisualPath(firstVisibleChild, currentElement))
          // The topmost element is focused - move one page up
          limitPosition = firstVisibleChild.ActualBounds.Bottom - (float) ActualHeight;
        else
          // An element inside our visible range is focused - move to first element
          limitPosition = ActualPosition.Y;
        FrameworkElement nextElement;
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Up)) != null &&
            (nextElement.ActualPosition.Y > limitPosition))
          currentElement = nextElement;
        currentElement.TrySetFocus();
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
        FrameworkElement lastVisibleChild = visibleChildren[_actualLastVisibleChild];
        float limitPosition;
        if (InVisualPath(lastVisibleChild, currentElement))
          // The element at the bottom is focused - move one page down
          limitPosition = lastVisibleChild.ActualPosition.Y + (float) ActualHeight;
        else
          // An element inside our visible range is focused - move to last element
          limitPosition = ActualPosition.Y + (float) ActualHeight;
        FrameworkElement nextElement;
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Down)) != null &&
            (nextElement.ActualBounds.Bottom < limitPosition))
          currentElement = nextElement;
        currentElement.TrySetFocus();
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
        FrameworkElement firstVisibleChild = visibleChildren[_scrollOffset];
        float limitPosition;
        if (InVisualPath(firstVisibleChild, currentElement))
          // The leftmost element is focused - move one page left
          limitPosition = firstVisibleChild.ActualBounds.Right - (float) ActualWidth;
        else
          // An element inside our visible range is focused - move to first element
          limitPosition = ActualPosition.X;
        FrameworkElement nextElement;
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Left)) != null &&
            (nextElement.ActualPosition.X > limitPosition))
          currentElement = nextElement;
        currentElement.TrySetFocus();
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
        FrameworkElement lastVisibleChild = visibleChildren[_actualLastVisibleChild];
        float limitPosition;
        if (InVisualPath(lastVisibleChild, currentElement))
          // The element at the bottom is focused - move one page down
          limitPosition = lastVisibleChild.ActualPosition.X + (float) ActualWidth;
        else
          // An element inside our visible range is focused - move to last element
          limitPosition = ActualPosition.X + (float) ActualWidth;
        FrameworkElement nextElement;
        while ((nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Right)) != null &&
            (nextElement.ActualBounds.Right < limitPosition))
          currentElement = nextElement;
        currentElement.TrySetFocus();
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
        for (int i = 0; i < _scrollOffset; i++)
          spaceBefore += visibleChildren[i].TotalDesiredSize().Width;
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
        for (int i = 0; i < _scrollOffset; i++)
          spaceBefore += visibleChildren[i].TotalDesiredSize().Height;
        return spaceBefore;
      }
    }

    public bool IsViewPortAtTop
    {
      get
      {
        if (Orientation == Panels.Orientation.Horizontal)
          return true;
        return _scrollOffset == 0;
      }
    }

    public bool IsViewPortAtBottom
    {
      get
      {
        if (Orientation == Panels.Orientation.Horizontal)
          return true;
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        return _actualLastVisibleChild == visibleChildren.Count - 1;
      }
    }

    public bool IsViewPortAtLeft
    {
      get
      {
        if (Orientation == Panels.Orientation.Vertical)
          return true;
        return _scrollOffset == 0;
      }
    }

    public bool IsViewPortAtRight
    {
      get
      {
        if (Orientation == Panels.Orientation.Vertical)
          return true;
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        return _actualLastVisibleChild == visibleChildren.Count - 1;
      }
    }

    #endregion
  }
}

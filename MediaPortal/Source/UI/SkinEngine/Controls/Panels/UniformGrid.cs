#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.General;
using MediaPortal.Utilities;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class UniformGrid : Panel, IScrollViewerFocusSupport, IScrollInfo
  {
    #region Protected fields

    protected AbstractProperty _columnsProperty;
    protected AbstractProperty _rowsProperty;

    protected bool _doScroll = false; // Set to true by a scrollable container (ScrollViewer for example) if we should provide logical scrolling

    // Index of the first visible column/row (left/top) which will be drawn at our ActualPosition -
    // when modified by method SetScrollOffset, they will be applied the next time Arrange is called.
    protected int _scrollIndexX = 0;
    protected int _scrollIndexY = 0;

    protected int _actualNumVisibleCols = 0;
    protected int _actualNumVisibleRows = 0;

    protected float _actualColumnWidth;
    protected float _actualRowHeight;

    protected int _actualColumns = 0;
    protected int _actualRows = 0;

    #endregion

    #region Ctor

    public UniformGrid()
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
      _columnsProperty = new SProperty(typeof(int), 0);
      _rowsProperty = new SProperty(typeof(int), 0);
    }

    void Attach()
    {
      _columnsProperty.Attach(OnCompleteLayoutGetsInvalid);
      _rowsProperty.Attach(OnCompleteLayoutGetsInvalid);
    }

    void Detach()
    {
      _columnsProperty.Detach(OnCompleteLayoutGetsInvalid);
      _rowsProperty.Detach(OnCompleteLayoutGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      UniformGrid g = (UniformGrid) source;
      Columns = g.Columns;
      Rows = g.Rows;
      DoScroll = g.DoScroll;
      Attach();
    }

    #endregion

    #region Public properties

    public AbstractProperty ColumnsProperty
    {
      get { return _columnsProperty; }
    }

    public int Columns
    {
      get { return (int) _columnsProperty.GetValue(); }
      set { _columnsProperty.SetValue(value); }
    }

    public AbstractProperty RowsProperty
    {
      get { return _rowsProperty; }
    }

    public int Rows
    {
      get { return (int) _rowsProperty.GetValue(); }
      set { _rowsProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    public void SetScrollIndex(int scrollIndexX, int scrollIndexY)
    {
      if (_scrollIndexX == scrollIndexX && _scrollIndexY == scrollIndexY)
        return;
      if (scrollIndexX < 0)
        scrollIndexX = 0;
      if (scrollIndexY < 0)
        scrollIndexY = 0;
      _scrollIndexX = scrollIndexX;
      _scrollIndexY = scrollIndexY;
      InvalidateLayout(false, true);
      InvokeScrolled();
    }

    protected void CalculateCellCounts(int numChildren, ref int columns, ref int rows)
    {
      if (rows == 0)
      {
        if (columns == 0)
        {
          // If both rows and columns are not set, lay out in a square 
          rows = (int) Math.Sqrt(numChildren); 
          if (rows * rows < numChildren)
            rows++;
          columns = rows;
        }
        else
          rows = (numChildren + columns - 1) / columns;
      }
      else if (columns == 0) 
        columns = (numChildren + rows - 1) / rows; 
    }

    protected SizeF CalculateDesiredSize(SizeF totalSize, bool measureChildren,
        out float desiredColumnWidth, out float desiredRowHeight)
    {
      desiredColumnWidth = _actualColumns == 0 ? float.NaN : (int)totalSize.Width / _actualColumns; // Can be float.NaN
      desiredRowHeight = _actualRows == 0 ? float.NaN : (int)totalSize.Height / _actualRows; // Can be float.NaN
      SizeF childSize;
      foreach (FrameworkElement child in GetVisibleChildren())
      {
        if (measureChildren)
        {
          childSize = new SizeF(totalSize.Width / _actualColumns, totalSize.Height / _actualRows);
          child.Measure(ref childSize);
        }
        else
          childSize = child.DesiredSize;
        if (float.IsNaN(desiredColumnWidth) || childSize.Width > desiredColumnWidth)
          desiredColumnWidth = childSize.Width;
        if (float.IsNaN(desiredRowHeight) || childSize.Height > desiredRowHeight)
          desiredRowHeight = childSize.Height;
      }
      return new SizeF(desiredColumnWidth * _actualColumns, desiredRowHeight * _actualRows);
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      _actualColumns = Columns;
      _actualRows = Rows;
      CalculateCellCounts(visibleChildren.Count, ref _actualColumns, ref _actualRows);

      float desiredColumnWidth;
      float desiredRowHeight;
      return CalculateDesiredSize(totalSize, true, out desiredColumnWidth, out desiredRowHeight);
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();

      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int visibleChildrenCount = visibleChildren.Count;

      if (_doScroll)
        CalculateDesiredSize(new SizeF((float) ActualWidth, (float) ActualHeight), false, out _actualColumnWidth, out _actualRowHeight);
      else
      {
        _actualColumnWidth = (float) (ActualWidth/_actualColumns);
        _actualRowHeight = (float) (ActualHeight/_actualRows);
      }

      _actualNumVisibleCols = (int) (ActualWidth/_actualColumnWidth);
      _actualNumVisibleRows = (int) (ActualHeight/_actualRowHeight);

      if (_doScroll)
      {
        int maxScrollIndexX = Math.Max(_actualColumns - _actualNumVisibleCols, 0);
        int maxScrollIndexY = Math.Max(_actualRows - _actualNumVisibleRows, 0);

        if (_scrollIndexX > maxScrollIndexX)
          _scrollIndexX = maxScrollIndexX;
        if (_scrollIndexY > maxScrollIndexY)
          _scrollIndexY = maxScrollIndexY;
      }
      else
      {
        _scrollIndexX = 0;
        _scrollIndexY = 0;
      }

      // Hint: We cannot skip the arrangement of children above _scrollOffset or below the last visible child
      // because the rendering and focus system also needs the bounds of the currently invisible children
      for (int i = 0; i < visibleChildrenCount; i++)
      {
        FrameworkElement child = visibleChildren[i];
        SizeF childSize = new SizeF(_actualColumnWidth, _actualRowHeight);
        PointF position = new PointF(
            ActualPosition.X + (i % _actualColumns - _scrollIndexX)*_actualColumnWidth,
            ActualPosition.Y + (i / _actualColumns - _scrollIndexY)*_actualRowHeight);

        ArrangeChild(child, child.HorizontalAlignment, child.VerticalAlignment, ref position, ref childSize);
        child.Arrange(SharpDXExtensions.CreateRectangleF(position, childSize));
      }
    }

    private void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    private void ScrollColToFirst(int col)
    {
      SetScrollIndex(col, _scrollIndexY);
    }

    private void ScrollRowToFirst(int row)
    {
      SetScrollIndex(_scrollIndexX, row);
    }

    private void ScrollColToLast(int col)
    {
      SetScrollIndex(col - (_actualNumVisibleCols - 1), _scrollIndexY);
    }

    private void ScrollRowToLast(int row)
    {
      SetScrollIndex(_scrollIndexX, row - (_actualNumVisibleRows - 1));
    }

    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      if (_doScroll)
      {
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        for (int i = 0; i < visibleChildren.Count; i++)
        {
          FrameworkElement currentChild = visibleChildren[i];
          if (InVisualPath(currentChild, element))
          { // Found child to make visible
            int childCol = i%_actualColumns;
            int childRow = i/_actualColumns;
            if (childCol < _scrollIndexX)
              ScrollColToFirst(childCol);
            else if (childCol > _scrollIndexX + _actualNumVisibleCols - 1)
              ScrollColToLast(childCol);
            if (childRow < _scrollIndexY)
              ScrollRowToFirst(childRow);
            else if (childRow > _scrollIndexY + _actualNumVisibleRows - 1)
              ScrollRowToLast(childRow);
            break;
          }
        }
      }
      base.BringIntoView(element, elementBounds);
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
      return true;
    }

    #endregion

    #region Rendering

    protected override IEnumerable<FrameworkElement> GetRenderedChildren()
    {
      RectangleF bounds = ActualBounds;
      return Children.Where(element =>
        { // Don't render elements which are not visible, if we can scroll
          RectangleF elementBounds = element.ActualBounds;
          if (!element.IsVisible) return false;
          if (elementBounds.Right > bounds.Right + DELTA_DOUBLE) return false;
          if (elementBounds.Left < bounds.Left - DELTA_DOUBLE) return false;
          if (elementBounds.Top < bounds.Top - DELTA_DOUBLE) return false;
          if (elementBounds.Bottom > bounds.Bottom + DELTA_DOUBLE) return false;
          return true;
        });
    }

    #endregion

    public override void SaveUIState(IDictionary<string, object> state, string prefix)
    {
      base.SaveUIState(state, prefix);
      state[prefix + "/ScrollIndexX"] = _scrollIndexX;
      state[prefix + "/ScrollIndexY"] = _scrollIndexY;
    }

    public override void RestoreUIState(IDictionary<string, object> state, string prefix)
    {
      base.RestoreUIState(state, prefix);
      object index;
      int? iScrollX;
      int? iScrollY;
      if (state.TryGetValue(prefix + "/ScrollIndexX", out index) && (iScrollX = index as int?).HasValue &&
          state.TryGetValue(prefix + "/ScrollIndexY", out index) && (iScrollY = index as int?).HasValue)
        SetScrollIndex(iScrollX.Value, iScrollY.Value);
    }

    #region IScrollViewerFocusSupport implementation

    public bool FocusUp()
    {
      return MoveFocus1(MoveFocusDirection.Up);
    }

    public bool FocusDown()
    {
      return MoveFocus1(MoveFocusDirection.Down);
    }

    public bool FocusLeft()
    {
      return MoveFocus1(MoveFocusDirection.Left);
    }

    public bool FocusRight()
    {
      return MoveFocus1(MoveFocusDirection.Right);
    }

    public bool FocusPageUp()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;

      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      float limitPosition = ActualPosition.Y; // Initialize the value as if an element inside our visible range is focused - we'll have to move to first element
      for (int i = 0; i < _actualColumns; i++)
      {
        FrameworkElement child = CollectionUtils.SafeGet(visibleChildren, i);
        if (child == null)
          break;
        if (InVisualPath(child, currentElement))
          // One of the topmost elements is focused - move one page up
          limitPosition = child.ActualBounds.Bottom - (float) ActualHeight;
      }
      FrameworkElement nextElement;
      while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Up)) != null &&
          (nextElement.ActualPosition.Y > limitPosition - DELTA_DOUBLE))
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    public bool FocusPageDown()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;

      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      float limitPosition = ActualPosition.Y + (float) ActualHeight; // Initialize the value as if an element inside our visible range is focused - we'll have to move to last element
      for (int i = 0; i < _actualColumns; i++)
      {
        FrameworkElement child = CollectionUtils.SafeGet(visibleChildren, (_scrollIndexY+_actualNumVisibleRows-1)*_actualColumns+_scrollIndexX+i);
        if (child == null)
          break;
        if (InVisualPath(child, currentElement))
          // One of the elements at the bottom is focused - move one page down
          limitPosition = child.ActualPosition.Y + (float) ActualHeight;
      }
      FrameworkElement nextElement;
      while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Down)) != null &&
          (nextElement.ActualBounds.Bottom < limitPosition + DELTA_DOUBLE))
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    public bool FocusPageLeft()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;

      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      float limitPosition = ActualPosition.X; // Initialize the value as if an element inside our visible range is focused - we'll have to move to leftmost element
      for (int i = 0; i < _actualRows; i++)
      {
        FrameworkElement child = CollectionUtils.SafeGet(visibleChildren, (_scrollIndexY+i)*_actualColumns+_scrollIndexX);
        if (child == null)
          break;
        if (InVisualPath(child, currentElement))
          // One of the lefmost elements is focused - move one page left
          limitPosition = child.ActualBounds.Right - (float) ActualWidth;
      }
      FrameworkElement nextElement;
      while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Left)) != null &&
          (nextElement.ActualPosition.X > limitPosition - DELTA_DOUBLE))
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    public bool FocusPageRight()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;

      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      float limitPosition = ActualPosition.X + (float) ActualWidth; // Initialize the value as if an element inside our visible range is focused - we'll have to move to rightmost element
      for (int i = 0; i < _actualColumns; i++)
      {
        FrameworkElement child = CollectionUtils.SafeGet(visibleChildren, (_scrollIndexY+i)*_actualColumns+_scrollIndexX+_actualNumVisibleCols-1);
        if (child == null)
          break;
        if (InVisualPath(child, currentElement))
          // One of the rightmost elements is focused - move one page right
          limitPosition = child.ActualPosition.X + (float) ActualWidth;
      }
      FrameworkElement nextElement;
      while ((nextElement = FindNextFocusElement(visibleChildren, currentElement.ActualBounds, MoveFocusDirection.Right)) != null &&
          (nextElement.ActualBounds.Right < limitPosition - DELTA_DOUBLE))
        currentElement = nextElement;
      return currentElement.TrySetFocus(true);
    }

    public bool FocusHome()
    {
      return MoveFocusN(MoveFocusDirection.Up) && MoveFocusN(MoveFocusDirection.Left);
    }

    public bool FocusEnd()
    {
      return MoveFocusN(MoveFocusDirection.Down) && MoveFocusN(MoveFocusDirection.Right);
    }

    public bool ScrollDown(int numLines)
    {
      if (IsViewPortAtBottom)
        return false;
      SetScrollIndex(_scrollIndexX, _scrollIndexY + numLines);
      return true;
    }

    public bool ScrollUp(int numLines)
    {
      if (IsViewPortAtTop)
        return false;
      SetScrollIndex(_scrollIndexX, _scrollIndexY - numLines);
      return true;
    }

    public bool Scroll(float deltaX, float deltaY)
    {
      return false;
    }

    public bool BeginScroll()
    {
      return true;
    }

    public bool EndScroll()
    {
      return true;
    }

    #endregion

    #region IScrollInfo implementation

    public event ScrolledDlgt Scrolled;

    public bool DoScroll
    {
      get { return _doScroll; }
      set { _doScroll = value; }
    }

    public float TotalWidth
    {
      get { return _actualColumns*_actualColumnWidth; }
    }

    public float TotalHeight
    {
      get { return _actualRows*_actualRowHeight; }
    }

    public float ViewPortWidth
    {
      get { return (float) ActualWidth; }
    }

    public float ViewPortStartX
    {
      get { return _scrollIndexX*_actualColumnWidth; } 
    }

    public float ViewPortHeight
    {
      get { return (float) ActualHeight; }
    }

    public float ViewPortStartY
    {
      get { return _scrollIndexY*_actualRowHeight; }
    }

    public bool IsViewPortAtTop
    {
      get { return _scrollIndexY == 0; }
    }

    public bool IsViewPortAtBottom
    {
      get { return _scrollIndexY + _actualNumVisibleRows == _actualRows; }
    }

    public bool IsViewPortAtLeft
    {
      get { return _scrollIndexX == 0; }
    }

    public bool IsViewPortAtRight
    {
      get { return _scrollIndexX + _actualNumVisibleCols == _actualColumns; }
    }

    public int NumberOfVisibleLines
    {
      get { return _actualNumVisibleRows; }
    }

    #endregion
  }
}

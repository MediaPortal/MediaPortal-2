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
  /// <summary>
  /// Positions child elements in sequential position from left to right, breaking content to the next line at the edge of the containing box. 
  /// Subsequent ordering happens sequentially from top to bottom or from right to left, depending on the value of the <see cref="Orientation"/> property.
  /// </summary>
  // This class is implemented very similar to StackPanel; that's by design. We use almost the same technique for the layouting our element lines
  // as the StackPanel uses for its children. We don't use inheritance to avoid making it more complicated, so the code duplication is by design.
  public class WrapPanel : Panel, IScrollInfo, IScrollViewerFocusSupport
  {
    #region Consts

    /// <summary>
    /// To unburden our focus system, we only return a limited number of lines when a focus movement request is made
    /// (Keys or PgDown/PgUp). Actually, we wouldn't need to consider more lines than our visible range plus one line
    /// (Key up/down/left/right) resp. one page (PgUp/Down). But after the scroll command, the next line's arrangement
    /// doesn't happen before the next render pass, and thus, if our input thread is faster than our render thread, the focus
    /// might still be outside the visible range when the next scroll command arrives. This value is the number of lines
    /// we consider for focus movement outside our visible range. It can be thought of as the number of render thread passes
    /// which might be missing when an input event arrives.
    /// </summary>
    public const int NUM_ADD_MORE_FOCUS_LINES = 2;

    #endregion

    #region Classes

    public class LineMeasurementComparer : IComparer<LineMeasurement>
    {
      public int Compare(LineMeasurement x, LineMeasurement y)
      {
        if (x.EndIndex < y.StartIndex)
          return -1;
        if (x.StartIndex > y.EndIndex)
          return 1;
        return 0;
      }
    }

    /// <summary>
    /// Holds the measured data for one line (row or column, depending on orientation) of elements in the <see cref="WrapPanel"/>.
    /// </summary>
    public struct LineMeasurement
    {
      public int StartIndex;
      public int EndIndex;
      public float TotalExtendsInOrientationDirection;
      public float TotalExtendsInNonOrientationDirection;

      public static LineMeasurementComparer LineMeasurementComparerInstance = new LineMeasurementComparer();

      public static LineMeasurement Create()
      {
        LineMeasurement result;
        result.StartIndex = -1;
        result.EndIndex = -1;
        result.TotalExtendsInOrientationDirection = 0;
        result.TotalExtendsInNonOrientationDirection = 0;
        return result;
      }
    }

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

    protected IList<LineMeasurement> _arrangedLines = new List<LineMeasurement>();
    protected int _actualFirstVisibleLineIndex = 0;
    protected int _actualLastVisibleLineIndex = -1;
    protected int _originalFirstVisibleChildIndex = 0;

    //Includes additional lines that are rendered within the scroll margin
    protected int _actualFirstRenderedLineIndex;
    protected int _actualLastRenderedLineIndex;

    protected float _pendingPhysicalOffset = 0;
    protected float _actualPhysicalOffset = 0;
    protected bool _forcedOpacityMask = false;

    #endregion

    #region Ctor

    public WrapPanel()
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
      _orientationProperty = new SProperty(typeof(Orientation), Orientation.Horizontal);
      _loopScrollProperty = new SProperty(typeof(bool), false);
      _scrollMarginProperty = new SProperty(typeof(Thickness), new Thickness());
    }

    void Attach()
    {
      _orientationProperty.Attach(OnCompleteLayoutGetsInvalid);
      _scrollMarginProperty.Attach(OnMeasureGetsInvalid);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnCompleteLayoutGetsInvalid);
      _scrollMarginProperty.Detach(OnMeasureGetsInvalid);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      WrapPanel p = (WrapPanel) source;
      Orientation = p.Orientation;
      LoopScroll = p.LoopScroll;
      ScrollMargin = p.ScrollMargin;
      Attach();
    }

    #endregion

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
    /// Whether to enable looping to first/last line when scrolling
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

    #region Layouting

    /// <summary>
    /// Sets the scrolling index to a value that the line with the given <paramref name="lineIndex"/> is the
    /// first (in case <paramref name="first"/> is set to <c>true</c>) or last (<paramref name="first"/> is set to <c>false</c>)
    /// visible line.
    /// </summary>
    /// <remarks>
    /// The scroll index might be corrected by the layout system to a better value, if necessary.
    /// </remarks>
    /// <param name="lineIndex">Index to scroll to.</param>
    /// <param name="first">Make the line with the given <paramref name="lineIndex"/> the first or last shown line.</param>
    public void SetScrollIndex(int lineIndex, bool first)
    {
      SetScrollIndex(lineIndex, first, false);
    }

    /// <summary>
    /// Sets the scrolling index to a value that the line with the given <paramref name="lineIndex"/> is the
    /// first (in case <paramref name="first"/> is set to <c>true</c>) or last (<paramref name="first"/> is set to <c>false</c>)
    /// visible line.
    /// </summary>
    /// <remarks>
    /// The scroll index might be corrected by the layout system to a better value, if necessary.
    /// </remarks>
    /// <param name="lineIndex">Index to scroll to.</param>
    /// <param name="first">Make the line with the given <paramref name="lineIndex"/> the first or last shown line.</param>
    /// <param name="force">Whether the scroll should happen immediately and not be delayed/animated.</param>
    public virtual void SetScrollIndex(double lineIndex, bool first, bool force)
    {
      int index = (int)lineIndex;
      float offset = (float)(lineIndex % 1);
      lock (_renderLock)
      {
        if (_pendingScrollIndex == index && _pendingPhysicalOffset == offset && _scrollToFirst == first ||
            (!_pendingScrollIndex.HasValue && _actualPhysicalOffset == offset &&
             ((_scrollToFirst && _actualFirstVisibleLineIndex == index) ||
              (!_scrollToFirst && _actualLastVisibleLineIndex == index))))
          return;
        _pendingScrollIndex = index;
        _pendingPhysicalOffset = offset;
        _scrollToFirst = first;
      }
      InvalidateLayout(false, true);
    }

    protected LineMeasurement CalculateLine(IList<FrameworkElement> children, int startIndex, SizeF? measureSize, bool invertLayoutDirection)
    {
      LineMeasurement result = LineMeasurement.Create();
      if (invertLayoutDirection)
        result.EndIndex = startIndex;
      else
        result.StartIndex = startIndex;
      result.TotalExtendsInNonOrientationDirection = 0;
      int numChildren = children.Count;
      int directionOffset = invertLayoutDirection ? -1 : 1;
      float offsetInOrientationDirection = 0;
      float extendsInOrientationDirection = measureSize.HasValue ? GetExtendsInOrientationDirection(Orientation, measureSize.Value) : float.NaN;
      int currentIndex = startIndex;
      for (; invertLayoutDirection ? (currentIndex >= 0) : (currentIndex < numChildren); currentIndex += directionOffset)
      {
        FrameworkElement child = children[currentIndex];
        SizeF desiredChildSize;
        if (measureSize.HasValue)
        {
          desiredChildSize = measureSize.Value;
          child.Measure(ref desiredChildSize);
        }
        else
          desiredChildSize = child.DesiredSize;
        float lastOffsetInOrientationDirection = offsetInOrientationDirection;
        offsetInOrientationDirection += GetExtendsInOrientationDirection(Orientation, desiredChildSize);
        if (!float.IsNaN(extendsInOrientationDirection) && offsetInOrientationDirection > extendsInOrientationDirection)
        {
          if (invertLayoutDirection)
            result.StartIndex = currentIndex + 1;
          else
            result.EndIndex = currentIndex - 1;
          result.TotalExtendsInOrientationDirection = lastOffsetInOrientationDirection;
          return result;
        }
        if (desiredChildSize.Height > result.TotalExtendsInNonOrientationDirection)
          result.TotalExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(Orientation, desiredChildSize);
      }
      if (invertLayoutDirection)
        result.StartIndex = currentIndex + 1;
      else
        result.EndIndex = currentIndex - 1;
      result.TotalExtendsInOrientationDirection = offsetInOrientationDirection;
      return result;
    }

    protected void LayoutLine(IList<FrameworkElement> children, PointF pos, LineMeasurement line)
    {
      float offset = 0;
      for (int i = line.StartIndex; i <= line.EndIndex; i++)
      {
        FrameworkElement layoutChild = children[i];
        SizeF desiredChildSize = layoutChild.DesiredSize;
        SizeF size;
        PointF location;

        if (Orientation == Orientation.Horizontal)
        {
          size = new SizeF(desiredChildSize.Width, line.TotalExtendsInNonOrientationDirection);
          location = new PointF(pos.X + offset, pos.Y);
          ArrangeChildVertical(layoutChild, layoutChild.VerticalAlignment, ref location, ref size);
          offset += desiredChildSize.Width;
        }
        else
        {
          size = new SizeF(line.TotalExtendsInNonOrientationDirection, desiredChildSize.Height);
          location = new PointF(pos.X, pos.Y + offset);
          ArrangeChildHorizontal(layoutChild, layoutChild.HorizontalAlignment, ref location, ref size);
          offset += desiredChildSize.Height;
        }

        layoutChild.Arrange(SharpDXExtensions.CreateRectangleF(location, size));
      }
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int numVisibleChildren = visibleChildren.Count;
      if (numVisibleChildren == 0)
        return new SizeF();
      float totalDesiredWidth = 0;
      float totalDesiredHeight = 0;
      int index = 0;
      while (index < numVisibleChildren)
      {
        LineMeasurement line = CalculateLine(visibleChildren, index, totalSize, false);
        if (line.EndIndex < line.StartIndex)
          // Element doesn't fit
          break;
        switch (Orientation)
        {
          case Orientation.Horizontal:
            if (line.TotalExtendsInOrientationDirection > totalDesiredWidth)
              totalDesiredWidth = line.TotalExtendsInOrientationDirection;
            totalDesiredHeight += line.TotalExtendsInNonOrientationDirection;
            break;
          case Orientation.Vertical:
            if (line.TotalExtendsInOrientationDirection > totalDesiredHeight)
              totalDesiredHeight = line.TotalExtendsInOrientationDirection;
            totalDesiredWidth += line.TotalExtendsInNonOrientationDirection;
            break;
        }
        index = line.EndIndex + 1;
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
        //Determing scroll margins in scroll direction
        float scrollMarginBefore;
        float scrollMarginAfter;
        GetScrollMargin(out scrollMarginBefore, out scrollMarginAfter);
        // Hint: We cannot skip the arrangement of lines above _actualFirstVisibleLineIndex or below _actualLastVisibleLineIndex
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
              _actualFirstVisibleLineIndex = pendingSI;
            else
            {
              _actualLastVisibleLineIndex = pendingSI;
              //If we have an offset then there will be part of an additional item visible
              if (physicalOffset != 0)
                _actualLastVisibleLineIndex++;
              invertLayouting = true;
            }
            _pendingScrollIndex = null;
          }

        _arrangedLines = new List<LineMeasurement>();
        int index = 0;
        while (index < numVisibleChildren)
        {
          LineMeasurement line = CalculateLine(visibleChildren, index, _innerRect.Size, false);
          _arrangedLines.Add(line);
          index = line.EndIndex + 1;
        }

        // 1) Calculate scroll indices
        if (_doScroll)
        { // Calculate last visible child
          //Substract scroll margins from avalable space, additional items in the margin will be added later
          float spaceLeft = actualExtendsInNonOrientationDirection - scrollMarginBefore - scrollMarginAfter;
          if (invertLayouting)
          {
            CalcHelper.Bound(ref _actualLastVisibleLineIndex, 0, _arrangedLines.Count - 1);

            //Allow space for partially visible items at top and bottom
            if (physicalOffset != 0)
            {
              float offsetItemSize = _arrangedLines[_actualLastVisibleLineIndex].TotalExtendsInOrientationDirection;
              spaceLeft += offsetItemSize;
              startPosition -= offsetItemSize * physicalOffset;
            }

            _actualFirstVisibleLineIndex = _actualLastVisibleLineIndex + 1;
            while (_actualFirstVisibleLineIndex > 0)
            {
              LineMeasurement line = _arrangedLines[_actualFirstVisibleLineIndex - 1];
              spaceLeft -= line.TotalExtendsInNonOrientationDirection;
              if (spaceLeft + DELTA_DOUBLE < 0)
                break;
              _actualFirstVisibleLineIndex--;
            }

            if (spaceLeft > 0)
            { // Correct the last scroll index to fill the available space
              int maxArrangedLine = _arrangedLines.Count - 1;
              while (_actualLastVisibleLineIndex < maxArrangedLine)
              {
                LineMeasurement line = _arrangedLines[_actualLastVisibleLineIndex + 1];
                spaceLeft -= line.TotalExtendsInNonOrientationDirection;
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break; // Found item which is not visible any more
                _actualLastVisibleLineIndex++;
              }
            }
          }
          else
          {
            CalcHelper.Bound(ref _actualFirstVisibleLineIndex, 0, _arrangedLines.Count - 1);

            //Allow space for partially visible items at top and bottom
            if (physicalOffset != 0)
            {
              float offsetItemSize = _arrangedLines[_actualFirstVisibleLineIndex].TotalExtendsInOrientationDirection;
              spaceLeft += offsetItemSize;
              startPosition -= offsetItemSize * physicalOffset;
            }

            _actualLastVisibleLineIndex = _actualFirstVisibleLineIndex - 1;
            while (_actualLastVisibleLineIndex < _arrangedLines.Count - 1)
            {
              LineMeasurement line = _arrangedLines[_actualLastVisibleLineIndex + 1];
              spaceLeft -= line.TotalExtendsInNonOrientationDirection;
              if (spaceLeft + DELTA_DOUBLE < 0)
                break;
              _actualLastVisibleLineIndex++;
            }

            if (spaceLeft > 0)
            { // Correct the first scroll index to fill the available space
              while (_actualFirstVisibleLineIndex > 0)
              {
                LineMeasurement line = _arrangedLines[_actualFirstVisibleLineIndex - 1];
                spaceLeft -= line.TotalExtendsInNonOrientationDirection;
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break; // Found item which is not visible any more
                _actualFirstVisibleLineIndex--;
              }
            }
          }
        }
        else
        {
          _actualFirstVisibleLineIndex = 0;
          _actualLastVisibleLineIndex = _arrangedLines.Count - 1;
        }

        _actualFirstRenderedLineIndex = _actualFirstVisibleLineIndex;
        _actualLastRenderedLineIndex = _actualLastVisibleLineIndex;
        //calculate additional lines in the scroll margin
        float inactiveSpaceLeft = scrollMarginBefore;
        while (_actualFirstRenderedLineIndex > 0)
        {
          inactiveSpaceLeft -= _arrangedLines[_actualFirstRenderedLineIndex - 1].TotalExtendsInNonOrientationDirection;
          if (inactiveSpaceLeft + DELTA_DOUBLE < 0)
            break;
          _actualFirstRenderedLineIndex--;
        }
        inactiveSpaceLeft = scrollMarginAfter;
        while (_actualLastRenderedLineIndex < _arrangedLines.Count - 1)
        {
          inactiveSpaceLeft -= _arrangedLines[_actualLastRenderedLineIndex + 1].TotalExtendsInNonOrientationDirection;
          if (inactiveSpaceLeft + DELTA_DOUBLE < 0)
            break;
          _actualLastRenderedLineIndex++;
        }

        // 2) Calculate start position
        for (int i = 0; i < _actualFirstVisibleLineIndex; i++)
        {
          LineMeasurement line = _arrangedLines[i];
          startPosition -= line.TotalExtendsInNonOrientationDirection;
        }

        // 3) Arrange children
        if (Orientation == Orientation.Vertical)
          _totalHeight = actualExtendsInOrientationDirection;
        else
          _totalWidth = actualExtendsInOrientationDirection;
        PointF position = Orientation == Orientation.Vertical ?
            new PointF(actualPosition.X + startPosition, actualPosition.Y) :
            new PointF(actualPosition.X, actualPosition.Y + startPosition);
        foreach (LineMeasurement line in _arrangedLines)
        {
          LayoutLine(visibleChildren, position, line);

          startPosition += line.TotalExtendsInNonOrientationDirection;
          if (Orientation == Orientation.Vertical)
          {
            position = new PointF(actualPosition.X + startPosition, actualPosition.Y);
            _totalWidth += line.TotalExtendsInNonOrientationDirection;
          }
          else
          {
            position = new PointF(actualPosition.X, actualPosition.Y + startPosition);
            _totalHeight += line.TotalExtendsInNonOrientationDirection;
          }
        }
      }
      else
      {
        _actualFirstVisibleLineIndex = _actualFirstRenderedLineIndex = 0;
        _actualLastVisibleLineIndex = _actualLastRenderedLineIndex = -1;
      }
      if (fireScrolled)
        InvokeScrolled();
    }

    protected void GetScrollMargin(out float widthBefore, out float widthAfter)
    {
      Thickness thickness = ScrollMargin ?? new Thickness();
      if (Orientation == Orientation.Vertical)
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
      MakeChildVisible(element, ref elementBounds);
      base.BringIntoView(element, elementBounds);
    }

    /// <summary>
    /// Summarizes the extends of the given <paramref name="lines"/> in non-orientation direction.
    /// </summary>
    /// <param name="lines">Lines to summarize.</param>
    /// <param name="startIndex">Index of the first line, inclusive.</param>
    /// <param name="endIndex">Index of the last line, exclusive.</param>
    /// <returns></returns>
    protected float SumActualLineExtendsInNonOrientationDirection(IList<LineMeasurement> lines, int startIndex, int endIndex)
    {
      int numLines = lines.Count;
      CalcHelper.Bound(ref startIndex, 0, numLines - 1);
      CalcHelper.Bound(ref endIndex, 0, numLines); // End index is exclusive
      if (startIndex == endIndex || numLines == 0)
        return 0;
      bool invert = startIndex > endIndex;
      if (invert)
      {
        int tmp = startIndex;
        startIndex = endIndex;
        endIndex = tmp;
      }
      float result = 0;
      for (int i = startIndex; i < endIndex; i++)
        result += lines[i].TotalExtendsInNonOrientationDirection;
      return invert ? -result : result;
    }

    protected virtual void MakeChildVisible(UIElement element, ref RectangleF elementBounds)
    {
      if (_doScroll)
      {
        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        int lineIndex = 0;
        IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
        foreach (LineMeasurement line in lines)
        {
          for (int childIndex = line.StartIndex; childIndex <= line.EndIndex; childIndex++)
          {
            FrameworkElement currentChild = visibleChildren[childIndex];
            if (InVisualPath(currentChild, element))
            {
              int oldFirstVisibleLine = _actualFirstVisibleLineIndex;
              int oldLastVisibleLine = _actualLastVisibleLineIndex;
              bool first;
              if (lineIndex < oldFirstVisibleLine)
                first = true;
              else if (lineIndex <= oldLastVisibleLine)
                // Already visible
                break;
              else
                first = false;
              SetScrollIndex(lineIndex, first);
              // Adjust the scrolled element's bounds; Calculate the difference between positions of childen at old/new child indices
              float extendsInOrientationDirection = SumActualLineExtendsInNonOrientationDirection(lines,
                  first ? oldFirstVisibleLine : oldLastVisibleLine, lineIndex);
              if (Orientation == Orientation.Horizontal)
                elementBounds.X -= extendsInOrientationDirection;
              else
                elementBounds.Y -= extendsInOrientationDirection;
              break;
            }
          }
          lineIndex++;
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
        bool linesBeforeAndAfter)
    {
      if (!IsVisible)
        return;
      if (Focusable)
        outElements.Add(this);
      IList<FrameworkElement> children = GetVisibleChildren();
      int numLinesBeforeAndAfter = linesBeforeAndAfter ? NUM_ADD_MORE_FOCUS_LINES : 0;
      AddFocusedElementRange(children, startingRect, _actualFirstVisibleLineIndex, _actualLastVisibleLineIndex,
          numLinesBeforeAndAfter, numLinesBeforeAndAfter, outElements);
    }

    protected void AddFocusedElementRange(IList<FrameworkElement> availableElements, RectangleF? startingRect,
        int firstLineIndex, int lastLineIndex, int linesBefore, int linesAfter, ICollection<FrameworkElement> outElements)
    {
      IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
      int numLines = lines.Count;
      if (numLines == 0)
        return;
      CalcHelper.Bound(ref firstLineIndex, 0, numLines - 1);
      CalcHelper.Bound(ref lastLineIndex, 0, numLines - 1);
      if (linesBefore > 0)
      {
        // Find children before the first index which have focusable elements.
        int formerNumElements = outElements.Count;
        for (int lineIndex = firstLineIndex - 1; lineIndex >= 0; lineIndex--)
        {
          LineMeasurement line = lines[lineIndex];
          for (int childIndex = line.StartIndex; childIndex <= line.EndIndex; childIndex++)
          {
            FrameworkElement fe = availableElements[childIndex];
            fe.AddPotentialFocusableElements(startingRect, outElements);
          }
          if (formerNumElements == outElements.Count)
            continue;
          // Found focusable elements
          linesBefore--;
          if (linesBefore == 0)
            break;
          formerNumElements = outElements.Count;
        }
      }
      for (int lineIndex = firstLineIndex; lineIndex <= lastLineIndex; lineIndex++)
      {
        LineMeasurement line = lines[lineIndex];
        for (int childIndex = line.StartIndex; childIndex <= line.EndIndex; childIndex++)
        {
          FrameworkElement fe = availableElements[childIndex];
          fe.AddPotentialFocusableElements(startingRect, outElements);
        }
      }
      if (linesAfter > 0)
      {
        // Find children after the last index which have focusable elements.
        int formerNumElements = outElements.Count;
        for (int lineIndex = lastLineIndex + 1; lineIndex < lines.Count; lineIndex++)
        {
          LineMeasurement line = lines[lineIndex];
          for (int childIndex = line.StartIndex; childIndex <= line.EndIndex; childIndex++)
          {
            FrameworkElement fe = availableElements[childIndex];
            fe.AddPotentialFocusableElements(startingRect, outElements);
          }
          if (formerNumElements == outElements.Count)
            continue;
          // Found focusable elements
          linesAfter--;
          if (linesAfter == 0)
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
    /// Focuses the first line if an item on the last line currently has focus
    /// </summary>
    /// <returns>true if the first line was focused</returns>
    protected virtual bool TryLoopToFirstLine()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
      if (lines.Count == 0)
        return false;
      var lastLine = lines[lines.Count - 1];
      for (int childIndex = lastLine.StartIndex; childIndex <= lastLine.EndIndex; childIndex++)
      {
        if (InVisualPath(visibleChildren[childIndex], currentElement))
        {
          //item on last line has focus
          //assume first line always has at least same number of items as last line
          //set focus to item in same position on first line
          SetScrollIndex(0, true);
          visibleChildren[childIndex - lastLine.StartIndex].SetFocusPrio = SetFocusPriority.Default;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Focuses the last line if the first line currently has focus
    /// </summary>
    /// <returns>true if the last line was focused</returns>
    protected virtual bool TryLoopToLastLine()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
      if (lines.Count == 0)
        return false;
      var firstLine = lines[0];
      for (int childIndex = firstLine.StartIndex; childIndex <= firstLine.EndIndex; childIndex++)
      {
        if (InVisualPath(visibleChildren[childIndex], currentElement))
        {
          //item on first line has focus
          var lastLine = lines[lines.Count - 1];
          //last line may have fewer items than first line
          //set focus to item in same position or last item if less
          int itemIndex = lastLine.StartIndex + childIndex;
          CalcHelper.Bound(ref itemIndex, 0, visibleChildren.Count - 1);
          SetScrollIndex(int.MaxValue, false);
          visibleChildren[itemIndex].SetFocusPrio = SetFocusPriority.Default;
          return true;
        }
      }
      return false;
    }

    #endregion

    #region Base overrides

    public override void BringIntoView(int index)
    {
      int firstVisibleLine = _actualFirstVisibleLineIndex;
      int lastVisibleLine = _actualLastVisibleLineIndex;
      IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
      int numLines = lines.Count;
      if (firstVisibleLine < 0 || firstVisibleLine >= numLines ||
          lastVisibleLine < 0 || lastVisibleLine >= numLines)
        return;
      if (index < lines[firstVisibleLine].StartIndex)
        SetScrollIndex(index, true, true);
      else if (index > lines[lastVisibleLine].EndIndex)
        SetScrollIndex(index, false, true);
    }

    public override void SaveUIState(IDictionary<string, object> state, string prefix)
    {
      base.SaveUIState(state, prefix);
      state[prefix + "/FirstVisibleLine"] = _actualFirstVisibleLineIndex;
    }

    public override void RestoreUIState(IDictionary<string, object> state, string prefix)
    {
      base.RestoreUIState(state, prefix);
      object first;
      int? iFirst;
      if (state.TryGetValue(prefix + "/FirstVisibleLine", out first) && (iFirst = first as int?).HasValue)
        SetScrollIndex(iFirst.Value, true, true);
    }

    #endregion

    #region Rendering

    protected override IEnumerable<FrameworkElement> GetRenderedChildren()
    {
      if (_actualFirstRenderedLineIndex < 0 || _actualLastRenderedLineIndex < _actualFirstRenderedLineIndex)
        return new List<FrameworkElement>();
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      int start = _arrangedLines[_actualFirstRenderedLineIndex].StartIndex;
      int end = _arrangedLines[_actualLastRenderedLineIndex].EndIndex;
      return visibleChildren.Skip(start).Take(end - start + 1);
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
      if (Orientation == Orientation.Horizontal)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Up) || (LoopScroll && TryLoopToLastLine());
      return false;
    }

    public virtual bool FocusDown()
    {
      if (Orientation == Orientation.Horizontal)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Down) || (LoopScroll && TryLoopToFirstLine());
      return false;
    }

    public virtual bool FocusLeft()
    {
      if (Orientation == Orientation.Vertical)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Left) || (LoopScroll && TryLoopToLastLine());
      return false;
    }

    public virtual bool FocusRight()
    {
      if (Orientation == Orientation.Vertical)
        return AlignedPanelMoveFocus1(MoveFocusDirection.Right) || (LoopScroll && TryLoopToFirstLine());
      return false;
    }

    public virtual bool FocusPageUp()
    {
      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
        if (lines.Count == 0)
          return false;
        int firstVisibleLineIndex = _actualFirstVisibleLineIndex;
        CalcHelper.Bound(ref firstVisibleLineIndex, 0, lines.Count - 1);
        LineMeasurement firstVisibleLine = lines[firstVisibleLineIndex];
        float limitPosition = ActualPosition.Y; // Initialize as if an element inside our visible range is focused - then, we must move to the first element
        for (int childIndex = firstVisibleLine.StartIndex; childIndex <= firstVisibleLine.EndIndex; childIndex++)
        {
          FrameworkElement child = visibleChildren[childIndex];
          if (!InVisualPath(child, currentElement))
            continue;
          // One of the topmost elements is focused - move one page up
          limitPosition = child.ActualBounds.Bottom - (float) ActualHeight;
          break;
        }
        int firstPlusOne = firstVisibleLineIndex + 1;
        CalcHelper.Bound(ref firstPlusOne, 0, lines.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(lines.Take(firstPlusOne).SelectMany(
            line => visibleChildren.Skip(line.StartIndex).Take(line.EndIndex - line.StartIndex + 1)),
            currentElement.ActualBounds, MoveFocusDirection.Up)) != null && (nextElement.ActualPosition.Y > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusPageDown()
    {
      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
        if (lines.Count == 0)
          return false;
        int lastVisibleLineIndex = _actualLastVisibleLineIndex;
        CalcHelper.Bound(ref lastVisibleLineIndex, 0, lines.Count - 1);
        LineMeasurement lastVisibleLine = lines[lastVisibleLineIndex];
        float limitPosition = ActualPosition.Y + (float) ActualHeight; // Initialize as if an element inside our visible range is focused - then, we must move to the last element
        for (int childIndex = lastVisibleLine.StartIndex; childIndex <= lastVisibleLine.EndIndex; childIndex++)
        {
          FrameworkElement child = visibleChildren[childIndex];
          if (!InVisualPath(child, currentElement))
            continue;
          // One of the elements at the bottom is focused - move one page down
          limitPosition = child.ActualPosition.Y + (float) ActualHeight;
        }
        int lastMinusOne = lastVisibleLineIndex - 1;
        CalcHelper.Bound(ref lastMinusOne, 0, lines.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(lines.Skip(lastMinusOne).SelectMany(
            line => visibleChildren.Skip(line.StartIndex).Take(line.EndIndex - line.StartIndex + 1)),
            currentElement.ActualBounds, MoveFocusDirection.Down)) != null && (nextElement.ActualBounds.Bottom < limitPosition + DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusPageLeft()
    {
      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
        if (lines.Count == 0)
          return false;
        int firstVisibleLineIndex = _actualFirstVisibleLineIndex;
        CalcHelper.Bound(ref firstVisibleLineIndex, 0, lines.Count - 1);
        LineMeasurement firstVisibleLine = lines[firstVisibleLineIndex];
        float limitPosition = ActualPosition.X; // Initialize as if an element inside our visible range is focused - then, we must move to the first element
        for (int childIndex = firstVisibleLine.StartIndex; childIndex <= firstVisibleLine.EndIndex; childIndex++)
        {
          FrameworkElement child = visibleChildren[childIndex];
          if (!InVisualPath(child, currentElement))
            continue;
          // One of the leftmost elements is focused - move one page left
          limitPosition = child.ActualBounds.Right - (float) ActualWidth;
          break;
        }
        int firstPlusOne = firstVisibleLineIndex + 1;
        CalcHelper.Bound(ref firstPlusOne, 0, lines.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(lines.Take(firstPlusOne).SelectMany(
            line => visibleChildren.Skip(line.StartIndex).Take(line.EndIndex - line.StartIndex + 1)),
            currentElement.ActualBounds, MoveFocusDirection.Left)) != null && (nextElement.ActualPosition.X > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public virtual bool FocusPageRight()
    {
      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        IList<FrameworkElement> visibleChildren = GetVisibleChildren();
        if (visibleChildren.Count == 0)
          return false;
        IList<LineMeasurement> lines = new List<LineMeasurement>(_arrangedLines);
        if (lines.Count == 0)
          return false;
        int lastVisibleLineIndex = _actualLastVisibleLineIndex;
        CalcHelper.Bound(ref lastVisibleLineIndex, 0, lines.Count - 1);
        LineMeasurement lastVisibleLine = lines[lastVisibleLineIndex];
        float limitPosition = ActualPosition.X + (float) ActualWidth; // Initialize as if an element inside our visible range is focused - then, we must move to the first element
        for (int childIndex = lastVisibleLine.StartIndex; childIndex <= lastVisibleLine.EndIndex; childIndex++)
        {
          FrameworkElement child = visibleChildren[childIndex];
          if (child == null)
            return false;
          if (!InVisualPath(child, currentElement))
            continue;
          // One of the elements at the bottom is focused - move one page down
          limitPosition = child.ActualPosition.X + (float) ActualWidth;
          break;
        }
        int lastMinusOne = lastVisibleLineIndex - 1;
        CalcHelper.Bound(ref lastMinusOne, 0, lines.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(lines.Skip(lastMinusOne).SelectMany(
            line => visibleChildren.Skip(line.StartIndex).Take(line.EndIndex - line.StartIndex + 1)),
            currentElement.ActualBounds, MoveFocusDirection.Right)) != null && (nextElement.ActualBounds.Right < limitPosition - DELTA_DOUBLE))
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
      visibleChildren[0].SetFocusPrio = SetFocusPriority.Default;
      return true;
    }

    public virtual bool FocusEnd()
    {
      IList<FrameworkElement> visibleChildren = GetVisibleChildren();
      if (visibleChildren.Count == 0)
        return false;
      SetScrollIndex(int.MaxValue, false);
      visibleChildren[visibleChildren.Count - 1].SetFocusPrio = SetFocusPriority.Default;
      return true;
    }

    public virtual bool ScrollDown(int numLines)
    {
      if (Orientation == Orientation.Horizontal)
      {
        if (IsViewPortAtBottom)
          return false;
        SetScrollIndex(_actualFirstVisibleLineIndex + numLines, true);
        return true;
      }
      return false;
    }

    public virtual bool ScrollUp(int numLines)
    {
      if (Orientation == Orientation.Horizontal)
      {
        if (IsViewPortAtTop)
          return false;
        SetScrollIndex(_actualFirstVisibleLineIndex - numLines, true);
        return true;
      }
      return false;
    }

    public bool Scroll(float deltaX, float deltaY)
    {
      int visibleLines = NumberOfVisibleLines;

      if (visibleLines == 0)
        return false;

      int numLines = Orientation == Orientation.Horizontal ?
        (int)(deltaY / (ActualHeight / visibleLines)) :
        0 /*TODO: we don't have a "visbile columns" yet... (int)(deltaX / (ActualWidth / visibleLines))*/;

      SetScrollIndex(_originalFirstVisibleChildIndex - numLines, true);
      return false;
    }

    public bool BeginScroll()
    {
      _originalFirstVisibleChildIndex = _actualFirstVisibleLineIndex;
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
      get { return Orientation == Orientation.Horizontal ? 0 : SumActualLineExtendsInNonOrientationDirection(_arrangedLines, 0, _actualFirstVisibleLineIndex); }
    }

    public virtual float ViewPortStartY
    {
      get { return Orientation == Orientation.Vertical ? 0 : SumActualLineExtendsInNonOrientationDirection(_arrangedLines, 0, _actualFirstVisibleLineIndex); }
    }

    public virtual bool IsViewPortAtTop
    {
      get { return Orientation == Orientation.Vertical || _actualFirstVisibleLineIndex == 0; }
    }

    public virtual bool IsViewPortAtBottom
    {
      get { return Orientation == Orientation.Vertical || _actualLastVisibleLineIndex == GetVisibleChildren().Count - 1; }
    }

    public virtual bool IsViewPortAtLeft
    {
      get { return Orientation == Orientation.Horizontal || _actualFirstVisibleLineIndex == 0; }
    }

    public virtual bool IsViewPortAtRight
    {
      get { return Orientation == Orientation.Horizontal || _actualLastVisibleLineIndex == GetVisibleChildren().Count - 1; }
    }

    public int NumberOfVisibleLines
    {
      get { return _actualLastVisibleLineIndex - _actualFirstVisibleLineIndex + 1; }
    }

    #endregion
  }
}

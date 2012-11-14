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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.Utils;
using MediaPortal.Utilities;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  public class VirtualizingStackPanel : StackPanel
  {
    #region Consts

    /// <summary>
    /// Number of items to keep in the invisible range before disposing items.
    /// </summary>
    public const int INVISIBLE_KEEP_THRESHOLD = 100;

    /// <summary>
    /// We have to cope with the situation where our items have all a DesiredSize of 0 because they first need some render
    /// cycles to set their values to be able to calculate their size. To avoid that we iterate through the complete
    /// collection and only finding 0 sized items, we limit the maximum number of items to <see cref="MAX_NUM_VISIBLE_ITEMS"/>.
    /// </summary>
    public const int MAX_NUM_VISIBLE_ITEMS = 50;

    #endregion

    #region Protected fields

    protected IItemProvider _itemProvider = null;

    // Assigned in Arrange
    protected int _arrangedItemsStartIndex = 0;
    protected IList<FrameworkElement> _arrangedItems = new List<FrameworkElement>();

    // Assigned in CalculateInnerDesiredSize
    protected float _averageItemSize = 0;

    protected IItemProvider _newItemProvider = null; // Store new item provider until next render cylce

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      VirtualizingStackPanel p = (VirtualizingStackPanel) source;
      _itemProvider = copyManager.GetCopy(p._itemProvider);
      _arrangedItems.Clear();
      _averageItemSize = 0;
    }

    public override void Dispose()
    {
      base.Dispose();
      IItemProvider itemProvider = _itemProvider;
      _itemProvider = null;
      if (itemProvider != null)
        MPF.TryCleanupAndDispose(itemProvider);
      itemProvider = _newItemProvider;
      _newItemProvider = null;
      if (itemProvider != null)
        MPF.TryCleanupAndDispose(itemProvider);
    }

    #endregion

    public void SetItemProvider(IItemProvider itemProvider)
    {
      if (_elementState == ElementState.Running)
        lock (Children.SyncRoot)
        {
          if (_newItemProvider == itemProvider)
            return;
          if (_newItemProvider != null)
            MPF.TryCleanupAndDispose(_newItemProvider);
          _newItemProvider = null;
          if (_itemProvider == itemProvider)
            return;
          _newItemProvider = itemProvider;
        }
      else
      {
        if (_newItemProvider == itemProvider)
          return;
        if (_newItemProvider != null)
          MPF.TryCleanupAndDispose(_newItemProvider);
        _newItemProvider = null;
        if (_itemProvider == itemProvider)
          return;
        if (_itemProvider != null)
          MPF.TryCleanupAndDispose(_itemProvider);
        _itemProvider = itemProvider;
      }
      InvalidateLayout(true, true);
    }

    #region Public properties

    public IItemProvider ItemProvider
    {
      get { return _itemProvider; }
    }

    public bool IsVirtualizing
    {
      get { return _itemProvider != null; }
    }

    #endregion

    #region Layouting

    public override void SetScrollIndex(int childIndex, bool first)
    {
      // Albert, 2010-12-28: We need to override this method because we need to lock on Children.SyncRoot
      lock (Children.SyncRoot)
      {
        if (_pendingScrollIndex == childIndex && _scrollToFirst == first ||
            (!_pendingScrollIndex.HasValue &&
             ((_scrollToFirst && _actualFirstVisibleChildIndex == childIndex) ||
              (!_scrollToFirst && _actualLastVisibleChildIndex == childIndex))))
          return;
        _pendingScrollIndex = childIndex;
        _scrollToFirst = first;
      }
      InvalidateLayout(true, true);
    }

    // It's actually "GetVisibleChildren", but that member already exists in Panel
    protected IList<FrameworkElement> GetMeasuredViewableChildren(SizeF totalSize, out SizeF resultSize)
    {
      resultSize = SizeF.Empty;
      IList<FrameworkElement> result = new List<FrameworkElement>(20);
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return result;

      int numItems = itemProvider.NumItems;
      if (numItems == 0)
        return result;
      float availableSize = GetExtendsInNonOrientationDirection(Orientation, totalSize);
      if (!_doScroll)
        _actualFirstVisibleChildIndex = 0;
      int start = _actualFirstVisibleChildIndex;
      CalcHelper.Bound(ref start, 0, numItems - 1);
      int end = start - 1;
      float sumExtendsInOrientationDirection = 0;
      float maxExtendsInNonOrientationDirection = 0;

      int ct = MAX_NUM_VISIBLE_ITEMS;

      // From scroll index until potentially up to the end
      do
      {
        if (end == numItems - 1)
          // Reached the last item
          break;
        FrameworkElement item = GetItem(end + 1, itemProvider, true);
        if (item == null || !item.IsVisible)
          continue;
        if (ct-- == 0)
          break;
        float childExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, item.DesiredSize);
        if (childExtendsInOrientationDirection > availableSize + DELTA_DOUBLE)
          break;
        float childExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(Orientation, item.DesiredSize);
        availableSize -= childExtendsInOrientationDirection;
        sumExtendsInOrientationDirection += childExtendsInOrientationDirection;
        if (childExtendsInNonOrientationDirection > maxExtendsInNonOrientationDirection)
          maxExtendsInNonOrientationDirection = childExtendsInNonOrientationDirection;
        result.Add(item);
        end++;
      } while (availableSize > 0 || !_doScroll);
      // If there is still space left, try to get items above scroll index
      while (availableSize > 0)
      {
        if (start == 0)
          // Reached the last item
          break;
        FrameworkElement item = GetItem(start - 1, itemProvider, true);
        if (item == null || !item.IsVisible)
          continue;
        if (ct-- == 0)
          break;
        float childExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, item.DesiredSize);
        if (childExtendsInOrientationDirection > availableSize + DELTA_DOUBLE)
          break;
        float childExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(Orientation, item.DesiredSize);
        availableSize -= childExtendsInOrientationDirection;
        sumExtendsInOrientationDirection += childExtendsInOrientationDirection;
        if (childExtendsInNonOrientationDirection > maxExtendsInNonOrientationDirection)
          maxExtendsInNonOrientationDirection = childExtendsInNonOrientationDirection;
        result.Insert(0, item);
        start--;
      }
      resultSize = Orientation == Orientation.Vertical ? new SizeF(maxExtendsInNonOrientationDirection, sumExtendsInOrientationDirection) :
          new SizeF(sumExtendsInOrientationDirection, maxExtendsInNonOrientationDirection);
      return result;
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      FrameworkElementCollection children = Children;
      lock (children.SyncRoot)
      {
        if (_newItemProvider != null)
        {
          if (children.Count > 0)
            children.Clear(false);
          if (_itemProvider != null)
            MPF.TryCleanupAndDispose(_itemProvider);
          _itemProvider = _newItemProvider;
          _newItemProvider = null;
          _updateRenderOrder = true;
        }
        _averageItemSize = 0;
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.CalculateInnerDesiredSize(totalSize);
        int numItems = itemProvider.NumItems;
        if (numItems == 0)
          return SizeF.Empty;

        SizeF resultSize;
        // Get all viewable children (= visible children inside our range)
        IList<FrameworkElement> exemplaryChildren = GetMeasuredViewableChildren(totalSize, out resultSize);
        if (exemplaryChildren.Count == 0)
        { // Might be the case if no item matches into totalSize. Fallback: Use the first visible item.
          for (int i = 0; i < numItems; i++)
          {
            FrameworkElement item = GetItem(i, itemProvider, true);
            if (item == null || !item.IsVisible)
              continue;
            exemplaryChildren.Add(item);
          }
        }
        if (exemplaryChildren.Count == 0)
          return SizeF.Empty;
        _averageItemSize = GetExtendsInOrientationDirection(Orientation, resultSize) / exemplaryChildren.Count;
        return Orientation == Orientation.Vertical ? new SizeF(resultSize.Width, resultSize.Height * numItems / exemplaryChildren.Count) :
            new SizeF(resultSize.Width * numItems / exemplaryChildren.Count, resultSize.Height);
      }
    }

    protected FrameworkElement GetItem(int childIndex, IItemProvider itemProvider, bool forceMeasure)
    {
      lock (Children.SyncRoot)
      {
        bool newlyCreated;
        FrameworkElement item = itemProvider.GetOrCreateItem(childIndex, this, out newlyCreated);
        if (item == null)
          return null;
        if (newlyCreated)
          // VisualParent and item.Screen were set by the item provider
          item.SetElementState(_elementState == ElementState.Running ? ElementState.Running : ElementState.Preparing);
        if (newlyCreated || forceMeasure)
        {
          SizeF childSize = Orientation == Orientation.Vertical ? new SizeF((float) ActualWidth, float.NaN) :
              new SizeF(float.NaN, (float) ActualHeight);
          item.Measure(ref childSize);
        }
        return item;
      }
    }

    protected override void ArrangeChildren()
    {
      bool fireScrolled = false;
      lock (Children.SyncRoot)
      {
        _arrangedItemsStartIndex = -1;
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
        {
          base.ArrangeChildren();
          return;
        }

        _totalHeight = 0;
        _totalWidth = 0;
        int numItems = itemProvider.NumItems;
        if (numItems > 0)
        {
          PointF actualPosition = ActualPosition;
          SizeF actualSize = new SizeF((float) ActualWidth, (float) ActualHeight);

          // For Orientation == vertical, this is ActualHeight, for horizontal it is ActualWidth
          float actualExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, actualSize);
          // For Orientation == vertical, this is ActualWidth, for horizontal it is ActualHeight
          float actualExtendsInNonOrientationDirection = GetExtendsInNonOrientationDirection(Orientation, actualSize);
          // If set to true, we'll check available space from the last to first visible child.
          // That is necessary if we want to scroll a specific child to the last visible position.
          bool invertLayouting = false;
          if (_pendingScrollIndex.HasValue)
          {
            fireScrolled = true;
            int pendingSI = _pendingScrollIndex.Value;
            CalcHelper.Bound(ref pendingSI, 0, numItems - 1);
            if (_scrollToFirst)
              _actualFirstVisibleChildIndex = pendingSI;
            else
            {
              _actualLastVisibleChildIndex = pendingSI;
              invertLayouting = true;
            }
            _pendingScrollIndex = null;
          }

          // 1) Calculate scroll indices
          if (_doScroll)
          {
            float spaceLeft = actualExtendsInOrientationDirection;
            if (invertLayouting)
            {
              CalcHelper.Bound(ref _actualLastVisibleChildIndex, 0, numItems - 1);
              _actualFirstVisibleChildIndex = _actualLastVisibleChildIndex + 1;
              int ct = MAX_NUM_VISIBLE_ITEMS;
              while (_actualFirstVisibleChildIndex > 0)
              {
                FrameworkElement item = GetItem(_actualFirstVisibleChildIndex - 1, itemProvider, true);
                if (item == null || !item.IsVisible)
                  continue;
                if (ct-- == 0)
                  break;
                spaceLeft -= GetExtendsInOrientationDirection(Orientation, item.DesiredSize);
                if (spaceLeft + DELTA_DOUBLE < 0)
                  break; // Found item which is not visible any more
                _actualFirstVisibleChildIndex--;
              }
              if (_actualFirstVisibleChildIndex > _actualLastVisibleChildIndex)
                // Happens if the item at _actualFirstVisibleChildIndex is bigger than the available space
                _actualFirstVisibleChildIndex = _actualLastVisibleChildIndex;
              if (spaceLeft > 0)
              { // Correct the last scroll index to fill the available space
                while (_actualLastVisibleChildIndex < numItems - 1)
                {
                  FrameworkElement item = GetItem(_actualLastVisibleChildIndex + 1, itemProvider, true);
                  if (item == null || !item.IsVisible)
                    continue;
                  if (ct-- == 0)
                    break;
                  spaceLeft -= GetExtendsInOrientationDirection(Orientation, item.DesiredSize);
                  if (spaceLeft + DELTA_DOUBLE < 0)
                    break; // Found item which is not visible any more
                  _actualLastVisibleChildIndex++;
                }
              }
            }
            else
            {
              CalcHelper.Bound(ref _actualFirstVisibleChildIndex, 0, numItems - 1);
              _actualLastVisibleChildIndex = _actualFirstVisibleChildIndex - 1;
              int ct = MAX_NUM_VISIBLE_ITEMS;
              while (_actualLastVisibleChildIndex < numItems - 1)
              {
                FrameworkElement item = GetItem(_actualLastVisibleChildIndex + 1, itemProvider, true);
                if (item == null || !item.IsVisible)
                  continue;
                if (ct-- == 0)
                  break;
                spaceLeft -= GetExtendsInOrientationDirection(Orientation, item.DesiredSize);
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
                  FrameworkElement item = GetItem(_actualFirstVisibleChildIndex - 1, itemProvider, true);
                  if (item == null || !item.IsVisible)
                    continue;
                  if (ct-- == 0)
                    break;
                  spaceLeft -= GetExtendsInOrientationDirection(Orientation, item.DesiredSize);
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
            _actualLastVisibleChildIndex = numItems - 1;
          }

          // 2) Arrange children
          if (Orientation == Orientation.Vertical)
            _totalWidth = actualExtendsInNonOrientationDirection;
          else
            _totalHeight = actualExtendsInNonOrientationDirection;
          _arrangedItems.Clear();

          _arrangedItemsStartIndex = _actualFirstVisibleChildIndex;
          // Heavy scrolling works best with at least two times the number of visible items arranged above and below
          // our visible children. That was tested out. If someone has a better heuristic, please use it here.
          int numArrangeAroundViewport = ((int) (actualExtendsInOrientationDirection / _averageItemSize) + 1) * NUM_ADD_MORE_FOCUS_ELEMENTS;
          // Elements before _actualFirstVisibleChildIndex
          float startOffset = 0;
          for (int i = _actualFirstVisibleChildIndex - 1; i >= 0 && i >= _actualFirstVisibleChildIndex - numArrangeAroundViewport; i--)
          {
            FrameworkElement item = GetItem(i, itemProvider, true);
            if (item == null || !item.IsVisible)
              continue;
            SizeF childSize = new SizeF(item.DesiredSize);
            // For Orientation == vertical, this is childSize.Height, for horizontal it is childSize.Width
            float desiredExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, childSize);
            startOffset -= desiredExtendsInOrientationDirection;
            if (Orientation == Orientation.Vertical)
            {
              PointF position = new PointF(actualPosition.X, actualPosition.Y + startOffset);

              childSize.Width = actualExtendsInNonOrientationDirection;

              ArrangeChildHorizontal(item, item.HorizontalAlignment, ref position, ref childSize);
              item.Arrange(new RectangleF(position, childSize));
              _totalHeight += desiredExtendsInOrientationDirection;
            }
            else
            {
              PointF position = new PointF(actualPosition.X + startOffset, actualPosition.Y);

              childSize.Height = actualExtendsInNonOrientationDirection;

              ArrangeChildVertical(item, item.VerticalAlignment, ref position, ref childSize);
              item.Arrange(new RectangleF(position, childSize));
              _totalWidth += desiredExtendsInOrientationDirection;
            }
            _arrangedItems.Insert(0, item);
            _arrangedItemsStartIndex = i;
          }

          startOffset = 0;
          // Elements from _actualFirstVisibleChildIndex to _actualLastVisibleChildIndex + _numArrangeAroundViewport
          for (int i = _actualFirstVisibleChildIndex; i < numItems && i <= _actualLastVisibleChildIndex + numArrangeAroundViewport; i++)
          {
            FrameworkElement item = GetItem(i, itemProvider, true);
            if (item == null || !item.IsVisible)
              continue;
            SizeF childSize = new SizeF(item.DesiredSize);
            // For Orientation == vertical, this is childSize.Height, for horizontal it is childSize.Width
            float desiredExtendsInOrientationDirection = GetExtendsInOrientationDirection(Orientation, childSize);
            if (Orientation == Orientation.Vertical)
            {
              PointF position = new PointF(actualPosition.X, actualPosition.Y + startOffset);

              childSize.Width = actualExtendsInNonOrientationDirection;

              ArrangeChildHorizontal(item, item.HorizontalAlignment, ref position, ref childSize);
              item.Arrange(new RectangleF(position, childSize));
              _totalHeight += desiredExtendsInOrientationDirection;

              startOffset += desiredExtendsInOrientationDirection;
            }
            else
            {
              PointF position = new PointF(actualPosition.X + startOffset, actualPosition.Y);

              childSize.Height = actualExtendsInNonOrientationDirection;

              ArrangeChildVertical(item, item.VerticalAlignment, ref position, ref childSize);
              item.Arrange(new RectangleF(position, childSize));
              _totalWidth += desiredExtendsInOrientationDirection;

              startOffset += desiredExtendsInOrientationDirection;
            }
            _arrangedItems.Add(item);
          }
          int numInvisible = numItems - _arrangedItems.Count; // Items which have not been arranged above, i.e. item extends have not been added to _totalHeight / _totalWidth
          float invisibleRequiredSize = numInvisible * _averageItemSize;
          if (_doScroll)
            invisibleRequiredSize += actualExtendsInOrientationDirection % _averageItemSize; // Size gap from the last item to the end of the actual extends
          if (Orientation == Orientation.Vertical)
            _totalHeight += invisibleRequiredSize;
          else
            _totalWidth += invisibleRequiredSize;

          itemProvider.Keep(_arrangedItemsStartIndex - INVISIBLE_KEEP_THRESHOLD,
              _arrangedItemsStartIndex + _arrangedItems.Count + INVISIBLE_KEEP_THRESHOLD);
        }
        else
        {
          _arrangedItemsStartIndex = 0;
          _actualFirstVisibleChildIndex = 0;
          _actualLastVisibleChildIndex = -1;
        }
      }
      if (fireScrolled)
        InvokeScrolled();
    }

    protected override void BringIntoView(UIElement element, ref RectangleF elementBounds)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
      {
        base.BringIntoView(element, elementBounds);
        return;
      }

      if (_doScroll)
      {
        IList<FrameworkElement> arrangedItemsCopy;
        int arrangedStart;
        int oldFirstViewableChild;
        int oldLastViewableChild;
        lock (Children.SyncRoot)
        {
          arrangedItemsCopy = new List<FrameworkElement>(_arrangedItems);
          arrangedStart = _arrangedItemsStartIndex;
          oldFirstViewableChild = _actualFirstVisibleChildIndex - arrangedStart;
          oldLastViewableChild = _actualLastVisibleChildIndex - arrangedStart;
        }
        if (arrangedStart < 0)
          return;
        int index = 0;
        foreach (FrameworkElement currentChild in arrangedItemsCopy)
        {
          if (InVisualPath(currentChild, element))
          {
            bool first;
            if (index < oldFirstViewableChild)
              first = true;
            else if (index <= oldLastViewableChild)
              // Already visible
              break;
            else
              first = false;
            SetScrollIndex(index + arrangedStart, first);
            // Adjust the scrolled element's bounds; Calculate the difference between positions of childen at old/new child indices
            float extendsInOrientationDirection = (float) SumActualExtendsInOrientationDirection(arrangedItemsCopy, Orientation,
                first ? oldFirstViewableChild : oldLastViewableChild, index);
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

    public override FrameworkElement GetElement(int index)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.GetElement(index);

      lock (Children.SyncRoot)
        return GetItem(index, itemProvider, true);
    }

    public override void AddChildren(ICollection<UIElement> childrenOut)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
      {
        base.AddChildren(childrenOut);
        return;
      }

      lock (Children.SyncRoot)
        CollectionUtils.AddAll(childrenOut, _arrangedItems);
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

    #region Rendering

    protected override IEnumerable<FrameworkElement> GetRenderedChildren()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.GetRenderedChildren();

      return _arrangedItems.Skip(_actualFirstVisibleChildIndex - _arrangedItemsStartIndex).
          Take(_actualLastVisibleChildIndex - _actualFirstVisibleChildIndex + 1);
    }

    #endregion

    #region Base overrides

    public override void AlignedPanelAddPotentialFocusNeighbors(RectangleF? startingRect, ICollection<FrameworkElement> outElements,
        bool elementsBeforeAndAfter)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
      {
        base.AlignedPanelAddPotentialFocusNeighbors(startingRect, outElements, elementsBeforeAndAfter);
        return;
      }
      if (!IsVisible)
        return;
      if (Focusable)
        outElements.Add(this);
      int first;
      int last;
      IList<FrameworkElement> arrangedItemsCopy;
      lock (Children.SyncRoot)
      {
        arrangedItemsCopy = new List<FrameworkElement>(_arrangedItems);
        first = _actualFirstVisibleChildIndex - _arrangedItemsStartIndex;
        last = _actualLastVisibleChildIndex - _arrangedItemsStartIndex;
      }
      int numElementsBeforeAndAfter = elementsBeforeAndAfter ? NUM_ADD_MORE_FOCUS_ELEMENTS : 0;
      AddFocusedElementRange(arrangedItemsCopy, startingRect, first, last,
          numElementsBeforeAndAfter, numElementsBeforeAndAfter, outElements);
    }

    protected override void SaveChildrenState(IDictionary<string, object> state, string prefix)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        base.SaveChildrenState(state, prefix);
      else
      {
        IList<FrameworkElement> arrangedItemsCopy;
        int index;
        lock (Children.SyncRoot)
        {
          arrangedItemsCopy = new List<FrameworkElement>(_arrangedItems);
          index = _arrangedItemsStartIndex;
        }
        state[prefix + "/ItemsStartIndex"] = index;
        state[prefix + "/NumItems"] = arrangedItemsCopy.Count;
        foreach (FrameworkElement child in arrangedItemsCopy)
          child.SaveUIState(state, prefix + "/Child_" + (index++));
      }
    }

    public override void RestoreChildrenState(IDictionary<string, object> state, string prefix)
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        base.RestoreChildrenState(state, prefix);
      else
      {
        object oNumItems;
        object oIndex;
        int? numItems;
        int? startIndex;
        if (state.TryGetValue(prefix + "/ItemsStartIndex", out oIndex) && state.TryGetValue(prefix + "/NumItems", out oNumItems) &&
            (startIndex = (int?) oIndex).HasValue && (numItems = (int?) oNumItems).HasValue)
        {
          int endIndexExcl = Math.Max(startIndex.Value + numItems.Value, itemProvider.NumItems);
          for (int i = startIndex.Value; i < endIndexExcl; i++)
          {
            FrameworkElement child = GetItem(i, itemProvider, false);
            if (child == null)
              continue;
            child.RestoreUIState(state, prefix + "/Child_" + i);
          }
        }
      }
    }

    public override bool FocusPageUp()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.FocusPageUp();

      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        int firstLocal;
        int firstVisibleChildIndex;
        int numItems;
        IList<FrameworkElement> localChildren;
        lock (Children.SyncRoot)
        {
          firstLocal = _actualFirstVisibleChildIndex - _arrangedItemsStartIndex;
          firstVisibleChildIndex = _actualFirstVisibleChildIndex;
          numItems = itemProvider.NumItems;
          localChildren = new List<FrameworkElement>(_arrangedItems);
          if (localChildren.Count == 0)
            return false;
          CalcHelper.Bound(ref firstLocal, 0, localChildren.Count - 1);
        }
        FrameworkElement firstVisibleChild = localChildren[firstLocal];
        if (_averageItemSize == 0)
          return false;
        if (InVisualPath(firstVisibleChild, currentElement))
        { // The topmost element is focused - move one page up
          int index = (int) (ActualHeight/_averageItemSize) - 1;
          CalcHelper.LowerBound(ref index, 1);
          index = firstVisibleChildIndex - index;
          CalcHelper.Bound(ref index, 0, numItems - 1);
          SetScrollIndex(index, true);
          FrameworkElement item = GetItem(index, itemProvider, false);
          if (item != null)
            item.SetFocusPrio = SetFocusPriority.Default;
          return true;
        }
        // An element inside our visible range is focused - move to first element
        float limitPosition = ActualPosition.Y;
        int firstPlusOne = firstLocal + 1;
        CalcHelper.Bound(ref firstPlusOne, 0, localChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(localChildren.Take(firstPlusOne), currentElement.ActualBounds, MoveFocusDirection.Up)) != null &&
            (nextElement.ActualPosition.Y > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public override bool FocusPageDown()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.FocusPageDown();

      if (Orientation == Orientation.Vertical)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        int lastLocal;
        int lastVisibleChildIndex;
        int numItems;
        IList<FrameworkElement> localChildren;
        lock (Children.SyncRoot)
        {
          lastLocal = _actualLastVisibleChildIndex - _arrangedItemsStartIndex;
          lastVisibleChildIndex = _actualLastVisibleChildIndex;
          numItems = itemProvider.NumItems;
          localChildren = new List<FrameworkElement>(_arrangedItems);
          if (localChildren.Count == 0)
            return false;
          CalcHelper.Bound(ref lastLocal, 0, localChildren.Count - 1);
        }
        FrameworkElement lastVisibleChild = localChildren[lastLocal];
        if (_averageItemSize == 0)
          return false;
        if (InVisualPath(lastVisibleChild, currentElement))
        { // The element at the bottom is focused - move one page down
          int index = (int) (ActualHeight/_averageItemSize) - 1;
          CalcHelper.LowerBound(ref index, 1);
          index = lastVisibleChildIndex + index;
          CalcHelper.Bound(ref index, 0, numItems - 1);
          SetScrollIndex(index, false);
          FrameworkElement item = GetItem(index, itemProvider, false);
          if (item != null)
            item.SetFocusPrio = SetFocusPriority.Default;
          return true;
        }
        // An element inside our visible range is focused - move to last element
        float limitPosition = ActualPosition.Y + (float) ActualHeight;
        int lastMinusOne = lastLocal - 1;
        CalcHelper.Bound(ref lastMinusOne, 0, localChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(localChildren.Skip(lastMinusOne), currentElement.ActualBounds, MoveFocusDirection.Down)) != null &&
            (nextElement.ActualBounds.Bottom < limitPosition + DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public override bool FocusPageLeft()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.FocusPageLeft();

      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        int firstLocal;
        int firstVisibleChildIndex;
        int numItems;
        IList<FrameworkElement> localChildren;
        lock (Children.SyncRoot)
        {
          firstLocal = _actualFirstVisibleChildIndex - _arrangedItemsStartIndex;
          firstVisibleChildIndex = _actualFirstVisibleChildIndex;
          numItems = itemProvider.NumItems;
          localChildren = new List<FrameworkElement>(_arrangedItems);
          if (localChildren.Count == 0)
            return false;
          CalcHelper.Bound(ref firstLocal, 0, localChildren.Count - 1);
        }
        FrameworkElement firstVisibleChild = localChildren[firstLocal];
        if (_averageItemSize == 0)
          return false;
        if (InVisualPath(firstVisibleChild, currentElement))
        { // The leftmost element is focused - move one page left
          int index = (int) (ActualWidth/_averageItemSize) - 1;
          CalcHelper.LowerBound(ref index, 1);
          index = firstVisibleChildIndex - index;
          CalcHelper.Bound(ref index, 0, numItems - 1);
          SetScrollIndex(index, true);
          FrameworkElement item = GetItem(index, itemProvider, false);
          if (item != null)
            item.SetFocusPrio = SetFocusPriority.Default;
          return true;
        }
        // An element inside our visible range is focused - move to first element
        float limitPosition = ActualPosition.X;
        int firstPlusOne = firstLocal + 1;
        CalcHelper.Bound(ref firstPlusOne, 0, localChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(localChildren.Take(firstPlusOne), currentElement.ActualBounds, MoveFocusDirection.Left)) != null &&
            (nextElement.ActualPosition.X > limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public override bool FocusPageRight()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.FocusPageRight();

      if (Orientation == Orientation.Horizontal)
      {
        FrameworkElement currentElement = GetFocusedElementOrChild();
        if (currentElement == null)
          return false;

        int lastLocal;
        int lastVisibleChildIndex;
        int numItems;
        IList<FrameworkElement> localChildren;
        lock (Children.SyncRoot)
        {
          lastLocal = _actualLastVisibleChildIndex - _arrangedItemsStartIndex;
          lastVisibleChildIndex = _actualLastVisibleChildIndex;
          numItems = itemProvider.NumItems;
          localChildren = new List<FrameworkElement>(_arrangedItems);
          if (localChildren.Count == 0)
            return false;
          CalcHelper.Bound(ref lastLocal, 0, localChildren.Count - 1);
        }
        FrameworkElement lastVisibleChild = localChildren[lastLocal];
        if (_averageItemSize == 0)
          return false;
        if (InVisualPath(lastVisibleChild, currentElement))
        { // The element at the bottom is focused - move one page down
          int index = (int) (ActualWidth/_averageItemSize) - 1;
          CalcHelper.LowerBound(ref index, 1);
          index = lastVisibleChildIndex + index;
          CalcHelper.Bound(ref index, 0, numItems - 1);
          SetScrollIndex(index, false);
          FrameworkElement item = GetItem(index, itemProvider, false);
          if (item != null)
            item.SetFocusPrio = SetFocusPriority.Default;
          return true;
        }
        // An element inside our visible range is focused - move to last element
        float limitPosition = ActualPosition.X + (float) ActualWidth;
        int lastMinusOne = lastLocal - 1;
        CalcHelper.Bound(ref lastMinusOne, 0, localChildren.Count - 1);
        FrameworkElement nextElement;
        while ((nextElement = FindNextFocusElement(localChildren.Skip(lastMinusOne), currentElement.ActualBounds, MoveFocusDirection.Right)) != null &&
            (nextElement.ActualBounds.Right < limitPosition - DELTA_DOUBLE))
          currentElement = nextElement;
        return currentElement.TrySetFocus(true);
      }
      return false;
    }

    public override bool FocusHome()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.FocusHome();

      lock (Children.SyncRoot)
      {
        if (itemProvider.NumItems == 0)
          return false;
        FrameworkElement item = GetItem(0, itemProvider, true);
        if (item != null)
          item.SetFocusPrio = SetFocusPriority.Default;
      }
      SetScrollIndex(0, true);
      return true;
    }

    public override bool FocusEnd()
    {
      IItemProvider itemProvider = ItemProvider;
      if (itemProvider == null)
        return base.FocusHome();

      int numItems;
      lock (Children.SyncRoot)
      {
        numItems = itemProvider.NumItems;
        if (numItems == 0)
          return false;
        FrameworkElement item = GetItem(numItems - 1, itemProvider, true);
        if (item != null)
          item.SetFocusPrio = SetFocusPriority.Default;
      }
      SetScrollIndex(numItems - 1, false);
      return true;
    }

    public override float ViewPortStartX
    {
      get
      {
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.ViewPortStartX;

        if (Orientation == Orientation.Vertical)
          return 0;
        int firstVisibleChildIndex = _actualFirstVisibleChildIndex;
        return firstVisibleChildIndex == 0 ? 0 : (firstVisibleChildIndex - 1) * _averageItemSize;
      }
    }

    public override float ViewPortStartY
    {
      get
      {
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.ViewPortStartY;

        if (Orientation == Orientation.Horizontal)
          return 0;
        int firstVisibleChildIndex = _actualFirstVisibleChildIndex;
        return firstVisibleChildIndex * _averageItemSize;
      }
    }

    public override bool IsViewPortAtTop
    {
      get
      {
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.IsViewPortAtTop;

        if (Orientation == Orientation.Horizontal)
          return true;
        return _actualFirstVisibleChildIndex == 0;
      }
    }

    public override bool IsViewPortAtBottom
    {
      get
      {
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.IsViewPortAtBottom;

        if (Orientation == Orientation.Horizontal)
          return true;
        return _actualLastVisibleChildIndex == itemProvider.NumItems - 1;
      }
    }

    public override bool IsViewPortAtLeft
    {
      get
      {
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.IsViewPortAtLeft;

        if (Orientation == Orientation.Vertical)
          return true;
        return _actualFirstVisibleChildIndex == 0;
      }
    }

    public override bool IsViewPortAtRight
    {
      get
      {
        IItemProvider itemProvider = ItemProvider;
        if (itemProvider == null)
          return base.IsViewPortAtRight;

        if (Orientation == Orientation.Vertical)
          return true;
        return _actualLastVisibleChildIndex == itemProvider.NumItems - 1;
      }
    }

    #endregion
  }
}

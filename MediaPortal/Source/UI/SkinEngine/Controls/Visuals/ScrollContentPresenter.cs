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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Brushes;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum ScrollAutoCenteringEnum
  {
    None,
    Horizontal,
    Vertical,
    HorizontalAndVertical
  }

  public class ScrollContentPresenter : ContentPresenter, IScrollInfo, IScrollViewerFocusSupport
  {
    #region Consts

    public const int NUM_SCROLL_PIXEL = 50;

    #endregion

    #region Protected fields

    protected bool _doScroll = false;
    protected float _scrollOffsetX = 0;
    protected float _scrollOffsetY = 0;
    protected float _actualScrollOffsetX = 0;
    protected float _actualScrollOffsetY = 0;
    protected bool _forcedOpacityMask = false;
    protected AbstractProperty _autoCenteringProperty;
    protected AbstractProperty _horizontalFitToSpaceProperty;
    protected AbstractProperty _verticalFitToSpaceProperty;

    #endregion

    #region Ctor

    public ScrollContentPresenter()
    {
      Init();
      Attach();
    }

    protected void Init()
    {
      _autoCenteringProperty = new SProperty(typeof(ScrollAutoCenteringEnum), ScrollAutoCenteringEnum.None);
      _horizontalFitToSpaceProperty = new SProperty(typeof(bool), false);
      _verticalFitToSpaceProperty = new SProperty(typeof(bool), false);
    }

    protected void Attach()
    {
      _autoCenteringProperty.Attach(OnArrangeGetsInvalid);
      _horizontalFitToSpaceProperty.Attach(OnScrollDisabledChanged);
      _verticalFitToSpaceProperty.Attach(OnScrollDisabledChanged);
    }

    protected void Detach()
    {
      _autoCenteringProperty.Detach(OnArrangeGetsInvalid);
      _horizontalFitToSpaceProperty.Detach(OnScrollDisabledChanged);
      _verticalFitToSpaceProperty.Detach(OnScrollDisabledChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ScrollContentPresenter scp = (ScrollContentPresenter) source;
      _doScroll = scp._doScroll;
      _scrollOffsetX = 0;
      _scrollOffsetY = 0;
      AutoCentering = scp.AutoCentering;
      HorizontalFitToSpace = scp.HorizontalFitToSpace;
      VerticalFitToSpace = scp.VerticalFitToSpace;
      Attach();
    }

    #endregion

    void OnScrollDisabledChanged(AbstractProperty property, object oldValue)
    {
      InvalidateLayout(true, false);
    }

    void InvokeScrolled()
    {
      ScrolledDlgt dlgt = Scrolled;
      if (dlgt != null) dlgt(this);
    }

    public void SetScrollOffset(float scrollOffsetX, float scrollOffsetY)
    {
      if (_scrollOffsetX == scrollOffsetX && _scrollOffsetY == scrollOffsetY)
        return;

      if (!IsHorzCentering)
      {
        if (scrollOffsetX < ActualWidth - TotalWidth)
          scrollOffsetX = (float) ActualWidth - TotalWidth;
        if (scrollOffsetX > 0)
          scrollOffsetX = 0;
      }
      if (!IsVertCentering)
      {
        if (scrollOffsetY < ActualHeight - TotalHeight)
          scrollOffsetY = (float) ActualHeight - TotalHeight;
        if (scrollOffsetY > 0)
          scrollOffsetY = 0;
      }

      _scrollOffsetX = scrollOffsetX;
      _scrollOffsetY = scrollOffsetY;
      InvalidateLayout(false, true);
      InvokeScrolled();
    }

    public override void BringIntoView(UIElement element, RectangleF elementBounds)
    {
      if (_doScroll || AutoCentering != ScrollAutoCenteringEnum.None)
      {
        float differenceX = 0;
        float differenceY = 0;

        if (IsHorzCentering)
          differenceX = CalculateCenteredScrollPos(elementBounds.X, elementBounds.Width, ActualPosition.X, ActualWidth);
        else if (_doScroll)
          differenceX = CalculateVisibleScrollDifference(elementBounds.X, elementBounds.Width, ActualPosition.X, ActualWidth); 

        if (IsVertCentering)
          differenceY = CalculateCenteredScrollPos(elementBounds.Y, elementBounds.Height, ActualPosition.Y, ActualHeight) - _actualScrollOffsetY;
        else if (_doScroll)
          differenceY = CalculateVisibleScrollDifference(elementBounds.Y, elementBounds.Height, ActualPosition.Y, ActualHeight);

        // Change rect as if children were already re-arranged
        elementBounds.X += differenceX;
        elementBounds.Y += differenceY;
        SetScrollOffset(_actualScrollOffsetX + differenceX, _actualScrollOffsetY + differenceY);
      }
      base.BringIntoView(element, elementBounds);
    }

    protected float CalculateVisibleScrollDifference(double elementPos, double elementSize, double actualPos, double actualSize)
    {
      double difference = 0.0f;
      if (elementPos + elementSize > actualPos + actualSize)
        difference = - (elementPos + elementSize - actualPos - actualSize);
      if (elementPos + difference < actualPos)
        difference = actualPos - elementPos;
      return (float) difference;
    }

    protected float CalculateCenteredScrollPos(double elementPos, double elementSize, double actualPos, double actualSize)
    {
      return (float) ((actualSize - elementSize) / 2.0 - (elementPos - actualPos));
    }

    protected bool IsHorzCentering
    {
      get { return AutoCentering != ScrollAutoCenteringEnum.None && AutoCentering != ScrollAutoCenteringEnum.Vertical; }
    }

    protected bool IsVertCentering
    {
      get { return AutoCentering != ScrollAutoCenteringEnum.None && AutoCentering != ScrollAutoCenteringEnum.Horizontal; }
    }

    protected override void ArrangeTemplateControl()
    {
      if (_templateControl == null)
      {
        _scrollOffsetX = 0;
        _scrollOffsetY = 0;
      }
      else
      {
        SizeF desiredSize = _templateControl.DesiredSize;
        PointF position;
        SizeF availableSize;
        if (_doScroll || AutoCentering != ScrollAutoCenteringEnum.None)
        {
          availableSize = _innerRect.Size;
          if (desiredSize.Width > _innerRect.Width)
          {
            if (!IsHorzCentering)
              _scrollOffsetX = Math.Max(_scrollOffsetX, _innerRect.Width - desiredSize.Width);
            availableSize.Width = desiredSize.Width;
          }
          else if (!IsHorzCentering)
            _scrollOffsetX = 0;

          if (desiredSize.Height > _innerRect.Height)
          {
            if (!IsVertCentering)
              _scrollOffsetY = Math.Max(_scrollOffsetY, _innerRect.Height - desiredSize.Height);
            availableSize.Height = desiredSize.Height;
          }
          else if (!IsVertCentering)
            _scrollOffsetY = 0;
          position = new PointF(_innerRect.X + _scrollOffsetX, _innerRect.Y + _scrollOffsetY);
        }
        else
        {
          _scrollOffsetX = 0;
          _scrollOffsetY = 0;
          position = new PointF(_innerRect.X, _innerRect.Y);
          availableSize = _innerRect.Size;
        }

        if (HorizontalFitToSpace)
          availableSize.Width = _innerRect.Size.Width;
        if (VerticalFitToSpace)
          availableSize.Height = _innerRect.Size.Height;

        ArrangeChild(_templateControl, _templateControl.HorizontalAlignment, _templateControl.VerticalAlignment,
            ref position, ref availableSize);
        RectangleF childRect = new RectangleF(position, availableSize);
        _templateControl.Arrange(childRect);
      }
      _actualScrollOffsetX = _scrollOffsetX;
      _actualScrollOffsetY = _scrollOffsetY;
    }

    public override bool IsChildRenderedAt(UIElement child, float x, float y)
    {
      // The ScrollContentPresenter clips all rendering outside its range, so first check if x and y are in its area
      return IsInArea(x, y) && base.IsChildRenderedAt(child, x, y);
    }

    public override void Render(RenderContext parentRenderContext)
    {
      if (OpacityMask == null && (TotalHeight > ActualHeight || TotalWidth > ActualWidth))
      {
        SolidColorBrush brush = new SolidColorBrush {Color = Color.Black};
        OpacityMask = brush;
        _forcedOpacityMask = true;
      }
      else if (_forcedOpacityMask && TotalHeight <= ActualHeight && TotalWidth <= ActualWidth && OpacityMask != null)
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
      // After rendering our children (in ContentPresenter.RenderOverride) the following line resets the RenderContext's bounds so
      // that rendering with an OpacityMask will clip the final output correctly to our scrolled viewport.
      localRenderContext.SetUntransformedBounds(ActualBounds);
    }

    public override void SaveUIState(IDictionary<string, object> state, string prefix)
    {
      base.SaveUIState(state, prefix);
      state[prefix + "/ScrollOffsetX"] = _scrollOffsetX;
      state[prefix + "/ScrollOffsetY"] = _scrollOffsetX;
  }

    public override void RestoreUIState(IDictionary<string, object> state, string prefix)
    {
      base.RestoreUIState(state, prefix);
      float? scrollOffsetX;
      float? scrollOffsetY;
      object so;
      if (state.TryGetValue(prefix + "/ScrollOffsetX", out so) && (scrollOffsetX = so as float?).HasValue &&
          state.TryGetValue(prefix + "/ScrollOffsetY", out so) && (scrollOffsetY = so as float?).HasValue)
        SetScrollOffset(scrollOffsetX.Value, scrollOffsetY.Value);
    }

    #region Public properties

    /// <summary>
    /// Gets or sets a value that determines whether focused elements are automatically scrolled to the center 
    /// of the viewport, and in which dimensions.
    /// </summary>
    public ScrollAutoCenteringEnum AutoCentering
    {
      get { return (ScrollAutoCenteringEnum) _autoCenteringProperty.GetValue(); }
      set { _autoCenteringProperty.SetValue(value); }
    }

    public AbstractProperty AutoCenteringProperty
    {
      get { return _autoCenteringProperty; }
    }

    public AbstractProperty HorizontalFitToSpaceProperty
    {
      get { return _horizontalFitToSpaceProperty; }
    }

    /// <summary>
    /// Makes the scroll content presenter's contents fit horizontally into its own space.
    /// If used inside a <see cref="ScrollViewer"/>, this property is configured by the scroll viewer according to its
    /// <see cref="ScrollViewer.HorizontalScrollBarVisibility"/> setting.
    /// </summary>
    public bool HorizontalFitToSpace
    {
      get { return (bool) _horizontalFitToSpaceProperty.GetValue(); }
      set { _horizontalFitToSpaceProperty.SetValue(value); }
    }

    public AbstractProperty VerticalFitToSpaceProperty
    {
      get { return _verticalFitToSpaceProperty; }
    }

    /// <summary>
    /// Makes the scroll content presenter's contents fit vertically into its own space.
    /// If used inside a <see cref="ScrollViewer"/>, this property is configured by the scroll viewer according to its
    /// <see cref="ScrollViewer.VerticalScrollBarVisibility"/> setting.
    /// </summary>
    public bool VerticalFitToSpace
    {
      get { return (bool) _verticalFitToSpaceProperty.GetValue(); }
      set { _verticalFitToSpaceProperty.SetValue(value); }
    }

    #endregion

    #region IScrollViewerFocusSupport implementation

    public bool FocusUp()
    {
      if (!MoveFocus1(MoveFocusDirection.Up))
        // We couldn't move the focus - fallback: move physical scrolling offset
        if (IsViewPortAtTop)
          return false;
        else
          SetScrollOffset(_scrollOffsetX, _scrollOffsetY + NUM_SCROLL_PIXEL);
      return true;
    }

    public bool FocusDown()
    {
      if (!MoveFocus1(MoveFocusDirection.Down))
        // We couldn't move the focus - fallback: move physical scrolling offset
        if (IsViewPortAtBottom)
          return false;
        else
          SetScrollOffset(_scrollOffsetX, _scrollOffsetY - NUM_SCROLL_PIXEL);
      return true;
    }

    public bool FocusLeft()
    {
      if (!MoveFocus1(MoveFocusDirection.Left))
        // We couldn't move the focus - fallback: move physical scrolling offset
        if (IsViewPortAtLeft)
          return false;
        else
          SetScrollOffset(_scrollOffsetX + NUM_SCROLL_PIXEL, _scrollOffsetY);
      return true;
    }

    public bool FocusRight()
    {
      if (!MoveFocus1(MoveFocusDirection.Right))
        // We couldn't move the focus - fallback: move physical scrolling offset
        if (IsViewPortAtRight)
          return false;
        else
          SetScrollOffset(_scrollOffsetX - NUM_SCROLL_PIXEL, _scrollOffsetY);
      return true;
    }

    public bool FocusPageUp()
    {
      ICollection<FrameworkElement> focusableChildren = new List<FrameworkElement>();
      FrameworkElement currentElement = GetFocusedElementOrChild();
      AddPotentialFocusableElements(currentElement == null ? new RectangleF?() : currentElement.ActualBounds, focusableChildren);
      if (focusableChildren.Count == 0)
        return false;
      float limitPosition;
      if (currentElement == null)
        limitPosition = ActualPosition.Y;
      else
      {
        if (currentElement.ActualPosition.Y - DELTA_DOUBLE < ActualPosition.Y)
          // Already topmost element
          limitPosition = (float) (ActualPosition.Y - ActualHeight);
        else
          limitPosition = ActualPosition.Y;
      }
      // Try to find last element inside the limit
      while (currentElement != null &&
          (currentElement.ActualPosition.Y > limitPosition))
      {
        FrameworkElement lastElement = currentElement;
        currentElement = FindNextFocusElement(focusableChildren, currentElement.ActualBounds, MoveFocusDirection.Up);
        if (currentElement != null)
          continue;
        currentElement = lastElement;
        break;
      }
      if (currentElement != null)
        return currentElement.TrySetFocus(true);
      // No element to focus - fallback: move physical scrolling offset
      if (IsViewPortAtTop)
        return false;
      SetScrollOffset(_scrollOffsetX, _scrollOffsetY + (float) ActualHeight);
      return true;
    }

    public bool FocusPageDown()
    {
      ICollection<FrameworkElement> focusableChildren = new List<FrameworkElement>();
      FrameworkElement currentElement = GetFocusedElementOrChild();
      AddPotentialFocusableElements(currentElement == null ? new RectangleF?() : currentElement.ActualBounds, focusableChildren);
      if (focusableChildren.Count == 0)
        return false;
      float limitPosition;
      if (currentElement == null)
        limitPosition = (float) (ActualPosition.Y + ActualHeight);
      else
      {
        if (currentElement.ActualPosition.Y + currentElement.ActualHeight + DELTA_DOUBLE > ActualPosition.Y + ActualHeight)
          // Already at bottom
          limitPosition = (float) (ActualPosition.Y + 2*ActualHeight);
        else
          limitPosition = (float) (ActualPosition.Y + ActualHeight);
      }
      // Try to find last element inside the limit
      while (currentElement != null &&
          (currentElement.ActualPosition.Y + currentElement.ActualHeight < limitPosition))
      {
        FrameworkElement lastElement = currentElement;
        currentElement = FindNextFocusElement(focusableChildren, currentElement.ActualBounds, MoveFocusDirection.Down);
        if (currentElement != null)
          continue;
        currentElement = lastElement;
        break;
      }
      if (currentElement != null)
        return currentElement.TrySetFocus(true);
      // No element to focus - fallback: move physical scrolling offset
      if (IsViewPortAtBottom)
        return false;
      SetScrollOffset(_scrollOffsetX, _scrollOffsetY - (float) ActualHeight);
      return true;
    }

    public bool FocusPageLeft()
    {
      ICollection<FrameworkElement> focusableChildren = new List<FrameworkElement>();
      FrameworkElement currentElement = GetFocusedElementOrChild();
      AddPotentialFocusableElements(currentElement == null ? new RectangleF?() : currentElement.ActualBounds, focusableChildren);
      if (focusableChildren.Count == 0)
        return false;
      float limitPosition;
      if (currentElement == null)
        limitPosition = ActualPosition.X;
      else
      {
        if (currentElement.ActualPosition.X - DELTA_DOUBLE < ActualPosition.X)
          // Already at left
          limitPosition = (float) (ActualPosition.X - ActualWidth);
        else
          limitPosition = ActualPosition.X;
      }
      // Try to find last element inside the limit
      while (currentElement != null &&
          (currentElement.ActualPosition.X > limitPosition))
      {
        FrameworkElement lastElement = currentElement;
        currentElement = FindNextFocusElement(focusableChildren, currentElement.ActualBounds, MoveFocusDirection.Left);
        if (currentElement != null)
          continue;
        currentElement = lastElement;
        break;
      }
      if (currentElement != null)
        return currentElement.TrySetFocus(true);
      // No element to focus - fallback: move physical scrolling offset
      if (IsViewPortAtTop)
        return false;
      SetScrollOffset(_scrollOffsetX + (float) ActualWidth, _scrollOffsetY);
      return true;
    }

    public bool FocusPageRight()
    {
      ICollection<FrameworkElement> focusableChildren = new List<FrameworkElement>();
      FrameworkElement currentElement = GetFocusedElementOrChild();
      AddPotentialFocusableElements(currentElement == null ? new RectangleF?() : currentElement.ActualBounds, focusableChildren);
      if (focusableChildren.Count == 0)
        return false;
      float limitPosition;
      if (currentElement == null)
        limitPosition = (float) (ActualPosition.X + ActualWidth);
      else
      {
        if (currentElement.ActualPosition.X + ActualWidth + DELTA_DOUBLE > ActualPosition.X + ActualWidth)
          // Already at right
          limitPosition = (float) (ActualPosition.X + 2*ActualWidth);
        else
          limitPosition = (float) (ActualPosition.X + ActualWidth);
      }
      // Try to find last element inside the limit
      while (currentElement != null &&
          (currentElement.ActualPosition.X + currentElement.ActualWidth < limitPosition))
      {
        FrameworkElement lastElement = currentElement;
        currentElement = FindNextFocusElement(focusableChildren, currentElement.ActualBounds, MoveFocusDirection.Right);
        if (currentElement != null)
          continue;
        currentElement = lastElement;
        break;
      }
      if (currentElement != null)
        return currentElement.TrySetFocus(true);
      // No element to focus - fallback: move physical scrolling offset
      if (IsViewPortAtRight)
        return false;
      SetScrollOffset(_scrollOffsetX - (float) ActualWidth, _scrollOffsetY);
      return true;
    }

    public bool FocusHome()
    {
      SetScrollOffset(0, 0);
      return true;
    }

    public bool FocusEnd()
    {
      FrameworkElement templateControl = TemplateControl;
      if (templateControl == null)
        return false;
      SetScrollOffset(-(float) templateControl.ActualWidth, -(float) templateControl.ActualHeight);
      return true;
    }

    public bool ScrollDown(int numLines)
    {
      if (IsViewPortAtBottom)
        return false;
      SetScrollOffset(_scrollOffsetX, _scrollOffsetY - numLines * NUM_SCROLL_PIXEL);
      return true;
    }

    public bool ScrollUp(int numLines)
    {
      if (IsViewPortAtTop)
        return false;
      SetScrollOffset(_scrollOffsetX, _scrollOffsetY + numLines * NUM_SCROLL_PIXEL);
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
      get
      {
        FrameworkElement templateControl = TemplateControl;
        return templateControl == null ? 0 : (float) templateControl.ActualWidth;
      }
    }

    public float TotalHeight
    {
      get
      {
        FrameworkElement templateControl = TemplateControl;
        return templateControl == null ? 0 : (float) templateControl.ActualHeight;
      }
    }

    public float ViewPortWidth
    {
      get { return (float) ActualWidth; }
    }

    public float ViewPortStartX
    {
      get { return -_actualScrollOffsetX; }
    }

    public float ViewPortHeight
    {
      get { return (float) ActualHeight; }
    }

    public float ViewPortStartY
    {
      get { return -_actualScrollOffsetY; }
    }

    public bool IsViewPortAtTop
    {
      get { return TemplateControl == null || Math.Abs(_actualScrollOffsetY) < DELTA_DOUBLE; }
    }

    public bool IsViewPortAtBottom
    {
      get { return TemplateControl == null || -_actualScrollOffsetY + ActualHeight + DELTA_DOUBLE >= TotalHeight; }
    }

    public bool IsViewPortAtLeft
    {
      get { return TemplateControl == null || Math.Abs(_actualScrollOffsetX) < DELTA_DOUBLE; }
    }

    public bool IsViewPortAtRight
    {
      get { return TemplateControl == null || -_actualScrollOffsetX + ActualWidth + DELTA_DOUBLE >= TotalWidth; }
    }

    #endregion
  }
}

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
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.MpfElements.Input;
using MediaPortal.Utilities.DeepCopy;
using KeyEventArgs = MediaPortal.UI.SkinEngine.MpfElements.Input.KeyEventArgs;
using MouseEventArgs = MediaPortal.UI.SkinEngine.MpfElements.Input.MouseEventArgs;
using Screen = MediaPortal.UI.SkinEngine.ScreenManagement.Screen;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum ScrollBarVisibility
  {
    Disabled,
    Auto,
    Hidden,
    Visible
  }

  public class ScrollViewer : ContentControl
  {
    protected const float SCROLLBAR_MINLENGTH = 10f;

    #region Protected fields

    protected AbstractProperty _scrollBarXSizeProperty;
    protected AbstractProperty _scrollBarYSizeProperty;
    protected AbstractProperty _scrollBarXKnobPosProperty;
    protected AbstractProperty _scrollBarXKnobWidthProperty;
    protected AbstractProperty _scrollBarXVisibleProperty;
    protected AbstractProperty _scrollBarYKnobPosProperty;
    protected AbstractProperty _scrollBarYKnobHeightProperty;
    protected AbstractProperty _scrollBarYVisibleProperty;

    protected AbstractProperty _canContentScrollProperty;
    protected AbstractProperty _verticalScrollBarVisibilityProperty;
    protected AbstractProperty _horizontalScrollBarVisibilityProperty;

    protected IScrollInfo _attachedScrollInfo = null;
    protected TouchEvent _lastTouchEvent;

    #endregion

    #region Ctor

    public ScrollViewer()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _scrollBarXSizeProperty = new SProperty(typeof(float), 0f);
      _scrollBarYSizeProperty = new SProperty(typeof(float), 0f);
      _scrollBarXKnobPosProperty = new SProperty(typeof(float), 0f);
      _scrollBarXKnobWidthProperty = new SProperty(typeof(float), 30f);
      _scrollBarXVisibleProperty = new SProperty(typeof(bool), false);
      _scrollBarYKnobPosProperty = new SProperty(typeof(float), 0f);
      _scrollBarYKnobHeightProperty = new SProperty(typeof(float), 30f);
      _scrollBarYVisibleProperty = new SProperty(typeof(bool), false);

      _canContentScrollProperty = new SProperty(typeof(bool), false);
      _verticalScrollBarVisibilityProperty = new SProperty(typeof(ScrollBarVisibility), ScrollBarVisibility.Auto);
      _horizontalScrollBarVisibilityProperty = new SProperty(typeof(ScrollBarVisibility), ScrollBarVisibility.Auto);
    }

    void Attach()
    {
      _verticalScrollBarVisibilityProperty.Attach(OnScrollBarVisibilityChanged);
      _horizontalScrollBarVisibilityProperty.Attach(OnScrollBarVisibilityChanged);

      ContentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      _verticalScrollBarVisibilityProperty.Detach(OnScrollBarVisibilityChanged);
      _horizontalScrollBarVisibilityProperty.Detach(OnScrollBarVisibilityChanged);

      ContentProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ScrollViewer sv = (ScrollViewer)source;
      ScrollBarXKnobPos = sv.ScrollBarXKnobPos;
      ScrollBarXKnobWidth = sv.ScrollBarXKnobWidth;
      ScrollBarYKnobPos = sv.ScrollBarYKnobPos;
      ScrollBarYKnobHeight = sv.ScrollBarYKnobHeight;
      CanContentScroll = sv.CanContentScroll;
      VerticalScrollBarVisibility = sv.VerticalScrollBarVisibility;
      HorizontalScrollBarVisibility = sv.HorizontalScrollBarVisibility;
      Attach();
    }

    #endregion

    void OnContentChanged(AbstractProperty property, object oldValue)
    {
      ConfigureContentScrollFacility();
      UpdateScrollBars();
    }

    void OnScrollBarVisibilityChanged(AbstractProperty property, object oldValue)
    {
      ConfigureContentScrollFacility();
      UpdateScrollBars();
    }

    protected override void DoFireEvent(string eventName)
    {
      base.DoFireEvent(eventName);
      if (eventName == LOADED_EVENT)
        UpdateScrollBars();
    }

    protected override ContentPresenter FindContentPresenter()
    {
      return TemplateControl == null ? null :
          TemplateControl.FindElement_DepthFirst(new SubTypeMatcher(typeof(ScrollContentPresenter))) as ContentPresenter;
    }

    /// <summary>
    /// Returns the control which is responsible for the scrolling.
    /// </summary>
    /// <remarks>
    /// Depending on our property <see cref="CanContentScroll"/>, either our content presenter's content will
    /// do the scrolling (<c>CanContentScroll == true</c>, i.e. logical scrolling) or the content presenter itself
    /// (<c>CanContentScroll == false</c>, i.e. physical scrolling).
    /// </remarks>
    /// <returns>The control responsible for doing the scrolling.</returns>
    protected IScrollInfo FindScrollControl()
    {
      ScrollContentPresenter scp = FindContentPresenter() as ScrollContentPresenter;
      if (scp == null)
        return null;
      IScrollInfo scpContentSI = scp.TemplateControl as IScrollInfo;
      if (CanContentScroll && scpContentSI != null)
        return scpContentSI;
      return scp;
    }

    void UpdateScrollBars()
    {
      IScrollInfo scrollInfo = FindScrollControl();
      if (scrollInfo == null)
        return;

      float totalWidth = scrollInfo.TotalWidth;
      float totalWidthNN = Math.Max(1, totalWidth); // Avoid divisions by zero

      float scrollAreaWidth = ScrollBarXSize;
      float w = Math.Min(scrollAreaWidth, Math.Max(
          scrollInfo.ViewPortWidth / totalWidthNN * scrollAreaWidth, SCROLLBAR_MINLENGTH));
      float x = Math.Min(scrollAreaWidth - w,
          scrollInfo.ViewPortStartX / totalWidthNN * scrollAreaWidth);

      ScrollBarXKnobWidth = w;
      ScrollBarXKnobPos = x;
      switch (HorizontalScrollBarVisibility)
      {
        case ScrollBarVisibility.Disabled:
        case ScrollBarVisibility.Hidden:
          ScrollBarXVisible = false;
          break;
        case ScrollBarVisibility.Auto:
          ScrollBarXVisible = !IsNear(totalWidth, 0) && totalWidth > scrollInfo.ViewPortWidth + DELTA_DOUBLE;
          break;
        case ScrollBarVisibility.Visible:
          ScrollBarXVisible = true;
          break;
      }

      float totalHeight = scrollInfo.TotalHeight;
      float totalHeightNN = Math.Max(1, totalHeight); // Avoid divisions by zero

      float scrollAreaHeight = ScrollBarYSize;
      float h = Math.Min(scrollAreaHeight, Math.Max(
          scrollInfo.ViewPortHeight / totalHeightNN * scrollAreaHeight, SCROLLBAR_MINLENGTH));
      float y = Math.Min(scrollAreaHeight - h,
          scrollInfo.ViewPortStartY / totalHeightNN * scrollAreaHeight);

      ScrollBarYKnobHeight = h;
      ScrollBarYKnobPos = y;
      switch (VerticalScrollBarVisibility)
      {
        case ScrollBarVisibility.Disabled:
        case ScrollBarVisibility.Hidden:
          ScrollBarYVisible = false;
          break;
        case ScrollBarVisibility.Auto:
          ScrollBarYVisible = !IsNear(totalHeight, 0) && totalHeight > scrollInfo.ViewPortHeight + DELTA_DOUBLE;
          break;
        case ScrollBarVisibility.Visible:
          ScrollBarYVisible = true;
          break;
      }
    }

    void ConfigureContentScrollFacility()
    {
      ScrollContentPresenter scp = FindContentPresenter() as ScrollContentPresenter;
      if (scp == null)
        return;
      scp.HorizontalFitToSpace = HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled;
      scp.VerticalFitToSpace = VerticalScrollBarVisibility == ScrollBarVisibility.Disabled;

      if (_attachedScrollInfo != null)
        _attachedScrollInfo.Scrolled -= OnScrollInfoScrolled;
      IScrollInfo scrollInfo = FindScrollControl();
      if (_attachedScrollInfo != null && _attachedScrollInfo != scrollInfo)
        _attachedScrollInfo.DoScroll = false;
      if (scrollInfo == null)
      {
        _attachedScrollInfo = null;
        return;
      }
      scrollInfo.DoScroll = true;
      _attachedScrollInfo = scrollInfo;
      _attachedScrollInfo.Scrolled += OnScrollInfoScrolled;
    }

    void OnScrollInfoScrolled(object sender)
    {
      UpdateScrollBars();
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      ConfigureContentScrollFacility();
      return base.CalculateInnerDesiredSize(totalSize);
    }

    protected override void ArrangeTemplateControl()
    {
      base.ArrangeTemplateControl();
      // We need to update the scrollbars after our own and our content's final rectangles are set
      UpdateScrollBars();
    }

    public AbstractProperty VerticalScrollBarVisibilityProperty
    {
      get { return _verticalScrollBarVisibilityProperty; }
    }

    public ScrollBarVisibility VerticalScrollBarVisibility
    {
      get { return (ScrollBarVisibility)_verticalScrollBarVisibilityProperty.GetValue(); }
      set { _verticalScrollBarVisibilityProperty.SetValue(value); }
    }

    public AbstractProperty HorizontalScrollBarVisibilityProperty
    {
      get { return _horizontalScrollBarVisibilityProperty; }
    }

    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
      get { return (ScrollBarVisibility)_horizontalScrollBarVisibilityProperty.GetValue(); }
      set { _horizontalScrollBarVisibilityProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarXSizeProperty
    {
      get { return _scrollBarXSizeProperty; }
    }

    public float ScrollBarXSize
    {
      get { return (float)_scrollBarXSizeProperty.GetValue(); }
      set { _scrollBarXSizeProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarYSizeProperty
    {
      get { return _scrollBarYSizeProperty; }
    }

    public float ScrollBarYSize
    {
      get { return (float)_scrollBarYSizeProperty.GetValue(); }
      set { _scrollBarYSizeProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarXKnobPosProperty
    {
      get { return _scrollBarXKnobPosProperty; }
    }

    public float ScrollBarXKnobPos
    {
      get { return (float)_scrollBarXKnobPosProperty.GetValue(); }
      set { _scrollBarXKnobPosProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarXKnobWidthProperty
    {
      get { return _scrollBarXKnobWidthProperty; }
    }

    public float ScrollBarXKnobWidth
    {
      get { return (float)_scrollBarXKnobWidthProperty.GetValue(); }
      set { _scrollBarXKnobWidthProperty.SetValue(value); }
    }

    public bool ScrollBarXVisible
    {
      get { return (bool)_scrollBarXVisibleProperty.GetValue(); }
      set { _scrollBarXVisibleProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarXVisibleProperty
    {
      get { return _scrollBarXVisibleProperty; }
    }

    public AbstractProperty ScrollBarYKnobPosProperty
    {
      get { return _scrollBarYKnobPosProperty; }
    }

    public float ScrollBarYKnobPos
    {
      get { return (float)_scrollBarYKnobPosProperty.GetValue(); }
      set { _scrollBarYKnobPosProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarYKnobHeightProperty
    {
      get { return _scrollBarYKnobHeightProperty; }
    }

    public float ScrollBarYKnobHeight
    {
      get { return (float)_scrollBarYKnobHeightProperty.GetValue(); }
      set { _scrollBarYKnobHeightProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarYVisibleProperty
    {
      get { return _scrollBarYVisibleProperty; }
    }

    public bool ScrollBarYVisible
    {
      get { return (bool)_scrollBarYVisibleProperty.GetValue(); }
      set { _scrollBarYVisibleProperty.SetValue(value); }
    }

    public AbstractProperty CanContentScrollProperty
    {
      get { return _canContentScrollProperty; }
    }

    public bool CanContentScroll
    {
      get { return (bool)_canContentScrollProperty.GetValue(); }
      set { _canContentScrollProperty.SetValue(value); }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
      // migration from OnMouseWheel(int numDetents)
      // - no need to check if mouse is over
      // - no need to call base class

      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      if (svfs == null)
        return;

      int scrollByLines = SystemInformation.MouseWheelScrollLines; // Use the system setting as default.

      IScrollInfo scrollInfo = svfs as IScrollInfo;
      if (scrollInfo != null && scrollInfo.NumberOfVisibleLines != 0) // If ScrollControl can shown less items, use this as limit.
        scrollByLines = scrollInfo.NumberOfVisibleLines;

      int numLines = e.NumDetents * scrollByLines;

      if (numLines < 0)
        svfs.ScrollDown(-1 * numLines);
      else if (numLines > 0)
        svfs.ScrollUp(numLines);
    }

    internal override void OnMouseMove(float x, float y, ICollection<FocusCandidate> focusCandidates)
    {
      // Only handle mouse moves if no touch event happens
      if (_lastTouchEvent == null)
        base.OnMouseMove(x, y, focusCandidates);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
      // consume event if touch is down
      if (_lastTouchEvent != null)
      {
        e.Handled = true;
      }
    }

    public override void OnTouchDown(TouchDownEvent touchEventArgs)
    {
      var isInArea = IsInArea(touchEventArgs.LocationX, touchEventArgs.LocationY);
      base.OnTouchDown(touchEventArgs);
      // Only start handling touch if it happened inside control's area
      if (!touchEventArgs.IsPrimaryContact || !isInArea)
        return;

      _lastTouchEvent = touchEventArgs;
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      if (svfs != null)
        svfs.BeginScroll();
    }

    public override void OnTouchUp(TouchUpEvent touchEventArgs)
    {
      base.OnTouchUp(touchEventArgs);
      if (!touchEventArgs.IsPrimaryContact)
        return;

      _lastTouchEvent = null;
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      if (svfs != null)
        svfs.EndScroll();
    }

    public override void OnTouchMove(TouchMoveEvent touchEventArgs)
    {
      base.OnTouchMove(touchEventArgs);
      if (!touchEventArgs.IsPrimaryContact)
        return;

      if (_lastTouchEvent != null)
      {
        // Transform screen (i.e. 720p) to skin coordinates (i.e. 1080p)
        float lastX = _lastTouchEvent.LocationX;
        float lastY = _lastTouchEvent.LocationY;
        float currX = touchEventArgs.LocationX;
        float currY = touchEventArgs.LocationY;
        if (!TransformMouseCoordinates(ref lastX, ref lastY) || !TransformMouseCoordinates(ref currX, ref currY))
          return;

        var scrollX = currX - lastX;
        var scrollY = currY - lastY;

        IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
        if (svfs == null)
          return;

        svfs.Scroll(scrollX, scrollY);
      }
    }

    protected override void OnKeyPress(KeyEventArgs e)
    {
      // migration from OnKeyPressed(ref Key key)
      // - no need the check if already handled, b/c this is done by the invoker
      // - no need to check if any child has focus, since event was originally invoked on focused element, 
      //   and the bubbles up the visual tree. This should also handle the subScroller issue, since the 
      //   sub scroller is asked 1st if it wants to handle the input
      // - instead of setting key to None, we set e.Handled = true

      if (e.Key == Key.Down && OnDown())
        e.Handled = true;
      else if (e.Key == Key.Up && OnUp())
        e.Handled = true;
      else if (e.Key == Key.Left && OnLeft())
        e.Handled = true;
      else if (e.Key == Key.Right && OnRight())
        e.Handled = true;
      else if (e.Key == Key.Home && OnHome())
        e.Handled = true;
      else if (e.Key == Key.End && OnEnd())
        e.Handled = true;
      else if (e.Key == Key.PageDown && OnPageDown())
        e.Handled = true;
      else if (e.Key == Key.PageUp && OnPageUp())
        e.Handled = true;
    }

    bool OnHome()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusHome();
    }

    bool OnEnd()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusEnd();
    }

    bool OnPageDown()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusPageDown();
    }

    bool OnPageUp()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusPageUp();
    }

    bool OnLeft()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusLeft();
    }

    bool OnRight()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusRight();
    }

    bool OnDown()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusDown();
    }

    bool OnUp()
    {
      IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
      return svfs != null && svfs.FocusUp();
    }
  }
}

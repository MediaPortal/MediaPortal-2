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

using System;
using System.Windows.Forms;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core.General;
using MediaPortal.Utilities.DeepCopy;
using Screen=MediaPortal.UI.SkinEngine.ScreenManagement.Screen;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
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

    #endregion

    #region Ctor

    public ScrollViewer()
    {
      Init();
      Attach();
      UpdateScrollBars();
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
    }

    void Attach()
    {
      ContentProperty.Attach(OnContentChanged);
    }

    void Detach()
    {
      ContentProperty.Detach(OnContentChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ScrollViewer sv = (ScrollViewer) source;
      ScrollBarXKnobPos = sv.ScrollBarXKnobPos;
      ScrollBarXKnobWidth = sv.ScrollBarXKnobWidth;
      ScrollBarYKnobPos = sv.ScrollBarYKnobPos;
      ScrollBarYKnobHeight = sv.ScrollBarYKnobHeight;
      CanContentScroll = sv.CanContentScroll;
      Attach();
      copyManager.CopyCompleted += OnCopyCompleted;
    }

    #endregion

    void OnCopyCompleted(ICopyManager copyManager)
    {
      ConfigureContentScrollFacility();
    }

    void OnContentChanged(AbstractProperty property, object oldValue)
    {
      UpdateScrollBars();
      ConfigureContentScrollFacility();
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
      if (CanContentScroll)
        return scp.Content as IScrollInfo;
      return scp;
    }

    void UpdateScrollBars()
    {
      ScrollContentPresenter scp = FindContentPresenter() as ScrollContentPresenter;
      if (scp == null)
        return;
      IScrollInfo scrollInfo = FindScrollControl();
      if (scrollInfo == null)
        return;
      float totalWidth = scrollInfo.TotalWidth;
      float totalWidthNN = Math.Max(1, totalWidth); // Avoid divisions by zero
      float totalHeight = scrollInfo.TotalHeight;
      float totalHeightNN = Math.Max(1, totalHeight); // Avoid divisions by zero

      float scrollAreaWidth = ScrollBarXSize;
      float scrollAreaHeight = ScrollBarYSize;
      // Hint about the coordinate systems used:
      // The values our calculations are based on are in the coordinate system which is scaled by the
      // SkinContext.Zoom setting. The output values must be in the original coordinate system, so we have to
      // subtract out the zoom value
      float w = Math.Min(scrollAreaWidth, Math.Max(
          scrollInfo.ViewPortWidth / totalWidthNN * scrollAreaWidth, SCROLLBAR_MINLENGTH));
      float x = Math.Min(scrollAreaWidth-w,
          scrollInfo.ViewPortStartX / totalWidthNN * scrollAreaWidth);
      float h = Math.Min(scrollAreaHeight, Math.Max(
          scrollInfo.ViewPortHeight / totalHeightNN * scrollAreaHeight, SCROLLBAR_MINLENGTH));
      float y = Math.Min(scrollAreaHeight - h,
          scrollInfo.ViewPortStartY / totalHeightNN * scrollAreaHeight);

      ScrollBarXKnobWidth = w;
      ScrollBarXKnobPos = x;
      ScrollBarXVisible = !IsNear(totalWidth, 0) && totalWidth > scrollInfo.ViewPortWidth + DELTA_DOUBLE;

      ScrollBarYKnobHeight = h;
      ScrollBarYKnobPos = y;
      ScrollBarYVisible = !IsNear(totalHeight, 0) && totalHeight > scrollInfo.ViewPortHeight + DELTA_DOUBLE;
    }

    void ConfigureContentScrollFacility()
    {
      IScrollInfo scrollInfo = FindScrollControl();
      if (scrollInfo == null)
        return;
      scrollInfo.CanScroll = true;
      scrollInfo.Scrolled += OnScrollInfoScrolled;
    }

    void OnScrollInfoScrolled(object sender)
    {
      UpdateScrollBars();
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();
      // We need to update the scrollbars after our own and our content's final rectangles are set
      UpdateScrollBars();
    }

    public AbstractProperty ScrollBarXSizeProperty
    {
      get { return _scrollBarXSizeProperty; }
    }

    public float ScrollBarXSize
    {
      get { return (float) _scrollBarXSizeProperty.GetValue(); }
      set { _scrollBarXSizeProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarYSizeProperty
    {
      get { return _scrollBarYSizeProperty; }
    }

    public float ScrollBarYSize
    {
      get { return (float) _scrollBarYSizeProperty.GetValue(); }
      set { _scrollBarYSizeProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarXKnobPosProperty
    {
      get { return _scrollBarXKnobPosProperty; }
    }

    public float ScrollBarXKnobPos
    {
      get { return (float) _scrollBarXKnobPosProperty.GetValue(); }
      set { _scrollBarXKnobPosProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarXKnobWidthProperty
    {
      get { return _scrollBarXKnobWidthProperty; }
    }

    public float ScrollBarXKnobWidth
    {
      get { return (float) _scrollBarXKnobWidthProperty.GetValue(); }
      set { _scrollBarXKnobWidthProperty.SetValue(value); }
    }

    public bool ScrollBarXVisible
    {
      get { return (bool) _scrollBarXVisibleProperty.GetValue(); }
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
      get { return (float) _scrollBarYKnobPosProperty.GetValue(); }
      set { _scrollBarYKnobPosProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarYKnobHeightProperty
    {
      get { return _scrollBarYKnobHeightProperty; }
    }

    public float ScrollBarYKnobHeight
    {
      get { return (float) _scrollBarYKnobHeightProperty.GetValue(); }
      set { _scrollBarYKnobHeightProperty.SetValue(value); }
    }

    public AbstractProperty ScrollBarYVisibleProperty
    {
      get { return _scrollBarYVisibleProperty; }
    }

    public bool ScrollBarYVisible
    {
      get { return (bool) _scrollBarYVisibleProperty.GetValue(); }
      set { _scrollBarYVisibleProperty.SetValue(value); }
    }

    public AbstractProperty CanContentScrollProperty
    {
      get { return _canContentScrollProperty; }
    }

    public bool CanContentScroll
    {
      get { return (bool) _canContentScrollProperty.GetValue(); }
      set { _canContentScrollProperty.SetValue(value); }
    }

    public override void OnMouseWheel(int numDetents)
    {
      base.OnMouseWheel(numDetents);

      if (!IsMouseOver)
        return;

      int numLines = numDetents * SystemInformation.MouseWheelScrollLines;
      if (numLines < 0)
      {
        IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
        if (svfs != null)
          svfs.ScrollDown(-1 * numLines);
      }
      else if (numLines > 0)
      {
        IScrollViewerFocusSupport svfs = FindScrollControl() as IScrollViewerFocusSupport;
        if (svfs != null)
          svfs.ScrollUp(numLines);
      }
    }

    public override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);

      if (key == Key.None)
        // Key event was handeled by child
        return;

      if (!CheckFocusInScope())
        return;

      if (key == Key.Down && OnDown())
        key = Key.None;
      else if (key == Key.Up && OnUp())
        key = Key.None;
      else if (key == Key.Left && OnLeft())
        key = Key.None;
      else if (key == Key.Right && OnRight())
        key = Key.None;
      else if (key == Key.Home && OnHome())
        key = Key.None;
      else if (key == Key.End && OnEnd())
        key = Key.None;
      else if (key == Key.PageDown && OnPageDown())
        key = Key.None;
      else if (key == Key.PageUp && OnPageUp())
        key = Key.None;
    }

    /// <summary>
    /// Checks if the currently focused control is contained in this scrollviewer and
    /// is not contained in a sub scrollviewer. This is necessary for this scrollviewer to
    /// handle the focus scrolling keys in this scope.
    /// </summary>
    bool CheckFocusInScope()
    {
      Screen screen = Screen;
      Visual focusPath = screen == null ? null : screen.FocusedElement;
      while (focusPath != null)
      {
        if (focusPath == this)
          // Focused control is located in our focus scope
          return true;
        if (focusPath is ScrollViewer)
          // Focused control is located in another scrollviewer's focus scope
          return false;
        focusPath = focusPath.VisualParent;
      }
      return false;
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

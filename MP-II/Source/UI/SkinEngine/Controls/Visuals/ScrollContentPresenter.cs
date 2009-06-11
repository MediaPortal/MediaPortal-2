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
using System.Drawing;
using MediaPortal.SkinEngine.SkinManagement;
using MediaPortal.Utilities.DeepCopy;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class ScrollContentPresenter : ContentPresenter, IScrollInfo, IScrollViewerFocusSupport
  {
    #region Consts

    public const int NUM_SCROLL_PIXEL = 50;

    #endregion

    #region Protected fields

    protected bool _canScroll = false;
    protected float _scrollOffsetX = 0;
    protected float _scrollOffsetY = 0;
    protected float _actualScrollOffsetX = 0;
    protected float _actualScrollOffsetY = 0;

    #endregion

    #region Ctor

    public ScrollContentPresenter()
    {
      Init();
      Attach();
    }

    void Init()
    { }

    void Attach()
    { }

    void Detach()
    { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ScrollContentPresenter scp = (ScrollContentPresenter) source;
      CanScroll = copyManager.GetCopy(scp.CanScroll);
      Attach();
    }

    #endregion

    public void SetScrollOffset(float scrollOffsetX, float scrollOffsetY)
    {
      if (_scrollOffsetX == scrollOffsetX && _scrollOffsetY == scrollOffsetY)
        return;
      if (scrollOffsetX < ActualWidth - TotalWidth)
        scrollOffsetX = (float) ActualWidth - TotalWidth;
      if (scrollOffsetY < ActualHeight - TotalHeight)
        scrollOffsetY = (float) ActualHeight - TotalHeight;
      if (scrollOffsetX > 0)
        scrollOffsetX = 0;
      if (scrollOffsetY > 0)
        scrollOffsetY = 0;
      _scrollOffsetX = scrollOffsetX;
      _scrollOffsetY = scrollOffsetY;
      Invalidate();
    }

    public override void MakeVisible(UIElement element, RectangleF elementBounds)
    {
      if (_canScroll)
      {
        float differenceX = 0;
        float differenceY = 0;
        if (elementBounds.X + elementBounds.Width > ActualPosition.X + ActualWidth)
          differenceX = - (float) (elementBounds.X + elementBounds.Width - ActualPosition.X - ActualWidth);
        if (elementBounds.X < ActualPosition.X)
          differenceX = ActualPosition.X - elementBounds.X;
        if (elementBounds.Y + elementBounds.Height > ActualPosition.Y + ActualHeight)
          differenceY = - (float) (elementBounds.Y + elementBounds.Height - ActualPosition.Y - ActualHeight);
        if (elementBounds.Y < ActualPosition.Y)
          differenceY = ActualPosition.Y - elementBounds.Y;
        // Change rect as if children were already re-arranged
        elementBounds.X += differenceX;
        elementBounds.Y += differenceY;
        SetScrollOffset(_actualScrollOffsetX + differenceX, _actualScrollOffsetY + differenceY);
      }
      base.MakeVisible(element, elementBounds);
    }

    public override void Arrange(RectangleF finalRect)
    {
      RemoveMargin(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      if (_templateControl == null)
      {
        _scrollOffsetX = 0;
        _scrollOffsetY = 0;
      }
      else
      {
        SizeF desiredSize = _templateControl.TotalDesiredSize();
        PointF position;
        SizeF availableSize;
        if (_canScroll)
        {
          if (desiredSize.Width > finalRect.Width)
            _scrollOffsetX = Math.Max(_scrollOffsetX, finalRect.Width - desiredSize.Width);
          else
            _scrollOffsetX = 0;
          if (desiredSize.Height > finalRect.Height)
            _scrollOffsetY = Math.Max(_scrollOffsetY, finalRect.Height - desiredSize.Height);
          else
            _scrollOffsetY = 0;
          position = new PointF(finalRect.X + _scrollOffsetX, finalRect.Y + _scrollOffsetY);
          availableSize = desiredSize;
        }
        else
        {
          _scrollOffsetX = 0;
          _scrollOffsetY = 0;
          position = new PointF(finalRect.X, finalRect.Y);
          availableSize = finalRect.Size;
        }

        ArrangeChild(_templateControl, ref position, ref availableSize);
        RectangleF childRect = new RectangleF(position, availableSize);
        _templateControl.Arrange(childRect);
      }
      _actualScrollOffsetX = _scrollOffsetX;
      _actualScrollOffsetY = _scrollOffsetY;

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      Initialize();
      InitializeTriggers();
      IsInvalidLayout = false;
    }

    public override void DoRender()
    {
      SkinContext.AddScissorRect(new Rectangle(
          (int) ActualPosition.X, (int) ActualPosition.Y, (int) ActualWidth, (int) ActualHeight));
      GraphicsDevice.Device.ScissorRect = SkinContext.FinalScissorRect.Value;
      GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, true);
      base.DoRender(); // Do the actual rendering
      SkinContext.RemoveScissorRect();
      Rectangle? origScissorRect = SkinContext.FinalScissorRect;
      if (origScissorRect.HasValue)
      {
        GraphicsDevice.Device.ScissorRect = SkinContext.FinalScissorRect.Value;
        GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, true);
      }
      else
        GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, false);
    }

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
      FrameworkElement currentElement = GetFocusedElementOrChild();
      // Try to find first element which extends our range
      while (currentElement != null &&
          (currentElement.ActualPosition.Y >= ActualPosition.Y))
        currentElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Up);
      if (currentElement == null)
      {
        // No element to focus - fallback: move physical scrolling offset
        if (IsViewPortAtTop)
          return false;
        SetScrollOffset(_scrollOffsetX, _scrollOffsetY + (float) ActualHeight);
        return true;
      }
      else
        return currentElement.TrySetFocus(true);
    }

    public bool FocusPageDown()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      // Try to find first element which extends our range
      while (currentElement != null &&
          (currentElement.ActualPosition.Y + currentElement.ActualHeight <= ActualPosition.Y + ActualHeight))
        currentElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Down);
      if (currentElement == null)
      {
        // No element to focus - fallback: move physical scrolling offset
        if (IsViewPortAtBottom)
          return false;
        SetScrollOffset(_scrollOffsetX, _scrollOffsetY - (float) ActualHeight);
        return true;
      }
      else
        return currentElement.TrySetFocus(true);
    }

    public bool FocusPageLeft()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      // Try to find first element which extends our range
      while (currentElement != null &&
          (currentElement.ActualPosition.X >= ActualPosition.X))
        currentElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Left);
      if (currentElement == null)
      {
        // No element to focus - fallback: move physical scrolling offset
        if (IsViewPortAtTop)
          return false;
        SetScrollOffset(_scrollOffsetX + (float) ActualWidth, _scrollOffsetY);
        return true;
      }
      else
        return currentElement.TrySetFocus(true);
    }

    public bool FocusPageRight()
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      // Try to find first element which extends our range
      while (currentElement != null &&
          (currentElement.ActualPosition.X + currentElement.ActualWidth <= ActualPosition.X + ActualWidth))
        currentElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Right);
      if (currentElement == null)
      {
        // No element to focus - fallback: move physical scrolling offset
        if (IsViewPortAtRight)
          return false;
        SetScrollOffset(_scrollOffsetX - (float) ActualWidth, _scrollOffsetY);
        return true;
      }
      else
        return currentElement.TrySetFocus(true);
    }

    public bool FocusHome()
    {
      SetScrollOffset(0, 0);
      return true;
    }

    public bool FocusEnd()
    {
      if (TemplateControl == null)
        return false;
      SetScrollOffset(-(float) TemplateControl.ActualWidth, -(float) TemplateControl.ActualHeight);
      return true;
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
      get { return TemplateControl == null ? 0 : (float) TemplateControl.ActualWidth; }
    }

    public float TotalHeight
    {
      get { return TemplateControl == null ? 0 : (float) TemplateControl.ActualHeight; }
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
      get { return TemplateControl == null || _actualScrollOffsetY == 0; }
    }

    public bool IsViewPortAtBottom
    {
      get { return TemplateControl == null || -_actualScrollOffsetY + ActualHeight >= TotalHeight; }
    }

    public bool IsViewPortAtLeft
    {
      get { return TemplateControl == null || _actualScrollOffsetX == 0; }
    }

    public bool IsViewPortAtRight
    {
      get { return TemplateControl == null || -_actualScrollOffsetX + ActualWidth >= TotalWidth; }
    }

    #endregion
  }
}

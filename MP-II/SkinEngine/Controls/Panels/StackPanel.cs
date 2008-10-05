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
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.InputManagement;
using SlimDX;
using SlimDX.Direct3D9;
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

    protected bool _isClipping = false; // Set to true if the contents need more space than available
    protected bool _canScroll = false; // Set to true if we are located in a scrollable container (ScrollViewer for example)

    // Desired scroll offsets - when modified by method SetScrollOffset, they are applied the next time 
    // Arrange is called
    protected float _scrollOffsetY = 0;
    protected float _scrollOffsetX = 0;

    // Actual scroll offsets - may differ from the desired scroll offsets
    protected float _actualScrollOffsetY = 0;
    protected float _actualScrollOffsetX = 0;

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
      _orientationProperty.Attach(OnPropertyInvalidate);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnPropertyInvalidate);
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

    public void SetScrollOffset(float scrollOffsetX, float scrollOffsetY)
    {
      if (_scrollOffsetX == scrollOffsetX && _scrollOffsetY == scrollOffsetY)
        return;
      _scrollOffsetX = scrollOffsetX;
      _scrollOffsetY = scrollOffsetY;
      Invalidate();
    }

    public override void Measure(ref SizeF totalSize)
    {
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      float totalDesiredHeight = 0;
      float totalDesiredWidth = 0;
      SizeF childSize;
      SizeF minSize = new SizeF(0, 0);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) 
          continue;
        childSize = new SizeF(0, 0);
        child.Measure(ref childSize);
        if (Orientation == Orientation.Vertical)
        {
          totalDesiredHeight += childSize.Height;
          if (childSize.Width > totalDesiredWidth)
            totalDesiredWidth = childSize.Width;
        }
        else
        {
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

      if (Double.IsNaN(Width))
        _desiredSize.Width = totalDesiredWidth;

      if (Double.IsNaN(Height))
        _desiredSize.Height = totalDesiredHeight;

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      if (_canScroll)
        // If we are able to scroll, we only need the biggest child control dimensions
        _desiredSize = minSize;
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("StackPanel.Measure: {0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("StackPanel.Arrange: {0} X {1}, Y {2} W {3} H {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      ComputeInnerRectangle(ref finalRect);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      _totalHeight = 0;
      _totalWidth = 0;
      switch (Orientation)
      {
        case Orientation.Vertical:
          {
            float startPositionX = _scrollOffsetX;
            float startPositionY = _scrollOffsetY;
            SizeF childSize = new SizeF(0, 0);
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) 
                continue;

              PointF location = new PointF(ActualPosition.X + startPositionX,
                  ActualPosition.Y + startPositionY);

              child.TotalDesiredSize(ref childSize);
              childSize.Width = (float) ActualWidth;

              ArrangeChildHorizontal(child, ref location, ref childSize);

              child.Arrange(new RectangleF(location, childSize));
              _totalWidth = Math.Max(_totalWidth, (float) child.ActualWidth);
              _totalHeight += (float) child.ActualHeight;

              startPositionY += childSize.Height;
            }
          }
          break;

        case Orientation.Horizontal:
          {
            float startPositionX = _scrollOffsetX;
            float startPositionY = _scrollOffsetY;
            SizeF childSize = new SizeF(0, 0);
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) 
                continue;
              PointF location = new PointF(ActualPosition.X + startPositionX,
                  ActualPosition.Y + startPositionY);
              child.TotalDesiredSize(ref childSize);
              childSize.Height = (float) ActualHeight;

              ArrangeChildVertical(child, ref location, ref childSize);

              child.Arrange(new RectangleF(location, childSize));
              _totalWidth += (float) child.ActualWidth;
              _totalHeight = Math.Max(_totalHeight, (float) child.ActualHeight);

              startPositionX += childSize.Width;
            }
          }
          break;
      }

      if (_totalHeight > finalRect.Height || _totalWidth > finalRect.Width)
        _isClipping = true;
      else
      {
        _isClipping = false;
        _scrollOffsetX = 0;
        _scrollOffsetY = 0;
      }

      _actualScrollOffsetX = _scrollOffsetX;
      _actualScrollOffsetY = _scrollOffsetY;
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        if (Screen != null) Screen.Invalidate(this);
        _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      }
      base.Arrange(finalRect);
    }

    public override void MakeVisible(RectangleF childRect)
    {
      float differenceX = 0;
      float differenceY = 0;
      if (childRect.X + childRect.Width > ActualPosition.X + ActualWidth)
        differenceX = - (float) (childRect.X + childRect.Width - ActualPosition.X - ActualWidth);
      if (childRect.X < ActualPosition.X)
        differenceX = ActualPosition.X - childRect.X;
      if (childRect.Y + childRect.Height > ActualPosition.Y + ActualHeight)
        differenceY = - (float) (childRect.Y + childRect.Height - ActualPosition.Y - ActualHeight);
      if (childRect.Y < ActualPosition.Y)
        differenceY = ActualPosition.Y - childRect.Y;
      // Change rect as if children were already re-arranged
      childRect.X += differenceX;
      childRect.Y += differenceY;
      base.MakeVisible(childRect);
      SetScrollOffset(_actualScrollOffsetX + differenceX, _actualScrollOffsetY + differenceY);
    }

    #endregion

    #region Rendering

    protected override void RenderChildren()
    {
      lock (_orientationProperty)
      {
        bool clipping = _isClipping; // FIXME Albert78: we need to synchronize the threads changing layout
        if (clipping)
        {
          SkinContext.AddScissorRect(new Rectangle(
              (int) ActualPosition.X, (int) ActualPosition.Y, (int) ActualWidth, (int) ActualHeight));
          GraphicsDevice.Device.ScissorRect = SkinContext.FinalScissorRect.Value;
          GraphicsDevice.Device.SetRenderState(RenderState.ScissorTestEnable, true);
        }
        
        foreach (FrameworkElement element in _renderOrder)
        {
          if (!element.IsVisible) 
            continue;
          RectangleF elementBounds = element.ActualBounds;
          if (clipping)
          { // Don't render elements which are not visible
            if (elementBounds.X + elementBounds.Width < ActualPosition.X) continue;
            if (elementBounds.Y + elementBounds.Height < ActualPosition.Y) continue;
            if (elementBounds.X > ActualPosition.X + ActualWidth) continue;
            if (elementBounds.Y > ActualPosition.Y + ActualHeight) continue;
          }
          element.Render();
        }

        if (clipping)
        {
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
      }
    }

    #endregion

    #region Focus movement

    protected FrameworkElement GetFocusedElementOrChild()
    {
      FrameworkElement result = FocusManager.FocusedElement;
      if (result == null)
        foreach (UIElement child in Children)
        {
          result = child as FrameworkElement;
          if (result != null)
            break;
        }
      return result;
    }

    protected bool MoveFocus1(MoveFocusDirection direction)
    {
      FrameworkElement currentElement = GetFocusedElementOrChild();
      if (currentElement == null)
        return false;
      FrameworkElement prevElement = PredictFocus(currentElement.ActualBounds, direction);
      if (prevElement == null) return false;
      prevElement.TrySetFocus();
      return true;
    }

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
        FrameworkElement nextElement;
        while (currentElement.ActualPosition.Y > ActualPosition.Y &&
            (nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Up)) != null)
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
        FrameworkElement nextElement;
        while (currentElement.ActualPosition.Y + currentElement.ActualHeight <
            ActualPosition.Y + ActualHeight &&
            (nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Down)) != null)
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
        FrameworkElement nextElement;
        while (currentElement.ActualPosition.X > ActualPosition.X &&
            (nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Left)) != null)
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
        FrameworkElement nextElement;
        while (currentElement.ActualPosition.X + currentElement.ActualWidth <
            ActualPosition.X + ActualWidth &&
            (nextElement = PredictFocus(currentElement.ActualBounds, MoveFocusDirection.Right)) != null)
          currentElement = nextElement;
        currentElement.TrySetFocus();
      }
      return false;
    }

    public bool FocusHome()
    {
      return MoveFocusN(Orientation == Visuals.Orientation.Horizontal ? MoveFocusDirection.Left : MoveFocusDirection.Up);
    }

    public bool FocusEnd()
    {
      return MoveFocusN(Orientation == Visuals.Orientation.Horizontal ? MoveFocusDirection.Right : MoveFocusDirection.Down);
    }

    #endregion

    #region Input handling

    public override void OnKeyPressed(ref Key key)
    {
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnKeyPressed(ref key);
        if (key == Key.None) return;
      }
    }

    public override void OnMouseMove(float x, float y)
    {
      if (y < ActualPosition.Y) return;
      if (y > ActualHeight + ActualPosition.Y) return;
      foreach (UIElement element in Children)
      {
        if (false == element.IsVisible) continue;
        element.OnMouseMove(x, y);
      }
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

    #endregion
  }
}

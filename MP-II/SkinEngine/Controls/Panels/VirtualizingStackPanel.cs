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
using SlimDX;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  public class VirtualizingStackPanel : Panel, IScrollInfo
  {
    #region Private fields

    Property _orientationProperty;
    int _startIndex;
    int _endIndex;
    int _controlCount;
    double _lineHeight;
    double _lineWidth;

    #endregion

    #region Ctor

    public VirtualizingStackPanel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _orientationProperty = new Property(typeof(Orientation), Orientation.Vertical);
      _startIndex = 0;
      _endIndex = 0;
      _lineWidth = 0.0;
      _lineHeight = 0.0;
      _controlCount = 0;
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
      VirtualizingStackPanel p = source as VirtualizingStackPanel;
      Orientation = copyManager.GetCopy(p.Orientation);
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

    public override void Measure(ref SizeF totalSize)
    {

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      float totalHeight = 0.0f;
      float totalWidth = 0.0f;
      SizeF childSize = new SizeF(0, 0);
      int index = 0;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) 
          continue;
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        if (Orientation == Orientation.Vertical)
        {
          child.Measure(ref childSize);
          totalHeight += childSize.Height;

          if (childSize.Width > totalWidth)
            totalWidth = childSize.Width;
        }
        else
        {
          child.Measure(ref childSize);
          totalWidth += childSize.Width;

          if (childSize.Height > totalHeight)
            totalHeight = childSize.Height;
        }
        index++;
        _controlCount++;
      }


      _desiredSize = new SizeF(totalWidth, totalHeight);

      if (Double.IsNaN(Width))
        _desiredSize.Width = totalWidth;

      if (Double.IsNaN(Height))
        _desiredSize.Height = totalHeight;

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      
      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("VirtualizingStackPanel.measure :{0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("VirtualizingStackPanel.Arrange :{0} X {1},Y {2} W {3}xH {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      
      ComputeInnerRectangle(ref finalRect);

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

      float totalWidth = 0;
      float totalHeight = 0;
      SizeF ChildSize = new SizeF();

      _controlCount = 0;
      int index = 0;
      switch (Orientation)
      {
        case Orientation.Vertical:
          {
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) 
                continue;

              if (index < _startIndex)
              {
                index++;
                continue;
              }
              PointF location = new PointF(ActualPosition.X, ActualPosition.Y + totalHeight);
              child.TotalDesiredSize(ref ChildSize);
   
              // Default behavior is to fill the content if the child has no size
              if (Double.IsNaN(child.Width))
              {
                ChildSize.Width = (float)ActualWidth;
              }

              // See if this should be included
              if (totalHeight + ChildSize.Height > Math.Ceiling(ActualHeight))
                break;

              //align horizontally 
              if (AlignmentX == AlignmentX.Center)
              {
                location.X += (float)((ActualWidth - ChildSize.Width) / 2);
              }
              else if (AlignmentX == AlignmentX.Right)
              {
                location.X = (float)(ActualWidth - ChildSize.Width);
              }

              ChildSize.Width = (float)ActualWidth;
              child.Arrange(new RectangleF(location, ChildSize));
              totalHeight += ChildSize.Height;
              index++;
              _controlCount++;
            }
          }
          break;

        case Orientation.Horizontal:
          {
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) 
                continue;
              if (index < _startIndex)
              {
                index++;
                continue;
              }
              PointF location = new PointF(ActualPosition.X + totalWidth, ActualPosition.Y);
              child.TotalDesiredSize(ref ChildSize);

              // Default behavior is to fill the content if the child has no size
              if (Double.IsNaN(child.Height))
              {
                ChildSize.Height = (float)ActualHeight;
              }

              // See if this should be included
              if (totalWidth + ChildSize.Width > Math.Ceiling(ActualWidth))
                break;

              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (float)((ActualHeight - ChildSize.Height) / 2);
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += (float)(ActualHeight - ChildSize.Height);
              }

              child.Arrange(new RectangleF(location, ChildSize));
              totalWidth += ChildSize.Width;
              index++;
              _controlCount++;
            }
          }
          break;
      }
      _endIndex = index;
      // Calculate hight per line / row
      if (_controlCount > 0)
      {
        _lineHeight = totalHeight / ((double)_controlCount);
        _lineWidth = totalWidth / ((double)_controlCount);
      }
      else
      {
        _lineHeight = totalHeight;
        _lineWidth = totalWidth;
      }

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
      FreeUnused();
    }

    protected override void RenderChildren()
    {
      lock (_orientationProperty)
      {

        int index = 0;
        foreach (UIElement element in _renderOrder)
        {
          if (!element.IsVisible) continue;
          if (index < _startIndex)
          {
            index++;
            continue;
          }

          element.Render();


          index++;
          if (index >= _endIndex) break;
        }
      }
    }

    #region IScrollInfo Members

    void FreeUnused()
    {
      UpdateRenderOrder(); 
      int index = 0;
      foreach (UIElement element in _renderOrder)
      {
        if (!element.IsVisible) continue;
        if (index < _startIndex || index >= _endIndex)
        { 
          element.FireUIEvent(UIEvent.Hidden, this);
          index++;
          continue;
        }
 
        element.FireUIEvent(UIEvent.Visible, this);

        index++;
      }
    }

    public bool LineDown(PointF point)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (_startIndex + _controlCount < Children.Count)
        {
          lock (_orientationProperty)
          {
            _startIndex++;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
      }
      return false;
    }

    public bool LineUp(PointF point)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (_startIndex > 0)
        {
          lock (_orientationProperty)
          {
            _startIndex--;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
      }
      return false;
    }

    public bool ScrollToItemWhichStartsWith(string text, int offset)
    {
      lock (_orientationProperty)
      {
        int firstItem = 0;
        FrameworkElement element = null;
        for (int i = 0; i < Children.Count; ++i)
        {
          element = (FrameworkElement)Children[i];
          if (element.Context != null)
          {
            if (element.Context.ToString().ToLower().StartsWith(text))
            {
              offset--;
              if (offset < 0)
              {
                firstItem = i;
                break;
              }
            }
          }
        }
        if (element == null) return false;
        _startIndex = 0;
        while (firstItem >= _controlCount)
        {
          _startIndex += _controlCount;
          firstItem -= _controlCount;
        }
        _startIndex += firstItem;
        while (_startIndex + _controlCount >= Children.Count)
          _startIndex--;

        Invalidate();
        UpdateLayout();
        OnMouseMove(element.ActualPosition.X, element.ActualPosition.Y);
      }
      return true;
    }

    public bool LineLeft(PointF point)
    {
      if (Orientation == Orientation.Horizontal)
      {
        if (_startIndex > 0)
        {
          lock (_orientationProperty)
          {
            _startIndex--;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
      }
      return false;
    }

    public bool LineRight(PointF point)
    {
      if (Orientation == Orientation.Horizontal)
      {
        if (_startIndex + _controlCount < Children.Count)
        {
          lock (_orientationProperty)
          {
            _startIndex++;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
      }
      return false;
    }

    public bool MakeVisible()
    {
      return false;
    }

    public bool PageDown(PointF point)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (_startIndex + 2 * _controlCount < Children.Count)
        {
          lock (_orientationProperty)
          {
            _startIndex += _controlCount;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
        else
        {
          lock (_orientationProperty)
          {
            _startIndex = Children.Count - _controlCount;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
      }
      return false;
    }

    public bool PageLeft(PointF point)
    {
      return false;
    }

    public bool PageRight(PointF point)
    {
      return false;
    }

    public bool PageUp(PointF point)
    {
      if (Orientation == Orientation.Vertical)
      {
        if (_startIndex > _controlCount)
        {
          lock (_orientationProperty)
          {
            _startIndex -= _controlCount;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
        else
        {
          lock (_orientationProperty)
          {
            _startIndex = 0;
            Invalidate();
            UpdateLayout();
            OnMouseMove(point.X, point.Y);
            return true;
          }
        }
      }
      return false;
    }

    public double LineHeight
    {
      get { return _lineHeight; }
    }

    public double LineWidth
    {
      get { return _lineWidth; }
    }

    public void Home(PointF point)
    {
      lock (_orientationProperty)
      {
        _startIndex = 0;
        Invalidate();
        UpdateLayout();
        OnMouseMove(Children[0].ActualPosition.X, Children[0].ActualPosition.Y);
      }
    }

    public void End(PointF point)
    {
      lock (_orientationProperty)
      {
        _startIndex = (Children.Count - _controlCount);
        Invalidate();
        UpdateLayout();
        FrameworkElement child = (FrameworkElement)Children[Children.Count - 1];
        OnMouseMove(child.ActualPosition.X, child.ActualPosition.Y);
      }
    }

    public void ResetScroll()
    {
      _startIndex = 0;
      //FreeUnused();
    }

    #endregion

    public override void OnKeyPressed(ref Key key)
    {
      int index = 0;
      foreach (UIElement element in Children)
      {
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        if (false == element.IsVisible) continue;
        element.OnKeyPressed(ref key);
        if (key == Key.None) return;
        index++;
        if (index >= _endIndex) break;
      }
    }

    public override void OnMouseMove(float x, float y)
    {
      int index = 0;
      foreach (UIElement element in Children)
      {
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        if (false == element.IsVisible) continue;
        element.OnMouseMove(x, y);
        index++;
        if (index >= _endIndex) break;
      }
    }

    #region Focus prediction

    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      int index = 0;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible || !(child is FrameworkElement)) continue;
        FrameworkElement fe = (FrameworkElement) child;

        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        FrameworkElement match = fe.PredictFocusUp(focusedFrameworkElement);
        if (match != null)
        {
          if (match == focusedFrameworkElement)
            continue;
          if (match.Focusable && match.IsVisible)
          {
            float distance = BorderDistance(match, focusedFrameworkElement);
            float centerDistance = CenterDistance(match, focusedFrameworkElement);
            if (bestMatch == null || distance < bestDistance ||
                distance == bestDistance && centerDistance < bestCenterDistance)
            {
              bestMatch = match;
              bestDistance = distance;
              bestCenterDistance = centerDistance;
            }
          }
        }
      }
      return bestMatch;
    }

    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      int index = 0;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible || !(child is FrameworkElement)) continue;
        FrameworkElement fe = (FrameworkElement) child;

        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        FrameworkElement match = fe.PredictFocusDown(focusedFrameworkElement);
        if (match != null)
        {
          if (match == focusedFrameworkElement)
            continue;
          if (match.Focusable && match.IsVisible)
          {
            float distance = BorderDistance(match, focusedFrameworkElement);
            float centerDistance = CenterDistance(match, focusedFrameworkElement);
            if (bestMatch == null || distance < bestDistance ||
                distance == bestDistance && centerDistance < bestCenterDistance)
            {
              bestMatch = match;
              bestDistance = distance;
              bestCenterDistance = centerDistance;
            }
          }
        }
      }
      return bestMatch;
    }

    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      int index = 0;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible || !(child is FrameworkElement)) continue;
        FrameworkElement fe = (FrameworkElement) child;

        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        FrameworkElement match = fe.PredictFocusLeft(focusedFrameworkElement);
        if (match != null)
        {
          if (match == focusedFrameworkElement)
            continue;
          if (match.Focusable && match.IsVisible)
          {
            float distance = BorderDistance(match, focusedFrameworkElement);
            float centerDistance = CenterDistance(match, focusedFrameworkElement);
            if (bestMatch == null || distance < bestDistance ||
                distance == bestDistance && centerDistance < bestCenterDistance)
            {
              bestMatch = match;
              bestDistance = distance;
              bestCenterDistance = centerDistance;
            }
          }
        }
      }
      return bestMatch;
    }

    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      float bestCenterDistance = float.MaxValue;
      int index = 0;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible || !(child is FrameworkElement)) continue;
        FrameworkElement fe = (FrameworkElement) child;

        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        FrameworkElement match = fe.PredictFocusRight(focusedFrameworkElement);
        if (match != null)
        {
          if (match == focusedFrameworkElement)
            continue;
          if (match.Focusable && match.IsVisible)
          {
            float distance = BorderDistance(match, focusedFrameworkElement);
            float centerDistance = CenterDistance(match, focusedFrameworkElement);
            if (bestMatch == null || distance < bestDistance ||
                distance == bestDistance && centerDistance < bestCenterDistance)
            {
              bestMatch = match;
              bestDistance = distance;
              bestCenterDistance = centerDistance;
            }
          }
        }
      }
      return bestMatch;
    }

    #endregion
  }
}

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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Presentation.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using RectangleF = System.Drawing.RectangleF;
using Presentation.SkinEngine.DirectX;
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Rendering;

namespace Presentation.SkinEngine.Controls.Panels
{
  public class VirtualizingStackPanel : Panel, IScrollInfo
  {
    Property _orientationProperty;
    int _startIndex;
    int _endIndex;
    int _controlCount;
    double _lineHeight;
    double _lineWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="StackPanel"/> class.
    /// </summary>
    public VirtualizingStackPanel()
    {
      Init();
    }
    public VirtualizingStackPanel(VirtualizingStackPanel v)
      : base(v)
    {
      Init();
      Orientation = v.Orientation;
    }

    void Init()
    {
      _orientationProperty = new Property(Orientation.Vertical);
      _orientationProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _startIndex = 0;
      _endIndex = 0;
      _lineWidth = 0.0;
      _lineHeight = 0.0;
      _controlCount = 0;

    }

    public override object Clone()
    {
      return new VirtualizingStackPanel(this);
    }

    /// <summary>
    /// Gets or sets the orientation property.
    /// </summary>
    /// <value>The orientation property.</value>
    public Property OrientationProperty
    {
      get
      {
        return _orientationProperty;
      }
      set
      {
        _orientationProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the orientation.
    /// </summary>
    /// <value>The orientation.</value>
    public Orientation Orientation
    {
      get
      {
        return (Orientation)_orientationProperty.GetValue();
      }
      set
      {
        _orientationProperty.SetValue(value);
      }
    }

    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      //      Trace.WriteLine(String.Format("VirtualizingStackPanel.Measure :{0} {1}x{2}", this.Name, (int)availableSize.Width, (int)availableSize.Height));

      float marginWidth = (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      float marginHeight = (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      _desiredSize = new System.Drawing.SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);
      if (Width <= 0)
        _desiredSize.Width = (float)(availableSize.Width - marginWidth);
      if (Height <= 0)
        _desiredSize.Height = (float)(availableSize.Height - marginHeight);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      float totalHeight = 0.0f;
      float totalWidth = 0.0f;
      SizeF childSize = new SizeF(_desiredSize.Width, _desiredSize.Height);
      int index = 0;
      _controlCount = 0;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        if (Orientation == Orientation.Vertical)
        {
          if (childSize.Width < 0) childSize.Width = 0;
          if (childSize.Height < 0) childSize.Height = 0;
          child.Measure(new SizeF(childSize.Width, 0));
          childSize.Height -= child.DesiredSize.Height;
          if (availableSize.Height > 0 && totalHeight + child.DesiredSize.Height > 5 + availableSize.Height)
            break;
          totalHeight += child.DesiredSize.Height;
          child.Measure(new SizeF(childSize.Width, child.DesiredSize.Height));
          if (child.DesiredSize.Width > totalWidth)
            totalWidth = child.DesiredSize.Width;
        }
        else
        {
          child.Measure(new SizeF(0, childSize.Height));
          childSize.Width -= child.DesiredSize.Width;
          if (availableSize.Width > 0 & totalWidth + child.DesiredSize.Width > 5 + availableSize.Width)
            break;
          totalWidth += child.DesiredSize.Width;

          child.Measure(new SizeF(child.DesiredSize.Width, childSize.Height));
          if (child.DesiredSize.Height > totalHeight)
            totalHeight = child.DesiredSize.Height;
        }
        index++;
        _controlCount++;
      }
      _endIndex = index;
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
      if (Width > 0) totalWidth = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) totalHeight = (float)Height * SkinContext.Zoom.Height;
      _desiredSize = new SizeF((float)totalWidth, (float)totalHeight);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
      _originalSize = _desiredSize;


      base.Measure(availableSize);
      //      Trace.WriteLine(String.Format("VirtualizingStackPanel.measure :{0} {1}x{2} returns {3}x{4}", this.Name, (int)availableSize.Width, (int)availableSize.Height, (int)_desiredSize.Width, (int)_desiredSize.Height));
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(RectangleF finalRect)
    {
      //      Trace.WriteLine(String.Format("VirtualizingStackPanel.arrange :{0} {1},{2} {3}x{4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      int index = 0;
      switch (Orientation)
      {
        case Orientation.Vertical:
          {
            float totalHeight = 0;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) continue;

              if (index < _startIndex)
              {
                index++;
                continue;
              }
              PointF location = new PointF((float)(this.ActualPosition.X), (float)(this.ActualPosition.Y + totalHeight));
              SizeF size = new SizeF(child.DesiredSize.Width, child.DesiredSize.Height);

              //align horizontally 
              if (AlignmentX == AlignmentX.Center)
              {
                location.X += (float)((layoutRect.Width - child.DesiredSize.Width) / 2);
              }
              else if (AlignmentX == AlignmentX.Right)
              {
                location.X = layoutRect.Right - child.DesiredSize.Width;
              }

              child.Arrange(new RectangleF(location, size));
              totalHeight += child.DesiredSize.Height;
              index++;
              if (index == _endIndex) break;
            }
          }
          break;

        case Orientation.Horizontal:
          {
            float totalWidth = 0;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) continue;
              if (index < _startIndex)
              {
                index++;
                continue;
              }
              PointF location = new PointF((float)(this.ActualPosition.X + totalWidth), (float)(this.ActualPosition.Y));
              SizeF size = new SizeF(child.DesiredSize.Width, child.DesiredSize.Height);

              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (float)((layoutRect.Height - child.DesiredSize.Height) / 2);
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += (float)(layoutRect.Height - child.DesiredSize.Height);
              }

              //ArrangeChild(child, ref location);
              child.Arrange(new RectangleF(location, size));
              totalWidth += child.DesiredSize.Width;
              index++;
              if (index == _endIndex) break;
            }
          }
          break;
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
        if (Window != null) Window.Invalidate(this);
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      }
      base.Arrange(layoutRect);
      FreeUnused();
    }
    /// <summary>
    /// Renders the visual
    /// </summary>
    /// 
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
    public override void Reset()
    {
      _startIndex = 0;
      base.Reset();
    }
    public bool LineDown(PointF point)
    {
      if (this.Orientation == Orientation.Vertical)
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
      if (this.Orientation == Orientation.Vertical)
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
        OnMouseMove((float)element.ActualPosition.X, (float)element.ActualPosition.Y);
      }
      return true;
    }

    public bool LineLeft(PointF point)
    {
      if (this.Orientation == Orientation.Horizontal)
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
      if (this.Orientation == Orientation.Horizontal)
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
      if (this.Orientation == Orientation.Vertical)
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
      if (this.Orientation == Orientation.Vertical)
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
      get
      {
        return _lineHeight;
      }
    }

    public double LineWidth
    {
      get
      {
        return _lineWidth;
      }
    }
    public void Home(PointF point)
    {
      lock (_orientationProperty)
      {
        _startIndex = 0;
        Invalidate();
        UpdateLayout();
        OnMouseMove((float)(Children[0].ActualPosition.X), (float)(Children[0].ActualPosition.Y));
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
        OnMouseMove((float)(child.ActualPosition.X), (float)(child.ActualPosition.Y));
      }
    }
    public void ResetScroll()
    {
      _startIndex = 0;
      //FreeUnused();
    }
    #endregion

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref MediaPortal.Control.InputManager.Key key)
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
        if (key == MediaPortal.Control.InputManager.Key.None) return;
        index++;
        if (index >= _endIndex) break;
      }
    }

    /// <summary>
    /// Called when the mouse moves
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
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

    #region focus prediction

    /// <summary>
    /// Predicts the next FrameworkElement which is position above this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusUp(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      int index = 0;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusUp(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match.Focusable)
          {
            if (match == focusedFrameworkElement)
            {
              continue;
            }
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.Y + match.ActualHeight >= bestMatch.ActualPosition.Y + bestMatch.ActualHeight)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position below this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Control.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusDown(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      int index = 0;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusDown(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.Y <= bestMatch.ActualPosition.Y)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position left of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Control.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusLeft(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      int index = 0;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusLeft(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.X >= bestMatch.ActualPosition.X)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }

    /// <summary>
    /// Predicts the next FrameworkElement which is position right of this FrameworkElement
    /// </summary>
    /// <param name="focusedFrameworkElement">The current  focused FrameworkElement.</param>
    /// <param name="key">The MediaPortal.Control.InputManager.Key.</param>
    /// <returns></returns>
    public override FrameworkElement PredictFocusRight(FrameworkElement focusedFrameworkElement, ref MediaPortal.Control.InputManager.Key key, bool strict)
    {
      FrameworkElement bestMatch = null;
      float bestDistance = float.MaxValue;
      int index = 0;
      foreach (FrameworkElement c in Children)
      {
        if (!c.IsVisible) continue;
        if (index < _startIndex)
        {
          index++;
          continue;
        }
        index++;
        if (index > _endIndex) break;
        if (!c.IsFocusScope) continue;
        FrameworkElement match = c.PredictFocusRight(focusedFrameworkElement, ref key, strict);
        if (key == MediaPortal.Control.InputManager.Key.None)
        {
          return match;
        }
        if (match != null)
        {
          if (match == focusedFrameworkElement)
          {
            continue;
          }
          if (match.Focusable)
          {
            if (bestMatch == null)
            {
              bestMatch = match;
              bestDistance = Distance(match, focusedFrameworkElement);
            }
            else
            {
              if (match.ActualPosition.X <= bestMatch.ActualPosition.X)
              {
                float distance = Distance(match, focusedFrameworkElement);
                if (distance < bestDistance)
                {
                  bestMatch = match;
                  bestDistance = distance;
                }
              }
            }
          }
        }
      }
      return bestMatch;
    }


    #endregion

  }
}

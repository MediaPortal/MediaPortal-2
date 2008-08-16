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
using System.Drawing;
using SlimDX;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Positions child elements in sequential position from left to right, 
  /// breaking content to the next line at the edge of the containing box. 
  /// Subsequent ordering happens sequentially from top to bottom or from right to left, 
  /// depending on the value of the Orientation property.
  /// </summary>
  public class WrapPanel : Panel
  {
    #region Private fields

    Property _orientationProperty;
    List<float> _sizeCol;

    #endregion

    #region Ctor

    public WrapPanel()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _orientationProperty = new Property(typeof(Orientation), Orientation.Horizontal);
      _sizeCol = new List<float>();
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
      WrapPanel p = source as WrapPanel;
      Orientation = copyManager.GetCopy(p.Orientation);
      Attach();
    }

    #endregion

    public Property OrientationProperty
    {
      get { return _orientationProperty; }
    }

    public Orientation Orientation
    {
      get { return (Orientation)_orientationProperty.GetValue(); }
      set { _orientationProperty.SetValue(value); }
    }

    public override void Measure(ref SizeF totalSize)
    {
      SizeF availableSize = new SizeF((float)Width * SkinContext.Zoom.Width, 
                                (float)Height * SkinContext.Zoom.Height);

      float totalHeight = 0.0f;
      float totalWidth = 0.0f;
      float initialHeight = availableSize.Height;
      float initialWidth = availableSize.Width;

      SizeF childSize = new SizeF();
      
      _sizeCol.Clear();
      float w = 0.0f;
      float h = 0.0f;
      switch (Orientation)
      {
        case Orientation.Horizontal:
          {
            w = childSize.Width;
            foreach (FrameworkElement child in Children)
            {
              child.Measure(ref childSize);
              // If width is not set, then just go on
              if (!double.IsNaN(availableSize.Width) && childSize.Width > availableSize.Width)
              {
                _sizeCol.Add(totalHeight);
                h += totalHeight;
                availableSize.Height -= totalHeight;
                availableSize.Width = initialWidth;
                totalHeight = 0.0f;
              }
              availableSize.Width -= childSize.Width;
              totalWidth += childSize.Width;

              if (childSize.Height > totalHeight)
                totalHeight = childSize.Height;

            }
            _sizeCol.Add(totalHeight);
            h += totalHeight;
          }
          break;

        case Orientation.Vertical:
          {
            h = childSize.Height;
            foreach (FrameworkElement child in Children)
            {
              child.Measure(ref childSize);
              if (!double.IsNaN(availableSize.Height) && childSize.Height > availableSize.Height)
              {
                _sizeCol.Add(totalWidth);
                w += totalWidth;
                childSize.Width -= totalWidth;
                childSize.Height = initialHeight;
                totalWidth = 0.0f;
              }
              childSize.Height -= childSize.Height;
              totalHeight += childSize.Height;
              if (childSize.Width > totalWidth)
                totalWidth = child.DesiredSize.Width;

            }
            _sizeCol.Add(totalWidth);
            w += totalWidth;
          }
          break;
      }
      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

      if (Double.IsNaN(Width))
        _desiredSize.Width = w;

      if (Double.IsNaN(Height))
        _desiredSize.Height = h;
      
      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }


      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("WrapPanel.measure :{0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));

    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("WrapPanel.Arrange :{0} X {1},Y {2} W {3}xH {4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      ComputeInnerRectangle(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      switch (Orientation)
      {
        case Orientation.Horizontal:
          {
            float offsetX = 0;
            float offsetY = 0;
            float totalHeight = 0;
            int offset = 0;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) continue;
              if (child.DesiredSize.Width + offsetX > _desiredSize.Width)
              {
                offsetX = 0;
                offsetY += totalHeight;
                totalHeight = 0;
                offset++;
              }
              PointF location = new PointF((float)(this.ActualPosition.X + offsetX), (float)(this.ActualPosition.Y + offsetY));
              SizeF size = new SizeF(child.DesiredSize.Width, child.DesiredSize.Height);

              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (float)((_sizeCol[offset] - child.DesiredSize.Height) / 2);
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += (float)(_sizeCol[offset] - child.DesiredSize.Height);
              }


              child.Arrange(new RectangleF(location, size));
              offsetX += child.DesiredSize.Width;
              if (child.DesiredSize.Height > totalHeight)
                totalHeight = child.DesiredSize.Height;
            }
          }
          break;

        case Orientation.Vertical:
          {
            float offsetX = 0;
            float offsetY = 0;
            float totalWidth = 0;
            int offset = 0;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible) continue;
              if (child.DesiredSize.Height + offsetY > _desiredSize.Height)
              {
                offsetY = 0;
                offsetX += totalWidth;
                totalWidth = 0;
                offset++;
              }
              PointF location = new PointF((float)(this.ActualPosition.X + offsetX), (float)(this.ActualPosition.Y + offsetY));
              SizeF size = new SizeF(child.DesiredSize.Width, child.DesiredSize.Height);

              //align horizontally 
              if (AlignmentX == AlignmentX.Center)
              {
                location.X += (float)((_sizeCol[offset] - size.Width) / 2);
              }
              else if (AlignmentX == AlignmentX.Right)
              {
                location.X += (float)(_sizeCol[offset] - child.Width);
              }
              child.Arrange(new RectangleF(location, size));
              offsetY += child.DesiredSize.Height;
              if (child.DesiredSize.Width > totalWidth)
                totalWidth = child.DesiredSize.Width;
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
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      }
      base.Arrange(finalRect);
    }
  }
}

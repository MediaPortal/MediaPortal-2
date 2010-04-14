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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Panels
{
  /// <summary>
  /// Positions child elements in sequential position from left to right, 
  /// breaking content to the next line at the edge of the containing box. 
  /// Subsequent ordering happens sequentially from top to bottom or from right to left, 
  /// depending on the value of the Orientation property.
  /// </summary>
  public class WrapPanel : Panel
  {
    #region Protected fields

    protected AbstractProperty _orientationProperty;
    protected IList<float> _sizeCol;

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
      _sizeCol = new List<float>();
    }

    void Attach()
    {
      _orientationProperty.Attach(OnLayoutPropertyChanged);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnLayoutPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      WrapPanel p = (WrapPanel) source;
      Orientation = p.Orientation;
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

    protected override SizeF CalculateDesiredSize(SizeF totalSize)
    {
      float initialHeight = totalSize.Height;
      float initialWidth = totalSize.Width;

      _sizeCol.Clear();
      float totalDesiredWidth = 0;
      float totalDesiredHeight = 0;
      float currentHeight = 0;
      float currentWidth = 0;
      switch (Orientation)
      {
        case Orientation.Horizontal:
          {
            totalDesiredWidth = totalSize.Width;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible)
                continue;

              SizeF childSize = new SizeF(totalDesiredWidth, float.NaN);
              child.Measure(ref childSize);
              // If width is not set, then just go on
              if (!double.IsNaN(totalSize.Width) && childSize.Width > totalSize.Width)
              {
                _sizeCol.Add(currentHeight);
                totalDesiredHeight += currentHeight;
                totalSize.Height -= currentHeight;
                totalSize.Width = initialWidth;
                currentHeight = 0;
              }
              totalSize.Width -= childSize.Width;
              currentWidth += childSize.Width;

              if (childSize.Height > currentHeight)
                currentHeight = childSize.Height;
            }
            _sizeCol.Add(currentHeight);
            totalDesiredHeight += currentHeight;
          }
          break;

        case Orientation.Vertical:
          {
            totalDesiredHeight = totalSize.Height;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible)
                continue;

              SizeF childSize = new SizeF(float.NaN, totalDesiredHeight);
              child.Measure(ref childSize);
              if (!double.IsNaN(totalSize.Height) && childSize.Height > totalSize.Height)
              {
                _sizeCol.Add(currentWidth);
                totalDesiredWidth += currentWidth;
                totalSize.Width -= currentWidth;
                totalSize.Height = initialHeight;
                currentWidth = 0;
              }
              totalSize.Height -= childSize.Height;
              currentHeight += childSize.Height;
              if (childSize.Width > currentWidth)
                currentWidth = childSize.Width;
            }
            _sizeCol.Add(currentWidth);
            totalDesiredWidth += currentWidth;
          }
          break;
      }
      
      return new SizeF(totalDesiredWidth, totalDesiredHeight);
    }

    protected override void ArrangeOverride(RectangleF finalRect)
    {
      base.ArrangeOverride(finalRect);
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
              if (!child.IsVisible)
                continue;

              SizeF childSize = child.DesiredSize;
              if (childSize.Width + offsetX > _desiredSize.Width)
              {
                offsetX = 0;
                offsetY += totalHeight;
                totalHeight = 0;
                offset++;
              }
              PointF location = new PointF(ActualPosition.X + offsetX, ActualPosition.Y + offsetY);
              SizeF size = new SizeF(childSize.Width, _sizeCol[offset]);

              ArrangeChildVertical(child, ref location, ref size);

              child.Arrange(new RectangleF(location, size));
              offsetX += childSize.Width;
              if (childSize.Height > totalHeight)
                totalHeight = childSize.Height;
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
              if (!child.IsVisible)
                continue;

              SizeF childSize = child.DesiredSize;
              if (childSize.Height + offsetY > _desiredSize.Height)
              {
                offsetY = 0;
                offsetX += totalWidth;
                totalWidth = 0;
                offset++;
              }
              PointF location = new PointF(ActualPosition.X + offsetX, ActualPosition.Y + offsetY);
              SizeF size = new SizeF(_sizeCol[offset], childSize.Height);

              ArrangeChildHorizontal(child, ref location, ref size);

              child.Arrange(new RectangleF(location, size));
              offsetY += childSize.Height;
              if (childSize.Width > totalWidth)
                totalWidth = childSize.Width;
            }
          }
          break;
      }
    }
  }
}

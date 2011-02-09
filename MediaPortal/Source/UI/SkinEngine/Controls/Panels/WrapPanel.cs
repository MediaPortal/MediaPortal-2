#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
  /// depending on the value of the <see cref="Orientation"/> property.
  /// </summary>
  public class WrapPanel : Panel
  {
    #region Protected fields

    protected AbstractProperty _orientationProperty;
    
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
    }

    void Attach()
    {
      _orientationProperty.Attach(OnCompleteLayoutGetsInvalid);
    }

    void Detach()
    {
      _orientationProperty.Detach(OnCompleteLayoutGetsInvalid);
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

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      float initialHeight = totalSize.Height;
      float initialWidth = totalSize.Width;

      float totalDesiredWidth = 0;
      float totalDesiredHeight = 0;
      float currentHeight = 0;
      float currentWidth = 0;
      switch (Orientation)
      {
        case Orientation.Horizontal:
          {
            totalDesiredWidth = totalSize.Width;
            foreach (FrameworkElement child in GetVisibleChildren())
            {
              SizeF childSize = new SizeF(totalDesiredWidth, float.NaN);
              child.Measure(ref childSize);
              // If width is not set, then just go on
              if (!double.IsNaN(totalSize.Width) && childSize.Width > totalSize.Width)
              {
                totalDesiredHeight += currentHeight;
                totalSize.Height -= currentHeight;
                totalSize.Width = initialWidth;
                currentHeight = 0;
              }
              totalSize.Width -= childSize.Width;

              if (childSize.Height > currentHeight)
                currentHeight = childSize.Height;
            }
            totalDesiredHeight += currentHeight;
          }
          break;

        case Orientation.Vertical:
          {
            totalDesiredHeight = totalSize.Height;
            foreach (FrameworkElement child in GetVisibleChildren())
            {
              SizeF childSize = new SizeF(float.NaN, totalDesiredHeight);
              child.Measure(ref childSize);
              if (!double.IsNaN(totalSize.Height) && childSize.Height > totalSize.Height)
              {
                totalDesiredWidth += currentWidth;
                totalSize.Width -= currentWidth;
                totalSize.Height = initialHeight;
                currentWidth = 0;
              }
              totalSize.Height -= childSize.Height;
              if (childSize.Width > currentWidth)
                currentWidth = childSize.Width;
            }
            totalDesiredWidth += currentWidth;
          }
          break;
      }

      return new SizeF(totalDesiredWidth, totalDesiredHeight);
    }

    protected void LayoutChildren(IList<FrameworkElement> children, int startIndex, int endIndex, PointF pos, float totalSize)
    {
      float offset = 0;
      for (int i = startIndex; i < endIndex; i++)
      {
        FrameworkElement layoutChild = children[i];
        SizeF desiredChildSize = layoutChild.DesiredSize;
        SizeF size;
        PointF location;

        if (Orientation == Orientation.Horizontal)
        {
          size = new SizeF(desiredChildSize.Width, totalSize);
          location = new PointF(pos.X + offset, pos.Y);
          ArrangeChildVertical(layoutChild, layoutChild.VerticalAlignment, ref location, ref size);
          offset += desiredChildSize.Width;
        }
        else
        {
          size = new SizeF(totalSize, desiredChildSize.Height);
          location = new PointF(pos.X, pos.Y + offset);
          ArrangeChildHorizontal(layoutChild, layoutChild.HorizontalAlignment, ref location, ref size);
          offset += desiredChildSize.Height;
        }

        layoutChild.Arrange(new RectangleF(location, size));
      }
    }

    protected override void ArrangeOverride()
    {
      base.ArrangeOverride();

      float offsetX = 0;
      float offsetY = 0;
      int startIndex = 0;
      IList<FrameworkElement> children = GetVisibleChildren();
      PointF actualPosition = ActualPosition;
      switch (Orientation)
      {
        case Orientation.Horizontal:
          {
            float totalHeight = 0;
            for (int currentIndex = 0; currentIndex < children.Count; currentIndex++)
            {
              FrameworkElement child = children[currentIndex];
              SizeF desiredChildSize = child.DesiredSize;
              offsetX += desiredChildSize.Width;
              if (offsetX > _innerRect.Width)
              {
                LayoutChildren(children, startIndex, currentIndex, new PointF(actualPosition.X, actualPosition.Y + offsetY), totalHeight);
                offsetX = desiredChildSize.Width;
                offsetY += totalHeight;
                totalHeight = 0;
                startIndex = currentIndex;
              }
              if (desiredChildSize.Height > totalHeight)
                totalHeight = desiredChildSize.Height;
            }
            if (startIndex < children.Count - 1)
              LayoutChildren(children, startIndex, children.Count - 1, new PointF(actualPosition.X, actualPosition.Y + offsetY), totalHeight);
          }
          break;

        case Orientation.Vertical:
          {
            float totalWidth = 0;
            for (int currentIndex = 0; currentIndex < children.Count; currentIndex++)
            {
              FrameworkElement child = children[currentIndex];
              SizeF desiredChildSize = child.DesiredSize;
              offsetY += desiredChildSize.Height;
              if (offsetY > _innerRect.Height)
              {
                LayoutChildren(children, startIndex, currentIndex, new PointF(actualPosition.X + offsetX, actualPosition.Y), totalWidth);
                offsetX += totalWidth;
                offsetY = desiredChildSize.Height;
                totalWidth = 0;
                startIndex = currentIndex;
              }
              if (desiredChildSize.Width > totalWidth)
                totalWidth = desiredChildSize.Width;
            }
            if (startIndex < children.Count - 1)
              LayoutChildren(children, startIndex, children.Count - 1, new PointF(actualPosition.X + offsetX, actualPosition.Y), totalWidth);
          }
          break;
      }
    }
  }
}

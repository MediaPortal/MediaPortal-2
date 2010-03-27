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
using SlimDX;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

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
      Orientation = copyManager.GetCopy(p.Orientation);
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

    public override void Measure(ref SizeF totalSize)
    {
      RemoveMargin(ref totalSize);

      SizeF availableSize = new SizeF((float) Width * SkinContext.Zoom.Width, (float) Height * SkinContext.Zoom.Height);
      if (double.IsNaN(availableSize.Width))
        availableSize.Width = totalSize.Width;
      if (double.IsNaN(availableSize.Height))
        availableSize.Height = totalSize.Height;

      float initialHeight = availableSize.Height;
      float initialWidth = availableSize.Width;

      _sizeCol.Clear();
      float totalWidth = 0;
      float totalHeight = 0;
      float currentHeight = 0;
      float currentWidth = 0;
      switch (Orientation)
      {
        case Orientation.Horizontal:
          {
            totalWidth = availableSize.Width;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible)
                continue;

              SizeF childSize = new SizeF(totalWidth, float.NaN);
              child.Measure(ref childSize);
              // If width is not set, then just go on
              if (!double.IsNaN(availableSize.Width) && childSize.Width > availableSize.Width)
              {
                _sizeCol.Add(currentHeight);
                totalHeight += currentHeight;
                availableSize.Height -= currentHeight;
                availableSize.Width = initialWidth;
                currentHeight = 0;
              }
              availableSize.Width -= childSize.Width;
              currentWidth += childSize.Width;

              if (childSize.Height > currentHeight)
                currentHeight = childSize.Height;
            }
            _sizeCol.Add(currentHeight);
            totalHeight += currentHeight;
          }
          break;

        case Orientation.Vertical:
          {
            totalHeight = availableSize.Height;
            foreach (FrameworkElement child in Children)
            {
              if (!child.IsVisible)
                continue;

              SizeF childSize = new SizeF(float.NaN, totalHeight);
              child.Measure(ref childSize);
              if (!double.IsNaN(availableSize.Height) && childSize.Height > availableSize.Height)
              {
                _sizeCol.Add(currentWidth);
                totalWidth += currentWidth;
                availableSize.Width -= currentWidth;
                availableSize.Height = initialHeight;
                currentWidth = 0;
              }
              availableSize.Height -= childSize.Height;
              currentHeight += childSize.Height;
              if (childSize.Width > currentWidth)
                currentWidth = childSize.Width;
            }
            _sizeCol.Add(currentWidth);
            totalWidth += currentWidth;
          }
          break;
      }
      
      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

      if (double.IsNaN(Width))
        _desiredSize.Width = totalWidth;

      if (double.IsNaN(Height))
        _desiredSize.Height = totalHeight;
      
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }

      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("WrapPanel.Measure: {0} returns {1}x{2}", Name, (int) totalSize.Width, (int) totalSize.Height));
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("WrapPanel.Arrange: {0} X {1} Y {2} W {3} H {4}", Name, (int) finalRect.X, (int) finalRect.Y, (int) finalRect.Width, (int) finalRect.Height));

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

              SizeF childSize = child.TotalDesiredSize();
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

              SizeF childSize = child.TotalDesiredSize();
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

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new RectangleF(finalRect.Location, finalRect.Size);
      }
      base.Arrange(finalRect);
    }
  }
}

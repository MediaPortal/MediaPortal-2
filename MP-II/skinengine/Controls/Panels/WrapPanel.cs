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
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using MediaPortal.Core.Properties;
using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using RectangleF = System.Drawing.RectangleF;
using SkinEngine.DirectX;
using SkinEngine.Controls.Visuals;

namespace SkinEngine.Controls.Panels
{
  public class WrapPanel : Panel
  {
    Property _orientationProperty;
    List<float> _sizeCol;
    /// <summary>
    /// Initializes a new instance of the <see cref="StackPanel"/> class.
    /// </summary>
    public WrapPanel()
    {
      Init();
    }
    public WrapPanel(WrapPanel v)
      : base(v)
    {
      Init();
    }

    void Init()
    {
      _orientationProperty = new Property(Orientation.Horizontal);
      _orientationProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
      _sizeCol = new List<float>();
    }

    public override object Clone()
    {
      return new WrapPanel(this);
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

    public override void Measure(SizeF availableSize)
    {
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
              child.Measure(new SizeF(0, childSize.Height));
              if (child.DesiredSize.Width > childSize.Width)
              {
                _sizeCol.Add(totalHeight);
                h += totalHeight;
                childSize.Height -= totalHeight;
                childSize.Width = _desiredSize.Width;
                totalHeight = 0.0f;
              }
              childSize.Width -= child.DesiredSize.Width;
              totalWidth += child.DesiredSize.Width;
              child.Measure(new SizeF(child.DesiredSize.Width, childSize.Height));
              if (child.DesiredSize.Height > totalHeight)
                totalHeight = child.DesiredSize.Height;

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
              child.Measure(new SizeF(childSize.Width, 0));
              if (child.DesiredSize.Height > childSize.Height)
              {
                _sizeCol.Add(totalWidth);
                w += totalWidth;
                childSize.Width -= totalWidth;
                childSize.Height = _desiredSize.Height;
                totalWidth = 0.0f;
              }
              childSize.Height -= child.DesiredSize.Height;
              totalHeight += child.DesiredSize.Height;
              child.Measure(new SizeF(child.DesiredSize.Width, childSize.Height));
              if (child.DesiredSize.Width > totalWidth)
                totalWidth = child.DesiredSize.Width;

            }
            _sizeCol.Add(totalWidth);
            w += totalWidth;
          }
          break;
      }
      if (Width > 0) w = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) h = (float)Height * SkinContext.Zoom.Height;
      _desiredSize = new SizeF((float)w, (float)h);


      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
      _originalSize = _desiredSize;


      base.Measure(availableSize);
    }
    public override void Arrange(RectangleF finalRect)
    {
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

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
      base.Arrange(layoutRect);
    }
  }
}

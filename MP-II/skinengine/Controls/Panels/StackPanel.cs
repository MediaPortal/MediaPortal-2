#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using SkinEngine.Controls.Visuals;
using System.Drawing;
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Panels
{
  public class StackPanel : Panel
  {
    Property _orientationProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StackPanel"/> class.
    /// </summary>
    public StackPanel()
    {
      Init();
    }
    public StackPanel(StackPanel v)
      : base(v)
    {
      Init();
    }
    void Init()
    {
      _orientationProperty = new Property(Orientation.Vertical);
      _orientationProperty.Attach(new PropertyChangedHandler(OnPropertyInvalidate));
    }
    public override object Clone()
    {
      return new StackPanel(this);
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
    public override void Measure(System.Drawing.Size availableSize)
    {
      float totalHeight = 0.0f;
      float totalWidth = 0.0f;
      Size childSize = availableSize;
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        child.Measure(childSize);
        if (Orientation == Orientation.Vertical)
        {
          childSize.Height -= child.DesiredSize.Height;
          totalHeight += child.DesiredSize.Height;
          if (child.DesiredSize.Width > totalWidth)
            totalWidth = child.DesiredSize.Width;
        }
        else
        {
          childSize.Width -= child.DesiredSize.Width;
          totalWidth += child.DesiredSize.Width;

          if (child.DesiredSize.Height > totalHeight)
            totalHeight = child.DesiredSize.Height;
        }
      }
      if (Width > 0) totalWidth = (float)Width;
      if (Height > 0) totalHeight = (float)Height;
      _desiredSize = new Size((int)totalWidth, (int)totalHeight);
      base.Measure(availableSize);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(Rectangle finalRect)
    {
      ActualPosition = new Microsoft.DirectX.Vector3(finalRect.Location.X + Margin.X, finalRect.Location.Y + Margin.Y, 1.0f); ;
      ActualWidth = finalRect.Width - (Margin.X + Margin.W);
      ActualHeight = finalRect.Height - (Margin.Y + Margin.Z);
      switch (Orientation)
      {
        case Orientation.Vertical:
          {
            float totalHeight = 0;
            foreach (UIElement child in Children)
            {
              if (!child.IsVisible) continue;
              Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + totalHeight));
              Size size = new Size(child.DesiredSize.Width, child.DesiredSize.Height);

              //align horizontally 
              if (AlignmentX == AlignmentX.Center)
              {
                location.X += (int)((finalRect.Width - child.DesiredSize.Width) / 2);
              }
              else if (AlignmentX == AlignmentX.Right)
              {
                location.X = finalRect.Right - child.DesiredSize.Width;
              }

              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (int)((finalRect.Height - DesiredSize.Height) / 2);
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += (int)(finalRect.Height - DesiredSize.Height);
              }

              child.Arrange(new Rectangle(location, size));
              totalHeight += child.DesiredSize.Height;
            }
          }
          break;

        case Orientation.Horizontal:
          {
            float totalWidth = 0;
            foreach (UIElement child in Children)
            {
              if (!child.IsVisible) continue;
              Point location = new Point((int)(this.ActualPosition.X + totalWidth), (int)(this.ActualPosition.Y));
              Size size = new Size(child.DesiredSize.Width, child.DesiredSize.Height);

              //align horizontally 
              if (DesiredSize.Width < finalRect.Width)
              {
                if (AlignmentX == AlignmentX.Center)
                {
                  location.X += (int)((finalRect.Width - DesiredSize.Width) / 2);
                }
                else if (AlignmentX == AlignmentX.Right)
                {
                  location.X += finalRect.Right - DesiredSize.Width;
                }
              }
              //align vertically 
              if (AlignmentY == AlignmentY.Center)
              {
                location.Y += (int)((finalRect.Height - child.DesiredSize.Height) / 2);
              }
              else if (AlignmentY == AlignmentY.Bottom)
              {
                location.Y += (int)(finalRect.Height - child.DesiredSize.Height);
              }

              child.Arrange(new Rectangle(location, size));
              totalWidth += child.DesiredSize.Width;
            }
          }
          break;
      }
      base.PerformLayout();
      base.Arrange(finalRect);
    }
  }
}

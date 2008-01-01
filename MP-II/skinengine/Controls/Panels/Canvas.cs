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
using System.Drawing;
using SkinEngine.Controls.Visuals;
using Rectangle = System.Drawing.Rectangle;

namespace SkinEngine.Controls.Panels
{
  public class Canvas : Panel
  {
    public Canvas()
    {
    }
    public Canvas(Canvas v)
      : base(v)
    {
    }

    public override object Clone()
    {
      return new Canvas(this);
    }
    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(Size availableSize)
    {
      Rectangle rect = new Rectangle(0, 0, 0, 0);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        child.Measure(availableSize);
        rect = Rectangle.Union(rect, new Rectangle(new Point((int)child.Position.X, (int)child.Position.Y), new Size((int)child.DesiredSize.Width, (int)child.DesiredSize.Height)));
      }
      if (Width > 0) rect.Width = (int)Width;
      if (Height > 0) rect.Height = (int)Height;
      _desiredSize = rect.Size;
      _desiredSize.Width += (int)(Margin.X + Margin.W);
      _desiredSize.Height += (int)(Margin.Y + Margin.Z);
      base.Measure(availableSize);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(Rectangle finalRect)
    {
      _availablePoint = new Point(finalRect.Location.X,finalRect.Location.Y);
      Rectangle layoutRect = new Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z);
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        Point p = new Point((int)(child.Position.X + this.ActualPosition.X), (int)(child.Position.Y + this.ActualPosition.Y));
        ArrangeChild(child, ref p);

        child.Arrange(new Rectangle(p, child.DesiredSize));
      }
      base.PerformLayout();
      base.Arrange(layoutRect);
    }
  }
}

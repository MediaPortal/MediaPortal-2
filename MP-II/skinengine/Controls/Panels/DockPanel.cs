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
using MediaPortal.Core.Properties;

namespace SkinEngine.Controls.Panels
{
  public class DockPanel : Panel
  {
    public override void Measure(Size availableSize)
    {
      foreach (UIElement child in Children)
      {
        child.Measure(availableSize);
      }
      if (Width > 0) availableSize.Width = (int)Width;
      if (Height > 0) availableSize.Height = (int)Height;
      _desiredSize = availableSize;
      base.Measure(availableSize);
    }

    public override void Arrange(Rectangle finalRect)
    {
      this.ActualPosition = new Microsoft.DirectX.Vector3(finalRect.X, finalRect.Y, 1.0f);
      this.ActualWidth = finalRect.Width;
      this.ActualHeight = finalRect.Height;

      float offsetTop = 0.0f;
      float offsetBottom = 0.0f;
      foreach (UIElement child in Children)
      {
        if (child.Dock == Dock.Top)
        {
          Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)this.ActualWidth, (int)child.DesiredSize.Height)));
          offsetTop += child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Bottom)
        {
          Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + ActualHeight - offsetBottom - child.DesiredSize.Height));
          child.Arrange(new Rectangle(location, new Size((int)this.ActualWidth, (int)child.DesiredSize.Height)));
          offsetBottom += child.DesiredSize.Height;
        }
      }
      float height = (float)(ActualHeight - (offsetBottom + offsetTop));
      float offsetLeft = 0.0f;
      float offsetRight = 0.0f;
      foreach (UIElement child in Children)
      {
        if (child.Dock == Dock.Left)
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)child.DesiredSize.Width, (int)height)));
          offsetLeft += child.DesiredSize.Width;
        }
        else if (child.Dock == Dock.Right)
        {
          Point location = new Point((int)(this.ActualPosition.X + ActualWidth - offsetRight - child.DesiredSize.Width), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)child.DesiredSize.Width, (int)height)));
          offsetRight += child.DesiredSize.Width;
        }
      }

      foreach (UIElement child in Children)
      {
        if (child.Dock == Dock.Center)
        {
          float width = (float)(ActualWidth - (offsetLeft + offsetRight));
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          child.Arrange(new Rectangle(location, new Size((int)width, (int)height)));
          offsetLeft += child.DesiredSize.Width;
        }
      }
      base.PerformLayout();
      base.Arrange(finalRect);
    }
  }
}

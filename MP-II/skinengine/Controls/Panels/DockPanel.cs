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
using Rectangle = System.Drawing.Rectangle;
namespace SkinEngine.Controls.Panels
{
  public class DockPanel : Panel
  {
    public DockPanel()
    {
    }
    public DockPanel(DockPanel v)
      : base(v)
    {
    }
    public override object Clone()
    {
      return new DockPanel(this);
    }
    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(Size availableSize)
    {
      _desiredSize = new System.Drawing.Size((int)Width, (int)Height);
      if (Width <= 0)
        _desiredSize.Width = (int)availableSize.Width - (int)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (int)availableSize.Height - (int)(Margin.Y + Margin.Z);

      System.Drawing.Size size = new Size(_desiredSize.Width, _desiredSize.Height);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;

        if (child.Dock == Dock.Top || child.Dock == Dock.Bottom)
        {
          child.Measure(size);
          size.Height -= child.DesiredSize.Height;
        }
      }
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;

        if (child.Dock == Dock.Left || child.Dock == Dock.Right)
        {
          child.Measure(size);
          size.Width -= child.DesiredSize.Width;
        }
      }
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;

        if (child.Dock == Dock.Center)
        {
          child.Measure(size);
          size.Width -= child.DesiredSize.Width;
          size.Height -= child.DesiredSize.Height;
        }
      }
      if (Width > 0) _desiredSize.Width = (int)Width;
      if (Height > 0) _desiredSize.Height = (int)Height;
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
      _finalRect = new System.Drawing.Rectangle(finalRect.Location, finalRect.Size);
      Rectangle layoutRect = new Rectangle(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (int)(Margin.X);
      layoutRect.Y += (int)(Margin.Y);
      layoutRect.Width -= (int)(Margin.X + Margin.W);
      layoutRect.Height -= (int)(Margin.Y + Margin.Z); ;
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      float offsetTop = 0.0f;
      float offsetBottom = 0.0f;
      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (child.Dock == Dock.Top)
        {
          Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + offsetTop));
          ArrangeChild(child, ref location, new Size((int)ActualWidth, 0));
          child.Arrange(new Rectangle(location, new Size((int)this.ActualWidth, (int)child.DesiredSize.Height)));
          offsetTop += child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Bottom)
        {
          Point location = new Point((int)(this.ActualPosition.X), (int)(this.ActualPosition.Y + ActualHeight - offsetBottom - child.DesiredSize.Height));
          ArrangeChild(child, ref location, new Size((int)ActualWidth, 0));
          child.Arrange(new Rectangle(location, new Size((int)this.ActualWidth, (int)child.DesiredSize.Height)));
          offsetBottom += child.DesiredSize.Height;
        }
      }
      float height = (float)(ActualHeight - (offsetBottom + offsetTop));
      float offsetLeft = 0.0f;
      float offsetRight = 0.0f;
      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (child.Dock == Dock.Left)
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          ArrangeChild(child, ref location, new Size(0, (int)height));
          child.Arrange(new Rectangle(location, new Size((int)child.DesiredSize.Width, (int)height)));
          offsetLeft += child.DesiredSize.Width;
        }
        else if (child.Dock == Dock.Right)
        {
          Point location = new Point((int)(this.ActualPosition.X + ActualWidth - offsetRight - child.DesiredSize.Width), (int)(this.ActualPosition.Y + offsetTop));
          ArrangeChild(child, ref location, new Size(0, (int)height));
          child.Arrange(new Rectangle(location, new Size((int)child.DesiredSize.Width, (int)height)));
          offsetRight += child.DesiredSize.Width;
        }
      }

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (child.Dock == Dock.Center)
        {
          float width = (float)(ActualWidth - (offsetLeft + offsetRight));
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          //ArrangeChild(child, ref location);
          child.Arrange(new Rectangle(location, new Size((int)width, (int)height)));
          offsetLeft += child.DesiredSize.Width;
        }
      }
      base.PerformLayout();
      base.Arrange(layoutRect);
    }

    protected virtual void ArrangeChild(FrameworkElement child, ref System.Drawing.Point p, Size s)
    {
      if (VisualParent == null) return;

      if (child.HorizontalAlignment == HorizontalAlignmentEnum.Center)
      {
        if (s.Width > 0)
          p.X += ((s.Width - child.DesiredSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        if (s.Width > 0)
          p.X += (s.Width - child.DesiredSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        if (s.Height > 0)
          p.Y += ((s.Height - child.DesiredSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        if (s.Height > 0)
          p.Y += (s.Height - child.DesiredSize.Height);
      }
    }
  }
}

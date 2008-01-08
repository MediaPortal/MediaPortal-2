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

        child.Measure(size);
        if (child.Dock == Dock.Top || child.Dock == Dock.Bottom)
        {
          size.Height -= child.TransformedSize.Height;
        }
        else if (child.Dock == Dock.Left || child.Dock == Dock.Right)
        {
          size.Width -= child.TransformedSize.Width;
        }
        else if (child.Dock == Dock.Center)
        {
          child.Measure(size);
          size.Width -= child.TransformedSize.Width;
          size.Height -= child.TransformedSize.Height;
        }
      }
      if (Width > 0) _desiredSize.Width = (int)Width;
      if (Height > 0) _desiredSize.Height = (int)Height;

      if (LayoutTransform != null)
      {
        Microsoft.DirectX.Matrix mNew;
        LayoutTransform.GetTransform(out mNew);
        mNew.M41 = 0;
        mNew.M42 = 0;
        float w = _desiredSize.Width;
        float h = _desiredSize.Height;
        float w1 = w * mNew.M11 + h * mNew.M21;
        float h1 = w * mNew.M12 + h * mNew.M22;
        _transformedSize = new Size((int)w1, (int)h1);

        _transformedSize.Width += (int)(Margin.X + Margin.W);
        _transformedSize.Height += (int)(Margin.Y + Margin.Z);
      }
      else
      {
        _desiredSize.Width += (int)(Margin.X + Margin.W);
        _desiredSize.Height += (int)(Margin.Y + Margin.Z);
        _transformedSize = _desiredSize;
      }

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
      float offsetLeft = 0.0f;
      float offsetRight = 0.0f;
      float offsetBottom = 0.0f;
      System.Drawing.Size size = new Size(layoutRect.Width, layoutRect.Height);
      int count = 0;
      foreach (FrameworkElement child in Children)
      {
        count++;
        if (!child.IsVisible) continue;
        if (child.Dock == Dock.Top)
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.Size(size.Width, 0));
          child.Arrange(new Rectangle(location, child.DesiredSize));
          offsetTop += child.TransformedSize.Height;
          size.Height -= child.TransformedSize.Height;
        }
        else if (child.Dock == Dock.Bottom)
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + layoutRect.Height - (offsetBottom + child.TransformedSize.Height)));
          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.Size(size.Width, 0));
          child.Arrange(new Rectangle(location, child.DesiredSize));
          offsetBottom += child.TransformedSize.Height;
          size.Height -= child.TransformedSize.Height;
        }
        else if (child.Dock == Dock.Left)
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.Size(0, size.Height));
          child.Arrange(new Rectangle(location, child.DesiredSize));
          offsetLeft += child.TransformedSize.Width;
          size.Width -= child.TransformedSize.Width;
        }
        else if (child.Dock == Dock.Right)
        {
          Point location = new Point((int)(this.ActualPosition.X + layoutRect.Width - (offsetRight + child.TransformedSize.Width)), (int)(this.ActualPosition.Y + offsetTop));

          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.Size(0, size.Height));
          child.Arrange(new Rectangle(location, child.DesiredSize));
          offsetRight += child.TransformedSize.Width;
          size.Width -= child.TransformedSize.Width;
        }
        else
        {
          Point location = new Point((int)(this.ActualPosition.X + offsetLeft), (int)(this.ActualPosition.Y + offsetTop));
          ArrangeChild(child, ref location, size);
          child.Arrange(new Rectangle(location, child.DesiredSize));
          offsetRight += child.TransformedSize.Width;
          size.Height -= child.TransformedSize.Height;
          size.Width -= child.TransformedSize.Width;
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
          child.Arrange(new Rectangle(location, child.DesiredSize));
          offsetLeft += child.TransformedSize.Width;
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
          p.X += ((s.Width - child.TransformedSize.Width) / 2);
      }
      else if (child.HorizontalAlignment == HorizontalAlignmentEnum.Right)
      {
        if (s.Width > 0)
          p.X += (s.Width - child.TransformedSize.Width);
      }
      if (child.VerticalAlignment == VerticalAlignmentEnum.Center)
      {
        if (s.Height > 0)
          p.Y += ((s.Height - child.TransformedSize.Height) / 2);
      }
      else if (child.VerticalAlignment == VerticalAlignmentEnum.Bottom)
      {
        if (s.Height > 0)
          p.Y += (s.Height - child.TransformedSize.Height);
      }
    }
  }
}

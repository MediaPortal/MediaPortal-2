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
    public override void Measure(SizeF availableSize)
    {
      _desiredSize = new System.Drawing.SizeF((float)Width, (float)Height);
      if (Width <= 0)
        _desiredSize.Width = (float)availableSize.Width - (float)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (float)availableSize.Height - (float)(Margin.Y + Margin.Z);

      System.Drawing.SizeF size = new SizeF(_desiredSize.Width, _desiredSize.Height);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;

        child.Measure(size);
        if (child.Dock == Dock.Top || child.Dock == Dock.Bottom)
        {
          size.Height -= child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Left || child.Dock == Dock.Right)
        {
          size.Width -= child.DesiredSize.Width;
        }
        else if (child.Dock == Dock.Center)
        {
          child.Measure(size);
          size.Width -= child.DesiredSize.Width;
          size.Height -= child.DesiredSize.Height;
        }
      }
      if (Width > 0) _desiredSize.Width = (float)Width;
      if (Height > 0) _desiredSize.Height = (float)Height;


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

      _desiredSize.Width += (float)(Margin.X + Margin.W);
      _desiredSize.Height += (float)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;

      base.Measure(availableSize);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(RectangleF finalRect)
    {
      _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z); ;
      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      float offsetTop = 0.0f;
      float offsetLeft = 0.0f;
      float offsetRight = 0.0f;
      float offsetBottom = 0.0f;
      System.Drawing.SizeF size = new SizeF(layoutRect.Width, layoutRect.Height);
      int count = 0;
      foreach (FrameworkElement child in Children)
      {
        count++;
        if (!child.IsVisible) continue;
        if (child.Dock == Dock.Top)
        {
          PointF location = new PointF((float)(this.ActualPosition.X + offsetLeft), (float)(this.ActualPosition.Y + offsetTop));
          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.SizeF(size.Width, 0));
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetTop += child.DesiredSize.Height;
          size.Height -= child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Bottom)
        {
          PointF location = new PointF((float)(this.ActualPosition.X + offsetLeft), (float)(this.ActualPosition.Y + layoutRect.Height - (offsetBottom + child.DesiredSize.Height)));
          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.SizeF(size.Width, 0));
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetBottom += child.DesiredSize.Height;
          size.Height -= child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Left)
        {
          PointF location = new PointF((float)(this.ActualPosition.X + offsetLeft), (float)(this.ActualPosition.Y + offsetTop));
          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.SizeF(0, size.Height));
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetLeft += child.DesiredSize.Width;
          size.Width -= child.DesiredSize.Width;
        }
        else if (child.Dock == Dock.Right)
        {
          PointF location = new PointF((float)(this.ActualPosition.X + layoutRect.Width - (offsetRight + child.DesiredSize.Width)), (float)(this.ActualPosition.Y + offsetTop));

          if (count == Children.Count)
            ArrangeChild(child, ref location, size);
          else
            ArrangeChild(child, ref location, new System.Drawing.SizeF(0, size.Height));
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetRight += child.DesiredSize.Width;
          size.Width -= child.DesiredSize.Width;
        }
        else
        {
          PointF location = new PointF((float)(this.ActualPosition.X + offsetLeft), (float)(this.ActualPosition.Y + offsetTop));
          ArrangeChild(child, ref location, size);
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetRight += child.DesiredSize.Width;
          size.Height -= child.DesiredSize.Height;
          size.Width -= child.DesiredSize.Width;
        }
      }

      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (child.Dock == Dock.Center)
        {
          float width = (float)(ActualWidth - (offsetLeft + offsetRight));
          PointF location = new PointF((float)(this.ActualPosition.X + offsetLeft), (float)(this.ActualPosition.Y + offsetTop));
          //ArrangeChild(child, ref location);
          child.Arrange(new RectangleF(location, child.DesiredSize));
          offsetLeft += child.DesiredSize.Width;
        }
      }
      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      _performLayout = true;
      base.Arrange(layoutRect);
    }

    protected virtual void ArrangeChild(FrameworkElement child, ref System.Drawing.PointF p, SizeF s)
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

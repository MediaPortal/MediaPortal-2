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
using SkinEngine.Rendering;
using MediaPortal.Presentation.Properties;
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
      System.Drawing.SizeF size = new SizeF(_desiredSize.Width, _desiredSize.Height);
      SizeF sizeTop = new SizeF();
      SizeF sizeLeft = new SizeF();
      SizeF sizeCenter = new SizeF();
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        if (size.Width < 0) size.Width = 0;
        if (size.Height < 0) size.Height = 0;
        child.Measure(size);
        if (child.Dock == Dock.Top || child.Dock == Dock.Bottom)
        {
          size.Height -= child.DesiredSize.Height;
          sizeTop.Height += child.DesiredSize.Height;
          if (child.DesiredSize.Width > sizeTop.Width)
            sizeTop.Width = child.DesiredSize.Width;
        }
        else if (child.Dock == Dock.Left || child.Dock == Dock.Right)
        {
          size.Width -= child.DesiredSize.Width;
          sizeLeft.Width += child.DesiredSize.Width;
          if (child.DesiredSize.Height > sizeLeft.Height)
            sizeLeft.Height = child.DesiredSize.Height;
        }
        else if (child.Dock == Dock.Center)
        {
          child.Measure(size);
          size.Width -= child.DesiredSize.Width;
          size.Height -= child.DesiredSize.Height;
          if (child.DesiredSize.Width > sizeCenter.Width)
            sizeCenter.Width = child.DesiredSize.Width;
          if (child.DesiredSize.Height > sizeCenter.Height)
            sizeCenter.Height = child.DesiredSize.Height;
        }
      }

      if (availableSize.Width == 0)
      {
        _desiredSize.Width = sizeLeft.Width;
        float w = Math.Max(sizeTop.Width, sizeCenter.Width);
        if (w > sizeLeft.Width)
          _desiredSize.Width = w;
      }
      if (availableSize.Height == 0)
      {
        _desiredSize.Height = sizeTop.Height + Math.Max(sizeLeft.Height, sizeCenter.Height);
      }

      if (Width > 0) _desiredSize.Width = (float)Width * SkinContext.Zoom.Width;
      if (Height > 0) _desiredSize.Height = (float)Height * SkinContext.Zoom.Height;
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

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(RectangleF finalRect)
    {
      //_finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
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

          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += (float)this.ActualPosition.X;
          location.Y += (float)this.ActualPosition.Y;
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
          PointF location = new PointF(offsetLeft, layoutRect.Height - (offsetBottom + child.DesiredSize.Height));
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += (float)this.ActualPosition.X;
          location.Y += (float)this.ActualPosition.Y;
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
          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += (float)this.ActualPosition.X;
          location.Y += (float)this.ActualPosition.Y;

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
          PointF location = new PointF(layoutRect.Width - (offsetRight + child.DesiredSize.Width), offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += (float)this.ActualPosition.X;
          location.Y += (float)this.ActualPosition.Y;

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
          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += (float)this.ActualPosition.X;
          location.Y += (float)this.ActualPosition.Y;
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

          PointF location = new PointF(offsetLeft, offsetTop);
          SkinContext.FinalLayoutTransform.TransformPoint(ref location);
          location.X += (float)this.ActualPosition.X;
          location.Y += (float)this.ActualPosition.Y;
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

      if (!finalRect.IsEmpty)
      {
        if (_finalRect.Width != finalRect.Width || _finalRect.Height != _finalRect.Height)
          _performLayout = true;
        _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
        if (Window!=null) Window.Invalidate(this);
      }
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

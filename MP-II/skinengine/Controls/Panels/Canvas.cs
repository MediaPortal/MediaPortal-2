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
    along with MediaPortal II.  If not, see <http://www.gnrenu.org/licenses/>.
*/

#endregion
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SkinEngine.Controls.Visuals;
using RectangleF = System.Drawing.RectangleF;

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
      RectangleF rect = new RectangleF(0, 0, 0, 0);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        child.Measure(new Size(0, 0));
        rect = RectangleF.Union(rect, new RectangleF(new PointF(0, 0), new SizeF(child.DesiredSize.Width, child.DesiredSize.Height)));
      }
      _originalSize = _desiredSize;

      if (LayoutTransform != null)
      {
        SkinContext.RemoveLayoutTransform();
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);
      _desiredSize.Width += marginWidth;
      _desiredSize.Height += marginHeight;
      //Trace.WriteLine(String.Format("canvas.measure :{0} {1}x{2} returns {3}x{4}", this.Name, (int)availableSize.Width, (int)availableSize.Height, (int)_desiredSize.Width, (int)_desiredSize.Height));

      base.Measure(availableSize);
    }

    /// <summary>
    /// Arranges the UI element
    /// and positions it in the finalrect
    /// </summary>
    /// <param name="finalRect">The final size that the parent computes for the child element</param>
    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("canvas.arrange :{0} {1},{2} {3}x{4}", this.Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      RectangleF layoutRect = new RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X * SkinContext.Zoom.Width);
      layoutRect.Y += (float)(Margin.Y * SkinContext.Zoom.Height);
      layoutRect.Width -= (float)((Margin.X + Margin.W) * SkinContext.Zoom.Width);
      layoutRect.Height -= (float)((Margin.Y + Margin.Z) * SkinContext.Zoom.Height);
      //SkinContext.FinalLayoutTransform.TransformRect(ref layoutRect);

      ActualPosition = new SlimDX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;

      if (LayoutTransform != null)
      {
        ExtendedMatrix m = new ExtendedMatrix();
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }


      foreach (FrameworkElement child in Children)
      {
        if (!child.IsVisible) continue;
        PointF p = new PointF((child.Position.X * SkinContext.Zoom.Width), (child.Position.Y * SkinContext.Zoom.Height));
        SkinContext.FinalLayoutTransform.TransformPoint(ref p);
        p.X += (float)this.ActualPosition.X;
        p.Y += (float)this.ActualPosition.Y;

        SizeF s = child.DesiredSize;

        child.Arrange(new RectangleF(p, s));
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

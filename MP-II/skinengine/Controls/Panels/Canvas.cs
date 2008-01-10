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
      _desiredSize = new System.Drawing.SizeF((float)Width, (float)Height);
      if (Width <= 0)
        _desiredSize.Width = (float)availableSize.Width - (float)(Margin.X + Margin.W);
      if (Height <= 0)
        _desiredSize.Height = (float)availableSize.Height - (float)(Margin.Y + Margin.Z);

      RectangleF rect = new RectangleF(0, 0, 0, 0);
      foreach (UIElement child in Children)
      {
        if (!child.IsVisible) continue;
        child.Measure(_desiredSize);
        rect = RectangleF.Union(rect, new RectangleF(new PointF(child.Position.X, child.Position.Y), new SizeF(child.DesiredSize.Width, child.DesiredSize.Height)));
      }
      if (Width > 0) rect.Width = (float)Width;
      if (Height > 0) rect.Height = (float)Height;
      _desiredSize = rect.Size;
      _desiredSize.Width += (float)(Margin.X + Margin.W);
      _desiredSize.Height += (float)(Margin.Y + Margin.Z);
      _originalSize = _desiredSize;

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
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      //SkinContext.FinalLayoutTransform.TransformRect(ref layoutRect);

      ActualPosition = new Microsoft.DirectX.Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
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
        PointF p = new PointF((child.Position.X), (child.Position.Y));
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
      _performLayout = true;
      base.Arrange(layoutRect);
    }

  }
}

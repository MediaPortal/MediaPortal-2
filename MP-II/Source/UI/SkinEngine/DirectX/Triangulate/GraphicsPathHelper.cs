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
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MediaPortal.SkinEngine.DirectX.Triangulate
{
  public class GraphicsPathHelper
  {
    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    public static GraphicsPath CreateRoundedRectPath(RectangleF baseRect, float radiusX, float radiusY,
        ExtendedMatrix layoutTransform)
    {
      GraphicsPath result = new GraphicsPath();
      if (radiusX <= 0.0f && radiusY <= 0.0f || baseRect.Width == 0 || baseRect.Height == 0)
      {
        // if corner radius is less than or equal to zero, 
        // return the original rectangle 
        result.AddRectangle(baseRect);
        result.CloseFigure();
      }
      else if (radiusX >= Math.Min(baseRect.Width, baseRect.Height) / 2.0)
        // if the corner radius is greater than or equal to half the width, or height (whichever is shorter) 
        // then return a capsule instead of a lozenge
        result = CreateCapsulePath(baseRect);
      else
      {
        // create the arc for the rectangle sides and declare a graphics path object for the drawing 
        float diameter = radiusX * 2.0F;
        SizeF sizeF = new SizeF(diameter, diameter);
        RectangleF arc = new RectangleF(baseRect.Location, sizeF);

        // top left arc 
        result.AddArc(arc, 180, 90);

        // top right arc 
        arc.X = baseRect.Right - diameter;
        result.AddArc(arc, 270, 90);

        // bottom right arc 
        arc.Y = baseRect.Bottom - diameter;
        result.AddArc(arc, 0, 90);

        // bottom left arc
        arc.X = baseRect.Left;
        result.AddArc(arc, 90, 90);

        result.CloseFigure();
      }
      if (layoutTransform != null)
      {
        Matrix mtx = new Matrix();
        mtx.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
        mtx.Multiply(layoutTransform.Get2dMatrix(), MatrixOrder.Append);
        mtx.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
        result.Transform(mtx);
      }
      result.Flatten();
      return result;
    }

    /// <summary>
    /// Gets the desired Capsular path.
    /// </summary>
    public static GraphicsPath CreateCapsulePath(RectangleF baseRect)
    {
      RectangleF arc;
      GraphicsPath path = new GraphicsPath();
      try
      {
        float diameter;
        if (baseRect.Width > baseRect.Height)
        {
          // return horizontal capsule 
          diameter = baseRect.Height;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 90, 180);
          arc.X = baseRect.Right - diameter;
          path.AddArc(arc, 270, 180);
        }
        else if (baseRect.Width < baseRect.Height)
        {
          // return vertical capsule 
          diameter = baseRect.Width;
          SizeF sizeF = new SizeF(diameter, diameter);
          arc = new RectangleF(baseRect.Location, sizeF);
          path.AddArc(arc, 180, 180);
          arc.Y = baseRect.Bottom - diameter;
          path.AddArc(arc, 0, 180);
        }
        else
          // return circle 
          path.AddEllipse(baseRect);
      }
      catch (Exception)
      {
        path.AddEllipse(baseRect);
      }
      finally
      {
        path.CloseFigure();
      }
      return path;
    }
  }
}
#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using SlimDX;

namespace MediaPortal.UI.SkinEngine
{
  /// <summary>
  /// Matrix class with extended functionality (transformations, inverting, ...).
  /// </summary>
  public class ExtendedMatrix
  {
    #region variables

    public Matrix Matrix;
    #endregion

    /// <summary>
    /// Initializes a new <see cref="ExtendedMatrix"/> which represents an identity matrix.
    /// </summary>
    public ExtendedMatrix()
    {
      Matrix = Matrix.Identity;
    }

    /// <summary>
    /// Multiplies the specified matrix.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns></returns>
    public ExtendedMatrix Multiply(ExtendedMatrix matrix)
    {
      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix = Matrix * matrix.Matrix;
      return m;
    }

    public void TransformPoint(ref System.Drawing.PointF p)
    {
      float w = p.X;
      float h = p.Y;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      p.X = w1;
      p.Y = h1;
    }

    public void TransformPoint(ref System.Drawing.Point p)
    {
      float w = p.X;
      float h = p.Y;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      p.X = (int) w1;
      p.Y = (int) h1;
    }


    public void TransformSize(ref System.Drawing.SizeF size)
    {
      if (Double.IsNaN(size.Width) || Double.IsNaN(size.Height))
        return;
      float w = size.Width;
      float h = size.Height;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      size.Width = w1;
      size.Height = h1;
    }

    public void TransformRect(ref System.Drawing.RectangleF rect)
    {
      float w = rect.Width;
      float h = rect.Height;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.Width = w1;
      rect.Height = h1;

      w = rect.X;
      h = rect.Y;
      w1 = w * Matrix.M11 + h * Matrix.M21;
      h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.X = w1;
      rect.Y = h1;
    }

    public void InvertSize(ref System.Drawing.SizeF size)
    {
      Matrix inverse = Matrix.Invert(Matrix);
      float w1 = size.Width * inverse.M11 + size.Height * inverse.M21;
      float h1 = size.Width * inverse.M12 + size.Height * inverse.M22;
      size.Width = w1;
      size.Height = h1;
    }

    public void InvertXY(ref float x, ref float y)
    {
      Matrix inverse = Matrix.Invert(Matrix);
      float w1 = x * inverse.M11 + y * inverse.M21;
      float h1 = x * inverse.M12 + y * inverse.M22;
      x = w1;
      y = h1;
    }

    public void TransformRectLocation(ref System.Drawing.Rectangle rect)
    {
      float w = rect.X;
      float h = rect.Y;
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      rect.X = (int)w1;
      rect.Y = (int)h1;
    }

    public void TransformXY(ref float w, ref float h)
    {
      float w1 = w * Matrix.M11 + h * Matrix.M21;
      float h1 = w * Matrix.M12 + h * Matrix.M22;
      w = w1;
      h = h1;
    }

    public void TransformXY(ref Vector2 vector)
    {
      float w1 = vector.X * Matrix.M11 + vector.Y * Matrix.M21;
      float h1 = vector.X * Matrix.M12 + vector.Y * Matrix.M22;
      vector.X = w1;
      vector.Y = h1;
    }
    public Vector3 Transform(Vector3 vector)
    {
      float w1 = vector.X * Matrix.M11 + vector.Y * Matrix.M21;
      float h1 = vector.X * Matrix.M12 + vector.Y * Matrix.M22;
      vector.X = w1;
      vector.Y = h1;
      return vector;
    }

    public System.Drawing.Drawing2D.Matrix Get2dMatrix()
    {
      return new System.Drawing.Drawing2D.Matrix(Matrix.M11, Matrix.M12, Matrix.M21, Matrix.M22, 0, 0);
    }

  } ;
}

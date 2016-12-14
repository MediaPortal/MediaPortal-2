#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using SharpDX;

namespace MediaPortal.UI.SkinEngine
{
  /// <summary>
  /// Extended functionality for the <see cref="Matrix"/> (transformations, inverting, ...).
  /// </summary>
  public static class MatrixOperations
  {
    public static Matrix Clone(this Matrix matrix)
    {
      return new Matrix
        {
          M11 = matrix.M11, M12 = matrix.M12, M13 = matrix.M13, M14 = matrix.M14,
          M21 = matrix.M21, M22 = matrix.M22, M23 = matrix.M23, M24 = matrix.M24,
          M31 = matrix.M31, M32 = matrix.M32, M33 = matrix.M33, M34 = matrix.M34,
          M41 = matrix.M41, M42 = matrix.M42, M43 = matrix.M43, M44 = matrix.M44,
        };
    }

    public static Matrix Scale(this Matrix matrix, float x, float y)
    {
      return matrix * new Matrix { M11 = x, M22 = y };
    }

    /// <summary>
    /// Returns a copy of this matrix with a removed translation part.
    /// </summary>
    /// <param name="matrix">The matrix to remove the translation part. The original matrix will not be changed.</param>
    /// <returns>Matrix with removed translation part.</returns>
    public static Matrix RemoveTranslation(this Matrix matrix)
    {
      return new Matrix
        {
          M11 = matrix.M11,
          M12 = matrix.M12,

          M21 = matrix.M21,
          M22 = matrix.M22,

          M33 = 1,
          M44 = 1
        };
    }

    ///// <summary>
    ///// Transforms the given point <paramref name="p"/> by this matrix.
    ///// </summary>
    ///// <param name="matrix">Transformation matrix.</param>
    ///// <param name="p">Point to transform.</param>
    //public static void Transform(this Matrix matrix, ref PointF p)
    //{
    //  Vector2 v = new Vector2(p.X, p.Y);
    //  matrix.Transform(ref v);
    //  p.X = v.X;
    //  p.Y = v.Y;
    //}

    /// <summary>
    /// Transforms the given point <paramref name="p"/> by this matrix.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="p">Point to transform. Will contain the transformed point after this method returns.</param>
    public static void Transform(this Matrix matrix, ref Point p)
    {
      Vector2 v = new Vector2(p.X, p.Y);
      matrix.Transform(ref v);
      p.X = (int) v.X;
      p.Y = (int) v.Y;
    }

    /// <summary>
    /// Transforms the point given by the coordinates <paramref name="x"/> and <paramref name="y"/> by this matrix.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="x">X coordinate of the point to transform. Will contain the transformed coordinate after
    /// this method returns.</param>
    /// <param name="y">Y coordinate of the point to transform. Will contain the transformed coordinate after
    /// this method returns.</param>
    public static void Transform(this Matrix matrix, ref float x, ref float y)
    {
      float w = x * matrix.M11 + y * matrix.M21 + matrix.M41;
      y = x * matrix.M12 + y * matrix.M22 + matrix.M42;
      x = w;
    }

    /// <summary>
    /// Transforms the given two-dimensional vector <paramref name="v"/> by this matrix.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="v">Vector to transform. Will contain the transformed vector after this method returns.</param>
    public static void Transform(this Matrix matrix, ref Vector2 v)
    {
      // DirectX uses row-major matrices, so we need to multiply the transposed matrix
      matrix.Transform(ref v.X, ref v.Y);
    }

    /// <summary>
    /// Transforms the size given by <paramref name="cx"/> and <paramref name="cy"/> by this matrix, ignoring translations.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="cx">Width to transform. Will contain the transformed coordinate after this method returns.</param>
    /// <param name="cy">Height to transform. Will contain the transformed coordinate after this method returns.</param>
    public static void TransformSize(this Matrix matrix, ref float cx, ref float cy)
    {
      float w = cx * matrix.M11 + cy * matrix.M21;
      cy = cx * matrix.M12 + cy * matrix.M22;
      cx = w;
    }

    /// <summary>
    /// Transforms a rect which is parallel to the coordinate axes, which has the given <paramref name="size"/>,
    /// by this matrix and returns the size of the transformed rectangle.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="size">Size of the rectangle to transform. Will contain the size of the transformed rectangle after
    /// this method returns.</param>
    public static void TransformIncludingRectangleSize(this Matrix matrix, ref Size2F size)
    {
      Vector2 p0 = new Vector2(0, 0);
      Vector2 p1 = new Vector2(size.Width, 0);
      Vector2 p2 = new Vector2(size.Width, size.Height);
      Vector2 p3 = new Vector2(0, size.Height);
      matrix.Transform(ref p0);
      matrix.Transform(ref p1);
      matrix.Transform(ref p2);
      matrix.Transform(ref p3);
      size.Width = Math.Max(Math.Abs(p0.X - p2.X), Math.Abs(p1.X - p3.X));
      size.Height = Math.Max(Math.Abs(p0.Y - p2.Y), Math.Abs(p1.Y - p3.Y));
    }

    public static RectangleF GetIncludingTransformedRectangle(this Matrix matrix, RectangleF rectangle)
    {
      Vector2 p0 = new Vector2(rectangle.Left, rectangle.Top);
      Vector2 p1 = new Vector2(rectangle.Right, rectangle.Top);
      Vector2 p2 = new Vector2(rectangle.Right, rectangle.Bottom);
      Vector2 p3 = new Vector2(rectangle.Left, rectangle.Bottom);
      matrix.Transform(ref p0);
      matrix.Transform(ref p1);
      matrix.Transform(ref p2);
      matrix.Transform(ref p3);
      RectangleF result = new RectangleF(
          Math.Min(Math.Min(p0.X, p1.X), Math.Min(p2.X, p3.X)),
          Math.Min(Math.Min(p0.Y, p1.Y), Math.Min(p2.Y, p3.Y)),
          Math.Max(Math.Abs(p0.X - p2.X), Math.Abs(p1.X - p3.X)),
          Math.Max(Math.Abs(p0.Y - p2.Y), Math.Abs(p1.Y - p3.Y)));
      return result;
    }

    ///// <summary>
    ///// Transforms the given point <paramref name="p"/> by the inverse of this matrix.
    ///// </summary>
    ///// <param name="matrix">Transformation matrix.</param>
    ///// <param name="p">Point to transform. Will contain the transformed point after this method returns.</param>
    //public static void Invert(this Matrix matrix, ref PointF  p)
    //{
    //  Matrix inverse = Matrix.Invert(matrix);
    //  inverse.Transform(ref p);
    //}

    /// <summary>
    /// Transforms the given point <paramref name="p"/> by the inverse of this matrix.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="p">Point to transform. Will contain the transformed point after this method returns.</param>
    public static void Invert(this Matrix matrix, ref Point p)
    {
      Matrix inverse = Matrix.Invert(matrix);
      inverse.Transform(ref p);
    }

    /// <summary>
    /// Transforms the point given by the coordinates <paramref name="x"/> and <paramref name="y"/> by the inverse
    /// of this matrix.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="x">X coordinate of the point to transform. Will contain the transformed coordinate after
    /// this method returns.</param>
    /// <param name="y">Y coordinate of the point to transform. Will contain the transformed coordinate after
    /// this method returns.</param>
    public static void Invert(this Matrix matrix, ref float x, ref float y)
    {
      Matrix inverse = Matrix.Invert(matrix);
      inverse.Transform(ref x, ref y);
    }

    /// <summary>
    /// Transforms the given two-dimensional vector <paramref name="v"/> by the inverse of this matrix.
    /// </summary>
    /// <param name="matrix">Transformation matrix.</param>
    /// <param name="v">Vector to transform. Will contain the transformed vector after this method returns.</param>
    public static void Invert(this Matrix matrix, ref Vector2 v)
    {
      Matrix inverse = Matrix.Invert(matrix);
      inverse.Transform(ref v);
    }

    /// <summary>
    /// Gets a matrix of the <see cref="System.Drawing.Drawing2D"/> namespace out of this matrix.
    /// </summary>
    /// <param name="matrix">Matrix to convert.</param>
    /// <returns>Matrix of the 2D-namespace.</returns>
    public static System.Drawing.Drawing2D.Matrix Get2dMatrix(this Matrix matrix)
    {
      return new System.Drawing.Drawing2D.Matrix(matrix.M11, matrix.M12, matrix.M21, matrix.M22, 0, 0);
    }
  }
}

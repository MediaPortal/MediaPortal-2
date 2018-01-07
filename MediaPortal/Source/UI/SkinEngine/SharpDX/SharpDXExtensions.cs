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

using System.Runtime.CompilerServices;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace MediaPortal.UI.SkinEngine
{
  public static class SharpDXExtensions
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.RectangleF ToDrawingRectF(this RectangleF rectangleF)
    {
      return new System.Drawing.RectangleF(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.SizeF ToDrawingSizeF(this Size2F size2F)
    {
      return new System.Drawing.SizeF(size2F.Width, size2F.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.Size ToDrawingSize(this Size2 sizeF)
    {
      return new System.Drawing.Size(sizeF.Width, sizeF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static System.Drawing.Point ToDrawingPoint(this Point point)
    {
      return new System.Drawing.Point(point.X, point.Y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle ToRect(this System.Drawing.Rectangle rectangleF)
    {
      return new Rectangle(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2F ToSize2F(this System.Drawing.SizeF sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2 ToSize2(this System.Drawing.Size sizeF)
    {
      return new Size2(sizeF.Width, sizeF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2 ToSize(this Size2F sizeF)
    {
      return new Size2((int)sizeF.Width, (int)sizeF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2F ToSize2F(this Size2 sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2F ToSize2F(this Rectangle rect)
    {
      return new Size2F(rect.Width, rect.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this Size2F sizeF)
    {
      return sizeF.Width == 0.0 && sizeF.Height == 0.0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle CreateRectangle(Vector2 location, Size2 size)
    {
      return new Rectangle((int)location.X, (int)location.Y, size.Width, size.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF CreateRectangleF(Vector2 location, Size2 size)
    {
      return new RectangleF(location.X, location.Y, size.Width, size.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF CreateRectangleF(Vector2 location, Size2F size)
    {
      return new RectangleF(location.X, location.Y, size.Width, size.Height);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float X(this RawRectangleF rect)
    {
      return rect.Left;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Y(this RawRectangleF rect)
    {
      return rect.Top;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2F Size(this RawRectangleF rect)
    {
      return new Size2F(rect.Width(), rect.Height());
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Width(this RawRectangleF rect)
    {
      return rect.Right - rect.Left;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Height(this RawRectangleF rect)
    {
      return rect.Top - rect.Bottom;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RectangleF ToRectangleF(this RawRectangleF rect)
    {
      return new RectangleF(rect.X(), rect.Y(), rect.Width(), rect.Height());
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawMatrix3x2 ToMatrix3x2(this RawMatrix matrix)
    {
      return new RawMatrix3x2(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawVector2 TopLeft(this RawRectangleF rect)
    {
      return new Vector2(rect.Left, rect.Top);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawVector2 Location(this RawRectangleF rect)
    {
      return new Vector2(rect.X(), rect.Y());
    }
  }
}

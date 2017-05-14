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

using SharpDX;
using SharpDX.Mathematics.Interop;

namespace MediaPortal.UI.SkinEngine
{
  public static class SharpDXExtensions
  {
    public static System.Drawing.RectangleF ToDrawingRectF(this RectangleF rectangleF)
    {
      return new System.Drawing.RectangleF(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    public static System.Drawing.SizeF ToDrawingSizeF(this Size2F size2F)
    {
      return new System.Drawing.SizeF(size2F.Width, size2F.Height);
    }
    public static System.Drawing.Size ToDrawingSize(this Size2 sizeF)
    {
      return new System.Drawing.Size(sizeF.Width, sizeF.Height);
    }
    public static System.Drawing.Point ToDrawingPoint(this Point point)
    {
      return new System.Drawing.Point(point.X, point.Y);
    }
    public static Rectangle ToRect(this System.Drawing.Rectangle rectangleF)
    {
      return new Rectangle(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    public static Size2F ToSize2F(this System.Drawing.SizeF sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    public static Size2 ToSize2(this System.Drawing.Size sizeF)
    {
      return new Size2(sizeF.Width, sizeF.Height);
    }
    public static Size2 ToSize(this Size2F sizeF)
    {
      return new Size2((int)sizeF.Width, (int)sizeF.Height);
    }
    public static Size2F ToSize2F(this Size2 sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    public static bool IsEmpty(this Size2F sizeF)
    {
      return sizeF.Width == 0.0 && sizeF.Height == 0.0;
    }
    public static Rectangle CreateRectangle(Vector2 location, Size2 size)
    {
      return new Rectangle((int)location.X, (int)location.Y, size.Width, size.Height);
    }

    public static RawRectangleF CreateRawRectangleF(Vector2 location, Size2F size)
    {
      return new RawRectangleF(location.X, location.Y, size.Width, size.Height);
    }

    public static RawRectangle CreateRawRectangle(Vector2 location, Size2 size)
    {
      return new RawRectangle((int)location.X, (int)location.Y, size.Width, size.Height);
    }

    public static float Width(this RawRectangleF rect)
    {
      return rect.Right - rect.Left;
    }

    public static float Height(this RawRectangleF rect)
    {
      return rect.Bottom - rect.Top;
    }

    public static Vector2 Center(this RawRectangleF rect)
    {
      return new Vector2(rect.Left + (rect.Width() / 2), rect.Top + (rect.Height() / 2));
    }

    public static Size2F Size(this RawRectangleF rect)
    {
      return new Size2F(rect.Width(), rect.Height());
    }

    public static Vector2 Location(this RawRectangleF rect)
    {
      return new Vector2(rect.Left, rect.Top);
    }

    public static RawRectangleF Union(this RawRectangleF rect1, RawRectangleF rect2)
    {
      return new RawRectangleF();
    }
  }
}

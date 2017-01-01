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
using System.Drawing.Drawing2D;
using System.Linq;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;

namespace MediaPortal.UI.SkinEngine.DirectX.Triangulate
{
  public class GraphicsPathHelper
  {
    public static void Flatten(PositionColoredTextured[][] subPathVerts, out PositionColoredTextured[] verts)
    {
      int numVertices = subPathVerts.Where(t => t != null).Sum(t => t.Length);
      if (numVertices == 0)
      {
        verts = null;
        return;
      }
      verts = new PositionColoredTextured[numVertices];
      long offset = 0;
      foreach (PositionColoredTextured[] spv in subPathVerts)
      {
        if (spv == null)
          continue;
        long length = spv.Length;
        Array.Copy(spv, 0, verts, offset, length);
        offset += length;
      }
    }

    /// <summary>
    /// Creates a closed rectangular <see cref="GraphicsPath"/> with rounded edges.
    /// </summary>
    /// <param name="baseRect">The rect which surrounds the created path.</param>
    /// <param name="radiusX">The X radius of the rounded edges.</param>
    /// <param name="radiusY">The Y radius of the rounded edges.</param>
    public static GraphicsPath CreateRoundedRectPath(RectangleF baseRect, float radiusX, float radiusY)
    {
      return CreateRoundedRectWithTitleRegionPath(baseRect, radiusX, radiusY, false, 0f, 0f);
    }

    /// <summary>
    /// Creates a rectangular <see cref="GraphicsPath"/> with rounded edges, optionally with an open title
    /// region specified by the parameters <paramref name="titleInset"/> and <paramref name="titleWidth"/>.
    /// </summary>
    /// <param name="baseRect">The rect which surrounds the created path.</param>
    /// <param name="radiusX">The X radius of the rounded edges.</param>
    /// <param name="radiusY">The Y radius of the rounded edges.</param>
    /// <param name="withTitleRegion">If set to <c>true</c>, a title region will be left out.</param>
    /// <param name="titleInset">Inset of the title region behind the corner. This parameter will only be used if
    /// <paramref name="withTitleRegion"/> is set to <c>true</c>.</param>
    /// <param name="titleWidth">Width of the title region to leave out. This parameter will only be used if
    /// <paramref name="withTitleRegion"/> is set to <c>true</c>.</param>
    public static GraphicsPath CreateRoundedRectWithTitleRegionPath(RectangleF baseRect, float radiusX, float radiusY,
        bool withTitleRegion, float titleInset, float titleWidth)
    {
      GraphicsPath result = new GraphicsPath();
      if (radiusX <= 0.0f && radiusY <= 0.0f || baseRect.Width == 0 || baseRect.Height == 0)
      {
        // if corner radius is less than or equal to zero, return the original rectangle
        if (withTitleRegion)
        { // If we should leave out a title region, we need to do it manually, because we need to start next to the
          // title.

          titleWidth = Math.Min(titleWidth, baseRect.Width - 2 * titleInset);
          // Right from the title to the upper right edge
          result.AddLine(baseRect.Left + 2* titleInset + titleWidth, baseRect.Top,
              baseRect.Right, baseRect.Top);
          // Upper right edge to lower right edge
          result.AddLine(baseRect.Right, baseRect.Top,
              baseRect.Right, baseRect.Bottom);
          // Lower right edge to lower left edge
          result.AddLine(baseRect.Right, baseRect.Bottom,
              baseRect.Left, baseRect.Bottom);
          // Lower left edge to upper left edge
          result.AddLine(baseRect.Left, baseRect.Bottom,
              baseRect.Left, baseRect.Top);
          // Upper left edge to the left side of the title
          result.AddLine(baseRect.Left, baseRect.Top, baseRect.Left + titleInset, baseRect.Top);
        }
        else
          result.AddRectangle(baseRect.ToDrawingRectF());
      }
      else
      {
        if (radiusX >= baseRect.Width / 2f)
          radiusX = baseRect.Width/2f;
        if (radiusY >= baseRect.Height / 2f)
          radiusY = baseRect.Height/2f;
        // create the arc for the rectangle sides and declare a graphics path object for the drawing 
        SizeF sizeF = new SizeF(radiusX * 2f, radiusY * 2f);
        RectangleF arc = SharpDXExtensions.CreateRectangleF(baseRect.Location, sizeF);

        if (withTitleRegion)
        {
          titleWidth = Math.Min(titleWidth, baseRect.Width - 2 * (radiusX + titleInset));
          // Right of the title to the upper right edge
          result.AddLine(baseRect.Left + radiusX + titleInset + titleWidth, baseRect.Top,
              baseRect.Right - radiusX, baseRect.Top);
        }

        // Top right arc 
        arc.X = baseRect.Right - radiusX * 2f;
        result.AddArc(arc.ToDrawingRectF(), 270, 90);

        // Bottom right arc 
        arc.Y = baseRect.Bottom - radiusY * 2f;
        result.AddArc(arc.ToDrawingRectF(), 0, 90);

        // Bottom left arc
        arc.X = baseRect.Left;
        result.AddArc(arc.ToDrawingRectF(), 90, 90);

        // Top left arc 
        arc.Y = baseRect.Top;
        result.AddArc(arc.ToDrawingRectF(), 180, 90);

        if (withTitleRegion)
          // Upper left edge to the left side of the title
          result.AddLine(baseRect.Left + radiusX, baseRect.Top, baseRect.Left + radiusX + titleInset, baseRect.Top);
        else
          result.CloseFigure();
      }
      result.Flatten();
      return result;
    }
  }
}
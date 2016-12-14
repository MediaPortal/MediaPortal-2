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

using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Ellipse : Shape
  {
    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      // Setup brushes
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using (GraphicsPath path = GetEllipse(_innerRect.ToDrawingRectF()))
        {
          PositionColoredTextured[] verts;
          float centerX;
          float centerY;
          PointF[] pathPoints = path.PathPoints;
          TriangulateHelper.CalcCentroid(pathPoints, out centerX, out centerY);
          if (Fill != null)
          {
            TriangulateHelper.FillPolygon_TriangleList(pathPoints, centerX, centerY, 1, out verts);
            Fill.SetupBrush(this, ref verts, context.ZOrder, true);
            PrimitiveBuffer.SetPrimitiveBuffer(ref _fillContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _fillContext);

          if (Stroke != null && StrokeThickness > 0)
          {
            TriangulateHelper.TriangulateStroke_TriangleList(pathPoints, (float) StrokeThickness, true, 1, PenLineJoin.Bevel, out verts);
            Stroke.SetupBrush(this, ref verts, context.ZOrder, true);
            PrimitiveBuffer.SetPrimitiveBuffer(ref _strokeContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _strokeContext);
        }
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private static GraphicsPath GetEllipse(RectangleF baseRect)
    {
      GraphicsPath mPath = new GraphicsPath();
      mPath.AddEllipse(baseRect);
      mPath.CloseFigure();
      mPath.Flatten();
      return mPath;
    }
  }
}

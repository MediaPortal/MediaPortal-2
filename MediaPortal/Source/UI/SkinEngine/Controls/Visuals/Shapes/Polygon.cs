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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;
using Point = System.Drawing.Point;
using RectangleF = System.Drawing.RectangleF;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Polygon : Shape
  {
    #region Protected fields

    protected AbstractProperty _pointsProperty;

    #endregion

    #region Ctor

    public Polygon()
    {
      Init();
    }

    void Init()
    {
      _pointsProperty = new SProperty(typeof(PointCollection), new PointCollection());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Polygon p = (Polygon) source;
      Points = new PointCollection(p.Points);
    }

    #endregion

    #region Public properties

    public AbstractProperty PointsProperty
    {
      get { return _pointsProperty; }
    }

    public PointCollection Points
    {
      get { return (PointCollection)_pointsProperty.GetValue(); }
      set { _pointsProperty.SetValue(value); }
    }

    #endregion

    #region Layouting

    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      // Setup brushes
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using (GraphicsPath path = CalculateTransformedPath(GetPolygon(), _innerRect))
        {
          float centerX;
          float centerY;
          PointF[] pathPoints = path.PathPoints;
          TriangulateHelper.CalcCentroid(pathPoints, out centerX, out centerY);
          PositionColoredTextured[] verts;
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
            TriangulateHelper.TriangulateStroke_TriangleList(pathPoints, (float) StrokeThickness, true, 1, StrokeLineJoin, out verts);
            Stroke.SetupBrush(this, ref verts, context.ZOrder, true);
            PrimitiveBuffer.SetPrimitiveBuffer(ref _strokeContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _strokeContext);
        }
      }
    }

    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      using (GraphicsPath p = CalculateTransformedPath(GetPolygon(), new SharpDX.RectangleF(0, 0, 0, 0)))
      {
        RectangleF bounds = p.GetBounds();
        return new Size2F(bounds.Width, bounds.Height);
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private GraphicsPath GetPolygon()
    {
      Point[] points = new Point[Points.Count];
      for (int i = 0; i < Points.Count; ++i)
        points[i] = Points[i].ToDrawingPoint();
      GraphicsPath mPath = new GraphicsPath();
      mPath.AddPolygon(points);
      mPath.CloseFigure();

      mPath.Flatten();
      return mPath;
    }

    #endregion
  }
}

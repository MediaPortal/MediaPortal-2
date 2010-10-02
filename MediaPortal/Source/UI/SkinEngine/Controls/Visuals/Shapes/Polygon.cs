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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

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
      _pointsProperty = new SProperty(typeof(IList<Point>), new List<Point>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Polygon p = (Polygon) source;
      Points = new List<Point>(p.Points);
    }

    #endregion

    #region Public properties

    public AbstractProperty PointsProperty
    {
      get { return _pointsProperty; }
    }

    public IList<Point> Points
    {
      get { return (IList<Point>) _pointsProperty.GetValue(); }
      set { _pointsProperty.SetValue(value); }
    }

    /// <summary>
    /// Returns the geometry representing this <see cref="Shape"/> 
    /// </summary>
    /// <param name="rect">The rect to fit the shape into.</param>
    /// <returns>An array of vertices forming triangle list that defines this shape.</returns>
    public override PositionColored2Textured[] GetGeometry(RectangleF rect)
    {
      PositionColored2Textured[] verts;
      using (GraphicsPath path = GetPolygon())
      {
        float centerX;
        float centerY;
        TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
        TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
      }
      return verts;
    }


    #endregion

    #region Layouting

    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);
      
      // Setup brushes
      PositionColored2Textured[] verts;
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using (GraphicsPath path = GetPolygon())
        {
          float centerX;
          float centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          if (Fill != null)
          {
            TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
            Fill.SetupBrush(this, ref verts, context.ZOrder, true);
            SetPrimitiveContext(ref _fillContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            DisposePrimitiveContext(ref _fillContext);

          if (Stroke != null && StrokeThickness > 0)
          {
            TriangulateHelper.TriangulateStroke_TriangleList(path, (float) StrokeThickness, true, out verts, null);
            Stroke.SetupBrush(this, ref verts, context.ZOrder, true);
            SetPrimitiveContext(ref _strokeContext, ref verts, PrimitiveType.TriangleList);
          }
          else
            DisposePrimitiveContext(ref _strokeContext);
        }
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private GraphicsPath GetPolygon()
    {
      Point[] points = new Point[Points.Count];
      for (int i = 0; i < Points.Count; ++i)
        points[i] = Points[i];
      GraphicsPath mPath = new GraphicsPath();
      mPath.AddPolygon(points);
      mPath.CloseFigure();

      mPath.Flatten();
      return mPath;
    }

    #endregion
  }
}

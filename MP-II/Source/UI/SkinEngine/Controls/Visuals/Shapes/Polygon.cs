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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Polygon : Shape
  {
    #region Private fields

    Property _pointsProperty;

    #endregion

    #region Ctor

    public Polygon()
    {
      Init();
    }

    void Init()
    {
      _pointsProperty = new Property(typeof(IList<Point>), new List<Point>());
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Polygon p = (Polygon) source;
      foreach (Point pt in p.Points)
        Points.Add(copyManager.GetCopy(pt));
    }

    #endregion

    #region Public properties

    public Property PointsProperty
    {
      get { return _pointsProperty; }
    }

    public IList<Point> Points
    {
      get { return (IList<Point>)_pointsProperty.GetValue(); }
    }

    #endregion

    #region Layouting

    protected override void PerformLayout()
    {
      if (!_performLayout)
        return;
      base.PerformLayout();
      //Trace.WriteLine("Polygon.PerformLayout() " + this.Name);
      
      double w = ActualWidth;
      double h = ActualHeight;
      SizeF rectSize = new SizeF((float)w, (float)h);

      ExtendedMatrix m = new ExtendedMatrix();
      m.Matrix *= _finalLayoutTransform.Matrix;
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        m.Matrix *= em.Matrix;
      }
      m.InvertSize(ref rectSize);
      RectangleF rect = new RectangleF(ActualPosition.X, ActualPosition.Y, rectSize.Width, rectSize.Height);

      //Fill brush
      PositionColored2Textured[] verts;
      if (Fill != null || (Stroke != null && StrokeThickness > 0))
      {
        using (GraphicsPath path = GetPolygon(rect))
        {
          float centerX;
          float centerY;
          TriangulateHelper.CalcCentroid(path, out centerX, out centerY);
          if (Fill != null)
          {
            if (SkinContext.UseBatching)
            {
              TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
              _verticesCountFill = verts.Length / 3;
              Fill.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);
              if (_fillContext == null)
              {
                _fillContext = new PrimitiveContext(_verticesCountFill, ref verts);
                Fill.SetupPrimitive(_fillContext);
                RenderPipeline.Instance.Add(_fillContext);
              }
              else
                _fillContext.OnVerticesChanged(_verticesCountFill, ref verts);
            }
            else
            {
              if (_fillAsset == null)
              {
                _fillAsset = new VisualAssetContext("Polygon._fillContext:" + Name, Screen.Name);
                ContentManager.Add(_fillAsset);
              }
              TriangulateHelper.FillPolygon_TriangleList(path, centerX, centerY, out verts);
              if (verts != null)
              {
                _fillAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
                Fill.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);

                PositionColored2Textured.Set(_fillAsset.VertexBuffer, ref verts);
                _verticesCountFill = (verts.Length / 3);
              }
            }
          }

          if (Stroke != null && StrokeThickness > 0)
          {
            if (SkinContext.UseBatching)
            {
              TriangulateHelper.TriangulateStroke_TriangleList(path, (float) StrokeThickness, true, out verts, _finalLayoutTransform);
              _verticesCountBorder = (verts.Length / 3);
              Stroke.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);
              if (_strokeContext == null)
              {
                _strokeContext = new PrimitiveContext(_verticesCountBorder, ref verts);
                Stroke.SetupPrimitive(_strokeContext);
                RenderPipeline.Instance.Add(_strokeContext);
              }
              else
                _strokeContext.OnVerticesChanged(_verticesCountBorder, ref verts);
            }
            else
            {
              if (_borderAsset == null)
              {
                _borderAsset = new VisualAssetContext("Polygon._borderContext:" + Name, Screen.Name);
                ContentManager.Add(_borderAsset);
              }
              TriangulateHelper.TriangulateStroke_TriangleList(path, (float) StrokeThickness, true, out verts, _finalLayoutTransform);
              if (verts != null)
              {
                _borderAsset.VertexBuffer = PositionColored2Textured.Create(verts.Length);
                Stroke.SetupBrush(ActualBounds, FinalLayoutTransform, ActualPosition.Z, ref verts);

                PositionColored2Textured.Set(_borderAsset.VertexBuffer, ref verts);
                _verticesCountBorder = (verts.Length / 3);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Get the desired Rounded Rectangle path.
    /// </summary>
    private GraphicsPath GetPolygon(RectangleF baseRect)
    {
      Point[] points = new Point[Points.Count];
      for (int i = 0; i < Points.Count; ++i)
      {
        points[i] = Points[i];
      }
      GraphicsPath mPath = new GraphicsPath();
      mPath.AddPolygon(points);
      mPath.CloseFigure();

      Matrix m = new Matrix();
      m.Translate(-baseRect.X, -baseRect.Y, MatrixOrder.Append);
      m.Multiply(_finalLayoutTransform.Get2dMatrix(), MatrixOrder.Append);
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        m.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      mPath.Transform(m);

      mPath.Flatten();
      return mPath;
    }

    #endregion
  }
}

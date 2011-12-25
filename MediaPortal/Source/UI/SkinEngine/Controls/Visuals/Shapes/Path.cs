#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX.Triangulate;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;
using SlimDX.Direct3D9;
using FillMode=System.Drawing.Drawing2D.FillMode;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Path : Shape
  {
    #region Protected fields

    protected AbstractProperty _dataProperty;
    protected bool _fillDisabled;

    #endregion

    #region Ctor

    public Path()
    {
      Init();
    }

    void Init()
    {
      _dataProperty = new SProperty(typeof(string), string.Empty);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Path p = (Path) source;
      Data = p.Data;
    }

    #endregion

    public AbstractProperty DataProperty
    {
      get { return _dataProperty; }
    }

    public string Data
    {
      get { return (string)_dataProperty.GetValue(); }
      set { _dataProperty.SetValue(value); }
    }

    protected override void DoPerformLayout(RenderContext context)
    {
      base.DoPerformLayout(context);

      // Setup brushes
      if (Fill != null || ((Stroke != null && StrokeThickness > 0)))
      {
        using (GraphicsPath path = CalculateTransformedPath(_innerRect))
        {
          if (Fill != null && !_fillDisabled)
          {
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(path))
            {
              PositionColoredTextured[][] subPathVerts = new PositionColoredTextured[gpi.SubpathCount][];
              using (GraphicsPath subPath = new GraphicsPath())
              {
                for (int i = 0; i < subPathVerts.Length; i++)
                {
                  bool isClosed;
                  gpi.NextSubpath(subPath, out isClosed);
                  PointF[] pathPoints = subPath.PathPoints;
                  TriangulateHelper.Triangulate(pathPoints, 1, out subPathVerts[i]);
                }
              }
              PositionColoredTextured[] verts;
              GraphicsPathHelper.Flatten(subPathVerts, out verts);
              if (verts != null)
              {
                Fill.SetupBrush(this, ref verts, context.ZOrder, true);
                PrimitiveBuffer.SetPrimitiveBuffer(ref _fillContext, ref verts, PrimitiveType.TriangleList);
              }
            }
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _fillContext);

          if (Stroke != null && StrokeThickness > 0)
          {
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(path))
            {
              PositionColoredTextured[][] subPathVerts = new PositionColoredTextured[gpi.SubpathCount][];
              using (GraphicsPath subPath = new GraphicsPath())
              {
                for (int i = 0; i < subPathVerts.Length; i++)
                {
                  bool isClosed;
                  gpi.NextSubpath(subPath, out isClosed);
                  PointF[] pathPoints = subPath.PathPoints;
                  TriangulateHelper.TriangulateStroke_TriangleList(pathPoints, (float) StrokeThickness, isClosed, 1,
                      out subPathVerts[i]);
                }
              }
              PositionColoredTextured[] verts;
              GraphicsPathHelper.Flatten(subPathVerts, out verts);
              if (verts != null)
              {
                Stroke.SetupBrush(this, ref verts, context.ZOrder, true);
                PrimitiveBuffer.SetPrimitiveBuffer(ref _strokeContext, ref verts, PrimitiveType.TriangleList);
              }
            }
          }
          else
            PrimitiveBuffer.DisposePrimitiveBuffer(ref _strokeContext);
        }
      }
    }

    protected override SizeF CalculateInnerDesiredSize(SizeF totalSize)
    {
      using (GraphicsPath p = CalculateTransformedPath(new RectangleF(0, 0, 0, 0)))
      {
        RectangleF bounds = p.GetBounds();
        return new SizeF(bounds.Width, bounds.Height);
      }
    }

    protected GraphicsPath ParsePath()
    {
      GraphicsPath result = new GraphicsPath(FillMode.Alternate);
      PointF lastPoint = new PointF();
      Regex regex = new Regex(@"[a-zA-Z][-0-9\.,-0-9\. ]*");
      MatchCollection matches = regex.Matches(Data);

      foreach (Match match in matches)
      {
        char cmd = match.Value[0];
        PointF[] points;
        string pointsStr = match.Value.Substring(1).Trim();
        if (pointsStr.Length > 0)
        {
          string[] txtpoints = pointsStr.Split(new char[] { ',', ' ' });
          if (txtpoints.Length == 1)
          {
            points = new PointF[1];
            points[0].X = (float) TypeConverter.Convert(txtpoints[0], typeof(float));
          }
          else
          {
            int c = txtpoints.Length / 2;
            points = new PointF[c];
            for (int i = 0; i < c; i++)
            {
              points[i].X = (float) TypeConverter.Convert(txtpoints[i * 2], typeof(float));
              if (i + 1 < txtpoints.Length)
                points[i].Y = (float) TypeConverter.Convert(txtpoints[i * 2 + 1], typeof(float));
            }
          }
        }
        else
          points = new PointF[] {};
        switch (cmd)
        {
          case 'm':
            {
              //Relative origin
              PointF point = points[0];
              lastPoint = new PointF(lastPoint.X + point.X, lastPoint.Y + point.Y);
              result.StartFigure();
            }
            break;
          case 'M':
            {
              //Absolute origin
              lastPoint = points[0];
              result.StartFigure();
            }
            break;
          case 'L':
            //Absolute Line
            foreach (PointF t in points)
            {
              result.AddLine(lastPoint, t);
              lastPoint = t;
            }
            break;
          case 'l':
            //Relative Line
            for (int i = 0; i < points.Length; ++i)
            {
              points[i].X += lastPoint.X;
              points[i].Y += lastPoint.Y;
              result.AddLine(lastPoint, points[i]);
              lastPoint = points[i];
            }
            break;
          case 'H':
            {
              //Horizontal line to absolute X 
              PointF point1 = new PointF(points[0].X, lastPoint.Y);
              result.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'h':
            {
              //Horizontal line to relative X
              PointF point1 = new PointF(lastPoint.X + points[0].X, lastPoint.Y);
              result.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'V':
            {
              //Vertical line to absolute y 
              PointF point1 = new PointF(lastPoint.X, points[0].X);
              result.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'v':
            {
              //Vertical line to relative y
              PointF point1 = new PointF(lastPoint.X, lastPoint.Y + points[0].X);
              result.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'C':
            //Quadratic Bezier curve command C21,17,17,21,13,21
            for (int i = 0; i < points.Length; i += 3)
            {
              result.AddBezier(lastPoint, points[i], points[i + 1], points[i + 2]);
              lastPoint = points[i + 2];
            }
            break;
          case 'c':
            //Quadratic Bezier curve command
            for (int i = 0; i < points.Length; i += 3)
            {
              points[i].X += lastPoint.X;
              points[i].Y += lastPoint.Y;
              result.AddBezier(lastPoint, points[i], points[i + 1], points[i + 2]);
              lastPoint = points[i + 2];
            }
            break;
          case 'F':
            //Set fill mode command
            if (points[0].X == 0.0f)
            {
              //the EvenOdd fill rule
              //Rule that determines whether a point is in the fill region by drawing a ray 
              //from that point to infinity in any direction and counting the number of path 
              //segments within the given shape that the ray crosses. If this number is odd, 
              //the point is inside; if even, the point is outside.
              result.FillMode = FillMode.Alternate;
            }
            else if (points[0].X == 1.0f)
            {
              //the Nonzero fill rule.
              //Rule that determines whether a point is in the fill region of the 
              //path by drawing a ray from that point to infinity in any direction
              //and then examining the places where a segment of the shape crosses
              //the ray. Starting with a count of zero, add one each time a segment 
              //crosses the ray from left to right and subtract one each time a path
              //segment crosses the ray from right to left. After counting the crossings,
              //if the result is zero then the point is outside the path. Otherwise, it is inside.
              result.FillMode = FillMode.Winding;
            }
            break;
          case 'z':
            result.CloseFigure();
            break;
        }
      }
      return result;
    }

    protected GraphicsPath CalculateTransformedPath(RectangleF baseRect)
    {
      GraphicsPath result = ParsePath();
      using (Matrix m = new Matrix())
      {
        RectangleF bounds = result.GetBounds();
        _fillDisabled = bounds.Width < StrokeThickness || bounds.Height < StrokeThickness;
        if (Width > 0) baseRect.Width = (float) Width;
        if (Height > 0) baseRect.Height = (float) Height;
        float scaleW;
        float scaleH;
        if (Stretch == Stretch.Fill)
        {
          scaleW = baseRect.Width/bounds.Width;
          scaleH = baseRect.Height/bounds.Height;
          m.Translate(-bounds.X, -bounds.Y, MatrixOrder.Append);
        }
        else if (Stretch == Stretch.Uniform)
        {
          scaleW = Math.Min(baseRect.Width/bounds.Width, baseRect.Height/bounds.Height);
          scaleH = scaleW;
          m.Translate(-bounds.X, -bounds.Y, MatrixOrder.Append);
        }
        else if (Stretch == Stretch.UniformToFill)
        {
          scaleW = Math.Max(baseRect.Width/bounds.Width, baseRect.Height/bounds.Height);
          scaleH = scaleW;
          m.Translate(-bounds.X, -bounds.Y, MatrixOrder.Append);
        }
        else
        {
          // Stretch == Stretch.None
          scaleW = 1;
          scaleH = 1;
        }
        // In case bounds.Width or bounds.Height or baseRect.Width or baseRect.Height were 0
        if (scaleW == 0 || float.IsNaN(scaleW) || float.IsInfinity(scaleW)) scaleW = 1;
        if (scaleH == 0 || float.IsNaN(scaleH) || float.IsInfinity(scaleH)) scaleH = 1;
        m.Scale(scaleW, scaleH, MatrixOrder.Append);

        m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
        result.Transform(m);
        result.Flatten();
      }
      return result;
    }
  }
}

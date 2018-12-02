#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Text.RegularExpressions;
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using SharpDX.Direct2D1;
using RectangleF = SharpDX.RectangleF;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  public class Path : Shape
  {
    #region Protected fields

    static readonly Regex PARSE_REGEX = new Regex(@"[a-zA-Z][-0-9\.,-0-9\. ]*");

    protected AbstractProperty _dataProperty;

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
      Path p = (Path)source;
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
        using (PathGeometry pathRaw = ParsePath())
        {
          SetGeometry(CalculateTransformedPath(pathRaw, _innerRect));

          //var boundaries = _geometry.GetBounds();
          var fill = Fill;
          if (fill != null && !_fillDisabled)
            fill.SetupBrush(this, ref _innerRect, context.ZOrder, true);

          var stroke = Stroke;
          if (stroke != null)
            stroke.SetupBrush(this, ref _strokeRect, context.ZOrder, true);
        }
      }
      else
        SetGeometry(null);
    }

    protected override Size2F CalculateInnerDesiredSize(Size2F totalSize)
    {
      using (PathGeometry pathRaw = ParsePath())
      using (var p = CalculateTransformedPath(pathRaw, new RectangleF(0, 0, 0, 0)))
      {
        var bounds = p.GetBounds();
        return new Size2F(bounds.Width(), bounds.Height());
      }
    }

    protected static Vector2 ToVector2(Vector2 point)
    {
      return new Vector2(point.X, point.Y);
    }

    protected PathGeometry ParsePath()
    {
      PathGeometry result = new PathGeometry(GraphicsDevice11.Instance.RenderTarget2D.Factory);
      using (var sink = result.Open())
      {
        Vector2 lastPoint = new Vector2();
        Regex regex = new Regex(@"[a-zA-Z][-0-9\.,-0-9\. ]*");
        MatchCollection matches = regex.Matches(Data);

        bool hasOpenFigure = false;

        foreach (Match match in matches)
        {
          char cmd = match.Value[0];
          Vector2[] points;
          string pointsStr = match.Value.Substring(1).Trim();
          if (pointsStr.Length > 0)
          {
            string[] txtpoints = pointsStr.Split(new char[] { ',', ' ' });
            if (txtpoints.Length == 1)
            {
              points = new Vector2[1];
              points[0].X = (float)TypeConverter.Convert(txtpoints[0], typeof(float));
            }
            else
            {
              int c = txtpoints.Length / 2;
              points = new Vector2[c];
              for (int i = 0; i < c; i++)
              {
                points[i].X = (float)TypeConverter.Convert(txtpoints[i * 2], typeof(float));
                if (i + 1 < txtpoints.Length)
                  points[i].Y = (float)TypeConverter.Convert(txtpoints[i * 2 + 1], typeof(float));
              }
            }
          }
          else
            points = new Vector2[] { };

          switch (cmd)
          {
            case 'm':
              {
                //Relative origin
                Vector2 point = points[0];
                lastPoint = new Vector2(lastPoint.X + point.X, lastPoint.Y + point.Y);
                if (hasOpenFigure)
                  sink.EndFigure(FigureEnd.Open);
                sink.BeginFigure(ToVector2(lastPoint), FigureBegin.Filled);
                hasOpenFigure = true;
              }
              break;
            case 'M':
              {
                //Absolute origin
                lastPoint = points[0];
                if (hasOpenFigure)
                  sink.EndFigure(FigureEnd.Open);
                sink.BeginFigure(ToVector2(lastPoint), FigureBegin.Filled);
                hasOpenFigure = true;
              }
              break;
            case 'L':
              //Absolute Line
              foreach (Vector2 t in points)
              {
                sink.AddLine(ToVector2(t));
                //result.AddLine(lastPoint, t);
                lastPoint = t;
              }
              break;
            case 'l':
              //Relative Line
              for (int i = 0; i < points.Length; ++i)
              {
                points[i].X += lastPoint.X;
                points[i].Y += lastPoint.Y;
                sink.AddLine(ToVector2(points[i]));
                //result.AddLine(lastPoint, points[i]);
                lastPoint = points[i];
              }
              break;
            case 'H':
              {
                //Horizontal line to absolute X 
                Vector2 point1 = new Vector2(points[0].X, lastPoint.Y);
                sink.AddLine(ToVector2(point1));
                lastPoint = new Vector2(point1.X, point1.Y);
              }
              break;
            case 'h':
              {
                //Horizontal line to relative X
                Vector2 point1 = new Vector2(lastPoint.X + points[0].X, lastPoint.Y);
                sink.AddLine(ToVector2(point1));
                lastPoint = new Vector2(point1.X, point1.Y);
              }
              break;
            case 'V':
              {
                //Vertical line to absolute y 
                Vector2 point1 = new Vector2(lastPoint.X, points[0].X);
                sink.AddLine(ToVector2(point1));
                lastPoint = new Vector2(point1.X, point1.Y);
              }
              break;
            case 'v':
              {
                //Vertical line to relative y
                Vector2 point1 = new Vector2(lastPoint.X, lastPoint.Y + points[0].X);
                sink.AddLine(ToVector2(point1));
                lastPoint = new Vector2(point1.X, point1.Y);
              }
              break;
            case 'C':
              //Quadratic Bezier curve command C21,17,17,21,13,21
              for (int i = 0; i < points.Length; i += 3)
              {
                sink.AddBezier(new BezierSegment
                {
                  Point1 = ToVector2(points[i]),
                  Point2 = ToVector2(points[i + 1]),
                  Point3 = ToVector2(points[i + 2])
                });
                lastPoint = points[i + 2];
              }
              break;
            case 'c':
              //Quadratic Bezier curve command
              for (int i = 0; i < points.Length; i += 3)
              {
                points[i].X += lastPoint.X;
                points[i].Y += lastPoint.Y;
                sink.AddBezier(new BezierSegment
                {
                  Point1 = ToVector2(points[i]),
                  Point2 = ToVector2(points[i + 1]),
                  Point3 = ToVector2(points[i + 2])
                });
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
                sink.SetFillMode(FillMode.Alternate);
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
                sink.SetFillMode(FillMode.Winding);
              }
              break;
            case 'z':
              sink.EndFigure(FigureEnd.Closed);
              hasOpenFigure = false;
              break;
          }
        }
        if (hasOpenFigure)
        {
          sink.EndFigure(FigureEnd.Open);
        }

        sink.Close();
      }
      return result;
    }
  }
}

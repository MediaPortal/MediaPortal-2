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
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine;
using SkinEngine.DirectX;

using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;

using SlimDX;
using SlimDX.Direct3D;
using SlimDX.Direct3D9;
using GeometryUtility;

namespace SkinEngine.Controls.Visuals
{
  public class Path : Shape
  {
    Property _dataProperty;
    PrimitiveType _fillPrimitiveType;



    public Path()
    {
      Init();
    }

    public Path(Path s)
      : base(s)
    {
      Init();
      Data = s.Data;
    }

    public override object Clone()
    {
      return new Path(this);
    }

    void Init()
    {
      _dataProperty = new Property("");
    }


    /// <summary>
    /// Gets or sets the data property.
    /// </summary>
    /// <value>The data property.</value>
    public Property DataProperty
    {
      get
      {
        return _dataProperty;
      }
      set
      {
        _dataProperty = value;
      }
    }
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    /// <value>The data.</value>
    public string Data
    {
      get
      {
        return (string)_dataProperty.GetValue();
      }
      set
      {
        _dataProperty.SetValue(value);
      }
    }
    public override void DoRender()
    {
      if (String.IsNullOrEmpty(Data)) return;
      if (!IsVisible) return;
      if ((Fill != null && _vertexBufferFill == null) ||
           (Stroke != null && _vertexBufferBorder == null) || _performLayout)
      {
        PerformLayout();
        _performLayout = false;
      }

      if (Fill != null)
      {
        //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Fill.BeginRender(_vertexBufferFill, _verticesCountFill, _fillPrimitiveType))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferFill, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(_fillPrimitiveType, 0, _verticesCountFill);
          Fill.EndRender();
        }
      }
      if (Stroke != null && StrokeThickness > 0)
      {
        GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Stroke.BeginRender(_vertexBufferBorder, _verticesCountBorder, PrimitiveType.TriangleStrip))
        {
          GraphicsDevice.Device.SetStreamSource(0, _vertexBufferBorder, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, _verticesCountBorder);
          Stroke.EndRender();
        }
      }

      _lastTimeUsed = SkinContext.Now;
    }
    protected override void PerformLayout()
    {
      TimeSpan ts;
      DateTime now = DateTime.Now;
      Free();
      double w = ActualWidth;
      double h = ActualHeight;
      float centerX, centerY;
      SizeF rectSize = new SizeF((float)w, (float)h);
      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        m.InvertSize(ref rectSize);
      }
      System.Drawing.RectangleF rect = new System.Drawing.RectangleF((float)ActualPosition.X, (float)ActualPosition.Y, rectSize.Width, rectSize.Height);

      //Fill brush
      PositionColored2Textured[] verts;
      GraphicsPath path;
      if (Fill != null || ((Stroke != null && StrokeThickness > 0)))
      {
        bool isClosed;
        if (Fill != null)
        {
          using (path = GetPath(rect, _finalLayoutTransform, out isClosed, 0))
          {
            CalcCentroid(path, out centerX, out centerY);
            Trace.WriteLine(String.Format("Path.PerformLayout() {0} points: {1} closed:{2}", this.Name, path.PointCount, isClosed));
            if (Fill != null)
            {
              _vertexBufferFill = Triangulate(path, centerX, centerY, isClosed, out verts, out _fillPrimitiveType);
              if (_vertexBufferFill != null)
              {
                Fill.SetupBrush(this, ref verts);

                PositionColored2Textured.Set(_vertexBufferFill, ref verts);
                if (_fillPrimitiveType == PrimitiveType.TriangleList)
                  _verticesCountFill = (verts.Length / 3);
                else
                  _verticesCountFill = (verts.Length - 2);
              }
            }
          }
          ts = DateTime.Now - now;
          Trace.WriteLine(String.Format(" fill:{0}", ts.TotalMilliseconds));
        }
        if (Stroke != null && StrokeThickness > 0)
        {
          using (path = GetPath(rect, _finalLayoutTransform, out isClosed, (float)(StrokeThickness)))
          {
            PolygonDirection direction = PointsDirection(path);
            WidthMode mode = WidthMode.RightHanded;
            if (direction == PolygonDirection.Count_Clockwise)
              mode = WidthMode.LeftHanded;
            //_vertexBufferBorder = ConvertPathToTriangleStrip(path, (float)(StrokeThickness), isClosed, out verts);

            _vertexBufferBorder = CalculateLinePoints(path, (float)StrokeThickness, false, mode, out verts);
            if (_vertexBufferBorder != null)
            {
              Stroke.SetupBrush(this, ref verts);

              PositionColored2Textured.Set(_vertexBufferBorder, ref verts);
              _verticesCountBorder = verts.Length - 2;// (verts.Length / 3);
            }

          }
        }
      }

      ts = DateTime.Now - now;
      Trace.WriteLine(String.Format("total:{0}", ts.TotalMilliseconds));
    }


    /// <summary>
    /// measures the size in layout required for child elements and determines a size for the FrameworkElement-derived class.
    /// </summary>
    /// <param name="availableSize">The available size that this element can give to child elements.</param>
    public override void Measure(System.Drawing.SizeF availableSize)
    {
      base.Measure(availableSize);
      /*
      using (GraphicsPath p = GetPath(new RectangleF(0, 0, 0, 0), null))
      {
        RectangleF bounds = p.GetBounds();

        _desiredSize = new System.Drawing.SizeF((float)bounds.Width, (float)bounds.Height);

        if (bounds.Width <= 0)
          _desiredSize.Width = ((float)availableSize.Width) - (float)(Margin.X + Margin.W);
        if (bounds.Height <= 0)
          _desiredSize.Height = ((float)availableSize.Height) - (float)(Margin.Y + Margin.Z);

        if (LayoutTransform != null)
        {
          ExtendedMatrix m = new ExtendedMatrix();
          LayoutTransform.GetTransform(out m);
          SkinContext.AddLayoutTransform(m);
        }
        SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

        if (LayoutTransform != null)
        {
          SkinContext.RemoveLayoutTransform();
        }
        _desiredSize.Width += (float)(Margin.X + Margin.W);
        _desiredSize.Height += (float)(Margin.Y + Margin.Z);

        _availableSize = new SizeF(availableSize.Width, availableSize.Height);
      }*/
    }
    public override void Arrange(System.Drawing.RectangleF finalRect)
    {
      base.Arrange(finalRect);
      /*

      _finalRect = new System.Drawing.RectangleF(finalRect.Location, finalRect.Size);
      System.Drawing.RectangleF layoutRect = new System.Drawing.RectangleF(finalRect.X, finalRect.Y, finalRect.Width, finalRect.Height);
      layoutRect.X += (float)(Margin.X);
      layoutRect.Y += (float)(Margin.Y);
      layoutRect.Width -= (float)(Margin.X + Margin.W);
      layoutRect.Height -= (float)(Margin.Y + Margin.Z);
      ActualPosition = new Vector3(layoutRect.Location.X, layoutRect.Location.Y, 1.0f); ;
      ActualWidth = layoutRect.Width;
      ActualHeight = layoutRect.Height;
      _performLayout = true;
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      base.Arrange(layoutRect);
       */
    }

    private GraphicsPath GetPath(RectangleF baseRect, ExtendedMatrix finalTransform, out bool isClosed, float thickNess)
    {
      isClosed = false;
      GraphicsPath mPath = new GraphicsPath();
      mPath.FillMode = System.Drawing.Drawing2D.FillMode.Alternate;
      PointF lastPoint = new PointF();
      Regex regex = new Regex(@"[a-zA-Z][-0-9\.,-0-9\. ]*");
      MatchCollection matches = regex.Matches(Data);

      foreach (Match match in matches)
      {
        char cmd = match.Value[0];
        PointF[] points = null;
        if (match.Value.Length > 0)
        {
          string[] txtpoints;
          txtpoints = match.Value.Substring(1).Split(new char[] { ',' });
          if (txtpoints.Length == 1)
          {
            points = new PointF[1];
            points[0].X = GetFloat(txtpoints[0]);
          }
          else
          {
            int c = txtpoints.Length / 2;
            points = new PointF[c];
            for (int i = 0; i < c; i++)
            {
              points[i].X = GetFloat(txtpoints[i * 2]);
              if (i + 1 < txtpoints.Length)
                points[i].Y = GetFloat(txtpoints[i * 2 + 1]);
            }
          }
        }
        switch (cmd)
        {
          case 'm':
            {
              //relative origin
              PointF point = points[0];
              lastPoint = new PointF(lastPoint.X + point.X, lastPoint.Y + point.Y);
            }
            break;
          case 'M':
            {
              //absolute origin
              lastPoint = points[0]; ;
            }
            break;
          case 'L':
            {
              //absolute Line
              for (int i = 0; i < points.Length; ++i)
              {
                mPath.AddLine(lastPoint, points[i]);
                lastPoint = points[i];
              }
            }
            break;
          case 'l':
            {
              //relative Line
              for (int i = 0; i < points.Length; ++i)
              {
                points[i].X += lastPoint.X;
                points[i].Y += lastPoint.Y;
                mPath.AddLine(lastPoint, points[i]);
                lastPoint = points[i];
              }
            }
            break;
          case 'H':
            {
              //Horizontal line to absolute X 
              PointF point1 = new PointF(points[0].X, lastPoint.Y);
              mPath.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'h':
            {
              //Horizontal line to relative X
              PointF point1 = new PointF(lastPoint.X + points[0].X, lastPoint.Y);
              mPath.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'V':
            {
              //Vertical line to absolute y 
              PointF point1 = new PointF(lastPoint.X, points[0].X);
              mPath.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'v':
            {
              //Vertical line to relative y
              PointF point1 = new PointF(lastPoint.X, lastPoint.Y + points[0].X);
              mPath.AddLine(lastPoint, point1);
              lastPoint = new PointF(point1.X, point1.Y);
            }
            break;
          case 'C':
            {
              //Quadratic Bezier Curve Command C21,17,17,21,13,21
              for (int i = 0; i < points.Length; i += 3)
              {
                mPath.AddBezier(lastPoint, points[i], points[i + 1], points[i + 2]);
                lastPoint = points[i + 2];
              }
            }
            break;
          case 'c':
            {
              //Quadratic Bezier Curve Command
              for (int i = 0; i < points.Length; i += 3)
              {
                points[i].X += lastPoint.X;
                points[i].Y += lastPoint.Y;
                mPath.AddBezier(lastPoint, points[i], points[i + 1], points[i + 2]);
                lastPoint = points[i + 2];
              }
            }
            break;
          case 'F':
            {
              //Horizontal line to relative X
              if (points[0].X == 0.0f)
              {
                //the EvenOdd fill rule
                //Rule that determines whether a point is in the fill region by drawing a ray 
                //from that point to infinity in any direction and counting the number of path 
                //segments within the given shape that the ray crosses. If this number is odd, 
                //the point is inside; if even, the point is outside.
                mPath.FillMode = System.Drawing.Drawing2D.FillMode.Alternate;
              }
              else if (points[0].X == 1.0f)
              {
                //the Nonzero fill rule.
                //Rule that determines whether a point is in the fill region of the 
                //path by drawing a ray from that point to infinity in any direction
                //and then examining the places where a segment of the shape crosses
                //the ray. Starting with a count of zero, add one each time a segment 
                //crosses the ray from left to right and subtract one each time a path
                //segment crosses the ray from right to left. After counting the crossings
                //, if the result is zero then the point is outside the path. Otherwise, it is inside.
                mPath.FillMode = System.Drawing.Drawing2D.FillMode.Winding;
              }
            }
            break;
          case 'z':
            {
              //close figure
              isClosed = true;
              mPath.CloseFigure();
            }
            break;
        }
      }

      System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
      if (finalTransform != null)
        m.Multiply(finalTransform.Get2dMatrix(), MatrixOrder.Append);

      ExtendedMatrix em = null;
      if (LayoutTransform != null)
      {
        LayoutTransform.GetTransform(out em);
        m.Multiply(em.Get2dMatrix(), MatrixOrder.Append);
      }
      m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      mPath.Transform(m);

      if (thickNess != 0.0)
      {
        //thickNess /= 2.0f;
        m = new System.Drawing.Drawing2D.Matrix();
        RectangleF bounds = mPath.GetBounds();
        float thicknessW = thickNess;
        float thicknessH = thickNess;
        if (finalTransform != null)
          finalTransform.TransformXY(ref thicknessW, ref thicknessH);
        if (em != null)
          em.TransformXY(ref thicknessW, ref thicknessH);
        thicknessW = (bounds.Width + Math.Abs(thicknessW));
        thicknessH = (bounds.Height + Math.Abs(thicknessH));

        float cx = bounds.X + (bounds.Width / 2.0f);
        float cy = bounds.Y + (bounds.Height / 2.0f);
        m.Translate(-cx, -cy, MatrixOrder.Append);
        m.Scale(thicknessW / bounds.Width, thicknessH / bounds.Height, MatrixOrder.Append);
        m.Translate(cx, cy, MatrixOrder.Append);
        mPath.Transform(m);
      }
      mPath.Flatten();
      return mPath;
    }

    static int useCommas = -1;
    protected float GetFloat(string floatString)
    {
      if (useCommas == -1)
      {
        float test = 12.03f;
        string comma = test.ToString();
        useCommas = (comma.IndexOf(",") >= 0) ? 1 : 0;
      }
      if (useCommas == 1)
      {
        floatString = floatString.Replace(".", ",");
      }
      else
      {
        floatString = floatString.Replace(",", ".");
      }
      float f;
      float.TryParse(floatString, out f);
      return f;
    }
  }
}

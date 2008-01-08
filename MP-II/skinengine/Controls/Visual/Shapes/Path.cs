#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.Properties;
using MediaPortal.Core.InputManager;
using SkinEngine;
using SkinEngine.DirectX;

using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = Microsoft.DirectX.Matrix;

using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace SkinEngine.Controls.Visuals
{
  public class Path : Shape
  {
    Property _dataProperty;
    ArrayList _points;



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
      _points = new ArrayList();
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
      base.DoRender();
      if (String.IsNullOrEmpty(Data)) return;
    }
    protected override void PerformLayout()
    {
      Trace.WriteLine("Path.PerformLayout()");
      float cx, cy;
      Free();
      double w = Width; if (w <= 0) w = ActualWidth;
      double h = Height; if (h <= 0) h = ActualHeight;
      Vector3 orgPos = new Vector3(ActualPosition.X, ActualPosition.Y, ActualPosition.Z);
      //Fill brush
      PositionColored2Textured[] verts;
      GraphicsPath path;
      PointF[] vertices;
      if (Fill != null)
      {
        path = GetPath(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)w, (float)h), out cx, out cy);
        vertices = ConvertPathToTriangleFan(path, (int)+(cx), (int)(cy));
        if (vertices != null)
        {
          _vertexBufferFill = new VertexBuffer(typeof(PositionColored2Textured), vertices.Length, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
          verts = (PositionColored2Textured[])_vertexBufferFill.Lock(0, 0);
          unchecked
          {
            for (int i = 0; i < vertices.Length; ++i)
            {
              verts[i].X = vertices[i].X;
              verts[i].Y = vertices[i].Y;
              verts[i].Z = 1.0f;
            }
          }
          Fill.SetupBrush(this, ref verts);
          _vertexBufferFill.Unlock();
          _verticesCountFill = (verts.Length - 2);
        }

      }
      //border brush

      if (Stroke != null && StrokeThickness > 0)
      {
        path = GetPath(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)w, (float)h), out cx, out cy);
        vertices = ConvertPathToTriangleStrip(path, (int)(cx), (int)(cy), (float)StrokeThickness);
        if (vertices != null)
        {

          _vertexBufferBorder = new VertexBuffer(typeof(PositionColored2Textured), vertices.Length, GraphicsDevice.Device, Usage.WriteOnly, PositionColored2Textured.Format, Pool.Default);
          verts = (PositionColored2Textured[])_vertexBufferBorder.Lock(0, 0);
          unchecked
          {
            for (int i = 0; i < vertices.Length; ++i)
            {
              verts[i].X = vertices[i].X;
              verts[i].Y = vertices[i].Y;
              verts[i].Z = 1.0f;
            }
          }
          Stroke.SetupBrush(this, ref verts);
          _vertexBufferBorder.Unlock();
          _verticesCountBorder = (verts.Length / 3);
        }
      }

    }


    private GraphicsPath GetPath(RectangleF baseRect, out float cx, out float cy)
    {
      GraphicsPath mPath = new GraphicsPath();
      PointF lastPoint = new PointF();
      int i = 0;
      Trace.WriteLine(String.Format("{0}", Data.Trim()));
      while (i < Data.Length)
      {
        char ch = Data[i];
        if (ch == 'm')
        {
          //relative origin
          PointF point = GetPoint(ref i);
          lastPoint = new PointF(lastPoint.X + point.X, lastPoint.Y + point.Y);
          Trace.WriteLine(String.Format("  m({0},{1})  ({2},{3})", point.X, point.Y, lastPoint.X, lastPoint.Y));
        }
        else if (ch == 'M')
        {
          //absolute origin
          PointF point = GetPoint(ref i);
          Trace.WriteLine(String.Format("  M({0},{1})", point.X, point.Y));
          lastPoint = new PointF(point.X, point.Y);
        }
        else if (ch == 'L')
        {
          //absolute Line
          ++i;
          while (true)
          {
            if (Data[i] == ' ') i++;
            if (Data[i] < '0' || Data[i] > '9') break;
            i--;
            PointF point1 = GetPoint(ref i);
            mPath.AddLine(lastPoint, point1);
            lastPoint = new PointF(point1.X, point1.Y);
            Trace.WriteLine(String.Format("  L({0},{1}) ({2},{3})", point1.X, point1.Y, lastPoint.X, lastPoint.Y));
            if (i >= Data.Length) break;
          }
        }
        else if (ch == 'l')
        {
          //relative Line
          ++i;
          while (true)
          {
            if (Data[i] == ' ') i++;
            if (Data[i] < '0' || Data[i] > '9') break;
            i--;
            PointF point1 = GetPoint(ref i);
            Trace.WriteLine(String.Format("  l({0},{1})", point1.X, point1.Y));
            point1.X += lastPoint.X;
            point1.Y += lastPoint.Y;
            mPath.AddLine(lastPoint, point1);
            lastPoint = new PointF(point1.X, point1.Y);
            if (i >= Data.Length) break;
          }
        }
        else if (ch == 'H')
        {
          //Horizontal line to absolute X
          float x = GetDecimal(ref i);
          PointF point1 = new PointF(x, lastPoint.Y);
          Trace.WriteLine(String.Format("  H({0})", x));
          mPath.AddLine(lastPoint, point1);
          lastPoint = new PointF(point1.X, point1.Y);
        }
        else if (ch == 'h')
        {
          //Horizontal line to relative X
          float x = GetDecimal(ref i);
          Trace.WriteLine(String.Format("  h({0})", x));
          PointF point1 = new PointF(lastPoint.X + x, lastPoint.Y);
          mPath.AddLine(lastPoint, point1);
          lastPoint = new PointF(point1.X, point1.Y);
        }
        else if (ch == 'V')
        {
          //Vertical line to absolute y
          float y = GetDecimal(ref i);
          Trace.WriteLine(String.Format("  V({0})", y));
          PointF point1 = new PointF(lastPoint.X, y);
          mPath.AddLine(lastPoint, point1);
          lastPoint = new PointF(point1.X, point1.Y);
        }
        else if (ch == 'v')
        {
          //Vertical line to relative y
          float y = GetDecimal(ref i);
          Trace.WriteLine(String.Format("  v({0})", y));
          PointF point1 = new PointF(lastPoint.X, lastPoint.Y + y);
          mPath.AddLine(lastPoint, point1);
          lastPoint = new PointF(point1.X, point1.Y);
        }
        else if (ch == 'C')
        {
          //Quadratic Bezier Curve Command
          ++i;
          while (true)
          {
            if (Data[i] == ' ') i++;
            if (Data[i] < '0' || Data[i] > '9') break;
            i--;
            PointF controlPoint1 = GetPoint(ref i);
            PointF controlPoint2 = GetPoint(ref i);
            PointF endpoint = GetPoint(ref i);
            mPath.AddBezier(lastPoint, controlPoint1, controlPoint2, endpoint);
            lastPoint = new PointF(endpoint.X, endpoint.Y);
            Trace.WriteLine(String.Format("  C({0},{1}) ({2},{3}) ({4},{5}  ({6},{7}))", controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, endpoint.X, endpoint.Y, lastPoint.X, lastPoint.Y));
            if (i >= Data.Length) break;
          }
        }
        else if (ch == 'c')
        {
          //Quadratic Bezier Curve Command
          ++i;
          while (true)
          {
            if (Data[i] == ' ') i++;
            if (Data[i] < '0' || Data[i] > '9') break;
            i--;
            PointF controlPoint1 = GetPoint(ref i);
            PointF controlPoint2 = GetPoint(ref i);
            PointF endpoint = GetPoint(ref i);
            Trace.WriteLine(String.Format("  c({0},{1}) ({2},{3}) ({4},{5})", controlPoint1.X, controlPoint1.Y, controlPoint2.X, controlPoint2.Y, endpoint.X, endpoint.Y));
            controlPoint1.X += lastPoint.X;
            controlPoint1.Y += lastPoint.Y;

            controlPoint2.X += lastPoint.X;
            controlPoint2.Y += lastPoint.Y;

            endpoint.X += lastPoint.X;
            endpoint.Y += lastPoint.Y;
            mPath.AddBezier(lastPoint, controlPoint1, controlPoint2, endpoint);
            lastPoint = new PointF(endpoint.X, endpoint.Y);
            if (i >= Data.Length) break;
          }
        }
        else if (ch == 'F')
        {
          //Horizontal line to relative X
          float x = GetDecimal(ref i);
          Trace.WriteLine(String.Format("  F({0})", x));
          if (x == 0.0f)
          {
            //the EvenOdd fill rule
            //Rule that determines whether a point is in the fill region by drawing a ray 
            //from that point to infinity in any direction and counting the number of path 
            //segments within the given shape that the ray crosses. If this number is odd, 
            //the point is inside; if even, the point is outside.
            mPath.FillMode = System.Drawing.Drawing2D.FillMode.Alternate;
          }
          else if (x == 1.0f)
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
        else if (ch == 'z')
        {
          //close figure
          mPath.CloseFigure();
          Trace.WriteLine(String.Format("  z"));
          break;
        }
        else
        {
          if (Data[i] != ' ' && Data[i] != '\r')
            Trace.WriteLine(String.Format("  ?:{0}", Data[i]));
          ++i;
        }
      }
      RectangleF bounds = mPath.GetBounds();
      float w = bounds.Width;
      float h = bounds.Height;
      float scaleX = 1;
      float scaleY = 1;
      /*
      if (w > 0 && baseRect.Width > 0 && Stretch != Stretch.None)
        scaleX = ((float)baseRect.Width) / w;
      if (h > 0 && baseRect.Height > 0 && Stretch != Stretch.None)
        scaleY = ((float)baseRect.Height) / h;

      System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
      m.Translate(-bounds.X, -bounds.Y, MatrixOrder.Append);

      m.Scale(scaleX, scaleY, MatrixOrder.Append);
      m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      */
      System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
      m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      mPath.Transform(m);
      mPath.Flatten();

      cx = cy = 0;
      if (mPath.PointCount > 0)
      {
        _points.Clear();
        PointF[] points = new PointF[mPath.PathPoints.Length];
        for (int x = 0; x < mPath.PathPoints.Length; ++x)
        {
          PointF f = new PointF(mPath.PathPoints[x].X, mPath.PathPoints[x].Y);
          points[x] = f;
          _points.Add(f);
        }
        CalcCentroid(points, out cx, out cy);
      }
      return mPath;
    }

    float GetDecimal(ref int i)
    {
      i++;
      string pointTxt = "";
      while ((Data[i] >= '0' && Data[i] <= '9') || (Data[i] == '.'))
      {
        pointTxt += Data[i];
        ++i;
        if (i >= Data.Length) break;
      }
      pointTxt = pointTxt.Trim();
      float x = GetFloat(pointTxt);
      return x;
    }
    PointF GetPoint(ref int i)
    {
      i++;
      string pointTxt = "";
      int comma = 0;
      while ((Data[i] >= '0' && Data[i] <= '9') || (Data[i] == '.' || Data[i] == ','))
      {
        if (Data[i] == ',')
        {
          comma++;
          if (comma == 2)
            break;
        }
        pointTxt += Data[i];
        ++i;
        if (i >= Data.Length) break;
      }
      pointTxt = pointTxt.Trim();
      string[] parts = pointTxt.Split(new char[] { ',' });
      if (parts.Length != 2)
        return new PointF(0, 0);
      float x = GetFloat(parts[0]);
      float y = GetFloat(parts[1]);
      return new PointF(x, y);
    }
    protected float GetFloat(string floatString)
    {
      float test = 12.03f;
      string comma = test.ToString();
      bool replaceCommas = (comma.IndexOf(",") >= 0);
      if (replaceCommas)
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
    void ZCross(ref PointF left, ref PointF right, out double result)
    {
      result = left.X * right.Y - left.Y * right.X;
    }
    void CalcCentroid(PointF[] points, out float cx, out float cy)
    {
      Vector2 centroid = new Vector2();
      double temp;
      double area = 0;
      PointF v1 = (PointF)points[points.Length - 1];
      PointF v2;
      for (int index = 0; index < points.Length; ++index, v1 = v2)
      {
        v2 = (PointF)points[index];
        ZCross(ref v1, ref v2, out temp);
        area += temp;
        centroid.X += (float)((v1.X + v2.X) * temp);
        centroid.Y += (float)((v1.Y + v2.Y) * temp);
      }
      temp = 1 / (Math.Abs(area) * 3);
      centroid.X *= (float)temp;
      centroid.Y *= (float)temp;

      cx = (float)(Math.Abs(centroid.X));
      cy = (float)(Math.Abs(centroid.Y));
    }
  }
}

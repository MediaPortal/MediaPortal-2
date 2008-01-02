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
    protected override void PerformLayout()
    {
      float cx, cy;
      Free();
      double w = Width; if (w == 0) w = ActualWidth;
      double h = Height; if (h == 0) h = ActualHeight;
      Vector3 orgPos = new Vector3(ActualPosition.X, ActualPosition.Y, ActualPosition.Z);
      //Fill brush
      PositionColored2Textured[] verts;
      GraphicsPath path;
      PointF[] vertices;
      if (Fill != null)
      {
        path = GetPath(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)w, (float)h), out cx, out cy);
        vertices = ConvertPathToTriangleFan(path, (int)+(cx), (int)(cy));
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
      //border brush

      if (Stroke != null && StrokeThickness > 0)
      {
        path = GetPath(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)w, (float)h), out cx, out cy);
        vertices = ConvertPathToTriangleStrip(path, (int)(cx), (int)(cy), (float)StrokeThickness);

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


    private GraphicsPath GetPath(RectangleF baseRect, out float cx, out float cy)
    {
      GraphicsPath mPath = new GraphicsPath();
      PointF lastPoint = new PointF();
      int i = 0;
      while (i < Data.Length)
      {
        char ch = Data[i];
        if (ch == 'm')
        {
          //relative origin
          PointF point = GetPoint(ref i);
          lastPoint = new PointF(lastPoint.X + point.X, lastPoint.Y + point.Y);
        }
        else if (ch == 'M')
        {
          //absolute origin
          PointF point = GetPoint(ref i);
          lastPoint = new PointF(point.X, point.Y);
        }
        else if (ch == 'L')
        {
          //absolute Line
          PointF point1 = GetPoint(ref i);
          PointF point2 = GetPoint(ref i);
          mPath.AddLine(lastPoint, point1);
          mPath.AddLine(point1, point2);
          lastPoint = new PointF(point2.X, point2.Y);
          while (true)
          {
            if (Data[i] == ' ') i++;
            if (Data[i] < '0' || Data[i] > '9') break;
            i--;
            point1 = GetPoint(ref i);
            mPath.AddLine(lastPoint, point1);
            lastPoint = new PointF(point1.X, point1.Y);
          }
        }
        else if (ch == 'l')
        {
          //relative Line
          while (true)
          {
            PointF point1 = GetPoint(ref i);
            mPath.AddLine(lastPoint, point1);
            lastPoint = new PointF(point1.X, point1.Y);
            if (Data[i] == ' ') i++;
            if (Data[i] < '0' || Data[i] > '9') break;
            i--;
          }
        }
        else if (ch == 'z')
        {
          //close figure
          mPath.CloseFigure();
          break;
        }
        else if (ch == ' ')
        {
          ++i;
        }
      }

      RectangleF bounds = mPath.GetBounds();
      float w = bounds.Width;
      float h = bounds.Height;
      float scaleX = 1;
      if (baseRect.Width > 0 && Stretch != Stretch.None)
        scaleX = ((float)baseRect.Width) / w;
      float scaleY = 1;
      if (baseRect.Height > 0 && Stretch != Stretch.None)
        scaleY = ((float)baseRect.Height) / h;

      System.Drawing.Drawing2D.Matrix m = new System.Drawing.Drawing2D.Matrix();
      m.Translate(-bounds.X, -bounds.Y, MatrixOrder.Append);

      m.Scale(scaleX, scaleY, MatrixOrder.Append);
      m.Translate(baseRect.X, baseRect.Y, MatrixOrder.Append);
      mPath.Transform(m);
      mPath.Flatten();

      _points.Clear();
      PointF[] points = new PointF[mPath.PathPoints.Length];
      for (int x = 0; x < mPath.PathPoints.Length; ++x)
      {
        PointF f = new PointF(mPath.PathPoints[x].X, mPath.PathPoints[x].Y);
        points[x] = f;
        _points.Add(f);
      }
      CalcCentroid(points, out cx, out cy);
      return mPath;
    }

    PointF GetPoint(ref int i)
    {
      i++;
      string pointTxt = "";
      while ((Data[i] >= '0' && Data[i] <= '9') || (Data[i] == '.' || Data[i] == ','))
      {
        pointTxt += Data[i];
        ++i;
      }
      pointTxt = pointTxt.Trim();
      string[] parts = pointTxt.Split(new char[] { ',' });
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

      cx = (float)centroid.X;
      cy = (float)centroid.Y;
    }
  }
}

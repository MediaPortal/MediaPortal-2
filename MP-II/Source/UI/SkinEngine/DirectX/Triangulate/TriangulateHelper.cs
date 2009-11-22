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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;
using Matrix=SlimDX.Matrix;

namespace MediaPortal.UI.SkinEngine.DirectX.Triangulate
{
  public class TriangulateHelper
  {
    #region Triangulation

    /// <summary>
    /// Generates a list of triangles from an interior point (<paramref name="cx"/>;<paramref name="cy"/>)
    /// to each point of the source <paramref name="path"/>. The path must be closed and describe a simple polygon,
    /// where no connection between (cx; cy) and a path points crosses the border (this means, from (cx; cy),
    /// each path point must be reached directly).
    /// The generated triangles are in the same form as if we would have generated a triangle fan,
    /// but this method returns them in the form of a triangle list.
    /// </summary>
    /// <param name="path">The source path which encloses the shape to triangulate.</param>
    /// <param name="cx">X coordinate of an interior point of the <paramref name="path"/>.</param>
    /// <param name="cy">Y coordinate of an interior point of the <paramref name="path"/>.</param>
    /// <param name="verts">Returns a list of vertices describing a triangle list.</param>
    public static void FillPolygon_TriangleList(GraphicsPath path, float cx, float cy, out PositionColored2Textured[] verts)
    {
      verts = null;
      int pointCount = path.PointCount;
      if (pointCount <= 2) return;
      PointF[] pathPoints = path.PathPoints;
      if (pointCount == 3)
      {
        verts = new PositionColored2Textured[3];

        verts[0].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, 1);
        verts[1].Position = new Vector3(pathPoints[1].X, pathPoints[1].Y, 1);
        verts[2].Position = new Vector3(pathPoints[2].X, pathPoints[2].Y, 1);
        return;
      }
      bool closed = pathPoints[0] == pathPoints[pointCount - 1];
      if (closed)
        pointCount--;
      int verticeCount = pointCount * 3;
      verts = new PositionColored2Textured[verticeCount];
      for (int i = 0; i < pointCount; i++)
      {
        int offset = i * 3;
        verts[offset].Position = new Vector3(cx, cy, 1);
        verts[offset + 1].Position = new Vector3(pathPoints[i].X, pathPoints[i].Y, 1);
        if (i + 1 < pointCount)
          verts[offset + 2].Position = new Vector3(pathPoints[i + 1].X, pathPoints[i + 1].Y, 1);
        else
          verts[offset + 2].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, 1);
      }
      return;
    }

    /// <summary>
    /// Generates a triangle fan from an interior point (<paramref name="cx"/>;<paramref name="cy"/>)
    /// to each point of the source <paramref name="path"/>. The path must describe a simple polygon,
    /// where no connection between (cx; cy) and a path points crosses the border (this means, from (cx; cy),
    /// each path point must be reached directly).
    /// The path will be closed automatically, if it is not closed.
    /// The generated triangles are in the same form as if we would have generated a triangle fan,
    /// but this method returns them as triangle list.
    /// </summary>
    /// <param name="path">The source path which encloses the shape to triangulate.</param>
    /// <param name="cx">X coordinate of an interior point of the <paramref name="path"/>.</param>
    /// <param name="cy">Y coordinate of an interior point of the <paramref name="path"/>.</param>
    /// <param name="verts">Returns a list of vertices describing a triangle fan.</param>
    public static void FillPolygon_TriangleFan(GraphicsPath path, float cx, float cy, out PositionColored2Textured[] verts)
    {
      verts = null;
      int pointCount = path.PointCount;
      if (pointCount <= 2) return;
      PointF[] pathPoints = path.PathPoints;
      if (pointCount == 3)
      {
        verts = new PositionColored2Textured[3];

        verts[0].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, 1);
        verts[1].Position = new Vector3(pathPoints[1].X, pathPoints[1].Y, 1);
        verts[2].Position = new Vector3(pathPoints[2].X, pathPoints[2].Y, 1);
        return;
      }
      bool close = pathPoints[0] != pathPoints[pointCount - 1];
      int verticeCount = pointCount + (close ? 2 : 1);

      verts = new PositionColored2Textured[verticeCount];

      verts[0].Position = new Vector3(cx, cy, 1); // First point is center point
      for (int i = 0; i < pointCount; i++)
        // Set the outer fan points
        verts[i + 1].Position = new Vector3(pathPoints[i].X, pathPoints[i].Y, 1);
      if (close)
        // Last point is the first point to close the shape
        verts[verticeCount - 1].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, 1);
      return;
    }

    static void GetInset(PointF nextpoint, PointF point, out float x, out float y, double thicknessW, double thicknessH, PolygonDirection direction, ExtendedMatrix finalLayoutTransform)
    {
      double ang = Math.Atan2(nextpoint.Y - point.Y, nextpoint.X - point.X);  //returns in radians
      const double pi2 = Math.PI / 2.0;

      if (direction == PolygonDirection.Clockwise)
        ang += pi2;
      else
        ang -= pi2;
      x = (float)(Math.Cos(ang) * thicknessW); //radians
      y = (float)(Math.Sin(ang) * thicknessH);
      if (finalLayoutTransform != null)
        finalLayoutTransform.TransformXY(ref x, ref y);
      x += point.X;
      y += point.Y;
    }

    static PointF GetNextPoint(PointF[] points, int i, int max)
    {
      i++;
      while (i >= max) i -= max;
      return points[i];
    }

    /// <summary>
    /// Converts the graphics path to an array of vertices using trianglestrip.
    /// </summary>
    public static void TriangulateStroke_TriangleList(GraphicsPath path, float thickness, bool close,
        out PositionColored2Textured[] verts, ExtendedMatrix finalTransLayoutform)
    {
      CPoint2D[] points = new CPoint2D[path.PathPoints.Length];
      for (int i = 0; i < path.PointCount; i++)
      {
        PointF pt = path.PathPoints[i];
        points[i] = new CPoint2D(pt.X, pt.Y);
      }
      PolygonDirection direction = CPolygon.GetPointsDirection(points);
      TriangulateStroke_TriangleList(path, thickness, close, direction, out verts, finalTransLayoutform);
    }

    /// <summary>
    /// Converts the graphics path to an array of vertices using trianglestrip.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="thickness">The thickness of the line.</param>
    /// <param name="close">True if we should connect the first and last point.</param>
    /// <param name="direction">The polygon direction.</param>
    /// <param name="verts">The generated verts.</param>
    /// <param name="finalLayoutTransform">Final layout transform.</param>
    /// <returns>vertex buffer</returns>
    public static void TriangulateStroke_TriangleList(GraphicsPath path, float thickness, bool close,
        PolygonDirection direction, out PositionColored2Textured[] verts, ExtendedMatrix finalLayoutTransform)
    {
      verts = null;
      if (path.PointCount <= 0)
        return;

      float thicknessW = thickness * SkinContext.Zoom.Width;
      float thicknessH = thickness * SkinContext.Zoom.Height;
      PointF[] points = path.PathPoints;

      int pointCount;
      if (close)
        pointCount = points.Length;
      else
        pointCount = points.Length - 1;

      int verticeCount = pointCount * 2 * 3;
      verts = new PositionColored2Textured[verticeCount];

      for (int i = 0; i < pointCount; i++)
      {
        int offset = i * 6;

        PointF nextpoint = GetNextPoint(points, i, points.Length);
        float x;
        float y;
        GetInset(nextpoint, points[i], out x, out y, thicknessW, thicknessH, direction, finalLayoutTransform);
        verts[offset].Position = new Vector3(points[i].X, points[i].Y, 1);
        verts[offset + 1].Position = new Vector3(nextpoint.X, nextpoint.Y, 1);
        verts[offset + 2].Position = new Vector3(x, y, 1);

        verts[offset + 3].Position = new Vector3(nextpoint.X, nextpoint.Y, 1);
        verts[offset + 4].Position = new Vector3(x, y, 1);

        verts[offset + 5].Position = new Vector3(nextpoint.X + (x - points[i].X), nextpoint.Y + (y - points[i].Y), 1);
      }
    }

    /// <summary>
    /// Creates a <see cref="PrimitiveType.TriangleList"/> of vertices which cover the interior of the
    /// specified <paramref name="path"/>. The path must be closed and describe a simple polygon.
    /// </summary>
    /// <param name="path">Path which may only contain one single subpath.</param>
    /// <param name="verts">Returns a <see cref="PrimitiveType.TriangleList"/> of vertices.</param>
    public static void Triangulate(GraphicsPath path, out PositionColored2Textured[] verts)
    {
      if (path.PointCount < 3)
      {
        verts = null;
        return;
      }
      if (path.PointCount == 3)
      {
        verts = new PositionColored2Textured[3];

        PointF[] pathPoints = path.PathPoints;
        verts[0].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, 1);
        verts[1].Position = new Vector3(pathPoints[1].X, pathPoints[1].Y, 1);
        verts[2].Position = new Vector3(pathPoints[2].X, pathPoints[2].Y, 1);
        return;
      }
      ICollection<CPolygon> polygons = new List<CPolygon>(new CPolygon(path).Triangulate());

      verts = new PositionColored2Textured[polygons.Count * 3];
      int offset = 0;
      foreach (CPolygon triangle in polygons)
      {
        verts[offset++].Position = new Vector3(triangle[0].X, triangle[0].Y, 1);
        verts[offset++].Position = new Vector3(triangle[1].X, triangle[1].Y, 1);
        verts[offset++].Position = new Vector3(triangle[2].X, triangle[2].Y, 1);
      }
    }

    /// <summary>
    /// Generates the vertices of a thickened line strip
    /// </summary>
    /// <param name="path">Graphics path on the line strip</param>
    /// <param name="thickness">Thickness of the line</param>
    /// <param name="close">Whether to connect the last point back to the first</param>
    /// <param name="widthMode">How to place the weight of the line relative to it</param>
    /// <returns>Points ready to pass to the Transform constructor</returns>
    public void CalculateLinePoints(GraphicsPath path, float thickness, bool close, WidthMode widthMode,
        out PositionColored2Textured[] verts)
    {
      verts = null;
      if (path.PointCount < 3)
      {
        if (close) return;
        else if (path.PointCount < 2)
          return;
      }

      Matrix matrix = new Matrix();
      int count = path.PointCount;
      PointF[] pathPoints = path.PathPoints;
      if (pathPoints[count - 2] == pathPoints[count - 1])
        count--;
      Vector2[] points = new Vector2[count];
      for (int i = 0; i < count; ++i)
        points[i] = new Vector2(pathPoints[i].X, pathPoints[i].Y);

      Vector2 innerDistance = new Vector2(0, 0);
      switch (widthMode)
      {
        case WidthMode.Centered:
          //innerDistance =thickness / 2;
          innerDistance = new Vector2((thickness / 2) * SkinContext.Zoom.Width, (thickness / 2) * SkinContext.Zoom.Height);
          break;
        case WidthMode.LeftHanded:
          //innerDistance = -thickness;
          innerDistance = new Vector2(-thickness * SkinContext.Zoom.Width, -thickness * SkinContext.Zoom.Height);
          break;
        case WidthMode.RightHanded:
          //innerDistance = thickness;
          innerDistance = new Vector2(thickness * SkinContext.Zoom.Width, thickness * SkinContext.Zoom.Height);
          break;
      }

      Vector2[] outPoints = new Vector2[(points.Length + (close ? 1 : 0)) * 2];

      float slope, intercept;
      //Get the endpoints
      if (close)
      {
        //Get the overlap points
        int lastIndex = outPoints.Length - 4;
        outPoints[lastIndex] = InnerPoint(matrix, innerDistance, points[points.Length - 2], points[points.Length - 1], points[0], out slope, out intercept);
        outPoints[0] = InnerPoint(matrix, innerDistance, ref slope, ref intercept, outPoints[lastIndex], points[0], points[1]);
      }
      else
      {
        //Take endpoints based on the end segments' normals alone
        outPoints[0] = Vector2.Modulate(innerDistance, GetNormal(points[1] - points[0]));
        TransformXY(ref outPoints[0], matrix);
        outPoints[0] = points[0] + outPoints[0];

        //outPoints[0] = points[0] + innerDistance * normal(points[1] - points[0]);
        Vector2 norm = Vector2.Modulate(innerDistance, GetNormal(points[points.Length - 1] - points[points.Length - 2])); //DEBUG

        TransformXY(ref norm, matrix);
        outPoints[outPoints.Length - 2] = points[points.Length - 1] + norm;

        //Get the slope and intercept of the first segment to feed into the middle loop
        slope = vectorSlope(points[1] - points[0]);
        intercept = lineIntercept(outPoints[0], slope);
      }

      //Get the middle points
      for (int i = 1; i < points.Length - 1; i++)
        outPoints[2 * i] = InnerPoint(matrix, innerDistance, ref slope, ref intercept, outPoints[2 * (i - 1)], points[i], points[i + 1]);

      //Derive the outer points from the inner points
      if (widthMode == WidthMode.Centered)
        for (int i = 0; i < points.Length; i++)
          outPoints[2 * i + 1] = 2 * points[i] - outPoints[2 * i];
      else
        for (int i = 0; i < points.Length; i++)
          outPoints[2 * i + 1] = points[i];

      //Closed strips must repeat the first two points
      if (close)
      {
        outPoints[outPoints.Length - 2] = outPoints[0];
        outPoints[outPoints.Length - 1] = outPoints[1];
      }
      int verticeCount = outPoints.Length;
      verts = new PositionColored2Textured[verticeCount];

      for (int i = 0; i < verticeCount; ++i)
        verts[i].Position = new Vector3(outPoints[i].X, outPoints[i].Y, 1);
    }

    protected static void ZCross(ref PointF left, ref PointF right, out double result)
    {
      result = left.X * right.Y - left.Y * right.X;
    }

    public static void CalcCentroid(GraphicsPath path, out float cx, out float cy)
    {
      int pointCount = path.PointCount;
      if (pointCount == 0)
      {
        cx = 0;
        cy = 0;
        return;
      }
      PointF[] pathPoints = path.PathPoints;
      Vector2 centroid = new Vector2();
      double temp;
      double area = 0;
      PointF v1 = pathPoints[pointCount - 1];
      PointF v2;
      for (int index = 0; index < pointCount; ++index, v1 = v2)
      {
        v2 = pathPoints[index];
        ZCross(ref v1, ref v2, out temp);
        area += temp;
        centroid.X += (float)((v1.X + v2.X) * temp);
        centroid.Y += (float)((v1.Y + v2.Y) * temp);
      }
      temp = 1 / (Math.Abs(area) * 3);
      centroid.X *= (float)temp;
      centroid.Y *= (float)temp;

      cx = Math.Abs(centroid.X);
      cy = Math.Abs(centroid.Y);
    }

    #endregion

    #region Math helpers

    /// <summary>the slope of v, or NaN if it is nearly vertical</summary>
    /// <param name="v">Vector to take slope from</param>
    private static float vectorSlope(Vector2 v)
    {
      return Math.Abs(v.X) < 0.001f ? float.NaN : (v.Y / v.X);
    }

    /// <summary>Finds the intercept of a line</summary>
    /// <param name="point">A point on the line</param>
    /// <param name="slope">The slope of the line</param>
    private static float lineIntercept(Vector2 point, float slope)
    {
      return point.Y - slope * point.X;
    }

    /// <summary>
    /// Calculates the unit length right-hand normal of v.
    /// </summary>
    /// <param name="v">Vector to find the normal of.</param>
    public static Vector2 GetNormal(Vector2 v)
    {
      //Avoid division by zero/returning a zero vector
      if (Math.Abs(v.Y) < 0.0001) return new Vector2(0, Math.Sign(v.X));
      if (Math.Abs(v.X) < 0.0001) return new Vector2(-Math.Sign(v.Y), 0);

      float r = 1 / v.Length();
      return new Vector2(-v.Y * r, v.X * r);
    }

    #endregion

    #region Point calculation

    /// <overloads>Computes points needed to connect thick lines properly</overloads>
    /// <summary>Finds the inside vertex at a point in a line strip</summary>
    /// <param name="distance">Distance from the center of the line that the point should be</param>
    /// <param name="lastPoint">Point on the strip before point</param>
    /// <param name="point">Point whose inside vertex we are finding</param>
    /// <param name="nextPoint">Point on the strip after point</param>
    /// <param name="slope">Assigned the slope of the line from lastPoint to point</param>
    /// <param name="intercept">Assigned the intercept of the line with the computed slope through the inner point</param>
    /// <remarks>
    /// This overload is less efficient for calculating a sequence of inner vertices because
    /// it does not reuse results from previously calculated points
    /// </remarks>
    public static Vector2 InnerPoint(Matrix matrix, Vector2 distance, Vector2 lastPoint, Vector2 point, Vector2 nextPoint, out float slope, out float intercept)
    {
      Vector2 lastDifference = point - lastPoint;
      slope = vectorSlope(lastDifference);
      intercept = lineIntercept(lastPoint + Vector2.Modulate(distance, GetNormal(lastDifference)), slope);
      return InnerPoint(matrix, distance, ref slope, ref intercept, lastPoint + Vector2.Modulate(distance, GetNormal(lastDifference)), point, nextPoint);
    }

    /// <summary>Finds the inside vertex at a point in a line strip</summary>
    /// <param name="distance">Distance from the center of the line that the point should be</param>
    /// <param name="lastSlope">Slope of the previous line in, slope from point to nextPoint out</param>
    /// <param name="lastIntercept">Intercept of the previous line in, intercept of the line through point and nextPoint out</param>
    /// <param name="lastInnerPoint">Last computed inner point</param>
    /// <param name="point">Point whose inside vertex we are finding</param>
    /// <param name="nextPoint">Point on the strip after point</param>
    /// <remarks>
    /// This overload can reuse information calculated about the previous point, so it is more
    /// efficient for computing the inside of a string of contiguous points on a strip
    /// </remarks>
    private static Vector2 InnerPoint(Matrix matrix, Vector2 distance, ref float lastSlope, ref float lastIntercept, Vector2 lastInnerPoint, Vector2 point, Vector2 nextPoint)
    {
      Vector2 edgeVector = nextPoint - point;
      //Vector2 innerPoint = nextPoint + distance * normal(edgeVector);
      Vector2 innerPoint = Vector2.Modulate(distance, GetNormal(edgeVector));

      TransformXY(ref innerPoint, matrix);
      innerPoint = nextPoint + innerPoint;

      float slope = vectorSlope(edgeVector);
      float intercept = lineIntercept(innerPoint, slope);

      float safeSlope, safeIntercept;	//Slope and intercept on one of the lines guaranteed not to be vertical
      float x;						//X-coordinate of intersection

      if (float.IsNaN(slope))
      {
        safeSlope = lastSlope;
        safeIntercept = lastIntercept;
        x = innerPoint.X;
      }
      else if (float.IsNaN(lastSlope))
      {
        safeSlope = slope;
        safeIntercept = intercept;
        x = lastInnerPoint.X;
      }
      else if (Math.Abs(slope - lastSlope) < 0.001)
      {
        safeSlope = slope;
        safeIntercept = intercept;
        x = lastInnerPoint.X;
      }
      else
      {
        safeSlope = slope;
        safeIntercept = intercept;
        x = (lastIntercept - intercept) / (slope - lastSlope);
      }

      if (!float.IsNaN(slope))
        lastSlope = slope;
      if (!float.IsNaN(intercept))
        lastIntercept = intercept;

      return new Vector2(x, safeSlope * x + safeIntercept);
    }

    public static void TransformXY(ref Vector2 vector, Matrix m)
    {
      float w1 = vector.X * m.M11 + vector.Y * m.M21;
      float h1 = vector.X * m.M12 + vector.Y * m.M22;
      vector.X = w1;
      vector.Y = h1;
    }

    #endregion
  }
}

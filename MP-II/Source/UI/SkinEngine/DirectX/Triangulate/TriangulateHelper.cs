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
using MediaPortal.SkinEngine.Controls.Visuals.Shapes;
using MediaPortal.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;
using Matrix=SlimDX.Matrix;

namespace MediaPortal.SkinEngine.DirectX.Triangulate
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
      if (pointCount <= 0) return;
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
      if (pointCount <= 0) return;
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
      PolygonDirection direction = PointsDirection(path);
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
    /// <param name="isCenterFill">True if center fill otherwise left hand fill.</param>
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
      PointF[] newPoints = new PointF[points.Length];

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
    /// <param name="cx">X coordinate of the path's centroid.</param>
    /// <param name="cy">Y coordinate of the path's centroid.</param>
    /// <param name="verts">Returns a <see cref="PrimitiveType.TriangleList"/> of vertices.</param>
    public static void Triangulate(GraphicsPath path, float cx, float cy, out PositionColored2Textured[] verts)
    {
      if (path.PointCount <= 3)
      {
        FillPolygon_TriangleList(path, cx, cy, out verts);
        return;
      }
      CPolygonShape cutPolygon = new CPolygonShape(path);
      cutPolygon.CutEar();

      int count = cutPolygon.NumberOfPolygons;
      verts = new PositionColored2Textured[count * 3];
      for (int i = 0; i < count; i++)
      {
        CPoint2D[] triangle = cutPolygon[i];
        int offset = i * 3;
        verts[offset].Position = new Vector3(triangle[0].X, triangle[0].Y, 1);
        verts[offset + 1].Position = new Vector3(triangle[1].X, triangle[1].Y, 1);
        verts[offset + 2].Position = new Vector3(triangle[2].X, triangle[2].Y, 1);
      }
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

    public static PolygonDirection PointsDirection(GraphicsPath points)
    {
      int nCount = 0;
      int nPoints = points.PointCount;

      if (nPoints < 3)
        return PolygonDirection.Unknown;
      PointF[] pathPoints = points.PathPoints;
      for (int i = 0; i < nPoints - 2; i++)
      {
        int j = (i + 1) % nPoints;
        int k = (i + 2) % nPoints;

        double crossProduct = (pathPoints[j].X - pathPoints[i].X) * (pathPoints[k].Y - pathPoints[j].Y);
        crossProduct = crossProduct - ((pathPoints[j].Y - pathPoints[i].Y) * (pathPoints[k].X - pathPoints[j].X));

        if (crossProduct > 0)
          nCount++;
        else
          nCount--;
      }

      if (nCount < 0)
        return PolygonDirection.Count_Clockwise;
      if (nCount > 0)
        return PolygonDirection.Clockwise;
      return PolygonDirection.Unknown;
    }

    #endregion

    #region Math helpers

    /// <summary>the slope of v, or NaN if it is nearly vertical</summary>
    /// <param name="v">Vector to take slope from</param>
    public static float vectorSlope(Vector2 v)
    {
      return Math.Abs(v.X) < 0.001f ? float.NaN : (v.Y / v.X);
    }

    /// <summary>Finds the intercept of a line</summary>
    /// <param name="point">A point on the line</param>
    /// <param name="slope">The slope of the line</param>
    public static float lineIntercept(Vector2 point, float slope)
    {
      return point.Y - slope * point.X;
    }

    /// <summary>The unit length right-hand normal of v</summary>
    /// <param name="v">Vector to find the normal of</param>
    public static Vector2 normal(Vector2 v)
    {
      //Avoid division by zero/returning a zero vector
      if (Math.Abs(v.Y) < 0.0001) return new Vector2(0, sgn(v.X));
      if (Math.Abs(v.X) < 0.0001) return new Vector2(-sgn(v.Y), 0);

      float r = 1 / v.Length();
      return new Vector2(-v.Y * r, v.X * r);
    }

    /// <summary>Finds the sign of a number</summary>
    /// <param name="x">Number to take the sign of</param>
    private static float sgn(float x)
    {
      return (x > 0f ? 1f : (x < 0f ? -1f : 0f));
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
      intercept = lineIntercept(lastPoint + Vector2.Modulate(distance, normal(lastDifference)), slope);
      return InnerPoint(matrix, distance, ref slope, ref intercept, lastPoint + Vector2.Modulate(distance, normal(lastDifference)), point, nextPoint);
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
    public static Vector2 InnerPoint(Matrix matrix, Vector2 distance, ref float lastSlope, ref float lastIntercept, Vector2 lastInnerPoint, Vector2 point, Vector2 nextPoint)
    {
      Vector2 edgeVector = nextPoint - point;
      //Vector2 innerPoint = nextPoint + distance * normal(edgeVector);
      Vector2 innerPoint = Vector2.Modulate(distance, normal(edgeVector));

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
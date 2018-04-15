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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes;
using Poly2Tri;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.DirectX.Triangulate
{
  public class TriangulateHelper
  {
    public const double DELTA_DOUBLE = 0.01;

    #region Triangulation

    /// <summary>
    /// Generates a list of triangles from an interior point (<paramref name="cx"/>;<paramref name="cy"/>)
    /// to each point of the source <paramref name="points"/>. The path must be closed and describe a simple polygon,
    /// where no connection between (cx; cy) and any path point crosses the border (this means, from (cx; cy),
    /// each path point must be reached directly).
    /// The generated triangles are in the same form as if we would have generated a triangle fan,
    /// but this method returns them in the form of a triangle list.
    /// </summary>
    /// <param name="points">The source points which enclose the shape to triangulate.</param>
    /// <param name="cx">X coordinate of an interior point of the <paramref name="points"/>.</param>
    /// <param name="cy">Y coordinate of an interior point of the <paramref name="points"/>.</param>
    /// <param name="zCoord">Z coordinate of the returned vertices.</param>
    /// <param name="verts">Returns a list of vertices describing a triangle list.</param>
    public static void FillPolygon_TriangleList(PointF[] points, float cx, float cy, float zCoord, out PositionColoredTextured[] verts)
    {
      verts = null;
      PointF[] pathPoints = AdjustPoints(points);
      int pointCount = pathPoints.Length;
      if (pointCount <= 2) return;
      if (pointCount == 3)
      {
        verts = new PositionColoredTextured[3];

        verts[0].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, zCoord);
        verts[1].Position = new Vector3(pathPoints[1].X, pathPoints[1].Y, zCoord);
        verts[2].Position = new Vector3(pathPoints[2].X, pathPoints[2].Y, zCoord);
        return;
      }
      bool closed = pathPoints[0] == pathPoints[pointCount - 1];
      if (closed)
        pointCount--;
      int verticeCount = pointCount * 3;
      verts = new PositionColoredTextured[verticeCount];
      for (int i = 0; i < pointCount; i++)
      {
        int offset = i * 3;
        verts[offset].Position = new Vector3(cx, cy, zCoord);
        verts[offset + 1].Position = new Vector3(pathPoints[i].X, pathPoints[i].Y, zCoord);
        if (i + 1 < pointCount)
          verts[offset + 2].Position = new Vector3(pathPoints[i + 1].X, pathPoints[i + 1].Y, zCoord);
        else
          verts[offset + 2].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, zCoord);
      }
    }

    /// <summary>
    /// Generates a triangle fan from an interior point (<paramref name="cx"/>;<paramref name="cy"/>)
    /// to each point of the source <paramref name="points"/>. The path must describe a simple polygon,
    /// where no connection between (cx; cy) and a path points crosses the border (this means, from (cx; cy),
    /// each path point must be reached directly).
    /// The path will be closed automatically, if it is not closed.
    /// The generated triangles are in the same form as if we would have generated a triangle fan,
    /// but this method returns them as triangle list.
    /// </summary>
    /// <param name="points">The source points which enclose the shape to triangulate.</param>
    /// <param name="cx">X coordinate of an interior point of the <paramref name="points"/>.</param>
    /// <param name="cy">Y coordinate of an interior point of the <paramref name="points"/>.</param>
    /// <param name="zCoord">Z coordinate of the returned vertices.</param>
    /// <param name="verts">Returns a list of vertices describing a triangle fan.</param>
    public static void FillPolygon_TriangleFan(PointF[] points, float cx, float cy, float zCoord, out PositionColoredTextured[] verts)
    {
      verts = null;
      PointF[] pathPoints = AdjustPoints(points);
      int pointCount = pathPoints.Length;
      if (pointCount <= 2) return;
      if (pointCount == 3)
      {
        verts = new PositionColoredTextured[3];

        verts[0].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, zCoord);
        verts[1].Position = new Vector3(pathPoints[1].X, pathPoints[1].Y, zCoord);
        verts[2].Position = new Vector3(pathPoints[2].X, pathPoints[2].Y, zCoord);
        return;
      }
      bool close = pathPoints[0] != pathPoints[pointCount - 1];
      int verticeCount = pointCount + (close ? 2 : 1);

      verts = new PositionColoredTextured[verticeCount];

      verts[0].Position = new Vector3(cx, cy, zCoord); // First point is center point
      for (int i = 0; i < pointCount; i++)
        // Set the outer fan points
        verts[i + 1].Position = new Vector3(pathPoints[i].X, pathPoints[i].Y, zCoord);
      if (close)
        // Last point is the first point to close the shape
        verts[verticeCount - 1].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, zCoord);
    }

    static PointF GetNextPoint(PointF[] points, int i, int max)
    {
      i++;
      while (i >= max) i -= max;
      return points[i];
    }

    /// <summary>
    /// Returns the last line of a closed path, which consists of the last point and the first point.
    /// </summary>
    /// <param name="points">All points of the path.</param>
    /// <param name="line">Point array of length 2 which represents the two points of the last line.</param>
    static void GetLastLine(PointF[] points, out PointF[] line)
    {
      int maxIdx = points.Length;
      line = new PointF[2];
      line[0] = points[maxIdx - 1];
      line[1] = points[0];
    }

    /// <summary>
    /// Does a translatory shift of a given line that consists of 2 points.
    /// </summary>
    /// <param name="point1">First line point.</param>
    /// <param name="point2">Second line point.</param>
    /// <param name="moveDistance">The distance to be moved.</param>
    /// <param name="point1Moved">Returns moved point 1.</param>
    /// <param name="point2Moved">Returns moved point 2.</param>
    static void MoveVector(PointF point1, PointF point2, double moveDistance, ref PointF point1Moved, ref PointF point2Moved)
    {
      Vector3 normalVector = new Vector3(-(point1.Y - point2.Y), point1.X - point2.X, 0);
      normalVector.Normalize();
      point1Moved.X = point1.X + (float) moveDistance * normalVector.X;
      point1Moved.Y = point1.Y + (float) moveDistance * normalVector.Y;

      point2Moved.X = point2.X + (float) moveDistance * normalVector.X;
      point2Moved.Y = point2.Y + (float) moveDistance * normalVector.Y;
    }

    /// <summary>
    /// Calculates the intersection of two lines. If they are parallel the result is PointF.Empty.
    /// This method currently does only calculate 2D intersections.
    /// </summary>
    /// <param name="a1">Line A point 1</param>
    /// <param name="a2">Line A point 2</param>
    /// <param name="b1">Line B point 1</param>
    /// <param name="b2">Line B point 2</param>
    /// <param name="intersection">Returns the intersection</param>
    /// <returns>True if intersection was possible</returns>
    static bool LineIntersect(PointF a1, PointF a2, PointF b1, PointF b2, out PointF intersection)
    {
      float dx = a2.X - a1.X;
      float dy = a2.Y - a1.Y;
      float da = b2.X - b1.X;
      float db = b2.Y - b1.Y;

      if (Math.Abs(da * dy - db * dx) < DELTA_DOUBLE)
      {
        // The segments are parallel.
        intersection = PointF.Empty;
        return false;
      }

      float t = (da * (a1.Y - b1.Y) + db * (b1.X - a1.X)) / (db * dx - da * dy);
      intersection = new PointF(a1.X + t * dx, a1.Y + t * dy);
      return true;
    }

    /// <summary>
    /// Converts the graphics path to an array of vertices using TriangleList.
    /// </summary>
    /// <param name="points">The points of the line.</param>
    /// <param name="thickness">The thickness of the line.</param>
    /// <param name="close">True if we should connect the first and last point.</param>
    /// <param name="zCoord">Z coordinate of the returned vertices.</param>
    /// <param name="lineJoin">The PenLineJoin to use.</param>
    /// <param name="verts">The generated verts.</param>
    public static void TriangulateStroke_TriangleList(PointF[] points, float thickness, bool close, float zCoord, PenLineJoin lineJoin, out PositionColoredTextured[] verts)
    {
      verts = null;
      PointF[] pathPoints = AdjustPoints(points);

      if (pathPoints.Length <= 0)
        return;

      int pointCount;
      if (close)
        pointCount = pathPoints.Length;
      else
        pointCount = pathPoints.Length - 1;

      int pointsLength = pathPoints.Length;
      List<PositionColoredTextured> vertList = new List<PositionColoredTextured>();

      PointF[] lastLine = new PointF[] { PointF.Empty, PointF.Empty };
      if (close)
        GetLastLine(pathPoints, out lastLine);

      for (int i = 0; i < pointCount; i++)
      {
        PointF currentPoint = pathPoints[i];
        PointF nextPoint = GetNextPoint(pathPoints, i, pointsLength);

        PointF movedCurrent = PointF.Empty;
        PointF movedNext = PointF.Empty;

        MoveVector(currentPoint, nextPoint, thickness, ref movedCurrent, ref movedNext);

        if (lastLine[0] != PointF.Empty && lastLine[1] != PointF.Empty)
        {
          // We move the original line by the needed thickness.
          PointF movedLast0 = PointF.Empty;
          PointF movedLast1 = PointF.Empty;
          MoveVector(lastLine[0], lastLine[1], thickness, ref movedLast0, ref movedLast1);

          // StrokeLineJoin implementation
          switch (lineJoin)
          {
            case PenLineJoin.Round:
            // We fallback to the Miter because we don't support the Round line join yet.
            case PenLineJoin.Miter:
              // We need to calculate the intersection of the 2 moved lines (Line A: movedCurrent/movedNext and Line B: movedLast0/movedLast1)
              PointF intersection;
              if (LineIntersect(movedCurrent, movedNext, movedLast0, movedLast1, out intersection))
              {
                vertList.Add(new PositionColoredTextured { Position = new Vector3(currentPoint.X, currentPoint.Y, zCoord) });
                vertList.Add(new PositionColoredTextured { Position = new Vector3(movedCurrent.X, movedCurrent.Y, zCoord) });
                vertList.Add(new PositionColoredTextured { Position = new Vector3(intersection.X, intersection.Y, zCoord) });

                vertList.Add(new PositionColoredTextured { Position = new Vector3(currentPoint.X, currentPoint.Y, zCoord) });
                vertList.Add(new PositionColoredTextured { Position = new Vector3(movedLast1.X, movedLast1.Y, zCoord) });
                vertList.Add(new PositionColoredTextured { Position = new Vector3(intersection.X, intersection.Y, zCoord) });
              }
              break;
            case PenLineJoin.Bevel:
              // This is currently not the exact WPF "Bevel" implementation, we only insert a simple triangle between the line ends.
              vertList.Add(new PositionColoredTextured { Position = new Vector3(currentPoint.X, currentPoint.Y, zCoord) });
              vertList.Add(new PositionColoredTextured { Position = new Vector3(movedCurrent.X, movedCurrent.Y, zCoord) });
              vertList.Add(new PositionColoredTextured { Position = new Vector3(movedLast1.X, movedLast1.Y, zCoord) });
              break;
          }
        }

        vertList.Add(new PositionColoredTextured { Position = new Vector3(currentPoint.X, currentPoint.Y, zCoord) });
        vertList.Add(new PositionColoredTextured { Position = new Vector3(nextPoint.X, nextPoint.Y, zCoord) });
        vertList.Add(new PositionColoredTextured { Position = new Vector3(movedCurrent.X, movedCurrent.Y, zCoord) });

        vertList.Add(new PositionColoredTextured { Position = new Vector3(nextPoint.X, nextPoint.Y, zCoord) });
        vertList.Add(new PositionColoredTextured { Position = new Vector3(movedNext.X, movedNext.Y, zCoord) });
        vertList.Add(new PositionColoredTextured { Position = new Vector3(movedCurrent.X, movedCurrent.Y, zCoord) });

        lastLine = new PointF[] { currentPoint, nextPoint };
      }
      verts = vertList.ToArray();
    }

    /// <summary>
    /// Creates a <see cref="PrimitiveType.TriangleList"/> of vertices which cover the interior of the
    /// specified <paramref name="points"/>. The path must be closed and describe a simple polygon.
    /// </summary>
    /// <param name="points">Points describing the border of a simple polygon.</param>
    /// <param name="zCoord">Z coordinate of the created vertices.</param>
    /// <param name="verts">Returns a <see cref="PrimitiveType.TriangleList"/> of vertices.</param>
    public static void Triangulate(PointF[] points, float zCoord, out PositionColoredTextured[] verts)
    {
      PointF[] pathPoints = AdjustPoints(points);
      if (pathPoints.Length < 3)
      {
        verts = null;
        return;
      }
      if (pathPoints.Length == 3)
      {
        verts = new PositionColoredTextured[3];

        verts[0].Position = new Vector3(pathPoints[0].X, pathPoints[0].Y, zCoord);
        verts[1].Position = new Vector3(pathPoints[1].X, pathPoints[1].Y, zCoord);
        verts[2].Position = new Vector3(pathPoints[2].X, pathPoints[2].Y, zCoord);
        return;
      }

      IList<DelaunayTriangle> polygons;
      try
      {
        // Triangulation can fail (i.e. polygon is self-intersecting)
        var poly = new Poly2Tri.Polygon(pathPoints.Select(p => new PolygonPoint(p.X, p.Y)));
        P2T.Triangulate(poly);
        polygons = poly.Triangles;
      }
      catch (Exception)
      {
        verts = null;
        return;
      }

      verts = new PositionColoredTextured[polygons.Count * 3];
      int offset = 0;
      foreach (DelaunayTriangle triangle in polygons)
      {
        verts[offset++].Position = new Vector3((float)triangle.Points[0].X, (float)triangle.Points[0].Y, zCoord);
        verts[offset++].Position = new Vector3((float)triangle.Points[1].X, (float)triangle.Points[1].Y, zCoord);
        verts[offset++].Position = new Vector3((float)triangle.Points[2].X, (float)triangle.Points[2].Y, zCoord);
      }
    }

    /// <summary>
    /// Describes how points should be painted in a LineStrip relative to its center.
    /// </summary>
    /// <remarks>
    /// The behavior of the <see cref="LeftHanded"/> and <see cref="RightHanded"/> modes depends on the order the points are
    /// listed in. <see cref="LeftHanded"/> will draw the line on the outside of a clockwise curve and on the
    /// inside of a counterclockwise curve; <see cref="RightHanded"/> is the opposite.
    /// </remarks>
    public enum WidthMode
    {
      /// <summary>
      /// Centers the width on the line.
      /// </summary>
      Centered,

      /// <summary>
      /// Places the width on the left-hand side of the line.
      /// </summary>
      LeftHanded,

      /// <summary>
      /// Places the width on the right-hand side of the line.
      /// </summary>
      RightHanded
    }

    /// <summary>
    /// Generates the vertices of a thickened line strip.
    /// </summary>
    /// <param name="points">Points of the line strip</param>
    /// <param name="thickness">Thickness of the line</param>
    /// <param name="close">Whether to connect the last point back to the first</param>
    /// <param name="widthMode">How to place the weight of the line relative to it</param>
    /// <param name="zCoord">Z coordinate of the returned vertices.</param>
    /// <param name="verts">Generated vertices.</param>
    public static void CalculateLinePoints(PointF[] points, float thickness, bool close, WidthMode widthMode, float zCoord,
        out PositionColoredTextured[] verts)
    {
      PointF[] pathPoints = AdjustPoints(points);
      verts = null;
      if (pathPoints.Length < 3)
      {
        if (close) return;
        if (pathPoints.Length < 2)
          return;
      }

      int count = pathPoints.Length;
      if (pathPoints[count - 2] == pathPoints[count - 1])
        count--;
      Vector2[] vPoints = new Vector2[count];
      for (int i = 0; i < count; ++i)
        vPoints[i] = new Vector2(pathPoints[i].X, pathPoints[i].Y);

      Vector2 innerDistance = new Vector2(0, 0);
      switch (widthMode)
      {
        case WidthMode.Centered:
          //innerDistance =thickness / 2;
          innerDistance = new Vector2(thickness / 2, thickness / 2);
          break;
        case WidthMode.LeftHanded:
          //innerDistance = -thickness;
          innerDistance = new Vector2(-thickness, -thickness);
          break;
        case WidthMode.RightHanded:
          //innerDistance = thickness;
          innerDistance = new Vector2(thickness, thickness);
          break;
      }

      Vector2[] outPoints = new Vector2[(vPoints.Length + (close ? 1 : 0)) * 2];

      float slope, intercept;
      //Get the endpoints
      if (close)
      {
        //Get the overlap points
        int lastIndex = outPoints.Length - 4;
        outPoints[lastIndex] = InnerPoint(innerDistance, vPoints[vPoints.Length - 2], vPoints[vPoints.Length - 1], vPoints[0], out slope, out intercept);
        outPoints[0] = InnerPoint(innerDistance, ref slope, ref intercept, outPoints[lastIndex], vPoints[0], vPoints[1]);
      }
      else
      {
        //Take endpoints based on the end segments' normals alone
        outPoints[0] = Vector2.Multiply(innerDistance, GetNormal(vPoints[1] - vPoints[0]));
        outPoints[0] = vPoints[0] + outPoints[0];

        //outPoints[0] = points[0] + innerDistance * normal(points[1] - points[0]);
        Vector2 norm = Vector2.Multiply(innerDistance, GetNormal(vPoints[vPoints.Length - 1] - vPoints[vPoints.Length - 2])); //DEBUG

        outPoints[outPoints.Length - 2] = vPoints[vPoints.Length - 1] + norm;

        //Get the slope and intercept of the first segment to feed into the middle loop
        slope = VectorSlope(vPoints[1] - vPoints[0]);
        intercept = LineIntercept(outPoints[0], slope);
      }

      //Get the middle points
      for (int i = 1; i < vPoints.Length - 1; i++)
        outPoints[2 * i] = InnerPoint(innerDistance, ref slope, ref intercept, outPoints[2 * (i - 1)], vPoints[i], vPoints[i + 1]);

      //Derive the outer points from the inner points
      if (widthMode == WidthMode.Centered)
        for (int i = 0; i < vPoints.Length; i++)
          outPoints[2 * i + 1] = 2 * vPoints[i] - outPoints[2 * i];
      else
        for (int i = 0; i < vPoints.Length; i++)
          outPoints[2 * i + 1] = vPoints[i];

      //Closed strips must repeat the first two points
      if (close)
      {
        outPoints[outPoints.Length - 2] = outPoints[0];
        outPoints[outPoints.Length - 1] = outPoints[1];
      }
      int verticeCount = outPoints.Length;
      verts = new PositionColoredTextured[verticeCount];

      for (int i = 0; i < verticeCount; ++i)
        verts[i].Position = new Vector3(outPoints[i].X, outPoints[i].Y, zCoord);
    }

    protected static void ZCross(ref PointF left, ref PointF right, out double result)
    {
      result = left.X * right.Y - left.Y * right.X;
    }

    public static void CalcCentroid(PointF[] points, out float cx, out float cy)
    {
      PointF[] pathPoints = AdjustPoints(points);
      int pointCount = pathPoints.Length;
      if (pointCount == 0)
      {
        cx = 0;
        cy = 0;
        return;
      }
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
        centroid.X += (float) ((v1.X + v2.X) * temp);
        centroid.Y += (float) ((v1.Y + v2.Y) * temp);
      }
      temp = 1 / (Math.Abs(area) * 3);
      centroid.X *= (float) temp;
      centroid.Y *= (float) temp;

      cx = Math.Abs(centroid.X);
      cy = Math.Abs(centroid.Y);
    }

    /// <summary>
    /// Removes double point entries or points which are almost the same.
    /// </summary>
    /// <param name="points">Array of points to adjust.</param>
    /// <returns>Adjusted point array. May be smaller than the input <paramref name="points"/> array.</returns>
    public static PointF[] AdjustPoints(PointF[] points)
    {
      List<PointF> result = new List<PointF>(points.Length);
      PointF? last = null;
      foreach (PointF point in points)
      {
        if (last.HasValue && SamePoints(last.Value, point))
          continue;
        last = point;
        result.Add(point);
      }
      // Check if the last point of the list is same as the start point, then remove it
      if (result.Count > 2 && SamePoints(result[0], result[result.Count - 1]))
        result.Remove(result[result.Count - 1]);
      return result.ToArray();
    }

    public static bool SamePoints(PointF point1, PointF point2)
    {
      return Math.Abs(point1.X - point2.X) < DELTA_DOUBLE && Math.Abs(point1.Y - point2.Y) < DELTA_DOUBLE;
    }

    #endregion

    #region Math helpers

    /// <summary>the slope of v, or NaN if it is nearly vertical</summary>
    /// <param name="v">Vector to take slope from</param>
    private static float VectorSlope(Vector2 v)
    {
      return Math.Abs(v.X) < 0.001f ? float.NaN : (v.Y / v.X);
    }

    /// <summary>Finds the intercept of a line</summary>
    /// <param name="point">A point on the line</param>
    /// <param name="slope">The slope of the line</param>
    private static float LineIntercept(Vector2 point, float slope)
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
    public static Vector2 InnerPoint(Vector2 distance, Vector2 lastPoint, Vector2 point, Vector2 nextPoint, out float slope, out float intercept)
    {
      Vector2 lastDifference = point - lastPoint;
      slope = VectorSlope(lastDifference);
      intercept = LineIntercept(lastPoint + Vector2.Multiply(distance, GetNormal(lastDifference)), slope);
      return InnerPoint(distance, ref slope, ref intercept, lastPoint + Vector2.Multiply(distance, GetNormal(lastDifference)), point, nextPoint);
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
    private static Vector2 InnerPoint(Vector2 distance, ref float lastSlope, ref float lastIntercept, Vector2 lastInnerPoint, Vector2 point, Vector2 nextPoint)
    {
      Vector2 edgeVector = nextPoint - point;
      //Vector2 innerPoint = nextPoint + distance * normal(edgeVector);
      Vector2 innerPoint = Vector2.Multiply(distance, GetNormal(edgeVector));

      innerPoint = nextPoint + innerPoint;

      float slope = VectorSlope(edgeVector);
      float intercept = LineIntercept(innerPoint, slope);

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

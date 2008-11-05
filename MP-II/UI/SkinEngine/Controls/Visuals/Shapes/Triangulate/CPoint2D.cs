using System;

namespace GeometryUtility
{
  /// <summary>
  /// Summary description for CPoint2D.
  /// </summary>

  //A point in Coordinate System
  public class CPoint2D
  {
    private float m_dCoordinate_X;
    private float m_dCoordinate_Y;

    public CPoint2D()
    {

    }

    public CPoint2D(float xCoordinate, float yCoordinate)
    {
      m_dCoordinate_X = xCoordinate;
      m_dCoordinate_Y = yCoordinate;
    }

    public float X
    {
      set
      {
        m_dCoordinate_X = value;
      }
      get
      {
        return m_dCoordinate_X;
      }
    }

    public float Y
    {
      set
      {
        m_dCoordinate_Y = value;
      }
      get
      {
        return m_dCoordinate_Y;
      }
    }

    public static bool SamePoints(CPoint2D Point1, CPoint2D Point2)
    {

      float dDeff_X =
        Math.Abs(Point1.X - Point2.X);
      float dDeff_Y =
        Math.Abs(Point1.Y - Point2.Y);

      if ((dDeff_X < ConstantValue.SmallValue) && (dDeff_Y < ConstantValue.SmallValue))
        return true;
      else
        return false;
    }

    public bool EqualsPoint(CPoint2D newPoint)
    {

      float dDeff_X = Math.Abs(m_dCoordinate_X - newPoint.X);
      float dDeff_Y = Math.Abs(m_dCoordinate_Y - newPoint.Y);

      if ((dDeff_X < ConstantValue.SmallValue) && (dDeff_Y < ConstantValue.SmallValue))
        return true;
      else
        return false;

    }

    /***To check whether the point is in a line segment***/
    public bool InLine(CLineSegment lineSegment)
    {
      bool bInline = false;

      float Ax, Ay, Bx, By, Cx, Cy;
      Bx = lineSegment.EndPoint.X;
      By = lineSegment.EndPoint.Y;
      Ax = lineSegment.StartPoint.X;
      Ay = lineSegment.StartPoint.Y;
      Cx = this.m_dCoordinate_X;
      Cy = this.m_dCoordinate_Y;

      float L = lineSegment.GetLineSegmentLength();
      float s = Math.Abs(((Ay - Cy) * (Bx - Ax) - (Ax - Cx) * (By - Ay)) / (L * L));

      if (Math.Abs(s - 0) < ConstantValue.SmallValue)
      {
        if ((SamePoints(this, lineSegment.StartPoint)) || (SamePoints(this, lineSegment.EndPoint)))
          bInline = true;
        else if ((Cx < lineSegment.GetXmax()) && (Cx > lineSegment.GetXmin()) && (Cy < lineSegment.GetYmax()) && (Cy > lineSegment.GetYmin()))
          bInline = true;
      }
      return bInline;
    }

    /*** Distance between two points***/
    public float DistanceTo(CPoint2D point)
    {
      return (float)Math.Sqrt((point.X - this.X) * (point.X - this.X) + (point.Y - this.Y) * (point.Y - this.Y));

    }

    public bool PointInsidePolygon(CPoint2D[] polygonVertices)
    {
      if (polygonVertices.Length < 3) //not a valid polygon
        return false;

      int nCounter = 0;
      int nPoints = polygonVertices.Length;

      CPoint2D s1, p1, p2;
      s1 = this;
      p1 = polygonVertices[0];

      for (int i = 1; i < nPoints; i++)
      {
        p2 = polygonVertices[i % nPoints];
        if (s1.Y > Math.Min(p1.Y, p2.Y))
        {
          if (s1.Y <= Math.Max(p1.Y, p2.Y))
          {
            if (s1.X <= Math.Max(p1.X, p2.X))
            {
              if (p1.Y != p2.Y)
              {
                float xInters = (s1.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
                if ((p1.X == p2.X) || (s1.X <= xInters))
                {
                  nCounter++;
                }
              }  //p1.y != p2.y
            }
          }
        }
        p1 = p2;
      } //for loop

      if ((nCounter % 2) == 0)
        return false;
      else
        return true;
    }

    /*********** Sort points from Xmin->Xmax ******/
    public static void SortPointsByX(CPoint2D[] points)
    {
      if (points.Length > 1)
      {
        CPoint2D tempPt;
        for (int i = 0; i < points.Length - 2; i++)
        {
          for (int j = i + 1; j < points.Length - 1; j++)
          {
            if (points[i].X > points[j].X)
            {
              tempPt = points[j];
              points[j] = points[i];
              points[i] = tempPt;
            }
          }
        }
      }
    }

    /*********** Sort points from Ymin->Ymax ******/
    public static void SortPointsByY(CPoint2D[] points)
    {
      if (points.Length > 1)
      {
        CPoint2D tempPt;
        for (int i = 0; i < points.Length - 2; i++)
        {
          for (int j = i + 1; j < points.Length - 1; j++)
          {
            if (points[i].Y > points[j].Y)
            {
              tempPt = points[j];
              points[j] = points[i];
              points[i] = tempPt;
            }
          }
        }
      }
    }

  }
}

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

/**************************************************
This unit is used to collect Analytic Geometry formulars
It includes Line, Line segment and CPolygon				
																				
Development by: Frank Shen                                    
Date: 08, 2004                                                         
Modification History:													
* *** **********************************************/

using System;
namespace MediaPortal.UI.SkinEngine.DirectX.Triangulate
{
  /// <summary>
  ///To define a line in the given coordinate system
  ///and related calculations
  ///Line Equation:ax+by+c=0
  ///</summary>

  //a Line in 2D coordinate system: ax+by+c=0
  public class CLine
  {
    //line: ax+by+c=0;
    protected float a;
    protected float b;
    protected float c;

    private void Initialize(float angleInRad, CPoint2D point)
    {
      //angleInRad should be between 0-Pi

      try
      {
        //if ((angleInRad<0) ||(angleInRad>Math.PI))
        if (angleInRad > 2 * Math.PI)
        {
          string errMsg = string.Format("The input line angle" + " {0} is wrong. It should be between 0-2*PI.", angleInRad);

          InvalidInputGeometryDataException ex = new InvalidInputGeometryDataException(errMsg);

          throw ex;
        }

        if (Math.Abs(angleInRad - Math.PI / 2) < ConstantValue.SmallValue) //vertical line
        {
          a = 1;
          b = 0;
          c = -point.X;
        }
        else //not vertical line
        {
          a = (float)-Math.Tan(angleInRad);
          b = 1;
          c = -a * point.X - b * point.Y;
        }
      }
      catch (Exception e)
      {
        System.Diagnostics.Trace.WriteLine(e.Message + e.StackTrace);
      }
    }


    public CLine(float angleInRad, CPoint2D point)
    {
      Initialize(angleInRad, point);
    }

    public CLine(CPoint2D point1, CPoint2D point2)
    {
      try
      {
        if (CPoint2D.SamePoints(point1, point2))
        {
          string errMsg = "The input points are the same";
          InvalidInputGeometryDataException ex = new InvalidInputGeometryDataException(errMsg);
          throw ex;
        }

        //Point1 and Point2 are different points:
        if (Math.Abs(point1.X - point2.X) < ConstantValue.SmallValue) //vertical line
        {
          Initialize((float)Math.PI / 2, point1);
        }
        else if (Math.Abs(point1.Y - point2.Y) < ConstantValue.SmallValue) //Horizontal line
        {
          Initialize(0, point1);
        }
        else //normal line
        {
          float m = (point2.Y - point1.Y) / (point2.X - point1.X);
          float alphaInRad = (float)Math.Atan(m);
          Initialize(alphaInRad, point1);
        }
      }
      catch (Exception e)
      {
        System.Diagnostics.Trace.WriteLine(e.Message + e.StackTrace);
      }
    }

    public CLine(CLine copiedLine)
    {
      this.a = copiedLine.a;
      this.b = copiedLine.b;
      this.c = copiedLine.c;
    }

    /*** calculate the distance from a given point to the line ***/
    public float GetDistance(CPoint2D point)
    {
      float x0 = point.X;
      float y0 = point.Y;

      float d = (float)Math.Abs(a * x0 + b * y0 + c);
      d = d / ((float)(Math.Sqrt(a * a + b * b)));

      return d;
    }

    /*** point(x, y) in the line, based on y, calculate x ***/
    public float GetX(float y)
    {
      //if the line is a horizontal line (a=0), it will return a NaN:
      float x;
      try
      {
        if (Math.Abs(a) < ConstantValue.SmallValue) //a=0;
        {
          throw new NonValidReturnException();
        }

        x = -(b * y + c) / a;
      }
      catch (Exception e)  //Horizontal line a=0;
      {
        //x = System.Float.NaN;
        x = 0;
        System.Diagnostics.Trace.WriteLine(e.Message + e.StackTrace);
      }

      return x;
    }

    /*** point(x, y) in the line, based on x, calculate y ***/
    public float GetY(float x)
    {
      //if the line is a vertical line, it will return a NaN:
      float y;
      try
      {
        if (Math.Abs(b) < ConstantValue.SmallValue)
        {
          throw new NonValidReturnException();
        }
        y = -(a * x + c) / b;
      }
      catch (Exception e)
      {
        //y = System.Float.NaN;
        y = 0;
        System.Diagnostics.Trace.WriteLine(e.Message + e.StackTrace);
      }
      return y;
    }

    /*** is it a vertical line:***/
    public bool VerticalLine()
    {
      if (Math.Abs(b - 0) < ConstantValue.SmallValue)
        return true;
      else
        return false;
    }

    /*** is it a horizontal line:***/
    public bool HorizontalLine()
    {
      if (Math.Abs(a - 0) < ConstantValue.SmallValue)
        return true;
      else
        return false;
    }

    /*** calculate line angle in radian: ***/
    public float GetLineAngle()
    {
      if (b == 0)
      {
        return (float)Math.PI / 2;
      }
      else //b!=0
      {
        float tanA = -a / b;
        return (float)Math.Atan(tanA);
      }
    }

    public bool Parallel(CLine line)
    {
      bool bParallel = false;
      if (this.a / this.b == line.a / line.b)
        bParallel = true;

      return bParallel;
    }

    /**************************************
     Calculate intersection point of two lines
     if two lines are parallel, return null
     * ************************************/
    public CPoint2D IntersecctionWith(CLine line)
    {
      CPoint2D point = new CPoint2D();
      float a1 = this.a;
      float b1 = this.b;
      float c1 = this.c;

      float a2 = line.a;
      float b2 = line.b;
      float c2 = line.c;

      if (!(this.Parallel(line))) //not parallen
      {
        point.X = (c2 * b1 - c1 * b2) / (a1 * b2 - a2 * b1);
        point.Y = (a1 * c2 - c1 * a2) / (a2 * b2 - a1 * b2);
      }
      return point;
    }
  }

  public class CLineSegment : CLine
  {
    //line: ax+by+c=0, with start point and end point
    //direction from start point ->end point
    private CPoint2D m_startPoint;
    private CPoint2D m_endPoint;

    public CPoint2D StartPoint
    {
      get
      {
        return m_startPoint;
      }
    }

    public CPoint2D EndPoint
    {
      get
      {
        return m_endPoint;
      }
    }

    public CLineSegment(CPoint2D startPoint, CPoint2D endPoint)
      : base(startPoint, endPoint)
    {
      this.m_startPoint = startPoint;
      this.m_endPoint = endPoint;
    }

    /*** chagne the line's direction ***/
    public void ChangeLineDirection()
    {
      CPoint2D tempPt;
      tempPt = this.m_startPoint;
      this.m_startPoint = this.m_endPoint;
      this.m_endPoint = tempPt;
    }

    /*** To calculate the line segment length:   ***/
    public float GetLineSegmentLength()
    {
      float d = (m_endPoint.X - m_startPoint.X) * (m_endPoint.X - m_startPoint.X);
      d += (m_endPoint.Y - m_startPoint.Y) * (m_endPoint.Y - m_startPoint.Y);
      d = (float)Math.Sqrt(d);

      return d;
    }

    /********************************************************** 
      Get point location, using windows coordinate system: 
      y-axes points down.
      Return Value:
      -1:point at the left of the line (or above the line if the line is horizontal)
       0: point in the line segment or in the line segment 's extension
       1: point at right of the line (or below the line if the line is horizontal)    
     ***********************************************************/
    public int GetPointLocation(CPoint2D point)
    {
      float Ax, Ay, Bx, By, Cx, Cy;
      Bx = m_endPoint.X;
      By = m_endPoint.Y;

      Ax = m_startPoint.X;
      Ay = m_startPoint.Y;

      Cx = point.X;
      Cy = point.Y;

      if (this.HorizontalLine())
      {
        if (Math.Abs(Ay - Cy) < ConstantValue.SmallValue) //equal
          return 0;
        else if (Ay > Cy)
          return -1;   //Y Axis points down, point is above the line
        else //Ay<Cy
          return 1;    //Y Axis points down, point is below the line
      }
      else //Not a horizontal line
      {
        //make the line direction bottom->up
        if (m_endPoint.Y > m_startPoint.Y)
          this.ChangeLineDirection();

        float L = this.GetLineSegmentLength();
        float s = ((Ay - Cy) * (Bx - Ax) - (Ax - Cx) * (By - Ay)) / (L * L);

        //Note: the Y axis is pointing down:
        if (Math.Abs(s - 0) < ConstantValue.SmallValue) //s=0
          return 0; //point is in the line or line extension
        else if (s > 0)
          return -1; //point is left of line or above the horizontal line
        else //s<0
          return 1;
      }
    }

    /***Get the minimum x value of the points in the line***/
    public float GetXmin()
    {
      return (float)Math.Min(m_startPoint.X, m_endPoint.X);
    }

    /***Get the maximum  x value of the points in the line***/
    public float GetXmax()
    {
      return (float)Math.Max(m_startPoint.X, m_endPoint.X);
    }

    /***Get the minimum y value of the points in the line***/
    public float GetYmin()
    {
      return (float)Math.Min(m_startPoint.Y, m_endPoint.Y);
    }

    /***Get the maximum y value of the points in the line***/
    public float GetYmax()
    {
      return (float)Math.Max(m_startPoint.Y, m_endPoint.Y);
    }

    /***Check whether this line is in a longer line***/
    public bool InLine(CLineSegment longerLineSegment)
    {
      bool bInLine = false;
      if ((m_startPoint.InLine(longerLineSegment)) && (m_endPoint.InLine(longerLineSegment)))
        bInLine = true;
      return bInLine;
    }

    /************************************************
     * Offset the line segment to generate a new line segment
     * If the offset direction is along the x-axis or y-axis, 
     * Parameter is true, other wise it is false
     * ***********************************************/
    public CLineSegment OffsetLine(float distance, bool rightOrDown)
    {
      //offset a line with a given distance, generate a new line
      //rightOrDown=true means offset to x incress direction,
      // if the line is horizontal, offset to y incress direction

      CLineSegment line;
      CPoint2D newStartPoint = new CPoint2D();
      CPoint2D newEndPoint = new CPoint2D();

      float alphaInRad = this.GetLineAngle(); // 0-PI
      if (rightOrDown)
      {
        if (this.HorizontalLine()) //offset to y+ direction
        {
          newStartPoint.X = this.m_startPoint.X;
          newStartPoint.Y = this.m_startPoint.Y + distance;

          newEndPoint.X = this.m_endPoint.X;
          newEndPoint.Y = this.m_endPoint.Y + distance;
          line = new CLineSegment(newStartPoint, newEndPoint);
        }
        else //offset to x+ direction
        {
          if (Math.Sin(alphaInRad) > 0)
          {
            newStartPoint.X = (float)(m_startPoint.X + Math.Abs(distance * Math.Sin(alphaInRad)));
            newStartPoint.Y = (float)(m_startPoint.Y - Math.Abs(distance * Math.Cos(alphaInRad)));

            newEndPoint.X = (float)(m_endPoint.X + Math.Abs(distance * Math.Sin(alphaInRad)));
            newEndPoint.Y = (float)(m_endPoint.Y - Math.Abs(distance * Math.Cos(alphaInRad)));

            line = new CLineSegment(newStartPoint, newEndPoint);
          }
          else //sin(FalphaInRad)<0
          {
            newStartPoint.X = (float)(m_startPoint.X + Math.Abs(distance * Math.Sin(alphaInRad)));
            newStartPoint.Y = (float)(m_startPoint.Y + Math.Abs(distance * Math.Cos(alphaInRad)));
            newEndPoint.X = (float)(m_endPoint.X + Math.Abs(distance * Math.Sin(alphaInRad)));
            newEndPoint.Y = (float)(m_endPoint.Y + Math.Abs(distance * Math.Cos(alphaInRad)));

            line = new CLineSegment(newStartPoint, newEndPoint);
          }
        }
      }//{rightOrDown}
      else //leftOrUp
      {
        if (this.HorizontalLine()) //offset to y directin
        {
          newStartPoint.X = m_startPoint.X;
          newStartPoint.Y = m_startPoint.Y - distance;

          newEndPoint.X = m_endPoint.X;
          newEndPoint.Y = m_endPoint.Y - distance;
          line = new CLineSegment(newStartPoint, newEndPoint);
        }
        else //offset to x directin
        {
          if (Math.Sin(alphaInRad) >= 0)
          {
            newStartPoint.X = (float)(m_startPoint.X - Math.Abs(distance * Math.Sin(alphaInRad)));
            newStartPoint.Y = (float)(m_startPoint.Y + Math.Abs(distance * Math.Cos(alphaInRad)));
            newEndPoint.X = (float)(m_endPoint.X - Math.Abs(distance * Math.Sin(alphaInRad)));
            newEndPoint.Y = (float)(m_endPoint.Y + Math.Abs(distance * Math.Cos(alphaInRad)));

            line = new CLineSegment(
              newStartPoint, newEndPoint);
          }
          else //sin(FalphaInRad)<0
          {
            newStartPoint.X = (float)(m_startPoint.X - Math.Abs(distance * Math.Sin(alphaInRad)));
            newStartPoint.Y = (float)(m_startPoint.Y - Math.Abs(distance * Math.Cos(alphaInRad)));
            newEndPoint.X = (float)(m_endPoint.X - Math.Abs(distance * Math.Sin(alphaInRad)));
            newEndPoint.Y = (float)(m_endPoint.Y - Math.Abs(distance * Math.Cos(alphaInRad)));

            line = new CLineSegment(newStartPoint, newEndPoint);
          }
        }
      }
      return line;
    }

    /********************************************************
    To check whether 2 lines segments have an intersection
    *********************************************************/
    public bool IntersectedWith(CLineSegment line)
    {
      float x1 = this.m_startPoint.X;
      float y1 = this.m_startPoint.Y;
      float x2 = this.m_endPoint.X;
      float y2 = this.m_endPoint.Y;
      float x3 = line.m_startPoint.X;
      float y3 = line.m_startPoint.Y;
      float x4 = line.m_endPoint.X;
      float y4 = line.m_endPoint.Y;

      float de = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
      //if de<>0 then //lines are not parallel
      if (Math.Abs(de - 0) > ConstantValue.SmallValue) //not parallel
      {
        float ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / de;
        float ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / de;

        if ((ua > 0) && (ua < 1) && (ub > 0) && (ub < 1))
          return true;
        else
          return false;
      }
      else	//lines are parallel
        return false;
    }

  }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using GeometryUtility;

namespace GeometryUtility
{
  public class CPolygonShape
  {
    //private CPoint2D[] m_aInputVertices;
    private CPoint2D[] m_aUpdatedPolygonVertices;

    private List<CPoint2D[]> m_alEars = new List<CPoint2D[]>();
    private CPoint2D[][] m_aPolygons;

    public int NumberOfPolygons
    {
      get
      {
        return m_aPolygons.Length;
      }
    }
    public CPoint2D[] this[int index]
    {
      get
      {
        return m_aPolygons[index];
      }
    }
    public CPoint2D[] Polygons(int index)
    {
      if (index < m_aPolygons.Length)
        return m_aPolygons[index];
      else
        return null;
    }

    public CPolygonShape(GraphicsPath path, bool isClosed)
    {
      int nVertices = path.PointCount;
      if (nVertices < 3)
      {
        System.Diagnostics.Trace.WriteLine("To make a polygon, " + " at least 3 points are required!");
        return;
      }

      List<PointF> points = new List<PointF>();
      PointF[] pathPoints = path.PathPoints;
      points.Add(pathPoints[0]);
      for (int i = 1; i < nVertices; ++i)
      {
        points.Add(pathPoints[i]);
      }

      if (points.Count >= 2)
      {
        if (points[points.Count - 1] == points[points.Count - 2])
        {
          points.RemoveAt(points.Count - 1);
        }
      }

      m_aUpdatedPolygonVertices = new CPoint2D[points.Count];
      for (int i = 0; i < points.Count; i++)
        m_aUpdatedPolygonVertices[i] = new CPoint2D(points[i].X, points[i].Y);

      //m_aUpdatedPolygonVertices should be in count clock wise
      if (CPolygon.PointsDirection(m_aUpdatedPolygonVertices) == PolygonDirection.Clockwise)
        CPolygon.ReversePointsDirection(m_aUpdatedPolygonVertices);

      //make a working copy,  m_aUpdatedPolygonVertices are
      //in count clock direction from user view
      // SetUpdatedPolygonVertices();
    }
    /*
    public CPolygonShape(CPoint2D[] vertices)
    {
      int nVertices = vertices.Length;
      if (nVertices < 3)
      {
        System.Diagnostics.Trace.WriteLine("To make a polygon, "
          + " at least 3 points are required!");
        return;
      }

      //initalize the 2D points
      m_aInputVertices = new CPoint2D[nVertices];

      for (int i = 0; i < nVertices; i++)
        m_aInputVertices[i] = vertices[i];

      //make a working copy,  m_aUpdatedPolygonVertices are
      //in count clock direction from user view
      SetUpdatedPolygonVertices();
    }
    private void SetUpdatedPolygonVertices()
    {
      int nVertices = m_aInputVertices.Length;
      m_aUpdatedPolygonVertices = new CPoint2D[nVertices];

      for (int i = 0; i < nVertices; i++)
        m_aUpdatedPolygonVertices[i] = m_aInputVertices[i];

      //m_aUpdatedPolygonVertices should be in count clock wise
      if (CPolygon.PointsDirection(m_aUpdatedPolygonVertices) == PolygonDirection.Clockwise)
        CPolygon.ReversePointsDirection(m_aUpdatedPolygonVertices);
    }*/

    /**********************************************************
    To check the Pt is in the Triangle or not.
    If the Pt is in the line or is a vertex, then return true.
    If the Pt is out of the Triangle, then return false.

    This method is used for triangle only.
    ***********************************************************/
    private bool TriangleContainsPoint(CPoint2D[] trianglePts, CPoint2D pt)
    {
      if (trianglePts.Length != 3)
        return false;

      for (int i = trianglePts.GetLowerBound(0); i < trianglePts.GetUpperBound(0); i++)
      {
        if (pt.EqualsPoint(trianglePts[i]))
          return true;
      }

      bool bIn = false;

      CLineSegment line0 = new CLineSegment(trianglePts[0], trianglePts[1]);
      CLineSegment line1 = new CLineSegment(trianglePts[1], trianglePts[2]);
      CLineSegment line2 = new CLineSegment(trianglePts[2], trianglePts[0]);

      if (pt.InLine(line0) || pt.InLine(line1) || pt.InLine(line2))
        bIn = true;
      else //point is not in the lines
      {
        double dblArea0 = CPolygon.PolygonArea(new CPoint2D[] { trianglePts[0], trianglePts[1], pt });
        double dblArea1 = CPolygon.PolygonArea(new CPoint2D[] { trianglePts[1], trianglePts[2], pt });
        double dblArea2 = CPolygon.PolygonArea(new CPoint2D[] { trianglePts[2], trianglePts[0], pt });

        if (dblArea0 > 0)
        {
          if ((dblArea1 > 0) && (dblArea2 > 0))
            bIn = true;
        }
        else if (dblArea0 < 0)
        {
          if ((dblArea1 < 0) && (dblArea2 < 0))
            bIn = true;
        }
      }
      return bIn;
    }


    /****************************************************************
    To check whether the Vertex is an ear or not based updated Polygon vertices

    ref. www-cgrl.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian
    /algorithm1.html

    If it is an ear, return true,
    If it is not an ear, return false;
    *****************************************************************/
    private bool IsEarOfUpdatedPolygon(CPoint2D vertex)
    {
      CPolygon polygon = new CPolygon(m_aUpdatedPolygonVertices);

      if (polygon.PolygonVertex(vertex))
      {
        bool bEar = true;
        if (polygon.PolygonVertexType(vertex) == VertexType.ConvexPoint)
        {
          CPoint2D pi = vertex;
          CPoint2D pj = polygon.PreviousPoint(vertex); //previous vertex
          CPoint2D pk = polygon.NextPoint(vertex);//next vertex

          for (int i = m_aUpdatedPolygonVertices.GetLowerBound(0); i < m_aUpdatedPolygonVertices.GetUpperBound(0); i++)
          {
            CPoint2D pt = m_aUpdatedPolygonVertices[i];
            if (!(pt.EqualsPoint(pi) || pt.EqualsPoint(pj) || pt.EqualsPoint(pk)))
            {
              if (TriangleContainsPoint(new CPoint2D[] { pj, pi, pk }, pt))
                bEar = false;
            }
          }
        } //ThePolygon.getVertexType(Vertex)=ConvexPt
        else  //concave point
          bEar = false; //not an ear/
        return bEar;
      }
      else //not a polygon vertex;
      {
        System.Diagnostics.Trace.WriteLine("IsEarOfUpdatedPolygon: " +
          "Not a polygon vertex");
        return false;
      }
    }

    /****************************************************
    Set up m_aPolygons:
    add ears and been cut Polygon togather
    ****************************************************/
    private void SetPolygons()
    {
      int nPolygon = m_alEars.Count + 1; //ears plus updated polygon
      m_aPolygons = new CPoint2D[nPolygon][];

      for (int i = 0; i < nPolygon - 1; i++) //add ears
      {
        CPoint2D[] points = m_alEars[i];

        m_aPolygons[i] = new CPoint2D[3]; //3 vertices each ear
        m_aPolygons[i][0] = points[0];
        m_aPolygons[i][1] = points[1];
        m_aPolygons[i][2] = points[2];
      }

      //add UpdatedPolygon:
      m_aPolygons[nPolygon - 1] = new CPoint2D[m_aUpdatedPolygonVertices.Length];

      for (int i = 0; i < m_aUpdatedPolygonVertices.Length; i++)
      {
        m_aPolygons[nPolygon - 1][i] = m_aUpdatedPolygonVertices[i];
      }
    }

    /********************************************************
    To update m_aUpdatedPolygonVertices:
    Take out Vertex from m_aUpdatedPolygonVertices array, add 3 points
    to the m_aEars
    **********************************************************/
    private void UpdatePolygonVertices(CPoint2D vertex)
    {
      System.Collections.ArrayList alTempPts = new System.Collections.ArrayList();

      bool found = false;
      for (int i = 0; i < m_aUpdatedPolygonVertices.Length; i++)
      {
        if (!found && vertex.EqualsPoint(m_aUpdatedPolygonVertices[i])) //add 3 pts to FEars
        {
          found = true;
          CPolygon polygon = new CPolygon(m_aUpdatedPolygonVertices);
          CPoint2D pti = vertex;
          CPoint2D ptj = polygon.PreviousPoint(vertex); //previous point
          CPoint2D ptk = polygon.NextPoint(vertex); //next point

          CPoint2D[] aEar = new CPoint2D[3]; //3 vertices of each ear
          aEar[0] = ptj;
          aEar[1] = pti;
          aEar[2] = ptk;

          m_alEars.Add(aEar);
        }
        else
        {
          alTempPts.Add(m_aUpdatedPolygonVertices[i]);
        } //not equal points
      }

      if (m_aUpdatedPolygonVertices.Length - alTempPts.Count == 1)
      {
        int nLength = m_aUpdatedPolygonVertices.Length;
        m_aUpdatedPolygonVertices = new CPoint2D[nLength - 1];

        for (int i = 0; i < alTempPts.Count; i++)
          m_aUpdatedPolygonVertices[i] = (CPoint2D)alTempPts[i];
      }
    }


    /*******************************************************
    To cut an ear from polygon to make ears and an updated polygon:
    *******************************************************/
    public void CutEar()
    {
      CPolygon polygon = new CPolygon(m_aUpdatedPolygonVertices);
      bool bFinish = false;

      //if (polygon.GetPolygonType()==PolygonType.Convex) //don't have to cut ear
      //	bFinish=true;

      if (m_aUpdatedPolygonVertices.Length == 3) //triangle, don't have to cut ear
        bFinish = true;

      CPoint2D pt = new CPoint2D();
      while (bFinish == false) //UpdatedPolygon
      {
        int i = 0;
        bool bNotFound = true;
        while (bNotFound && (i < m_aUpdatedPolygonVertices.Length)) //loop till find an ear
        {
          pt = m_aUpdatedPolygonVertices[i];
          if (IsEarOfUpdatedPolygon(pt))
            bNotFound = false; //got one, pt is an ear
          else
            i++;
        } //bNotFount
        //An ear found:}
        if (pt != null)
        {
          int count = m_aUpdatedPolygonVertices.Length;
          UpdatePolygonVertices(pt);
          if (count == m_aUpdatedPolygonVertices.Length)
          {
            //sanity check
            bFinish = true;
          }
        }
        polygon = new CPolygon(m_aUpdatedPolygonVertices);
        //if ((polygon.GetPolygonType()==PolygonType.Convex)
        //	&& (m_aUpdatedPolygonVertices.Length==3))
        if (m_aUpdatedPolygonVertices.Length == 3)
          bFinish = true;
      } //bFinish=false
      SetPolygons();
    }
  }
}

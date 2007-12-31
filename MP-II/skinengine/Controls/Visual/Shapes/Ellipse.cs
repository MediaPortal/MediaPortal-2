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
  public class Ellipse : Shape
  {

    public Ellipse()
    {
      Init();
    }

    public Ellipse(Ellipse s)
      : base(s)
    {
      Init();
    }

    public override object Clone()
    {
      return new Ellipse(this);
    }

    void Init()
    {
    }


    /// <summary>
    /// Performs the layout.
    /// </summary>
    protected override void PerformLayout()
    {
      Free();
      double w = Width; if (w == 0) w = ActualWidth;
      double h = Height; if (h == 0) h = ActualHeight;
      Vector3 orgPos = new Vector3(ActualPosition.X, ActualPosition.Y, ActualPosition.Z);
      float centerX = (float)(ActualPosition.X + ActualWidth / 2);
      float centerY = (float)(ActualPosition.Y + ActualHeight / 2);
      //Fill brush
      PointF[] vertices;
      PositionColored2Textured[] verts;
      GraphicsPath path;
      if (Fill != null)
      {
        path = GetEllipse(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)ActualWidth, (float)ActualHeight));
        vertices = ConvertPathToTriangleFan(path, (int)+(centerX), (int)(centerY));

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
        path = GetEllipse(new RectangleF(ActualPosition.X, ActualPosition.Y, (float)ActualWidth, (float)ActualHeight));
        vertices = ConvertPathToTriangleStrip(path, (int)(centerX), (int)(centerY), (float)StrokeThickness);

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
        _verticesCountBorder = (verts.Length /3);
      }

      ActualPosition = new Vector3(orgPos.X, orgPos.Y, orgPos.Z);
      ActualWidth = w;
      ActualHeight = h;
    }

    #region Get the desired Rounded Rectangle path.
    private GraphicsPath GetEllipse(RectangleF baseRect)
    {

      GraphicsPath mPath = new GraphicsPath();
      mPath.AddEllipse(baseRect);
      mPath.CloseFigure();
      mPath.Flatten();
      return mPath;
    }
    #endregion


  }
}

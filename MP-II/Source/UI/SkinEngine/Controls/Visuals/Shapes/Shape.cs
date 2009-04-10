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
using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.ContentManagement;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using PointF = System.Drawing.PointF;
using SizeF = System.Drawing.SizeF;
using Matrix = SlimDX.Matrix;
using Brush = MediaPortal.SkinEngine.Controls.Brushes.Brush;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.SkinEngine.Controls.Visuals.Shapes.Triangulate;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.SkinEngine.SkinManagement;

namespace MediaPortal.SkinEngine.Controls.Visuals.Shapes
{
  /// <summary>
  /// Describes to a LineStrip how it should place the line's width relative to its points
  /// </summary>
  /// <remarks>
  /// The behavior of the LeftHanded and RightHanded modes depends on the order the points are
  /// listed in. LeftHanded will draw the line on the outside of a clockwise curve and on the
  /// inside of a counterclockwise curve; RightHanded is the opposite.
  /// </remarks>
  public enum WidthMode
  {
    /// <summary>
    /// Centers the width on the line
    /// </summary>
    Centered,

    /// <summary>
    /// Places the width on the left-hand side of the line
    /// </summary>
    LeftHanded,

    /// <summary>
    /// Places the width on the right-hand side of the line
    /// </summary>
    RightHanded
  }

  public class Shape : FrameworkElement, IUpdateEventHandler
  {
    #region Protected fields

    protected Property _stretchProperty;
    protected Property _fillProperty;
    protected Property _strokeProperty;
    protected Property _strokeThicknessProperty;

    // Albert: We should rework the system how vertex buffers (together with their vertex counts) are stored.
    // We should implement a "typed vertexbuffer" class, holding the vertices, their count and their primitivetype.
    // Currently, only triangle lists are used, which limits all vertex buffers to this type.
    protected VisualAssetContext _fillAsset;
    protected int _verticesCountFill;
    protected VisualAssetContext _borderAsset;
    protected int _verticesCountBorder;
    protected bool _performLayout;
    protected PrimitiveContext _fillContext;
    protected PrimitiveContext _strokeContext;
    protected UIEvent _lastEvent;
    protected bool _hidden;

    #endregion

    #region Ctor

    public Shape()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _fillProperty = new Property(typeof(Brush), null);
      _strokeProperty = new Property(typeof(Brush), null);
      _strokeThicknessProperty = new Property(typeof(double), 1.0);
      _stretchProperty = new Property(typeof(Stretch), Stretch.None);
    }

    void Attach()
    {
      _fillProperty.Attach(OnFillBrushPropertyChanged);
      _strokeProperty.Attach(OnStrokeBrushPropertyChanged);
      _strokeThicknessProperty.Attach(OnStrokeThicknessChanged);
    }

    void Detach()
    {
      _fillProperty.Detach(OnFillBrushPropertyChanged);
      _strokeProperty.Detach(OnStrokeBrushPropertyChanged);
      _strokeThicknessProperty.Detach(OnStrokeThicknessChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Shape s = (Shape) source;
      Fill = copyManager.GetCopy(s.Fill);
      Stroke = copyManager.GetCopy(s.Stroke);
      StrokeThickness = copyManager.GetCopy(s.StrokeThickness);
      Stretch = copyManager.GetCopy(s.Stretch);
      Attach();
    }

    #endregion

    void OnStrokeThicknessChanged(Property property, object oldValue)
    {
      _performLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnFillBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.FillChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnStrokeBrushChanged(IObservable observable)
    {
      _lastEvent |= UIEvent.StrokeChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnFillBrushPropertyChanged(Property property, object oldValue)
    {
      if (oldValue is Brush)
        ((Brush)oldValue).ObjectChanged -= OnFillBrushChanged;
      if (Fill != null)
        Fill.ObjectChanged += OnFillBrushChanged;
      OnFillBrushChanged(null);
    }

    void OnStrokeBrushPropertyChanged(Property property, object oldValue)
    {
      if (oldValue is Brush)
        ((Brush) oldValue).ObjectChanged -= OnStrokeBrushChanged;
      if (Stroke != null)
        Stroke.ObjectChanged += OnStrokeBrushChanged;
      OnStrokeBrushChanged(null);
    }

    public Property StretchProperty
    {
      get { return _stretchProperty; }
    }

    public Stretch Stretch
    {
      get { return (Stretch)_stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public Property FillProperty
    {
      get { return _fillProperty; }
    }

    public Brush Fill
    {
      get { return (Brush) _fillProperty.GetValue(); }
      set { _fillProperty.SetValue(value); }
    }

    public Property StrokeProperty
    {
      get { return _strokeProperty; }
    }

    public Brush Stroke
    {
      get { return (Brush) _strokeProperty.GetValue(); }
      set { _strokeProperty.SetValue(value); }
    }

    public Property StrokeThicknessProperty
    {
      get { return _strokeThicknessProperty; }
    }

    public double StrokeThickness
    {
      get { return (double)_strokeThicknessProperty.GetValue(); }
      set { _strokeThicknessProperty.SetValue(value); }
    }

    protected virtual void PerformLayout()
    {
      _performLayout = false;
    }

    public override void DoBuildRenderTree()
    {
      if (!IsVisible) 
        return;
      PerformLayout();
      _lastEvent = UIEvent.None;
    }

    public override void DestroyRenderTree()
    {
      if (_fillContext != null)
      {
        RenderPipeline.Instance.Remove(_fillContext);
        _fillContext = null;
      }
      if (_strokeContext != null)
      {
        RenderPipeline.Instance.Remove(_strokeContext);
        _strokeContext = null;
      }
    }

    void SetupBrush(UIEvent uiEvent)
    {
      if ((uiEvent & UIEvent.FillChange) != 0 || (uiEvent & UIEvent.OpacityChange) != 0)
      {
        if (Fill != null && _fillContext != null)
        {
          RenderPipeline.Instance.Remove(_fillContext);
          Fill.SetupPrimitive(_fillContext);
          RenderPipeline.Instance.Add(_fillContext);
        }
      }
      if ((uiEvent & UIEvent.StrokeChange) != 0 || (uiEvent & UIEvent.OpacityChange) != 0)
      {
        if (Stroke != null && _strokeContext != null)
        {
          RenderPipeline.Instance.Remove(_strokeContext);
          Stroke.SetupPrimitive(_strokeContext);
          RenderPipeline.Instance.Add(_strokeContext);
        }
      }
    }

    public void Update()
    {
      if ((_lastEvent & UIEvent.Visible) != 0)
        _hidden = false;
      if (_hidden)
      {
        _lastEvent = UIEvent.None;
        return;
      }
      UpdateLayout();
      PerformLayout();
      if ((_lastEvent & UIEvent.Hidden) != 0)
      {
        if (_fillContext != null)
          RenderPipeline.Instance.Remove(_fillContext);
        if (_strokeContext != null)
          RenderPipeline.Instance.Remove(_strokeContext);
        _fillContext = null;
        _strokeContext = null;
        _performLayout = true;
        _hidden = true;
      }
      else if (_lastEvent != UIEvent.None)
        SetupBrush(_lastEvent);
      _lastEvent = UIEvent.None;
    }

    public override void DoRender()
    {
      if (!IsVisible) return;
      if (Fill == null && Stroke == null) return;
      if (Fill != null)
      {
        if ((_fillAsset != null && !_fillAsset.IsAllocated) || _fillAsset == null)
          _performLayout = true;
      }
      if (Stroke != null)
      {
        if ((_borderAsset != null && !_borderAsset.IsAllocated) || _borderAsset == null)
          _performLayout = true;
      }
      PerformLayout();

      SkinContext.AddOpacity(Opacity);
      if (_fillAsset != null && _fillAsset.VertexBuffer != null)
      {
        //GraphicsDevice.TransformWorld = SkinContext.FinalMatrix.Matrix;
        //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Fill.BeginRender(_fillAsset.VertexBuffer, _verticesCountFill, PrimitiveType.TriangleList))
        {
          GraphicsDevice.Device.SetStreamSource(0, _fillAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountFill);
          Fill.EndRender();
        }
        _fillAsset.LastTimeUsed = SkinContext.Now;
      }
      if (_borderAsset != null && _borderAsset.VertexBuffer != null)
      {
        //GraphicsDevice.Device.VertexFormat = PositionColored2Textured.Format;
        if (Stroke.BeginRender(_borderAsset.VertexBuffer, _verticesCountBorder, PrimitiveType.TriangleList))
        {
          GraphicsDevice.Device.SetStreamSource(0, _borderAsset.VertexBuffer, 0, PositionColored2Textured.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(PrimitiveType.TriangleList, 0, _verticesCountBorder);
          Stroke.EndRender();
        }
        _borderAsset.LastTimeUsed = SkinContext.Now;
      }
      SkinContext.RemoveOpacity();
    }

    public override void FireUIEvent(UIEvent eventType, UIElement source)
    {
      base.FireUIEvent(eventType, source);
      if (SkinContext.UseBatching)
      {
        if ((_lastEvent & UIEvent.Hidden) != 0 && eventType == UIEvent.Visible)
          _lastEvent = UIEvent.None;
        if ((_lastEvent & UIEvent.Visible) != 0 && eventType == UIEvent.Hidden)
          _lastEvent = UIEvent.None;
        if (_hidden && eventType != UIEvent.Visible) return;
        _lastEvent |= eventType;
        if (Screen != null) Screen.Invalidate(this);
      }
    }

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
      x = (float) (Math.Cos(ang) * thicknessW); //radians
      y = (float) (Math.Sin(ang) * thicknessH);
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

    public static void TriangulateStroke_TriangleList(GraphicsPath path, float thickness, bool isClosed,
        out PositionColored2Textured[] verts, ExtendedMatrix finalTransLayoutform)
    {
      PolygonDirection direction = PointsDirection(path);
      TriangulateStroke_TriangleList(path, thickness, isClosed, direction, out verts, finalTransLayoutform);
    }

    public static void TriangulateStroke_TriangleList(GraphicsPath path, float thickness, bool isClosed,
        PolygonDirection direction, out PositionColored2Textured[] verts, ExtendedMatrix finalLayoutTransform)
    {
      verts = null;
      if (path.PointCount <= 0) return;
      thickness /= 2.0f;
      float thicknessW = thickness * SkinContext.Zoom.Width;
      float thicknessH = thickness * SkinContext.Zoom.Height;
      PointF[] points = path.PathPoints;
      int pointCount = points.Length;
      int verticeCount = pointCount * 2 * 3;

      verts = new PositionColored2Textured[verticeCount];

      for (int i = 0; i < pointCount; ++i)
      {
        int offset = i * 6;
        PointF nextpoint = GetNextPoint(points, i, pointCount);
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
    /// Converts the graphics path to an array of vertices using trianglestrip.
    /// </summary>
    public static void TriangulateStroke_TriangleList(GraphicsPath path, float thickness, bool isClosed,
        out PositionColored2Textured[] verts, ExtendedMatrix finalTransLayoutform, bool isCenterFill)
    {
      PolygonDirection direction = PointsDirection(path);
      TriangulateStroke_TriangleList(path, thickness, isClosed, direction, out verts, finalTransLayoutform, isCenterFill);
    }

    /// <summary>
    /// Converts the graphics path to an array of vertices using trianglestrip.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="thickness">The thickness of the line.</param>
    /// <param name="isClosed">True if we should connect the first and last point.</param>
    /// <param name="direction">The polygon direction.</param>
    /// <param name="verts">The generated verts.</param>
    /// <param name="finalLayoutTransform">Final layout transform.</param>
    /// <param name="isCenterFill">True if center fill otherwise left hand fill.</param>
    /// <returns>vertex buffer</returns>
    public static void TriangulateStroke_TriangleList(GraphicsPath path, float thickness, bool isClosed,
        PolygonDirection direction, out PositionColored2Textured[] verts, ExtendedMatrix finalLayoutTransform,
        bool isCenterFill)
    {
      verts = null;
      if (path.PointCount <= 0) 
        return;

      float thicknessW = thickness * SkinContext.Zoom.Width;
      float thicknessH = thickness * SkinContext.Zoom.Height;
      PointF[] points = path.PathPoints;
      PointF[] newPoints = new PointF[points.Length];
      int pointCount;
      float x = 0f;
      float y = 0f;

      if (isClosed)
        pointCount = points.Length;
      else
        pointCount = points.Length - 1;

      int verticeCount = pointCount * 2 * 3;
      verts = new PositionColored2Textured[verticeCount];

      // If center fill then we must move the points half the inset
      if (isCenterFill)
      {
        int lastPoint = points.Length - 1;

        for (int i = 0; i < points.Length - 1; i++)
        {
          PointF nextpoint = GetNextPoint(points, i, points.Length);
          GetInset(nextpoint, points[i], out x, out y, -thicknessW / 2.0, -thicknessH / 2.0, direction, finalLayoutTransform);
          newPoints[i].X = x;
          newPoints[i].Y = y;
        }
        newPoints[lastPoint].X = points[lastPoint].X + (x - points[lastPoint - 1].X);
        newPoints[lastPoint].Y = points[lastPoint].Y + (y - points[lastPoint - 1].Y);
        points = newPoints;
      }

      for (int i = 0; i < pointCount; i++)
      {
        int offset = i * 6;

        PointF nextpoint = GetNextPoint(points, i, points.Length);
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
    protected static void Triangulate(GraphicsPath path, float cx, float cy, out PositionColored2Textured[] verts)
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

    /// <summary>
    /// Generates the vertices of a thickened line strip
    /// </summary>
    /// <param name="path">Graphics path on the line strip</param>
    /// <param name="thickness">Thickness of the line</param>
    /// <param name="close">Whether to connect the last point back to the first</param>
    /// <param name="widthMode">How to place the weight of the line relative to it</param>
    /// <returns>Points ready to pass to the Transform constructor</returns>
    protected VertexBuffer CalculateLinePoints(GraphicsPath path, float thickness, bool close, WidthMode widthMode,
        out PositionColored2Textured[] verts)
    {
      verts = null;
      if (path.PointCount < 3)
      {
        if (close) return null;
        else if (path.PointCount < 2)
          return null;
      }

      Matrix matrix = new Matrix();
      if (_finalLayoutTransform != null)
      {
        matrix = _finalLayoutTransform.Matrix;
      }
      if (LayoutTransform != null)
      {
        ExtendedMatrix em;
        LayoutTransform.GetTransform(out em);
        matrix *= em.Matrix;
      }
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
        outPoints[0] = Vector2.Modulate(innerDistance, normal(points[1] - points[0]));
        TransformXY(ref outPoints[0], matrix);
        outPoints[0] = points[0] + outPoints[0];

        //outPoints[0] = points[0] + innerDistance * normal(points[1] - points[0]);
        Vector2 norm = Vector2.Modulate(innerDistance, normal(points[points.Length - 1] - points[points.Length - 2])); //DEBUG

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
      VertexBuffer vertexBuffer = PositionColored2Textured.Create(verticeCount);
      verts = new PositionColored2Textured[verticeCount];

      for (int i = 0; i < verticeCount; ++i)
        verts[i].Position = new Vector3(outPoints[i].X, outPoints[i].Y, 1);
      return vertexBuffer;
    }

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("Shape.Arrange :{0} X {1},Y {2} W {3}xH {4}", Name, (int)finalRect.X, (int)finalRect.Y, (int)finalRect.Width, (int)finalRect.Height));

      RemoveMargin(ref finalRect);

      _finalRect = new RectangleF(finalRect.Location, finalRect.Size);

      ActualPosition = new Vector3(finalRect.Location.X, finalRect.Location.Y, SkinContext.GetZorder());
      ActualWidth = finalRect.Width;
      ActualHeight = finalRect.Height;

      //Trace.WriteLine(String.Format("Label.Arrange Zorder {0}", ActualPosition.Z));
      _performLayout = true;
      _finalLayoutTransform = SkinContext.FinalLayoutTransform;
      base.Arrange(finalRect);
      if (Screen != null) Screen.Invalidate(this);
    }

    public override void Measure(ref SizeF totalSize)
    {
      _desiredSize = new SizeF((float)Width * SkinContext.Zoom.Width, (float)Height * SkinContext.Zoom.Height);

      if (LayoutTransform != null)
      {
        ExtendedMatrix m;
        LayoutTransform.GetTransform(out m);
        SkinContext.AddLayoutTransform(m);
      }
      SkinContext.FinalLayoutTransform.TransformSize(ref _desiredSize);

      if (LayoutTransform != null)
        SkinContext.RemoveLayoutTransform();

      totalSize = _desiredSize;
      AddMargin(ref totalSize);

      //Trace.WriteLine(String.Format("shape.measure :{0} returns {1}x{2}", this.Name, (int)totalSize.Width, (int)totalSize.Height));
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
      else if (nCount > 0)
        return PolygonDirection.Clockwise;
      else
        return PolygonDirection.Unknown;
    }

    #region Math helpers

    /// <summary>the slope of v, or NaN if it is nearly vertical</summary>
    /// <param name="v">Vector to take slope from</param>
    protected static float vectorSlope(Vector2 v)
    {
      return Math.Abs(v.X) < 0.001f ? float.NaN : (v.Y / v.X);
    }

    /// <summary>Finds the intercept of a line</summary>
    /// <param name="point">A point on the line</param>
    /// <param name="slope">The slope of the line</param>
    protected static float lineIntercept(Vector2 point, float slope)
    {
      return point.Y - slope * point.X;
    }

    /// <summary>The unit length right-hand normal of v</summary>
    /// <param name="v">Vector to find the normal of</param>
    protected static Vector2 normal(Vector2 v)
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
    protected Vector2 InnerPoint(Matrix matrix, Vector2 distance, Vector2 lastPoint, Vector2 point, Vector2 nextPoint, out float slope, out float intercept)
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
    protected Vector2 InnerPoint(Matrix matrix, Vector2 distance, ref float lastSlope, ref float lastIntercept, Vector2 lastInnerPoint, Vector2 point, Vector2 nextPoint)
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

    public void TransformXY(ref Vector2 vector, Matrix m)
    {
      float w1 = vector.X * m.M11 + vector.Y * m.M21;
      float h1 = vector.X * m.M12 + vector.Y * m.M22;
      vector.X = w1;
      vector.Y = h1;
    }

    #endregion

    public override void Deallocate()
    {
      base.Deallocate();
      if (Fill != null)
        Fill.Deallocate();
      if (Stroke != null)
        Stroke.Deallocate();
      if (_fillAsset != null)
      {
        _fillAsset.Free(true);
        ContentManager.Remove(_fillAsset);
        _fillAsset = null;
      }
      if (_borderAsset != null)
      {
        _borderAsset.Free(true);
        ContentManager.Remove(_borderAsset);
        _borderAsset = null;
      }
      if (_fillContext != null)
      {
        RenderPipeline.Instance.Remove(_fillContext);
        _fillContext = null;
      }
      if (_strokeContext != null)
      {
        RenderPipeline.Instance.Remove(_strokeContext);
        _strokeContext = null;
      }
    }

    public override void Allocate()
    {
      base.Allocate();
      if (Fill != null)
        Fill.Allocate();
      if (Stroke != null)
        Stroke.Allocate();
      _performLayout = true;
    }
  }
}

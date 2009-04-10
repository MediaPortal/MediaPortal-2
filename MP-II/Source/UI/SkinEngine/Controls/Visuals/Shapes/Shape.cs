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
using MediaPortal.SkinEngine.DirectX.Triangulate;
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
        outPoints[lastIndex] = TriangulateHelper.InnerPoint(matrix, innerDistance, points[points.Length - 2], points[points.Length - 1], points[0], out slope, out intercept);
        outPoints[0] = TriangulateHelper.InnerPoint(matrix, innerDistance, ref slope, ref intercept, outPoints[lastIndex], points[0], points[1]);
      }
      else
      {
        //Take endpoints based on the end segments' normals alone
        outPoints[0] = Vector2.Modulate(innerDistance, TriangulateHelper.normal(points[1] - points[0]));
        TriangulateHelper.TransformXY(ref outPoints[0], matrix);
        outPoints[0] = points[0] + outPoints[0];

        //outPoints[0] = points[0] + innerDistance * normal(points[1] - points[0]);
        Vector2 norm = Vector2.Modulate(innerDistance, TriangulateHelper.normal(points[points.Length - 1] - points[points.Length - 2])); //DEBUG

        TriangulateHelper.TransformXY(ref norm, matrix);
        outPoints[outPoints.Length - 2] = points[points.Length - 1] + norm;

        //Get the slope and intercept of the first segment to feed into the middle loop
        slope = TriangulateHelper.vectorSlope(points[1] - points[0]);
        intercept = TriangulateHelper.lineIntercept(outPoints[0], slope);
      }

      //Get the middle points
      for (int i = 1; i < points.Length - 1; i++)
        outPoints[2 * i] = TriangulateHelper.InnerPoint(matrix, innerDistance, ref slope, ref intercept, outPoints[2 * (i - 1)], points[i], points[i + 1]);

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

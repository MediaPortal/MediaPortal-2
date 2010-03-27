#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Drawing;
using MediaPortal.Core.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using SizeF = System.Drawing.SizeF;
using Brush = MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
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

    protected AbstractProperty _stretchProperty;
    protected AbstractProperty _fillProperty;
    protected AbstractProperty _strokeProperty;
    protected AbstractProperty _strokeThicknessProperty;

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

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      // Fill and Stroke are disposed by the BrushCache
    }

    void Init()
    {
      _fillProperty = new SProperty(typeof(Brush), null);
      _strokeProperty = new SProperty(typeof(Brush), null);
      _strokeThicknessProperty = new SProperty(typeof(double), 1.0);
      _stretchProperty = new SProperty(typeof(Stretch), Stretch.None);
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

    void OnStrokeThicknessChanged(AbstractProperty property, object oldValue)
    {
      _performLayout = true;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnFillBrushChanged(IObservable observable)
    {
      _performLayout = true;
      _lastEvent |= UIEvent.FillChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnStrokeBrushChanged(IObservable observable)
    {
      _performLayout = true;
      _lastEvent |= UIEvent.StrokeChange;
      if (Screen != null) Screen.Invalidate(this);
    }

    void OnFillBrushPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (oldValue is Brush)
        ((Brush) oldValue).ObjectChanged -= OnFillBrushChanged;
      if (Fill != null)
        Fill.ObjectChanged += OnFillBrushChanged;
      OnFillBrushChanged(null);
    }

    void OnStrokeBrushPropertyChanged(AbstractProperty property, object oldValue)
    {
      if (oldValue is Brush)
        ((Brush) oldValue).ObjectChanged -= OnStrokeBrushChanged;
      if (Stroke != null)
        Stroke.ObjectChanged += OnStrokeBrushChanged;
      OnStrokeBrushChanged(null);
    }

    public AbstractProperty StretchProperty
    {
      get { return _stretchProperty; }
    }

    public Stretch Stretch
    {
      get { return (Stretch) _stretchProperty.GetValue(); }
      set { _stretchProperty.SetValue(value); }
    }

    public AbstractProperty FillProperty
    {
      get { return _fillProperty; }
    }

    public Brush Fill
    {
      get { return (Brush) _fillProperty.GetValue(); }
      set { _fillProperty.SetValue(value); }
    }

    public AbstractProperty StrokeProperty
    {
      get { return _strokeProperty; }
    }

    public Brush Stroke
    {
      get { return (Brush) _strokeProperty.GetValue(); }
      set { _strokeProperty.SetValue(value); }
    }

    public AbstractProperty StrokeThicknessProperty
    {
      get { return _strokeThicknessProperty; }
    }

    public double StrokeThickness
    {
      get { return (double) _strokeThicknessProperty.GetValue(); }
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
        if (_fillAsset == null || !_fillAsset.IsAllocated)
          _performLayout = true;
      }
      if (Stroke != null)
      {
        if (_borderAsset == null || !_borderAsset.IsAllocated)
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

    public override void Arrange(RectangleF finalRect)
    {
      //Trace.WriteLine(String.Format("Shape.Arrange: {0} X {1},Y {2} W {3}xH {4}", Name, (int) finalRect.X, (int) finalRect.Y, (int) finalRect.Width, (int) finalRect.Height));

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

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
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.Rendering;
using Brush = MediaPortal.UI.SkinEngine.Controls.Brushes.Brush;
using MediaPortal.Utilities.DeepCopy;
using MediaPortal.UI.SkinEngine.SkinManagement;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Shapes
{
  /// <summary>
  /// Describes to a LineStrip how it should paint its points relative to its center.
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

  public class Shape : FrameworkElement, IUpdateEventHandler
  {
    #region Protected fields

    protected AbstractProperty _stretchProperty;
    protected AbstractProperty _fillProperty;
    protected AbstractProperty _strokeProperty;
    protected AbstractProperty _strokeThicknessProperty;

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
      StrokeThickness = s.StrokeThickness;
      Stretch = s.Stretch;
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

    protected void PerformLayout()
    {
      if (!_performLayout)
        return;
      _performLayout = false;
      DoPerformLayout();
    }

    /// <summary>
    /// Allocates the <see cref="_fillContext"/> and <see cref="_strokeContext"/> variables.
    /// This method will be overridden in sub classes.
    /// </summary>
    protected virtual void DoPerformLayout()
    {
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
      base.DestroyRenderTree();
      RemovePrimitiveContext(ref _fillContext);
      RemovePrimitiveContext(ref _strokeContext);
    }

    void SetupBrush(UIEvent uiEvent)
    {
      if ((uiEvent & UIEvent.FillChange) != 0 || (uiEvent & UIEvent.OpacityChange) != 0)
      {
        if (Fill != null && _fillContext != null)
          Fill.SetupPrimitive(_fillContext);
      }
      if ((uiEvent & UIEvent.StrokeChange) != 0 || (uiEvent & UIEvent.OpacityChange) != 0)
      {
        if (Stroke != null && _strokeContext != null)
          Stroke.SetupPrimitive(_strokeContext);
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
        RemovePrimitiveContext(ref _fillContext);
        RemovePrimitiveContext(ref _strokeContext);
        _performLayout = true;
        _hidden = true;
      }
      else if (_lastEvent != UIEvent.None)
        SetupBrush(_lastEvent);
      _lastEvent = UIEvent.None;
    }

    public override void DoRender()
    {
      PerformLayout();

      SkinContext.AddOpacity(Opacity);
      if (_fillContext != null)
      {
        GraphicsDevice.Device.VertexFormat = _fillContext.VertexFormat;
        if (Fill.BeginRender(_fillContext))
        {
          GraphicsDevice.Device.VertexFormat = _fillContext.VertexFormat;
          GraphicsDevice.Device.SetStreamSource(0, _fillContext.VertexBuffer, 0, _fillContext.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(_fillContext.PrimitiveType, 0, _fillContext.NumVertices);
          Fill.EndRender();
        }
      }
      if (_strokeContext != null)
      {
        if (Stroke.BeginRender(_strokeContext))
        {
          GraphicsDevice.Device.VertexFormat = _strokeContext.VertexFormat;
          GraphicsDevice.Device.SetStreamSource(0, _strokeContext.VertexBuffer, 0, _strokeContext.StrideSize);
          GraphicsDevice.Device.DrawPrimitives(_strokeContext.PrimitiveType, 0, _strokeContext.NumVertices);
          Stroke.EndRender();
        }
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

    protected override void ArrangeOverride(RectangleF finalRect)
    {
      base.ArrangeOverride(finalRect);
      _performLayout = true;
      if (Screen != null)
        Screen.Invalidate(this);
    }

    public override void Deallocate()
    {
      base.Deallocate();
      if (Fill != null)
        Fill.Deallocate();
      if (Stroke != null)
        Stroke.Deallocate();
      RemovePrimitiveContext(ref _fillContext);
      RemovePrimitiveContext(ref _strokeContext);
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

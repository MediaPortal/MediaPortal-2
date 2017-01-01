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

using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.Controls.Transforms;
using MediaPortal.UI.SkinEngine.Controls.Visuals;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Brushes
{
  public abstract class Brush : DependencyObject, IObservable
  {
    #region Protected fields

    protected AbstractProperty _opacityProperty;
    protected AbstractProperty _relativeTransformProperty;
    protected AbstractProperty _transformProperty;
    protected AbstractProperty _freezableProperty;
    protected RectangleF _vertsBounds;
    protected Matrix? _finalBrushTransform = null;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();

    #endregion

    #region Ctor

    protected Brush()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      MPF.TryCleanupAndDispose(RelativeTransform);
      MPF.TryCleanupAndDispose(Transform);
    }

    void Init()
    {
      _opacityProperty = new SProperty(typeof(double), 1.0);
      _relativeTransformProperty = new SProperty(typeof(Transform), null);
      _transformProperty = new SProperty(typeof(Transform), null);
      _freezableProperty = new SProperty(typeof(bool), false);
      _vertsBounds = new RectangleF(0, 0, 0, 0);
    }

    void Attach()
    {
      _opacityProperty.Attach(OnPropertyChanged);
      _relativeTransformProperty.Attach(OnRelativeTransformChanged);
      _transformProperty.Attach(OnTransformChanged);
    }

    void Detach()
    {
      _opacityProperty.Detach(OnPropertyChanged);
      _relativeTransformProperty.Detach(OnRelativeTransformChanged);
      _transformProperty.Detach(OnTransformChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      Brush b = (Brush) source;
      Opacity = b.Opacity;
      RelativeTransform = copyManager.GetCopy(b.RelativeTransform);
      Transform = copyManager.GetCopy(b.Transform);
      Freezable = b.Freezable;
      _finalBrushTransform = null;
      Attach();
    }

    #endregion

    void OnRelativeTransformChanged(AbstractProperty prop, object oldVal)
    {
      Transform oldTransform = (Transform) oldVal;
      if (oldTransform != null)
        oldTransform.ObjectChanged -= OnRelativeTransformChanged;
      Transform transform = (Transform) prop.GetValue();
      if (transform != null)
        transform.ObjectChanged += OnRelativeTransformChanged;
    }

    void OnTransformChanged(AbstractProperty prop, object oldVal)
    {
      Transform oldTransform = (Transform) oldVal;
      if (oldTransform != null)
        oldTransform.ObjectChanged -= OnTransformChanged;
      Transform transform = (Transform) prop.GetValue();
      if (transform != null)
        transform.ObjectChanged += OnTransformChanged;
    }

    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    #region Protected methods

    protected void FireChanged()
    {
      _objectChanged.Fire(new object[] {this});
    }

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected virtual void OnPropertyChanged(AbstractProperty prop, object oldValue)
    {
      FireChanged();
    }

    protected virtual void OnRelativeTransformChanged(IObservable trans)
    {
      _finalBrushTransform = null;
      FireChanged();
    }

    protected virtual void OnTransformChanged(IObservable trans)
    {
      _finalBrushTransform = null;
      FireChanged();
    }

    #endregion

    #region Public properties

    public AbstractProperty FreezableProperty
    {
      get { return _freezableProperty; }
    }

    public bool Freezable
    {
      get { return (bool) _freezableProperty.GetValue(); }
      set { _freezableProperty.SetValue(value); }
    }

    public AbstractProperty OpacityProperty
    {
      get { return _opacityProperty; }
    }

    public double Opacity
    {
      get { return (double) _opacityProperty.GetValue(); }
      set { _opacityProperty.SetValue(value); }
    }

    public AbstractProperty RelativeTransformProperty
    {
      get { return _relativeTransformProperty; }
    }

    public Transform RelativeTransform
    {
      get { return (Transform) _relativeTransformProperty.GetValue(); }
      set { _relativeTransformProperty.SetValue(value); }
    }

    public AbstractProperty TransformProperty
    {
      get { return _transformProperty; }
    }

    public Transform Transform
    {
      get { return (Transform) _transformProperty.GetValue(); }
      set { _transformProperty.SetValue(value); }
    }

    public virtual Texture Texture
    {
      get { return null; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Scales the specified u/v coordinates.
    /// </summary>
    public virtual void Scale(ref float u, ref float v, ref Color4 color)
    { }

    public virtual void SetupBrush(FrameworkElement parent, ref PositionColoredTextured[] verts, float zOrder, bool adaptVertsToBrushTexture)
    {
      if (!UpdateBounds(ref verts))
        return;
      float w = _vertsBounds.Width;
      float h = _vertsBounds.Height;
      float xoff = _vertsBounds.X;
      float yoff = _vertsBounds.Y;
      if (adaptVertsToBrushTexture)
        for (int i = 0; i < verts.Length; i++)
        {
          PositionColoredTextured vert = verts[i];
          float x = vert.X;
          float u = x - xoff;
          u /= w;

          float y = vert.Y;
          float v = y - yoff;
          v /= h;

          if (u < 0) u = 0;
          if (u > 1) u = 1;
          if (v < 0) v = 0;
          if (v > 1) v = 1;
          unchecked
          {
            Color4 color = Color.White;
            color.Alpha *= (float) Opacity;
            vert.Color = color.ToBgra();
          }
          vert.Tu1 = u;
          vert.Tv1 = v;
          vert.Z = zOrder;
          verts[i] = vert;
        }
    }

    protected bool UpdateBounds(ref PositionColoredTextured[] verts)
    {
      if (verts == null)
      {
        _vertsBounds = RectangleF.Empty;
        return false;
      }
      float minx = float.MaxValue;
      float miny = float.MaxValue;
      float maxx = 0;
      float maxy = 0;
      foreach (PositionColoredTextured vert in verts)
      {
        if (vert.X < minx) minx = vert.X;
        if (vert.Y < miny) miny = vert.Y;

        if (vert.X > maxx) maxx = vert.X;
        if (vert.Y > maxy) maxy = vert.Y;
      }
      _vertsBounds = new RectangleF(minx, miny, maxx - minx, maxy - miny);
      return true;
    }

    public Matrix GetCachedFinalBrushTransform()
    {
      Matrix? transform = _finalBrushTransform;
      if (transform.HasValue)
        return transform.Value;
      if (Transform != null)
      {
        transform = Matrix.Scaling(new Vector3(_vertsBounds.Width, _vertsBounds.Height, 1));
        transform *= Matrix.Invert(Transform.GetTransform());
        transform *= Matrix.Scaling(new Vector3(1/_vertsBounds.Width, 1/_vertsBounds.Height, 1));
      }
      else
        transform = Matrix.Identity;
      _finalBrushTransform = transform;
      return transform.Value;
    }

    public bool BeginRenderBrush(PrimitiveBuffer primitiveContext, RenderContext renderContext)
    {
      if (_vertsBounds.IsEmpty)
        return false;
      return BeginRenderBrushOverride(primitiveContext, renderContext);
    }

    protected abstract bool BeginRenderBrushOverride(PrimitiveBuffer primitiveContext, RenderContext renderContext);

    public abstract void EndRender();

    public virtual void Allocate()
    { }

    public virtual void Deallocate()
    { }

    #endregion
  }
}

#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.MpfElements;
using SharpDX;
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
    protected SharpDX.Direct2D1.Brush _brush2D = null;
    protected volatile bool _refresh = false;

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
      Brush b = (Brush)source;
      Opacity = b.Opacity;
      RelativeTransform = copyManager.GetCopy(b.RelativeTransform);
      Transform = copyManager.GetCopy(b.Transform);
      Freezable = b.Freezable;
      // TODO: copy?
      _brush2D = copyManager.GetCopy(b._brush2D);
      _finalBrushTransform = null;
      Attach();
    }

    #endregion

    void OnRelativeTransformChanged(AbstractProperty prop, object oldVal)
    {
      Transform oldTransform = (Transform)oldVal;
      if (oldTransform != null)
        oldTransform.ObjectChanged -= OnRelativeTransformChanged;
      Transform transform = (Transform)prop.GetValue();
      if (transform != null)
        transform.ObjectChanged += OnRelativeTransformChanged;
    }

    void OnTransformChanged(AbstractProperty prop, object oldVal)
    {
      Transform oldTransform = (Transform)oldVal;
      if (oldTransform != null)
        oldTransform.ObjectChanged -= OnTransformChanged;
      Transform transform = (Transform)prop.GetValue();
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
      _objectChanged.Fire(new object[] { this });
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

    public virtual SharpDX.Direct2D1.Brush Brush2D
    {
      get
      {
        // TODO: should not be the case. Allocate happens before, but is missing in rendering. DeepCopy?!
        if (_refresh || _brush2D == null)
          Allocate();
        return _brush2D;
      }
    }

    public AbstractProperty FreezableProperty
    {
      get { return _freezableProperty; }
    }

    public bool Freezable
    {
      get { return (bool)_freezableProperty.GetValue(); }
      set { _freezableProperty.SetValue(value); }
    }

    public AbstractProperty OpacityProperty
    {
      get { return _opacityProperty; }
    }

    public double Opacity
    {
      get { return (double)_opacityProperty.GetValue(); }
      set { _opacityProperty.SetValue(value); }
    }

    public AbstractProperty RelativeTransformProperty
    {
      get { return _relativeTransformProperty; }
    }

    public Transform RelativeTransform
    {
      get { return (Transform)_relativeTransformProperty.GetValue(); }
      set { _relativeTransformProperty.SetValue(value); }
    }

    public AbstractProperty TransformProperty
    {
      get { return _transformProperty; }
    }

    public Transform Transform
    {
      get { return (Transform)_transformProperty.GetValue(); }
      set { _transformProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Scales the specified u/v coordinates.
    /// </summary>
    public virtual void Scale(ref float u, ref float v, ref Color4 color)
    { }

    public virtual void SetupBrush(FrameworkElement parent, ref RectangleF boundary, float zOrder, bool adaptVertsToBrushTexture)
    {
      if (!UpdateBounds(ref boundary))
        return;
    }

    protected bool UpdateBounds(ref RectangleF verts)
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
      if (verts.Left < minx) minx = verts.Left;
      if (verts.Top < miny) miny = verts.Top;

      if (verts.Right > maxx) maxx = verts.Right;
      if (verts.Bottom > maxy) maxy = verts.Bottom;

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
        transform *= Matrix.Scaling(new Vector3(1 / _vertsBounds.Width, 1 / _vertsBounds.Height, 1));
      }
      else
        transform = Matrix.Identity;
      _finalBrushTransform = transform;
      return transform.Value;
    }

    public virtual void Allocate()
    {
      TryDispose(ref _brush2D);
    }

    public virtual void Deallocate()
    {
      TryDispose(ref _brush2D);
    }


    protected Vector2 TransformToBoundary(Vector2 relativeCoord)
    {
      var x = _vertsBounds.Left + _vertsBounds.Width * relativeCoord.X;
      var y = _vertsBounds.Top + _vertsBounds.Height * relativeCoord.Y;
      return new Vector2(x, y);
    }

    #endregion
  }
}

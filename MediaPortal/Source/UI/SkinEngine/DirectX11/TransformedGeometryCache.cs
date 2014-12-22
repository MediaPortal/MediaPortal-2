using System;
using SharpDX;

namespace MediaPortal.UI.SkinEngine.DirectX11
{

  /// <summary>
  /// Provides a cache for transformed geometries. If the transform is not changed, the existing geometry will be reused.
  /// Changes to transform requires to recreate a <see cref=" SharpDX.Direct2D1.TransformedGeometry"/>.
  /// Note:
  /// This class takes the ownership of the original geometry, this means the caller must not dispose it. When the instance of <see cref="TransformedGeometryCache"/> is
  /// disposed, or the geometry is exchanged by <see cref="UpdateGeometry"/>, the given geometry will be disposed.
  /// </summary>
  public class TransformedGeometryCache : IDisposable
  {
    protected SharpDX.Direct2D1.Geometry _originalGeom;
    protected SharpDX.Direct2D1.TransformedGeometry _transformedGeom;
    protected Matrix _transform = Matrix.Identity;
    protected object _syncObj = new object();

    public object SyncObj { get { return _syncObj; } }

    /// <summary>
    /// Returns the original geoemtry, set by <see cref="UpdateGeometry"/>.
    /// </summary>
    public SharpDX.Direct2D1.Geometry OriginalGeom
    {
      get
      {
        lock (_syncObj)
          return _originalGeom;
      }
    }

    /// <summary>
    /// Returns the transformed geometry. The value might be null, if no geom was set by <see cref="UpdateGeometry"/> before.
    /// </summary>
    public SharpDX.Direct2D1.TransformedGeometry TransformedGeom
    {
      get
      {
        lock (_syncObj)
        {
          if (_transformedGeom == null && _originalGeom != null)
          {
            var originalGeom = _originalGeom;
            if (originalGeom != null && !originalGeom.IsDisposed)
              _transformedGeom = new SharpDX.Direct2D1.TransformedGeometry(GraphicsDevice11.Instance.Context2D1.Factory, originalGeom, _transform);
          }
          return _transformedGeom;
        }
      }
    }

    /// <summary>
    /// Indicates if there is a valid <see cref="TransformedGeom"/>.
    /// </summary>
    public bool HasGeom
    {
      get
      {
        lock (_syncObj)
          return TransformedGeom != null;
      }
    }

    public void UpdateGeometry(SharpDX.Direct2D1.Geometry newGeom)
    {
      lock (_syncObj)
      {
        if (_originalGeom != null)
          _originalGeom.Dispose();
        _originalGeom = newGeom;
        UpdateTransform(Matrix.Identity, true);
      }
    }

    public void UpdateTransform(Matrix newTransform, bool forceRefresh = false)
    {
      lock (_syncObj)
      {
        if (/*newTransform != _transform ||*/ forceRefresh)
        {
          _transform = newTransform;
          if (_transformedGeom != null)
          {
            _transformedGeom.Dispose();
            _transformedGeom = null;
          }
        }
      }
    }

    public void Dispose()
    {
      lock (_syncObj)
      {
        if (_transformedGeom != null)
        {
          _transformedGeom.Dispose();
          _transformedGeom = null;
        }
      }
      if (_originalGeom != null)
      {
        _originalGeom.Dispose();
        _originalGeom = null;
      }
    }
  }
}

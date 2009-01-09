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

using MediaPortal.Core.General;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.Controls.Transforms;
using MediaPortal.SkinEngine.Controls.Visuals;
using MediaPortal.SkinEngine;
using MediaPortal.SkinEngine.DirectX;
using MediaPortal.SkinEngine.Rendering;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Brushes
{
  /// <summary>
  /// todo:
  ///   - transforms
  ///   - stretchmode
  ///   - tilemode
  ///   - alignmentx/alignmenty
  ///   - viewbox
  ///   - resource cleanup (textures & vertexbuffers)
  /// </summary>
  public class Brush : DependencyObject, IObservable
  {
    #region Private fields

    Property _opacityProperty;
    Property _relativeTransformProperty;
    Transform _transformProperty;
    Property _freezableProperty;
    bool _isOpacity;
    protected System.Drawing.RectangleF _bounds;
    protected System.Drawing.PointF _orginalPosition;
    protected System.Drawing.PointF _minPosition;

    #endregion

    #region Ctor

    public Brush()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _isOpacity = false;
      _opacityProperty = new Property(typeof(double), 1.0);
      _relativeTransformProperty = new Property(typeof(TransformGroup), new TransformGroup());
      _transformProperty = null;
      _freezableProperty = new Property(typeof(bool), false);
      _bounds = new System.Drawing.RectangleF(0, 0, 0, 0);
      _orginalPosition = new System.Drawing.PointF(0, 0);
    }

    void Attach()
    {
      _opacityProperty.Attach(OnPropertyChanged);
    }

    void Detach()
    {
      _opacityProperty.Detach(OnPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      Brush b = source as Brush;
      IsOpacityBrush = copyManager.GetCopy(b.IsOpacityBrush);
      Opacity = copyManager.GetCopy(b.Opacity);
      RelativeTransform = copyManager.GetCopy(b.RelativeTransform);
      Transform = copyManager.GetCopy(b.Transform);
      Freezable = copyManager.GetCopy(b.Freezable);
      Attach();
    }

    #endregion

    public event ObjectChangedHandler ObjectChanged;

    #region Protected methods

    protected void Fire()
    {
      if (ObjectChanged != null)
        ObjectChanged(this);
    }

    /// <summary>
    /// Called when a property changed.
    /// </summary>
    /// <param name="prop">The property.</param>
    /// <param name="oldValue">The old value of the property.</param>
    protected virtual void OnPropertyChanged(Property prop, object oldValue)
    { }

    #endregion

    #region Public properties

    public Property FreezableProperty
    {
      get { return _freezableProperty; }
    }

    public bool Freezable
    {
      get { return (bool)_freezableProperty.GetValue(); }
      set { _freezableProperty.SetValue(value); }
    }

    public Property OpacityProperty
    {
      get { return _opacityProperty; }
    }

    public double Opacity
    {
      get { return (double)_opacityProperty.GetValue(); }
      set { _opacityProperty.SetValue(value); }
    }

    public Property RelativeTransformProperty
    {
      get { return _relativeTransformProperty; }
    }

    public TransformGroup RelativeTransform
    {
      get { return (TransformGroup)_relativeTransformProperty.GetValue(); }
      set { _relativeTransformProperty.SetValue(value); }
    }

    public Transform Transform
    {
      get { return _transformProperty; }
      set { _transformProperty = value; }
    }

    public bool IsOpacityBrush
    {
      get { return _isOpacity; }
      set { _isOpacity = value; }
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
    /// <param name="u">The u.</param>
    /// <param name="v">The v.</param>
    /// <param name="color">The color.</param>
    public virtual void Scale(ref float u, ref float v, ref Color4 color)
    { }

    public virtual void SetupBrush(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      float w = (float)element.ActualWidth;
      float h = (float)element.ActualHeight;
      float xoff = _bounds.X;
      float yoff = _bounds.Y;
      if (element.FinalLayoutTransform != null)
      {
        w = _bounds.Width;
        h = _bounds.Height;
        element.FinalLayoutTransform.TransformXY(ref w, ref h);
        element.FinalLayoutTransform.TransformXY(ref xoff, ref yoff);
      }
      for (int i = 0; i < verts.Length; ++i)
      {
        float u, v;
        float x1, y1;
        y1 = (float)verts[i].Y;
        v = (float)(y1 - (element.ActualPosition.Y + yoff));
        v /= (float)(h);

        x1 = (float)verts[i].X;
        u = (float)(x1 - (element.ActualPosition.X + xoff));
        u /= (float)(w);

        if (u < 0) u = 0;
        if (u > 1) u = 1;
        if (v < 0) v = 0;
        if (v > 1) v = 1;
        unchecked
        {
          Color4 color = ColorConverter.FromColor(System.Drawing.Color.White);
          color.Alpha *= (float)Opacity;
          verts[i].Color = color.ToArgb();
        }
        verts[i].Tu1 = u;
        verts[i].Tv1 = v;
        verts[i].Z = element.ActualPosition.Z;
      }
    }

    protected void UpdateBounds(FrameworkElement element, ref PositionColored2Textured[] verts)
    {
      float minx = float.MaxValue;
      float miny = float.MaxValue;
      float maxx = 0;
      float maxy = 0;
      for (int i = 0; i < verts.Length; ++i)
      {
        if (verts[i].X < minx) minx = verts[i].X;
        if (verts[i].Y < miny) miny = verts[i].Y;

        if (verts[i].X > maxx) maxx = verts[i].X;
        if (verts[i].Y > maxy) maxy = verts[i].Y;
      }
      if (element.FinalLayoutTransform != null)
      {
        maxx -= minx;
        maxy -= miny;
        minx -= (float)element.ActualPosition.X;
        miny -= (float)element.ActualPosition.Y;
        element.FinalLayoutTransform.InvertXY(ref minx, ref miny);
        element.FinalLayoutTransform.InvertXY(ref maxx, ref maxy);

        _orginalPosition.X = (float)element.ActualPosition.X;
        _orginalPosition.Y = (float)element.ActualPosition.Y;
        _minPosition.X = _orginalPosition.X + minx;
        _minPosition.Y = _orginalPosition.Y + miny;
      }
      _bounds = new System.Drawing.RectangleF(minx, miny, maxx, maxy);
    }

    public virtual bool BeginRender(VertexBuffer vertexBuffer, int primitiveCount,
        PrimitiveType primitiveType)
    {
      return false;
    }

    public virtual void BeginRender(Texture tex)
    { }

    public virtual void EndRender()
    { }

    public virtual void Allocate()
    { }

    public virtual void Deallocate()
    { }

    public virtual void SetupPrimitive(PrimitiveContext context)
    { }

    #endregion
  }
}

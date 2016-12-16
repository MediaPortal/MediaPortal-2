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

using SharpDX;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public class RenderContext
  {
    #region Protected fields

    protected readonly float _zOrder = 1.0f;
    protected readonly double _opacity = 1.0f;
    protected readonly Matrix _transform;
    protected RectangleF _transformedRenderBounds = RectangleF.Empty;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="startingTransform">Initial transform to use in this context.</param>
    /// <param name="untransformedBounds">Bounds of the element currently being rendered, in local space.</param>
    public RenderContext(Matrix startingTransform, RectangleF untransformedBounds)
    {
      _transform = startingTransform;
      SetUntransformedContentsBounds(untransformedBounds);
    }

    /// <summary>
    /// Creates a new instance of this class. This constructor gets typically called from the <see cref="Derive"/> method.
    /// </summary>
    /// <param name="transform">Combined current transformation to use in this context.</param>
    /// <param name="opacity">Combined opacity value.</param>
    /// <param name="untransformedBounds">Bounds of the element currently being rendered, in local space.</param>
    /// <param name="zOrder">Z coordinate of the currently rendered element.</param>
    public RenderContext(Matrix transform, double opacity, RectangleF untransformedBounds, float zOrder)
    {
      _zOrder = zOrder;
      _opacity = opacity;
      _transform = transform;
      SetUntransformedContentsBounds(untransformedBounds);
    }

    protected void SetUntransformedContentsBounds(RectangleF bounds)
    {
      _transformedRenderBounds = _transform.GetIncludingTransformedRectangle(bounds);
    }

    #endregion

    #region Public methods

    public RenderContext Derive(RectangleF bounds, Matrix? localLayoutTransform,
        Matrix? localRenderTransform, Vector2? renderTransformOrigin,
        double localOpacity)
    {
      Matrix finalTransform = _transform.Clone();
      if (localLayoutTransform.HasValue && localLayoutTransform != Matrix.Identity)
      {
        // Layout transforms don't support translations, so center the transformation matrix at the start point
        // of the control and apply the layout transform without translation
        Vector2 origin = new Vector2(bounds.X + 0.5f*bounds.Width, bounds.Y + 0.5f*bounds.Height);
        Matrix transform = Matrix.Translation(new Vector3(-origin.X, -origin.Y, 0));
        transform *= localLayoutTransform.Value.RemoveTranslation();
        transform *= Matrix.Translation(new Vector3(origin.X, origin.Y, 0));
        finalTransform = transform * finalTransform;
      }
      if (localRenderTransform.HasValue && localRenderTransform != Matrix.Identity)
      {
        Vector2 origin = renderTransformOrigin.HasValue ? new Vector2(
            bounds.X + bounds.Width * renderTransformOrigin.Value.X,
            bounds.Y + bounds.Height * renderTransformOrigin.Value.Y) : new Vector2(bounds.X, bounds.Y);
        Matrix transform = Matrix.Translation(new Vector3(-origin.X, -origin.Y, 0));
        transform *= localRenderTransform.Value;
        transform *= Matrix.Translation(new Vector3(origin.X, origin.Y, 0));
        finalTransform = transform * finalTransform;
      }
      RenderContext result = new RenderContext(finalTransform, _opacity * localOpacity, bounds, _zOrder - 0.001f) { ClearOccupiedAreaOnly = ClearOccupiedAreaOnly };
      return result;
    }

    #endregion

    #region Public properties

    public float ZOrder
    {
      get { return _zOrder; }
    }

    public double Opacity
    {
      get { return _opacity; }
    }

    public Matrix Transform
    {
      get { return _transform; }
    }

    public RectangleF OccupiedTransformedBounds
    {
      get { return _transformedRenderBounds; }
    }

    /// <summary>
    /// If set to <c>true</c>, only the area of <see cref="OccupiedTransformedBounds"/> will be cleared on render target. This gives
    /// a better performance, but can only be used if no Effect which affects a larger area then the actual bounds.
    /// </summary>
    public bool ClearOccupiedAreaOnly { get; set; }

    #endregion

    public void IncludeUntransformedContentsBounds(RectangleF bounds)
    {
      RectangleF includingTransformedBounds = _transform.GetIncludingTransformedRectangle(bounds);
      IncludeTransformedContentsBounds(includingTransformedBounds);
    }

    public void SetUntransformedBounds(RectangleF bounds)
    {
      _transformedRenderBounds = _transform.GetIncludingTransformedRectangle(bounds);
    }

    public void IncludeTransformedContentsBounds(RectangleF bounds)
    {
      _transformedRenderBounds = RectangleF.Union(_transformedRenderBounds, bounds);
    }
  }
}

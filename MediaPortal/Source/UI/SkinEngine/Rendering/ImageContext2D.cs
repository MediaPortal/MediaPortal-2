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

using System;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  /// <summary>
  /// This class provides a common framework for rendering images at specific sizes within a larger primitive.
  /// Images will always be centered and any space will be set to a given color. More advanced features allow for
  /// custom effect (such a greyscale) to be applied and for a belended transition between two <see cref="ImageContext2D"/>
  /// objects.
  /// </summary>
  public class ImageContext2D : EffectContext
  {
    protected const float FLOAT_EQUALITY_LIMIT = 0.001f;

    #region Protected fields

    protected EffectAsset _effectTransition;
    protected SizeF _frameSize;
    protected SizeF _rotatedFrameSize;
    protected Vector4 _imageTransform;
    protected SizeF _lastImageSize;

    protected string _shaderBaseName = null;
    protected string _shaderTransitionBaseName = null;
    protected string _shaderTransformName = null;
    protected string _shaderTransitionName = null;
    protected RightAngledRotation _rotation = RightAngledRotation.Zero;

    protected Bitmap1 _bitmap;
    protected Brush _maskBrush;

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the size of area the image will render into. Any aspect ration adjustment must be 
    /// done prior to setting this property.
    /// </summary>
    /// <remarks>
    /// This property is needed to center the image to be rendered into the given frame.
    /// </remarks>
    public SizeF FrameSize
    {
      get { return _frameSize; }
      set
      {
        _frameSize = value;
        _rotatedFrameSize = GetRotatedSize(value);
        _refresh = true;
      }
    }

    /// <summary>
    /// Gets the rotated <see cref="FrameSize"/> for image size adjustments. If a <see cref="Rotation"/> is used, image size
    /// adjustments must be done against this property. If no rotation is used, this property returns the same value as
    /// <see cref="FrameSize"/>, so image size adjustments should always be done using this property.
    /// </summary>
    public SizeF RotatedFrameSize
    {
      get { return _rotatedFrameSize; }
    }

    /// <summary>
    /// Gets or sets the effect to be used as the base for rendering. Other shaders fragments may be added to
    /// create the final effect.
    /// </summary>
    public string ShaderBase
    {
      get { return _shaderBaseName; }
      set
      {
        _shaderBaseName = value;
        _refresh = true;
      }
    }

    /// <summary>
    /// Gets or sets the partial effect to be used for transforming the image.
    /// </summary>
    public string ShaderTransform
    {
      get { return _shaderTransformName; }
      set
      {
        _shaderTransformName = value;
        _refresh = true;
      }
    }

    /// <summary>
    /// Gets or sets the partial effect to be used for the image effect.
    /// </summary>
    public string ShaderTransition
    {
      get { return _shaderTransitionName; }
      set
      {
        _shaderTransitionName = value;
        _refresh = true;
      }
    }

    /// <summary>
    /// Gets or sets an additional rotation.
    /// </summary>
    /// <remarks>
    /// If a rotation is used, the image will be shown rotated against the frame (see <see cref="FrameSize"/>).
    /// If the class using this <see cref="ImageContext"/> adjusts the bitmap size to the frame (for example uniform stretch),
    /// it must adapt the original bitmap size to the <see cref="RotatedFrameSize"/> and use the result, without an extra rotation,
    /// as parameter for the render methods.
    /// </remarks>
    public RightAngledRotation Rotation
    {
      get { return _rotation; }
      set
      {
        _rotation = value;
        _rotatedFrameSize = GetRotatedSize(_frameSize);
        _refresh = true;
      }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Renders the <see cref="ImageContext"/>.
    /// </summary>
    /// <param name="renderContext">The current rendering context.</param>
    /// <param name="targetRect">The size, the final image should take within the frame. This size is given in the same
    /// orientation as the <paramref name="bitmap"/>, i.e. it is not rotated.</param>
    /// <param name="bitmap">A bitmap object containing the image.</param>
    /// <param name="textureClip">The section of the bitmap that should be rendered. Values are between 0 and 1.</param>
    /// <param name="borderColor">The color to use outside the image's boundaries.</param>
    /// <param name="frameData">Additional data to be used by the shaders.</param>
    /// <returns><c>true</c> if the rendering operation was started.</returns>
    public bool StartRender(RenderContext renderContext, RectangleF targetRect, Bitmap1 bitmap, RectangleF textureClip, Color borderColor, Vector4 frameData)
    {
      _bitmap = bitmap;
      if (_maskBrush == null)
        _maskBrush = new SolidColorBrush(GraphicsDevice11 .Instance.Context2D1, Color.Black);

      LayerParameters1 layerParameters = new LayerParameters1
      {
        ContentBounds = renderContext.OccupiedTransformedBounds,
        LayerOptions = LayerOptions1.None,
        MaskAntialiasMode = AntialiasMode.PerPrimitive,
        MaskTransform = Matrix.Identity,
        Opacity = 1.0f,
        OpacityBrush = _maskBrush
      };

      GraphicsDevice11.Instance.Context2D1.PushLayer(layerParameters, null);
      GraphicsDevice11.Instance.Context2D1.DrawBitmap(_bitmap, targetRect, textureClip, (float)renderContext.Opacity, renderContext);
      GraphicsDevice11.Instance.Context2D1.PopLayer();
      return true;
    }

    ///// <summary>
    ///// Starts a rendering operation where two images are mixed together using a transition effect.
    ///// </summary>
    ///// <param name="renderContext">The current rendering context.</param>
    ///// <param name="mixValue">A value between 0.0 and 1.0 that governs how much each image contributes to the final rendering.</param>
    ///// <param name="startContext">The <see cref="ImageContext"/> data for the starting position of the transition
    ///// (this context is the end point).</param>
    ///// <param name="targetEndImageSize">The size, the final end image should take within the frame. This size is given in the same
    ///// orientation as the <paramref name="endTexture"/>, i.e. it is not rotated.</param>
    ///// <param name="endTexture">A bitmap object containing the end image.</param>
    ///// <param name="endTextureClip">The section of the end bitmap that should be rendered. Values are between 0 and 1.</param>
    ///// <param name="borderColor">The color to use outside the image's boundaries.</param>
    ///// <param name="startFrameData">Additional data to be used by the starting image shaders.</param>
    ///// <param name="endFrameData">Additional data to be used by the ending image shaders.</param>
    ///// <returns><c>true</c> if the rendering operation was started.</returns>
    //public bool StartRenderTransition(RenderContext renderContext, float mixValue, ImageContext startContext,
    //    SizeF targetEndImageSize, Texture endTexture, RectangleF endTextureClip, Color borderColor, Vector4 startFrameData, Vector4 endFrameData)
    //{
    //  RefreshParameters(targetEndImageSize, endTexture, endTextureClip);
    //  if (_effectTransition == null)
    //    _effectTransition = ContentManager.Instance.GetEffect(GetTransitionEffectName());
    //  if (_lastTexture == null || _effectTransition == null)
    //    return false;

    //  // Apply effect parameters    
    //  _effectTransition.Parameters[PARAM_OPACITY] = (float) renderContext.Opacity;
    //  _effectTransition.Parameters[PARAM_RELATIVE_TRANSFORM] = _inverseRelativeTransformCache;
    //  _effectTransition.Parameters[PARAM_BRUSH_TRANSFORM] = _imageTransform;
    //  _effectTransition.Parameters[PARAM_FRAME_DATA] = endFrameData;
    //  _effectTransition.Parameters[PARAM_MIX_AB] = mixValue;

    //  startContext.ApplyTransitionParametersAsStartingSource(_effectTransition, startFrameData);

    //  // Disable antialiasing for image rendering.
    //  GraphicsDevice.Device.SetRenderState(RenderState.MultisampleAntialias, false);

    //  // Set border colour for area outside of bitmap boundaries
    //  GraphicsDevice.Device.SetSamplerState(0, SamplerState.BorderColor, borderColor.ToBgra());
    //  GraphicsDevice.Device.SetSamplerState(1, SamplerState.BorderColor, borderColor.ToBgra());

    //  // Render
    //  _effectTransition.StartRender(_lastTexture, renderContext.Transform);
    //  return true;
    //}

    ///// <summary>
    ///// Completes a transition rendering operation.
    ///// </summary>
    //public void EndRenderTransition()
    //{
    //  if (_effectTransition != null)
    //    _effectTransition.EndRender();      
    //}

    /// <summary>
    /// A helper method that adjusts a given frame size so that an image fitted within it not be scaled by
    /// the transformation used to scale the skin to the graphics device (window).
    /// </summary>
    /// <param name="frameSize">The skin relative size.</param>
    /// <returns>The passed size pre-scaled to compensate for any Skin-to-GraphicsDevice transformations.</returns>
    public static SizeF AdjustForSkinAR(SizeF frameSize)
    {
      // Adjust target size to match final Skin scaling
      frameSize.Width *= GraphicsDevice.Width / (float)SkinContext.SkinResources.SkinWidth;
      frameSize.Height *= GraphicsDevice.Height / (float)SkinContext.SkinResources.SkinHeight;
      return frameSize;
    }

    public SizeF GetRotatedSize(SizeF size)
    {
      return _rotation == RightAngledRotation.HalfPi || _rotation == RightAngledRotation.ThreeHalfPi ? new SizeF(size.Height, size.Width) : size;
    }

    public override void Clear()
    {
      base.Clear();
      _effectTransition = null;
      DependencyObject.TryDispose(ref _maskBrush);
    }

    #endregion

    #region Protected methods

    protected override bool StartRender(RenderContext renderContext, Color borderColor, Vector4 frameData)
    {
      if (_effect == null || _lastTexture == null)
        return false;

      //// Apply effect parameters
      //_effect.Parameters[PARAM_OPACITY] = (float) renderContext.Opacity;
      //_effect.Parameters[PARAM_FRAME_DATA] = frameData;
      //_effect.Parameters[PARAM_RELATIVE_TRANSFORM] = _inverseRelativeTransformCache;
      //_effect.Parameters[PARAM_BRUSH_TRANSFORM] = _imageTransform;

      //if (_extraParameters != null)
      //  foreach (KeyValuePair<string, object> pair in _extraParameters)
      //    _effect.Parameters[pair.Key] = pair.Value;

      //// Disable antialiasing for image rendering.
      //GraphicsDevice.Device.SetRenderState(RenderState.MultisampleAntialias, false);

      //// Set border colour for area outside of bitmap boundaries
      //GraphicsDevice.Device.SetSamplerState(0, SamplerState.BorderColor, borderColor.ToBgra());

      //// Render
      //_effect.StartRender(_lastTexture, renderContext.Transform);
      return true;
    }

    protected override void RefreshParameters(SizeF targetImageSize, Texture texture, RectangleF textureClip)
    {
      // If necessary update our image transformation to best fit the frame
      if (_refresh || texture != _lastTexture ||
          Math.Abs(targetImageSize.Width - _lastImageSize.Width) > FLOAT_EQUALITY_LIMIT ||
          Math.Abs(targetImageSize.Height - _lastImageSize.Height) > FLOAT_EQUALITY_LIMIT
          || textureClip != _lastTextureClip)
      {
        _lastTexture = texture;
        _lastTextureClip = textureClip;

        // Convert image dimensions to bitmap space

        // We're doing the relative transform first in the shader, that's why we just have to rotate the frame size
        Vector4 textureRect = new Vector4(0.0f, 0.0f,
            (targetImageSize.Width + 1.0f) / _rotatedFrameSize.Width, (targetImageSize.Height + 1.0f) / _rotatedFrameSize.Height);

        // Center bitmap
        textureRect.X = (1.0f - textureRect.Z) / 2.0f;
        textureRect.Y = (1.0f - textureRect.W) / 2.0f;

        // Compensate for bitmap surface borders
        textureRect.Z /= textureClip.Width;
        textureRect.W /= textureClip.Height;

        // Determine correct 2D transform for mapping the bitmap to the correct place
        float repeatx = 1.0f / textureRect.Z;
        float repeaty = 1.0f / textureRect.W;
        if (repeatx < 0.001f || repeaty < 0.001f)
        {
          _effect = null;
          _refresh = true;
          return;
        }

        _inverseRelativeTransformCache = TranslateRotation(_rotation);
        _inverseRelativeTransformCache.Invert();
        _imageTransform = new Vector4(textureRect.X * repeatx - textureClip.X, textureRect.Y * repeaty - textureClip.Y, repeatx, repeaty);

        // Build our effects
        _effect = ContentManager.Instance.GetEffect(GetEffectName());
        // The transition effect will be allocated when required
        _effectTransition = null;

        // Trigger refresh event so that the calling class can update any custom parameters
        if (OnRefresh != null)
          OnRefresh();

        _lastImageSize = targetImageSize;
        _refresh = false;
      }
    }

    protected static Matrix TranslateRotation(RightAngledRotation rar)
    {
      if (rar == RightAngledRotation.Zero)
        return Matrix.Identity;
      float rotation = 0;
      switch (rar)
      {
        case RightAngledRotation.HalfPi:
          rotation = (float)Math.PI / 2;
          break;
        case RightAngledRotation.Pi:
          rotation = (float)Math.PI;
          break;
        case RightAngledRotation.ThreeHalfPi:
          rotation = (float)Math.PI * 3 / 2;
          break;
      }
      Matrix matrix = Matrix.Translation(-0.5f, -0.5f, 0);
      matrix *= Matrix.RotationZ(rotation);
      matrix *= Matrix.Translation(0.5f, 0.5f, 0);
      return matrix;
    }

    protected void ApplyTransitionParametersAsStartingSource(EffectAsset effect, Vector4 frameData)
    {
      //effect.Parameters[PARAM_TEXTURE_START] = _lastTexture;
      //effect.Parameters[PARAM_RELATIVE_TRANSFORM_START] = _inverseRelativeTransformCache;
      //effect.Parameters[PARAM_BRUSH_TRANSFORM_START] = _imageTransform;
      //effect.Parameters[PARAM_FRAME_DATA_START] = frameData;
    }

    #endregion
  }
}

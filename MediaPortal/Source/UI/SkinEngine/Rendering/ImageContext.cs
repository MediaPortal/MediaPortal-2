#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Collections.Generic;
using System.Drawing;
using MediaPortal.Core;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SlimDX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  public delegate void ImageContextRefreshHandler();

  /// <summary>
  /// This class provides a common framework for rendering images at specific sizes within a larger primitive.
  /// Images will always be centered and any space will be set to a given color. More advanced features allow for
  /// custom effect (such a greyscale) to be applied and for a belended transition between two <see cref="ImageContext"/>
  /// objects.
  /// </summary>
  public class ImageContext
  {
    /// <summary>
    /// This event is fired when the context determines that an important property has changed (or <see cref="Refresh"/> is called), 
    /// and allows the calling class to update any additonal effect parameters.
    /// </summary>
    public ImageContextRefreshHandler OnRefresh;
    
    #region Consts

    protected const string EFFECT_DEFAULT_NAME = "imagecontext_default";
    protected const string EFFECT_BASE_DEFAULT_NAME = "imagecontext_base";
    protected const string EFFECT_BASE_TRANSITION_DEFAULT_NAME = "imagecontext_transition_base";
    protected const string EFFECT_TRANSFORM_DEFAULT_NAME = @"transforms\none";
    protected const string EFFECT_EFFECT_DEFAULT_NAME = @"effects\none";
    protected const string EFFECT_TRANSITION_DEFAULT_NAME = @"transitions\dissolve";

    protected const string PARAM_OPACITY = "g_opacity";
    protected const string PARAM_TEXTURE = "g_texture";
    protected const string PARAM_BRUSH_TRANSFORM = "g_imagetransform";
    protected const string PARAM_FRAME_DATA = "g_framedata";

    // Transition parameters
    protected const string PARAM_TEXTURE_START= "g_textureA";
    protected const string PARAM_BRUSH_TRANSFORM_START = "g_imagetransformA";
    protected const string PARAM_FRAME_DATA_START = "g_framedataA";
    protected const string PARAM_MIX_AB = "g_mixAB";

    protected const float FLOAT_EQUALITY_LIMIT = 0.001f;

    #endregion

    #region Protected fields

    protected EffectAsset _effect;
    protected EffectAsset _effectTransition;
    protected SizeF _frameSize;
    protected Vector4 _imageTransform;
    protected SizeF _lastImageSize;
    protected Texture _lastTexture;
    protected bool _refresh = true;

    protected string _shaderBaseName = null;
    protected string _shaderTransitionBaseName = null;
    protected string _shaderTransformName = null;
    protected string _shaderEffectName = null;
    protected string _shaderTransitionName = null;
    protected Dictionary<string, object> _extraParameters;
    
    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the size of area the image will render into. Any aspect ration adjustment must be 
    /// done prior to setting this property.
    /// </summary>
    public SizeF FrameSize
    {
      get { return _frameSize; }
      set
      {
        _frameSize = value;
        _refresh = true;
      }
    }

    /// <summary>
    /// Gets or sets the extra effect parameters to use when rendering.
    /// </summary>
    public Dictionary<string, object> ExtraParameters
    {
      get { return _extraParameters; }
      set { _extraParameters = value; }
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
    public string ShaderEffect
    {
      get { return _shaderEffectName; }
      set
      {
        _shaderEffectName = value;
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

    #endregion

    #region Public methods

    public void Update(SizeF imageSize, Texture texture, float maxU, float maxV)
    {
      // Note: There is no check on maxU / MaxV or texture changing. This must be handled by the calling class!
      RefreshParameters(imageSize, texture, maxU, maxV);
    }

    public void Update(SizeF imageSize, TextureAsset texture)
    {
      _refresh |= texture.Texture != _lastTexture;
      RefreshParameters(imageSize, texture.Texture, texture.MaxU, texture.MaxV);
    }

    /// <summary>
    /// Renders this <see cref="ImageContext"/>.
    /// </summary>
    /// <param name="renderContext">The current rendering context.</param>
    /// <param name="imageSize">The size of the final image within the frame.</param>
    /// <param name="texture">A texture asset containing the image.</param>
    /// <param name="borderColor">The color to use outside the image's boundaries.</param>
    /// <param name="frameData">Additional data to be used by the shaders.</param>
    /// <returns><c>true</c> if the rendering operation was started.</returns>
    public bool StartRender(RenderContext renderContext, SizeF imageSize, TextureAsset texture,
        int borderColor, Vector4 frameData)
    {
      Update(imageSize, texture);
      return StartRender(renderContext, borderColor, frameData);
    }

    /// <summary>
    /// Renders the <see cref="ImageContext"/>.
    /// </summary>
    /// <param name="renderContext">The current rendering context.</param>
    /// <param name="imageSize">The size of the final image within the frame.</param>
    /// <param name="texture">A texture object containing the image.</param>
    /// <param name="maxU">The value of the U texture coord that defines the horizontal extent of the image.</param>
    /// <param name="maxV">The value of the U texture coord that defines the horizontal extent of the image.</param>
    /// <param name="borderColor">The color to use outside the image's boundaries.</param>
    /// <param name="frameData">Additional data to be used by the shaders.</param>
    /// <returns><c>true</c> if the rendering operation was started.</returns>
    /// <remarks>
    /// There is no check on maxU / maxV or texture changing. This must be handled by the calling class!
    /// </remarks>
    public bool StartRender(RenderContext renderContext, SizeF imageSize, Texture texture, float maxU, 
      float maxV, int borderColor, Vector4 frameData)
    {
      Update(imageSize, texture, maxU, maxV);
      return StartRender(renderContext, borderColor, frameData);
    }

    /// <summary>
    /// Starts a rendering operation where two images are mixed together using a transition effect.
    /// </summary>
    /// <param name="renderContext">The current rendering context.</param>
    /// <param name="mixValue">A value between 0.0 and 1.0 that governs how much each image contributes to the final rendering.</param>
    /// <param name="startContext">The <see cref="ImageContext"/> data for the starting position of the transition (this context is the end point).</param>
    /// <param name="borderColor">The color to use outside the image's boundaries.</param>
    /// <param name="startFrameData">Additional data to be used by the starting image shaders.</param>
    /// <param name="endFrameData">Additional data to be used by the ending image shaders.</param>
    /// <returns><c>true</c> if the rendering operation was started.</returns>
    public bool StartRenderTransition(RenderContext renderContext, float mixValue, ImageContext startContext, int borderColor, Vector4 startFrameData, Vector4 endFrameData)
    {
      if (_effectTransition == null)
        _effectTransition = ServiceRegistration.Get<ContentManager>().GetEffect(GetTransitionEffectName());
      if (_lastTexture == null || _effectTransition == null)
        return false;

      // Apply effect parameters    
      _effectTransition.Parameters[PARAM_OPACITY] = (float)renderContext.Opacity;
      _effectTransition.Parameters[PARAM_BRUSH_TRANSFORM] = _imageTransform;
      _effectTransition.Parameters[PARAM_FRAME_DATA] = endFrameData;
      _effectTransition.Parameters[PARAM_MIX_AB] = mixValue;

      startContext.ApplyTransitionParametersAsStartingSource(_effectTransition, startFrameData);

      // Set border colour for area outside of texture boundaries
      GraphicsDevice.Device.SetSamplerState(0, SamplerState.BorderColor, borderColor);
      GraphicsDevice.Device.SetSamplerState(1, SamplerState.BorderColor, borderColor);
          
      // Render
      _effectTransition.StartRender(_lastTexture, renderContext.Transform);
      return true;
    }

    /// <summary>
    /// Completes a rendering operation.
    /// </summary>
    public void EndRender()
    {
      if (_effect != null)
        _effect.EndRender();
    }

    /// <summary>
    /// Completes a transition rendering operation.
    /// </summary>
    public void EndRenderTransition()
    {
      if (_effectTransition != null)
        _effectTransition.EndRender();      
    }

    /// <summary>
    /// A helper method that adjusts a given frame size so that an image fitted within it not be scaled by
    /// the transformation used to scale the skin to the graphics device (window).
    /// </summary>
    /// <param name="frameSize">The skin relative size.</param>
    /// <returns>The passed size pre-scaled to compensate for any Skin-to-GraphicsDevice transformations.</returns>
    public static SizeF AdjustForSkinAR(SizeF frameSize)
    {
      // Adjust target size to match final Skin scaling
      frameSize.Width *= GraphicsDevice.Width / (float) SkinContext.SkinResources.SkinWidth;
      frameSize.Height *= GraphicsDevice.Height / (float) SkinContext.SkinResources.SkinHeight;
      return frameSize;
    }

    /// <summary>
    /// Triggers a refresh of this <see cref="ImageContext"/>'s effect parameters.
    /// </summary>
    public void Refresh()
    {
      _refresh = true;
    }

    #endregion

    #region Protected methods

    protected bool StartRender(RenderContext renderContext, int borderColor, Vector4 frameData)
    {
      if (_effect == null || _lastTexture == null)
        return false;

      // Apply effect parameters
      _effect.Parameters[PARAM_OPACITY] = (float) renderContext.Opacity;
      _effect.Parameters[PARAM_FRAME_DATA] = frameData;
      _effect.Parameters[PARAM_BRUSH_TRANSFORM] = _imageTransform;

      if (_extraParameters != null)
        foreach (KeyValuePair<string, object> pair in _extraParameters)
          _effect.Parameters[pair.Key] = pair.Value;

      // Set border colour for area outside of texture boundaries
      GraphicsDevice.Device.SetSamplerState(0, SamplerState.BorderColor, borderColor);
      // Render
      _effect.StartRender(_lastTexture, renderContext.Transform);
      return true;
    }

    protected void RefreshParameters(SizeF imageSize, Texture texture, float maxU, float maxV)
    {
      _lastTexture = texture;
      // If necessary update our image transformation to best fit the frame
      if (_refresh || Math.Abs(imageSize.Width - _lastImageSize.Width) > FLOAT_EQUALITY_LIMIT ||
          Math.Abs(imageSize.Height - _lastImageSize.Height) > FLOAT_EQUALITY_LIMIT)
      {
        // Convert image dimensions to texture space
        Vector4 textureRect = new Vector4(0.0f, 0.0f, imageSize.Width+1.0f, imageSize.Height+1.0f);
        textureRect.Z /= _frameSize.Width;
        textureRect.W /= _frameSize.Height;

        // Center texture
        textureRect.X += (1.0f - textureRect.Z) / 2.0f;
        textureRect.Y += (1.0f - textureRect.W) / 2.0f;

        // Compensate for texture surface borders
        textureRect.Z /= maxU;
        textureRect.W /= maxV;

        // Determine correct 2D transform for mapping the texture to the correct place
        float repeatx = 1.0f / textureRect.Z;
        float repeaty = 1.0f / textureRect.W;
        if (repeatx < 0.001f || repeaty < 0.001f)
        {
          _effect = null;
          _refresh = true;
          return;
        }
        _imageTransform = new Vector4(textureRect.X * repeatx, textureRect.Y * repeaty, repeatx, repeaty);

        // Build our effects
        _effect = ServiceRegistration.Get<ContentManager>().GetEffect(GetEffectName());
        // The transition effect will be allocated when required
        _effectTransition = null;

        // Trigger refresh event so that the calling class can update any custom parameters
        if (OnRefresh != null)
          OnRefresh();

        _lastImageSize = imageSize;
        _refresh = false;
      }
    }

    protected void ApplyTransitionParametersAsStartingSource(EffectAsset effect, Vector4 frameData)
    {
      effect.Parameters[PARAM_TEXTURE_START] = _lastTexture;
      effect.Parameters[PARAM_BRUSH_TRANSFORM_START] = _imageTransform;
      effect.Parameters[PARAM_FRAME_DATA_START] = frameData;
    }

    protected string GetEffectName()
    {
      if (_shaderBaseName == null && _shaderEffectName == null && _shaderTransformName == null)
        return EFFECT_DEFAULT_NAME;
      // Using at least one custom component, build a composite effect
      string name = _shaderBaseName ?? EFFECT_BASE_DEFAULT_NAME;
      name += ';' + (_shaderTransformName ?? EFFECT_TRANSFORM_DEFAULT_NAME);
      name += ';' + (_shaderEffectName ?? EFFECT_EFFECT_DEFAULT_NAME);
      return name;
    }

    protected string GetTransitionEffectName()
    {
      string name = _shaderTransitionBaseName ?? EFFECT_BASE_TRANSITION_DEFAULT_NAME;
      name += ';' + (_shaderTransitionName ?? EFFECT_TRANSITION_DEFAULT_NAME);
      name += ';' + (_shaderTransformName ?? EFFECT_TRANSFORM_DEFAULT_NAME);
      name += ';' + (_shaderEffectName ?? EFFECT_EFFECT_DEFAULT_NAME);
      return name;
    }

    #endregion

    public void Clear()
    {
      _effect = null;
      _effectTransition = null;
      _lastTexture = null;
    }
  }
}

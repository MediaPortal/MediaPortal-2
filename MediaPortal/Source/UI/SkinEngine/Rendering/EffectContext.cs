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

using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;

namespace MediaPortal.UI.SkinEngine.Rendering
{
  /// <summary>
  /// EffectContext extends the ImageContext to allow using custom effects.
  /// </summary>
  public class EffectContext
  {
    /// <summary>
    /// This event is fired when the context determines that an important property has changed (or <see cref="Refresh"/> is called), 
    /// and allows the calling class to update any additonal effect parameters.
    /// </summary>
    public ImageContextRefreshHandler OnRefresh;

    #region Protected fields

    protected EffectAsset _effect;
    protected Texture _lastTexture;
    protected RectangleF _lastTextureClip;
    protected Matrix _inverseRelativeTransformCache;
    protected bool _refresh = true;

    protected string _shaderEffectName = null;
    protected Dictionary<string, object> _extraParameters;

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the extra effect parameters to use when rendering.
    /// </summary>
    public Dictionary<string, object> ExtraParameters
    {
      get { return _extraParameters; }
      set { _extraParameters = value; }
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

    #endregion

    #region Public methods

    public void Update(SizeF targetImageSize, Texture texture, RectangleF textureClip)
    {
      RefreshParameters(targetImageSize, texture, textureClip);
    }

    /// <summary>
    /// Renders the <see cref="ImageContext"/>.
    /// </summary>
    /// <param name="renderContext">The current rendering context.</param>
    /// <param name="targetImageSize">The size, the final image should take within the frame. This size is given in the same
    /// orientation as the <paramref name="texture"/>, i.e. it is not rotated.</param>
    /// <param name="texture">A texture object containing the image.</param>
    /// <param name="textureClip">The section of the texture that should be rendered. Values are between 0 and 1.</param>
    /// <param name="borderColor">The color to use outside the image's boundaries.</param>
    /// <param name="frameData">Additional data to be used by the shaders.</param>
    /// <returns><c>true</c> if the rendering operation was started.</returns>
    public bool StartRender(RenderContext renderContext, SizeF targetImageSize, Texture texture, RectangleF textureClip,
        Color borderColor, Vector4 frameData)
    {
      RefreshParameters(targetImageSize, texture, textureClip);
      return StartRender(renderContext, borderColor, frameData);
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
    /// Triggers a refresh of this <see cref="EffectContext"/>'s effect parameters.
    /// </summary>
    public void Refresh()
    {
      _refresh = true;
    }

    #endregion

    #region Protected methods

    protected virtual bool StartRender(RenderContext renderContext, Color borderColor, Vector4 frameData)
    {
      if (_effect == null || _lastTexture == null)
        return false;

      // Apply effect parameters
      if (_extraParameters != null)
        foreach (KeyValuePair<string, object> pair in _extraParameters)
          _effect.Parameters[pair.Key] = pair.Value;

      // Set border colour for area outside of texture boundaries
      GraphicsDevice.Device.SetSamplerState(0, SamplerState.BorderColor, borderColor.ToBgra());
      // Render
      _effect.StartRender(_lastTexture, renderContext.Transform);
      return true;
    }

    protected virtual void RefreshParameters(SizeF targetImageSize, Texture texture, RectangleF textureClip)
    {
      if (_refresh || _lastTexture != texture)
      {
        // Build our effects
        _lastTexture = texture;
        _effect = ContentManager.Instance.GetEffect(GetEffectName());
        _refresh = false;
      }
    }

    protected virtual string GetEffectName()
    {
      return _shaderEffectName;
    }

    #endregion

    public virtual void Clear()
    {
      _effect = null;
      _lastTexture = null;
    }
  }
}

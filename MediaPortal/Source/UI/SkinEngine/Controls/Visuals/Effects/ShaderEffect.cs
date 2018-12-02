#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct2D1;
using EffectContext = MediaPortal.UI.SkinEngine.Rendering.EffectContext;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  /// <summary>
  /// Provides a custom bitmap effect by using a PixelShader. It uses the <see cref="Rendering.EffectContext"/> to use any
  /// full shader file (in contrast to <see cref="ImageEffect"/>).
  /// </summary>
  public abstract class ShaderEffect : Effect
  {
    protected EffectContext _effectContext;
    protected string _shaderEffectName;
    protected bool _refresh = true;

    #region Rendering

    protected override bool BeginRenderEffectOverride(Bitmap1 texture, RenderContext renderContext)
    {
      if (_refresh)
      {
        _effectContext = new EffectContext();
        _refresh = false;
      }

      RectangleF rect = renderContext.OccupiedTransformedBounds.ToRectangleF();
      Size2F frameSize = new Size2F(rect.Width, rect.Height);
      //_effectContext.ExtraParameters = GetShaderParameters();
      _effectContext.ShaderEffect = _shaderEffectName;

      Vector4 lastFrameData = new Vector4(rect.Width, rect.Height, 0.0f, 0.0f);
      _effectContext.StartRender(renderContext, frameSize, texture, CROP_FULLSIZE, Color.Transparent, lastFrameData);
      return true;
    }

    protected virtual Dictionary<string, object> GetShaderParameters()
    {
      return null;
    }

    #endregion
  }
}

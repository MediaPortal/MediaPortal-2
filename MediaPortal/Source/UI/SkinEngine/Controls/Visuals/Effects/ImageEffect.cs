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
using MediaPortal.UI.SkinEngine.Rendering;
using MediaPortal.UI.SkinEngine.SkinManagement;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals.Effects
{
  /// <summary>
  /// ImageEffect uses the <see cref="ImageContext"/> to apply a partitial shader effect stored in <c>shaders\effects</c> directory of the Skin.
  /// </summary>
  public abstract class ImageEffect : Effect
  {
    protected ImageContext _imageContext;
    protected string _partialShaderEffect;
    protected bool _refresh = true;

    #region Rendering

    protected override bool BeginRenderEffectOverride(Texture texture, RenderContext renderContext)
    {
      if (_refresh)
      {
        _imageContext = new ImageContext();
        _refresh = false;
      }

      RectangleF rect = renderContext.OccupiedTransformedBounds;
      SizeF frameSize = new SizeF(rect.Width, rect.Height);
      _imageContext.FrameSize = frameSize;
      _imageContext.ExtraParameters = GetShaderParameters();
      _imageContext.ShaderEffect = SkinResources.EFFECTS_SUB_DIRECTORY + '\\' + _partialShaderEffect;

      Vector4 lastFrameData = new Vector4(rect.Width, rect.Height, 0.0f, 0.0f);
      _imageContext.StartRender(renderContext, frameSize, texture, CROP_FULLSIZE, Color.Transparent, lastFrameData);
      return true;
    }

    protected virtual Dictionary<string, object> GetShaderParameters()
    {
      return null;
    }

    public override void EndRender()
    {
      _imageContext.EndRender();
    }

    #endregion
  }
}

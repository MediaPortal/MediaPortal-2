#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  /// <summary>
  /// Encapsulates an effect which can render a vertex buffer with a texture.
  /// The effect gets loaded from the skin's shaders directory.
  /// </summary>
  public class EffectAsset<T> : AssetWrapper<T>
    where T : IEffectAssetCore, new()
  {
    public EffectAsset(T core) : base(core) { }

    public Effect Effect
    {
      get
      {
        return _assetCore.Effect;
      }
    }

    public bool Allocate()
    {
      return _assetCore.Allocate();
    }

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="texture"/>.
    /// </summary>
    /// <param name="texture">The texture to be rendered.</param>
    /// <param name="texture2">Second texture to be rendered.</param>
    /// <param name="renderContext">Render context.</param>
    public void StartRender(Bitmap1 texture, RenderContext renderContext, Bitmap1 texture2 = null)
    {
      _assetCore.StartRender(texture, renderContext, texture2);
    }
  }
}

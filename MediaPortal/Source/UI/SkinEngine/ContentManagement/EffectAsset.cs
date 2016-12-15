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
using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  /// <summary>
  /// Encapsulates an effect which can render a vertex buffer with a texture.
  /// The effect gets loaded from the skin's shaders directory.
  /// </summary>
  public class EffectAsset : AssetWrapper<EffectAssetCore>
  {
    public EffectAsset(EffectAssetCore core) : base(core) { }

    public bool Allocate()
    {
      return _assetCore.Allocate();
    }

    public IDictionary<string, object> Parameters
    {
      get { return _assetCore.Parameters; }
    }

    public void StartRender(Matrix finalTransform)
    {
      _assetCore.StartRender(null, 0, finalTransform);
    }

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="texture"/> in the stream of number <code>0</code>.
    /// </summary>
    /// <param name="texture">The texture to be rendered.</param>
    /// <param name="finalTransform">Final render transformation to apply.</param>
    public void StartRender(Texture texture, Matrix finalTransform)
    {
      _assetCore.StartRender(texture, 0, finalTransform);
    }

    /// <summary>
    /// Starts the rendering of the given texture <paramref name="texture"/> in the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="texture">The texture to be rendered.</param>
    /// <param name="stream">Number of the stream to render.</param>
    /// <param name="finalTransform">Final render transformation to apply.</param>
    public void StartRender(Texture texture, int stream, Matrix finalTransform)
    {
      _assetCore.StartRender(texture, stream, finalTransform);
    }

    /// <summary>
    /// Ends the rendering of the stream of number <code>0</code>.
    /// </summary>
    public void EndRender()
    {
      _assetCore.EndRender(0);
    }

    /// <summary>
    /// Ends the rendering of the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Number of the stream to end the rendering.</param>
    public void EndRender(int stream)
    {
      _assetCore.EndRender(stream);
    }

    public void Render(Texture texture, PrimitiveBuffer vertexdata, Matrix finalTransform)
    {
      StartRender(texture, finalTransform);
      vertexdata.Render(0);
      EndRender();
    }
  }
}
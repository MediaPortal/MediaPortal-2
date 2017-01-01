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

using MediaPortal.UI.SkinEngine.ContentManagement.AssetCore;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class RenderTextureAsset : AssetWrapper<RenderTextureAssetCore>, ITextureAsset
  {
    public RenderTextureAsset(RenderTextureAssetCore core) : base(core) { }

    /// <summary>
    /// Gets the texture resource for this asset.
    /// </summary>
    public Texture Texture
    {
      get { return _assetCore.Texture; }
    }

    /// <summary>
    /// Gets the first (and only) texture surface resource.
    /// </summary>
    public Surface Surface0
    {
      get { return _assetCore.Surface0; }
    }

    /// <summary>
    /// Gets the width of this render-texture.
    /// </summary>
    public int Width
    {
      get { return _assetCore.Width; }
    }

    /// <summary>
    /// Get the height of this render-texture.
    /// </summary>
    public int Height
    {
      get { return _assetCore.Height; }
    }

    /// <summary>
    /// Gets the size of this render-texture.
    /// </summary>
    public Size Size
    {
      get { return _assetCore.Size; }
    }

    /// <summary>
    /// Gets the maximum horizontal texture coord of the actual image in the texture resource. Due
    /// to the power-of-two limitation on texture sizes the image size may differ from the texture size.
    /// </summary>
    public float MaxU
    {
      get { return _assetCore.MaxU; }
    }

    /// <summary>
    /// Gets the maximum vertical texture coord of the actual image in the texture resource. Due
    /// to the power-of-two limitation on texture sizes the image size may differ from the texture size.
    /// </summary>
    public float MaxV
    {
      get { return _assetCore.MaxV; }
    }

    /// <summary>
    /// Allocates a new render-texture with the specified size and default format.
    /// </summary>
    public void AllocateRenderTarget(int width, int height)
    {
      _assetCore.Allocate(width, height, Usage.RenderTarget, Format.A8R8G8B8);
    }

    /// <summary>
    /// Allocates a new dynamic texture with the specified size and default format.
    /// </summary>
    public void AllocateDynamic(int width, int height)
    {
      _assetCore.Allocate(width, height, Usage.Dynamic, Format.A8R8G8B8);
    }

    /// <summary>
    /// Allocates a new dynamic texture with the specified size and default format.
    /// </summary>
    public void AllocateDynamic(int width, int height, Format format)
    {
      _assetCore.Allocate(width, height, Usage.Dynamic, format);
    }

    /// <summary>
    /// Allocates a new render-texture with the specified parameters.
    /// </summary>
    public void AllocateCustom(int width, int height, Usage usage, Format format)
    {
      _assetCore.Allocate(width, height, usage, format);
    }
  }
}

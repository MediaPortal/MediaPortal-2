#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.SkinEngine.DirectX;
using SlimDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class TextureAsset : AssetWrapper<TextureAssetCore>, ITextureAsset
  {
    public TextureAsset(TextureAssetCore core) : base(core) { }

    /// <summary>
    /// Attempts to allocate the texture resource.
    /// </summary>
    public void Allocate()
    {
      _assetCore.Allocate();
    }

    /// <summary>
    /// Bind the texture to the given stream in preparation for drawing.
    /// </summary>
    public void Bind(int streamNumber)
    {
      GraphicsDevice.Device.SetTexture(streamNumber, _assetCore.Texture);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use a thumbnail or the original image.
    /// </summary>
    /// <value><c>true</c> if using thumbnail; otherwise, <c>false</c>.</value>
    public bool UseThumbnail
    {
      get { return _assetCore.UseThumbnail; }
      set { _assetCore.UseThumbnail = value; }
    }

    /// <summary>
    /// Get the Uri associated with this resource.
    /// </summary>
    public string Name
    {
      get { return _assetCore.Name; }
    }

    /// <summary>
    /// Gets the width of the texture resource.
    /// </summary>
    public int Width
    {
      get { return _assetCore.Width; }
    }

    /// <summary>
    /// Gets the height of the texture resource.
    /// </summary>
    public int Height
    {
      get { return _assetCore.Height; }
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
    /// Gets the actual SlimDX texture resource.
    /// </summary>
    public Texture Texture
    {
      get
      {
        return _assetCore.Texture;
      }
    }
  }
}

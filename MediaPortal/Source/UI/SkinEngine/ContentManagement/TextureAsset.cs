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
using MediaPortal.UI.SkinEngine.DirectX;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.ContentManagement
{
  public class TextureAsset : AssetWrapper<TextureAssetCore>, ITextureAsset
  {
    public TextureAsset(TextureAssetCore core) : base(core) { }

    /// <summary>
    /// Loads the specified texture from the file / Uri.
    /// </summary>
    public virtual void Allocate()
    {
      _assetCore.Allocate();
    }

    /// <summary>
    /// Loads the specified texture from the file / Uri asynchronously.
    /// </summary>
    public virtual void AllocateAsync()
    {
      _assetCore.AllocateAsync();
    }

    /// <summary>
    /// Allows allocation to be re-attempted after a failed texture load.
    /// </summary>
    public void ClearFailedState()
    {
      _assetCore.ClearFailedState();
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
    /// Gets or sets a value that determines dimension of a thumbnail.
    /// This property is only used in combination with <see cref="UseThumbnail"/>=true, to force a specific dimension
    /// for thumnbnails (Windows thumbnail cache offers 32, 96, 256 and 1024 size, the minimum matching size is used).
    /// </summary>
    public int ThumbnailDimension
    {
      get { return _assetCore.ThumbnailDimension; }
      set { _assetCore.ThumbnailDimension = value; }
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
    /// Gets a value indicating whether this texture could not be laoded.
    /// </summary>
    public bool LoadFailed
    {
      get { return _assetCore.LoadFailed; }
    }

    /// <summary>
    /// Gets the actual SharpDX texture resource.
    /// </summary>
    public Texture Texture
    {
      get { return _assetCore.Texture; }
    }
  }
}

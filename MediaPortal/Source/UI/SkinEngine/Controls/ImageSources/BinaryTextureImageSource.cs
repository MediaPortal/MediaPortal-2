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

using System;
using System.Security.Cryptography;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.UI.SkinEngine.Rendering;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// Image source which supports binary data of an image.
  /// </summary>
  public class BinaryTextureImageSource : TextureImageSource
  {
    #region Protected fields

    protected static SHA1 sha1 = SHA1.Create();

    protected TextureAsset _texture = null;
    protected bool _flipX = false;
    protected bool _flipY = false;

    protected byte[] _textureData;
    protected string _key;
    protected RightAngledRotation _rotation;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a <see cref="BinaryTextureImageSource"/> for the given data.
    /// </summary>
    /// <param name="textureData">Binary data to create the texture for.</param>
    /// <param name="rotation">Desired rotation for the given image.</param>
    /// <param name="key">Unique key for storing the generated texture in the <see cref="ContentManager"/>.</param>
    public BinaryTextureImageSource(byte[] textureData, RightAngledRotation rotation, string key)
    {
      _key = key;
      _textureData = textureData;
      _rotation = rotation;
    }

    /// <summary>
    /// Constructs a <see cref="BinaryTextureImageSource"/> for the given data.
    /// </summary>
    /// <param name="textureData">Binary data to create the texture for.</param>
    /// <param name="rotation">Desired rotation for the given image.</param>
    public BinaryTextureImageSource(byte[] textureData, RightAngledRotation rotation) : this(textureData, rotation, BitConverter.ToString(sha1.ComputeHash(textureData))) {}

    #endregion

    #region ImageSource implementation

    public override bool IsAllocated
    {
      get { return _texture != null && _texture.IsAllocated; }
    }

    protected override Texture Texture
    {
      get { return _texture == null ? null : _texture.Texture; }
    }

    protected override SizeF RawSourceSize
    {
      get { return (_texture != null && _texture.IsAllocated) ? new SizeF(_texture.Width, _texture.Height) : new SizeF(); }
    }

    protected override RectangleF TextureClip
    {
      get { return _texture == null ? RectangleF.Empty : new RectangleF(0, 0, _texture.MaxU, _texture.MaxV); }
    }

    public override void Allocate()
    {
      _imageContext.Rotation = _rotation;
      if (_texture == null && _textureData != null)
        _texture = ContentManager.Instance.GetTexture(_textureData, _key);
      TextureAsset texture = _texture;
      if (texture != null && !texture.IsAllocated)
        texture.Allocate();
    }

    public override void Deallocate()
    {
      base.Deallocate();
      _texture = null;
    }

    #endregion

    #region Protected methods

    protected override void FreeData()
    {
      _texture = null;
      base.FreeData();
    }

    #endregion
  }
}

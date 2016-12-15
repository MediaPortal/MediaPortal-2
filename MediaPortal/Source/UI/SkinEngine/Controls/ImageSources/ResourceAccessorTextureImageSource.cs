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

using System.IO;
using MediaPortal.Common.ResourceAccess;
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
  /// Image source which loads the data of an image from a stream.
  /// </summary>
  public class ResourceAccessorTextureImageSource : TextureImageSource
  {
    #region Protected fields

    protected TextureAsset _texture = null;
    protected bool _flipX = false;
    protected bool _flipY = false;

    protected IFileSystemResourceAccessor _resourceAccessor;
    protected Stream _stream;
    protected string _key;
    protected RightAngledRotation _rotation;

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a <see cref="ResourceAccessorTextureImageSource"/> for the given data.
    /// </summary>
    /// <param name="resourceAccessor">The resource accessor to load the texture data from.</param>
    /// <param name="rotation">Desired rotation for the given image.</param>
    public ResourceAccessorTextureImageSource(IFileSystemResourceAccessor resourceAccessor, RightAngledRotation rotation)
    {
      _key = resourceAccessor.CanonicalLocalResourcePath.Serialize();
      _resourceAccessor = resourceAccessor;
      _stream = _resourceAccessor.OpenRead();
      _rotation = rotation;
    }

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
      TextureAsset texture = _texture;
      if (texture == null && _stream != null)
        texture = _texture = ContentManager.Instance.GetTexture(_stream, _key);
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
      _stream.Close();
      _resourceAccessor.Dispose();
      base.FreeData();
    }

    #endregion
  }
}

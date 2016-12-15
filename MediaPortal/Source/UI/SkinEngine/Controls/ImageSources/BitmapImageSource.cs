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
using MediaPortal.Common.General;
using MediaPortal.UI.SkinEngine.ContentManagement;
using MediaPortal.Utilities.DeepCopy;
using SharpDX;
using SharpDX.Direct3D9;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.ImageSources
{
  /// <summary>
  /// <see cref="BitmapImageSource"/> acts as a source provider / renderer for the <see cref="Visuals.Image"/> control.
  /// Most conventional image formats are supportted. 
  /// All images are loaded synchronously, except when thumbnails are used, so it is best used for skin images.
  /// For images that require asynchronous loading (such as poster art) use MultiImage.
  /// </summary>
  /// <remarks>
  /// This class adds additional properties to the <see cref="TextureImageSource"/> and implements the loading and handling of
  /// the <see cref="Texture"/>.
  /// </remarks>
  public class BitmapImageSource : TextureImageSource
  {
    #region Protected fields

    protected AbstractProperty _uriSourceProperty;
    protected AbstractProperty _decodePixelWidthProperty;
    protected AbstractProperty _decodePixelHeightProperty;

    protected TextureAsset _texture = null;
    protected AbstractProperty _thumbnailDimensionProperty;

    protected bool _thumbnail = false;

    #endregion

    #region Ctor

    public BitmapImageSource()
    {
      Init();
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      Detach();
      Deallocate();
    }

    void Init()
    {
      _uriSourceProperty = new SProperty(typeof(string), null);
      _decodePixelWidthProperty = new SProperty(typeof(int), 0);
      _decodePixelHeightProperty = new SProperty(typeof(int), 0);
      _thumbnailDimensionProperty = new SProperty(typeof(int), 0);
    }

    void Attach()
    {
      _uriSourceProperty.Attach(OnSourceChanged);
    }

    void Detach()
    {
      _uriSourceProperty.Detach(OnSourceChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Detach();
      BitmapImageSource b = (BitmapImageSource) source;
      UriSource = b.UriSource;
      DecodePixelWidth = b.DecodePixelWidth;
      DecodePixelHeight = b.DecodePixelHeight;
      Thumbnail = b.Thumbnail;
      ThumbnailDimension = b.ThumbnailDimension;
      BorderColor = b.BorderColor;
      
      Attach();
      FreeData();
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Gets or sets the path to the image for this source.
    /// </summary>
    public string UriSource
    {
      get { return (string) _uriSourceProperty.GetValue(); }
      set { _uriSourceProperty.SetValue(value); }
    }

    public AbstractProperty UriSourceProperty
    {
      get { return _uriSourceProperty; }
    }

    /// <summary>
    /// Gets or sets a value that determines the width of the image stored in memory. 
    /// For large images this can decrease memory use and improve performance.
    /// To preserve the image's aspect ratio only set one of DecodeWidth or DecodeHeight.
    /// </summary>
    public int DecodePixelWidth
    {
      get { return (int) _decodePixelWidthProperty.GetValue(); }
      set { _decodePixelWidthProperty.SetValue(value); }
    }

    public AbstractProperty DecodePixelWidthProperty
    {
      get { return _decodePixelWidthProperty; }
    }

    /// <summary>
    /// Gets or sets a value that determines the width of the image stored in memory. 
    /// For large images this can decrease memory use and improve performance.
    /// To preserve the image's aspect ratio only set one of DecodeWidth or DecodeHeight.
    /// </summary>
    public int DecodePixelHeight
    {
      get { return (int) _decodePixelHeightProperty.GetValue(); }
      set { _decodePixelHeightProperty.SetValue(value); }
    }

    public AbstractProperty DecodePixelHeightProperty
    {
      get { return _decodePixelHeightProperty; }
    }

    /// <summary>
    /// Gets or sets a value indicating that the image will be loaded as a thumbnail.
    /// </summary>
    /// <remarks>
    /// This is not an MPF accessible property. To set it use the Thumbnail property on the owner Image control.
    /// </remarks>
    public bool Thumbnail
    {
      get { return _thumbnail; }
      set { 
        if (value != _thumbnail) 
          FreeData(); 
        _thumbnail = value; 
      }
    }
    /// <summary>
    /// Gets or sets a value that determines dimension of a thumbnail.
    /// This property is only used in combination with <see cref="Thumbnail"/>=true, to force a specific dimension
    /// for thumnbnails (Windows thumbnail cache offers 32, 96, 256 and 1024 size, the minimum matching size is used).
    /// </summary>
    public int ThumbnailDimension
    {
      get { return (int) _thumbnailDimensionProperty.GetValue(); }
      set { _thumbnailDimensionProperty.SetValue(value); }
    }

    public AbstractProperty ThumbnailDimensionProperty
    {
      get { return _thumbnailDimensionProperty; }
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
      string uri = UriSource;
      if (String.IsNullOrEmpty(uri))
      {
        if (_texture != null)
        {
          FreeData();
          FireChanged();
        }
        return;
      }
      TextureAsset texture = _texture;
      if (texture == null)
        texture = _texture = ContentManager.Instance.GetTexture(uri, DecodePixelWidth, DecodePixelHeight, Thumbnail);
      if (texture != null && !texture.IsAllocated)
      {
        if (Thumbnail)
          texture.ThumbnailDimension = ThumbnailDimension;
        texture.Allocate();
        if (texture.IsAllocated)
        {
          _imageContext.Refresh();
          FireChanged();
        }
      }
    }

    #endregion

    #region Protected methods

    protected void OnSourceChanged(AbstractProperty prop, object oldValue)
    {
      FreeData();
      FireChanged();
    }

    protected override void FreeData()
    {
      _texture = null;
      base.FreeData();
    }

    #endregion
  }
}

#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Cache;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.UI.Thumbnails;
using SlimDX;
using SlimDX.Direct3D9;

// TODO: Add support for web thumbnails? Requires changing IAsyncThumbnailGenerator

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public class TextureAssetCore : TemporaryAssetBase, IAssetCore
  {
    public event AssetAllocationHandler AllocationChanged = delegate { };

    #region Consts / Enums

    protected enum State
    {
      None,
      LoadingThumb,
      LoadingAsync,
      Loaded,
      Failed
    };

    #endregion

    #region Protected fields

    protected readonly string _textureName;
    protected Texture _texture = null;
    protected bool _useThumbnail = true;
    protected int _width = 0;
    protected int _height = 0;
    protected int _decodeWidth = 0;
    protected int _decodeHeight = 0;
    protected float _maxV;
    protected float _maxU;
    protected int _allocationSize = 0;
    protected State _state = State.None;

    protected WebClient _webClient;
    protected FileStream _fileStream;
    protected byte[] _imageBuffer;
    protected DataStream _imageDataStream;

    #endregion

    #region Ctor
    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAsset"/> class.
    /// </summary>
    /// <param name="textureName">Name of the texture.</param>
    public TextureAssetCore(string textureName) : this(textureName, 0, 0)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAsset"/> class.
    /// </summary>
    /// <param name="textureName">Name of the texture.</param>
    /// <param name="decodeWidth">Width to re-scale the texture to.</param>
    /// <param name="decodeHeight">Height to rescale the texture to.</param>
    public TextureAssetCore(string textureName, int decodeWidth, int decodeHeight)
    {
      _textureName = textureName;
      if (string.IsNullOrEmpty(_textureName))
        _state = State.Failed;
      _decodeWidth = decodeWidth;
      _decodeHeight = decodeHeight;
    }

    #endregion

    #region Public properties

    public Texture Texture
    {
      get
      {
        KeepAlive();
        return _texture;
      }
    }

    public bool UseThumbnail
    {
      get { return _useThumbnail; }
      set { _useThumbnail = value; }
    }

    public string Name
    {
      get { return _textureName; }
    }

    public int Width
    {
      get { return _width; }
    }

    public int Height
    {
      get { return _height; }
    }

    public float MaxU
    {
      get { return _maxU; }
    }

    public float MaxV
    {
      get { return _maxV; }
    }

    public bool LoadFailed
    {
      get { return _state == State.Failed; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Loads the specified texture from the file.
    /// </summary>
    public virtual void Allocate()
    {
      Allocate(false);
    }

    /// <summary>
    /// Loads the specified texture from the file asyncronously.
    /// </summary>
    public virtual void AllocateAsync()
    {
      Allocate(true);
    }

    /// <summary>
    /// Allows allocation to be re-attempted after a failed texture load.
    /// </summary>
    public void ClearFailedState()
    {
      _state = State.None;
    }

    #endregion

    #region Protected allocation helpers

        /// <summary>
    /// Loads the specified texture from the file.
    /// </summary>
    protected void Allocate(bool async)
    {
      KeepAlive();
      if (IsAllocated || _state == State.Failed || _state == State.LoadingAsync)
        return;

      // Has an Asyncronous load or web download completed?
      if (CheckAsyncCompletion())
        return;

      // Is the path a local skin file?
      string sourceFilePath = SkinContext.SkinResources.GetResourceFilePath(
        SkinResources.IMAGES_DIRECTORY + "\\" + _textureName);
      
      if (sourceFilePath == null || !File.Exists(sourceFilePath))
      {
        // Is it an absolute Uri?
        Uri uri;
        if (!Uri.TryCreate(_textureName, UriKind.Absolute, out uri))
        {
          ServiceRegistration.Get<ILogger>().Error("Cannot open texture: {0}", _textureName);
          _state = State.Failed;
          return;
        }
        else if (uri.IsFile)
          sourceFilePath = uri.LocalPath;
        else
        {
          AllocateFromWeb(uri);      
          return;
        }
      }
      
      if (UseThumbnail)
      {
        IAsyncThumbnailGenerator generator = ServiceRegistration.Get<IAsyncThumbnailGenerator>();
        ImageType imageType;
        if (generator.GetThumbnail(sourceFilePath, out _imageBuffer, out imageType))
          AllocateFromInternalBuffer();
        else
          _state = State.LoadingThumb;
      }
      else if (async)
        AllocateFromFileAsync(sourceFilePath);
      else
        AllocateFromFile(sourceFilePath);
    }

    protected bool CheckAsyncCompletion()
    {
      if (_state == State.Loaded)
      {
        if (_imageDataStream != null)
          AllocateFromInternalDataStream();
        else if (_imageBuffer != null)
          AllocateFromInternalBuffer();
        else
          throw new InvalidDataException("Unexpected data state encountered");
        return true;
      }
      return false;
    }

    protected void AllocateFromFile(string path)
    {
      ImageInformation info;
      _texture = Texture.FromFile(GraphicsDevice.Device, path, _decodeWidth, _decodeHeight, 1, Usage.None, Format.A8R8G8B8,
          Pool.Default, Filter.None, Filter.None, 0, out info);
      FinalizeAllocation(info.Width, info.Height);
    }

    protected void AllocateFromFileAsync(string path)
    {
      try {
        // Create a FileStream for Asyncronous access.
        _fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 2048, true); 
        if (_fileStream.Length == 0)
          throw new IOException("Image file is empty");

        // Prepare for file loading
        _imageBuffer = new byte[4096];
        _imageDataStream = new DataStream(_fileStream.Length, true, true);
        _state = State.LoadingAsync;

        // Read first chunk
        _fileStream.BeginRead(_imageBuffer, 0, _imageBuffer.Length, AllocateAsyncCallback, null);
      }
      catch (IOException e)
      {
        ServiceRegistration.Get<ILogger>().Error("Contentmanager: Image '{0}' could not be opened: {1}", e);
        _state = State.Failed;
        FreeTemporaryResources();
      }
    }

    protected void AllocateFromInternalDataStream()
    {
      ImageInformation info;
      _imageDataStream.Seek(0, SeekOrigin.Begin);
      _texture = Texture.FromStream(GraphicsDevice.Device, _imageDataStream, 0, _decodeWidth, _decodeHeight, 1, 
          Usage.None, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out info);
      FinalizeAllocation(info.Width, info.Height);
    }

    protected void AllocateFromInternalBuffer()
    {
      ImageInformation info;
      _texture = Texture.FromMemory(GraphicsDevice.Device, _imageBuffer, _decodeWidth, _decodeHeight, 1, Usage.None, Format.A8R8G8B8, 
        Pool.Default, Filter.None, Filter.None, 0, out info);
      FinalizeAllocation(info.Width, info.Height);
    }

    protected void AllocateFromWeb(Uri uri)
    {
      if (_webClient != null)
        _webClient.Dispose();
      
      // Try an load it as a web resource
      _webClient = new WebClient
        {
            CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
        };
      _webClient.DownloadDataCompleted += WebDownloadComplete;
      _webClient.DownloadDataAsync(uri);
      _state = State.LoadingAsync;
    }

    void FinalizeAllocation(int fileWidth, int fileHeight)
    {
      if (_texture != null)
      {
        SurfaceDescription desc = _texture.GetLevelDescription(0);
        _width = fileWidth;
        _height = fileHeight;
        _maxU = fileWidth / ((float) desc.Width);
        _maxV = fileHeight / ((float) desc.Height);
        _allocationSize = desc.Width * desc.Height * 4;
        AllocationChanged(AllocationSize);
        _state = State.None;
      }
      else
        _state = State.Failed;

      FreeTemporaryResources();
    }

    void FreeTemporaryResources()
    {
      if (_fileStream != null)
        _fileStream.Dispose();
      _fileStream = null;

      if (_imageDataStream != null)
        _imageDataStream.Dispose();
      _imageDataStream = null;

      if (_webClient != null)
        _webClient.Dispose();
      _webClient = null;

      _imageBuffer = null;
    }

    #endregion

    #region Asyncronous callbacks

    protected void WebDownloadComplete(object sender, DownloadDataCompletedEventArgs e)
    {
      FreeTemporaryResources();
      if (e.Error != null)
      {
        ServiceRegistration.Get<ILogger>().Error("Contentmanager: Failed to download {0} - {1}", _textureName, e.Error.Message);
        _state = State.Failed;
        return;
      }
      // Store image data for allocation later
      _imageBuffer = e.Result;
      _state = State.Loaded;
    }

    protected void AllocateAsyncCallback(IAsyncResult result)
    {
      int read = _fileStream.EndRead(result);

      if (read == 0)
      {
        // File read complete
        _state = State.Loaded;
        _fileStream.Dispose();
        _fileStream = null;
        _imageBuffer = null;
      }
      else
      {
        // Read next chunk
        _imageDataStream.Write(_imageBuffer, 0, read);
        _fileStream.BeginRead(_imageBuffer, 0, _imageBuffer.Length, AllocateAsyncCallback, null);
      }
    }

    #endregion

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get { return (_texture != null); }
    }

    public int AllocationSize
    {
      get { return IsAllocated ? _allocationSize : 0; }
    }

    public void Free()
    {
      if (_texture != null)
      {
        lock (_texture)
        {
          AllocationChanged(-AllocationSize);
          _texture.Dispose();
          _texture = null;
        }
      }
      FreeTemporaryResources();
    }

    #endregion

    protected void FireAllocationChanged(int allocation)
    {
      AllocationChanged(allocation);
    }
  }

  /// <summary>
  /// A version of TextureAssetCore that provides simple access to solid color textures for internal use.
  /// </summary>
  public class ColorTextureAssetCore : TextureAssetCore
  {
    protected const int TEXTURE_SIZE = 16;
    protected Color _color;

    public ColorTextureAssetCore(Color color) : base(color.ToString())
    {
      _color = color;
      _maxU = 1.0f;
      _maxV = 1.0f;
    }
    
    public override void Allocate()
    {
      if (!IsAllocated)
      {
        byte[] buffer = new byte[TEXTURE_SIZE*TEXTURE_SIZE*4];
        int offset = 0;
        while (offset < TEXTURE_SIZE*TEXTURE_SIZE*4)
        {
          buffer[offset++] = _color.R;
          buffer[offset++] = _color.G;
          buffer[offset++] = _color.B;
          buffer[offset++] = _color.A;
        }

        _texture = new Texture(GraphicsDevice.Device, TEXTURE_SIZE, TEXTURE_SIZE, 1, Usage.Dynamic, Format.A8R8G8B8,
              Pool.Default);

        DataRectangle rect = _texture.LockRectangle(0, LockFlags.Discard);
        rect.Data.Write(buffer, 0, buffer.Length);
        _texture.UnlockRectangle(0);

        _width = TEXTURE_SIZE;
        _height = TEXTURE_SIZE;

        _allocationSize = TEXTURE_SIZE*TEXTURE_SIZE*4;
        base.FireAllocationChanged(_allocationSize);
      }
    }
  }
}
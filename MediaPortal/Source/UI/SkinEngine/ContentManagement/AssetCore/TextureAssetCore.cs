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

// TODO: Add support for web thumbnails? Requires changing IThumbnailGenerator

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public class TextureAssetCore : TemporaryAssetBase, IAssetCore
  {
    #region Consts / Enums

    protected enum State
    {
      None,
      LoadingThumb,
      LoadingSync,
      LoadingAsync,
      Loaded,
      Failed
    };

    private const int ASYNCRONOUS_TIMEOUT_MS = 1000 * 60 * 2;

    #endregion

    #region Internal classes

    /// <summary>
    /// Class for holding data about the current asyncronous download operation.
    /// </summary>
    protected class WebDownloadContext
    {
      public DateTime timeStarted;
      public WebClient webClient;
      public byte[] imageBuffer;
    }

    /// <summary>
    /// Class for holding data about the current asyncronous file reading operation.
    /// </summary>
    protected class AsyncronousFileReadContext
    {
      public DateTime timeStarted;
      public FileStream fileStream;
      public byte[] imageBuffer;
      public DataStream imageDataStream;
      public bool isCancelled = false;
    }

    #endregion

    #region Protected fields

    protected readonly string _textureName;
    protected Texture _texture = null;
    protected bool _useThumbnail = true;
    protected int _thumbnailDimension = 0;
    protected int _width = 0;
    protected int _height = 0;
    protected int _decodeWidth = 0;
    protected int _decodeHeight = 0;
    protected float _maxV;
    protected float _maxU;
    protected int _allocationSize = 0;
    protected State _state = State.None;
    protected object _syncObj = new object();

    protected WebDownloadContext _webDownloadContext;
    protected AsyncronousFileReadContext _fileReadContext;

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

    #region Public properties & events

    public event AssetAllocationHandler AllocationChanged;

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

    public int ThumbnailDimension
    {
      get { return _thumbnailDimension; }
      set { _thumbnailDimension = value; }
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
      get { return CheckState(State.Failed); }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Loads the specified texture from the file.
    /// </summary>
    public virtual void Allocate()
    {
      Allocate_NoLock(false);
    }

    /// <summary>
    /// Loads the specified texture from the file asyncronously.
    /// </summary>
    public virtual void AllocateAsync()
    {
      Allocate_NoLock(true);
    }

    /// <summary>
    /// Allows allocation to be re-attempted after a failed texture load.
    /// </summary>
    public void ClearFailedState()
    {
      lock (_syncObj)
        if (_state == State.Failed)
          _state = State.None;
    }

    #endregion

    #region Protected allocation helpers

        /// <summary>
    /// Loads the specified texture from the file.
    /// </summary>
    protected void Allocate_NoLock(bool async)
    {
      KeepAlive();
      if (IsAllocated)
        return;

      string sourceFilePath;
      lock (_syncObj)
      {
        // If data has been cached asyncronously then complete allocation now
        if (_state == State.Loaded)
        {
          CompleteAsyncronousOperation();
          return;
        }
        if (_state != State.None && _state != State.LoadingThumb)
        {
          CheckAsyncTimeouts();
          return;
        }

        // Is the path a local skin file?
        sourceFilePath = SkinContext.SkinResources.GetResourceFilePath(SkinResources.IMAGES_DIRECTORY + "\\" + _textureName);

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
          if (uri.IsFile)
            sourceFilePath = uri.LocalPath;
          else
          {
            AllocateFromWeb(uri);
            return;
          }
        }
        if (!UseThumbnail)
        {
          if (async)
            AllocateFromFileAsync(sourceFilePath);
          else
            AllocateFromFile(sourceFilePath);
          return;
        }
      }
      AllocateThumbAsync_NoLock(sourceFilePath);
    }

    protected void CheckAsyncTimeouts()
    {
      lock (_syncObj)
      {
        // Check for timeouts
        if (_fileReadContext != null)
        {
          // Calling FileStream.Close should trigger read operation completion and throw a (caught) exception in the 
          //  completion callback
          if ((DateTime.Now - _fileReadContext.timeStarted).TotalMilliseconds > ASYNCRONOUS_TIMEOUT_MS)
            _fileReadContext.isCancelled = true;
        }
        if (_webDownloadContext != null)
        {
          if ((DateTime.Now - _webDownloadContext.timeStarted).TotalMilliseconds > ASYNCRONOUS_TIMEOUT_MS)
            _webDownloadContext.webClient.CancelAsync();
        }
      }

    }

    protected void CompleteAsyncronousOperation()
    {
      lock (_syncObj)
      {
        if (_fileReadContext != null && _fileReadContext.imageDataStream != null)
          AllocateFromFileDataStream(_fileReadContext);
        else if (_webDownloadContext != null && _webDownloadContext.imageBuffer != null)
          AllocateFromWebBuffer(_webDownloadContext);
        else // Set failed state and dispose context info
          FinalizeAllocation(0, 0);
      }
    }

    protected void AllocateFromFile(string path)
    {
      lock (_syncObj)
      {
        _state = State.LoadingSync;

        ImageInformation info;
        try
        {
          _texture = Texture.FromFile(GraphicsDevice.Device, path, _decodeWidth, _decodeHeight, 1, Usage.None, Format.A8R8G8B8,
              Pool.Default, Filter.None, Filter.None, 0, out info);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading texture from file '{0}'", e, path);
          return;
        }
        FinalizeAllocation(info.Width, info.Height);
      }
    }
    
    protected void AllocateFromFileAsync(string path)
    {
      lock (_syncObj)
        try
        {
          _state = State.LoadingAsync;

          // Create our asyncronous context
          _fileReadContext = new AsyncronousFileReadContext
            {
              // Log start time to detect timeouts
              timeStarted = DateTime.Now,
              // Create a FileStream for Asyncronous access.
              fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 2048, true),
              // Prepare for file loading
              imageBuffer = new byte[4096],
            };

          if (_fileReadContext.fileStream.Length == 0)
            throw new IOException("Image file is empty");

          // Prepare our SlimDX stream
          _fileReadContext.imageDataStream = new DataStream(_fileReadContext.fileStream.Length, true, true);

          // Read first chunk
          _fileReadContext.fileStream.BeginRead(_fileReadContext.imageBuffer, 0, _fileReadContext.imageBuffer.Length,
              AllocateAsyncCallback, _fileReadContext);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("TextureAsset: Image '{0}' could not be opened: {1}", _textureName, e);
          _state = State.Failed;
          DisposeFileReadContext();
        }
    }

    protected void AllocateThumbAsync_NoLock(string path)
    {
      IThumbnailGenerator generator = ServiceRegistration.Get<IThumbnailGenerator>();
      _state = State.LoadingThumb;
      generator.GetThumbnail_Async(path, _thumbnailDimension, _thumbnailDimension, ThumbnailCreated);
    }


    protected void AllocateFromWeb(Uri uri)
    {
      lock (_syncObj)
      {
        _state = State.LoadingAsync;

        _webDownloadContext = new WebDownloadContext
        {
          // Log start time to detect timeouts
          timeStarted = DateTime.Now,
          // Load texture as a web resource
          webClient = new WebClient
          {
            CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
          },
        };
        _webDownloadContext.webClient.DownloadDataCompleted += WebDownloadComplete;
        _webDownloadContext.webClient.DownloadDataAsync(uri, _webDownloadContext);
      }
    }

    protected void AllocateFromFileDataStream(AsyncronousFileReadContext stateObject)
    {
      lock (_syncObj)
      {
        ImageInformation info;
        stateObject.imageDataStream.Seek(0, SeekOrigin.Begin);
        try
        {
          _texture = Texture.FromStream(GraphicsDevice.Device, stateObject.imageDataStream, 0, _decodeWidth, _decodeHeight, 1, 
              Usage.None, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out info);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading texture from file data stream", e);
          return;
        }
        FinalizeAllocation(info.Width, info.Height);
      }
    }

    protected void AllocateFromWebBuffer(WebDownloadContext stateObject)
    {
      lock (_syncObj)
        AllocateFromBuffer(stateObject.imageBuffer);
    }

    protected void AllocateFromBuffer(byte[] data)
    {
      lock (_syncObj)
      {
        ImageInformation info;
        try
        {
          _texture = Texture.FromMemory(
              GraphicsDevice.Device, data, _decodeWidth, _decodeHeight, 1,
              Usage.None, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out info);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading texture from memory buffer", e);
          return;
        }
        FinalizeAllocation(info.Width, info.Height);
      }
    }

    protected void FinalizeAllocation(int fileWidth, int fileHeight)
    {
      lock (_syncObj)
      {
        DisposeFileReadContext();
        DisposeWebContext();

        if (_texture != null)
        {
          SurfaceDescription desc = _texture.GetLevelDescription(0);
          _width = fileWidth;
          _height = fileHeight;
          _maxU = fileWidth/((float) desc.Width);
          _maxV = fileHeight/((float) desc.Height);
          _allocationSize = desc.Width*desc.Height*4;
          AllocationChanged(AllocationSize);
          _state = State.None;
        }
        else
          _state = State.Failed;
      }
    }

    void DisposeWebContext()
    {
      lock (_syncObj)
      {
        if (_webDownloadContext != null)
        {
          if (_webDownloadContext.webClient != null)
          {
            _webDownloadContext.webClient.Dispose();
            _webDownloadContext.webClient = null;
          }
          _webDownloadContext = null;
        }
      }
    }

    void DisposeFileReadContext()
    {
      lock (_syncObj)
      {
        if (_fileReadContext != null)
        {
          if (_fileReadContext.fileStream != null)
          {
            _fileReadContext.fileStream.Dispose();
            _fileReadContext.fileStream = null;
          }
          if (_fileReadContext.imageDataStream != null)
          {
            _fileReadContext.imageDataStream.Dispose();
            _fileReadContext.imageDataStream = null;
          }
          _fileReadContext = null;
        }
      }
    }

    protected bool CheckState(State testState)
    {
      lock (_syncObj)
        return _state == testState;
    }

    #endregion

    #region Asyncronous callbacks

    protected void WebDownloadComplete(object sender, DownloadDataCompletedEventArgs e)
    {
      lock (_syncObj)
      {
        if (e.Cancelled || e.Error != null)
        {
          ServiceRegistration.Get<ILogger>().Error("TextureAsset: Failed to download {0} - {1}", _textureName, 
            e.Cancelled ? "Request timed out." : e.Error.Message);
          _state = State.Failed;
          DisposeWebContext();
          return;
        }

        // Store image data for allocation later
        WebDownloadContext state = (WebDownloadContext) e.UserState;
        state.imageBuffer = e.Result;
        _state = State.Loaded;
      }
    }

    protected void AllocateAsyncCallback(IAsyncResult result)
    {
      lock (_syncObj)
      {
        AsyncronousFileReadContext state = (AsyncronousFileReadContext) result.AsyncState;
        if (state.fileStream == null || state.imageDataStream == null)
          return;
        if (_fileReadContext.isCancelled)
        {
          ServiceRegistration.Get<ILogger>().Error("TextureAsset: Loading of image '{0}': File access timed out.", _textureName);
          _state = State.Failed;
          DisposeFileReadContext();
          return;
        }

        try
        {
          int read = state.fileStream.EndRead(result);
          if (read == 0)
          {
            // File read complete
            _state = State.Loaded;
          }
          else
          {
            // Write data our SlimDX stream
            state.imageDataStream.Write(state.imageBuffer, 0, read);
            // Read next chunk
            state.fileStream.BeginRead(state.imageBuffer, 0, state.imageBuffer.Length, AllocateAsyncCallback, state);
          }
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("TextureAsset: Image '{0}' could not be opened: {1}", _textureName, e);
          _state = State.Failed;
          DisposeFileReadContext();
        }
      }
    }

    public void ThumbnailCreated(string sourcePath, bool success, byte[] imageData, ImageType imageType)
    {
      lock (_syncObj)
      {
        if (success)
          AllocateFromBuffer(imageData);
        else
          _state = State.Failed;
      }
    }

    #endregion

    #region IAssetCore implementation

    public bool IsAllocated
    {
      get
      {
        lock (_syncObj)
          return _texture != null;
      }
    }

    public int AllocationSize
    {
      get
      {
        lock (_syncObj)
          return IsAllocated ? _allocationSize : 0;
      }
    }

    public void Free()
    {
      lock (_syncObj)
      {
        if (_texture != null)
        {
          AllocationChanged(-AllocationSize);
          _texture.Dispose();
          _texture = null;
        }
        DisposeWebContext();
        DisposeFileReadContext();
        _state = State.None;
      }
    }

    #endregion

    protected void FireAllocationChanged(int allocation)
    {
      AssetAllocationHandler dlgt = AllocationChanged;
      if (dlgt != null)
        dlgt(allocation);
    }
  }

  public class ThumbnailBinaryTextureAssetCore : TextureAssetCore
  {
    private byte[] _binaryThumbdata = null;

    public ThumbnailBinaryTextureAssetCore(byte[] binaryThumbdata, string textureName)
      : base (textureName, 0, 0)
    {
      _binaryThumbdata = binaryThumbdata;
    }

    public override void Allocate()
    {
      AllocateFromBuffer(_binaryThumbdata);
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
      lock (_syncObj)
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
        }
      }
      // Don't hold a lock while calling external code
      FireAllocationChanged(_allocationSize);
    }

    public override void AllocateAsync()
    {
      Allocate();
    }
  }
}
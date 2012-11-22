#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.DirectX;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.Network;
using SlimDX;
using SlimDX.Direct3D9;
using MediaPortal.Common.Services.ThumbnailGenerator;

// TODO: Add support for web thumbnails? Requires changing IThumbnailGenerator

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public class BaseTextureAssetCore : TemporaryAssetCoreBase, IAssetCore
  {
    protected Texture _texture = null;
    protected int _width = 0;
    protected int _height = 0;
    protected int _decodeWidth = 0;
    protected int _decodeHeight = 0;
    protected float _maxV;
    protected float _maxU;
    protected int _allocationSize = 0;

    #region Public properties & events

    public Texture Texture
    {
      get
      {
        KeepAlive();
        return _texture;
      }
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

    #endregion

    protected void AllocateFromStream_NoLock(Stream stream)
    {
      stream.Seek(0, SeekOrigin.Begin);
      Texture texture;
      ImageInformation info;
      try
      {
        texture = Texture.FromStream(GraphicsDevice.Device, stream, 0, _decodeWidth, _decodeHeight, 1, 
            Usage.None, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out info);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading texture from file data stream", e);
        return;
      }
      FinalizeAllocation(texture, info.Width, info.Height);
    }

    protected void AllocateFromBuffer_NoLock(byte[] data)
    {
      Texture texture;
      ImageInformation info;
      try
      {
        texture = Texture.FromMemory(
            GraphicsDevice.Device, data, _decodeWidth, _decodeHeight, 1,
            Usage.None, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out info);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading texture from memory buffer", e);
        return;
      }
      FinalizeAllocation(texture, info.Width, info.Height);
    }

    protected virtual void FinalizeAllocation(Texture texture, int fileWidth, int fileHeight)
    {
      lock (_syncObj)
      {
        if (texture != null)
        {
          if (_texture != null)
          {
            _texture.Dispose();
            AllocationChanged(_allocationSize);
          }
          _texture = texture;

          SurfaceDescription desc = _texture.GetLevelDescription(0);
          _width = fileWidth;
          _height = fileHeight;
          _maxU = fileWidth/((float) desc.Width);
          _maxV = fileHeight/((float) desc.Height);
          _allocationSize = desc.Width*desc.Height*4;
          AllocationChanged(_allocationSize);
        }
      }
    }

    #region IAssetCore implementation

    public event AssetAllocationHandler AllocationChanged;

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

    public virtual void Free()
    {
      lock (_syncObj)
      {
        if (_texture != null)
        {
          FireAllocationChanged(-AllocationSize);
          _texture.Dispose();
          _texture = null;
        }
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

  public class TextureAssetCore : BaseTextureAssetCore
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

    public delegate void AsyncOperationFinished(AsyncLoadOperation operation);

    private const int ASYNCHRONOUS_TIMEOUT_MS = 1000 * 60 * 2;

    #endregion

    #region Internal classes

    public abstract class AsyncLoadOperation : IDisposable
    {
      protected byte[] _imageBuffer = null;
      protected object _syncObj = new object();
      protected bool _finished = false;

      protected DateTime _startTime;
      protected AsyncOperationFinished _finishCallback;

      protected AsyncLoadOperation(AsyncOperationFinished finishCallback)
      {
        _startTime = DateTime.Now;
        _finishCallback = finishCallback;
      }

      protected void OperationCompleted(byte[] imageBuffer)
      {
        lock (_syncObj)
        {
          _finished = true;
          _imageBuffer = imageBuffer;
        }
        _finishCallback(this);
      }

      protected void OperationFailed()
      {
        lock (_syncObj)
          _finished = true;
        _finishCallback(this);
      }

      public abstract void Dispose();

      public abstract void Cancel();

      public bool Running
      {
        get { return !_finished; }
      }

      public bool DataAvailable
      {
        get { return _finished && _imageBuffer != null; }
      }

      public byte[] ImageBuffer
      {
        get { return _imageBuffer; }
      }

      public DateTime StartTime
      {
        get { return _startTime; }
      }

      public TimeSpan TimeElapsed
      {
        get { return DateTime.Now - _startTime; }
      }
    }

    public class AsyncWebLoadOperation : AsyncLoadOperation
    {
      protected WebClient _webClient;
      protected Uri _uri;

      public AsyncWebLoadOperation(Uri uri, AsyncOperationFinished finishCallback) : base(finishCallback)
      {
        _uri = uri;
        // Load texture as a web resource
        _webClient = new WebClient
          {
            CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
          };
        _webClient.DownloadDataCompleted += DownloadComplete;
        if (NetworkUtils.IsNetworkConnected())
          _webClient.DownloadDataAsync(uri, null);
        else
        {
          ServiceRegistration.Get<ILogger>().Error("AsyncStreamLoadOperation: No Network connected");
          OperationFailed();
        }
      }

      public override void Dispose()
      {
        lock (_syncObj)
        {
          if (_webClient == null) return;
          _webClient.Dispose();
          _webClient = null;
        }
      }

      public override void Cancel()
      {
        lock (_syncObj)
          _webClient.CancelAsync();
      }

      protected void DownloadComplete(object sender, DownloadDataCompletedEventArgs e)
      {
        if (e.Cancelled || e.Error != null)
        {
          ServiceRegistration.Get<ILogger>().Error("AsyncWebLoadOperation: Failed to download {0} - {1}", _uri, e.Cancelled ? "Request timed out" : e.Error.Message);
          OperationFailed();
          return;
        }
        OperationCompleted(e.Result);
      }
    }

    public class AsyncStreamLoadOperation : AsyncLoadOperation
    {
      protected bool _isCancelled = false;
      
      protected Stream _stream;
      protected string _streamName;
      protected int _position;

      public AsyncStreamLoadOperation(Stream stream, String streamName, AsyncOperationFinished finishCallback) : base(finishCallback)
      {
        _stream = stream;
        _streamName = streamName;
        if (_stream.Length == 0)
          throw new IOException("Image stream is empty");

        _imageBuffer = new byte[stream.Length];
        _position = 0;

        // Read first chunk
        _stream.BeginRead(_imageBuffer, _position, _imageBuffer.Length, AsyncLoadCallback_NoLock, null);
      }

      public override void Dispose()
      {
        lock (_syncObj)
        {
          _stream.Close();
          _stream.Dispose();
          _stream = null;
        }
      }

      public override void Cancel()
      {
        _stream.Close();
      }

      protected void AsyncLoadCallback_NoLock(IAsyncResult result)
      {
        bool completeOperation = false;
        lock (_syncObj)
        {
          if (_isCancelled)
          {
            ServiceRegistration.Get<ILogger>().Error("AsyncStreamLoadOperation: Loading of stream '{0}': Stream access timed out", _streamName);
            OperationFailed();
            return;
          }

          try
          {
            int numRead = _stream.EndRead(result);
            if (numRead == 0)
              completeOperation = true;
            else
            {
              _position += numRead;
              // Read next chunk
              _stream.BeginRead(_imageBuffer, _position, _imageBuffer.Length, AsyncLoadCallback_NoLock, null);
            }
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Error("AsyncStreamLoadOperation: Stream '{0}' could not be read: {1}", e, _streamName);
            OperationFailed();
          }
        }
        if (completeOperation)
          OperationCompleted(_imageBuffer);
      }
    }

    #endregion

    #region Protected fields

    protected readonly string _textureName;
    protected bool _useThumbnail = true;
    protected int _thumbnailDimension = 0;
    protected State _state = State.None;

    protected AsyncLoadOperation _asyncLoadOperation;

    #endregion

    #region Ctor

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAsset"/> class.
    /// </summary>
    /// <param name="textureName">Name of the texture. Can either be a file name relative
    /// to the <see cref="SkinResources.IMAGES_DIRECTORY"/>, a web URL or a file URL.</param>
    public TextureAssetCore(string textureName) : this(textureName, 0, 0)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAsset"/> class.
    /// </summary>
    /// <param name="textureName">Name of the texture. Can either be a file name relative
    /// to the <see cref="SkinResources.IMAGES_DIRECTORY"/>, a web URL or a file URL.</param>
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

    /// <summary>
    /// Name of the texture, can either be a file name relative to the <see cref="SkinResources.IMAGES_DIRECTORY"/>,
    /// a web URL or a file URL.
    /// </summary>
    public string Name
    {
      get { return _textureName; }
    }

    public bool LoadFailed
    {
      get { return CheckState(State.Failed); }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Loads the specified texture from the source file or URI.
    /// </summary>
    public virtual void Allocate()
    {
      Allocate_NoLock(false);
    }

    /// <summary>
    /// Loads the specified texture from the file asynchronously.
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
    /// Loads the specified texture from the file or from the URI.
    /// </summary>
    protected void Allocate_NoLock(bool async)
    {
      KeepAlive();
      if (IsAllocated)
        return;

      string sourceFilePath;
      lock (_syncObj)
      {
        // If data has been cached asynchronously then complete allocation now
        if (_state == State.Loaded)
          return;
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
        if (_asyncLoadOperation != null && _asyncLoadOperation.TimeElapsed.TotalMilliseconds > ASYNCHRONOUS_TIMEOUT_MS)
            _asyncLoadOperation.Cancel();
    }

    protected bool CheckState(State testState)
    {
      lock (_syncObj)
        return _state == testState;
    }

    protected void DisposeAsyncOperation()
    {
      lock (_syncObj)
        if (_asyncLoadOperation != null)
        {
          _asyncLoadOperation.Dispose();
          _asyncLoadOperation = null;
        }
    }

    protected override void FinalizeAllocation(Texture texture, int fileWidth, int fileHeight)
    {
      base.FinalizeAllocation(texture, fileWidth, fileHeight);
      _state = _texture == null ? State.Failed : State.None;
    }

    protected void AllocateFromFile(string path)
    {
      lock (_syncObj)
        _state = State.LoadingSync;

      Texture texture;
      ImageInformation info;
      try
      {
        texture = Texture.FromFile(GraphicsDevice.Device, path, _decodeWidth, _decodeHeight, 1, Usage.None, Format.A8R8G8B8,
            Pool.Default, Filter.None, Filter.None, 0, out info);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading texture from file '{0}'", e, path);
        return;
      }
      FinalizeAllocation(texture, info.Width, info.Height);
    }

    protected void AllocateFromFileAsync(string path)
    {
      lock (_syncObj)
      {
        _state = State.LoadingAsync;
        Stream stream;
        try
        {
          stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 2048, true);
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Error("TextureAssetCore: Image '{0}' could not be opened: {1}", _textureName, e);
          _state = State.Failed;
          return;
        }
        _asyncLoadOperation = new AsyncStreamLoadOperation(stream, path, AsyncOperationComplete);
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
        _state = State.LoadingAsync;
      _asyncLoadOperation = new AsyncWebLoadOperation(uri, AsyncOperationComplete);
    }

    #endregion

    #region Asynchronous callbacks

    protected void AsyncOperationComplete(AsyncLoadOperation operation)
    {
      byte[] imageBuffer;
      lock (_syncObj)
      {
        if (!operation.DataAvailable)
        {
          _state = State.Failed;
          DisposeAsyncOperation();
          return;
        }
        imageBuffer = operation.ImageBuffer;
      }
      AllocateFromBuffer_NoLock(imageBuffer);
      lock (_syncObj)
        _state = State.Loaded;
    }

    protected void ThumbnailCreated(string sourcePath, bool success, byte[] imageData, ImageType imageType)
    {
      if (success)
        AllocateFromBuffer_NoLock(imageData);
      else
        lock (_syncObj)
          _state = State.Failed;
    }

    #endregion

    public override void Free()
    {
      DisposeAsyncOperation();
      _state = State.None;
      base.Free();
    }
  }

  /// <summary>
  /// A version of <see cref="TextureAssetCore"/> that gets its texture data from a byte buffer.
  /// </summary>
  public class BinaryTextureAssetCore : TextureAssetCore
  {
    private readonly byte[] _binaryThumbdata;

    public BinaryTextureAssetCore(byte[] binaryThumbdata, string textureName) : base(textureName, 0, 0)
    {
      _binaryThumbdata = binaryThumbdata;
    }

    public override void Allocate()
    {
      AllocateFromBuffer_NoLock(_binaryThumbdata);
    }

    public override void AllocateAsync()
    {
      Allocate();
    }
  }

  /// <summary>
  /// A version of <see cref="TextureAssetCore"/> that gets its texture data from a stream.
  /// </summary>
  public class StreamTextureAssetCore : TextureAssetCore
  {
    private readonly Stream _stream;

    public StreamTextureAssetCore(Stream stream, string textureName) : base(textureName, 0, 0)
    {
      _stream = stream;
    }

    public override void Allocate()
    {
      lock (_syncObj)
        _state = State.LoadingAsync;
      _asyncLoadOperation = new AsyncStreamLoadOperation(_stream, _textureName, AsyncOperationComplete);
    }

    public override void AllocateAsync()
    {
      Allocate();
    }
  }

  /// <summary>
  /// A version of <see cref="TextureAssetCore"/> that provides simple access to solid color textures for internal use.
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
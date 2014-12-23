#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.IO;
using System.Net;
using System.Net.Cache;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.UI.SkinEngine.DirectX11;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities.Network;
using MediaPortal.Common.Services.ThumbnailGenerator;
using SharpDX.Direct2D1;
using SharpDX.WIC;
using PixelFormat = SharpDX.WIC.PixelFormat;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

// TODO: Add support for web thumbnails? Requires changing IThumbnailGenerator

namespace MediaPortal.UI.SkinEngine.ContentManagement.AssetCore
{
  public class BaseBitmapAssetCore : TemporaryAssetCoreBase, IAssetCore
  {
    /// <summary>
    /// Defines the maximum size that is used for rendering image textures. Larger images will be scaled down to fit this size.
    /// </summary>
    public const int MAX_TEXTURE_DIMENSION = 2048;

    protected Bitmap1 _bitmap = null;
    protected int _width = 0;
    protected int _height = 0;
    protected int _decodeWidth = 0;
    protected int _decodeHeight = 0;
    protected float _maxV;
    protected float _maxU;
    protected int _allocationSize = 0;

    protected static Guid WIC_PIXEL_FORMAT = PixelFormat.Format32bppPRGBA;

    #region Public properties & events

    public Bitmap1 Bitmap
    {
      get
      {
        KeepAlive();
        return _bitmap;
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
      try
      {
        if (stream.CanSeek)
          stream.Seek(0, SeekOrigin.Begin);

        // open the image file for reading
        using (var inputStream = new WICStream(GraphicsDevice11.Instance.FactoryWIC, stream))
        using (var decoder = new BitmapDecoder(GraphicsDevice11.Instance.FactoryWIC, inputStream, DecodeOptions.CacheOnLoad))
        {
          // decode the loaded image to a format that can be consumed by D2D
          var formatConverter = new FormatConverter(GraphicsDevice11.Instance.FactoryWIC);
          formatConverter.Initialize(decoder.GetFrame(0), WIC_PIXEL_FORMAT);
          _bitmap = Bitmap1.FromWicBitmap(GraphicsDevice11.Instance.Context2D1, formatConverter);
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading bitmapSource from file data stream", e);
        return;
      }
      if (_bitmap != null)
        FinalizeAllocation(_bitmap, (int)_bitmap.Size.Width, (int)_bitmap.Size.Height);
    }

    protected void AllocateFromBuffer_NoLock(byte[] data)
    {
      if (data == null || data.Length == 0)
        return;

      using (Stream dataStream = new MemoryStream(data))
        AllocateFromStream_NoLock(dataStream);
    }

    //protected Texture AllocateFromImageStream(Stream dataStream, ref ImageInformation info)
    //{
    //  Texture bitmapSource = null;
    //  FIBITMAP image = FIBITMAP.Zero;
    //  try
    //  {
    //    image = FreeImage.LoadFromStream(dataStream);

    //    // Write uncompressed data to temporary stream.
    //    using (var memoryStream = new MemoryStream())
    //    {
    //      // Scale down larger images
    //      int resizeWidth = MAX_TEXTURE_DIMENSION;
    //      int resizeHeight = MAX_TEXTURE_DIMENSION;

    //      if (_decodeWidth > 0)
    //        resizeWidth = Math.Min(_decodeWidth, MAX_TEXTURE_DIMENSION);

    //      if (_decodeHeight > 0)
    //        resizeHeight = Math.Min(_decodeHeight, MAX_TEXTURE_DIMENSION);

    //      Stream loadStream = dataStream;
    //      if (!image.IsNull)
    //      {
    //        image = ResizeImage(image, resizeWidth, resizeHeight);
    //        FreeImage.SaveToStream(image, memoryStream, FREE_IMAGE_FORMAT.FIF_BMP);
    //        loadStream = memoryStream;
    //      }

    //      loadStream.Position = 0;
    //      bitmapSource = Texture.FromStream(GraphicsDevice.Device, loadStream, (int)loadStream.Length, _decodeWidth, _decodeHeight, 1,
    //        Usage.None, Format.A8R8G8B8, Pool.Default, Filter.None, Filter.None, 0, out info);
    //    }
    //  }
    //  catch (Exception)
    //  {
    //    ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading bitmapSource from stream using FreeImage and DirectX");
    //  }
    //  finally
    //  {
    //    FreeImage.UnloadEx(ref image);
    //  }
    //  return bitmapSource;
    //}

    protected virtual void FinalizeAllocation(Bitmap1 bitmap, int fileWidth, int fileHeight)
    {
      lock (_syncObj)
      {
        if (bitmap == null)
          return;

        // Don't dispose same instance, as it would free the underlying stream
        if (_bitmap != null && _bitmap != bitmap)
        {
          _bitmap.Dispose();
          AllocationChanged(_allocationSize);
        }
        _bitmap = bitmap;

        var desc = bitmap.Size;
        _width = fileWidth;
        _height = fileHeight;
        _maxU = fileWidth / desc.Width;
        _maxV = fileHeight / desc.Height;
        _allocationSize = (int)desc.Width * (int)desc.Height * 4;
        AllocationChanged(_allocationSize);
      }
    }

    #region IAssetCore implementation

    public event AssetAllocationHandler AllocationChanged;

    public bool IsAllocated
    {
      get
      {
        lock (_syncObj)
          return _bitmap != null;
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
        if (_bitmap != null)
        {
          FireAllocationChanged(-AllocationSize);
          _bitmap.Dispose();
          _bitmap = null;
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

  public class BitmapAssetCore : BaseBitmapAssetCore
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
      protected static SystemName _localSystem = SystemName.GetLocalSystemName();
      protected WebClient _webClient;
      protected Uri _uri;

      public AsyncWebLoadOperation(Uri uri, AsyncOperationFinished finishCallback)
        : base(finishCallback)
      {
        _uri = uri;
        // Load bitmapSource as a web resource
        _webClient = new WebClient
          {
            CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
          };
        _webClient.DownloadDataCompleted += DownloadComplete;
        if (NetworkConnectionTracker.IsNetworkConnected || _localSystem.IsAddressOrAlias(uri.Host))
          _webClient.DownloadDataAsync(uri, null);
        else
        {
          ServiceRegistration.Get<ILogger>().Warn("AsyncStreamLoadOperation: No Network connected");
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
          ServiceRegistration.Get<ILogger>().Warn("AsyncWebLoadOperation: Failed to download {0} - {1}", _uri, e.Cancelled ? "Request timed out" : e.Error.Message);
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

      public AsyncStreamLoadOperation(Stream stream, String streamName, AsyncOperationFinished finishCallback)
        : base(finishCallback)
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
            ServiceRegistration.Get<ILogger>().Warn("AsyncStreamLoadOperation: Loading of stream '{0}': Stream access timed out", _streamName);
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
            ServiceRegistration.Get<ILogger>().Warn("AsyncStreamLoadOperation: Stream '{0}' could not be read", e, _streamName);
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
    /// <param name="textureName">Name of the bitmapSource. Can either be a file name relative
    /// to the <see cref="SkinResources.IMAGES_DIRECTORY"/>, a web URL or a file URL.</param>
    public BitmapAssetCore(string textureName)
      : this(textureName, 0, 0)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureAsset"/> class.
    /// </summary>
    /// <param name="textureName">Name of the bitmapSource. Can either be a file name relative
    /// to the <see cref="SkinResources.IMAGES_DIRECTORY"/>, a web URL or a file URL.</param>
    /// <param name="decodeWidth">Width to re-scale the bitmapSource to.</param>
    /// <param name="decodeHeight">Height to rescale the bitmapSource to.</param>
    public BitmapAssetCore(string textureName, int decodeWidth, int decodeHeight)
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
    /// Name of the bitmapSource, can either be a file name relative to the <see cref="SkinResources.IMAGES_DIRECTORY"/>,
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
    /// Loads the specified bitmapSource from the source file or URI.
    /// </summary>
    public virtual void Allocate()
    {
      Allocate_NoLock(false);
    }

    /// <summary>
    /// Loads the specified bitmapSource from the file asynchronously.
    /// </summary>
    public virtual void AllocateAsync()
    {
      Allocate_NoLock(true);
    }

    /// <summary>
    /// Allows allocation to be re-attempted after a failed bitmapSource load.
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
    /// Loads the specified bitmapSource from the file or from the URI.
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
            ServiceRegistration.Get<ILogger>().Warn("Cannot open bitmapSource: {0}", _textureName);
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

    protected override void FinalizeAllocation(Bitmap1 bitmap, int fileWidth, int fileHeight)
    {
      base.FinalizeAllocation(bitmap, fileWidth, fileHeight);
      _state = _bitmap == null ? State.Failed : State.None;
    }

    protected void AllocateFromFile(string path)
    {
      lock (_syncObj)
        _state = State.LoadingSync;

      try
      {
        using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 2048, true))
          AllocateFromStream_NoLock(stream);
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Error loading bitmapSource from file '{0}'", e, path);
      }
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
          ServiceRegistration.Get<ILogger>().Warn("TextureAssetCore: Image '{0}' could not be opened: {1}", _textureName, e);
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
  /// A version of <see cref="TextureAssetCore"/> that gets its bitmapSource data from a byte buffer.
  /// </summary>
  public class BinaryBitmapAssetCore : TextureAssetCore
  {
    private readonly byte[] _binaryThumbdata;

    public BinaryBitmapAssetCore(byte[] binaryThumbdata, string textureName)
      : base(textureName, 0, 0)
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
  /// A version of <see cref="TextureAssetCore"/> that gets its bitmapSource data synchronously from a stream.
  /// </summary>
  public class SynchronousStreamBitmapAssetCore : BitmapAssetCore
  {
    private readonly Stream _stream;

    public SynchronousStreamBitmapAssetCore(Stream stream, string textureName)
      : base(textureName, 0, 0)
    {
      _stream = stream;
    }

    public override void Allocate()
    {
      AllocateFromStream_NoLock(_stream);
    }

    public override void AllocateAsync()
    {
      Allocate();
    }
  }

  /// <summary>
  /// A version of <see cref="TextureAssetCore"/> that gets its bitmapSource data from a stream.
  /// </summary>
  public class StreamBitmapAssetCore : BitmapAssetCore
  {
    private readonly Stream _stream;

    public StreamBitmapAssetCore(Stream stream, string textureName)
      : base(textureName, 0, 0)
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

}

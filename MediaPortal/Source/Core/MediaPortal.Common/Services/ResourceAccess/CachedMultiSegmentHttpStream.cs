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
using System.Collections.Generic;
using System.IO;
using System.Net;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.Network;
using System.Threading;
using MediaPortal.Utilities.SystemAPI;
using UPnP.Infrastructure.Utils;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// <see cref="CachedMultiSegmentHttpStream"/> implements a read-only Stream for accessing HTTP sources that support Range requests.
  /// Multiple MemoryStream caches are created internally to buffer access to different stream positions.
  /// </summary>
  public class CachedMultiSegmentHttpStream : Stream
  {
    #region Constants

    public const int CHUNK_SIZE = 4 * 512 * 1024; // 4 * 512 kB chunk size
    public const int NUM_READAHEAD_CHUNKS = 2;
    public const int MAX_NUM_CACHES = 10;

    #endregion

    #region Private & protected fields

    protected readonly string _url;
    protected readonly IPAddress _localIpAddress;
    protected readonly long _length;
    protected long _position = 0;
    private IList<HttpRangeChunk> _chunkCache = new List<HttpRangeChunk>();

    #endregion

    #region Internal classes

    /// <summary>
    /// Represents one single chunk of a complete file which is requested via an HTTP URL.
    /// This chunk is multithreading-safe and can asynchronously collect its data.
    /// </summary>
    protected class HttpRangeChunk : IDisposable
    {
      protected const string PRODUCT_VERSION = "MediaPortal/2.0";

      /// <summary>
      /// Timeout for HTTP Range request in ms.
      /// </summary>
      public const int HTTP_RANGE_REQUEST_TIMEOUT = 2000;

      /// <summary>
      /// Sometimes, the HTTP request is broken, so try multiple times.
      /// </summary>
      public const int MAX_NUM_TRIES = 4;

      // Stream data
      protected MemoryStream _cacheStream = new MemoryStream(CHUNK_SIZE); // null if disposed
      protected readonly long _startIndex; // Inclusive
      protected readonly long _endIndex; // Exclusive
      protected readonly string _url;
      protected readonly IPAddress _localIpAddress;

      // Data for async request control
      protected readonly object _syncObject = new object();
      protected readonly ManualResetEvent _readyEvent = new ManualResetEvent(false);
      protected volatile bool _filled = false;
      protected volatile Exception _exception = null;
      protected volatile HttpWebRequest _pendingRequest = null;
      protected int _numTries = 0;

      protected static string _userAgent;

      static HttpRangeChunk()
      {
        _userAgent = WindowsAPI.GetOsVersionString() + " HTTP/1.1 " + PRODUCT_VERSION;
      }

      public HttpRangeChunk(long start, long end, long wholeStreamLength, string url, IPAddress localIpAddress)
      {
        _startIndex = start;
        _endIndex = Math.Min(wholeStreamLength, end);
        _url = url;
        _localIpAddress = localIpAddress;
        Load_Async();
      }

      #region IDisposable Member

      public void Dispose()
      {
        lock (_syncObject) // Lock to avoid interference with async response retrieval
        {
          _cacheStream.Dispose();
          _cacheStream = null;
        }
        HttpWebRequest request = _pendingRequest;
        _pendingRequest = null;
        if (request != null)
          request.Abort();
        _filled = false;
        _exception = new ObjectDisposedException("HttpRangeChunk");
        _readyEvent.Set();
        _readyEvent.Close();
      }

      #endregion

      /// <summary>
      /// Initializes the HTTP range request asynchronously.
      /// </summary>
      protected void Load_Async()
      {
        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(_url);
        NetworkUtils.SetLocalEndpoint(request, _localIpAddress);
        request.Method = "GET";
        request.KeepAlive = true;
        request.AllowAutoRedirect = true;
        request.UserAgent = _userAgent;
        request.AddRange(_startIndex, _endIndex - 1);

        IAsyncResult result = request.BeginGetResponse(OnResponseReceived, request);
        NetworkHelper.AddTimeout(request, result, HTTP_RANGE_REQUEST_TIMEOUT);
        _pendingRequest = request;
        _numTries++;
      }

      private void OnResponseReceived(IAsyncResult ar)
      {
        HttpWebResponse response = null;
        bool finished = false;
        lock (_syncObject) // Lock to avoid interference with disposal
          try
          {
            try
            {
              response = (HttpWebResponse) ((HttpWebRequest) ar.AsyncState).EndGetResponse(ar);
              if (_cacheStream != null) // If disposed, the stream is null
              {
                using (Stream source = response.GetResponseStream())
                {
                  const int MAX_BUF_SIZE = 64535;
                  int numRead = (int) (_endIndex - _startIndex);
                  byte[] buffer = new byte[Math.Min(numRead, MAX_BUF_SIZE)];
                  int readBytes;
                  while (numRead > 0 && (readBytes = source.Read(buffer, 0, Math.Min(buffer.Length, numRead))) > 0)
                  {
                    _cacheStream.Write(buffer, 0, readBytes);
                    numRead -= readBytes;
                  }
                  _filled = true;
                }
              }
              finished = true;
            }
            catch (Exception e)
            {
              if (_numTries >= MAX_NUM_TRIES)
              {
                ServiceRegistration.Get<ILogger>().Error("HttpRangeChunk: Error receiving data from {0}", e, _url);
                _exception = e;
                finished = true;
              }
              else if (_cacheStream != null) // If disposed, the stream is null
                Load_Async();
              return;
            }
          }
          finally
          {
            if (finished)
            {
              _pendingRequest = null;
              if (_cacheStream != null) // If disposed, the stream is null
                _readyEvent.Set();
            }
            if (response != null)
              response.Close();
          }
      }

      #region Public members

      /// <summary>
      /// Start index of this chunk. Inclusive.
      /// </summary>
      public long StartIndex
      {
        get { return _startIndex; }
      }

      /// <summary>
      /// End index of this chunk. Exclusive.
      /// </summary>
      public long EndIndex
      {
        get { return _endIndex; }
      }

      /// <summary>
      /// Event which is signalled when this chunk finished collecting its data.
      /// When this event is signalled, either <see cref="IsFilled"/> is <c>true</c> or <see cref="IsErroneous"/> is <c>true</c>.
      /// </summary>
      public ManualResetEvent ReadyEvent
      {
        get { return _readyEvent; }
      }

      /// <summary>
      /// Returns <c>true</c> when this chunk is filled. The <see cref="ReadyEvent"/> will be set when this property is set.
      /// </summary>
      public bool IsFilled
      {
        get { return _filled; }
      }

      /// <summary>
      /// Returns <c>true</c> when this chunk is filled. The <see cref="ReadyEvent"/> will be set when this property is set.
      /// </summary>
      public bool IsErroneous
      {
        get { return _exception != null; }
      }

      /// <summary>
      /// Reads data from this data chunk. If the chunk's data have not been collected yet, this method blocks until
      /// the data is availab.e
      /// </summary>
      /// <param name="position">Absolute position in the whole data, has to be bigger or equal to <see cref="StartIndex"/> and
      /// lower than <see cref="EndIndex"/>.</param>
      /// <param name="buffer">Buffer to write the data to.</param>
      /// <param name="offset">Offset in the buffer to begin writing.</param>
      /// <param name="count">Desired number of bytes to be read. The actual number of bytes read might be lower than this parameter.</param>
      /// <returns>Number of bytes actually read from this data chunk.</returns>
      public int Read(long position, byte[] buffer, int offset, int count)
      {
        if (_cacheStream == null)
          return 0;
        _readyEvent.WaitOne(HTTP_RANGE_REQUEST_TIMEOUT);
        if (_exception != null)
          throw new IOException(string.Format("Error receiving HTTP range {0}-{1}", _startIndex, _endIndex - 1), _exception);
        _cacheStream.Position = position - _startIndex;
        return _cacheStream.Read(buffer, offset, count);
      }

      #endregion
    }
    
    #endregion

    #region Construction & destruction

    /// <summary>
    /// Creates a <see cref="CachedMultiSegmentHttpStream"/> that can read data over HTTP.
    /// </summary>
    /// <param name="url">URL to retrieve the contents from.</param>
    /// <param name="localIpAddress">Local IP address to help the underlaying HTTP request modules finding the correct outgoing
    /// network interface. If this parameter is set, the outgoing request will be made on the network interface this local IP address
    /// is bound to. If this parameter is set to <c>null</c>, the interface will be choosen automatically, which can fail.</param>
    /// <param name="streamLength">Expected length of the resource stream.</param>
    public CachedMultiSegmentHttpStream(string url, IPAddress localIpAddress, long streamLength)
    {
      _length = streamLength;
      _url = url;
      _localIpAddress = localIpAddress;
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      if (_chunkCache == null)
        return;
      if (disposing)
      {
        foreach (HttpRangeChunk chunk in _chunkCache)
          chunk.Dispose();
      }
      _chunkCache = null;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Always returns true.
    /// </summary>
    public override bool CanRead { get { return true; } }

    /// <summary>
    /// Always returns true.
    /// </summary>
    public override bool CanSeek { get { return true; } }

    /// <summary>
    /// Always returns false.
    /// </summary>
    public override bool CanWrite { get { return false; } }

    /// <summary>
    /// Gets the length of the resource at the given URL.
    /// </summary>
    public override long Length
    {
      get { return _length; }
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public override long Position
    {
      get { return _position; }
      set
      {
        if (value < 0 || value > _length) throw new IOException();
        if (value == _position) return; // Already there
        _position = value;
      }
    }

    #endregion

    #region Base overrides

    public override long Seek(long offset, SeekOrigin origin)
    {
      if (_chunkCache == null)
        throw new ObjectDisposedException("CachedMultiSegmentHttpStream");
      long newPos = _position;
      switch (origin)
      {
        case SeekOrigin.Begin:
          newPos = offset;
          break;
        case SeekOrigin.Current:
          newPos += offset;
          break;
        case SeekOrigin.End:
          newPos = _length + offset;
          break;
        default:
          break;
      }
      if (newPos < 0 || newPos > _length)
        throw new IOException();
      return _position = newPos;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (buffer == null)
        throw new ArgumentNullException();
      if (offset + count > buffer.Length)
        throw new ArgumentException();
      if (offset < 0 || count < 0)
        throw new ArgumentOutOfRangeException();
      if (_chunkCache == null)
        throw new ObjectDisposedException("CachedMultiSegmentHttpStream");
      if (_position >= _length)
        return 0;
      HttpRangeChunk chunk = ProvideReadAhead(_position, NUM_READAHEAD_CHUNKS);
      if (chunk == null)
        throw new IOException();

      int numRead = chunk.Read(_position, buffer, offset, count);

      _position += numRead;
      return numRead;
    }

    /// <summary>
    /// Always throws <see cref="NotSupportedException"/>.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Always throws <see cref="NotSupportedException"/>.
    /// </summary>
    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    /// Always throws <see cref="NotSupportedException"/>.
    /// </summary>
    public override void Flush()
    {
      throw new NotSupportedException();
    }

    #endregion

    #region Protected members

    /// <summary>
    /// Tries to find a chunk in the chunk list which covers the given <paramref name="start"/> position.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="addChunkIfNotExists">If set to <c>true</c>, the requested chunk will be added to the cache if it doesn't exist yet.</param>
    /// <param name="result">Returns the chunk or <c>null</c>, if no chunk was found for the given <paramref name="start"/> position.
    /// The returned chunk might not be filled yet.</param>
    /// <returns><c>true</c> if chunk was found.</returns>
    protected bool GetMatchingChunk(long start, bool addChunkIfNotExists, out HttpRangeChunk result)
    {
      if (start >= 0 && start < _length)
      {
        for (int i = _chunkCache.Count - 1; i >= 0; i--)
        {
          HttpRangeChunk chunk = _chunkCache[i];
          if (chunk.StartIndex <= start && chunk.EndIndex > start)
          {
            // Reorder LRU cache
            _chunkCache.RemoveAt(i);
            if (chunk.IsErroneous)
                // Retry if a chunk was erroneous
              break;
            _chunkCache.Add(chunk);
            result = chunk;
            return true;
          }
        }
        if (addChunkIfNotExists)
        {
          AddChunk(start, out result);
          return true;
        }
      }
      result = null;
      return false;
    }

    /// <summary>
    /// Adds a new chunk to the cache and returns it.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="chunk">Returns the cache.</param>
    /// <returns><c>true</c> if successful.</returns>
    private void AddChunk(long start, out HttpRangeChunk chunk)
    {
      if (_chunkCache.Count == MAX_NUM_CACHES)
      {
        // Remove chunk which hasn't been used the longest time
        _chunkCache[0].Dispose();
        _chunkCache.RemoveAt(0);
      }
      _chunkCache.Add(chunk = new HttpRangeChunk(start, start + CHUNK_SIZE, Length, _url, _localIpAddress));
    }

    protected HttpRangeChunk ProvideReadAhead(long position, int numReadaheadChunks)
    {
      HttpRangeChunk result;
      if (!GetMatchingChunk(position, true, out result))
        return null;
      HttpRangeChunk currentChunk = result;
      for (int i = numReadaheadChunks - 1; i > 0; i--)
        if (!GetMatchingChunk(currentChunk.EndIndex, true, out currentChunk))
          break;
      return result;
    }

    #endregion
  }
}

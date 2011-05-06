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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

namespace MediaPortal.Core.Services.MediaManagement
{
  /// <summary>
  /// <see cref="CachedMultiSegmentHttpStream"/> implements a read-only Stream for accessing HTTP sources that support Range requests.
  /// Multiple MemoryStream caches are created internally to buffer access to different stream positions.
  /// </summary>
  public class CachedMultiSegmentHttpStream : Stream
  {
    #region Constants

    //TODO find good cache size/count combinations
    private const int CACHE_SIZE = 512 * 1024; // 512 kB Cache
    private const int CACHE_COUNT = 10; // max. 10 * 512 kB

    #endregion

    #region Variables

    private readonly string _url;
    private readonly long _length;
    private long _position;
    private long _totalBytesRead;
    private int _totalReads;
    readonly List<HttpStreamCache> _cache = new List<HttpStreamCache>(CACHE_COUNT);

    #endregion

    #region Internal classes

    internal class HttpStreamCache: IDisposable
    {
      public MemoryStream CacheStream = new MemoryStream(CACHE_SIZE);
      public long StartIndex;
      public long EndIndex;
      public bool Filled;
      public long StreamLength;
      public String Url;

      // FIXME: This method is only a workaround for missing long support in AddRange.
      //        It should be removed when the project is switched to .Net 4.
      protected void SetRequestLongRange(HttpWebRequest request, long start, long end)
      {
        MethodInfo method = typeof(WebHeaderCollection).GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        const string key = "Range";
        long safeEnd = Math.Min(StreamLength - 1, end);
        string val = string.Format("bytes={0}-{1}", start, safeEnd);
        method.Invoke(request.Headers, new object[] { key, val });
      }

      /// <summary>
      /// Initializes the cache and retrieves the data from HTTP request.
      /// </summary>
      public void InitCache()
      {
        byte[] buffer = new byte[64000];
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Url);
        SetRequestLongRange(request, StartIndex, EndIndex);
        HttpWebResponse result = (HttpWebResponse)request.GetResponse();
        using (Stream stream = result.GetResponseStream())
        {
          using (BinaryReader sr = new BinaryReader(stream))
          {
            BinaryWriter sw = new BinaryWriter(CacheStream);
            int readBytes = sr.Read(buffer, 0, buffer.Length);
            while (readBytes > 0)
            {
              sw.Write(buffer, 0, readBytes);
              readBytes = sr.Read(buffer, 0, buffer.Length);
            }
          }
          Filled = true;
        }
      }

      #region IDisposable Member

      public void Dispose()
      {
        CacheStream.Dispose();
      }

      #endregion
    }
    
    #endregion

    #region Constructor

    /// <summary>
    /// Creates a <see cref="CachedMultiSegmentHttpStream"/> that can read data over HTTP.
    /// </summary>
    public CachedMultiSegmentHttpStream(string url, long streamLength)
    {
      _length = streamLength;
      _url = url;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the total number of bytes read from the stream.
    /// </summary>
    public long TotalBytesRead { get { return _totalBytesRead; } }

    /// <summary>
    /// Gets the total number of Reads() performed over the stream.
    /// </summary>
    public long TotalReads { get { return _totalReads; } }

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
      get
      {
        return _length;
      }
    }

    /// <summary>
    /// Gets the current position in the stream.
    /// </summary>
    public override long Position
    {
      get
      {
        return _position;
      }
      set
      {
        if (value < 0) throw new ArgumentException();
        if (value == _position) return; // already there
        _position = value;
      }
    }

    #endregion

    #region Overrides

    /// <summary>
    ///   Sets the Position in the stream.
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
      switch (origin)
      {
        case SeekOrigin.Begin:
          _position = offset;
          break;
        case SeekOrigin.Current:
          _position += offset;
          break;
        case SeekOrigin.End:
          _position = Length + offset;
          break;
        default:
          break;
      }
      return Position;
    }

    /// <summary>
    /// Reads data from the stream. Requests to this method will be buffered using a MemoryStream.
    /// </summary>
    /// <param name='buffer'>the buffer into which to insert the data that is read.</param>
    /// <param name='offset'>the offset into the buffer, at which to begin to insert the data that is read.</param>
    /// <param name='count'>the number of bytes to read.</param>
    /// <returns>The number of bytes actually read.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
      int n = 0;
      HttpStreamCache cache;
      if (!GetMatchingCache(_position, _position + count, out cache))
        if (!AddCache(_position, out cache))
          return 0;

      if (cache.Filled)
      {
        cache.CacheStream.Position = _position - cache.StartIndex;
        n = cache.CacheStream.Read(buffer, offset, count);
      }
      
      _totalBytesRead += n;
      _totalReads++;
      _position += n;
      return n;
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

    #region Members

    /// <summary>
    /// Tries to find a matching cache stream in the cache list.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <param name="cache">Returns the cache or null.</param>
    /// <returns>True if cache was found.</returns>
    private bool GetMatchingCache(long start, long end, out HttpStreamCache cache)
    {
      cache = null;
      foreach (HttpStreamCache httpStreamCache in _cache)
      {
        if (httpStreamCache.StartIndex <= start && httpStreamCache.EndIndex > end)
        {
          cache = httpStreamCache;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Adds a new stream to the cache and returns it.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="cache">Returns the cache.</param>
    /// <returns>True if successful.</returns>
    private bool AddCache(long start, out HttpStreamCache cache)
    {
      if (_cache.Count == CACHE_COUNT)
      {
        _cache[0].Dispose();
        _cache.RemoveAt(0);
      }
      cache = new HttpStreamCache { StartIndex = start, EndIndex = start + CACHE_SIZE - 1, StreamLength = Length, Url = _url };
      cache.InitCache();
      _cache.Add(cache);
      return true;
    }

    #endregion
  }
}
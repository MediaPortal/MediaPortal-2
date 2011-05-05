// ------------------------------------------------------------------
//
// This is a read-only System.IO.Stream that reads documents from an
// HTTP endpoint.  Via the HTTP Range option, it supports seek
// operations over HTTP.
//
// build it with:
//   csc /target:library /doc:HttpRangeStream.XML /out:HttpRangeStream.dll /r:System.DLL HttpRangeStream.cs
//
// last saved:
// Time-stamp: <2010-March-30 15:24:09>
// ------------------------------------------------------------------
//
// Copyright (c) 2010 by Dino Chiesa
// All rights reserved!
//
// ------------------------------------------------------------------
// This code module is licensed under the Microsoft Public License.
//
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the software,
// you accept this license. If you do not accept the license, do not use the
// software.
//
// 1. Definitions
//
// The terms "reproduce," "reproduction," "derivative works," and "distribution"
// have the same meaning here as under U.S. copyright law.  A "contribution" is
// the original software, or any additions or changes to the software.  A
// "contributor" is any person that distributes its contribution under this
// license.  "Licensed patents" are a contributor's patent claims that read
// directly on its contribution.
//
//
// 2. Grant of Rights
//
// (A) Copyright Grant- Subject to the terms of this license, including the
// license conditions and limitations in section 3, each contributor grants you a
// non-exclusive, worldwide, royalty-free copyright license to reproduce its
// contribution, prepare derivative works of its contribution, and distribute its
// contribution or any derivative works that you create.
//
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose of
// its contribution in the software or derivative works of the contribution in the
// software.
//
//
// 3. Conditions and Limitations
//
// (A) No Trademark License- This license does not grant you rights to use any
// contributors' name, logo, or trademarks.
//
// (B) If you bring a patent claim against any contributor over patents that you
// claim are infringed by the software, your patent license from such contributor
// to the software ends automatically.
//
// (C) If you distribute any portion of the software, you must retain all
// copyright, patent, trademark, and attribution notices that are present in the
// software.
//
// (D) If you distribute any portion of the software in source code form, you may
// do so only under this license by including a complete copy of this license with
// your distribution. If you distribute any portion of the software in compiled or
// object code form, you may only do so under a license that complies with this
// license.
//
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may have
// additional consumer rights under your local laws which this license cannot
// change. To the extent permitted under your local laws, the contributors exclude
// the implied warranties of merchantability, fitness for a particular purpose and
// non-infringement.
//
// ------------------------------------------------------------------
//


using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;

namespace MediaPortal.Core.Services.MediaManagement
{
  /// <summary>
  ///   A Stream that supports Read() and Seek() over HTTP.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The <see href='http://tools.ietf.org/html/rfc2616'>HTTP
  ///     protocol</see> supports a Range header, that allows clients to
  ///     direct servers an offset within the bytestream for the
  ///     specified resource, that the client would like to read. This
  ///     class takes advantage of the Range header, to provide <see
  ///     cref='System.IO.Stream'/> that supports <c>Read()</c> and
  ///     <c>Sepek()</c> operations efficiently over HTTP.
  ///   </para>
  /// </remarks>
  public class HttpRangeStream : Stream
  {
    private readonly string _url;
    private readonly long _length;
    private long _position;
    private long _totalBytesRead;
    private int _totalReads;
    //TODO find good cache size/count combinations
    private const int CACHE_SIZE = 512*1024; // 512 kB Cache
    private const int CACHE_COUNT = 10; // max. 10 * 512 kB

    internal class HttpStreamCache: IDisposable
    {
      public MemoryStream CacheStream = new MemoryStream(CACHE_SIZE);
      public long StartIndex;
      public long EndIndex;
      public bool Filled;
      public long StreamLength;
      public String Url;

      protected void SetRequestLongRange(HttpWebRequest request, long start, long end)
      {
        MethodInfo method = typeof(WebHeaderCollection).GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        const string key = "Range";
        long safeEnd = Math.Min(StreamLength - 1, end);
        string val = string.Format("bytes={0}-{1}", start, safeEnd);
        method.Invoke(request.Headers, new object[] { key, val });
      }

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

    readonly List<HttpStreamCache> _cache = new List<HttpStreamCache>();

    /// <summary>
    ///   Creates a Stream that can read data over HTTP.
    /// </summary>
    public HttpRangeStream(string url, long streamLength)
    {
      _length = streamLength;
      _url = url;
    }

    /// <summary>
    ///   Retrieves the total number of bytes read from the stream.
    /// </summary>
    public long TotalBytesRead { get { return _totalBytesRead; } }

    /// <summary>
    ///   Retrieves the total number of Reads() performed over the stream.
    /// </summary>
    public long TotalReads { get { return _totalReads; } }

    /// <summary>
    ///   Always returns true.
    /// </summary>
    public override bool CanRead { get { return true; } }

    /// <summary>
    ///   Always returns true.
    /// </summary>
    public override bool CanSeek { get { return true; } }

    /// <summary>
    ///   Always returns false.
    /// </summary>
    public override bool CanWrite { get { return false; } }


    /// <summary>
    ///   Returns the length of the resource at the given URL.
    /// </summary>
    public override long Length
    {
      get
      {
        return _length;
      }
    }


    /// <summary>
    ///   Returns the current position in the stream.
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

    private bool GetMatchingCache(long start, long end,  out HttpStreamCache cache)
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

    private bool AddCache(long start, out HttpStreamCache cache)
    {
      cache = new HttpStreamCache {StartIndex = start, EndIndex = start + CACHE_SIZE - 1, StreamLength = Length, Url = _url};
      cache.InitCache();
      _cache.Add(cache);
      if (_cache.Count >= CACHE_COUNT)
      {
        _cache[0].Dispose();
      _cache.RemoveAt(0);
      }
      return true;
    }

    /// <summary>
    ///   Reads data from the stream.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This method reads data from the stream into the provided
    ///     buffer.  It copies the data read into the buffer starting at
    ///     the given offset. It attempts to read <paramref name="count"/>
    ///     bytes.
    ///   </para>
    /// </remarks>
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
    ///   Always throws <see cref="NotSupportedException"/>.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    ///   Always throws <see cref="NotSupportedException"/>.
    /// </summary>
    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    /// <summary>
    ///   Always throws <see cref="NotSupportedException"/>.
    /// </summary>
    public override void Flush()
    {
      throw new NotSupportedException();
    }
  }
}
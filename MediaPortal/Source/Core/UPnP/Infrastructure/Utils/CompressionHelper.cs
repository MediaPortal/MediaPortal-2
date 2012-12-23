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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using HttpServer;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// <see cref="CompressionHelper"/> provides methods to compress and decrompress Streams using gzip compression.
  /// </summary>
  static class CompressionHelper
  {
    /// <summary>
    /// Gets a dictionary of supported compression methods.
    /// </summary>
    public readonly static IDictionary<string, IDeCompressor> Compressors = new Dictionary<string, IDeCompressor>
      {
        {"deflate", new DeflateCompression()},
        {"gzip", new GzipCompression()}
      };

    private const int BUFFER_SIZE = 1024;

    // For debugging purpose
    private const bool DISABLE_COMPRESSION = false;

    /// <summary>
    /// Checks if the current request header for "accept-encoding" contains a supported compression method.
    /// </summary>
    /// <param name="acceptEncoding">Request header</param>
    /// <param name="preferredEncoding">Gets the preferred encoding</param>
    /// <returns><c>true</c> if compression is supported.</returns>
    public static bool IsSupportedCompression(string acceptEncoding, out string preferredEncoding)
    {
      preferredEncoding = null;
      if (string.IsNullOrEmpty(acceptEncoding))
        return false;
      string lowerEncoding = acceptEncoding.ToLowerInvariant();
      foreach (string encoding in Compressors.Keys)
      {
        if (lowerEncoding.Contains(encoding))
        {
          preferredEncoding = encoding;
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Gets the list of supported encoding methods.
    /// </summary>
    /// <returns>Comma separated list of encodings</returns>
    public static string GetAcceptedEncodings()
    {
      return string.Join(", ", Compressors.Keys);
    }

    /// <summary>
    /// Compresses the content of the given <paramref name="inputStream"/> and returns a compressed Stream.
    /// </summary>
    /// <param name="encoding">The key of the <see cref="Compressors"/> collection.</param>
    /// <param name="inputStream">Input Stream to read from. Reading will start from current <see cref="Stream.Position"/>.</param>
    /// <returns>Compressed stream</returns>
    private static Stream Compress(string encoding, Stream inputStream)
    {
      MemoryStream compressed = new MemoryStream();
      Stream compressedStream;
      IDeCompressor compressor = Compressors[encoding];

      using (compressedStream = compressor.CreateCompressionStream(compressed))
      {
        byte[] buffer = new byte[BUFFER_SIZE];
        int nRead;
        while ((nRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
          compressedStream.Write(buffer, 0, nRead);
      }
      compressed.Position = 0;
      return compressed;
    }

    /// <summary>
    /// Decompresses the content of the given <paramref name="inputStream"/> and returns an uncompressed Stream.
    /// </summary>
    /// <param name="encoding">The key of the <see cref="Compressors"/> collection.</param>
    /// <param name="inputStream">Input Stream to read from. Reading will start from current <see cref="Stream.Position"/>.</param>
    /// <returns>Unompressed stream</returns>
    public static Stream Decompress(string encoding, Stream inputStream)
    {
      MemoryStream decompressed = new MemoryStream();
      Stream compressedStream;
      IDeCompressor compressor = Compressors[encoding];

      using (compressedStream = compressor.CreateDeCompressionStream(inputStream))
      {
        byte[] buffer = new byte[BUFFER_SIZE];
        int nRead;
        while ((nRead = compressedStream.Read(buffer, 0, buffer.Length)) > 0)
          decompressed.Write(buffer, 0, nRead);
      }
      decompressed.Position = 0;
      return decompressed;
    }

    /// <summary>
    /// Helper method to handle decompression of <see cref="WebResponse"/> streams. If the header contains a "Content-Encoding" for type "gzip", the
    /// returned stream will automatically be decompressed.
    /// </summary>
    /// <param name="response">Response to process</param>
    /// <returns>Decompressed stream.</returns>
    public static Stream Decompress(WebResponse response)
    {
      string preferredEncoding;
      bool decompress = IsSupportedCompression(response.Headers.Get("CONTENT-ENCODING"), out preferredEncoding);
      return decompress ? Decompress(preferredEncoding, response.GetResponseStream()) : response.GetResponseStream();
    }

    /// <summary>
    /// Helper method to write the contents of <paramref name="inputStream"/> to the <paramref name="response"/> body. Depending on the
    /// accepted encodings (<paramref name="acceptEncoding"/> argument), the result will be compressed or not.
    /// </summary>
    /// <param name="acceptEncoding">The Request's accepted encodings</param>
    /// <param name="response">Response to be written</param>
    /// <param name="inputStream">Input stream</param>
    public static void WriteCompressedStream(string acceptEncoding, IHttpResponse response, MemoryStream inputStream)
    {
      string preferredEncoding;
      bool compress = IsSupportedCompression(acceptEncoding, out preferredEncoding);
      
      byte[] buffer;
      if (!compress || DISABLE_COMPRESSION)
      {
        buffer = inputStream.ToArray();
        response.ContentLength = buffer.Length;
        response.Body.Write(buffer, 0, buffer.Length);
        return;
      }

      using (MemoryStream compressedStream = (MemoryStream) Compress(preferredEncoding, inputStream))
        buffer = compressedStream.ToArray();

      response.AddHeader("Content-Encoding", preferredEncoding);
      // If there were multiple methods supported, we need to indicate the varying header
      if (acceptEncoding != preferredEncoding)
        response.AddHeader("Vary", "Accept-Encoding");

      response.ContentLength = buffer.Length;
      response.Body.Write(buffer, 0, buffer.Length);
    }
  }

  interface IDeCompressor
  {
    Stream CreateCompressionStream(Stream targetStream);
    Stream CreateDeCompressionStream(Stream targetStream);
  }

  public class GzipCompression : IDeCompressor
  {
    public Stream CreateCompressionStream(Stream targetStream)
    {
      return new GZipStream(targetStream, CompressionMode.Compress, true);
    }

    public Stream CreateDeCompressionStream(Stream targetStream)
    {
      return new GZipStream(targetStream, CompressionMode.Decompress, true);
    }
  }

  public class DeflateCompression : IDeCompressor
  {
    public Stream CreateCompressionStream(Stream targetStream)
    {
      return new DeflateStream(targetStream, CompressionMode.Compress, true);
    }

    public Stream CreateDeCompressionStream(Stream targetStream)
    {
      return new DeflateStream(targetStream, CompressionMode.Decompress, true);
    }
  }
}

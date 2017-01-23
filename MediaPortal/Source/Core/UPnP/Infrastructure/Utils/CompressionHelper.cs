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

// For debugging purpose
//#define DISABLE_COMPRESSION

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Linq;
using HttpServer;

namespace UPnP.Infrastructure.Utils
{
  /// <summary>
  /// <see cref="CompressionHelper"/> provides methods to compress and decompress streams using different compression methods (<see cref="Compressors"/>).
  /// </summary>
  static class CompressionHelper
  {
    /// <summary>
    /// Gets a list of supported compression methods. The order of the compressors is important, the first matching compressor will be preferably used.
    /// </summary>
    public readonly static IList<IDeCompressor> Compressors = new List<IDeCompressor>
      {
        new DeflateCompression(),
        new GzipCompression()
      };

    private const int BUFFER_SIZE = 1024;

    /// <summary>
    /// Checks if the current request header for "accept-encoding" contains a supported compression method and returns the preferred
    /// compressor class to be used according to that request header.
    /// </summary>
    /// <param name="acceptEncoding">Request header of "Accept-Encoding".</param>
    /// <returns>The preferred compressor.</returns>
    public static IDeCompressor CheckSupportedCompression(string acceptEncoding)
    {
      if (string.IsNullOrEmpty(acceptEncoding))
        return null;
      string[] lowerEncodings = acceptEncoding.ToLowerInvariant().Split(new[] {','});
      foreach (IDeCompressor compressor in Compressors)
      {
        string encoding = compressor.EncodingName;
        if (lowerEncodings.Any(e => e.Trim() == encoding))
          return compressor;
      }
      return null;
    }

    /// <summary>
    /// Gets the list of supported encoding methods.
    /// </summary>
    /// <returns>Comma separated list of encodings.</returns>
    public static string GetAcceptedEncodings()
    {
      return Compressors.Select(c => c.EncodingName).Aggregate((c1, c2) => c1 + ", " + c2);
    }

    /// <summary>
    /// Compresses the content of the given <paramref name="inputStream"/> and returns a compressed stream.
    /// </summary>
    /// <param name="compressor">The <see cref="IDeCompressor"/> to be used.</param>
    /// <param name="inputStream">Input Stream to read from. Reading will start from current <see cref="Stream.Position"/>.</param>
    /// <returns>Compressed stream.</returns>
    private static Stream Compress(IDeCompressor compressor, Stream inputStream)
    {
      MemoryStream compressed = new MemoryStream();
      Stream compressedStream;

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
    /// Decompresses the content of the given <paramref name="inputStream"/> and returns an uncompressed stream.
    /// </summary>
    /// <param name="compressor">The <see cref="IDeCompressor"/> to be used.</param>
    /// <param name="inputStream">Input Stream to read from. Reading will start from current <see cref="Stream.Position"/>.</param>
    /// <returns>Uncompressed stream.</returns>
    public static Stream Decompress(IDeCompressor compressor, Stream inputStream)
    {
      MemoryStream decompressed = new MemoryStream();
      Stream compressedStream;

      using (compressedStream = compressor.CreateDecompressionStream(inputStream))
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
    /// Helper method to handle decompression of <see cref="WebResponse"/> streams. If the header contains a "Content-Encoding" that is supported by one of
    /// the <see cref="Compressors"/>, the returned stream will automatically be decompressed.
    /// </summary>
    /// <param name="response">Response to process.</param>
    /// <returns>Decompressed stream.</returns>
    public static Stream Decompress(WebResponse response)
    {
      IDeCompressor compressor = CheckSupportedCompression(response.Headers.Get("CONTENT-ENCODING"));
      Stream responseStream = response.GetResponseStream();
      return compressor == null ? responseStream : Decompress(compressor, responseStream);
    }

    /// <summary>
    /// Helper method to write the contents of <paramref name="inputStream"/> to the <paramref name="response"/> body. Depending on the
    /// accepted encodings (<paramref name="acceptEncoding"/> argument), the result will be compressed or not.
    /// </summary>
    /// <param name="acceptEncoding">The Request's accepted encodings.</param>
    /// <param name="response">Response to be written.</param>
    /// <param name="inputStream">The input stream the will be written into the Response.</param>
    public static void WriteCompressedStream(string acceptEncoding, IHttpResponse response, MemoryStream inputStream)
    {
      IDeCompressor compressor = CheckSupportedCompression(acceptEncoding);
      
      byte[] buffer;
#if !DISABLE_COMPRESSION
      if (compressor == null)
      {
#endif
        buffer = inputStream.ToArray();
        response.ContentLength = buffer.Length;
        response.Body.Write(buffer, 0, buffer.Length);
        return;
#if !DISABLE_COMPRESSION
      }
#endif

      using (MemoryStream compressedStream = (MemoryStream) Compress(compressor, inputStream))
        buffer = compressedStream.ToArray();

      response.AddHeader("Content-Encoding", compressor.EncodingName);
      // If there were multiple methods supported, we need to indicate the varying header.
      if (acceptEncoding != compressor.EncodingName)
        response.AddHeader("Vary", "Accept-Encoding");

      response.ContentLength = buffer.Length;
      response.Body.Write(buffer, 0, buffer.Length);
    }
  }

  /// <summary>
  /// Internal interface to use different stream compression methods.
  /// </summary>
  interface IDeCompressor
  {
    /// <summary>
    /// Gets the encoding name that is used inside http headers (i.e. "gzip", "deflate").
    /// </summary>
    string EncodingName { get; }

    /// <summary>
    /// Creates a compression stream for the <paramref name="targetStream"/>.
    /// </summary>
    /// <param name="targetStream">Stream that will be compressed.</param>
    /// <returns>Compression stream.</returns>
    Stream CreateCompressionStream(Stream targetStream);

    /// <summary>
    /// Creates a decompression stream for the <paramref name="targetStream"/>.
    /// </summary>
    /// <param name="targetStream">Stream that will be decompressed.</param>
    /// <returns>Decompression stream.</returns>
    Stream CreateDecompressionStream(Stream targetStream);
  }

  /// <summary>
  /// Compressor which uses a GZIP compression.
  /// </summary>
  public class GzipCompression : IDeCompressor
  {
    public string EncodingName { get { return "gzip"; } }

    public Stream CreateCompressionStream(Stream targetStream)
    {
      return new GZipStream(targetStream, CompressionMode.Compress, true);
    }

    public Stream CreateDecompressionStream(Stream targetStream)
    {
      return new GZipStream(targetStream, CompressionMode.Decompress, true);
    }
  }

  /// <summary>
  /// Compressor which uses a DEFLATE compression.
  /// </summary>
  public class DeflateCompression : IDeCompressor
  {
    public string EncodingName { get { return "deflate"; } }

    public Stream CreateCompressionStream(Stream targetStream)
    {
      return new DeflateStream(targetStream, CompressionMode.Compress, true);
    }

    public Stream CreateDecompressionStream(Stream targetStream)
    {
      return new DeflateStream(targetStream, CompressionMode.Decompress, true);
    }
  }
}
